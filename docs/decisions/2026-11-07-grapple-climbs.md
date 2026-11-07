# The grapple climbs

- Status: accepted; supersedes [0044](0044-the-harpoon-reels-a-piece-to-a-point.md) on a click while hanging and on ladders keeping their place
- Date: 2026-11-07

## Context

In the playtest of 2026-11-07 the user dug down to the magnetite of the planet's amber band, about 800 cells under the surface,
and spent more than 30 real minutes getting back up. A ladder that long is 400 pieces, 800 scrap. The harpoon's grapple could pull
the hero 39 cells, but a hang ended only by letting go: the line's own step ate the jump that let go and left the hero no coyote
time, so it dropped, and the next shot waited out the 5 s cooldown.

Four ways back were weighed, for 800 cells:

| | Time | The player's cost | The deep's danger | Work |
| --- | --- | --- | --- | --- |
| The grapple reworked | ~55 s | none: the harpoon comes early (0068) | kept; the climb is a skill | client, ~100 lines |
| A recall beacon or teleport | ~3 s | a device and its charge | the way back is gone | a sim module, an item, art, sound |
| A rope line the player places | ~60 s, anchors placed going down | materials, planning | kept, for one who planned | Grid and Building, art |
| A lift or the pod's winch | ~40 s, once built | power, a machine, 800 cells of building | kept | a module; moving floors under client-run heroes |

The user picked the grapple: it saves the player who did not prepare, needs nothing new on the wire, and the deep stays a trip.
Played, the first version made the harpoon itself the climber; the user asked for a tool of its own instead, since players know the
harpoon as the 5 s gun that reels debris, and for a line that a movement key does not drop.

## Decision

- **The grappling gun** (`Content.Grapple`, item `grapple`) is the harpoon's machine (`HarpoonShot.Grapple()`) made to climb:
  its rod grips only structure, never a piece; **every grip pulls at once**, no click; it pulls at 30 cells a second, half again
  the harpoon's, and reloads in 1 s, so a miss costs a moment. Range, rod speeds and `PullStop` are the harpoon's. Made at the
  workbench from ship parts, pricier than the harpoon: 2 tubes, 2 gears, a coil, 8 wire.
- **Its look**, picked by the user from five, is «Кошка»: the harpoon's plank stock under a fat spring tube with hoops and a
  bell mouth, a gear pair and crank driving the spool, firing a three-prong grapnel (`tools/anime-art/subjects/hero.js`).
- **Hanging, a click fires again** once the gun is reloaded, the hero held where it hangs while the new rod flies (`PullTo` kept
  through `Flying`). The harpoon keeps the same tricks but waits out its 5 s: it climbs, slowly, and makes the grapple worth making.
- **A grip from a hang pulls on with no click**, toward `PullStop` short of the new hook. A miss lets the hero fall; a piece the
  harpoon hooks from a hang lets it fall too, the rod left in the piece to reel. A jump while the new rod flies lets go of the old
  hook, the rod flying on as a shot from the ground.
- **Only a jump lets go** (`HarpoonShot.LetsGoBy`): A and D on the line do nothing, so a jump off it can be steered. Played, the
  user asked for a way off without a jump: **a right click lets go** of a gun in hand at any time the line holds the hero, mid-pull too,
  the hero falling on with the pull's speed (`GunHold.Drops`).
- **The line is footing.** `Hero.Pulled` starts the coyote's steps afresh and keeps a Space pressed on the line in the jump
  buffer (0089), so the step after the line lets go jumps as from the floor: a hero hung by a ledge's lip jumps onto it.
- **Others see it** (0065): a rod heard stuck and then flying is a shot; a friend's gun is drawn loaded after its own cooldown.
- Up an 800-cell shaft (`GrappleClimbTests`): the grappling gun in about 40 s, a hop of ~36 cells each 1.7 s; the harpoon in about
  two minutes, a hop each reload.
- **Nothing in the sim** but the item and its recipe: the pull stays the controller's (0044), heroes client-authoritative (0002).

## Consequences

- **The route must offer a hook within the range.** Straight up a shaft the rod flies out and misses: the player aims at the
  wall high up. A hall wider than 40 cells is crossed along its walls; a miss over lava can kill.
- Ladders no longer hold their place as the only quick way up: they stay the shared, keyless route of a mine heroes come back to.
- Clicks while the hero is pulled are dropped: a click chains once it hangs. A shot fired falling still falls.
- While the new rod flies the old hook is not asked whether it still holds: a wall taken down under it holds the hero for the
  flight's half second. In space the chain stops at the tether.
- Two guns in the belt keep two cooldowns; the one put away seats its rod and reloads.
- Dev: `--climb` beams the hero to the bottom of the first planet's deepest chasm, the grappling gun in hand, the harpoon beside
  it, the drill to dig deeper.
