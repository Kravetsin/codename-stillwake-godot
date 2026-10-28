# hazards (client)

## Files

| File | Purpose |
| --- | --- |
| `MeteorView.cs` | A meteor flying in and striking the hull: the rock and its trail, then the blast's ring, chunks, dust and sound. |

Meteors seen ([hull-meteors](../../../docs/decisions/2026-10-28-hull-meteors.md),
[meteor-blast](../../../docs/decisions/2026-10-28-meteor-blast.md), [meteor-wave](../../../docs/decisions/2026-10-29-meteor-wave.md)): drawn only, from the sim's `HazardsModule.Incoming`, read
once a tick. Made only where the surface's kind has a shower (`SurfaceViews`).

## Rules

- **Through its warning** a rock flies in from up the drift's side (`Reach`, `Drop` cells off) onto the wall's face toward
  the drift, nearing as the ticks run out, the frame's progress between them. Its radius grows with power; drawn unshaded at
  `Layers.Effects`, its front white-hot over the rock's grey. A trail as long as its power, a bloom and a held light at the
  head growing as it nears, embers and smoke shed behind with no gravity, on the effects stage.
- **Each tick it is coming** the walls and back walls within its blast's bound (`HazardsModule.Farthest`) are noted; those
  gone at the strike are what it broke.
- **The strike**, as the meteor leaves the sim: the wreck's bite (`HitLooks.Of[Impact.Wreck]`) thrown back toward the drift
  out of the face. Out of every wall gone, hot chunks cooling as they fly on its way, and dust, a chunk and dust out of each
  back wall gone; a shock ring as wide as the farthest gone and a flash. Heard on World where it struck: the wreck's break
  (`Sound.Break`) if any went, its chop (`Sound.Chop`) if the wall held, the rock shattering on it: glowing shards flung back
  the way it came, a glint, a small ring and a flash.
- `-- --meteors` plays the new game's wreck with its first meteor seen coming at once; `--meteors=N` gives it power N (1
  shows a held hit on scrap).

## Deliberately not handled

- No warning but the rock: no sound before the strike, no mark on the wall it will hit.
- Chunks fly through walls, and are looks only: nothing thrown lands as items.
