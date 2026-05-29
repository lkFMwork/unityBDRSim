# FitzMark BDR Simulator — Master Design Plan
### Visuals · Models · World · Performance · UX

> North star: **"GTA for freight brokering."** A cohesive, alive, juicy, performant
> world where the call is one verb among many and building your book is the game.
> This document is the upgrade blueprint across rendering, art direction, models,
> the open world, performance ("lag"), and UX/UI. Gameplay systems themselves live
> in [`RPG_DESIGN.md`](./RPG_DESIGN.md); this plan is how it all *looks, feels, and
> runs*.

---

## 0. Principles & hard constraints (these shape every decision)

1. **Make greybox look *intentional*, not unfinished.** Art direction beats asset
   count. A disciplined palette + lighting + post-processing makes primitives read
   as a deliberate stylized look. This is the single highest-ROI lever and it's
   almost entirely code/config — perfect for our workflow.
2. **Original / IP-safe only.** No trademarked logos, characters, sprites, audio,
   moves, or level designs. Everything is procedurally generated, originally
   authored, or clearly-licensed CC0 — never copied IP.
3. **Procedural-first.** The project must run from a fresh clone with no asset-store
   dependency. Authored/licensed art is an *accelerator*, layered behind the same
   code seams (e.g. `AvatarBuilder.SetConfig`), never a hard requirement to boot.
4. **Blind-build workflow.** I write code here; I cannot compile. You open Unity
   (6000.4.9f1 + URP 17.4.0), run `Tools → Fitzmark BDR → Setup Project`, and paste
   Console errors. ⇒ Favor **small, verifiable increments** over big-bang drops.
5. **Keep the clean seams.** Respect the assembly graph
   (Data → Simulation → Core → UI → World → Editor). New presentation/services code
   slots in without creating cycles.
6. **Honesty about "AAA."** True authored, rigged, animated 3D models require Blender
   authoring (by you, or original meshes I generate procedurally). The realistic path
   to a *AAA-feeling* product is **stylized art direction + lighting + post + game
   feel + UX polish** first; bespoke hero models second. We will not fake it with
   copied assets.

---

## 1. Art Direction & Visual Identity

**Chosen style: "Corporate Stylized Low-Poly."** Flat-shaded PBR, clean silhouettes,
confident restrained palette, **lighting as the hero**, generous game-feel. It is:
achievable procedurally, reads as deliberate, and scales to polish via lighting/post
rather than asset volume.

**Tone:** aspirational hustle. Warm daylight bullpen energy, neon data-dashboards for
the desk work, open-road romance for freight in motion. Optimistic, kinetic,
professional — not grim, not cartoonish.

**Look pillars**
- **Silhouette & shape language** — rounded-corporate forms, readable at a glance.
- **Bold, restrained palette** — few colors, used with intent; brand as the spine.
- **Lighting is the hero** — strong key + soft fill + AO + bloom turns flat shapes 3D.
- **Motion & juice** — everything responds; numbers count, panels glide, wins pop.
- **One cohesive UI** — every screen feels like the same product.

### 1.1 Brand palette (placeholder — swap in exact FitzMark brand hex)
A logistics-corporate scheme; treat as tokens, not hard-coded colors.

| Token | Role | Hex (placeholder) |
|---|---|---|
| `brand/primary` | FitzMark identity, headers, key CTAs | `#1B6FE0` (signal blue) |
| `brand/deep` | backgrounds, panels | `#0E1626` (navy) |
| `brand/steel` | neutral surfaces, trucks, structure | `#566173` |
| `accent/road` | highlights, money, attention | `#F2A33C` (amber) |
| `state/positive` | wins, delivered, cash up | `#3FB873` |
| `state/warning` | expiring, caution | `#EAA833` |
| `state/danger` | churn, claims, losses | `#D94C4C` |
| `text/primary` / `text/muted` | copy | `#EEF2F8` / `#9AA6B8` |

> Action: replace the ad-hoc colors in `UiTheme.cs` with a single tokenized
> **`PaletteAsset`** (ScriptableObject) so the whole UI + world recolor from one place,
> with light/dark variants and **colorblind-safe** semantics (never color alone).

---

## 2. Visual Design System

### 2.1 Materials & color (and the batching fix)
- **Central `MaterialLibrary`** (a Core/Presentation service): a *small* set of shared,
  SRP-Batcher-friendly URP/Lit materials — `Matte`, `Metal`, `Glass`, `Emissive`,
  `Road`, `Terrain`, `Skin`, `Fabric`. Color comes from a **`MaterialPropertyBlock`**
  per renderer, **not** a new material per object.
  - This *directly removes* the current anti-pattern: every primitive today does
    `Shader.Find(...)` + `new Material(...)` (see §4). One cached shader, shared
    materials, per-renderer property blocks ⇒ massive draw-call + GC win and a
    consistent look.
- **Gradient skybox + distance fog** for depth and mood; tinted per district
  (office = warm interior, road = open sky, desk = cool focus).

### 2.2 Lighting & atmosphere
- **Key + fill model:** one strong directional key (soft shadows on heroes), soft
  ambient/SH fill, rim where it sells silhouette.
- **Baked GI** for static world geometry; **light probes** for dynamic characters;
  **reflection probes** for spec on vehicles/glass.
- **Post-processing volume stack** (URP Volume):
  - Tonemapping (ACES/Neutral), Bloom, Color grading / LUT, Vignette.
  - **SSAO** (renderer feature) — the biggest "free" realism bump for flat shapes.
  - Depth of Field on portraits/menus; subtle film grain + chromatic aberration for
    sheen; **screen-space shadows**; optional SSR later.
- **Day/night + weather** (world phase) for the open world and the road.

### 2.3 Models & characters — the upgrade path
Current state (grounded): [`AvatarBuilder`](../Assets/Scripts/UI/AvatarBuilder.cs)
builds **11 primitive GameObjects per figure**, each with its own `Shader.Find` +
`new Material`, animated by a single idle sway. Good seam (`SetConfig`), poor density.

- **Step A — procedural polish (no imports):** combine the body into **one mesh with
  shared materials** (or a `SkinnedMeshRenderer`), better proportions, rounded forms,
  accessories (headset for calls, badge, clipboard). Shared `MaterialLibrary` + property
  blocks. Cuts ~11 materials → ~3 and ~11 draws → ~1–2 per avatar.
- **Step B — rig & animate:** a humanoid skeleton + **Animator** with states: idle,
  walk, talk (call), type (desk), gesture, celebrate (close), defeat. Blend trees +
  **look-at IK** on calls. `SetConfig` stays the seam.
  - *Sourcing options (your call — see question at end):*
    **(1) Fully original** — author a low-poly humanoid + rig in Blender (we own it).
    **(2) Mixamo** — free, licensed humanoid animations on an original mesh (fast).
    **(3) CC0 packs** (e.g. Quaternius) — free, license-clean, original-IP.
- **Vehicles:** original low-poly tractor-trailer (the freight hero prop) with proper
  PBR; later Rigidbody physics for the road. Trailers liveried with the FitzMark
  palette (no real-world logos).

### 2.4 Environment & world art
- **Modular kit** on a consistent scale grid: office interior (desks, glass walls,
  break room), city blocks, the open road (highway, overpasses, signage — generic),
  Texas landmarks (stylized, generic). Trim-sheet / vertex-color shading keeps it
  cheap and cohesive.
- Procedural placement with authored "set pieces" for memorable spots (the FitzMark HQ,
  a truck stop, the loading dock).

### 2.5 VFX & game feel ("juice")
- **Particles:** cash burst + coin shimmer on commission, confetti + light shafts on a
  close, tire dust + exhaust on trucks, hit sparks + dust in the fighter, pickup
  shimmer in the platformer, "new tender" ping on the board.
- **Tweening:** a tiny dependency-free tween utility (coroutine/animation-curve based)
  for button press/hover, panel slide/fade, **number count-ups** (cash, margin),
  toast pop, screen shake, and **hit-stop** in the fighter.
- **Camera:** dynamic framing, subtle handheld shake, DoF rack-focus on the active
  speaker, punch-in on big moments.

### 2.6 Typography & iconography
- **Migrate `Text` → TextMeshPro (SDF).** Crisper at all sizes, far better batching,
  rich features. Define a **type scale**: Display / Title / Heading / Body / Caption,
  with weights and consistent spacing. Pick a clean, legible **free/owned** typeface.
- **Original icon set** (line + solid): freight, truck, phone, email, video, carrier,
  margin/money, the five stats, calendar, quest, achievement — authored as an SDF
  sprite atlas or icon font so they're sharp and recolorable from the palette.

---

## 3. The World — open-world architecture

**Goal:** a cohesive, navigable place — not the current set of disconnected scenes
(MainMenu, City, Texas, Office, Platformer, GatekeeperDuel, CallFloor, FreightDesk).

- **Architecture: persistent core + additive districts.**
  - A always-loaded **`CoreSystems`** scene: `GameManager`, audio, the persistent UI
    shell (top bar + phone menu), save. It never unloads.
  - Gameplay areas load **additively** and unload on leave: **Office hub**, **City**,
    **USA national map** (the remote book), **Texas overworld** (local), **the open
    road** (freight in transit), plus modal activity scenes (call, desk, fight,
    platformer commute).
  - Why additive: lower memory, faster transitions, no singleton re-init churn, and it
    preserves the clean per-area controllers we already have.
- **Navigation model (the GTA "phone + map"):**
  - **The Office is home base** you can walk. From your desk: calls & the freight desk.
  - **USA map** = travel/abstract for the national book (remote channels).
  - **Texas overworld** = local accounts, reached via the platformer commute.
  - **The open road** = optional drive/observe layer for in-transit loads (ties the
    freight desk to the world visually).
  - A persistent **quick-menu ("phone")** jumps anywhere; clear "back to hub" always.
- **Streaming & static optimization:** occlusion culling, LOD groups, **baked GI +
  baked occlusion** for static geo, shadow-distance/cascade tuning, mesh-combine on
  static props (kills per-primitive draw cost).
- **Living world:** NPC reps in the bullpen (idle/type/walk), trucks driving the
  highway, a **market ticker** (rates moving), day/night — the world advances whether
  or not you touch it, reinforcing the sim.

---

## 4. Performance Plan ("lag")

**Target:** 60 fps on a typical desktop (16.6 ms/frame), graceful scaling via quality
tiers. **Method:** Unity **Profiler** + **Frame Debugger** + **Memory Profiler**;
measure before/after each change; never optimize blind.

### 4.1 Concrete hotspots in *this* codebase (grounded) → fixes
| Issue | Where (grounded) | Fix |
|---|---|---|
| `Shader.Find` per material at runtime | `AvatarBuilder`, `FightController`, `TexasMapController`, `PlatformerLevelGenerator`, `CityController` (+ editor) — **19 calls** | Cache shader once in `MaterialLibrary`; never `Shader.Find` in gameplay |
| `new Material()` per object (no batching) | **7+ sites**; ~11 *unique* materials **per avatar** | Shared materials + `MaterialPropertyBlock`; enable **SRP Batcher** + GPU instancing |
| Per-`Update` string allocation in HUDs | `FightController.RefreshHud` builds strings every frame; other `Update()` loops | Cache last values, update **on change only**, `StringBuilder`, no per-frame GC |
| Full-canvas rebuild per action | `FreightDeskController` destroys + recreates the whole canvas on every click | Incremental updates, **split canvases** (static vs dynamic), **pool** list rows |
| `Camera.main` repeatedly | **6 sites** (does a tag search each call) | Cache the camera reference once |
| `FindFirstObjectByType` | **7 sites** | Cache / inject references; avoid in hot paths |
| `CreatePrimitive` auto-colliders | **15 sites** (world props keep colliders) | Strip unneeded colliders; mesh-combine static geo |
| Legacy uGUI `Text` + deep `LayoutGroup` nesting | all UI | Migrate to **TMP**, flatten layouts, virtualize long lists |

### 4.2 Rendering performance
- **SRP Batcher ON**; **GPU instancing** for repeated props (buildings, NPCs, trucks);
  static/dynamic batching where it helps; **occlusion culling**; **LOD groups**;
  **baked GI**; tuned shadow distance + cascades; texture atlasing; mesh-combine.
- Linear color space, HDR, post tuned per quality tier.

### 4.3 Scripting & memory
- Cache components; **no `Find`/`GetComponent` in `Update`**; event-driven UI (no
  polling); **object pools** for load rows, particles, NPCs, toasts; zero per-frame
  allocations on hot paths; Addressables + compression for any future assets; audio
  streaming.
- **Quality tiers** (Low/Med/High/Ultra) wired to a **Settings menu** so users scale
  post, shadows, particles, draw distance.

### 4.4 Likely #1 culprit
Given the UI is entirely **code-built legacy uGUI** with **full-canvas rebuilds**, the
biggest real-world "lag"/hitch source is UI. The **TMP/UI-Toolkit migration + canvas
splitting + incremental updates** (Phase B) is both the top UX win and the top perf
win. Treat it as a headline item, not cleanup.

---

## 5. UX & UI Design

### 5.1 Information architecture (the cockpit)
- **CRM dashboard = home base** — the "phone/map" of our GTA. One daily cockpit:
  pipeline (lead → qualified → quoted → won → active), today's tasks, **cash/day/energy**
  top bar, calendar, notifications, and quick-launch to every activity.
- **Persistent top bar** everywhere: cash, day/week, energy/time budget, alerts.
- **Quick-menu ("phone")** to jump to any system; **world map** for travel. Always a
  clear "where am I / how do I get back."

### 5.2 Screen-by-screen redesign (to the system & palette)
Call floor · Freight desk · Fighter HUD · Platformer HUD · Texas map · Office · Menus —
each re-skinned to the design system, same components, same motion language.

### 5.3 Onboarding & tutorial
- First-run **guided "rookie week"**: create BDR → first call → first close → first
  load on the desk → end day. Contextual tips, teaching **empty-states** (the freight
  desk's empty-state pattern, generalized), dismissible coach marks.

### 5.4 Feedback & juice
- Every action confirms: **sound + motion + number count-up + toast**. Big moments
  (a close, a fat-margin delivery, a promotion) get a **celebratory beat** (confetti,
  cash burst, stinger). Failures (churn, fell-through, hang-up) are **clear and kind**,
  never punishing-feeling.

### 5.5 Accessibility & readability
- Scalable text (TMP), **colorblind-safe** semantics (icon + label, never color only),
  high-contrast mode, **reduced-motion** toggle, input remapping, subtitles for any VO,
  generous hit targets, legible contrast ratios.

### 5.6 Input & control scheme
- Unify on the **Input System**, one consistent verb set across mouse+keyboard
  (gamepad-ready). No per-scene surprises; a single bindings screen; document controls
  in-game (the fighter/platformer already show hints — standardize them).

---

## 6. Audio (supports the visuals/UX)
- Procedural/synth now → **designed later**. **AudioManager** + mixer with buses
  (Music / SFX / UI / Ambience / VO) and ducking.
- **Adaptive music** layered per activity (calm desk, tense negotiation, upbeat win,
  driving road). **SFX** for UI, world, freight events, fighter hits, platformer
  pickups. **Ambience** (office hum, highway, phone room tone). Optional stylized
  **call "gibberish" VO** so calls feel voiced without scripting/licensing real lines.

---

## 7. Technical architecture & asset pipeline
- **URP config** owned by the one-click setup tool: renderer features (SSAO, decals,
  screen-space shadows), Volume profile, quality tiers, Linear + HDR.
- **Original-content pipeline:** Blender → glTF/FBX → Unity; scale/orientation/naming
  conventions; **`PaletteAsset`** + **`MaterialLibrary`** + a **VFX** and **Audio**
  bank as ScriptableObjects; **Addressables** for streamed world chunks.
- **New services** (slot into the assembly graph without cycles; likely a new
  `Presentation`/`Services` layer or extensions to Core/UI):
  `MaterialLibrary`, `Tween`, `AudioManager`, `SettingsService`, `SceneStreaming`,
  `CameraDirector`, `VFXPool`.
- **`ProjectSetupTool` stays authoritative** — extend it to build the `CoreSystems`
  scene, configure post/quality, and register the Volume + palette so a fresh clone
  still goes "open → Setup → Play."

---

## 8. Phased roadmap (sequenced; each phase ships & is testable in Unity)

> Each phase: built blind by me → you `git pull`, re-run Setup, play-test, paste
> errors. Ordered by **ROI × low-risk-first**.

- **Phase A — Rendering & art-direction foundation (biggest instant lift, lowest risk).**
  `PaletteAsset` + `MaterialLibrary` (shared mats + property blocks), URP **Volume**
  post stack (tonemap/bloom/SSAO/color-grade/vignette), per-scene **lighting rig**,
  gradient skybox + fog, **SRP Batcher** + shader caching. *Also fixes the top perf
  anti-patterns (Shader.Find / new Material).* The whole game looks intentional
  overnight.
- **Phase B — UI/UX system + UI performance.** TMP migration, design tokens, the
  **persistent top bar + cockpit shell**, juice/tween util, **incremental UI updates +
  canvas split + row pooling** (kills UI lag). Re-skin all screens.
- **Phase C — Characters & animation.** Proc-mesh avatar upgrade → rig → **Animator** +
  blend trees + look-at IK; the freight **truck** prop. (Asset-sourcing decision applies
  here.)
- **Phase D — The world.** `CoreSystems` + **additive streaming**, USA national map +
  cohesive office hub, **living world** (NPCs, traffic, market ticker), baked
  GI/occlusion/LOD.
- **Phase E — Audio + full VFX pass + accessibility + settings/quality tiers.**
- **Phase F — Polish & perf hardening.** Camera direction, celebratory cutscene beats,
  photo-mode, profile to the 60 fps budget across tiers.

(Gameplay pillars from `RPG_DESIGN.md` — living economy/rivals, multi-channel outreach,
CRM home base — interleave with B/D where they share UI/world work.)

---

## 9. Risks, constraints & dependencies
- **IP/originality guardrail** — all art/audio original or CC0; no trademarks. (Limits
  "just import AAA," steers us to procedural + lighting + original authoring.)
- **Blind-build loop** — mitigate with small increments and static self-audits; you are
  the compiler.
- **Biggest single lift** — legacy-uGUI → TMP/UI-Toolkit migration; also the biggest
  UX + perf payoff, so it's front-loaded (Phase B).
- **Authored 3D** — bespoke rigged models need Blender authoring (you, or original
  meshes I generate); procedural polish (Phase A/C-Step-A) gets us most of the way
  with zero imports.

## 10. Definition of done (quality bar)
60 fps on target hardware across quality tiers · one cohesive look on every screen ·
no orphaned/unintentional greybox · consistent navigation + persistent shell ·
onboarding present · accessible (color/text/motion/input) · audio-juiced feedback on
every action · **100% original / IP-safe**.
