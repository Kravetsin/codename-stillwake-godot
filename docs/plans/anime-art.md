# Moving the game to the new art

The look: [ink and watercolour](../decisions/2026-11-01-ink-and-watercolour.md), its rules in
[the tool's README](../../tools/anime-art/README.md#the-style). The plumbing and its costs: [anime cel art](../decisions/2026-11-01-anime-cel-art.md).
This is the way there: 905 pictures, in families, each landing in its own PR, the game never shown half and half to a player.

## Where it stands

Landed, off by default: the plumbing (`HiRes`, the atlas), `tools/anime-art/`, the `--anime` demos, and 385 hi-res pictures:
the testbed room whole, the hero's first look, ten icons, a stone and soil ground, ice crystals, moss that failed. All of them are
now drawn in ink and watercolour; scrap has its first rust.

## How it lands

**Hi-res stays off by default until the switch.** `main` keeps playing in pixels; `-- --anime-art` turns the new art on for us.
Off, the atlas stays at its old size too, so a pixel player pays nothing. A family lands as soon as it is done, branches stay
short, and neither developer sits on a long-lived branch. The switch is one PR at the end.

**A new picture while this runs** is still drawn in pixels, as today: `HiRes.Load` finds no hi-res one and the pixels show. A
script lists the pictures with no `art/hd/` file, and steps 5 and 6 sweep them. Pixel machines look chunky under the flag until
then; only we see that, and nothing ships before the switch.

**Picks.** Not 905 of them. The user picks on an Artifact what is a new look: the hero's looks and each family of nature. Icons,
walls and machines follow the accepted style by palette and kit, and a contact sheet and playing them are the check.

## Steps

Each is one PR, with frames near and far beside the same frame without the flag, read by eye.

### 1. Land the slice

Done: the flag is `--anime-art`, the plumbing off without it, the game drawn as it was. No more art. If the probe fails, reverting
the plumbing and `art/hd/` is cheap.

**Still to do: measure a mid-range card** with the flag on at zoom 2.5, before more is drawn: `Scale` and the atlas are fixed before 900
pictures rest on them, and the planet and the rest of the ship will grow the atlas. Report its size against 8192 too, and the load
time, for every picture is now mipmapped at startup. A miss here changes the plan; at the switch it would redraw everything.

### 2. Nature: the probe the switch waits on

The slice proved the ship and not the planet, which is about a third of the pictures and the hardest of them. The user's notes
from playing it are this step's list:

- **Grass.** Done for soil: turf over the block's top and thick tufts (`top_soil`), accepted from a frame. The flora's own tufts are
  still pixels, and the look on other planet types is open.
- **Back walls** must stay dark and quiet behind what stands before them. Darkened on the ship, stone and soil; the stone and soil
  ones are checked in frames only.
- **Scrap is too clean.** The pixel hull is rusty and battered; this one has first rust at its seams. Bring more damage back, and the
  wreck's dress (`wreck_dress`, still pixels) with it.
- **Moss.** Stretching one logical pixel along a run cannot carry this style. Draw the run in pieces instead, which changes
  `CaveView`'s `Band`.
- **The stone's pattern** was stained glass. Done: large masses of close tone, only some cracks drawn, accepted by the user.

The acacia is drawn (`flora`); the far ones in the sky are still pixels.

**Creatures:** the loper, the tapir and the sixleg are drawn, [as parts on the same rig](../decisions/2026-11-01-creatures-in-ink.md). The
sentry and the warden are the hard ones: `PosedSprite` paints them on the CPU every frame, and at 4 times that
is 16 times the pixels. Decide for them: parts as the rest, or a fixed set of poses.

**Exit:** the user walks a planet's surface and a cave with the flag on and accepts them. If nature does not hold after this step,
stop and decide again before drawing more: the ship in cel beside pixel nature is not a game we ship.

### 3. The hero whole

Every hair, face and beard of the look editor (only the first of each is drawn; the rest show as pixels on the new head, seen in
`--store`), the jetpack, the cape, the sheet and its screen, every held tool. These load past `HiRes` by path (`Figure`, `CapeView`,
`HeldLook`, `HeadLampArt`, `WornPicture`). `HeldArt` blends a tool's fitted modules into its pixels: without this a tool would turn
pixel once a module is on. Drop amber from the player's colours ([the mark](../decisions/2026-11-01-player-colour-mark.md)), a check
of each colour against the suit.

### 4. Icons

The 78 items and the tool icons' baking. Most reused by the ship's kit; a contact sheet at 32 and 48 px is the check.

### 5. The ship, the rest

Every material's walls by palette; machines not in the testbed; the glows still pixels (`arcBench_glow`, `fit_glow`,
`machine_arcLamp_glow`, the pod's porthole). **For each machine, list what the client draws over it at fixed pixels first** and keep
those spots: the capacitor bank's gauge moved in the slice. Far repetition of plates along a long run.

### 6. The planet, the rest

All planet types' grounds and flora, caves' roots, threads, stalactites and glowcaps, creatures, homes, ruins, debris. Debris and
far wrecks load past `HiRes` by path (`DebrisLooks`, `FarWrecks`), creatures too (`CreatureLooks`).

### 7. The switch

The interface's frames, which are pixels still (`Tablet`, `WristPieces` count nine-patch margins in logical pixels, `MarkerLayer`);
a mid-range card measured again against the 60 fps budget, video memory, load time and the size of the download (`art/hd` ships
lossless, by `export_presets.cfg`); the flag's default turned. Small enough to revert.

A follow-up then removes the pixel art and what only it needs (ramps, `Halved`), logical pictures made by reducing.

## Size

Two agent passes took about an hour for the slice's 385 pictures, generators flattering that. The whole is a guess of 15 to 25 such
passes, each with the user's eye after it. Steps 2 and 5 will show how far the guess is off.

## Deliberately not here

- A shader to repaint suits: the mark replaces it.
- A style for each planet type beyond its palette, till one is asked for.
- Tuning for 4K.
