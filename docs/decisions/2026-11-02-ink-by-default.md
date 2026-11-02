# The game is drawn in ink by default

- Status: accepted; the user's call, 2026-11-02. Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md) and [the batch](2026-11-02-ink-batch.md).

## Decision

`HiRes.On` is true unless `-- --pixel-art` is given. `--anime-art` is gone. The pixel art stays only until its follow-up removes it
and what only it needs ([the plan](../plans/anime-art.md)); until then `--pixel-art` is there to compare and to fall back on, and
reverting the switch is one line.

## Measured

On the same build, pixels against ink, 1280×720, Debug, an RTX 4060 Ti and the machine's weak integrated Radeon as the two ends either side
of a mid-range card, which we do not have. Frame times are `--measure`'s average:

| Scene | RTX 4060 Ti, ms | Integrated Radeon, ms | Textures, MB | First frame, s |
| --- | --- | --- | --- | --- |
| testbed, zoom 2.5 | 2.35 → 2.34 | 5.00 → 5.10 | 31 → 112 | 3–4.5 → 4.1 |
| planet, zoom 2.5 | 1.33 → 1.30 | 8.01 → 8.33 | 73 → 225 | 5.2 → 6.6 |
| reference world, looked at whole | 5.68 → 5.65 | 19.34 → 19.19 | 64 → 140 | 4–5 → 4 |

- Ink costs video memory and about a second and a half on the planet's first frame, and no frame time worth the name. The reference world
  missing 60 fps on the integrated card (52) is the lights', the same in both.
- The ship's atlas is 2048×5792, under the 8192 we had to stay within; 512×784 in pixels.
- The release build is 202 MB in all, of which `art/hd` is 18 MB of PNG before import.

## What the switch found

The title's plate was drawn pixel by pixel in code (`PlateArt`), so no list of pictures held it, and it was the first thing a player would see.
It is drawn in ink now (`title_plate`), on `PlateArt`'s size and its seat, lamp and anchor; `PlateArt` draws it only under `--pixel-art`.

## Deliberately not done

- **Not seen in play:** the user called the switch without the plan's walk of a surface and a cave being written up, and these were never framed: a fitted
  tool, the bench's glows, the seated pod, dark homes, the wreck's crack shader, the Air and Power build cards, the pipe's card, a hatch opening, a door's ghost, the
  tech tree's strip. A wrong one is a small fix, not a reason to wait.
- **No mid-range card measured:** the two ends above bracket it, and the frame time barely moves on either.
- **No download budget:** none was ever set, and 18 MB of a 202 MB build does not call for one.
