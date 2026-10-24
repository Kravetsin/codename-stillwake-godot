---
description: Add a machine the player builds — its row of data, art, name, testbed spot and the checks before a PR.
argument-hint: <kind> "<what it does>"
---

User arguments: `$ARGUMENTS` — the machine's kind (camelCase, `capacitorBank`) and what it does. Ask for either if
missing, and stop.

A machine whose behaviours already exist is **a row of data, its art and its name** (0113, 0114): no system names a
kind, and the build menu is laid out from the row. Done when the steps below are, the full tests pass and a frame shows
it. Walked last on `39f6ed5c` (the capacitor bank) and on today's code with a throwaway machine.

## 0. Is it data, or a new mechanic?

Read `MachineSpec`'s parameters at the top of `sim/Content/Machines.cs` and `MachineTrait` below it. If what it does is
one of them (makes or draws power, stores it, holds slots, burns fuel, charges, fits tools, a trait such as `Zone`), it
is data: go on. If not, it is a new mechanic: stop and start with `design-author` (CLAUDE.md, Workflow), not here.

A trait (`Has: MachineTrait.<Trait>`) is what makes systems treat it as their own: `Container` if drones should store in
and take from it as from a crate, `Zone` for a drone bay's area. Give one only when the systems that read it must; each
trait's list of machines is spelled out in `sim/Tests/MachineTraitsTests.cs` (`EachTraitIsWhatItsMachinesAre`): add the
kind there too.

## 1. Its row — `sim/Content/Machines.cs`

One line in `Machines.All`, beside the machines of its kind, with a comment above only for a non-obvious why:
- `Width`, `Height` in cells (8 px each; a map character draws 2×2 cells).
- `Cost`: whole units of real items (`ContentTests` checks); `BuildTicks` like its neighbours' (a small one 60–100, a
  4×4 one 200).
- With `Makes`, `Draws` or `Store`: `Plug`, in pixels of its picture, inside it.
- `Menu: new(MenuTabs.<Tab>, <order>)`: its card; order 10s apart, the card's place in its tab.
- `Char`: a map character nothing else takes, and `Means`. `Legend.Map` in `sim/Content/Legend.cs` is every taken one:
  the space, `.`, the structures' (`sim/Content/Structures.cs`), the pipe's `- v x`, the machines', `S`.

Every number a designer may tune lives here or in `sim/Content`, never in a module.

## 2. Its picture — `game/art/machine_<kind>.png`

`Width × 8` by `Height × 8` pixels (more only with a rise, as the arc bench's vise: `ShipTiles.RiseOf`). The grid draws it
by name, and a missing one throws on the first frame (`MachineArtTests`). After adding it:
`godot --headless --path game --import` makes its `.import`; commit both. A painter script, if you wrote one, goes in
`tools/ship-art/machines/` with its row in `tools/ship-art/README.md`.

## 3. Its name — `game/features/building/logic/Building.En.cs`

`["tool.<kind>"] = "Its name"` and `["tool.<kind>.about"] = "What it is for, in a sentence."`, beside the others.
Player text only through these keys.

## 4. Its spot on the testbed — `game/dev/logic/Testbed.cs`

`TestbedTests.DrawsEveryKindOfMachine` wants every machine on the testbed. Put its `Char` in `Testbed.Map` in place of
`.` cells inside a room, standing on the bottom row or another machine, a character per 2×2 cells (so a 2×4 machine
is one column of two characters), out of the airlock (the rightmost room). It is tight: read the whole drawing first.
Then `tools/test.sh Testbed game.tests` is the judge: the drones and the air must still hold. Mention it in the map's summary if it is
wired (a cable in the list under the map).

## 5. The menu's order — `game.tests/BuildMenuDataTests.cs`

Two tests spell the menu out tab by tab; add its kind where its order puts it, in both.

## 6. Words — `docs/glossary.md`

A line for its game name (and the Russian word, if there is one) → `<kind>`, if a player will say it.

## Checks before the PR

- `tools/test.sh` whole: `ContentTests`, `MachineArtTests`, `TestbedTests`, `BuildMenuDataTests`, `BuildingToolsTests`
  (its name and about), `MachineTraitsTests` (with a trait), `MachineKindGuardTests` (nothing names its kind where a
  trait is meant).
- A frame: `tools/snap.ps1 out.png -- --testbed --near=<kind>`; look at it.
- No save change: machines are saved by kind. Only new saved state in a module follows the save rule (CLAUDE.md).
- No hand-written list to extend beyond the ones above (the menu's order, the testbed, a trait's machines); if you found
  another, say so in the PR and add it here.
- Commit `feat(<module>): <kind> …`, the module its behaviour belongs to (`power` for a store).
