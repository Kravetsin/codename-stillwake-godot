# Fauna

Creatures living on a planet's surface ([0102](../../docs/decisions/0102-creatures-live-in-the-sim.md)): spawned out of
sight round every hero by the habitats of the surface's planet type, walking and idling, struck by any weapon, knocked,
flinching and fleeing, ramming a hero when cornered ([0103](../../docs/decisions/0103-a-ram-costs-the-suit-air.md)), dying
into their yields, and gone again when no hero is near. The ship has none.

- **Depends on:** `Grid` (`IGridQueries`: its size, what is solid, back walls), `Crew` (`ICrewQueries`: every hero's
  reported cell; `ICrewOps`: `Shock`, a ram's cost to the suit), `Items` (`IItemsQueries`: `Damage`, a tool's hit by `ToolStats` (0098), and `WeaponProblem`, an electric
  tool flat or not running; `IItemsOps`: `DropAt`, the floor a dead creature's yields fall on).
- **State** (`FaunaState`, members internal, saved and hashed): `Creatures` in spawn order, so in id order, each a
  `Creature` (public, internal setters) `{ Id, Species, X, Y, VX, VY, Damage, Mode, ModeLeft, SeenAt }`: its species by id
  in `Content.Fauna.All`, the bottom middle of its body at `X`, `Y` in thousandths of a cell (`FaunaModule.Unit`), its speed
  a tick, the hit points weapons took off it, what it is doing (`Mode`: `Idle`, `Wander`, `Flee`, `Hurt`, `WindUp`, `Charge`,
  `Retreat`, `Escape`, saved by number)
  and the ticks left of it, and the clock it was last in a hero's keep box; `NextId`, never reused; `Clock`, ticks run on a
  surface with life or creatures. **Derived** from the rest, never saved: its facing (the sign of `VX`; the client keeps
  the last while it stands), whom it flees (the nearest hero, found as a flinch ends), the beat (`Clock % Fauna.Beat`).
- **Files:** `Fauna.cs` — the module, `Strike`, `Release`, queries; `Footing.cs` — `FeetOf`; `Live.cs` — the system: keeping, modes, steering,
  moving; `Spawn.cs` — the ring and the habitats; `State.cs`; `Fuzz.cs`.
- **Runs** last in `Registry.All`, after `Crew` in the simulation phase, so where heroes are is this tick's; saved last.

## Rules

- **Nothing aboard.** A surface whose kind is no `PlanetType` with habitats, and with no creature let out, runs nothing and
  draws nothing: the ship's slice never changes, its clock never starts. `Release` aboard (no gravity) is refused
  `fauna.notHere`. A drawn planet map (the test strip) has no habitats but creatures let out live there.
- **A tick,** creatures in id order: kept or gone (below); pushed up a cell, still, while a wall built since stands in its
  body (its head kept on the grid); else its mode's clock counts down, a mode ending picks the next (below); a walker on the
  ground wandering or in flight (`Flee`, `Retreat`, `Escape`) turns back from a wall it cannot step up and from a drop of `Fauna.Drop` (6) rows or more, in
  the column just ahead or, over flat ground, any its edge reaches at its mode's speed this tick (a loper's flight, 30 cells
  a second, would else overhang the brink by more than a cell) — unless it leaps (below) — then goes that speed; idle on the ground it stands still; then it moves. Then, on the beat, spawns.
- **Moving** is integer and per axis, under the surface's gravity (`Kind.Fall(Fauna.Gravity)`, 42 a tick on the first
  planet, at most `Fauna.MaxFall`). Solid is a wall (a slope stood on at its top, whole), a floor or a closed door, and past
  the grid's edge, as for a stack; machines are not. Across: into what is solid it steps up as many cells as it fits, up to `Fauna.StepUp` (2, the 16 px cell
  it stepped before, 0104), if it stood on the ground, else stops flush, keeping its way for its steering to leap or turn; walking off a step
  as deep it steps down onto it, never falling, so a slope's run of steps is walked. Down: it lands flush on what is under it; up, it
  stops flush under what is over it. It moves in parts of under a cell each way (its flight is 1500 a tick, its fall up to 1200), so
  no wall or floor a cell thick is passed.
- **Jumps** (`Jump.cs`), for a species with a `Jump`: where a walker would turn, it first tries a leap, flown through the grid
  tick by tick as the move would fly it (`Lands`), up to 120 ticks: taken if it lands at least two cells on and no more than
  `Fauna.Drop` rows down, else it turns. Fleeing, it tries the lowest of three — sized to carry its feet over a ledge of
  `Fauna.WanderLedge` (4) cells, over one a step higher (6), and its whole `Jump` — over walls and across gaps; wandering, only the first and
  only up a wall at its feet, so it does not bound about. In the air it goes `min(speed, JumpAcross)` across and presses flush on
  what it meets, carried over once its feet clear it. `Leaps.Height`, `Length` and `SpeedFor` give a jump's shape under a gravity:
  the loper's 880 up and 800 across on the first planet (42 a tick) rise some 8.8 cells and land level some 34 on. A leap is flown
  in the move's parts of under a cell, so nothing passes through a wall.
- **Modes:** `Idle` (a draw of `IdleLeast`..`IdleMost` ticks) → `Wander` (a draw of its way and of `WanderLeast`..`WanderMost`
  ticks) → `Idle`; a strike → `Flee` for `FleeTicks` at once, the knock's way, for a species with no hit-stun (`Stun` 0, the
  neutral loper); else → `Hurt` for its `Stun` ticks, steering nothing → `Flee`, its way away from the nearest hero by reported
  cell (box distance in cells, the first in id order at a tie; level, or none, the knock's way) → `Idle`. Only an idle or a
  wander begun draws from the Rng, and a ram's draws (below).
- **Rams** (0103), one a hit. Struck by a hero within `Fauna.RamBackReach` (24) cells of it by box distance (the striker's
  reported cell), a species with a `Ram` and no stun draws its `RamChance` in a thousand (the tapir's 900, the loper's and the
  sixleg's 700; the fluff has none); drawn, it flees as ever but in `Retreat`, for a second draw of `RamFleeLeast`..`RamFleeMost`
  (12..20) ticks, then turns back → `WindUp`; a retreat that must turn at a wall or drop it cannot leap winds up there at once.
  Not drawn (or struck from farther), it is in `Flee`, and a flight that must turn so, with the nearest hero within
  `Fauna.RamReach` (12) on the side it would turn to, draws its `RamChance` again: cornered, → `WindUp`. A wind-up is
  `Fauna.WindUp` (8) ticks standing still, counted from the ground → `Charge` toward the nearest hero by reported cell as it
  ends (none, level with it or farther than a charge runs: → `Escape` instead), at `Fauna.Charge` (1400) a tick along the ground,
  stepping up and down a step as a walker, never leaping, till its middle is `Fauna.RamPast` (6) cells past that hero's
  (→ `Escape` on its way), a wall it cannot step up or a drop (→ `Escape`, turned from it), or `Fauna.ChargeTicks` (40) (→
  `Escape` from the nearest hero). `Escape` is a flight of `FleeTicks` that never rams → `Idle`: its ram spent, it rams only
  when struck again. A strike in a wind-up or a charge knocks it and draws anew, as any strike.
- **`HitBy(Creature)`,** from the hero its charge touched, as that hero's client saw it (0034's word): refused `fauna.gone`
  and `fauna.notAttacking` for one not charging. The sender's suit pays the species' `Ram` (`ICrewOps.Shock`: none in creative
  mode or for a hero out, the last unit passing it out as the cold would); the creature recoils into `Escape`, back the way it
  came at its flight's speed, hopping `Hop` unless it rises already. So a charge hits once: a second hero's `HitBy` on it is
  refused. Read from the creature alone: a command never sees where heroes are.
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
- **`FeetOf(creature)`,** a query for the client's drawing: the ground's surface under its back and front feet
  (`Fauna.FootSpan`, 22 % of its width either side of its middle), a slope's diagonal or a cell's top, read in its feet's row and
  the four under, so a step of its body moves neither foot; a foot with none there hangs `Fauna.Teeter` (1.2 cells) under its body. Up and down a run of slopes each foot moves no faster than the body walks across
  (`FaunaFootingTests`), where the body's own `Y` steps a whole cell in a tick.- **Kept or gone:** by the cell under its middle; in some hero's keep box (`Fauna.Keep`, 160 × 96 either way) it is seen; out
  of every one it is gone `Fauna.Linger` (600) ticks after it was last seen, and at once out of every hard box (220 × 128). No
  hero on the surface, none stays: the last to leave takes them all, damage and all.
- **Species** (`Content.Fauna.All`): the loper, the tapir, the sixleg and the fluff, one life with numbers by body (0102's
  slice 3 has the table); each names its `Look`, the design the client draws. The first planet's habitats: the loper, tapir and
  sixleg on the open surface (weights 3, 2, 2), the fluff on cave floors of band 0 (2) and band 1, the garden (3).
- **Spawning,** when the clock is a multiple of `Fauna.Beat` (40), on a type with habitats: for each hero in id order with
  fewer than `Fauna.PerKeep` (10) in its keep box, while the surface holds fewer than `Fauna.Max` (40): three draws, always
  taken — the side, the columns out (`RingFrom`..`RingTo`, 124 to 152, past the view's 120), the habitat by weight — then down
  that column from the top of the hero's keep box, the first row where the species' body stands in every cell open, on
  something solid, and of the habitat's place: no back wall under the open sky (`Place.Surface`), a back wall in its depth
  band (`Place.Band`, `Terrain.BandOf`) in a cave. None off the grid, without a place, or with a cell in any hero's view
  (`Fauna.View`, the debris' 60 × 34). It starts idle `IdleLeast` ticks, seen now.

## Guarantees

- Every creature is on the grid and, but the ticks after a wall is built into it, out of everything solid; its damage is
  under its hit points; ids only grow; a surface never holds more than `Fauna.Max`; what is left of its mode is never more
  than the mode lasts; a charge hurts at most once, and every suit stays from nothing to full (`FaunaInvariantTests`).
- A surface with no habitats and no creatures leaves the slice and the Rng untouched.

## Deliberately not handled

- **Creatures do not collide** with each other or with heroes, as debris does not (0006).
- **A slope is stood on at its top:** a walker on one floats over its diagonal, its body stepping a whole cell a tick; the client
  draws its feet on the diagonal by `FeetOf`.
- **Senses through rock:** fleeing is from the nearest hero by box distance, walls between or not.
- **A creature walled in to the grid's top** stays in the wall: it rises only while there is a row over its head.
- **Despawning loses damage;** a half-hunted loper left for 30 s is gone with it.
- **Unlimited biomass:** lopers spawn while heroes stand by; the cap and the beat are the only limits.
- **A ring point with no place wastes the beat** for that hero: on a cliff column, or in a cave under the surface.
- **A ram aims where the hero was reported** as the wind-up ended, up to a second behind; it runs RamPast past that cell.
- **A second hero a charge touched** is knocked on its own screen but pays nothing.
- **The drill held on a rammer** draws anew at every one of its quick hits: the retreat begins again each time, so it turns
  back only once the drilling stops.
- **A stunned species never rams back** (none has a stun yet): its hit-stun steers its flight.
