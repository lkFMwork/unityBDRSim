using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Persistent cockpit HUD: a top bar (BDR name · cash · day/week · level) and a
    /// quick-travel menu button, carried across the explorable hub scenes so money and
    /// navigation are always one glance / one click away — the GTA "phone + status bar"
    /// feel. Self-bootstraps into a persistent canvas; shown only on the walkable hubs
    /// (Office, City) to avoid overlapping screens that have their own chrome. Cash
    /// changes count up and punch via <see cref="Tween"/>.
    /// </summary>
    public class HudShell : MonoBehaviour
    {
        private static HudShell _instance;

        private Canvas _canvas;
        private GameObject _quickMenu;
        private Text _name, _cash, _day, _level;
        private float _lastCash;
        private int _lastDay = int.MinValue, _lastLevel = int.MinValue;
        private bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[HudShell]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HudShell>();
        }

        private void Awake()
        {
            Build();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyVisibility(SceneManager.GetActiveScene().name);
            Refresh(true);
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CloseQuickMenu();
            ApplyVisibility(scene.name);
            Refresh(true);
        }

        private static bool ShowsOn(string sceneName) =>
            sceneName == SceneNames.Office || sceneName == SceneNames.City;

        private void ApplyVisibility(string sceneName)
        {
            bool show = ShowsOn(sceneName) && GameManager.Instance.HasProfile;
            if (_canvas != null) _canvas.gameObject.SetActive(show);
            if (show) UiFactory.EnsureEventSystem(); // same guarded path the scenes use
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf) return;
            Refresh(false);
        }

        private void Refresh(bool immediate)
        {
            var c = GameManager.Instance.Profile;
            if (c == null) return;

            if (immediate || !Mathf.Approximately(_lastCash, c.cash))
            {
                if (immediate) { if (_cash != null) _cash.text = "$" + c.cash.ToString("N0"); }
                else { Tween.CountUp(_cash, _lastCash, c.cash); Tween.PunchScale(_cash.transform, 0.16f); }
                _lastCash = c.cash;
            }
            if (c.career.day != _lastDay)
            {
                _lastDay = c.career.day;
                if (_day != null) _day.text = $"Day {_lastDay} · Wk {(_lastDay - 1) / 5 + 1}";
            }
            if (c.level != _lastLevel)
            {
                _lastLevel = c.level;
                if (_level != null) _level.text = $"Lv {_lastLevel}";
            }
            if (_name != null) _name.text = c.DisplayName;
        }

        // ---- build ----------------------------------------------------------

        private void Build()
        {
            if (_initialized) return;
            _initialized = true;

            var canvasGo = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150; // above scene UI, below toasts (200)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildTopBar(canvasGo.transform);
            BuildQuickMenu(canvasGo.transform);
        }

        private void BuildTopBar(Transform parent)
        {
            var bar = UiFactory.Panel(parent, new Color(0.07f, 0.09f, 0.14f, 0.92f), "TopBar");
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 52f);
            rt.anchoredPosition = Vector2.zero;
            UiFactory.HLayout(bar.gameObject, pad: 12, spacing: 10, expandW: false, expandH: true);

            _name = UiFactory.Label(bar.transform, "", 17, UiTheme.AccentStrong, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(_name.gameObject, flexW: 1f);

            _cash = UiFactory.Label(bar.transform, "$0", 17, UiTheme.Positive, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Size(_cash.gameObject, prefW: 150f);

            _day = UiFactory.Label(bar.transform, "Day 1", 15, UiTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Size(_day.gameObject, prefW: 150f);

            _level = UiFactory.Label(bar.transform, "Lv 1", 15, UiTheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Size(_level.gameObject, prefW: 70f);

            var menu = UiFactory.Button(bar.transform, "☰  Menu", ToggleQuickMenu, UiTheme.Accent,
                UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(menu.gameObject, prefW: 110f);
        }

        private void BuildQuickMenu(Transform parent)
        {
            _quickMenu = UiFactory.Panel(parent, new Color(0f, 0f, 0f, 0.65f), "QuickMenu").gameObject;
            var backdrop = _quickMenu.GetComponent<Button>();
            if (backdrop == null) backdrop = _quickMenu.AddComponent<Button>();
            backdrop.transition = Selectable.Transition.None;
            backdrop.onClick.AddListener(CloseQuickMenu);
            UiFactory.Stretch((RectTransform)_quickMenu.transform);

            var panel = UiFactory.Panel(_quickMenu.transform, UiTheme.Panel, "Panel").gameObject;
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(380f, 470f);
            prt.anchoredPosition = Vector2.zero;
            UiFactory.VLayout(panel, pad: 18, spacing: 10, expandH: true, align: TextAnchor.UpperCenter);

            UiFactory.Label(panel.transform, "GO TO", 18, UiTheme.AccentStrong, TextAnchor.MiddleCenter, FontStyle.Bold);
            QuickButton(panel.transform, "CRM Dashboard", OpenCrm);
            QuickButton(panel.transform, "Freight Desk", () => GameManager.Instance.GoToFreightDesk());
            QuickButton(panel.transform, "The Office", () => GameManager.Instance.GoToOffice());
            QuickButton(panel.transform, "Texas — local clients", () => GameManager.Instance.GoToTexas());
            QuickButton(panel.transform, "Prospecting (Outreach)", OpenOutreach);
            QuickButton(panel.transform, "Business Upgrades", OpenUpgrades);
            QuickButton(panel.transform, "Main Menu", () => GameManager.Instance.ReturnToMenu());

            _quickMenu.SetActive(false);
        }

        private void OpenUpgrades()
        {
            var c = GameManager.Instance.Profile;
            if (c != null) new UpgradesView(_canvas.transform, c, null).Open();
        }

        private void OpenOutreach()
        {
            var c = GameManager.Instance.Profile;
            if (c != null) new OutreachView(_canvas.transform, c, null).Open();
        }

        private void OpenCrm()
        {
            var c = GameManager.Instance.Profile;
            if (c != null) new CrmView(_canvas.transform, c, null).Open();
        }

        private void QuickButton(Transform parent, string label, System.Action action)
        {
            var b = UiFactory.Button(parent, label, () => { CloseQuickMenu(); action(); },
                UiTheme.Accent, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(b.gameObject, flexW: 1f, prefH: 50f);
        }

        private void ToggleQuickMenu()
        {
            if (_quickMenu == null) return;
            bool open = !_quickMenu.activeSelf;
            _quickMenu.SetActive(open);
            if (open) Tween.PunchScale(_quickMenu.transform.GetChild(0), 0.06f, 0.2f);
        }

        private void CloseQuickMenu()
        {
            if (_quickMenu != null) _quickMenu.SetActive(false);
        }
    }
}
