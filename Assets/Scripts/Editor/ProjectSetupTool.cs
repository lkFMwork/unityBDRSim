using System;
using System.Collections.Generic;
using System.Reflection;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
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
                             $"• {created} sample scenarios created\n" +
                             $"• MainMenu and CallFloor scenes built\n" +
                             $"• Build settings configured\n\n" +
                             $"Press Play to run the simulator.";
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
            BuildCallFloorScene();
            Debug.Log("[Fitzmark BDR] Built MainMenu, CharacterCreate, and CallFloor scenes.");
        }

        private static void BuildCharacterCreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.11f);
            cam.fieldOfView = 50f;
            camGo.transform.position = new Vector3(-0.5f, 1.05f, -3.3f);
            camGo.transform.LookAt(new Vector3(-0.8f, 1.0f, 0f));
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
                new EditorBuildSettingsScene(CallFloorScenePath, true)
            };
            Debug.Log("[Fitzmark BDR] Build settings set to MainMenu + CharacterCreate + CallFloor.");
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
    }
}
