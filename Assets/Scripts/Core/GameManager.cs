using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// Persistent, lazily-created singleton that survives scene loads. Holds the
    /// scenario the player picked in the menu and the most recent call report, and
    /// owns scene transitions between the menu and the call floor.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[GameManager]");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>Scenario selected in the menu and played on the call floor.</summary>
        public ScenarioDefinition SelectedScenario { get; private set; }

        /// <summary>Report from the most recently completed call (for results screens).</summary>
        public CallReport LastReport { get; set; }

        /// <summary>The player's persistent BDR, loaded from disk. Null until one is created.</summary>
        public BDRCharacter Profile { get; private set; }

        public bool HasProfile => Profile != null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (Profile == null)
                Profile = SaveSystem.Load();
        }

        /// <summary>Create a new BDR, persist it, and make it the active profile.</summary>
        public void CreateProfile(BDRCharacter character)
        {
            Profile = character;
            SaveSystem.Save(character);
        }

        /// <summary>Write the current profile to disk (call after XP gains, edits, etc.).</summary>
        public void SaveProfile()
        {
            if (Profile != null) SaveSystem.Save(Profile);
        }

        /// <summary>Wipe the saved profile (for a "new game" / reset).</summary>
        public void DeleteProfile()
        {
            Profile = null;
            SaveSystem.Delete();
        }

        public void GoToCharacterCreate() => SceneManager.LoadScene(SceneNames.CharacterCreate);

        public void StartScenario(ScenarioDefinition scenario)
        {
            SelectedScenario = scenario;
            LastReport = null;
            SceneManager.LoadScene(SceneNames.CallFloor);
        }

        public void ReturnToMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

        public void ReplayCurrent()
        {
            LastReport = null;
            SceneManager.LoadScene(SceneNames.CallFloor);
        }

        /// <summary>
        /// Resolves the scenario to run on the call floor: the menu selection, or
        /// the first catalog scenario as a fallback so the CallFloor scene is
        /// playable on its own (handy when testing in the editor).
        /// </summary>
        public ScenarioDefinition ResolveActiveScenario()
        {
            if (SelectedScenario != null) return SelectedScenario;
            SelectedScenario = ScenarioCatalog.First;
            return SelectedScenario;
        }
    }
}
