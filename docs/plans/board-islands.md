# Design: board islands

## Problem

A board's places grow from what was found and in what order, so no two worlds match. Dragged notes made a mess, and finds
arrive as a pile of ink. A player cannot see what exists, what is close, or what a goal still needs.

## Player experience

1. T opens a screen showing seven district blocks, flowing from Сырьё. Every thing already has its frame, in one of three
   states:
   - unknown: a dim «?»
   - almost: a silhouette with a progress bar and «нужно: …»
   - known: a picture and a name

   Market-only things are tagged «рынок». The look is a crisp screen, as on the mockup
   (https://claude.ai/artifact/CcJq8ohtL66e6HQgQW2azc, «Острова» picked 2026-11-18).
2. Hovering a thing lights its direct ties. A toggle lights its whole upstream chain down to raw.
3. Mining copper ore flashes the edge once and the key shows 1. The side list reads «Добыта медная руда → Медь · Проволока
   (+2 почти)». Clicking the line flies the camera there. New frames glow until hovered, and «прочитать всё» clears them all.
4. Picking a thing fills the bottom strip as today. Its pin button makes its chain glow and opens a checklist of what is
   missing, which ticks off as things arrive. The pin clears itself once its thing is known.

**When it goes wrong:** a craft has all its parts but no machine stands. It stays a silhouette whose «нужно» names the
furnace. Once a furnace stands on any surface, a line «Построена печь → Железо» arrives.

## Decided defaults

- Search finds only what the board names: known and almost things, never an unknown «?».
- The pointer over a «?» says «ещё не открыто», no name (board-names gives way here).
- Contraband, and loot no recipe uses, go to Сырьё.

## Out of scope

- Dragging, player arrangements, player-made chains.
- "Приходит из других районов" chips.
- Who found a thing in co-op.
- A history of read batches.
- Upgrades on the board (they stay as market rows).
- Any sim change.

## Scale

- **World:** every item, blueprint and craft in Content, about 200 frames.
- **Players:** knowledge is world-wide; the read state and the pin belong to each machine.
- **Frequency:** reads 4 times a second, only past a key built from `Knowledge.Version` and every surface's `Grid.Version`.
  Expect a few batches a minute early in play.

## Approach

- **Data:** `logic/BoardDistricts` lists the districts in flow order, each with its lane and its things in order, as ids
  only. It is client presentation, so it lives in the client (a table edit never rebuilds or re-tests the sim). Nothing in
  Content already says it: item groups and build tabs cut the world differently. Whoever adds a planet-stage craft edits
  one table, and a test names anything left without a district or listed twice.
- **`logic/BoardLayout`** replaces `BoardSlots` and `BoardBox`. It is a pure function of Content, built once per run like
  `TechLinks`.
  - Depth: an item that is only found is 0, a recipe is 1 + the max of its parts, a smelted item is 1 + the min of its
    sources. A cycle meets a visited guard and counts 0.
  - Column = depth minus the block's smallest depth. Rows follow the list order. Blocks are sized by what they hold and
    placed by lane and order.
- **`BoardView`:** each frame is Unknown, Almost (`BoardPencil` > 0) or Known (found or granted); a market good carries a
  «рынок» tag on either.
- **`logic/BoardBatches`** replaces `BoardPile`. Each read compares what is known with the last read. What became known
  in one read forms one batch, its cause fixed then and never recomputed (hovering never reshuffles):
  - the items newly obtained in that read, by `Obtained` order: «Добыто: X»; a thing that is itself such an item belongs
    to its own line;
  - else a station newly standing: «Построено: X»;
  - else «Открыто» (a blueprint picked up, a purchase).

  Batches and `Seen` are kept in the board file. A thing is unread while it is in a batch and not in `Seen`; hovering or
  clicking adds it. The key counts batches with an unread thing. The first read with no file counts everything known as
  seen (a new world knows nothing yet; a late guest gets no flood). An old file's `Marked` folds into `Seen`; its `Moved`
  is dropped; `Orders` stays.
- **`logic/BoardChain`:** the upstream chain runs through parts, a craft's station, and one source of a smelted item (one
  that was had, else the shallowest). The pin and the "whole chain" toggle share it.
- **Machine check fix:** the standing-machine check reads every surface through `Registry.On(world, s).Grid`, as
  `Research` does (`BoardPage.ReadOf` and `TabletKey` read only the hero's grid).
- **Look:** a crisp screen built from Control nodes, as on the mockup; frames are made once on open and only a frame whose
  state changed is restyled on a read. New player text in `Research.En.cs` and `Research.Ru.cs`.

## Determinism and co-op

Nothing enters the sim; the board sends no command. Layout depends on Content only, so every peer and every late guest
sees the same places. `Seen` and the pin are kept on each machine, never sent. A late guest or a lost file starts with
everything already seen.

## Trade-offs accepted

- **Hover counts as reading.** A sweep of the mouse clears glows; the side list is the record.
- **A content update can reflow blocks.** Finds never move anything.
- **A cause names what arrived in that read**, not who brought it, nor which of two items mattered more.
- **The chain follows one source of a smelted item.** Alternatives show in the strip.
- **Old board files lose their dragged positions and marks.** Dropping dragging is the rule.

## Tests

Plain C# logic in `game.tests`:

- **Layout:** each Content thing is placed exactly once; no two frames overlap; raw items sit at depth 0 and a craft at
  1 + max; a cycle terminates; the layout does not depend on the world.
- **Batches:** a find brings its crafts under one cause; a craft granted after its machine forms a "built" batch; hovering
  never changes a batch; the counter counts batches; a first read with no file has none.
- **Chain:** missing raw items and machines are listed; a smelted item takes one source.
- **Machine check:** a machine standing only on a planet counts.
- **Files:** an old file's `Marked` reads as seen.

## Cost

- **Modules:** client only.
- **Survive:** `TechLinks`, `BoardSearch` (fed Known and Almost things), `BoardSeen` (without marks), `BoardFile` (adds
  `Pinned`), `BoardKeep` (without spots), `TechCamera`, `BoardPencil`, `BoardPictures`, `FindFlash`, `StripParts`.
- **Gone:** `BoardSlots`, `BoardBox`, `BoardThreads`, `BoardPile`, ink on the board. `BoardInk` stays for the radar.
- **Try:** the plain game with districts, the three states, ties on hover, batches and the pin.
- **Ship:** the `research` README and Files table; a new ADR replacing the slots, threads, dragging and pile parts of the-board, board-pile, board-thoughts,
  board-edges and board-room, and the unknown-frame part of board-names.
- **Budgets:** layout is built once, a read is O(things), and a frame only moves the camera and the glows.
