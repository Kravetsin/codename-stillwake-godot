# logistics (client): the drone badge and drones on screen

Drones ([0062](../../../docs/decisions/0062-drones-keep-machines-going.md)), the client of `sim/Logistics`.
It reads `LogisticsModule` (`Served`, `Servable`, `Reached`, `Drones`, `Low`, `ZoneOf`), `IItemsQueries` (`Crate`),
`IGridQueries` (`MachinesOf`, `MachineById`, `StructureAt`) and `IPowerQueries` (`NodeAt`, `NetworkOf`, `Charges`), and changes the sim only by sending `SetServed`. Its badge lives in
other features' frames, wired by `Main`: in the header of every machine frame the items feature opens
(`InventoryPanel.Served`) and of a queued station's (`FabricatorPanel.Served`), where drones have work. The bay's four
slots, their charge bars and its frame's bolt are the items feature's. The mockup it follows: "Drones Keep It Going"; the look
and motion are the user's picks from "Drone Look Concepts": drone A (the steel quad), bay C (the perch), motion "lively"
(`game/Content/DroneLook.cs`).

## Files

| File               | Holds                                                                                         | Godot |
| ------------------ | --------------------------------------------------------------------------------------------- | ----- |
| `Served.cs`        | the badge's look and tooltip keys; this peer's own `SetServed` in flight shown at once (0014); where a drone is between ticks; a bay's slots that came to hold a drone or stopped; whether a clamp worked | no |
| `Hover.cs`         | a drone's motion as drawn: the spring after the sim, its lean, face, bob, rotors and swinging cargo; its lift; a landing | no |
| `Whir.cs`          | a drone's rotors heard: its motor, and from it, its speed and its id, its loop's pitch and volume | no |
| `ServedBadge.cs`   | a machine's drone badge, at the right of its frame's header: shows and toggles whether drones serve it | yes |
| `LogisticsView.cs` | in the world: drones in flight with their cargo and their lamps' light, taking off and landing; docked drones, their pads' strips, charge lamps and light; a bay's beacon; a bay's zone; drones heard | yes |

`Served.cs`, `Hover.cs` and `Whir.cs` are compiled again into `game.tests/` (`ServedTests`, `HoverTests`, `DroneSoundTests`). The testbed (`-- --testbed`) runs two drones from a
powered bay in room one through the closed door, keeping the furnace, the Former and the grow capsule going from the
supply crate of twenty stacks of chunks by the pod, the farm's crate switched off, for ten minutes in air three
generators keep (`TestbedTests`); `--open=droneBay` shows the bay's frame and zone
(`--open-at 130` once a drone is home charging), `--open=crate@2` the farm crate's crossed badge, `--near=furnace`
starts the hero in the middle of the room, and `--card=droneBay --pick` holds a bay's blueprint over the ship.

## Guarantees

- **The badge** (the mockup): a drone's icon in a small box, 22 by 18, at the right of the header. Lit, sky behind
  it, while drones serve the machine and a bay reaches it; dim and crossed while switched off; grey where no bay
  reaches. Shown only where drones ever have work (`Servable`): crates, fed stations, a fire's machine, queued stations;
  not on a bay, the analyzer or the charging station. A click sends `SetServed` with the other state. The one tooltip
  says the state ("Drones keep this going", "Drones leave this alone", "No drone bay reaches this", and both when
  switched off out of reach) and what a click does. Read every 100 ms and at once after its own command, showing its
  own switch in flight before its tick.
- **A remembered input** (`IItemsQueries.KeptAt`, while drones serve the machine; `InventoryPanel.Kept`, set by
  `Main`): a small sky dot in its top-left corner, lit a little; empty, a faint ghost of the item's own picture. Its
  tooltip, empty, says "Drones keep this full: X", and "none in reach" in red when no crate or output a bay reaches holds
  it (`Supplied`, `InventoryPanel.Supplied`). The slot box draws both (`SlotBox.Kept`).
- **The bay:** its frame shows four slots, each a drone's faint silhouette while empty, its tooltip "put a drone here
  to fly"; a docked drone's slot draws its charge bar as a battery's does (green, amber under a quarter); the header's
  bolt shows what it draws, lit while it charges a drone (`Power.Charges`). In the world the bay is the perch
  (`machine_droneBay.png`): a docked drone sits folded on its pad (`drone_dock`, lit as machines are), its eye unlit over
  it unless it is flat. Under each, unlit: its four charge lamps on the shelf's edge by quarters of its charge, the next
  blinking in while it charges, one amber under a quarter (`Charges.Share`); while the bay has power (a network that
  makes some) its strip (`droneBay_pad`), pulsing 0.8 times a second while it charges and steady once full, and a little
  light round it, green, amber under a quarter. The beacon on the perch's top shines sky while the bay has power,
  lighting round it a little. Slots are read every frame. Its zone, the sim's `ZoneOf`, is outlined dashed sky over a
  faint fill while its frame is open; with a bay's card in hand every bay's zone is outlined and the one the card would
  make under the pointer, clipped at the grid as the sim's. A drone with no charge shows `drone_empty`, its lens dark.
- **Drones in flight:** each its picture (`drone_fly_0..3`, the rotors' four frames), lit with its normal map so the side
  toward a lamp is brighter (`PixelArt.Lit`), its eye and status lamp unlit over it, turned with it. It follows a target
  on a spring (`Hover`, `DroneLook.Lively`: 3.2 Hz, damping 0.5, stepped at 240 Hz): where the sim has it between ticks
  (`Served.Smoothed`) and its speed, so it eases off and in, springs a little past a stop and rounds a corner. The
  target flies 8 pixels over the cell's centre, or 2 under a wall, floor or closed door above, so it never pokes into a
  ceiling. It leans into its speed (9 degrees cruising) and its acceleration (at most 20), faces the way it flies, bobs
  1.5 pixels every 1.1 s (half at speed), its rotors turning with its spin. Its cargo hangs from the clamp under its body
  on a short tether, upright, its jaws over the stack's corners, swinging as a pendulum the drone's acceleration pushes
  back. Its lamp shines sky while its rotors run and lights round it a little; flying home low (`Low`) it blinks amber
  instead, lighting more. Read every frame from the live list. No numbers and no seconds.
- **Taking off and landing** (`Served.Changed`, every frame): a drone new to the sim's list rises from the pad of its bay
  that just stopped holding a drone, holding a pixel over it for 0.22 s as its rotors spin up, then follows the sim. One
  gone from the list lands on the pad of its bay that just came to hold a drone, the nearest: over it 4 pixels up for
  0.55 s, then down onto it over 0.45 s as its rotors stop; the pad shows no docked drone till it touches down. One
  launched from a pad a drone is still landing on takes that drone over, never having touched down. One gone with no pad
  filled (dropped, or home to a full bay) is gone at once. A drone already in the air when the view starts (a load, a
  join) is met where it is.
- **Heard** (`features/sound/README.md` has the numbers): each drone's rotors where it is drawn, a loop spun up as it
  lifts off and down once it touches down or drops, higher and louder as it flies faster, wavering, each drone tuned
  apart (`Whir`); a thud as it lifts off its pad and as it touches down; a click as its clamp takes a stack and as it
  puts some or all of it in (`Served.Gripped`). All on World with a station's reach, quieter than a station at work.

## Deliberately not handled

- The bay and the badge's switch are silent; so is a drone flying home low: its amber lamp says it (the user picked no
  warning). A clamp putting cargo down on the floor, its target gone, clicks as a delivery does.
- A drone flies in front of the machines and heroes it passes, never behind; tilted, its pixels are turned, not redrawn.
- A drone landing reads its pad from the slots alone: two drones of one bay docking the same tick take the two pads
  nearest each, in the order the view meets them.
- The badge on a machine with nothing drones would do yet (a furnace with every slot empty) still reads lit: it says
  drones would serve it, not that they are.
- `Reached` floods every bay's zone each time the badge reads, ten times a second for the one frame open: cheap at a
  few bays.
