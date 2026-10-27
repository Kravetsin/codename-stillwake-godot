# Plan: tool icons from the held picture

Branch `feat/tool-icons`, worktree `../stillwake-icons`. ADR to write: `docs/decisions/2026-10-27-tool-icons.md`.

## The user's rules

- A tool — an item held as a picture (`HeldLook.Of(item) != null`: the fire axe, the harpoon, the drill, the nailer, the
  railgun, the screwdriver, and any to come) — shows its held picture in every icon of it, turned 45° to point up-right.
- Fitted to the slot with a 2 px margin, along the diagonal of its drawn pixels: the screwdriver as large as the railgun.
- Off the pixel grid: each source pixel a crisp turned square with smooth edges, no staircase (the user's pick, variant E of
  the mock page).
- Every other item keeps its 16 px icon at ×2.
- A tool's card draws no picture and no icon: its name and kind, its tiles, its key.

## Decisions

- **The bake** (`game/render/logic/ToolIcon.cs`, plain C#): `Bake(ReadOnlySpan<byte> rgba, int width, int height, int side)`
  returns `side`×`side` straight RGBA8. Its drawn box is the pixels with alpha above 0; turned −45° about its middle, scaled
  so `(w + h) / √2` of it is `side − 2 × HeldLook.IconMargin` (2). Each output pixel is the mean of 8×8 sub-samples, each
  the nearest source pixel, averaged with premultiplied alpha: a turned square per source pixel, its edges by area.
- **`ItemIcons`:** `Icon(item, side)` returns a tool's bake of that side, cached by (item, side), else the 16 px icon.
  `Icon(item)`, kept for the sites that never show a tool and the world's, returns a tool's world bake
  (`HeldLook.IconWorldSide` = 64, with mipmaps); the slot sides' bakes have none, drawn 1:1. The held pictures' images are
  read once, at load, with the silhouettes (`MakeSilhouettes`).
- **Slots:** `Slots.IconBox(item, slot)` is `(0, slot)` for a tool (its margin in the bake) and `(Inset, slot − 2 × Inset)`
  else. `SlotBox` (`_icon`, `_kept`, its shake from the box's inset), `InventoryPanel`'s held icon, `FabricatorPanel`,
  `AnalyzerPanel` and the handbook's loot, where a tool can show, place and size by it and ask `Icon(item, side)`.
- **Other sites that can show a tool**, at their own side: `ItemCardView`'s cost and made rows, `PointerTip`, `DevPanel`,
  `BuildMenu`'s thumb, `InventoryPanel.Input`'s and `SlotFlights`' flights (the slot side, drawn Nearest).
- **The world:** `FloorLayer` and `DebrisView` draw a tool's world bake with `LinearWithMipmaps`, its scale from the
  texture's width; its silhouette is the world bake whitened; `ClearBelow` of a tool is its margin scaled. `PickUpFlights`
  and drone cargo use the world bake, Nearest.
- **The card:** `ToolCard` draws no picture for an item with a held look; the tiles' rows stay as they are (three at most a
  row). The nail, with no held look, keeps its 64 px icon. `ToolPicture` is deleted with what only it used: `CardLook`'s
  `ToolScale`, `EffectScale`, `RunMs`, `RestMs`, its own constants, and `EffectsView`'s card constructor and branch (its
  `matter` folded into the stage one). `DrawWindow` and its ground texture move to `WornPicture`.
- **The old tool icons** `game/art/items/{fireAxe,harpoon,drill,nailer,railgun,screwdriver}.png` and their imports go.

## Tasks

1. `ToolIcon` and its tests (the turn, the edges, the margin property).
2. `ItemIcons` and `Slots.IconBox`; the call sites above.
3. The world sites; the silhouette and `ClearBelow`.
4. The card: no picture; `ToolPicture` and its dead paths deleted.
5. The old icons deleted; anything that loaded them by name checked (`res://art/items/`).
6. ADR, READMEs (render, items, shared), map lines; delete this plan.
7. Frames: the hotbar, a tool's card, a tool on the floor; full suite, format, ExportRelease; `/review-implementation`; PR.

## Tests

- `ToolIcon`: a one-row bar bakes opaque toward the top-right and clear toward the bottom-left (breaks with no turn); edge
  pixels hold partial alpha, the outer 2 px ring is clear, a solid source pixel's middle keeps its exact colour; an FsCheck
  property over sources 1..64 by 1..64 with clear borders and sides 8..64: every bake within its margin.
- Card: a tool's card has no picture (its view's tree), the nail's has its icon.
- Frames: hotbar (Normal), a tool card, a tool lying on the floor.

## Deliberately not handled

- Module overlays and the loaded, charged or spinning frames are not in icons; the slot's module badge names a module.
- Flights and drone cargo draw a tool's world bake Nearest: a brief shimmer.
- On the floor and the debris a tool is a 64 px bake filtered by the GPU, not exact per screen pixel.
- Bakes are not redone at another zoom or UI scale: there is no stretch mode.
- No angle or size per tool.
