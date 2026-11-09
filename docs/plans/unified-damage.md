# Unified damage

## Goal

One rule decides what any hit breaks, so a player who learns it with a pickaxe can predict what a charge does to a derelict
and what a meteor does to the hull. Users now: tools (`Hew`, `HewCells`), shots (nailer, railgun), the blasting charge,
meteors. Next: a meteor shower event, a station that overheats and blows up.

## Rules

1. A hit has damage. It breaks a cell whose remaining strength (strength minus its crack) it covers; the surplus goes on to
   the cell behind, times the hitter's pierce (percent). Short of that, it adds to the cell's crack.
2. A burst (the charge, a meteor) is the same hit in every direction, through the shared `Wave`: a step costs falloff, a
   wall costs its remaining strength. A wall the wave reaches but cannot pay for takes what was left as a crack.
3. Built cells (walls not `Natural`, doors, ladders, hatches, floors) take a burst's damage times `Damage.BuiltTakes`, so
   one charge only cracks a scrap wall. Tools and shots hit them in full: a planet has no marks, and a pickaxe is how your
   own wall comes down there.
4. Unbreakable stops every hit: bedrock, lava, fog, `Fixed`.
5. What breaks goes: by a tool or shot, to the hitter (as today); by a charge, dropped at its cell for anyone (today only
   ore drops), crushed where the material says so: stone gives 1 gravel instead of 1 rock (a crusher gives 2 and a chance
   of quartz, so the charge is quick but wasteful, and a stone wall still wants a pickaxe's rock); by a meteor, lost.
6. Cracks heal `Builds.CrackHeals` ticks after their last hit, now longer. No repair mechanic.
7. Where tools hit does not change: `ToolsBreak` surfaces, a vessel's box. On the ship tools still hit only piles; the
   plan takes walls apart.
8. Machines break too: at their hit points their cost, slots and fitted modules drop at their cell, queued crafts are
   refunded, cables come unplugged. The lifepod stays `Fixed`. Built machines take bursts times `BuiltTakes`, as walls do.
9. A relic a burst takes from its cradle shocks whoever threw it by `CradleShock`, wherever they stand; a warden whose
   post breaks stands down, as today. Alloy's strength makes the spire cost 2–3 charges.

## How it works

Building owns damage: it already owns cracks, `Hit`, `Blast` and what breaking gives. No new module, no new command.

- One strength scale: `Material.Armour` goes; tools, shots and bursts all read `Strength`. Materials are content, and
  removing it saves nothing; the tenths step below bumps the format once. The wall card loses its meteor number.
- `IBuildingOps.Hit` returns the surplus (0 when it only cracked); shots multiply it by their pierce and fly on.
- `HewCells` (what the client's tool edge sends) and `Hew` carry each broken cell's surplus one cell further along the line
  from the hero through it, once per command: a cell takes the largest carry, after all primary hits, in ascending order.
  `HewCells` learns the hero's position from what the sim already knows of it, not from a new command field.
- `IBuildingOps.Burst(peer, cell, power, reach, spoils)` replaces `Blast` and owns the one wave-entry rule that Creatures
  and Hazards copy today. It returns the broken cells so Hazards still drops the nets hung on them. Overheating later is a
  `Burst` at the machine's own cell.
- `Wave` reports a cell it reached but could not pay for. That cell cracks once, by what was left on the cheapest path to
  it, never once per neighbour that asked. A built piece cracks at its head cell only, as `HitPiece` does.
- Pierce per weapon in Content, 0 where it would show nothing. `Material` gains what a burst crushes it to (stone: gravel).
- Machines get hit points per kind in Content and a crack on their head cell; breaking one is an Items op that drops its
  slots and modules and a Crafting refund of its queue, then the existing removal that unplugs cables.
- Determinism: integers. Carry and the built cut round down; entering a built cell rounds up.

## Numbers to start from (sim/Content)

- In tenths: soil 40, stone 120, alloy 600, scrap 20, iron/hull 50. Pickaxe about 70 so stone leaves a surplus, pierce
  50 %; railgun pierce 65 % with block damage raised so soil leaves a surplus; others 0.
- The charge's power and step are retuned on the one scale: stone opens about 4 out, soil the whole circle of 8, alloy only
  cracks; a second charge breaks alloy's first layer.
- `BuiltTakes` in permille, tuned so one charge cracks scrap and a second breaks it (about 37); an iron hull takes 4–5.
- `CrackHeals` 200 → 1200 ticks.
- Meteors: power retuned to the one scale and spread, so about one in four breaches scrap alone; their interval,
  warning and the shower event stay out of this change.
- Strengths and block damages move to tenths (×10), with a save step for cracks: at whole units the carry, the built
  cut and machine hit points have no room (the plan's findings).

## Player sequences

1. Pickaxe into soil: each swing breaks the front cell and cracks the one behind, so digging straight takes fewer swings.
2. Charge at a stone face: a round crater, its rim cracked; gravel and ore lie in it; a second charge goes further.
3. Charge at a spire: a ring of deep cracks; a second within the heal time opens the first layer.
4. Charge at a derelict's hull or your own scrap wall: cracks; two in a row break it.
5. Railgun into soil: two cells break, the third cracks.
6. A meteor on the hull: most leave a crack with a weak flash; the largest, or a second on a cracked patch, breach it,
   alarm as today.
7. Charge by a furnace full of ore: cracks on it; a second charge breaks it, and its cost, ore and modules lie on the floor.
8. Three charges into the spire from outside: the hall opens, the relic falls, and the thrower's suit takes the shock.

## Deliberate omissions

- Bursts do not harm heroes or creatures (as today); damage to creatures and wrecks is unchanged.
- Back walls as today: meteors break them, tools and the charge do not.
- Tools aboard the ship as they are; the plan's marks take walls apart.
- No repair of cracks, no faster healing.
- No meteor shower, no overheating yet; overheating is a `Burst` at the machine's cell and rule 8 already spills it.
- No anti-griefing: a guest's charges crack the host's hull.
- `MeteorHit` (dev command) still breaks the wall at once.

## Risks

- Hull meteors change character: today one tears 7–17 scrap cells; after, most crack and the largest breach. Rarer until the
  shower event comes.
- A crater is a mine: dozens of gravel and soil stacks drop in one tick.
- On a derelict's hull one pickaxe swing breaks a cell while a charge needs two (rule 3).
- The railgun's surplus carry replaces the share-per-pierce rule of `2026-10-27-shots-pierce`; shots into rock stop sooner.
- "The charge mines" reverses `2026-11-08-blasting-charge` (a crater went to dust); a new ADR supersedes both.
