# 0031 — Worlds, the title and migrations

- Status: accepted
- Date: 2026-10-16

## Context

The game booted straight into the wreck, kept one save slot written only on Save, lost the session
on a crash or a quit, and refused every save once the save format changed. The player could not keep
two worlds, nor tell what Load would bring back.

## Decision

- **A title screen** opens the game with no arguments, loading nothing: Continue (the last world
  played; New game on a first run), Worlds, Play together, Quit; panels open opposite their button.
  Any argument boots straight into a world as before, for the dev runs and recordings.
- **Worlds are folders** under `user://saves/worlds/<id>/`: `world.json`, the save before it as
  `world.bak.json`, and `name.txt`. The list reads a save's first keys (`format`, `tick`) and the
  file's time; nothing else is kept about a world. A world a build cannot play is listed grey with why.
- **Saving:** every five minutes of world time, on Save, on exit to the title, on quit, on the
  window's close and before an update restarts the game; written on the main thread at a tick
  boundary. Only the machine that keeps the world saves: a guest never does.
- **The Esc menu** is Resume, Save, Settings, Save & exit to title, Save & quit (a guest: leave and
  quit); its foot names the world, when it was saved, and the code while hosting. Exit to the title
  reloads the scene.
- **Play together** lives on the title: hosting enters the world picked (or Continue's, or a new one);
  joining loads no world of its own. Stopping hosting is leaving to the title.
- **Migrations:** every bump of the save format ships a step turning a save of the format before into
  the next (`sim/Registry/Migrations.cs`), run on saves read from disk; a fixture save of every format
  since 3 must upgrade, load and play on.
- **Characters apart from worlds come later;** blueprints belong to the world.

## Consequences

- An update no longer wipes progress, so long as each format bump writes its step and fixture.
- A crash loses at most five minutes.
- Settings are only in the world: the suit colour needs one.
