# Flora (client)

What grows on a planet's surface, seen (0079). The plants are Building's (`sim/Building/Flora.cs`): grown from the
seed with the ground, felled by a hit or with their ground. This feature only draws them.

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
- Motes in caves, by the moss there: caves have no plants yet.
