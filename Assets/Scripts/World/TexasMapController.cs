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
    /// The Super Mario World–style overworld. Each US STATE is a world; its cities are
    /// level-nodes laid along a path, cleared in sequence (beat a city's commute + meeting to
    /// open the next). Clearing every city in a state opens a gate to the next state (fixed
    /// company difficulty order, your home branch's state first). A freight truck token hops the
    /// path; press Enter to play the node you're on. The career menu (Office, Calls, CRM, etc.)
    /// lives behind the ☰ Menu button and the home world's HQ node. Attach to the Texas scene.
    /// (Scene/method names keep the "Texas" label for compatibility; it is now the generic map.)
    /// </summary>
    public class TexasMapController : MonoBehaviour
    {
        private enum Kind { City, Hq, NextWorld }

        private class Node
        {
            public Kind Kind;
            public string Title;
            public Vector2 Anchor;       // normalized screen position
            public LocalClient Client;   // cities only
            public int CityIndex;
            public bool Unlocked;        // reachable on the path
            public bool Cleared;
            public RectTransform Rt;
        }

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _truck;
        private RectTransform _pathLayer;
        private readonly List<Node> _nodes = new();
        private Node _current;
        private bool _busy;

        private TMP_Text _stats;
        private TMP_Text _worldLabel;
        private TMP_Text _enterLabel;
        private RectTransform _enterBtn;

        private BDRCharacter _c;
        private int _day;
        private StateWorld _world;

        private void Start()
        {
            var gm = GameManager.Instance;
            gm.HubScene = SceneNames.Texas;
            _c = gm.Profile;
            if (_c != null) BootstrapCareer(gm);
            _day = _c != null ? _c.career.day : 1;
            _world = WorldSystem.CurrentWorld(_c) ?? WorldRegistry.All[0];
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
            ri.texture = OverworldArt.StateTerrain(StableHash(_world.Id));
            ri.raycastTarget = false;
            _root = ri.rectTransform;
            UiFactory.Stretch(_root);

            // Path lines drawn under the nodes.
            var pl = new GameObject("Paths", typeof(RectTransform));
            pl.transform.SetParent(_root, false);
            _pathLayer = pl.GetComponent<RectTransform>();
            UiFactory.Stretch(_pathLayer);
            _pathLayer.SetAsFirstSibling();

            BuildNodes();
            BuildPaths();
            BuildTruck();
            BuildHud();

            // Start the truck on the player's current frontier city (first uncleared unlocked).
            var startNode = _nodes.Find(n => n.Kind == Kind.City && n.Unlocked && !n.Cleared)
                            ?? _nodes.Find(n => n.Kind == Kind.City) ?? _nodes[0];
            _truck.anchorMin = _truck.anchorMax = startNode.Anchor;
            _current = startNode;
            UpdateEnter();
        }

        // Lay the world's cities along a gentle serpentine path; HQ near the branch city; the
        // next-world gate after the last city.
        private void BuildNodes()
        {
            var cities = _world.Cities;
            int n = cities.Count;
            for (int i = 0; i < n; i++)
            {
                var city = cities[i];
                var anchor = PathAnchor(i, n);
                var node = new Node
                {
                    Kind = Kind.City,
                    Title = CityName(city.City),
                    Anchor = anchor,
                    Client = city,
                    CityIndex = i,
                    Unlocked = _c != null && WorldSystem.IsCityUnlocked(_c, _world, i),
                    Cleared = _c != null && WorldSystem.IsCityCleared(_c, city.Id)
                };
                MakeMarker(node, CityColor(node), false);
                _nodes.Add(node);

                // HQ home-base node sits beside the branch office in the home world only.
                if (city.IsBranch && _c != null && city.StateId == _c.homeStateId)
                {
                    var hq = new Node
                    {
                        Kind = Kind.Hq,
                        Title = "FITZMARK HQ",
                        Anchor = anchor + new Vector2(0f, 0.11f),
                        Unlocked = true
                    };
                    MakeMarker(hq, new Color(0.95f, 0.82f, 0.36f), true);
                    _nodes.Add(hq);
                }
            }

            // Gate to the next world, shown past the final city.
            var next = WorldSystem.NextWorld(_c, _world);
            if (next != null)
            {
                bool open = WorldSystem.IsWorldCleared(_c, _world);
                var gate = new Node
                {
                    Kind = Kind.NextWorld,
                    Title = open ? $"▶ {next.Name}" : $"🔒 {next.Name}",
                    Anchor = PathAnchor(n, n) + new Vector2(0.04f, 0f),
                    Unlocked = open
                };
                MakeMarker(gate, open ? new Color(0.36f, 0.78f, 0.62f) : new Color(0.5f, 0.5f, 0.55f), true);
                _nodes.Add(gate);
            }
        }

        // A serpentine left→right path that wraps down to the next row, SMW-style. `count` is the
        // number of cities; index `count` (the next-world gate) extends one past the last row.
        private Vector2 PathAnchor(int i, int count)
        {
            int perRow = Mathf.Max(4, Mathf.CeilToInt(count / 2f));
            int rows = Mathf.Max(1, Mathf.CeilToInt((count + 1) / (float)perRow));
            int row = i / perRow;
            int col = i % perRow;
            if (row % 2 == 1) col = perRow - 1 - col; // snake back the other way
            float x = perRow <= 1 ? 0.5f : Mathf.Lerp(0.12f, 0.88f, col / (perRow - 1f));
            float y = rows <= 1 ? 0.5f : Mathf.Lerp(0.66f, 0.26f, row / (float)(rows - 1));
            y += (col % 2 == 0 ? 0.03f : -0.03f); // gentle wiggle so it reads as a winding trail
            return new Vector2(x, y);
        }

        private void BuildPaths()
        {
            // Connect consecutive city nodes with dotted line segments.
            var cityNodes = _nodes.FindAll(n => n.Kind == Kind.City);
            cityNodes.Sort((a, b) => a.CityIndex.CompareTo(b.CityIndex));
            for (int i = 0; i < cityNodes.Count - 1; i++)
                DrawPath(cityNodes[i], cityNodes[i + 1]);
            // Path to the next-world gate from the last city.
            var gate = _nodes.Find(n => n.Kind == Kind.NextWorld);
            if (gate != null && cityNodes.Count > 0)
                DrawPath(cityNodes[cityNodes.Count - 1], gate);
        }

        private void DrawPath(Node a, Node b)
        {
            const int dots = 9;
            bool traversable = a.Cleared; // lit once you've cleared the earlier node
            for (int d = 1; d < dots; d++)
            {
                float t = d / (float)dots;
                var pos = Vector2.Lerp(a.Anchor, b.Anchor, t);
                var dot = UiFactory.Panel(_pathLayer,
                    traversable ? new Color(0.95f, 0.86f, 0.45f, 0.9f) : new Color(1f, 1f, 1f, 0.18f), "Dot");
                var rt = dot.rectTransform;
                rt.anchorMin = rt.anchorMax = pos;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(7f, 7f);
                rt.anchoredPosition = Vector2.zero;
                dot.raycastTarget = false;
            }
        }

        private void MakeMarker(Node node, Color tint, bool building)
        {
            var go = new GameObject(node.Title, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = node.Anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = building ? new Vector2(32f, 32f) : new Vector2(28f, 28f);
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = OverworldArt.Marker(building);
            img.color = tint;
            img.preserveAspect = true;

            var btn = go.GetComponent<Button>();
            var captured = node;
            btn.onClick.AddListener(() => OnNodeClicked(captured));

            // A check mark over cleared cities.
            if (node.Kind == Kind.City && node.Cleared)
            {
                var tick = UiFactory.Label(go.transform, "✓", 18, new Color(0.25f, 1f, 0.4f),
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                UiFactory.Stretch(tick.rectTransform);
                tick.raycastTarget = false;
            }

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

            // World banner (top-left under the bar): "WORLD 1 — TEXAS · 2/10 cities".
            _worldLabel = UiFactory.Label(_canvas.transform, "", 16, UiTheme.AccentStrong,
                TextAnchor.UpperLeft, FontStyle.Bold);
            var wrt = _worldLabel.rectTransform;
            wrt.anchorMin = new Vector2(0.02f, 0.86f);
            wrt.anchorMax = new Vector2(0.6f, 0.92f);
            wrt.offsetMin = wrt.offsetMax = Vector2.zero;
            RefreshWorldLabel();

            var hint = UiFactory.Label(_canvas.transform,
                "Click a level to drive there  ·  Arrow keys to hop  ·  Enter to play  ·  ☰ Menu for office",
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

        private void RefreshWorldLabel()
        {
            if (_worldLabel == null || _c == null) return;
            var seq = WorldSystem.Sequence(_c);
            int idx = 0;
            for (int i = 0; i < seq.Count; i++) if (seq[i].Id == _world.Id) { idx = i; break; }
            int cleared = 0;
            foreach (var city in _world.Cities) if (WorldSystem.IsCityCleared(_c, city.Id)) cleared++;
            _worldLabel.text = $"WORLD {idx + 1} — {_world.Name.ToUpper()}   <size=12>{cleared}/{_world.Cities.Count} cities</size>";
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
                if (Vector2.Dot(d.normalized, dir) < 0.45f) continue;
                float score = d.magnitude - Vector2.Dot(d, dir) * 0.25f;
                if (score < bestScore) { bestScore = score; best = n; }
            }
            if (best != null) DriveTo(best);
        }

        private void UpdateEnter()
        {
            if (_enterLabel == null || _current == null) return;
            foreach (var n in _nodes)
                if (n.Rt != null) n.Rt.localScale = Vector3.one * (n == _current ? 1.18f : 1f);

            string verb;
            bool actionable = true;
            switch (_current.Kind)
            {
                case Kind.Hq:
                    verb = "🏢  Enter"; break;
                case Kind.NextWorld:
                    verb = _current.Unlocked ? "✈  Fly to" : "🔒  Locked —";
                    actionable = _current.Unlocked;
                    break;
                default: // City
                    if (!_current.Unlocked) { verb = "🔒  Locked —"; actionable = false; }
                    else if (_current.Cleared)
                        verb = (_c != null && TerritorySystem.IsCityUnlocked(_c, _current.Client.Id))
                            ? "🚚  Drive into" : "🗺  Travel to";
                    else verb = "🗺  Travel to";
                    break;
            }
            _enterLabel.text = $"{verb} {_current.Title}";
            var img = _enterBtn.GetComponent<Image>();
            if (img != null) img.color = actionable ? UiTheme.Positive : UiTheme.PanelDark;
        }

        // ---- interaction ----------------------------------------------------

        private void Interact(Node node)
        {
            if (node == null) return;
            switch (node.Kind)
            {
                case Kind.Hq:
                    GameManager.Instance.GoToOffice();
                    break;
                case Kind.NextWorld:
                    if (node.Unlocked) AdvanceToNextWorld();
                    else Toasts.Show($"Clear every city in {_world.Name} to open {node.Title}.");
                    break;
                case Kind.City:
                    if (!node.Unlocked)
                        Toasts.Show(node.CityIndex > 0
                            ? $"Clear {CityName(_world.Cities[node.CityIndex - 1].City)} first."
                            : "Locked.");
                    else TravelTo(node.Client);
                    break;
            }
        }

        private void AdvanceToNextWorld()
        {
            var next = WorldSystem.NextWorld(_c, _world);
            if (next == null) return;
            Toasts.Show($"On to World — {next.Name}!");
            // Re-enter the overworld; CurrentWorld() now resolves to the next open world.
            GameManager.Instance.GoToTexas();
        }

        private void TravelTo(LocalClient client)
        {
            if (_c == null || client == null) return;
            // The SMW path is the only gate; meetings still respect their own stage cooldown,
            // but you can always attempt the commute to clear the level.
            GameManager.Instance.PendingClientId = client.Id;
            GameManager.Instance.TravelToCity(client.Id);
        }

        // ---- helpers --------------------------------------------------------

        private Color CityColor(Node node)
        {
            if (node.Cleared) return new Color(0.34f, 0.62f, 1f);   // blue = done
            if (!node.Unlocked) return new Color(0.45f, 0.45f, 0.50f); // grey = locked
            return new Color(0.34f, 0.80f, 0.44f);                  // green = playable
        }

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
                c.acknowledgedRank = rank;
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
