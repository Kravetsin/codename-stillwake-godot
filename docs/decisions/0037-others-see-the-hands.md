# 0037 — Others see the hands

- Status: accepted; the swing taken from the sample drawn superseded by [0038](0038-one-swing-one-hit.md)
- Supersedes: "Other heroes are not seen holding or swinging the axe" of
  [0034](0034-wreckage-and-the-axe.md), and its echo in [0036](0036-upright-in-space.md)
- Date: 2026-10-16

## Context

A hero was sent to the others (0002, 0013) with its movement, its head's turn and whether it held the
plan. So a friend never saw the fire axe in its hand or swinging at a wreck, the screwdriver turning at
a plan, or the planner beaming plans in: only the wreck cracking and the plan filling, as if by
themselves. The tools will only grow in number.

## Decision

- **The hands go with the hero,** in the same replicated state, outside the sim (0002): the item it
  wields, by id (`Seen.Wields`, `fireAxe`; none for an empty hand), and what the hands are busy with
  and where (`Seen.Busy`: `Work` a plan, `Chop` a wreck, `Beam` plans in; a point in thousandths of a
  cell). A new tool is a new item id, a new kind of work at most a new name: no message changes shape.
- **Drawn by the same code as one's own.** `HeroView.ShowHands` puts what the hero wields and works in
  its figure's hands, this machine's from its aim and buttons, another's as sent; the figure decides
  what an item looks like (only the axe has a picture today).
- **Taken from the sample, never between two.** Positions and the head are drawn between samples; the
  hands are as the sample at or before that moment says.
- **A beam is sent as work while it fades** (`Figure.BeamMs`) and drawn once when it begins, to the
  first cell placed only: the others see one line where the placer sees up to eight.
- Written by name and left out while empty, so an idle hero is sent exactly as before; a kind of work
  not known decodes as no hero at all, as any garbled state does.

## Consequences

- A hero swinging at a drifting wreck is sent every 50 ms, the wreck's point moving: as a walking one is.
- A friend's swing keeps its own pace from when its chop was first seen, not each hit's tick: the
  blade coming down and the wreck's jolt may be up to a swing apart (superseded by 0038: once per swing
  sent, as heard).
- The sounds stay with what is worked: the chop where the wreck takes damage (`DebrisView`), the
  screwdriver where a plan progresses (`BuildingView`), once for every peer whoever works it.
