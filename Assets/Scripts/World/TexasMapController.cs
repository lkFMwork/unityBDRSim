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
        private readonly List<Node> _nodes = new();

        // Fixed tile grid for the SMW-style map.
        private const int GridCols = 10;
        private const int GridRows = 7;
        private readonly List<Vector2Int> _cells = new(); // level cells, in grid coords (col,row)
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

            // Assign each city (and the gate) a grid cell along a serpentine route, then compose a
            // themed SMW map from real Kenney map tiles (grass base + path overlays + decorations).
            LayoutCells();

            // The map rect is a fixed-aspect grid filling the canvas; tiles live inside it.
            var mapGo = new GameObject("Map", typeof(RectTransform));
            mapGo.transform.SetParent(_canvas.transform, false);
            _root = mapGo.GetComponent<RectTransform>();
            FitMapRect(_root, GridCols * 64, GridRows * 64);

            BuildTileMap();
            BuildNodes();
            BuildTruck();
            BuildHud();

            // Start the truck on the player's current frontier city (first uncleared unlocked).
            var startNode = _nodes.Find(n => n.Kind == Kind.City && n.Unlocked && !n.Cleared)
                            ?? _nodes.Find(n => n.Kind == Kind.City) ?? _nodes[0];
            _truck.anchorMin = _truck.anchorMax = startNode.Anchor;
            _current = startNode;
            UpdateEnter();
        }

        // Compose the world from real map tiles: a grass/terrain base in every cell, then yellow
        // path overlays forming the trail through the level cells, then scattered decorations.
        private void BuildTileMap()
        {
            var theme = WorldMapTheme.For(_world.Id);
            var rng = new System.Random(StableHash(_world.Id));

            var dirs = new Dictionary<Vector2Int, PathDir>();
            for (int i = 0; i < _cells.Count - 1; i++) CarvePath(dirs, _cells[i], _cells[i + 1]);

            // Region kind per cell: an east–west biome band gives each state internal geography
            // (forested/lush west → plains centre → mountains in the east, water along the top/edges).
            for (int gy = 0; gy < GridRows; gy++)
                for (int gx = 0; gx < GridCols; gx++)
                {
                    var cell = new Vector2Int(gx, gy);
                    float ew = gx / (float)(GridCols - 1);
                    var region = RegionAt(theme, cell, ew, dirs);

                    // 1) terrain base (animated water gets the WaterAnimator).
                    if (region == Region.Water) WaterTile(cell);
                    else TerrainTile(theme, region, cell);

                    // 2) yellow trail overlay where the path runs (Kenney path reads cleanly as SMW trail).
                    if (dirs.TryGetValue(cell, out var d))
                    {
                        var key = PathTileFor(d);
                        if (key != null) TileImage(key, cell, sorting: 1);
                    }
                    // 3) terrain objects off the trail: mountains east, forest/hills elsewhere.
                    else if (!NearTrail(dirs, cell)) LpcDecoration(theme, region, cell, ew, rng);
                }
        }

        private enum Region { Grass, Sand, Snow, MountainBand, Water }

        // Which terrain a cell is, from biome + east–west position. Top edge rows tend to water.
        private Region RegionAt(WorldMapTheme theme, Vector2Int cell, float ew, Dictionary<Vector2Int, PathDir> dirs)
        {
            bool onTrail = dirs.ContainsKey(cell);
            // Water along the very top row (and far corners), but never under the trail.
            if (!onTrail && cell.y >= GridRows - 1 && theme.Biome != MapBiome.Desert) return Region.Water;

            switch (theme.Biome)
            {
                case MapBiome.Desert:
                    return ew > 0.82f ? Region.MountainBand : Region.Sand;
                case MapBiome.Forest:
                    return ew > 0.76f ? Region.MountainBand : Region.Grass;
                case MapBiome.Bluegrass: // TN/AL/GA: mountains in the east
                    return ew > 0.72f ? Region.MountainBand : Region.Grass;
                default: // Heartland/Plains
                    return ew > 0.86f ? Region.MountainBand : Region.Grass;
            }
        }

        private void TerrainTile(WorldMapTheme theme, Region region, Vector2Int cell)
        {
            Sprite s = region switch
            {
                Region.Sand => LpcOverworldArt.Sand,
                Region.Snow => LpcOverworldArt.Snow,
                // mountain band sits on grass (desert: sand) — the mountain object is added as decor
                Region.MountainBand => theme.Biome == MapBiome.Desert ? LpcOverworldArt.Sand : LpcOverworldArt.Grass,
                _ => theme.Biome == MapBiome.Desert ? LpcOverworldArt.Sand : LpcOverworldArt.Grass
            };
            SpriteTile(s, cell, sorting: 0);
        }

        private void WaterTile(Vector2Int cell)
        {
            var frames = LpcOverworldArt.WaterFrames();
            var img = SpriteTile(frames.Length > 0 ? frames[0] : LpcOverworldArt.Grass, cell, sorting: 0);
            var anim = img.gameObject.AddComponent<WaterAnimator>();
            anim.frames = frames;
            anim.phase = (cell.x * 0.37f + cell.y * 0.19f); // de-sync neighbouring tiles
        }

        // Mountains in the mountain band; forests/hills as scattered objects elsewhere.
        private void LpcDecoration(WorldMapTheme theme, Region region, Vector2Int cell, float ew, System.Random rng)
        {
            if (region == Region.MountainBand)
            {
                if (rng.NextDouble() < 0.55)
                    SpriteTile(LpcOverworldArt.Mountain(rng.Next(5)), cell, sorting: 2, scaleW: 1.8f, scaleH: 1.8f);
                return;
            }
            if (rng.NextDouble() >= 0.20) return;
            Sprite s = theme.Biome switch
            {
                MapBiome.Desert => LpcOverworldArt.HillDesert(rng.Next(5)),
                MapBiome.Forest => LpcOverworldArt.Tree(true),
                _ => rng.Next(2) == 0 ? LpcOverworldArt.Tree(false) : LpcOverworldArt.Hill(rng.Next(5))
            };
            SpriteTile(s, cell, sorting: 2, scaleW: 1.4f, scaleH: 1.4f);
        }

        [System.Flags]
        private enum PathDir { None = 0, Up = 1, Down = 2, Left = 4, Right = 8 }

        // Walk an L-shaped route between two cells, recording which directions the path enters/
        // leaves each cell so the right connector tile can be chosen.
        private void CarvePath(Dictionary<Vector2Int, PathDir> dirs, Vector2Int a, Vector2Int b)
        {
            var cur = a;
            void Step(Vector2Int next, PathDir outDir, PathDir inDir)
            {
                Add(dirs, cur, outDir);
                Add(dirs, next, inDir);
                cur = next;
            }
            // Horizontal first, then vertical (matches the serpentine layout's right-angles).
            while (cur.x != b.x)
            {
                int nx = cur.x + (cur.x < b.x ? 1 : 0) - (cur.x > b.x ? 1 : 0);
                bool right = b.x > cur.x;
                Step(new Vector2Int(cur.x + (right ? 1 : -1), cur.y),
                    right ? PathDir.Right : PathDir.Left, right ? PathDir.Left : PathDir.Right);
            }
            while (cur.y != b.y)
            {
                bool up = b.y > cur.y;
                Step(new Vector2Int(cur.x, cur.y + (up ? 1 : -1)),
                    up ? PathDir.Up : PathDir.Down, up ? PathDir.Down : PathDir.Up);
            }
        }

        private static void Add(Dictionary<Vector2Int, PathDir> d, Vector2Int c, PathDir dir)
        {
            d.TryGetValue(c, out var cur);
            d[c] = cur | dir;
        }

        private bool NearTrail(Dictionary<Vector2Int, PathDir> dirs, Vector2Int c)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dirs.ContainsKey(new Vector2Int(c.x + dx, c.y + dy))) return true;
            return false;
        }

        // Pick the path connector sprite matching the set of directions this cell joins.
        private static string PathTileFor(PathDir d)
        {
            bool u = (d & PathDir.Up) != 0, dn = (d & PathDir.Down) != 0;
            bool l = (d & PathDir.Left) != 0, r = (d & PathDir.Right) != 0;
            int count = (u ? 1 : 0) + (dn ? 1 : 0) + (l ? 1 : 0) + (r ? 1 : 0);
            if (count >= 4) return MapPackArt.PathCross;
            if (count == 3)
            {
                if (!u) return MapPackArt.PathTeeDown;   // L+R+D
                if (!dn) return MapPackArt.PathTeeUp;    // L+R+U
                return l ? MapPackArt.PathTeeDown : MapPackArt.PathTeeUp; // fallbacks
            }
            if (l && r) return MapPackArt.PathH;
            if (u && dn) return MapPackArt.PathV;
            if (dn && r) return MapPackArt.PathCornerDR;
            if (dn && l) return MapPackArt.PathCornerDL;
            if (u && r) return MapPackArt.PathCornerUR;
            if (u && l) return MapPackArt.PathCornerUL;
            // a single-direction stub → use a straight in that axis
            if (l || r) return MapPackArt.PathH;
            if (u || dn) return MapPackArt.PathV;
            return null;
        }

        // Layer containers so z-order is correct regardless of creation order: base < path < decor.
        private RectTransform _baseLayer, _pathLayer, _decorLayer;

        private RectTransform Layer(ref RectTransform layer, string name)
        {
            if (layer != null) return layer;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root, false);
            layer = go.GetComponent<RectTransform>();
            UiFactory.Stretch(layer);
            return layer;
        }

        // A Kenney path-overlay tile by key (the yellow trail), one cell. sorting selects the layer.
        private Image TileImage(string spriteKey, Vector2Int cell, int sorting, float scale = 1f) =>
            PlaceTile(SpriteLibrary.GetUnit(spriteKey, new Color(0.4f, 0.6f, 0.35f)), cell, sorting, scale, scale);

        // An LPC sprite in a grid cell. scaleW/scaleH > 1 lets wide objects (mountains/hills) spill
        // past one cell, anchored to the cell's bottom-centre so they sit on the ground believably.
        private Image SpriteTile(Sprite sprite, Vector2Int cell, int sorting, float scaleW = 1f, float scaleH = 1f) =>
            PlaceTile(sprite, cell, sorting, scaleW, scaleH);

        private Image PlaceTile(Sprite sprite, Vector2Int cell, int sorting, float scaleW, float scaleH)
        {
            RectTransform parent = sorting == 0 ? Layer(ref _baseLayer, "Base")
                : sorting == 1 ? Layer(ref _pathLayer, "Path") : Layer(ref _decorLayer, "Decor");
            var go = new GameObject("Tile", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            if (sorting == 2)
            {
                // Decoration object: width grows symmetrically, height grows UPWARD from the cell's
                // bottom edge so mountains/trees/hills sit on the ground rather than float.
                float padX = (1f - scaleW) * 0.5f;
                rt.anchorMin = new Vector2((cell.x + padX) / GridCols, cell.y / (float)GridRows);
                rt.anchorMax = new Vector2((cell.x + 1f - padX) / GridCols, (cell.y + scaleH) / GridRows);
            }
            else // base/path fill the cell exactly
            {
                rt.anchorMin = new Vector2(cell.x / (float)GridCols, cell.y / (float)GridRows);
                rt.anchorMax = new Vector2((cell.x + 1f) / GridCols, (cell.y + 1f) / GridRows);
            }
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = sorting == 2;
            img.raycastTarget = false;
            return img;
        }

        // Assign each city — and the next-world gate — a grid cell along a serpentine route that
        // fills the map. _cells[i] is city i; the last entry (if any) is the gate. The tile-map
        // renderer carves the connecting trail through these same cells.
        private void LayoutCells()
        {
            _cells.Clear();
            int n = _world.Cities.Count + (WorldSystem.NextWorld(_c, _world) != null ? 1 : 0);
            int usableCols = GridCols - 2;          // 1-tile margin each side
            int perRow = Mathf.Min(usableCols, Mathf.Max(3, Mathf.CeilToInt(n / 2f)));
            int rowsUsed = Mathf.Max(1, Mathf.CeilToInt(n / (float)perRow));
            for (int i = 0; i < n; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                if (row % 2 == 1) col = perRow - 1 - col;         // snake back
                int gx = 1 + (perRow <= 1 ? 0 : Mathf.RoundToInt(col * (usableCols - 1) / (float)(perRow - 1)));
                // rows fill from upper-middle downward, leaving the top row for sky/water
                int gy = GridRows - 2 - (rowsUsed <= 1 ? 0 : Mathf.RoundToInt(row * (GridRows - 3) / (float)(rowsUsed - 1)));
                _cells.Add(new Vector2Int(Mathf.Clamp(gx, 0, GridCols - 1), Mathf.Clamp(gy, 0, GridRows - 1)));
            }
        }

        private void BuildNodes()
        {
            var cities = _world.Cities;
            for (int i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                var node = new Node
                {
                    Kind = Kind.City,
                    Title = CityName(city.City),
                    Anchor = CellAnchor(_cells[i]),
                    Client = city,
                    CityIndex = i,
                    Unlocked = _c != null && WorldSystem.IsCityUnlocked(_c, _world, i),
                    Cleared = _c != null && WorldSystem.IsCityCleared(_c, city.Id)
                };
                MakeMarker(node, CityColor(node), false);
                _nodes.Add(node);

                // HQ home-base node sits just above the player's OWN branch office (one only).
                if (_c != null && city.Id == _c.homeBranchId)
                {
                    var hq = new Node
                    {
                        Kind = Kind.Hq,
                        Title = "FITZMARK HQ",
                        Anchor = CellAnchor(_cells[i]) + new Vector2(0f, 0.13f),
                        Unlocked = true
                    };
                    MakeMarker(hq, new Color(0.95f, 0.82f, 0.36f), true);
                    _nodes.Add(hq);
                }
            }

            // Gate to the next world, on the final cell past the last city.
            var next = WorldSystem.NextWorld(_c, _world);
            if (next != null && _cells.Count > cities.Count)
            {
                bool open = WorldSystem.IsWorldCleared(_c, _world);
                var gate = new Node
                {
                    Kind = Kind.NextWorld,
                    Title = open ? $"▶ {next.Name}" : $"🔒 {next.Name}",
                    Anchor = CellAnchor(_cells[cities.Count]),
                    Unlocked = open
                };
                MakeMarker(gate, open ? new Color(0.36f, 0.78f, 0.62f) : new Color(0.5f, 0.5f, 0.55f), true);
                _nodes.Add(gate);
            }
        }

        // Center of grid cell (col,row) as a normalized anchor on the map rect (y up).
        private Vector2 CellAnchor(Vector2Int cell) =>
            new Vector2((cell.x + 0.5f) / GridCols, (cell.y + 0.5f) / GridRows);

        // Fill the 1280x720 canvas with the map, preserving the tile texture's aspect (letterbox
        // the shorter axis) so square tiles stay square and cell anchors line up with the art.
        private static void FitMapRect(RectTransform rt, int texW, int texH)
        {
            const float canvasW = 1280f, canvasH = 720f;
            float scale = Mathf.Min(canvasW / texW, canvasH / texH);
            float w = texW * scale, h = texH * scale;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
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
