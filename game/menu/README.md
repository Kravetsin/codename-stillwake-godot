# menu (client)

The title the game opens on and the game menu on Esc (0031). Not in the TypeScript game, which had
neither: its save and load were the dev panel's buttons and its suit colours a row under the oxygen bar.

- **The title** (`TitleScreen`), with no arguments: Continue (the last world played, its name and play
  time; New game on a first run), Worlds, Play together, Quit, a column on the left, centred, over its
  own space — stars streaming by, wreckage tumbling past — so nothing of the game behind shows. Worlds
  and Play together open a panel opposite their button, closed by pressing it again. The version,
  Open logs folder and the update at its foot. **Worlds** (`WorldsPanel`): the worlds of `platform/WorldIndex`, the last
  played first, a grey row with why for one this build cannot play; Play the one picked, Delete it after
  a confirmation in the panel, New world by name. **Play together** (`TogetherPage`): Host enters the
  world picked (else Continue's, else a new one) and opens it on the relay; Join by code loads no world
  of its own. While joining, nothing is played.
- **Testbed**, in a debug build only (`play.cmd`; an export has no button): under the others, it plays
  `dev/Testbed`, as `-- --testbed` does — a ready ship with every machine once, both rooms aired, the
  furnace lit, the analyzer at work, a crate holding a full stack of every item, creative mode on. A
  dev world: no world id, so never saved nor listed in Worlds; the menu's exit goes back to the title.

- **Esc, in one place** (`Shared.Escape`, read by `GameMenu._Input` before any other node): the menu
  shown takes it first, Settings going back to the menu's first page and that page closing it; else
  Esc closes the inventory and its crate and puts the plan away (0022), all at once; only with
  nothing open or in hand does it open the menu.
- **Resume** closes it. **Settings** holds the suit's colour (`features/crew`'s `SuitColours`: the one
  worn ringed, those others wear dimmed, a free one a click away sending `crew`'s `PickColour`,
  refused by the sim as before) and **Open logs folder**. **Save** writes the world to its folder, the save before kept as its
  backup. **Save & exit to title** saves, leaves (a host's guests let go) and reloads the scene to the
  title; **Save & quit** quits, as the window's close would. A guest keeps nothing: **Leave to title**
  and **Quit**, no Save. The foot names the world, when it was saved (or why not), and the code while
  hosting. On the title Esc opens nothing. The world is also saved every five minutes of it.
- **The world runs on behind it**, alone too. In co-op every peer runs the same ticks and none may
  stop; alone, a pause would be a second way to run the world for one case. A line in the menu says so.
- **While it is open** the game behind it takes nothing: the menu keeps every key (the hotbar, Tab,
  B, E, the overlays, zoom, the dev panel's backquote), its dark backdrop every click and the wheel,
  and `Ui.MenuOpen` stills the rest: the hero walks on no key held as it opened, and the hands and
  the hero's aim see no pointer, so no ghost, outline, prompt or tip shows behind it.
- **Logs** are Godot's own (`platform/Logs`): `user://logs/godot.log` for each run, the last five
  kept, written by a release too; its first line after Godot's names the build and the system. Godot
  writes every C# exception escaping a callback, a signal or an `async` continuation there with its
  stack, and the game plays on. Every print is flushed (`application/run/flush_stdout_on_print`), so
  a run that dies outright keeps what it printed up to then.

## Files

| File          | Holds                                                                          | Godot |
| ------------- | ------------------------------------------------------------------------------ | ----- |
| `GameMenu.cs` | the menu, its pages and buttons, and Esc for the whole game                    | yes   |
| `TitleScreen.cs` | the title: its buttons, its panels opposite them, its space               | yes   |
| `WorldsPanel.cs` | the worlds, Play, Delete, New world                                        | yes   |
| `TogetherPage.cs` | host or join on the relay, the password kept                             | yes   |
| `MenuDemo.cs` | `-- --menu-demo`: a script for recordings, in the items demo's room            | yes   |

`game/shared/Escape.cs` (what one press of Esc does) is plain C#, compiled again into `game.tests/`
(`EscapeTests`).

## Deliberately not handled

- Keys rebound, sound, video or language settings: there are none to set yet.
- Saving while joined to someone else's world: the world is the host's.
- The testbed played together, saved, refilled or given items by a command: it is for looking, and
  starts afresh each time.
- Renaming, copying or exporting a world; loading its backup from the title.
- Esc switching an overlay off: an overlay only shows (0022), and Esc with one on opens the menu.
- A button held on the world as Esc opens the menu stays held behind it, with no pointer to work.
- An exception on a thread of our own goes unlogged in a release (Godot logs it in debug builds
  only): the game starts no threads or tasks. A stack overflow's stack reaches only the console.
- Two copies of the game on one machine write the same `godot.log` over each other.
