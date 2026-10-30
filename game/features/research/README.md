# research (client): the tree on the tablet and the trade station's screen

The tree as the player walks it ([tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md)). T takes it out
anywhere. A trade station is worked as a crate is, through the items feature's panel, with the ship's credits under its slots.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `TechPanel.cs`       | the tree on the tablet: the map, its gates and blocks, dragged and zoomed | yes   |
| `StationScreens.cs`  | a trade station's screen, drawn over its picture                          | yes   |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/TechCamera.cs` | How the map lies in the view: dragged, zoomed, kept in bounds | no |
| `logic/TechMap.cs` | Where each node stands on the map, worked out from what it needs | no |

## Guarantees

- The window reads the sim four times a second. It is built once and only restyled, for a rebuild under the pointer would eat
  a click. It changes the world only by sending `BuyBlock` and `Deposit`. A press buys only if the pointer did not move: a drag starting on a node buys nothing.
- A block is green once bought, edged in amber while it can be bought, dim while out of reach. A gate is violet while it takes
  a deposit. It opens on the first thing to do the first time, and after that stays where it was left; the head's "next" brings that into view.
- A trade station's screen types lines while it holds what sells, and is dark while not. It is read every 100 ms and drawn
  unlit, for a screen gives its own light.
