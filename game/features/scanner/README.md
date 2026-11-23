# scanner (client): a wreck's hit points as numbers

The scanner is a form of the light drone a player paired with ([the-scanner](../../../docs/decisions/2026-11-07-the-scanner.md), drone-companion),
learned from ten wreck pieces broken ([drone-lessons](../../../docs/decisions/2026-11-21-drone-lessons.md)). While this player scans, a row of
ember dots over the wreck under the pointer shows what is left of its hit points. The sim knows
the item only as gear; the readout is the client's, read from the debris module's pieces.

## Files

| File | Holds |
| --- | --- |
| `logic/Scan.cs` | `Readout` (a point, a value left, a maximum); `Scan.Of`, `OfPile`: the wreck or pile under the pointer and its hit points left |
| `logic/ScanDots.cs` | the gauge: 26 ember dots lit by the hit points left, each the spark of a mark on the drone's reticle, out to the row and home to its mark, a hit's shudder and burst embers |
| `ScannerView.cs` | the gauge over the wreck's top edge, drawn, telling the drone's reticle where its dots are |

## Rules

- **Only while scanning**: this player's paired light drone in its scanner mode (`HeroView.Scanning`, light-drone); a friend's
  scan shows nothing here.
- **Over a pile** in the hold, failing a wreck (`Scan.OfPile`): its `Hp` less its crack, over the middle of its top
  (2026-11-07-pile-hit-points); a pile's readout id is `-1 - cell`, a wreck's its piece's.
- **Over a wreck**, by the box the hands aim by (`game/shared/logic/WreckAim.cs`: its picture and half a cell), the
  first in spawn order where wrecks overlap. Nothing over a small piece, empty space, or while the hero's pointer is off (a
  menu, a computer or the tree open).
- **`left / max`**: the kind's `Wreckage.HpOf` less the piece's `Damage`, never below 0; at the middle of the wreck's
  picture's top edge where it is drawn this frame (`PieceDrawn`), so it drifts with it.
- **Ember dots** (drone-threads, [scan-form](../../../docs/decisions/2026-11-21-scan-form.md)), burning into this player's colour
  ([drone-colour](../../../docs/decisions/2026-11-22-drone-colour.md)): 26 over the wreck, dot j lit
  `clamp(left / (max / 26) - j, 0, 1)` (`ScanDots`). Each is the spark of one of the reticle's 26 marks: it leaves its mark for the
  row, hops from wreck to wreck without coming back, and comes home when the pointer leaves, its mark dim while it is out (with no
  drone the dots come and go in place); lost ones burst off as falling embers; red with the drone's alarm; none at `max <= 0`. On a layer that
  follows the camera, unlit by its lights, whole screen pixels (`Phosphor.Scale`).
- **Worn**, the crest along the helmet's crown, its dots running down its glass: `crew`'s `Figure` draws it from
  `tools/anime-art/subjects/scanner.js`'s pictures, placed by the generated `game/shared/logic/ScannerInk.cs`. Others see it too.

## Deliberately not handled

- Creatures: `Readout` takes any point, left and maximum; nothing reads them yet.
- A hit in flight: the number drops on the tick the hit applies.
