# 0092 — The whole ladder: grabbed on the move, slid down, leapt off, pulled up out of

- Status: accepted
- Date: 2026-10-21
- Supersedes in part: 0077's "a jump off one goes on with up held (it is not caught again while rising)"

## Context

The ladder was ported as the TypeScript had it: grabbed only with up or down and no side held, never while rising; the hero
snapped onto its middle; up and down at one speed, 10.8 cells a second; the hands' cycle a fixed 3.6 cells climbed, so they
slid past the rungs; and at the top the figure went from hands over its head to standing in one frame. The research page
that proposed 0089 and 0090 drew the whole ladder in its JS copy of the controller and the figure (its ladder card, item 9),
and the user approved it with "numbers: as proposed".

## Decision

The rules are the client controller's (`game/features/crew/Hero.cs`), integers at 60 steps a second, every number in
`Stillwake.Content.Hero`; the figure's are drawn only (`Pose.cs`, `Motion.cs`, `Figure.cs`), its numbers in
`game/Content/HeroMotion.cs` (`Ladder`).

- **Up grabs on the move.** Up grabs a ladder the body touches walking, running, falling or rising: a run at a ladder with
  up held, or a jump at one, takes it. Not for `Regrab` 12 steps (200 ms) after a jump or a walk off one, so a jump off it
  with up held goes on, and is caught again higher if up is still held then. Down grabs as before: standing or falling, no
  side held. On the ground a side walks off the ladder unless up is held too, which climbs; in mid-air a side turns the
  hero, as before (0077).
- **Eased onto its middle:** a step moves the hero `LadderCentring` 450 thousandths of the way, rounded toward the hero,
  onto it within `LadderSnap` 25: 5 steps from 450 off, 6 from the column's edge. The step is not taken where the body would
  not fit; down at the top of a one-cell shaft, the hero stays on the ladder, held at its top, till it fits.
- **Speeds:** up `ClimbUp` 110 (6.6 cells a second), down `ClimbDown` 150 (9), and with Shift `Slide` 300 (18). `Climb` 180 is
  gone.
- **A leap off to a side:** Space with a side held leaves at `LadderJumpAcross` 900 thousandths of the run across (198) and
  `LadderJumpRise` 850 of the jump up (352), rounded toward zero; held or cut as any jump (0089). Straight up it is the
  floor's jump.
- **The top, without a jump in the figure:** the body still rises at the climb's speed to the top and stands on it the step
  its feet reach it, never further than a step went. The figure pulls itself up meanwhile, from where the hero is:
  `Hero.LedgeAbove` reads how far over the feet the ladder's top is, the highest ladder cell (or hatch it climbs through)
  within three rows, where the hero fits standing (none under a closed hatch). From the hands' highest hold (34 px over the
  feet) the hands go onto it over 4 px and stay there as the body rises past them, closing in under the shoulder to push
  down; within 18 px the front foot steps up onto it; within 8 px the back leg straightens; the body leans up to 0.15 rad into
  it, upright by the ledge. At the ledge the pose is all but the standing one, so the step that stands shows no change.
  Climbing down from a top plays it backwards.
- **Hands on rungs:** the climb's cycle is a cell, read from the hero's height, not from the distance since the pose began.
  `art/ladder.png` draws rungs 5 and 12 px down each cell; the front hand takes the one, the back hand the other, each for
  0.55 of a cell climbed, so a held rung stays where the ladder draws it while the body passes it; each foot takes the rung
  its opposite hand's stroke puts it on. The body is no longer lifted on each pull, which would move a held rung.
- **The slide:** read from the fall alone (faster than the climb down, all of it at the slide's speed): both hands high on
  the rails, the legs straight; the gloves throw 0.8 sparks a step off the rails (`Luminous.RailSpark`): tiny warm streaks
  flicked up and out, arcing down and cooling in under a third of a second, each lighting its spot a little (0075).
- **Friends see all of it from what a hero sends:** the climb's limbs from `Climbing`, `Y` and `Vy`, the ledge from `X`, `Y`
  and the grid, the slide and its sparks from `Vy`. No new field; `LetGo` stays with its own peer.

## Why these choices

- **A pull-up spread over the climb's last two cells rather than a 170 ms pose after the top.** The page kept the step onto
  the top and blended a mantle pose in for 170 ms after it. The brief asked for no teleport; at 6.6 cells a second the last
  two cells take a third of a second, and a body that keeps rising while its hands stay on the ledge is the pull-up itself.
  Ending it all but standing leaves nothing for an after-pose to ease.
- **The cycle by height, not by distance:** a friend's hero is drawn from interpolated positions; by height its hands hold
  the same rungs its own peer draws, and a hero that grabs a ladder mid-climb starts with its hands on rungs.
- **The hold's foot tops are computed** from the rungs and the hands' highest hold, so a ladder redrawn with other rungs
  needs only its two numbers.

## Deliberately left out

- **The hands are drawn ahead of the body**, as before, out past the helmet so they never cross the visor: in profile they
  meet the rungs' height, not the rails. Drawn on the rails, the front glove would cover the visor at the top of each reach.
- **A three-wide hatch's edge is out of reach:** pulling up through one, the hands press down over its opening where the
  hatch is open, not on the deck beside it. Through a one-cell shaft they press on its edge.
- **No sound for the slide:** every new sound goes through an audition of picked candidates; the slide is silent till then.
- **Zero-g is unchanged:** a body afloat swims, ladders or not, as before; a ladder in a room that vents lets go of the hero.
- **The page's corner correction, ledge assist and the camera** are other items.
- **The pull-up faces where the hero faces** (the pointer's side), whichever side the floor is on.

Recorded with `-- --ladders --autopilot --lit --zoom 4` (`HeroAutopilot.Ladders`, `LaddersRun`: a run grabbed, the climb
through a hatch, down and slid, a jump caught over a pit, a leap off) and `-- --strip --ladders --autopilot --zoom 4` (the
strip's shaft, under the planet's gravity). Tested in `LadderTests`, `HeroTests` and `PoseTests`.
