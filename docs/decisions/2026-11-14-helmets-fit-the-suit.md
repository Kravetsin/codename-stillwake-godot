# Helmets fit the suit, each turning its own way

- Status: accepted
- Date: 2026-11-14

## Context

The traced helmets ([helmets-traced](2026-11-14-helmets-traced.md), [quilted](2026-11-14-quilted-helmet-and-traced-art.md))
sat on the suit as three loose pieces. The near arm's upper picture showed its round dark top over the collar; looking up or
down tipped the whole helmet, collar and all, about the neck; and the collars ended in a straight cut across the torso.

## Decision

- **Each helmet turns its own way** (`HelmetTurn`, `game/features/crew/logic/BodyFit.cs`, client art, not tunables):
  - `helmet` **soft**, as the hood: the ball nods whole with the look, its black rubber seal the band that bends, about the
    seal's middle (row 11.75); the seal's base stays on the torso. The seal is bent as drawn, 3.5 px tall: no taller bellows was
    needed. (It first turned **ringed**, the ball about its middle on a fixed ring, as on a bearing; see the verdict below.)
  - `quiltedHelmet`, `catcherHood`, `catcherCap`, `catcherBeak` **soft**: the top with its plate or mask turns, the base stays on
    the shoulders, the cloth between bends as a limb does at its joint ([bent-limbs](2026-11-14-bent-limbs.md)), one pixel below
    the neck so a plate over the chin turns whole. The head and face inside bend with it, so none shows through the cloth.
  - `heavyHelmet` **nods on its gasket**: the dome, with its brass neck ring, hoses and the face behind its glass, turns whole
    with the look (a `Ring` fit) about the middle of a rubber gasket under the ring (row 11.8); the shoulder piece stays on the
    torso, and the gasket alone bends between them, in a band 1.4 px tall (`HelmetFit.Sealed`, `SealBy`). The bell had no soft
    part, so the gasket is new art: the ring's own shape and that shape lowered 0.9 px, in the bell's hose rubber, drawn by the
    engine under the ring (tools/anime-art helmets.js, `belled`). The bell is three pictures, `hero_heavyHelmet_shell` (the dome),
    `_collar` (the shoulder piece, the ring's and the gasket's place left open) and `_seal`, so neither brass nor steel stretches.
    (It was first **bolted**, the face turning inside; see the verdict below.)
  - `catcherHelm` **bolted**: never turns; the face inside turns about the head's middle, and the lamp's beam aims where the
    hero looks.
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

## The verdict in play

The user played the first cut (PR 577): the ringed basic helmet "animates very badly", and the face turning inside the bell was
disliked too; the quilted hood "connects well to everything" and stays as it is. The basic helmet became soft like the hood. For
the bell, keeping the face still behind the glass, only the beam looking, was offered and judged worse still: the bell keeps its
face turning until a way is found, perhaps after the basic helmet's example.

The soft basic helmet was loved in play (PR 579): "ооо, вот это отлично!", and the bell was asked to move the same way. A soft
bend of the bell itself stretched its brass ring and shoulder piece where they lifted apart, the ring's edge not level; so its dome
turns rigidly and only its new gasket bends.

**F8 in any dev run** (`game/dev/HelmetTurnKey.cs`, `BodyFit.Trial`) turns every helmet worn the next way the code has, named in a
corner: soft, bolted (the face turning), on a ring, bolted with the face still (`BoltedStill`), then each its own again.
A soft trial bends about one pixel below the neck, a ring about the basic helmet's ball or the neck, a bolted one about the head.

## Cost

Three node transforms a frame and seven bent pictures; a mesh is rebuilt only when its bend changes, so only on a soft helmet
while the head moves (seven 17 × 9 grids). Nothing is allocated a frame. Fades are baked once at load.

## Not handled

- A soft helmet looking down leaves a near-invisible pixel of the shell's faint edge stretched in its band.
- A held item raised near the shoulder passes under the collar, as under the shoulder it covers.
- The basic helmet's lamp card (`hero_helmet`) stays the whole helmet; a dead hero's crumble breaks the pictures unturned.
