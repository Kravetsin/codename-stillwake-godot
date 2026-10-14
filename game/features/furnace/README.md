# furnace (client)

A furnace at work without a word (`apps/game/src/features/furnace/`, G-08): fire in its mouth and
embers rising from it while it burns, neither while it is out, for want of fuel, air or room. Drawn
only: the sim (`sim/Furnace`) decides whether it burns.

- **Reads:** `IGridQueries` — `Machines` (furnaces and their cells), `Width`; `FurnaceModule.At(id)`
  — `Burning`, read every frame, as TypeScript did.
- **Fire:** a `Sprite2D` per burning furnace at `FireAt` (14, 17) in its picture, the inside of its
  mouth (TypeScript's `FIRE_AT`), unlit for it gives light, showing `art/fire_0..2.png` (exported from
  `features/furnace/art.ts`) in turn every 110 ms, never the same picture twice running.
- **Embers:** 7 a second from the middle of a burning furnace, a little to either side, owed between
  frames (`Embers.cs`: TypeScript's `EFFECTS.ember` and its particle step, a speck with an additive
  halo, flickering as it fades).
- **Light:** `LightView.Burns` — a burning furnace's fire lights its room in `Light.Fire` from the
  middle of its bottom row, a shadowed light like a lamp's; `Main` answers it from `At(id).Burning`.
- **Recording:** `-- --furnace` starts in the wreck with a furnace in room A, by the start, holding
  scrap chunks and planks, played on until it burns (`FurnaceDemo.cs`, the items put in through the
  save).

## Files

| File             | Holds                                                    |
| ---------------- | -------------------------------------------------------- |
| `FurnaceView.cs` | the layer: fires and where embers come from              |
| `Embers.cs`      | the embers: emitted, moved, aged and drawn              |
| `FurnaceDemo.cs` | the world of `--furnace`                                 |

## Deliberately not handled

- The furnace's panel (its slots, progress and why it stands): with the items feature's panels.
- Embers as a light of their own (TypeScript's glowing particles in the light scene): with the
  effects feature, which takes embers over with dust, sparks and air.
- `dev/furnace-fire.test.ts`, which kept `FIRE_AT` in step with the furnace's ASCII picture: the
  pictures are PNGs here, checked by eye in a recording.
