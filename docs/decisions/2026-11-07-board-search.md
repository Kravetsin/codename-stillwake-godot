# The board has its own search

- Status: accepted
- Date: 2026-11-07
- Amends [the-board](2026-11-05-the-board.md): how a thing on the board is found

## Context

The board grows to dozens of notes and shapes, and a player looking for one recipe pans and zooms till they spot it.
The build menu has a search, but it finds cards to build, not what the board knows. The user asked for a search on
the research tablet that lists the board's recipes as a name is typed and opens the one clicked.

## Decision

- **Its own field, at the board's top right** (`BoardFind`), apart from the build menu's. Both share one look,
  `shared/SearchField`: a lens, the `/` key's cap while empty and idle, sky while typed in.
- **It finds only what the board shows**: notes and shapes, never the unseen, so it gives nothing away. A shape's name
  is no secret: the pointer already says it.
- **By name, in the language played, as a substring, case ignored** (`BoardSearch.Matching`). Names starting with the
  text come first, then notes before shapes, then by name; at most 8 rows. A fuzzy match finds too much among short names.
- **A pick is a click on the board.** The thing is picked (its ties lit, the strip saying what it takes), put on top,
  and the board glides to it in 0.3 s at a size with words, so the player sees where it went.
- **Keys:** `/` or a click types in it, `/` selecting what was typed so a new name replaces it. The arrows light a row,
  Enter picks the lit one. While typing T, M, Tab, E and B are letters, and the hero stands, as with every field. Esc
  empties the search first and puts the tablet away only then, as in the build menu.
- **It shows while anything stands on the board and no entry is read**: while reading, a click on the board does
  nothing either. Put away or left for the market, it is forgotten.

## Deliberately not handled

- **The market has no search:** it never scrolls, so everything on it is in view.
- **No fuzzy or transliterated match** ("plita" finds no «Плита»): the player types in the language they play in.
- **The list does not follow the board while open:** it is worked out as the text changes, and a find landing mid-typing
  shows at the next keystroke.
