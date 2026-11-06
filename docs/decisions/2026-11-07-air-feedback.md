# Air and power answer the player loudly

- Status: accepted; amends [0081](0081-the-wrist-is-one-column-of-gauges-and-keys.md)'s suit gauge and canister swirl,
  [the-last-breath](2026-11-05-the-last-breath.md)'s sky-blue gauge, and [energy-ring](2026-11-06-energy-ring.md)'s discharges
- Date: 2026-11-07

## Context

At the next playtest the user found the new energy tab good but wanted the bolt to rattle and catch the eye while a tool
runs. They barely noticed the suit gauge, wanted a bright refill from a canister, found the canister's swirl round the hero
hard to see even looking at him, and its hiss too loud. One pick page showed four candidates for each of three things: the
gauge as air is spent, the gauge as a canister is breathed, and the effect round the hero.

## Decision

- **The bolt rattles.** Each beat jolts it up to 0.7 px off its place (the grinder's 1.2 px), settling as the beat fades,
  and a beat throws a discharge one time in four (the grinder's nearly one in two), three times as often as before.
- **The O2 sign turns red** and pulses while the suit runs low, when the beep sounds and the visor fogs (25%). The lamps
  stay sky blue: red lamps read as wounds before.
- **A lamp lost whole chips off** (picked: "Лампа отлетает"): it flashes white, a chip of it jumps off, tumbles and falls
  out of the tab, pale sparks scatter. `LampFlakes`, drawn over the frame by `WristGauge`.
- **A canister floods the tab** (picked: "Плашка вспыхивает небом", with the motes the surge had kept at the user's word):
  a white flash, the screen flooded sky, the frame's rim burning, the lit lamps whitening and cooling from the left, the
  lamps it filled glowing for 1.8 s, the tab shaking for a fifth of a second. `GaugeWash`; it replaces the surge's sky wash
  and its sweep.
- **A canister's air is flung out of the chest** (picked: the user's own idea, "Разлёт со шлейфами"): a flash, then 24
  motes flying out to the sides and up, slowing and curling, each dropping a trail of finer motes that hangs and fades.
  It replaces the vortex round the body (`AirSwirl.Fling`).
- **The canister's hiss is 9 dB quieter**, about −29 dB RMS.
- **Room air breathed again is unchanged**: one wave of pale light across the lamps and an inhale, then the slow rise with
  its light running along the lit lamps and motes rising. The canister is what the player does; room air comes on its own.

Rejected: for the gauge as air goes, the gauge breathing with the hero, a flowing level dripping light, the edge lamp ticking
and jittering; for a canister, a white front flooding right, the sign flaring a beam along the lamps, a pressure ring with
sparks; round the hero, air drawn in to the visor, a jet from the back valve, a denser vortex.

## Deliberately not handled

- A helmet put on or off drops the shown fill at once; any lamps it drops chip off as if lost.
- The fling's trail motes share the hero layer's particle limit; several heroes breathing at once thin out rather than
  cost frames.
