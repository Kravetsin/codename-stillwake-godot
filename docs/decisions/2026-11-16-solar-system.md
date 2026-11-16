# The ship flies between the planets of its solar system

- Status: accepted
- Date: 2026-11-16
- Supersedes: the radar's look in [radar-contacts](2026-11-08-radar-contacts.md) (the sweep over a planet's curve) and the
  placeholder course line of [shuttle-flight](2026-11-16-shuttle-flight.md); the starting engine's own fire key

## Context

The user wanted planets to differ and the crew to fly between them (2026-11-15): one planet loaded at a time, a jump refused
until everyone is aboard and calling them home, pods never lost, a solar system of five to ten planets that the starting
engine flies within, a flight of at most a minute, each planet's card naming its type and the technology of its ruins. The
flight came first (2026-11-16); making the planets differ is the next step. The radar was redesigned with it: the user found
the old one unclear and not in the game's style, picked one zoomable map with callouts beside things (layout Б, callouts А).

## Decision

- **A world has one solar system** of `Content.Landing.Planets` (6), each a `SystemPlanet(Type, Technology)`, every one the
  first planet's type and arc ruins today. Landing keeps `At`, the planet the ship is at (0 from the start), and `Course`
  (`To`, `TicksLeft`), both saved only off their defaults: no format bump.
- **`PlotCourse(Planet)`**, landing's, set from the radar: the engine, the lowest-id machine aboard with an `Exhaust`, needs
  full power, `JumpFuel` (8 l) in its 20 l store and its nozzle open; refused `landing.hereAlready`, `inFlight`, `noEngine`,
  `engineUnpowered`, `noFuel`, `noExhaust`, and last `crewAway` while any hero is off the ship or a pod flies from it.
  `crewAway` is the call: every peer flashes amber at the edges and beeps. Applied, the fuel is burnt, the planet below is
  closed for good (`ISurfaces.LeavePlanet`), and the course runs `FlightTicks` (600, 30 s); descents are refused
  `inFlight` meanwhile. At zero `At` is its planet; the next descent opens a new surface from a new seed, a revisit too.
- **The flight is seen** as stars torn into lines (`Stars.Warp`, eased over 4 s at each end, `Warp.cs`), the engine's flame
  and glow at its nozzle (`EngineFlame`), a one-shot burn and a one-shot arrival (`JumpCall`), never on a load or join.
- **The radar is one map** (`RadarScreen`, `NavMap`): the system from above in the tablet's ink, the sun in the middle,
  each planet on its orbit; the wheel zooms 12 times down to the ship's planet, where the ship's orbit and the contacts on it
  show. The pointer names a thing; a click opens a callout beside it, on the side away from the middle, tied by a line of
  ink: a planet's number, type, ruins, flight time, fuel and `Plot course`; a contact's class, size, decks, distance, the
  shuttle's litres there and back, when it leaves, and `Set course` (the shuttle's route). A route set is a dashed line and
  its words on top; a stretch of space a band on the ship's orbit. Esc or a click on empty map puts the callout away.
- **The computer opens over the screen** as the tablet does, a close in its corner.
- **The radar draws only on a change** (zoom, pan, pointer, flight, a read, a blink) and writes its ink once: sketched boxes
  are kept by size and moved, orbits are unit circles scaled, the leader is rewritten only when its ends move, tooltips are
  made when first shown. Rebuilt every frame it cost 1.4 ms and 328 KB a frame, a collection every seven frames.

## Deliberately not handled

- Planets alike; other systems, better engines, fuel by distance, events on the way, changing course in flight.
- No vote and no naming of who is away: the call is wordless. A second engine adds nothing.
- Contacts ignore the flight: they keep counting, and a route can be set in flight.
- A dev's `OpenSurface` in flight opens a planet the arrival does not close. The generic suites' world has no fuelled
  engine; `CourseTests`' property fuzzes the applied course.
- The looks and sounds are stand-ins until the user picks them on a page.
