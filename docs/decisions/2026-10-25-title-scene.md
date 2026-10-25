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
  by to the left, behind the plate: the stars, shards of metal, now and then an astronaut gone limp; the hero's head lifts
  to one going by. Below, the rim of a vast dark planet, the figure drawn in its cold light; far off, the ship he
  came from, broken in two, a window or two still lit. Stars glide, shared between the pixels they lie across, and the
  gradients carry a little noise, so dark ones show no bands. Tried first and dropped: the game's own wreck run as a world behind the title — it
  read as the game paused, not as a scene, and needed the HUD hidden and the world muted.
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
