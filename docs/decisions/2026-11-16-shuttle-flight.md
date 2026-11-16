# The crew flies to a vessel in the shuttle and back

- Status: accepted; built (pictures and sounds plain until slice 3 of [shuttle-flight](../plans/shuttle-flight.md))
- Date: 2026-11-16
- Supersedes: the "next slices" line of [crew-shuttle](2026-11-15-crew-shuttle.md); the radar's Set off sending `Jump` in
  [vessel-worlds](2026-11-09-vessel-worlds.md)

## Context

The radar finds vessels ([radar-contacts](2026-11-08-radar-contacts.md)), each a world of its own
([vessel-worlds](2026-11-09-vessel-worlds.md)), and the crew shuttle stands on its launch platform
([crew-shuttle](2026-11-15-crew-shuttle.md)). Until now the radar's button jumped the presser alone into the vessel. The
user asked (2026-11-09, 2026-11-15) for the trip as a crew: pick a contact, set a course, up to four board, set off, fly,
arrive together beside the vessel, explore, fly home and dock.

## Decision

- **The course is places'**: `SetCourse(Contact)` from the radar's «Проложить маршрут» / "Set course", one per world, set by
  anyone aboard, the last of a tick winning; cleared when its contact runs out, is jumped to, or is used. Every peer's radar
  shows a line to it and «Маршрут выбран, садитесь в шатл»; every shuttle aboard lights its course lamp.
- **Seats with places**: a machine holds `MachineSpec.Riders` heroes (the shuttle four), each at a place, the lowest free;
  place i lights slit i; a rider is never seen. `Launch` stays the pod's (`Riders == 1`).
- **Set off** («Отправиться», places' `SetOff`): aboard it needs a course, a powered run and a clear shaft; the contact is
  used, the course cleared, and Landing starts a `Voyage` of the seats held, the run opened under the shuttle. Off the ship
  it flies home. Flights take `Shuttle.FlightTicks` whatever the distance.
- **All riders to one world**: at the voyage's end one vessel world opens (`ISurfaces.Open(kind, seed)`, a system's ask,
  opened after the phases in the order asked) and every rider still on the surface is sent there in seat order, the first
  bringing the shuttle. One out flies on and arrives out; one gone is not sent.
- **The gap spot**: the shuttle stops in open space, its right edge `Shuttle.Gap` (30) cells left of the vessel's frame, its
  bottom row on the spawn's row; a vessel world floats, so a `Lands` machine needs no ground there. The jetpack crosses the
  gap; a player without one flies home for it.
- **Home is free** and docks the shuttle on its home platform, riders seated; home built over or its run gone, it is laid
  there as its cost. Its home is saved with it off the ship (`Parked`).
- **Fuel is burnt from the shuttle's own store** ([fuel-liquid](2026-11-16-fuel-liquid.md), its 200 l tank): the round trip,
  `2 × ceil(km / KmPerLitre)` litres with 10 km a litre, is shown on the radar and the shuttle's panel; setting off aboard
  needs it in the store (`landing.noFuel`, one millilitre short is short) and takes it all at once. The rest flies with the
  shuttle (`Voyage.Fuel`), stands in its store by the vessel, comes home, and outlives a closing world or is laid as cans with
  its cost, as a pod's fuel does.
- **Leaving a friend is allowed**: flying home while a friend is on the vessel takes the shuttle; the friend gets home only by
  dying or quitting, and that world then closes as usual.
- **The shuttle never dies with its world** (`IOutlives`, in `World.Close`): a vessel world closing hands the ship the homes of
  its parked shuttles and of its voyages, each docked home empty. A voyage's riders keep the surface they left open until
  they arrive.

## Deliberately not handled

- The return burns nothing, so a shuttle out is never stranded. Distance changes the cost only, not the flight time. Laid as
  its cost, its fuel's part of a litre is lost, as a pod's.
- No air or hold in the shuttle: suits cover the crossing, loot comes home in hands.
- No other way to the vessel: a friend who missed the launch cannot follow. No jetpack: fly home for one.
- The gap is measured from the frame's left edge, whichever side the door is on; nothing checks the spot at runtime, a
  content test keeps the margin wide enough.
- A contact is used even if nobody arrives; no revisit. The pod's `Flight` and the shuttle's `Voyage` stay two records.
- Nothing takes the shuttle apart off the ship, as with a pod.
- The course is set only aboard, from a computer's radar, by anyone, while riders sit too; they fly to the latest.
- A course cleared by its contact running out shows only by the lamp going out and the route text going.
- Riders who all quit mid-return while a friend stays on the vessel: the shuttle docks home only once that world closes.
- Returning to a home built over by another shuttle, later riders sit in that one.
- Plain pictures: the shuttle vanishes and appears, the riders see the pod's flight screen; the lit slits, the lamp and the
  course line are placeholders until slice 3's pick.
