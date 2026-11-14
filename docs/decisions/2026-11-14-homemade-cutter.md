# The plasma cutter is the homemade one

- Status: accepted; supersedes the cutter's pick in [salvage-tools-raw](2026-11-14-salvage-tools-raw.md)
- Date: 2026-11-14

## Context

The cutter shipped raw as the copper-ring reference, its jet drawn from the nozzle through the rings. In the game the jet lay over
the rings: the jet is drawn above the hero, additive, so it covers whatever the tool's picture holds. On that reference a rail runs
through the rings' middles, along the jet's own line, so even a front layer for the rings would hide the jet behind the rail or
show it over it. The user had picked the plainer reference for that reason.

## Decision

- The cutter is the user's pick, «Самодельный»: a rusted tube on a wooden pistol grip, two copper coils over it, a gas bottle
  under, green insulators and a copper tip. Raw, 32 × 15 as before.
- The jet leaves from the tip. Nothing of the tool lies along the jet, so nothing has to cover it.
- Running, the tip glows white-hot in violet (`hero_plasmaCutter_on`), in code.

## Consequences

- The jet needs no layer above or below the tool's picture.

## Deliberate omissions

- The coils do not light while it runs: the jet and the lit tip already say it is on.
