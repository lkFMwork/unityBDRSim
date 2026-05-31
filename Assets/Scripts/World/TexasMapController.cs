using System.Collections;
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
    /// The Texas overworld — the whole game hub, SNES-style. A pixel-art map of Texas
    /// with a drivable freight truck that node-hops between places: the territory's
    /// account cities (travel to clients) and the FITZMARK HQ compound, whose buildings
    /// stand in for every former menu action (calls, outreach, freight desk, CRM, skills,
    /// upgrades, quests, records, market, end day). Click a place to drive there, or use
    /// the arrow keys; press Enter to go in. Attach to a GameObject in the Texas scene.
    /// </summary>
    public class TexasMapController : MonoBehaviour
    {
        private enum Kind
        {
            Account, Office, Call, Outreach, Freight, Crm,
            Skills, Upgrades, Quests, Achievements, Leaderboard, Market, EndDay
        }

        private class Node
        {
            public Kind Kind;
            public string Title;
            public Vector2 Anchor;      // normalized screen position
            public LocalClient Client;  // accounts only
            public RectTransform Rt;
            public bool Available = true;
        }

        private Canvas _canvas;
        private RectTransform _root;    // full-screen terrain rect; nodes/truck live here
        private RectTransform _truck;
        private readonly List<Node> _nodes = new();
        private Node _current;
        private bool _busy;

        private TMP_Text _stats;
        private TMP_Text _enterLabel;
        private RectTransform _enterBtn;

        private BDRCharacter _c;
        private int _day;

        private void Start()
        {
            var gm = GameManager.Instance;
            gm.HubScene = SceneNames.Texas;
            _c = gm.Profile;
            if (_c != null) BootstrapCareer(gm);
            _day = _c != null ? _c.career.day : 1;
            Build();
            if (_c != null)
            {
                if (!string.IsNullOrEmpty(gm.CareerFlash))
                {
                    Toasts.Show(gm.CareerFlash);
                    gm.CareerFlash = null;
                }
                MaybeShowPromotion(_c);
            }
        }

        /// <summary>Run the per-hub career upkeep the old menu used to do on every visit.</summary>
        private void BootstrapCareer(GameManager gm)
        {
            CareerSystem.EnsureStarted(_c);
            var doneQuests = QuestSystem.Sync(_c);
            var doneAch = AchievementSystem.Sync(_c);
            if (string.IsNullOrEmpty(gm.CareerFlash))
            {
                if (doneQuests.Count > 0) gm.CareerFlash = QuestSystem.FlashFor(doneQuests);
                else if (doneAch.Count > 0) gm.CareerFlash = AchievementSystem.FlashFor(doneAch);
            }
            gm.SaveProfile();
        }

        private void Update()
        {
            if (_busy) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_current != null) Interact(_current);
                return;
            }
            Vector2 dir = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dir = Vector2.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dir = Vector2.right;
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dir = Vector2.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dir = Vector2.down;
            if (dir != Vector2.zero) HopInDirection(dir);
        }

        // ---- build ----------------------------------------------------------

        private void Build()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.11f);
            }

            _canvas = UiFactory.CreateScreenCanvas("Overworld");

            var terrain = new GameObject("Terrain", typeof(RectTransform), typeof(RawImage));
            terrain.transform.SetParent(_canvas.transform, false);
            var ri = terrain.GetComponent<RawImage>();
            ri.texture = OverworldArt.TexasTerrain();
            ri.raycastTarget = false;
            _root = ri.rectTransform;
            UiFactory.Stretch(_root);

            BuildHqGrounds();
            BuildNodes();
            BuildTruck();
            BuildHud();

            var office = _nodes.Find(n => n.Kind == Kind.Office) ?? _nodes[0];
            _truck.anchorMin = _truck.anchorMax = office.Anchor;
            _current = office;
            UpdateEnter();
        }

        private void BuildHqGrounds()
        {
            var hq = UiFactory.Panel(_root, new Color(0.10f, 0.12f, 0.17f, 0.93f), "HQ");
            var rt = hq.rectTransform;
            rt.anchorMin = new Vector2(0.030f, 0.040f);
            rt.anchorMax = new Vector2(0.360f, 0.440f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var title = UiFactory.Label(hq.transform, "FITZMARK  HQ", 14, UiTheme.AccentStrong,
                TextAnchor.UpperCenter, FontStyle.Bold);
            var lrt = title.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.88f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        private void BuildNodes()
        {
            // Account cities, placed by geography on the painted terrain.
            foreach (var client in TerritoryRegistry.All)
            {
                var uv = OverworldArt.ToUv(client.MapX, client.MapZ);
                var anchor = new Vector2(Mathf.Lerp(0.05f, 0.95f, uv.x), Mathf.Lerp(0.07f, 0.92f, uv.y));
                bool available = _c != null && TerritorySystem.IsAvailable(_c, client, _day);
                // Label by CITY (the place you fast-travel to), not the company.
                var n = new Node
                {
                    Kind = Kind.Account,
                    Title = CityName(client.City),
                    Anchor = anchor,
                    Client = client,
                    Available = available
                };
                MakeMarker(n, AccountColor(client), false);
                _nodes.Add(n);
            }

            // HQ buildings — a 4x3 grid in the compound, standing in for the old menu.
            float[] xs = { 0.072f, 0.152f, 0.232f, 0.312f };
            float[] ys = { 0.345f, 0.225f, 0.108f };
            var grid = new (Kind kind, string title, Color tint)[]
            {
                (Kind.Office, "Office", new Color(0.78f, 0.62f, 0.40f)),
                (Kind.Call, "Take a Call", new Color(0.32f, 0.74f, 0.46f)),
                (Kind.Outreach, "Outreach", new Color(0.30f, 0.62f, 1f)),
                (Kind.Freight, "Freight Desk", new Color(0.36f, 0.78f, 0.62f)),
                (Kind.Crm, "CRM", new Color(0.45f, 0.60f, 0.95f)),
                (Kind.Skills, "Training", new Color(0.66f, 0.50f, 0.90f)),
                (Kind.Upgrades, "Supply", new Color(0.93f, 0.70f, 0.28f)),
                (Kind.Market, "Market", new Color(0.92f, 0.52f, 0.34f)),
                (Kind.Quests, "Quests", new Color(0.40f, 0.78f, 0.80f)),
                (Kind.Achievements, "Trophies", new Color(0.95f, 0.82f, 0.36f)),
                (Kind.Leaderboard, "Rankings", new Color(0.74f, 0.78f, 0.84f)),
                (Kind.EndDay, "Turn In", new Color(0.52f, 0.55f, 0.90f)),
            };
            for (int i = 0; i < grid.Length; i++)
            {
                var (kind, title, tint) = grid[i];
                var n = new Node
                {
                    Kind = kind,
                    Title = title,
                    Anchor = new Vector2(xs[i % 4], ys[i / 4])
                };
                MakeMarker(n, tint, true);
                _nodes.Add(n);
            }
        }

        private void MakeMarker(Node node, Color tint, bool building)
        {
            var go = new GameObject(node.Title, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = node.Anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = building ? new Vector2(30f, 30f) : new Vector2(26f, 26f);
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = OverworldArt.Marker(building);
            img.color = tint;
            img.preserveAspect = true;

            var btn = go.GetComponent<Button>();
            var captured = node;
            btn.onClick.AddListener(() => OnNodeClicked(captured));

            // Readable label chip below the marker.
            var chip = UiFactory.Panel(go.transform, new Color(0f, 0f, 0f, 0.55f), "Chip");
            var crt = chip.rectTransform;
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(Mathf.Max(46f, node.Title.Length * 7.4f + 12f), 17f);
            crt.anchoredPosition = new Vector2(0f, -3f);
            chip.raycastTarget = false;
            var lbl = UiFactory.Label(chip.transform, node.Title, 11, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(lbl.rectTransform);
            lbl.raycastTarget = false;

            node.Rt = rt;
        }

        private void BuildTruck()
        {
            var go = new GameObject("Truck", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            _truck = go.GetComponent<RectTransform>();
            _truck.pivot = new Vector2(0.5f, 0.5f);
            _truck.sizeDelta = new Vector2(42f, 25f);
            var img = go.GetComponent<Image>();
            img.sprite = OverworldArt.Truck();
            img.preserveAspect = true;
            img.raycastTarget = false;
            go.transform.SetAsLastSibling();
        }

        private void BuildHud()
        {
            var bar = UiFactory.Panel(_canvas.transform, new Color(0.05f, 0.07f, 0.11f, 0.90f), "TopBar");
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0f, 0.935f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            UiFactory.HLayout(bar.gameObject, pad: 14, spacing: 12, expandW: true, expandH: true);

            _stats = UiFactory.Label(bar.transform, "", 15, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(_stats.gameObject, flexW: 1f);

            var settings = UiFactory.Button(bar.transform, "☰ Menu",
                () => new SettingsView(_canvas.transform, RefreshHud).Open(),
                UiTheme.PanelDark, UiTheme.TextMuted, 14, TextAnchor.MiddleCenter);
            UiFactory.Size(settings.gameObject, prefW: 96f);

            RefreshHud();

            // Bottom: drive hint + the Enter button.
            var hint = UiFactory.Label(_canvas.transform,
                "Click a place to drive there  ·  Arrow keys to hop  ·  Enter to go in",
                13, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0.30f, 0.012f);
            hrt.anchorMax = new Vector2(0.97f, 0.055f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            var enter = UiFactory.Button(_canvas.transform, "", () => { if (_current != null) Interact(_current); },
                UiTheme.Positive, Color.white, 16, TextAnchor.MiddleCenter);
            _enterBtn = enter.GetComponent<RectTransform>();
            _enterBtn.anchorMin = new Vector2(0.30f, 0.060f);
            _enterBtn.anchorMax = new Vector2(0.97f, 0.105f);
            _enterBtn.offsetMin = _enterBtn.offsetMax = Vector2.zero;
            _enterLabel = enter.GetComponentInChildren<TMP_Text>();
        }

        private void RefreshHud()
        {
            if (_stats == null) return;
            var c = GameManager.Instance.Profile;
            _c = c;
            if (c == null) { _stats.text = "No BDR loaded."; return; }
            int week = CareerSystem.Week(c.career.day);
            _stats.text =
                $"<b>{c.DisplayName}</b>  ·  Lv {c.level}  ·  Day {c.career.day} (Wk {week})   " +
                $"<color=#7FE0A0>${c.cash:N0}</color>   " +
                $"Calls {c.career.callsRemainingToday}/{CareerSystem.CallsPerDay(c)}   " +
                $"Quota {c.career.weekDealsWon}/{c.career.weekDealsGoal}";
        }

        // ---- movement -------------------------------------------------------

        private void OnNodeClicked(Node node)
        {
            if (_busy) return;
            if (node == _current) Interact(node);
            else DriveTo(node);
        }

        private void DriveTo(Node node)
        {
            if (_busy || node == null) return;
            StartCoroutine(DriveRoutine(node));
        }

        private IEnumerator DriveRoutine(Node target)
        {
            _busy = true;
            Vector2 from = _truck.anchorMin;
            Vector2 to = target.Anchor;

            float dx = to.x - from.x;
            if (Mathf.Abs(dx) > 0.001f)
            {
                var s = _truck.localScale;
                s.x = Mathf.Abs(s.x) * (dx < 0f ? -1f : 1f);
                _truck.localScale = s;
            }

            float dur = Mathf.Clamp(Vector2.Distance(from, to) * 1.9f, 0.16f, 0.7f);
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                _truck.anchorMin = _truck.anchorMax = Vector2.Lerp(from, to, u);
                yield return null;
            }
            _truck.anchorMin = _truck.anchorMax = to;
            _current = target;
            _busy = false;
            UpdateEnter();
        }

        private void HopInDirection(Vector2 dir)
        {
            if (_current == null) return;
            Node best = null;
            float bestScore = float.MaxValue;
            foreach (var n in _nodes)
            {
                if (n == _current) continue;
                Vector2 d = n.Anchor - _current.Anchor;
                if (Vector2.Dot(d.normalized, dir) < 0.45f) continue; // roughly in the pressed direction
                float score = d.magnitude - Vector2.Dot(d, dir) * 0.25f;
                if (score < bestScore) { bestScore = score; best = n; }
            }
            if (best != null) DriveTo(best);
        }

        private void UpdateEnter()
        {
            if (_enterLabel == null || _current == null) return;
            // Pulse the active node.
            foreach (var n in _nodes)
                if (n.Rt != null) n.Rt.localScale = Vector3.one * (n == _current ? 1.18f : 1f);

            string verb = _current.Kind switch
            {
                Kind.Account => _current.Available ? "🚚  Drive into" : "🔒  Locked —",
                Kind.Call => "▶  Work the phones at",
                Kind.EndDay => "▶  Turn in the day at",
                _ => "▶  Enter"
            };
            _enterLabel.text = $"{verb} {_current.Title}";
            var img = _enterBtn.GetComponent<Image>();
            if (img != null)
                img.color = (_current.Kind == Kind.Account && !_current.Available)
                    ? UiTheme.PanelDark : UiTheme.Positive;
        }

        // ---- interaction ----------------------------------------------------

        private void Interact(Node node)
        {
            if (node == null) return;
            var gm = GameManager.Instance;
            switch (node.Kind)
            {
                case Kind.Account: TravelTo(node.Client); break;
                case Kind.Office: gm.GoToOffice(); break;
                case Kind.Freight: gm.GoToFreightDesk(); break;
                case Kind.Call:
                    if (_c != null && CareerSystem.HasCallsLeft(_c)) gm.TakeColdCall();
                    else Toasts.Show("No calls left today — turn in the day.");
                    break;
                case Kind.Outreach: new OutreachView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Crm: new CrmView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Skills: new SkillTreeView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Upgrades: new UpgradesView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Quests: new QuestLogView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Achievements: new AchievementsView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Leaderboard: new LeaderboardView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.Market: new MarketView(_canvas.transform, _c, RefreshHud).Open(); break;
                case Kind.EndDay: ConfirmEndDay(); break;
            }
        }

        private void ConfirmEndDay()
        {
            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.78f), "EndDayConfirm");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);

            UiFactory.Label(overlay.transform, "Turn in the day?", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                "Bills are paid, the freight book ticks over, and a new day begins.",
                15, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);

            var row = UiFactory.Panel(overlay.transform, new Color(0f, 0f, 0f, 0f), "Row").gameObject;
            UiFactory.HLayout(row, spacing: 12, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 56f);

            var go = UiFactory.Button(row.transform, "End the Day ▶", () =>
            {
                Destroy(overlay.gameObject);
                EndDay();
            }, UiTheme.Positive, Color.white, 17, TextAnchor.MiddleCenter);
            UiFactory.Size(go.gameObject, prefW: 200f);

            var cancel = UiFactory.Button(row.transform, "Not yet",
                () => Destroy(overlay.gameObject), UiTheme.PanelDark, UiTheme.TextMuted, 17,
                TextAnchor.MiddleCenter);
            UiFactory.Size(cancel.gameObject, prefW: 160f);
        }

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
            GameManager.Instance.GoToTexas(); // reload the overworld on the new day (shows the flash)
        }

        private static string Join(string a, string b) =>
            string.IsNullOrEmpty(b) ? a : (string.IsNullOrEmpty(a) ? b : a + "  " + b);

        private void TravelTo(LocalClient client)
        {
            if (_c == null || client == null) return;
            if (!TerritorySystem.IsAvailable(_c, client, _day))
            {
                Toasts.Show(!TerritorySystem.IsUnlocked(_c, client)
                    ? $"{client.Company} unlocks at level {client.RequiredLevel}."
                    : $"{client.Company}: next stage in {TerritorySystem.DaysUntilAvailable(_c, client.Id, _day)} day(s).");
                return;
            }

            // Fast-travel into the city itself; you drive to a business there to take the meeting.
            GameManager.Instance.PendingClientId = client.Id;
            GameManager.Instance.GoToCity(client.Id);
        }

        // ---- helpers --------------------------------------------------------

        private Color AccountColor(LocalClient client)
        {
            if (_c == null) return Color.gray;
            if (TerritorySystem.IsClosed(_c, client.Id)) return new Color(0.34f, 0.62f, 1f);
            if (!TerritorySystem.IsUnlocked(_c, client)) return new Color(0.45f, 0.45f, 0.50f);
            if (!TerritorySystem.IsAvailable(_c, client, _day)) return new Color(0.92f, 0.70f, 0.22f);
            return new Color(0.34f, 0.80f, 0.44f);
        }

        // "Austin, TX" -> "Austin" for a clean city label on the map.
        private static string CityName(string cityField)
        {
            if (string.IsNullOrEmpty(cityField)) return "City";
            int comma = cityField.IndexOf(',');
            return comma > 0 ? cityField.Substring(0, comma) : cityField;
        }

        private static int StableHash(string s)
        {
            int h = 17;
            foreach (char ch in s) h = h * 31 + ch;
            return h;
        }

        private void MaybeShowPromotion(BDRCharacter c)
        {
            string rank = ProgressionSystem.RankTitle(c.level);
            if (string.IsNullOrEmpty(c.acknowledgedRank))
            {
                c.acknowledgedRank = rank; // first run — record, no ceremony
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
                () => Destroy(overlay.gameObject), UiTheme.Positive, Color.white, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(ok.gameObject, prefW: 240f, prefH: 54f);
        }
    }
}
