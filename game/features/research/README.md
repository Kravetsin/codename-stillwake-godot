# research (client): the tree on the tablet and the trade station's screen

The tree as the player walks it ([tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md), [tech-rings](../../../docs/decisions/2026-10-30-tech-rings.md)). T takes it out
anywhere. A trade station is worked as a crate is, through the items feature's panel.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `TechPanel.cs`       | the tree on the tablet: tabs of rings, a hub, nodes, the strip about what is picked | yes   |
| `StationScreens.cs`  | a trade station's screen, drawn over its picture                          | yes   |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/TechCamera.cs` | How the map lies in the view: dragged, zoomed, kept in bounds | no |
| `logic/TechLinks.cs` | How a block ties to the others, worked out from the recipes | no |
| `logic/TechRings.cs` | Where each block stands on a tab, from its ring and branch | no |

## Guarantees

- The window reads the sim four times a second. It is built once and only restyled, for a rebuild under the pointer would eat
  a click. It changes the world only by sending `BuyBlock` and `Deposit`. A press picks only if the pointer did not move; only the strip's button buys or deposits.
- A block is green once bought, edged in amber while it can be bought, dim while out of reach. The hub is violet while its gate takes
  a deposit, and the rings its gates opened are lit. It opens on the hub the first time, and after that stays where it was left; the head's "next" picks that and brings it into view.
- What is picked ties to others by colour, known or not: teal what it takes, pink where it is made, sky what it goes into; a gate violet to the ring it opens. All else dims.
- A trade station's screen types lines while it holds what sells, and is dark while not. It is read every 100 ms and drawn
  unlit, for a screen gives its own light.
