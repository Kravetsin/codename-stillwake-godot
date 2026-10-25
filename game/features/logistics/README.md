# logistics (client): the drone badge and drones on screen

Drones ([0062](../../../docs/decisions/0062-drones-keep-machines-going.md)), the client of `sim/Logistics`.
It reads `LogisticsModule` (`Served`, `Servable`, `Reached`, `Drones`, `Low`, `ZoneOf`), `IItemsQueries` (`Crate`),
`IGridQueries` (`MachinesWith(Zone)`: the bays, 0113; `MachineById`, `StructureAt`) and `IPowerQueries` (`NodeAt`,
`NetworkOf`, `Charges`), and changes the sim only by sending `SetServed`. Its badge lives in other features' frames,
wired by `Main`: the header of every machine frame (`InventoryPanel.Served`) and a queued station's
(`FabricatorPanel.Served`), where drones have work. The bay's slots, charge bars and frame bolt are the items
feature's. It follows the mockup "Drones Keep It Going" and the user's picks from "Drone Look Concepts": drone A,
bay C (the perch), motion "lively". Numbers live in `game/Content/logic/DroneLook.cs`.

## Files

| File               | Holds                                                                                         | Godot |
| ------------------ | --------------------------------------------------------------------------------------------- | ----- |
| `logic/Served.cs`  | the badge's look and tooltip keys; own `SetServed` in flight (0014); bay slots filled or emptied; a clamp's grip | no |
| `logic/Hover.cs`   | a drone's drawn motion: spring, lean, face, bob, rotors, swinging cargo, lift, landing | no |
| `logic/Trail.cs`   | where a flying drone is drawn between ticks: its path averaged over the last ticks | no |
| `logic/Whir.cs`    | a drone's rotors heard: pitch and volume from its speed and id | no |
| `ServedBadge.cs`   | a machine's drone badge, a wrist key in its frame's header (0081) | yes |
| `LogisticsView.cs` | drones in flight and docked, pads, lamps, a bay's beacon and zone; drones heard | yes |
| `DroneLight.cs`    | drones in light (0075): rotor discs and glints, floor dust under them, lamp trail, shimmer, landing puffs | yes |
| `logic/Logistics.En.cs` | Its player text (`En`) | no |

Tests: `ServedTests`, `HoverTests`, `TrailTests`, `DroneSoundTests`. The testbed (`-- --testbed`, `TestbedTests`) runs two drones from a powered bay through a closed
door keeping the furnace, Former and grow capsule going from a supply crate, the farm's crate switched off;
`--open=droneBay` (`--open-at 130`) shows the bay's frame and zone, `--open=crate@2` a crossed badge,
`--near=furnace` starts in that room, `--card=droneBay --pick` holds a bay's blueprint.

## Guarantees

- **The badge** (0081): a wrist key (`Shared.WristKey`), its face a drone, at the right of the header beside a
  crate's sort key. Lit (sky, drone glowing) while drones serve the machine and a bay reaches it; dim and crossed
  while switched off; grey where no bay reaches. Shown only where drones ever have work (`Servable`): not on a bay,
  the analyzer or the charging station. A click sends `SetServed` with the other state. The one tooltip says the
  state (served, left alone, no bay reaches, or both of the last) and what a click does. Read every 100 ms and at
  once after its own command, showing its own switch before its tick (0014).
- **A remembered input** (`KeptAt`, while served; `InventoryPanel.Kept`): a small sky dot in its top-left; empty, a
  faint ghost of the item. Its tooltip, empty, says "Drones keep this full: X", with "none in reach" in red when no
  crate or output a bay reaches holds it (`Supplied`). `SlotBox.Kept` draws both.
- **The bay's frame:** four slots, each a drone's faint silhouette while empty (tooltip "put a drone here to fly");
  a docked drone's slot shows its charge bar as a battery's (green, amber under a quarter); the header bolt shows the
  draw, lit while charging (`Power.Charges`).
- **The bay in the world** (`machine_droneBay.png`): a docked drone sits folded on its pad (`drone_dock`, lit), its
  eye unlit over it unless flat; under each, unlit, four charge lamps by quarters (`Charges.Share`), the next
  blinking in while charging, amber under a quarter. While the bay has power: each pad's strip (`droneBay_pad`),
  pulsing while charging, steady when full, with a little light (green, amber under a quarter); the beacon on top
  shines sky, lighting round it a little. Slots are read every
  frame. Its zone (`ZoneOf`) is outlined dashed sky over a faint fill while its frame is open; with a bay's card in
  hand every zone is, and the one the card would make, clipped at the grid. A flat drone shows `drone_empty`.
- **Drones in flight:** `drone_fly_0..3` (rotor frames), lit by its normal map (`PixelArt.Lit`), eye and status lamp
  unlit over it. It follows a spring target (`Hover`, `DroneLook.Lively`) where the sim has it between ticks
  (`Trail`: its path through the last ticks averaged over `TrailTicks`, so it keeps an even speed and turns a corner
  along a curve, a little behind the sim), easing, overshooting a stop. The target flies half a cell over the cell's centre,
  lower under a ceiling in the two cells above, so its body (taller than a cell, 0104) keeps clear of it; a doorway is
  no ceiling to a drone in one or flying up or down through it. It leans into speed
  and acceleration, faces its way, bobs, rotors turning. Cargo hangs from the clamp on a short tether, upright, jaws
  over the stack's corners, swinging as a pendulum the drone's acceleration pushes. Its lamp shines sky while its
  rotors run, lighting round it a little; flying home `Low` it
  blinks amber, brighter. Read every frame from the live list. No numbers, no seconds.
- **Taking off and landing** (`Served.Changed`, every frame): a drone new to the list rises from the pad of its bay
  that just emptied, holding over it as its rotors spin up. One gone lands on the nearest pad of its bay that just
  filled: hovering, then down as its rotors stop; the pad shows no docked drone till it touches down. One launched
  from a pad a drone is still landing on takes that drone over. One gone with no pad filled (dropped, or home to a
  full bay) is gone at once. A drone in the air when the view starts (a load, a join) is met where it is.
- **Heard** (`features/sound/README.md`): each drone's rotors where drawn, a loop spun up at lift-off and down on
  touching down or dropping, higher and louder faster, wavering, each tuned apart (`Whir`); a thud lifting off and
  touching down; a click as its clamp takes or puts a stack (`Served.Gripped`). On World with a station's reach,
  quieter than a station at work.

## Deliberately not handled

- The bay and the badge's switch are silent; so is a drone flying home low: its amber lamp says it. A clamp putting
  cargo on the floor clicks as a delivery does.
- A drone flies in front of machines and heroes, never behind; tilted, its pixels are turned, not redrawn.
- A landing reads its pad from the slots alone: two drones of one bay docking one tick take the nearest pads in the
  order the view meets them.
- The badge on a machine with nothing to do yet (an empty furnace) still reads lit: drones would serve it.
- `Reached` floods every bay's zone each time the badge reads, ten times a second for the open frame: cheap at a
  few bays.
