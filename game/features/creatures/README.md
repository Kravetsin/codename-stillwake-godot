# creatures (client)

## Files

| File | Purpose |
| --- | --- |
| `CreatureBody.cs` | One body, posed. |
| `logic/CreatureGait.cs` | Its legs' pace. |
| `logic/CreatureMotion.cs` | Reactions to the sim, blows. |
| `CreatureParts.cs` | Parts in ink, read on load. |
| `logic/CreaturePose.cs` | Each rig node this frame, its bend. |
| `logic/CreatureVariety.cs` | One differs from the next. |
| `CreaturesView.cs` | Bodies between ticks. |
| `logic/Creatures.En.cs` | Its player text |
| `logic/Creatures.Ru.cs` | Russian |
| `logic/OwnShots.cs` | Own shots' hits, heard. |
| `PosedSprite.cs` | A rig-drawn creature, placed. |
| `InkRig.cs` | A body in ink, sprites by its rig. |
| `CreatureFx.cs` | A blow's nearest hero, sparks. |
| `IstukanBodies.cs` | The Istukan (warden): sprite, lamp, streak, fall. |
| `logic/IstukanArt.cs` | Its pose, its anchors, its lights. |
| `logic/IstukanMotion.cs` | Its moves, off the sim. |
| `logic/IstukanFate.cs` | Why one is gone. |
| `logic/IstukanRig.cs` | Its parts in a pose: tested. |
| `LurkerBodies.cs` | The cave lurkers: rigs, lights, hits. |
| `LurkerFxView.cs` | A capback's gas, glob and puddle, heard. |
| `logic/LurkerFx.cs` | Their sprites and lights: tested. |
| `logic/GlobFlight.cs` | A glob's lob: tested. |
| `logic/LurkerArt.cs` | Its pose and colours. |
| `logic/LurkerMotion.cs` | How it moves, off the sim. |
| `logic/LurkerRig.cs` | Its parts in a pose: tested. |
| `BreakerBodies.cs` | Breakers out of their nests. |
| `BreakerSprite.cs` | One breaker's pieces. |
| `logic/BreakerKind.cs` | Its design and moves. |
| `logic/BreakerMotion.cs` | Its flight and fight, off the sim. |
| `logic/BreakerPack.cs` | A nest's pair and the rest. |
| `logic/BreakerRig.cs` | Its pieces on joints: tested. |
| `logic/KnotMoves.cs` | «Клубок». |
| `WorkingNests.cs` | A pair at work on each nest. |
| `TickBodies.cs` | Ticks (the sentry). |
| `ShotsView.cs` | Shots but a guest's own, hits, bursts, arcs. |
| `logic/TickArt.cs` | Its pose, anchors, leg sums. |
| `logic/TickMotion.cs` | Its moves, off the sim. |
| `logic/TickRig.cs` | Its parts in a pose: tested. |
| `DocksView.cs` | A vessel's capsules and stands. |
| `logic/DockWake.cs` | A dock's waking, as shares: tested. |
| `EscortBodies.cs` | The escort A1: lamp, gauss traces. |
| `logic/EscortRig.cs` | Its parts in a pose: tested. |
| `logic/Footfalls.cs` | A guard's steps heard: tested. |

The creatures (0102). Drawn only: the sim (`sim/Creatures`)
spawns, moves, knocks, kills them and drops their loot. Striking one is `features/crew`'s (`ToolRays`,
`Aiming.StrikesOf`), sending `Strike`.

- **Reads:** `CreaturesModule.Creatures`, `Hits`, `Docks`; `Client.InFlight` (this player's own strikes, as sent);
  `Ui.Heroes`; `Ui.Bits` (a friend's drill); `Session.Progress`.

## Rules

- **Only what the camera sees**: bodies within the view and a margin, pooled. One gone while drawn was killed,
  or a guard calmed.
- **Drawn between ticks**: kept at the last two ticks, `Progress` of the way, a tick behind the sim. Upright (0106) but for
  `CreatureMotion.Tilt`. Steps eased (`StepEase`). Faces the way it moves; still, a near hero, else the way it last moved.
- **The body** (`CreatureBody`, 0102): the design its species' `Look` names, in `InkDesigns`: finished pictures, with a white
  copy for the flash (`CreatureParts`, creatures-in-ink). A sprite a part in rig order, turned about its
  pivot (`CreaturePose`), lit as the world (`Lit`), on `Layers.Creatures` under the heroes. Its eye
  open, half shut, shut or wide, its pupil on a near hero. Idle with no hero near it grazes; with one near it notices.
- **Bending parts** (creatures-bend): a part with a `Bend` is one picture bent
  in a band (`shared/BentPicture`): a knee folds back as its foot lifts, a hock (`Hind`, a sixleg's rear four) forward;
  a neck and tail bend for their turn. A leg fades in inside its body, there at any swing.
- **Glowing parts give light**: added over it unlit, their spots lights in their colour (`Lights`, the light
  view's `Creatures`).
- **Each its own** (`CreatureVariety`, 0102): from its id and the surface's seed, hashed, the same on every peer. Size,
  coat, morphs and markings vary, drawn only.
- **Its legs** (`CreatureGait`, 0102) go by the ground it moved on its last tick, eased by time, never a frame's step,
  capped by `CreatureLooks.Runs`. Fast, a gallop. Tucked in the air.
- **A blow** (`CreatureMotion`, 0102): an own strike reacts as it is sent, another as the sim's `Damage` rises (an own one not
  twice). A white flash, a squash, a flinch the way it is knocked, a shake. A drill's hit is its share of the axe's (`Impacts.ShareOf`).
- **A ram's tell** (0103), read off `WindUp` and `Charge`: it faces the hero and crouches, trembling; it snorts
  (`Sound.Snort`). Charging it gallops.
- **Jumps**: a crouch, a spring; landing, a squash as hard as the fall.
- **Seen and heard by what it is** (0093, 0094): `HitLooks.Creatures` names each species' `Impact` (`Flesh`, `CoolFlesh`;
  no blood), thrown from the side facing the hero; `Sound.Creatures` its hit, thump and death, an own on Suit
  (`Sound.Felt`), a friend's on World.
- **Death** (kills-crumble): a glint, shock ring and death sound (`Sound.CreatureDeath`); held white `KillLooks.PopMs`, then it
  stands as its pixels, crumbling top down (`shared/PixelBursts`), loot sprayed (`features/items`).
- **The tick**: `TickRig` places its pictures from its pose (`TickArt`), lit; eye, cracks and a blow's white over it unlit. `TickMotion` reads the sim: `Clamped` grips,
  else legs folded, nose along its flight. Eye teal, yellow `Alerted` or homing, red crouched, cyan aiming. Winding up: red arcs. Flying: a burner.
  `CutLeft`: it saws at the line; `LastCut`: sparks, a recoil. Killed: cracks, crumbling. Heard on Suit (`Sound.Ticks`).
- **Breakers** (breakers-look): a nest's pair at work, the rest out from under its piece (`BreakerPack`).
- **The Istukan** (spire-guards): drawn as the tick is. Asleep, lamp dark; woken
  teal, waiting yellow, red from its crouch on; flying fists first, a thin streak behind; stuck, shivering. Gone: `IstukanFate`. Heard on World. `--ground=spire-guards`.
- **Guards and lurkers in ink** (guards-in-ink, cave-hostiles): `TickRig`, `IstukanRig`, `LurkerRig` place `InkRig`s.
  A capback dims noticing, flares crouched, puffs venting; a snail shuts. Killed, they sag or crack. Its gas, glob and puddle are
  `LurkerFx` (lurker-attacks); the puddle waits for its glob.
- **Vessel guards** (derelict-guards): `DocksView` lists the `Docks` as the world opens: a capsule G1 with the back-wall objects,
  a stand S1 before the machines (over furniture), under the unseen fog. A capsule blinks its amber pip; waking (`DockWake`), its
  lamp flashes, its iris opens, the spider (`TickBodies`) unfolds and drops; a stand lowers its escort (`StandWake`). A wreck's:
  awake, docks open, unheard (wreck-guards). The escort (`EscortRig`): a red lamp, its gun to its aim, a cyan gauss trace.
  Heard on World (`Footfalls`, `Sound.Escorts`); a claw or pounce lands the sentry's punch. `--vessel=<class> --dock[=escort]`, `--wrecked`.
- **A surface left or a world loaded**: every body put away at once, no puff.

## Dev

`-- --planet --creatures[=species|look]` lets six creatures out by the hero (`dev/CreaturesDemo`,
`Release`); `--creatures-trace` logs strides and looks; `--creatures-herd` ten in a row; without `--manual` `CreatureHands` hunts them with the axe, then the drill. `--creatures-drill` holds the
drill on one loper; `--strip --creatures --creatures-course` puts a loper by the strip's walls and shaft for its leaps.

## Deliberately not handled

- **Creatures overlap** one another and the heroes: a struck herd bunches up.
- **A friend's strike shows a round trip late,** and its tool is guessed: a running drill's bit by it, else the axe.
- **No outline** under the rays.
- **One design and one set of sounds per species;** no calls or cries. Individuals differ drawn only: a big one is struck,
  jumps and fits as a small one.
- **A tick saws at its claw,** not where the hook held: the hook point is the harpoon's.
