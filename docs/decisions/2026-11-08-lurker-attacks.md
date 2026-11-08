# Lurker attacks

- Status: accepted; supersedes the lurkers' attacks of [cave-hostiles](2026-11-07-cave-hostiles.md); the spit and its looks' layering superseded by [lobbed-glob](2026-11-08-lobbed-glob.md)
- Date: 2026-11-08

## Context

Played, both cave lurkers were pushers: the capback pounced, the geode snail rolled a steady lunge at 24 cells a second, past
the hero and back every two seconds, and killed almost at once. Nothing in the caves asked the player to read space or timing,
and a gun killed either unanswered. The user asked for a capback that gasses a hero about to step on it and spits at one gone
far, and a snail that can be hurt only out of its shell, which it leaves only for a hero close by.

## Decision

- **Patches**, a shared mechanic of `Creatures`: a box of cells a creature lays, kept its kind's `Lasts` (`PatchKind`), saved
  beside the arcs. Every breath each awake hero whose reported cell is in one, with nothing solid on the line from its middle,
  pays its `Damage` through `ICrewOps.Shock`, as lava does: no cape share. Standing in two pays both. The module's clock runs
  while one lies, though its creature is gone. Users: the capback's gas cloud (9×6, 5 s) and its slime's puddle (4×4, 10 s), 2 a
  breath each.
- **The capback vents** (`Vent`): a hero within 4 cells, in line, and it lays its gas round its middle at once, no wind-up, then
  stands venting half a second, no blow cutting it; not again for 10 s. A hero walking in is only watched.
- **Provoked, it spits.** Answering (a vent, a blow) provokes a guard with `Provokes` for that long (30 s); provoked, it spits at
  a hero from the shot's `From` (8 cells) to its `Reach` (20): the sentry's `Shoot`, its arc's end dropped to the floor as a
  puddle (`Leaves`). The glob itself hurts no one.
- **The snail's shell** (`Shell`, a drive after `Guard`): a reflex counting `Hold` (1 s) after a hero within `Reach` (4 cells).
  The hit path asks every drive first (`DriveRule.Shields`): shut, a hit does nothing, no knock, no answer; open, a shot still
  glances. Only melee kills it. Open, it bites: a short steady `Lunge` (4 cells, 3 units). The roll is gone.
- **One save step** (format 55): the capback's first move is a vent where it was a pounce, so a capback mid-act stops. The
  snail's shell comes after its guard, so its saved places keep their meaning.
- **The caps' shudder** eases to rest out of a wind-up: a cut wind-up or a blow had left it shaking for good.
- **The looks and sounds, picked by the user** on their page (2026-11-08): the gas «Клубы со спорами», eight ink-lined
  yellow-green puffs thrown from the caps with 22 lit spores in them, each on its own slow loop, breathing in brightness (the
  user asked for the puffs with «Спорень»'s spores, moved smoothly, not swapped frame by frame); the slime «Споровая», a violet
  glob with two drops and a teal-glowing puddle with spores rising off it. The sounds: a valve's hiss (vent and spit), a slime's
  splat (landing), a pick on rock for a shut shell struck. Drawn by `tools/anime-art/subjects/lurkerattacks.js`, placed by
  `LurkerFx`. The sim lays the puddle as it strikes the arc, so the client flies the glob first and shows the puddle on landing.

## Consequences

- A sniper beyond 20 cells is unanswered by a capback; one that never struck it is spat at once it is provoked.
- A hero dashing through a cloud between breaths pays nothing; a cell lags about a second away from creatures, so a puddle
  hurts late, as lava does.
- Gas is not air: it stays in its box, ignores rooms and doors, hurts no creature and slows nobody.
- A gunner cannot help with a snail; two can axe it.
