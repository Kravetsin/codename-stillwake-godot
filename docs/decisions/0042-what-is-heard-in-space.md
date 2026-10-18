# 0042 — What is heard in space

- Status: accepted; superseded in part by [0059](0059-machines-heard-at-work.md): the Suit bus raised 6 dB in
  space, the bed 11 dB under the title's track
- Supersedes: of [0033](0033-first-sound.md), "only Suit is left" while floating and the consequence
  "a low pressing drone in space may come later; the silence is the first answer"; of
  [0034](0034-wreckage-and-the-axe.md), every peer hearing every hit and break; of
  [0035](0035-music-on-the-title.md), "nothing plays in a world yet"
- Date: 2026-10-16

## Context

Out in space the World bus falls silent (0033), but the Suit bus played clear: a friend's axe on a
wreck rang out across the void as loud as one's own, and one's own rang as if in air. Sound does not
travel through vacuum; it does travel through a body. And with the world gone quiet the spacewalk was
dead air between chops.

## Decision

- **In space a hero hears only what it touches, through the suit.** While this player's hero floats,
  the Suit bus — an action's sound (out here a glove's thump), its own axe's chop, the break of a wreck its own hit broke — is
  dulled over the same 0.6 s as the World bus fades: a low-pass falling from 20 kHz to 900 Hz at
  24 dB an octave, eased in log space, and a low shelf under 300 Hz rising to a gain of 1.25 (about
  +4 dB) for the thud's body. No ring, no crack: a dull thunk through glove and helmet. Aboard,
  everything sounds as before.
- **The alarm is in the helmet's air, so it stays clear:** a bus of its own, Helmet, into Effects,
  never filtered. Settings will still set Master, Effects, Ambient, UI and Music; World, Suit and Helmet
  are the hearing's.
- **Another hero's hit is silent** for everyone: it does not touch you, and a wreck is only ever out in
  space. Its jolt, sparks, cracks and burst are still seen by every peer. The sound stays bound to the
  target, the wreck, as Terraria binds a hit's sound to what is hit; the swing has none yet.
- **Own is known from what this peer sent,** not from the sim: the client's own `Hit` commands sit in
  `Client.InFlight` from the frame they are sent until their tick runs. A piece whose damage rises (or
  which breaks) in the frame one of its own hits left the list was hit by this player
  (`features/debris/OwnHits`). Nothing changes in `sim/` or `net/`; no sound goes over the wire.
- **Music fills the silence of space.** As this player's hero leaves the ship, a quiet bed fades in on
  the Music bus over 2.5 s, 8 dB under the title's track; back aboard it fades out over 2 s, so the
  vents' hum returns, and stops once silent. It follows the same reading as the muffle: a placed hero
  floating, so the title and dev worlds without a hero never play it. The placeholder is the team's
  synthesised drone "Wreck", CC0, until the team writes its own.

## Consequences

- Two heroes chopping one wreck each hear only their own blows; a guest watching hears nothing.
- A wreck a friend breaks in the very frame one's own hit on it runs, refused as the wreck is gone, is
  heard breaking as one's own: a frame's window, on a wreck one was chopping anyway; left be.
- The action click would all but vanish in space: a high click with almost nothing under 900 Hz
  (about 40 dB down through the muffle). So while this player's hero floats, an action plays a glove's
  low thump instead (Faulkin's "LowHit_01", CC0), on the same Suit bus, muffled as all there is; aboard
  it is the click as before. Both are chosen by the same floating the hearing reads.
- A hero chopping from an open airlock, aboard, hears its own chop clear, as anything touched in air.
- Music by place or by moment beyond the spacewalk is still to be decided.
