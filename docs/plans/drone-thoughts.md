# Design: the drone's thoughts and moods

## Problem
Players find out a place is deadly only after the damage starts: the body has cooled, or the meteor has landed. The drone
flies beside the hero and never shows what it senses or feels, so it reads as a lamp, not a companion.

## Decided (the user, 2026-11-21)
- Thoughts: the dart's meteor and bolt, cold, heat; no grit. Cold and heat re-show each 3° deeper. Own drone only.
  Creative shows them. Thoughts show only once the player knows the alarms form (learned from emergency lamps).
- Moods, look «Контур» (`tools/pick-kit/pages/drone-moods.js`, `outline`): curious, find, bored, love, pride. Not fear,
  sleep, confused.

## Player experience
1. Just after pairing, the drone tilts toward a red emergency lamp off screen and draws a question mark of sparks
   (curious). The hero walks there, and the drone learns the alarms.
2. Beyond the hull's warmth, a snowflake of sparks rises over the drone, holds, and unwinds. Three degrees colder, again.
3. A meteor comes. The drone darts at it, red, and holds the meteor glyph until the impact.
4. Each new item: the drone hops while it scans the icon (find). After the scan lesson, it rises and swells under a crown
   (pride).
5. After a minute standing still, three dots show and it sinks and dims (bored). Waking after a death, a heart shows and
   it presses to the helmet (love).

Goes wrong: in the cold with the bag open, nothing shows. When the bag closes, the snowflake shows if still cold.

## Out of scope
- Grit, low air, hunting, hunger and lava as thoughts: the HUD and the drone's red tint show them.
- Fear, sleep, confused. Love after a long time apart: the drone is never away longer than a lesson. A find star.
- A friend's drone thinking or feeling. Sounds, text, a mute setting. Anything sent, saved or hashed.

## Scale
Only your own drone, your own hero's cell and the teaching machines within `Wonder`. A few thoughts a minute at most;
curious and bored at most once every 2 minutes.

## Approach
Client only, Godot-free. `DroneWill` owns a new `DroneThoughts`; `DroneSenses` gains the cues and `DroneIntent` the glyph
and how far it has formed. `HeroView` sets the cues for your own figure only.
- Gate: `Research.Knows(you, AlarmsMode)`; always true in creative.
- Cold, heat: `Crew.Felt` and `Crew.Heat` at the body cell; level `ceil(degrees / Step)`. Shows when the level rises above
  the last shown; that mark drops only after the level stayed lower for `ForgetMs`.
- Meteor, bolt: while the will darts.
- Curious: a machine that teaches a form you do not know, within `Wonder` cells (`Lessons.Teacher` gains a reach argument;
  the sim keeps `Reach`). Once per machine, the eye on it.
- Find: while `FindScan.Over` sweeps an item, the drone hops only; the icon is the picture.
- Pride: a deed lesson ends. Bored: the anchor stays within `BoredBox` for `BoredAfterMs`, the hero awake, no window open;
  once until it leaves the box. Love: `HeroView`'s `woke`.

One shape over the drone at a time (review cut): a thought or mood starts only when nothing higher is showing (a window,
the dart, a gesture, a thought before a mood), and is never resumed once cut. Moods wait while the scan reads. Nothing
shows while the hero is passed out, in the meeting or seated.

Cold and heat show when the level rises above the highest shown; that mark resets only at level 0 (review cut: no
`ForgetMs`). Love and pride play at once; curious and bored each have a plain cooldown.

Look «Контур»: 8 glyph polylines that `drone.js` exports into `DroneInk`. In 0.4 s, hold 1.5 s, out 0.4 s. Alarms red at
42 px/s; moods soft at 18 px/s; each mood moves the body as the pick showed.

Tunables in `DroneThoughts.Tune`: `Step` 3°, `ForgetMs` 20 000, `MoodGapMs` 120 000, `BoredAfterMs` 60 000, `BoredBox`
8 px. `Lessons.Wonder` (40 cells) in `sim/Content` beside `Reach`.

## Determinism and co-op
Nothing enters the sim; `Teacher` only gains an argument. No `Random`. A friend's drone shows neither thoughts nor moods.
A late joiner's levels start at zero.

## Trade-offs accepted
- Minutes at one level of cold → one snowflake; then the thermometer carries it.
- Love or pride behind a long open window → shows late, never lost.
- Curious about a lamp behind a wall → the drone looks through walls (drone-motion).
- A cold thought takes the lamp mirror's sparks for about 2 s → warnings outrank the mode.
- Pride once per player today (the scan is the only deed lesson).

## Tests
- Scenarios (game.tests, seeded will, scripted senses): cold once, again one Step deeper, nothing within ForgetMs; heat
  alike; no gate nothing; creative shows; dart glyph holds; a bolt interrupts the snowflake, which returns; a gesture,
  window or icon over the head holds cold back; curious once per machine within Wonder; bored after BoredAfterMs, again
  only after moving; love on waking, pride after a deed lesson; nothing for a friend.
- Sim: `Teacher` at reach 40 finds a lamp 30 cells away; the module still teaches only within `Reach`.
- Scenario tests only (review cut). Pin: the wreck's upper emergency lamp (about 39.7 cells from the core) is within
  `Wonder`.

## Cost
About 400 lines plus 250 of tests: crew `DroneWill`, `DroneSenses`, `DroneSwarm`, `LightDrone`, `HeroView`, `Figure`, new
`DroneThoughts`; `drone.js` glyphs; `Lessons.Teacher` reach; `Lessons.Wonder`.

## Settled
- Both emergency lamps are 35–40 cells from the core, beyond `Reach` 24: the alarms are not learned at pairing, so
  curious points the way first. `Wonder` 40 is needed.
- A mode in use does not stop boredom (lead's call; the user may say otherwise).

- Bored (the user, 2026-11-21): a minute standing still, as designed (`BoredAfterMs`, `BoredBox`), not the perch timer.
- Love: on waking after a death (the user did not object).
