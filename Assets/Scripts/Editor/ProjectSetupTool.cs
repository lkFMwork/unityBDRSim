using System;
using System.Collections.Generic;
using System.Reflection;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
using Fitzmark.BDRSim.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// One-click project bootstrapper. Because the playable assets (URP pipeline,
    /// scenes, sample scenario data) can't be hand-authored as YAML reliably, this
    /// tool generates them programmatically so a freshly cloned project goes from
    /// "opens in Unity" to "press Play and run a call" with a single menu click:
    ///
    ///     Tools → Fitzmark BDR → Setup Project (One-Click)
    ///
    /// Each step is also exposed individually and is safe to re-run.
    /// </summary>
    public static class ProjectSetupTool
    {
        private const string Menu = "Tools/Fitzmark BDR/";

        private const string ScenesFolder = "Assets/Scenes";
        private const string SettingsFolder = "Assets/Settings";
        private const string ResourcesFolder = "Assets/Resources";
        private const string ScenariosFolder = "Assets/Resources/Scenarios";

        private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        private const string CharacterCreateScenePath = ScenesFolder + "/CharacterCreate.unity";
        private const string CityScenePath = ScenesFolder + "/City.unity";
        private const string TexasScenePath = ScenesFolder + "/Texas.unity";
        private const string OfficeScenePath = ScenesFolder + "/Office.unity";
        private const string PlatformerScenePath = ScenesFolder + "/Platformer.unity";
        private const string GatekeeperDuelScenePath = ScenesFolder + "/GatekeeperDuel.unity";
        private const string CallFloorScenePath = ScenesFolder + "/CallFloor.unity";

        [MenuItem(Menu + "Setup Project (One-Click)", false, 0)]
        public static void SetupAll()
        {
            try
            {
                EnsureFolders();
                ConfigureUrp();
                int created = CreateSampleContent();
                BuildScenes();
                ConfigureBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(MainMenuScenePath);

                string msg = $"Fitzmark BDR Simulator is set up.\n\n" +
                             $"• {created} sample (practice) scenarios created\n" +
                             $"• MainMenu, CharacterCreate, and CallFloor scenes built\n" +
                             $"• Build settings configured\n\n" +
                             $"Press Play, then create your BDR — Sales Style, point-buy\n" +
                             $"stats, and abilities — and start the career.";
                Debug.Log("[Fitzmark BDR] " + msg.Replace("\n", " "));
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("Fitzmark BDR Simulator", msg, "Let's go");
            }
            catch (Exception e)
            {
                Debug.LogError("[Fitzmark BDR] Setup failed: " + e);
            }
        }

        // ---- folders --------------------------------------------------------

        [MenuItem(Menu + "Create Folders", false, 20)]
        public static void EnsureFolders()
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(SettingsFolder);
            EnsureFolder(ResourcesFolder);
            EnsureFolder(ScenariosFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ---- sample content -------------------------------------------------

        [MenuItem(Menu + "Create Sample Scenarios", false, 21)]
        public static int CreateSampleContent()
        {
            EnsureFolders();
            int count = 0;

            // 1) Easy — friendly-ish manufacturer, one objection.
            {
                var p = ScriptableObject.CreateInstance<ProspectProfile>();
                p.contactName = "Pat Morgan";
                p.title = "Logistics Manager";
                p.companyName = "Hoosier Components Co.";
                p.industry = "Industrial / Manufacturing";
                p.location = "Indianapolis, IN";
                p.personality = ProspectPersonality.Busy;
                p.startingPatience = 0.75f;
                p.startingTrust = 0.40f;
                p.priceSensitivity = 0.40f;
                p.incumbentProvider = "a regional broker";
                p.painPoints = new List<string>
                {
                    "Carriers no-show during peak season",
                    "Spotty communication once freight is picked up"
                };
                p.likelyObjections = new List<ObjectionType> { ObjectionType.AlreadyHaveBroker };
                p.lanes = new List<Lane>
                {
                    new Lane
                    {
                        label = "Indianapolis, IN -> Detroit, MI",
                        origin = "Indianapolis, IN", destination = "Detroit, MI",
                        miles = 290, mode = FreightMode.FullTruckload, equipment = EquipmentType.DryVan,
                        loadsPerWeek = 8, currentRatePerMile = 2.55f, fitzmarkCostPerMile = 2.15f
                    }
                };
                CreateOrReplace(p, ScenariosFolder + "/Prospect_HoosierComponents.asset");

                var s = ScriptableObject.CreateInstance<ScenarioDefinition>();
                s.scenarioId = "cold-midwest-mfg";
                s.title = "Cold Call: Midwest Manufacturer";
                s.difficulty = DifficultyTier.Easy;
                s.gatekeeperPresent = false;
                s.targetWeeklyMargin = 500f;
                s.briefing = "Hoosier Components ships parts to the Detroit auto corridor and has " +
                             "been let down on capacity. Build rapport, run discovery, and earn a load.";
                s.prospect = p;
                CreateOrReplace(s, ScenariosFolder + "/Scenario_MidwestManufacturer.asset");
                count++;
            }

            // 2) Medium — skeptical food distributor, claims history, reefer.
            {
                var p = ScriptableObject.CreateInstance<ProspectProfile>();
                p.contactName = "Dana Cole";
                p.title = "Director of Logistics";
                p.companyName = "Crossroads Foods";
                p.industry = "Food & Beverage Distribution";
                p.location = "Indianapolis, IN";
                p.personality = ProspectPersonality.Skeptical;
                p.startingPatience = 0.60f;
                p.startingTrust = 0.30f;
                p.priceSensitivity = 0.55f;
                p.incumbentProvider = "two core carriers";
                p.painPoints = new List<string>
                {
                    "Reefer capacity falls apart every summer",
                    "Got burned on a temperature claim last year"
                };
                p.likelyObjections = new List<ObjectionType>
                {
                    ObjectionType.BadPastExperience, ObjectionType.RatesTooHigh
                };
                p.lanes = new List<Lane>
                {
                    new Lane
                    {
                        label = "Indianapolis, IN -> Atlanta, GA",
                        origin = "Indianapolis, IN", destination = "Atlanta, GA",
                        miles = 530, mode = FreightMode.Refrigerated, equipment = EquipmentType.Reefer,
                        loadsPerWeek = 5, currentRatePerMile = 3.10f, fitzmarkCostPerMile = 2.65f
                    }
                };
                CreateOrReplace(p, ScenariosFolder + "/Prospect_CrossroadsFoods.asset");

                var s = ScriptableObject.CreateInstance<ScenarioDefinition>();
                s.scenarioId = "reefer-food-distributor";
                s.title = "Reefer Lane: Food Distributor";
                s.difficulty = DifficultyTier.Medium;
                s.gatekeeperPresent = false;
                s.targetWeeklyMargin = 700f;
                s.briefing = "Crossroads Foods needs reliable reefer capacity to Atlanta and is " +
                             "gun-shy after a temperature claim. Rebuild trust before you talk price.";
                s.prospect = p;
                CreateOrReplace(s, ScenariosFolder + "/Scenario_FoodDistributor.asset");
                count++;
            }

            // 3) Hard — enterprise shipper, gatekeeper, goes direct.
            {
                var p = ScriptableObject.CreateInstance<ProspectProfile>();
                p.contactName = "Alex Rivera";
                p.title = "VP of Supply Chain";
                p.companyName = "Meridian Industrial";
                p.industry = "Heavy Equipment";
                p.location = "Chicago, IL";
                p.personality = ProspectPersonality.PriceDriven;
                p.startingPatience = 0.50f;
                p.startingTrust = 0.25f;
                p.priceSensitivity = 0.80f;
                p.incumbentProvider = "an in-house fleet plus direct carriers";
                p.painPoints = new List<string>
                {
                    "Direct carriers reject overflow lanes",
                    "No single source of truth for tracking"
                };
                p.likelyObjections = new List<ObjectionType>
                {
                    ObjectionType.WeGoDirectToCarriers,
                    ObjectionType.SendMeAnEmail,
                    ObjectionType.NoTimeRightNow
                };
                p.lanes = new List<Lane>
                {
                    new Lane
                    {
                        label = "Chicago, IL -> Dallas, TX",
                        origin = "Chicago, IL", destination = "Dallas, TX",
                        miles = 925, mode = FreightMode.FullTruckload, equipment = EquipmentType.DryVan,
                        loadsPerWeek = 12, currentRatePerMile = 2.40f, fitzmarkCostPerMile = 2.10f
                    }
                };
                CreateOrReplace(p, ScenariosFolder + "/Prospect_MeridianIndustrial.asset");

                var s = ScriptableObject.CreateInstance<ScenarioDefinition>();
                s.scenarioId = "enterprise-skeptical-vp";
                s.title = "Enterprise Shipper: Skeptical VP";
                s.difficulty = DifficultyTier.Hard;
                s.gatekeeperPresent = true;
                s.targetWeeklyMargin = 2000f;
                s.briefing = "Meridian runs freight in-house and goes direct to carriers. Get past " +
                             "the front desk, find the gap, and position Fitzmark as overflow coverage.";
                s.prospect = p;
                CreateOrReplace(s, ScenariosFolder + "/Scenario_EnterpriseVP.asset");
                count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fitzmark BDR] Created {count} sample scenarios in {ScenariosFolder}.");
            return count;
        }

        private static void CreateOrReplace(UnityEngine.Object asset, string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
        }

        // ---- scenes ---------------------------------------------------------

        [MenuItem(Menu + "Build Scenes", false, 22)]
        public static void BuildScenes()
        {
            EnsureFolders();
            BuildMainMenuScene();
            BuildCharacterCreateScene();
            BuildCityScene();
            BuildTexasMapScene();
            BuildOfficeScene();
            BuildPlatformerScene();
            BuildGatekeeperDuelScene();
            BuildCallFloorScene();
            Debug.Log("[Fitzmark BDR] Built 8 scenes (MainMenu, CharacterCreate, City, Texas, Office, Platformer, GatekeeperDuel, CallFloor).");
        }

        private static void BuildTexasMapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.20f);
            cam.farClipPlane = 600f;
            camGo.transform.position = new Vector3(-6f, 34f, -34f);
            camGo.AddComponent<AudioListener>();

            var controller = new GameObject("TexasMap");
            controller.AddComponent<TexasMapController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TexasScenePath);
        }

        private static void BuildPlatformerScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f);
            cam.farClipPlane = 500f;
            camGo.transform.position = new Vector3(0f, 3.2f, -12f);
            camGo.AddComponent<AudioListener>();

            var controller = new GameObject("PlatformerLevel");
            controller.AddComponent<PlatformerSceneController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PlatformerScenePath);
        }

        private static void BuildOfficeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            camGo.transform.position = new Vector3(0f, 10f, -16f);
            camGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30
            SetMat(floor, CityMaterial("office_floor", new Color(0.40f, 0.38f, 0.34f)));

            // Perimeter walls (fully enclosed; you leave via the Exit interactable)
            Wall("Wall_N", new Vector3(0f, 2f, 15f), new Vector3(30f, 4f, 0.5f));
            Wall("Wall_S", new Vector3(0f, 2f, -15f), new Vector3(30f, 4f, 0.5f));
            Wall("Wall_E", new Vector3(15f, 2f, 0f), new Vector3(0.5f, 4f, 30f));
            Wall("Wall_W", new Vector3(-15f, 2f, 0f), new Vector3(0.5f, 4f, 30f));

            var deskMat = CityMaterial("office_desk", new Color(0.45f, 0.34f, 0.26f));
            var tableMat = CityMaterial("office_table", new Color(0.30f, 0.30f, 0.34f));
            var loungeMat = CityMaterial("office_lounge", new Color(0.30f, 0.42f, 0.40f));

            // Bullpen
            Building("Desk", new Vector3(7f, 0.5f, 4f), new Vector3(2.4f, 1f, 1.3f), deskMat);
            Building("Desk", new Vector3(7f, 0.5f, 1f), new Vector3(2.4f, 1f, 1.3f), deskMat);
            Building("Desk", new Vector3(11f, 0.5f, 4f), new Vector3(2.4f, 1f, 1.3f), deskMat);
            var myDesk = Building("Your Desk", new Vector3(11f, 0.5f, 1f), new Vector3(2.4f, 1f, 1.3f), deskMat);
            AddInteractable(myDesk, Interactable.Kind.Desk, "Your Desk — make a call", 0, 3.5f);
            Marker(myDesk, "desk", new Color(0.3f, 0.8f, 0.4f));

            // Meeting room
            Building("Meeting Table", new Vector3(-7f, 0.5f, -6f), new Vector3(6f, 1f, 2.4f), tableMat);

            // Break room
            Building("Counter", new Vector3(-11f, 0.7f, 11f), new Vector3(5f, 1.4f, 1.2f), loungeMat);
            Building("Couch", new Vector3(-5f, 0.4f, 12f), new Vector3(3.4f, 0.8f, 1.4f), loungeMat);

            // Exit (south wall)
            var exit = Building("Exit", new Vector3(0f, 1.5f, -14.6f), new Vector3(2.6f, 3f, 0.4f),
                CityMaterial("office_exit", new Color(0.20f, 0.40f, 0.75f)));
            AddInteractable(exit, Interactable.Kind.Exit, "Leave to the City", 0, 4.5f);
            Marker(exit, "office", new Color(0.30f, 0.60f, 1f));

            var controller = new GameObject("OfficeController");
            controller.AddComponent<OfficeController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OfficeScenePath);
        }

        private static void Wall(string name, Vector3 pos, Vector3 scale) =>
            Building(name, pos, scale, CityMaterial("office_wall", new Color(0.52f, 0.52f, 0.58f)));

        private static void BuildCityScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.60f, 0.80f); // sky
            cam.farClipPlane = 500f;
            camGo.transform.position = new Vector3(0f, 12f, -14f);
            camGo.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(12f, 1f, 12f); // 120 x 120
            SetMat(ground, CityMaterial("ground", new Color(0.30f, 0.34f, 0.30f)));

            CreateRoad("Road_NS", new Vector3(0f, 0.02f, 0f), new Vector3(6f, 0.06f, 110f));
            CreateRoad("Road_EW", new Vector3(0f, 0.02f, 0f), new Vector3(110f, 0.06f, 6f));
            CreateRoad("Road_NS2", new Vector3(20f, 0.02f, 0f), new Vector3(6f, 0.06f, 110f));
            CreateRoad("Road_EW2", new Vector3(0f, 0.02f, 20f), new Vector3(110f, 0.06f, 6f));

            var office = Building("FitzMark Office", new Vector3(-8f, 2.5f, 7f),
                new Vector3(7f, 5f, 7f), CityMaterial("office", new Color(0.20f, 0.40f, 0.75f)));
            AddInteractable(office, Interactable.Kind.Office, "FitzMark Office", 0, 6f);
            Marker(office, "office", new Color(0.30f, 0.60f, 1f));

            AddClient("Acme Foods", 12f, 10f, 4f, 1);
            AddClient("Vertex Components", -14f, -12f, 5f, 2);
            AddClient("Harbor Distribution", 30f, -8f, 6f, 3);
            AddClient("Pioneer Mills", -26f, 26f, 4f, 4);

            var fillers = new (float x, float z, float h, int c)[]
            {
                (10f, -16f, 5f, 0), (32f, 12f, 7f, 1), (-30f, -6f, 4f, 2),
                (-12f, 30f, 6f, 0), (34f, 28f, 8f, 1), (-34f, 18f, 5f, 2),
                (26f, -28f, 6f, 0), (-24f, -28f, 7f, 1), (38f, 2f, 4f, 2), (-38f, -2f, 9f, 0)
            };
            var fillerMats = new[]
            {
                CityMaterial("b1", new Color(0.52f, 0.52f, 0.56f)),
                CityMaterial("b2", new Color(0.60f, 0.55f, 0.50f)),
                CityMaterial("b3", new Color(0.46f, 0.50f, 0.60f))
            };
            foreach (var f in fillers)
                Building("Building", new Vector3(f.x, f.h * 0.5f, f.z), new Vector3(6f, f.h, 6f), fillerMats[f.c]);

            var controller = new GameObject("CityController");
            controller.AddComponent<CityController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScenePath);
        }

        private static void AddClient(string label, float x, float z, float height, int seed)
        {
            var b = Building(label, new Vector3(x, height * 0.5f, z), new Vector3(6f, height, 6f),
                CityMaterial("client", new Color(0.55f, 0.45f, 0.35f)));
            AddInteractable(b, Interactable.Kind.Client, label, seed, 5f);
            Marker(b, "client", new Color(1f, 0.82f, 0.20f));
        }

        private static GameObject Building(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            SetMat(go, mat);
            return go;
        }

        private static void CreateRoad(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            SetMat(go, CityMaterial("road", new Color(0.12f, 0.12f, 0.13f)));
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col); // don't block movement
        }

        private static void Marker(GameObject building, string key, Color color)
        {
            float top = building.transform.position.y + building.transform.localScale.y * 0.5f;
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m.name = building.name + "_Marker";
            m.transform.position = new Vector3(building.transform.position.x, top + 1.4f, building.transform.position.z);
            m.transform.localScale = new Vector3(0.7f, 2.8f, 0.7f);
            var col = m.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            SetMat(m, CityMaterial("marker_" + key, color));
        }

        private static void AddInteractable(GameObject go, Interactable.Kind kind, string label, int seed, float range)
        {
            var it = go.AddComponent<Interactable>();
            it.kind = kind;
            it.label = label;
            it.seed = seed;
            it.range = range;
        }

        private static void SetMat(GameObject go, Material mat)
        {
            if (mat == null) return;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private static Material CityMaterial(string key, Color color)
        {
            string path = SettingsFolder + $"/City_{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { existing.color = color; return existing; }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void BuildGatekeeperDuelScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.05f, 0.06f);
            camGo.transform.position = new Vector3(0f, 1f, -10f);
            camGo.AddComponent<AudioListener>();

            var controller = new GameObject("GatekeeperFight");
            controller.AddComponent<FightController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GatekeeperDuelScenePath);
        }

        private static void BuildCharacterCreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.11f);
            cam.fieldOfView = 45f;
            camGo.transform.position = new Vector3(0f, 0.95f, -5f);
            camGo.transform.rotation = Quaternion.identity; // straight-on; controller reaffirms this at runtime
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

            var controller = new GameObject("CharacterCreate");
            controller.AddComponent<CharacterCreateController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CharacterCreateScenePath);
        }

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Vector3(0f, 1f, -10f), Quaternion.identity);

            var controller = new GameObject("MainMenu");
            controller.AddComponent<MainMenuController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void BuildCallFloorScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Vector3(0f, 1.6f, -2.4f), Quaternion.Euler(8f, 0f, 0f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            BuildOfficeProps();

            var controller = new GameObject("CallScreen");
            controller.AddComponent<Fitzmark.BDRSim.UI.CallScreenController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CallFloorScenePath);
        }

        private static void CreateCamera(Vector3 pos, Quaternion rot)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
            cam.fieldOfView = 60f;
            camGo.transform.SetPositionAndRotation(pos, rot);
            camGo.AddComponent<AudioListener>();
        }

        private static void BuildOfficeProps()
        {
            Material mat = MakePropMaterial();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            ApplyMaterial(floor, mat);

            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.position = new Vector3(0f, 0.4f, 1.2f);
            desk.transform.localScale = new Vector3(2.2f, 0.8f, 1.0f);
            ApplyMaterial(desk, mat);

            var monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "Monitor";
            monitor.transform.position = new Vector3(-0.4f, 1.15f, 1.45f);
            monitor.transform.localScale = new Vector3(1.0f, 0.6f, 0.08f);
            ApplyMaterial(monitor, mat);

            var phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phone.name = "Desk Phone";
            phone.transform.position = new Vector3(0.7f, 0.85f, 1.0f);
            phone.transform.localScale = new Vector3(0.35f, 0.12f, 0.5f);
            ApplyMaterial(phone, mat);
        }

        private static Material MakePropMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            const string path = SettingsFolder + "/Fitzmark_OfficeMaterial.mat";
            var mat = new Material(shader) { color = new Color(0.32f, 0.36f, 0.44f) };
            CreateOrReplace(mat, path);
            return mat;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }

        // ---- build settings -------------------------------------------------

        [MenuItem(Menu + "Configure Build Settings", false, 23)]
        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(CharacterCreateScenePath, true),
                new EditorBuildSettingsScene(CityScenePath, true),
                new EditorBuildSettingsScene(TexasScenePath, true),
                new EditorBuildSettingsScene(OfficeScenePath, true),
                new EditorBuildSettingsScene(PlatformerScenePath, true),
                new EditorBuildSettingsScene(GatekeeperDuelScenePath, true),
                new EditorBuildSettingsScene(CallFloorScenePath, true)
            };
            Debug.Log("[Fitzmark BDR] Build settings: MainMenu, CharacterCreate, City, Office, GatekeeperDuel, CallFloor.");
        }

        // ---- URP (best-effort, via reflection so there is no compile-time dep) --

        [MenuItem(Menu + "Configure URP Pipeline", false, 24)]
        public static void ConfigureUrp()
        {
            try
            {
                EnsureFolders();

                Type rendererType = FindType("UnityEngine.Rendering.Universal.UniversalRendererData");
                Type urpType = FindType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
                if (rendererType == null || urpType == null)
                {
                    Debug.LogWarning("[Fitzmark BDR] URP types not found — is the URP package installed? " +
                                     "Skipping pipeline setup. You can assign a URP asset manually in " +
                                     "Project Settings → Graphics.");
                    return;
                }

                var rendererData = ScriptableObject.CreateInstance(rendererType);
                CreateOrReplace(rendererData, SettingsFolder + "/Fitzmark_URP_Renderer.asset");

                MethodInfo create = urpType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
                UnityEngine.Object urpObj;
                if (create != null && create.GetParameters().Length == 1)
                    urpObj = (UnityEngine.Object)create.Invoke(null, new object[] { rendererData });
                else
                    urpObj = (UnityEngine.Object)ScriptableObject.CreateInstance(urpType);

                CreateOrReplace(urpObj, SettingsFolder + "/Fitzmark_URP.asset");

                var pipeline = urpObj as RenderPipelineAsset;
                if (pipeline == null)
                {
                    Debug.LogWarning("[Fitzmark BDR] Created URP asset is not a RenderPipelineAsset; " +
                                     "assign it manually in Project Settings → Graphics.");
                    return;
                }

                GraphicsSettings.defaultRenderPipeline = pipeline;

                int original = QualitySettings.GetQualityLevel();
                int levels = QualitySettings.names.Length;
                for (int i = 0; i < levels; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
                QualitySettings.SetQualityLevel(original, false);

                Debug.Log("[Fitzmark BDR] URP pipeline created and assigned.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Fitzmark BDR] URP auto-setup failed (non-fatal): " + e.Message +
                                 "\nThe project still runs; assign a URP asset manually if 3D props render pink.");
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        // ---- convenience ----------------------------------------------------

        [MenuItem(Menu + "Open Main Menu Scene", false, 40)]
        public static void OpenMainMenu()
        {
            if (System.IO.File.Exists(MainMenuScenePath))
                EditorSceneManager.OpenScene(MainMenuScenePath);
            else
                Debug.LogWarning("[Fitzmark BDR] MainMenu scene not found — run Setup Project first.");
        }

        [MenuItem(Menu + "Reset Saved BDR", false, 41)]
        public static void ResetSavedBdr()
        {
            Fitzmark.BDRSim.Core.SaveSystem.Delete();
            Debug.Log("[Fitzmark BDR] Saved BDR deleted — press Play to run character creation again.");
        }
    }
}
