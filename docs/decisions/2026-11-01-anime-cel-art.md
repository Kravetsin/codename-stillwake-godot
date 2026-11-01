# Anime cel art

- Status: accepted as the direction; the switch waits on the nature probe in [the plan](../plans/anime-art.md).

Item icons had run out of pixels at 16 × 16, and the hero, at 42 pixels tall, could not carry a designed suit. Three rounds of
samples tried hand sketches, ink and wash, soft anime, plain vector and filters laid over crisp vector. The user's pick:
**anime cel, crisp, no filter**: a dark ink line, flat colour, one hard shadow tone, one highlight; "not too simple, not too
realistic". What the earlier rounds lacked was the drawing, not the manner: they traced 16 px sprites, and these are drawn anew.

A slice then ran in the real game: the testbed room redrawn whole, the hero, ten icons, a cave corner. The user played it and
agreed to go on.

## What it is

- It stays behind `-- --anime-art` until the switch; without it the game is drawn as it was.
- Every picture is drawn in code (`tools/anime-art/`) with one engine, one line weight per world pixel and one light, and
  exported at **4 times** its logical size: zoom reaches 2.5, the rest is room for the screen.
- A hi-res picture stands in for the picture of its name (`game/art/hd/<name>.png`, `game/render/HiRes.cs`) and **reports its
  logical size**, its texture filtered smoothly with mipmaps whatever node draws it. So layout, tiles, rig, joints and every
  caller of a picture's size stay as they are, and no sim code knows.
- The ship's atlas grows with it, a clear cell round each picture with its edges copied into it, for mipmaps reach past a
  picture's edge. Godot's own texture padding is off: it copies by the reported size.
- The hero is shaded from straight above, so turning changes nothing; the world's art may light from the side.

## What it costs

- **Measured, the whole room, an RTX 4060 Ti:** 395 fps at zoom 2.5 and 167 on the reference world, against 419 and 181 with
  only the first pieces in. One run each, so partly noise. About 60 MB of video memory for the atlas (2048 × 5760 with mipmaps).
  No mid-range card measured yet.
- **Softness.** At zoom 1.25 (the hero 52 px) the art is plainly softer than the pixels were; it is crisp only near. Accepted:
  the price of smooth minifying, and it reads.
- **Overlays placed by pixel.** What the client draws over a picture at fixed pixels (`StoreLights`' gauge, screens, lamps)
  needs the new picture to keep that spot. A bank's gauge moved off its slot this way in the slice.
- **The work.** Tiles and edges are generators, a new material mostly a palette; machines and icons are drawn one by one, about
  30 lines an icon, 50 to 75 a machine. Nature is the hardest, and the moss of the slice failed.

## Deliberate omissions

- **No live vector and no filter:** baked textures, static, as the rest of the renderer expects.
- **No whole-suit recolour:** [the player's colour is a mark](2026-11-01-player-colour-mark.md).
- **No hi-res normal maps:** the dome is low-frequency, built from the logical picture; beside a lamp nothing looked misplaced.
- **No resolution past 4 ×:** a 4K screen at the nearest zoom will soften; we do not handle it until a player complains.
- **Logical pictures stay:** size, outline, crumbling pixels and normals read them. Once the pixel art is gone they are made by
  reducing the hi-res picture.
