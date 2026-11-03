# Cast scrap: scrap walls of smelted scrap, rust telling them apart

- Status: accepted
- Date: 2026-11-03

## Context

The first playtest up to the landing found hull walls of raw chunks only: a chunk a cell, while the scrap a furnace smelts out
of a chunk (four of it) went into wire, nails, floors and ladders. A player with a furnace wanted to build walls of it too.

## Decision

- **A second set of scrap walls, `castScrap`:** a wall and a back wall of 2 smelted scrap, in every other number the same as
  patched scrap's (strength 2, leak 30, armour 2, 7 ticks). Smelted, a chunk makes two of them against one patched wall; the
  patched wall stays the way to mend a hole with no furnace. Its cards stand after scrap's. It is learned by the rule, as every
  wall is, once a castaway holds the scrap.
- **Rust tells them apart (the user's pick):** every patched scrap wall now carries the wreck's speckled rust, grown from its
  seams by the wreck's own code (`AnimeSheetRust` in `tools/anime-art/subjects/wreck.js`), lighter than the wreck's dress so
  the wreck's roof still reads oldest. Cast scrap is the same sheet of plates with no rust at all.
- It is a material of its own, not `As: "scrap"`: taken down, it gives back its own two scrap, so smelting and dismantling make
  no loop.

## Consequences

- Patched walls a player builds inside the wreck now match it; a cast one shows as new metal.
- The back walls are alike for both: rust on a back wall that dark would not show. Accepted.
- Strength stays the same by choice: iron remains the upgrade of the hull, cast scrap only its cheaper, cleaner scrap.
