# Registry

## Files

| File | Purpose |
| --- | --- |
| `Migrations.cs` | Old saves made loadable (0031): each step turns a save of one format into the next, the JSON of `World.Save` in and … |
| `Registry.cs` | Every module of one world, wired: each takes what it depends on through its constructor, and `All` lists them in … |

Every module of one world, wired through their constructors and listed in run order (`MODULES` in the
TypeScript sim). A new module adds a property, its construction after its dependencies, and its place
in `All`. Tests, the network and the game build worlds from a `new Registry()`: a world gets one of
its own, as modules hold its state.

## Surfaces

A world is made of surfaces ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md)), each a
registry of its own. Surface 0, the ship, is the registry a world is made or loaded with; its `All` is an `IModules`
whose `Another()` makes the registry of a new surface, sharing the ship's globals (`Players`, `Knowledge`), which only
the ship lists, first. `Registry.On(world, surface)` is the registry a surface runs. The core routes each command to
the registry of its sender's surface (`World.SurfaceOf`), refusing one stamped on a surface its sender has left
`world.elsewhere`; it runs every surface each tick in id order. `world.open` opens a surface of a kind in
`Content.Surfaces` and `world.transfer` moves the sender there, carrying what `ICarries` modules hold of it (crew's
hero, items' inventory and run). A save's `slices` are the ship's; `surfaces` and `where` are written only when
another surface exists.

## The wreck

`Wreck` is the world every new game starts in: the wreck of `Content.Wreck` centred in a world of
`Content.World`'s size, its build zone in the middle (`Layout.InWorld`,
[0048](../../docs/decisions/0048-a-big-world-a-build-zone-inside-it.md)). `WreckMap` is the wreck as
drawn, 192×96 a character a 16 px cell, doubled to 384×192 cells (`Layout.Doubled`, 0104), with no zone: dev runs and the
tests whose cells are the drawing's play it.

## Migrations

`Migrations.cs` turns a save of any format since `Migrations.Oldest` into this build's
([0031](../../docs/decisions/0031-worlds-the-title-and-migrations.md)), a step a format, each with a
fixture in `Tests/Fixtures`. Format 28 halved the cell (0104): a save from before holds cells and lengths of another scale,
and a planet's edits laid on a ground that now grows differently, so none migrates. `Oldest` is 28, there are no steps yet,
and the title lists older saves grey with the reason.
