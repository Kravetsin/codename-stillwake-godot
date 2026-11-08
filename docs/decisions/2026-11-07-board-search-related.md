# The board's search finds what is tied

- Status: accepted
- Date: 2026-11-07
- Amends [board-search](2026-11-07-board-search.md): what a query matches, and how the board shows the list

## Context

Playtesters still could not find things on the board: the search took only a substring of a name in the language
played. Typing a material did not show what makes it or what it goes into, a slip or the other language found nothing.
The user asked for a search that also finds related things, built from data rather than synonym lists.

## Decision

- **Word starts, either language, one slip** (`shared/logic/TextMatch`). Every typed word must start a word of the
  name, case ignored, ё read as е, punctuation as spaces: "стена жел" finds «Стена: железо». A word of 4 letters or more
  forgives one slip (a letter wrong, missing, extra, or two swapped): "furnase" finds Furnace. Shorter words forgive
  none and the middle of a word matches nothing; these keep short queries from flooding, the old ADR's worry. A thing
  is matched by its name in the language played and in the other one (`ItemCards.NameIn`); its row shows the played one.
- **Tiers:** the whole name exactly, then word starts, then with a slip. The best tier's matches are what the query
  names.
- **Ties from the recipes.** When it names at most 3 things, each is followed by what the board would light if it were
  picked: where it is made, what it is made of, what takes it or is made at it (`TechLinks.TieOf`, one hop, the same
  rule as the board's coloured threads, now shared). Weaker matches come after. A thing is listed once, at most 8 rows.
  A broader query lists names only.
- **Only what the board shows** is offered or named, as before: a never-seen thing brings in nothing.
- **Look (picked by the user, candidate D of four):** a tied row is taller and says why under its name in its thread's
  colour ("makes Iron", «делает: Железо»). While the list is open, the board lights the listed things and dims the
  rest, threads included, without moving the camera; closing it or picking a row returns to the pick's own lighting.

## Deliberately not handled

- **Two hops:** iron ore lists iron, not the furnace that smelts it; the player follows the chain through the Iron row.
- **The item's kind** ("Material", "Salvage") matches nothing: it fits dozens of things.
- **Transliteration and the wrong keyboard layout** ("plita", "gkbnf"): either language typed in its own letters works.
- **Slips in words of 3 letters or fewer** and matches inside a word ("cra" no longer finds Scrap).
- **The build menu's search** keeps its own matching.
