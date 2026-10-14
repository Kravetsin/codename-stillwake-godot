# 0026 — Light from emitters, by cells

- Status: proposed — an experiment, switchable live against the current Godot lights
- Date: 2026-10-14
- Compared with: [0019](0019-light-by-cells-in-colour.md) (the TypeScript game's light) and the
  Godot lights of `game/features/light` today

## Context

The Godot port lights the ship with `PointLight2D`s: sharp shadows from walls and open doors, and
an even, well-shaded ship. The TypeScript game lit it cell by cell (0019), and its furnace felt
better: sharp, crackling, embers lighting the cells they pass. Each light here is also tuned by
hand — a texture, a reach, a falloff, a cap — so giving something new light means designing a new
light. The team wants light rules that are easy to edit: put a layer that emits on a thing — a stick
the hero holds — and it lights correctly, only its strength or colour to choose. Only some things
emit: the fire in a furnace's mouth and its embers, a lamp's bulb, the stars through holes.

## Decision

A second lighting mode, **cells**, beside the current one, **godot**. One of the two lights the
world at a time; `--light=cells|godot` picks it at the start (godot by default), F9 or the dev
panel's `light:` button switches it while playing. Nothing of the godot mode is removed.

- **Emitters.** `render/Emitter.cs` is a node: a child of a `Sprite2D`, it makes the sprite's
  glowing pixels give off light as the sprite is drawn now (moved, flipped, its picture changed); a
  picture drawn otherwise (a tile) is given as its `Picture`, laid from the emitter's place. A pixel
  glows when it is opaque and at least as bright as its kind's `Glowing` (`EmitterMask`). Its
  `Strength` is how brightly it shines now: a lamp's power and flicker, a fire's blaze.
- **One table** (`Light.Emits` in `game/Content/Light.cs`): lamp, fire, ember, spark, star, each a
  colour, a strength and `Glowing`; the TypeScript values. Beside it what a cell keeps of light
  crossing it: `Air`, `Machine`, `WallIn`/`WallDeep`/`WallOut`, and `DoorIn` for a closed door.
- **Each frame** (`features/light/CellLight.cs`), over the screen and `Light.Margin` cells round it:
  1. every cell read as air, machine, wall or closed door, and the stars put in every open cell with
     no back wall and past the grid's edge;
  2. every emitter's glowing pixels placed in the world and counted a cell: the cell holding most
     of them gets the kind's colour × strength × `Strength`, each other cell its share by its count —
     so an emitter's light does not depend on how many of its pixels glow, and does not halve as it
     moves across a cell's edge;
  3. each glowing particle (ember, spark) gives its colour × strength × its fading alpha to its cell;
  4. the TypeScript sweeps (`Sweeps.Spread`, the one Starlight uses too): rows and columns both ways,
     `Light.Rounds` times, each colour the brightest way;
  5. a float texture of a texel a cell, its alpha marking cells that block light.
- **Drawn** by `render/Lit.cs`'s material, in `fragment()`: blended between the four cells round a
  point, those of its own side only (walls with walls, all else with the open), then
  `min(1, ambient + light)` per colour, as the TypeScript game did. A normal-mapped picture is
  shaded toward where the field grows brighter: the slope of its brightness half a cell either way,
  as steep as a lamp's falloff in air giving light from that side 45 degrees up (as a lamp's in the
  godot mode), flat light coming from straight in front; a flat surface takes the field's colour
  whole either way.
- **The modes never both light.** In cells the `PointLight2D`s of lamps, fires and particles and
  the starlight's directional light are hidden (not freed), and the fire's additive glow with them
  (the TypeScript game had none); the lamps' glows stay. In godot the material ignores the cells'
  texture.

## Adding an emitter: the stick

1. A kind in `Light.Emits`, say
   `public static readonly Emission Torch = new((1f, 0.6f, 0.2f), 1.2f, Glowing: 0.5f);`.
2. On the sprite that draws the stick: `stick.AddChild(new Emitter(L.Emits.Torch));` (`L` for
   `Stillwake.Game.Light`, as the features alias it).
   The pixels of its picture at least half bright now light the cells they are drawn in, following
   the stick as the hand moves and flips it.
3. To make it flicker or burn down, set the emitter's `Strength` each frame.

No texture, reach, falloff or shadow is designed: the sweeps give every emitter the same look.

## Trade-offs

- **Soft cell shadows, not sharp ones.** Light spreads cell by cell and round corners as far as its
  rounds take it: an open door lets a soft spill into a dark room, where Godot's lights throw a
  sharp wedge through it. Nothing smaller than a cell casts a shadow; heroes cast none.
- **Per colour at whole,** as the TypeScript game: a hero in orange by a lamp turns yellowish where
  the light passes whole; the godot mode scales the colours together to keep their hue.
- **A fire lights its own furnace and the door beside it fully,** and its crackle shows as the
  whole room's light jumping, as in the TypeScript game; the godot mode's fire pools more steadily.
- **Cost,** release build, RTX 4060 Ti, measured with `--measure` (`cells=` average/worst ms of the
  work each frame, allocation once its buffers fit the screen):
  | world, view                                  | cells     | per frame          | allocated |
  | -------------------------------------------- | --------- | ------------------ | --------- |
  | wreck with the furnace, zoom 2               | 3,888     | 0.08 ms            | 0 B       |
  | reference world, zoom 1 (the farthest a player zooms) | 11,868 | 0.23 ms     | 0 B       |
  | reference world whole on screen, zoom 0.31   | 60,564    | 1.23 ms (2.6 worst) | 0 B      |
  The development build Godot runs from `--path game` is unoptimised and about four times slower
  (4.4 ms whole). The float texture is uploaded each frame: 16 bytes a cell, about 1 MB whole.
- **Only what has an emitter gives light.** A lamp's light comes from its bulb's pixels, a fire's
  from its flames; a picture drawn with no sprite (the lamps' tiles) needs an emitter placed by its
  feature.
- **Not handled:** shaded items not drawn with `Lit.Set` lose the godot lamps' light in the cells
  mode (not audited); a rotated normal-mapped picture is shaded as if unrotated.

## Consequences

- The team compares the two in the same build and keeps one; the loser's code goes, and this ADR is
  accepted or rejected.
- `Sweeps` is shared: starlight in the godot mode and the cells mode spread light the same way.
