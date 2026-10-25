# The title is a drawn scene of despair

- Status: accepted; amends [0031](0031-worlds-the-title-and-migrations.md)'s title
- Date: 2026-10-25

## Context

0031's title was a placeholder: a column of boxed buttons over drawn stars and grey rectangles tumbling past, the update
a small button at its foot, Worlds a list of grey rows with a name field under them. Players missed the update, found
New world unclear, and the backdrop said nothing of the game. The title's music (0035) is a lonely one; the screen did
not match it.

## Decision

- **The title is a scene drawn for it**, none of it the game's world (`TitleBackdrop`, its own screen layer under the words):
  a torn chunk of hull drawn pixel by pixel (`PlateArt`: skin, frame, tears, hanging wires, a broken rib) drifting right
  through the dark and turning slowly; its hero hunched on its end, legs over space, head hanging (`Pose.Sit`), tethered
  to it, a red emergency lamp blinking beside him, the only warm light there is. All else comes from the right and goes
  by to the left, behind the plate: the stars, shards of metal, now and then an astronaut gone limp. The plate and its hero
  are drawn half as large again as the rest: they are the scene.
- **The hero reads as despair**: hunched deep, arms hanging limp between the knees, legs barely stirring, head hung low;
  a heavy sigh every quarter minute lifts him a little and he sinks back. His head moves only slowly (eased over seconds),
  lifting a little to an astronaut going by. A first version turned it toward each one within a tenth of a second and
  swung the legs: it read as fidgeting.
- **The stars are the game's**: its star tile, a screen pixel a star, streaming as aboard. Stars drawn in the scene's
  doubled pixels and shared between two of them flickered.
- **The shade blends over the screen** (`hint_screen_texture`) with its noise on the final colour: noise in an alpha
  over near-black moved the colour less than a step, and its bands showed.
- Tried and dropped: the game's own wreck run as a world behind the title (it read as the game paused, needed the HUD
  hidden and the world muted); a far freighter torn in two, the raider that did it firing into it, and a lit planet below
  — drawn in code they read as cheap beside the hero, and crowded him.
- **The scene first.** For a moment only the scene and its music; then the name, then the choices fade in. Any key or
  click brings them at once.
- **Choices are words, not boxes**, over the screen darkened from the left: Continue (with its world, play time and
  when), Worlds, Play together, Settings (the settings kept on this machine, shared with the game menu), Quit. A
  choice slides its panel out beside it; choosing again or Esc closes it.
- **The update is a notice**, top right, warm and breathing, with its Update button, shown from the first moment, not
  faded in with the rest; its progress shows in it.
- **Worlds are cards.** New world comes first and opens into its name, showing the name it gets if left empty. Each
  world's card holds a picture of its ship (`shot.png` in its folder), when it was played, how long, and why it cannot
  be played if so; the picked card holds Play and Delete, Delete asked once more inside the card.
- **The picture is drawn apart.** A small viewport shares the world's canvas, so no HUD, menu or panel is in it and
  nothing flickers; it is taken on every save and whenever the Esc menu opens, the way out of a world.

## Consequences

- The title runs no world: a few hundred drawn pixels and some sprites, nothing saved, no HUD to hide.
- A world saved before pictures were kept shows a blank card until it is played and saved or the menu opened.
- Not done: the player's own ship behind the title (a full load only to look), a scene that changes with progress, the
  hero played on the title, keyboard navigation of the choices.
