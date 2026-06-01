using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using Fitzmark.BDRSim.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// "Getting to a company" as a lightweight route/map strategy beat (FTL / Oregon-Trail feel):
    /// the drive across the city is a short sequence of legs, and at each leg you pick a route —
    /// highway, main streets, back roads, a shortcut, a coffee stop — trading TIME against
    /// COMPOSURE and risk. Spend your time budget badly and you miss the meeting window; arrive
    /// calm for a stronger pitch. Reaching the company hands off to the existing meeting. Pure
    /// screen-space UI — no 3D city, cheap to run and to extend with new events.
    /// </summary>
    public class CityRouteController : MonoBehaviour
    {
        private const int Legs = 4;
        private static readonly Color Secondary = new(0.70f, 0.74f, 0.82f);

        private struct Route
        {
            public string Name, Blurb, GoodMsg, BadMsg;
            public int Time, Comp, Risk, BadTime, BadComp;
        }

        private static readonly Route[] AllRoutes =
        {
            new Route { Name = "Highway",      Blurb = "fast, but jams",      Time = 12, Comp = 0,  Risk = 45, BadTime = 20, BadComp = -12, GoodMsg = "open road — made great time", BadMsg = "stuck dead in a jam" },
            new Route { Name = "Main streets", Blurb = "steady & reliable",   Time = 18, Comp = 2,  Risk = 20, BadTime = 8,  BadComp = -4,  GoodMsg = "green lights most of the way", BadMsg = "caught every red light" },
            new Route { Name = "Back roads",   Blurb = "slow but calming",    Time = 26, Comp = 10, Risk = 0,  BadTime = 0,  BadComp = 0,   GoodMsg = "scenic and relaxing", BadMsg = "" },
            new Route { Name = "Shortcut",     Blurb = "a real gamble",       Time = 8,  Comp = -2, Risk = 50, BadTime = 24, BadComp = -8,  GoodMsg = "the alley paid off", BadMsg = "dead end — had to backtrack" },
            new Route { Name = "Coffee stop",  Blurb = "detour for a latte",  Time = 16, Comp = 16, Risk = 0,  BadTime = 0,  BadComp = 0,   GoodMsg = "caffeinated and sharp", BadMsg = "" },
        };

        private BDRCharacter _c;
        private ScenarioDefinition _scenario;
        private string _cityName = "the city";
        private string _company = "the client";

        private int _time = 90;      // minutes until the meeting window closes
        private int _composure = 70; // 0..100, carried into the pitch
        private int _leg;
        private System.Random _rng;

        private Canvas _canvas;
        private TMP_Text _stats;
        private TMP_Text _log;
        private Transform _choiceRow;
        private RectTransform _progressFill;

        private void Start()
        {
            var gm = GameManager.Instance;
            gm.HubScene = SceneNames.City;
            _c = gm.Profile;

            string cityId = gm.ActiveCityId;
            var client = TerritoryRegistry.Get(cityId);
            if (client != null) _cityName = client.City;

            int week = _c != null ? CareerSystem.Week(_c.career.day) : 1;
            int level = _c != null ? _c.level : 1;
            int difficulty = Mathf.Clamp(1 + (level - 1) / 2 + (week - 1), 1, 10);
            int seed = StableHash(cityId) ^ (_c != null ? _c.career.day * 7919 + _c.callsMade * 101 : 12345);
            _rng = new System.Random(seed);
            _scenario = ProspectGenerator.Generate(difficulty, seed);
            if (_scenario != null)
            {
                _scenario.gatekeeperPresent = true; // a gatekeeper still guards the meeting
                if (_scenario.prospect != null) _company = _scenario.prospect.companyName;
            }

            BuildUi();
            if (_c != null) CareerSystem.EnsureStarted(_c);

            if (_c != null && !CareerSystem.HasCallsLeft(_c))
            {
                _log.text = "No calls left today — head back to the office and end the day.";
                AddCentredButton("Back to the map", () => GameManager.Instance.GoToTexas());
            }
            else
            {
                NextLeg();
            }
        }

        // ---- flow ---------------------------------------------------------------

        private void NextLeg()
        {
            RefreshStats();
            ClearChoices();
            if (_time <= 0) { Missed(); return; }
            if (_leg >= Legs) { Arrive(); return; }

            var routes = RollRoutes();
            for (int i = 0; i < routes.Count; i++)
            {
                Route route = routes[i];
                var btn = MakeButton(_choiceRow, $"{route.Name}\n<size=70%>{route.Blurb}</size>",
                    () => Choose(route));
                var rt = (RectTransform)btn.transform;
                float pad = 0.015f;
                rt.anchorMin = new Vector2(i / (float)routes.Count + pad, 0f);
                rt.anchorMax = new Vector2((i + 1) / (float)routes.Count - pad, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void Choose(Route r)
        {
            int time = r.Time, comp = r.Comp;
            string msg = r.GoodMsg;
            if (r.Risk > 0 && _rng.Next(100) < r.Risk)
            {
                time += r.BadTime;
                comp += r.BadComp;
                msg = r.BadMsg;
            }
            _time -= time;
            _composure = Mathf.Clamp(_composure + comp, 0, 100);
            string compStr = comp >= 0 ? $"+{comp}" : comp.ToString();
            _log.text = $"<b>{r.Name}</b>: {msg}   (−{time} min, composure {compStr})";
            _leg++;
            NextLeg();
        }

        private void Arrive()
        {
            ClearChoices();
            RefreshStats();
            _log.text = $"You reached <b>{_company}</b> with {_time} min to spare — composure {_composure}/100.";
            AddCentredButton("Into the meeting ▶", () =>
            {
                if (_c != null)
                {
                    CareerSystem.ConsumeCall(_c);
                    _c.inPersonMeetings++;
                    GameManager.Instance.SaveProfile();
                }
                GameManager.Instance.StartCareerCall(_scenario);
            });
        }

        private void Missed()
        {
            ClearChoices();
            _log.text = "Gridlock everywhere — you missed the meeting window.";
            var retry = MakeButton(_choiceRow, "Try the drive again", () => GameManager.Instance.GoToCity());
            Place((RectTransform)retry.transform, 0.08f, 0.48f);
            var leave = MakeButton(_choiceRow, "Give up — back to the map", () => GameManager.Instance.GoToTexas());
            Place((RectTransform)leave.transform, 0.52f, 0.92f);
        }

        // ---- ui -----------------------------------------------------------------

        private void BuildUi()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
            }

            _canvas = UiFactory.CreateScreenCanvas("CityRoute");
            var bg = UiFactory.Panel(_canvas.transform, new Color(0.10f, 0.12f, 0.17f, 1f), "Bg");
            UiFactory.Stretch(bg.rectTransform);

            var title = UiFactory.Label(_canvas.transform, $"Getting to {_company}", 28, UiTheme.TextPrimary, TextAnchor.MiddleCenter);
            Band(title.rectTransform, 0.86f, 0.96f);
            var sub = UiFactory.Label(_canvas.transform, _cityName, 16, Secondary, TextAnchor.MiddleCenter);
            Band(sub.rectTransform, 0.81f, 0.86f);

            _stats = UiFactory.Label(_canvas.transform, "", 18, UiTheme.TextPrimary, TextAnchor.MiddleCenter);
            Band(_stats.rectTransform, 0.71f, 0.79f);

            var track = UiFactory.Panel(_canvas.transform, new Color(1f, 1f, 1f, 0.12f), "Track");
            Band(track.rectTransform, 0.655f, 0.68f, 0.1f, 0.9f);
            var fill = UiFactory.Panel(track.transform, new Color(0.40f, 0.75f, 0.55f, 1f), "Fill");
            _progressFill = fill.rectTransform;
            _progressFill.anchorMin = new Vector2(0f, 0f);
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = Vector2.zero;
            _progressFill.offsetMax = Vector2.zero;

            _log = UiFactory.Label(_canvas.transform, "Pick your route to the meeting.", 16, Secondary, TextAnchor.MiddleCenter);
            Band(_log.rectTransform, 0.50f, 0.60f, 0.07f, 0.93f);

            var rowGo = new GameObject("Choices", typeof(RectTransform));
            rowGo.transform.SetParent(_canvas.transform, false);
            _choiceRow = rowGo.transform;
            Band((RectTransform)_choiceRow, 0.26f, 0.46f, 0.05f, 0.95f);

            var leave = MakeButton(_canvas.transform, "Leave city", () => GameManager.Instance.GoToTexas());
            var lrt = (RectTransform)leave.transform;
            lrt.anchorMin = new Vector2(0.40f, 0.06f);
            lrt.anchorMax = new Vector2(0.60f, 0.13f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }

        private void RefreshStats()
        {
            if (_stats != null)
                _stats.text = $"⏱ {_time} min    ☕ Composure {_composure}/100    Leg {Mathf.Min(_leg + 1, Legs)}/{Legs}";
            if (_progressFill != null)
                _progressFill.anchorMax = new Vector2(Mathf.Clamp01((float)_leg / Legs), 1f);
        }

        private void AddCentredButton(string label, System.Action onClick)
        {
            var btn = MakeButton(_choiceRow, label, onClick);
            Place((RectTransform)btn.transform, 0.30f, 0.70f);
        }

        private static Button MakeButton(Transform parent, string label, System.Action onClick) =>
            UiFactory.Button(parent, label, onClick,
                new Color(0.20f, 0.42f, 0.62f), Color.white, 16, TextAnchor.MiddleCenter);

        private static void Place(RectTransform rt, float xMin, float xMax)
        {
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ClearChoices()
        {
            for (int i = _choiceRow.childCount - 1; i >= 0; i--)
                Destroy(_choiceRow.GetChild(i).gameObject);
        }

        private List<Route> RollRoutes()
        {
            var pool = new List<Route>(AllRoutes);
            var picks = new List<Route>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int k = _rng.Next(pool.Count);
                picks.Add(pool[k]);
                pool.RemoveAt(k);
            }
            return picks;
        }

        private static void Band(RectTransform rt, float yMin, float yMax, float xMin = 0f, float xMax = 1f)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static int StableHash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 17;
            int h = 17;
            foreach (char ch in s) h = h * 31 + ch;
            return h;
        }
    }
}
