using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using Fitzmark.BDRSim.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// The Texas overworld for LOCAL accounts. Shows a greybox map of Texas with a
    /// node per local client (colored by status) and a selectable board: any account
    /// at/under your level is reachable, but a meeting starts a time-gap cooldown
    /// before its next, harder stage opens. Picking one launches the platformer
    /// "commute" to that client. Attach to a GameObject in the Texas scene.
    /// </summary>
    public class TexasMapController : MonoBehaviour
    {
        private Canvas _canvas;

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.Texas;
            BuildMap();
            BuildBoard();
        }

        private void BuildMap()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(-6f, 34f, -34f);
                cam.transform.LookAt(new Vector3(-6f, 0f, 0f));
                cam.fieldOfView = 46f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.14f, 0.20f);
                cam.farClipPlane = 600f;
            }
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lgo = new GameObject("Sun");
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.1f;
                lgo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Texas";
            ground.transform.localScale = new Vector3(9f, 1f, 9f);
            SetMat(ground, Mat(new Color(0.45f, 0.40f, 0.28f)));

            var c = GameManager.Instance.Profile;
            int day = c != null ? c.career.day : 1;
            foreach (var client in TerritoryRegistry.All)
            {
                var node = GameObject.CreatePrimitive(PrimitiveType.Cube);
                node.name = client.City;
                node.transform.position = new Vector3(client.MapX, 1f, client.MapZ);
                node.transform.localScale = new Vector3(2.2f, 2f, 2.2f);
                SetMat(node, Mat(NodeColor(c, client, day)));
            }
        }

        private static Color NodeColor(BDRCharacter c, LocalClient client, int day)
        {
            if (c == null) return Color.gray;
            if (TerritorySystem.IsClosed(c, client.Id)) return new Color(0.30f, 0.60f, 1f);
            if (!TerritorySystem.IsUnlocked(c, client)) return new Color(0.40f, 0.40f, 0.45f);
            if (!TerritorySystem.IsAvailable(c, client, day)) return new Color(0.90f, 0.70f, 0.20f);
            return new Color(0.30f, 0.80f, 0.40f);
        }

        private void BuildBoard()
        {
            _canvas = UiFactory.CreateScreenCanvas("TexasHud");

            var panel = UiFactory.Panel(_canvas.transform, UiTheme.Panel, "Board");
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.52f, 0f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel.gameObject, pad: 16, spacing: 8, expandH: false);

            UiFactory.Label(panel.transform, "TEXAS — LOCAL TERRITORY", 22, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            var c = GameManager.Instance.Profile;
            int day = c != null ? c.career.day : 1;
            UiFactory.Label(panel.transform,
                c != null ? $"Day {day} · Level {c.level} — visit any unlocked account in person" : "",
                13, UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);

            if (!string.IsNullOrEmpty(GameManager.Instance.CareerFlash))
            {
                UiFactory.Label(panel.transform, GameManager.Instance.CareerFlash, 13, UiTheme.Positive,
                    TextAnchor.UpperLeft, FontStyle.Bold);
                GameManager.Instance.CareerFlash = null;
            }

            var content = UiFactory.MakeScrollView(panel.transform, new Color(0f, 0f, 0f, 0.12f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);
            foreach (var client in TerritoryRegistry.All)
                BuildRow(content, client, c, day);

            var footer = UiFactory.Panel(panel.transform, Clear, "Footer").gameObject;
            UiFactory.HLayout(footer, spacing: 10, expandW: true, expandH: true);
            UiFactory.Size(footer, prefH: 50f);
            var office = UiFactory.Button(footer.transform, "Office",
                () => GameManager.Instance.GoToOffice(), UiTheme.PanelDark, UiTheme.TextMuted, 15,
                TextAnchor.MiddleCenter);
            UiFactory.Size(office.gameObject, flexW: 1f);
            var menu = UiFactory.Button(footer.transform, "Menu",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.PanelDark, UiTheme.TextMuted, 15,
                TextAnchor.MiddleCenter);
            UiFactory.Size(menu.gameObject, flexW: 1f);
        }

        private void BuildRow(Transform parent, LocalClient client, BDRCharacter c, int day)
        {
            var row = UiFactory.Panel(parent, UiTheme.PanelDark, "Row").gameObject;
            UiFactory.HLayout(row, pad: 10, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 68f, flexW: 1f);

            int stage = c != null ? TerritorySystem.CurrentStage(c, client.Id) : 1;
            bool closed = c != null && TerritorySystem.IsClosed(c, client.Id);
            bool unlocked = c != null && TerritorySystem.IsUnlocked(c, client);
            bool available = c != null && TerritorySystem.IsAvailable(c, client, day);

            string status = closed ? "<color=#5FBF7F>CLOSED ✓</color>"
                : !unlocked ? $"<color=#9AA0AA>Reach level {client.RequiredLevel} to unlock</color>"
                : !available ? $"<color=#EAA833>Next stage in {TerritorySystem.DaysUntilAvailable(c, client.Id, day)} day(s)</color>"
                : $"<color=#5FBF7F>Stage {stage}: {TerritorySystem.StageName(stage)} — ready</color>";

            var info = UiFactory.Label(row.transform,
                $"<b>{client.Company}</b>  <size=12>{client.City}</size>\n<size=12>{status}</size>",
                15, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(info.gameObject, flexW: 1f);

            var btn = UiFactory.Button(row.transform, available ? "Travel ▶" : "—",
                () => { if (available) Travel(client); },
                available ? UiTheme.Positive : UiTheme.PanelDark,
                available ? Color.white : UiTheme.TextMuted, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(btn.gameObject, prefW: 120f);
            btn.interactable = available;
        }

        private void Travel(LocalClient client)
        {
            var c = GameManager.Instance.Profile;
            if (c == null) return;
            int day = c.career.day;
            if (!TerritorySystem.IsAvailable(c, client, day)) return;

            int stage = TerritorySystem.CurrentStage(c, client.Id);
            int difficulty = TerritorySystem.StageDifficulty(c, client);
            int seed = StableHash(client.Id) + stage * 7919 + day;

            var scenario = ProspectGenerator.Generate(difficulty, seed);
            scenario.prospect.companyName = client.Company;
            scenario.prospect.location = client.City;
            scenario.title = $"{TerritorySystem.StageName(stage)}: {client.Company}";
            scenario.briefing =
                $"{client.City} — {TerritorySystem.StageName(stage)} with {client.Company}. " + scenario.briefing;

            c.inPersonMeetings++;
            var done = QuestSystem.Sync(c);
            if (done.Count > 0) GameManager.Instance.CareerFlash = QuestSystem.FlashFor(done);
            GameManager.Instance.SaveProfile();

            GameManager.Instance.PendingClientId = client.Id;
            GameManager.Instance.HubScene = SceneNames.Texas;
            GameManager.Instance.StartTravel(scenario, true);
        }

        private static int StableHash(string s)
        {
            int h = 17;
            foreach (char ch in s) h = h * 31 + ch;
            return h;
        }

        private static void SetMat(GameObject go, Material m)
        {
            if (m == null) return;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        private static Material Mat(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return m;
        }

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    }
}
