# Wixx The Bard

A single-player 2D platformer played entirely on a 5-fret guitar controller.
**Godot 4 · C# · Steam target.** Mega Drive-silhouette pixel art.

- `SPEC.md` — design source of truth (kept with the design docs).
- `CLAUDE.md` — engineering law. Read both in full at the start of every session.

## Milestone status

**M1 — Input + Options (this commit).** The data-driven guitar input layer and
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
