# Wixx The Bard

A single-player 2D platformer played entirely on a 5-fret guitar controller.
**Godot 4 · C# · Steam target.** Mega Drive-silhouette pixel art.

- `SPEC.md` — design source of truth (kept with the design docs).
- `CLAUDE.md` — engineering law. Read both in full at the start of every session.

## Milestone status

**M0 — Scaffold (this commit).** A Godot 4 C# project that opens and runs, repo
layout, formatter/lint config, a `Tunables` resource as the single home for
gameplay numbers, a gdUnit4 test harness, and a hello-world scene: a
`CharacterBody2D` box on a real `TileMapLayer` with collision, moved by the
keyboard, proving collision + fixed-step (`_PhysicsProcess`, 60 Hz) movement.

The remappable, data-driven guitar input layer lands in **M1**.

## Repo layout

```
scenes/    Godot scenes (Main.tscn — the hello-world level)
scripts/   C# gameplay scripts (Player, LevelGeometry)
config/    Tunables.cs + Tunables.tres — the single source of gameplay numbers
tests/     gdUnit4 C# tests
reference/ READ-ONLY validated GDScript prototype (WixxMovement.gd)
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

**Controls (M0 keyboard placeholder):** `←` / `→` move, `Space` jump. The box
falls under gravity, lands on the floor, collides with the side walls and the
ledge.

## Test

```bash
dotnet test
```

Runs the gdUnit4 suite (requires `GODOT_BIN` set). Expect the two
`TunablesTest` cases to pass.

## Format / lint

```bash
dotnet format WixxTheBard.csproj
```

Style and naming rules (PascalCase methods, `_camelCase` private fields) live in
`.editorconfig`.
