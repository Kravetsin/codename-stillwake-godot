# Damage scale: the plan

Design: [damage-scale.md](damage-scale.md). Worktree `../stillwake-damage-scale`, branch `feat/damage-scale`. Each step is one
commit that passes the full `dotnet test Stillwake.sln`; each new rule gets a test proven to bite by breaking it once.

## 1. `feat(building)`: one scale; a player's own ship takes a tenth

The rule and the numbers change together: the old share (1/27 of anything built) on the new strengths, or the new share on
the old ones, would make every test in between meaningless.

- **Content.**
  - `Materials`: scrap, wreckScrap and castScrap 20 → 400; iron and `Vessels.Hull` (it draws as iron) 50 → 500; glass, mica
    and hide 10 → 200. Soil, stone, plaster, veins and alloy unchanged. Update each `Strength` comment to give its pickaxe
    blows.
  - `Build.Strength` default 80 → 200 (floor, ladder, platform); door and hatch rows `Strength: 500`.
  - `Damage.MachineHp` 20 → 400. `Damage.BuiltTakes` 37 goes; `Damage.OwnShipTakes` 100 (thousandths) comes, its summary
    the rule: what a player's own burst does aboard.
  - `Meteors`: both showers, `BeforeLanding` and `StartingSpace`, `PerPower` 220 → 170; their comments' numbers (541 a cell,
    440–1100, 1352) recomputed to the design's table.
- **`Bursts.cs`.** `Burst` first clears its four lists and sets a field `_burstOwn = peer != FloorStack.Nobody`, all before the
  first `BurstEnter` (today `_burstPaid` is read for the first cell before it is cleared: a stale list from the last burst
  would decide one cell on the host only, a desync; fixed here, with a test). A cell is *aboard* where `Aboard(cell)`:
  `!Rules.ToolsBreak` (the surface's rule that tools break nothing, the player's own ship) and not `InVessel(cell)`. A cell is
  *softened* when `_burstOwn` and aboard. For a softened cell:
  - its entry cost, wall (natural or built), machine (a pile too) or piece alike, is `strengthLeft * 1000 / OwnShipTakes`
    rounded up (`Built`'s ceiling, renamed `Softened`): × 10 today;
  - unpaid, it cracks by `powerLeft * OwnShipTakes / 1000`, rounded down, `powerLeft` being what the burst had left on
    reaching it (a 500 charge, a step from the wall, cracks scrap by 49; nine break it). Rounding the cost up keeps that crack below
    `strengthLeft`, so it never silently clears.
  - Everywhere else a cell costs its remaining strength and cracks by what is left, with no share. The
    `wall.Natural` and `spec.Hp != null` branches go. `Built` and `ToBreak` go.
  - `Aboard(cell)` replaces the condition in `Digging.Stops` (line 86), with no change in behaviour, so tools and bursts
    share it.
  - Back walls are reached only under `Spoils.Lost` (meteors, the dev hit, peer `Nobody`), so they always cost in full.
- **`Hazards.cs`.** The dev `MeteorHit` with no power bursts the wall's full `Strength` at reach 0
  (`_grid.WallAt(c.Cell)!.Strength`): on a cracked wall it overpays, and reach 0 keeps the surplus in that cell. Tested on a
  cracked scrap wall and on alloy: each breaks, its back wall stands.
- **Tests** (rewritten to the new numbers, not deleted):
  - `BurstTests`: the share is by place and by sender.
    - A charge aboard costs ten times and cracks by a tenth: wall, door piece, machine and pile alike.
    - A `Nobody` burst aboard is in full.
    - Any burst in a vessel's box aboard is in full.
    - Any burst on a planet is in full; the built and natural walls there cost the same.
    - The FsCheck property in `BurstTests` computes the expected cost by the new rule and asserts an unpaid crack stays
      below what is left.
    - A second burst after a save and load breaks what it breaks in the world kept running (the stale-list fix).
    - Bites: drop the `peer` test once and watch the meteor case fail; drop `InVessel` once and watch the box case fail.
  - `BlastChargeTests`, `MachineBreakTests`: shares by place; a charge off the ship breaks scrap struck and cracks its
    neighbours; on iron, the cell struck; on alloy, a crack; nine charges aboard break a scrap wall.
  - `HazardsTests.Meteors`, `NetTests`: the design's table.
    - Power 2 cracks scrap by 340; 3 holes the outer cell; 5 breaches a two-thick room.
    - Iron holes at 3 and breaches at 6.
    - A second power 3 on a fresh crack breaches.
    - A meteor next to a thrown charge aboard is still in full.
    - The pre-landing shower too: power 2 cracks, 3 holes, 5 breaches.
    - `HazardsTests.Meteors.Scrap` reads `Strength`.
  - Tool counts: `PickaxeTests`, `CarryTests`, `DrillArcTests`, `RailgunTests`/`ShotsTests`, `WreckScrapTests`,
    `HewTests`, `DigTests`, `DebrisTests` and any other failing count move to the new blows; scenario maps whose `#` scrap
    a tool or shot must break at once. The pickaxe breaks alloy in 8, iron in 7 and
    scrap in 6. Where a test only needs "breaks at once", it draws soil instead of scrap.
  - `DigTests`: tools still do nothing aboard and work in a vessel's box, through `Aboard`.
  - `ContentTests`: built walls stay below alloy and the pickaxe breaks every breakable material, so no wall is ever out
    of a hand's reach.
- **Client.** `effects/logic/Impacts.cs` `ShareAt` gives the "whole hit" only to a natural wall's breaking blow; now that built
  walls take six blows, it takes any breakable wall (crack plus damage against `Strength`); its tests follow. `Look` keeps
  metal sparks on built walls. Nothing else reads the share; `DigView` and the effects read a crack against `Strength`, so they scale. Dev rooms
  that throw charges at hulls (`--charge` and the like) are run once to check they still show what they are for; a demo
  relying on a one-blow wall gets soil.
- **No save step.** No strength drops, so every saved crack stays below its cell's strength; old cracks (at most 20 on
  a machine or hull) become a few per cent and heal.

## 2. `docs`: the scale's ADR; the plans go

- New ADR `docs/decisions/2026-11-09-damage-scale.md`. Status: "accepted; built. Supersedes in part
  [unified-damage](2026-11-09-unified-damage.md) (the built share)". It covers:
  - the blow table;
  - the own-ship rule and why it is by sender and place, not material;
  - the meteor table;
  - the deliberate omissions and risks from the design.
- `unified-damage` status gains "superseded in part by damage-scale". `tools/adr-index.ps1` rewrites the index.
- Doc comments that quote the share or `ToBreak`: `Content/Blasting.cs`, `Materials.cs` (header), `Machines.cs`
  (`HitPoints`), `Damage.cs`, `Building.cs:92`, `Bursts.cs`. `meteor-wave` is an old ADR and stays as written.
- READMEs, each under 8 KB (Creatures too, line 75, if it quotes the share):
  - Building: bursts, the share, the dev hit;
  - Hazards: the meteor numbers;
  - `game/features/hazards` if it quotes powers.
- `docs/concept.md` if it quotes a strength.
- Delete `damage-scale.md` and this plan.
- PR, merged on green.

## Not in this plan

Marks on planets (a misplaced wall there is six pickaxe blows) and salvage tools: each a task of its own.
