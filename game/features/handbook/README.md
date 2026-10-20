# handbook (client): the game's reference, on the tablet

The Handbook ([0066](../../../docs/decisions/0066-the-handbook.md)): the game's reference on J, debris now,
crafts and some mechanics later. It mirrors no sim module: it is a shell, and each section is the page of the
feature it speaks of. It is shown on the build menu's rugged tablet
([0083](../../../docs/decisions/0083-the-handbook-is-a-tablet.md)).

- **The tablet** (`shared/Tablet`, the build menu's own): the frame's 9-slice, its plate, stylus, lamp, glass and
  blue wash; J lifts it from below the bottom edge to the middle of the screen in 130 ms and puts it away in 100 ms
  (`TabletMotion`), its screen fading in as the plan's, lit at once when taken out within 1.5 s. Put away, it takes
  no input at once and shows only while it slides down.
- **Its size** (`shared/HandbookScreen`) is fixed by the slot size alone, as the build menu's: 838×376 at the
  normal slot (950×440 at the large), the bezel 52 px more each way. The numbers are `Content/Handbook`'s.
- **The screen** (`HandbookPanel`): the sections as the build menu's tabs (lit sky, a shade lighter under the
  pointer), what the chosen section says at the strip's right (`HandbookSection.Head`, the debris tally); under
  them the chosen section's body, seen through a clipped view and scrolled by the wheel anywhere over the tablet,
  a row of kinds a notch, a thin bar beside it showing where (the thumb only while there is more); along the foot
  the book, "Handbook" and the keys. A section is a `HandbookSection(Name, Body, Head)`: `Body` is a control its
  feature builds and keeps current while it is shown, laid out to the screen's body (`HandbookScreen.BodyWidth`).
- **Sections now:** Debris, `features/debris`'s `HandbookPage` (its README). Next: Crafts, from
  `Research.Recipes()` (known, cost, not analysed); Mechanics, static i18n pages, all open.
- **Keys:** J takes it out and puts it away, in a world with the menu shut. Opening it frees the hand and puts
  away what is open, the inventory, the machine under it and the plan: `Main` hands it that as `Opening`, so the
  shell never reaches into the items or building feature. Esc puts it away through the menu's `Escape` (`Main`'s
  `busy` and `putAway`); Tab and E put it away and go on (the inventory opens, E works what is in reach). The game
  menu's **Handbook (J)** line takes it out too, so a player who never pressed J finds it.
- **Drawn** on its own layer (`ScreenLayers.Handbook`), over the HUD and the bars, under the dev panel and the
  menu. The tablet stops the mouse and the wheel, so a click on it reaches no hand in the world and the wheel no
  hotbar. The world runs on behind it, the hero still walks: there is no pause in co-op.

## Files

| File               | Holds                                                         | Godot |
| ------------------ | ------------------------------------------------------------- | ----- |
| `HandbookPanel.cs` | the tablet's screen: tabs, body, scrollbar, foot; J, Tab and E; `HandbookSection` | yes   |

The book's picture is a placeholder drawn in code (`AsciiArt.Book`) till one is picked. Its layout is tested in
`HandbookTests`.

## Deliberately not handled

- A "new" mark on the book or on a kind: no marker anywhere on the HUD, and the window remembers nothing per
  player (0066).
- The tablet covers the hero while the world runs: a player reading it in danger is in danger.
- B takes the plan out with the Handbook still out: both show, the build menu at the screen's foot, under it.
- No dragging the scrollbar or clicking its track: the wheel reaches everything, as in the build menu.
