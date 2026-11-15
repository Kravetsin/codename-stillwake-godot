# Creatures bend like the hero

- Status: accepted
- Date: 2026-11-14

## Context

The loper was drawn by hand in code: a leg was one rigid picture turned at its hip, the head one picture turned at the
neck's root, the tail one turned at its root. It walked on stilts, and grazing swung the whole head and neck down like a
lever. The hero's limbs bend whole at the joint since bent-limbs, and the user wanted the creatures to move the same way:
legs that bend at the knee, a neck and tail that bend, no seam at any bend. The user picked a generated reference of a
long-legged, long-necked grazer to redraw the loper from.

## Decision

- **The loper is traced** from that reference (`tools/anime-art/refs/loper.png`), as the hero's suits are: the shape from
  the picture; the line, paint and wash from our engine. The picture never ships. `trace.js` already cut a picture by its
  alpha when no `background` is set, so it reads the transparent reference as it is; a piece may now be a `keep` polygon
  instead of a crop box, so the body, neck, legs and tail are cut from one picture where they overlap. Scaled so its trunk
  is as long as the hand-drawn loper's; its legs and neck are the reference's, so it stands taller, its head about the
  hero's.
- **A bending part is one picture bent in a band** (`BentLimb` and `BentPicture`, moved to `game/shared` so the hero and
  the creatures share them). It is drawn whole, never cut at the bend, so no end, pad or cap can show there: what the user
  preferred on the hero. A bone need not hang straight down: a part's `Bend` gives its joint, band and the bone's
  direction, so a neck leaning forward or a tail lying back bends along itself.
- **Legs bend at the knee and hock.** Today's gait is kept, its stride, timing and phases; the knee is a delta on it: a
  leg folds `Pose.Knee` times its lift, so it stands straight on the ground and folds as its foot lifts, more at a gallop
  (lift up to 1.6). A foreleg folds its foot back, a hind leg forward; in the air the forelegs tuck folded. Where the body
  stands counts the knee (`CreaturePose.Drop`), so feet stay on the ground.
- **The neck and the tail bend instead of turning.** The neck and skull are one picture bending along the neck: what
  turned the head (grazing, nibbling, watching, fleeing) bends it, its root staying on the shoulders. It bends about a
  line by the throat, not the neck's middle: the wide throat inside a grazing bend folded over itself about the middle. The eye and ears hang
  past the band and ride the skull. The tail bends in one band along its length: its sway, its tuck in flight.
- **Rigid:** the body, the hips and shoulders, the ears. The far legs are the near ones in darker paint, behind the body.
- **For the next quadrupeds** nothing else is needed: a design's part with a `Bend` is drawn bent and posed so; a design
  without one turns its parts whole, as every other creature still does.

## Deliberately not handled

- Grazing does not quite reach the ground: the neck bending near its root lowers the mouth to the grass's tips, about a
  cell up, as high as the hand-drawn loper's. A shoulder dip or a second band would reach lower; not done until asked.
- The tail is one band, not a chain: the reference's tail is short and stiff, and one bend reads.
- A crumbling loper breaks into its rigid pictures: they differ only in the bands, for the frame before the burst.
- The inside of a bend does not crease: the tests hold that no painted part folds over itself within the bends the pose
  drives (a knee 1.6 times `Knee`, a neck grazing a little past `Graze`); a pose driven further could fold one.
- Other creatures keep their rigid legs until each is redrawn.
