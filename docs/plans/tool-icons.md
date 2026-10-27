# Plan: tool icons from the held picture

Branch `feat/tool-icons`, worktree `../stillwake-icons`. ADR to write: `docs/decisions/2026-10-27-tool-icons.md`; it
supersedes the card section of 0098 (the live `ToolPicture`).

Why: a 16 px tool icon is a second drawing of the tool that must be kept in step with its held picture, and in a 40 px slot
it is small. The held picture, turned and fitted, is the tool as the player holds it, at the slot's full size, and every
later tool gets its icon free with its `HeldLook`.

## The user's rules

- A **tool** here is an item with a held picture (`HeldLook.Of(item) != null`): the fire axe, the harpoon, the drill, the
  nailer, the railgun, the screwdriver, and any to come. (`ItemCards.IsTool`, which picks the big card, is another set: it
  has the nail and not the screwdriver.)
- Its icon, everywhere, is its held picture (`HeldLook.Picture`, never its loaded, charged or spinning frames or module
  layers) turned so its head, which points to +x in the picture, points up-right.
- Fitted to the icon's square with a 2 px margin along the diagonal of its drawn pixels: the screwdriver spans as much as the
  railgun.
- Off the pixel grid: each source pixel a crisp turned square with smooth edges, no staircase (the user's pick from the mock
  page: a ×8 nearest upscale, then a smooth downscale).
- Every other item keeps its 16 px icon.
- A tool's card draws no picture of the tool: its name and kind, its tiles, its key. The nail (a card tool with no held
  look) keeps its icon at 64 px on top.

## Decisions

- **The bake** (`game/render/logic/ToolIcon.cs`, plain C#): `Bake(ReadOnlySpan<byte> rgba, int width, int height, int side)`
  returns `side`×`side` straight RGBA8. The drawn box is the pixels with alpha above 0. In screen coordinates (y down) it is
  turned by −45° about the box's middle, so +x goes to (1, −1)/√2, and scaled so `(w + h) / √2` is `side − 2 × IconMargin`.
  Each output pixel is the mean of 8×8 sub-samples, each the nearest source pixel, averaged with premultiplied alpha; clear
  output pixels take the colour of their nearest drawn neighbour, so a filtered or mipmapped bake has no dark fringe.
  `DrawnBox` and `ScaleOf` are internal; the sub-sample count is a private constant.
- **New constants** in `HeldLook`: `IconMargin = 2` (px of the drawn square) and `IconWorldSide = 64` (the side of the bake the
  world draws).
- **`ItemIcons`:**
  - `Icon(item, side)`: a tool's bake of exactly `side` px, cached by (item, side), no mipmaps; for any other item the 16 px
    icon, `side` unused. Used wherever a tool can show in the UI.
  - `Icon(item)`: unchanged for other items; for a tool, the world bake (`IconWorldSide`, with mipmaps). Used by the world
    sites below and by the sites that never show a tool (`SuitGauge`, `ServedBadge`, `ModuleSocket`, `WornPicture`, the
    handbook's debris kinds).
  - The held pictures' images are read once at load, beside the silhouettes (`MakeSilhouettes`), so a bake needs no GPU
    readback. A tool's `Silhouette` is its world bake whitened.
- **Slots:** `Slots.IconBox(item, slotSide) : (int Offset, int Side)` is `(0, slotSide)` for a tool (its margin is in the
  bake) and `(Inset, slotSide − 2 × Inset)` else. `slotSide` is the slot's own side, 40 or 56 (`SlotBox`'s size), so the bake
  is drawn 1:1. Used by `SlotBox` (`_icon`, `_kept`, its shake about the box's offset), `InventoryPanel`'s held icon,
  `FabricatorPanel`, `AnalyzerPanel` and the handbook's loot.
- **Other UI sites**, each asking `Icon(item, side)` at its own drawn side: `ItemCardView`'s cost and made rows (16),
  `PointerTip`, `DevPanel`, `BuildMenu`'s thumb.
- **Flights between slots** (`SlotFlights`, started by `InventoryPanel.Input`): a tool flies as the bake of the target slot's
  side, drawn in the target's whole box (`IconBox`) scaled by the lift, Nearest; other items as today.
- **The world:** `FloorLayer` and `DebrisView` draw a tool's world bake with `LinearWithMipmaps`, its scale from the
  texture's width (16 px of world for the whole bake, as an icon today); `ClearBelow` of a tool is its clear bottom rows in
  the bake, converted to world px the same way. `PickUpFlights` (into the slots) and drone cargo draw the world bake, Nearest.
- **The card:** `ToolCard` draws no picture when the item has a held look; the nail keeps `Picture(Icon(nail), 64)`. The
  tiles' rows stay as they are (three at most a row). `ToolPicture` is deleted with what only it used: `CardLook`'s
  `ToolScale`, `EffectScale`, `RunMs`, `RestMs`; its own constants; `EffectsView`'s constructor taking glims, flares, bolts
  and matter, with its `_matterDrawn` branch (the stage constructor always draws matter). `DrawWindow` and its ground texture
  move to `WornPicture`; doc mentions of `ToolPicture` (DrillLight, ItemCardView) go.
- **The old tool icons** `game/art/items/{fireAxe,harpoon,drill,nailer,railgun,screwdriver}.png` and their `.import` go.
  `tools/ship-art/drill/gen.js` reads `items/fireAxe.png` and `items/drill.png` for its candidate sheet: it reads the held
  pictures instead; the `ship-art` README rows that name these icons as outputs say they were removed.

## Tasks

1. `HeldLook`'s constants; `ToolIcon` and its tests.
2. `ItemIcons` (both overloads, the image read, the silhouette) and `Slots.IconBox` with its test.
3. The UI sites, the flights.
4. The world sites and `ClearBelow`.
5. The card; `ToolPicture` and its dead paths deleted.
6. The old icons deleted, the asset test, `ship-art`.
7. ADR, READMEs (`game/render`, `game/features/items`, `tools/ship-art`), map lines; delete this plan.
8. Frames; full suite, `dotnet format`, the `ExportRelease` build; `/review-implementation`; PR.

## Tests (in `game.tests`, against `logic/`)

- `ToolIcon`: a one-row bar bakes opaque toward the top-right and clear toward the bottom-left (fails with no turn); edge
  pixels hold partial alpha, the outer 2 px ring is clear, the middle of a solid source pixel keeps its exact colour; an
  FsCheck property over sources 1..64 by 1..64 with clear borders and sides 8..64: every bake clear within its margin.
- `Slots.IconBox`: `(0, side)` exactly for items with a held look, the inset box for the rest.
- Assets: every item with a held look has no `game/art/items/<id>.png`, and every other item in `Content` still has one.
- Frames (`tools/snap.ps1`), looked at: the hotbar at the Normal slot size with every tool, a tool's card with its cost row,
  a tool on the floor. The card is frame-checked only: its view is Godot code.

## Deliberately not handled

- Module layers and the loaded, charged or spinning frames are not in icons: an icon names the item; the slot's module badge
  names its modules.
- Below about 24 px (the card's 16 px rows, small pointer tips) a long tool is under half a screen px per source pixel: a
  soft silhouette, not crisp squares. Those rows name the item beside it.
- Flights and drone cargo draw a bake scaled with Nearest: a brief shimmer while it moves.
- On the floor and the debris a tool is a 64 px bake filtered by the GPU, not exact per screen pixel.
- Bakes are made for the sides the UI draws; the game has no UI scale or stretch mode to bake again for.
- No angle or size per tool: one rule gives every tool the same pose.
