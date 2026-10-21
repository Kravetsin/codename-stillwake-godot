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
| `Drag.cs`         | a drag's line of cells and what the card or removal would do in each (`drag.ts`)               | no    |
| `NearGrid.cs`     | the grid fading out around the pointer while a card is out (`near-grid.ts`)                    | no    |
| `BuildingView.cs` | plans as ghosts in marching dashes along their silhouettes turning real as they are built, marks in red dashes turning into their red ghost as they are taken down, ghosts, the tips, the buttons, the pipette, plan work | yes |
| `BuildGlow.cs`    | the light of work on plans (0075): spark bursts, glints, the molten spot with its smoke and shimmer, the cut's line, a finished plan's sweep | yes |
| `Catalogue.cs`    | the menu's fixed frame (0073): its size by the slot size, the rows scrolled, the card under the pointer, the scrollbar's thumb | no    |
| `BuildMenu.cs`    | the menu shown while the plan is out, docked at the bottom middle, the screen of a rugged tablet: tabs with counts, the search, the cards, the card in hand and the hint, a card's words in the one tooltip; B, / (Esc: `game/menu`) | yes |
| `DigView.cs`      | digging seen (0077): each crack in `Building.Cracks` drawn in three stages by its share of the wall's strength (`GroundArt.Crack`), on a slope only over its solid half, its middle in it (0079), chips in the material's colour (`GroundLooks`) falling from each hit, dust and a slow cloud on a break, all drawn behind the walls (`Luminous.Low`) so none crosses a shaft's; a friend's digs alike. It reads the short list each frame and redraws only when it changed | yes |

Shared with the other features: `game/shared/` (`Ui`: the hand, the overlay on, the words by the
pointer; `Hands`: every view of this player's hands wired together, the building first to take the
buttons; `PointerTip`), `game/i18n/En.cs` (the words), `game/render/` (`Buttons`/`WorldButtons`: the mouse
as the world reads it; `BlueprintArt`/`PlanTiles`: the ship's own tiles for plans, ghosts and
thumbnails; `PlanCut`/`BuiltParts`: where a plan being built turns real, and its real share drawn lit;
`Blueprint`: how planned pipes, doors and walls join their neighbours (a planned door is drawn a door or a hatch by `DoorShape`, 0067), which the crew's aim
outline draws too; `PlanOutline`/`PlanShapes`/`MarchingDashes`: the dashes along the silhouettes of plans and marks; `Sketch`), and the
tablet's slide and its screen coming on (`TabletMotion`, warm for 1.5 s after it is put away, 0073), by which the wrist
computer's screens come on too (0074), and a screen's soft light (`ScreenGlow`), both in `game/shared/`. The plain files are compiled again into `game.tests/`, where `tools.test.ts`,
`drag.test.ts` and `near-grid.test.ts` are ported one for one, walls planned cut to a shape tested in `PlanShapeTests`, the menu's frame in `CatalogueTests`, its slide and screen in `TabletMotionTests`; working a plan is the hero's hands
(`features/crew`, `AimTests`).

## Guarantees

- **The cards follow the blueprints** (0077, 0084): a wall or back wall card for each material a blueprint builds it of
  (`Blueprints.Has`): soil and stone walls and back walls besides the hull's, no bedrock. A natural back wall's card and plan
  show the ground's own back tile.

- One thing in hand (`Holding`): free, the plan, a card or the cable. The plan put away takes the card;
  the hand freed elsewhere (a hotbar slot, the wheel) puts the plan away.
- **R slopes walls** (0069, part 2). With a hull wall's card in hand — not a floor, ladder, door, back wall or
  machine — R turns the shape walls are planned in: whole, ◢, ◣, ◤, ◥ and round again; Shift+R the other way
  (`PlanHand.Shape`, `PlanHand.Turn`). The shape sticks, for every wall card, until turned again; what is sent is the
  card's placement with it (`PlanHand.InHand`, `Tools.Shaped`), so a drag plans every cell of its line cut to it, even
  turned mid-drag. Wall cards' thumbnails and the card in hand show the cut. The pipette picks a wall with its shape
  (`Tools.ShapePicked`). The ghost, the plan, its dashes and its turning real as it is worked all follow the cut: a
  cut wall's pictures are the atlas's own (`ShipTiles.PictureOf`, `ImageOf`), so `BlueprintArt`, `PlanShapes` and the
  aim's outline read them as any picture. A wall planned or in hand runs on into a wall beside it only where that one's
  facing side is whole (`Blueprint.WallLinksOf`), as the grid draws built walls, so its edges match. The structure
  tab's hint names R. A wall's shape is changed by taking it apart and planning it again; there is no hammer.
- The open tab is the mode (`PlanHand.Tab`): Air shows the pipes overlay, Power the cables. The overlay
  shown is never kept — `Tools.OverlayShown` from the plan out, the tab and `Ui.AirOn` on each read — so
  with the menu closed it is air or nothing. The air itself is drawn whenever O wants it, under every tab, the
  tab's pipes or cables over it (0072); the overlay shown stays the tab's, for the buttons and the pointer's tip.
  The menu opens on the tab it closed on; a card picked opens
  its tab; another tab lets the card go; a tab with nothing known is never the tab.
- **A surface's build list** (0076): only the cards the kind of the hero's surface allows are shown
  (`Tools.Allowed`, by `Registry.Rules`; the cable as `cable`), nor picked by Q: on the test strip, the structure tab
  alone, without pipes, machines or cables. A plan for anything else the sim refuses `building.notHere`.
- Left drags place along a line (a machine where it began); walls and back walls fill the rectangle between
  the drag's corners, at most `Drag.MostAcross` (32) cells each way from where it began, as the preview judges
  and draws every cell each frame (a whole grid took 54 ms). Straight, a rectangle is a line, so a wall is still
  drawn a row at a time; aslant, it is a room's worth. Middle drags cancel plans when begun on one,
  else mark what is built in the tab's layers (Air: pipes, valves, machines). Begun where only walls' and back
  walls' blueprints are planned, a middle drag takes them off over the same rectangle (`Drag.CellsOf`), and only
  them: a pipe or machine planned inside it stays. Begun on any other plan, a mark or what is built, it runs
  along a line as before: a rectangle of marks on built walls was not asked for. Q picks the card under the
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
- A thing marked for taking down shows the same tip (a `MarkTip`): its card's picture and name, then a row an
  item taking it apart gives back, its icon at 32 pixels and "+2" in green — the sim's refund
  (`BuildingModule.RefundOf`, what the mark's last tick gives). A blueprint lacking something under the
  pointer is told first.
- **The menu is a docked catalogue of fixed size** (0073): 638×204 px at the normal slot, 862×252 at the large, 12 px
  above the bottom edge in the middle, whatever the tab, the search or the number of cards (`Catalogue`). Tabs with
  their counts of known cards and the search field on top; 14 by 3 cards, the inventory's slots, the card in hand
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
  plain to see; unlit, so it reads in the dark, and giving no light. Pipes, doors and walls join what is
  planned and built beside them, as the grid draws them built, so a row of walls is one wall.
- A planned pipe or valve, and one in hand, reaches toward every conduit beside it, built, planned or in
  the same drag, as `GridView` joins built ones; a built pipe beside it gets a stub toward it in its look
  (`Tools.StubsOf`), and a planned one beside a ghost the ghost's, so they meet with no half cell between.
  Stubs are drawn with the plan, over the machines a built pipe runs behind. A stub is the pipe's arms toward
  the plans only (`Blueprint.ArmsOf`), never its joint: a built pipe ending at a plan drawn whole as a ghost
  looked unbuilt, so a run half built could not be told from one not begun.
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
  (six cells at most), in space they drift. Each burst glints at the tip and flickers a light there. The
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
