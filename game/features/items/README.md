# items (client): the hotbar, the inventory, crates and stacks on the floor

This machine's items, ported from `apps/game/src/features/items/` and the item parts of
`features/crew/` (`pickup.ts`, `pickup-popups.ts`) (G-08). It reads the sim through `IItemsQueries`,
`IGridQueries`, `IRoomsQueries` and `IResearchQueries`, handed in every frame from the current
registry, the debris module's `CaughtFrom` for what its own catches gave, the furnace module's `At` for a furnace or a burner generator (`InventoryPanel.Furnace`)
and the power module's `Charges` for a charging station (`InventoryPanel.Charger`), and changes it only
by sending `Move`,
`Drop`, `PickUp` and `Sort`.

## Files

| File               | Holds                                                                                        | Godot |
| ------------------ | -------------------------------------------------------------------------------------------- | ----- |
| `Click.cs`         | the click rules (`click.ts`) and quick equip (0047): `Clicks.On`, `Picked`, `SlotClick`, `Clicked`| no    |
| `Hotbar.cs`        | the row of nine and the wheel (`hotbar.ts`); the digit each slot shows, the key that chooses it | no    |
| `Equipment.cs`     | the equipment slots by kind, and which light up or dim while a stack is carried (`SlotGlow`, 0047) | no    |
| `Reading.cs`       | the slots as this peer shows them: `AfterMoves` over its `Move`s, `Drop`s and `Sort`s in flight (0014); which items are marked unanalysed, a stack's tip | no    |
| `Lessons.cs`       | an analyzer's panel: its stage, how far, what an analysed item goes into, each one's tip (0027) | no    |
| `Reach.cs`         | `HeroSpot`; pick-ups (`pickup.ts`)                                                           | no    |
| `OwnCatches.cs`    | what this peer's own grabs of debris put into its inventory, once each as they run            | no    |
| `SortButton.cs`    | the sort button: a small square with three bars and an arrow down, drawn in code, lighter under the pointer (0008) | yes   |
| `SlotBox.cs`       | one slot, `Ui.Slot` wide: icon, count, tint, chosen, picked, hovered, a hotbar slot's digit, the violet "?" of an item not analysed; an equipment slot's looks | yes   |
| `AnalyzerPanel.cs` | an analyzer's panel: its slot and screen, a hint, its bar, what its item goes into           | yes   |
| `AnalyzerScreen.cs`| the screen in that panel: dark, running lines, a check                                         | yes   |
| `InventoryPanel.cs`| the hotbar, the inventory, the equipment and the hands' tool at the right edge, the open crate, furnace, electric smelter, grow capsule, burner generator, analyzer, charging station or drone bay in the machine frame under the inventory, with the logistics feature's drone badge in the header where drones have work (`Served`, set by `Main`, 0062) and a bay's bolt lit while it charges (`Charger`), the stack in hand; all built again at another slot size | yes   |
| `EnergyBar.cs`     | the energy bar right of the suit's, while an electric tool is held (0053)                      | yes   |
| `FloorLayer.cs`    | stacks on the floor, rimmed, bobbing at rest, side by side when still, gliding between ticks, turning in space (`floor-layer.ts`) | yes |
| `PickUpPopups.cs`  | "Scrap (12)" rising over the hero's head                                                     | yes   |
| `ItemsView.cs`     | the feature: pick-ups, the layers and the panel                                              | yes   |
| `ItemsDemo.cs`     | `-- --items`: a room with a crate, a furnace and an analyzer, and a script for recordings    | yes   |

What it shows of charge, a slot's bar, the empty battery's picture, the card's meter, the station's words and
the energy bar, is worked out in `game/shared/Charges.cs` (no Godot), shared with the power feature's
`ChargerLights`.

The files without Godot are compiled again into `game.tests/`, where their TypeScript tests are ported
one for one: `click.test.ts` (`ClickTests`, with quick equip), `hotbar.test.ts` (`HotbarTests`), `pickup.test.ts`
(`PickUpTests`), a test for the fold of own moves (`CrateTests`), one for the analyzer's panel
(`LessonsTests`), one for the "?" and a stack's tip (`UnanalysedTests`), one for the equipment's glow (`EquipmentTests`), one for an item's card (`ItemCardTests`), one for own catches (`OwnCatchesTests`), one for the slots' size, tint and setting (`SlotsTests`, of `game/shared/Slots.cs`) and one for charge
on screen (`ChargesTests`). Opening a crate is aimed
as everything E works is, in the crew feature (`AimTests`).

## Guarantees

- What the slots show is the world with this peer's own moves and drops in flight made (`Reading`),
  read every 100 ms and at once after each move it sends, so a quick next click acts on what is seen.
- Clicks follow `click.ts` exactly: left picks a stack and puts it on the next slot, right picks half
  rounded up or puts one, but on a canister of the hero's own with none picked breathes it (`Ui.Breathe`, 0047), shift sends a stack across to the open crate (a fuel into the fuel slot of a
  furnace or a burner generator, its machine's `FuelSlot`), a click on the picked slot puts it back. A press begins a drag; let go over another slot it
  puts the stack there, over the world it throws it. Along the closed hotbar a drag moves a stack.
- **Quick equip** (0047, Terraria's): shift on a worn kind of item (`ItemSpec.Equip`) in the hero's own slots, a
  crate open or not, puts it on the first free equipment slot of its kind (the belt's first free of three), or with
  none free swaps it with the first worn there, one `Move` either way; shift on what is worn takes it off onto the
  first free own slot, the hotbar first, and does nothing when all are full. With a charging station open a battery
  goes into it instead, worn or not. A wearable in a crate goes to the inventory as any stack does. The sim decides:
  a refused move (the slot taken meanwhile, or a swap of two alike stacks of one) changes nothing. With the
  inventory closed a shift-click on the hotbar only chooses, as any click: Shift is thrust and run there, and a
  click while flying would swap what is worn.
- One hand (0022, `Ui.Hand`): 1–9 and the wheel over the world choose a hotbar slot and free the hand,
  putting the plan away; Tab frees it too as it opens the inventory. Q throws the chosen slot's whole
  stack from the hero's cell the way it faces, and does nothing while a card is in hand. The chosen
  slot is edged 2 px near-white only while the hand is free. Each hotbar slot shows the digit that
  chooses it (`Hotbar.KeyOf`) small in its top-left corner, open or closed (0058).
- **Layout** (0058): the hotbar at the top-left margin; the suit's and the energy gauges under it and the
  inventory's grid under them, at heights worked out from the slot size (`Slots.GaugeTop`,
  `Slots.InventoryTop`); the open machine under the grid, level with its left edge.
- **Slot size** (0058): every slot, the stack in hand and the analyzer's pictures are `Ui.Slot` wide, 40 px
  (icons 32, the art ×2) or 56 (icons 48, ×3) as Esc → Settings chooses (`Ui.SlotScale`). A change builds
  the hotbar, the inventory, the equipment and the open machine again at once, keeping what is open.
- **Tints** (0058): a slot holding a tool (`ItemCards.IsTool`) is tinted amber, one holding a worn item
  (`ItemSpec.Equip`: the jetpack, the battery) the equipment's green, the rest plain grey (`Slots.TintOf`,
  painted by `Shared.SlotLooks`); under the pointer a tinted slot's edge goes full amber or green. In the
  inventory, the hotbar, crates and machines alike.
- **Equipment** (0047, 0058): at the screen's right edge while the inventory is open, its top level with
  the grid's, a block laid out as the body, each row of slots under its name (head, back, belt;
  `equip.<kind>`): an empty slot darker, its edge faint, a worn one edged a little green, no silhouettes.
  While a stack is in hand or dragged, the equipment slots that would wear it (`Equipment.GlowOf`, from
  `Items.Equip`) are lit green with a glow and the others dimmed; the sim decides (`items.notEquipment`).
  Its last row, **Hands** (`equip.hands`), shows the screwdriver the right button works with: display-only
  (0030), nothing is put in it; its tooltip says what it does, no key is written on it.
- **Every machine opens in one frame** (`Shared.MachineFrame`, 0058) under the inventory: the same panel,
  its name (`tool.<kind>`) in bold and, for one on the power grid, a bolt and its power in the header, lit
  while it works and dark at 0 otherwise (a burner generator's is what it makes, lit while it burns, an
  analyzer's lit while it analyses); then its body, below.
- **Sorting** (0008): a small sort button (`SortButton`, its tip "Sort") sits in the top-right corner of the bag's
  panel, right of its grid, and in an open crate's frame header, left of the drone badge; only a crate has one. A click
  sends `Sort` for the bag or that crate and shows it at once (`Reading`). It puts a picked stack back first: its
  slot may hold another item after. A stack picked whose slot comes to hold another item, by a friend's sort or swap
  at a shared crate, is let go as one emptied is (`Clicks.StillHeld`). No key sorts.
- What the free hand holds (`Ui.Wielded`, 0034): the item in the chosen slot while the hand is free,
  else none, set on every refresh; the crew swings the fire axe and aims the harpoon by it.
- Tab opens and closes the inventory; Esc closes it and the crate, and puts the plan away with them,
  before it opens the game menu (`Shared.Escape`, `game/menu`).
- The crate open (`Ui.OpenCrate`) is set by the crew's E within reach and cleared out of reach; the
  panel shows it at once.
- An analyzer's panel speaks only of the item in its slot (`Lessons`): empty, its slot, its dark
  screen and a hint; at work, running lines on the screen, a bar and "Analysing… N%" in violet; without
  power, a dark screen and "No power"; holding an item in no cost (`Analysable`, 0027 amended), a dark
  screen and "Nothing to learn · <item> goes into nothing" in grey, no bar, nothing listed; done (the
  item analysed, at once for one analysed before), a check on the screen and, under "Analysed · <item>
  goes into:" in green, every blueprint and craft whose cost has that item, in rows that wrap: opened
  ones edged green, closed ones greyed out. Over each, the one tooltip says "Opened", or each item of its cost not analysed yet, or that analysis
  cannot open it yet. Its pictures are built again only when they change.
- Every slot showing an item not analysed yet, the stack in hand too, has a violet "?" in its
  top-right corner (the top-left is the hotbar's digit), never in creative mode. Only an item in some
  cost waits for analysis (`ItemCards.Unknown`, from `Research.Analysable`): a tool, the jetpack, the
  battery or biofuel never shows it, nor its card the band.
- Every stack, in the inventory, the hotbar, a crate, a furnace or an analyzer, shows its card in the
  one tooltip (`Shared.PointerTip`) at once by the pointer (`Shared.ItemCards`, drawn by
  `Shared.ItemCardView`, the mockup's plaque): its icon large, its name and its kind (`item.<id>.kind`);
  a violet "? Not analysed" band while unanalysed. A tool (the fire axe, the harpoon) is edged and its kind
  written in amber, and adds three tiles from content, the first in amber, one decimal at most
  (`CardTile`): the axe's damage a second, damage and seconds between hits (`Content.Axe`: damage over
  `SwingTicks` / `TicksPerSecond`), its effects (`Axe.Effects`, none yet) as chips in their colours, and no
  list of what it breaks, since every weapon breaks anything with HP; the harpoon's range in cells, reload in seconds and reel
  speed in cells a second (`Content.Harpoon`), and "Debris" and "Wreckage"; then its key and hint. Any
  other item says under "Goes into" what has it in its cost: crafts, then blueprints, then the cable,
  in content's order, four at most and "+N" for the rest; nothing when it goes into nothing; and its key
  where it has one (`item.<id>.key`): a canister "RMB breathe", the jetpack "Shift thrust in space". The
  screwdriver's gear slot keeps its line tip: it builds, and has no damage to tile.
- **Charge** (0053): a slot holding a battery draws a thin bar along its icon's foot, no numbers: green,
  amber under a quarter of full, an empty track at none (`Charges.BarOf`); an empty battery shows its own
  picture (`battery_empty`, `Charges.Picture`), in a slot, in the hand, on its card and on the floor. Its
  card adds a CHARGE meter under the head: the percent of full, a bar, and a bolt with what it holds of
  full in power-ticks, the units machines draw in (`⚡ 1170 / 1800`), both rounded down (`Charges.MeterOf`).
  No time is shown: a battery is a store for any tool, and how long one lasts is felt, not spelled out.
  An electric tool's card shows its `Draw` as a machine shows its own: a bolt and the number.
- **A charging station's panel** (the station mockup): in its frame's header its name and a bolt with what it
  draws, lit while it charges (`IPowerQueries.Charges`), dark at 0 otherwise; its slot beside a bar of what the battery
  holds, and under it "empty", "full", "charging" or "no power" (`Charges.StationState`). The sim refuses
  anything but a battery there (`items.notBattery`).
- **The energy bar** (`EnergyBar`, a layer of its own made by `Main`), right of the suit's gauge, shows the
  charge of the batteries on the belt (`IItemsQueries.BeltCharge`) of what they hold full, only while the
  free hand holds an electric item (`Ui.Wielded`, an item with a `Draw`), in the colour of that item's energy
  (`Charges.ColourOf`: charge's green); hidden otherwise. Read four times a second. No item is electric
  yet; `-- --electric=fireAxe` counts one as electric in a dev run, so the bar can be seen.
- A furnace's panel is its input over its fuel, the fire between them, its bar and its outputs; a
  burner generator's its fire over its fuel slot, and in its header a bolt with the power it makes
  (its `Makes`), lit while it burns and dark at 0 while cold. Both say why they stand still under them
  (`furnace.full`, `furnace.noFuel`, `furnace.noAir`), from the furnace module's `At`.
- An electric smelter's panel is the furnace's without the fuel (0057): its input over its mark, orange
  while it works, its bar and its three outputs; in its header a bolt with what it draws, lit while it works
  and dark at 0 otherwise, as the Former's; under it `furnace.full` when it has no room for what it makes. It
  reads `CraftingModule` (`Works`, `Progress`, `WorkOf`, `TooCold`, `CanSmelt`), handed in by `Main`.
- A grow capsule's panel is the smelter's (0060): its input over its mark, pink as its lamp while it grows, a green
  bar and its two outputs, the bolt with what it draws. Its empty input shows a faint seed (`SlotBox.Ghost`: the one
  item of its station's process). What a fed station does not work (rock in a capsule, a seed in a smelter) fills no
  bar and says nothing.
- **Too cold:** a furnace or a smelter whose input holds what needs more heat than it has turns the
  mark between its slots cold blue, with no line under the panel; over the mark the one tooltip says
  "Needs more heat · " and the first fed station hot enough, by name (`Reading.TooColdTip`).
- Where the open inventory's grid ends on the screen, its left edge and bottom, is set every frame
  (`Ui.InventoryFoot`), null while it is closed: the crafting feature docks a queued station's panel
  (the Fabricator, the Former) there, where this feature opens every other machine, so a queued station
  gets no crate panel here. Its input slots (the Former's two, the assembler's three) are this feature's all the
  same, lent to the crafting feature's panel (`InventoryPanel.Lend`, wired by `Main`): clicked, dragged, tipped and
  shown as any machine's, forgotten as they leave the tree. A shift-click from the inventory into a queued station
  goes onto its own kind in the inputs, else the first empty input, else the first, as the sim puts a move naming
  none (`Clicks`). A stack let go over any panel, another feature's too, is not thrown.
- Stacks of different items still in one cell are drawn side by side, out in space too, where a
  broken wreck's loot floats off together; only aboard do they bob.
- A stack in a cell the hero's body overlaps is picked up unless the sim would refuse it or its
  pick-up is in flight, and vanishes from the floor at once. Passed out the hero picks nothing up, so it never
  takes back the body it has just left (0061); passing out also closes the inventory, letting go of the stack in hand.
- **What a catch gave shows over the head too,** as a pick-up does: a supply pod, a crate, a scrap chunk caught by
  hand (reeled in by the harpoon or not) goes straight into the inventory, its loot drawn as the grab runs, so the
  client cannot say it at the press. An own `Grab` seen in flight one frame and gone the next has run
  (`OwnCatches`, as `OwnHits` for hits); the debris module says what that piece's catch put into the inventory
  (`CaughtFrom`), each item a line of `PickUpPopups`, stacked as pick-ups are and adding to a line of the same item.
  Only this peer's own: a friend's grab never flew here. A refused grab shows nothing. What did not fit floats and
  shows when it is picked up, never twice; a wreck broken by a hit floats all its loot, so it shows only as picked up.

## Main's hook

`Main` makes an `ItemsView` beside the hero, with the hands' `Ui`, and calls `Frame(client, grid,
rooms, items, research, debris, spot, progress, ms)` every frame from `_m`, after the hero's frame, so its grabs are seen in flight; `HeroSpot`
is the hero's feet, facing, body cell and whether it is passed out, so this feature never reads the crew feature
itself.

## Differences from the TypeScript

- Pick-ups and their popups lived in `crew` there; here `crew`'s hero only reports where it stands,
  and the items feature does the rest. The popups are drawn on the screen over the head, so the text
  stays sharp at any zoom.
- The panels are Control nodes styled as the Tailwind classes were (`render/Style.cs`); the slot
  hover is a hit test, since Godot sends no enter events while a button is held.
- The floor draws over the machines and under the heroes (`Layers.FloorItems`): a stack beside a
  furnace shows, and a hero walks over it; over a body (`Layers.Bodies`), so what it left shows on it.

## Not ported yet

- Others' heroes do not show what they carry; nothing did in the TypeScript either.
