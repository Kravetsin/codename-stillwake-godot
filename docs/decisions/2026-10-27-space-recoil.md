# Space recoil

- Status: accepted; built
- Date: 2026-10-27

## Context

The player expected a gun to push its shooter back in space and found none. Out of the hull the hero swims with no momentum:
its speed each step is the keys' (`Hero.Swim`), so a push added to it would be gone the next step. Only the jetpack keeps a
speed of its own. A gun's `Knock` is the push its hit gives what it strikes, not its shooter.

## Decision

- **A gun's recoil** (`Gun.Recoil`, thousandths of a cell a step): the nailer 60, the railgun 220. Each shot fired afloat kicks
  the hero back along the barrel as drawn (`Hero.Kicked`), on the client, as all of its movement is (0002).
- **A kick of its own:** `Hero.Kx`, `Hero.Ky` drift the hero on besides its swim, kept `Hero.KickKeep` thousandths (950) a
  step, rounded toward zero: the railgun's carries it some four cells over about a second and a half, the nailer's about one.
  Swimming adds to it; a jetpack folds it into its own speed, to drag off as any does. The tether and walls hold it as they
  hold a swim.
- **Afloat only:** aboard, on a planet, on a ladder or in the fog a shot kicks no one, and a kick still running is dropped
  there: feet, gravity and the fog take it.

## Consequences

- Others see a kicked friend as they see its swim, from what it sends; the sim never hears of recoil.
- A kick into a wall presses on it till it fades.
- Rapid fire stacks kicks: two railgun shots two seconds apart overlap little; a faster gun later may need a cap.
