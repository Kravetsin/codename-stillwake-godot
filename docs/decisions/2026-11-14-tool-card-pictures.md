# A tool's card shows its picture enlarged

- Status: accepted; supersedes "the card draws no picture of a tool" in [tool icons](2026-10-27-tool-icons.md)
- Date: 2026-11-14

## Context

Tools are now traced from picked references in ink and watercolour, much finer than the pixel pictures the tool icons were
first baked from. Turned into a 40 px slot they barely read; the user: "в клеточках они почти не читаются". Worn gear and
food already show their picture enlarged in a window on their card ([food and gear cards](2026-11-14-food-and-gear-cards.md)).

## Decision

- **A held tool's card opens with its picture** in the same dark window a worn item's sits in, across the card's top
  (`ToolPicture`), above its name and kind.
- **The picture is the fine icon one** (`icons/<held picture>`, drawn 384 px along its length), unturned, head to the right,
  mipmapped: `ItemIcons.CardPicture`.
- **It is fitted** (`ToolPictureFit`): as large as fits in the 214 × 76 window less a 6 px margin, its shape kept, never more
  than 5 screen pixels a world pixel, so the screwdriver does not swell into a blur.
- The screwdriver, held but with no numbers, keeps its compact card with the same window on top.
- **A tool's kind is only "Tool"** where its second word restated its name («Инструмент · Рельсотрон» under «Рельсотрон»);
  the builder's tools keep «Стройка» and the blasting charge «Бросок», which say what the name does not.
- The slot keeps its turned icon.

## Not handled

- The card shows the plain picture: not loaded, charged or spinning, and without fitted modules; their tiles say what they do.
