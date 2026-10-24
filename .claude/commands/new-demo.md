---
description: Add a dev demo — a scenario class for a `-- --flag` that plays something for a recording or a check by frame.
argument-hint: <--flag> "<what it shows>"
---

User arguments: `$ARGUMENTS` — the flag (`--store`) and what the run shows. Ask for either if missing, and stop.

Read `game/dev/demos/README.md` first: how a run is put together from scenarios, their hooks and their order. Its
"Adding a demo" list is this recipe's source; follow it, then the checks below.

## Steps

1. **Its script or node**, if it needs one: in its feature, `game/features/<name>/<Name>Demo.cs`, or in `game/dev/`,
   `<Name>Demo.cs`. The name must end in `Demo.cs`: release builds drop those. A helper only demos use goes in
   `game/dev/demos/` or ends in `Demo.cs` too.
2. **Its scenario**, in `game/dev/demos/`, in the file of its kind (hero, machine, plan, debris, planet; its own file
   for a family): `[Dev("--flag", "what it shows (NameDemo)")]` on a class deriving `DevScenario`, overriding only the
   hooks it needs — `Map`, `Create`, `Seed` (`Hosted` to seed when hosting too), `Script`, `Start`, `Attach`, `Started`,
   `Frame`. Its flag goes nowhere else. A modifier it reads (`--manual`) goes into `DevModes.All` in
   `game/dev/logic/DevModes.cs`, if not there already.
3. **An `Order`** only where it must win over, or yield to, another scenario's map, world, script or start.
4. **Its row** in the Files table of `game/dev/demos/README.md`, for a new file, or its flag in its file's row.

## Checks before the PR

- `tools/test.sh DevModes game.tests`: the flag parses and is read.
- `dotnet build game/Stillwake.Game.csproj -c ExportRelease`: nothing kept names the demo (CI runs it too).
- A frame or a sheet: `tools/snap.ps1 out.png -- --flag`, or `-Movie <seconds>`; look at it.
- Commit `feat(<feature>): -- --flag …` or `chore(dev): …`.
