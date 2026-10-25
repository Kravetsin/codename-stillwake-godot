# menu (client)

The title the game opens on and the game menu on Esc (0031). Not in the TypeScript game, which had
neither: its save and load were the dev panel's buttons and its suit colours a row under the oxygen bar.

- **The title scene** (2026-10-25-title-scene, `TitleBackdrop`): drawn here, none of it the game's world — a torn chunk of hull
  (`logic/PlateArt`) drifting right, its hero hunched on its end, tethered, a red lamp blinking; shards and now and then a
  limp astronaut going by behind it to the left (`logic/TitleScene`); the planet's rim, the broken ship far off.
- **The title** (`TitleScreen`): the scene alone for a moment, then the name and the choices fading in, words over the
  darkened left (`TitleItem`) — Continue (the last world played; New game on a first run), Worlds, Play together,
  Settings (`SettingsRows`), Quit; any key or click brings them at once. A choice slides its panel out beside it; choosing
  again or Esc closes it. A newer version shows top right with Update; the build at the foot. **Worlds** (`WorldsPanel`):
  New world first, opening into its name (the name it gets if left empty shown); the worlds of `platform/WorldIndex`, each
  a card with its ship as last pictured (`render/WorldShot`, taken on every save and as the Esc menu opens), when played
  and how long, why it cannot be played; the picked card holds Play and Delete, asked once more in the card, the folder
  going from disk. Any world can be picked (`logic/WorldPick`); Play takes a playable one; a double click plays.
  **Play together** (`TogetherPage`): Host enters the
  playable world picked (else Continue's, else a new one) and opens it on the relay; Join by code loads no world
  of its own. While joining, nothing is played.
- **The old save slot:** v0.0.3 and before kept one save, `user://saves/world.json`. Shown, the title
  first takes it in as a world, "Saved before worlds", last played when it was saved
  (`WorldIndex.Adopt`), and renames the slot `world.adopted.json`, so it is taken once and kept.
  A save of a format this build cannot bring up to date shows as such in the list.
- **Testbed**, in a debug build only (`play.cmd`; an export has no button): under the others, it plays
  `dev/Testbed`, as `-- --testbed` does — a ready ship with every machine once, both rooms aired, the
  furnace lit, the analyzer at work, a crate holding a full stack of every item a hero can carry (no wreckage), creative mode on. A
  dev world: no world id, so never saved nor listed in Worlds; the menu's exit goes back to the title.

- **Esc, in one place** (`Shared.Escape`, read by `GameMenu._Input` before any other node): the menu
  shown takes it first, Settings going back to the menu's first page and that page closing it; else
  Esc closes the inventory and its crate and the Handbook and puts the plan away (0022), all at once; only with
  nothing open or in hand does it open the menu.
- **Resume** closes it. **Handbook (J)** closes it and opens the Handbook (0066): the line says which key does,
  for a player who has not found it. **Settings** holds the suit's colour (`features/crew`'s `SuitColours`: the one
  worn ringed, those others wear dimmed, a free one a click away sending `crew`'s `PickColour`,
  refused by the sim as before), the **slot size**, Normal (40 px) or Large (56 px), the one in use lit (`Ui.SlotScale`,
  0058: applied at once, kept on this machine in `user://settings.cfg` by `platform/Settings`), **heat shimmer**
  On or Off (0075, `Shimmer.Enabled`, on by default, kept there too), and **Open logs folder**.
  **Save** writes the world to its folder, the save before kept as its
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
| `TitleScreen.cs` | the title: its name and choices fading in, the panel slid out, the update notice | yes |
| `TitleItem.cs` | a title choice: words that light and slide under the pointer, a line under them | yes |
| `TitleBackdrop.cs` | the title scene: space, the planet, the far ship, the plate, its hero and lamp, what drifts by | yes |
| `SettingsRows.cs` | the settings kept on this machine, the title's and the game menu's: slots, shimmer, logs | yes |
| `WorldsPanel.cs` | New world, the worlds' cards with their pictures, Play, Delete | yes |
| `TogetherPage.cs` | host or join on the relay, the password kept                             | yes   |
| `MenuDemo.cs` | `-- --menu-demo`: a script for recordings, in the items demo's room            | yes   |
| `logic/Together.cs` | playing together as the menu shows it: the mode, the session's code, the suit colours aboard (this player's first) … | no |
| `VersionCorner.cs` | the build in the screen's bottom-right corner, faint: which version a player runs, to tell a friend or a bug … | yes |
| `logic/TitleScene.cs` | the title scene's arithmetic: the plate's sway, what drifts past, when and how | no |
| `logic/PlateArt.cs` | the title's plate drawn pixel by pixel, the hero's seat on it | no |
| `logic/WorldPick.cs` | the Worlds panel's pick: any world, so one that cannot be played can be deleted; Play only a playable one | no |
| `logic/Menu.En.cs` | Its player text (`En`) | no |

`game/shared/logic/Escape.cs` (what one press of Esc does) is tested by `EscapeTests`; the pick by `WorldPickTests`; the scene by `TitleSceneTests`.

## Deliberately not handled

- Keys rebound, sound, video or language settings: there are none to set yet.
- Saving while joined to someone else's world: the world is the host's.
- The testbed played together, saved, refilled or given items by a command: it is for looking, and
  starts afresh each time.
- Renaming, copying or exporting a world; loading its backup from the title.
- A world never pictured (saved before pictures were kept, or never opened the menu in) shows a blank card till it is.
- The title scene is the same every time, not the player's own ship: loading a save only to look at it costs a load.
- Esc switching an overlay off: an overlay only shows (0022), and Esc with one on opens the menu.
- A button held on the world as Esc opens the menu stays held behind it, with no pointer to work.
- An exception on a thread of our own goes unlogged in a release (Godot logs it in debug builds
  only): the game starts no threads or tasks. A stack overflow's stack reaches only the console.
- Two copies of the game on one machine write the same `godot.log` over each other.
