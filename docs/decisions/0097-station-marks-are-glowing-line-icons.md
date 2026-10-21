# 0097 — Station marks are glowing line icons

- Status: accepted; built
- Date: 2026-10-21
- Amends: [0086](0086-modules-one-socket-for-machines-and-tools.md)'s tech icon, now one case of a shared icon;
  [0060](0060-the-farm-a-grow-capsule-is-a-fed-station.md)'s faint seed and [0062](0062-drones-keep-machines-going.md)'s
  drone silhouette in empty slots, now ghosts of marks

## Context

The arc module's mark (0086) is a crisp vector icon: a thin hexagon, a glyph in thin lines, a white-hot core, a coloured
rim, a soft glow, a dark halo in a slot's corner, crackling on twos. Every other mark a slot wears with a station open was
7 px pixel art drawn at 14 (`AsciiArt.Flame`, `AsciiArt.Sprout`) or a bold "?" label: three languages for one job. The
user asked for the others in the module's style, and for the station's own slots to show their mark, as a socket shows
its tech's ghost. The furnace's two inputs, ore and fuel, shared one flame, so its panel could not say which slot is which.

## Decision

- **One icon, its look data.** `IconLooks.Look(Frame, Glyph, Core, Rim, Glow, Motion, Pivot)`: strokes in shares of the
  side, drawn by `IconCanvas` (the arc icon page's passes, generalised from `TechCanvas`), shown by `GlowIcon` (from
  `TechIcon`). `TechLooks` holds the technologies, `MarkLooks` the station marks, by `SlotMark`.
- **The frame says the kind.** A hexagon is a module's technology, reserved for it; a ring of the hexagon's width is a
  station's work. One family at a glance (lines, halo, glow, size, corner), and a module still tells itself apart.
- **A mark per meaning**, each its glyph, colours and motion, gentle and on twos:
  | Mark | Glyph | Colour | Motion |
  | --- | --- | --- | --- |
  | burns (a fire's fuel) | flame | orange | flickers from its tip, its foot still, brightness 0.85–1.15 |
  | smelts (a fed station's input) | hot ingot, two wisps | gold | wisps sway as heat rises, brightness breathes |
  | grows (grow capsule) | sprout on the ground | green | sways from its foot |
  | not analysed; the analyzer's slot | "?" | violet | still: it marks most of an early bag at once |
  | charges (charging station's slot) | battery | charge's green | bars light from the bottom, then empty |
  | docks (drone bay's slots) | quad from above | the drones' sky | blades spin, neighbours the other way |
  Smelting and burning get their own marks, so the furnace's inputs differ at a glance.
- **When a mark shows is unchanged** (`Reading.MarkOf`, `HereOf`): only its picture changed. Charges and docks are only
  ghosts; no bag slot wears them, as no bag slot wore a mark for those stations before.
- **A station's empty slot shows the ghost of its mark** (`Reading.GhostOf`), as a socket does: rim only, faint, still,
  filling the slot's inset. While a stack that would go in is carried it lights and moves (`Reading.Lights`): what wears
  that mark with the station open, a fuel, what waits for analysis, what holds a charge, a drone. A slot drones keep
  shows its kept item instead. Outputs, crates and queued stations' inputs show none.
- **Cheap:** a motion that comes round (`IconLooks.Cycle`) is painted once per frame for every icon of its look, size and
  show and shared as a texture; only the arc's crackle is painted afresh, as before. Each icon starts at a frame of its own.
- **Cards:** "Smelts here" and the like, and the "Not analysed" band, carry the lit mark at 24 px, moving as the slots' marks do, as a module's strip its tech; at 12 px the motion did not read.
- **Dev:** `-- --testbed --open=<kind> --marks <item> --into <slot>` empties that machine, turns creative off, forgets
  what was analysed, fills the bag with what each station marks, and carries the item over the slot (`MarksDemo`).

## Rejected

- **The hexagon for every mark:** a module would no longer be recognisable by its frame.
- **No frame:** larger glyphs at 14 px, but a loose set of pictures rather than one family beside the module's mark.
- **A crucible for smelting:** at 14 px its bowl and melt read as a face; the ingot reads as metal hot from the fire.
- **A side-on drone:** with its legs it read as a cart; from above, four rotors read as a drone.
- **Motion on the "?":** a bag of unknown items would twitch all over; it marks a state, not what a station does.

## Deliberately not handled

- **The ingot and the sprout are small at 14 px:** recognisable by colour first, shape second; a pass with the user's eye
  on them may enlarge the glyphs.
- **The furnace's fire square between its slots** stays a lit square: it shows the fire's state, not what goes in.
- **No sound** for a ghost lighting.
