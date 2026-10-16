# Plan: title screen, worlds with autosave, save migrations

(Design by design-author; review pending. Mockup: https://claude.ai/artifact/AoXfobeSNA4ELsNpJEaxxQ)

## Problem
The game boots straight into the wreck. One save slot, written only on Save, so a crash or a quit loses the session. Every `World.SaveFormat` bump makes every old save refuse to load. The player cannot keep two worlds.

## Player experience
1. First run: the title over drifting stars and tumbling debris; Continue reads "New game", creates "World 1" in the wreck and plays it.
2. Esc: Resume, Save, Settings, Save & exit to title, Save & quit; foot "World 1 · saved 3 min ago".
3. Next launch: Continue "World 1 · 2 h 14 min" returns to a save at most 5 minutes old.
4. Worlds: new world "Factory". Play together: Host "Factory" enters it, the code in the Esc foot; a friend types it into Join on their own title.
5. After a sim-changing update Continue still loads: the save is migrated as it loads.

Goes wrong: a world saved by a newer build is grey, "Made by a newer version", Play disabled. A load that throws stays on the title with "Failed: <why>"; `world.bak.json` stays on disk.

## Out of scope
Characters apart from worlds; renaming, copying, import/export, Steam Cloud; loading the backup from the UI; pausing behind Esc; downgrades; TypeScript saves and formats below 3; saving on a guest; autosaving dev worlds.

## On disk, `user://saves`
```
worlds/<id>/world.json      World.Save()
worlds/<id>/world.bak.json  the save before it
worlds/<id>/meta.json       {name, created, lastPlayed, playSeconds, version, format}
desync.json                 unchanged
```
Ids: creation time `yyyyMMdd-HHmmss` (`-2` on a clash), never shown. Names: free text, trimmed, ≤ 32 code points, duplicates allowed, empty → "World N". `meta.json` after `world.json`, both written aside then moved; `playSeconds` = `Tick / TicksPerSecond`. Backup: `File.Replace(temp, target, backup)`. An old `saves/world.json` is adopted once as "Saved world". A `characters/` folder can sit beside `worlds/` later.

## Code
Plain C# in `game/platform` (tested): `WorldMeta`; `WorldIndex` (`List` newest first, ties by id; `Create`, `Delete`, `Read`, `Write`, `Adopt`; status per entry from the meta: playable, newer, too old, damaged); `Continue.Pick`; `Autosave` (due every 6000 ticks); `Boot.Of(args)` (the title unless a dev flag picks a world: `--perf --gallery --gym --items --menu-demo --furnace --debris --pod --plan-demo --autopilot --host --join` boot straight in, with no world id, never autosaving).

`Main`: `_Ready` asks `Boot`; dev flags as today; otherwise a `TitleScreen` and no session. `Play(id)` reads, `Migrations.Upgrade`, `World.Load`, session, views; a failure stays on the title. `NewWorld(name)` creates and saves at once. `HostWorld(id)` = Play then Serve. Joining failing reports to the panel instead of quitting. `ExitToTitle` saves (if owned), closes the link, sets `Main.Next` and reloads the scene. A guest whose host leaves goes to the title, "The host left". Quit and the window's close save on the main thread first. `LoadWorld` goes.

Autosave: after `session.Frame`, at a tick boundary, `World.Save()` on the main thread, the write on a task (the `Saves` lock orders writes); exit and quit write synchronously. A failed write is logged; the Esc foot reads "Not saved: <why>".

Menu: `TitleScreen` (CanvasLayer: the column on the left, panels opposite the button; `Stars` and `TitleDebris` behind; version and update at the foot); `WorldsPanel` (rows, Play "<name>", Delete with an in-panel confirmation, New world, grey rows with their reason); `TogetherPage` on the title (Host "<picked or Continue's world>" enters at once; Join by code); `GameMenu`: Resume, Save, Settings, Save & exit to title, Save & quit; foot: world, saved N min ago, the code while hosting; a guest gets Leave to title and Quit. Load, `canLoad` and the Coop page go (`MenuPage.Coop` leaves `Escape`).

## Migrations (`sim/Registry/Migrations.cs`)
`Oldest = 3`; `Steps[i]` turns format `Oldest+i` into the next (`JsonObject` in, out); `Upgrade(text)` runs every step up to `World.SaveFormat`. Only file loads upgrade (Welcome and Resync carry same-build saves). `World.Load` still refuses any other format, so a missing step fails loudly. A new module's slice is written by its step, by hand: empty where a running world allows it, else derived from other slices; a removed module's slice is deleted, renamed fields moved.

## Determinism and co-op
Nothing new enters the sim. Migration runs on the host before the world exists; guests get the migrated world through Welcome. Guests never save. `lastPlayed` is the client's clock, outside the sim.

## Trade-offs
Up to 5 minutes lost on a crash. The save is built on the main thread every 5 minutes, a hitch like a join. A crash between world and meta leaves the meta a save stale. Exit to title reloads the scene. Play time is world time. Hosting starts from the title only. Two windows on one world: last writer wins (dev only).

## Tests
game.tests: list order; meta statuses; Delete; the backup; adoption once; Continue's pick; autosave due at 6000 ticks and reset; Boot per flag. sim/Tests: a fixture of every format 3..SaveFormat upgrades, loads, plays 100 ticks, saves at SaveFormat; a fixture exists for every format; `Steps.Length == SaveFormat − Oldest`; `Upgrade` leaves a current save unchanged.

## PRs
1. Migrations, the format-3 fixture, load through `Upgrade` (before the crafting plan's format bump). 2. World index, backup, adoption; boots into Continue's pick. 3. Autosave, save on quit/close, the new Esc menu. 4. Title with Continue/New game/Settings/Quit, exit to title, Boot. 5. Worlds panel. 6. Play together on the title; leaving to the title.

## Open questions
Settings on the title (the suit colour needs a world): a preferred colour kept by this machine, sent on entering? Hosting from the title only, Stop hosting = exit to title? Host on a first run creates "World 1"? Measure `World.Save()` on the reference world before PR 3.
