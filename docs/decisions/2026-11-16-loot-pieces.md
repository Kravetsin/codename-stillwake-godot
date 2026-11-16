# Loot lies as a few small objects that bounce and tip onto a side

- Status: accepted; built; supersedes in part [kill-feel](2026-10-29-kill-feel.md) (the floor's rim, bob and hops) and
  0075 (a stack's breathing rim)
- Date: 2026-11-16

## Context

A blasting charge dropped one stack per broken cell, some two hundred in soil, and each lay as a full 16 px icon in a
breathing white rim, about half the hero's height. The crater it opened was carpeted with icons, soil worth 1 as loud as
opal worth 150. The user asked for loot that looks good lying about, by the genre's canons, so a player wants to take it.
On a first pick page (https://claude.ai/artifact/D2YqV7X3SSkrHUUKcUCJu6) they rejected loot shown only near the hero: what
lies there must always be seen. They liked plain, realistic objects, and noted a player is mostly right beside the loot,
and asked for physics. A second page (https://claude.ai/artifact/WJHQJwAPoXSpmJGfuagtMq) showed objects that tumble,
bounce by material and lie at an angle; they picked "lie", pieces that never touch each other, and played it.

## Decision

- **A burst throws what it breaks in pieces** (sim, `Spoils.Drop`): all a burst breaks is gathered, item by item, and
  `Spray`ed from the burst's cell in at most `Floor.BurstPieces` (14) pieces, every item at least one, by `SprayRules` as a
  kill's loot. A machine's contents still spill at the machine.
- **Stacks bounce** (sim, `FloorRules.Move`): off a floor, ceiling or wall a stack keeps `Floor.BounceOf(item)` thousandths of
  its speed, turned back, rounded toward none, and stops below `BounceLeast`. Soil and what grows keep none, metal 400,
  crystal 550, anything unnamed stone's 250. In integers, so every peer sees the same bounces and a stack is taken where it
  is drawn. Afloat, nothing changes: a stack flies on.
- **A stack is one to three small objects** (client, `LootLooks`): its icon at 0.7, one object, two from 2, three from 10, the
  third laid over two. No rim, no bob. Flying or afloat they turn about their middle; a bounce clinks; landing squashes and
  each object tips, over 180 ms, onto the side of its picture's hull nearest how it landed, a side at least half the longest
  with its middle over it (`LootPose`). Within 4 cells of the hero's body a thin warm outline fades in. Seeds, quartz,
  crystal, opal and arc shards glint.
- **No physics engine.** The pick page used one; in the game the sim's flight, fall, bounce and slide stand in for it, and
  the client only turns pictures. A hull is taken once a picture, from the silhouette walk made while the game loads.

## Consequences

- A blast leaves about a dozen stacks rather than two hundred: the floor costs every peer less each tick.
- Pieces may lie over each other: they do not collide, by the pick.
- Bouncing changes every stack's landing, not only a blast's; a drop thrown by a hero bounces too.

## Deliberately not handled

- No dust from soil nor sparks from ore on landing: the pick page showed them; left for a play pass.
- Nothing bounces in space, where a stack flies on as before.
- A stack merged in a cell, or grown past 2 or 10, is drawn anew and picks new angles: rare, and a still pile.
