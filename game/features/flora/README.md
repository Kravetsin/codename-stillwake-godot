# Flora (client)

What grows on a planet, seen: on its surface (0079) and in its caves (0080). The plants are Building's
(`sim/Building/Flora.cs`): grown from the seed with the ground, felled by a hit or with what holds them. This feature only
draws them — the surface's in `FloraView`, the caves' in `CaveView` — and tints the caves' stone by depth (`CaveTint`).

- **Reads:** `BuildingModule.Plants` (each `Plant(Cell, Kind)`, ascending, only ever fewer) and, for the motes,
  `IGridQueries.StructureAt` and `BackAt` down the columns in view.
- **Draws** (`FloraView.cs`): a `Sprite2D` per plant standing — a few hundred on the first planet, so no tile layer —
  lit as the world is (`Lit`), at `Layers.Flora`, over the ground and its moss, behind the heroes. Its picture is
  `art/planet/{kind}_{variant}`, the variant a hash of its cell, its foot in the middle of the picture's bottom row,
  `FloraLooks.Of[kind].Sink` pixels under the top of the soil it grows from. What of it glows
  (`{kind}_{variant}_glow`: seed pods, berries, a bell, a rock's mica, a tuft's pale tip, each with its halo) is a second
  sprite added over it unlit, at `Layers.Glows`.
- **Pictures:** drawn in the "Stillwake" style, exported from the concept's own drawing code (the surface style
  studies' panel 9): three acacias with hanging fronds and seed pods, two bushes of berries, two rocks, bells in teal,
  violet and amber, three tufts.
- **Light:** each glow picture's brightest six-pixel squares, at most `FloraLooks.MostLights` (3) a plant, are where it
  lights from, in their colour (`Lights`, read by the light feature each frame for the plants near the view; light
  README).
- **Felled:** a plant gone from `Plants` since the last look is taken away in a fall of `FloraLooks.Fallen` leaves (a
  rock's pebbles), matter falling as on a planet, behind the walls (`Luminous.Low`). Only the count is compared each
  frame; the list is walked only when it changed.
- **Motes** (0075's luminous toolkit): while a glowing plant is in view, `FloraLooks.MotesASecond` (7) fine soft dots
  are made, at most `MostMotes` (40) alive: `NearShare` of them by a glowing part, in its colour, the rest pale gold over
  the ground in the open air of a column in view (its first wall down, under no back wall). They drift up slowly,
  curling, and fade over 3.5–7 s; they give no light. None is made off the screen, and none while no glowing plant is
  in view.

## Deliberately not handled

- Swaying, wind, seasons: the pictures stand still.
- A plant's light is a small lamp's in daylight too, where it hardly shows; it is for the shade and the evening that
  does not come yet.

## Caves (0080)

Direction C of the cave concept — light that changes with depth — with direction A's glow moss and alien threads in the
garden: cold blue near the surface, teal and violet in the middle, warm amber and red near the bottom.

- **Reads:** `BuildingModule.Plants` of a cave kind (`Content.Flora.Cave`), `IGridQueries.StructureAt`, `ShapeAt` and
  `ChangedSince`, `Terrain.BandOf` for a growth's band, and where each helmet lamp shines (`HeroView.HeadLamps`, as the light
  takes it).
- **Draws** (`CaveView.cs`) by chunks of 32 × 32 cells, those within a chunk of the view, let go past three: a chunk is two
  canvas items, its pictures lit (`Lit`, at `Layers.Flora`, over the ground) and their glows added unlit (at `Layers.Glows`),
  each from one atlas packed at the first look of every cave picture (`art/caves`), so a chunk is a batch for each. Some
  thirty-eight thousand growths on the first planet, no node apiece. A floor's growth stands its picture's bottom row in the first
  row of the wall under it, sunk to the middle of a slope; a ceiling's hangs from the last row of the wall over it; one in
  the rock is 24 × 24 round its cell. A picture is `{kind}_{variant}`, the variant a hash of the cell; a stalactite's and a
  stalagmite's `{kind}_{band}_{variant}`, in its band's stone; moss one picture a face of its stone open now — `top` tufts,
  `bottom` strands, `left`, `right` a film — green in the cold band (`mossGreen`), teal deeper. A felled growth's chunk is
  drawn again, a glowing one puffing spores, a spike its pebbles; a grid change draws again the chunks round it, for moss.
- **Pictures:** cut from the cave concept's own drawing code (`objects.js`: glowcaps, crystals, spikes, threads, roots,
  hanging caps, its geode, ammonite and ribs, ore veins, heat cracks, moss) by the export beside it, as the surface's were:
  each glowing pixel's bloom, tight, in `_glow`. Change the drawing code and export again; never edit the pictures.
- **Light** (`CaveLooks`): each glowing kind's one light, from its glow's middle, in its colour — teal, violet, ice-blue,
  amber, rose, ember, green near the surface — at its strength and keep out to its reach (5 cells for a cap or a crystal,
  3 for moss); moss from three in ten, roots and heat cracks from a share too. Only those of the chunks near the view, the
  nearest its middle first, at most `CaveLooks.MostLights` (56), go to the light (`LightView.Caves`).
- **Glints:** mica, veins, crystals and geodes sparkle where a helmet's beam falls on them, nothing between (walked a few
  pixels at a time through the grid's walls): a white point and a cross of its colour, twinkling, as bright as the beam is
  there. Mica draws nothing else.
- **Drift** (0075's toolkit): spores up from glowcaps, drips from one stalactite in three, faint embers up from the heat's
  cracks — `MotesASecond` (9) from the sources in view, at most `MostMotes` (48) alive, none off the screen.
- **Tint** (`CaveTint.cs`): one band of colour per depth band, multiplied over all that is drawn under `Layers.Edges` in its
  rows — walls, edges, back walls — and nothing over it, so the growths and the light keep their colours: cool blue-grey,
  as it is, warmer, red-brown, blended from each band's middle to the next, from `TintFrom` (40) rows under the surface's
  middle. A quad a band across the view, drawn again when the view moves.
- **Cost** (`--ground=hall --measure`, the first planet's lushest garden hall, RTX 4060 Ti, 1280 × 720): 804–827 fps at
  zoom 1 against 868–875 with no cave growth drawn (render CPU 0.39 ms against 0.37, 82 draws against 75), 924 at zoom 2
  against 965–980; the view 0.05–0.08 ms a frame, allocating nothing, 56 lights of it at zoom 1. Without the atlas it was
  772 draws and 630 fps.

### Deliberately not handled

- Moss grows only on whole stone, not on slopes or soil; on whichever of its faces are open now, so digging beside it
  shows more of it.
- A glowcap cluster on a slope is sunk to the slope's middle: its caps on the high side stand in the rock a little.
- The light of a growth past the 56 nearest the view's middle is left out; in a hall seen whole some far ones go dark.
- Drips do not land: they fall and fade.
