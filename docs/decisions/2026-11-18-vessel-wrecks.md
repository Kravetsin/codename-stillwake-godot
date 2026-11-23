# Vessel wrecks

- Status: accepted
- Date: 2026-11-18

## Context

Every vessel the radar found arrived whole ([generated-vessels](2026-10-30-generated-vessels.md), "wrecks to come"). A wreck turns
the same four classes into places that read and play differently: a crater is a new way in, an opened room floats, a burnt hold
holds less, a broken ship is two halves; the story told without words: what happened, in what order, how long ago. The user
picked the look on a mockup drawn from real stamped vessels: https://claude.ai/artifact/X9dqanpx3SFFVFXDsUt3Z8 (version 5).

## Decision

- **A wreck** is one of the four classes built as intact, then a **chain of 1–3 events** in time order. `WreckPercent` (60) of
  contacts are wrecks, drawn from the seed; the rest intact. Count by class, each count as likely: shuttle 1, freighter 1–2,
  liner and tanker 1–3. Each next event is drawn evenly from those allowed on the ship as it stands; none allowed ends the chain
  short.

| Event | Allowed when | Where | At most |
|---|---|---|---|
| Explosion | not yet drawn; its seat outside the arrival room's ring | the stern's engine room's main engine; a tanker's tall tank of the two bays nearest the middle | 1 |
| Fire | a compartment ≥ 4 wide is sealed, unburnt, not the arrival room | first: the galley (liner) or widest life support, else quarters; after an explosion the 1–2 sealed nearest it within 3 radii; else the nearest the latest event | any |
| Icing | first (power loss), or the latest breach is unfrozen | the latest breach; first, the whole ship | 1 |
| Asteroid | fewer than 2 (liner, tanker) or 1 (others), a spot exists | a column over a module's top, 3 crater radii from another, its crater clear of the arrival room | 1–2 |
| Break | not a shuttle, not yet drawn, a rib 22+ columns from both ends whose cut leaves the arrival room whole | the rib nearest the latest explosion or asteroid (its cause), else overload: one of the two nearest the middle | 1 |

- **An asteroid freezes its own breach** (a strike alone read dull): its frozen compartments recorded, icing refused after it.
- **Groups and ages:** fire after an explosion, icing after an explosion, asteroid or break, and a break after an explosion or an
  asteroid join the group before them. The last group is fresh or old (`FreshPercent`), every earlier one old. An old first group
  brings scavengers: skin panels taken round 2–4 ribs (ribs kept), up to two holes patched. Engines stay unless an event carved
  them; rooms not reached keep everything (scavengers take nothing from rooms).
- **The arrival room** (the compartment just inside the intact vessel's first door) and two cells round it are never touched; a
  break leaves it whole on one side, the spawn following its door.
- **The radar marks a wreck** (2026-11-18, the user): its silhouette cracked and darker, "Wrecked" under its distance, in its tip and its
  callout's title, read from its seed by `Shipyard.IsWreck` without building it; what wrecked it is learnt on arrival.
- **One builder:** `Shipyard.Build(cls, seed, forced)` returns `Built(Vessel, VesselWreck?, Parts)`: described, stamped, wrecked
  (`Hulk`, a port of the mockup's geometry on integers), every caller on it (`Places.Opened`, `World.Open`, the client's dress,
  lights, signs, beacons, looks and motion); `Stamp` internal. Draws are the vessel's own streams: 3 the wreck, 4 + i event i,
  9 scavenging, so an intact vessel stamps as before. A jump builds once: `World.Open` builds for the map and hands the same
  `Built` to `Places.Opened` (`ISurfaces.Vessel`, set only while the surface opens); the jump tick's budget is a desk's 200 ms.
- **Forced chains are data:** `Find(Class, Wreck?, Wrecked?)` (`Wrecked` a dev's ask for a wrecked or an intact vessel: the seed drawn again till `IsWreck` agrees) → `Contact.Wreck` → `ISurfaces.Visit`/`Open` and the shuttle's `Voyage` →
  `Surface.Wreck` (saved), each null for the seed's own; an empty chain is intact; a step not allowed at its turn is skipped.
  Every peer and a late joiner build the same. The dev flag `--vessel=<class> --wrecked=blast,fire,rock:old` forces one.
- **The record** (`VesselWreck`): each event's kind, age, group, centre, radius, direction, carved cells, burnt or frozen
  compartments (a fire's heat each), break, stones and cause; the vented compartments; the patches. The client draws from it alone
  and never re-runs the damage: looks painted once per 32-cell chunk (`game/features/wreck`), motes as one multimesh a kind and
  chunk swaying in their shader, drifters (shards, bits, stones), sparks and vents from the chunks in view.
- **After the user played it** (2026-11-20):
  - *Nothing breakable stands in the air or floats* (2026-11-21, the user: thrown machines laid in space read unreal). A machine
    keeps the side it was stamped resting on: a lamp's or a chandelier's ceiling (`Hulk.Hangs`), else its floor whenever anything
    held it there (a tall tank stands on its floor, never by the wall beside it), else the side most held (an outside part off the
    hull: a truss, its container, a sphere, a nozzle, the boom). Once the chain is done, one holding by fewer cells of that side
    than stamped is lost, again till all hold. What an event throws out or knocks loose is lost too, never laid nor drawn. Only
    decoration is outside the hull's support: motes, shards, bits, stones, dust, ice. A stone lies only on a floor under its whole
    width. Scavengers leave the plates a part rests on.
  - *A room a breach opened is not dark* (hidden-rooms): what joins space shows from the first look; sealed rooms stay dark till seen.
  - *Lamps* burn as an abandoned vessel's: the airlock's lit, one flickering, `LampRedPercent` lit, the rest dead; a wreck's lit
    ones red, an intact vessel's white. A lamp an event carved is gone.
  - *A tanker brings 150–300 litres of fuel* (mean 219, 10th–90th percentile 163–287 over seeds 1–200): its fuel column holds
    `ColumnTank`, a tank bay's capsule is fuel `FuelCapsulePercent` (55) in a hundred (a sphere under it then water); a fuel tank
    broken gives `FuelOnBreaking` besides what it held.

## Later

- **Bodies in suits** come in a change of their own, their look being picked on https://claude.ai/artifact/VhnfPuF5M6Fruf8MSucWMx.
  Their placement as designed: 0–2 per explosion, fire, asteroid or break, in compartments the event reached (by a shut door a
  fire stopped at, by a breach, or afloat just outside one), never in the arrival room; breakable, a suit's scrap in them.

## Deliberately not handled

- A compartment cut off by a break or a crater: tools break what stands, a hero cuts in; no reachability past the arrival room.
- No warning before docking with a burnt or frozen wreck; hazards aboard (live cables, fire still burning, cold that hurts).
- Ice as water and stones as ore (a stone gives a small asteroid's loot for now); debris near the wreck as salvage the net
  catches (drifters are decoration only); air simulated aboard (rooms stay airless); an in-game replay of the events.
- Contacts found before content changes come out differently after: contacts are short-lived.
- A chain ending short of its count when nothing is allowed (a small ship burnt through): rare, measured by the property tests.
