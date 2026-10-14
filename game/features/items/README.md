# items (client): the hotbar, the inventory, crates and stacks on the floor

This machine's items, ported from `apps/game/src/features/items/` and the item parts of
`features/crew/` (`pickup.ts`, `pickup-popups.ts`) (G-08). It reads the sim through `IItemsQueries`,
`IGridQueries`, `IRoomsQueries` and `IResearchQueries`, handed in every frame from the current
registry, and the furnace's `At` (`InventoryPanel.Furnace`), and changes it only by sending `Move`,
`Drop` and `PickUp`.

## Files

| File               | Holds                                                                                        | Godot |
| ------------------ | -------------------------------------------------------------------------------------------- | ----- |
| `Click.cs`         | the click rules (`click.ts`): `Clicks.On`, `Picked`, `SlotClick`, `Clicked`                  | no    |
| `Hotbar.cs`        | the row of nine and the wheel (`hotbar.ts`)                                                  | no    |
| `Reading.cs`       | the slots as this peer shows them: `AfterMoves` over its `Move`s and `Drop`s in flight (0014) | no    |
| `Reach.cs`         | `HeroSpot`; pick-ups (`pickup.ts`)                                                           | no    |
| `SlotBox.cs`       | one slot: icon, count, chosen, picked, hovered                                               | yes   |
| `InventoryPanel.cs`| the hotbar, the inventory, the open crate, furnace or analyzer, the stack in hand, the name tip | yes |
| `FloorLayer.cs`    | stacks on the floor, rimmed, bobbing at rest, gliding between ticks, turning in space (`floor-layer.ts`) | yes |
| `PickUpPopups.cs`  | "Scrap (12)" rising over the hero's head                                                     | yes   |
| `ItemsView.cs`     | the feature: pick-ups, the layers and the panel                                              | yes   |
| `ItemsDemo.cs`     | `-- --items`: a room with a crate, a furnace and an analyzer, and a script for recordings    | yes   |

The files without Godot are compiled again into `game.tests/`, where their TypeScript tests are ported
one for one: `click.test.ts` (`ClickTests`), `hotbar.test.ts` (`HotbarTests`), `pickup.test.ts`
(`PickUpTests`), and a test for the fold of own moves (`CrateTests`). Opening a crate is aimed as
everything E works is, in the crew feature (`AimTests`).

## Guarantees

- What the slots show is the world with this peer's own moves and drops in flight made (`Reading`),
  read every 100 ms and at once after each move it sends, so a quick next click acts on what is seen.
- Clicks follow `click.ts` exactly: left picks a stack and puts it on the next slot, right picks half
  rounded up or puts one, shift sends a stack across to the open crate (a fuel into a furnace's fuel
  slot), a click on the picked slot puts it back. A press begins a drag; let go over another slot it
  puts the stack there, over the world it throws it. Along the closed hotbar a drag moves a stack.
- One hand (0022, `Ui.Hand`): 1–9 and the wheel over the world choose a hotbar slot and free the hand,
  putting the plan away; Tab frees it too as it opens the inventory. Q throws the chosen slot's whole
  stack from the hero's cell the way it faces, and does nothing while a card is in hand. The chosen
  slot is outlined only while the hand is free.
- Tab opens and closes the inventory; Esc closes it and the crate, and puts the plan away with them,
  before it opens the game menu (`Shared.Escape`, `game/menu`).
- The crate open (`Ui.OpenCrate`) is set by the crew's E within reach and cleared out of reach; the
  panel shows it at once.
- Where the open inventory ends on the screen is set every frame (`Ui.InventoryEdge`), null while it
  is closed: the crafting feature docks its crafts there. A stack let go over any panel, another
  feature's too, is not thrown.
- A stack in a cell the hero's body overlaps is picked up unless the sim would refuse it or its
  pick-up is in flight, and vanishes from the floor at once.

## Main's hook

`Main` makes an `ItemsView` beside the hero, with the hands' `Ui`, and calls `Frame(client, grid,
rooms, items, research, spot, progress, ms)` every frame from `_m`, after the hero's frame; `HeroSpot`
is the hero's feet, facing and body cell, so this feature never reads the crew feature itself.

## Differences from the TypeScript

- Pick-ups and their popups lived in `crew` there; here `crew`'s hero only reports where it stands,
  and the items feature does the rest. The popups are drawn on the screen over the head, so the text
  stays sharp at any zoom.
- The panels are Control nodes styled as the Tailwind classes were (`render/Style.cs`); the slot
  hover is a hit test, since Godot sends no enter events while a button is held.
- The floor draws over the machines and under the heroes (`Layers.FloorItems`): a stack beside a
  furnace shows, and a hero walks over it.

## Not ported yet

- Others' heroes do not show what they carry; nothing did in the TypeScript either.
