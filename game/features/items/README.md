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
| `Click.cs`         | the click rules (`click.ts`), quick equip (0047) and shift between the bag and the hotbar: `Clicks.On`, `Picked`, `SlotClick`, `Clicked`; where a shifted stack landed (`Clicks.Landed`) | no    |
| `Hotbar.cs`        | the row of nine and the wheel (`hotbar.ts`); the digit each slot shows, the key that chooses it | no    |
| `Equipment.cs`     | the equipment slots by kind, and which light up or dim while a stack is carried (`SlotGlow`, 0047) | no    |
| `Reading.cs`       | the slots as this peer shows them: `AfterMoves` over its `Move`s, `Drop`s and `Sort`s in flight (0014); which items are marked unanalysed, or smelted, burnt or grown by the open machine (`MarkOf`), a stack's tip | no    |
| `Lessons.cs`       | an analyzer's panel: its stage, how far, what an analysed item goes into, each one's tip (0027) | no    |
| `Reach.cs`         | `HeroSpot`; pick-ups (`pickup.ts`)                                                           | no    |
| `OwnCatches.cs`    | what this peer's own grabs of debris put into its inventory, once each as they run, and where each piece was | no    |
| `PickUpFlights.cs` | a stack picked up flying home (0075), off the floor or out of a catch: its icon on an arc to the chest, a ribbon of light behind it, a glint, a ring and a flash as it lands; a catch's one after another, `Fx.MostRibbons` at once | yes |
| `SortButton.cs`    | the sort key (0008, 0081): a wrist key whose face shows three bars and an arrow down, drawn in code, lit sky under the pointer, white while held, glowing a moment after | yes   |
| `SortSweep.cs`     | a sort's light: a bright line crossing the sorted grid, a band of sky behind it (0081) | yes   |
| `SlotBox.cs`       | one slot, `Ui.Slot` wide: icon, count, tint, chosen, picked, hovered, a hotbar slot's digit, its corner's mark: the open station's flame or sprout, or the violet "?" of an item not analysed; an equipment slot's looks | yes   |
| `AnalyzerPanel.cs` | an analyzer's panel: its slot and screen, a hint, its bar, what its item goes into           | yes   |
| `AnalyzerScreen.cs`| the screen in that panel: dark, running lines, a check                                         | yes   |
| `InventoryPanel.cs`| the hotbar, the inventory, the equipment and the hands' tool at the right edge, the open crate, furnace, electric smelter, grow capsule, burner generator, analyzer, charging station or drone bay in the machine frame under the inventory, with the logistics feature's drone badge in the header where drones have work (`Served`, set by `Main`, 0062) and a bay's bolt lit while it charges (`Charger`), the stack in hand; all built again at another slot size | yes   |
| `EnergyBar.cs`     | the energy gauge right of the suit's, always shown, four cells a belt battery, and its readout in power units on the column's rail (0053, 0081), in tabs under the wrist computer's cuff (0074) | yes   |
| `FloorLayer.cs`    | stacks on the floor, unlit with a breathing rim, standing on their line and bobbing up from it, along a slope, side by side when still, gliding between the last two ticks, turning in space (`floor-layer.ts`) | yes |
| `SlotFlights.cs`   | a stack sent across with shift flying from its slot to where it landed, on the HUD: its icon on a slight arc, a ribbon of light behind it, a pulse, a flash and a glint on the slot as it lands (0075) | yes   |
| `PickUpPopups.cs`  | "Scrap (12)" rising over the hero's head, a soft shadow round it, a band of light sweeping across it once (0075) | yes   |
| `ItemsView.cs`     | the feature: pick-ups, the layers and the panel                                              | yes   |
| `ItemsDemo.cs`     | `-- --items`: a room with a crate, a furnace and an analyzer, and a script for recordings, the sort keys pressed at its end | yes   |

The wrist computer's metal and screens (0074) are `game/shared/`'s: `WristPieces` (no Godot: each piece's 9-slice and
where its window lies in it), `WristArt` (the art laid round a window, a screen drawn) and `WristScreen` (a panel
that is a screen in its metal, hardware mounted on it; `MachineFrame` is one), shared with the crew's suit gauge and the crafting feature's
Fabricator; the screens come on by the build tablet's `TabletMotion`. Its keys (`WristKey`, the sort keys and the drone
badge) and its gauges (`WristGauge`, moved by `GaugeMotion`, laid out by `Slots.GaugeRowOf` and `MeterCells`) are
shared too (0081); their numbers are `WristLook`'s.

What it shows of charge, a slot's bar, the empty battery's picture, the card's meter, the station's words and
the energy bar, is worked out in `game/shared/Charges.cs` (no Godot), shared with the power feature's
`ChargerLights`.

The files without Godot are compiled again into `game.tests/`, where their TypeScript tests are ported
one for one: `click.test.ts` (`ClickTests`, with quick equip, shift between the bag and the hotbar and where a shifted stack landed), `hotbar.test.ts` (`HotbarTests`), `pickup.test.ts`
(`PickUpTests`), a test for the fold of own moves (`CrateTests`), one for the analyzer's panel
(`LessonsTests`), one for the "?" and a stack's tip (`UnanalysedTests`), one for the open station's marks (`SlotMarksTests`), one for the equipment's glow (`EquipmentTests`), one for an item's card (`ItemCardTests`), one for own catches (`OwnCatchesTests`), one for the slots' size, tint and setting (`SlotsTests`, of `game/shared/Slots.cs`), one for charge
on screen (`ChargesTests`) and one for the wrist computer's metal laid round the HUD at both slot sizes (`WristPiecesTests`). Opening a crate is aimed
as everything E works is, in the crew feature (`AimTests`).

## Guarantees

- What the slots show is the world with this peer's own moves and drops in flight made (`Reading`),
  read every 100 ms and at once after each move it sends, so a quick next click acts on what is seen.
- Clicks follow `click.ts` exactly: left picks a stack and puts it on the next slot, right picks half
  rounded up or puts one, but on a canister of the hero's own with none picked breathes it (`Ui.Breathe`, 0047), shift sends a stack across to the open crate (a fuel into the fuel slot of a
  furnace or a burner generator, its machine's `FuelSlot`), a click on the picked slot puts it back. A press begins a drag; let go over another slot it
  puts the stack there, over the world it throws it. Along the closed hotbar a drag moves nothing: it chooses each slot it passes over, as in Terraria, since a carried stack is not shown there.
- **Quick equip** (0047, Terraria's): shift on a worn kind of item (`ItemSpec.Equip`) in the hero's own slots, a
  crate open or not, puts it on the first free equipment slot of its kind (the belt's first free of three), or with
  none free swaps it with the first worn there, one `Move` either way; shift on what is worn takes it off onto the
  first free own slot, the hotbar first, and does nothing when all are full. With a charging station open a battery
  goes into it instead, worn or not. A wearable in a crate goes to the inventory as any stack does. The sim decides:
  a refused move (the slot taken meanwhile, or a swap of two alike stacks of one) changes nothing. With the
  inventory closed a shift-click on the hotbar only chooses, as any click: Shift is thrust and run there, and a
  click while flying would swap what is worn.
- **Shift between the bag and the hotbar** (Minecraft's): with the inventory open and no machine open, shift on a
  stack in the bag puts it on the hotbar, and on one on the hotbar puts it in the bag (`Clicks.Across`): onto the
  first stack alike (one item, one charge) with room there, else the first empty slot, one `Move` naming that slot,
  the sim moving what fits, so a stack only partly merged leaves the rest for the next shift. An open crate or
  machine comes first (what is shifted goes to it, as above), and quick equip before both: a worn kind of item goes
  to the equipment. With neither a stack alike with room nor an empty slot there, nothing is sent and the slot's
  icon shakes, a fifth of a second.
- **A shifted stack flies to where it went** (`SlotFlights`, in the style of the pick-up flight, 0075): every
  shift-click that sends a `Move`, to a crate, a furnace's fuel or a station's input, onto the equipment, off it,
  back to the bag or between the bag and the hotbar, flies the stack's icon from its slot to each slot it landed in, as
  `Reading` shows the move at once (`Clicks.Landed`: the slots shown holding more of its item than before), never
  waiting for the host: a quarter of a second on a slight arc, a little larger at its top, a thin ribbon of light behind
  it, landing in a pulse round the slot, a brief flash in it and a glint. A slot that was empty shows its new stack as
  the icon lands. A move `Reading` shows changing nothing (the sim would refuse it) flies nothing and shakes the slot.
  Over every panel, under the stack carried; several landings one after another, `Fx.MostRibbons` at most.
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
- **Look: a wrist computer** (0074). Every panel is a screen in the build tablet's scuffed metal, pixel art at two
  screen pixels a pixel, 9-slices whose edges tile, so the large slot's frames are as crisp; the slots, their sizes and
  places, digits, counts, marks, bars, the stack in hand and the tooltip are as they were, drawn on the screens.
  Always on, the **cuff**: a thin rim round the hotbar, two small bumpers and a dim status light, no glow; under it the
  suit's gauge in its tab and, while it shows, the energy bar in another, one seam between them. Open, the **forearm
  display** round the bag (bumpers, a worn warning stripe, screws, signal pips and a lamp) and the **equipment's unit**
  at the right edge, both lighting what is round them faintly in the screen's blue; under the bag, the open machine's
  **module**, square on top where it meets the display, which squares its own bottom edge then: one dark seam
  between, and the machine's screen greyer and greener than the hero's navy, so a crate's slots never read as the
  bag's. Opened, the screens' content fades in as the tablet's screen comes on (from 60 to 140 ms, never brighter than
  lit; at once within 1.5 s of closing); the metal shows at once, and closing is at once. The bezels take clicks: a
  stack let go over them is not thrown.
- **Every machine opens in one frame** (`Shared.MachineFrame`, 0058) under the inventory: the same panel,
  its name (`tool.<kind>`) in bold and, for one on the power grid, a bolt and its power in the header, lit
  while it works and dark at 0 otherwise (a burner generator's is what it makes, lit while it burns, an
  analyzer's lit while it analyses); then its body, below.
- **Sorting** (0008, 0081): a sort key (`SortButton`, its tip "Sort"), a hardware key of the wrist's metal, stands on
  the bag display's right edge, outside the screen, level with its first row, so the bag is as wide as the hotbar; an
  open crate's square one sits in its frame header, left of the drone badge; only a crate has one. A press sends `Sort`
  for the bag or that crate and shows it at once (`Reading`): light crosses the sorted grid (`SortSweep`) and, for
  the bag, the display's lamp flares. It puts a picked stack back first: its
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
- **What the open machine takes** (playtest 1: a friend could not tell rock smelts): while a fed station's
  window is open, every inventory, hotbar and equipment slot whose item it processes (`CraftingModule.Processes`:
  of its process, no hotter than it) shows its mark where the "?" sits, and before the "?": a small orange
  flame (`AsciiArt.Flame`, 7 px drawn at 14) for the furnace and the electric smelter, a small green sprout
  (`AsciiArt.Sprout`) for the grow capsule. A machine with a fire (the furnace, the burner generator) marks its
  fuel, what has `BurnTicks`, with the same flame: one sign for "this fire takes it", the tooltip telling the
  two apart. An item too hot for the station (silicon at the furnace) gets no mark; the machine's own slots
  none either. The card in the tooltip adds a line in the mark's colour, the mark before it: "Smelts here",
  "Burns here" or "Grows here" (`ItemCard.Here`, `Reading.HereOf`). The stack in hand keeps only the "?".
- Every stack, in the inventory, the hotbar, a crate, a furnace or an analyzer, shows its card in the
  one tooltip (`Shared.PointerTip`) at once, pinned beside its slot, 8 px off it, its top level with the slot's, and
  flipped to the slot's left where the screen ends, so it never covers its own slot (`TipPlace.Beside`; the slot comes
  as the tip's `Anchor` from `TipSpots`). The card (`Shared.ItemCards`, drawn by `Shared.ItemCardView`, the item
  card mockup) is a little screen of the wrist computer, shaped by what the item is:
  - *Anything else* (materials, parts, salvage): the compact card, its name and kind (`item.<id>.kind`) on one line and
    nothing more — no icon again, no list of what it goes into; a key where it has one (a canister "RMB breathe"), a
    drone's charge.
  - *A tool* (the fire axe, the harpoon, the drills), edged amber: its held picture whole at 6× in a dark window
    (`ToolPicture`), a drill running in it over and over (`CardLook`): its bit turning through its frames, its lamp lit,
    a warm glow at its point and its light thrown as the hand's is (`render/DrillLight`) into particles of its own
    drawn at 2× (`EffectsView` without matter): sparks off the point, the arc drill's coil crackling in bursts. Under
    it its name and kind in amber and three tiles from content, the first in amber, one decimal at most (`CardTile`):
    a weapon's damage a second, damage and seconds between hits (`Content.Weapons`), its effects (none yet) as chips;
    the harpoon's range in cells, reload in seconds and reel speed in cells a second (`Content.Harpoon`). An electric
    tool adds a fourth, its `Draw` by a bolt, in power units as a machine shows its own. Then its key and hint.
  - *Equipment*, edged green (the medium card): its picture in the window (`WornPicture`) beside its name, kind and one
    number — the helmet lamp on the helmet, its beam reaching out with motes drifting through it, and its reach in
    cells (`Light.HeadLampReach`) with its beam across and a bolt at 0, for it draws nothing; a battery glowing green
    as much as it holds and its charge; the jetpack no number — then its key and hint under both.
  Under any card, what the open fed station or fire does with it, in the mark's colour, the mark before it: "Smelts
  here", "Burns here" or "Grows here" (`ItemCard.Here`, `Reading.HereOf`), a fed station adding what it makes of one
  and how long one takes there at its speed, "→ scrap ×4 · 3s" (`ItemCards.OutputAt`); a violet "? Not analysed" band
  while unanalysed; a station's block. The screwdriver's gear slot keeps its line tip: it builds, and has no damage to tile.
- **Charge** (0053): a slot holding a battery draws a thin bar along its icon's foot, no numbers: green,
  amber under a quarter of full, an empty track at none (`Charges.BarOf`); an empty battery shows its own
  picture (`battery_empty`, `Charges.Picture`), in a slot, in the hand, on its card and on the floor. Its
  card's number is its charge: the percent of full, large and green, a bar, and a bolt with what it holds of
  full in power-ticks, the units machines draw in (`⚡ 1170 / 1800`), both rounded down (`Charges.MeterOf`).
  No time is shown: a battery is a store for any tool, and how long one lasts is felt, not spelled out.
- **A charging station's panel** (the station mockup): in its frame's header its name and a bolt with what it
  draws, lit while it charges (`IPowerQueries.Charges`), dark at 0 otherwise; its slot beside a bar of what the battery
  holds, and under it "empty", "full", "charging" or "no power" (`Charges.StationState`). The sim refuses
  anything but a battery there (`items.notBattery`).
- **The energy gauge** (`EnergyBar`, a layer of its own made by `Main`, 0081), right of the suit's gauge, shows the
  charge of the batteries on the belt (`IItemsQueries.BeltCharge`, `Charges.BeltOf`) always, whatever is held, so the
  gauge row is always as wide as the hotbar: the bolt, then four cells for each battery the belt can wear, a wider gap
  between batteries, lit as far as a belt of full batteries is charged; charge's green, amber and pulsing under a
  quarter of what the worn batteries hold full (as a slot's bar), dim with none worn. Its readout, on the column's
  right rail, gives the same charge in whole power units grouped by thin spaces ("4 300"), never seconds (0053).
  Read four times a second; the cells and the number move smoothly between reads, a hit flashing and draining
  (`GaugeMotion`), the number counting the lost part as it drains. `-- --charge` moves a belt battery into the bag
  and back at its end, for recordings.
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
- **A stack stands out and stands on its floor.** Its picture is drawn unlit, in its own colours in a dark room,
  its pale rim added over it as light (0075) and breathing with its bob, faintest at rest on its line and brightest at
  the top. The rim's lowest pixel sits on the point the sim rests it on, the clear rows under the item's picture
  counted (`ItemIcons.ClearBelow`); the bob only rises from there. On a floor slope's diagonal it lies turned along it
  about that point (45°). Moving, it is drawn between where the last two ticks had it, a tick behind: a guess past the
  newest would carry a falling stack into the floor for a frame before it landed, which read as sinking.
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
  Each item flies home too (`PickUpFlights`, 0075 slice 3): from where the piece was drawn when its grab was first seen
  in flight (kept by `OwnCatches`; the piece is hidden from then on), one after another three 60 Hz frames apart, each
  arc a little higher, the first landing with the ring and flash and the rest with a glint; at most `Fx.MostRibbons` (4)
  fly at once, the rest shown by their popup lines alone. A grab whose piece was already gone flies nothing.

## Main's hook

`Main` makes an `ItemsView` beside the hero, with the hands' `Ui`, and calls `Frame(client, grid,
rooms, items, research, debris, spot, progress, ms)` every frame from `_m`, after the hero's frame, so its grabs are seen in flight; `HeroSpot`
is the hero's feet, facing, body cell and whether it is passed out, so this feature never reads the crew feature
itself.

## Differences from the TypeScript

- Pick-ups and their popups lived in `crew` there; here `crew`'s hero only reports where it stands,
  and the items feature does the rest. The popups are drawn on the screen over the head, so the text
  stays sharp at any zoom.
- The panels are Control nodes styled as the Tailwind classes were (`render/Style.cs`), the panels' own boxes now
  the wrist computer's screens and metal (0074); the slot hover is a hit test, since Godot sends no enter events while a button is held.
- The floor draws over the machines and under the heroes (`Layers.FloorItems`): a stack beside a
  furnace shows, and a hero walks over it; over a body (`Layers.Bodies`), so what it left shows on it.

## Not ported yet

- Others' heroes do not show what they carry; nothing did in the TypeScript either.
