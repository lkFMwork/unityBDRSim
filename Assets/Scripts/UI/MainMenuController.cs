using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Builds the main menu at runtime: a branded header and a scenario picker
    /// driven by <see cref="ScenarioCatalog"/>. Selecting a scenario hands off to
    /// <see cref="GameManager"/>, which loads the call floor.
    /// Attach this to a GameObject in the MainMenu scene (the setup tool does this).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private Canvas _canvas;

        private void Start()
        {
            ScenarioCatalog.Invalidate(); // pick up any newly authored scenarios
            BuildUi();
        }

        private void BuildUi()
        {
            _canvas = UiFactory.CreateScreenCanvas("MenuCanvas");

            var root = UiFactory.Panel(_canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 24, spacing: 12, expandH: false,
                align: TextAnchor.UpperCenter);
            Tween.FadeIn(root.gameObject.AddComponent<CanvasGroup>(), 0.3f);

            UiFactory.Label(root.transform, "FITZMARK", 42, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            UiFactory.Label(root.transform, "BDR Life — Sales RPG", 20, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Normal, "Subtitle");

            var gm = GameManager.Instance;
            if (!gm.HasProfile)
            {
                BuildCreatePrompt(root.transform);
                BuildFooter(root.transform, false);
                return;
            }

            CareerSystem.EnsureStarted(gm.Profile);
            gm.HubScene = SceneNames.MainMenu;

            var doneQuests = QuestSystem.Sync(gm.Profile);
            var doneAch = AchievementSystem.Sync(gm.Profile);
            if (string.IsNullOrEmpty(gm.CareerFlash))
            {
                if (doneQuests.Count > 0) gm.CareerFlash = QuestSystem.FlashFor(doneQuests);
                else if (doneAch.Count > 0) gm.CareerFlash = AchievementSystem.FlashFor(doneAch);
            }
            gm.SaveProfile();

            if (!string.IsNullOrEmpty(gm.CareerFlash))
            {
                UiFactory.Label(root.transform, gm.CareerFlash, 16, UiTheme.Positive,
                    TextAnchor.MiddleCenter, FontStyle.Bold, "Flash");
                gm.CareerFlash = null;
            }

            BuildCharacterHeader(root.transform, gm.Profile);
            BuildCareerPanel(root.transform, gm.Profile);

            var dash = UiFactory.Button(root.transform, "Open CRM Dashboard", OpenCrm,
                UiTheme.AccentStrong, Color.white, 17, TextAnchor.MiddleCenter);
            UiFactory.Size(dash.gameObject, prefH: 48f, flexW: 1f);

            BuildActionButtons(root.transform, gm.Profile);

            var placesRow = UiFactory.Panel(root.transform, UiTheme.Background, "Places").gameObject;
            UiFactory.HLayout(placesRow, spacing: 10, expandW: true, expandH: true);
            UiFactory.Size(placesRow, prefH: 50f);

            var officeBtn = UiFactory.Button(placesRow.transform, "Go to the Office",
                () => GameManager.Instance.GoToOffice(), UiTheme.Accent, UiTheme.TextPrimary,
                16, TextAnchor.MiddleCenter);
            UiFactory.Size(officeBtn.gameObject, flexW: 1f);

            var mapBtn = UiFactory.Button(placesRow.transform,
                "Travel Texas — local clients ▶",
                () => GameManager.Instance.GoToTexas(), UiTheme.AccentStrong, Color.white,
                16, TextAnchor.MiddleCenter);
            UiFactory.Size(mapBtn.gameObject, flexW: 1.4f);

            var deskBtn = UiFactory.Button(placesRow.transform,
                "Freight Desk — your book ▶",
                () => GameManager.Instance.GoToFreightDesk(), UiTheme.Positive, Color.white,
                16, TextAnchor.MiddleCenter);
            UiFactory.Size(deskBtn.gameObject, flexW: 1.4f);

            var recordsRow = UiFactory.Panel(root.transform, UiTheme.Background, "Records").gameObject;
            UiFactory.HLayout(recordsRow, spacing: 10, expandW: true, expandH: true);
            UiFactory.Size(recordsRow, prefH: 44f);

            var achBtn = UiFactory.Button(recordsRow.transform, "Achievements", OpenAchievements,
                UiTheme.Panel, UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(achBtn.gameObject, flexW: 1f);

            var leadBtn = UiFactory.Button(recordsRow.transform, "Leaderboard", OpenLeaderboard,
                UiTheme.Panel, UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(leadBtn.gameObject, flexW: 1f);

            var upgradeBtn = UiFactory.Button(recordsRow.transform, "Upgrades", OpenUpgrades,
                UiTheme.Panel, UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(upgradeBtn.gameObject, flexW: 1f);

            UiFactory.Label(root.transform, "Practice calls (no quota)", 16, UiTheme.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Bold, "PracticeHeader");

            var listGo = UiFactory.Panel(root.transform, UiTheme.Background, "List").gameObject;
            UiFactory.VLayout(listGo, pad: 0, spacing: 8);
            UiFactory.Size(listGo, flexH: 1f);

            var scenarios = ScenarioCatalog.All;
            if (scenarios.Count == 0)
            {
                UiFactory.Label(listGo.transform,
                    "No practice scenarios found. Run Tools → Fitzmark BDR → Setup Project.",
                    14, UiTheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold, "Empty");
            }
            else
            {
                foreach (var scenario in scenarios)
                    BuildScenarioRow(listGo.transform, scenario);
            }

            BuildFooter(root.transform, true);

            MaybeShowPromotion(gm.Profile);
        }

        private void BuildCareerPanel(Transform parent, BDRCharacter c)
        {
            var panel = UiFactory.Panel(parent, UiTheme.PanelDark, "Career").gameObject;
            UiFactory.VLayout(panel, pad: 12, spacing: 4, expandH: false);
            UiFactory.Size(panel, flexW: 1f);

            int week = CareerSystem.Week(c.career.day);
            UiFactory.Label(panel.transform, $"Day {c.career.day}  ·  Week {week}", 18,
                UiTheme.AccentStrong, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Label(panel.transform,
                $"Calls left today: {c.career.callsRemainingToday} / {CareerSystem.CallsPerDay(c)}",
                15, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Label(panel.transform,
                $"Week quota: {c.career.weekDealsWon} / {c.career.weekDealsGoal} deals",
                15, UiTheme.TextPrimary, TextAnchor.MiddleLeft);

            int activeAccounts = c.accounts != null ? c.accounts.FindAll(a => a != null && a.active).Count : 0;
            UiFactory.Label(panel.transform,
                $"Cash ${c.cash:N0}  ·  Book: {activeAccounts} account(s)  ·  Lifetime margin ${c.lifetimeMargin:N0}",
                14, UiTheme.Positive, TextAnchor.MiddleLeft, FontStyle.Bold);

            if (c.bestWinStreak > 0 || c.currentWinStreak > 0)
                UiFactory.Label(panel.transform,
                    $"Win streak: {c.currentWinStreak}  (best {c.bestWinStreak})",
                    14, UiTheme.TextMuted, TextAnchor.MiddleLeft);
        }

        private void BuildActionButtons(Transform parent, BDRCharacter c)
        {
            var row = UiFactory.Panel(parent, UiTheme.Background, "Actions").gameObject;
            UiFactory.HLayout(row, spacing: 10, expandW: true, expandH: true);
            UiFactory.Size(row, prefH: 60f);

            bool canCall = CareerSystem.HasCallsLeft(c);
            var take = UiFactory.Button(row.transform,
                canCall ? "Take a Call" : "No calls left — End Day", TakeCareerCall,
                canCall ? UiTheme.Positive : UiTheme.PanelDark, Color.white, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(take.gameObject, flexW: 1f);
            take.interactable = canCall;

            var outreach = UiFactory.Button(row.transform, "Outreach", OpenOutreach,
                UiTheme.AccentStrong, Color.white, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(outreach.gameObject, prefW: 150f);

            var endDay = UiFactory.Button(row.transform, "End Day ▶", EndDay,
                UiTheme.Accent, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(endDay.gameObject, prefW: 130f);

            var skills = UiFactory.Button(row.transform, "Skill Tree", OpenSkillTree,
                UiTheme.Panel, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(skills.gameObject, prefW: 130f);

            var quests = UiFactory.Button(row.transform, "Quests", OpenQuestLog,
                UiTheme.Panel, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(quests.gameObject, prefW: 130f);
        }

        private void OpenQuestLog()
        {
            new QuestLogView(_canvas.transform, GameManager.Instance.Profile, null).Open();
        }

        private void OpenAchievements()
        {
            new AchievementsView(_canvas.transform, GameManager.Instance.Profile, null).Open();
        }

        private void OpenLeaderboard()
        {
            new LeaderboardView(_canvas.transform, GameManager.Instance.Profile, null).Open();
        }

        private void OpenUpgrades()
        {
            new UpgradesView(_canvas.transform, GameManager.Instance.Profile, null).Open();
        }

        private void OpenOutreach()
        {
            new OutreachView(_canvas.transform, GameManager.Instance.Profile, null).Open();
        }

        private void OpenCrm()
        {
            new CrmView(_canvas.transform, GameManager.Instance.Profile, null).Open();
        }

        private void MaybeShowPromotion(BDRCharacter c)
        {
            string rank = ProgressionSystem.RankTitle(c.level);
            if (string.IsNullOrEmpty(c.acknowledgedRank))
            {
                c.acknowledgedRank = rank; // first run — just record it, no ceremony
                GameManager.Instance.SaveProfile();
                return;
            }
            if (rank == c.acknowledgedRank) return;

            c.acknowledgedRank = rank;
            c.unspentSkillPoints += 1;
            GameManager.Instance.SaveProfile();
            ShowPromotion(rank);
        }

        private void ShowPromotion(string rank)
        {
            Vfx.Celebrate();
            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.85f), "Promotion");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);

            UiFactory.Label(overlay.transform, "PROMOTED!", 44, UiTheme.Positive,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform, $"You're now a {rank}.", 22, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform, "+1 Skill Point", 16, UiTheme.Warning,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            var ok = UiFactory.Button(overlay.transform, "Onward ▶",
                () => UnityEngine.Object.Destroy(overlay.gameObject), UiTheme.Positive, Color.white,
                18, TextAnchor.MiddleCenter);
            UiFactory.Size(ok.gameObject, prefW: 240f, prefH: 54f);
        }

        private void TakeCareerCall() => GameManager.Instance.TakeColdCall();

        private void EndDay()
        {
            var result = GameManager.Instance.EndBusinessDay(out var freight, out var economy, out var marketEvent);
            string flash = "";
            if (result.WeekEnded)
                flash = result.QuotaMet
                    ? $"Week cleared! {result.DealsWon}/{result.Goal} deals — +{result.RewardSkillPoints} SP, +{result.RewardXp} XP."
                    : $"Week missed: {result.DealsWon}/{result.Goal} deals. New week, fresh start.";
            flash = Join(flash, freight.Summary());
            flash = Join(flash, economy.Summary());
            flash = Join(flash, marketEvent);
            GameManager.Instance.CareerFlash = string.IsNullOrEmpty(flash) ? null : flash;
            GameManager.Instance.ReturnToMenu();
        }

        private static string Join(string a, string b) =>
            string.IsNullOrEmpty(b) ? a : (string.IsNullOrEmpty(a) ? b : a + "  " + b);

        private void OpenSkillTree()
        {
            new SkillTreeView(_canvas.transform, GameManager.Instance.Profile,
                () => GameManager.Instance.ReturnToMenu()).Open();
        }

        private void BuildCreatePrompt(Transform parent)
        {
            UiFactory.Label(parent,
                "Create your Business Development Rep, then build their career one call at a time.",
                16, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic, "Tagline");

            var spacer = UiFactory.Panel(parent, new Color(0f, 0f, 0f, 0f), "Spacer").gameObject;
            UiFactory.Size(spacer, flexH: 1f);

            var create = UiFactory.Button(parent, "▶  Create Your BDR",
                () => GameManager.Instance.GoToCharacterCreate(), UiTheme.Positive, Color.white,
                22, TextAnchor.MiddleCenter);
            UiFactory.Size(create.gameObject, prefH: 72f, flexW: 1f);
        }

        private void BuildCharacterHeader(Transform parent, BDRCharacter c)
        {
            var panel = UiFactory.Panel(parent, UiTheme.Panel, "CharHeader").gameObject;
            UiFactory.VLayout(panel, pad: 14, spacing: 6, expandH: false);
            UiFactory.Size(panel, flexW: 1f);

            UiFactory.Label(panel.transform, c.DisplayName, 24, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Label(panel.transform,
                $"{ProgressionSystem.RankTitle(c.level)} · Level {c.level}", 15, UiTheme.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            int floor = ProgressionSystem.TotalXpForLevel(c.level);
            int span = ProgressionSystem.XpForNextLevel(c.level);
            var xp = UiFactory.MakeMeter(panel.transform, $"XP  {c.xp - floor} / {span}", UiTheme.Accent);
            xp.Set(ProgressionSystem.LevelProgress(c.xp));

            var a = c.attributes;
            UiFactory.Label(panel.transform,
                $"CHA {a.charisma}  ·  NEG {a.negotiation}  ·  PK {a.productKnowledge}  ·  " +
                $"RES {a.resilience}  ·  PRO {a.prospecting}",
                14, UiTheme.TextPrimary, TextAnchor.MiddleLeft);

            if (c.unspentSkillPoints > 0)
                UiFactory.Label(panel.transform,
                    $"• {c.unspentSkillPoints} skill point(s) to spend in the Skill Tree",
                    13, UiTheme.Warning, TextAnchor.MiddleLeft, FontStyle.Bold);

            UiFactory.Label(panel.transform,
                $"Calls {c.callsMade}  ·  Wins {c.dealsWon}  ·  Win rate {Mathf.RoundToInt(c.WinRate * 100f)}%",
                13, UiTheme.TextMuted, TextAnchor.MiddleLeft);
        }

        private void BuildFooter(Transform parent, bool hasProfile)
        {
            var footer = UiFactory.Panel(parent, UiTheme.Background, "Footer").gameObject;
            UiFactory.HLayout(footer, spacing: 12, expandW: true);
            UiFactory.Size(footer, prefH: 46f);

            if (hasProfile)
            {
                var newBdr = UiFactory.Button(footer.transform, "New BDR",
                    () => GameManager.Instance.GoToCharacterCreate(), UiTheme.PanelDark,
                    UiTheme.TextMuted, 16, TextAnchor.MiddleCenter);
                UiFactory.Size(newBdr.gameObject, prefW: 150f);
            }

            var settings = UiFactory.Button(footer.transform, "Settings", OpenSettings,
                UiTheme.PanelDark, UiTheme.TextMuted, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(settings.gameObject, prefW: 150f);

            var quit = UiFactory.Button(footer.transform, "Quit", QuitApp,
                UiTheme.PanelDark, UiTheme.TextMuted, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(quit.gameObject, prefW: 150f);
        }

        private void OpenSettings()
        {
            new SettingsView(_canvas.transform, null).Open();
        }

        private void BuildScenarioRow(Transform parent, ScenarioDefinition scenario)
        {
            var rowBtn = UiFactory.Button(parent, string.Empty,
                () => GameManager.Instance.StartScenario(scenario),
                UiTheme.Panel, UiTheme.TextPrimary);
            UiFactory.Size(rowBtn.gameObject, prefH: 84f, flexW: 1f);

            // Replace the empty default label with a richer two-line layout.
            var content = rowBtn.transform.GetChild(0).gameObject; // the "Text" child
            var txt = content.GetComponent<Text>();
            if (txt != null)
            {
                string contact = scenario.prospect != null ? scenario.prospect.DisplayHeadline : "";
                txt.text =
                    $"<b>{scenario.title}</b>   <color={DifficultyHex(scenario.difficulty)}>" +
                    $"[{scenario.difficulty}]</color>\n<size=14>{contact}</size>";
                txt.alignment = TextAnchor.MiddleLeft;
            }
        }

        private static string DifficultyHex(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Easy => "#5FBF7F",
            DifficultyTier.Medium => "#EAA833",
            DifficultyTier.Hard => "#D94C4C",
            _ => "#FFFFFF"
        };

        private static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
