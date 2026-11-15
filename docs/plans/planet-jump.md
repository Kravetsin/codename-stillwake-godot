# Plan: the ship's jump between planets

The engine fires: the planet below closes for good and the next descent opens a new one. One planet is ever open in a
world. Planet variety is not in this plan; where it goes is under "Heading".

Words: a **surface slot** is a surface's place in the world's list (its id). A **closed** one keeps its id, kind and
seed and nothing else, no modules and no state, as a vessel world's once everyone left it (vessel-worlds). A **fuel
slot** is a machine's item slot: the pod's, the engine's.

## Rules (the user's, 2026-11-15)

- The drop pod flies down and up as often as wanted, as today. Only the engine's jump leaves a planet.
- A left planet is never returned to. Nothing of it is saved but its closed surface slot.
- The jump is refused while any hero is not aboard: on a planet, on a vessel world, or in a pod in the air. The refused
  attempt is the call to everyone, wordless: a sound and a light on the screen's edge, on every peer wherever its hero
  is. Its look and sound are picked later on a page; this plan plays stand-ins.
- A pod needs no new rule: it goes where its hero goes (`ICarries`), so everyone aboard means every pod aboard.
- The first planet is today's type; every later one the same type, its seed drawn from the world's Rng as the descent
  opens it, as today.
- Blind and instant: no destination, no travel time. It needs power and `Content.Landing.JumpFuel` (25) biofuel items
  in the engine's fuel slot.

## Player sequences

1. Solo: up in the pod, biofuel into the engine, **Fire** on its key. The planet closes; Descend opens a new one.
2. One below: the press fires nothing and calls everyone. The one below flies up; the press fires.
3. One in a pod in the air, down or up: as 2.
4. A death below: the hero wakes aboard, its pod docked. The jump fires; the body and its loot, the planet's Items'
   own, go with the planet.
5. Fired twice with no descent between: the second is refused, `landing.nothingBelow`.
6. Save and load after a jump, and a guest joining after one: the closed slot is in the save; the next descent grows
   the same planet on every peer.

## Sim

**Core (`ISurfaces`, `Sim.cs`)**

- `bool Away`: some peer stands off the ship (`_where` not empty). A peer who left the game was sent aboard by its
  farewell; one not yet entered counts aboard.
- `bool PlanetOpen`: an open surface of a planet's kind exists.
- `void LeavePlanet()`: closes every open planet surface in id order, its modules dropped, its surface slot kept
  closed. More than one is open only by the dev panel. Only while a command applies (as `Visit`); throws with a peer on
  one or a send queued to one: our bug, `crewAway` comes first.
- `OpenPlanet()` skips closed slots. Today it returns the lowest planet slot, closed or not, and `Send` would throw.
- `Landed` (a planet was ever opened: Hazards' stronger meteors on the hull) stays as it is: it counts closed slots,
  so they hold after a jump.

**Grid**: `IGridQueries.Exhausts(machineId)`, the placement rule (`ExhaustsThroughHull`) asked of a standing machine.

**Landing**: `FireEngine(int Engine)`, the engine's machine id, and `FireProblem(engine)`, in this order:

| Reason | When |
| --- | --- |
| `landing.noEngine` | no such machine here, or one whose spec has no `Exhaust` |
| `landing.engineUnpowered` | its power short: `Satisfaction` under 1000, thousandths of its draw |
| `landing.noFuel` | under `JumpFuel` biofuel in its fuel slot, slot 0 (the pod's text; the client gives it the number) |
| `landing.noExhaust` | `Exhausts` false: its nozzle walled in since it was built |
| `landing.nothingBelow` | no planet open |
| `landing.crewAway` | `Away`, or this Landing has a flight (a descent; a return's rider is `Away`). Last, so the call means "ready but for you" |

Applied: `JumpFuel` taken from slot 0, `Surfaces.LeavePlanet()`. No new saved state, no format bump.

Order in a tick: commands apply in stamp order, before any system. A `Launch` applied before the jump makes a flight,
so the jump is refused `crewAway`; a `Launch` applied after it descends to the new planet.

## Client

- `EngineKey` under the engine's fuel slot, as `LaunchKey` under a pod's: lit when it can fire, pressable too at
  `crewAway`, dim with the refusal in its tooltip otherwise.
- The call: every peer runs every command, so every peer's tick report holds the refusal; `Main` already walks them.
  A `landing.crewAway` one plays the call on every peer: a sound and the screen's edge flashing, no text. One press,
  one call.
- The burn: played when a planet's surface slot turns closed between two ticks of one world, never on a load, a join
  or a resync.
- Nothing of the closed planet is drawn or kept: the views are of the surface the own hero stands on, the ship then.
- Stand-ins for the call and the burn until the user's picks.

## Tests

- Scenarios (`JumpTests`): the fire closes the planet and takes the fuel, and the next descent opens a new slot of the
  first type's kind; each refusal, the world untouched (a hero below, a descent in the air, a return in the air, a hero
  on a vessel world, passed out below); a death below, then the jump, a save and a load; the second fire refused; the
  landed shower after a jump; a jump, a save and load, then a descent, two worlds alike: what proves the close, for the
  fuzz keeps peers off the ship and its fires are nearly all refused.
- Invariant (FsCheck), the README's "an owned pod stands by its owner" carried on: nobody away and no flight means no
  pod stands off the ship. Proven to bite once by leaving the pod behind when its owner's hero leaves.
- Landing's `Fuzz` draws `FireEngine` now and then, for its refusals. The tests' own `ISurfaces` get the new members.

## Heading (not built)

The planet a descent opens is `Content.Landing.Planet`, one constant. Its named users to come, each changing what a
planet is, none changing the jump:

1. A planet's technology: the ruins' set, Arc on the first, a random one after. A planet is the first when no planet
   slot, open or closed, is before it.
2. Numbers drawn from the seed: gravity, cold, ores, relief, palette. Rules are found by kind id alone today
   (`Surfaces.Get`) in `Attach`, `Open` and `Load`; these become one description from kind, seed and count, derived,
   never saved.
3. New planet types.

## Deliberately not handled

- Choosing a destination, a travel time, returning.
- A vote: one press fires once everyone is aboard.
- A warning about loot or a body left below.
- A player idle below: co-op among friends, the host removes them.
- Who is away, by name: the call says someone is. Added if four players miss it.
- A limit on calls: mashing the key is loud for everyone, among friends.
