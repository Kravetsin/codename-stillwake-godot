# research (client): the tech window and the stations' screens

The tree as the player walks it ([tech-graph](../../../docs/decisions/2026-10-30-tech-graph.md)). A tech station
opens its own window on E. A trade station is worked as a crate is, through the items feature's panel, with the ship's
credits under its slots.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `TechPanel.cs`       | a tech station's window: the tree's map, its gates and blocks            | yes   |
| `StationScreens.cs`  | each station's screen, drawn over its picture                             | yes   |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/TechMap.cs` | Where each node stands on the map, worked out from what it needs | no |

## Guarantees

- The window reads the sim four times a second. It is built once and only restyled, for a rebuild under the pointer would eat
  a click. It changes the world only by sending `BuyBlock` and `Deposit`.
- A block is green once bought, edged in amber while it can be bought, dim while out of reach. A gate is violet while it takes
  a deposit. It scrolls to the first thing to do each time it opens.
- A tech station's screen blinks a cursor. A trade station's types lines while it holds what sells, and is dark while not.
  Both are read every 100 ms and drawn unlit, for a screen gives its own light.
