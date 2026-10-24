# power (client): cables as ropes

Cables hanging between their nodes and their overlay, shown while the stripper is held (0108), where they are laid, carried and cut
(0017, 0021, 0022, 0030), ported from `apps/game/src/features/power/` and `render/cable.ts` (G-08). The sim's
`sim/Power` decides: the client reads `Cables`, `Networks`, `NodeAt`, `NetworkOf`, `Satisfaction`,
`LayProblem` and `MoveProblem`, and sends `Lay`, `Cut` and `Move`.

## Files

| File            | Holds                                                                                  | Godot |
| --------------- | -------------------------------------------------------------------------------------- | ----- |
| `Wiring.cs`     | what a press takes hold of (`GripAt`), supports on the way (`SupportOnWay`), `LightOf` (`wiring.ts`) | no    |
| `CableRopes.cs` | every cable as ropes, a rope per piece, hanging aboard and floating outside; carried ends, landings and cables just laid (`ropes.ts`) | no    |
| `PowerHands.cs` | the hands with the stripper (0108), its menu's one card the cable (0110; peg moving stays on Ctrl+drag): the left lays and leads on through supports, with Ctrl carries a peg or plug, a right press near a cable cuts (`mode.ts`) | no    |
| `PowerView.cs`  | the cables, pegs, plugs, grommets and bolts; the overlay, its colours and the ghost    | yes   |
| `ChargerLights.cs` | each charging station's light and the battery in its bay (0053)                     | yes   |
| `StoreLights.cs` | each capacitor bank's gauge, its spark and its light (0101)                          | yes   |
| `StoreGauge.cs`  | how the gauge reads: segments lit by how full, the spark's way and place (tested)     | no    |
| `StoreWorld.cs`, `StoreDemo.cs` | the world of `-- --store` and its script: a bank filling, then emptying into an arc smelter | `StoreDemo` |

`game/render/CableShape.cs` (`render/cable.ts`: ends, hangs, pieces through the hull) and
`game/render/Rope.cs` (the tether's rope, now also started from given points and brushed by bodies)
are shared. Ported tests: `cable.test.ts`, `ropes.test.ts`, `wiring.test.ts`; beyond them (`CableRopesTests`), a cable
moving on every frame at 240 Hz, let go between two steps, and moved off a hull crossing.

## Guarantees

- **A charging station without a word** (`ChargerLights`, made by `Main.ShowWorld` with the registry):
  its light pulses green while it charges (`IPowerQueries.Charges`), shines steady once its battery is
  full on a network that makes power, and is dark empty or unpowered; a battery in its bay shows with
  its charge rising up it, amber under a quarter. Drawn unlit over its picture, the light lighting the
  room round it a little in the same green (a `Glow`), none while dark. Read every 100 ms; the pulse
  runs every frame. The picture is a placeholder (a cabinet, a light, a bay) until art is picked.

- **A capacitor bank without a word** (`StoreLights`, 0101): its gauge of six segments lit from the foot as far as it
  is full (`IPowerQueries.Store`), the top one as bright as it is full within it, the lowest never dark until empty;
  green, amber under a quarter as a battery's bar. A spark runs up the lit segments while it takes power in and down
  them while it gives out, every 0.9 s, none while it holds. Drawn unlit over its picture, it lights the room round it
  in its colour, more the fuller it is, none when empty (a `Glow`). Read every 100 ms. The power overlay's tip on a node
  of a network with stores adds what they hold of full under what it makes and draws; out of every overlay, with no card
  in hand, the pointer on a bank shows the one tooltip saying only "8 400 / 12 000 ↑" (`StoreGauge.Reading`: grouped as
  the wrist's energy tab, ↑ charging, ↓ giving out), read from `IPowerQueries.Store` as the world runs (`PowerView`).
- A cable is the same rope throughout: laid, it is the rope it was dragged as from the first press; a
  carried end is the real cable, highlighted white (red where it may not be let go); dropped on a node
  it waits there until the world moves it and goes on from where it hangs, never snapping back unless the
  move was refused (two seconds). It goes on from the line it was drawn as even where its pieces change:
  a cable through the hull moved to a node on it, or one laid from outside to a lamp inside, starts each
  new piece along its stretch of that line (`CableRopes.StartsOf`); the TypeScript snapped those to a
  fresh hang.
- A dragged cable runs to where it would end: a machine's socket, an anchor's middle.
- While a cable is carried, every powered machine's socket glows and is outlined along its own picture,
  a screen pixel wide (`render/Outline.cs`): green where the cable would plug in, grey out of reach,
  red where it may not.
- Cables sync with the world at most every 250 ms once it has run a tick; bolts, pegs, plugs and loose
  plugs are made anew only when what they show changes, and the ropes' lines and traces are refilled
  in kept buffers, so a frame allocates next to nothing.
- Ropes step 60 times a second, a step ahead of the clock, and are drawn between their last two steps
  (`Rope.DrawnAt`), so they move on every frame of a faster screen rather than hold still three frames
  in four at 240 Hz; they sway out of every hero's body: this one and the others where they are drawn
  (`HeroView.Others`).
