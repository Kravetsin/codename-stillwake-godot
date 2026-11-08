# The pickaxe cuts close by

- Status: accepted; amends the fan of [the-pickaxe](2026-11-07-the-pickaxe.md)
- Date: 2026-11-07

## Context

Played, the pickaxe's swing showed a cut far too wide for a thin point: a swing's dust lies from the blade's reach out to its
weapon's range (`AirCuts`, tool-reach), and the pickaxe's point passes 30.6 px from the shoulder while its range of 9 cells was
72 px, a band over 30 px deep. The user asked for a thin cut, a shorter reach, and a somewhat wider zone close to the hero.

## Decision

- **The pickaxe's fan is 48 steps wide (67.5°), 9 rays, 4.5 cells long**, the axe's range: from 3.5 cells it takes five rows of
  a face against the drill's three; from 4.5 cells, none, where the drill still takes five.
- **Its range is its point's**: 36 px against the point's 30.6, so its dust is a band of about 6 px along the point's path. A
  test keeps the range within a cell past the drawn point.
- Its block damage, pace and dust are unchanged; no per-tool dust is added: the range alone makes the cut thin.

## Consequences

- The pickaxe digs the face beside the hero and no farther; the drill stays the long reach.
