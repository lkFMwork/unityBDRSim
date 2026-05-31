using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Real-time fighting match against the gatekeeper (replaces the turn-based
    /// gauntlet). Best of 3 rounds with a timer and an announcer; you control the
    /// left fighter, an AI controls the gatekeeper. Win the match → into the meeting;
    /// lose → you didn't get past. Attach to a GameObject in the GatekeeperDuel scene.
    ///
    /// Controls: A/D move · W/Space jump · J light · K heavy · U launcher · L special ·
    /// I (or hold away + L) projectile · LeftShift block. Launchers pop the foe up for a
    /// juggle; combos (hits during hitstun) scale damage down so they're strong but fair.
    /// </summary>
    public class FightController : MonoBehaviour
    {
        private enum Phase { Intro, Fighting, RoundEnd, MatchEnd }

        private Canvas _canvas;
        private Fighter _player;
        private Fighter _gk;
        private FighterAI _ai;
        private string _company = "the gatekeeper";

        private Phase _phase = Phase.Intro;
        private float _phaseTimer;
        private bool _introFightShown;
        private float _roundTime;
        private int _round = 1;
        private int _playerWins;
        private int _gkWins;
        private bool _matchBuilt;

        private UiFactory.Meter _pHealth;
        private UiFactory.Meter _gHealth;
        private TMP_Text _pName, _gName, _timer, _announce, _combo;
        private int _comboCount;
        private float _comboTimer;
        private float _announceUntil;

        private static readonly Vector3 PlayerStart = new Vector3(-3f, 0f, 0f);
        private static readonly Vector3 GkStart = new Vector3(3f, 0f, 0f);

        private void Start()
        {
            var scenario = GameManager.Instance.ResolveActiveScenario();
            _company = scenario != null && scenario.prospect != null ? scenario.prospect.companyName : "Front Desk";

            int diff = 4;
            if (scenario != null)
            {
                diff = scenario.difficulty == DifficultyTier.Hard ? 8
                    : scenario.difficulty == DifficultyTier.Medium ? 5 : 2;
                if (scenario.isKeyAccount) diff = Mathf.Min(diff + 2, 10);
            }

            BuildArena();

            var c = GameManager.Instance.Profile;
            float pHp = 100f, pDmg = 1f;
            if (c != null)
            {
                pHp = 100f + (c.attributes.charisma - 5) * 8f;
                pDmg = 1f + (c.attributes.negotiation - 5) * 0.05f;
            }
            var playerCfg = c != null ? c.avatar : new AvatarConfig();

            _player = SpawnFighter(PlayerStart, true, playerCfg, Mathf.Max(60f, pHp), pDmg);
            _gk = SpawnFighter(GkStart, false, GatekeeperConfig(), 80f + diff * 8f, 0.8f + diff * 0.05f);
            _player.Opponent = _gk;
            _gk.Opponent = _player;
            _player.HitConnected += OnPlayerHit;
            _ai = new FighterAI(diff);

            BuildHud();
            StartRound();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_comboTimer > 0f) { _comboTimer -= dt; if (_comboTimer <= 0f) _comboCount = 0; }
            if (_announce != null && Time.time > _announceUntil) _announce.text = "";

            switch (_phase)
            {
                case Phase.Intro:
                    TickIdle();
                    _phaseTimer -= dt;
                    if (!_introFightShown && _phaseTimer <= 1.0f)
                    {
                        _introFightShown = true;
                        Announce("FIGHT!", 1.0f);
                    }
                    if (_phaseTimer <= 0f) { _phase = Phase.Fighting; _roundTime = 60f; }
                    break;

                case Phase.Fighting:
                    _player.Tick(ReadPlayerIntent());
                    _gk.Tick(_ai.Tick(_gk, _player, dt));
                    _roundTime -= dt;
                    if (_gk.IsKO) EndRound(true);
                    else if (_player.IsKO) EndRound(false);
                    else if (_roundTime <= 0f) EndRound(_player.Health >= _gk.Health);
                    break;

                case Phase.RoundEnd:
                    TickIdle();
                    _phaseTimer -= dt;
                    if (_phaseTimer <= 0f)
                    {
                        if (_playerWins >= 2 || _gkWins >= 2) _phase = Phase.MatchEnd;
                        else { _round++; StartRound(); }
                    }
                    break;

                case Phase.MatchEnd:
                    TickIdle();
                    if (!_matchBuilt) { _matchBuilt = true; BuildMatchResult(); }
                    break;
            }

            RefreshHud();
        }

        private void TickIdle()
        {
            _player?.Tick(default);
            _gk?.Tick(default);
        }

        private FighterIntent ReadPlayerIntent()
        {
            float move = Input.GetAxisRaw("Horizontal");
            // Holding "away" from the opponent + Special throws the projectile (the move you
            // can see fly); Special alone is the close-range power strike.
            bool away = _player != null && ((move < -0.1f && _player.FacingRight) ||
                                            (move > 0.1f && !_player.FacingRight));
            AttackType atk =
                  Input.GetKeyDown(KeyCode.J) ? AttackType.Light
                : Input.GetKeyDown(KeyCode.K) ? AttackType.Heavy
                : Input.GetKeyDown(KeyCode.U) ? AttackType.Launcher
                : Input.GetKeyDown(KeyCode.L) ? (away ? AttackType.Projectile : AttackType.Special)
                : Input.GetKeyDown(KeyCode.I) ? AttackType.Projectile
                : AttackType.None;
            return new FighterIntent
            {
                move = move,
                jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W),
                attack = atk,
                block = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.S)
            };
        }

        private void StartRound()
        {
            _player.ResetForRound(PlayerStart, true);
            _gk.ResetForRound(GkStart, false);
            _phase = Phase.Intro;
            _phaseTimer = 2.0f;
            _introFightShown = false;
            _comboCount = 0;
            Announce($"ROUND {_round}", 1.2f);
        }

        private void EndRound(bool playerWon)
        {
            if (playerWon) { _playerWins++; _gk.PlayVictory(); _player.PlayVictory(); Announce("K.O.!", 2f); }
            else { _gkWins++; _gk.PlayVictory(); Announce("DOWN!", 2f); }
            _phase = Phase.RoundEnd;
            _phaseTimer = 2.2f;
        }

        private void OnPlayerHit(bool combo)
        {
            _comboCount = combo ? _comboCount + 1 : 1;
            _comboTimer = 1.3f;
        }

        // ---- spawn / arena --------------------------------------------------

        private static Fighter SpawnFighter(Vector3 pos, bool faceRight, AvatarConfig cfg, float maxHp, float dmg)
        {
            var go = new GameObject(faceRight ? "PlayerFighter" : "GatekeeperFighter");
            go.transform.position = pos;

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Body carries the FighterRig (procedural lunge/recoil/flash via localPosition);
            // a static Kenney platformer character is parented inside it — static + procedural
            // compose cleanly (no idle animation fighting the lunges), which suits the arcade
            // fighter better than a Mixamo rig. Player and gatekeeper use different characters.
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            string charModel = faceRight
                ? "Models/kenney_platformer-kit/Models/FBX format/character-oopi"
                : "Models/kenney_platformer-kit/Models/FBX format/character-oozi";
            ModelLibrary.Spawn(charModel, body.transform, Vector3.zero, faceRight ? 90f : -90f, 1f,
                placeholderColor: faceRight ? new Color(0.3f, 0.55f, 0.85f) : new Color(0.8f, 0.3f, 0.3f),
                placeholderLabel: false, fitHeight: 1.7f);
            body.AddComponent<FighterRig>();

            var fighter = go.AddComponent<Fighter>(); // Awake grabs the CC + the child rig
            fighter.Setup(maxHp, dmg);
            return fighter;
        }

        private static AvatarConfig GatekeeperConfig() => new AvatarConfig
        {
            skinTone = 2, outfitColor = 1, accentColor = 4, hairColor = 0, build = 2, height = 1.05f
        };

        private void BuildArena()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.4f, -8.5f);
                cam.transform.LookAt(new Vector3(0f, 1.2f, 0f));
                cam.fieldOfView = 46f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.10f, 0.06f, 0.08f);
            }
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lgo = new GameObject("Arena Light");
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.1f;
                lgo.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 1f);
            var fr = floor.GetComponent<Renderer>();
            if (fr != null) fr.sharedMaterial = Mat(new Color(0.18f, 0.16f, 0.20f));
        }

        // ---- HUD ------------------------------------------------------------

        private void BuildHud()
        {
            _canvas = UiFactory.CreateScreenCanvas("FightHud");

            _pName = AnchoredLabel(new Vector2(0.04f, 0.93f), new Vector2(0.45f, 0.99f), "YOU",
                TextAnchor.MiddleLeft);
            _gName = AnchoredLabel(new Vector2(0.55f, 0.93f), new Vector2(0.96f, 0.99f), _company,
                TextAnchor.MiddleRight);

            _pHealth = MakeBar(new Vector2(0.04f, 0.865f), new Vector2(0.49f, 0.925f), UiTheme.Positive, false);
            _gHealth = MakeBar(new Vector2(0.51f, 0.865f), new Vector2(0.96f, 0.925f), UiTheme.Danger, true);

            _timer = AnchoredLabel(new Vector2(0.42f, 0.92f), new Vector2(0.58f, 1f), "", TextAnchor.MiddleCenter);
            _timer.fontSize = 26;

            _announce = AnchoredLabel(new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.62f), "",
                TextAnchor.MiddleCenter);
            _announce.fontSize = 46;
            _announce.color = UiTheme.Warning;

            _combo = AnchoredLabel(new Vector2(0.05f, 0.6f), new Vector2(0.45f, 0.7f), "", TextAnchor.MiddleLeft);
            _combo.fontSize = 22;
            _combo.color = UiTheme.AccentStrong;

            var hint = AnchoredLabel(new Vector2(0f, 0f), new Vector2(1f, 0.06f),
                "A/D move · W jump · J light · K heavy · U launcher · L special · I/away+L projectile · Shift block",
                TextAnchor.MiddleCenter);
            hint.fontSize = 14;
            hint.color = UiTheme.TextMuted;
        }

        private void RefreshHud()
        {
            if (_player != null)
            {
                _pHealth.Set(_player.MaxHealth > 0 ? _player.Health / _player.MaxHealth : 0f);
                _pName.text = $"YOU   {RoundPips(_playerWins)}";
            }
            if (_gk != null)
            {
                _gHealth.Set(_gk.MaxHealth > 0 ? _gk.Health / _gk.MaxHealth : 0f);
                _gName.text = $"{RoundPips(_gkWins)}   {_company}";
            }
            if (_timer != null)
                _timer.text = _phase == Phase.Fighting ? Mathf.CeilToInt(Mathf.Max(0f, _roundTime)).ToString() : "";
            if (_combo != null)
                _combo.text = (_comboCount >= 2 && _comboTimer > 0f) ? $"{_comboCount} HIT COMBO!" : "";
        }

        private static string RoundPips(int wins) => wins >= 2 ? "● ●" : wins == 1 ? "● ○" : "○ ○";

        private void BuildMatchResult()
        {
            bool won = _playerWins >= 2;
            if (won) Vfx.Celebrate();
            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.82f), "Result");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);

            UiFactory.Label(overlay.transform, won ? "YOU WIN!" : "YOU LOSE",
                44, won ? UiTheme.Positive : UiTheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                won ? "You muscled past the front desk. Now close the decision-maker."
                    : "The gatekeeper held the line. No meeting this time.",
                16, UiTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Italic);

            if (won)
            {
                bool flawless = _gkWins == 0;
                if (flawless)
                    UiFactory.Label(overlay.transform, "Flawless Victory!", 16, UiTheme.Warning,
                        TextAnchor.MiddleCenter, FontStyle.Bold);
                var go = UiFactory.Button(overlay.transform, "Into the meeting ▶",
                    () => GameManager.Instance.OnGatekeeperCleared(flawless), UiTheme.Positive, Color.white,
                    18, TextAnchor.MiddleCenter);
                UiFactory.Size(go.gameObject, prefW: 260f, prefH: 54f);
            }
            else
            {
                var back = UiFactory.Button(overlay.transform, "Back",
                    () => GameManager.Instance.OnGatekeeperFailed(), UiTheme.Accent, UiTheme.TextPrimary,
                    18, TextAnchor.MiddleCenter);
                UiFactory.Size(back.gameObject, prefW: 260f, prefH: 54f);
            }
        }

        // ---- HUD helpers ----------------------------------------------------

        private TMP_Text AnchoredLabel(Vector2 min, Vector2 max, string text, TextAnchor align)
        {
            var panel = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.35f), "Lbl");
            var rt = panel.rectTransform;
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var label = UiFactory.Label(panel.transform, text, 16, UiTheme.TextPrimary, align, FontStyle.Bold);
            UiFactory.Stretch(label.rectTransform);
            return label;
        }

        private UiFactory.Meter MakeBar(Vector2 min, Vector2 max, Color color, bool mirror)
        {
            var bg = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.55f), "Bar");
            var rt = bg.rectTransform;
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var fill = UiFactory.Panel(bg.transform, color, "Fill");
            var meter = new UiFactory.Meter { Fill = fill.rectTransform, Caption = null, Mirror = mirror };
            meter.Set(1f);
            return meter;
        }

        private static Material Mat(Color color) => Fitzmark.BDRSim.UI.MaterialLibrary.Get(color);

        private void Announce(string text, float seconds)
        {
            if (_announce == null) return;
            _announce.text = text;
            _announceUntil = Time.time + seconds;
        }
    }
}
