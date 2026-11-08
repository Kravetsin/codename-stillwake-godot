# Radar contacts

- Status: accepted
- Date: 2026-11-08

## Context

The radar was only a picture: the ship, the sweep, a planet's curve. The concept has the radar find places and the engine
take the ship there. The user asked for the first half: random points on the orbit, each a place with a derelict ship,
at different distances from the player's ship; one coming every 15 to 30 minutes and staying 15; a click frames it and
names it "Derelict ship".

## Decision

- **Contacts are places' contacts** ([generated-vessels](2026-10-30-generated-vessels.md)): the vessels the radar found,
  each grown from its class and seed. The radar finds them itself now, on a timer aboard; a contact also has a place on
  the orbit (`Offset`) and a time left (`Left`). `Find` adds one by hand, placed the same way. A first take had a module of
  its own, `contacts`; it met places on merging and was folded in, so there is one kind of contact and one jump.
  Every peer sees the same ones: drawn from the world's `Rng`, counted in ticks.
- **The numbers** (`c/Contacts.cs`): the first a fixed 15 minutes into a world (so short runs and tests never draw); each
  next 15 to 30 minutes after the last, evenly; each seen 15 minutes; 25 to 100 radar units either side of the ship, 100
  being the radar's edge. A unit is 10 km (`KmPerUnit`): a contact lies 250 to 1 000 km away, and a selected one says how
  far under its name ("640 km").
- **One at a time.** With these numbers a contact goes as the next comes at the soonest, so the radar shows one or none,
  empty for up to 15 minutes between them. More at once means a longer stay or a shorter wait.
- **On the radar** a contact is a green diamond on the ship's orbit, an offset of 100 at 45 % of the screen's width from
  the ship. It flares as the beam passes it and fades over the turn; in its last 2 minutes it blinks. A click frames it
  with corner brackets and names it "Derelict ship" whatever its class; a click on empty screen lets go. The pointer names
  it too. While one is selected a "Set off" button stands under the radar: it sends places' `Jump`, the vessel written
  right of the ship as built, the contact gone.
- **The selection is the player's own** (`Ui.RadarContact`), kept while the computer is closed and let go once its
  contact is gone.
- Save format 59: a contact held in an older world lies at the nearest, right of the ship, with the whole 15 minutes;
  the radar's own first comes 15 minutes after loading.

## Consequences

- The radar's look of a contact is a first take, drawn in code; its pick on a page of candidates comes later.
- Where a contact lies changes nothing yet: every vessel is written the same gap right of the ship.
- The computer's rules still live in the client: contacts come whether or not a radar is built, and only aboard; a
  computer on another surface shows none.
