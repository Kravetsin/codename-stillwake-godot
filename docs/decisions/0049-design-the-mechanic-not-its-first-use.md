# 0049 — Design the mechanic, not its first use

- Status: accepted
- Supersedes: principle 2 of `CLAUDE.md`, "No abstraction without two callers today", and every
  "(principle 2)" reason in earlier ADRs and READMEs that waited for a second caller
- Date: 2026-10-17

## Context

Principle 2 held back any shared module until two callers existed. Here a feature is rarely made once:
it is a mechanic that grows over many changes. The rule made each change patch its own caller. The
grid is the example: rooms, pipes, the light, the tile view, the starlight and the door prediction
each learned on their own what changed, by comparing whole layers with a copy. Five callers of one
need, solved five times; on a bigger world a door froze the frame. The shared answer, a log of changed
cells kept by the grid, waited for "a second caller" that was there all along.

## Decision

- **Design the mechanic, not its first use.** Before building, name where the mechanic is heading:
  the users it will have this stage and next, taken from the concept and the plans. Build the shared
  module that serves them, rather than patching each caller and generalising later.
- **Still no option or hook without a named user.** A knob "for flexibility", a field "in case", an
  interface nobody is planned to implement do not go in. A user named in the concept or a plan counts;
  "maybe some day" does not.
- **When three fixes look alike, stop and find the common root** before making a fourth.

## Consequences

- Designs list the users a shared module is for. Reviewers flag abstractions with no named user, not
  abstractions with one user today.
- Earlier decisions that deferred a shared module "until a second caller" are open to revisit when
  their mechanic next grows.
