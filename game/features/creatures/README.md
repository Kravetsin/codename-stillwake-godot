# creatures (client)

## Files

| File | Purpose |
| --- | --- |
| `CreatureBody.cs` | One creature's body (0102) in the design its species names: its rig's parts, posed. |
| `logic/CreatureGait.cs` | How fast a creature's legs go (0102). |
| `logic/CreatureMotion.cs` | How one creature's body reacts (0102) to what the sim decided and to blows. |
| `CreatureParts.cs` | The creatures' parts as pictures (0102), drawn in ink (`art/hd/creatures`): each read once and shared by every individual, … |
| `logic/CreaturePose.cs` | What each node of a creature's rig does this frame (0102), the loper page's pose ported (its `pose()`, the user's … |
| `logic/CreatureVariety.cs` | How one creature differs from the next of its kind (0102). |
| `CreaturesView.cs` | The creatures: each drawn between ticks where the sim put it, facing the way it goes, its … |
| `logic/Creatures.En.cs` | Its player text |
| `logic/Creatures.Ru.cs` | Russian |
| `logic/OwnShots.cs` | The creatures this player's own shots hit, heard as its blows. |
| `PosedSprite.cs` | A creature its rig draws: placed, turned about its middle. |
| `InkRig.cs` | A guard's body in ink: a sprite a picture, as its rig places them. |
| `CreatureFx.cs` | What the views share of a blow: the nearest hero, its sparks. |
| `IstukanBodies.cs` | The Istukan (the warden): sprite, lamp, its flight's streak, sparks, the fall. |
| `logic/IstukanArt.cs` | Its pose, its anchors, its lights. |
| `logic/IstukanMotion.cs` | How it moves and its moments, read off the sim. |
| `logic/IstukanFate.cs` | Why one is gone: calmed, killed or shut down. |
| `logic/IstukanRig.cs` | Where its parts go in a pose, in ink: plain C#, tested. |
| `LurkerBodies.cs` | The cave lurkers: rigs, lights, dust, hits. |
| `logic/LurkerArt.cs` | Its pose and colours. |
| `logic/LurkerMotion.cs` | How it moves, off the sim. |
| `logic/LurkerRig.cs` | Where its parts go in a pose: tested. |
| `TickBodies.cs` | The tick (the sentry), drawn at `TickRig.Size`: sprite, lamp, arcs, burner, sparks, death. |
| `ShotsView.cs` | Every peer's shots but a guest's own, drawn a tick behind; heard fired; their hits; the sentry's arcs. |
| `logic/TickArt.cs` | The tick's pose and anchors, the page's leg sums. |
| `logic/TickMotion.cs` | How a tick moves and its moments, read off the sim. |
| `logic/TickRig.cs` | Where its parts go in a pose, in ink: plain C#, tested. |

The creatures ([0102](../../../docs/decisions/0102-creatures-live-in-the-sim.md)). Drawn only: the sim (`sim/Creatures`)
spawns, moves, knocks and kills them and drops what they give. Striking one is the hero's hands (`features/crew`): the
fan of rays from the shoulder and the cutter's beam are tested against each creature's body and a margin
(`ToolRays`, `Aiming.StrikesOf`, tool-reach), and send `Strike`.

- **Reads:** `CreaturesModule.Creatures`, `Hits`; `Client.InFlight` (this player's own strikes, the frame they are sent);
  `Ui.Heroes` (whom a creature watches and flinches from); `Ui.Bits` (a friend's running drill); `Session.Progress`.

## Rules

- **Only what the camera sees** (no ADR): a body only for a creature within the view and a margin, from a pool that never
  frees. A creature gone while drawn was killed: nothing despawns in view, but a guard calmed with no hero in its arena.
- **Drawn between ticks** (0102): kept at the tick before and the last, drawn `Progress` of the way, a tick behind the
  sim. Upright (0106), only the flinch's and flight's tilt (`CreatureMotion.Tilt`). Whole steps eased by `StepEase` over `CreatureLooks.StepMs`, read from its positions so
  every peer draws it alike (0106). Faces the way it moves; still, toward a near hero, else the way it last moved.
- **The body** (`CreatureBody`, 0102): the design its species' `Look` names, in `InkDesigns`: finished pictures, with a white
  copy for the flash (`CreatureParts`, 2026-11-01-creatures-in-ink). A sprite a part in rig order, turned about its
  pivot (`CreaturePose`), at screen resolution, lit as the world (`Lit`), on `Layers.Creatures` under the heroes. Its eye
  open, half shut, shut or wide, its pupil on a near hero. Idle with no hero near it grazes; with one near it notices.
- **Glowing parts give light** (0102): added over it unlit, their spots lights in their colour (`Lights`, the light
  view's `Creatures`).
- **Each its own** (`CreatureVariety`, 0102): from its id and the surface's seed by an integer hash, the same on every peer. Size,
  coat, morphs and markings vary, drawn only.
- **Its legs** (`CreatureGait`, 0102) go by the ground the sim moved it on its last tick, eased by time, never a frame's
  step, capped by `CreatureLooks.Runs`, so any frame rate draws alike. Fast, a gallop. Tucked in the air.
- **A blow** (`CreatureMotion`, 0102): an own strike reacts as it is sent, another as the sim's `Damage` rises (an own one not
  twice). A white flash, a squash, a flinch the way it is knocked, a shake. A drill's hit is its share of the axe's (`Impacts.ShareOf`).
- **A ram's tell** (0103), read off `WindUp` and `Charge`: it faces the hero and crouches (`CreatureMotion.Bracing`), trembling; it snorts
  (`Sound.Snort`, a placeholder). Charging it gallops. Its hit on a hero is `features/crew`'s.
- **Jumps** (no ADR), off the sim: a crouch, a spring; landing, a squash as hard as the fall.
- **Seen and heard by what it is** (0093, 0094): `HitLooks.Creatures` names each species' `Impact` (`Flesh`, `CoolFlesh`;
  no blood), thrown from the side facing the hero, its share for a drill; `Sound.Creatures` its hit, thump and death,
  an own (struck or shot) on Suit heard far (`Sound.Felt`), a friend's on World.
- **Death** (2026-10-29-kills-crumble): a glint, shock ring and death sound (`Sound.CreatureDeath`); held white `KillLooks.PopMs`, then it
  stands as its pixels, crumbling top down (`shared/PixelBursts`), loot sprayed (`features/items`).
- **The tick**: `TickRig` places its pictures from its pose (`TickArt`), lit; eye, cracks and a blow's white over it unlit. `TickMotion` reads the sim: `Clamped` grips,
  else legs folded, nose along its flight. Eye teal, yellow `Alerted` or flying home, red from its crouch on, cyan aiming an arc. Winding up: red arcs. Flying: a burner,
  sparks, smoke. `CutLeft`: it faces the line and saws; `LastCut`: sparks down the line, a recoil. Killed: cracks, sparks, its pixels crumbling. Heard on Suit (`Sound.Ticks`).
- **The Istukan** ([spire-guards](../../../docs/decisions/2026-10-31-spire-guards.md)): drawn as the tick is. Asleep a column, lamp dark; woken
  teal, waiting yellow, red from its crouch on; flying fists first, a thin streak behind; stuck, shivering. Gone: `IstukanFate`. Heard on World (`Sound.Istukans`). `--ground=spire-guards`, with `--take-at` the cradle.
- **Guards and lurkers in ink** ([guards](../../../docs/decisions/2026-11-01-guards-in-ink.md), [lurkers](../../../docs/decisions/2026-11-07-cave-hostiles.md)): `TickRig`, `IstukanRig`, `LurkerRig` place `InkRig`s.
  A capback dims noticing, flares crouched; a snail rolls. Killed, they sag or crack. Heard as walkers.
- **A surface left or a world loaded** (no ADR): every body put away at once, no puff.

## Dev

`-- --planet --creatures[=species|look]` lets six creatures out either side of the hero (`dev/CreaturesDemo`, the sim's
`Release`); `--creatures-trace` logs strides and looks; `--creatures-herd` ten
of a kind in a row; without `--manual` `dev/demos/CreatureHands` hunts them with the axe, then the drill. `--creatures-drill` holds the
drill on one loper; `--strip --creatures --creatures-course` puts a loper by the strip's walls and shaft for its leaps.

## Deliberately not handled

- **Creatures overlap** one another and the heroes, as the sim lets them: a struck herd bunches into one lump.
- **A friend's strike shows a round trip late,** and its tool is guessed: a running drill's bit by it, else the axe.
- **No outline** under the rays.
- **One design and one set of sounds per species;** no calls or cries. Individuals differ drawn only: a big one is struck,
  jumps and fits where a small one does.
- **A tick saws at its claw,** not where the hook held: the hook point is the harpoon's.
