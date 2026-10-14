# Stillwake

A 2D side-view space game in Godot 4.7 and C#: an always-moving ship salvages debris and turns it into
automated production, in co-op for 4+ players. What the game is: [docs/concept.md](docs/concept.md).
How we work on it: [CLAUDE.md](CLAUDE.md).

The game is being ported from its TypeScript prototype
([Kravetsin/codename-stillwake](https://github.com/Kravetsin/codename-stillwake)):
[docs/plans/port.md](docs/plans/port.md).

## Run

Needs the .NET 8 SDK and Godot 4.7 (.NET build).

```
dotnet build Stillwake.sln
dotnet test Stillwake.sln
godot --path game
```
