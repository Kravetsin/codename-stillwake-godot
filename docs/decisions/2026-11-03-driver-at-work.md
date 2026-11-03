# The power driver runs only at its work

- Status: accepted
- Date: 2026-11-03
- Builds on [power-driver](2026-11-03-power-driver.md), and partly supersedes it: "it draws whenever the left button builds
  with it, at plans or not" and "while it runs … it works the square" no longer hold.

Played, the first power driver drew its charge on any press, at nothing too, with nothing to show for it, and its square showed
only once the press had started it, so over plans the pointer showed one cell and over bare floor an empty box. The user: the
square shows only over plans, marks too, and the charge goes only while it works.

**The square follows the charge, not the run.** `Aiming.SideOf` takes the square while the mode is the square and the belt can
pay its draw (`CanDrain`), so it shows before the press and holds while the run catches up a round trip later; the flicker the
first ADR feared came from tying it to the run.

**Only over work.** `Aiming.WorkedSquare` gives the square only while its cells hold a plan, a mark or a broken machine; over
nothing the pointer aims at one cell, as the screwdriver's does.

**It runs while it works.** `Run` is wanted only while the left button works a target that can be worked; a press at nothing, or
at plans all refused, draws nothing. Until the sim runs it, a round trip, it works at the screwdriver's pace (`PaceOf`).

## Deliberately not here

- A stronger frame for the square over a run of plans: the aim's glow is the same as one cell's, faint on pale ghosts; a new
  look is the user's pick.
- The build prompt naming the tool in hand: its keys are shared by every builder.
