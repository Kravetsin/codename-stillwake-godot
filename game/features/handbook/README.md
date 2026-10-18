# handbook (client): the game's reference, one window

The Handbook ([0066](../../../docs/decisions/0066-the-handbook.md)): a big window in the middle of the screen
on J, which will grow into the game's reference — debris now, crafts and some mechanics later. It mirrors no sim
module: it is a shell, and each section is the page of the feature it speaks of.

- **The shell** (`HandbookPanel`): a header (the book, "Handbook", the J keycap), on the left the sections, a row
  each with its icon and name, the chosen one lit, and on the right the chosen section's body. A section is a
  `HandbookSection(Name, Icon, Body)`: `Body` is a control its feature builds and keeps current while it is shown.
  Nothing else: no tabs of its own, no search, no layout but the side list.
- **Sections now:** Debris, `features/debris`'s `HandbookPage` (its README). Next: Crafts, from
  `Research.Recipes()` (known, cost, not analysed); Mechanics, static i18n pages, all open.
- **Keys:** J opens and closes it, in a world with the menu shut. Opening it frees the hand and puts away what is
  open, the inventory, the machine under it and the plan: `Main` hands it that as `Opening`, so the shell never
  reaches into the items or building feature. Esc closes it through the menu's `Escape` (`Main`'s `busy` and
  `putAway`); Tab and E close it and go on (the inventory opens, E works what is in reach). The game menu's
  **Handbook (J)** line opens it too, so a player who never pressed J finds it.
- **Drawn** on its own layer (`ScreenLayers.Handbook`), over the HUD and the bars, under the dev panel and the
  menu. Its window stops the mouse, so a click on it reaches no hand in the world. The world runs on behind it,
  the hero still walks: there is no pause in co-op.

## Files

| File               | Holds                                        | Godot |
| ------------------ | -------------------------------------------- | ----- |
| `HandbookPanel.cs` | the window, the side list, J, Tab and E; `HandbookSection` | yes   |

The book's picture is a placeholder drawn in code (`AsciiArt.Book`) till one is picked.

## Deliberately not handled

- A "new" mark on the book or on a kind: no marker anywhere on the HUD, and the window remembers nothing per
  player (0066).
- The window covers the hero while the world runs: a player reading it in danger is in danger.
- B takes the plan out with the Handbook still open: both show, the build menu at the screen's foot, under it.
