# Setup & Troubleshooting

## First-time setup

1. Install **Unity 6 LTS** (`6000.0.x`) through Unity Hub.
2. Add this folder as a project in Unity Hub and open it.
3. Wait for the Package Manager to restore packages (URP, uGUI, Test Framework).
4. Run **Tools → Fitzmark BDR → Setup Project (One-Click)**.
5. Press **Play**.

The setup command is idempotent — run it again any time to regenerate the URP
assets, sample scenarios, scenes, and Build Settings.

### What the setup tool does

| Step | Result |
| --- | --- |
| Create Folders | `Assets/Scenes`, `Assets/Settings`, `Assets/Resources/Scenarios` |
| Configure URP Pipeline | Creates a URP asset + renderer and assigns them to Graphics/Quality |
| Create Sample Scenarios | 3 `ScenarioDefinition` + `ProspectProfile` assets in `Resources/Scenarios` |
| Build Scenes | `MainMenu.unity` and `CallFloor.unity` with cameras, light, and controllers |
| Configure Build Settings | Adds both scenes to the build list |

Each step is also available individually under the same **Tools → Fitzmark BDR**
menu.

## Running tests

**Window → General → Test Runner → EditMode → Run All.** The suite validates the
negotiation engine, the scorecard, and the full call flow without entering Play
mode.

## Troubleshooting

**The menu is empty / "No scenarios found."**
Run *Tools → Fitzmark BDR → Setup Project (One-Click)* (or *Create Sample
Scenarios*). The menu loads scenarios from `Assets/Resources/Scenarios/`.

**3D props render pink/magenta.**
URP isn't assigned. Re-run *Tools → Fitzmark BDR → Configure URP Pipeline*, or set
a URP asset manually under *Project Settings → Graphics → Default Render
Pipeline* and *Project Settings → Quality*. The UI is unaffected by this — only
the (cosmetic) desk props are.

**Pressing Play does nothing / wrong scene.**
Open `Assets/Scenes/MainMenu.unity` (or run *Tools → Fitzmark BDR → Open Main
Menu Scene*) and confirm both scenes are in *File → Build Settings*.

**UI buttons don't respond to clicks.**
There must be an `EventSystem` in the scene — the controllers create one
automatically. The project uses Unity's classic Input Manager (no Input System
package), so the default `StandaloneInputModule` works out of the box. If your
team adds the Input System package, set *Project Settings → Player → Active Input
Handling* to **Both**.

**Package Manager flags a version in `manifest.json`.**
The pinned versions target Unity 6.0 LTS. Let the Package Manager resolve to the
version bundled with your editor, or update the pin to match.

## Continuous integration (optional, future)

This container-based environment can't run the Unity Editor, so CI isn't wired
up here. A typical setup would use
[GameCI](https://game.ci/) (`game-ci/unity-test-runner`) in GitHub Actions to run
the EditMode suite on a self-hosted or licensed runner. The engine assemblies are
deliberately UI-free to make that straightforward.
