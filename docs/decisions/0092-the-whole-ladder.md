# 0092 — The whole ladder, and a jump that steps up onto a ledge it looked to reach

- Status: accepted
- Date: 2026-10-21
- Supersedes in part: 0077's "a jump off one goes on with up held (it is not caught again while rising)"; 0005's "a jump
  clears three cells but not four", for ledges: a held jump now steps up onto four

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
- **A calm hand over hand:** the climb's cycle is `Ladder.Stride` 4 cells climbed, read from the hero's height, not from
  the distance since the pose began: a hand reaches over the other every 2 cells, 3.3 times a second climbing up, 4.5 down.
  Each hand holds from 8.6 px over the shoulder down 8.8 px for 0.6 of the cycle, then is brought back up; each foot the
  same with the opposite hand. Not tied to the rungs: the hands slide up the rails between reaches. A first build held the
  rungs of `ladder.png` exactly, a cycle a cell; played, the hands went far too fast (13 reaches a second), so it went.
- **The ledge assist:** a jump that falls just short of a ledge it is pressed against steps up onto it, by what the player
  sees: the higher foot as the air pose draws it at the jump's speed (`Poses.FootLift`), in thousandths of a cell
  (`Hero.MantleReach`): 278 rising, 297 at the top (Space held, the knees tucked), 246 a step into the fall, 144 at 60,
  78 at 150, 36 falling fast. When the feet are short of a whole wall's top by no more, the hero is in a jump (`Leaping`,
  off the floor or a ladder: not a walk off an edge, nor a fall into a hole), its key is held toward the wall, it rises no
  faster than `MantleRise` 110 a step (faster, the jump clears it by itself), and it fits standing on the top
  `MantleAcross` 60 over its edge with room to rise there, it is raised by up to 110 a step and, the step its feet reach the
  top, put 60 onto it, standing; at most three steps, no step longer than a climb's. Let go of the key and it falls as any:
  the assist never holds the hero. The figure shows it in the ladder top's pull-up at its end (`Poses.PullUp`): the front
  foot raised onto the ledge, the arms pressing down, the body leaning in. A friend's hero shows it from its position and
  rise alone (`Hero.MantleLedge`: rising or still in the air, pressed against a wall whose top is in reach).
- **What it reaches:** a held jump's feet rise 3954 aboard, 46 short of a fourth cell: a **four-cell ledge is now reached**,
  five not (1046 short). On the planet 6014: six cleared outright as before, seven not (986 short). A tap (1222) reaches
  no more than before.
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
- **The cycle by height, not by distance:** a friend's hero is drawn from interpolated positions; by height its hands are
  where its own peer draws them.
- **The assist's reach is the drawn foot, not a fixed half cell:** the complaint was a foot seen over the ledge that did not
  get the hero on it. The reach is what the pose draws, so a jump looks able to reach exactly what it reaches; about a third
  of a cell at most, less falling, where the legs hang.
- **Only from a jump, only pushing toward the wall:** a hero walking off a ledge and holding back toward it slides down its
  wall, as it did, and one falling into a hole one cell wide must stay in it (playtest 1).
- **Four cells aboard, flagged:** 0005 set "a jump clears three cells but not four"; a jump still clears three, but with the
  assist it gets onto four. A doorway three tall, a shaft, a hatch are unchanged; a four-high wall is no longer a barrier
  to a hero who jumps. The user decides whether that stands; `MantleReach` read from the foot is what would shrink.

## Deliberately left out

- **The hands are drawn ahead of the body**, as before, out past the helmet so they never cross the visor: in profile they
  are not on the rails. Drawn on the rails, the front glove would cover the visor at the top of each reach.
- **A three-wide hatch's edge is out of reach:** pulling up through one, the hands press down over its opening where the
  hatch is open, not on the deck beside it. Through a one-cell shaft they press on its edge.
- **No sound for the slide:** every new sound goes through an audition of picked candidates; the slide is silent till then.
- **Zero-g is unchanged:** a body afloat swims, ladders or not, as before; a ladder in a room that vents lets go of the hero.
- **The page's corner correction and the camera** are other items. The assist takes whole walls and closed doors only: a
  slope's step is the walk's own (0069).
- **The pull-up faces where the hero faces** (the pointer's side), whichever side the floor is on.

Recorded with `-- --ladders --autopilot --lit --zoom 4` (`HeroAutopilot.Ladders`, `LaddersRun`: a run grabbed, the climb
through a hatch, down and slid, a jump caught over a pit, a leap off) and `-- --strip --ladders --autopilot --zoom 4` (the
strip's shaft, under the planet's gravity), and `-- --ledges --autopilot --lit --zoom 3` (`HeroAutopilot.Ledges`: a held
jump stepping up onto four cells, one falling short of five). Tested in `LadderTests`, `LedgeAssistTests`, `HeroTests`
(`JumpsOntoALedgeThreeCellsHighNotFour` became `JumpsOntoALedgeFourCellsHighWithTheLedgeAssistNotFive`),
`PlanetHeroTests` and `PoseTests`.
