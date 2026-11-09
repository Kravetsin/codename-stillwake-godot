# Blasts are round

- Status: accepted
- Date: 2026-11-09
- Supersedes the aslant cost and the box of [meteor-wave](2026-10-29-meteor-wave.md); its wave, soaking and order stand

## Context

The user saw the blasting charge ([blasting-charge](2026-11-08-blasting-charge.md)) open square craters. The wave both it and a
meteor spread (`sim/Core/Wave`) charged a step `Step` 2 to a side and `Diagonal` 3 aslant, then a wall's cost on top, the same
either way: in stone 10 straight, 11 aslant, so the wave went farther aslant than straight and drew a square with its corners on
the diagonals. Cheap ground ran past the reach and the box of `most` cells either way cut it square. The user asked for one
way to compute a blast, reused by the meteor as by the charge; it was already one, so its measure changed, not its callers.

## Decision

- **A cell's whole cost scales with the step's length:** `(Step + what entering costs) × 5` to a side, up or down, `× 7` aslant
  (`Wave.Straight`, `Wave.Aslant`; 7/5 ≈ √2). Even ground of any kind now spreads it near round, an octagon within a few
  per cent of a circle. Callers keep counting in a straight step's units: their charges are multiplied by 5 inside, and what
  reaching a cell cost comes back divided by 5, rounded up, so a back wall a meteor breaks for what is left spends at least that.
- **Its limit is a circle:** a cell farther than `most` cells from the start (`dx² + dy² > most²`) is not reached.
- **The charge retuned** round, then a third smaller at the user's ask: `Power` 63, `Reach` 8. Stone (10 a cell) opens a bowl 6
  out down a shaft, soil the whole circle of 8, the ruins' alloy within 3. Over open ground the air (2 a cell) is the cheap way: a charge
  landing on a floor runs along it and then down, a wide shallow bowl, as a blast in the open would.
- **Meteors keep their numbers:** a hull is a line of walls, so along it nothing changed; across a thick wall aslant, a little
  less goes. Every meteor test passed unchanged.
- A test pins it and was seen to bite on both old rules: soil opens the circle and nothing past it, and stone is as far out
  aslant as straight down, give or take a cell.

## Deliberately not handled

- Rays from the burst with shadows behind what stops them: the wave already bends round what stops it; rays would cost more
  and change the meteor for no difference at 8 px cells.
- An exact circle: the octagon's few per cent are below a cell at these reaches.
