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

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

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
