# 0043 — A solar panel needs the stars

- Status: accepted
- Supersedes: of [0017](0017-power-runs-on-cables.md), "a solar panel works only in space"
- Date: 2026-10-17

## Context

A panel made power wherever all its cells were space. A room torn open, the wreck's room B or any room
with a hole two cells wide, is space to the air, yet its back wall still stands behind the panel: the
player sees a panel in a room, making power. The light already tells the rule the other way: the
stars shine in only where no back wall is.

## Decision

- **A panel makes power only where it sees the stars:** every cell of it in space, with no back wall
  behind it. A back wall built behind a working panel shades it.
- Maps draw a solar panel with no back wall behind it; every other cell but space keeps one.
- Format 6. Its step takes the back wall from behind every panel that made power, wholly in space, so
  it makes power still; a panel in a room made none and is left as it was.

## Consequences

- The rule is seen without words: a panel on the stars works, one on a wall does not.
- The panel does not yet show that it is shaded; the build ghost and the panel's own look may say so
  later.
