# Switched-off sign

- Status: accepted
- Date: 2026-11-08

## Context

A machine a player switches off in its panel (`Power.SetSwitch`, industrial-oxygen) draws nothing, so the power view put the
blinking red bolt of a machine with no power over it: it read as a fault, not a choice.

## Decision

- **The power symbol, steady:** over a machine switched off, `sign_off`, a ring open at the top with a bar into it, in
  periwinkle (#8c9cf0, core #e2e6ff), 9 by 9, never blinking. The user picked it (A) of four on a pick page: the power
  symbol, a toggle switched off, a crossed-out bolt and a pause sign.
- Drawn by `tools/anime-art/subjects/signs.js` as the other power signs are; a broken machine's wrench still comes first.

## Consequences

- A machine switched off reads apart from one starved of power at a glance; switched on again, the bolt rules as before.
