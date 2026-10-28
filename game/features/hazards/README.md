# hazards (client)

## Files

| File | Purpose |
| --- | --- |
| `MeteorView.cs` | A meteor seen coming and striking the hull: its streak, then sparks, dust and its sound. |

Meteors seen ([hull-meteors](../../../docs/decisions/2026-10-28-hull-meteors.md)): drawn only, from the sim's
`HazardsModule.Incoming`, read once a tick. Made only where the surface's kind has a shower (`SurfaceViews`).

## Rules

- **Through its warning** a white-hot head flies in from far up the drift's side (`Reach`, `Drop` cells off) onto the
  wall's face toward the drift, nearing as the ticks run out, the frame's progress between them: a trail behind it, a
  bloom and a held light at the head growing as it nears, embers shed with no gravity. On the effects stage.
- **The strike**, as the meteor leaves the sim: the wreck's bite (`HitLooks.Of[Impact.Wreck]`) thrown back toward the
  drift out of the face; a broken wall's dust too. Heard on World where it struck: the wreck's break (`Sound.Break`) if
  the wall went, its chop (`Sound.Chop`) if it held.
- `-- --meteors` plays the new game's wreck with its first meteor due at once.

## Deliberately not handled

- No warning but the streak: no sound before the strike, no mark on the wall it will hit.
