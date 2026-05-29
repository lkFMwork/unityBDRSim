# Architecture

## Assemblies and dependency flow

The runtime is split into assembly definitions with a strict one-way dependency
graph. The engine carries no UI or editor dependencies, which keeps it
unit-testable and lets the call logic be reused by a different front-end later.

```
Fitzmark.BDRSim.Data         (ScriptableObjects, freight domain types)
        ▲
Fitzmark.BDRSim.Simulation   (call engine — pure C#, no UI)
        ▲
Fitzmark.BDRSim.Core         (GameManager, scene flow, scenario discovery)
        ▲
Fitzmark.BDRSim.UI           (runtime uGUI screens)

Fitzmark.BDRSim.Editor       (editor-only: setup/generator) → references all of the above
Fitzmark.BDRSim.Tests        (EditMode NUnit) → references Data/Simulation/Core
```

- **Data** has no project dependencies. `ProspectProfile` and `ScenarioDefinition`
  are `ScriptableObject`s; `Lane`, the enums, and `ObjectionCatalog` are plain
  serializable types.
- **Simulation** depends only on Data and uses no `UnityEngine` UI/scene APIs, so
  the whole call loop runs in edit-mode tests.
- **Core** wires runtime services: the persistent `GameManager`, `SceneNames`,
  and `ScenarioCatalog` (Resources-based content discovery).
- **UI** builds the screens at runtime and talks to the engine only through
  `CallSession`'s public surface and events.
- **Editor** is the generator; it has no runtime footprint.

## The call engine

[`CallSession`](../Assets/Scripts/Simulation/CallSession.cs) is the orchestrator
and the only object the UI needs to drive a call:

| Member | Purpose |
| --- | --- |
| `Begin()` | Starts the call, emits the opening, publishes first choices |
| `CurrentChoices` | The dialogue options selectable right now |
| `Choose(DialogueChoice)` | Applies a choice and advances the state machine |
| `Stage` / `Outcome` / `IsOver` | Current state |
| events | `NarratorLine`, `RepLine`, `ProspectLine`, `StateChanged`, `ChoicesChanged`, `CallEnded` |

Supporting pieces:

- [`ProspectState`](../Assets/Scripts/Simulation/ProspectState.cs) — runtime
  trust/patience/mood, raised & handled objections, agreed rate.
- [`Scorecard`](../Assets/Scripts/Simulation/Scorecard.cs) — points per
  `ScoreCategory`, clamped, with percentage + letter grade.
- [`NegotiationEngine`](../Assets/Scripts/Simulation/NegotiationEngine.cs) —
  deterministic accept/counter/reject given the lane economics, prospect trust,
  and price sensitivity.
- [`DialogueLibrary`](../Assets/Scripts/Simulation/DialogueLibrary.cs) — generates
  the choices (and their score/trust/patience effects) for each stage from the
  scenario data. This is the "content" layer.
- [`CallEvaluator`](../Assets/Scripts/Simulation/CallEvaluator.cs) — turns a
  finished session into a graded `CallReport` with strengths and coaching.

## UI ↔ engine flow

```
CallScreenController.Start()
  └─ GameManager.ResolveActiveScenario()
  └─ new CallSession(scenario)
  └─ subscribe to session events
  └─ session.Begin()

[player clicks a choice button]
  └─ session.Choose(choice)
        ├─ RepLine / ProspectLine / NarratorLine  → append to transcript
        ├─ StateChanged                            → refresh trust/patience/stage
        ├─ ChoicesChanged                          → rebuild choice buttons
        └─ CallEnded                               → CallEvaluator → results overlay
```

The UI is built entirely in C# (see
[`UiFactory`](../Assets/Scripts/UI/UiFactory.cs)) using uGUI and Unity's built-in
runtime font, so it needs no prefabs, sprites, fonts, or asset references to
render — important because the scenes are generated rather than hand-authored.

## Why the generator exists

Scenes (`.unity`), URP pipeline assets, and `ScriptableObject` instances are
normally produced by the Editor and contain GUID/fileID cross-references that are
error-prone to hand-write. Instead of committing fragile YAML, the project ships
[`ProjectSetupTool`](../Assets/Scripts/Editor/ProjectSetupTool.cs), which builds
all of it deterministically and idempotently from a single menu command. URP is
configured via reflection so the editor assembly has **no compile-time dependency
on URP package internals** and degrades gracefully if the package layout changes.
