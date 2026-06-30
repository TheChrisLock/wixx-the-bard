# Wixx The Bard

A single-player 2D platformer played entirely on a 5-fret guitar controller.
**Godot 4 · C# · Steam target.** Mega Drive-silhouette pixel art.

- `SPEC.md` — design source of truth (kept with the design docs).
- `CLAUDE.md` — engineering law. Read both in full at the start of every session.

## Milestone status

**M4 — Spell performance (this commit).** One ability end-to-end (SPEC §5): trigger →
global time-slow + desaturation → right-to-left note chart (calibration-relative) →
success fires the spell + cooldown / fail = cooldown only.

- **Pure, Godot-free rhythm core** (`scripts/Performance/`, namespace
  `WixxTheBard.Performance`), all unit-tested under plain `dotnet test`:
  - **`PerformanceJudge`** — the rhythm-window math. Classifies each strum into
    Perfect / Good / Miss / Stray, picks the nearest open note in the strummed lane,
    and auto-misses notes whose window elapses. **Timing is calibration-relative
    (rule 6):** every verdict is measured against the player's stored A/V latency
    offset, never raw clock time (a consistently-late player with a matching offset
    judges dead-on).
  - **`SpellPerformance`** — the trigger→play→resolve state machine. The note clock
    runs at **real time** so the song keeps tempo and input stays 60 Hz-sampled
    (rule 3); the performance completes a Good-window after the last note and reports
    a success/fail/perfect `PerformanceResult` (SPEC §5.4).
  - **`NoteChart` + `SpellCharts.Kindle`** — the one shipped ability is a fixed
    tier-1 single-note song (SPEC §5.2/§5.3). Note times are **content**, not
    feel-tunables; the windows / success threshold / slow factor / cooldown are.
- **`Player`** triggers on **Blue (Special1)** when grounded and off-cooldown, then
  owns the strum as the sole "play" consumer (rule 5): a strum (either direction) is
  a strike and the held fret picks the lane — all read through data-driven **verbs**,
  never raw indices (rule 2). On completion the spell cooldown always starts; success
  fires a grey-box chord-blast. The world creeps on a published `TimeSlowFactor`
  budget (SPEC §5.2 global slow) — deliberately not via `Engine.TimeScale`, which
  would starve the fixed-tick rhythm input.
- **Presentation reads state, never drives it (rule 7):** `PerformanceHud` draws the
  right→left highway + strike zone + result banner; `DesaturationOverlay` +
  `Desaturate.gdshader` grey the world while the chart is up, leaving the highway
  vivid. Both observe the authoritative `Player` performance state.
- **`Tunables`** carries every M4 number (Perfect/Good windows, success threshold,
  time-slow factor, spell cooldown, banner hold) — no magic numbers in gameplay code.

**M3 — The verbs.** The full instrument's gameplay verbs on top of
M2 movement (SPEC §2.3/§2.4/§4.4), each as pure Godot-free logic the node reads:

- **Pure verb logic** (`scripts/Verbs/`, namespace `WixxTheBard.Verbs`), all
  unit-tested under plain `dotnet test`:
  - **Lute swing** (`SwingController`) — a short, re-armable active hitbox window.
  - **Tilt super-jump** (`SuperJumpController`) — fires on the tilt *level* (not an
    edge) + on-floor + cooldown, then launches via `MovementCore.ForcedLaunch` so it
    is **uncut** (rule 4) and reaches ~5× height.
  - **Whammy crouch/slide** (`CrouchState` + `SlideController`) — crouch lets you
    move at a slow crouch-walk; crouching with real momentum triggers a **committed
    slide** that glides a short distance and decays to a stop on its own (ignoring
    the held strum), and won't re-trigger until the whammy is released and re-pressed.
  - **Tar struggle** (`TarState`) — plunge in → sink → **clean alternate strum**
    climbs (mashing one way barely helps) → surface breach leaps out (uncut) → full
    submersion drowns and respawns at the entry edge.
  - **`Cooldown`** — the shared fixed-tick gate behind the super-jump.
- **`Player`** orchestrates all four from data-driven `GuitarInput` verbs (never raw
  indices), in `_PhysicsProcess` at the fixed tick; the strum is consumed by exactly
  one mode at a time (movement *or* tar — rule 5).
- **`Enemy`** (basic, dispatchable by swing or slide) + **`TarPit`** (Area2D hazard)
  wired into `Main.tscn`; a floor gap hosts the pit.
- **`Tunables`** carries every M3 number (swing window, super-jump velocity/launch/
  cooldown, slide threshold, all `Tar*` values) — no magic numbers in gameplay code.

**M2 — Core movement.** Wixx's real `CharacterBody2D` movement,
porting the validated `/reference` feel onto real collision and the data-driven
input layer (SPEC §2.2, §2.3, §14).

- **Pure, Godot-free movement core** (`scripts/Movement/`, namespace
  `WixxTheBard.Movement`): the Hold scheme (Scheme B), sprint-charge → max-speed,
  the variable-height jump, and — baked into the physics contract — the
  **forced-launch jump-cut exemption** (CLAUDE.md rule 4) that M3's super-jump and
  tar-exit leap will use. All constants are per-60 Hz-tick, ported 1:1 from
  `/reference`; all unit-tested under plain `dotnet test`.
- **`Player`** drives the core from `GuitarInput` verbs (never raw indices),
  entirely in `_PhysicsProcess` at the fixed 60 Hz tick, scaling per-tick velocity
  to px/second for `MoveAndSlide` against the real `TileMapLayer`.
- **`Tunables`** now carries the ported movement/gravity numbers (the single
  source of truth — no magic numbers in gameplay code).

**M1 — Input + Options.** The data-driven guitar input layer and
the Options screen that houses both control remapping and A/V latency
calibration — the project's #1 de-risk item (SPEC §10, §11, §12).

- **Pure, Godot-free input core** (`scripts/Controls/`, namespace
  `WixxTheBard.Controls`): verbs → bindings resolution, press-to-bind capture
  with **axis rest/direction auto-detect**, common-controller presets as data,
  robust latency-offset math, and JSON persistence. All unit-tested under plain
  `dotnet test`.
- **`GuitarInput` autoload**: polls the joypad's *raw* button/axis state each
  fixed tick, resolves it against the active bindings, and exposes verbs (never
  indices) to gameplay. Keyboard fallback OR'd in for dev.
- **Options screen** (`scenes/Options.tscn`): per-verb rebind, preset picker, a
  live input monitor (every verb lights when engaged), and a tap-the-metronome
  A/V calibration. Opened with **Esc** from the hello-world scene; the box now
  moves through the new input layer.

**M0 — Scaffold.** Godot 4 C# project, repo layout, formatter/lint, the
`Tunables` resource, gdUnit4 harness, and a `CharacterBody2D` box on a real
`TileMapLayer` proving collision + fixed-step (`_PhysicsProcess`, 60 Hz) movement.

## Repo layout

```
scenes/           Godot scenes (Main.tscn, Options.tscn)
scripts/          C# gameplay scripts (Game, Player, LevelGeometry, GuitarInput, OptionsScreen)
scripts/Controls/ Pure, Godot-free input core (bindings, capture, presets, calibration) — unit-tested
scripts/Movement/ Pure, Godot-free movement core (Hold scheme, jump, sprint, rule-4 launch) — unit-tested
config/           Tunables.cs + Tunables.tres — the single source of gameplay numbers
tests/            gdUnit4 C# tests
reference/        READ-ONLY validated GDScript prototype (WixxMovement.gd)
```

## Prerequisites

- **.NET SDK 8.0+** (`dotnet`)
- **Godot 4.4 .NET (Mono/C#) build.** Set `GODOT_BIN` to its executable so the
  test harness and headless commands can find it, e.g.
  `export GODOT_BIN=/path/to/Godot_v4.4-stable_mono`

> NuGet package versions in `WixxTheBard.csproj` (`gdUnit4.api`,
> `gdUnit4.test.adapter`) target Godot 4.4; bump them to match your installed
> Godot if it differs.

## Build

```bash
dotnet build WixxTheBard.csproj
```

The first run from the Godot editor also (re)generates the C# glue. To build the
Godot project headlessly:

```bash
"$GODOT_BIN" --headless --path . --build-solutions --quit
```

## Run

Open in the editor:

```bash
"$GODOT_BIN" --path .
```

Or run the main scene directly:

```bash
"$GODOT_BIN" --path . scenes/Main.tscn
```

**Controls.** With a guitar plugged in, the bound strum moves the box and Green
jumps (default = the validated Ardwiino map; remap anything in Options). Keyboard
fallback for dev: `←` / `→` move, `Space` jump, `Shift` sprint, `J` swing,
`K`/`L` specials, `↓` crouch, `X` super-jump (the last five light the Options
live monitor but aren't yet wired to movement — that's M2/M3). Press **Esc** to
open Options (remapping + A/V calibration).

## Test

```bash
dotnet test
```

Runs the gdUnit4 suite. The M0 smoke test (`ScaffoldSmokeTest`) is Godot-free, so
it passes under a plain `dotnet test` host. Tests that instantiate Godot types
(`Resource`, nodes, etc.) must run inside the Godot runtime — set `GODOT_BIN` so
the gdUnit4 adapter can host them; those land in M1.

## Format / lint

```bash
dotnet format WixxTheBard.csproj
```

Style and naming rules (PascalCase methods, `_camelCase` private fields) live in
`.editorconfig`.
