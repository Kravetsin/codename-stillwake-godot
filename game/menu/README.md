# menu (client)

The title the game opens on and the game menu on Esc (0031). Not in the TypeScript game, which had
neither: its save and load were the dev panel's buttons and its suit colours a row under the oxygen bar.

- **The title scene** (2026-10-25-title-scene, `TitleBackdrop`), none of it the game's world: the game's stars and a torn
  chunk of hull drifting right (`logic/PlateArt`), its hero hunched on it in despair, tethered, a red lamp blinking;
  shards and now and then a limp astronaut going by behind it (`logic/TitleScene`).
- **The title** (`TitleScreen`): the scene alone for a moment, then the name and the choices fading in, words over the
  darkened left (`TitleItem`) — Continue (the last world played; New game on a first run), Worlds, Play together,
  Settings (`SettingsRows`), Quit; any key or click brings them at once. A choice slides its panel out beside it; choosing
  again or Esc closes it. A newer version shows top right with Update; the build at the foot. **Worlds** (`WorldsPanel`):
  New world first, opening into its name; the worlds of `platform/WorldIndex`, each
  a card with its ship as last pictured (`render/WorldShot`, on every save and as the Esc menu opens), when played
  and how long, why it cannot be played; the picked card holds Play and Delete (asked once more), the folder
  going from disk. Any world can be picked (`logic/WorldPick`); Play takes a playable one; a double click plays.
  **Play together** (`TogetherPage`): Host lists the worlds as Worlds does and
  opens the one picked on the relay, its code shown at once (`KeyCard`), kept in Esc; Join by code loads no world
  of its own. While joining, nothing is played.
- **The old save slot:** v0.0.3 and before kept one save, `user://saves/world.json`. Shown, the title
  first takes it in as a world, "Saved before worlds", last played when it was saved
  (`WorldIndex.Adopt`), and renames the slot `world.adopted.json`, so it is taken once and kept.
  A save of a format this build cannot bring up to date shows as such in the list.
- **Testbed**, in a debug build only (`play.cmd`; an export has no button): under the others, it plays
  `dev/Testbed`, as `-- --testbed` does — a ready ship, every machine once, aired and lit, a crate of every
  item, creative mode on. A
  dev world: no world id, so never saved nor listed in Worlds; the menu's exit goes back to the title.

- **Esc, in one place** (`Shared.Escape`, read by `GameMenu._Input` before any other node): the menu
  shown takes it first, Settings going back to the menu's first page and that page closing it; else
  Esc closes the inventory and its crate and the Handbook and puts the plan away (0022), all at once; only with
  nothing open or in hand does it open the menu.
- **Resume** closes it. **Handbook (J)** closes it and opens the Handbook (0066), its line naming
  the key. **Look** opens the look editor (`features/crew`). **Settings** holds the suit's colour (`features/crew`'s
  `SuitColours`, a free one a click away sending `crew`'s `PickColour`), the **slot size**, Normal (40 px) or Large
  (56 px) (`Ui.SlotScale`, 0058: applied at once, kept on this machine in `user://settings.cfg` by
  `platform/Settings`), **heat shimmer** On or Off (0075, `Shimmer.Enabled`, kept there too), the
  **screen** in a window, full screen or frameless (a dev run keeps its window), and **Open logs folder**.
  **Save** writes the world to its folder, the save before kept as its
  backup. **Save & exit to title** saves, leaves (a host's guests let go) and reloads the scene to the
  title; **Save & quit** quits, as the window's close would. A guest keeps nothing: **Leave to title**
  and **Quit**, no Save. Hosting, the code tops it with Copy (`KeyBox`); the foot names the world, when it was saved
  (or why not), why hosting stopped. On the title Esc opens nothing. The world is also saved every five minutes of it.
- **The world runs on behind it**, alone too. In co-op every peer runs the same ticks and none may
  stop; alone, a pause would be a second way to run the world for one case. A line in the menu says so.
- **While it is open** the game behind it takes nothing: the menu keeps every key (the hotbar, Tab,
  B, E, the overlays, zoom, the dev panel's backquote), its dark backdrop every click and the wheel,
  and `Ui.MenuOpen` stills the rest: the hero walks on no key held as it opened, and the hands and
  the hero's aim see no pointer, so no ghost, outline, prompt or tip shows behind it.
- **Logs** are Godot's own (`platform/Logs`): `user://logs/godot.log` a run, the last five kept, a release's too,
  its first line naming the build and the system. A C# exception escaping a callback, a signal or an `async`
  continuation goes there with its stack, the game playing on. Every print is flushed
  (`application/run/flush_stdout_on_print`): a run that dies keeps what it printed.

## Files

| File          | Holds                                                                          | Godot |
| ------------- | ------------------------------------------------------------------------------ | ----- |
| `GameMenu.cs` | the menu, its pages and buttons, and Esc for the whole game                    | yes   |
| `TitleScreen.cs` | the title: its name and choices fading in, the panel slid out, the update notice | yes |
| `TitleItem.cs` | a title choice: words that light and slide, a line under them | yes |
| `TitleBackdrop.cs` | the title scene: the stars, the plate, its hero and lamp, what drifts by | yes |
| `SettingsRows.cs` | the settings kept on this machine, the title's and the game menu's: language, slots, shimmer, logs | yes |
| `WorldsPanel.cs` | New world, the worlds' cards with their pictures, Play, Delete | yes |
| `TogetherPage.cs` | host a world picked or join on the relay, the password kept | yes |
| `KeyCard.cs` | the code over a world as it opens to friends | yes |
| `KeyBox.cs` | the code with Copy, the suits aboard: the card's and the Esc menu's | yes |
| `MenuDemo.cs` | `-- --menu-demo`: a script for recordings, in the items demo's room            | yes   |
| `logic/Together.cs` | playing together as the menu shows it: the mode, the code, the suits aboard; `HostKey` | no |
| `VersionCorner.cs` | the build in the screen's bottom-right corner, faint, to tell a friend or a bug report | yes |
| `logic/TitleScene.cs` | the title scene's arithmetic: the plate's sway, what drifts past, when and how | no |
| `logic/PlateArt.cs` | the title's plate: its size, the hero's seat, lamp and ring | no |
| `Dither.cs` | the title's shade and glow, blended with noise on the final colour so no bands show | yes |
| `logic/WorldPick.cs` | the Worlds panel's pick: any world, so one that cannot be played can be deleted; Play only a playable one | no |
| `logic/Menu.En.cs` | Its player text | no |
| `logic/Menu.Ru.cs` | Russian | no |

`game/shared/logic/Escape.cs` (a press of Esc) is tested by `EscapeTests`; the pick by `WorldPickTests`; the scene by `TitleSceneTests`.

## Deliberately not handled

- Keys rebound, sound or video settings: there are none to set yet.
- Saving while joined to someone else's world: the world is the host's.
- Hosting mid-play, or stopping: Host is on the title; leaving ends it.
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
