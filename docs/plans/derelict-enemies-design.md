# Design: Derelict guards (spider and robot aboard intact vessels)

Picks: spider A «Зонд», capsule G1 «Зрачок — впадина», robot A1 «Конвоир», stand S1 «Люлька»
(`derelict-enemies-spider-mockup.html`, `derelict-enemies-guard-mockup.html`).

## Problem
Today an intact vessel puts up no fight. A hero walks from room to room and empties it, so a liner is just a longer freighter.
Nothing aboard defends the salvage.

## Player experience
1. The crew jumps to an intact freighter. Through an open door, a hero sees an octagonal capsule on a cabin's back wall, its
   hazard lamp dim.
2. He steps past the door. The lamp flashes, the iris turns open, and the spider unfolds and drops to the floor.
3. It strikes him with an arc from across the room. He backs out and shuts the door. The spider opens it, follows him,
   climbs the shaft wall and opens the hatch.
4. In a liner's two-deck hall, a hero walks in under a cradle. The clamps release, the robot is lowered, its lamp turns red
   and it steps off. When he is far it stops and fires a gauss shot. When he is close it dashes and claws. When he climbs to
   the gallery it shoots up at him.
5. He hides behind a crate on the gallery. The robot walks off through a door to patrol its deck, so he has to come down to
   go on.

**Goes wrong:** a hero kills the spider. Its capsule stays open and empty for the rest of the visit; nothing comes back.

## Out of scope
- Guards on wrecked vessels (any `VesselCause` but `None`), on stations or on the player's ship.
- Crawling on ceilings or back walls. Robots on ladders or through hatches.
- Stealth: a guard always knows where every awake hero aboard is. The sentry already senses through rock.
- Guards waking each other, alarms, guards shutting doors.
- New sounds: the spider keeps the tick's, and the robot stays silent until its sound is picked.
- Any change to the sentry or the warden.

## Scale
- A shuttle gets 1 spider; a liner about 6–12 spiders and 1–2 robots. A test computes the maximum per class and pins it under
  `Creatures.Max` (40).
- Up to 4 players aboard one vessel.
- Each tick, every docked guard checks whether one of up to 4 heroes stands in its room. Awake guards search a path every
  `Paths.Replan` ticks, at most one search a tick per surface. A few door openings a minute.

## Approach
**Where it is heading.** The mechanic is guards placed from a vessel's description and woken by its rooms. Users now: intact
vessels. Next: wrecked vessels (the vessel-wrecks plan) and enemy vessels (`docs/plans/vessels.md`). Climbing a wall also
serves the "climbing aboard" raids in `docs/plans/creatures.md`.

**No new module.**
- **Creatures** owns the guards: bodies, brains, hits, spawning. A `guards` module would need Creatures' state anyway.
- **Core's `Shipyard.Guards(Vessel)`** places them: pure, drawing from a new stream of the seed. The sim and the client read it.

**Rooms.** A room is a `Vessel.Compartment`. Small means `Decks == 1`; big means `Decks > 1`. Spiders = ⌈small/5⌉, robots =
⌈big/3⌉, each in a distinct room drawn at random. A capsule sits on a room's back wall at a drawn column, its bottom just
above the floor; a stand at a floor column of a big room that no fitting covers. Creatures re-derives the vessel from its
class and the surface seed, as Places' `Opened` does; rooms, docks, the way-in door and each robot's deck are `[JsonIgnore]`,
rebuilt in `Rebuild()`. The client's `Sight` is not used.

**Spawn.** In Creatures' `Opened()`, one docked guard per dock, in `Guards` order, so creature ids follow it. Its dock cell
is kept in `Mem("dock")`. A new `Lifetime.Aboard`: never swept by keep boxes (a vessel is wider than one); gone only when
killed or when its world closes. Places never applies damage today, so every vessel world counts as intact for now.

**Content.**
- `Spider`: the `Sentry` with a 3×3 walker body that has `Climb`, `Lifetime.Aboard`, the brain `[Stalk]`, and the sentry's
  own pounce and arc.
- `Escort` (A1 «Конвоир»): a 4×5 walker that never jumps, brain `[Stalk]`, moves `Lunge` (dash and claw) and `Shoot` with a
  new `gauss` projectile, a straight instant line that stops at walls.
- The `Aboard` rules (species, small or big, rooms per guard) and dock sizes.

**The `Stalk` drive.** It takes the warden's `Ward` sleep and wake steps, not `Ward` itself (`Ward` never walks).
- **Docked** until an awake hero's reported cell is inside its room.
- **Waking** for `Wake` ticks.
- **Hunting:** its quarry is the nearest awake hero aboard (box distance, then the lowest peer id). In line within `Strike` →
  its first move; in line within `Notice` → its second move (aimed up too); otherwise it runs a path to the hero. With no
  path, the robot patrols its deck room to room and the spider steers straight at the hero.

**Bodies.** `Walker` gains `Climb` (up and down the face of a solid wall) and `Opens` (`Doors` for the robot,
`DoorsAndHatches` for the spider). At a shut doorway on its way, a guard waits `Pry` ticks, then opens it through
`IGridOps.SetDoorway`; Creatures now takes Grid's ops. A guard never opens the way-in door.

**Paths.** Generalised from the catcher's `Flyer` to any walker. New step kinds (climb, fall, through a shut doorway) are
appended, never renumbered.

**Interface.** No commands, no events. `Posture` gains `Docked`, `Waking`, `Climbing`.

**Client (f/creatures).** `DocksView` draws the capsules (G1) and stands (S1) from `Shipyard.Guards`, behind bodies and under
the unseen-room fog; a dock shows closed while a `Docked` or `Waking` guard is at it, open otherwise. The tick rig gains
folded, unfolding and climbing poses. A new A1 rig, built the way the Istukan's is. The hazard lamp and the red lamp are 2D
lights. Art exported through `tools/anime-art`.

## Determinism and co-op
- Inputs: heroes' reported cells come from crew commands; hits arrive as `HitBy` and `Bolted` from the hit hero's client, as
  today; placement uses Shipyard's local Rng, never the world's.
- Integer units: thousandths and ticks; counts round up `(n + per - 1) / per`.
- Order: guards run in id order (placement order); quarry ties go to the lowest peer id; path steps keep their fixed order.
- A joiner receives the surface's snapshot, guards included; rooms and docks are rebuilt from class and seed.
- Saves: new `Mem` keys, an appended enum value and appended step numbers need no format bump. A load closes vessel worlds.

## Trade-offs accepted
- A hero standing in a doorway's columns is in neither room → no wake until he is past it → the rule the user asked for.
- A far quarry and a path search that hits its node cap → the spider steers straight and may wait at a wall.
- A closed vessel world never reopens (Places) → nothing kept across closing: the dead stay dead, woken guards never re-sleep.
- Guards know where every hero is → hiding works only against the robot, off its deck or out of its line of fire.

## Tests
- Scenarios: guard counts per class; seen through a door or standing in the doorway → no wake, one step in → wake and drop;
  the spider climbs a wall, never a back wall; it opens a door and a hatch; no guard opens the way-in door; robot: gauss at a
  far hero, lunge at a near one, a shot up at the gallery; hero behind cover on the gallery → the robot leaves through a door,
  never a hatch; a killed guard leaves its dock empty.
- Invariants over every class and seed: counts follow the formula; every dock inside its room; no guard in solid; the spider
  only on a floor, against a wall, falling or in a move; the robot on its deck's rows.

## Cost
Creatures (bodies, `Stalk`, Paths, spawn, Grid ops), Core's Shipyard, Registry wiring; client `DocksView`, the robot rig, the
tick poses; content: 2 species, `Aboard` rules, the gauss shot, dock sizes. About 2,200 lines (sim 900, client 800, tests 500).

## Open questions
1. Loot from killed guards: (a) the sentry's yields for the spider, more circuits and scrap for the robot; (b) nothing.
2. Toughness: (a) spider = sentry (30 HP), robot ×2 (60); (b) robot ×3 (90) and armoured against shots.
3. No reachable hero: (a) keep hunting; (b) return to its capsule/stand after a while and re-dock.
4. The way-in door: (a) never; (b) the spider follows into space.
5. A guard in the arrival room: (a) allowed; (b) never drawn there.
6. A blow on a docked guard from outside its room: (a) wakes it; (b) shielded while docked; (c) can be killed asleep.
