# 0033 — First sound

- Status: accepted
- Date: 2026-10-16

## Context

The game was silent: a breach, a low suit, a friend working beside you went unheard. A first pass
needs a few cues (steps, landing, the suit's alarm, the screwdriver, venting, the vents' hum, the
furnace, doors and valves, button clicks, what the hands do) and a shape later passes, settings and
better recordings fit into without rework.

## Decision

- **No sound in the sim.** Sound is presentation: nothing under `sim/` or `net/` changes, nothing goes
  over the network. Each peer plays what it sees, hooked where the client already draws the event
  (the figure's stride and landing, the suit gauge, the plan's sparks, the venting streams, the fire,
  the drawing of doors and valves).
- **Buses by name are the settings contract** (`game/default_bus_layout.tres`, named in
  `game/Content/Sound.cs`): UI, Ambient and Effects into Master; World (every hero's steps, machines,
  air, doors) and Suit (the action click, the suit's alarm) into Effects. Settings will set Master,
  Effects, Ambient and UI; World and Suit belong to the hearing.
- **Silence while floating in space.** While this player's hero floats outside the hull, the World
  bus's low-pass falls to a few hundred hertz as the bus fades out, over 0.6 s; only Suit is left.
  Silence by the listener's air was tried first: realistic, but a room without air fell silent with
  nothing on screen to say why, unless the air overlay was open. Floating is always seen. A
  breathing loop was tried too and dropped: it read as a hiss.
- **One soft click for actions** (a crate opened, a canister loaded, debris caught, an item picked
  up), as Terraria does: it signals that the action happened rather than imitating the object, so it
  never sounds like a tin can and is heard in space too. Doors and valves keep sounds of their own,
  heard where they stand by everyone near.
- **Things that run make their own sound where they are:** the ship's hum comes from vents with
  oxygen behind them, not from the air; each burning furnace crackles. The Ambient bus waits for a
  first ambience.
- **Each sound carries as far as it should:** full volume within a few cells, fading to silence at
  its own distance, so a hum fills the room it is in and is gone two rooms away, while a door or a
  breach is heard across the ship.
- **Data, not code:** each cue's files and volume live in `Sound.cs`; one-shots are randomizers over
  their variants with a little pitch spread, loops OGG looped on import. Files are normalised before
  they are committed, so the mix is set by each cue's volume, and a file is swapped by changing data.
- **CC0 only**, because the repository is public: a file must be free to redistribute. Every file is
  listed in `game/audio/CREDITS.md`, and a test fails on one missing or under another licence. The
  files are Kenney's CC0 packs, Freesound's CC0 recordings, chosen by ear from candidates, and
  placeholders generated for the game.
- Players are children of the feature that plays them, never held in a static, so a scene reload frees
  them; the `AudioServer` survives it, so the hearing resets its buses when it starts.

## Consequences

- A room without air sounds like any other; the air overlay and the suit gauge tell of it.
- A low pressing drone in space may come later; the silence is the first answer.
- Music, sliders, reverb, occlusion, other heroes' hands and most machines wait for later passes.
- CC-BY sounds wait for a credits screen; Sonniss's packs, which forbid redistribution, stay out.
