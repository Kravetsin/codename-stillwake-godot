# platform (client)

What the game keeps and does on the player's machine, apart from any world: who plays here, the settings, the worlds on
disk, the logs, updates.

## Files

| File | Holds | Godot |
| ---- | ----- | ----- |
| `Profile.cs` | who plays on this machine: a player key made on the first run, kept (0032) | yes |
| `Settings.cs` | the settings kept in `user://settings.cfg`: language, slots, shimmer, volumes, screen, keys, the hero's look | yes |
| `Logs.cs` | Godot's own log files, and opening their folder | yes |
| `Updater.cs` | a newer release asked of GitHub at the start, fetched and restarted into | yes |
| `logic/Screens.cs` | how the window takes the screen, as the settings file names it | no |
| `logic/WorldIndex.cs` | the worlds on disk: their folders, names, when played, whether this build can play them | no |
| `logic/Saves.cs` | save files in a folder, one a slot | no |
| `logic/Roster.cs` | who is who in a world: each player's key and seat | no |
| `logic/Updates.cs` | which build this is, and whether a newer one is out | no |
| `logic/GcPace.cs` | when the game collects its youngest garbage itself | no |
| `logic/Platform.En.cs` | its player text | no |
| `logic/Platform.Ru.cs` | Russian | no |

## Rules

- **Settings** (`Settings`): read once at the start, written at once on a change. A file that cannot be read gives the
  defaults; one that cannot be written keeps the setting for this run alone. A dev run (any game argument) keeps its
  window as its command line made it and plays the default keys, so a frame or a script is the same on every machine.
  - **Screen**: `[video] screen`, `windowed`, `fullscreen` (exclusive) or `borderless` (Godot's non-exclusive full
    screen, a frameless window over the whole screen); an unknown word is a window.
  - **Keys**: `[binds]`, an action's id and Godot's name of its physical key, only those changed from the defaults
    (2026-11-04-key-binds, `shared/logic/Binds`); read by putting each on the defaults in turn, so none is shared.
- **Logs** are Godot's own (`Logs`): `user://logs/godot.log` a run, the last five kept, a release's too, its first line
  naming the build and the system. A C# exception escaping a callback, a signal or an `async` continuation goes there
  with its stack, the game playing on. Every print is flushed (`application/run/flush_stdout_on_print`): a run that
  dies keeps what it printed.

Tested by `ScreensTests`, `BindsTests` (reading the keys back), `WorldIndexTests`, `SavesTests`, `RosterTests`, `UpdatesTests`, `GcPaceTests`.

## Deliberately not handled

- An exception on a thread of our own goes unlogged in a release (Godot logs it in debug builds only): the game starts
  no threads or tasks. A stack overflow's stack reaches only the console.
- Two copies of the game on one machine write the same `godot.log` and `settings.cfg` over each other.
