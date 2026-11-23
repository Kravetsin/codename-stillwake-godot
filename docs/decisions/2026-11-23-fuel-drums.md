# Fuel drums, and every burst hurts every body

- Status: accepted
- Date: 2026-11-23

## Context

Space held debris to salvage and creatures to fight, but nothing in it was dangerous by itself, and nothing hit an area
but the blasting charge. The bursts kept old exceptions: a burst hurt no creature, and a hero's burst no hero
([blasting-charge](2026-11-08-blasting-charge.md), [companion-fights](2026-11-22-companion-fights.md)). The user asked
for drums to shoot into a pack, or to harpoon into a cluster of debris and salvage several pieces at once.

## Decision

- **A fuel drum** (`fuelDrum`) is a wreckage kind of the space debris: 4 hp, 4×4 cells, every weapon breaks it, loot one
  scrap chunk. It drifts in the ship's stream (weight 4) and the far field (4, 6, 8 by tier), never in the asteroid band, a
  stretch or on a planet.
- **Broken, it bursts with no fuse** where its middle was, by whoever broke it: that peer's own-ship tenth and kills. A hew
  bursts it that tick; a shot or another burst, the next tick, so a chain ripples a tick a link. Debris queues the burst
  (`PendingBurst`, saved, as a shot breaks one after the tick's drain); Creatures, which alone reaches walls, wrecks, heroes
  and creatures, sets it off.
- **Every burst hurts every body** (the user: «Взрывы наносят урон всему, с чем взаимодействуют»): every awake hero and
  live creature within its reach and in its sight takes the blast's `Body`, the one who set it off too («без исключений»).
  The charge 10, the catcher's shell 3, its missile 6, a drum 8. A body hit directly by a shot is not hurt again by its
  burst. The one damage rule stands: a player's burst takes a tenth off their own ship, everything else full.
- **The drum's burst** (`Blasting.Drum`): 300 to spend, so a scrap hull cell cracks and holds; 60 off each wreck within 6,
  past every stream wreck's hit points; 8 off a body, twice a breaker's 4. Under the charge's, as drums are free.
- **Its look** is the user's pick «Длинный баллон»: a long gas bottle, its burst drawn as a shot's.

## Deliberately not handled

- **No fuse:** an axe blow on a drum hurts the one who struck; drums are for shooting from range.
- **No falloff, no push:** flat damage in reach; nothing is knocked or moved by a burst.
- **No burst on impact:** nothing collides yet (0006); a reeled drum dragged into a wreck stays whole.
- **A pack round the hero** sits inside a drum's reach: blowing it up hurts the hero too. A drum one player reeled and
  another shot hurts both if near.
- **No alarm:** the drum's look is the warning.
- **No icon:** a drum is never in a bag, so it has no item picture; anywhere one is asked for, its debris picture stands in.
