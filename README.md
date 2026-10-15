# Stillwake

A 2D side-view space game in Godot 4.7 and C#: an always-moving ship salvages debris and turns it into
automated production, in co-op for 4+ players. What the game is: [docs/concept.md](docs/concept.md).
How we work on it: [CLAUDE.md](CLAUDE.md).

The game is being ported from its TypeScript prototype
([Kravetsin/codename-stillwake](https://github.com/Kravetsin/codename-stillwake)):
[docs/plans/port.md](docs/plans/port.md).

## Run

Needs the .NET 8 SDK and Godot 4.7 (.NET build); the scripts look for Godot in
`%USERPROFILE%\Tools\godot-4.7.2\` unless `STILLWAKE_GODOT` names the exe.

- `play.cmd` — builds the C# and starts the game; arguments go to the game: `play.cmd --host`,
  `play.cmd --join=192.168.1.5`, `play.cmd --debris`.
- `export.cmd` — the Windows build, `build\windows\Stillwake.exe`, zipped as `build\Stillwake-windows.zip`,
  with `host.cmd` and `join.cmd` beside it: co-op across networks through the playtest server's relay
  (ADR 0012), each asking for a session name and the playtest password. By hand:
  `Stillwake.exe -- --relay --host=name` and `Stillwake.exe -- --relay --join=name`, the password in
  `STILLWAKE_PASSWORD` or `--password=...`; `--relay=ws://...` names another relay.
- `dotnet test Stillwake.sln` — every test.
