# Releasing

A release is what friends play and what the game updates itself to. It is made on request, never on
every push: the team says "release", and a tag goes up.

1. On `main`, green, tag the next version and push the tag: `git tag v0.4` then `git push origin v0.4`.
   Versions count up `v0.1`, `v0.2`, … ; a patch to one is `v0.4.1`.
2. CI (`.github/workflows/release.yml`) exports the Windows game with that version stamped into it
   (`STILLWAKE_VERSION`, read by `game/Stillwake.Game.csproj`) and publishes
   `Stillwake-windows.zip` as the GitHub release of the tag, with `host.cmd` and `join.cmd` beside the game.
3. Every release asks GitHub for the latest at its start (`game/platform/Updater.cs`). Offered a newer
   one, the Esc menu shows **Update to vX.Y**: the zip is fetched, unpacked into the user folder, and a
   script waits for the game to close, copies the new build over it and starts it again. Saves live in
   the user folder and are left alone.

The version shows at the foot of the Esc menu: `v0.4 · <commit>` for a release, `dev · <commit>` for a
build made at a desk (`play.cmd`, `export.cmd`), which never offers an update. Co-op needs the same build
on every peer: a guest on another version is told to update.

Releases are public, as the repository is. The playtest relay's password is never in a build.
