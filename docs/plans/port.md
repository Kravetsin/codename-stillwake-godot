# Plan: the port to Godot and C#

Prototype 2 of `Kravetsin/codename-stillwake`, rebuilt on Godot 4.7 and C#
([0025](../decisions/0025-godot-and-csharp.md)). The TypeScript repo is the specification: a module's
README says what it guarantees, its tests are ported one for one and must pass here. Nothing new is
designed during the port; where C# or Godot makes a rule awkward, the rule keeps its meaning and the
difference goes into the module's README.

**Done when** a player cannot tell the two builds apart, but for the light: the start in the wreck,
salvage, the furnace, building, cables, air, the analyzer, co-op over the network. Then crafting
([crafting.md](crafting.md)) is built here.

Order: every task leaves `dotnet build` and `dotnet test` green and the game running.

## G-01 — The foundation ✅

The repo, the solution, CI (format, build, tests), the analyzers, the sim core (modules, phases, commands, saves, hashes, the
Rng, layouts), `grid` and `rooms` with their tests, the wreck as content, a Godot project drawing it.

## G-02 — The generic suites ✅

- Save, load and play on for every registered module; the desync fuzz: two worlds fed the same
  random commands from each module's fuzz generator, hashes compared every tick. A module with
  commands and no generator fails the suite.
- **Done when** the suites cover `grid` and `rooms`, and fail for a module that breaks them.

## G-03 — Content ✅

Everything in `packages/content` as C# data in `sim/Content`: items, materials, builds, machines with
costs, air, power, crew, hero, debris and loot, furnace, research, light. Its content tests ported.
Light is drawn only and in floats, so it lives in `game/Content/Light.cs`; the tests link that file.

## G-04 — The sim modules ✅

In dependency order, each with its README, tests and fuzz generator ported: `hazards`, `pipes`,
`items`, `power`, `atmosphere`, `crew`, `debris`, `furnace`, `building`, `research`. Then the wreck's
own tests (`modules/wreck.test.ts`).

- Ported: all twelve, and the wreck's own tests (`WreckTests`).
- A module namespace `Stillwake.Sim.<X>` hides the content class `Stillwake.Content.<X>` inside
  `Stillwake.Sim.*`: write `Content.Furnace.FuelSlot`.
- **Done when** every TypeScript sim test has a passing C# twin, and a sim tick on the reference
  world stays under 2 ms.

## G-05 — The network

`packages/net` over Godot's ENet: host and client, stamping, the measured reserve (0015), own actions
shown at once (0014), joining from a snapshot, hash checks, a local transport for tests, and the
desync fuzz across host and guest. A relay for playtests across networks (0012) when needed.

- **Built:** `net/` (its README) and `game/net/EnetLink.cs`; two Godot processes share the
  wreck, hashes checked, a drifted guest resynced, 355 ms round trip under `--lag=100 --jitter=60`.
- **Built since:** the folds of 0014 — `grid.AfterToggles` with `game/shared/ShownGrid.cs`,
  `items.AfterMoves`, debris grabs — and heroes replicated, drawn by `features/crew` (`Remote.cs`).
- **Left:** the relay is not needed while ENet connects directly; Steam crosses networks (G-09).

## G-06 — The world on screen ✅ (but overlays and i18n)

The camera; walls, edges, back walls, floors, ladders, doors and pipes as `TileMapLayer`s from the
grid's layers, kept current as it changes; machines as sprites; light (lamps by their satisfaction,
flicker, starlight, helmet lamps, fire) with normal maps and merged occluders; i18n. The art moves
from ASCII pictures to PNGs in `game/art`.

## G-07 — The hero ✅

The controller (`features/crew/hero.ts`, client-authoritative, unit-tested), the rig and its poses,
aim and reach, the tether and spacewalks, the camera following.

- Left from G-06/G-07: overlays and i18n. Suit air, passing out and the room reports came with
  `crew`; every hero lit by the ship's light, other players' heroes (`remote.ts`, interpolated, with
  their tethers), suit colours recoloured from the PNGs and the colour picker came with the world
  polish. The TypeScript game has no helmet lamp.

## G-08 — The features

Inventory, hotbar and crates; building (the plan, the menu with thumbnails, ghosts, removal, the
pipette, moving pegs); power (cables as ropes, the power mode); pipes and the air overlay; the furnace
(fire, embers); debris and catching (physics on the host); the analyzer and its panel; effects; the
dev panel.

- **Effects built** (`features/effects`, `render/Particles.cs`): dust from steps and landings, sparks
  from work on plans, embers, air streaming out of venting rooms; sparks and embers light their
  surroundings (0019). The air overlay's forecast counts the heroes breathing.

## G-09 — Builds and playtest

Saves as files, a Windows export, a build the team can hand to a friend; then the parity playtest
against the TypeScript build. Steam (GodotSteam) after.

## Deliberately not handled

- Old saves: TypeScript saves do not load here.
- The TypeScript playtest server keeps serving the old build until G-09.
- The review agents in `.claude/` are adapted once, with G-01; their checks grow with the port.
