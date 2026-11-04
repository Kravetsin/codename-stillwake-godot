# What stands in for the pointer

- Status: superseded by [mode-glyph](2026-11-04-mode-glyph.md)
- Date: 2026-11-04

The power driver's mode, square or one cell, did not show until the pointer was on a plan. Of five marks on a pick page
(https://claude.ai/artifact/3MgvejnNNhkfcW2LaFBGF2: a light on the tool, four lights, a badge on the slot, a glyph by the
pointer, the frame's shadow under the pointer) the user took the last, as the pointer itself: "the shadow with its square
instead of the cursor", and the same for a card in hand. A carried stack already was the pointer.

**One owner of the arrow.** `Ui.Arrow` (`PointerArrow`): whatever stands in for the arrow says so every frame by its own name;
`Hands` hides the arrow while anything does, never under the game menu. The carried stack (`items`) is its first user, then:

- **A builder in hand** (screwdriver, power driver), over the world, not passed out or seated: its frame (`PointerFrame`,
  `Aiming.FrameAt`), the cells the next press works. The square while it would work one, else the cell; one cell on a whole
  thing; the square wanted with no charge, the cell and the square dotted round it. Teal (`Outline.SquareReady`), breathing,
  unlit, over the heroes; grey out of reach. The aim's own outline and glow show as before.
- **A card in hand**, over the world, while its ghost is drawn (`PlaceTool`): the ghost. A removal keeps the arrow.

Over any panel (`GuiGetHoveredControl`) the arrow is back.

## Deliberately not here

- The axe, guns, the harpoon and the drill keep the arrow: they strike along rays from the hand, and a crosshair is a
  question of its own.
- The point within a cell is not shown: everything a builder or a card does is by cells, and the frame is what it does.
- No badge on the slot or light on the tool: the pointer shows the mode where the player looks.
