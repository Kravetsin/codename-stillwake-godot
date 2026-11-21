# The title's name turns to dust and back

- Status: accepted
- Date: 2026-11-21

## Context

The name on the title was a plain label, CASTOFF in the mono font. The user asked for a more interesting look: a picked
font and an effect, the last letters breaking into pieces. Five fonts with five breaks were compared on a pick page
(`tools/pick-kit/pages/title-logo.js`); the user picked «Пепел», Oswald crumbling into fine dust, and asked for the whole
word to go and come back, in a loop. Four loops of it were compared on the same page.

## Decision

- **«Пепел», take «Отражение».** CASTOFF in Oswald, pale cold letters with a watercolour grain and a warm edge toward the
  title's sun. A 19.5 s loop: whole for 7.5 s, then the word turns to dust from its right end over 3.5 s, the wind carrying
  the dust toward the sun; the dust slows, stops for about a second, and comes back the same way; whole again at 17 s.
- **The word is whole most of the time.** It is the game's name: about 10 s of every loop it reads whole, and the loop's
  seam falls inside that time, so nothing jumps.
- **Exported by the code that drew it.** `tools/anime-art/subjects/titlename.js` is the pick page's code: it bakes the word
  at twice the page's scale, a map of when each of its pixels leaves, and the dust's 3,200 grains into `TitleNameInk.cs`.
  The game moves them: the word fades pixel by pixel in a shader by that map, the grains drift by the page's arithmetic
  (`TitleName`), warming near the sun.
- **The font stays with the exporter.** Oswald (SIL OFL 1.1) is in `tools/anime-art/fonts/oswald`, with its licence; the
  game ships only the pictures.

## Consequences

- The name keeps its place over the choices and fades in with the title as before; its loop starts as the title opens.
- Its warmth is computed for the sun's place on the 1280 × 720 page: where the scene is scaled to a wider window the
  warm edge leans a little off the real sun.
- Not done: other words in Oswald, the menu's choices restyled, sound for the dust.
