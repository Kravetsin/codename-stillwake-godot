# Helmets fit the suit, each turning its own way

- Status: accepted
- Date: 2026-11-14

## Context

The traced helmets ([helmets-traced](2026-11-14-helmets-traced.md), [quilted](2026-11-14-quilted-helmet-and-traced-art.md))
sat on the suit as three loose pieces. The near arm's upper picture showed its round dark top over the collar; looking up or
down tipped the whole helmet, collar and all, about the neck; and the collars ended in a straight cut across the torso.

## Decision

- **Each helmet turns its own way** (`HelmetTurn`, `game/features/crew/logic/BodyFit.cs`, client art, not tunables):
  - `helmet` **ringed**: the ball turns about its middle (6.7, 6) on a black neck ring that stays with the torso, as on a
    bearing; the ball's picture is kept within its circle under the ring, so no gap opens.
  - `quiltedHelmet`, `catcherHood`, `catcherCap`, `catcherBeak` **soft**: the top with its plate or mask turns, the base stays on
    the shoulders, the cloth between bends as a limb does at its joint ([bent-limbs](2026-11-14-bent-limbs.md)), one pixel below
    the neck so a plate over the chin turns whole. The head and face inside bend with it, so none shows through the cloth.
  - `heavyHelmet`, `catcherHelm` **bolted**: never turns; the face inside turns about the head's middle, and the lamp's beam
    aims where the hero looks.
  - Bare, or any helmet with no fit: turns whole about the neck, as before.
- **Two layers.** The upper part (shell and glass above the fit's cut) is drawn under the near arm, so a raised hand passes in
  front of the face; the lower part (the shell's rows from the cut, or the basic helmet's own `hero_helmet_collar`) is fixed with
  the torso and drawn over the near arm's shoulder, as a pauldron.
- **What rides on the crown** (lamp, scanner, goggles' glow, the visor's breath) turns with the upper part; on a bolted helmet
  it stays, and `HeadLampToward` takes the look.
- **No shoulder end.** Every upper arm's picture, and its light, fades in from nothing at row 1.5 to whole at 4.5 of its box,
  baked at load: no round end shows at the shoulder, standing or raised, either arm, any suit. The catchers' bodies keep theirs:
  their receiver is strapped there.
- **No straight cut.** The bell's and the quilted hood's bottoms fade into the suit over their last rows.
- **Seen** on the dress sheet (`-- --dress-sheet=<png>`): every player helmet on every suit at looks −0.6, 0, +0.5, walking,
  running, climbing, aiming up; the catchers' gear.

## Cost

Three node transforms a frame and seven bent pictures; a mesh is rebuilt only when its bend changes, so only on a soft helmet
while the head moves (seven 17 × 9 grids). Nothing is allocated a frame. Fades are baked once at load.

## Not handled

- A soft helmet looking down leaves a near-invisible pixel of the shell's faint edge stretched in its band.
- A held item raised near the shoulder passes under the collar, as under the shoulder it covers.
- The basic helmet's lamp card (`hero_helmet`) stays the whole helmet; a dead hero's crumble breaks the pictures unturned.
