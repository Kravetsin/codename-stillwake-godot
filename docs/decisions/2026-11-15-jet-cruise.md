# A jetpack pays for changing speed, not for distance

- Status: accepted; supersedes of [0047](0047-equipment-the-jetpack-and-the-slower-swim.md) that every tick of thrust costs air,
  and of [food and the body](2026-11-12-food-and-the-body.md) the canister's 25 s cooldown
- Date: 2026-11-15

## Context

A jetpack vented the suit's air every tick Shift was held: thrust two units a second on top of a breath, three in all, and
without thrust a hero stops within some fourteen cells, so Shift was held the whole way. The basic helmet's 45 units flew
about fifteen seconds. Before the canister had a cooldown, a flight was a canister pressed every five seconds, a formality;
with the cooldown the far debris was closed, as the way back did not fit in a tank. The user wants the cooldown kept, so a
canister stays a choice with a price, and the stifling gone.

## Decision

- **A cruise is free.** Thrusting along the way a hero already flies at `Jetpack.Top`, within about six degrees
  (`Hero.Cruising`), vents nothing: only the breath is paid. A start, a turn, a brake or a dodge vents as before. The client
  says so: it sends `Thrust` on only while it vents, so the sim's rule (a unit every `Jetpack.TicksPerUnit` ticks of thrust)
  is unchanged.
- **Seen and heard as it is paid.** The plume and the roar show only while the jetpack vents, a hero's own as everyone's;
  a cruise is silent. The card's line reads "air used speeding up, turning or braking".
- **The control is unchanged:** Shift held flies and let go brakes as before. Free gliding (no drag) was weighed and turned
  down: it overshoots a wreck and cannot dodge a sentry in time.
- **A canister's cooldown is 20 s** (`Crew.CanisterCooldown` 400 ticks), easier to count; a canister still gives 20.

## Not done

- A bigger canister (30) and a stronger deep breath from canned meat were weighed; left for a playtest of this.
- Air always 100 with suits leaking, as walls do, was discussed and set aside: one number down a ladder of dozens of suits ends.

## Consequences

The basic helmet's tank crosses some 1300 cells at full speed, three times as far; a fight or a careful approach costs as
much air as before.
