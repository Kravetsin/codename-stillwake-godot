# One damage scale; the own ship takes a tenth

- Status: accepted; built; supersedes in part [unified-damage](2026-11-09-unified-damage.md) (the built share, scrap 20 and
  iron 50, meteors' `PerPower` 220, `ToBreak`) and [catcher-paths](2026-11-09-catcher-paths.md) (a catcher's strike at the
  built share)
- Date: 2026-11-09

## Context

Unified damage gave every hit one rule but kept strengths tuned for digging ground with a pickaxe. Built walls were never
meant to be hit: scrap 20 and iron 50 went at one blow of anything, so a derelict's hull melted in seconds. Its built share
(a 27th of a burst for anything not natural) protected the wrong thing: it made the player's alloy walls weaker aboard than
iron, and every built thing on a planet nearly charge-proof. What the player wanted protected is their own ship from
their own charges, nothing else.

## Decision

1. **Strengths are set by pickaxe blows** (75 a blow), anchored on alloy as it was (600, eight): the strongest wall stays
   breakable by hand, so no player is stuck before one. Scrap, wreck and cast scrap 400 (six blows); iron and a vessel's hull
   500 (seven); glass, mica and hide 200; a machine (`Damage.MachineHp`) 400, a scrap wall's; a door or hatch segment
   (`Build.Strength`) 500, floor, ladder and platform 200. Ground, plants, ruins' furniture and piles keep theirs.
2. **A player hits their own ship at a tenth.** A burst a player set off (its `peer` not `Nobody`: the thrown charge) pays
   ten times a cell's remaining strength (`Damage.OwnShipTakes` 100 ‰, rounded up) and cracks an unpaid cell by a tenth of
   what was left (rounded down) where the cell is the ship's: tools break nothing on the surface (`!Rules.ToolsBreak`) and it
   is outside a vessel's box. That is `Aboard(cell)`, shared with `Digging.Stops`. Everything else hits in full: meteors and
   later hazards aboard, any burst on planets and derelicts, built or natural alike. `Natural` no longer decides damage.
   A charge thrown aboard cracks a scrap wall by 49; nine break it.
3. **Meteors** hit in full, `PerPower` 170 for both showers. On a scrap hull two thick (400 a cell, 802 through): power 2
   (340) cracks it, 3–4 hole the outer cell, 5–7 breach; iron holes at 3 and breaches at 6. A second meteor of power 3 or
   more on a fresh crack still breaches. `MeteorHit(cell)` (dev) bursts with the wall's full strength at reach 0;
   `BuildingModule.ToBreak` is gone.
4. **A burst clears its lists before pricing its first cell.** It read `_burstPaid` for the first cell before clearing it, so a
   machine rebuilt on the cell of one the last burst broke entered free on the host only: a desync, fixed here.
5. **The client** gives a built wall's breaking blow the whole hit flash, as rock's (`Impacts.ShareAt`).
6. **A catcher's strike** (`Wear`, `Strikes`) takes what is built in full too, as it only ever strikes where tools break:
   a scrap wall is four strikes of its 120.

No save step: no strength drops, so every saved crack stays below its cell's strength; old cracks become a few per cent and
heal within a minute.

## Accepted findings

- Iron is only a quarter stronger than scrap: an iron hull breaches at power 6, not 5.
- Building on a planet base slows: a misplaced scrap wall is six pickaxe blows to remove.
- The nailer (10) is useless against hulls; the railgun (140) takes four shots for a door.
- A built wall struck with a tool on a derelict now cracks first, as rock does.

## Deliberately not handled

- A charge on a planet base hits what is built in full, no friendly-fire guard: a guest's charge breaks a scrap wall or a
  machine there, its cost and contents dropping. Only the ship is protected; on a planet the charge is for digging.
- A player's claimed vessel is never "the ship": its box takes bursts in full.
- No per-material resistance to bursts: a wall's strength is the one number.
- Marks do not work on planets (`building.notHere`): a misplaced wall there has only the pickaxe, a task of its own.
- Salvage tools come with their own design; the numbers leave room for a cutter fast on metal.
