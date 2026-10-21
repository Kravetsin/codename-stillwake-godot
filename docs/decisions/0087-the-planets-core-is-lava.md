# 0087 — The planet's core is lava

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
- **It looks as the cave concepts drew it** (the core of the depth columns the user picked, 0080's page; client,
  `LavaView`): over its tiles its band is drawn unlit by a shader, one quad its length that the GPU clips to the view, with
  the concept's own recipe — value noise in 6-pixel cells for its patches, cracks where a 9-pixel noise crosses its middle — in
  the colours its page shows lit: deep orange, orange and gold patches, pale yellow cracks, a white-hot surface two pixels
  deep. The concept is a still; in the game the patches churn slowly along, the cracks drift the other way and open and
  close, each patch breathing a little. Over open lava a warm haze rises a cell (the concept's bloom, an added quad) and
  embers drift up, scattered at random along it (the caves' own embers). Its tiles (`art/planet/*_lava`) are the same
  recipe, still, made by a script; it has no living edge.
- **Open lava glows; buried lava shows its crust.** Whether the open is over each column of its surface is a texel of a row
  texture, laid again when the grid changes, blended across a cell: under rock the band is the concept's dark unlit colours,
  its cracks alone glowing dimly through, so open lava reads at a glance.
- **It gives light where the open is over it:** a deep-orange lamp every `LavaLooks.LightEvery` (3) cells of an open
  stretch, reaching 7 cells and holding well (the concept's), each breathing a little, at most `LavaLooks.MostLights`,
  through the light feature's sources (`LightView.Lava`), found each frame along the view's columns only.

## Measured

On this machine (RTX 4060 Ti), 1280 × 720, `--ground=lava --measure` (beside open lava at the first planet's bottom):
665 fps, render CPU 0.47 ms, GPU 0.24 ms, 97 draws, 7 lava lights, 29 cave lights (743 fps and 0.43 ms with the
first, sampled band, no haze nor embers).

## Deliberately not handled

- **Lava does not flow, spread or fill a hole**, and nothing falls into it: it is a band of walls.
- **No warning before the burn but the suit's gauge, the glow and the haze:** no sound or screen tint yet.
- **"Too hard" on lava,** the bedrock's refusal: a refusal of its own when lava does more than burn.
- **A hero reported a second late burns a second late**, and one jumping more than half a cell over it burns not at all:
  the reported cell is the body's middle (0050).
- **Lava under rock lights nothing:** its crust glows dimly, a light inside a wall reaches no open cell.
- **Its look is ported, not its picture:** the concept's bloom, the light it adds over its colours, is a haze a cell high and
  the lamps; its noise is the GPU's own hash, so its patches are like the concept's, not the same ones.
- **Items dropped on lava stay there,** a body included, where a friend walking to it burns as well.
