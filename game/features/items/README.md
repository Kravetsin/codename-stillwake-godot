# items (client): the hotbar, the inventory, crates and stacks on the floor

This machine's items. It reads the sim through `IItemsQueries`, `IGridQueries`, `IRoomsQueries`, `IResearchQueries`,
debris's `CaughtFrom`, `ItemsModule.Gains`, the furnace module's `At` and power's `Charges`, `ChargeRate` and `Draw`, handed in every frame,
and changes it only by sending `Move`, `Drop`, `PickUp`, `Sort`, `Stash`, `Fit` and `Unfit`.

## Files

| File | Holds |
| --- | --- |
| `logic/Click.cs` | click rules, quick equip, shift, sockets; flights |
| `logic/Hotbar.cs` | the row of nine and the wheel; each slot's digit and key |
| `logic/Equipment.cs` | equipment slots by kind; which glow for a carried stack (0047) |
| `logic/Reading.cs` | slots with own moves in flight (0014); marks, ghosts, tips |
| `logic/Reach.cs` | `HeroSpot`; pick-ups and the magnet |
| `logic/FloorFeel.cs` | a stack's landing |
| `logic/LootPose.cs` | the side it lies on |
| `logic/OwnCatches.cs` | what own grabs and net collects gave, and where each piece was |
| `logic/OwnGains.cs` | own new gains in each surface's `Gains`, once each |
| `logic/GainSpots.cs` | gains by their place |
| `logic/GainLines.cs` | one place's lines |
| `logic/TrashSparks.cs` | the trash's burst |
| `PickUpFlights.cs` | icons and sparks flying home (0075) |
| `SortButton.cs` | the sort key, a wrist hardware key (0008, 0081) |
| `StashButton.cs` | the stash key under it, the fan glyph |
| `GearTabs.cs` | the equipment's two tabs |
| `FavouriteStar.cs` | the kept mark, in a slot and by the pointer |
| `SortSweep.cs` | a sort's light across the sorted grid (0081) |
| `TrashBurst.cs` | it, drawn |
| `SlotBox.cs` | one slot |
| `ModuleSocket.cs` | a machine's socket for one module (0086) |
| `InventoryPanel.cs` | hotbar, inventory, equipment, the open machine's frame; the stack in hand |
| `InventoryPanel.Machine.cs` | the open machine's body by kind and its readouts |
| `InventoryPanel.Sockets.cs` | module sockets: laid out, lent, fitted, tipped (0086) |
| `InventoryPanel.Input.cs` | keys, clicks, drags, drops; moves sent |
| `FloorLayer.cs` | stacks on the floor |
| `SlotFlights.cs` | a shifted stack flying to where it landed (0075) |
| `GainSpotsView.cs` | the counts beside their places |
| `ItemsView.cs` | the feature: pick-ups, the layers and the panel |
| `ItemsDemo.cs` | `-- --items`: a room with a crate and a furnace |
| `logic/Items.En.cs` | player text |
| `logic/Items.Ru.cs` | Russian |

The wrist's metal, screens, keys and gauge (0074, 0081) are `game/shared/`'s (`Wrist*`), charge on screen its
`logic/Charges.cs`.

## Guarantees

- **What the slots show** is the world with this peer's own commands in flight made (`Reading`,
  0014), read every 100 ms and after each send.
- **Clicks**: left picks a stack and puts it; right picks half, rounded up, or puts one, or breathes an own
  canister (0047). A drag let go over a slot puts, over the world throws, over a panel nothing; along
  the closed hotbar it only chooses.
- **Shift** sends a stack to the open machine (a fuel to its `FuelSlot`; a queued station's own kind, else first
  empty input); else quick equip (0047) for a worn kind, swapping with the first worn when none is free; else between
  bag and hotbar, onto the first alike stack with room or empty slot, the slot shaking when there is none.
- **Every shift that sends flies** (0075): `Clicks.On`/`OnSocket` decide its `Flight`, at most `Fx.MostRibbons`.
- **One hand** (0022): 1–9 and the wheel choose a hotbar slot and free the hand; Tab opens the inventory; Q throws the
  chosen stack (not while a card is in hand). `Ui.Wielded` is the chosen item while the hand is free (0034).
  Esc or passing out closes the inventory and crate first. `Ui.OpenCrate` is set by crew's E within reach.
- **Layout and size** (0058): hotbar top-left, gauges under it, then the grid, in creative the trash under it
  (trash-slot), the open machine under them, equipment at the right edge; every slot `Ui.Slot` wide, rebuilt on
  the setting. Tints by `Slots.TintOf`: tools amber, worn items green.
- **Equipment** (0047, 0058, gear-doll): crew's hero (`Doll`), slots by what they dress (`DollLayout`), lined to
  them, no captions; the screen ends past the last open belt. A carried stack lights slots that would wear it, dims the
  rest, and turns `GearTabs` from **Appearance** to the gear.
- **No bag before the pairing** (drone-companion): none of it open, its window hidden (`Hotbar.InBag`).
- **Look** (0074): hotbar, equipment in the wrist's metal; the paired bag the drone's hologram (`HoloWindow`, drone-frame),
  opening at once; a machine's window a hologram in its station's tone (machine-holograms), traced by pens, trailing
  the hero (`HoloPen`, `HoloSway`, drone-frame).
- **One machine frame** (0058): name, and for a powered one a bolt with its power (`IPowerQueries.Draw`, modules in),
  lit while it works. Furnace and burner generator say why they stand (`furnace.*`, from `At`); smelter, grow capsule
  and charger panels follow 0057, 0060, 0053. Too cold: the mark turns blue, its tip names a station hot enough (0057).
  Queued stations open at `Ui.InventoryFoot`, inputs lent by `Lend` (0058).
- **Module sockets** (0086): one per module a host takes, empty showing its tech's ghost, glowing while one that fits
  is carried; fit by drop, click or shift; take out by press. Slots holding a fitting module show its tech icon.
- **Icons** (0097): glowing lines, a hexagon a tech, a ring a station's work; only seen ones move. Inventory actions click
  (0081), not the wheel; a breathed canister pulses; cooling, its slots grey, swept back (canister-mark).
- **A tool in a slot** (tool-icons): its icon fills the slot (`Slots.IconBox`); any other item sits in the inset.
- **Station marks** (0097): an open station marks the slots it works (`Smelts`, `Burns`, `Grows`), the card what
  it makes; an empty one shows the mark's ghost, lit while a fitting stack is carried.
- **Sorting** (0008, 0081): the bag's or a crate's key sends `Sort`, shown at once; a picked stack put back first, let go if its slot changed (`Clicks.StillHeld`).
- **Stash** (2026-11-06-quick-stash): `Bind.Stash` or the key under sort sends `Stash` when `StashPlan` moves anything;
  icons fly from the chest to each crate. Alt+click marks an item kept (`Ui.Favourites`, local): a star on it.
- **The one tooltip** (0073): a stack's card follows the pointer. A tool: its picture large (tool-card-pictures), `ToolStats` icon rows in groups (tool-card-rows), boosts in the module colour (0098), a
  charge its blast; food, gear, canister, bottle: a picture and rows (food-and-gear-cards, liquid-cards); a battery its charge (0053).
- **Charge** (0053): a battery slot draws a bar, amber under a quarter; empty, its picture.
- **Floor stacks** (loot-pieces): 1–3 small objects (`LootLooks`), unlit, side by side when still; over
  machines, under heroes. In the air they turn about their middle; landing squashes (`FloorFeel`) and tips each onto its
  nearest side (`LootPose`); a bounce clinks. Warm outline near the hero; seeds, crystals glint. None in a creature or body.
- **Pick-up**: a stack the hero's body overlaps is picked up unless refused, flying home; loot nobody dropped within
  `Floor.PickUpReach` too, pulled in; a body's all within `Floor.BodyReach`, one line; never while passed out (0061).
- **Own catches** (0075): a grab or net `Collect` seen in flight then gone has run; `CaughtFrom` counts each item
  where its piece was drawn (`NetHolds`), only when new since it set off: a refused one shows nothing.
- **The counts** (2026-11-06-gain-spots): a column of "icon +n" beside each place that gave, a gain within `GainSpots.Near`
  of one showing going onto it; each a spark into the chest. Digs, fellings, refunds too (`OwnGains`).

## Main's hook

`Main` makes an `ItemsView` with the hands' `Ui`; `Frame(...)` runs every frame after the hero's (own grabs seen in
flight), with `NetHolds` (`NetShows.Lying`) and `HeroSpot`: feet, facing, cell, passed out.

## Deliberately not handled

- Others' heroes do not show what they carry.
