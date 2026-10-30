# hazards (client)

## Files

| File | Purpose |
| --- | --- |
| `AuroraView.cs` | A magnetic flare coming and striking: the aurora over the stars, then the white flash over the world. |
| `MeteorView.cs` | A meteor flying in and striking the hull: the rock and its trail, then the blast's ring, chunks, dust and sound. |
| `logic/FlareLook.cs` | How a flare is seen, as numbers: its warning's run, the lamps' share, the cables' glow, the flash. |

Meteors seen ([hull-meteors](../../../docs/decisions/2026-10-28-hull-meteors.md),
[meteor-blast](../../../docs/decisions/2026-10-28-meteor-blast.md), [meteor-wave](../../../docs/decisions/2026-10-29-meteor-wave.md)): drawn only, from the sim's `HazardsModule.Incoming`, read
once a tick. Made only where the surface's kind has a shower (`SurfaceViews`). Magnetic flares seen
([magnetic-flare](../../../docs/decisions/2026-10-30-magnetic-flare.md)): from `FlareComing` and `FlareLeft`, on every
surface, for the dev panel strikes one anywhere.

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
- **A flare's warning** (`FlareLook.Near`, 0 to 1): an aurora's two curtains of rays over the stars on their own canvas
  layer, under the world, so seen only where it is open to space; green along the hem, violet up the folds, swaying at a
  third of the shader's pace. It comes in from nothing, eased, over the first 40% of the warning, and grows on to whole at
  the strike (`FlareLook.Aurora`). The lamps falter (`FlareLook.LampShare`, through `LightView`'s flicker, set in `Main`)
  and the cables glow (`PowerView`).
- **A flare's strike**, as its warning ends: a pale white flash over the world (`ScreenLayers.FlareFlash`), eased up in
  `FlashRiseMs` and out by `FlashMs`; the curtains ease out over `AuroraFadeMs`. Each machine it broke (new in
  `IPowerQueries.Broken` that tick) bursts where it stands, the bursts spread over 0.6 s: the resonator's crackle
  (`Sound.Zap`) pitched up, a lamp's glass (`Sound.GlassBreak`) high and soft with it, both files reused, no new sound.
  What it broke is shown by `power` (dimmed, sparking).
- `-- --flare` plays the new game's wreck with a flare's warning begun at once.

## Deliberately not handled

- No warning but the rock: no sound before the strike, no mark on the wall it will hit.
- Chunks fly through walls, and are looks only: nothing thrown lands as items.
- A flare is silent through its warning; a client joining between its strike and the next frame misses the flash and bursts.
