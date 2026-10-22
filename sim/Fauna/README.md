# Fauna

Creatures living on a planet's surface ([0102](../../docs/decisions/0102-creatures-live-in-the-sim.md)): spawned out of
sight round every hero by the habitats of the surface's planet type, walking and idling, struck by any weapon, knocked,
flinching and fleeing, dying into their yields, and gone again when no hero is near. The ship has none.

- **Depends on:** `Grid` (`IGridQueries`: its size, what is solid, back walls), `Crew` (`ICrewQueries`: every hero's
  reported cell), `Items` (`IItemsQueries`: `Damage`, a tool's hit by `ToolStats` (0098), and `WeaponProblem`, an electric
  tool flat or not running; `IItemsOps`: `DropAt`, the floor a dead creature's yields fall on).
- **State** (`FaunaState`, members internal, saved and hashed): `Creatures` in spawn order, so in id order, each a
  `Creature` (public, internal setters) `{ Id, Species, X, Y, VX, VY, Damage, Mode, ModeLeft, SeenAt }`: its species by id
  in `Content.Fauna.All`, the bottom middle of its body at `X`, `Y` in thousandths of a cell (`FaunaModule.Unit`), its speed
  a tick, the hit points weapons took off it, what it is doing (`Mode`: `Idle`, `Wander`, `Flee`, `Hurt`, saved by number)
  and the ticks left of it, and the clock it was last in a hero's keep box; `NextId`, never reused; `Clock`, ticks run on a
  surface with life or creatures. **Derived** from the rest, never saved: its facing (the sign of `VX`; the client keeps
  the last while it stands), whom it flees (the nearest hero, found as a flinch ends), the beat (`Clock % Fauna.Beat`).
- **Files:** `Fauna.cs` — the module, `Strike`, `Release`, queries; `Live.cs` — the system: keeping, modes, steering,
  moving; `Spawn.cs` — the ring and the habitats; `State.cs`; `Fuzz.cs`.
- **Runs** last in `Registry.All`, after `Crew` in the simulation phase, so where heroes are is this tick's; saved last.

## Rules

- **Nothing aboard.** A surface whose kind is no `PlanetType` with habitats, and with no creature let out, runs nothing and
  draws nothing: the ship's slice never changes, its clock never starts. `Release` aboard (no gravity) is refused
  `fauna.notHere`. A drawn planet map (the test strip) has no habitats but creatures let out live there.
- **A tick,** creatures in id order: kept or gone (below); pushed up a cell, still, while a wall built since stands in its
  body (its head kept on the grid); else its mode's clock counts down, a mode ending picks the next (below); a walker on the
  ground in `Wander` or `Flee` turns back from a wall it cannot step up and from a drop of `Fauna.Drop` (3) rows or more, in
  the column just ahead or, over flat ground, the one its edge reaches at its mode's speed this tick (a loper's flight, 15 cells
  a second, would else overhang the brink by most of a cell), then goes that speed; idle on the ground it stands still; then it moves. Then, on the beat, spawns.
- **Moving** is integer and per axis, under the surface's gravity (`Kind.Fall(Fauna.Gravity)`, 21 a tick on the first
  planet, at most `Fauna.MaxFall`). Solid is a wall (a slope stood on at its top, whole), a floor or a closed door, and past
  the grid's edge, as for a stack; machines are not. Across: into what is solid it steps up one cell if it stood on the
  ground and the cell over is free, else stops flush, turned back while walking; walking off a step one cell deep it steps down
  onto it, never falling, so a slope's run of steps is walked. Down: it lands flush on what is under it; up, it stops flush
  under what is over it. A tick moves less than a cell, so the next column or row is the only one entered.
- **Modes:** `Idle` (a draw of `IdleLeast`..`IdleMost` ticks) → `Wander` (a draw of its way and of `WanderLeast`..`WanderMost`
  ticks) → `Idle`; a strike → `Flee` for `FleeTicks` at once, the knock's way, for a species with no hit-stun (`Stun` 0, the
  neutral loper); else → `Hurt` for its `Stun` ticks, steering nothing → `Flee`, its way away from the nearest hero by reported
  cell (box distance in cells, the first in id order at a tie; level, or none, the knock's way) → `Idle`. Only an idle or a
  wander begun draws from the Rng.
- **`Strike(Creature, Tool)`:** refused `fauna.gone` for a creature gone or never spawned, `items.flat` and
  `items.notRunning` for an electric tool as `IItemsQueries.WeaponProblem` says (0055). A tool that deals no damage is our
  bug and throws. It takes `Items.Damage(sender, tool)` off the creature; at its hit points it dies, its `Yields` drawn as a
  container's loot (`Loots.Roll`) and dropped by nobody on the cell of its top row under its middle, falling to where it stood.
  Else it is knocked, stunned or fleeing as its species says (above): its speed across the species' `Knock` away from the
  striker's reported cell (level, or none on the grid: back against its way), but never slower than it already runs that way,
  `Hop` up unless it rises already; a tool striking faster than `Fauna.KnockPace` (10 ticks, the axe's) knocks and hops its
  pace's share, rounded down, so the drill's ten hits a second push it about as far as the axe's two, and cannot hold a
  fleeing one under the bit. Reach, pace and whether the sender holds the tool are the client's word (0034).
- **`Release(Species, Cell)`,** a dev command (`--fauna`): a creature idling with its body's bottom left in the cell.
  Refused `fauna.notHere` aboard, `fauna.full` at `Fauna.Max`, `fauna.blocked` off the grid or where its body would be in
  anything solid. An unknown species is our bug.
- **Kept or gone:** by the cell under its middle; in some hero's keep box (`Fauna.Keep`, 80 × 48 either way) it is seen; out
  of every one it is gone `Fauna.Linger` (600) ticks after it was last seen, and at once out of every hard box (110 × 64). No
  hero on the surface, none stays: the last to leave takes them all, damage and all.
- **Spawning,** when the clock is a multiple of `Fauna.Beat` (40), on a type with habitats: for each hero in id order with
  fewer than `Fauna.PerKeep` (10) in its keep box, while the surface holds fewer than `Fauna.Max` (40): three draws, always
  taken — the side, the columns out (`RingFrom`..`RingTo`, 62 to 76, past the view's 60), the habitat by weight — then down
  that column from the top of the hero's keep box, the first row where the species' body stands in every cell open, on
  something solid, and of the habitat's place: no back wall under the open sky (`Place.Surface`), a back wall in its depth
  band (`Place.Band`, `Terrain.BandOf`) in a cave. None off the grid, without a place, or with a cell in any hero's view
  (`Fauna.View`, the debris' 60 × 34). It starts idle `IdleLeast` ticks, seen now.

## Guarantees

- Every creature is on the grid and, but the ticks after a wall is built into it, out of everything solid; its damage is
  under its hit points; ids only grow; a surface never holds more than `Fauna.Max` (`FaunaInvariantTests`).
- A surface with no habitats and no creatures leaves the slice and the Rng untouched.

## Deliberately not handled

- **Creatures do not collide** with each other or with heroes, as debris does not (0006).
- **A slope is stood on at its top:** a walker on one floats over its diagonal; the client draws it down to the diagonal.
- **Senses through rock:** fleeing is from the nearest hero by box distance, walls between or not.
- **A creature walled in to the grid's top** stays in the wall: it rises only while there is a row over its head.
- **Despawning loses damage;** a half-hunted loper left for 30 s is gone with it.
- **Unlimited biomass:** lopers spawn while heroes stand by; the cap and the beat are the only limits.
- **A ring point with no place wastes the beat** for that hero: on a cliff column, or in a cave under the surface.
