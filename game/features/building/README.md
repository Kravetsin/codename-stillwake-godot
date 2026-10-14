# building (client): the plan in hand

The plan, its menu and the hand at work (0022), ported from `apps/game/src/features/building/` and
`session/ui.ts` (G-08). Game rules stay in `sim/Building`: the client shows what `Problem`, `ClearAt`,
`Lacking`, `CostOf`, `TicksFor` and `CostFor` say and sends `PlacePlan`, `Deconstruct`, `CancelPlan` and
`Work`.

## Files

| File              | Holds                                                                                          | Godot |
| ----------------- | ---------------------------------------------------------------------------------------------- | ----- |
| `Tools.cs`        | the cards and tabs, `CostOf`, `OverlayFor`, `TabFor`, `LayersFor`, `Known`, the pipette's `CardAt` (`tools.ts`) | no    |
| `PlanHand.cs`     | the card in hand and the plan out, kept in step with `Ui.Hand` and `Ui.OverlayOn` (`tools.ts`'s stores) | no    |
| `Drag.cs`         | a drag's line of cells and what the card or removal would do in each (`drag.ts`)               | no    |
| `NearGrid.cs`     | the grid fading out around the pointer while a card is out (`near-grid.ts`)                    | no    |
| `Blueprint.cs`    | how planned pipes and doors join their neighbours (`blueprint.ts`)                            | no    |
| `BuildingView.cs` | plans in blue as their own pictures, marks, bars, ghosts, the buttons, the pipette, plan work  | yes   |
| `BuildMenu.cs`    | the menu shown while the plan is out: tabs, thumbnails, tooltips, the card in hand; B and Esc  | yes   |

Shared with the other features: `game/shared/` (`Ui`: the hand, the overlay on, the words by the
pointer; `Hands`: every view of this player's hands wired together, the building first to take the
buttons; `PointerTip`), `game/i18n/En.cs` (the words), `game/render/` (`Buttons`/`WorldButtons`: the mouse
as the world reads it; `BlueprintArt`/`PlanTiles`: the ship's own tiles for plans, ghosts and
thumbnails; `Sketch`). The plain files are compiled again into `game.tests/`, where `tools.test.ts`,
`drag.test.ts` and `near-grid.test.ts` are ported one for one; working a plan is the hero's hands
(`features/crew`, `AimTests`).

## Guarantees

- One thing in hand (`Holding`): free, the plan, a card or the cable. A card in hand is the plan out;
  the plan put away takes the card; the hand freed elsewhere (a hotbar slot) puts the plan away. Each
  card shows its overlay and the plan put away gives back the one before; another overlay puts the cable
  away.
- Left drags place along a line (a machine where it began); right drags cancel plans when begun on one,
  else mark what is built in the overlay's layers; the middle button picks the card under the pointer,
  known cards only. A drag ends with the hand that began it.
- With an empty hand, the left button held on a plan is the hero's (crew's `Aiming.UsesOf`): within its
  reach it delivers once per sweep or works one tick per tick released, at most five a frame.
- Plans redraw only when the plans change; ghosts are tiles laid and erased each frame.
- Work on a plan throws sparks from its middle, 0.3 a tick of work it goes on by (`Effect.Spark`).

## Differences from the TypeScript

- Plans and ghosts are `TileMapLayer`s on one shared atlas (`ShipTiles.Shared`), tinted as a whole; an
  iron back wall planned is always the plain panel, never the grille.
- The refusal beside the pointer (`PointerTip`) is drawn on the screen at the world's point and size,
  so it reads as world text, 10 pixels of the world, but stays sharp at any zoom.
- The menu's cost row shows item pictures from `art/items`; prices read the inventory every 250 ms.
