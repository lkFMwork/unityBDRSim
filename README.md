# Fitzmark BDR Simulator

A Unity 3D training simulator for **Business Development Representatives** at
**Fitzmark**, a third-party logistics (3PL) freight brokerage. Reps practice the
full sales call — opening, discovery, the value pitch, objection handling, rate
negotiation, and the close — against data-driven shipper personas, and get a
graded scorecard with coaching after every call.

> **Status:** playable prototype scaffold. The simulation engine, content model,
> UI, and example scenarios are all in place. It is built to be opened in the
> Unity Editor and extended.

---

## Requirements

- **Unity 6 LTS** (`6000.0.x`) — see [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt)
- **Universal Render Pipeline (URP)** and the packages listed in [`Packages/manifest.json`](Packages/manifest.json) (Unity restores these automatically on first open)

This repository contains the **project sources** (scripts, packages, settings).
Unity regenerates `Library/`, `Temp/`, IDE solution files, etc. on first open —
those are intentionally git-ignored.

---

## Quick start

1. **Install Unity 6 LTS** via Unity Hub (any `6000.0.x` patch — the Hub will
   offer to open the project with your installed version).
2. **Open the project**: in Unity Hub → *Add* → select this folder → open.
   Let the Package Manager finish restoring packages.
3. **Run one-click setup** from the Editor menu:

   ```
   Tools  →  Fitzmark BDR  →  Setup Project (One-Click)
   ```

   This generates the URP pipeline asset, the sample Fitzmark scenarios, the two
   scenes (`MainMenu`, `CallFloor`), and the Build Settings — then opens the menu
   scene.
4. **Press Play.** Pick a scenario and run a call.

> **Why a setup step?** Scene files, URP assets, and `ScriptableObject` content
> are normally produced by the Editor, not hand-written. Rather than commit
> fragile generated YAML, the project ships a generator
> ([`ProjectSetupTool`](Assets/Scripts/Editor/ProjectSetupTool.cs)) that builds
> them deterministically. Re-running it is always safe.

---

## How a call works

Each call is a small state machine. The rep picks one line per stage; every line
moves the prospect's **trust** and **patience** meters and scores one of six
coaching dimensions.

```
Opening → [Gatekeeper] → Discovery → Value Pitch → [Objection] → Negotiation → Close → Wrap
```

- **Trust** rises with rapport, good discovery, and tailored value; it lets you
  defend a higher rate in negotiation.
- **Patience** drains when you waste the prospect's time. Hit zero and they hang
  up.
- **Negotiation** is real margin math: each lane has the rate the shipper pays
  today and Fitzmark's cost to cover the load. Offer too high and you lose the
  deal; too low and you give away margin. See
  [`NegotiationEngine`](Assets/Scripts/Simulation/NegotiationEngine.cs).
- **Scoring** grades Rapport, Discovery, Value Articulation, Objection Handling,
  Negotiation, and Close, then surfaces coaching on the weakest areas.

See [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) for the full design.

---

## Project layout

```
Assets/
  Scripts/
    Data/         ScriptableObjects + freight domain types (no gameplay logic)
    Simulation/   The call engine: stages, scoring, negotiation, dialogue, session
    Core/         GameManager, scene flow, scenario discovery
    UI/           Runtime uGUI screens (menu + call HUD), built in code
    Editor/       One-click setup / scene + content generator
  Tests/
    EditMode/     NUnit tests for the engine (negotiation, scoring, call flow)
  Resources/
    Scenarios/    Generated ScenarioDefinition + ProspectProfile assets
  Scenes/         Generated MainMenu.unity + CallFloor.unity
  Settings/       Generated URP pipeline + material assets
Packages/         Package manifest (URP, uGUI, Test Framework)
ProjectSettings/  Unity project version + settings
docs/             Architecture, game design, and setup notes
```

The runtime code is split into assemblies with a one-way dependency flow
(`Data → Simulation → Core → UI`) so the engine stays free of Unity-specific and
UI concerns and is unit-testable. See
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## Authoring content

Add a scenario without touching code:

1. `Assets → Create → Fitzmark BDR → Prospect Profile` — fill in the contact,
   disposition (personality, trust, patience, price sensitivity), pain points,
   likely objections, and freight lanes.
2. `Assets → Create → Fitzmark BDR → Scenario` — set the briefing and success bar
   and point it at your prospect.
3. Put both assets under `Assets/Resources/Scenarios/`. They appear in the menu
   automatically (loaded by [`ScenarioCatalog`](Assets/Scripts/Core/ScenarioCatalog.cs)).

Dialogue lines and their scoring live in
[`DialogueLibrary`](Assets/Scripts/Simulation/DialogueLibrary.cs) — extend any
stage there.

---

## Running the tests

In the Editor: **Window → General → Test Runner → EditMode → Run All**.

The edit-mode suite covers the negotiation math, the scorecard, and the
end-to-end call flow — it runs without entering Play mode.

---

## Notes & next steps

- The UI is built in code with uGUI and Unity's built-in font so it renders with
  zero asset setup. A production pass would move it to TextMeshPro or UI Toolkit
  with authored layouts and Fitzmark branding.
- The "3D" call floor is intentionally minimal (a desk vignette behind the HUD).
  The training value is in the dialogue/negotiation loop; richer environments,
  characters, and VO are natural follow-ups.
- Package versions in the manifest target Unity 6.0 LTS; if the Package Manager
  flags a version, let it resolve to the one bundled with your editor.
