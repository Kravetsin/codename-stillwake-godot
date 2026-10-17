# furnace (client)

Fires at work without a word (`apps/game/src/features/furnace/`, G-08): fire in the mouth of a
furnace or a burner generator and embers rising from it while it burns, and a generator's flywheel
spinning; none of it while it is out, for want of fuel, air or room. Drawn only: the sim
(`sim/Furnace`, which keeps both machines' fires) decides whether it burns.

- **Reads:** `IGridQueries` — `Machines` (furnaces, generators and their cells), `Width`;
  `FurnaceModule.At(id)` — `Burning`, read every frame, as TypeScript did.
- **Fire:** a `Sprite2D` per burning machine at `FireAt(kind)` in its picture, the inside of its mouth
  — the furnace's (14, 17) (TypeScript's `FIRE_AT`), the generator's (4, 16), each mouth drawn with the
  furnace's grate to fit — unlit for it gives light, showing `art/fire_0..2.png` (exported from
  `features/furnace/art.ts`) in turn every 110 ms, never the same picture twice running.
- **Embers:** 7 a second from the top of the fire in its mouth (5, 2 from the fire's corner, 3 px to
  either side), owed between frames, emitted into `Particles.Shared` as `Effect.Ember` and drawn and
  lit by the effects feature.
- **Crackle:** with each new picture its fire draws how brightly it burns (`Flames.Blaze`): from
  `Flames.Low` to whole, held till the next picture, or one picture in ten a coal cracks: it flares
  to `Flames.Crack` and throws up `Flames.CrackEmbers` embers at once. `BlazeOf(id)` answers it, 0
  while out. The fire's sound, `Sound.Fire`, loops with each fire, begun somewhere along the loop.
- **Flywheel:** over a burning generator's picture, lit as the machine is, `art/burner_wheel_0..3.png`
  in turn every 55 ms, each a twelfth of a turn on (its three spokes repeat every third): a turn and a
  half a second, each generator a picture off by its id. Out, the wheel at rest in its picture shows.
- **Light:** `LightView.Fire` and `LightView.FireAt` — a burning fire lights round its mouth in
  `Light.Fire`, a shadowed light like a lamp's, and glows: the furnace's from the middle of its bottom
  row, the generator's from low in its mouth (`FurnaceView.LightAt`). `Main` answers both, adding this
  view before the light's so the light follows the picture of the same frame.
- **Recording:** `-- --furnace` starts in the wreck with a furnace in room A, by the start, holding
  scrap chunks and planks, played on until it burns (`FurnaceDemo.cs`, the items put in through the
  save). `-- --testbed` has a burner generator lit in room two, powering the lamp over it and the
  airlock's.

## Files

| File             | Holds                                                         |
| ---------------- | ------------------------------------------------------------- |
| `FurnaceView.cs` | the layer: fires, flywheels, where embers come from and light |
| `Flames.cs`      | how brightly a fire burns with each picture, and cracks       |
| `FurnaceDemo.cs` | the world of `--furnace`                                      |

## Art

`art/machine_burnerGenerator.png` and `art/burner_wheel_*.png` were drawn as ASCII in the furnace's
scrap palette (ship-art.ts) and exported, as the other machines were: a squat box of scrap, its mouth
the furnace's grate, a stovepipe, a dynamo plate with a window onto its copper windings, a flywheel
on it, its socket under the window.

## Deliberately not handled

- The panels (slots, progress and why it stands): with the items feature's panels.
- A furnace too cold for what its input holds (silicon): no fire lights; the crafting feature draws the
  cold mark in its mouth, as on every fed station (0057).
- A hum of the generator's own: a new sound goes through the user's audition first; it crackles as the
  furnace does.
- The flywheel stops dead when the fire goes out, back at its picture's rest.
- `dev/furnace-fire.test.ts`, which kept `FIRE_AT` in step with the furnace's ASCII picture: the
  pictures are PNGs here, checked by eye in a recording.
