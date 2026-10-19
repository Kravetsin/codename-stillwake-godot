# 0073 — A drop chance is a percent

- Status: accepted
- Date: 2026-10-19
- Supersedes in part: [0066](0066-the-handbook.md) (the chance pie on a loot slot)

## Context

0066 drew a loot slot's chance as a small pie in its top-left corner, words being kept for the tooltip. Playing, the
pie read as a timer running down: a filled segment from twelve o'clock is how a cooldown is drawn, so a slot looked
like something waiting rather than something likely.

## Decision

- **The slot says its chance as a percent** in its top-left corner, in the same small mono text as the range in the
  bottom-right corner: a hundred over its one-in-n, a half rounded up (1 in 3 is "33%", 1 in 8 is "13%";
  `LootSlot.Percent`). A slot with no roll shows nothing there, as it had no pie.
- **The tooltip says both:** "25% (1 in 4)", the odds as a player may count them too.

## Deliberately not handled

- A chance under half a percent would read "0%". No loot rolls that rarely (the rarest is 1 in 6); a rarer one would
  need a "<1%" here first.
