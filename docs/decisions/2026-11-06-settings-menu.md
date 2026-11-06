# The Esc menu on the tablet, Settings in categories

- Status: accepted; changes the game menu of [0031](0031-worlds-the-title-and-migrations.md) and the Controls page of [key-binds](2026-11-04-key-binds.md)
- Date: 2026-11-06

## Context

The playtest of 2026-11-06 asked for volume settings, but not as more buttons: the Esc menu already had ten, two of them
(Handbook, Tablet) opening what the tablet's edge keys open, and its settings were one long column with the keys on a page of
their own. There was no way to set the volume.

## Decision

- **The first page is short:** Resume, Save, Settings, Save & exit to title, Save & quit (a guest: Leave to title, Quit), the
  host's code and Update over them when they apply. The Handbook, the Tablet and Controls have no line: the edge keys open the
  first two, Settings holds the keys.
- **On the tablet.** The menu is the rugged tablet's screen (`shared/Tablet`, its own frame, plate, stylus and glass), raised
  from the screen's foot as the Handbook is, over the darkened world. The user picked the "tablet window" candidate of five
  (Slate, Phosphor, Tablet window, Amber console, Hologram), with the game's own tablet art in place of the drawn bezel.
- **Settings in categories,** the tablet's tabs along the top, the first lit as it opens, a row a setting below:
  Sound (Master, Effects, Music), Graphics (the screen, heat shimmer), Game (language, slot size, Open logs folder), Controls
  (the keys), and Hero in a world only (the suit's colour, Edit look), last so the title's tabs stand as a world's. The title's
  Settings shows the same view in its panel; its Controls choice is gone.
- **Volume:** a slider of 11 steps for each of Master, Effects (World, Suit and Helmet go through it) and Music. Step 10 is
  today's mix (0 dB) and the default; each step down is `Sound.VolumeStepDb` (3 dB) quieter, so even to the ear; 0 mutes the
  bus. Set as it moves, kept at each step in `user://settings.cfg` (`[sound]`, a missing key reading as 10), applied at the
  start before the title's music.
- **Esc** walks Settings → the first page → closed; a key waiting to be bound still takes it first.

## Deliberately not handled

- Sliders for UI (one click, quieted with Master), Ambient (nothing plays on it) and World, Suit, Helmet (the hearing's).
- Louder than today's mix; remembering the category last opened; a Co-op category (no co-op setting exists).
- The title's panel is not the tablet: there is no hero holding one there.

## Consequences

- A player who never found J or T loses the menu's reminder of them; the edge keys are always on screen.
- Look is a level deeper, under Hero; a first run opens the look editor anyway.
