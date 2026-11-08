# Hurts are shown

- Status: accepted
- Date: 2026-11-08

## Context

Played, the capback's gas and slime hurt with no sign: the air gauge slid down a little as it does every breath, and the player
could not tell a puddle hurt at all. Every hurt had its own feedback or none: a ram its thud, an arc the stun gun's hit, lava
its burn, a zap or poison nothing of the hero's. The user picked, on the poison feedback page, the screen's edge washing in a
colour and the gauge's loss blinking, and asked for them as one look for every kind of hurt, its colour by the kind, with one
neutral sound of the hero under each source's own.

## Decision

- **The sim counts hurts with their kind.** `ICrewOps.Shock` and `Blow` take a `Harm` (`Blow`, `Shock`, `Poison`, `Burn`);
  each one that takes air, and each breath on lava, adds one to `Hero.Hurts` and sets `Hero.Hurt`. Saved fields added with
  "none yet" defaults: no format step. Creative and a hero out take none. The kinds: a creature's ram or punch a blow; a
  sentry's arc, a resonator's zap, a cradle dug a shock; a capback's gas or slime poison; lava a burn.
- **The client shows each new one** (`HurtSeen`: a count risen since the last frame's read; the first read, a count that
  fell, no hero, nothing): `HurtView` washes the screen's edges in the kind's colour, a ragged watercolour rim up in 0.06 s and
  gone by 0.7 s, and the suit gauge reads the sim at once, its lost part blinking in the colour for 0.6 s before it drains white
  as ever. Colours in `HurtLook`: blow red, shock cold cyan, poison yellow-green, burn orange.
- **One sound of the hero** on each hurt, under the source's own (a ram's thud, an arc's hit, lava's burn), in the helmet: the
  user's pick of six (four not a voice), a muffled «oof» (unfa's, CC0). Asked to vary it, it is three takes a few tones apart,
  never one twice running, each ±10% in pitch.

## Consequences

- A blow the cape spares whole is no hurt: nothing shows.
- Cold, thin air, running and the last breath are drains, not hurts: the frost, the fogged visor and the tunnel show them.
- Lava's burn sound keeps its own client clock from the touch (0087), so it can fall up to a breath apart from the edge.
- Friends' hurts show and sound nothing on this screen; their own sources' sounds still carry.
