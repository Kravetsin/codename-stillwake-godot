# Tool icons from the held picture

- Status: accepted; built. Supersedes the card's live picture in [0098](0098-the-tool-bench-modules-in-tools.md) (`ToolPicture`)
- Date: 2026-10-27

## Context

Each tool had two drawings: its held picture and a 16 px icon, kept in step by hand. In a 40 px slot the icon was small, and
the card drew the held picture again, large and running. The player's words: icons "as in Terraria", the held picture turned
up-right, full size in the slot, "not on the game's grid", the screwdriver as large as the railgun; and the card need not draw
it again, "it is seen well enough in the inventory". The pick was variant E of the icon page: a ×8 nearest upscale, then a
smooth downscale, each source pixel a crisp turned square with soft edges.

## Decision

- **A tool is an item with a held picture** (`HeldLook.Of(item) != null`): the fire axe, the harpoon, the drill, the nailer,
  the railgun, the screwdriver. It has no icon drawn; `art/items` has none of them.
- **The bake** (`render/logic/ToolIcon`): the plain held picture, never its loaded, charged or spinning frames or module
  layers, its drawn box turned −45° (+x, its head, to up-right) about its middle and scaled so its diagonal, `(w + h)/√2`,
  fills the square less `HeldLook.IconMargin` (2 px) each side. Each output pixel is the mean of 8×8 sub-samples, each the
  nearest source pixel, with premultiplied alpha; clear pixels take their nearest drawn pixel's colour, so filtering shows no
  fringe.
- **Baked for the side it is drawn at**, cached by item and side (`ItemIcons.Icon(item, side)`), and drawn 1:1: a slot's is
  the whole slot (`Slots.IconBox`: a tool's box is the slot, any other item's the inset), 40 or 56 px; a card's cost row 16.
  Flights between slots draw the target's bake at its box. The world draws a 64 px bake (`HeldLook.IconWorldSide`,
  `ItemIcons.Icon(item)`) with mipmaps, filtered on the floor and in space; its silhouette and clear rows come from it.
- **The card draws no picture of a tool**: name and kind, tiles, key. The nail, never held, keeps its icon on top.
  `ToolPicture`, its running drill and its own particles went with it.

## Consequences

- A new tool gets its icon with its `HeldLook`; nothing else to draw.
- Below about 24 px a long tool is under half a pixel per source pixel: soft, not crisp. Only the card's rows and small
  tips draw that small, with the item's name beside it.
- Flights and drone cargo scale a bake with Nearest while it moves; the floor and space filter a 64 px bake, not exact per
  screen pixel.
- An icon shows no fitted module (the slot's badge does) and no state (loaded, charged).
