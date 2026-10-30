# research (client): the tech window and the stations' screens

The tree as the player walks it ([trade-and-tech](../../../docs/decisions/2026-10-30-trade-and-tech.md)). A tech station
opens its own window on E. A trade station is worked as a crate is, through the items feature's panel, with the ship's
credits under its slots.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `TechPanel.cs`       | a tech station's window: tiers, deposits, branches of blocks to buy       | yes   |
| `StationScreens.cs`  | each station's screen, drawn over its picture                             | yes   |
| `logic/Research.En.cs` | Its player text (`En`) | no |

## Guarantees

- The window reads the sim four times a second and builds itself again only when what it shows has changed; credits are retyped in place,
  for a sale must not rebuild the key under a click. It changes the
  world only by sending `BuyBlock` and `Deposit`.
- It opens on the last tier open, where the next thing to buy is. A block is green once bought, edged in amber while it can
  be bought, dim while not; its tooltip names what it opens and why it cannot be bought.
- A tech station's screen blinks a cursor. A trade station's types lines while it holds what sells, and is dark while not.
  Both are read every 100 ms and drawn unlit, for a screen gives its own light.
