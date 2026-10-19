# 0072 — Air shows under every build tab

- Status: accepted
- Date: 2026-10-19
- Supersedes in part: [0030](0030-the-build-tab-is-the-mode.md) ("while the Air or Power tab is open its overlay shows instead")

## Context

0030 made the open build tab the mode and kept one overlay at a time: on the Air or Power tab the pipes or cables
showed instead of the air, and O waited for the menu to close. Playing, a player laying pipes to a room wants to see
that room's air while doing it, pressed O on the Air tab, and saw nothing happen: the menu had to be put away to
look, and taken out again to build.

## Decision

- **Air is drawn whenever O wants it** (`Ui.AirOn`), under every build tab. A tab's own overlay, pipes or cables, is
  drawn over it: the air lies behind the walls (`Layers.Air`), the overlays over everything (`Layers.Overlay`).
- **The tab is still the mode.** `Ui.OverlayOn` stays the tab's overlay, else air if wanted: it says what the middle
  button removes, what Q picks and which overlay speaks for the pointer. Under the Air or Power tab the air is drawn
  but says nothing of the room under the pointer; the cables keep their tip.
- The O chip is lit whenever air is wanted, since it is drawn.

## Deliberately not handled

- Two tips at once: the room's air beside the pipes tab's pointer would say something, but two overlays writing the
  one world tip would fight every frame. Close the menu, or open the structure tab, to read a room.
