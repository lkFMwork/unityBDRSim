using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Produces the dialogue options available at each stage of a call. This is
    /// the training "content" layer: the difference between a strong and weak
    /// rep is encoded here as the score, trust, and patience effects of each line.
    ///
    /// Content is generated from the scenario data (lane rates, incumbent, pain
    /// points, objections) so a single library serves every prospect. Trainers
    /// can extend any stage by adding entries to the relevant builder.
    /// </summary>
    public class DialogueLibrary
    {
        private readonly ScenarioDefinition _scenario;
        private readonly ProspectProfile _prospect;
        private readonly Lane _lane;

        public DialogueLibrary(ScenarioDefinition scenario)
        {
            _scenario = scenario;
            _prospect = scenario != null ? scenario.prospect : null;
            _lane = scenario != null ? scenario.PrimaryLane : null;
        }

        public List<DialogueChoice> GetChoices(CallStage stage, ProspectState state, CallStage next)
        {
            return stage switch
            {
                CallStage.Opening => Opening(next),
                CallStage.Gatekeeper => Gatekeeper(next),
                CallStage.Discovery => Discovery(next),
                CallStage.ValuePitch => ValuePitch(next),
                CallStage.ObjectionHandling => ObjectionHandling(state, next),
                CallStage.Negotiation => Negotiation(),
                CallStage.Closing => Closing(state),
                CallStage.Wrap => Wrap(),
                _ => new List<DialogueChoice>()
            };
        }

        private List<DialogueChoice> Opening(CallStage next) => new()
        {
            new DialogueChoice
            {
                Text = $"\"Hi {FirstName()}, this is Jordan with Fitzmark — I know I'm " +
                       "catching you out of the blue. Do you have 30 seconds for why I called?\"",
                Response = "Alright, you've got 30 seconds.",
                Quality = ChoiceQuality.Strong, Category = ScoreCategory.Rapport,
                ScoreValue = 8f, TrustDelta = 0.10f, PatienceDelta = 0.05f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"Hi, I'm calling from Fitzmark, a freight brokerage. Who handles your " +
                       "shipping decisions?\"",
                Response = "That'd be me. What's this about?",
                Quality = ChoiceQuality.Adequate, Category = ScoreCategory.Rapport,
                ScoreValue = 4f, TrustDelta = 0.02f, PatienceDelta = -0.02f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"We've got the best rates and the best trucks in the business — " +
                       "let me tell you all about Fitzmark.\"",
                Response = "...is this a sales call?",
                Quality = ChoiceQuality.Weak, Category = ScoreCategory.Rapport,
                ScoreValue = 0f, TrustDelta = -0.08f, PatienceDelta = -0.12f, AdvanceTo = next
            }
        };

        private List<DialogueChoice> Gatekeeper(CallStage next) => new()
        {
            new DialogueChoice
            {
                Text = "\"Hi, I'm hoping you can help me — I need to reach whoever manages " +
                       "outbound freight. Who would that be?\"",
                Response = "Sure, let me put you through to them.",
                Quality = ChoiceQuality.Strong, Category = ScoreCategory.Rapport,
                ScoreValue = 6f, TrustDelta = 0.05f, PatienceDelta = 0.03f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"Can you transfer me to your logistics department?\"",
                Response = "Hold please.",
                Quality = ChoiceQuality.Adequate, Category = ScoreCategory.Rapport,
                ScoreValue = 3f, TrustDelta = 0f, PatienceDelta = -0.03f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"Just put me through to the decision maker, it's important.\"",
                Response = "And who are you again?",
                Quality = ChoiceQuality.Weak, Category = ScoreCategory.Rapport,
                ScoreValue = 0f, TrustDelta = -0.05f, PatienceDelta = -0.08f, AdvanceTo = next
            }
        };

        private List<DialogueChoice> Discovery(CallStage next) => new()
        {
            new DialogueChoice
            {
                Text = $"\"Help me understand your operation — what are your main lanes, how " +
                       $"much volume on something like {LaneLabel()}, and where do your current " +
                       "carriers let you down?\"",
                Response = DiscoveryReveal(),
                Quality = ChoiceQuality.Strong, Category = ScoreCategory.Discovery,
                ScoreValue = 9f, TrustDelta = 0.10f, PatienceDelta = 0.04f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"What kind of freight do you move?\"",
                Response = "Mostly truckload, some on the dock. Why?",
                Quality = ChoiceQuality.Adequate, Category = ScoreCategory.Discovery,
                ScoreValue = 4f, TrustDelta = 0.02f, PatienceDelta = -0.02f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"Let me tell you everything Fitzmark can do for you.\"",
                Response = "You don't even know what I ship.",
                Quality = ChoiceQuality.Weak, Category = ScoreCategory.Discovery,
                ScoreValue = 0f, TrustDelta = -0.08f, PatienceDelta = -0.10f, AdvanceTo = next
            }
        };

        private List<DialogueChoice> ValuePitch(CallStage next) => new()
        {
            new DialogueChoice
            {
                Text = $"\"Based on what you said about {TopPain()}, here's where we help: a vetted " +
                       "carrier base for your lanes plus 24/7 track-and-trace so you're never in " +
                       "the dark on a load.\"",
                Response = "Okay, that's actually relevant to us.",
                Quality = ChoiceQuality.Strong, Category = ScoreCategory.ValueArticulation,
                ScoreValue = 9f, TrustDelta = 0.10f, PatienceDelta = 0.02f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"We do truckload, LTL, intermodal, drayage, reefer, flatbed — " +
                       "basically everything.\"",
                Response = "Sure, like every other broker.",
                Quality = ChoiceQuality.Adequate, Category = ScoreCategory.ValueArticulation,
                ScoreValue = 4f, TrustDelta = 0f, PatienceDelta = -0.04f, AdvanceTo = next
            },
            new DialogueChoice
            {
                Text = "\"We'll beat whatever you're paying now, guaranteed.\"",
                Response = "Cheapest broker today is the one that drops my load tomorrow.",
                Quality = ChoiceQuality.Weak, Category = ScoreCategory.ValueArticulation,
                ScoreValue = 1f, TrustDelta = -0.06f, PatienceDelta = -0.06f, AdvanceTo = next
            }
        };

        private List<DialogueChoice> ObjectionHandling(ProspectState state, CallStage next)
        {
            ObjectionType type = state.ActiveObjection ?? ObjectionType.NotInterested;

            return new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    Text = StrongObjectionReply(type),
                    Response = "Hm. Okay, I hadn't thought about it that way.",
                    Quality = ChoiceQuality.Strong, Category = ScoreCategory.ObjectionHandling,
                    ScoreValue = 9f, TrustDelta = 0.10f, PatienceDelta = 0.03f,
                    ResolvesObjection = type, AdvanceTo = next
                },
                new DialogueChoice
                {
                    Text = "\"I hear you. Would it be worth a quick look anyway?\"",
                    Response = "Maybe. Keep going.",
                    Quality = ChoiceQuality.Adequate, Category = ScoreCategory.ObjectionHandling,
                    ScoreValue = 4f, TrustDelta = 0f, PatienceDelta = -0.03f,
                    ResolvesObjection = type, AdvanceTo = next
                },
                new DialogueChoice
                {
                    Text = "\"Respectfully, I think you're wrong about that.\"",
                    Response = "Excuse me?",
                    Quality = ChoiceQuality.Weak, Category = ScoreCategory.ObjectionHandling,
                    ScoreValue = 0f, TrustDelta = -0.12f, PatienceDelta = -0.15f,
                    ResolvesObjection = type, AdvanceTo = next
                }
            };
        }

        private List<DialogueChoice> Negotiation()
        {
            float cost = _lane != null ? _lane.fitzmarkCostPerMile : 2.0f;

            float premium = Round2(cost * 1.20f);
            float fair = Round2(cost * 1.12f);
            float thin = Round2(cost * 1.05f);

            return new List<DialogueChoice>
            {
                RateOffer(premium, "Hold firm on a premium rate (protects margin, risks the deal)"),
                RateOffer(fair, "Offer a fair, competitive rate (balanced)"),
                RateOffer(thin, "Buy the business with a thin margin (easy yes, little profit)")
            };
        }

        private DialogueChoice RateOffer(float rate, string note) => new()
        {
            Text = $"\"For {LaneLabel()} I can do ${rate:0.00} per mile.\"  — {note}",
            IsRateOffer = true,
            OfferRatePerMile = rate,
            Category = ScoreCategory.Negotiation
            // Response, score, and stage transition are filled in by CallSession
            // after NegotiationEngine evaluates the offer.
        };

        private List<DialogueChoice> Closing(ProspectState state)
        {
            bool deal = state.DealAgreed;
            return new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    Text = deal
                        ? "\"Great — let's prove it. Tender me one load this week and I'll " +
                          "earn the rest of your freight.\""
                        : "\"I get it on price today. Can I be your backup on a tough lane and " +
                          "earn a shot when a carrier falls through?\"",
                    Response = deal ? "Deal. I'll send you a load." : "...alright, stay in touch.",
                    Quality = ChoiceQuality.Strong, Category = ScoreCategory.Close,
                    ScoreValue = 9f, TrustDelta = 0.05f, AdvanceTo = CallStage.Wrap
                },
                new DialogueChoice
                {
                    Text = "\"Want me to follow up next week?\"",
                    Response = "Sure, follow up.",
                    Quality = ChoiceQuality.Adequate, Category = ScoreCategory.Close,
                    ScoreValue = 4f, AdvanceTo = CallStage.Wrap
                },
                new DialogueChoice
                {
                    Text = "\"Okay, well... thanks for your time.\"",
                    Response = "Yep. Bye.",
                    Quality = ChoiceQuality.Weak, Category = ScoreCategory.Close,
                    ScoreValue = 0f, AdvanceTo = CallStage.Wrap
                }
            };
        }

        private List<DialogueChoice> Wrap() => new()
        {
            new DialogueChoice
            {
                Text = "Log the call and wrap up.",
                Response = string.Empty,
                Quality = ChoiceQuality.Adequate, Category = ScoreCategory.Close,
                ScoreValue = 0f, EndsCall = true, AdvanceTo = CallStage.Completed
            }
        };

        // ---- content helpers -------------------------------------------------

        private string FirstName()
        {
            if (_prospect == null || string.IsNullOrEmpty(_prospect.contactName)) return "there";
            int space = _prospect.contactName.IndexOf(' ');
            return space > 0 ? _prospect.contactName.Substring(0, space) : _prospect.contactName;
        }

        private string LaneLabel() => _lane != null ? _lane.label : "your main lane";

        private string TopPain() =>
            (_prospect != null && _prospect.painPoints != null && _prospect.painPoints.Count > 0)
                ? _prospect.painPoints[0].ToLowerInvariant()
                : "keeping freight covered on time";

        private string DiscoveryReveal()
        {
            string lane = LaneLabel();
            string pain = (_prospect != null && _prospect.painPoints != null && _prospect.painPoints.Count > 0)
                ? _prospect.painPoints[0]
                : "we sometimes get let down on capacity";
            string incumbent = (_prospect != null && !string.IsNullOrEmpty(_prospect.incumbentProvider))
                ? $" We mostly use {_prospect.incumbentProvider}."
                : " We handle most of it in-house.";
            return $"\"{lane} is our bread and butter. Honestly, {pain.ToLowerInvariant()}.{incumbent}\"";
        }

        private string StrongObjectionReply(ObjectionType type)
        {
            var info = ObjectionCatalog.Get(type);
            // The strong reply demonstrates the coaching tip in the rep's voice.
            return type switch
            {
                ObjectionType.AlreadyHaveBroker =>
                    "\"That's great — I'm not asking you to fire anyone. Where do they " +
                    "struggle when capacity gets tight? Let me be your backup there.\"",
                ObjectionType.RatesTooHigh =>
                    "\"Fair. What's a service failure on this lane actually cost you when it " +
                    "happens? Let's compare total cost, not just rate per mile.\"",
                ObjectionType.SendMeAnEmail =>
                    "\"Happy to — and so it's not just another email you skim, can I walk " +
                    "you through it Thursday for five minutes?\"",
                ObjectionType.WeGoDirectToCarriers =>
                    "\"Smart — keep those direct relationships. I want the overflow and the " +
                    "lanes your carriers turn down. Pure upside for you.\"",
                ObjectionType.NoTimeRightNow =>
                    "\"Totally respect that. Give me 60 seconds — if it's not relevant I'll " +
                    "let you go and won't call back.\"",
                ObjectionType.NotInterested =>
                    "\"Fair enough. One question: when a carrier no-shows, who covers it? " +
                    "If that's ever a headache, that's exactly what I fix.\"",
                ObjectionType.BadPastExperience =>
                    "\"I'm sorry that happened — what went wrong? Here's the claims and " +
                    "tracking process we run so it can't happen the same way again.\"",
                ObjectionType.NeedToCheckWithBoss =>
                    "\"Makes sense. What will your manager want to see? Let's build the case " +
                    "together so it lands when you bring it to them.\"",
                _ => $"\"{info.CoachingTip}\""
            };
        }

        private static float Round2(float v) => (float)System.Math.Round(v, 2);
    }
}
