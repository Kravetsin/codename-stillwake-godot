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
| `Reading.cs`       | the slots as this peer shows them: `AfterMoves` over its `Move`s and `Drop`s in flight (0014); which items are marked unanalysed, a stack's tip | no    |
| `Lessons.cs`       | an analyzer's panel: its stage, how far, what an analysed item goes into, each one's tip (0027) | no    |
| `Reach.cs`         | `HeroSpot`; pick-ups (`pickup.ts`)                                                           | no    |
| `SlotBox.cs`       | one slot: icon, count, chosen, picked, hovered, the violet "?" of an item not analysed        | yes   |
| `AnalyzerPanel.cs` | an analyzer's panel: its slot and screen, a hint, its bar, what its item goes into           | yes   |
| `AnalyzerScreen.cs`| the screen in that panel: dark, running lines, a check                                         | yes   |
| `InventoryPanel.cs`| the hotbar, the inventory, the open crate, furnace or analyzer, the stack in hand            | yes   |
| `FloorLayer.cs`    | stacks on the floor, rimmed, bobbing at rest, side by side when still, gliding between ticks, turning in space (`floor-layer.ts`) | yes |
| `PickUpPopups.cs`  | "Scrap (12)" rising over the hero's head                                                     | yes   |
| `ItemsView.cs`     | the feature: pick-ups, the layers and the panel                                              | yes   |
| `ItemsDemo.cs`     | `-- --items`: a room with a crate, a furnace and an analyzer, and a script for recordings    | yes   |

The files without Godot are compiled again into `game.tests/`, where their TypeScript tests are ported
one for one: `click.test.ts` (`ClickTests`), `hotbar.test.ts` (`HotbarTests`), `pickup.test.ts`
(`PickUpTests`), a test for the fold of own moves (`CrateTests`), one for the analyzer's panel
(`LessonsTests`) and one for the "?" and a stack's tip (`UnanalysedTests`). Opening a crate is aimed
as everything E works is, in the crew feature (`AimTests`).

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
- What the free hand holds (`Ui.Wielded`, 0034): the item in the chosen slot while the hand is free,
  else none, set on every refresh; the crew swings the fire axe by it.
- Tab opens and closes the inventory; Esc closes it and the crate, and puts the plan away with them,
  before it opens the game menu (`Shared.Escape`, `game/menu`).
- The crate open (`Ui.OpenCrate`) is set by the crew's E within reach and cleared out of reach; the
  panel shows it at once.
- An analyzer's panel speaks only of the item in its slot (`Lessons`): empty, its slot, its dark
  screen and a hint; at work, running lines on the screen, a bar and "Analysing… N%" in violet; without
  power, a dark screen and "No power"; done (the item analysed, at once for one analysed before), a
  check on the screen and, under "Analysed · <item> goes into:" in green, every blueprint and craft
  whose cost has that item, in rows that wrap: opened ones edged green, closed ones greyed out. Over
  each, the one tooltip says "Opened", or each item of its cost not analysed yet, or that analysis
  cannot open it yet. Its pictures are built again only when they change.
- Every slot showing an item not analysed yet, the stack in hand too, has a violet "?" in its
  top-left corner, never in creative mode; the slot's fill is left for rarity. A stack's name shows in
  the one tooltip (`Shared.PointerTip`) at once by the pointer, and for such an item a violet line under
  it says to put it in an analyzer.
- Where the open inventory ends on the screen is set every frame (`Ui.InventoryEdge`), null while it
  is closed: the crafting feature docks the lifepod's Fabricator there while E has the pod open, so the
  pod gets no crate panel here. A stack let go over any panel, another
  feature's too, is not thrown.
- Stacks of different items still in one cell are drawn side by side, out in space too, where a
  broken wreck's loot floats off together; only aboard do they bob.
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
