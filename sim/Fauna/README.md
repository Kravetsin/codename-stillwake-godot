# Fauna

## Files

| File | Purpose |
| --- | --- |
| `Fauna.cs` | Creatures living on a planet's surface (0102): spawned out of sight round every hero by the habitats of its type … |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Jump.cs` | Jumping (0102): a walker that must turn first tries a leap over the wall or across the gap, its landing found by … |
| `Live.cs` | The fauna's system (0102): each creature, in id order, kept or gone by where the heroes are, then its mode's clock … |
| `Spawn.cs` | Spawning (0102): round each hero, on the beat, out of every hero's view, where a habitat of the surface's type is. |
| `State.cs` | The saved slice and its records: `Mode`, `Creature`, `FaunaState`. |

Creatures living on a planet's surface ([0102](../../docs/decisions/0102-creatures-live-in-the-sim.md)): spawned out of
sight round every hero by the habitats of the planet type, walking, idling, struck by any weapon, knocked, fleeing,
ramming a hero when cornered ([0103](../../docs/decisions/0103-a-ram-costs-the-suit-air.md)), dying into their yields,
and gone again when no hero is near. The ship has none.

- **Depends on:** `Grid` (size, solid, back walls); `Crew` (heroes' reported cells; `Shock`, a ram's cost to the suit);
  `Items` (`Damage` by `ToolStats` (0098), `WeaponProblem`; `DropAt` for a dead creature's yields).
- **State** (`FaunaState`, saved and hashed): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Mode,
  ModeLeft, SeenAt }`, position the bottom middle of its body in thousandths of a cell (`FaunaModule.Unit`), `Mode` saved
  by number; `NextId`, never reused; `Clock`, ticks run on a surface with life or creatures. **Derived:** facing (sign of
  `VX`), whom it flees (nearest hero as a flinch ends), the beat (`Clock % Fauna.Beat`).
- **Runs** last in `Registry.All`, after `Crew` in the simulation phase, so heroes' cells are this tick's; saved last.

## Rules

- **Nothing aboard** (0102): a surface whose kind has no habitats and no creature let out runs nothing and draws nothing.
  A drawn planet map has no habitats, but creatures let out live there.
- **A tick,** in id order: kept or gone; pushed up a cell, still, while something built since stands in its body (no
  ADR); else its mode's clock counts down; a walker wandering or in flight turns back from a wall it cannot step up and
  from a drop of `Fauna.Drop` rows or more ahead, unless it leaps; idle on the ground it stands; then it moves. Then, on
  the beat, spawns.
- **Moving** (0102, 0106): integer, per axis, under the surface's gravity (`Kind.Fall`, capped `Fauna.MaxFall`). Solid is
  a wall, floor, closed door and past the grid's edge; machines are not. On the ground it steps up to `Fauna.StepUp`
  (0104) and down as deep without falling, so a stair is walked; in the air it stops flush. It moves in parts under a
  cell, so no wall a cell thick is passed.
- **Jumps** (`Jump.cs`, 0102), for a species with a `Jump`: where a walker would turn it first flies a leap through the
  grid tick by tick (`Lands`), taken if it lands at least two cells on and no more than `Fauna.Drop` rows down. Fleeing,
  the lowest of three that lands (`Fauna.WanderLedge`, a step higher, its whole `Jump`); wandering, only the first, up a
  wall at its feet. In the air it goes `min(speed, JumpAcross)`.
- **Modes** (0102): `Idle` (drawn ticks) → `Wander` (drawn way and ticks) → `Idle`. A strike → `Flee` at once for a
  species with no `Stun`; else `Hurt` for `Stun` ticks → `Flee` from the nearest hero by box distance (first in id order
  at a tie; level or none, the knock's way) → `Idle`. Only an idle or wander begun, and a ram, draw from the Rng.
- **Rams** (0103), one a hit. Struck from within `Fauna.RamBackReach`, a species with a `Ram` and no stun draws
  `RamChance`: drawn, it flees in `Retreat` for a drawn `RamFleeLeast..RamFleeMost`, then → `WindUp` (at once if it must
  turn where it cannot leap). Not drawn, a `Flee` that must turn with the nearest hero within `Fauna.RamReach` on that
  side draws again: cornered, → `WindUp`. `WindUp` stands `Fauna.WindUp` ticks → `Charge` at the nearest hero along the
  ground at `Fauna.Charge`, never leaping, till `Fauna.RamPast` past it, a wall or drop, or `Fauna.ChargeTicks` → `Escape`
  (no hero to charge: `Escape`). `Escape` is a flight that never rams. A strike in a wind-up or
  charge knocks and draws anew.
- **`HitBy(Creature)`** (0103), from the hero a charge touched, as its client saw it (0034): the sender's suit pays the
  species' `Ram` (`Shock`: nothing in creative mode or for a hero out); the creature recoils into `Escape`, hopping. One
  hit per charge; read from the creature alone.
- **`Strike(Creature, Tool)`** (0102): takes `Items.Damage` off it; at its hit points it dies, its `Yields` rolled as
  loot (`Loots.Roll`) and dropped by nobody on the cell under its middle. Else knocked by its species' `Knock` away from
  the striker's reported cell (none: back against its way), never slower than it already runs, `Hop` up unless rising; a
  tool faster than `Fauna.KnockPace` knocks its pace's share, rounded down. Reach, pace and holding are the client's word.
- **`Release(Species, Cell)`** (0102), a dev command (`--fauna`): a creature idling with its body's bottom left in the cell.
- **Kept or gone** (0102), by the cell under its middle: in some hero's keep box (`Fauna.Keep`) it is seen; out of every
  one it is gone `Fauna.Linger` ticks after last seen, and at once out of every hard box. No hero on the surface: none stays.
- **Species** (`Content.Fauna.All`, 0102): each names its `Look`, the client's design; habitats are data per planet type.
- **Spawning** (0102), on the beat on a type with habitats: per hero in id order with fewer than `Fauna.PerKeep` in its
  keep box, while the surface holds fewer than `Fauna.Max`: three draws, always taken (side, columns out between
  `RingFrom` and `RingTo`, habitat by weight); then down that column from the keep box's top, the first row where the body
  stands open on something solid in the habitat's place (`Place.Surface`: no back wall; `Place.Band`: a back wall in its
  depth band). None off the grid, without a place, or with a cell in any hero's view (`Fauna.View`). Starts idle, seen.

## Commands

`Strike(Creature, Tool)`: `fauna.gone`, `items.flat`, `items.notRunning` (0055). `HitBy(Creature)`: `fauna.gone`,
`fauna.notAttacking`. `Release(Species, Cell)`: `fauna.notHere` aboard, `fauna.full` at `Fauna.Max`, `fauna.blocked` off
the grid or in anything solid. A tool dealing no damage or an unknown species throws: our bug.

## Guarantees

- Every creature is on the grid and, but the ticks after a wall is built into it, out of everything solid; damage below
  hit points; ids only grow; never more than `Fauna.Max`; a mode's ticks left never exceed its length; a charge hurts at
  most once; every suit stays between empty and full (`FaunaInvariantTests`).
- A surface with no habitats and no creatures leaves the slice and the Rng untouched.

## Deliberately not handled

- **Creatures do not collide** with each other or with heroes (0006).
- **A step is taken in a tick** (0106): the client eases the picture over it.
- **Senses through rock:** it flees the nearest hero, walls or not.
- **Walled in at the grid's top** it stays in the wall: it rises only while there is a row over its head.
- **Despawning loses damage.** **Unlimited biomass:** the cap and the beat are the only limits.
- **A ring point with no place wastes the beat** for that hero.
- **A ram aims where the hero was reported** as the wind-up ended (0103). A second hero a charge touched pays nothing.
- **The drill held on a rammer** draws anew at every hit, so it turns back only once the drilling stops.
- **A stunned species never rams back** (none has a stun yet).
