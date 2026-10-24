# items (client): the hotbar, the inventory, crates and stacks on the floor

This machine's items. It reads the sim through `IItemsQueries`, `IGridQueries`, `IRoomsQueries`, `IResearchQueries`,
debris's `CaughtFrom`, the furnace module's `At` and power's `Charges`, `ChargeRate` and `Draw`, handed in every frame,
and changes it only by sending `Move`, `Drop`, `PickUp`, `Sort`, `Fit` and `Unfit`.

## Files

| File | Holds | Godot |
| --- | --- | --- |
| `Click.cs` | the click rules, quick equip, shift across, sockets; every shift's `Flight` and where it landed | no |
| `Hotbar.cs` | the row of nine and the wheel; each slot's digit and key | no |
| `Equipment.cs` | the equipment slots by kind, and which glow while a stack is carried (0047) | no |
| `Reading.cs` | the slots with own moves in flight (0014); marks, ghosts, tips; the open machine's module host | no |
| `Lessons.cs` | an analyzer's panel: stage, progress, what the item goes into (0027) | no |
| `Reach.cs` | `HeroSpot`; pick-ups | no |
| `OwnCatches.cs` | what this peer's own grabs put into its inventory, and where each piece was | no |
| `PickUpFlights.cs` | a stack picked up flying home to the chest (0075) | yes |
| `SortButton.cs` | the sort key, a wrist hardware key (0008, 0081) | yes |
| `SortSweep.cs` | a sort's light across the sorted grid (0081) | yes |
| `SlotBox.cs` | one slot: icon, count, tint, digit, mark, badge, ghost | yes |
| `AnalyzerPanel.cs` | an analyzer's panel | yes |
| `ModuleSocket.cs` | a machine's socket for one module (0086) | yes |
| `AnalyzerScreen.cs` | the analyzer's screen: dark, running lines, a check | yes |
| `InventoryPanel.cs` | the hotbar, inventory, equipment and the open machine's frame; the stack in hand | yes |
| `EnergyBar.cs` | the energy tab at the gauge row's right end (0053, 0081) | yes |
| `FloorLayer.cs` | stacks on the floor | yes |
| `SlotFlights.cs` | a shifted stack flying to where it landed (0075) | yes |
| `PickUpPopups.cs` | "Scrap (12)" rising over the hero's head (0075) | yes |
| `ItemsView.cs` | the feature: pick-ups, the layers and the panel | yes |
| `ItemsDemo.cs` | `-- --items`: a room with a crate, a furnace and an analyzer, for recordings | yes |

The wrist computer's metal, screens, keys and gauge (0074, 0081) are `game/shared/`'s (`WristPieces`, `WristArt`,
`WristScreen`, `WristKey`, `WristGauge`); charge on screen is `game/shared/Charges.cs`. Files without Godot are
compiled into `game.tests/`.

## Guarantees

- **What the slots show** is the world with this peer's own moves, drops, sorts and fits in flight made (`Reading`,
  0014), read every 100 ms and at once after each send (no ADR).
- **Clicks** (no ADR): left picks a stack and puts it; right picks half, rounded up, or puts one, or breathes an own
  canister (0047). A drag let go over a slot puts, over the world throws; along the closed hotbar it only chooses.
  A stack let go over any panel or bezel is not thrown.
- **Shift** sends a stack to the open machine (a fuel to its `FuelSlot`; a queued station's own kind, else first
  empty input); else quick equip (0047) for a worn kind, swapping with the first worn when none is free; else between
  bag and hotbar (no ADR), onto the first alike stack with room or empty slot, the slot shaking when there is none.
  The sim decides; a refused move changes nothing.
- **Every shift that sends flies** (0075): `Clicks.On`/`OnSocket` decide the `Flight` from the command in one place
  and the panel sends through `Act`; `ShiftFlightsTests` fails a command with no flight. At most `Fx.MostRibbons`.
- **One hand** (0022): 1–9 and the wheel choose a hotbar slot and free the hand; Tab opens the inventory; Q throws the
  chosen stack (not while a card is in hand). `Ui.Wielded` is the chosen item while the hand is free (0034).
  Esc closes the inventory and crate before the game menu. `Ui.OpenCrate` is set by crew's E within reach.
- **Layout and size** (0058): hotbar top-left, gauges under it, then the grid, the open machine under the grid,
  equipment at the right edge; every slot `Ui.Slot` wide, rebuilt at once when the setting changes. Tints by
  `Slots.TintOf`: tools amber, worn items green.
- **Equipment** (0047, 0058): head, back, belt rows; while a stack is carried, slots that would wear it glow, others
  dim. The screwdriver is a hotbar item (0107): no hands row.
- **Look** (0074): every panel is a screen in the wrist's metal; a machine's screen is greyer than the bag's. Content
  fades in with the tablet's screen; closing is instant.
- **One machine frame** (0058): name, and for a powered one a bolt with its power (`IPowerQueries.Draw`, modules in),
  lit while it works. Furnace and burner generator say why they stand (`furnace.*`, from `At`); smelter, grow capsule
  and charger panels follow 0057, 0060, 0053. Too cold: the mark turns blue, its tip names a station hot enough (0057).
  Queued stations dock at `Ui.InventoryFoot`, their input slots lent by `Lend` (0058).
- **Module sockets** (0086): one per module a host takes, empty showing its tech's ghost, glowing while one that fits
  is carried; fit by drop, click or shift; take out by press. Slots holding a fitting module show its tech icon.
- **Icons** (0097): glowing line icons, hexagon for a tech, ring for a station's work; only seen ones move.
- **Station marks** (0097): with a fed station or fire open, slots it processes show its mark (`Smelts`, `Burns`,
  `Grows`) and the card says "Smelts here" and what it makes; an empty station slot shows its mark's ghost, lit while a
  fitting stack is carried. Not analysed: a violet "?" (`ItemCards.Unknown`, 0027), never in creative mode.
- **Sorting** (0008, 0081): the bag's key and a crate's key send `Sort`; shown at once with a sweep. A picked stack is
  put back first; one whose slot changed under it is let go (`Clicks.StillHeld`).
- **The one tooltip** (0073): a stack's card follows the pointer, never covering its slot. Tools show picture and
  `ToolStats` tiles, boosted numbers in the module's colour (0098); equipment its picture and one number; a battery its
  charge in power units, never time (0053).
- **Charge** (0053): a battery slot draws a bar (amber under a quarter); an empty one its own picture. The energy tab
  (0081) always shows the belt's charge in whole power units, read four times a second.
- **Every inventory action clicks** (0081), not the wheel. A canister breathed pulses its slot.
- **Floor stacks** (0075, 0106): drawn unlit with a breathing rim, resting on the sim's point (`ClearBelow`), level,
  between the last two ticks, side by side when still; over machines and bodies, under heroes.
- **Pick-up** (no ADR): a stack the hero's body overlaps is picked up unless refused, and vanishes at once; never while
  passed out (0061), which also closes the inventory.
- **Own catches** (0075): a grab seen in flight then gone has run; `CaughtFrom` gives each item a popup line and a
  flight home from the piece. Only this peer's; a refused or already-gone grab shows nothing.

## Main's hook

`Main` makes an `ItemsView` with the hands' `Ui` and calls `Frame(...)` every frame after the hero's, so own grabs are
seen in flight; `HeroSpot` carries the hero's feet, facing, cell and passed-out state, so this never reads crew.

## Deliberately not handled

- Others' heroes do not show what they carry.
