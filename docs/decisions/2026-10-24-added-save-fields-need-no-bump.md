# A save's unknown field is refused, so an added one needs no format bump

- Status: accepted; amends [0031](0031-worlds-the-title-and-migrations.md)'s migrations
- Date: 2026-10-24

## Context

0031 made every change to the save format a bump of `World.SaveFormat`, with a step in `Migrations` and a fixture
save. Most changes only add a field: a new machine's state, a counter a module starts keeping. Each still cost a
bump, an identity step and a fixture, and two branches adding fields at once both took the next format number. A field this build did not know was dropped silently, so a bump was the only thing that kept an
older build from loading a newer save and losing what it could not read.

## Decision

- `World.Load` reads every slice with `UnmappedMemberHandling.Disallow` (#185): a field this build does not know is
  a newer build's, and the load is refused, saying which slice, instead of the field being dropped.
- An older save without a field reads it as its initializer's value. So **a field added with an initializer that
  means "none yet" needs no bump**, no step and no fixture.
- Removing or renaming a field, changing what it means, or adding a module still bumps the format with its step
  in `Migrations`, as 0031 says.

## Consequences

- Most saved-state changes touch only their module; `World.SaveFormat` and `Migrations` change rarely.
- A newer build's save with no bump shows as playable in an older build's world list, and is refused only when
  opened, with a message. Accepted: friends update together, and nothing is lost.
- The test `RefusesAFieldThisBuildDoesNotKnowAndReadsOneMissingAsItsDefaultSoAnAddedFieldNeedsNoBump` holds both
  halves.
