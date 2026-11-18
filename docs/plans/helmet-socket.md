# Design: the helmet socket

## Problem
Each painted helmet is a whole picture from GPT, fitted afterwards by hand. These have gone wrong again and again:
- Its bottom is never the same twice. It has drawn its own neck ring, bulged behind the neck onto the shoulder, sat lower
  or wider than the basic one, or covered the rubber.
- What hangs below the bend stretches when the hero looks up or down: the skins' beaks and jaws (fancy-skins), now the
  dust wrap's filter.
- Every helmet needs rows of its own (`BodyFit`, `LampMounts`, `ScannerInk`), set by eye, so no two move alike.

## Decided with the user
- The engine owns what touches the body: the bottom edge, the rubber seal, the glass. GPT paints only the dome above them.
- The seam must not look glued on. The dome's lowest part is still GPT's own paint, and a candidate whose seam does not
  match is caught on the pick page, on the hero, before anyone picks it.
- Helmets that cannot move to the socket may stay as they are.

## Where it is heading
- This stage: the five gear-roles helmets (heated, heat, dust wrap, hide and bone, boiler).
- Next: space-zone helmets (cold band, hot belt), planet helmets from the spare picks, black-market helmets. Every new
  helmet is a socket helmet.
- The nine fancy skins work today and stay as they are; one moves over only when it is redrawn for another reason.
- Not users: the heavy bell (its own gasket, its own dome size) and the catchers' gear (bolted, soft hoods).

## The socket
All in the helmet picture's pixels, 76 × 72 hd (the 13 × 12 box with 3 px round it, 4 hd px a world px), as the basic
helmet's pictures are.

| Part | Owner | What it is |
| --- | --- | --- |
| **Rim line** | engine | The basic shell's own bottom outline, from box row 8.25 (hd 44) down. Every dome ends exactly on it. |
| **Seal** | engine | The basic rubber ring (`hero_helmet_collar`), drawn under the dome. It bends, and nothing else does. |
| **Glass** | engine | The basic visor's place and shape, the face behind it. A helmet may tint it: the heat helmet is gold. |
| **Mounts** | engine | The lamp at (8, −2) and the scanner at (6.2, −1.9), the basic helmet's, for every socket helmet. |
| **Must zone** | GPT | The basic shell's silhouette. It must be painted: the lamp's and scanner's mounts sit on its brow. |
| **May zone** | GPT | One polygon, drawn once in the tool: one world px round the must zone at its top and back, above the rim line, and at the front 2 world px past the glass, down to the rim line. The front reach is for a snout, a mask or a filter; its first user is the dust wrap's filter. |
| **Never** | — | Anything below the rim line, or outside the may zone. |

**One way to turn.** Every socket helmet turns whole about the seal's middle (`SealY` 11.75), and only the engine's seal
bends: the skins' fit, `BodyFit.Shell`, unchanged. The soft `Ball` fit is not enough: the dome's own bottom, rows 9.75 to
12.5 of the box, lies in its bending band (`SealY` ± 2), and the dust wrap's filter stretched there. Under `Shell` nothing
painted bends, whatever GPT draws there.

**The basic helmet.** It is the template and stays soft (`Ball`), the user's pick of PR 579. In the try, the F8 key shows it
on the socket fit too. If play prefers the socket, it moves over and the soft fit is left to the hoods.

## Making one
A tool in `tools/anime-art/socket/` (node), committed, in three steps.

1. **Template** (`template.js`): a 3 × 2 sheet of 512 px cells. The top-left cell is the basic helmet, unchanged, as the
   reference. The other five are the must zone in flat grey, the glass in flat blue, the black seal under it, on navy. The
   prompt (kept in the tool) asks GPT to paint the dome in the grey, to end on the black seal as the basic helmet does, and
   to change nothing else. The candidate's own picture goes with it as a reference.
2. **Cut and check** (`cut.js`). It cuts each cell by the grid to 76 × 72, measured against the reference cell. Then:
   - everything outside the may zone or below the rim line is dropped;
   - whatever GPT painted over the glass is dropped; the engine's glass is under it;
   - the rim line is inked in the darkest colour of the dome's own outline, so the seam is in its paint, not the basic
     helmet's. This is the only style step code takes.

   A candidate is **refused** when less than 98% of the must zone is painted: it would leave a hole under the lamp or the
   glass. Refused cells are drawn again. Everything else is shape that was cropped. The report gives each candidate's
   cropped paint, below the rim and outside the zone, and the pick page shows what the crop left.
3. **Pick page.** Each candidate that passes is shown on the hero in the game's own frames: a dress-sheet run with the
   candidates laid in, looking up, ahead and down, walking. A mismatched seam shows here.

Once picked, the tool writes `hero_<id>_shell.png`, the card, and `hero_<id>_visor.png`: the engine's glass, tinted if asked,
open where the dome stands in front of it. The game draws the glass over the dome, so a snout painted over the glass's edge
would hide behind it otherwise.

## In the game
- `Figure.PictureOf`: a socket helmet's seal is `helmet_collar`, its collar is none, and its visor is `visor` unless it has
  its own.
- A list of socket helmets in `BodyFit` gives each the `Shell` fit; the rows of the five gear-roles helmets go.
- `LampMounts` and `ScannerInk` lose the five helmets' rows: their `Of` already falls back to the basic one's.
- One test in `SkinCatalogueTests`: every socket helmet has its shell and card, no seal or collar picture of its own, and
  the `Shell` fit.
- The five gear-roles helmets are drawn again through the tool; their `_seal`, `_collar` and copied visor files go.
- Checked on the dress sheet: a dead socket helmet's crumble, and the eyes' glow (`hero_<helmet>_lit`) inside one.

## Deliberately not handled
- **No metal neck ring.** A fixed ring between dome and rubber would make every helmet even more alike, but the basic
  helmet has none and its look is the one the user keeps.
- **Nothing hangs below the rim:** no fringe, jaw, beak or skirt. A snout goes in front, above the rim line.
- **No helmet bigger than the may zone:** tall horns and crests are cropped away.
- **The nine skins are not moved.** They work today. Redrawing them would cost nine sheets and risk breaking them, and
  players would gain nothing yet.
- **One glass for all.** No slits, no second eye, no other shape: the face, the breath on the glass and the retch film
  all assume the basic glass.
- **The seam is not judged by code.** The checks catch shape, not style. The pick page on the hero is where a seam that
  looks glued on is caught.
- **The bell and the catchers keep their own fits.**
