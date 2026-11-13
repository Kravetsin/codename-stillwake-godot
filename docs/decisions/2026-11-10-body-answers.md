# The body answers the weapon

- Status: accepted; supersedes in part [space-recoil](2026-10-27-space-recoil.md) (a shot on the ground shoves) and
  [0090](0090-the-figure-leans-skids-and-lands-by-its-motion.md) (a landing sends no ring)
- Date: 2026-11-10

## Context

With dust and grit thrown round it ([air-gusts](2026-11-10-air-gusts.md)), a hero firing a railgun stood rooted. Only the arm moved
in a stroke. The axe combos once put the whole body into a strike (branch `feat/axe-combos`, PR 514, closed). The user
dropped them for their pale smear trail, not their body. Asked for "the body and recoil,
fully", for every weapon.

## Decision

- **One layer answers an action** (`BodyPlay`, ported from the combos' body without their chain).
  - The upper body hangs from a waist node and turns about the hip by a lean. The hip dips and pushes.
  - Each foot steps with its boot flat, never past the floor free ahead or behind (`Hero.Room`).
  - The far hand may grip the haft (`HaftGrip`). The aimed arm may kick up.
  - Standing, all of it plays. Striding, only the torso and the arms do. In the air, afloat, climbing or lying, nothing does.
  - The layer blends in and out over 100 ms.
- **Answers are data** (`BodyAnswers`), keyed by the weapon.
  - **Strokes.** A stroke's answer plays on the stroke's own clock, in shares of it from the hit. A slow tool's answer is as
    slow, and the 50 ms stop at a bite holds the body with the arm.
    - The overhand: back on the wind-up, into the hit, a step in. The axe plays it in full and the pickaxe at 0.75.
    - The heave (the sledgehammer, ×1.3): the far hand on the haft from the wind-up, a long step in.
    - The jab (the spear): a lunge.
  - **Shots.** A shot's recoil plays on the real clock over 350 ms, scaled by the gun's `Recoil` over the railgun's 220 (the
    nailer 0.27): the torso thrown back, the gun arm up, the hip back, the far foot stepping back.
  - The arm aims before its kick, and the next shot comes long after it ends, so the kick never moves a shot.
- **Real recoil on the ground.** Standing on the ground, a shot shoves the hero across at `Recoil` × 1.3 a step. The floor
  brakes it as a walk let go: a railgun's level shot goes about half a cell back, a nailer's a few hundredths. A wall
  behind stops it, and an edge drops the hero. Afloat it drifts as before. In the air, on a ladder or gliding, nothing
  happens, and a shot straight down shoves no one. The shove is the hero's own client-authoritative move (0002): nothing
  new reaches the sim.
- **No rings of light.** A landing's flat ring (0090) and the railgun's were taken out by the user in play. The landing's
  dust, grains and a heavy landing's streaks stay.
- **Where it is heading.** The next users are hits that throw the body: a lurker's strike, a blast, a meteor shower
  (unified damage's open users). They are the same keys from an outside cause, and the flinch (`Poses.Flinched`) moves onto
  this layer when the first of them is built.

## Not handled

- A friend's strokes answer with the body, from the figure as drawn here. A friend's shot does not: its arm hangs unaimed
  and no shot cue reaches its figure. Its shove arrives with the position it sends.
- The harpoon, the grapple and a thrown charge do not recoil the body.
- The drawn blade moves a pixel or two with the lean. The hits are cast from the hero as before.
- Shoved off a ledge by a railgun, the hero falls, as walking off it would.
