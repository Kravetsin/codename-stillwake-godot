# scanner (client): a wreck's hit points as numbers

The scanner is an item worn in the helmet's mount, as the lamp is ([the-scanner](../../../docs/decisions/2026-11-07-the-scanner.md)).
While this player wears it, a hologram over the wreck under the pointer shows what is left of its hit points. The sim knows
the item only as gear; the readout is the client's, read from the debris module's pieces.

## Files

| File | Holds |
| --- | --- |
| `logic/Scan.cs` | `Readout` (a point, a value left, a maximum); `Scan.Of`, `OfPile`: the wreck or pile under the pointer and its hit points left |
| `logic/Phosphor.cs` | the picked readout's 3 by 5 digits, its box over the point with room for the maximum's digits, its whole screen pixels |
| `ScannerView.cs` | the readout over the wreck's top edge: dark glass, its digits added, its own scanlines; in and out as the pointer comes and goes |

## Rules

- **Only while worn** by this player (`Items.Equipped(own, Helmet.Scanner)`); a friend's scanner shows nothing here.
- **Over a pile** in the hold, failing a wreck (`Scan.OfPile`): its `Hp` less its crack, over the middle of its top
  (2026-11-07-pile-hit-points); a pile's readout id is `-1 - cell`, a wreck's its piece's.
- **Over a wreck**, by the box the hands aim by (`game/shared/logic/WreckAim.cs`: its picture and half a cell), the
  first in spawn order where wrecks overlap. Nothing over a small piece, empty space, or while the hero's pointer is off (a
  menu, a computer or the tree open).
- **`left / max`**: the kind's `Wreckage.HpOf` less the piece's `Damage`, never below 0; at the middle of the wreck's
  picture's top edge where it is drawn this frame (`PieceDrawn`), so it drifts with it.
- **Phosphor**, as picked, steadied (2026-11-07-steady-scanner): green digits on dark glass, the value flaring white as it falls,
  no flicker or grain. On a layer that follows the camera, unlit by its lights, each pixel whole screen pixels (`Phosphor.Scale`).
- **Worn**, the crest along the helmet's crown, its dots running down its glass: `crew`'s `Figure` draws it from
  `tools/anime-art/subjects/scanner.js`'s pictures, placed by the generated `game/shared/logic/ScannerInk.cs`. Others see it too.

## Deliberately not handled

- Creatures: `Readout` takes any point, left and maximum; nothing reads them yet.
- A hit in flight: the number drops on the tick the hit applies.
