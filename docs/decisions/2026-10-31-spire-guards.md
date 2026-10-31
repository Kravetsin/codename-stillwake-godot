# Spire guards

- Status: accepted
- Date: 2026-10-31
- Extends [creatures-are-composed](2026-10-26-creatures-are-composed.md); the chamber of [0096](0096-the-sky-shard.md) becomes a hall

## Context

A spire's chamber was an empty room: a hero dropped in and took the shard unopposed, and nothing on the first planet offered a fight
worth choosing. A hero landing has the nailer at best, so a danger here must be dodged rather than outgunned, and it must leave a choice.

## Decision

- **Two machines of the arc makers guard each shard,** in a hall at the spire's bottom (`Ruins.Spire`, 25 characters wide, its ceiling `HallCover` rows under the ground). Melee only on the first planet, so a hero can dodge.
- **The choice:** kill them for circuits (`circuit` 2 to 3, `alloyScrap` 1 to 2 each, 24 HP), or fell the cradle at once. The hall's light dies and the living guards fall, dropping nothing.
- **A post is a rock piece in the hall's end wall** (`Growth.Post`, `Lifetime.Posted`): its creature stands by it while its relic stands and a hero is in the keep box, asleep. A kill fells the post, so dead is dead for good, as one int in `Felled`; digging out its wall fells it too. A saved list in Creatures would repeat what Building keeps, and saved guards from world start would tick with no one near.
- **`Ward`** runs asleep, waking, waiting, its pounce, stuck, waiting. It wakes on an awake hero in the arena box, a blow, or a sibling. A guard waits while a sibling crouches or flies, not while one is stuck, so one's window is the other's crouch. No hero in the arena and no blow for 5 s (`Calm`), it goes and comes back whole asleep: nothing flies home.
- **A walker's pounce flies** (`MoveRule.Flies`): no gravity, no steps, over at the first solid cell, aimed from its middle at the hero's. A lunge sets nothing, so lopers walk as they did.
- **Quiet removal** drops a guard in `Live`'s loop with no yields, never through `Dead`, which is not saved: its relic taken, its post gone, or calm.
- **Its look, the user's pick: «Истукан»,** the [guard page's](https://claude.ai/artifact/Lp99qu7tzLVfCphA1xLkT3) candidate 4, ported from the page's code (`IstukanArt`, `IstukanMotion`): a column in its niche, one block fists first in flight, blocks when it falls.
- **The fall is the client's** (`IstukanFate`). Gone with its relic taken, or its post's wall gone, it is shut down; gone with its post felled and the wall standing, killed; gone with both standing, calmed, and it simply is no longer there, with no hero in the arena to see it. Fallen, its lamp stutters out, it drops to the floor under it, lies 3 s as blocks and crumbles.
- **Its sounds, the user's picks** from the [sound page](https://claude.ai/artifact/29qL2G5VjURhUQdTtv5jMw), all on the World bus, a planet having air (`Sound.Istukans`, `game/audio/CREDITS.md`): wake Robot Awakening, alert Synthetic monster growl, wind-up a hydraulic office chair, pounce burst3, a blow on a hero PUNCH-BOXING-03, killed Computer Breaking Sound, shut down MachinePowerOff. Its landing is a hero's on alloy (`Sound.LandAlloy`) and a tool's blow on it a tool's on alloy, as the user chose; none of the page's tool blows pleased. The user also liked Big robot footstep 002 (813053) for the landing: a one-line swap after the play pass.
- **No save-format bump:** no field is removed or changes meaning, a new species adds none to an old brain, and ground regrows from the seed with edits applied by cell.

Tunables are in `sim/Content`: `Warden`'s `Pounce(WindUp 16, Speed 1800, Ticks 28, Damage 5)` (0.8 s, 36 cells a second, 1.4 s at most, 5 units) and `Ward(Wake 10, Stuck 20, Calm 100)`. All placeholders for a play pass: the nailer kills one in 4 nails, about 24 s for both against about 14 pounces and 40 units of suit; the axe needs 12 blows a guard.

## Deliberately not handled

- **A better gun, day and night, nests, other enemies, scaling by players:** later, after the first play pass. Four heroes find it easier.
- **Old saves:** a spire moved or redrawn keeps old edits by cell, so holes may sit in new walls and a felled cradle may stand anew with four more shards. Not worth a migration.
- **Same tick:** a wall a shot breaks or a meteor's fell gives guards one tick more; a strike landed in the commands of the tick the cradle falls, on a guard's last hit points, still pays. Taken together with a kill, the client hears a shut down.
- **Sniping:** a long tunnel or a lucky ledge angle shoots for loot, at 15 s an alloy cell and 12 s a guard. Sneaking under the floor fells the cradle with the guards never woken, the loot given up.
- **Walls built in the hall** stop flights: a friends' game, no guard. A blocked niche waits for a later beat.
- **Fluffs may spawn in the hall,** harmless, as in homes. Guards count toward `PerKeep` and `Max`, so a post waits for room at the cap.
- **Once in a while one guard goes twice running.**

## Consequences

- The fight has been recorded (`--ground=spire-guards`, with `--take-at` the cradle) and not yet played by the user: the numbers wait on that.
- A nest can use the same `Post` record with a piece that can be hit.
