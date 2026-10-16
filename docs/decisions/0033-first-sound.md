# 0033 — First sound

- Status: accepted
- Date: 2026-10-16

## Context

The game was silent: a breach, a low suit, a friend working beside you went unheard, and vacuum, the
main danger, was only seen. A first pass needs a few cues (steps, landing, the suit's alarm, the screwdriver,
venting, button clicks, the vents' hum, what the hands touch) and a shape later passes, settings and real recordings
fit into without rework.

## Decision

- **No sound in the sim.** Sound is presentation: nothing under `sim/` or `net/` changes, nothing goes
  over the network. Each peer plays what it sees, hooked where the client already draws the event
  (the figure's stride and landing, the suit gauge, the plan's sparks, the venting streams).
- **Buses by name are the settings contract** (`game/default_bus_layout.tres`, named in
  `game/Content/Sound.cs`): UI, Ambient and Effects into Master; World (every hero's steps, machines,
  air) and Suit (what this hero's hands touch, its suit's alarm) into Effects. Settings will set Master, Effects,
  Ambient and UI; World and Suit belong to the hearing.
- **Silence in vacuum, by the listener's air.** While this player's placed hero has its body in a cell
  without air, the World bus's low-pass falls to a few hundred hertz as the bus fades out, over
  0.6 s: a silence that swallows everything, own steps too. Only the Suit bus is left —
  what the hands touch (a crate, a tank, a caught piece of debris, an item picked up) and the
  suit's alarm, heard through the suit. A breathing loop was tried and dropped: it read as a hiss. A
  source in vacuum is not silenced on its own: only where the listener stands decides.
- **Data, not code:** each cue's files and volume live in `Sound.cs`; one-shots are randomizers over
  their variants with a little pitch spread, loops OGG looped on import. Files are normalised before
  they are committed, so the mix is set by each cue's volume, and a file is swapped by changing data.
- **CC0 only**, because the repository is public: a file must be free to redistribute. Every file is
  listed in `game/audio/CREDITS.md`, and a test fails on one missing or under another licence. The
  first files are Kenney's CC0 packs, recorded foley from Freesound's CC0 sounds and placeholders
  generated for the game. Doors and valves sound where they stand, for everyone near.
- **The ship's hum comes from its vents,** heard where they are, while oxygen is behind them — not from
  the air, which makes no sound of its own. The Ambient bus waits for a first ambience.
- Players are children of the feature that plays them, never held in a static, so a scene reload frees
  them; the `AudioServer` survives it, so the hearing resets its buses when it starts.

## Consequences

- The wreck starts without air round the pod: a new world opens silent until the room fills.
- A low pressing drone in vacuum may come later; the silence is the first answer.
- Music, sliders, reverb, occlusion, other heroes' hands and most machines wait for later passes.
- CC-BY sounds wait for a credits screen; Sonniss's packs, which forbid redistribution, stay out.
