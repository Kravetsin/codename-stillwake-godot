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
| `PlanWork.cs`     | the left button held on a plan with an empty hand: the plan parts of `crew/aim.ts`             | no    |
| `BuildingView.cs` | plans in blue as their own pictures, marks, bars, ghosts, the buttons, the pipette, plan work  | yes   |
| `BuildMenu.cs`    | the menu shown while the plan is out: tabs, thumbnails, tooltips, the card in hand; B and Esc  | yes   |

Shared with the other features: `game/shared/` (`Ui`: the hand, the overlay on, the words by the
pointer; `Hands`: every view of this player's hands wired together, the building first to take the
buttons; `PointerTip`; `Words`, the local i18n table), `game/render/` (`Buttons`/`WorldButtons`: the mouse
as the world reads it; `BlueprintArt`/`PlanTiles`: the ship's own tiles for plans, ghosts and
thumbnails; `Sketch`). The plain files are compiled again into `game.tests/`, where `tools.test.ts`,
`drag.test.ts`, `near-grid.test.ts` and the plan tests of `aim.test.ts` are ported one for one.

## Guarantees

- One thing in hand (`Holding`): free, the plan, a card or the cable. A card in hand is the plan out;
  the plan put away takes the card; the hand freed elsewhere (a hotbar slot) puts the plan away. Each
  card shows its overlay and the plan put away gives back the one before; another overlay puts the cable
  away.
- Left drags place along a line (a machine where it began); right drags cancel plans when begun on one,
  else mark what is built in the overlay's layers; the middle button picks the card under the pointer,
  known cards only. A drag ends with the hand that began it.
- With an empty hand, the left button held on a plan within the hero's reach delivers once per sweep or
  works one tick per tick released, at most five a frame.
- Plans redraw only when the plans change; ghosts are tiles laid and erased each frame.

## Differences from the TypeScript

- Plans and ghosts are `TileMapLayer`s on one shared atlas (`ShipTiles.Shared`), tinted as a whole; an
  iron back wall planned is always the plain panel, never the grille.
- The refusal beside the pointer is screen text (`PointerTip`), not text in the world.
- The menu's cost row shows item pictures from `art/items`; prices read the inventory every 250 ms.

## Not ported yet

- Sparks from work on a plan (effects, G-08).
- The hero holding the plan in its hands (`HeroView` passes `holdsPlan: false`), and the wheel and 1–9
  freeing the hand: with the hotbar (port/items-ui), which sets `Ui.Hand` to `Free`.
- The words move to the game's i18n table once it lands (`game/shared/Words.cs`).
