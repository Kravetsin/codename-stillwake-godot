# The power chain

- Status: accepted. The fermenter and the biofuel generator built first; the treadmill wheel in a PR of its own.
- Decided by the user (2026-11-06), after the playtest of that day, `design-author`, `design-reviewer` and `/review-plan`
  (`docs/plans/power-chain.md` while it is built).
- Amends [0060](0060-the-farm-a-grow-capsule-is-a-fed-station.md): biomass no longer smelts at the furnace or the smelter.

The planet stage asked for more power than burner generators fed by hand give. Three machines, each a row of data on mechanics
that exist, make a chain: a powered vat that makes the fuel, a heavy burner of that fuel, and the earliest source of all, a
wheel a hero runs in.

## What the player meets

1. **The fermenter** opens with the grow capsule's parts (frame, glass, tube, plate, a circuit). It is a fed station on power:
   biomass or fat in, biofuel out, one in 160 ticks at a full share, drawing 2. It is **the only way to biofuel**: biomass and
   fat put in a furnace or a smelter sit there unworked, as a seed does. Fat still burns in any fuel slot.
2. **Before the first landing** the player grows biomass in a grow capsule as before and ferments it, instead of smelting it,
   into the drop pod's five biofuel. One machine more, of the same tier, on the power the capsule already needs.
3. **The biofuel generator**, 6×4, of a planet's fuel pump, frame, plate, coils and tube: a fuel burner whose slot takes biofuel
   alone, making 16 a tick for the 1600 ticks a biofuel burns, 25 600 a biofuel against the burner generator's 9 600. It
   breathes its room's air and lights only while its network wants a fire, as every burner does (0101).
4. **The treadmill wheel**, 6×6, of planks, gears, a coil and scrap: the first power there is. A hero gets in with E and holds a
   move key to run; while an awake rider runs, it makes 4, anywhere, in a room or out. Empty, still or its rider passed out, it
   makes nothing. Running, the hero breathes as a runner does.

## Decisions

- **Biofuel at the fermenter only** (the user's word). The furnace stays the fire of raw salvage; fuel making is a powered
  step of the farm, so the farm's chain reads seed → capsule → biomass → fermenter → biofuel. A new process, `ferment`, on
  the existing fed-station model: no code in Crafting, only an item's `Process` and a station's.
- **The biofuel generator is data.** `FuelSlot` and `Takes` together: a fire that burns one fuel only. The engine keeps
  `Takes` without a `FuelSlot`, since it must not burn its tank.
- **Its fuel pump puts it at the planet stage**, so the first planet is where the ship's power grows past hand-fed burners.
- **The wheel has no stamina** (the user's word): a hero must stay in it, so it cannot replace a burner, which runs alone.
- **`Anywhere`, not `Seats`, frees a maker from the stars.** Power reads a content flag; a crank or a water wheel would share
  it. A wheel counts in `Steady` with the panels, so while it covers its network no fire lights.
- **Landing rests the wheel.** It owns seats, so each construction it rests every seated maker whose rider is not here, awake
  and exerting. `Exert`, the hero's running, already reaches the sim as a command; a seated hero's held move key sets it.

- **Their looks are the user's picks** (in ink and watercolour, `tools/anime-art/subjects/machines.js`): the fermenter a
  glass drip column of mash, its copper line into a condenser coil and a tap dripping into a jerrycan, bubbling under a
  green lamp; the biofuel generator a hot-bulb engine, a firebox and pump by two finned cylinders and a holed flywheel that
  turns while it burns; the wheel a steel hoop lined with planks on two rollers, the rider drawn between the turning hoop
  and a grab rail in front.

## Deliberately not handled

- A rider pausing lets a fire on its network light, and that fuel burns to its end.
- No stamina, no output scaled by the rider's speed; running in vacuum costs the suit a runner's breath.
- Biomass in a furnace from an older save sits there till taken out.
- No ferment mark on the slot (the crusher's precedent: the crucible); no new sounds, reused cues until the user picks.
