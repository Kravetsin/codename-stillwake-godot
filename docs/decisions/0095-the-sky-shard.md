# 0095 — The sky shard: the arc relic, its spire a light well, a home's heart

- Status: accepted
- Date: 2026-10-21
- Design: the arc relic concept page the user picked on (seven relics, three spires, two homes, five gameplay questions)
- Amends: [0085](0085-ruins-on-the-first-planet.md) (the shrine and its arc coil are gone; the spire is redrawn),
  [0086](0086-modules-one-socket-for-machines-and-tools.md) (the arc module is made of a shard), [0091](0091-homes-of-the-arc-makers.md)
  (the hearth burns while the home's heart feeds it; the spire's chamber is furnished anew)

## Context

The spires' relic was a placeholder: an arc coil in a niche of the chamber's wall, which looked like something the makers had
wound themselves, and nothing said why it was worth more than anything the ship makes. The spire's shaft was black, its lamp a
blue dot no different from a star, and its ledges, three rows apart either side by turns, could be dropped down but hardly
climbed back up ("going up I hit my head every time"). The concept page drew seven alien relics, each in a cradle the makers built
round it, three spires built round what fell, and a chip of a relic in a home.

## Decision

- **The relic is the sky shard** (the page's design 1): black glass that does not melt, white cracks alight in it, held upright by
  a copper collar on three legs over porcelain. Its light is violet-white, the arc tech's mark. The line for the story, kept for the
  friend: "They did not invent the arc: it fell on their heads, and everything they built grew up around the shards"
  (`item.arcShard.lore`, the Russian beside it).
- **It splits into four shards** (`arcShard`, gear, a stack of 8): a spire's cradle (`Ruins.Cradle`, a piece three across and up
  on the chamber's floor, 8 hit points, four swings of the axe) gives four; a chip in a niche of the hearth home's wall
  (`Ruins.Niche`, 2 hit points) gives one. Each shard is an arc module: the module's craft takes one where it took the coil
  (`arcShard 1, coil 2, circuit 2, frame 1`). Analysed, a shard opens the module's blueprint, once, as research opens any craft
  (0027); it stays in the analyzer. `arcCoil` is gone, its item, picture and shrine; a dev save holding one may no longer load (accepted, as before).
- **Every hit on the cradle discharges into its striker** (the page's "only when you hit it"): the axe's hit `Ruins.CradleShock`
  (5) units of its suit, through `ICrewOps.Shock`, the resonator's path; a faster tool's hit its pace's share of the axe's, rounded
  up, the drill's 1 and the arc drill's 2, so a second of hitting costs 10 whatever strikes (as 0094 makes a second of hits look
  alike). The felling hit too. Untouched it only glows and hums; dug out from under, it falls for the digger unshocked.
- **Which relics are taken is the sim's, from the felled cells** (`BuildingModule.Relics`, `Relic(Cell, Kind, Taken)`, derived
  from `Felled`), so every peer, every load and a friend joining later agree which spires and homes still burn. No new state.
- **The spire is a light well with the shard's roots** (the user's mix: V2's well, V1's roots and the shard). The lens in its tip
  over a flue open down its middle column; a ladder from the room's floor down that column to the chamber, the light's own
  column, the ledges either side lit by it; the chamber five rows tall, the cradle under the ladder's foot, a cache, a table, a
  lamp over it, a shelf, the banner, a cobweb. While its relic stands it burns: the shard sparking (small arcs on twos, a reach
  to the chamber's metal now and then, a big discharge every 2.6–5.5 s re-lighting the room, sparks raining), the shaft lit up
  its column, the lens alight throwing a pillar of light 72 cells into the sky, the roots glowing in the rock round the chamber.
- **Taken, the spire goes dark** (the page's "after"): watched, the cradle sparks three seconds more, its arcs thinning, a last
  discharge into the ladder's foot, then everything goes out: pillar, shaft, lens, roots. From a hill one sees which spires are
  taken. Found taken (a load, a friend joining), it is dark at once. The empty cradle stays, drawn only.
- **The ladder is the way out.** Measured on the old shaft: from a ledge the next is three rows up, a held jump's six on the
  planet, but the ledge over the hero's head on the same side is six rows up, five open rows for a body 2.625 tall, so the feet
  rise 2.375, short of the ledge assist's third of a cell (0092). A ladder up the light column reads with the light well (its
  rungs in the beam) and climbs out whatever the ledges; the room's floor is closed but for the ladder's top, so the hero stands
  on it and walks out through a plug. A controller test climbs out of the spire as five seeds grow it.
- **A home's heart** (the page's home A): the chip in its niche by the hearth home's arch, a copper cable from it to the hearth
  and up and along the ceiling to the lamp over the table. Only those two burn, the lamp violet-white as the relic, the hearth's
  arcs violet; every other lamp of the home that may go out is dead, the common network long gone (`HomesLooks.Fed`, `MoodOf`).
  Taken, they go dark and the cable is blackened.
- **Seen and heard** (`game/features/ruins`: `SpiresView`, `RelicSparks`, `HomesView`): pictures exported from the page's own
  drawing code (`tools/homes-art/relic.js`), the page's live layer at 24 steps a second; the zap is the resonator's placeholder
  and the hum the charging station's synthesised one half again as high, till the user's audition.

## Rejected

- **The page's "relic → 4 chips at the assembler"**: the cradle gives the four itself; a craft splitting one item into four
  would be a station rule of its own for one use.
- **Shocking every drill hit for 5**: 50 a second, a suit gone in a second; the share keeps the user's 5 a swing.
- **Moving the ledges four rows apart** instead of a ladder: the body gets longer or the ledges fewer, the jump still needs the
  ledge's edge, and a ladder down the beam reads better with the well.
- **A cave light for the lens**: the caves' view bakes its pictures by chunk; the lens's glass is drawn dark there and its light
  laid over it by the spires' view, which knows when it goes out.

## Deliberately not handled

- **The empty cradle can't be hit**: it is drawn, no piece is left; its copper is not salvage.
- **A lens dug out of a standing spire** throws no pillar; its shaft and roots still burn.
- **Day and night**: the planet's sky is day; the pillar reads white-violet over it, not the page's night.
- **A friend sees a hit's arc into the nearest hero**, not necessarily the one who struck: the client is not told who struck.
- **More relics** (the page's other six) and **more homes with hearts**: one each; the shard's look is design 1 only.
- **Old dev saves** with an arc coil or a shrine felled lose them; the spire's new shape stamps where the old one stood.
