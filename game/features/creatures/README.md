# creatures (client)

## Files

| File | Purpose |
| --- | --- |
| `CreatureBody.cs` | One creature's body (0102) in the design its species names: its rig's parts, posed. |
| `logic/CreatureGait.cs` | How fast a creature's legs go (0102). |
| `logic/CreatureMotion.cs` | How one creature's body reacts (0102) to what the sim decided and to blows. |
| `CreatureParts.cs` | The creatures' parts as pictures (0102): each index map of `art/creatures` recoloured once, at first use, by a palette … |
| `logic/CreaturePose.cs` | What each node of a creature's rig does this frame (0102), the loper page's pose ported (its `pose()`, the user's … |
| `logic/CreatureVariety.cs` | How one creature differs from the next of its kind (0102). |
| `CreaturesView.cs` | The creatures: each drawn between ticks where the sim put it, facing the way it goes, its … |
| `logic/Creatures.En.cs` | Its player text (`En`) |
| `logic/OwnShots.cs` | The creatures this player's own shots hit, heard as its blows. |
| `PlaceholderBodies.cs` | A plain box for a look with no art yet. |
| `TickBodies.cs` | The tick (the sentry): sprite, lamp, arcs, burner, sparks, death. |
| `ShotsView.cs` | Every peer's shots but a guest's own, drawn a tick behind; heard fired; their hits; the sentry's arcs. |
| `logic/TickArt.cs` | The tick's sprite drawn from its pose (the page's `drawTick`): plain C#, tested. |
| `logic/TickMotion.cs` | How a tick moves and its moments, read off the sim. |

The creatures ([0102](../../../docs/decisions/0102-creatures-live-in-the-sim.md)). Drawn only: the sim (`sim/Creatures`)
spawns, moves, knocks and kills them and drops what they give. Striking one is the hero's hands (`features/crew`): the
axe's blade and the drill's bit are tested against each creature's body as against a wreck's box
(`Aiming.StruckCreatures`, `Aiming.TouchedCreatures`, `Aiming.StrikesOf`), and send `Strike`.

- **Reads:** `CreaturesModule.Creatures`, `Hits`; `Client.InFlight` (this player's own strikes, the frame they are sent);
  `Ui.Heroes` (whom a creature watches and flinches from); `Ui.Bits` (a friend's running drill); `Session.Progress`.

## Rules

- **Only what the camera sees** (no ADR): a body only for a creature within the view and a margin round it, from a pool
  that grows to the most ever on screen and never frees. A creature gone while drawn was killed: nothing despawns in view.
- **Drawn between ticks** (0102): kept at the tick before and the last, drawn `Progress` of the way, a tick behind the
  sim. Upright where the sim stands it, no lean to the ground (0106), only the flinch's and flight's tilt
  (`CreatureMotion.Tilt`). Whole steps eased by `StepEase` over `CreatureLooks.StepMs`, never slower
  than it moves across, read from its positions so every peer draws it alike (0106). Faces the way it moves; still,
  toward a near hero, else the way it last moved.
- **The body** (`CreatureBody`, 0102): the design its species' `Look` names. Parts in `art/creatures/<design>/` are index maps
  (R the ramp, G the tone) recoloured once at first use by the planet's palette (`CreatureParts`, `CreatureLooks.PaletteOf`),
  with a white copy for the flash and glow maps a tone up; painted by `tools/planet-art/creatures`, rig, pose, eye and
  palettes generated into `Content/logic/CreatureArt.cs`. A sprite a part in rig order, hung from its parent and turned about its
  pivot (`CreaturePose`), at screen resolution, lit as the world (`Lit`), on `Layers.Creatures` under the heroes. Its eye
  open, half shut, shut or wide, its pupil on a near hero. Idle with no hero near it grazes; with one near it notices.
- **Glowing parts give light** (0102): added over it unlit, their spots lights in their colour (`Lights`, the light
  view's `Creatures`).
- **Each its own** (`CreatureVariety`, `CreatureLooks.Varieties`, 0102): from its id and the surface's seed by an integer
  hash, the same on every peer, never the world's random stream. Size, coat shade and tone, morphs and markings vary,
  drawn only, scaled about its feet; the sim's box, hit points and speed are the species'.
- **Its legs** (`CreatureGait`, 0102) go by the ground the sim moved it on its last tick, eased by time, never a frame's
  step: `MostStrides · tanh(v / (MostStrides · Stride))` strides a second, capped by `CreatureLooks.Runs`, so any frame rate
  draws alike. Fast, the pose blends into a gallop. Tucked in the air.
- **A blow** (`CreatureMotion`, 0102): an own strike reacts as it is sent, another as the sim's `Damage` rises
  (an own one not twice). A white flash, a squash springing back past round, a flinch
  the way it is knocked, wide eye and pinned ears, a shake. A drill's hit is its share of the
  axe's (`Impacts.ShareOf`). Fleeing it leans forward and stretches long.
- **A ram's tell** (0103), read off the sim's `WindUp` and `Charge`: winding up it faces the hero however far and crouches
  (`CreatureMotion.Bracing`), nose down, ears pinned, trembling, hind feet scraping dust; it snorts as it begins (a puff
  and `Sound.Snort`, a placeholder). Charging it gallops, throwing dust. Its hit on a hero is `features/crew`'s to show.
- **Jumps** (no ADR), read off the sim (a rise from the ground faster than any blow's hop): a crouch, then a spring tall
  and thin, ears laid back; on landing a squash as hard as the fall was fast.
- **Seen and heard by what it is** (0093, 0094): `HitLooks.Creatures` names each species' `Impact` (`Flesh`, `CoolFlesh`;
  no blood), thrown from the side facing the hero, its share for a drill; `Sound.Creatures` its hit, thump and death,
  an own (struck or shot) on Suit heard far (`Sound.Felt`), a friend's on World.
- **Death** (2026-10-29-kills-crumble): a glint, shock ring and death sound (`Sound.CreatureDeath`); held white `KillLooks.PopMs`, then it
  stands as its pixels, crumbling top down (`shared/PixelBursts`), hit and puff thrown, loot sprayed (`features/items`).
- **The tick** (the sentry page's «Клещ»): its sprite drawn each frame from its pose (`TickArt`), lit; eye, cracks and a blow's
  white over it unlit. `TickMotion` reads the sim: `Clamped` grips (legs down, magnets aglow, a bounce), else legs folded, nose
  along its flight; X moving along its wreck while clamped steps its legs. Eye teal, yellow `Alerted` or flying home, red from
  its crouch on, lighting the wreck. Winding up: trembling, red arcs between its legs. Aiming an arc: eye cyan, flickering,
  held once aimed, its charge heard. Flying: an amber burner, sparks and smoke.
  Struck: flash, shake, fine sparks. `CutLeft`: it faces the line (its reel's hero), rears, saws with its claw and mandible,
  sparks thickening; `LastCut`: sparks and a whip of light down the line, a recoil. Killed (its wreck gone too): cracks for the
  hold, then a burst of sparks, its pixels left to crumble. A pounce cut short hit a hero: sparks. Heard on Suit (`Sound.Ticks`); a friend's blow on World.
- **A surface left or a world loaded** (no ADR): every body put away at once, no puff.

## Dev

`-- --planet --creatures[=species|look]` lets six creatures out either side of the hero (`dev/CreaturesDemo`, the sim's
`Release`); `--ground=band1 --creatures=fluff --manual` in a cave. `--creatures-trace` logs strides and looks; `--creatures-herd` ten
of a kind in a row; without `--manual` `dev/demos/CreatureHands` hunts them with the axe, then the drill. `--creatures-drill` holds the
drill on one loper; `--strip --creatures --creatures-course` puts a loper by the strip's walls and shaft for its leaps.

## Deliberately not handled

- **Creatures overlap** one another and the heroes, as the sim lets them: a struck herd bunches into one lump.
- **A friend's strike shows a round trip late,** and its tool is guessed: a running drill's bit by it, else the axe.
- **No outline or prompt** for a creature under the blade: the hit is the answer.
- **One design and one set of sounds per species;** no calls or cries. Individuals differ drawn only: a big one is struck,
  jumps and fits where a small one does.
- **A tick saws at its claw,** not where the hook held: the hook point is the harpoon's. No sawing sound yet: none picked.
