# The back walls' focus keeps the front faint

- Status: accepted; supersedes in part [back-focus-clear](2026-11-07-back-focus-clear.md) (the front hidden with the back focused)
- Date: 2026-11-12

## Context

With the back walls focused the front was hidden (back-focus-clear). Played, only the back walls were left: no doors,
machines or front walls, and the player lost where the rooms were.

## Decision

- **With the back walls focused the front is drawn faint** (`Tools.FrontInBack`, 25%): walls, doors, machines, cables,
  pipes and the front's plans. Fainter than the old 35% (0110), which covered the back walls.
- **The back walls stay drawn as painted, out of the light** (`Tools.BackUnlit`): that, not hiding the front, is what lets
  a dark room's back walls read.

## Accepted limitations

- The front still tints the back walls under it a little; a back wall's material reads best where nothing stands.
