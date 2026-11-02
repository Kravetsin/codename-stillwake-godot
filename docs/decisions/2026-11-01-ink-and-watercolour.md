# Ink and watercolour

- Status: accepted as the look; the switch still waits on the nature probe in [the plan](../plans/anime-art.md).
- Supersedes the look, not the plumbing, of [anime cel art](2026-11-01-anime-cel-art.md): hi-res pictures at 4 times, `HiRes`, the
  atlas and the flag stay as they are.

Cel held for icons and failed for everything else: every wall and nature candidate in it read as a cartoon, and the game is about
a lone scavenger. Rather than iterate on walls, the user asked for one named style, chosen on one object. Five techniques were
drawn on the same piece of hull ([the page](https://claude.ai/artifact/5oU8Kn48KSJ3ZV4d2mgjuz)): etching, watercolour, gouache, noir,
airbrush. The user's pick: **ink and watercolour**.

The icons and the hero were then redrawn in it ([the page](https://claude.ai/artifact/J2bcigFNj1qGH2m3F7ZpSv)). The first strength
looked dusty and faded, so the colour came back and the grain and blooms went down to a trace; the user accepted that one.

## What it is

- **A finish, not a filter.** `tools/anime-art/wash.js` replaces `Anime.bake`: it draws a subject's vector forms again as washes and a
  pen line. Nothing is laid over a finished picture; the user rejected that twice.
- **One finish for every subject**, so the rules are in [one place](../../tools/anime-art/README.md#the-style) and a drawing needs no
  change of its own to follow them. A subject's work is its forms and materials.
- **Checked once in the game:** the testbed room with its light, a frame by eye at zoom 2.5. Stations stand clear of the back wall.

## What it costs

- **Size:** the same 385 pictures in `game/art/hd` went from 1.5 MB to 3.4 MB: washes compress worse than flat cel. At 905 pictures
  that is still small against the game.
- **Softness:** at zoom 1.25 the wash is mostly lost and the pictures read as colour and line. Accepted: the forms still read.
- **Cartoon risk:** a clean colour sits close to one, and the hero's large head and eyes are the nearest thing to it. Those are the
  drawing's, not the finish's.

## Deliberate omissions

- **No per-picture strength:** one setting for all, or the art stops matching.
- **No moving washes:** static textures, as cel.
- **Not yet in the finish:** what draws straight on a canvas (`wall-edge_*`, door panels, the player's mark). They look crisper
  than the rest; each moves when its family does.
