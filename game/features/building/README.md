# building (client): the plan in hand

The plan, its menu and the hand at work (0022, 0030), ported from `apps/game/src/features/building/` and
`session/ui.ts` (G-08). Game rules stay in `sim/Building`: the client shows what `Problem`, `ClearAt`,
`Lacking`, `Parts`, `CostOf`, `TicksFor` and `CostFor` say and sends `PlacePlan`, `Deconstruct`, `CancelPlan`
and `Work`.

## Files

| File              | Holds                                                                                          | Godot |
| ----------------- | ---------------------------------------------------------------------------------------------- | ----- |
| `Tools.cs`        | the cards and tabs, `CostOf`, `OverlayFor`, `TabFor`, `LayersFor`, `Known`, the pipette's `CardAt` (`tools.ts`); how a plan joins what is beside it, `LinksOf`, `StubsOf` | no    |
| `PlanHand.cs`     | the card in hand and the plan out, kept in step with `Ui.Hand` and `Ui.OverlayOn` (`tools.ts`'s stores) | no    |
| `Drag.cs`         | a drag's line of cells and what the card or removal would do in each (`drag.ts`)               | no    |
| `NearGrid.cs`     | the grid fading out around the pointer while a card is out (`near-grid.ts`)                    | no    |
| `BuildingView.cs` | plans as pale ghosts in marching dashes turning real as they are built, marks and their bars, ghosts, the buttons, the pipette, plan work | yes |
| `BuildMenu.cs`    | the menu shown while the plan is out: tabs, thumbnails, the card in hand, a card's words in the one tooltip; B (Esc: `game/menu`) | yes |

Shared with the other features: `game/shared/` (`Ui`: the hand, the overlay on, the words by the
pointer; `Hands`: every view of this player's hands wired together, the building first to take the
buttons; `PointerTip`), `game/i18n/En.cs` (the words), `game/render/` (`Buttons`/`WorldButtons`: the mouse
as the world reads it; `BlueprintArt`/`PlanTiles`: the ship's own tiles for plans, ghosts and
thumbnails; `PlanCut`/`BuiltParts`: where a plan being built turns real, and its real share drawn lit;
`Blueprint`: how planned pipes, doors and walls join their neighbours, which the crew's aim
outline draws too; `PlanOutline`: the dashes round plans; `Sketch`). The plain files are compiled again into `game.tests/`, where `tools.test.ts`,
`drag.test.ts` and `near-grid.test.ts` are ported one for one; working a plan is the hero's hands
(`features/crew`, `AimTests`).

## Guarantees

- One thing in hand (`Holding`): free, the plan, a card or the cable. The plan put away takes the card;
  the hand freed elsewhere (a hotbar slot, the wheel) puts the plan away.
- The open tab is the mode (`PlanHand.Tab`): Air shows the pipes overlay, Power the cables. The overlay
  shown is never kept — `Tools.OverlayShown` from the plan out, the tab and `Ui.AirOn` on each read — so
  with the menu closed it is air or nothing. The menu opens on the tab it closed on; a card picked opens
  its tab; another tab lets the card go; a tab with nothing known is never the tab.
- Left drags place along a line (a machine where it began); middle drags cancel plans when begun on one,
  else mark what is built in the tab's layers (Air: pipes, valves, machines); Q picks the card under the
  pointer onto its tab, known cards only. A drag ends with the hand that began it.
- The right button held on a plan is the hero's, whatever the hand holds (crew's `Aiming.UsesOf`): within
  its reach it works one tick per tick released, at most five a frame; the sim takes an item from the hero
  as each part of a blueprint begins (0063). There is no delivery apart from work.
- A cost reads the same in the menu and under the pointer (`CostLine.Lacking`): what this player carries
  over what is still needed — the cost less what was put in — green when it covers it, red when not.
  Both weigh `Ui.Carried`, read by `Hands` every 250 ms. A card's tooltip names its items; a blueprint's
  (`Ui.CostTip`, a `PlanTip`) shows at its top the build card's picture and name, "Wall: iron", as the plan's
  tinted look can hide what it is, then a row an item: its icon at 32 pixels and "100/2", no name; a row all
  paid faded with a tick. "Still needed" counts down an item a part as the blueprint is worked; the tip shows
  only while it lacks something: once all is paid, the plan real all over says the rest.
- Under the cards, the card in hand and one line of what the buttons do on the tab (`Tools.HintOf`); the
  menu's top is `Ui.MenuTop`, which the prompt for E keeps above.
- A plan is a ghost of what it builds (`PlanLook.Ghost`): its own picture greyed and lifted 60% toward white,
  a little blue, so what was planned — a wall's or back wall's material too, for the other players — stays
  plain to see; unlit, so it reads in the dark, and giving no light. Pipes, doors and walls join what is
  planned and built beside them, as the grid draws them built, so a row of walls is one wall.
- A planned pipe or valve, and one in hand, reaches toward every conduit beside it, built, planned or in
  the same drag, as `GridView` joins built ones; a built pipe beside it gets a stub toward it in its look
  (`Tools.StubsOf`), and a planned one beside a ghost the ghost's, so they meet with no half cell between.
  Stubs are drawn with the plan, over the machines a built pipe runs behind.
- Round each group of plans — walls, back walls, ladders, doors, each row of floor along its strip, each
  machine round its footprint — dashes a world pixel wide march clockwise, 5 pixels a second
  (`PlanOutline`, worked out from the cells: the atlas has no room round its pictures to trace). A planned
  machine has a "+" on its corner. Pipes and valves, lines themselves, are not outlined.
- Planned back walls and their dashes are drawn just over the rooms' air (`Layers.PlanBacks`), under the
  pipes, walls and machines built in front of them; the rest of a plan over the ship.
- The ghost in hand is drawn the same way, tinted plainly green where the sim would take it, red where not.
- A blueprint being built turns into the real thing as it is worked (0063): walls, back walls, ladders,
  doors and machines from the bottom up, floors and conduits from the left, the cut at `Progress / TicksFor`
  of its box, rounded down to a pixel, so it moves a row or a column of the art at a time. The first touch of
  a part pays its item (the sim), then the part fills smoothly under the screwdriver; where work stopped for
  want of an item the line sits exactly at that part's boundary, as the sim stops the work there. The cut
  follows the ticks, not the frames between them: a tick is a pixel or two of a wall, a step too small to
  smooth, and guessing ahead would overshoot a stalled part. No bar: the amber delivery bar and the white
  work bar on blueprints are gone; the dashes stay round the whole plan until it is built, and a machine
  keeps its "+".
- The real share is drawn as the grid draws what is built: each piece of the picture at its layer of the
  ship (`Layers.BackWalls` … `Layers.Machines`, so heroes pass in front of it), lit by `Lit`, walls, edges
  and doors keeping `Light.WallIn`; the rest stays the unlit ghost over the ship. It is lit by what reaches
  its cell now: a back wall over a hole takes the stars through it until it is built.
- How: a plan with nothing put in is a tile, as before; one being built is split at its cut into two atlas
  regions per piece (`PlanCut.Split`), drawn by `BuiltParts` (a lit canvas item a tile layer) and
  `PlanTiles` (a ghost canvas item beside each tile layer). No node, material or shader parameter a plan —
  instance uniforms take slots of Godot's global buffer per item, which `Lit` already ran out of — so the
  cost grows with draw commands only, batched by canvas item, and only when the plans change. Measured with
  580 half-built walls redrawn every other frame (zoom 1): 2.95 ms a frame against 2.81 ms for the same plans
  not begun, render CPU 0.35 against 0.33 ms, 71 draws against 63 (an RTX 4060 Ti).
- Marks are projections of what they take out, in red, as before, with the white bar of the work done
  along their bottom while they are worked.
- Plans redraw only when the plans or the grid change (`Version`: what is built beside a plan changes how
  it joins); ghosts are tiles laid and erased each frame; the dashes are
  painted again each frame while there are plans.
- A removal's ghost is a box per cell edged a screen pixel wide at any zoom (a primitive line).
- Work on a plan throws sparks, 0.3 a tick of work it goes on by (`Effect.Spark`), from its side facing
  the nearest hero within a little past the hands' reach and toward that hero (`Tools.SparksOf`, the
  heroes from `Ui.Heroes`, set by `crew`); with none near, from its middle, upward. Whoever works it,
  every peer sees the same side: the nearest hero, not the one working, which the sim does not tell.

## Differences from the TypeScript

- Plans and ghosts are `TileMapLayer`s on one shared atlas (`ShipTiles.Shared`), drawn as a whole by one
  shader; an iron back wall planned is always the plain panel, never the grille. The TypeScript game drew
  plans as blue projections, each wall cell edged all round.
- The refusal beside the pointer (`PointerTip`) is drawn on the screen at the world's point and size,
  so it reads as world text, 10 pixels of the world, but stays sharp at any zoom.
- The menu's cost row shows item pictures from `art/items`; prices read the inventory every 250 ms.
- A blueprint's tip says what is carried over what is still needed, as the menu does, not what was
  delivered over the whole cost.
- A blueprint is paid and turns real a part at a time (0063); the TypeScript game took its whole cost on
  the first press and showed delivery and work as bars.
