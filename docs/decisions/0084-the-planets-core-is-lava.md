# 0084 — The planet's core is lava

- Status: accepted
- Date: 2026-10-21
- Amends: [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md)'s bedrock rows

## Context

A planet's ground (0077) ended in three rows of bedrock: a grey floor at the bottom of the deepest caves, nothing to find
and nothing to fear. The user wanted the planet's bottom to be its core, lava, glowing and deadly to touch, so the
deepest caves end somewhere worth seeing and dangerous to fall into.

## Decision

- **Lava is a natural material** (`Materials`, code 6, `Content.Lava.Material`): a wall like bedrock, unbreakable and
  never built, so digging it is refused as bedrock's is (`building.bedrock`, "Too hard") and no blueprint places it. The
  ground's bottom rows are its core, `Ground.CoreRows`: `LavaRows` (2) of lava over `BedrockRows` (1) of bedrock, the
  lava between the bedrock's columns at the sides, which stay bedrock the whole height. Three rows as before, so every
  seed's caves, landmarks and depth bands are as they were. Nothing grows on lava: the caves' floor growth over the core
  is no longer set (0080's ember crystals there).
  - *Rejected: lava as open cells a hero sinks into.* It would read more like liquid, but open cells along the whole
    bottom join every cave into one, grow floor growth on the bedrock under them, and need new rules in movement, rooms
    and the light. A solid band needs none: a hero stands on it.
- **Touching it burns the suit** (crew): at every breath, a hero whose reported cell has lava in it or up to
  `Lava.Reach` (2) rows under it — standing on it, its middle two rows over its feet — pays `Lava.Burn` (15) units more,
  the suit alone, as the cold past the field (0050) is paid. With the breath, a full suit (45) is gone on the third breath:
  two to three seconds from the breath after the sim hears of the hero there. Passing out is death as anywhere (0061): the
  body falls where the hero was reported, and off the ship it wakes aboard, at the pod. Creative mode keeps the suit full.
- **It reaches the sim as every hero position does:** the reported cell (`Enter`, 0050), at most a second behind the body.
  No new command, no physics on the host.
- **It glows and gives light** (client, `LavaView`): its tiles are drawn as any ground's, and over them its band is drawn
  unlit by a shader, one quad its length (the GPU clips it to the view): `art/planet/wall_lava` sampled twice, two sheets of
  dark crust drifting apart over molten cracks, bent by slow waves, each patch breathing, the surface a white-hot row of
  pixels fading to orange. Where the open is over its surface it lights the cave in deep orange: a lamp every
  `LavaLooks.LightEvery` (3) cells of an open stretch, each breathing a little, at most `LavaLooks.MostLights`, through the
  light feature's sources (`LightView.Lava`), found each frame along the view's columns only. Lava under rock glows too,
  in the cut the side view shows, lighting nothing.
- **Its pictures are placeholders,** made by a script (dark crust plates, molten cracks between them, a few sparks over
  its surface), till an audition picks its art.

## Measured

On this machine (RTX 4060 Ti), 1280 × 720, `--ground=lava --measure` (beside open lava at the first planet's bottom):
743 fps, render CPU 0.43 ms, 96 draws, 8 lava lights, 29 cave lights.

## Deliberately not handled

- **Lava does not flow, spread or fill a hole**, and nothing falls into it: it is a band of walls.
- **No warning before the burn but the suit's gauge and the glow:** no heat haze, sound or screen tint yet.
- **"Too hard" on lava,** the bedrock's refusal: a refusal of its own when lava does more than burn.
- **A hero reported a second late burns a second late**, and one jumping more than half a cell over it burns not at all:
  the reported cell is the body's middle (0050).
- **Lava under rock lights nothing:** a light inside a wall reaches no open cell; the rock over it stays dark.
- **Items dropped on lava stay there,** a body included, where a friend walking to it burns as well.
