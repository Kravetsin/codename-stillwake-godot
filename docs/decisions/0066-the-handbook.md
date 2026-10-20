# 0066 — The Handbook

- Status: accepted; the window (the shell's look, its side list) superseded by [0083](0083-the-handbook-is-a-tablet.md)
- Date: 2026-10-18

## Context

The first co-op playtest asked for a journal of debris: which kinds there are, which we have met, and what each
can drop, with a kind's icon and its possible loot opening once one is broken or caught. Nothing in the game said
which kinds exist or what an engine block holds; a player learnt it only by breaking one. The window that shows it
will grow into the game's reference — a list of crafts and some mechanics — so it is built as a shell with
sections, Debris the first.

From the mockup (`journal-ui`), the picks: a big window in the middle on J (not a tab under the inventory); loot as
slots with their range and a chance pie (not lines of text); loot revealed item by item; kinds not found shown as
silhouettes (not hidden); one journal for the world (not one per player).

## Decision

- **Debris owns the record.** `DebrisState.Found`: every kind found, in the order first found, each a `FoundKind
  { Kind, Items, Looks }`, the items seen come out of it and the looks caught or broken, in the order first seen. Appended only, never sorted: the client
  orders it by `Handbook.DebrisRows` and `Loot.Of`. Saved and hashed; a guest gets it in the welcome. A module of
  its own would have had debris call it from inside its handlers to write a list nothing in the sim reads.
- **When it is written.** On a `Grab` that applies (by hand, or a piece reeled in by harpoon and caught) and on the
  `Hit` that breaks a wreck, after its early return: the kind, and every item that catch's or break's `LootOf` drew,
  kept in a local and given both to the inventory or floor and to the record. Not from what the inventory took: what
  floats off a full inventory was seen too. A scrap chunk or a hull plate, with no `Loot.Of`, records itself. The look the piece wore is recorded too. Nothing
  on a hit that does not break, a reel, a refused command or a floor stack picked up. No new command, refusal, event
  or Rng draw.
- **Looks are the sim's.** A kind drifts in several pictures (twelve scrap chunks, eight crates), and a player must
  know the very one they caught. Which a piece wears was the client's, its id round its pictures; it moves to
  `sim/Content`: `Looks.CountOf(kind)` and `Looks.Of(kind, id)`, the one function the client draws pieces by and
  debris records looks by. The client keeps only the pictures' file names (`DebrisLooks`).
- **Save format 18.** The step writes `debris.found = []`: nothing in an older save says what an item came out of.
- **Presentation is the client's.** `game/Content/Handbook.DebrisRows`: four rows by where to look (salvage and
  wrecks by the ship, the asteroid band, derelicts far out), every kind that spawns in exactly one, beside
  `DebrisLooks`. The sim never reads it.
- **The shell** (`features/handbook`, `HandbookPanel`): a centred window, a side list of sections (a name, an icon,
  a control the section's feature builds), the chosen one's body beside it. J opens and closes it; Esc, Tab and E
  close it. Opening it puts away the inventory, the machine under it and the plan and frees the hand, wired in `Main`
  (`Opening`, and the menu's `busy` and `putAway`), so no feature reaches into another.
- **The Debris section** (`features/debris`, `HandbookPage`): the kinds in their rows, a found one in its picture,
  the rest black (`Modulate`); the chosen kind's look big, a strip of all its looks (those not found black, a click on
  a found one showing it big) with "2/5", its name, hit points or "Caught by hand", and its loot as slots: range in the corner once seen, a pie of its chance, an item not seen yet a black
  silhouette with its pie. Names, "???" and "1 in 4" are said only in the one pointer tooltip.
- **The counter counts looks** found of all there are ("23/81"), not kinds: every look is one to collect and fills
  the journal, while the kinds found already show in the rows as pictures against silhouettes. Its tooltip says both.
- **Found by the menu.** The game menu's "Handbook (J)" line opens it, so a player who never pressed J learns it.

## Consequences

- Worlds saved before begin with an empty Handbook.
- The record is the world's, like what the analyzer learns: a friend's catch fills it for everyone; nothing says
  who found what. Two catches of one kind on one tick are recorded in the host's order.
- The window covers the hero while the world runs: co-op has no pause.
- Next sections plug in the same way: Crafts will read `Research.Recipes()` (known, cost, not analysed) with no new
  sim state; Mechanics, static pages of i18n text, all open (if ever gated, by saved state: a known blueprint, a
  kind found).

## Not done

- No "new" mark: no amber dot on a kind or on the book, no HUD marker, no popup over the hero.
- No "where to look" strip per kind beyond its row, no seconds to break per weapon, no counts of catches, no reward
  for a full Handbook; creative mode reveals nothing more.
- No per-player Handbook, nor any rebuilding of the record for old worlds.
- `docs/concept.md`'s "no tutorial text" is left as it is for now; Mechanics pages will be reference the player opens.
