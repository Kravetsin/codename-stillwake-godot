# Design: Fog edges on planets

## Problem
Today a planet stops at a bedrock wall at each side of the grid, so the player walks into the end of the world, on the
surface and underground alike. The planet should feel endless, but the sim should stay a finite grid.

## Player experience
1. The hero walks toward a side, on the surface or along a tunnel. The land ahead fades into fog and the world's sound
   dulls.
2. Past the last of the ground the hero floats: no weight, no rising or sinking, only on along the way it was going,
   or back. The fog thickens over everything, the hero at last too.
3. At the band's middle the whole screen, sky included, is fog.
4. The fog thins. The hero floats out of the other side's fog high over the ground and falls to it.
5. Turned back before the middle, the hero floats back out at the height it went in, into its tunnel or onto its
   ground.

What goes wrong: in the fog an axe strikes nothing (`building.nothingToDig`: the band is open air and unbreakable back
wall), and a plan shows red (`grid.outsideZone`).

## Out of scope
- Wrapping in the sim, in any module, or aboard the ship.
- Creatures, stacks and thrown items crossing over. They stay on their side.
- Air in the fog: the suit breathes there as on the planet; crossing takes a few seconds.
- Markers, positional sound and the camera's lead taking the short way across the seam.
- Migrating planet saves. Planets are reached only from the dev panel until the drop pod's launch slice
  (2026-10-25-the-drop-pod); from then old planet saves are player saves, and that slice's ADR says what becomes of them.
  Terrain has changed under saves without migration before (0085, 0091).

## Scale
- World: two bands of `Ground.Fog` columns. No work per tick in the sim.
- Players: any number. Each hero glides and is moved alone.
- Frequency: about one crossing per hero every few minutes; a crossing is some 2 to 4 seconds of gliding.

## Approach
- **The ground: a hollow band** (sim, `Terrain.Generate`). In each of the `Ground.Fog` columns at either side, inside
  the full-height bedrock columns (`Ground.BedrockSide`): sky above the column's ground line, open air below it down to
  the core with a bedrock back wall (dark, as a cave is; unbreakable), the core's lava and bedrock as elsewhere. No
  landmark, ruin, vein, growth or hall, no cave growth facing it from the zone side, and no cave mouth within a band's
  width of it. `Layout.Zone` is the columns between the bands. (F-1 grew the bands solid bedrock; this hollows them.)
- **Existing rules keep the band untouched:** Grid refuses plans outside the zone (`grid.outsideZone`); a hit finds
  nothing to break in it, its back wall bedrock (`building.nothingToDig`), though a fan from it reaches the zone as
  from anywhere. Creatures find every column outside the zone solid (F-1, `CreaturesModule.Solid`): none goes in,
  none spawns there.
- **Nothing new in the sim API:** no command, query, event or saved field. A grown grid already saves its zone.
- **Who uses it:** every planet type now (`First` and `Small`). Later types get a band by setting `Fog`.
- **Client:**
  - **The glide:** while the hero's body centre is outside the zone, no gravity pulls it and its vertical speed is 0;
    Up, Down and jump do nothing; left and right pace it as on the ground (walk, run). It is drawn in the floating
    pose. Entering it lets go of the harpoon. A passed-out hero glides too, never falling into the band's depth.
  - **The move:** crossing a band's seam outward, the hero is moved by the period to the other band's seam, at a fixed
    height: `Ground.FogDrop` rows over that band's ground line beside the zone — grown from the seed, never changed by
    play, within a row of the zone's natural ground there — the speed kept, any harpoon let go, its cell reported at
    once. A seated hero (a drop pod's seat) is never moved. It glides on out of the band and falls when its body centre
    is back in the zone. `HeroView` moves its last step too and snaps the camera (`CameraFollow.Snap`).
  - **The fog:** a new `fog` feature, a screen layer over the whole world and the backdrop, the player's own hero and
    friends included. On each column it thickens from nothing some columns inside the zone to nearly full at the
    zone's side, so the hollow band is never seen clear, and full at the seam and out. The whole screen thickens too as
    the view centre goes from the zone's side to the seam, and at a move holds full for a moment before it eases.
  - **Sound:** while the hero is in a band, the World bus is muffled as floating in space muffles it.

## Determinism and co-op
The glide and the move are the client's own, like all hero movement (0002). The sim only learns the hero's cell from
its `Enter` reports. The zone is a whole number of columns taken from content. Other players' heroes already snap
when two samples are more than 6 cells apart (`Remote.Snap`). A player joining mid-session gets the zone with the ground.

## Trade-offs accepted
- **In by a tunnel, out from the sky:** whatever height the hero enters at, it leaves the far band at the drop height.
  Turning back just past a seam crosses again and leaves the first band at the drop height too, not into the tunnel.
- **Turn back before the seam, or stop on it:** no move; it floats back out at its own height.
- **A wall built at the zone side across the drop height** stops a hero gliding out: it breaks it from the band or
  turns back.
- **Crossing mid-action** (holding E, drilling, a hit in flight): the sim reads it against the old cell until the
  `Enter` sent that frame; it is refused or lands on the side left, nothing worse.
- **A stack dropped or thrown into a band** falls to its bottom, the core, and is lost until it expires. A hero who
  passes out in a band leaves its carried load there the same way: rare, accepted.
- **Load or join:** a hero stands at the spawn, never in a band.
- **Planets saved before F-1** keep their full-grid zone: no glide, no fog; walking off their ground into a band falls
  to the core. Regenerate them. Planets saved since have the narrow zone; their bands, never edited, regrow hollow.
  Landmarks keep their places, so nothing else moves under those saves.
- **A frame hitch on arrival** while the far side's chunks are drawn is hidden by the fog.
- **The cold:** the suit pays none within `Field.Margin` (128) of the zone, wider than any band (64 and 8).

## Tests
- Scenarios (sim): a band is sky above its ground line and open air with a bedrock back wall below, down to the core;
  no growth, ruin, landmark, vein or hall in it; the ground at its zone side is whole for a band's width; a hit and a
  plan there are refused; no command changes a band's cell; the zone kept across a load; no cold anywhere.
- Client logic (`game.tests`): gliding keeps the height and the horizontal pace, Up, Down and jump do nothing; a
  crossing moves by exactly the period and to the drop height, both ways; turning back before the seam or stopping
  on it does not move; arriving, the hero glides out and falls once back in the zone; a ship world, a test map with
  a zone and a planet whose zone is the whole grid neither glide nor move.

## Cost
- Changed: sim Core (`Terrain`, a small PR on F-1); client crew, sound; a new client feature, `fog`.
- Content: `Ground.Fog` (64 for `First`, 8 for `Small`), `Ground.FogDrop` (12 and 6 rows). Client looks: a fog colour
  per type, how far inside the zone the fog begins and how thick it is at the zone's side, the flash's hold and fade.
- Estimated diff: about 500 lines with tests.

## Decided with the user
- 64 columns per band on `First` (32 to the seam), 8 on `Small`.
- The fog band is open at every height: the hero glides through it weightless at the height it entered, cannot rise
  or sink, and drops out of the far fog from a fixed height over the ground. This answers the band underground.
- The whole screen thickens toward the seam, so the move never pops.
- The fog lies over everything in the world, the player's own hero and friends included.
