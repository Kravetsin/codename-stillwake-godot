# Hold to fire

- Status: accepted; built. Changes how a gun is fired in [shots are the sims](2026-10-26-shots-are-the-sims.md) and [the railgun](2026-10-27-the-railgun.md)
- Date: 2026-11-04

## Context

Each shot of the nailer needed a fresh click timed to its reload, and the railgun a fresh press after its cooldown: in a
fight the player watched the gun instead of the target. The player's word: hold the button and it fires whenever it is
ready; the railgun charges while held, fires let go, and held again right after starts charging as soon as it may.

## Decision

- **A gun fires while the left button is held.** Held with its cooldown out, a gun with `Charge` 0 fires at once and again
  each time its cooldown runs out, a `Pace` apart: the time a frame overshot the cooldown counts towards the next, so the
  shots keep the pace and do not slip by a frame each. A click still fires one shot when ready; a click in the cooldown is
  not kept for later.
- **A gun with `Charge` begins charging whenever it is held with its cooldown out**, by itself if the button was down
  before; it fires let go full, as before. Let go early, it fires nothing and charges again from nothing.
- **Nothing to fire, nothing sent.** With no ammo or a flat belt (`items.flat`), held, the client checks every frame and
  sends no `Fire`: it clicks dry at the press and once as the cooldown runs out, never every frame.
- **All of it is the client's** (`GunHand`): pace and charge stay its word (0034, the railgun); the sim's `Fire` and its
  refusals do not change.

## Deliberately not handled

- No setting to go back to a click per shot: nobody asked for it.
- A railgun's charge begun by itself counts the whole frame it began in, up to a frame early; its pace is 2 s.
