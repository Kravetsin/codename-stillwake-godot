# The tool in hand is the one that works

- Status: accepted; supersedes "A tool works with its stack" (which stack) and "Seen as the sim decides" (from which stack)
  in [0098](0098-the-tool-bench-modules-in-tools.md)
- Date: 2026-11-14

## Context

0098 let the sim find a tool's stack by its item: the first one carried with a module, else the first one. With two drills
the fitted one drilled, whichever was in hand. The user carried an arc spear and took a plain one to compare. The plain one in
hand sparked and looked fitted, and it hit as fitted too, as `Damage`, `BlockDamage` and the run's draw all read that stack.
The drill did the same. The sim was never told which slot the hand holds, so it guessed.

## Decision

- **The sim knows the hand.** `Wield(Slot)` (`sim/Items/Hand.cs`) sets the hotbar slot the sender's hero holds. It is refused
  `items.notHotbar` off the hotbar. The client sends it while its chosen slot (`Ui.Chosen`, set by the inventory panel with
  `Wielded`) differs from `Wielding(peer)`, first in `UseHands`, before any run or hit of the frame, as `Run` is sent.
- **The hand is a slot, not a stack.** A stack moved into that slot is now in hand, as the hotbar shows it. Dropping it
  empties the hand.
- **`ToolStack(peer, item)` is the stack in hand while it is that item, else none.** No other stack is ever chosen. Then:
  a hit with a tool not in hand deals the weapon's own damage, as one with none carried did (0034); `Run` of one is refused
  `items.notHeld`; a run stops on the tick the hand no longer holds its item; `Fire` of a gun not in hand is `creatures.noGun`.
- **Saved** as `ItemsState.Hands`, the peers whose hand is not the first slot, written `wielding` only while some is. A
  save from before has every hand on the first slot, the client's own start, so no format bump. It travels with the peer
  from surface to surface, with its run.
- **Seen as the sim decides, still:** `Figure.Fitted` reads `ToolStack`, so one's own hero and a friend's show the stack in
  hand. The spear's arc (spear-effects) and the drill's crackle follow it with no change of theirs.

## Considered

- **The slot on each command that uses a tool** (`Run`, `Strike`, `Fire`, `Hew`, `HewCells`, `HewWrecks`, `HewPieces`):
  seven commands and their senders changed. A friend's figure would still not know the slot without a new field in the hero's
  replicated state. One hand in the sim serves every command and every view.
- **Falling back to the first carried stack** when the hand holds another item: only tests and the fuzz hit with a tool not
  in hand; a player cannot. Dropped, so the sim never guesses again.

## Not handled

- **The one tick after a switch**, before the host applies `Wield`, the own figure may show the hand's previous stack: a
  tick, unseen.
