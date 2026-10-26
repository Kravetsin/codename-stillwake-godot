# Creatures are composed

- Status: accepted; nothing built yet: the plan is [`creatures`](../plans/creatures.md)
- Date: 2026-10-26

## Context

The game will have very many and very different creatures, Terraria- or Starbound-like: hostiles in space and on
planets, bosses with phases and big or segmented bodies, raids on the ship, predators and prey, an immortal pet a
player fights beside, maybe a companion that carries things. The first hostile is to be a sentry robot guarding a
derelict (a far-field wreck kind, 0056).

Today every creature is one flat `Content.Fauna.Species` record (16 fields, one more for every behaviour) driven by one
state machine: `Mode` has 8 values saved by number, switched in `Live`, the ram written into the switch, "the nearest
hero" the only target (0102, 0103). The client reads `Mode.WindUp` and `Mode.Charge` in five places. A sentry would add a
mode and a body branch, and every pet, turret or boss after it more: the 29 files the hatch touched (0112, 0113) again,
in the module that will grow the most.

## Decision

**One module owns creatures, renamed `Creatures`** (from `Fauna`: robots and pets are not fauna). No service modules:
- **Damage has no slice of its own.** Each target's health already lives with its owner: a suit in Crew, cracks in
  Building, a piece's damage in Debris, a creature here. A `Hit` record (who struck, the amount, from where, the
  weapon's pace) carries its source, and each owner's op is its entry point.
- **Shots live here too,** when they come: creatures fire them and they hurt creatures in the same tick, and two modules
  cannot depend on each other. Hero guns will send their `Fire` here.
- **Status effects live on their bearer,** as 0100 put them for cracks and debris: on heroes in Crew, on creatures here.
- **Factions have no state:** a content table plus a grudge on the creature.

**A species is composed** of data records in `Content`, each served by one rule class in the module:
- a **body** (`Shape`: walker, clinger, later flyer, segmented…) with what it can do (`Gait`: walk, climb, fly);
- its **life** (hit points, knock, hop);
- a **faction**;
- a **brain**: a priority list of drives (graze, flee, retaliate, guard…);
- its **moves**: acts in phases, wind-up → active → recover (a lunge, later a shot, a slam, a bite), each with the
  `Damage` it deals;
- a **lifetime** (`Lifetime`): wild (the keep box and linger, as now), bound to a home, and later kept (bosses, nests,
  pens) and owned (a pet with its owner);
- its yields, and where it spawns (`Spawner` records: habitats now, `OnDebris`, `Lair`, `Raid` with their users).

A new creature from existing rules is content, art and a name. A new mechanic is one rule class and its record; no
other file changes. A boss that fits nothing gets its own drive classes and view: the intended escape hatch.

**The rules' contracts** (a sketch; names may settle in code):

```csharp
abstract class DriveRule<D> where D : Drive {
  virtual bool Wants(Mind m, Creature c, D d);              // asked on a think tick; never draws from the Rng
  virtual void Enter(Mind m, Creature c, D d);              // takes charge; may draw
  abstract bool Run(Mind m, Creature c, D d, ref Intent go); // the drive in charge, every tick; false yields
  virtual void Hurt(Mind m, Creature c, D d, in Hit h);     // every drive on a hit, in brain order; may Claim
  virtual Turn Blocked(Mind m, Creature c, D d, int way);   // the body can go no further and found no leap
  virtual void Ended(Mind m, Creature c, D d, ActEnd how);  // an act the drive in charge began has ended
}
abstract class BodyRule<S> where S : Shape { bool Fits(...); void Step(Mind m, Creature c, S s, in Intent go); Box BoxOf(...); }
abstract class ActRule<M> where M : Move { void Begin(Mind m, Creature c, M mv, Ref aim); void Tick(Mind m, Creature c, M mv, ref Intent go); }
abstract class SpawnRule<P> where P : Spawner { void Beat(Mind m, P p); }
```

- **Rules hold no state.** One instance per module instance; whatever a creature needs remembered lives on the
  creature. `Mind` is the tick's context: heroes' cells, `Nearest(creature, stance, reach)` over the relations, `Claim`,
  `Begin`, `Hurt`, `Spawn`, the grid, the Rng and the owners' ops.
- **Discovery.** The module maps each record type to its rule class by reflection over its own assembly: a lookup
  never iterated, so no registry line and no shared list two developers edit. A record with no rule, or two, fails a
  contract test.
- **The brain.** On a creature's think tick the drives above the one in charge are asked whether they want to take
  over; the first that does, does. The drive in charge runs every tick and may yield, and then the drives are asked
  from the top. A drive may `Claim` the creature on a hit only if it outranks the drive in charge.
- **Acts** record the peers they have hit: a ram still hits a hero once, and a slam can hit all four.
- **The hit path,** one for every source: the struck body's wards and knock, then the damage (to the body sharing its
  life, for a segment), an interruptible act cut short, every drive told in brain order, the striker taken as the
  grudge (the creature's `Target`) when it has none, death at its hit points.
- **Death marks; one sweep removes.** A creature marked dead is gone to everything from that moment: commands on it are
  refused as gone, queries and the fuzz skip it, it counts toward no cap. Its yields roll at the mark. The sweep runs at
  the end of the creatures' loop, before spawning; the mark is never saved. Spawns append and act from the next tick.
- **Targets by relation:** (from, to) → ignore, fear or hunt; heroes and owned creatures are faction `crew`; a grudge
  overrides the table. Nearest by box distance, heroes before creatures at a tie, then by id.
- **Refs** (to a hero, creature, piece or cell) are resolved where they are used; one that resolves to nothing reads as
  none, and nothing clears them.
- **Saves.** A rule that needs its own numbers on a creature keeps them in `Mem`: a list of (key, int) pairs kept in
  ordinal order of the key (`string.CompareOrdinal`), never a dictionary, so the order a load produces is the order
  that was saved. A new rule then needs no new field and no bump. The first rule needing a list (a worm's trail) adds a
  typed part and bumps once. Looks stay derived from the surface's seed and the id (`CreatureVariety`), as now; a pet,
  which takes a new id on every surface, will carry its looks when pets come.
- **The client** draws by a look kind (the parts rig, a placeholder box, later segments and boss views), its poses and
  sounds keyed by the drive and act in charge and the act's phase, never by species; a guard test fails on a species
  id in client logic outside the looks.

**Decided with the user:**
- Creatures may damage the ship: raids will come. Boarders want to reach a hero and break only the walls and doors in
  their way, never machines.
- Creatures fight each other through factions: predators and prey, pets and hostiles.
- A pet is immortal, as a Terraria summoner's minion, one per player; the first, far off, perhaps a dog found on a
  derelict.
- Penned animals need air: without it they die over about ten minutes.
- A boss heals and goes home once every hero has left its arena: it cannot be worn down by dying and coming back.
- **Each player's own screen comes first.** This is co-op, not a competitive shooter: what each player sees must look
  right and stay stable; exact agreement between screens is not a goal, and no lag compensation is built. Hits on a hero
  stay judged by that hero's client (0034, 0103).
- **Heroes report as in Terraria:** while a hostile creature is within its keep box, a hero reports its cell on every
  tick it changes; otherwise every `Crew.ReportEvery` ticks, as 0050 set for the cold at the world's edge. Chasers and
  pets then aim a round trip behind, not a second.

**The stress test.** Before this was accepted the design was walked through thirteen future creatures: a segmented
worm boss, a phased boss that breaks walls and summons, a sentry turret that shoots, a drone swarm, a cave ambusher, a
mimic chest, a nest, a shield-bearer, a predator of lopers, the pet, an item-carrying companion, a penned animal aboard,
and a ship raid. None needs a change to the tick loop or the hit path, and none a new kind of saved state beyond the
pieces below; each needs only rule files, records, and these shared pieces, built with their first user: shots, owned
creatures crossing surfaces (`ICarries`, 0076), wards, paths (a flow field by the body's gait, through ladders and
hatches), a body sharing another's life, a `Building` op that wears walls and doors, and `Use` on a creature. The
raid's real cost is Building's: the ship has no breakable hull yet.

## Deliberately not handled

- **Collisions** between creatures, or between creatures and heroes: bodies overlap (0006 stands).
- **Line of sight:** creatures sense through rock.
- **Behaviour trees as data or a scripting language:** rules are C# classes.
- **PvP, and pets attacking heroes.**
- **Healing and damage across a despawn:** a creature that despawns forgets its damage.
- **A creature's own inventory:** the companion's bag is its peer's (Items carries it across surfaces already).
- **A\* for wild walkers:** they steer as today; paths come with boarders and swarms.
- **Fields and rules with no user yet** (wards, facing, owner, shared life, think intervals, neighbour buckets, the
  flinch of a stunned species): each comes with its first creature; a field added with a "none" initializer needs no
  save bump.

## Rejected

- **Hostiles as a guard record on a debris piece:** a second home for creatures, and nothing for the planets' hostiles.
- **A module per concern (Combat, Projectiles, Damage):** a module writes only its own slice, so what moves or hurts a
  creature must live with it; the split modules would need each other.
- **Keeping the flat species and mode switch, adding modes:** every new creature would touch the switch and the client.
- **Reporting the hero's cell once a second near hostiles, or five times:** enemies would charge at empty air.
- **A `SortedDictionary` for `Mem`:** System.Text.Json rebuilds it with the culture's comparer on a load, so a loaded
  world would order its keys differently from the host's and the hashes would part.
