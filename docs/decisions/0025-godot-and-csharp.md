# 0025 — Godot 4 and C#

- Status: accepted
- Date: 2026-10-14
- Supersedes: [0001](0001-stack-and-platform.md)

## Context

Four days of Stillwake on TypeScript, PixiJS, React and Electron (0001) built prototype 2 in
`Kravetsin/codename-stillwake`. Light took two rewrites (0018, 0019) on a renderer with no 2D light
system and GLSL ES 1.00 filters, and the team expects more of the same: shaders, particles, sound,
input, packaging, all assembled by hand. 0001 turned Godot down because agents were thought to work
poorly in an editor-centred engine. A spike (`stillwake-godot-spike`, not published) tested that
before deciding:

- **Look:** the wreck from our own map and art, lit by Godot's 2D lights with shadows, normal maps
  worked out from the pixel art, starlight through the torn roof, a helmet lamp: in an hour.
- **Sim:** the core, grid and rooms in plain C#, each module an assembly, tests ported from Vitest
  to xUnit and FsCheck, the world's mulberry32 drawing the same numbers as the TypeScript one.
- **Determinism guarded by the compiler:** BannedApiAnalyzers and an analyzer of our own refuse
  floating point, clocks, `Random`, culture-dependent strings and iteration in unguaranteed order.
- **Lockstep over ENet** between two Godot windows: a guest joining mid-game from a snapshot, the
  host's hashes catching a deliberately broken world on the next check.
- **Performance:** the 256×128 reference ship with 99 shadowed lamps at about 2000 fps at game zoom
  and 210 fps with the whole ship on screen, drawn with `TileMapLayer` and occluders merged per wall
  run (sprites and occluders per cell gave 18 fps zoomed out).
- **UI:** the inventory with the furnace panel in Control nodes in minutes, 1.3–1.5× the code React
  needed.
- **The agent's loop:** everything is text (C#, `.tscn`, `project.godot`); `--write-movie` records
  frames an agent reads; headless import and `dotnet test` run without the editor.

## Decision

| Layer          | Choice                                                                                   |
| -------------- | ---------------------------------------------------------------------------------------- |
| Engine         | Godot 4.7, .NET build, Forward+ renderer                                                 |
| Language       | C# 12 on .NET 8 everywhere: sim, net, client                                             |
| Simulation     | plain C# assemblies with no Godot in them, one per module; deterministic fixed tick      |
| Determinism    | Microsoft.CodeAnalysis.BannedApiAnalyzers and `sim/Analyzers`, errors at build time      |
| Rendering      | Godot 2D: `TileMapLayer` for cells, sprites for machines and heroes, 2D lights, shaders  |
| UI             | Godot Control nodes                                                                      |
| Physics        | Godot physics on the host only, reaching the sim as commands (0006)                      |
| Network        | our lockstep (0002, 0013) over ENet in development, GodotSteam in release                |
| Tests          | xUnit and FsCheck for the sim; scripted runs recorded with `--write-movie` for the client |

- The co-op model, the module rules and the decisions 0002–0024 stand; what they say of TypeScript
  packages is read as of the C# assemblies that replace them.
- Electron goes: Godot exports the desktop builds. Windows first, as before.

## Alternatives considered

- **Stay on TypeScript:** no rewrite, but every engine feature stays ours to build, light first.
- **GDScript:** Godot's own language, but dynamic: no compile-time module boundaries, no analyzers,
  and slower for a sim that walks a 256×128 grid every tick.
- **Unity:** heavy for simple 2D, a licence to watch.

## Consequences

- The game is rewritten in C#: the TypeScript repo is the specification, its tests the acceptance
  tests (`docs/plans/port.md`).
- Cells are drawn as tiles, never a node per cell, and occluders are merged per wall run: a node per
  cell costs CPU time with every light.
- An agent checks the client through recorded frames and stdout, about 10–20 s a check against
  Vite's instant reload; a dev-only query port into a running game would close part of that gap.
- Playtests are builds, not a link: the playtest page and relay of 0012 serve the TypeScript game
  until the port reaches it.
- C# web export is unsupported in Godot 4; web is out of scope anyway.
