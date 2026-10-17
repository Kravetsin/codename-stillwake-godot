# Registry

Every module of one world, wired through their constructors and listed in run order (`MODULES` in the
TypeScript sim). A new module adds a property, its construction after its dependencies, and its place
in `All`. Tests, the network and the game build worlds from a `new Registry()`: a world gets one of
its own, as modules hold its state.

## The wreck

`Wreck` is the world every new game starts in: the wreck of `Content.Wreck` centred in a world of
`Content.World`'s size, its build zone in the middle (`Layout.InWorld`,
[0048](../../docs/decisions/0048-a-big-world-a-build-zone-inside-it.md)). `WreckMap` is the wreck as
drawn, 192×96 with no zone: dev runs and the tests whose cells are the drawing's play it.

## Migrations

`Migrations.cs` turns a save of any format since `Migrations.Oldest` into this build's
([0031](../../docs/decisions/0031-worlds-the-title-and-migrations.md)), a step a format, each with a
fixture in `Tests/Fixtures`. Format 10 moved worlds into the big world (0048): the save's grid is
centred in `Content.World` by the offset its own size gives, (928, 464) for the wreck, and every cell
saved moves by it — the five layers, machines, floor stacks (their `x`, `y` in thousandths too), cable
ends, plans, aired cells, the crew's spawn, pod and heroes — as does every piece of debris (`x`, `y`,
`start`, `end`, its reel's point); the grid gets the world's zone. A cell off the old grid, which only
the fuzz reports, becomes -1.

Format 14 moved what a furnace was smelting from its fire to crafting's `smelts` (0057), in thousandths of a
tick, dropped the fires kept only for it, and put crafting's slice before building's, as the modules now run.
