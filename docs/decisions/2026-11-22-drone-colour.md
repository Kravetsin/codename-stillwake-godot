# The drone burns in its player's colour; eight suit colours, none amber

- Status: accepted; supersedes in part [drone-threads](2026-11-21-drone-threads.md) (all of it in ember),
  [thread-icons](2026-11-21-thread-icons.md) (the threads in ember), [drone-windows](2026-11-21-drone-windows.md) (the
  corners in ember) and
  [player-colour-mark](2026-11-01-player-colour-mark.md) (the six colours)
- Date: 2026-11-22

## Context

Every drone was the same ember, so friends' drones looked alike. One colour alone also vanished on some grounds: the user
liked two, for where one merges the other is seen. An amber player would then have had only one.

## Decision

- **The salts** (`Salts`, game/shared): the pick «С облачком» (`tools/pick-kit/pages/drone-salts.js`, candidate `flame`,
  [page](https://claude.ai/artifact/8tfQXJRsDhJK4ZogJXeZJs)), after «Соли в пламени» on
  [the first page](https://claude.ai/artifact/CfTKvdVbNuQ1hEjGYVuxY2). The coal, its cracks and eye stay ember; what leaves
  it burns through a pale flame into the player's colour, with a faint cloud of that colour round it. The one rule for the
  drone, its windows' corners, its icons, the gauge's dots and what its scan and visions draw.
- **The colour is the suit's**, the bracelet's one, so every player sees each drone in its own player's colour.
- **The palette** is the pick's eight (`Suits.All`, `Content.Crew.Colours`): lime, cyan, blue, purple, sky, violet, pink,
  green. None red, which is danger's; none amber, which is lost in the ember.
- **Saves:** a colour's index is saved, so format 72 moves each of the old six to the same colour among the eight or the
  nearest, amber to lime.
- **An alarm** turns all of it red, the cloud gone: the same red for every colour.

## Rejected

- **Another sign of low air** in place of the red pulse: a page of candidates was made and all were turned down.

## Deliberately not handled

- The red pulse of low air reads weakly on some colours; the HUD shows the air.
- A form's still picture on the board and over the head in a scan (`FormPictures`) stays ember: one picture a size.
- With more than eight players two share a colour, as Players hands them out.
