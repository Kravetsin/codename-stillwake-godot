# The board says what the hero is thinking

- Status: accepted; a shape's count of what it lacks superseded by [one-to-understand](2026-11-12-one-to-understand.md)
- Date: 2026-11-02
- Amends [the pile](2026-11-02-board-pile.md): what the board says after a pile, and [tablet-key](2026-10-31-tablet-key.md): how long a mark lasts

## Context

After playing the pile, the user found four things. The words saying what a shape lacks appeared once and went. The numbers
needed to work something out were headed "Made from", which reads as a recipe, and nothing explained why the hero wanted
more of something. Marks went when the tablet closed, and new things got lost. Picking a furnace drew lines to everything
that costs iron.

## Decision

- **Every shape says what it lacks, in its box, always.** It stands where a note has its name: the first lack only, "2 more"
  and the picture of what. A name would not fit a box at the zoom that frames a board, and a picture reads at any size. What
  was never had shows "?". Beside a shape, 30 of them would lie on each other, so the words that were written once beside the
  closest shape after a pile are gone.
- **The strip speaks as the hero.** On a shape its numbers are headed "I need more of these to work it out", or "Enough of
  these, now somewhere to make it". "Made from" stays for what is known, where it is the recipe.
- **A mark lasts until the player presses the thing.** A click or a drag counts: either way they have it in hand. It lasts
  through a close and a restart, in the board's file. A thread stands out only until the tablet closes, or a heap of white
  lines would stay.
- **A pick ties only what is direct.** What it takes, where it is made, what takes it, and what comes out of it. A furnace
  ties to iron and not to what iron goes into; the player follows the chain. So iron now says it is made at a furnace. The
  market's rings use the same ties.

## Deliberately not handled

- **Clearing many marks at once:** after "Take all" on 40 finds each loop goes by its own press. The user prefers that nothing
  is lost; a button can come if it grates.
- **More than the first lack in the box:** there is room for one. The strip lists them all.
- **A shape's words under the zoom where words go:** only the picture shows there, as with a note's name.

## An idea, not built

The player's own chains: a place to choose cards, connect them and keep a chain to remember, once there are many resources
and it gets easy to lose track. It is no trouble yet. The user's idea was a tab with a constructor; threads of the player's
own on this board may be enough, since cards already move.
