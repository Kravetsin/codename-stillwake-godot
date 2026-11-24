# Wreck guards

- Status: accepted
- Date: 2026-11-25
- Supersedes in part: [derelict-guards](2026-11-23-derelict-guards.md) (guards on intact vessels only; an awake guard hunting
  its quarry whatever the distance; the robot always leaving its deck for a door with its quarry off it)

## Context

Guards stood only aboard intact vessels, so a wreck, half the contacts, was the safe one to loot. Once guards lived on, the play
pass showed three more faults: a spider ran after a hero across the whole vessel, never letting go; a spider on a wreck walked off
a floor over a hole, flew a pounce through one and fell out the hull's bottom off the map; and a robot whose hero climbed a ladder
above it turned away for the nearest door though it could see and shoot him. The user asked for guards on wrecks, a spider that
loses its hero and patrols, and a robot that stays under a hero it sees. Same mechanic as derelict-guards
([0049](0049-design-the-mechanic-not-its-first-use.md)): placement in `Shipyard.Guards`, brains in Creatures; enemy vessels and
stations next.

## Decision

**Placement on a wreck** (`Shipyard.Guards`, from the same stream 16 as an intact vessel's, so the wreck stays as it was):

- Fewer: `ceil(small / 7)` spiders and `ceil(big / 4)` robots (`Vessels.Aboard.Rule.WreckPer`); intact vessels keep 5 and 3
  (`Per`). A cut room's bow half is not counted again.
- A dock anywhere its box still stands: refused if a cell of the box is in an event's carved list (`WreckEvent.Carved`) or has
  lost its back wall (open to space), or a cell of the floor row under it is carved or bare (a sunk deck). Ladders and hatches are kept clear as before, a shaft cut short too; only furniture the
  chain left standing makes a stretch crowded. Never the arrival room (as before).
- Places lays `Shipyard.Docked(built.Parts, Shipyard.Guards(built))` for a wreck as for an intact vessel: a capsule still replaces
  what its box covers.

**Born awake on a wreck** (`Aboard.Opened`): each guard keeps `DockDrive.Woke` from birth, its dock open from the first, its next
drive in charge. No wake animation or sound: `DocksView` draws the wreck's docks open and silent.

**The spider loses its hero and patrols** (`Prowl`, `Content.Creatures.Spider`):

- With no awake hero aboard within `Lose` (40 cells, squared distance) it patrols; one within `Chase` (30) it hunts again. Between
  the two it keeps what it was doing.
- A patrol goal is drawn from the world's Rng (`CreaturesModule.RoamTo`), out of a hundred: `UpAWall` 25 a spot up a room's solid
  side wall (no doorway in it), climbed to and clung to `Cling` 40–80 ticks (2–4 s), then down to the floor; `OtherDeck` 33 a room
  on another deck, by the well rising from the floor it stands on (`WayUp`), or a hatch in its floor down; the rest a room on its
  own deck. Another goal once there, at a brink or wall it cannot pass, after `Roam` (1200 ticks), when no hatch leads from its
  floor toward its goal's deck, or when a way by decks would have to turn back.
- Up a well it climbs on through the next hatch only where that hatch's well rises from the floor it came up to (`WellOver`); under
  anything solid but a shut hatch (a wreck's sunk deck) it lets go.
- Pace: patrolling at its `Walk` (300) and its climb cut to the same share of its run, rounded down (`Climb · Walk / Run` = 100);
  hunting at its `Run` (900) and `Climb` (300).
- **Never leaves the hull**: no step past the hull box's side or the open way in, none over a hole down to the hull box's bottom
  (`Floored`); a pounce or leap stops where it would. A hunt waits at such a brink; a patrol draws another goal.

**The robot keeps a seen hero** (`Patrol`, the escort): its quarry off its deck but in its sight (its gun's line, within its
`Notice`) keeps it on its deck under him, shooting when ready, never prying a door for it; only out of sight does it head for a
door. Patrolling to a door it walks at its `Walk` (175); going for its quarry, at its `Run` (350).

**Saves:** new `Mem` keys and a defaulted `Prowl` record, no format bump; guards live only in vessel worlds, which a load closes.

**Tests:** `WreckGuardsTests` (placement, awake at open docks, two worlds alike aboard a wrecked liner), `SpiderPatrolTests`
(lose, chase, pace, the brink, decks and walls, two worlds alike through a patrol and a join), `EscortAboardTests` (sight).

## Deliberately not handled

- Stealth beyond distance: within `Chase` the spider knows where a hero is through any wall.
- A patrolling robot: with no hero aboard it stands, as before; only the spider loses a hero and roams.
- Path search: a patrol goes by decks, wells and doorways; a goal the decks cannot reach is dropped for another.
- A wreck's events sparing its guards: a burnt, frozen or vented room may hold a dock (only carved or bare cells refuse one), and
  its guard is born after the chain, unharmed.
- Guards on stations, enemy vessels or the player's ship.

## Accepted limitations

- Patrol goals come from the world's Rng, so a vessel world's draws move with its guards; the same on every peer.
