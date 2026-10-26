# Design: Fog edges on planets

## Problem
Today a planet stops at a bedrock wall at each side of the grid, so the player walks into the end of the world. The planet should feel endless, but the sim should stay a finite grid.

## Player experience
1. The hero walks toward a side. The land ahead fades into fog and the world's sound dulls.
2. Deeper in, the ground disappears, and at last the hero itself.
3. At the band's middle the whole screen, sky included, turns to fog for a moment.
4. The fog thins and the hero walks out of the other side's fog. It keeps its pace and its height over the ground.

What goes wrong: in the fog an axe strikes nothing (`building.nothingToDig`: the band is bedrock), and a plan shows red (`grid.outsideZone`).

## Out of scope
- Wrapping in the sim, in any module, or aboard the ship.
- Creatures, stacks and thrown items crossing over. They stay on their side.
- Going into the fog underground. Caves stop at the band, and the band's rock is bedrock.
- Markers, positional sound and the camera's lead taking the short way across the seam.
- Migrating planet saves. Planets are reached only from the dev panel until the drop pod's launch slice
  (2026-10-25-the-drop-pod); from then old planet saves are player saves, and that slice's ADR says what becomes of them.
  Terrain has changed under saves without migration before (0085, 0091).

## Scale
- World: two bands of `Ground.Fog` columns. No work per tick in the sim.
- Players: any number. Each hero is moved alone.
- Frequency: about one crossing per hero every few minutes.

## Approach
- **The rule lives in two places.** `Ground.Fog` sets the band width for each planet type. `Terrain.Generate` grows
  no caves, mouths, landmarks, spires, homes, veins or plants in a band, makes every solid cell of it bedrock (soil
  included), and sets `Layout.Zone` to the columns between the bands. `Ground.BedrockSide` stays: the outermost
  columns are still the full-height wall, now deep in the fog.
- **Existing rules do the rest; no module changes for them:**
  - Grid already refuses plans outside the zone (`grid.outsideZone`).
  - `Hew` already stops at an unbreakable wall without striking it, so a hit in the band is refused with
    `building.nothingToDig`.
- **The creatures' walker** treats columns outside the zone as solid, so creatures never walk into the fog and none
  can spawn there. This is the one sim module that changes.
- **Nothing new in the sim API:** no command, query, event or saved field. A grown grid already saves its zone.
- **Who uses it:** every planet type now (`First` and `Small`). Later types get a band by setting `Fog`.
- **Client:**
  - **The move:** the hero controller moves the hero when its body's middle crosses a band's middle going outward.
    It shifts by ±(Width − Fog) cells, which is the mirrored place at the middle. The feet keep their height over the
    highest ground under the body, the speed is kept, and any harpoon is let go. A seated hero (a drop pod's seat) or
    one passed out is never moved. `HeroView` shifts its last step by the same offset and snaps the camera
    (`CameraFollow.Snap`); the flash hides the lost lead.
  - **The fog:** a new `fog` feature, drawn with a shader on a node that follows the camera, over the world and the
    backdrop, the player's own hero included. On each screen column the fog is clear at the zone's side and full
    from the middle outward. It covers the open air down to the band's ground, so underground the side still looks
    like bedrock. Past the band's inner half the whole screen thickens too, full at the middle; on a move it stays
    full for `FlashMs`, then fades out over `FadeMs`.
  - **Sound:** while the hero is in a band, `SoundView` eases the World bus's low-pass to `MuffledHz` over
    `MuffleMs`, the space muffle. The volume and the Suit bus are left alone.

## Determinism and co-op
The move is the client's own, like all hero movement (0002). The sim only learns the new cell from the next `Enter`
report. The zone is a whole number of columns taken from content. Other players' heroes already snap when two
samples are more than 6 cells apart (`Remote.Snap`). A player joining mid-session gets the zone with the ground.

## Trade-offs accepted
- **Turn back before the middle, or stand on it:** no move; the crossing has to pass the middle outward.
- **Jump or fall across:** the hero arrives at the same height over the ground with the same speed. Under gravity
  the jetpack gives no thrust, so nothing flies higher than a jump.
- **Crossing mid-action** (holding E, drilling, a hit in flight): the sim reads it against the old cell until the
  next `Enter`; it is refused or lands on the side left, nothing worse.
- **A stack dropped or thrown in the band** lies on the band's ground on its side. Past the middle it cannot be
  reached and is lost until it expires.
- **Load in the fog:** reported cells lie on the inner halves of the bands, so the hero walks out. If rounding puts
  it past a middle, walking outward meets the bedrock wall as today.
- **Old dev saves** regrow with bands but keep their saved full-grid zone, so building in their fog works; spires
  and homes may move and old holes in band columns stay. Regenerate them.
- **A frame hitch on arrival** while the far side's chunks are drawn is hidden by the fog.
- **The cold:** the suit pays none within `Field.Margin` (128) of the zone, wider than any band (64 and 8).

## Tests
- Scenarios, over `First` and `Small`:
  - A hew into band ground is refused with `building.nothingToDig`.
  - A plan touching a band cell is refused with `grid.outsideZone`.
  - A walker turns back at the zone's side; nothing spawns in a band.
  - A band has no open cell below its ground, no ruin, vein or plant, and no overhang.
  - `EdgeCost` is 0 everywhere on the planet.
- Client logic tests (`game.tests`):
  - A crossing moves the hero by exactly the period, both ways.
  - Turning back before the middle, or standing on it, does not move the hero; a seated hero is never moved.
  - A hero in the air keeps its height over the ground and its speed.
- Invariant (FsCheck): fuzzed commands never change a cell outside the zone (Grid's README states it).

## Cost
- Changed modules: sim Core (`Terrain`), Creatures (walker); client crew, sound; a new client feature, `fog`.
- Content: `Ground.Fog` (64 for `First`, 8 for `Small`). Client looks: a fog colour for each type, `FlashMs`, `FadeMs`.
- Estimated diff: about 450 lines with tests.

## Decided with the user
- 64 columns per band on `First` (32 to walk before the move), 8 on `Small`.
- Past the band's inner half the whole screen thickens toward full as the hero nears the middle, so the flash
  never pops.
- The fog lies over everything in the world, friends included: a friend fades into one band and out of the other.
  The player's own hero too: the fog is a screen layer over the whole world (see the plan).
