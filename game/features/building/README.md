# building (client): the plan in hand

The plan, its menu and the hand at work (0022, 0030). Game rules stay in `sim/Building`: the client shows what its queries say
(`Problem`, `Lacking`, `CostOf`, `RefundOf`, `TicksFor`) and sends `PlacePlan`, `Deconstruct`, `CancelPlan` and `Work`.

## Files

| File | Purpose |
| --- | --- |
| `logic/Tools.cs` | Tabs and cards from slots (0114), focus, removal, overlay, search, `CardAt`, `LinksOf`. |
| `logic/Hints.cs` | Hint lines for the hand and the pointer. |
| `HintWing.cs` | The hint screen. |
| `HintChip.cs` | The pointer's hint. |
| `logic/PlanHand.cs` | Card in hand and plan out, in step with `Ui.Hand`, `Ui.OverlayOn`; the focused layer. |
| `logic/Drag.cs` | A drag's cells, laid a stamp at a time, and what the card or removal would do in each. |
| `BuildingView.cs` | Plans and marks drawn, the tips, the buttons, the pipette, plan work. |
| `logic/SquareTip.cs` | The power driver's square's cost. |
| `logic/Sliver.cs` | Glow too thin to draw. |
| `BuildGlow.cs` | Work's light (0075): sparks, glints, molten spot, cut, sweep. |
| `Shafts.cs` | Platform shafts kept clear, drawn red or green. |
| `logic/Catalogue.cs` | The menu's frame (0073): slot size, scrolled rows, hover, scrollbar. |
| `BuildMenu.cs` | The tablet menu: modes, tabs with counts, search, cards, card in hand, tooltip. |
| `logic/WallStats.cs` | A wall's card numbers: its leak. |
| `DigView.cs` | Digging seen (0077): faults in three stages; a break's chips, dust, cloud. |
| `logic/BlockFault.cs` | A dug block's fault in ink (damage-looks). |
| `PlanKey.cs` | A key in a wing: a layer's, a mode's (0110). |
| `PlanWing.cs` | The tablet's wings for the keys: modes left, layers right. |
| `logic/Building.En.cs` | Its player text |
| `logic/Building.Ru.cs` | Russian |

Shared: `game/shared/` (`Ui`, `Hands`, `PointerTip`, `Tablet`, `TabletMotion`, `ScreenGlow`), `game/i18n/logic/En.cs`,
`game/render/` (`Buttons`, `WorldButtons`, `BlueprintArt`, `PlanTiles`, `PlanCut`, `BuiltParts`, `Blueprint`,
`PlanOutline`, `PlanShapes`, `MarchingDashes`, `Sketch`). Files in `logic/` are Godot-free and tested in `game.tests/`.

## Rules

- **One thing in hand** (0022, `Holding`): free, the plan or a card. The plan put away takes the card; the hand freed
  elsewhere puts the plan away. A drag ends with the card that began it.
- **The menu is content** (0114): a card is a row with a `MenuSlot` (machine, build, a material's wall and back wall),
  on its tab by `Order`; `MenuTabs.All` orders the tabs (`Tools.TabsOf`). Every built material has both (0077, 0084,
  0108; no bedrock). A card not known is not shown, nor a tab with none but the ship's (`ShownEmpty`). Walls have no shape to
  turn (0106); pipes are laid by piping (0109).
- **Left plans, right removes in the focused layer** (0108, 0110, `Tools.ActOf`): the plan has a focus, the front
  (walls, machines, doors, hatches, ladders, floors) or the back walls. A card picked sets its own
  (`FocusOf`); the middle button or a layer's `PlanKey` turns it, each layer keeping its card in hand. Menu and search show the
  focused layer's cards. A card in hand, the right (or Esc) lets it go (right-lets-go); with none it marks or
  cancels in the focused layer (`RemovalIn`): begun on a plan of that layer it cancels its plans in the rectangle, else
  marks all of that layer built there, each piece once. The other at 35%; round the pointer the back's clears the front, unlit (back-focus-faint). Off the ship
  (`PlanHand.Aboard`) nothing fades, no see-through (`SeesThrough`). No removal card; no grid drawn.
- **The plan's modes** (plan-modes, `PlanHand.Mode`): building, wiring, piping, by Z X C or the left wing's keys; kept
  while the plan is away. Wiring and piping show their cards alone, no search, layers or pipette (`CardsOf`); the card picked
  (`ToolCard`) is what the left lays. Leaving building puts the card in hand aside till return. Off the ship the plan
  only builds (`ModeShown`); the sim refuses `power.notAboard`, `pipes.notAboard`.
- **The overlay shown is never kept** (0072, 0108, 0109, `OverlayShown`): from the plan, mode, tab and `Ui.AirOn` on
  each read: wiring the cables, piping the pipes; else air whenever O wants it.
- **A surface's build list** (0076, `Tools.Allowed`): only cards the hero's surface allows are shown or picked.
- **Placing** (0104, 0111, `Drag.StrokeOf`): a click places one cell; a drag places along a line, walls and back walls
  fill the rectangle within `Drag.MostAcross` of where it began; a machine once where it began. Each cell is
  its own `PlacePlan`, ascending, none twice. Pieces go whole, a piece's length a step: a ladder's or door segment's
  2×2, a hatch's 2×1, one `PlacePlan` at its top-left; a doorway joins as built ones by the kind planned (`CodeOf`).
  Only digging keeps the 2×2 brush, Ctrl for one cell (0104). G (Drop) picks the known card under the pointer (0022).
- **The menu only plans** (0107): plans are built and marks taken down by a screwdriver from the hotbar, the left
  held within reach, `Screwdriver.Work` ticks per tick, at most five a frame; each part of a blueprint takes an item as
  it begins (0063). No delivery apart from work.
- **Costs** (0063, `CostLine.Lacking`): what this player carries (`Ui.Carried`, each 250 ms) over
  the cost less what was put in, green when covered, red when not, alike in menu and tip. A blueprint's tip
  (`PlanTip`) shows its card's picture and name, then an item a row; shown only while it lacks something. The power
  driver's square sums its blueprints', else its marks' (`SquareTip`).
- **A mark's tip** (0064, `MarkTip`): its card's picture and name, then what it gives back, green (`RefundOf`).
- **The menu is a docked tablet of fixed size** (0073, 0107): bottom middle, 10 by 2 cards, the card in hand edged sky blue; the wheel over it scrolls a row, never the
  hotbar. B slides it in and out; put away it takes no input. Picking a card leaves it open; one
  taken by G is scrolled into view; it opens on its last tab.
- **Search** (0073, `/` or a click): every tab's known cards whose names hold the words. Enter or a click
  elsewhere ends typing, Esc clears first (`DropSearch`); a tab or G ends it; put away, it is forgotten.
- **Hints** (build-hints): a line per action open now, its press drawn and two words; the build line flashes as
  plans stand, red with no builder. By the pointer: its act there.
- **Plans are ghosts** (0064, `PlanTiles`): greyed, unlit, joined to what is planned and built beside them; back
  walls fainter, just over the rooms' air (`PlanBacks`), the rest over the ship, under the heroes (driver-order). The
  ghost in hand is green where the sim takes it, red where not (0022).
- **Dashes** (0064): round each group of plans (a machine alone) marching
  dashes run along the silhouette, shared sides cancelled; a planned machine a "+". Pipes and
  valves unoutlined unless marked. Rebuilt on a change of plans, grid or view.
- **A blueprint turns real as worked** (0063): bottom up (floors and conduits from the left), the cut at
  `Progress / TicksFor`, by ticks; the real share lit, the
  rest the ghost. No bars. No node, material or shader parameter a plan (`PlanCut`, `BuiltParts`).
- **A mark** (0064) keeps its own picture in red dashes and turns into its red ghost from the top as worked.
- **A repair** (magnetic-flare): drawn as its machine's blueprint being built, by `RepairProgress`.
- **Work's light** (0075, 0094, `BuildGlow`, `Tools.SparksOf`): sparks face the nearest hero, alike on all
  peers; a molten spot, smoke, shimmer; the cut glows; swept when done. The
  ratchet there; by a power driver, its motor (`Ui.Driven`).
- **Digging** (0077, 0079, damage-looks): a fault's stage is its share of strength; a hit's look is the crew's (0094);
  a breakable machine cracks as a wreck (pile-crack-net); a piece's crack and dust span its cells (one-damage-rule).

## Deliberately not handled

- An iron back wall planned is always the plain panel, never the grille.
- The cut does not smooth between ticks: a guess overshoots a stalled part.
- The sim does not tell who works a plan: sparks face the nearest hero.
