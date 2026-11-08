# A macOS build

- Status: accepted
- Date: 2026-11-04

## Context

A friend on a Mac should be able to play. Nothing in the game stands in the way: the sim and the session are plain
.NET without floats, so a Mac and a Windows peer compute the same world, ENet runs everywhere, and Godot 4.7's .NET build
exports to macOS. Only the release was Windows-only: one export preset, and an updater that hands over to `cmd.exe`.

## Decision

- **One universal app.** The `macOS` preset exports `Stillwake.app` for Apple Silicon and Intel at once, from the same
  Linux runner as the Windows build. Apple Silicon needs ETC2/ASTC textures, so the project imports them too
  (`rendering/textures/vram_compression/import_etc2_astc`).
- **Ad-hoc signed, not notarized.** Godot's built-in signer signs the app ad hoc, which Apple Silicon needs to run it at
  all, with library validation off so the .NET runtime loads. There is no Apple Developer account, so no notarization:
  the first launch of a downloaded app is blocked by Gatekeeper, and the player opens it once with right-click → Open (or
  `xattr -cr Stillwake.app`).
- **Released beside Windows.** `Stillwake-macos.zip` holds the app and `host.command` / `join.command`, the co-op
  launchers. Each build updates from its own platform's zip, by its suffix.
- **The updater on macOS** writes a shell script that waits for the game to close, unpacks with `ditto` (Godot's zip
  reader would drop the execute bits), puts the new app in place of the old bundle and opens it. Files downloaded by the
  game carry no quarantine mark, so an update opens without Gatekeeper's prompt.

## Deliberately not handled

- **Notarization and a Developer ID**: it costs a yearly account and buys only a smoother first launch for friends.
  Revisit for a Steam release.
- **The launchers are not updated** by the macOS updater: it replaces only the app, which may live in Applications
  without them. They change rarely; a new zip brings them.
- **No macOS desk export in `export.cmd`**: a zip made on Windows loses the execute bits. The Godot command in
  `docs/releasing.md` makes one by hand.
- **Not yet run on a Mac.** The export is checked from Windows; the first friend on a Mac is the test.
