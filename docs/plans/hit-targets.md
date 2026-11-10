# Plan: one answer to "what does a hit at this cell strike"

## Why

[unified-damage](../decisions/2026-11-09-unified-damage.md) gave every hit one formula: a strength, a crack, a surplus
carried on. It did not give them one target. Twelve places each decide for themselves what stands at a cell, which cell
keeps its crack and how strong it is, and they disagree:

- **Doors and hatches.** They crack, but no crack is drawn: `DigView.MaterialAt` skips pieces.
- **Stations.**
  - A tool's rays pass through a station. `Grid.Blocks` reads only structure (`sim/Grid/Grid.cs:441`).
  - So a swing at a furnace hits the wall behind it, or nothing. There is no spark, no sound and no crack.
  - Only bursts touch stations.
- **Two numbers per machine.**
  - `Hp` is what tools take off a pile, measured in a weapon's `Damage`.
  - `HitPoints` is what bursts take off any machine, measured in tenths of strength.
  - A burst reads a pile's `Hp` 12 as 12 tenths, so a charge or meteor walks through a pile almost free.

## The decision

- **One public query** of `BuildingModule` says what a hit at a cell strikes. Every hitter and every view asks it, so a new
  kind of target (the next machine, a new piece) is added in one place.
- **What the query does not decide:**
  - Whether a ray stops, which stays `WallAt` or `Blocks`.
  - How much of a hit a target takes. That is one rule for every hitter, `Taken` (PR 2): a player's hit on their own ship takes a tenth, every other in full.

  Stopping stays with each hitter; the share is one function.

## The twelve places

| Where | Asks | File |
| --- | --- | --- |
| sim | a burst's cost to enter, its break, its crack | `sim/Building/Bursts.cs` `BurstEnter`, `Break`, `HeadAt`, the crack loop |
| sim | a tool ray's hit, a carry, which damage | `sim/Building/Digging.cs` `Stops`, `PileAt`, `Carried`, `HitAll`, `PieceAt` |
| sim | a shot's take, key and surplus | `Digging.cs` `Hit`, `ShotTakes`, `ShotKey`, `Surplus` (`IBuildingQueries`, read by `sim/Creatures/Shots.cs`) |
| sim | which cracks heal or go | `Digging.cs` `Crackable` |
| sim | a pile needs the axe to take apart | `sim/Building/Rules.cs:91` |
| client | spark kind | `game/features/effects/logic/Impacts.cs` `At` |
| client | flash share | `Impacts.ShareAt` |
| client | crack drawing, break dust | `game/features/building/DigView.cs` `MaterialAt`, `PileMaterial`, `ShowPile` |
| client | hit sound | `game/features/sound/logic/GroundSounds.cs` `HitAt` |
| client | where a running tool touches | `game/features/crew/logic/ToolRays.cs:82`, `ToolEdge.cs:96` |
| client | which damage a hit uses (pile `Damage`, else `BlockDamage`) | `game/features/crew/HeroView.Hands.cs` `Struck` |
| client | a pile's readout | `game/features/scanner/logic/Scan.cs` `OfPile` |

`game/shared/logic/Piles.cs` (`At`, `Cracked`) only answers this question. It goes in PR 1.

## The query

```csharp
/// <summary>What a hit at a cell strikes: what it is, the cell its crack is kept at, and how much breaks it.</summary>
public readonly record struct HitTarget(TargetKind Kind, int Key, int? Strength, bool Wreck);
public enum TargetKind { Wall, Piece, Machine, Plant }

public HitTarget? TargetAt(int cell);   // BuildingModule, and on IBuildingQueries
```

| Kind | Where | `Key` | `Strength` |
| --- | --- | --- | --- |
| Wall | the front wall at the cell | the cell | its material's; null for `Material.Unbreakable` (bedrock, lava) |
| Machine | any cell of it (the lifepod is one) | its top-left | `MachineSpec.Strength`; null when `Fixed` (the lifepod) |
| Piece | any cell of a floor, ladder, platform, door or hatch | its head (`PieceOf(cell)[0]`) | its build's `Strength` |
| Plant | a plant in an open cell (no structure) | the plant's own cell | its flora's; 0 falls to any hit |

- **Null** means no target: an open cell without a plant, a grate or any structure with no build, and anything out of
  bounds. `TargetAt` does not read `Zone`: the fog is each hitter's rule, as today.
- **A plant a wall holds** is not the target at that wall: the wall is. "Holds" is `HeldBy`: a growth standing on the wall,
  in its stone, or hanging from it.
- **Null `Strength`** means nothing breaks it: a hit lands, sparks and stops there. The ship's walls keep their strength:
  that tools aboard break nothing is the tools' rule (`Aboard`), not the target's.
- **Units:**
  - `Wreck` is true for a pile only. Then `Strength` is in a weapon's `Damage`, as a wreck's.
  - Otherwise `Strength` is in tenths, the unit of `Weapon.BlockDamage` and of material strength.
  - A hitter picks `Damage` or `BlockDamage` by `Wreck`.
  - One line mixes the units on purpose: a burst pays a pile's `Strength` as tenths (accepted, below).
- **Read order:** wall first, then machine, then piece, then plant. A machine and a structure never share a cell
  (`sim/Grid/README.md`, Invariants), so the order between those two is moot.
- **Cost:** `TargetAt` calls `PieceOf` only for a piece, as `PieceAt` does today.
- **The client's look** comes from the same data:
  - a wall by its material;
  - a piece or machine as built metal (`Impact.Alloy`), as a built wall;
  - a pile as a wreck;
  - a plant as its growth.

  The sim says nothing about looks.

## Steps

### PR 1: the sim asks once (no behaviour change)

0. **Tests first, on main's code.** Each is a scenario, and each must pass before and after the refactor:
   - a burst crosses grass and costs nothing there;
   - a burst stops at a grate and at the fog;
   - a tool's ray passes a ladder, aboard, and a station, on a planet, and strikes a pile behind each;
   - a burst cracks a door at its head;
   - a back ruin piece under a front one keeps its crack.
1. **Content:**
   - `MachineSpec.Strength => Fixed ? null : Hp ?? Damage.MachineHp`; `Fixed` wins over `Hp`.
   - `MachineSpec.Wreck => !Fixed && Hp != null`.
   - `HitPoints` goes. Its readers switch to `Strength`: `Bursts.cs`, `Digging.cs:379`, `MachineBreakTests.cs:86,148`.
   - `Hp` stays as the number a pile's row writes.
2. **`BuildingModule.TargetAt`** goes in a new file, `sim/Building/Targets.cs`, with the record and enum. `PieceAt` moves
   there as a private helper; its callers keep it where they need a piece's `Build`.
3. **Bursts:**
   - `BurstEnter`:
     - out of `Zone`: null (stops), first, as today;
     - no target and an empty structure: 0;
     - no target and a structure (a grate): null;
     - a plant: 0, as an open cell;
     - null `Strength`: null;
     - else `_burstPaid` by `Key`, then `Share(cell, Remaining(Key, Strength))`.
   - `HeadAt` is deleted; `Key` replaces it.
   - `Break`:
     - a wall: `BreakWall`;
     - a machine whose `Key` is the cell: `BreakMachine`;
     - a piece whose `Key` is the cell: `BreakPiece`;
     - a plant or no target: nothing.
   - The crack lambda adds `(Key, crack)` for a wall, machine or piece, never a plant. The crack loop calls
     `Cracked(Key, Strength, left)`.
4. **Tools:** `Stops` keeps its stop rule and asks `TargetAt` only for what it adds.

   | Case | Added to `cells` | Stops |
   | --- | --- | --- |
   | a pile (`Wreck`), anywhere | `Key` | yes |
   | aboard (`Aboard(cell)`), anything else | nothing | `WallAt != null \|\| Blocks` |
   | a wall | `Key` if `Strength` is not null | yes |
   | a piece | `Key` | `Blocks(cell)` |
   | a machine, not a pile | nothing until PR 2 | no |
   | a plant | added to `plants` once | no |
   | nothing | nothing | `Blocks(cell)` |

   - `PileAt` becomes `TargetAt(cell) is { Wreck: true }`.
   - `Carried`: a wall or piece target with a strength, never a pile or plant.
   - `HitAll` chooses `Damage` or `BlockDamage` by `Wreck`, and the hit by `Kind`.
5. **Shots:**
   - `ShotTakes`: where tools break, in `Zone`, a wall or piece target with a strength. That is today's rule.
   - `ShotKey`: a piece's `Key`, else the cell itself. A grate has no target and keeps its cell, as today.
   - `Surplus`: reads the target's `Strength`, after `ShotTakes`.
   - `Shots.cs` is unchanged.
6. **`Crackable`:**
   - Back walls as today.
   - A front crack stays where `TargetAt(cell)` is a wall, piece or machine with a strength, keyed at the cell.
   - A plant's crack stays by today's own-cell check (`_plantOwn`). That keeps a back plant under a front one.
   - `Rules.cs:91` reads `Wreck`.
7. **`game/shared/logic/Piles.cs`** is deleted. Its readers (`Impacts`, `DigView`, `GroundSounds`, `HeroView.Hands`, `ToolRays`, `ToolEdge`) read
   `m.Building.TargetAt(cell)` with `Wreck` in its place, the same behaviour.
8. **Tests:**
   - Step 0's tests and every existing test pass.
   - `TargetTests`: one per kind, with key and strength, including a door's lower cell (its head), a machine's far cell (its
     top-left), the lifepod (null strength, not `Wreck`), bedrock, an open cell, a grate, and a plant a wall holds (the wall).
   - It bites: break a piece's `Key` once and watch a door test fail.
   - `BudgetTests` keeps its burst numbers.
9. **Docs:**
   - `sim/Building/README.md` gets one rule line: `TargetAt` is the only place a hit's target is chosen. It is at 8086 bytes,
     so cut what is added.
   - A new ADR, `2026-11-10-hit-targets.md`:
     - one query;
     - stopping stays with each hitter; the share is one rule (PR 2);
     - the scales meet in `Wreck`;
     - the two numbers of a machine are one.

### PR 2: one rule for every hit; doors crack, stations spark and break

**The rule.** It is damage-scale's rule, now for every hitter:
- A hit a player makes on their own ship takes a tenth (`Damage.OwnShipTakes`). This covers a tool, a shot and a charge.
- Every other hit takes in full.
- "Their own ship" is `Aboard(cell)`: where the surface's tools break nothing, outside a vessel's box.
- A pile is cargo, not ship: it takes in full anywhere.
- No target is spared otherwise. Stations stand only on the ship and in vessels: a tenth aboard, in full in a vessel.

A fan's rays pass through what does not block them (a ladder, an open door, a plant) and strike each once per swing. A
station joins them.

1. **One share** for every hitter: `Taken(peer, cell, hit)`.
   - Where `peer` is a player, `Aboard(cell)` holds and the target is not `Wreck`, it is `hit × OwnShipTakes / 1000`,
     rounded down. Otherwise it is `hit`.
   - Bursts keep their `Share`/`Softened` pair, which is the same test priced upward. Tools and shots call `Taken`.
   - A creature's hit has no player peer and takes in full.
2. **Tools strike machines and the ship:**
   - `Stops` loses its aboard branch. Aboard, a ray stops and strikes as anywhere else.
   - A machine with a strength is added by `Key`. The ray goes on through it, as through a ladder.
   - `HitAll` and the carries apply `Taken`. A pickaxe takes 7 off a station or a scrap wall aboard, which breaks it in 58
     blows; in a vessel it takes 75, so 6 blows. The axe takes 4 aboard and 40 elsewhere.
   - What breaks gives its cost to the hitter, as taking it apart does.
   - The lifepod (null `Strength`) is struck and sparks, and never breaks.
   - `HewProblemOf` loses `building.notHere`: a fan that strikes nothing is `building.nothingToDig` everywhere. Marks keep
     their `notHere` (`Rules.cs`).
3. **Shots strike machines and the ship:**
   - `ShotTakes` drops `Rules.ToolsBreak`. A shot strikes a wall, a piece or a machine with a strength in `Zone` or in a
     vessel's box, as `Listed` reads cells.
   - A machine is a pass of its own in `TargetIn`, keyed by its `Key` (`ShotTarget.Machine` = 3), met once. Shots save
     their `Passed`, and an added enum value needs no format bump (added-save-fields-need-no-bump).
   - The block damage goes through `Taken`. Broken, the target carries the surplus on (`Gun.Carries`); only cracked, it stops
     the shot. A nail takes 1 off the ship.
   - `StepOf`, the client's copy, judges the same way through `Surplus`.
4. **A machine broken by a hit** (`BreakMachine`, shared with bursts):
   - What it holds spills, and its queue's refund falls at its cell.
   - Its cost goes to the hitter, as a wall's does.
   - Its cables stay on a peg. A debris net goes by `FreeNet`.
   - `BreakMachine` takes the hitter (`int? to`) beside `Spoils`; a burst passes none.
5. **`Impacts`:**
   - `At(IGridQueries, BuildingModule, cell)`:
     - a wall: by its material, as today;
     - a piece or machine: `Alloy`;
     - a pile: `Wreck`;
     - a plant: its growth.
   - `ShareAt(grid, building, cell, tool, damage)`:
     - with a target: 1 when its crack (`building.Cracks` at `Key`) plus `damage` reaches `Strength`, else the tool's share;
     - with no target or a null `Strength`: the tool's share.
   - The client's `damage` is the sim's `Taken` for that peer and cell. `BuildingModule` exposes it.
6. **`GroundSounds.HitAt`:** a piece or machine rings as metal under the tool (`S.WreckHitOf(tool)`), as a built wall does.
7. **`DigView`:**
   - For each drawn crack it keeps the target's kind and the cells it covers, read when drawn (a piece's `PieceOf(Key)`).
   - A piece's crack draws over every cell, in built tones (`DamageLooks.Scrap`), at `Target.Strength`. Machines draw as
     today.
   - A crack is broken out when `TargetAt(Key)` no longer has that kind and key, not when the cell is empty: a plant that
     appears there afterwards hides no dust.
   - A broken piece throws its dust from each stored cell.
   - `MaterialAt` and `PileMaterial` go. `ShowPile` asks `Wreck`.
8. **Touches** (`ToolRays.cs:82`, `ToolEdge.cs:96`) and the hands' look and sound (`HeroView.Hands.Struck`):
   - A ray touches the first cell where it stops, or where `TargetAt` is a plant or a machine.
   - A station sparks where the ray first meets it.
9. **What the player sees:**
   - Every hit on anything built sparks or chips. A station, a door or a hatch rings as metal and shows its cracks; doors and
     hatches show them over all their cells.
   - Aboard, these come slowly, at a tenth; in a vessel and on a planet, in full.
   - What breaks falls apart in dust and chips. A station spills what it held and gives its cost to whoever broke it.
10. **Tests:**
    - A pickaxe aboard takes 7 off a station and off a scrap wall.
    - In a vessel the same blow takes 75, and a station breaks in six.
    - A pile aboard takes the weapon's damage in full.
    - A creature's hit aboard takes in full.
    - A nail aboard takes 1.
    - A fan over a station and the wall behind it hits both once.
    - A broken station's cost goes to the hitter, and its contents spill.
    - A railgun breaks a station in a vessel and carries on; the nailer stops in it.
    - The lifepod takes nothing.
    - `game.tests/ImpactsTests`: a door, a station and the lifepod give `Alloy`; the blow that breaks a door has share 1.
    - Each test is proven to bite.
11. **Dev room:** `--vessel --breakers` (pickaxe, sledgehammer, axe, charges, railgun, creative). It shows both sides of
    the rule: the vessel's hatches and machines in full, the ship's at a tenth.
12. **Docs:**
    - The damage-scale ADR's item 2 is superseded by the new ADR's "every player hit, not only a charge".
    - The effects, building and creatures READMEs get their rule lines.

## Settled with the user (2026-11-10)

- **One rule:** a player's hit on their own ship takes a tenth, whatever made it; every other hit takes in full. Tools and
  shots join the charge. There is no exception for stations, doors or a base.
- **A burst passes a pile almost free** (12 tenths). This is accepted: the player clears the hold's piles at the start,
  long before the first charge, which is found on planets.

## Deliberately not handled

- **Back walls** keep their own crack path. Only a meteor's burst breaks them (`BackCost`); tools left them (0107).
- **The scanner** reads piles only. A readout over a door or a station could follow when the player asks how hurt it is.
- **Wrecks in space** (`Debris`) are not grid targets. `Wreck` says only that a pile is measured like one.
- **A door's crack** looks like a built wall's, not a picture of its own.
