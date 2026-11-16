# Plan: the solar system and the flight between its planets

The crew picks a planet of its system on the radar and the ship flies there: half a minute of stars torn past the hull
and the engine burning at the stern. Then the next descent lands on the planet picked. The flight is what this step is
about (the user, 2026-11-16); the planets are alike for now, and making them differ is the next step.

Words: a **surface slot** is a surface's place in the world's list (its id); a **closed** one keeps its id, kind and
seed and nothing else, as a vessel world's once everyone left it (vessel-worlds). A **system planet** is an entry of
the solar system, by index; each visit to it opens a new surface slot from a new seed.

## Rules (the user's, 2026-11-15 and 16)

- A world has one solar system of `Content.Landing.System` planets (6; 5 to 10 in time), each with a planet type and a
  technology, the ruins' set that prevails there. Today every one is the first type and Arc. The ship starts at
  planet 1, so the first descent goes there, as today.
- The course is set on the radar: a planet picked, its card read (its number, type and technology), Set course pressed.
  The engine needs power and `Content.Landing.JumpFuel` (25) biofuel in its fuel slot, each flight. Its own key goes.
- The flight is `Content.Landing.FlightTicks` (600, 30 s). Meanwhile descents are refused, `landing.inFlight`.
- Leaving closes the planet below for good: one planet loaded per world, nothing of it kept. Flying back to a planet
  visited is allowed and lands somewhere new: another seed.
- Refused while any hero is not aboard (on a planet, on a vessel world, in a pod in the air); the refusal is the call
  to everyone, wordless: a sound and an amber flash along the screen's edge on every peer, wherever its hero is.
- A pod needs no new rule: it goes where its hero goes (`ICarries`).
- The looks and sounds are stand-ins until the user picks them on a page. Stand-in sounds are one-shot cues.

## Player sequences

1. Solo: engine built, powered, fuelled. At the computer's radar a strip of the system runs along the top: the sun,
   six planets, the ship by planet 1. A click on planet 4 shows its card; Set course. The burn: a white flash, a
   blast; the planet below closes; the engine flames out of the stern; the stars stream past faster and faster, torn
   into lines, and ease off at the end. On the radar the ship's mark crosses to planet 4. A thud: arrived. Descend
   opens planet 4.
2. A hero below, in a pod in the air, or on a vessel world: Set course calls everyone; nothing leaves.
3. Descend pressed in flight: dim, `landing.inFlight` in its tooltip.
4. Back to planet 1: a course like any other; a new landing place.
5. Save and load in flight, or a guest joining in flight: the ship arrives on the same tick on every peer; the stars
   stream as they would. No burn plays for a load or a join.

## Sim (Landing, extending the try)

Saved in `LandingState` (aboard only; defaults mean "none yet", so no format bump): `At`, the system planet the ship
is at (0, shown as Planet 1), and `Course(To, TicksLeft)` (null). Both are written only off their defaults, so old saves and
hashes, and every planet's slice, stay as they were.

`SetCourse(int Planet)` replaces `FireEngine`. Its engine is the lowest-id machine aboard whose spec has an `Exhaust`.

| Reason | When |
| --- | --- |
| `landing.noEngine` | off the ship, or no engine aboard |
| `landing.hereAlready` | `Planet` is `At`, or no planet of the system |
| `landing.inFlight` | a course runs |
| `landing.engineUnpowered` | its `Satisfaction` under 1000 (thousandths of its draw) |
| `landing.noFuel` | under `JumpFuel` biofuel in slot 0 |
| `landing.noExhaust` | `Exhausts` false: its nozzle walled in since it was built |
| `landing.crewAway` | `Away`, or a flight in the air from the ship; last: "ready but for you" |

Applied: the fuel taken, `Surfaces.LeavePlanet()` (none open, nothing to close), `Course = (Planet, FlightTicks)`.
A Construction system aboard counts it down; at 0, `At = To`, `Course = null`. `Launch` aboard is refused
`landing.inFlight` while a course runs. `nothingBelow` and `ISurfaces.PlanetOpen` go. The planet a descent opens is
still `Content.Landing.Planet`: every system planet is that kind today.

Order in a tick: commands before systems, so arrival comes after the tick's commands: a `Launch` on the arrival tick
is refused and descends on the next. The count starts in the set-off tick's own Construction: a course set on tick T
arrives in the Construction of tick T + FlightTicks − 1. The client's warp and the radar's mark read
`FlightTicks − TicksLeft + progress` alike (`logic/Warp.cs`).

A world saved with a friend below and loaded alone is not stuck: `Enter` releases every other peer's hero with `Leave`,
which sends it aboard, so `Away` is false on the first tick.

## Client

- **The radar's system strip** (`RadarScreen`): the sun at its left, the planets evenly along a line, the ship's mark
  over `At`, moved toward `To` by the course's progress in flight. A click on a planet picks it (and lets a contact go,
  and the other way round); its card: `Planet N`, its type, its technology; the bottom button says Set course and sends
  `SetCourse`, dim with the reason on the card but at `crewAway`, which sends: the call.
- **The flight** (`Stars`): a warp from 0 to 1 eased in over the first 4 s and out over the last 4 s, read from the
  course's ticks and the session's progress. The star tile streams up to 60 times faster, lines drawn over it longer
  with the warp; the far wrecks and the dust fade out meanwhile.
- **The engine burning** (`features/landing`): a flickering flame out of each engine's nozzle, its light a `Glow`, while
  a course runs.
- **The cues** (`JumpCall`): the call (amber edge, a beep) on a `crewAway` refusal; the burn (white edge, a one-shot
  blast) when a course appears and the arrival (a one-shot thud) when it ends, between two ticks of one world, never on
  a load, a join or a resync. `EngineKey` goes.

## Tests

- Scenarios (`CourseTests`, from `JumpTests`): a course takes the fuel, closes the planet, refuses descents, and after
  `FlightTicks` the ship is at its planet and the descent opens a new slot; the first descent with no course; each
  refusal, the world untouched (a hero below, a pod down, a pod up, a hero on a vessel world, no power, no fuel, the
  nozzle walled in, here already, in flight); a `Launch` on the arrival tick refused; flying back opens a new slot;
  save and load in flight, two worlds alike and arriving together.
- Invariant (FsCheck): whenever a course starts every hero and pod is aboard and no planet is open; a twin saved and
  loaded hashes alike. Proven to bite once by leaving the pod behind when its owner's hero leaves.
- Landing's `Fuzz` draws `SetCourse`. Client: the jump's cues are one-shots; the warp's curve (`logic/Warp.cs`).

## Heading (not built)

The next step makes the planets differ: each system planet's type, technology and numbers (gravity, cold, ores, relief)
drawn from a seed, saved; planet 1 always Arc; the descent opening the type at `At`, and a surface's rules found from its
description rather than its kind id (`Surfaces.Get` in `Attach`, `Open` and `Load`). After it, other systems and better
engines.

## Deliberately not handled

- Other systems and galaxies, better engines, engine parts from the planets.
- Differences between planets, names; a vote; naming who is away; changing course in flight; fuel by distance; events
  on the way. Contacts react to nothing of it: they keep counting, and Set off works in flight.
- A second engine adds nothing: the first is the one.
- A dev's `OpenSurface` in flight opens a planet the arrival does not close: the next descent goes there. Dev only.
- The generic suites' world has no fuelled engine, so their `SetCourse` is refused; the applied course is fuzzed by
  `CourseTests`' property, long enough to arrive.
