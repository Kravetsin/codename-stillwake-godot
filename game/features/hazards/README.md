# hazards (client)

## Files

| File | Purpose |
| --- | --- |
| `AuroraView.cs` | A magnetic flare coming and striking: the aurora over the stars, then the white flash over the world. |
| `BoltView.cs` | A storm's bolt: fires over the zone and its helmets, the leader and strokes, its light and shade, sparks, soot, thunder, the burn. |
| `MeteorView.cs` | A meteor flying in and striking the hull: the rock and its trail, then the blast's ring, chunks, dust and sound. |
| `logic/BoltLook.cs` | How a bolt is seen, as numbers: the channel and forks, the leader's steps, the strokes, the light, fires, the burn. |
| `logic/FlareLook.cs` | How a flare is seen, as numbers: its warning's run, the lamps' share, the cables' glow, the flash. |
| `logic/MeteorWarn.cs` | Where a strike warns: the aired, sealed room behind the wall struck, if any. |

Meteors seen ([hull-meteors](../../../docs/decisions/2026-10-28-hull-meteors.md),
[meteor-blast](../../../docs/decisions/2026-10-28-meteor-blast.md), [meteor-wave](../../../docs/decisions/2026-10-29-meteor-wave.md)): drawn only, from the sim's `HazardsModule.Incoming`, read
once a tick. Made only where the surface's kind has a shower (`SurfaceViews`). Magnetic flares seen
([magnetic-flare](../../../docs/decisions/2026-10-30-magnetic-flare.md)): from `FlareComing` and `FlareLeft`, on every
surface, for the dev panel strikes one anywhere. Lightning seen ([storm-lightning](../../../docs/decisions/2026-11-16-storm-lightning.md)): from `Strike`,
made where a stretch of the surface's kind has a bolt.

## Rules

- **Through its warning** a rock flies in from up the drift's side (`Reach`, `Drop` cells off) onto the wall's face toward
  the drift, nearing as the ticks run out, the frame's progress between them. Its radius grows with power; drawn unshaded at
  `Layers.Effects`, its front white-hot over the rock's grey. A trail as long as its power, a bloom and a held light at the
  head growing as it nears, embers and smoke shed behind with no gravity, on the effects stage.
- **Each tick it is coming** the walls and back walls within the shower's `Reach` are noted; those
  gone at the strike are what it broke.
- **The strike**, as the meteor leaves the sim: the wreck's bite (`HitLooks.Of[Impact.Wreck]`) thrown back toward the drift
  out of the face. Out of every wall gone, hot chunks cooling as they fly on its way, and dust, a chunk and dust out of each
  back wall gone; a shock ring as wide as the farthest gone and a flash. Heard on World where it struck: the wreck's break
  (`Sound.Break`) if any went, its chop (`Sound.Chop`) if the wall held, the rock shattering on it: glowing shards flung back
  the way it came, a glint, a small ring and a flash. A room behind the wall, aired and still sealed (`MeteorWarn`), flashes the
  breach alarm's red (`Fx.Alarm`) once, weak: a crack seen before a breach (unified-damage).
- `-- --meteors` plays the new game's wreck with its first meteor seen coming at once; `--meteors=N` gives it power N (2
  cracks scrap) and, ten seconds after it strikes, a second of that power at the row's face, no rock seen.
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
- `-- --flare` plays the new game's wreck with a flare's warning begun at once; `--flare=repair` with its lamps and generator
  broken, a lamp half repaired.
- **A bolt glowing** (`Strike`): St Elmo's fires on the zone's ground cells (each column's top wall under open sky within
  `Spread`), more lit and stronger as the glow grows, and on the helmet of every hero in the zone (`BoltLook.HelmetFire`); a
  faint violet light. In its last `Leader` seconds the leader steps down from above the farthest view (`ShipCamera.Farthest`),
  so it is never short.
- **The strike**, when the strike has gone after a glow this view saw: the return stroke climbs the channel, three strokes
  (`Strokes`), forks in the first; the channel's light (`Flares.Hold`) and a blue shade over the rest of the screen
  (`ScreenLayers.BoltShade`); thunder (`Sound.Thunder`), sparks and dark grains, arcs over the zone, soot streaked downwind
  (kept while the surface is shown). The shock is `Harm.Lightning`: no blue hurt wash (`HurtLook.Washes`).
- **The burn**, for the local hero in the zone under the open sky, creative or not: the channel burnt into the screen
  (`ScreenLayers.BoltBurn`), held about 5 s, gone by `BurnDone`, and a white edge flash with arcs (`EdgeFlash`).
- `-- --bolt` plays a planet in creative with a bolt every 5 s; the dev panel's «bolt» sends `BoltNow`.

## Deliberately not handled

- No warning but the rock: no sound before the strike, no mark on the wall it will hit.
- Chunks fly through walls, and are looks only: nothing thrown lands as items.
- A flare is silent through its warning; a client joining between its strike and the next frame misses the flash and bursts.
- Soot is lost when the surface is left or loaded; a client joining between a strike and its next frame misses it.
