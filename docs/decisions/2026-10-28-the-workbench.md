# The workbench

- Status: accepted; built
- Date: 2026-10-28
- Amends [0057](0057-stations-one-model-for-every-crafting-machine.md) and
  [0028](0028-the-lifepod-and-crafting-by-machine-only.md): the pod no longer makes every craft

## Context

The pod's Fabricator made every part and tool and kept gaining rows: the parts, the axe, harpoon, nailer and nails, the
jetpack, the battery, the drill, the helmet lamp, the suit. With it, power and the Former were a speed-up, never a step:
everything up to the drop pod could be made unpowered at the pod. The economy rework (`docs/plans/economy.md`) wants each
station to be a decision and the pod to give only what a lifepod would.

## Decision

- **The pod gives the suit and the screwdriver:** the helmet, the suit and the screwdriver, all free. A lost screwdriver
  is made again at once, so no hero is ever left unable to build the furnace.
- **The workbench** (`workbench`, 4×2) is a queued station of the pod's model: no power, a fifth of full speed, a queue of
  four, paid by hand, three output slots and no inputs. It is built of raw salvage with no smelting (4 scrap chunks, 2
  hull plates, 4 planks), as the furnace is, and known from the start. It makes the basic parts (wire, plate, gear, tube,
  coil, rotor) and the first tools (axe, harpoon, nailer, nails, helmet lamp).
- **The Former** keeps the parts at five times the speed and is the only maker of frames and motors, and now of the
  jetpack, the battery and the drill. Power comes before the jetpack.
- **No loop:** the burner generator and the Former are built of the workbench's parts and a salvaged circuit, never of a
  frame. The helmet lamp takes a coil instead of silicon, so it is the bench's alone.
- `EverythingButAPlanetsOwnOpens…` (`ContentTests.Production`) walks the stations from space alone and keeps the drop pod
  reachable through the split.

## Consequences

- The first hour is pod → furnace and workbench → burner generator → Former. The Former is the first real choice.
- Tests about the queue's mechanics moved from the pod to the workbench, which has the same model.
- Not handled: drones never feed the workbench (no inputs), as they never fed the pod.
