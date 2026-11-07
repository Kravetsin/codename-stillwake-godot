# The right button lets the card go

- Status: accepted; supersedes in part [0108](0108-left-plans-right-removes.md) (the right removing with a card in hand)
- Date: 2026-11-07

## Context

0108 put removal on the right button in the open plan, card in hand or not. Played, the right was pressed out of habit to
put the card down, as in most building games, and marked the wall under the pointer for taking down instead. Nothing let a
card go but clicking it again in the menu; B and Esc put the whole plan away.

## Decision

- **With a card in hand the right button lets it go** (`PlanHand.LetGo`, `Tools.ActOf`): click or drag, it marks, cancels
  and places nothing; the plan stays out. Held on, it shows no removal till released.
- **With nothing in hand the right removes as before** (0108, 0110): a rectangle in the focused layer, cancelling plans if
  begun on one, else marking what is built.
- **Esc lets the card go before it puts the plan away**: a search first, then the card, then the plan (`Escape`).
- The hint says it: "put back" on the right with a card in hand, "take down" with none; by the pointer, "cancel" over a plan
  only with none.
- Wiring and piping keep the right cutting: their card is always in hand, and there is no plan to cancel.

## Rejected

- **Removal as its own card or key, the right never removing** (scheme B of the controls proposal): safer, but every removal
  takes one more step. Kept for later if a second right press still marks things by habit.
- **Click lets go, drag removes**: a click that slips a cell still marks.

## Accepted limitations

- Taking something down with a card in hand is two presses: the right lets the card go, a second right drag removes.
- A habitual double press of the right with a card in hand still marks: the first lets go, the second removes. A mark is a
  plan, cancelled by the right on it, and nothing comes down until a screwdriver works it.
