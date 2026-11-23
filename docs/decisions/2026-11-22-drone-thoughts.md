# The drone shows what it senses and feels

- Status: accepted; supersedes in part [drone-motion](2026-11-21-drone-motion.md) (no warnings of cold, heat or flares) and
  [drone-lessons](2026-11-21-drone-lessons.md) (the alarms form gating nothing yet, its stand-in icon)
- Date: 2026-11-22

## Context

The drone flies beside its hero and showed nothing of what it sensed or felt, so it read as a lamp rather than a companion,
and a place's danger was learned from the damage. The alarms form was learned and did nothing yet.

## Decision

`DroneThoughts` (game/features/crew/logic), owned by `DroneWill`: this player's own drone only, client only, nothing sent or
saved. One picture over the drone at a time, by rank. A window, a gesture, an icon over the head or a lesson holds them
back. Nothing shows while the hero is passed out, seated or held by a meeting or a vision.

- **Alarms**, red, only with the alarms form (in creative every form counts as known):
  - the meteor or bolt while the drone darts at it;
  - a magnetic flare once as its warning begins (the user: such a flare will kill in future);
  - cold and heat each 3 degrees of the body off normal: 34, 31, 28 and 40, 43. A level counts as shown only after its
    picture held, so one cut sooner shows again; back within 3 degrees of normal they start over.
- **Moods**, waiting while the scanner reads:
  - love on waking from a death, pride after a deed lesson: both wait, never lost;
  - curious at a machine that teaches a form not yet known, within `Lessons.Wonder` (40 cells, beyond `Reach`, so it points
    the way to the wreck's emergency lamps): once a machine, 2 minutes apart;
  - bored after a minute within an 8 px box, under every other mood: it stays until the hero moves and comes back after a
    cut, so a player who stepped away still sees it.
- **A find** is a hop of the body with no picture: the icon is the picture.
- **The look** is the pick «Контур» (`tools/pick-kit/pages/drone-moods.js`, candidate `outline`, db doc `picks/droneMoods`),
  exported by `tools/anime-art/subjects/drone.js`: in 0.5 s, held 3 s, out 0.5 s. 1.5 s was played and could not be read in
  time. The flare's magnet was added after the pick. The alarms form's icon is now the meteor's glyph.
- `-- --drone-moods` plays them.

## Deliberately not handled

- Grit, low air, hunting, hunger and lava as thoughts: the HUD and the drone's red tint show them.
- Fear, sleep, confusion; love after a long time apart.
- Friends' drones thinking or feeling; sound, text, a setting to mute them.
- A long stay at one degree level is one picture: the thermometer carries it on.
- The drone looks at a teacher or a danger through walls.
