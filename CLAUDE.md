# CLAUDE.md — Wixx The Bard

**This file is law.** Read it and `SPEC.md` in full at the start of every session,
before touching anything. `SPEC.md` is the design source of truth; this file is the
engineering law. Where they conflict, stop and surface it — don't pick a side.

---

## Project

A guitar-controller 2D platformer. **Godot 4, C#.** Single-player. Steam target.
Solo dev, "for fun but shipped." Mega Drive-silhouette pixel art, modern polish.
The whole game is played on a 5-fret guitar peripheral; the rhythm of play *is* the
game. Full design in `SPEC.md`.

> **Language note:** C# is assumed (SPEC §10, matches the Gear Heart stack). If this
> project switches to GDScript, change only this line and rule 9 — every other rule
> holds unchanged.

## The `/reference` folder

If present, it holds `WixxMovement.gd` (the validated GDScript movement grey-box) and
the SPEC §14 findings. It is **READ-ONLY** and it is the **source of proven feel and
tuning numbers**. Port constants and behaviour *from* it. Do **not** build on it, and
do **not** copy its hand-rolled floor physics wholesale — the real game uses
`CharacterBody2D` with real collision and data-driven input. The prototype's only job
is to tell you the numbers and the feel that already passed a hardware test.

---

## Inviolable rules

1. **Tunables are the single source of truth.** Every gameplay number — movement
   accel/friction/max-speed, jump velocities, sprint curve, all `TAR_*` values,
   cooldowns, time-slow factor, rhythm windows — lives in one tunables resource/module
   (e.g. `res://config/Tunables`). **No magic numbers in gameplay code.** The validated
   values come from `/reference`; treat them as the starting point, exposed for tuning.

2. **Input is 100% data-driven. Never hardcode a guitar index.** No `button 11`,
   `axis 2`, or fret literals anywhere in gameplay. All bindings resolve through the
   remappable input layer. **Nothing about a guitar's layout is guessable** (SPEC §14):
   frets land on arbitrary buttons, the strum may be buttons *or* an axis, and analog
   inputs rest at −1, +1, or 0. The binder must **press-to-bind** every verb and
   **auto-detect each axis's rest value and direction** (engaged = deflection from rest,
   either way). Common-controller presets are data, not code paths.

3. **Fixed-timestep gameplay.** All movement, physics, timers, cooldowns, and rhythm
   logic run in `_PhysicsProcess` at the fixed tick. The `/reference` constants assume
   a 60 Hz step and port 1:1 only if you keep that contract. Rendering, juice, and VFX
   may live in `_Process`; gameplay must not depend on frame rate.

4. **Forced launches are exempt from the jump-height cut. (The footgun — SPEC §14.)**
   The variable-jump-height cut (reducing upward velocity when the jump button is
   released early) must apply **only** to a button-driven jump. Any launch *not* driven
   by the jump button — super-jump, tar-exit leap — must never be cut, or it silently
   collapses to a fraction of its height. Bake the exemption in; never reintroduce this.

5. **Strum mode discipline.** The strum's meaning is mode-dependent — Exploration
   direction (Hold), Music Run run-cadence, tar struggle, spell-performance notes — and
   the game is **always in exactly one mode**. No two strum consumers may be active at
   once. Movement is **Hold (Scheme B)**, locked by hardware test; flick is cut.

6. **Rhythm timing is calibration-relative.** Every note-timing judgement is evaluated
   against the player's stored A/V latency offset, never against raw frame time or audio
   clock alone. The offset is set in the same Options screen as control remapping.

7. **Gameplay state is authoritative; visuals read from it.** Animation, particles,
   camera, and shaders observe gameplay state — they never drive it. Juice can never
   desync a mechanic. (The platformer-scale analogue of the sim/presentation split.)

8. **Scope discipline.** One milestone per session. Do not expand scope beyond the
   milestone. Do not refactor unrelated code — "while I was in there" is how a green
   repo goes red. Touch only what the milestone needs.

9. **Godot / C# conventions.** Scenes own their scripts; prefer signals over polling
   where it reads cleaner; one responsibility per node/script; PascalCase methods,
   `_camelCase` private fields; no allocations in hot `_PhysicsProcess` paths. Keep the
   input layer, tunables, and gameplay nodes in separate, testable units.

10. **Escalation, not invention.** Any decision this file and `SPEC.md` don't cover,
    that touches a pillar, a tunable contract, or an inviolable rule, escalates one tier
    (Haiku→Sonnet→Opus) rather than being improvised. Disagreements with the spec itself
    surface to the human.

---

## Model routing

Default: **opusplan** (Opus plans, Sonnet executes).

- **Full Opus** for the load-bearing, easy-to-get-subtly-wrong work: **M1** (input
  pipeline + remapper + A/V calibration), **M2** (the `CharacterBody2D` physics contract,
  incl. rule 4), **M4** (rhythm performance + latency), and the **review gates on M1, M4,
  and the M6 fun gate**.
- **Sonnet** for M2 build-out, M3 verbs, M5 boss, level/UI/art-integration work.
- **Haiku** for file discovery, asset wiring, and lookups only.

---

## Milestone plan (vertical slice)

Sequenced by SPEC §11 de-risk order; the slice is SPEC §12.

- **M0 — Scaffold.** Godot 4 C# project, repo layout, format/lint, `Tunables`,
  test harness (gdUnit4), a "press play, a box moves on a real tilemap" hello-world.
- **M1 — Input + Options. (do first; #1 risk)** Data-driven input layer, press-to-bind
  remapper with axis rest/direction auto-detect, common-controller presets, persistence,
  and the A/V latency calibration screen. Same Options screen for both.
- **M2 — Core movement.** `CharacterBody2D`, real collision, **Hold** scheme,
  variable-height jump (rule 4 honoured), sprint. Port `/reference` constants.
- **M3 — The verbs.** Lute swing + basic enemies, tilt super-jump (self-calibrating,
  level-fired, cooldown-gated, uncut), whammy crouch/slide, tar hazard (vertical
  climb → breach → leap, per SPEC §4.4).
- **M4 — Spell performance.** One ability end-to-end: trigger → global time-slow +
  desaturation → right-to-left note chart (calibration-relative) → success fires +
  cooldown / fail = cooldown only.
- **M5 — Boss.** One Rock Off: call-and-response phases + simultaneous-highway phases.
- **M6 — ★ Vertical slice + FUN GATE.** Assemble one biome end-to-end. **This is the
  "is it fun?" checkpoint** — the human plays it on the real guitar and decides go/no-go
  before any phase-2 work.

**Phase 2 (post-gate, not yet detailed):** ability library + anytime radial loadout,
more biomes, full boss roster, story delivery, one Music Run level (proof of archetype),
Steam page + achievements, **Accessibility Mode** (standard-pad fallback, clearly labelled).

---

## Workflow

`implement → review gate → fix → re-gate (only if BLOCKING findings) → human plays the
"done when" criteria on the real guitar → next milestone, fresh session.`

Gates **verify by running tests and the build, not by reading intent.** Feel criteria
are verified by the human, on hardware — an agent cannot sign off "feels good."
