# fauna (client)

The creatures ([0102](../../../docs/decisions/0102-creatures-live-in-the-sim.md)). Drawn only: the sim (`sim/Fauna`) spawns,
moves, knocks and kills them and drops what they give. Striking one is the hero's hands (`features/crew`): the axe's blade and
the drill's bit are tested against each creature's body as against a wreck's box, wherever the hero stands
(`Aiming.StruckCreatures`, `Aiming.TouchedCreatures`, `Aiming.StrikesOf`), and send `Strike`.

- **Reads:** `FaunaModule.Creatures`; `Client.InFlight` — this player's own strikes, seen the frame they are sent; `Ui.Heroes` —
  whom a creature watches and flinches from; `Ui.Bits` — a friend's running drill by a creature struck; the grid, for the
  ground under a creature's feet; `Session.Progress`.
- **Only what the camera sees:** a body only for a creature within the camera's view and 4 cells round it, from a pool that grows
  to the most ever on screen and never frees. A creature gone from the world while drawn was killed: nothing despawns in a
  hero's view.
- **Drawn between ticks:** each body kept at the tick before and the last, drawn `Progress` of the way between them, a tick
  behind the sim, so a knock, a step up or down a cell and a landing glide. On the ground its feet are put on the ground under
  each (a slope's diagonal, the step below an overhang, at most 0.6 of a cell down), the body between the two and tilted to
  them up to 0.4 rad, eased over 70 ms: the sim stands it on the top of a whole cell. Facing the way it moves; still, toward a
  hero within 9 cells, else the way it last moved.
- **The body** (`CreatureBody`): the design its species' `Look` names, as the user picked it on the loper page (classic, tapir,
  sixleg, fluff). Its parts (`art/fauna/<design>/`) are index maps — R the ramp, G the tone — recoloured once at first use by
  the planet's palette (`CreatureParts`, `FaunaLooks.PaletteOf`, the first planet's «Саванна»), with a white copy for the flash
  and the glow maps in the glow ramp a tone up; painted by `tools/planet-art/fauna`, their rig, pose numbers, eye and palettes
  generated into `Content/FaunaArt.cs`. A sprite a part in the rig's order, each hung from its parent's place and turned about
  its pivot (`CreaturePose`, the page's pose, plain C#, tested), drawn at the screen's resolution; lit as the world is (`Lit`),
  with `Layers.Creatures`, under the heroes. Its eye: open, half shut grazing, shut blinking, wide startled, its pupil on the
  hero within 9 cells by whole pixels. Idle with no hero near for 1.5 s it grazes, head down nibbling and up for a look; with
  one near it notices it, head up, ears perked. Its glowing parts are added over it unlit, and their spots (squares of 7 px of
  a glow map, at most 4 a creature) are lights in their colour (`Lights`, the light view's `Creatures`): the fluff's back and
  antennae light a cave.
- **Each its own** (`CreatureVariety`, plain C#, tested; `FaunaLooks.Varieties`, per species): from its id and the surface's seed
  by an integer hash, the same on every peer and never the world's random stream. Drawn 82–116 % of its design's size (a
  golden-ratio step an id: any three in spawn order span most of the range), about the middle of its feet, so it still stands
  on the ground; the sim's box, hit points and speed are the species'. Its coat, the fur ramp only, shaded up to three quarter
  tones within the ramp and blended a step or two toward a neighbouring ramp of the palette, cool and warm by turns an id; its
  outline, eye, skin, belly and glow as painted. One in seven a morph, a whole tone darker (loper, sixleg) or paler (tapir,
  fluff). Each marking of its marks part (a run of touching pixels) dropped a quarter to a third of the time, those kept faint,
  half or bold; a glowing one lights only where worn. A look's pictures are made once and kept (`CreatureParts`).- **Its legs** (`CreatureGait`, plain C#, tested) go by the ground the sim moved it on its last tick, eased over 90 ms, never a
  frame's step: `MostStrides · tanh(v / (MostStrides · Stride))` strides a second, one each `Stride` pixels at a crawl, rising
  smoothly to a cap (`FaunaLooks.Runs`: the loper's 3.2, the tapir's 2.6, the sixleg's 2.8, the fluff's 3.4), counted by time
  so 60 and 144 frames a second draw alike. From 90 px a second to 200 the pose blends into a gallop: forelegs together, hind
  legs half a stride on (six legs keep their tripods), wider reach, a bound a stride, the body rocking nose up and down and
  stretching out and gathering; the fluff bounds in hops, squashed on landing. Tucked in the air, eased in and out.
- **A blow** (`CreatureMotion`, plain C#, tested): this player's own strike reacts the frame it is sent, a round trip before the
  sim's knock; a friend's as the sim's `Damage` rises (an own strike's damage within 0.7 s is not shown twice). It flashes
  white (an unlit copy of the silhouette over it, 150 ms), squashes wide and flat and springs back past round (380 ms), flinches,
  its top thrown the way it is knocked (320 ms), its eye wide and ears pinned (900 ms), and shakes a pixel either way (70 ms;
  the blade stops on it a moment, the figure's own `Bit`). All drawn only: its place is never held, as a neutral creature has
  no hit-stun and bolts at the blow. A drill's hit is its share of the axe's (`Impacts.ShareOf`): flashes and shakes less, ten
  a second. Fleeing it leans forward and stretches long, ears back and tail flown, all eased in with its flight.
- **Jumps** read off the sim (a rise faster than 250 a tick from the ground, past any blow's hop): a 70 ms crouch, then a spring
  tall and thin easing back over 380 ms, ears laid back; on landing a squash as hard as the fall was fast.
- **Seen and heard by what it is** (0093, 0094): `HitLooks.Creatures` names each species' `Impact` (the loper's `Flesh`: white
  slash streaks, tufts of fur, a warm puff, a white glint and shock line, no blood; the cool-coated three's `CoolFlesh`, its
  tufts lilac), thrown from its side facing the hero, its
  share for a drill; `Sound.Creatures` its hit, a body thump under it and its death (placeholders from the files here: the cap's
  squelch lower, a soft landing's thud higher, a heavy landing's), this player's own on Suit, a friend's on World. An own axe
  hit nudges the view as a wreck's does.
- **Death:** its hit thrown whole, fur and soft puffs every way, a white glint and a shock ring, and its body swelling white and
  fading over 170 ms; what it gives falls from its top row as the sim drops it (`features/items`). Heard where it fell.
- **A surface left or a world loaded:** every body put away at once, no puff.

## Dev

`-- --planet --fauna` lets six creatures out either side of the hero, every kind in turn (`--fauna=fluff`, a species or a
look: that kind only), from the hero's row up out of a hill or down to a floor, so in a cave too (`--ground=band1
--fauna=fluff --manual`) (`dev/FaunaDemo`, the sim's `Release`); `--fauna-trace` logs each one's strides a second and its looks as it comes into view; `--fauna-herd` lets out ten of a kind in a row, five a side; it logs every strike
and every creature gone; without `--manual` `dev/FaunaHands` hunts them as a player's keys and mouse would, the axe first, the
drill after the first kill (or 16 s), the nearest within 6 rows and 45 columns of where it began, in sight. Record with
`--write-movie … --fixed-fps 20 --quit-after 900 -- --planet --fauna --zoom 2`. `--fauna-drill` stands the hero still with the
drill on one loper beside it; `--strip --fauna --fauna-course` beams it to the strip's open ground by its walls of three and two
and its shaft, a loper beside, for its leaps.

## Deliberately not handled

- **Creatures overlap** one another and the heroes, as the sim lets them: a struck herd bunches into one lump.
- **A friend's strike shows a round trip late,** as the sim's damage rises, and its tool is guessed: a running drill's bit by the
  creature, else the axe.
- **No outline or prompt** for a creature under the blade: the hit is the answer.
- **One design and one set of sounds per species;** no calls, no pain cries: sounds go through the user's audition. Its individuals
  differ in size, coat and markings only, drawn: a big one is struck, jumps and fits where a small one does.
- **A dying body's parts show through each other** as they fade, each alone, for its 170 ms under the white swell; a far
  ear's glow is added over the body, as the page adds every glow last.
