# 0039 — Melee by contact

- Status: accepted
- Supersedes: of [0034](0034-wreckage-and-the-axe.md), the left button "held on a wreck within reach"
  (the hero's reach of six cells); of [0038](0038-one-swing-one-hit.md), "a swing begun on a wreck in
  reach" and its one `Hit` to that wreck
- Date: 2026-10-16

## Context

The fire axe swung only while the pointer was on a wreck within the hero's reach, six cells: a remote
control, not a blade. A wreck four cells off was chopped by an axe that never came near it, and a
click in the air did nothing at all.

## Decision

- **The left button with the axe always swings,** wherever the pointer is: aboard, at air, at a
  wreck. The swing is aimed at the pointer as it begins (`AxeSwing.At`), fixed for that swing; held,
  each next swing is aimed anew. The hero faces that point till the swing ends, swimming the other way
  too (`HeroInput.Swings`), so the blade never flips sides mid-swing.
- **The blade hits what it touches as it strikes** (`Aiming.Struck`, client only). At the strike
  (`AxeSwing.HitAt`) the downstroke's last stretch is tested: the sector from the line to the swing's
  point to `AxeSwing.Through` past it, `AxeSwing.Edge` (0.15 rad) wider either side, out to
  `Axe.Reach` from the shoulder of the body upright, the lean ignored. Never the raised stretch over
  the head. It is sampled at three distances every 5° against each wreck's aim box, its picture and a
  quarter-cell margin, square to the world. Only out in space, only wrecks: each wreck touched gets one
  `Hit` per swing, if the sim would take it; a swing touching nothing sends nothing.
- **The arc's shape has one source:** `AxeSwing.Raised` and `Through` draw the swing and bound the
  contact. Only the reach is new content: `Axe.Reach`, 2000 thousandths of a cell, the arm (0.69 of a
  cell) and the axe's picture (1.2 cells) end to end.
- **One melee item, one table:** `Axe` keeps its constants and `Hit(Piece)` its shape (principle 2).
  A second item that deals damage turns `Axe` into a table by item and adds the item to `Hit`.
- **A small piece is still caught:** by E, or by a click on one within reach, as before; that click,
  and the hold it begins, swing nothing. A click anywhere else swings.
- **Aimed at as it would be struck:** with the axe, the outline and "[Hold LMB] Chop" mark only the
  first wreck, in spawn order, that a swing begun now toward the pointer would touch (`Ui.AimedPiece`
  is one piece). A wreck under the pointer out of the blade's reach is not marked.
- **Others swing toward the point too:** `Busy.At` is the swing's point (the wire is unchanged), and the
  last chop heard is kept with its swing number while the hero's hands are idle (`Busy.Chopping`), so a
  friend's figure swings toward the right point though its `Busy` ends right after.
- **Aboard, the swing plays and touches nothing.** No sound of its own: a chop is heard only where a
  wreck takes it (0034).

## Consequences

- Chopping means swimming up to the wreck: from four cells the blade cuts air, from one and a half it
  bites.
- The mark is judged by the pointer now, the strike by the point fixed at the swing's start, about a
  quarter second later (`HitAt` of a 500 ms swing, 227 ms); a pointer swept between them may mark a
  wreck the swing then misses, or the reverse. A wreck drifts some 0.05 of a cell meanwhile, inside the
  margin.
- The lean (up to 0.3 of a cell at the shoulder) and a wreck's rocking are ignored: the contact is the
  upright body against the still picture.
- The sim still does not check reach (principle 6): it takes any `Hit` from a hero in space.
- Swinging toward a point straight above or below the hero keeps the side it faced: the pointer's
  dead zone decides no side.
- Two wrecks overlapping in the blade's way both crack from one swing; the host's order and the hit
  points decide as for two heroes' hits.
