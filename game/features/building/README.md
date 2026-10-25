# building (client): the plan in hand

The plan, its menu and the hand at work (0022, 0030). Game rules stay in `sim/Building`: the client shows what
`Problem`, `ClearAt`, `Lacking`, `CostOf`, `RefundOf`, `TicksFor` and `CostFor` say and sends `PlacePlan`,
`Deconstruct`, `CancelPlan` and `Work`.

## Files

| File | Purpose |
| --- | --- |
| `logic/Tools.cs` | The tabs and cards from content's menu slots (0114), focus and removal, the overlay shown, search, hints, `CardAt` (the pipette), how a plan joins (`LinksOf`, `DoorLinks`). |
| `logic/PlanHand.cs` | The card in hand and the plan out, in step with `Ui.Hand` and `Ui.OverlayOn`; the focused layer. |
| `logic/Drag.cs` | A drag's cells, laid a stamp at a time, and what the card or removal would do in each. |
| `BuildingView.cs` | Plans and marks drawn (ghosts, dashes, cuts), the tips, the buttons, the pipette, plan work. |
| `BuildGlow.cs` | The light of work on plans (0075): sparks, glints, the molten spot, the cut's line, a finished plan's sweep. |
| `logic/Catalogue.cs` | The menu's fixed frame (0073): size by slot size, rows scrolled, card under the pointer, scrollbar. |
| `BuildMenu.cs` | The tablet menu: modes, tabs with counts, search, cards, card in hand, hint, tooltip; B, `/`, Z X C. |
| `DigView.cs` | Digging seen (0077): cracks in three stages, a break's chips, dust and cloud, behind the walls. |
| `PlanKey.cs` | A key in a wing: a layer's (0110), a mode's (plan-modes). |
| `PlanWing.cs` | The tablet's metal wings the keys sit in: modes left, layers right. |
| `logic/Building.En.cs` | Its player text (`En`) |

Shared: `game/shared/` (`Ui`, `Hands`, `PointerTip`, `Tablet`, `TabletMotion`, `ScreenGlow`), `game/i18n/logic/En.cs`,
`game/render/` (`Buttons`, `WorldButtons`, `BlueprintArt`, `PlanTiles`, `PlanCut`, `BuiltParts`, `Blueprint`,
`PlanOutline`, `PlanShapes`, `MarchingDashes`, `Sketch`). Files in `logic/` are Godot-free and tested in `game.tests/`.

## Rules

- **One thing in hand** (0022, `Holding`): free, the plan or a card. The plan put away takes the card; the hand freed
  elsewhere puts the plan away. A drag ends with the card that began it.
- **The menu is content** (0114): a card is a row with a `MenuSlot` (machine, build, a material's wall and back wall),
  on its tab by `Order`; `MenuTabs.All` orders the tabs (`Tools.TabsOf`). Every built material has both (0077, 0084,
  0108; no bedrock). A card not known is not shown, nor a tab with none but the ship's (`ShownEmpty`). Walls have no shape to
  turn (0106). Pipes are not planned: piping lays them (0109).
- **Left plans, right removes in the focused layer** (0108, 0110, `Tools.ActOf`): the plan has a focus, the front
  (everything solid: walls, machines, doors, hatches, ladders, floors) or the back walls. A card picked sets its own
  (`FocusOf`); the middle button or a layer's `PlanKey` turns it, each layer keeping its card in hand. Menu and search show the
  focused layer's cards. The right button, click or drag, with a card or none, marks or cancels in the focused layer
  only (`RemovalIn`): begun on a plan of that layer it cancels its plans in the rectangle, else marks everything of
  that layer built there, each piece or machine once. The other layer is drawn at 35% (`AlphasOf`). Away from the ship
  (`PlanHand.Aboard`) nothing fades, no see-through (`SeesThrough`). No removal card; no grid drawn.
- **The plan's modes** (plan-modes, `PlanHand.Mode`): building, wiring, piping, by Z X C or the left wing's keys; kept
  while the plan is away. Wiring and piping show their cards alone, no search, layers or Q (`CardsOf`); the card picked
  (`ToolCard`) is what the left lays. Leaving building puts the card in hand aside till return. Off the ship the plan
  only builds (`ModeShown`); the sim refuses `power.notAboard`, `pipes.notAboard`.
- **The overlay shown is never kept** (0072, 0108, 0109, `OverlayShown`): from the plan, mode, tab and `Ui.AirOn` on
  each read: wiring the cables, piping the pipes; else air whenever O wants it.
- **A surface's build list** (0076, `Tools.Allowed`): only cards the hero's surface allows are shown or picked by Q.
- **Placing** (0104, 0111, `Drag.StrokeOf`): a click places one cell; a drag places along a line, walls and back walls
  fill the rectangle within `Drag.MostAcross` of where it began (no ADR); a machine once where it began. Each cell is
  its own `PlacePlan`, ascending, none twice. Pieces go whole, a piece's length a step: a ladder's or door segment's
  2×2, a hatch's 2×1, one `PlacePlan` at its top-left; a doorway joins as built ones by the kind planned (`CodeOf`).
  Only digging keeps the 2×2 brush, Ctrl for one cell (0104). Q picks the known card under the pointer (0022).
- **The menu only plans** (0107): plans are built and marks taken down by the screwdriver from the hotbar, the left
  held within reach, `Screwdriver.Work` ticks per tick, at most five a frame; each part of a blueprint takes an item as
  it begins (0063). No delivery apart from work.
- **Costs** (0063; colours no ADR, `CostLine.Lacking`): what this player carries (`Ui.Carried`, every 250 ms) over
  the cost less what was put in, green when covered, red when not, alike in menu and tip. A blueprint's tip
  (`PlanTip`) shows its card's picture and name, then an item a row; shown only while it lacks something.
- **A mark's tip** (0064, `MarkTip`): its card's picture and name, then what it gives back, "+2" in green
  (`RefundOf`). A lacking blueprint under the pointer is told first.
- **The menu is a docked rugged tablet of fixed size** (0073, 0107): bottom middle, two slot sizes, 10 by 2 cards a
  step larger than inventory slots, the card in hand edged sky blue; the wheel over it scrolls a row, never the
  hotbar. B slides it in and out (`TabletMotion`); put away it takes no input. Picking a card leaves it open; one
  taken by Q is scrolled into view; it opens on the tab it closed on.
- **Search** (0073, `/` or a click): every tab's known cards whose names hold the words. The field takes every key,
  the hero standing still; Enter or a click elsewhere ends typing, Esc clears (`DropSearch`, before the game menu's
  Esc); a tab clicked or a card Q takes that the words miss ends it; put away, it is forgotten.
- **The hint** (no ADR, `HintOf`): one line under the cards, cut by an ellipsis.
- **Plans are ghosts** (0064, `PlanTiles`): their own picture greyed, lifted toward white, a little blue, unlit, back
  walls fainter, joined to what is planned and built beside them. Planned back walls draw just over the rooms' air
  (`Layers.PlanBacks`), the rest over the ship. The ghost in hand is green where the sim takes it, red where not (0022).
- **Dashes** (0064): round each group of plans (walls, back walls, ladders, doors, hatches, floors; each machine
  alone) marching dashes run along the silhouette, shared sides cancelled; a planned machine has a "+". Pipes and
  valves are not outlined, unless marked. Silhouettes are found on the CPU per picture, never from the atlas; the mesh
  is rebuilt only when plans, grid, zoom or screen height change.
- **A blueprint turns real as worked** (0063): from the bottom up (floors and conduits from the left), the cut at
  `Progress / TicksFor`, a pixel at a time, following ticks not frames; the real share lit as the grid draws it, the
  rest the unlit ghost. No bars. No node, material or shader parameter per plan (`PlanCut`, `BuiltParts`).
- **A mark** (0064) keeps its own picture in red dashes and turns into its red ghost from the top down as worked.
- **Work's light** (0075, 0094, `BuildGlow`, `Tools.SparksOf`): sparks burst from the side facing the nearest hero,
  not the worker, so every peer sees the same; a molten spot, smoke, shimmer; the cut glows; a blueprint finished is
  swept by light.
- **Digging** (0077, 0079): a crack's stage is its share of the wall's strength; a hit's own look is the crew's (0094).

## Deliberately not handled

- An iron back wall planned is always the plain panel, never the grille.
- The cut does not smooth between ticks: guessing ahead would overshoot a stalled part.
- Who works a plan is not told by the sim, so sparks face the nearest hero.
