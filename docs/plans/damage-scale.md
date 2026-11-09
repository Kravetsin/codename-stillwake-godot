# Damage scale

## Goal

Unified damage (2026-11-09-unified-damage) gave every hit one rule but kept strengths tuned for digging ground with a pickaxe.
Built walls were never meant to be hit: scrap 20 and iron 50 go at one blow of anything, so a derelict's hull melts in
seconds, and a charge off the ship opens iron nine cells deep. Its other half, a share of 1/27 for anything not natural,
protects the wrong thing: a player's alloy wall counts as ground and is weaker aboard than iron. Users now: tools and shots on
derelicts and planets, the charge, meteors on the ship. Next: salvage tools (cutter, lance, grinder), the meteor shower event,
a station that overheats.

## Rules

1. **Strengths are set by blows**, the pickaxe's 75 a blow the measure, anchored on alloy as it is (600, 8 blows): it stays
   breakable by hand, so no player is stuck before a wall. Built walls sit just under it.
2. **A player hits their own ship at a tenth**, not by material: a burst a player set off (a charge, its `peer` a hero, not
   `Nobody`) does a tenth of its damage (`Damage.OwnShipTakes` 100 ‰) to any cell of the ship: where tools do not break
   (`!ToolsBreak`) and outside a vessel's box, exactly where a pickaxe does nothing today. Everything else is in full:
   meteors and later hazards aboard, any burst on planets and derelicts, built or natural alike. `Natural` no longer
   decides damage.
3. **Meteors** keep their character on the stronger hull, in full: their power per point is raised so the same power still
   cracks, holes or breaches.

## Numbers (sim/Content)

| What | Now | New | Pickaxe blows | Charge (500) off the ship |
| --- | --- | --- | --- | --- |
| soil | 40 | 40 | 1 | the whole circle |
| stone, plaster, veins | 100–120 | unchanged | 2 | about 4 out |
| scrap, wreck and cast scrap | 20 | 400 | 6 | the cell struck, its neighbours cracked |
| iron | 50 | 500 | 7 | the cell struck |
| alloy | 600 | 600 | 8 | cracks; a second breaks it |

The rest of what is built moves with the hull, or a power-2 meteor would take every window and door beside a crack: glass,
mica and hide 10 → 200; a machine (`Damage.MachineHp`, a scrap wall's) 20 → 400; a door or hatch segment (`Build.Strength`)
80 → 500; floor, ladder, platform 80 → 200. Plants, ruins' furniture and piles stay: they are tuned for tools already.

Aboard, a player's charge pays ten times these, a pile's `Hp` too: it cracks a scrap wall by 49 of its 400 and nine break it; a pile
(10–12) still goes to one.

Meteors (`PerPower` 170, reach 4) on a scrap hull two thick (400 a cell, 802 through):

| Power | Burst | Scrap hull |
| --- | --- | --- |
| 2 | 340 | cracks the outer cell by 340 of 400 |
| 3–4 | 510–680 | holes the outer cell, cracks the inner |
| 5–7 | 850–1190 | breaches, the face stripped a few cells up and down |

(802: 400 for the outer cell, 2 to step, 400 for the inner.) Iron (500 a cell) holes at 3, breaches at 6. A second meteor of power 3 or more on a fresh crack (60 left to pay, then 402
for the inner cell) still breaches.

Other tools fall in line: fire axe and drill (40) 10 blows on scrap, sledgehammer (60) 7, railgun (140) 3 shots, nailer (10)
40. Their tests that count hits on built walls (`DrillArcTests` and the like) are rewritten to the new counts.

## How it works

- `Bursts.cs`: the built share (`Built`, `BuiltTakes` 37) becomes the own-ship share: a cell's entry cost is its remaining
  strength, times 10 when the burst's `peer` is a hero and the cell is aboard; such an unpaid cell cracks by `left / 10`,
  rounded down. One test of place, `Aboard(cell)`, the same condition `Stops` already uses for tools, decided once a burst
  for the peer. `ToBreak(material)` goes: the dev `MeteorHit` (peer `Nobody`, in full) spends the wall's full strength at reach 0.
- Content only otherwise: `Materials`, `Build.Strength` per build, `Damage.MachineHp`, `Meteors.PerPower`. Bursts stay
  small (a charge 500, a meteor at most 1190), so `Wave`'s buckets do not grow.
- No save step: no strength drops, so a saved crack stays below its cell's strength and heals within a minute.

## Player sequences

1. A pickaxe on a derelict's scrap wall: six blows, the cell behind cracked by the carry; iron seven.
2. A charge on a derelict's hull: the cell struck goes, a ring of cracks round it; a second widens the hole.
3. A charge thrown aboard by mistake, or by a guest: cracks on the hull, nothing opens; nine in one place to break a wall.
   A meteor beside it is not softened.
4. A meteor on the hull: mostly a crack or a holed outer cell; the strongest breach, as now.
5. A pickaxe on the spire's alloy: eight blows a cell, slow but never stuck.
6. A player's own wall on a planet: six blows to take down, where it was one.

## Deliberately not handled

- Salvage tools: they come with their own design; the numbers leave room for a cutter fast on metal.
- A player's claimed vessel is never "the ship": its box takes bursts in full.
- A charge on a planet base hits what is built in full, no friendly-fire guard: a guest's charge breaks a scrap wall or a
  machine there (its cost and contents drop). Only the ship is protected; on a planet the charge is for digging.
- No per-material resistance to bursts: a wall's strength is the one number.
- A misplaced wall on a planet has only the pickaxe, six blows: marks do not work on planets (`building.notHere`), and making
  them work is a task of its own.
- Plants, ruins, piles and creature damage unchanged.

## Risks

- Building and digging on a planet base slows: a misplaced wall is six blows to remove.
- Iron is only a quarter stronger than scrap, so an iron hull is little safer from meteors (breach at 6, not 5).
- The nailer becomes useless against hulls; the railgun is the hull gun.
