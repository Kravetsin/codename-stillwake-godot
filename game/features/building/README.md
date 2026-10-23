# building (client): the plan in hand

The plan, its menu and the hand at work (0022, 0030), ported from `apps/game/src/features/building/` and
`session/ui.ts` (G-08). Game rules stay in `sim/Building`: the client shows what `Problem`, `ClearAt`,
`Lacking`, `CostOf`, `RefundOf`, `TicksFor` and `CostFor` say and sends `PlacePlan`, `Deconstruct`, `CancelPlan`
and `Work`.

## Files

| File              | Holds                                                                                          | Godot |
| ----------------- | ---------------------------------------------------------------------------------------------- | ----- |
| `Tools.cs`        | the cards and tabs, `CostOf`, `OverlayFor`, `TabFor`, `LayersFor`, `Known`, the pipette's `CardAt` (`tools.ts`); how a plan joins what is beside it, `LinksOf`, `StubsOf` | no    |
| `PlanHand.cs`     | the card in hand and the plan out, kept in step with `Ui.Hand` and `Ui.OverlayOn` (`tools.ts`'s stores) | no    |
| `Drag.cs`         | a drag's line of cells, laid a stamp at a time, and what the card or removal would do in each (`drag.ts`) | no    |
| `BuildingView.cs` | plans as ghosts in marching dashes along their silhouettes turning real as they are built, marks in red dashes turning into their red ghost as they are taken down, ghosts, the tips, the buttons, the pipette, plan work | yes |
| `BuildGlow.cs`    | the light of work on plans (0075): spark bursts, glints, the molten spot with its smoke and shimmer, the cut's line, a finished plan's sweep | yes |
| `Catalogue.cs`    | the menu's fixed frame (0073): its size by the slot size, the rows scrolled, the card under the pointer, the scrollbar's thumb | no    |
| `BuildMenu.cs`    | the menu shown while the plan is out, docked at the bottom middle, the screen of a rugged tablet: tabs with counts, the search, the cards, the card in hand and the hint, a card's words in the one tooltip; B, / (Esc: `game/menu`) | yes |
| `DigView.cs`      | digging seen (0077): each crack in `Building.Cracks` drawn in three stages by its share of the wall's strength, one picture a stage (`GroundArt.Crack(stage)`, 0079), and a break's chips in the material's colour (`GroundLooks`), dust and a slow cloud, all drawn behind the walls (`Luminous.Low`) so none crosses a shaft's; a friend's digs alike. A hit's own look is thrown by the crew where it strikes (0094, `features/effects`). It reads the short list each frame and redraws only when it changed | yes |

Shared with the other features: `game/shared/` (`Ui`: the hand, the overlay on, the words by the
pointer; `Hands`: every view of this player's hands wired together, the building first to take the
buttons; `PointerTip`), `game/i18n/En.cs` (the words), `game/render/` (`Buttons`/`WorldButtons`: the mouse
as the world reads it; `BlueprintArt`/`PlanTiles`: the ship's own tiles for plans, ghosts and
thumbnails, a card's at the art's 16 px whatever the cell (0104); `PlanCut`/`BuiltParts`: where a plan being built turns real, and its real share drawn lit;
`Blueprint`: how planned pipes and walls join their neighbours (a planned door, hatch or ladder is its piece whole, 0104), which the crew's aim
outline draws too; `PlanOutline`/`PlanShapes`/`MarchingDashes`: the dashes along the silhouettes of plans and marks; `Sketch`), and the
tablet's slide and its screen coming on (`TabletMotion`, warm for 1.5 s after it is put away, 0073), by which the wrist
computer's screens come on too (0074), and a screen's soft light (`ScreenGlow`), both in `game/shared/`. The plain files are compiled again into `game.tests/`, where `tools.test.ts`,
`drag.test.ts` is ported one for one; the grid round the pointer is gone (0110), the menu's frame in `CatalogueTests`, its slide and screen in `TabletMotionTests`; working a plan is the hero's hands
(`features/crew`, `AimTests`).

## Guarantees

- **The cards follow the blueprints** (0077, 0084, 0108): a card for each material a blueprint builds a wall of
  (`Blueprints.Has`): soil and stone besides the hull's, no bedrock; then one for each it builds a back wall of. A natural back
  wall's plan shows the ground's own back tile.
- **The left button plans, the right removes in the focused layer** (0108, 0110, `Tools.ActOf`): every card places what it
  places on the left. The open plan has a focus (`PlanHand.Focus`), one of two layers: the front (everything that cannot
  stack in a cell: walls, machines, doors, hatches, ladders, floors and the other pieces) and the back walls. A card picked
  sets it to its own (`Tools.FocusOf`); a press of the middle button toggles it (`PlanHand.ToggleFocus`, `Tools.Toggled`),
  the card staying in hand. The wheel is the hotbar's over the world and scrolls the cards over the tablet, as before. The
  right button, by click or drag, with a card or none, marks or cancels in the focused layer only (`Tools.RemovalIn`; back
  walls behind machines too): everything of that layer in the rectangle, each piece once; what else is under the drag's
  start decides nothing. The focused layer is drawn whole, the other at 35% (`Tools.AlphasOf` of `PlanHand.Shown`, set by
  `Main` on the grid's layers and the machines' views), so the back wall behind a lifepod shows. Beside the tablet's right
  bezel two stacked pictures, no words, show the layers, the focused one lit. Away from the ship (`PlanHand.Aboard`) the
  middle button toggles nothing, nothing fades, the pictures hide, and the axe's and drill's fade round the pointer is off
  too (`PlanHand.SeesThrough`), so the ground never shows what is in it; a card still sets what the right button takes.
  The ghost before a press is the card's, the removal's while the right button is held. There is no removal card. No grid
  is drawn: the ghost and the pointer's outline show where a card snaps. A tab with nothing known is not shown.
- **The hand tools' menus** (0110): holding the stripper or the wrench, the plan put away, the same tablet shows that
  tool's cards alone (`Tools.CardsOf`) on one tab named for it, with no search: the stripper's cable, the wrench's pipe
  and valve. Each card shows how many of its item the player carries and its cost in the tooltip; the card picked
  (`PlanHand.ToolCard`, the first when the tool comes into the hand) decides what the left button lays. The tool put away
  closes it. Away from the ship there is no such menu, and the two tools lay and cut nothing (`PlanHand.ToolCards`, the
  overlay not shown); the sim refuses `power.notAboard` and `pipes.notAboard` too.

- One thing in hand (`Holding`): free, the plan or a card. The plan put away takes the card;
  the hand freed elsewhere (a hotbar slot by its number or the wheel) puts the plan away.
- **Walls are whole** (0106): a wall card has no shape to turn. A wall planned or in hand joins the walls beside it
  like any placement that joins (`Blueprint.Joins`), as the grid draws built walls.
- The overlay shown is never kept — `Tools.OverlayShown` from the plan out, the tab, the item wielded and `Ui.AirOn` on
  each read — so with the menu closed it is the cables while the stripper is held (0108), the pipes while the wrench is
  (0109), else air or nothing; no tab has an overlay of its own now. The air itself is drawn whenever O wants it, under every tab, the
  tab's pipes or cables over it (0072); the overlay shown stays the tab's, for the buttons and the pointer's tip.
  The menu opens on the tab it closed on; a card picked opens
  its tab; another tab lets the card go; a tab with nothing known is never the tab.
- **A surface's build list** (0076): only the cards the kind of the hero's surface allows are shown
  (`Tools.Allowed`, by `Registry.Rules`), nor picked by Q: on the test strip, the structure tab
  alone, without pipes or machines. A plan for anything else the sim refuses `building.notHere`.
- **Building has no brush**: a click places one cell, a drag many (walls and back walls a rectangle, a floor and a pipe a
  line); a machine is placed once where the stroke began. Each cell is its own `PlacePlan`, ascending, none twice, each
  judged alone (`Drag.StrokeOf`). Only digging keeps the 2×2 brush (`Shared/Brush`, Ctrl for one cell).
- **Pieces are placed whole** (0104): a ladder's 2×2, laid along a drag a piece's length a step, so a shaft grows
  two cells at a time; a door's 2×6 round the pointer where the stroke began, a hatch's 6×2 (`Drag.Oriented`) when the stroke
  runs further across than down. Each is one `PlacePlan` at its top-left, drawn, ghosted and marked as its whole picture from
  there, as a machine is; a removal on any of its cells marks the whole piece.
- Left drags place along a line (a machine where it began); walls and back walls fill the rectangle between
  the drag's corners, at most `Drag.MostAcross` (32) cells each way from where it began, as the preview judges
  and draws every cell each frame (a whole grid took 54 ms). Straight, a rectangle is a line, so a wall is still
  drawn a row at a time; aslant, it is a room's worth. Right drags work their rectangle (`Drag.CellsOf`) in the
  focused layer alone (0110): begun on a plan of that layer, a mark included, they cancel its plans in it; else they
  mark everything of that layer built in it, each piece or machine once. Q picks the card under the
  pointer onto its tab, known cards only, a back wall's its own. A drag ends with the card that began it.
- **The menu only plans** (0107). Plans are built and marks taken apart by the hero's hands with the screwdriver from the
  hotbar, the menu put away: the left button held on one (crew's `Aiming.UsesOf`), within its reach, works
  `Screwdriver.Work` ticks per tick released, at most five ticks a frame; the sim takes an item from the hero
  as each part of a blueprint begins (0063). There is no delivery apart from work.
- A cost reads the same in the menu and under the pointer (`CostLine.Lacking`): what this player carries
  over what is still needed — the cost less what was put in — green when it covers it, red when not.
  Both weigh `Ui.Carried`, read by `Hands` every 250 ms. A card's tooltip names its items; a blueprint's
  (`Ui.CostTip`, a `PlanTip`) shows at its top the build card's picture and name, "Wall: iron", as the plan's
  tinted look can hide what it is, then a row an item: its icon at 32 pixels and "100/2", no name; a row all
  paid faded with a tick. "Still needed" counts down an item a part as the blueprint is worked; the tip shows
  only while it lacks something: once all is paid, the plan real all over says the rest.
- A thing marked for taking down shows the same tip (a `MarkTip`): its card's picture and name, then a row an
  item taking it apart gives back, its icon at 32 pixels and "+2" in green — the sim's refund
  (`BuildingModule.RefundOf`, what the mark's last tick gives). A blueprint lacking something under the
  pointer is told first.
- **The menu is a docked catalogue of fixed size** (0073): 622×192 px at the normal slot, 782×224 at the large, 12 px
  above the bottom edge in the middle, whatever the tab, the search or the number of cards (`Catalogue`). Tabs with
  their counts of known cards and the search field on top; 10 by 2 cards a step of the art larger than the inventory's
  slots (0107: 56 px with 48 px pictures, 72 with 64), the card in hand
  edged sky blue; the wheel anywhere over the panel scrolls a row a notch (never the hotbar), a thin bar showing
  where. A tab opens at its top row; a card taken by Q is scrolled into view. Picking a card leaves the menu open.
- **The menu is a rugged tablet's screen** (0073): its frame a 9-slice of pixel art at two screen pixels a pixel
  (`art/tablet_frame.png`, the plate and the stylus over it), tiled so both slot sizes are crisp; the tablet itself, its
  frame, lights, glass, slide, tabs and scrollbar, is `shared/Tablet` (measures in `TabletLayout`), the Handbook's too
  (0083). B slides it up from
  the bottom edge in 130 ms, easing out, and down in 100 ms, easing in (`TabletMotion`); put away, it takes no input at
  once and shows only while it slides. Its screen fades in from 60 to 140 ms, lit at once when taken out within 1.5 s
  of being put away; no flash. A faint wash of its blue and its lamp's glow are added round it, dimmed with it.
- **The search** (/ or a click): every tab's known cards whose names hold the words (`Tools.Matching`), no tab lit
  while it shows. The field takes every key while typed in, B and Q too, and the hero stands still (`HeroView`).
  Enter or a click elsewhere ends the typing; Esc clears the words, the menu staying open (`BuildMenu.DropSearch`,
  asked first by the game menu's Esc); a tab clicked or a card Q takes that the words miss ends it; put away, the
  menu forgets it.
- Under the cards, the card in hand and one line of what the buttons do on the tab (`Tools.HintOf`), at 11 px so it
  fits beside the longest name; a longer one is cut with an ellipsis, never the frame widened. The card under the
  pointer is found by where the mouse is each frame (`Catalogue.CardAt`), so its tooltip follows a scroll. The
  menu's top is `Ui.MenuTop`, which the prompt for E keeps above.
- A plan is a ghost of what it builds (`PlanTiles`): its own picture greyed and lifted 60% toward white,
  a little blue, at 88% opacity (back walls 65%, as they are behind; 70% and 45% before 0064), so what was planned — a wall's or back wall's material too, for the other players — stays
  plain to see; unlit, so it reads in the dark, and giving no light. Doors and walls join what is
  planned and built beside them, as the grid draws them built, so a row of walls is one wall.
- Pipes are not planned: the wrench lays them as spans (0109, `features/pipes`).
- Round each group of plans — walls, back walls, ladders, doors, floors, each machine on its own — dashes as
  wide as the aim's outline (a screen pixel at 1080 lines, whatever the zoom) march clockwise, 3 line widths on in
  5, 5 a second, along the group's silhouette (0064): a strip just outside the opaque pixels of its pictures,
  a convex corner filled, so a tank is outlined round its legs and a ladder round its rails. A group's shapes join: the steps two of them share along a
  common side cancel, so a row of walls is one outline and two floors side by side one strip. A hole smaller
  than a cell is part of the silhouette (the gaps between a ladder's rungs, a vent's grille); a room walled
  round is outlined inside too. A planned machine has a "+" on its corner. Pipes and valves, lines themselves,
  are not outlined.
- How the silhouettes are found: on the CPU, per picture, from each picture's own image (`PixelArt`), never
  from the ship's atlas — it packs pictures with no room round them, so a shader tracing alpha there would
  pick up the neighbours. `PlanShapes` keeps a mask a picture and, for each placement as it is laid (its joins,
  and which cell of a picture laid over the world it shows), the rim of its pieces' pixels together.
  `PlanOutline` merges a group's rims, traces them into straight sides and counts each pixel's place round its
  loop. A shader outline was not used for machines either: one way for all, and only the CPU can drop the
  sides a group's shapes share; and bounds of each atlas region in a shader would take an instance uniform or a
  node a plan, which 0063 keeps out.
- The dashes are a mesh (`MarchingDashes`): a quad a side of the outline, each pixel carrying in its UV how far
  round its line it lies, in line widths; the shader shows 3 in 5 and moves them with the time. Built only when
  the plans or the grid change, never for work done, and when the zoom or the screen's height does; marching
  costs no frame anything, and all plans are one draw (back walls and marks one more each). Measured with 578 half-built walls (zoom 1, RTX 4060 Ti): 0.88 ms a frame,
  render CPU 0.25 ms, 62 draws, against 1.64 ms, 0.31 ms and 62 for the cell boxes painted every frame
  before; a plan placed and cancelled by turns, each time every outline found again, costs 3 ms a time.
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
- A thing marked for taking down keeps its own picture, as the grid draws it, inside red marching dashes
  along its silhouette, the same as a plan's; marked things of a kind join as plans do, each machine on its
  own; a marked pipe or valve is outlined too, as nothing else shows it marked. As it is worked it turns
  into its red ghost (the plan look, more red) from the top down, a floor or a conduit from the right: the
  mirror of a blueprint being built, the cut at what is left of its work. No bar: the ghost says how far it
  has gone, the dashes that it is marked.
- Plans redraw only when the plans or the grid change (`Version`: what is built beside a plan changes how
  it joins); ghosts are tiles laid and erased each frame.
- A removal's ghost is a box per cell edged a screen pixel wide at any zoom (a primitive line).
- Work on a plan throws bursts of sparks (0075, `BuildGlow`), 0.42 a tick of work it goes on by, three
  streaks each fanned down-biased from its side facing the nearest hero within a little past the hands'
  reach and toward that hero (`Tools.SparksOf`, the heroes from `Ui.Heroes`, set by `crew`); with none
  near, from its middle, upward. Aboard they fall and bounce once off the first wall or floor under them
  (twelve cells at most), in space they drift. Each burst glints at the tip and flickers a light there. The
  tip glows as a molten spot, hotter the longer its plan is worked (0.006 a 60 Hz frame, cooling 0.012
  once no burst came for 250 ms), lighting round it, faint smoke rising and the air over it shimmering
  (`render/Shimmer.cs`). The cut where a blueprint turns real (0063) is a thin line of sky light bleeding
  5 px into what is built, motes rising off it while it is worked; a blueprint gone while all but built
  (at most a tenth of its work, or 10 ticks, short when last seen) is swept by a band of light the way it
  was built, its corner glinting and motes thrown off its edge. Whoever works it, every peer sees the
  same side: the nearest hero, not the one working, which the sim does not tell.

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
- Marks keep their picture in red dashes and turn into their red ghost as they are worked (0064); the
  TypeScript game drew them as red projections with a work bar, and outlined plans cell by cell.
