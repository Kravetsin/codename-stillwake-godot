# The trade capsule is a freight canister in ink

- Status: accepted
- Date: 2026-11-04
- Changes [trade-capsule](2026-10-30-trade-capsule.md)'s look: its pixel picture and round flame are gone

## Context

The capsule a trade station sends off was still the 10 × 11 pixel picture from before the [ink and watercolour](2026-11-01-ink-and-watercolour.md),
with a flickering circle for a flame: the last pixel art the player sees in flight.

## Decision

- **A freight canister:** a squat banded drum on two swept fins, a keep-clear stripe, an amber lamp and the eye on its lid it is hoisted by.
  The user picked it from five on [Trade capsule](https://claude.ai/artifact/Vyb7JhyHrgZoh71aWWvHJb), against a slim courier dart, a return
  pod with a heat shield and chute, a tube of welded pipes and an egg-shaped cell in a frame.
- **Drawn by the `capsule` subject** of `tools/anime-art`: `capsule`, 18 × 26 world pixels, its size by the station (`CapsulePath.Near` is 1,
  the picture's own; `Far` 0.13, as small as before), and three frames of an ink flame, `capsule_flame_<n>`, in the fire's hard steps,
  played at 15 a second under its bell.
- **The flame kicks** half again as long for `CapsulePath.Kick` ticks as it lifts off and as it touches down.

## Deliberately not handled

- The pick page's station jolt and screen flash as the capsule goes: they belong to the station's picture, not the capsule's.
