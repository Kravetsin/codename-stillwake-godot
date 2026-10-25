# The title is a scene in the wreck

- Status: accepted; amends [0031](0031-worlds-the-title-and-migrations.md)'s title
- Date: 2026-10-25

## Context

0031's title was a placeholder: a column of boxed buttons over drawn stars and grey rectangles tumbling past, the update
a small button at its foot, Worlds a list of grey rows with a name field under them. Players missed the update, found
New world unclear, and the backdrop said nothing of the game. The title's music (0035) is a lonely one; the screen did
not match it.

## Decision

- **The title shows the game itself.** With no arguments the wreck a new world starts in runs behind the title: a world
  made like any other, its views drawing it, never saved and never listed (`Main.Title`). Its hero sits on the torn
  deck's edge, legs over space, looking out (`Pose.Sit`); debris in the game's own art and now and then another
  astronaut drift past from where it looks, left to right, as a ship always moving would see them, near ones before
  the wreck, far ones behind it, dimmer; the camera wanders slowly. The HUD's screen layers are hidden while it shows;
  the World bus is muted, the music alone heard. Left the moment the title closes, whichever way.
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

- The title costs a running world: the small wreck, a few ms a frame, nothing saved.
- Every way the title closes calls `LeaveScene`, giving the HUD back the layers it hid and the camera its zoom.
- A world saved before pictures were kept shows a blank card until it is played and saved or the menu opened.
- Not done: the last played world behind the title (a full load only to look), a scene that changes with progress, the
  hero played on the title, keyboard navigation of the choices.
