using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Runs a single "commute" platformer level — now a true 2D sprite-tilemap stage. You must
    /// beat the level to reach a local client; on success it hands off to the meeting, on
    /// failure you didn't make it. Orthographic camera, sprite player + tiles (Kenney Pixel
    /// Platformer when imported, placeholders until then). Attach to a GameObject in the
    /// Platformer scene.
    /// </summary>
    public class PlatformerSceneController : MonoBehaviour
    {
        private const int SolidLayer = 8; // tiles live here; the 2D controller raycasts it

        private Canvas _canvas;
        private Camera _cam;
        private GameObject _player;
        private Platformer2DController _ctrl;
        private TMP_Text _statusLabel;
        private string _company = "the client";
        private bool _over;

        private void Start()
        {
            var scenario = GameManager.Instance.SelectedScenario;
            _company = scenario != null && scenario.prospect != null ? scenario.prospect.companyName : "the client";

            int difficulty = 3;
            if (scenario != null)
            {
                difficulty = scenario.difficulty == DifficultyTier.Hard ? 8
                    : scenario.difficulty == DifficultyTier.Medium ? 5 : 2;
                if (scenario.isKeyAccount) difficulty = Mathf.Min(difficulty + 2, 10);
            }

            BuildArena();

            var level = new GameObject("Level");
            Platformer2DGenerator.Build(difficulty, unchecked(System.Environment.TickCount),
                level.transform, SolidLayer, out Vector3 start);

            SpawnPlayer(start);
            BuildHud();
        }

        private void BuildArena()
        {
            _cam = Camera.main;
            if (_cam != null)
            {
                _cam.orthographic = true;            // 2D side view
                _cam.orthographicSize = 6f;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f); // sky
                _cam.transform.rotation = Quaternion.identity;
                _cam.transform.position = new Vector3(0f, 4f, -10f);
            }
        }

        private void SpawnPlayer(Vector3 start)
        {
            _player = new GameObject("Player");
            _player.transform.position = start;

            // Kinematic Rigidbody2D + trigger collider so prop pickups (coins/enemies/goal)
            // fire OnTriggerEnter2D; movement/solid collision is the controller's own raycasts.
            var rb = _player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var trigger = _player.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.7f, 1f);
            trigger.offset = new Vector2(0f, 0.5f);

            _ctrl = _player.AddComponent<Platformer2DController>();

            // Sprite body: a child SpriteRenderer driven by SpriteAnimator (Kenney pixel
            // character frames when imported, a colored placeholder square until then).
            var bodyGo = new GameObject("Body", typeof(SpriteRenderer));
            bodyGo.transform.SetParent(_player.transform, false);
            var anim = bodyGo.AddComponent<SpriteAnimator>();
            anim.Setup(bodyGo.GetComponent<SpriteRenderer>(),
                idleKey: PixelPlatformerArt.CharIdle,
                jumpKey: PixelPlatformerArt.CharJump,
                walkKeys: new[] { PixelPlatformerArt.CharWalkA, PixelPlatformerArt.CharWalkB },
                tintColor: new Color(0.55f, 0.5f, 0.9f));
            // Normalize the body to ~1.4 tiles tall; the animator owns scale (facing/squash/big).
            var sr = bodyGo.GetComponent<SpriteRenderer>();
            float baseScale = 1.4f;
            if (sr.sprite != null)
            {
                float h = sr.sprite.bounds.size.y;
                if (h > 0.0001f) baseScale = 1.4f / h;
            }
            anim.baseScale = baseScale;

            _ctrl.Init(start, anim, 1 << SolidLayer);
            _ctrl.Won += OnWon;
            _ctrl.Failed += OnFailed;
            _ctrl.LivesChanged += _ => RefreshStatus();
            _ctrl.CoinsChanged += _ => RefreshStatus();
            _ctrl.PowerChanged += _ => RefreshStatus();
        }

        private void LateUpdate()
        {
            if (_cam == null || _player == null) return;
            float t = 8f * Time.deltaTime;
            Vector3 p = _player.transform.position;
            float camX = Mathf.Lerp(_cam.transform.position.x, p.x + 2f, t);
            float camY = Mathf.Lerp(_cam.transform.position.y, Mathf.Max(4f, p.y + 1.5f), t);
            _cam.transform.position = new Vector3(camX, camY, -10f);
        }

        // ---- HUD ------------------------------------------------------------

        private void BuildHud()
        {
            _canvas = UiFactory.CreateScreenCanvas("PlatformerHud");

            var top = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.5f), "Top");
            Anchor(top.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 1f));
            _statusLabel = UiFactory.Label(top.transform, "", 16, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(_statusLabel.rectTransform);

            var bottom = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.45f), "Hint");
            Anchor(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.07f));
            var hint = UiFactory.Label(bottom.transform,
                "← →  move      Space  jump (hold for higher)      stomp enemies, mind the gaps",
                14, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Stretch(hint.rectTransform);

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (_statusLabel == null || _ctrl == null) return;
            string power = _ctrl.Big ? "  ★ BIG" : "";
            _statusLabel.text = $"Lives: {_ctrl.Lives}    Coins: {_ctrl.Coins}{power}    →   Get to {_company}";
        }

        // ---- outcomes -------------------------------------------------------

        private void OnWon()
        {
            if (_over) return;
            _over = true;
            ShowResult(true, $"You made it to {_company}!", "Into the meeting ▶",
                () => GameManager.Instance.OnTravelComplete());
        }

        private void OnFailed()
        {
            if (_over) return;
            _over = true;
            var overlay = ResultOverlay("You didn't make the meeting.", UiTheme.Danger);
            var row = UiFactory.Panel(overlay.transform, new Color(0f, 0f, 0f, 0f), "Buttons").gameObject;
            UiFactory.HLayout(row, spacing: 12, expandW: true, expandH: true);
            UiFactory.Size(row, prefH: 56f);
            var retry = UiFactory.Button(row.transform, "Retry the commute",
                () => SceneManager.LoadScene(SceneNames.Platformer), UiTheme.Accent, UiTheme.TextPrimary,
                17, TextAnchor.MiddleCenter);
            UiFactory.Size(retry.gameObject, flexW: 1f);
            var back = UiFactory.Button(row.transform, "Give up",
                () => GameManager.Instance.OnTravelFailed(), UiTheme.PanelDark, UiTheme.TextPrimary,
                17, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 160f);
        }

        private void ShowResult(bool good, string headline, string buttonLabel, System.Action onClick)
        {
            var overlay = ResultOverlay(headline, good ? UiTheme.Positive : UiTheme.Danger);
            var btn = UiFactory.Button(overlay.transform, buttonLabel, () => onClick(),
                good ? UiTheme.Positive : UiTheme.Accent, Color.white, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(btn.gameObject, prefW: 280f, prefH: 54f);
        }

        private Transform ResultOverlay(string headline, Color color)
        {
            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.8f), "Result");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);
            UiFactory.Label(overlay.transform, headline, 34, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            return overlay.transform;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
