# The drone catches up on a spring and shines from open air

- Status: accepted; supersedes in part [drone-motion](2026-11-21-drone-motion.md) (the body: the anchor, `Drift`, `Slack`
  and `Reach`; the perch box) and [light-drone](2026-11-21-light-drone.md) (walls ignored by the perch and the light)
- Date: 2026-11-22

## Context

Played, the drone sat in the head. In a fast flight it hung behind as on a wall: the `Reach` circle held it and the
catch-up never closed the last 23 px at cruise. Twisting in flight moved its anchor with the leaning feet, so it changed
sides in a blink. Moved out of the head it often stood in a wall, and then the light came from the head or from the next room.

## Decision

- **A spring** (`DroneFlight`), critically damped about the hero's pace as learnt a little late: a start or a turn leaves
  it behind, then it outflies the hero until it is by it and keeps by it. The further it is left the quicker it answers,
  eased, never jerked. No circle. It flies in small steps with the anchor moved through the frame, so it is the same at
  30 and 144 fps. A jump of the anchor over `Lost` still puts it at its goal.
- **The anchor** is 24 px over the neck in the hero's column; afloat, where it is over an upright body, so a lean or a
  twist moves nothing; lying, over the body's middle.
- **Perches are aside**, 12 to 18 px either way, never over the head.
- **Open air** (`OpenAir.Nearest`), one rule for the beam, what it glints on and the perch: the point nearest the wish
  that is out of walls, off their faces and in sight of the neck. A perch also keeps 13 px from the helmet's middle where
  there is room. With no such point the light comes from the neck.

## Deliberately not handled

- It still flies through walls on its way; only where it settles and where it shines from are kept open.
- A dart, a visit, a window's place and love go where they go, walls or not.
- On a long fall it is left some cells behind, then catches up.
