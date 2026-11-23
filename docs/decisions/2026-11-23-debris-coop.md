# Debris physics in co-op: a feed addressed to the ship, guests ease corrections

- Status: accepted
- Date: 2026-11-23

## Context

Step 3 of `docs/plans/debris-physics.md`, closing the co-op gaps [debris-host-physics](2026-11-23-debris-host-physics.md)
left. Step 2 ran the bodies only for the surface the host player's hero stood on, and was
played alone. The user asked that the physics always run, the host on a planet too; played with a guest, the guest's pieces
jerked at every correction, small snaps that felt bad.

## Decision

- **A command addressed to a surface:** a command type marked `ISurfaceAddressed` (Core) is applied on the surface it carries,
  whatever surface its sender stands on, and is never refused `world.elsewhere`; one naming a surface not open is our bug and
  throws. `Client.Send` keeps the surface it carries and `Client.InFlight` keeps it when the sender moves. Only `Moved` is so
  marked, addressed to `DebrisModule.BodiesOn`, the ship's surface, the only one debris drifts by.
- **The host player's bodies run for the ship's pieces wherever its hero is,** a guest's lines among them, each rope anchored
  at its holder's newest sample carried on at most `AnchorAhead` and then held.
- **Every peer draws every line** from the sim's `Lines`, sagging by its slack and straight once taut.
- **Guests ease corrections** (`Shared.PieceEase`): a jump of a piece's sim pose against where it was drawn the frame before
  goes into an offset of place and angle that dies away over `EaseMs` (120 ms); ordinary motion passes through; a jump over
  `SnapAt` (3 cells: a load, a resync) snaps. Only the drawing eases (`PieceDrawn.Pose`); aim, hits and the scan read the
  sim (`PieceDrawn.At`, `Shape`).
- **`FeedSlack` 125 → 60** (a sixteenth of a cell): smaller corrections. An untouched body settles onto the sim's drift, so
  only knocked and towed bodies are fed.

## Deliberately not handled

- **A guest's towed piece trails by the round trip;** no prediction (0002).
- **A creature on a wreck** may stand off it by the correction for about 0.1 s on a guest: its feet follow the sim, the wreck is
  drawn eased.
- **A guest's hook point** is taken on the eased picture, off by the same small correction.
- **A line whose holder changes surface** drops a tick later; reaching a transfer means entering a room, which lets go already.
