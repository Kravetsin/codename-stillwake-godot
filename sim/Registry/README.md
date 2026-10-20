# Registry

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

Format 15 lets a floor stack carry a body tag (`body`: peer and tick died, 0061), written only when it has one: the
step changes nothing but the format, and the stacks of older saves are no one's body.

Format 16 gave the Former and the circuit assembler their input slots (0057): what each held gains its inputs,
empty, in front, so what it made stays in its outputs; a machine already of its new size is left as it is.
Entries saved before have no feed and load as paid.

Format 17 brought drones (0062): an empty `logistics` slice (no machine switched off, no drone flying), last, its
clock the save's tick; and the circuit
assembler's fourth input, empty, after its three, so its outputs keep their stacks (an assembler of seven slots, as
format 16's step now pads one from before, is left as it is).

Format 18 brought the Handbook (0066): debris's `found`, empty, for nothing in an older save says which kind of
debris an item came out of; one already there is left as it is.

Format 20 brought the world's globals (0076): `players` (each hero's suit colour, out of crew's heroes, in peer order)
and `knowledge` (research's `granted` and `analysed`), the first two slices; a save already holding `players` is
left as it is.

Format 21 brought digging (0077): building's `cracks`, empty, the ship's and every surface's, and a grown surface's grid saved as
its seed's edits (`GrownGrid`; no older save holds a grown surface); a `cracks` already there is left
as it is.
