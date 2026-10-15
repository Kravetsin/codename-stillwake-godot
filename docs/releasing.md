# Releasing

A release is what friends play and what the game updates itself to. It is made on request, never on
every push: the team says "release", and a tag goes up.

## Versions

Semantic, `MAJOR.MINOR.PATCH`, so a player can tell where the game stands:

- `0.0.x` — playtests: whatever is ready, released when the team wants friends on it. Most releases.
- `0.x.0` — a milestone the team names: a minor version up, the patch back to 0.
- `1.0.0` — the game released. Before it, everything is a work in progress.

## Steps

1. Run `release.cmd` (`release.cmd minor`, `release.cmd major`, or `release.cmd v0.1.0` for another
   step). It fetches `origin/main` and the tags, takes the next version after the latest, refuses while
   `main`'s CI is not green (when the GitHub CLI can tell), asks to confirm, then tags `main` and
   pushes the tag. Two releasing at once cannot clash: the second tag push is refused, and running it
   again takes the next number. Say in the chat who releases, all the same.
2. CI (`.github/workflows/release.yml`) exports the Windows game with that version stamped into it
   (`STILLWAKE_VERSION`, read by `game/Stillwake.Game.csproj`) and publishes
   `Stillwake-windows.zip` as the GitHub release of the tag, with `host.cmd` and `join.cmd` beside the game.
3. Every release asks GitHub for the latest at its start (`game/platform/Updater.cs`). Offered a newer
   one, the Esc menu shows **Update to vX.Y.Z**: the zip is fetched, unpacked into the user folder, and a
   script waits for the game to close, copies the new build over it and starts it again. Saves live in
   the user folder and are left alone.

The version shows in the screen's bottom-right corner and at the foot of the Esc menu: `v0.0.2` for a
release, `dev <commit>` for a build made at a desk (`play.cmd`, `export.cmd`), which never offers an
update. Playing together, the corner also shows how long this player's actions take to happen. Co-op
needs the same build on every peer: a guest on another version is told to update.

Releases are public, as the repository is. The playtest relay's password is never in a build.
