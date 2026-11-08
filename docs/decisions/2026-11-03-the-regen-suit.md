# The regen suit and the suit's mode

- Status: accepted
- Date: 2026-11-03

## Context

The heavy helmet ([the-heavy-helmet](2026-11-03-the-heavy-helmet.md)) gives a flight more air, but the air still only runs
down. The user wanted a body that turns belt charge into air, balanced so it never makes the hero immortal: "it just
keeps the breath going and slows the air's fall a little in flight". Since [suits-are-worn](2026-10-27-suits-are-worn.md)
the body is a slot of its own, so the regen suit is a body, worn with any helmet. More bodies with an ability of their
own will come, so the switch is one mode for every body that has one. A canister got a key too.

## Decision

- **The regen suit** (`Suit.Regen`, `regenSuit`). On, after each breath it gives back what the breath took from the
  helmet's air, at most `Suit.RegenMost` (1) a breath, for `Suit.RegenCost` (15) power-ticks of the belt's charge a unit,
  taken as an electric tool's draw: from the first battery on the belt that holds it all. Nothing taken, nothing paid (an
  aired room, the pod's air); no battery holding 15, nothing made. It runs after the thin-air clamp and before the cold.
  - With today's numbers (a breath a second, running 2 a breath, the jetpack a unit per 10 ticks, a battery 1800
    power-ticks): at rest the air holds, running loses 1 a second, the jetpack 2 instead of 3, a battery gives 120 air.
  - It never pays the cold, off the grid, lava or a blow, and never charges itself: the charger alone does.
  - A tool running from the same belt pays first, for `Items` runs before `Crew`; what is left pays the suit.
  - 15 a unit, not the 30 first proposed: at 30 a battery gave 60 air, too dear to wear (the user).
- **Its blueprint only the black market sells**, on `order5` beside the belt's third slot, the charge it runs on: eight
  times its worth, 12000 (`ResearchTests`). Made at the workbench of a frame, plates, coils, circuits, wire and tubes.
- **The suit's mode** is the hero's flag (`Hero.SuitOn`, saved, off for a new hero), worked only while the worn body has
  `ItemSpec.Switch`: `SuitMode(On)` is refused `crew.noSuitMode` switching on without one, never switching off. The flag is
  the hero's, so another switched body put on is on with it; its light says so.
- **V switches the mode and R breathes a canister**, each also a key on the tablet's edge beside air, build and research:
  dim and inert while the sim would refuse it, its tip saying why; the mode's key lit while on. The edge is five keys wide.
- **Looks, as picked** from candidates drawn at the game's scale: the "cable harness". Quilted grey, cables in copper
  bands from a battery socket at the hip to a brass ionizer collar with vent slits. On with charge, a wave of white-violet
  light climbs the bands from the boots to the collar after each breath and arcs flicker in the slits (`SuitGlow`); on
  without it, the bands dark and a red pip blinking at the socket. A later body has its own `hero_<id>_<part>`, its lights
  `_lit`, `_arcs` and `_pip`, drawn unlit over it. The "capacitor spine" was picked for a later suit, after the planet
  descent; its ability comes with it.
- **The keys' glyphs** were placeholders; [edge-lenses](2026-11-03-edge-lenses.md) draws the mode's as a lens and drops the canister's key.

## Consequences

- At rest in vacuum the air never runs out while the belt holds charge: the regen suit is a way to wait, not to fly.
  Flight still ends; the cold still kills.
- A battery holding less than one payment is stranded for the suit, as for a tool.
- The wave runs on every breath while the mode is on and the belt could pay, whether or not air was made: by the pod it
  pulses for nothing. Accepted: the light says the suit is ready.
- A body lying in a bag and a hero seated in a pod show no lights.
- A switched body never turns itself off on an empty belt: the pip says it, and nothing is drained.
