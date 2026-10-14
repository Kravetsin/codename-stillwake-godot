# menu (client)

The game menu on Esc. Not in the TypeScript game, which had none: its save and load were the dev
panel's buttons and its suit colours a row under the oxygen bar.

- **Esc, in one place** (`Shared.Escape`, read by `GameMenu._Input` before any other node): the menu
  shown takes it first, Settings going back to the menu's first page and that page closing it; else
  Esc closes the inventory and its crate and puts the plan away (0022), all at once; only with
  nothing open or in hand does it open the menu.
- **Resume** closes it. **Settings** holds the suit's colour (`features/crew`'s `SuitColours`: the one
  worn ringed, those others wear dimmed, a free one a click away sending `crew`'s `PickColour`,
  refused by the sim as before). **Save** writes the world as this machine has it to the `world` slot
  (`user://saves/world.json`, `platform/Saves.cs`); **Load** reads it back as a session of this
  machine's own, at the same speed, and is greyed out with a line saying so unless it plays alone.
  **Quit** quits, as the window's close would, logging the session's last line.
- **The world runs on behind it**, alone too. In co-op every peer runs the same ticks and none may
  stop; alone, a pause would be a second way to run the world for one case. A line in the menu says so.
- **While it is open** the game behind it takes nothing: the menu keeps every key (the hotbar, Tab,
  B, E, the overlays, zoom, the dev panel's backquote), its dark backdrop every click and the wheel,
  and `Ui.MenuOpen` stills the rest: the hero walks on no key held as it opened, and the hands and
  the hero's aim see no pointer, so no ghost, outline, prompt or tip shows behind it.

## Files

| File          | Holds                                                                          | Godot |
| ------------- | ------------------------------------------------------------------------------ | ----- |
| `GameMenu.cs` | the menu, its pages and buttons, and Esc for the whole game                    | yes   |
| `MenuDemo.cs` | `-- --menu-demo`: a script for recordings, in the items demo's room            | yes   |

`game/shared/Escape.cs` (what one press of Esc does) is plain C#, compiled again into `game.tests/`
(`EscapeTests`).

## Deliberately not handled

- Keys rebound, sound, video or language settings: there are none to set yet.
- More than one save slot, or saving while joined to someone else's world: the world is the host's.
- Esc switching an overlay off: an overlay only shows (0022), and Esc with one on opens the menu.
- A button held on the world as Esc opens the menu stays held behind it, with no pointer to work.
