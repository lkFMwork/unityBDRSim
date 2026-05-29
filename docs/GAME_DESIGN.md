# Game Design — Fitzmark BDR Simulator

## Goal

Train Business Development Representatives to run a freight sales call well. A BDR
at a 3PL brokerage prospects shippers, uncovers their freight pain, positions
Fitzmark's value, handles objections, negotiates a profitable rate, and closes a
first load. The sim turns that into a graded, repeatable exercise.

## The call as a state machine

```
Opening → [Gatekeeper] → Discovery → Value Pitch → [Objection] → Negotiation → Close → Wrap
```

| Stage | What good looks like |
| --- | --- |
| **Opening** | Permission-based intro; earn the first 30 seconds before pitching. |
| **Gatekeeper** *(some scenarios)* | Be human, give a reason, get routed to the decision maker. |
| **Discovery** | Diagnose before prescribing: lanes, volume, modes, current provider, pain. |
| **Value Pitch** | Tie Fitzmark capability to the *specific* pain the prospect named. |
| **Objection** | Acknowledge, then reframe — never argue. |
| **Negotiation** | Lead with a rate that protects margin; defend it with value. |
| **Close** | Ask for a concrete next step — a trial load or a booked follow-up. |

## Meters

- **Trust (0–1)** — how bought-in the prospect is. Raised by rapport, sharp
  discovery, and tailored value. Higher trust lets you hold a higher rate.
- **Patience (0–1)** — how much time they'll give you. Drained by weak openers,
  feature-dumping, and arguing. At zero, they hang up and the call ends.

Mood (Cold → Guarded → Receptive → Engaged) is a readout of the trust/patience
blend shown on the HUD.

## Scoring

Six categories, each worth up to 20 points (120 total). Overall percent maps to
a letter grade (A ≥ 90%, B ≥ 80%, C ≥ 70%, D ≥ 60%, else F).

| Category | Earned during |
| --- | --- |
| Rapport | Opening / gatekeeper |
| Discovery | Discovery questions |
| Value Articulation | Pitch |
| Objection Handling | Objection stage |
| Negotiation | Rate offer outcome |
| Close | Closing ask |

After the call, [`CallEvaluator`](../Assets/Scripts/Simulation/CallEvaluator.cs)
calls out the strongest areas and gives coaching on the two weakest dimensions
that fell below "competent" (60%).

## Negotiation model

Each lane carries:

- `currentRatePerMile` — what the shipper pays their incumbent today.
- `fitzmarkCostPerMile` — Fitzmark's cost to cover the load.

The rep's **gross margin** is `offer − cost`. The prospect expects a certain
amount of **savings** versus today, modulated by two traits:

```
requiredSavings = 0.03 + (priceSensitivity × 0.08) − (trust × 0.05)   [clamped −3%..15%]
acceptableCeiling = currentRate × (1 − requiredSavings)
```

- Offer **≤ ceiling** → **accepted**.
- Offer slightly above (≤ 6%) → **counter** at the ceiling.
- Offer far above → **rejected** (and trust/patience take a hit).

So a *price-driven* prospect demands deeper savings, while a *trusted* rep can
sell value and hold a higher rate. The model is fully deterministic, which keeps
it teachable and unit-testable.

## Example scenarios (generated)

| Scenario | Difficulty | Prospect | Hook |
| --- | --- | --- | --- |
| Cold Call: Midwest Manufacturer | Easy | Pat Morgan, Logistics Mgr, Hoosier Components | FTL Indy→Detroit; carrier no-shows; "already have a broker" |
| Reefer Lane: Food Distributor | Medium | Dana Cole, Dir. Logistics, Crossroads Foods | Reefer Indy→Atlanta; prior claim; rebuild trust before price |
| Enterprise Shipper: Skeptical VP | Hard | Alex Rivera, VP Supply Chain, Meridian Industrial | Gatekeeper; goes direct to carriers; sell overflow coverage |

Difficulty is expressed through the prospect's starting trust/patience, price
sensitivity, objection stack, and whether a gatekeeper stands in the way.

## Design extension ideas

- Multiple discovery questions per call (currently one representative choice).
- Multi-round negotiation with counters the rep can accept or push back on.
- Account/CRM meta-layer: pipeline stages across many calls.
- Randomized prospect mood and objection ordering for replayability.
- Voice-over and animated 3D characters on the call floor.
