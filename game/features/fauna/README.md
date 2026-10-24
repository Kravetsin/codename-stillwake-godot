# fauna (client)

## Files

| File | Purpose |
| --- | --- |
| `CreatureBody.cs` | One creature's body (0102) in the design its species names, as the user picked it on the loper page: its rig's … |
| `logic/CreatureGait.cs` | How fast a creature's legs go (0102): plain C#, tested. |
| `logic/CreatureMotion.cs` | How one creature's body reacts (0102), from what the sim decided and when it was struck: plain C#, tested. |
| `CreatureParts.cs` | The creatures' parts as pictures (0102): each index map of `art/fauna` recoloured once, at first use, by a palette … |
| `logic/CreaturePose.cs` | What each node of a creature's rig does this frame (0102), the loper page's pose ported (its `pose()`, the user's … |
| `logic/CreatureVariety.cs` | How one creature differs from the next of its kind (0102), drawn only: plain C#, tested. |
| `FaunaView.cs` | The creatures: each drawn between ticks where the sim put it, facing the way it goes, its … |

The creatures ([0102](../../../docs/decisions/0102-creatures-live-in-the-sim.md)). Drawn only: the sim (`sim/Fauna`)
spawns, moves, knocks and kills them and drops what they give. Striking one is the hero's hands (`features/crew`): the
axe's blade and the drill's bit are tested against each creature's body as against a wreck's box
(`Aiming.StruckCreatures`, `Aiming.TouchedCreatures`, `Aiming.StrikesOf`), and send `Strike`.

- **Reads:** `FaunaModule.Creatures`; `Client.InFlight` (this player's own strikes, the frame they are sent);
  `Ui.Heroes` (whom a creature watches and flinches from); `Ui.Bits` (a friend's running drill); `Session.Progress`.

## Rules

- **Only what the camera sees** (no ADR): a body only for a creature within the view and a margin round it, from a pool
  that grows to the most ever on screen and never frees. A creature gone while drawn was killed: nothing despawns in view.
- **Drawn between ticks** (0102): kept at the tick before and the last, drawn `Progress` of the way, a tick behind the
  sim. Upright where the sim stands it, no lean to the ground (0106), only the flinch's and flight's tilt
  (`CreatureMotion.Tilt`). Its whole steps are eased by `game/shared/logic/StepEase.cs` over `FaunaLooks.StepMs`, never slower
  than it moves across, read from its positions so every peer draws it alike (0106). Faces the way it moves; still,
  toward a near hero, else the way it last moved.
- **The body** (`CreatureBody`, 0102): the design its species' `Look` names. Parts in `art/fauna/<design>/` are index maps
  (R the ramp, G the tone) recoloured once at first use by the planet's palette (`CreatureParts`, `FaunaLooks.PaletteOf`),
  with a white copy for the flash and glow maps a tone up; painted by `tools/planet-art/fauna`, rig, pose, eye and
  palettes generated into `Content/logic/FaunaArt.cs`. A sprite a part in rig order, hung from its parent and turned about its
  pivot (`CreaturePose`), at screen resolution, lit as the world (`Lit`), on `Layers.Creatures` under the heroes. Its eye
  open, half shut, shut or wide, its pupil on a near hero. Idle with no hero near it grazes; with one near it notices.
- **Glowing parts give light** (0102): added over it unlit, their spots lights in their colour (`Lights`, the light
  view's `Creatures`).
- **Each its own** (`CreatureVariety`, `FaunaLooks.Varieties`, 0102): from its id and the surface's seed by an integer
  hash, the same on every peer, never the world's random stream. Size, coat shade and tone, morphs and markings vary,
  drawn only, scaled about its feet; the sim's box, hit points and speed are the species'. A look's pictures are made once
  and kept.
- **Its legs** (`CreatureGait`, 0102) go by the ground the sim moved it on its last tick, eased by time, never a frame's
  step: `MostStrides · tanh(v / (MostStrides · Stride))` strides a second, capped by `FaunaLooks.Runs`, so any frame rate
  draws alike. Fast, the pose blends into a gallop (six legs keep their tripods; the fluff hops). Tucked in the air.
- **A blow** (`CreatureMotion`, 0102): this player's own strike reacts the frame it is sent, a friend's as the sim's
  `Damage` rises (an own strike's damage is not shown twice). A white flash, a squash springing back past round, a flinch
  the way it is knocked, wide eye and pinned ears, a shake; never holding its place. A drill's hit is its share of the
  axe's (`Impacts.ShareOf`). Fleeing it leans forward and stretches long.
- **A ram's tell** (0103), read off the sim's `WindUp` and `Charge`: winding up it faces the hero however far and crouches
  (`CreatureMotion.Bracing`), nose down, ears pinned, trembling, hind feet scraping dust; it snorts as it begins (a puff
  and `Sound.Snort`, a placeholder). Charging it gallops, throwing dust. Its hit on a hero is `features/crew`'s to show.
- **Jumps** (no ADR), read off the sim (a rise from the ground faster than any blow's hop): a crouch, then a spring tall
  and thin, ears laid back; on landing a squash as hard as the fall was fast.
- **Seen and heard by what it is** (0093, 0094): `HitLooks.Creatures` names each species' `Impact` (`Flesh`, `CoolFlesh`;
  no blood), thrown from the side facing the hero, its share for a drill; `Sound.Creatures` its hit, thump and death
  (placeholders), an own hit on Suit, a friend's on World. An own axe hit nudges the view as a wreck's does.
- **Death** (0102): its hit thrown whole, a glint and shock ring, the body swelling white and fading; what it gives falls
  as the sim drops it (`features/items`). Heard where it fell.
- **A surface left or a world loaded** (no ADR): every body put away at once, no puff.

## Dev

`-- --planet --fauna[=species|look]` lets six creatures out either side of the hero (`dev/FaunaDemo`, the sim's
`Release`); `--ground=band1 --fauna=fluff --manual` in a cave. `--fauna-trace` logs strides and looks; `--fauna-herd` ten
of a kind in a row; without `--manual` `dev/FaunaHands` hunts them with the axe, then the drill. `--fauna-drill` holds the
drill on one loper; `--strip --fauna --fauna-course` puts a loper by the strip's walls and shaft for its leaps.

## Deliberately not handled

- **Creatures overlap** one another and the heroes, as the sim lets them: a struck herd bunches into one lump.
- **A friend's strike shows a round trip late,** and its tool is guessed: a running drill's bit by it, else the axe.
- **No outline or prompt** for a creature under the blade: the hit is the answer.
- **One design and one set of sounds per species;** no calls or cries. Individuals differ drawn only: a big one is struck,
  jumps and fits where a small one does.
- **A dying body's parts show through each other** as they fade; a far ear's glow is added over the body.
