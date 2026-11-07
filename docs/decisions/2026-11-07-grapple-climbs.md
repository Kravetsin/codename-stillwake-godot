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

## Decision

- **A click while hanging fires again** at once, the cooldown not checked but restarted, as every shot does. The hero stays held
  where it hangs while the new rod flies (`PullTo` kept through `Flying`).
- **A grip from a hang pulls on with no click**, toward `Harpoon.PullStop` short of the new hook. A miss lets the hero fall and
  the next shot waits for the cooldown; a piece hooked from a hang lets it fall too, the rod left in the piece to reel. A jump or a
  movement key while the new rod flies lets go of the old hook, the rod flying on as a shot from the ground.
- **The line is footing.** `Hero.Pulled` starts the coyote's steps afresh and keeps a Space pressed on the line in the jump
  buffer (0089), so the step after the harpoon lets go jumps as from the floor: a hero hung by a ledge's lip jumps onto it.
- **Others hear it** (0065): a rod heard stuck and then flying is a shot, its sound, puff and empty gun drawn.
- A hop is at most the range less `PullStop`, ~39 cells, in ~2.5 s: 800 cells in about 55 s up a shaft (`GrappleClimbTests`).
- **Nothing in the sim:** the pull stays the controller's (0044), heroes client-authoritative (0002). `PullSpeed`, the range and the
  cooldown are unchanged; the pull is also the hero's way round the ship.

## Consequences

- **The route must offer a hook within the range.** Straight up a shaft the rod flies out and misses: the player aims at the
  wall high up. A hall wider than 40 cells is crossed along its walls; a miss over lava can kill.
- Ladders no longer hold their place as the only quick way up: they stay the shared, keyless route of a mine heroes come back to.
- Clicks while the hero is pulled are dropped, as before: a click chains once it hangs. A shot fired falling still falls.
- While the new rod flies the old hook is not asked whether it still holds: a wall taken down under it holds the hero for the
  flight's half second. In space the chain stops at the tether.
- Dev: `--climb` beams the hero to the bottom of the first planet's deepest chasm, harpoon in hand, the drill to dig deeper.
