# Catcher motion

- Status: accepted
- Date: 2026-11-10

## Context

The catcher is a rare mini-boss that faces the player all game, so the user asked for it to feel alive. In space it flew in the
jump pose, legs apart and arms up. Its velocity snapped onto the hero every tick, so its moves read as aimed rather than alive. It
threw its charge straight out of its body. Its aim thread snapped onto the hero and stopped there, though a laser does not stop at
the air it passes through.

## Decision

- **Inertia (sim, PR 532).** Afloat, its velocity turns toward what it wants by at most `Hunt.Accel` an axis a tick (100: from rest
  to full speed in half a second). It overshoots a hero who darts past and swings round. Its standoff brakes a stopping distance
  early, so it does not overshoot the band it throws from.
- **The tell before a throw (sim, PR 532).** It holds the charge `Lob.Tell` ticks (12, 0.6 s), standing still, then throws.
  `FightOf` counts the ticks down as `ThrowLeft`.
- **Flight «Как герой»** (the user's pick of four on a pick page): the hero's own float pose (`Pose.Float`) as it is, drawn
  along a curve through the ticks' places (`CatcherMotion.Along`). The way at each place is the mean of the moves on either side,
  and the next move is guessed from the last two; a jump is drawn straight. It faces its way past a dead band (`FaceBand`), or
  faces what it aims at. Its plume is longer the harder it speeds up or brakes (`JetPlume.Thrust`), and it leans against its
  thrust (`Leaning`).
- **Throw «С пояса, толчком».** The charge hangs at its belt all the while (`Figure.Belt`). In the tell its back hand unhooks
  it, the fuse flares, it draws the charge to its chest and pushes it straight out at the release (`CatcherMotion.ThrowOf`, key
  poses in ms into the tell). Its body leans into the push. The hand comes back as it floats, and the next charge hangs at the
  belt again. The figure's back hand takes an item over its pose (`Figure.OffHand`), its fuse sparking from the thrown charge's
  own frames.
- **Aim «Поиск»**, first drawn by the client: a thread turning at 1.2 rad/s, roaming 0.11 rad about the hero and settling on it
  220 ms before the tell. Played, its beam left from where its gun did not point, the sim striking at the cell it held. So the
  aim moved into the sim, and the roam went:
  - **The gun turns in the sim.** Its heading is a fine heading, 4096 to the turn (`Headings.Fine`, `Rays.FineHeadingTo`). It turns
    toward its quarry at most `Gunfire.Turn` a tick. Its tell holds a cell, the gun kept on it as the catcher moves, and the beam
    is struck along the gun (`StrikeAlong`).
  - **A hero could outrun it.** At first the gun turned at 1.2 rad/s and its tell waited till the gun was on its quarry: a hero
    circling it was never shot. See the fight in space below.
  - **The client draws the sim's gun** between ticks (`CatcherMotion.GunAngle`). Where what it aims at changes (a move, the laser
    burning), its drawn gun chases at `ChaseRate` (8 rad/s), never jumping. It turns round to its raised gun only past upright
    (`FaceSlack`).
  - **The thread** runs on through the hero, fading over 45 cells, to where the laser would stop: its whole `Through` (120 cells),
    or the first solid (`CreaturesModule.BeamStop`, the same rule that ends an arc). The dot shows only where the line crosses the hero.
- **A flown path (sim).** Played, it hung on a single block of hull between it and its hero till it blinked. Afloat, where its body
  does not clear the straight way to its quarry, it flies round by a path. The path search flies (`StepKind.Fly`): a cell across or
  diagonally, a room's cells as walls, through a window `Paths.FlownMargin` (64) cells round. It steers at its speed toward the
  farthest of its next 12 steps it goes straight to, cutting corners. The path is kept till it runs out or its quarry moves 4 cells
  off its goal: searched afresh each time, its way round a block flipped side to side.
- **No dead stops afloat (sim).** Played, it jerked between acts. Where it held still, it now brakes at `Accel`: spent after a blink,
  gathering for one, leaving. In a move's wind-up it sheds a quarter of its speed a tick (`CreaturesModule.Slow`). A move over, it
  flies on at no more than its hunt's speed, rather than stopping.
- **The fight in space (sim, the user's call on playing it).** Afloat it read as a fly, not a mini-boss: its goal was its hero's
  cell, so it lived in its pounce (500–900 of 1100 ticks on the starting ship), fired 1–3 times a minute, and hung by the hull where
  its hero's cell had no way in. Now, where things float:
  - **It keeps its range** (`Hunt.Range`: 20–35 cells). Every `Paths.Replan` ticks it picks a spot on the ring 27 cells round its
    hero, open and in its sight, its bearing turned `Orbit` (32 fine headings) its way round (by its id), the ring's 32 bearings
    scanned from there (`RingSpot`); it flies there, at half its speed once in the band. With no spot it hunts as before. The pounce
    is left for a hero who comes within `Strike`.
  - **Never hung by the hull.** Played, it hung 10 s under the stepped belly of the hold: pressed to it a fraction off its own cell,
    no step of its path was straight ahead, so the path was dropped and found again each tick. It settles onto its cell first. Not
    closing on its spot for `LagAfter` ticks, out of the band, it blinks onto it.
  - **Its laser leads and keeps its beat.** The gun turns at 3 rad/s (`Turn` 98) toward where its hero heads over the tell, its drift
    in the last sample scaled (`LeadOf`), and the tell starts on the beat whether or not the gun is on. A miss runs into the air.
    A hero flying straight is hit; one that turns at the tell is missed: dodging, not circling, escapes it.
  - **The barrage** (the user's idea, `Hunt.Barrage`): every 25 s, its hero in sight within `Far`, a blink's gather, then 10 blinks
    8 ticks apart round its hero, a third of a turn on each time, the ring 12 and 20 cells off by turns. Landed, still, its gun is
    set on its hero's cell and fires a `Gunfire.Tell` later. A still hero takes every beam, one flying through few. It is the blink
    chained (`Volley`, `BarrageAt` in `Mem`), `Busy` while it runs, so nothing else begins; no spot ends it, so does leaving.
  - **The client** reuses the blink's flash and sound, the thread and the beam. A blink sets the drawn gun at once, and a beam
    still burning stays where it was fired from.

## Deliberately not handled

- The thrown charge leaves from the catcher's middle, as the sim throws it, not from its fist a cell off.
- An abandoned throw (no shelter left, or the catcher leaving) plays out as if thrown, the hand empty.
- On a planet it hovers in the air pose as before; only afloat does it float.
- Facing flips at once, as the hero's does; the page turned it over 180 ms.
- The pounce still launches at its full speed at once, a dash; only its wind-up eases.
- While it is in a move, gathers, stands spent or throws, its gun does not turn; it turns on from where it was after.
- The range, the lead and the barrage are afloat only: the planet's fight is as it was.
- The barrage's gather looks like a blink's: nothing warns that ten shots follow. A warning comes only if play asks for one.
- Another hero in the line of a barrage shot takes the beam, as with every laser; blows during it go unanswered.
- A hero no ring spot sees (deep in a recess) is hunted as before: by a path to its cell, the pounce, the blink.
- Its lead is a straight line: a hero turning is not read.
