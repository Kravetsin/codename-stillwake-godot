# The board is fixed district islands on a grid

- Status: accepted
- Date: 2026-11-18
- Supersedes in part: [the-board](2026-11-05-the-board.md) (places grown from what was found and in what order),
  [board-pile](2026-11-02-board-pile.md) (the pile of unread notes), [board-thoughts](2026-11-02-board-thoughts.md) (the
  hand-drawn loop and threads to the three closest), [board-edges](2026-11-02-board-edges.md) and
  [board-room](2026-11-02-board-room.md) (slots, dragging and the growing box), [board-names](2026-11-08-board-names.md) (an
  unknown frame named)

## Context

Players found the board hard to manage. Where a note stood depended on what was found and in what order, so no two worlds
matched. Dragged notes made a mess, and many finds at once arrived as a pile of ink. A player could not see what exists, what is
close, or what a goal still needs, while the board is where they spend most of their time and how a newcomer learns what is made
from what. The user picked «Острова» on a mockup (https://claude.ai/artifact/CcJq8ohtL66e6HQgQW2azc) and asked for a screen
rather than ink, as everything in the tablet is ([tablet-windows-are-screens](0083-the-handbook-is-a-tablet.md)).

## Decision

- **Seven districts, every thing in one place from the start.** Сырьё, Плавка и материалы, Детали, Машины, Стены и постройки,
  Ферма и еда, Снаряжение (`BoardDistricts`, client data: a table edit never rebuilds the sim). Every blueprint, craft and
  item has exactly one frame, so a find never moves anything and two worlds look alike.
- **Columns by steps from raw, worked out by code** (`BoardLayout`). An item only found is 0, a recipe 1 + the most of its
  parts, a smelted item 1 + the least of its sources; a cycle counts 0. No hand coordinates, since crafts are provisional. The
  steps are not labelled: shown, they read as an order the player must follow.
- **A grid of big cells and gutters.** A frame is 3 by 3 cells of 30 px and stands on a big cell of a 120 px pitch; blocks and
  lanes keep to the same pitch, and the background draws the pattern, so the board looks laid out by hand.
- **Three states.** Unknown is a dim «?» saying only «ещё не открыто»; almost is a silhouette with a progress bar and
  «не хватает: N» (every part never had and the machine to stand; the pointer lists them); known is a picture and a name. A
  silhouette with nothing missing says «можно получить». Market goods are tagged «рынок» and never almost.
- **Ties only on hover or pick**, in four colours (from, into, goal, new); a toggle lights the whole chain down to raw.
- **Finds as cards.** Each read that made something known is a batch, its cause fixed then (obtained, built, else opened). Its
  card shows what opened as pictures, what came closer (almost with more parts had than before) as silhouettes, and the cause
  small under them. Each picture flies to its frame; new frames glow until hovered; the key counts unread cards. The newest 40
  are kept per machine.
- **A pinned goal** lights its chain and lists what it still lacks; it clears itself once known.
- **A machine counts on any open surface**, as research grants: the board reads every open surface's grid.

## Deliberately not handled

- **No dragging or player arrangement**: the mess is what this removes.
- **Old board files lose their positions and loops**; their marks fold into what was seen, so nothing floods.
- **Hover counts as reading**: a sweep of the mouse clears glows; the cards are the record.
- **A content update can reflow blocks.** Finds never do.
- **The chain follows one source of a smelted item**; the strip shows the others.
- **Who found a thing in co-op is not shown**, nor a history past 40 cards.
