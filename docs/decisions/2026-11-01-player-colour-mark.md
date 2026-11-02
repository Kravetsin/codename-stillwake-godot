# The player's colour is a mark

- Status: accepted for the new art ([anime cel](2026-11-01-anime-cel-art.md)); the pixel hero still swaps its ramps until the switch.

A suit used to be repainted whole in its player's colour (`SuitArt.Of`, a swap of exact ramps). The user wants suits with a style
and colour of their own later, and repainting would spoil them; smooth art has no exact ramps to swap either, so it would have
cost a shader as well. So the suit keeps the colours it was drawn in, and the player's colour is a small mark on it, tinted by
`Modulate` and unlit, so it reads in a dark room: a lamp and a stripe on the helmet's crown, a lamp on the chest.

## How much mark it takes

Tried at the far zoom, a hero of 52 px, on the yellow suit:

| Mark | Tells players apart |
| ---- | -------------------- |
| a lamp of 1.6 world px | no |
| a lamp of 2.6 world px in a dark socket | only with a crop |
| and a stripe of 6.5 × 1.3 world px on the crown | cyan and purple do, lit and in the dark, on a look rather than a glance |

Far off, the marker layer still tells players apart better than the suit does, and it stays.

## Limits

- **A player's colour close to the suit's does not read:** amber on the yellow suit shows only as a dark socket. The palette must
  drop what the suit's own colour hides. Check again when a suit is added.
- **Bare, without a helmet, only the chest's lamp is left:** it just reads. We do not add more, for a hero is bare only indoors,
  among friends near enough to see each other.
