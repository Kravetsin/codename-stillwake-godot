# Plan: the drone's thoughts

Not built. Designed by design-author, cut by design-reviewer; the user's choices (2026-11-21) applied. A thought is a glyph the
drone «Нити» draws with its own sparks over itself ([drone-threads](../decisions/2026-11-21-drone-threads.md)): no plates, no
hologram. It shows only once the drone has learned the alarms ([drone-lessons](../decisions/2026-11-21-drone-lessons.md)).

## Decided (the user, 2026-11-21)

- First thoughts: the dart's meteor and bolt, cold, heat. No grit: the storm shows itself.
- Cold and heat show again each 3° deeper ("for the player that is already felt"), so `Step` is 3°.
- Only the player's own drone thinks: no friend's thoughts, nothing from `HeroView.Others`.
- Creative mode shows them too, so they can be tried there (the user tests in creative).
- The look is the pick «Контур» (`tools/pick-kit/pages/drone-moods.js`, candidate `outline`, db doc `picks/droneMoods`): a
  glyph is an outline of threads with a current of sparks along it, drawn out in 0.4 s, held 1.5 s, wound back in 0.4 s. An
  alarm glows red, pulsing 2.4 times a second, its sparks running 42 px/s; a mood smoulders, its sparks at 18 px/s.
- The moods kept: curious, find, bored, love, pride (fear, sleep and confused cut).

## Problem

A player learns that a place is deadly only after the damage has started: the body has cooled, the meteor has landed. The drone
flies beside the hero all the time and never says what it senses, nor how it feels about what its player does.

## Player experience

1. **The alarms learned.** Passing the wreck's emergency lamps, the drone learns the alarms. Until then it thinks nothing.
2. **Into the cold and back.** The hero walks out of the hull's warm zone. As `Felt` rises above 0, a snowflake is drawn over
   the drone, holds and winds back in. Three degrees colder further out, it shows again. Back inside, nothing. Out again
   within 20 s, nothing; after 20 s it shows again.
3. **A meteor while running.** The drone darts and reddens as it does today; the meteor glyph comes as it darts, stays until
   the impact, then winds in.
4. **Two at once.** A snowflake shows when a bolt glows: the snowflake winds in, the bolt comes. After the bolt, the snowflake
   comes again if the hero is still in the cold.
5. **Moods.** The drone tilts at a lamp it could learn from (curious), bounces at a find (find), sinks and drifts when nothing
   happens for long (bored), presses to the helmet when the hero comes back after a long time apart or is reborn (love), and
   swells when it learns a form by watching (pride).

What goes wrong: a hero stepping in and out at the warm zone's edge gets one snowflake, then quiet. In the cold with the bag
open, nothing shows until the bag closes.

## The moods (proposed triggers, the user to confirm)

| Mood | Glyph and motion (the page's) | When |
| --- | --- | --- |
| curious | a question mark; tilts aside, the eye askance | near something it could learn from and does not know: a teaching machine within `Lessons.Reach`, once a visit |
| find | two stars; bounces, looks at the find, orbits faster | a find, or a form learned (after the find scan's beat) |
| bored | three dots; sinks, drifts lazily, orbits slower | the drone's idle timer: nothing done and nothing shown for a long while |
| love | a heart; presses to the helmet, looks at the hero | back by the hero after a long time apart, or the hero reborn |
| pride | a crown; rises, swells, the eye up | a form learned by watching (a deed lesson: the scan) |

## Out of scope

- Low air (`Danger.Read`): the air bar and the drone's red pulse show it.
- Hunted (`Crew.Hunted`): the tracker blinks, the catchers are marked, the drone leans toward the hunter.
- Hunger, lava: their own dials and glow already show them.
- Grit, friends' thoughts, sounds, text, a mute setting, anything sent or saved.

## Approach

Thoughts live in the will, client only. `DroneWill` owns a Godot-free `DroneThoughts`; `DroneIntent` gains the thought being
shown and its weight, 0 to 1. `DroneSenses` gains `Cold` (`Crew.Felt`) and `Heat` (`Crew.Heat`) at the body cell `BodyHeat`
uses, and `Alarms`, whether this player knows `LightDrone.AlarmsMode`. Meteor (`Hazards.Incoming`) and bolt (`Hazards.Strike`)
thoughts belong to the dart. Moods read what the client already has: the find scan's records, `Lessons.Teacher`, the will's
idle time, the hero's distance and rebirth.

- **Gate:** no thought while the alarms are unknown. The dart itself stays innate.
- **Rank:** the dart's danger, then cold or heat, then the moods.
- **Level:** cold and heat count `ceil(degrees / Step)`; a dart's danger counts once per hazard; a mood once per its cause.
- **When a thought shows:** when its level is higher than the level last shown. The last-shown level drops to the current one
  only after it has been lower for `ForgetMs`.
- **One thought at a time.** A higher-ranked one interrupts the one showing, which winds in over `CutMs` and counts as not
  shown. A lower-ranked one waits and is dropped if it no longer applies.
- **None while** a window is open, the hero gestures, is passed out, or is in the drone meeting.

On the client, `LightDrone` draws a thought as a shape of `DroneSwarm` (drone-threads: "the thoughts add glyphs to the same
table"): its polylines exported from `tools/anime-art/subjects/drone.js` into `DroneInk`, above the core, a gesture's fan's
place. The mood's motion is the will's: a tilt, a bounce, a sink, a press, a swell. The alarms' board icon (`FormIcons.Alarms`, a
stand-in) becomes the alarm glyph.

Tunables live in the client (`DroneThoughts.Tune`), as in drone-motion: `Step` 3°, `ForgetMs` 20 000, `DrawS` 0.4, `HoldS` 1.5,
`WindS` 0.4, `CutMs` 100; the moods' idle and apart times to settle in play.

## Determinism and co-op

Nothing enters the sim and nothing is sent: only this player's drone thinks, from the shared sim's queries. No `Random`.

## Trade-offs accepted

- Minutes at one level of cold bring one snowflake; after that the thermometer and the frost carry it. A hint, not a gauge.
- The bag open in the cold: the snowflake shows after it closes; the player chose to pause.
- A bolt under a roof still shows: the dart already does this (drone-motion).
- A friend never sees your drone think.

## Tests

- Scenarios (game.tests, seeded will, scripted senses):
  - No thought before the alarms are known; the dart still darts.
  - Entering the cold: one draw, hold and wind; 60 s more brings nothing.
  - One `Step` colder: it shows again. Out and in within `ForgetMs`: nothing; after it: again. Heat likewise.
  - A dart's thought holds while the danger is reported, then winds in; never twice for one hazard.
  - A bolt interrupts the snowflake; it comes back if still cold, is dropped if not. A mood waits behind both.
  - Window, gesture, passed out: nothing shows; a place thought shows after the window closes.
  - Creative shows them; a hazard beyond `Notice` or on another surface shows nothing.
  - Each mood from its trigger, once per cause.
- Invariants (FsCheck, random streams of senses): at most one thought at a time; a kind never shows twice at the same level
  within `ForgetMs`.

## Cost

- No sim changes. Client crew: `DroneWill`, `DroneSenses`, `DroneIntent`, `DroneSwarm`, `LightDrone`; new `logic/DroneThoughts`;
  `DroneInk` regenerated with nine glyphs.
- About 300 lines plus 250 of tests.

## Open questions

- **The start of a hunt.** The tracker starts blinking and catchers come: a thought then, or is leaning toward the hunter enough?
- **The moods' triggers** above are proposals: the user confirms them, and the idle and apart times, before building.
