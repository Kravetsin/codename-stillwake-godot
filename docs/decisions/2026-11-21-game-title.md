# The game is called Castoff

- Status: accepted
- Date: 2026-11-21

## Context

Stillwake was a codename, too close to *Still Wakes the Deep* for a Steam title. On 2026-11-11 the user picked
**Castoff**: a thrown-away thing (salvage), an outcast (the escaped slave with the tracker), and "cast off" (leaving port,
the escape). No game, itch page or jam entry was found named exactly that; trademark and domain are not checked yet.

## Decision

- **What a player sees says Castoff:** the window (`config/name`), the title's name, the log's first line, the exe and its
  metadata, the macOS app and bundle id, the release zips and the GitHub release's title, the co-op launchers.
- **The code keeps Stillwake:** namespaces, assemblies, the solution, the repository, the `STILLWAKE_*` variables.
  Renaming them costs a diff through every file and buys a player nothing.
- **The user folder moves once.** Godot names it after `config/name`, so the rename left everyone's worlds and settings
  in `app_userdata/Stillwake`. On the first start `UserFolder.Adopt` moves every entry the new folder lacks; what both
  hold stays (Godot writes `logs` before any code runs). It runs in `Main`'s static constructor, before any field reads
  the folder.
- **One update by hand.** A build from before the rename updates by restarting `Stillwake.exe` (`Stillwake.app` on
  macOS); the new zip holds `Castoff.exe`, so the old updater restarts the old build. Testers download the first
  Castoff release by hand once; from then on the updater works again.

## Consequences

- Not handled: a bridge release that teaches old updaters the new name (two releases to spare one download), and
  running an old build again after the move (it starts with an empty Stillwake folder).
- Old releases and their zips keep the name Stillwake on GitHub.
