using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Orchestrates a single sales call as a small state machine over
    /// <see cref="CallStage"/>. It owns the prospect's runtime state and the
    /// scorecard, exposes the choices available right now, and raises events the
    /// UI subscribes to (transcript lines, meter changes, call end). It carries no
    /// Unity dependencies so the whole call loop is unit-testable.
    /// </summary>
    public class CallSession
    {
        public ScenarioDefinition Scenario { get; }
        public ProspectState Prospect { get; }
        public Scorecard Score { get; }
        public Lane Lane { get; }

        public CallStage Stage => Prospect.Stage;
        public CallOutcome Outcome { get; private set; } = CallOutcome.InProgress;
        public bool IsOver => Stage == CallStage.Completed || Outcome == CallOutcome.HungUp;

        /// <summary>Effects from the player's BDR attributes; never null.</summary>
        public CharacterModifiers Modifiers { get; }

        private readonly DialogueLibrary _library;
        private List<DialogueChoice> _choices = new();

        private readonly List<AbilityDefinition> _abilities = new();
        private readonly Dictionary<string, int> _abilityUses = new();
        private float _negotiationPrimer;

        // ---- events the UI listens to ---------------------------------------
        /// <summary>System narration: stage banners, deal summaries, etc.</summary>
        public event Action<string> NarratorLine;
        /// <summary>Echo of the line the rep (player) just chose.</summary>
        public event Action<string> RepLine;
        /// <summary>What the prospect says back.</summary>
        public event Action<string> ProspectLine;
        /// <summary>Meters / stage changed — refresh the HUD.</summary>
        public event Action StateChanged;
        /// <summary>The set of selectable choices changed.</summary>
        public event Action<IReadOnlyList<DialogueChoice>> ChoicesChanged;
        /// <summary>The call ended with the given outcome.</summary>
        public event Action<CallOutcome> CallEnded;
        /// <summary>Usable abilities or their remaining uses changed.</summary>
        public event Action AbilitiesChanged;

        public CallSession(ScenarioDefinition scenario) : this(scenario, null) { }

        public CallSession(ScenarioDefinition scenario, CharacterModifiers modifiers)
        {
            Scenario = scenario;
            Modifiers = modifiers ?? CharacterModifiers.Neutral;
            Prospect = new ProspectState(scenario != null ? scenario.prospect : null);
            Score = new Scorecard();
            Lane = scenario != null ? scenario.PrimaryLane : null;
            _library = new DialogueLibrary(scenario);
        }

        public IReadOnlyList<DialogueChoice> CurrentChoices => _choices;

        public IReadOnlyList<AbilityDefinition> Abilities => _abilities;
        public int AbilityUsesLeft(string abilityId) =>
            _abilityUses.TryGetValue(abilityId, out var n) ? n : 0;

        /// <summary>Set the active abilities available this call (call before <see cref="Begin"/>).</summary>
        public void ConfigureAbilities(IReadOnlyList<AbilityDefinition> abilities)
        {
            _abilities.Clear();
            _abilityUses.Clear();
            if (abilities == null) return;
            foreach (var a in abilities)
            {
                _abilities.Add(a);
                _abilityUses[a.Id] = a.UsesPerCall;
            }
        }

        /// <summary>Fire an ability if it has uses left.</summary>
        public bool UseAbility(AbilityDefinition ability)
        {
            if (ability == null || IsOver || AbilityUsesLeft(ability.Id) <= 0) return false;
            _abilityUses[ability.Id]--;
            ApplyAbility(ability);
            AbilitiesChanged?.Invoke();
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Starts the call. Emits the opening narration and first choices.</summary>
        public void Begin()
        {
            if (Scenario == null || Scenario.prospect == null)
            {
                Narrate("No scenario loaded — nothing to simulate.");
                Outcome = CallOutcome.Rejected;
                Finish();
                return;
            }

            // The gatekeeper (if any) is handled by the Gatekeeper Gauntlet before
            // this scene loads, so the call always opens with the decision-maker.
            Prospect.Stage = CallStage.Opening;
            Prospect.AdjustTrust(Modifiers.TrustBonus); // your Charisma warms the open
            Narrate($"— Connected with {Prospect.Profile.companyName} —");
            if (!string.IsNullOrEmpty(Scenario.briefing))
                Narrate($"Briefing: {Scenario.briefing}");
            ProspectSays($"This is {FirstName()}.");

            StateChanged?.Invoke();
            RefreshChoices();
        }

        /// <summary>Apply a one-shot call event (a random opening situation), narrated to the log.</summary>
        public void ApplyEvent(CallEvent callEvent)
        {
            if (callEvent == null || IsOver) return;
            Prospect.AdjustTrust(callEvent.TrustDelta);
            Prospect.AdjustPatience(callEvent.PatienceDelta);
            Narrate($"— {callEvent.Title} —  {callEvent.Narration}");
            StateChanged?.Invoke();
        }

        /// <summary>Apply the rep's selected choice and advance the call.</summary>
        public void Choose(DialogueChoice choice)
        {
            if (choice == null || IsOver) return;

            if (!string.IsNullOrEmpty(choice.Text))
                RepSays(choice.Text);

            CallStage stageWhenChosen = Prospect.Stage;

            if (choice.IsRateOffer)
                ResolveRateOffer(choice);
            else
                ApplyEffects(choice);

            // Closing line determines how the call is logged.
            if (stageWhenChosen == CallStage.Closing && !choice.IsRateOffer)
                DetermineOutcomeFromClose(choice);

            // Patience exhausted at any point ends the call abruptly.
            if (Prospect.IsBailing && stageWhenChosen != CallStage.Wrap)
            {
                Outcome = CallOutcome.HungUp;
                Narrate("*click* — they hung up.");
                Finish();
                return;
            }

            if (choice.EndsCall || choice.AdvanceTo == CallStage.Completed)
            {
                Prospect.Stage = CallStage.Completed;
                Finish();
                return;
            }

            if (choice.AdvanceTo.HasValue)
                EnterStage(choice.AdvanceTo.Value);

            StateChanged?.Invoke();
            RefreshChoices();
        }

        // ---- internals ------------------------------------------------------

        private void ApplyEffects(DialogueChoice choice)
        {
            float scoreAdd = choice.ScoreValue;
            if (scoreAdd > 0f) scoreAdd += Modifiers.BonusFor(choice.Category); // your stats reward good plays
            Score.Add(choice.Category, scoreAdd);

            Prospect.AdjustTrust(choice.TrustDelta);
            Prospect.AdjustPatience(ModifiedPatience(choice.PatienceDelta));

            if (choice.ResolvesObjection.HasValue)
                Prospect.ResolveActiveObjection();

            if (!string.IsNullOrEmpty(choice.Response))
                ProspectSays(choice.Response);
        }

        // Resilience softens patience losses; gains pass through unchanged.
        private float ModifiedPatience(float delta) =>
            delta < 0f ? delta * Modifiers.PatienceDrainMultiplier : delta;

        private void ResolveRateOffer(DialogueChoice choice)
        {
            // Rate offers always lead into the close.
            choice.AdvanceTo = CallStage.Closing;

            if (Lane == null)
            {
                Prospect.AgreeToDeal(choice.OfferRatePerMile);
                Score.Add(ScoreCategory.Negotiation, 8f);
                ProspectSays("Sure, that works.");
                return;
            }

            float sensitivity = Prospect.Profile != null ? Prospect.Profile.priceSensitivity : 0.5f;
            float negotiationSkill = Modifiers.NegotiationSkill + _negotiationPrimer;
            _negotiationPrimer = 0f; // primer is consumed by the offer
            NegotiationResult result = NegotiationEngine.Evaluate(
                Lane, choice.OfferRatePerMile, Prospect.Trust, sensitivity, negotiationSkill);

            Prospect.AdjustTrust(result.TrustDelta);
            Prospect.AdjustPatience(ModifiedPatience(result.PatienceDelta));
            ProspectSays(result.Reason);

            float target = Scenario != null ? Scenario.targetWeeklyMargin : 0f;

            switch (result.Status)
            {
                case NegotiationStatus.Accepted:
                {
                    Prospect.AgreeToDeal(choice.OfferRatePerMile);
                    float weekly = result.WeeklyMargin;
                    float negScore;
                    if (result.MarginPerMile <= 0f) negScore = 4f;            // won but no margin
                    else if (target > 0f && weekly >= target) negScore = 18f; // won and hit the bar
                    else negScore = 12f;                                      // won with some margin
                    Score.Add(ScoreCategory.Negotiation, negScore);
                    Narrate($"Agreed at ${choice.OfferRatePerMile:0.00}/mi → " +
                            $"~${weekly:0}/wk gross margin.");
                    break;
                }
                case NegotiationStatus.Countered:
                {
                    Prospect.AgreeToDeal(result.CounterRatePerMile);
                    float weekly = Lane.WeeklyMargin(result.CounterRatePerMile);
                    float negScore = Lane.MarginPerMile(result.CounterRatePerMile) > 0f ? 9f : 3f;
                    Score.Add(ScoreCategory.Negotiation, negScore);
                    Narrate($"Settled on their counter of ${result.CounterRatePerMile:0.00}/mi → " +
                            $"~${weekly:0}/wk gross margin.");
                    break;
                }
                default: // Rejected
                {
                    Score.Add(ScoreCategory.Negotiation, 2f);
                    Narrate("No deal on rate — the lane stays with the incumbent.");
                    break;
                }
            }
        }

        private void ApplyAbility(AbilityDefinition ability)
        {
            RepSays($"( uses {ability.Name} )");
            switch (ability.EffectType)
            {
                case AbilityEffectType.RestorePatience:
                    Prospect.AdjustPatience(ability.Magnitude);
                    Narrate("You read the room and ease off — the prospect relaxes.");
                    break;
                case AbilityEffectType.BoostTrust:
                    Prospect.AdjustTrust(ability.Magnitude);
                    Narrate("You land a genuine connection — trust ticks up.");
                    break;
                case AbilityEffectType.ClearObjection:
                    if (Prospect.ActiveObjection.HasValue)
                    {
                        Score.Add(ScoreCategory.ObjectionHandling, 6f);
                        Prospect.ResolveActiveObjection();
                        Narrate("You reframe the objection and defuse it.");
                    }
                    else
                    {
                        Narrate("Nothing to reframe right now.");
                    }
                    break;
                case AbilityEffectType.NegotiationPrimer:
                    _negotiationPrimer += ability.Magnitude;
                    Narrate("You set a strong anchor for the next number.");
                    break;
            }
        }

        private void DetermineOutcomeFromClose(DialogueChoice choice)
        {
            bool deal = Prospect.DealAgreed;
            Outcome = (deal, choice.Quality) switch
            {
                (true, ChoiceQuality.Strong) => CallOutcome.WonCommitment,
                (true, ChoiceQuality.Adequate) => CallOutcome.WonTrial,
                (true, _) => CallOutcome.NoSaleFollowUp,   // had the deal, fumbled the ask
                (false, ChoiceQuality.Weak) => CallOutcome.Rejected,
                (false, _) => CallOutcome.NoSaleFollowUp,
            };
        }

        private void EnterStage(CallStage stage)
        {
            Prospect.Stage = stage;

            switch (stage)
            {
                case CallStage.Opening:
                    Narrate("— Connected —");
                    ProspectSays($"This is {FirstName()}, what can I do for you?");
                    break;
                case CallStage.Discovery:
                    Narrate("— Discovery —");
                    break;
                case CallStage.ValuePitch:
                    Narrate("— Pitch —");
                    break;
                case CallStage.ObjectionHandling:
                {
                    ObjectionType type = PendingObjection() ?? ObjectionType.NotInterested;
                    Prospect.RaiseObjection(type);
                    Narrate("— Objection —");
                    ProspectSays(ObjectionCatalog.Get(type).ProspectLine);
                    break;
                }
                case CallStage.Negotiation:
                    Narrate("— Negotiation —");
                    if (Lane != null)
                        Narrate($"They're open to a number on {Lane.label} " +
                                $"(they pay ~${Lane.currentRatePerMile:0.00}/mi today).");
                    break;
                case CallStage.Closing:
                    Narrate("— Close —");
                    break;
                case CallStage.Wrap:
                    Narrate("— Wrap —");
                    break;
            }
        }

        private void Finish()
        {
            if (Outcome == CallOutcome.InProgress)
                Outcome = CallOutcome.NoSaleFollowUp;
            _choices = new List<DialogueChoice>();
            ChoicesChanged?.Invoke(_choices);
            StateChanged?.Invoke();
            CallEnded?.Invoke(Outcome);
        }

        private void RefreshChoices()
        {
            CallStage next = NextStage(Prospect.Stage);
            _choices = _library.GetChoices(Prospect.Stage, Prospect, next);
            ChoicesChanged?.Invoke(_choices);
        }

        /// <summary>The linear stage flow, branching only around the gatekeeper and objection.</summary>
        public CallStage NextStage(CallStage current)
        {
            switch (current)
            {
                case CallStage.Gatekeeper:
                    return CallStage.Opening;
                case CallStage.Opening:
                    return CallStage.Discovery;
                case CallStage.Discovery:
                    return CallStage.ValuePitch;
                case CallStage.ValuePitch:
                    return PendingObjection().HasValue ? CallStage.ObjectionHandling : CallStage.Negotiation;
                case CallStage.ObjectionHandling:
                    return CallStage.Negotiation;
                case CallStage.Negotiation:
                    return CallStage.Closing;
                case CallStage.Closing:
                    return CallStage.Wrap;
                default:
                    return CallStage.Completed;
            }
        }

        /// <summary>First likely objection the prospect hasn't had handled yet, if any.</summary>
        private ObjectionType? PendingObjection()
        {
            var list = Prospect.Profile != null ? Prospect.Profile.likelyObjections : null;
            if (list == null) return null;
            foreach (var o in list)
                if (!Prospect.HasHandled(o))
                    return o;
            return null;
        }

        private string FirstName()
        {
            string name = Prospect.Profile != null ? Prospect.Profile.contactName : null;
            if (string.IsNullOrEmpty(name)) return "speaking";
            int space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name;
        }

        private void Narrate(string s) => NarratorLine?.Invoke(s);
        private void RepSays(string s) => RepLine?.Invoke(s);
        private void ProspectSays(string s) => ProspectLine?.Invoke(s);
    }
}
