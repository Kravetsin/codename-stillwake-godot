# Limbs bent whole at the joint

- Status: accepted; its two pictures meeting at a seam superseded by [whole-limbs](2026-11-16-whole-limbs.md)
- Date: 2026-11-14

## Context

Each arm and leg was two rigid pictures, the upper turned at the shoulder or hip and the lower at the elbow or knee, the
upper's rounded end drawn over the lower's top. On the plain first suit the seam hardly showed. On the detailed suits
since (the quilted white heat suit, the regen suit's bands), the lower picture's top slid round under the upper's end in
every stride and swing, and the seam showed badly in motion.

A whole-limb picture that bends was tried behind a dev key (PR 570), against the rigid pictures on the same hero. The
user was wary of a rubber-hose cartoon look; having played it, the user adopted it outright: it looks far more natural in
every way.

## Decision

- **Poses do not change.** `Pose.cs` and every joint angle stay as they are; only how the parts are drawn changes. Client
  only.
- **A picture is a grid bent in a band at the joint** (`logic/BentLimb.cs`). Within `Band` (4 px) round the elbow or
  knee each row of the picture turns about the joint's centre by a smoothstep share of the bend; above the band it is
  rigid with the upper bone, below it with the lower. Rows turn rather than blend their places, so the limb keeps its width
  at a sharp bend instead of pinching. The rest stays rigid, so buckles and quilting keep their shape, and the hand, held
  tools, the boot and its sole and the tracker on the shin are exactly where the rigid limb had them.
- **Nine points a row.** A row turns whole, so two points would place it exactly, but a quad turned into a wedge round
  the joint's outside is drawn as two triangles mapped flat, which kinked the watercolour along their diagonal into radial
  streaks at a sharp knee. Narrow quads keep every point of the picture within a quarter pixel of where it belongs.
- **No rounded end shows at the joint.** The two pictures meet at a seam where both are full width (`SeamOf`, from each
  made picture's opaque rows, `SuitArt.RowsOf`): the lower is drawn from where it is full, past its rounded top, and the
  upper down to where it still is, fading out over the lower across the seam, so its rounded end (the elbow's cap, the
  knee's pad) is never drawn and nothing marks where the elbow or knee is. Drawing the upper whole, its end bent round the
  joint's outside, was merged first and turned back: it read as capsule limbs, and the user preferred the joint unseen.
- **Drawn by `BentPicture`**, a child of each limb sprite and of its band of light, with the sprite's material, picture,
  offset and ankle cut, where the sprite is in the figure's order. The sprite draws nothing itself: it carries the
  picture as worn and what a crumbling body breaks into.

## Cost

Per figure, eight limb pictures and their lit bands, each about 9 by 19 points. A grid is made again only when its
picture changes (dressing), and recomputed and redrawn only when its limb's bend does; nothing is allocated in C# per
frame, only Godot's copy of the arrays at a redraw. The sprites still issue a transparent draw each.

## Deliberately not handled

- A crumbling body (`Figure.Pixels`) breaks into the rigid pictures: it differs only within the band, for one frame
  before the burst.
- A pad or cap drawn at the end of an upper picture is not shown; a suit that wants one at the joint draws it on the
  lower picture's full part or above the upper's seam.
- The inside of a sharp bend folds over itself, as the rigid pictures overlapped there; nothing creases it.
- The shoulder, the hip, the neck and the ankle stay rigid joints: the torso and the boot cover them.
- The band is the same 4 px for arms and legs and every suit; tuned per picture only if a suit asks for it.
