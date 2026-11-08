# Radar contacts

- Status: accepted
- Date: 2026-11-08

## Context

The radar was only a picture: the ship, the sweep, a planet's curve. The concept has the radar find places and the engine
take the ship there. The user asked for the first half: random points on the orbit, each a place with a derelict ship,
at different distances from the player's ship; one coming every 15 to 30 minutes and staying 15; a click frames it and
names it "Derelict ship".

## Decision

- **Contacts live in the sim**, a global module `contacts` beside `players` and `knowledge`: every peer sees the same
  ones, and the jump to come reads them. Drawn from the world's `Rng`, counted in ticks.
- **The numbers** (`c/Contacts.cs`): the first a fixed 15 minutes into a world (so short runs and tests never draw); each
  next 15 to 30 minutes after the last, evenly; each seen 15 minutes; 25 to 100 radar units either side of the ship, 100
  being the radar's edge. A unit is 10 km (`KmPerUnit`): a contact lies 250 to 1 000 km away, and a selected one says how
  far under its name ("640 km").
- **One at a time.** With these numbers a contact goes as the next comes at the soonest, so the radar shows one or none,
  empty for up to 15 minutes between them. More at once means a longer stay or a shorter wait.
- **On the radar** a contact is a green diamond on the ship's orbit, an offset of 100 at 45 % of the screen's width from
  the ship. It flares as the beam passes it and fades over the turn; in its last 2 minutes it blinks. A click frames it
  with corner brackets and names its kind over it; a click on empty screen lets go. The pointer names it too. While one
  is selected a "Set off" button stands under the radar: it does nothing till the jump.
- **The selection is the player's own** (`Ui.RadarContact`), kept while the computer is closed and let go once its
  contact is gone: the jump's target to be.
- Save format 57: an older world gets the slice empty, its first contact 15 minutes after loading.

## Consequences

- The radar's look of a contact is a first take, drawn in code; its pick on a page of candidates comes before the jump.
- The computer's rules still live in the client: contacts come whether or not a radar is built.
