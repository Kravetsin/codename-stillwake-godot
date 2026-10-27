# Hero art without a suit

The painter of the hero under the suit: the jumpsuit, the bare head with its faces and hair, and the helmet split into
a shell and a glass visor so the face shows through it. The pictures were picked on the concept page
[`tools/concepts/hero-looks`](../concepts/hero-looks). The suit's own parts (`hero_helmet`, `hero_torso`, …) have no
painter.

Plain Node, no packages. `parts.js` holds every picture as pixel rows and the ramps; `paint.js` writes them.

| Painter | Writes in `game/art` | Inputs | Command | Reproduces |
| ------- | -------------------- | ------ | ------- | ---------- |
| `paint.js` (with `parts.js`) | `hero_head`, `hero_face_0-2`, `hero_hair_0-4`, `hero_visor`, `hero_helmet_shell`, `hero_jumpsuit_{torso,upperArm,forearm,thigh,shin}` (16) | `game/art/hero_helmet.png` | `node tools/hero-art/paint.js [artDir]` | exact 16/16 |

It fails when a head picture leaves the helmet's outline or two ramps share a colour.

## Templates

Every variant of a slot shares one template (0003): its size and the joint it turns at, in pixels.

| Slot | Pictures | Size | Joint |
| ---- | -------- | ---- | ----- |
| head | `hero_head`, `hero_face_*`, `hero_hair_*`, `hero_helmet_shell`, `hero_visor` | 13×12, the helmet's | (6.5, 12), the neck |
| torso | `hero_jumpsuit_torso` | 12×15 | (6, 0) |
| upper arm | `hero_jumpsuit_upperArm` | 5×9 | (2.5, 2.5) |
| forearm | `hero_jumpsuit_forearm`, the hand in skin where the glove is | 5×10 | (2.5, 2.9) |
| thigh | `hero_jumpsuit_thigh` | 6×12 | (3, 2.5) |
| shin | `hero_jumpsuit_shin`, a shoe where the boot is | 9×11 | (3, 2.18) |

The head's layers lie inside the helmet's opaque pixels, so a suited head shows only through the visor's window
(row 4 to 7, column 7 to 11). Drawn in order on the helmet's node: head, face, hair; suited, then the shell and the
glass. The glass is translucent, its glint moved to the window's upper back corner, off the eye.

## Ramps

Each ramp is four shades, light, own, shade, outline. A picture is drawn in one ramp per kind and recoloured by
swapping each shade for the same shade of another, as `SuitArt.Of` does for suits.

| Kind | Keys in `parts.js` | Drawn in | Variants |
| ---- | ------------------ | -------- | -------- |
| suit | `l s d o` | `Suits.ShadesOf(Suits.Drawn)` | the four `Suits.All` |
| skin | `L S D O` | `SKINS[2]` | 7, light to deep |
| hair | `1 2 3 4` | `HAIRS[1]` | 6: black, brown, copper, blond, grey, rose |

The faces use the skin's ramp for the mouth and freckles and the hair's for the brows; the eyes (`e w`), the mouth's
red (`m`), the gear's greys and the glass keep their colours.
