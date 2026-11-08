# A find flashes along the tablet's edge, once a batch; the bulb and the flight go

- Status: accepted
- Date: 2026-11-08
- Supersedes the bulb and the find's flight of [hologram-keys](2026-11-05-hologram-keys.md); its keys, numbers and credits stay

## Context

After two days of play the user found the find's flight, the picture sliding 0.9 s down a beam from the bulb at the top of the screen into the
research key, more irritating than useful, and the bulb itself more than was needed. What it has to do is make the player notice, once, that
something waits on the board. The lamp on the old key was not seen ([bulb-signal](2026-11-02-bulb-signal.md)): nothing moved there, and the eye is on
the hero. So the signal stays at the edge, where the key is, but it moves along the whole width of the screen, which the side of the eye catches.

## Decision

- **A flash along the edge** (`FindFlash`, `FindFlashView`): amber light runs in along the top of the tablet's edge, the rim or the tablet that is up,
  from both ends to the research key in 0.45 s, gathering speed. As it lands the whole edge lights and fades, and the key flares, a ring of light
  spreading off it, over 0.6 s. Drawn additively through the keys' `holo.gdshader`. Seen by this player only.
- **One flash a batch:** finds while the light runs in join it; finds for 1.5 s after it began go straight onto the number with no flash of their
  own. Ten finds at once flash once.
- **The number** on the research key holds the finds the light carries until it lands, then counts up and flares (`CountFlare`). The key stays amber
  while anything is unread: the lasting reminder.
- Unchanged from the bulb: opening the tablet puts it out and nothing flashes while it is open; what waited when a world was opened, or on another
  world's board, flashes nothing.
- The bulb, the flight, their pictures (`bulb_*`, `bulb.js`) and `--bulb` are removed; `-- --flash` plays one find, three together and one more.

## Deliberately not handled

- Sound, and any sign of which find it was: the board shows that when opened.
- A flash while the research key has not shown yet (the very first note) runs to the edge's middle, as the key flickers in there.
