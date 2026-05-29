using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Fitzmark.BDRSim.Tests
{
    public class CallSessionTests
    {
        private static ScenarioDefinition MakeScenario(
            bool gatekeeper, List<ObjectionType> objections,
            float patience = 0.8f, float trust = 0.4f, float priceSensitivity = 0.4f)
        {
            var p = ScriptableObject.CreateInstance<ProspectProfile>();
            p.contactName = "Test Prospect";
            p.companyName = "Testco";
            p.startingPatience = patience;
            p.startingTrust = trust;
            p.priceSensitivity = priceSensitivity;
            p.painPoints = new List<string> { "tight capacity" };
            p.likelyObjections = objections ?? new List<ObjectionType>();
            p.lanes = new List<Lane>
            {
                new Lane
                {
                    label = "Indianapolis, IN -> Detroit, MI",
                    miles = 290, loadsPerWeek = 8,
                    currentRatePerMile = 2.55f, fitzmarkCostPerMile = 2.15f
                }
            };

            var s = ScriptableObject.CreateInstance<ScenarioDefinition>();
            s.prospect = p;
            s.gatekeeperPresent = gatekeeper;
            s.targetWeeklyMargin = 500f;
            s.briefing = "test briefing";
            return s;
        }

        private static void Choose(CallSession s, int index) => s.Choose(s.CurrentChoices[index]);

        [Test]
        public void GatekeeperScenarioStartsAtGatekeeperThenOpening()
        {
            var withGatekeeper = new CallSession(MakeScenario(true, null));
            withGatekeeper.Begin();
            Assert.AreEqual(CallStage.Gatekeeper, withGatekeeper.Stage);
            Assert.AreEqual(CallStage.Opening, withGatekeeper.NextStage(CallStage.Gatekeeper));

            var noGatekeeper = new CallSession(MakeScenario(false, null));
            noGatekeeper.Begin();
            Assert.AreEqual(CallStage.Opening, noGatekeeper.Stage);
            Assert.AreEqual(CallStage.Discovery, noGatekeeper.NextStage(CallStage.Opening));
        }

        [Test]
        public void BeginPopulatesOpeningChoices()
        {
            var s = new CallSession(MakeScenario(false, null));
            s.Begin();
            Assert.AreEqual(CallStage.Opening, s.Stage);
            Assert.That(s.CurrentChoices.Count, Is.GreaterThan(0));
        }

        [Test]
        public void StrongPathBooksAndClosesTheDeal()
        {
            var s = new CallSession(MakeScenario(false, null));
            s.Begin();

            Choose(s, 0); // strong opening   -> Discovery
            Choose(s, 0); // strong discovery -> Value pitch
            Choose(s, 0); // strong pitch     -> Negotiation (no objections queued)

            Assert.AreEqual(CallStage.Negotiation, s.Stage);

            Choose(s, s.CurrentChoices.Count - 1); // cheapest rate -> accepted -> Closing
            Assert.IsTrue(s.Prospect.DealAgreed);
            Assert.AreEqual(CallStage.Closing, s.Stage);

            Choose(s, 0); // strong close -> Wrap (records WonCommitment)
            Choose(s, 0); // wrap up      -> Completed

            Assert.IsTrue(s.IsOver);
            Assert.AreEqual(CallOutcome.WonCommitment, s.Outcome);
        }

        [Test]
        public void WeakChoicesExhaustPatienceAndHangUp()
        {
            var s = new CallSession(MakeScenario(false, null, patience: 0.2f, trust: 0.5f));
            s.Begin();

            Choose(s, 2); // weak opening   -> patience drops, advances to Discovery
            Choose(s, 2); // weak discovery -> patience hits zero -> hang up

            Assert.AreEqual(CallOutcome.HungUp, s.Outcome);
            Assert.IsTrue(s.IsOver);
        }

        [Test]
        public void PitchRaisesQueuedObjectionThenResolves()
        {
            var s = new CallSession(MakeScenario(false,
                new List<ObjectionType> { ObjectionType.RatesTooHigh }));
            s.Begin();

            Choose(s, 0); // -> Discovery
            Choose(s, 0); // -> Value pitch
            Choose(s, 0); // -> Objection handling (raises the queued objection)

            Assert.AreEqual(CallStage.ObjectionHandling, s.Stage);
            Assert.IsTrue(s.Prospect.ActiveObjection.HasValue);
            Assert.AreEqual(ObjectionType.RatesTooHigh, s.Prospect.ActiveObjection.Value);

            Choose(s, 0); // strong objection reply resolves it -> Negotiation
            Assert.AreEqual(CallStage.Negotiation, s.Stage);
            Assert.IsTrue(s.Prospect.HasHandled(ObjectionType.RatesTooHigh));
        }

        [Test]
        public void CharacterModifiersRaiseStartingTrust()
        {
            var scenario = MakeScenario(false, null, trust: 0.4f);
            var charismatic = CharacterModifiers.FromAttributes(new BDRAttributes { charisma = 10 });

            var s = new CallSession(scenario, charismatic);
            s.Begin();

            Assert.That(s.Prospect.Trust, Is.GreaterThan(0.4f));
        }

        [Test]
        public void UsingAnAbilityAppliesItsEffectAndConsumesAUse()
        {
            var s = new CallSession(MakeScenario(false, null, patience: 0.3f));
            s.ConfigureAbilities(new List<AbilityDefinition> { AbilityLibrary.Get("second_wind") });
            s.Begin();

            float before = s.Prospect.Patience;
            Assert.IsTrue(s.UseAbility(AbilityLibrary.Get("second_wind")));
            Assert.That(s.Prospect.Patience, Is.GreaterThan(before));

            Assert.AreEqual(0, s.AbilityUsesLeft("second_wind"));
            Assert.IsFalse(s.UseAbility(AbilityLibrary.Get("second_wind"))); // no uses left
        }
    }
}
