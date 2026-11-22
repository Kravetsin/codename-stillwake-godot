# The ship's music plays now and then

- Status: accepted; supersedes of [0042](0042-what-is-heard-in-space.md) the bed of space playing whenever a hero floats
- Date: 2026-11-22

## Context

A world had no music but the bed of space. For the playtest the user made two tracks with Suno to fill the silence: quiet,
with a few louder moments, to be replaced by real music. An endless loop tires; games of long sessions play a track, leave
a silence, and play another. The user picked that, and asked that music neither stops nor follows other rules out in space.

## Decision

- **A track, a silence, a track.** The first begins a minute in; each next 3–6 minutes after the last ended, the two in
  turn from a random first.
- **Aboard and out in space alike, never on a planet:** every surface with no gravity of its own (`Surfaces.Kind.Floats`),
  a derelict's as the ship's. On a planet the wait stands still and a track begun fades out but plays on, so it is heard
  again from where it has got to.
- **The bed of space fills only the silences:** it eases out as a track begins and back in once it ends.
- **In the background:** on the Music bus, about half as loud as the title's track (10 dB under it), both levelled to
  about −17 LUFS with their range narrowed to 5 LU, so their louder moments stay in the background.
- **Played as made:** their cues vary no pitch, unlike other one-shots.
- **Placeholders under 0035's rule:** `Suno Basic, non-commercial: replace before sale` in `game/audio/CREDITS.md`. The
  files hold sound alone: no cover, no maker's tags.

## Consequences

- Each peer has its own clock and draw: friends in one world hear different tracks at different moments.
- The clock is the session's, never saved: after a start or an exit to the title the first track is a minute away again.
- Not done: music by danger or event, music on planets, a setting for how often it plays.
