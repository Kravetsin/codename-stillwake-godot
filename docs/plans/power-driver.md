# The power driver

A second builder for the middle of the space stage: the power driver (шуруповёрт; the screwdriver every hero starts with is
the отвёртка), bought on the black market. Written by `design-author`, cut down by `design-reviewer`, reviewed by
`/review-plan`, its rules confirmed by the user on 2026-11-03. Its claims about the code were checked by reading it
(`Prices.WorthOf`, `MarketOrdersTests`, `Aiming.AimAt`/`UsesOf`, `Motor`, `Items.Run`); nothing has been run.

Numbers used below: the sim runs 20 ticks a second (`Sim.TicksPerSecond`); a battery holds 1800 power-ticks
(`Content.Charge.Battery`); the screwdriver is `Work: 1`, one plan a cell. "A round trip" is a command's way to the host
and back stamped, a few ticks on a LAN, about 100 ms on Steam.

## What the player meets

1. Once `order2` opens (plates and gears sold), the market offers the power driver beside the second belt slot. Bought, the
   workbench makes it.
2. A battery on the belt, the left button held on plans: the 2×2 square of cells under the pointer is outlined, and in each
   of its cells the plan of every layer is worked, each a tick of work a tick (`Work: 1`), as the screwdriver works one. So
   walls and back walls go up, and marks come down, up to four times as fast, eight with both layers planned.
3. A machine, door, ladder or repair is one plan, so it goes at the screwdriver's pace, however many cells of it the square covers.
4. The right button, the driver in hand, switches it between the square and one cell, as the screwdriver works; the outline
   shows which. The mode is this player's and stays till switched.
5. Under the pointer, the square's tip shows what all its plans still need together against what the player carries, green
   when covered, red when not, as a blueprint's tip does today.
6. Short of items, it builds what it can, in a fixed order: the bottom row first, left to right, the front before the back
   wall; a plan it cannot pay for waits.
7. It draws 1 power-tick a tick while the left button is held with it, at plans or not, as the drill draws (0055): a battery
   is 1800 ticks, 90 s. Flat, it works one cell, as the screwdriver does; nobody needs to carry the screwdriver beside it.

**What goes wrong.** Bought before batteries are made (circuits, about `order4`), it is only a screwdriver till then, and only
the energy tab says why. This is the user's pick.

## Decisions

- **Content.** `Screwdriver(Item, Work, Area)`: `Area` is the side of the square worked while the tool runs. `Basic` is
  `("screwdriver", Work: 1, Area: 1)`; `PowerDriver` is `("powerDriver", Work: 1, Area: 2)`. The power stays the item's
  (`ItemSpec.Draw: 1`, `Gear`, `ItemGroup.Tool`): `Run` and `ItemsModule.Runs` already drain it.
- **Recipe** (the user's, with a circuit): plate 2, gear 2, rotor 1, coil 1, wire 2, circuit 1, 120 ticks, at the
  workbench beside the drill (`Content.Stations`). On `Content.Market.Unsold`, as every tool.
- **The market's block**: `("powerDriver", Times: 4)` in `order2`, priced 4 × its worth, rounded as `Prices.Rounded` rounds.
  `Prices.WorthOf` (sim/Research/Prices.cs) falls back to `Market.WorthOf(id)` when `id` is no blueprint; today it reads
  `Prints[id]` and would throw. The rule that grants what is obtained already skips the market tab (`Research.Walked`).
- **The market row**: `order2` already holds five goods, the cap of `MarketOrdersTests.TheMarketFitsItsPageOrABlockAskingForMoreBringsScrolling`.
  The market page is being redrawn for more goods in the user's other task; this PR raises the cap to 6, and until the
  redraw the sixth block may sit past the page's frame.
- **The square**: `Brush.Corner(2, 2, …)` round the cell corner nearest the pointer, as the placing stamp (0104), clamped to the
  grid. Its side is `Area` when the driver's mode is the square and `Items.Running(peer)` is the driver, else 1.
- **Targets, apart from `Aim`**: `Aiming.WorkTargets(m, peer, hero, cells)` reads the square's cells bottom row first, left to
  right; in each cell in reach (`Reaches`), each layer's oldest workable plan (front, then back), else a broken machine's
  repair; deduped, the first kept. `Aim` keeps the pointer's press, crate and prompt; the targets list is built from the
  square alone, so a square whose pointer cell holds nothing, a crate or a far cell still works its other cells. `UsesOf`
  takes the list and sends `due × Work` `Work` (or `Repair`) per target, `MaxWorkPerFrame` per target. The hand turns
  (`working`) while any target is workable.
- **The prompt and tip**: the build prompt shows while any target is workable; with none, the refusal of the first target in
  order. The square's tip sums each blueprint's `BuildingModule.CostOf(id)` less its `Plan.Delivered` (`CostLine`, `PlanTip`).
- **The mode**: `Ui.DriverSquare` (client, not saved, the square on start), switched by a right click with the driver in hand
  and the plan away (the right button does nothing then today; with the plan out it marks). The see-through that a held
  right button opens round the pointer is not opened while the driver is in hand.
- **Run**: `Motor.Wanted` is handed the driver's item whenever the left button builds with it, so it runs at nothing, as the
  drill does. A run that followed the targets would flicker the square a round trip behind the pointer. `Run` on another
  item replaces the run and off stops it (`ItemsModule.ApplyRun`), so switching tools needs nothing new. The driver's run is
  kept apart from the drill's in `HeroView.Hands`: `Drill()`, `FanOf`, `HearDrill` and `Spinning` read the drill only.
- **Words**: `item.powerDriver`, `.kind`, `.key`, `.about`, and the mode's two names, in `Items.En.cs`/`Items.Ru.cs` and
  `Crew.En.cs`/`Crew.Ru.cs`. Prompts stay "[Screwdriver, hold LMB]".

## Look and sound (picked)

- **Look:** «Пистолет» (`pistol` on the pick page): a short pistol-grip driver in worn green enamel, a battery slab under the grip
  with three charge lights (green running, red flat). Drawn into `tools/anime-art/subjects/hero.js` (`hero_powerDriver`,
  `_held`, its turning frames) and `items.js` (`items/powerDriver`) from the page's `candidates.js`; `HeldLook.PowerDriver`.
  `game.tests/ItemCardTests.cs:378`, which holds every builder to the screwdriver's look, changes with it.
- **Sound:** «Винт в металл: петля» (The-Sacha-Rush, freesound 455339, CC0): the bite once (12.80–13.60 s), the steady stretch
  looped while held (13.60–14.30 s, a 100 ms seam), the run-down on letting go (14.30–14.62 s); levelled as the drill's whir.
  A `Cue` set beside `Screwdriver`, played from the worked square while the driver runs at a target; credited in
  `game/audio/CREDITS.md`. The screwdriver's ratchet stays for one cell flat.

## Saved, derived, determinism, co-op

`Work`, `Repair` and `Run` are the only inputs, all commands; charge is whole power-ticks. `Running` is saved and carried, so a
joining peer has it. The square, its mode and its order are the client's choice and not checked (principle 6, 0002); the square
widens a round trip after the press. No save format change: one item, one craft, one block of content; a save past `order2`
shows the block on load, as the market is derived. The new item changes what `ItemsModule.Fuzz` draws for a seed: replayed
`[Property(Replay = …)]` seeds are re-checked.

## Tests

- Scenario: `Run("powerDriver")` drains 1 power-tick a tick.
- Scenario: the workbench refuses the craft (`crafting.unknown`) before the block is bought; it queues after `BuyBlock`.
- Content: every non-gate node's price is above 0, and the block's is 4 × worth; `Area` and `Work` at least 1.
- `MarketOrdersTests`: the cap at 6.
- Client logic (`game.tests`): the square at the grid's edge has its cells once; bottom row first, left to right, front before
  back; one target per layer a cell; a machine under two cells worked once; a pointer cell with no plan still works the others;
  reach per cell; not running, or the mode one cell, works one cell; the per-frame cap per target; the square's tip sums its
  blueprints.
- A frame of the square at work and a recording of every state (`tools/snap.ps1`).

## Steps

1. Content and sim: `Screwdriver.Area`, the item, the craft, the block, `Prices.WorthOf`, the market row cap; the scenario and
   content tests.
2. Client: `WorkTargets`, the square, `UsesOf` over targets, the mode and its right click, the run and its split from the
   drill, the outline, the tip, the words; logic tests; a frame.
3. Look and sound: the art exported by `tools/anime-art`, `HeldLook.PowerDriver`, `ItemCardTests`; the cuts, `Cue`, credits;
   a recording.
4. Docs: Building's `Work` rule, crew's hands, items' card, the glossary (шуруповёрт → `powerDriver`, ≠ бур `drill`), an ADR
   for the work area; this plan deleted.

## Deliberately not handled

- Faster work on one plan: the user chose the area alone.
- Modules fitted into the driver.
- A charge drained while a hold lingers on nothing, and for a round trip after letting go: as the drill.
- A plan the player cannot pay for waits silently while others in the square go on; a second plan may be refused
  `items.notEnough` after the first took the last item, the client's preview being a frame old.
- `building.heroInside`: walls round the hero's own cells wait, as with the screwdriver.
- Two players on one plan finish it sooner; their later `Work` comes back `building.noPlan`, harmless.
- Marks taken apart four at a time drop four refunds at the hero (`GiveOrDrop`).
- A friend's driver is not drawn turning nor heard by others till its look and cue land; then as the drill is.
- Up to 4 × 2 targets × 5 `Work` a frame from one peer after a stall, against 5 today; no cap on commands was found in `net/Net`
  (its only limit is ticks run per update), to be confirmed by a co-op run.
- Any sim check of the tool, the square or the run on `Work`.
