using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// Persistent, lazily-created singleton that survives scene loads. Holds the
    /// scenario the player picked in the menu and the most recent call report, and
    /// owns scene transitions between the menu and the call floor.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[GameManager]");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>Scenario selected in the menu and played on the call floor.</summary>
        public ScenarioDefinition SelectedScenario { get; private set; }

        /// <summary>Report from the most recently completed call (for results screens).</summary>
        public CallReport LastReport { get; set; }

        /// <summary>The player's persistent BDR, loaded from disk. Null until one is created.</summary>
        public BDRCharacter Profile { get; private set; }

        public bool HasProfile => Profile != null;

        /// <summary>Whether the in-progress call counts toward the career (vs. a practice call).</summary>
        public bool IsCareerCall { get; private set; }

        /// <summary>Transient message shown once on the menu (e.g. a week-end summary).</summary>
        public string CareerFlash { get; set; }

        /// <summary>Where meetings return to — the menu, or a map hub if you set it there.</summary>
        public string HubScene { get; set; } = SceneNames.MainMenu;

        /// <summary>The local (Texas) account being visited, so its stage can advance on a win.</summary>
        public string PendingClientId { get; set; } = "";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (Profile == null)
                Profile = SaveSystem.Load();

            // Apply scene atmosphere (ambient + fog) on every load, including this one.
            SceneManager.sceneLoaded += OnSceneLoaded;
            Atmosphere.Apply();
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode) => Atmosphere.Apply();

        /// <summary>Create a new BDR, persist it, and make it the active profile.</summary>
        public void CreateProfile(BDRCharacter character)
        {
            Profile = character;
            SaveSystem.Save(character);
        }

        /// <summary>Write the current profile to disk (call after XP gains, edits, etc.).</summary>
        public void SaveProfile()
        {
            if (Profile != null) SaveSystem.Save(Profile);
        }

        /// <summary>Wipe the saved profile (for a "new game" / reset).</summary>
        public void DeleteProfile()
        {
            Profile = null;
            SaveSystem.Delete();
        }

        public void GoToCharacterCreate() => SceneManager.LoadScene(SceneNames.CharacterCreate);

        public void StartScenario(ScenarioDefinition scenario) => StartPractice(scenario);

        /// <summary>Run a one-off practice call (no career calls consumed, no quota credit).</summary>
        public void StartPractice(ScenarioDefinition scenario)
        {
            IsCareerCall = false;
            PendingClientId = ""; // remote/phone — not a local account visit
            SelectedScenario = scenario;
            LastReport = null;
            EnterMeeting(scenario);
        }

        /// <summary>
        /// Spend a call from today's budget on a fresh cold prospect (scaled by level,
        /// week, and the Lead Intelligence upgrade) and head into the meeting. Shared by
        /// the menu and the CRM dashboard.
        /// </summary>
        public void TakeColdCall()
        {
            var c = Profile;
            if (c == null || !CareerSystem.HasCallsLeft(c)) return;
            CareerSystem.ConsumeCall(c);
            SaveProfile();
            int week = CareerSystem.Week(c.career.day);
            int difficulty = Mathf.Clamp(1 + (c.level - 1) / 2 + (week - 1) + EconomySystem.LeadQuality(c), 1, 10);
            int seed = unchecked(System.Environment.TickCount + c.callsMade * 7 + c.career.day);
            StartCareerCall(ProspectGenerator.Generate(difficulty, seed));
        }

        /// <summary>Run a career call that counts toward the day/week.</summary>
        public void StartCareerCall(ScenarioDefinition scenario)
        {
            IsCareerCall = true;
            PendingClientId = ""; // remote/phone — not a local account visit
            SelectedScenario = scenario;
            LastReport = null;
            EnterMeeting(scenario);
        }

        // A gatekeeper means you must win the duel before reaching the decision-maker.
        private void EnterMeeting(ScenarioDefinition scenario)
        {
            if (scenario != null && scenario.gatekeeperPresent)
                SceneManager.LoadScene(SceneNames.GatekeeperDuel);
            else
                SceneManager.LoadScene(SceneNames.CallFloor);
        }

        /// <summary>Called when the player wins the gatekeeper duel — proceed to the call.</summary>
        public void OnGatekeeperCleared(bool flawless = false)
        {
            if (Profile != null)
            {
                Profile.gatekeepersBeaten++;
                if (flawless) Profile.flawlessGatekeepers++;
                SaveProfile();
            }
            SceneManager.LoadScene(SceneNames.CallFloor);
        }

        /// <summary>Called when the player loses the gatekeeper duel — back to the hub.</summary>
        public void OnGatekeeperFailed()
        {
            if (IsCareerCall)
                CareerFlash = "A gatekeeper shut you down — no meeting today.";
            ReturnToHub();
        }

        // ---- commute platformer (the Mario-style level you beat to REACH a city) ----

        /// <summary>The city the commute platformer is travelling to; entered on a win.</summary>
        public string TravelDestinationCityId { get; private set; } = "";

        /// <summary>
        /// Travel to a city from the map. If its city is already unlocked, fast-travel
        /// straight in; otherwise play the commute platformer first — winning unlocks the
        /// city (fast-travel thereafter) and drops you into it.
        /// </summary>
        public void TravelToCity(string clientId)
        {
            if (Profile != null && TerritorySystem.IsCityUnlocked(Profile, clientId))
            {
                GoToCity(clientId);
                return;
            }
            TravelDestinationCityId = clientId ?? "";
            HubScene = SceneNames.Texas;
            LastReport = null;
            SceneManager.LoadScene(SceneNames.Platformer);
        }

        /// <summary>Legacy entry: play a platformer level, then a meeting (kept for callers).</summary>
        public void StartTravel(ScenarioDefinition scenario, bool career)
        {
            IsCareerCall = career;
            SelectedScenario = scenario;
            TravelDestinationCityId = "";
            LastReport = null;
            SceneManager.LoadScene(SceneNames.Platformer);
        }

        /// <summary>Won the commute: unlock + enter the destination city, or fall back to a meeting.</summary>
        public void OnTravelComplete()
        {
            if (!string.IsNullOrEmpty(TravelDestinationCityId))
            {
                if (Profile != null) TerritorySystem.UnlockCity(Profile, TravelDestinationCityId);
                SaveProfile();
                var dest = TravelDestinationCityId;
                TravelDestinationCityId = "";
                GoToCity(dest);
                return;
            }
            EnterMeeting(SelectedScenario);
        }

        /// <summary>Didn't make it — back to the map.</summary>
        public void OnTravelFailed()
        {
            if (!string.IsNullOrEmpty(TravelDestinationCityId))
            {
                CareerFlash = "You didn't make the commute — try the route again.";
                TravelDestinationCityId = "";
                GoToTexas();
                return;
            }
            if (IsCareerCall)
                CareerFlash = "You didn't make it to the client — no meeting today.";
            ReturnToHub();
        }

        public void ReturnToMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

        /// <summary>Return to whichever hub the current meeting was launched from.</summary>
        public void ReturnToHub() =>
            SceneManager.LoadScene(string.IsNullOrEmpty(HubScene) ? SceneNames.MainMenu : HubScene);

        /// <summary>The territory whose city we're currently visiting (themes the city + its name).</summary>
        public string ActiveCityId { get; set; } = "";

        public void GoToCity()
        {
            HubScene = SceneNames.City;
            SceneManager.LoadScene(SceneNames.City);
        }

        /// <summary>Fast-travel into a specific territory's drivable city.</summary>
        public void GoToCity(string clientId)
        {
            ActiveCityId = clientId ?? "";
            HubScene = SceneNames.City;
            SceneManager.LoadScene(SceneNames.City);
        }

        public void GoToOffice()
        {
            HubScene = SceneNames.Office;
            SceneManager.LoadScene(SceneNames.Office);
        }

        public void GoToTexas()
        {
            HubScene = SceneNames.Texas;
            SceneManager.LoadScene(SceneNames.Texas);
        }

        /// <summary>Open the freight desk (your book of business). Returns to the hub you came from.</summary>
        public void GoToFreightDesk() => SceneManager.LoadScene(SceneNames.FreightDesk);

        /// <summary>
        /// End the workday once: advance the career meta-loop (which bumps the day and
        /// resets calls) and then tick the freight book on the new day. Used by both the
        /// menu's "End Day" and the freight desk's "Advance Day" so a day means one thing.
        /// </summary>
        public CareerDayResult EndBusinessDay(out FreightDayDigest freight, out EconomyDigest economy,
            out string marketEvent)
        {
            freight = default;
            economy = default;
            marketEvent = null;
            if (Profile == null) return default;
            var result = CareerSystem.EndDay(Profile);
            var rng = new System.Random(unchecked(System.Environment.TickCount ^ (Profile.career.day * 92821)));
            freight = FreightSystem.OnDayAdvanced(Profile, Profile.career.day, rng);
            economy = EconomySystem.OnDayAdvanced(Profile, Profile.career.day, rng, result.WeekEnded);
            OutreachSystem.OnDayAdvanced(Profile); // refill the day's touches, cool neglected leads
            if (result.WeekEnded) marketEvent = MarketEvents.RollWeekly(Profile, rng);
            SaveProfile();
            return result;
        }

        public void ReplayCurrent()
        {
            LastReport = null;
            SceneManager.LoadScene(SceneNames.CallFloor);
        }

        /// <summary>
        /// Resolves the scenario to run on the call floor: the menu selection, or
        /// the first catalog scenario as a fallback so the CallFloor scene is
        /// playable on its own (handy when testing in the editor).
        /// </summary>
        public ScenarioDefinition ResolveActiveScenario()
        {
            if (SelectedScenario != null) return SelectedScenario;
            SelectedScenario = ScenarioCatalog.First;
            return SelectedScenario;
        }
    }
}
