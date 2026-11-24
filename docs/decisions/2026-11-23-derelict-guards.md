# Derelict guards

- Status: accepted
- Date: 2026-11-23

## Context

An intact vessel put up no fight: a hero walked room to room and emptied it, so a liner was only a longer freighter. The user
asked for guards aboard: a spider asleep in a capsule on a cabin's back wall and a guard robot hung in a stand in a big hall,
both waking when a hero walks into their room and then hunting the crew through the vessel. The mechanic is guards placed from a
vessel's description and woken by its rooms ([0049](0049-design-the-mechanic-not-its-first-use.md)): intact vessels now; wrecked
vessels and enemy vessels next, so placement lives in Core's Shipyard, where they can reuse it.

## Decision

**No new module.** Creatures owns the guards (bodies, brains, hits, spawn); Core's `Shipyard.Guards` places them. No commands, no
events, no path search: guards route by the vessel's simple shape (decks, doorways, the hatch well).

**Placement** (`Shipyard.Guards(Built)`, rules in `Content.Vessels.Aboard`), pure, the same on every peer:

- None on a wreck (`Wreck != null`). An intact vessel's are drawn from Shipyard's own stream **16**, never the world's Rng, so the
  vessel itself stays as it was.
- A room is a `Vessel.Compartment`: small is one deck tall, big more. Spiders: `ceil(small / 5)`; robots: `ceil(big / 3)`, rooms
  counted with the arrival room, then `min(count, candidates)`. Candidates are the rooms of that size, never the arrival room,
  where the dock's box (capsule 5×5 cells, stand 7×8) stands on the floor clear of the way (ladders, the cells over a hatch).
  Rooms drawn by a partial Fisher–Yates, then one column each; listed spiders first, then robots, each by room index.
- A stretch clear of furniture is preferred. With none, a **capsule replaces** the furniture under it (`Shipyard.Docked`: what its
  box covers is not laid, a crate with its loot) and a **stand stands over** it, drawn in front (the user's word for both). No
  fitting is solid, so a robot never stands inside one.
- Places lays `Shipyard.Docked(built.Parts, Shipyard.Guards(built))`; `Shipyard.WayIn` names the way-in door.

**Creatures:**

- **Spawn:** in `Opened()`, before any other creature on the surface, one guard asleep per dock in `Guards` order, so ids follow it.
  It re-derives the vessel from the surface's class, seed and wreck, never the grid (Places writes the vessel after Creatures runs).
  A guard keeps only its dock's index in `Mem`; docks, rooms, hull box, floors, hatches, doors and way in are derived. Guards count
  toward `Creatures.Max`; the largest class's count is pinned under it by a test. A sleeping guard checks up to four heroes against
  one room; an awake one probes a few cells a tick.
- **`Lifetime.Aboard`:** never swept by keep boxes (a vessel is wider than one), never let out by `Release`; gone only killed or
  with its world.
- **`Dock`** (the warden's wake, not `Ward`): an awake hero's reported cell (his body's middle) inside the dock's room, not a
  doorway, or a blow from anywhere, wakes it: `Wake` ticks waking, then awake for good, never asleep again. A blow hurts as usual,
  so one can be killed asleep; its dock stays open and empty.
- **Quarry:** the nearest awake hero aboard (in the hull box) by squared cell distance, the first in the crew's order at a tie. With
  no hero aboard a guard stands where it is.
- **The spider** (`Prowl`): the sentry's species on a walker body that climbs. On its quarry's deck it runs at him, leaping what its
  jump clears and climbing a solid wall inside the hull, never a back wall or ceiling; on another deck it takes the hatch well,
  opening the hatch. It stands on an open hatch unless its quarry is below. It fights as the sentry (pounce, arc), never from a
  wall; its pounce flies at the hero's middle and stops at the hull box, never out an open way in.
- **The robot** (`Escort`, `Patrol`): a 4×5 walker that never jumps, bound to its deck's floor rows. Awake it always closes in
  to claw (`Lunge`) within `Strike` columns on its deck; on its way it fires the **gauss** at a hero in line within `Notice`, on
  any deck, up too, without stopping (`Afoot`), a shot begun at least 3 s after the last (`Shoot.Every`), never during a lunge,
  which drops a shot it was charging. With its quarry off its deck it walks to the deck's nearest door (the lower column at a tie)
  and on, turning at a wall. This is the user's change after play: it first stopped to shoot from afar.
- **The gauss:** a creature shot through `Shots.Fire`, from the gun's height, crossing its reach in a tick; stopped by the first
  solid cell or awake hero, six suit units, no burst.
- **Doors:** a shut doorway on its way, a guard pries `Pry` ticks, then opens it through `IGridOps.SetDoorway` (Creatures now takes
  Grid's ops); the doorway changed meanwhile, it starts over. The spider opens doors and hatches, the robot doors only; neither ever
  the way in. A door shut on a guard (0006) it pries where it stands.
- **Loot and toughness:** the spider has the sentry's life and yields; the robot 60 hit points, never knocked, more circuits and
  scrap. Every distance and time is content, tuned in play.

**Client** (`f/creatures`): `DocksView` draws the docks from `CreaturesModule.Docks`, under the unseen rooms' fog; a capsule's
hazard lamp blinks, flashes and lights its room as its iris opens step by step and the spider unfolds and drops; a stand's clamp,
winch and yoke let its robot down. `EscortBodies`/`EscortRig` draw the robot, its red lamp and its gauss trace. A guard's pounce or
claw on a hero is heard as the sentry's punch; every waking moment, step, pry, shot and death is heard on World.

**Picks:**

- Spider A «Зонд» (the sentry's own look) and capsule G1 «Зрачок — впадина», after rounds of compact and wall capsules:
  https://claude.ai/artifact/KjV2ZuEDvxSknKE8ncZEye.
- Robot A1 «Конвоир» and stand S1 «Люлька»: https://claude.ai/artifact/7b65Cj3htH8ruyeboVHPha. Drawn in ink by
  `tools/anime-art` subjects `guards` and `escort`.
- Sounds, 17 picks, cut and credited in `game/audio/CREDITS.md`: the capsule's alarm and iris; the stand's clamp, winch and yoke; the
  robot powering up; the spider's step and pry; the robot's step, servo, pry, gauss charge, shot and ricochet, dash, hit and death:
  https://claude.ai/artifact/GreKceXJNtoqAgJccAdqS9.

**Saves:** guards live only in vessel worlds, which a load closes. New `Mem` keys, the appended `Lifetime` and `Posture` values
need no format bump; `CreatureContractTests` pins the new species.

## Deliberately not handled

- Stealth: a guard knows where every awake hero aboard is; hiding works only against the robot, off its deck or out of its line.
- Ceilings, back walls and the hull's outside for the spider; ladders and hatches for the robot.
- Re-docking: an awake guard never sleeps again. Guards waking each other, alarms, guards shutting doors.
- Following a hero into space: no guard opens the way in, and a pounce stops at the hull.
- Guards on wrecked vessels, stations, enemy vessels or the player's ship: later, from the same `Shipyard.Guards`.

## Accepted limitations

- A guard's saved dock index points into a list rebuilt from content on load. Safe because a load closes vessel worlds; were
  they ever saved open, a content change could move a guard's dock.
- A guard opens a door in the Simulation phase, after Derivation: Rooms and Atmosphere see it a tick later.
- A hero standing in a doorway's columns is in neither room, so a guard wakes only once he is past it (the user's rule).
- No path is searched: at a wall it can neither leap nor climb (the way in, the hull's end) the spider stands till its quarry moves.
