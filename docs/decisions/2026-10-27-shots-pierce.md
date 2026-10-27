# Shots pierce what they break

- Status: accepted; built: the nailer. Amends [shots are the sim's](2026-10-26-shots-are-the-sims.md): a hit no longer always ends a shot, and the host draws its own shots as the sim's
- Date: 2026-10-27

## Context

A nail that broke a wreck was drawn flying on through it and doing nothing. The sim's shot did stop and hit. The shooter's
own copy (`GunHand`) was stepped after the world's tick, so on the host, whose `Fire` applies on the next tick, the copy
and the sim's shot reached the wreck on the same tick, and the copy found it already gone. The same went for a creature the
nail killed and a block it broke.

The player's word: a shot that breaks what it hits flies on and hurts the next thing, its damage cut by the same share at
each, "one logic for the nailer and the railgun, only the numbers different": the nailer keeps half, the railgun to come
about 65–70% and passes everything it hits. And the cards of guns and weapons show their damage to bodies and to blocks.

## Decision

- **A gun's keep.** `Gun.PierceKeep`, thousandths (0..1000): after each thing a shot breaks or kills, both its damages, to
  bodies and to blocks, are times the keep, rounded down (`Content.Shots.DamageOf(gun, projectile, pierced)`). The nailer
  keeps 500: 6, 3, 1, then nothing; its block damage 1, then 0. No gun (a creature's), no fold. The keep is the gun's, as its
  speed and reach are: the same nail from the railgun flies otherwise.
- **What pierces:** what the hit breaks or kills, a wreck broken, a creature killed, a cell broken. Debris's and Building's
  `Hit` and Creatures' hit path say whether it broke or died; `IDebrisQueries.Breaks` and the hit path's own check are what
  they decide by.
- **The walk.** A cell the shot's walk asks holds its targets in turn: solid, then the lowest-id live creature, then the
  first wreck still out. Each is hit with what the shot has kept after `Shot.Pierced`; one that breaks or dies, the shot
  counts it and looks again in the same cell, and with nothing left there walks on in the same tick, its point and arc as if
  nothing had been there. A hit that does not break ends the shot there, `Met`. One it has nothing left for ends it too,
  the op not called (no blow stamped, no knock, no draw from the Rng). A knock is the gun's whole, every hit.
- **Hits are told.** `CreaturesModule.Hits`, derived, lists every hit of the tick, `ShotHit { Shot, By, Projectile, Gun,
  Cell, From, What, Broke }`, the last of an ended shot's too and one of nothing left; the client shows hits by these, by
  what the sim met, not by the grid after the tick, where what broke is gone.
- **The host draws its own shots as the sim's** and flies no copy (`ShotDraws`); its fire is heard at the click. A guest
  flies its copy a round trip ahead, as before, by `CreaturesModule.StepOf(motion, projectile, gun, passed)`: the sim's walk
  judged by what each hit would do (`Breaks`, the hit path's check) and changing nothing; it skips what it has predicted
  broken (`ShotPass`), a wreck being several cells, and stops at what is solid.
- **Saved:** `Shot.Pierced`, 0 not written: an old save loads as shots that passed nothing. No format bump.
- **Cards:** a weapon's tiles gain BLOCK, its block damage (with its modules); a gun's are DMG and BLOCK with its first
  ammo, RELOAD and PIERCE (its keep, "50%"); its ammo's are what it adds, "+2" and "+0". Tiles past three wrap in rows as
  even as they come.

## Deliberately not handled

- **A guest's copy at a block it would break** stops there; the sim's shot flies on, seen by the guest only in what breaks.
- **A guest's prediction is wrong** when another peer's blow lands first: cosmetic, as before.
- **A frame that runs two ticks** shows only the last tick's hits, as with `Ended`; a joiner sees no hits of shots fired
  before it came.
- **Pierce kind "everything"** and the memory of a living body passed through (the railgun's) come with the railgun, with
  their tests: no gun uses them yet.

## Rejected

- **Replaying the sim's hits in the host's copy:** a second judge for one peer, where drawing the sim's shot is the truth.
- **The damage left after a kill flying on** (6 into 2 left, 4 on): the player chose one share per pierce, the same logic
  for every gun.
- **A test gun in content** to cover "pierce everything" now: content holds what the game has.
