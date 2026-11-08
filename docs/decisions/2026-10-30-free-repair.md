# A repair takes only work

- Status: accepted
- Date: 2026-10-30
- Supersedes the repair cost and the refund rule of [magnetic-flare](2026-10-30-magnetic-flare.md)

## Context

A repair cost a quarter of the machine's parts, paid on its first tick. A flare can break what makes those parts, and a
player with no spare wire then has no way back: a softlock, for flares come whatever the player has. A broken machine
also showed the same red bolt as one with no power, so nothing said which of the two to fix.

## Decision

- **A repair takes nothing but its work**: the same ticks as before (`RepairRules.Share` of the build's), with the
  screwdriver, which is crafted from nothing at the lifepod. The only refusal left is `building.notBroken`.
- **Taken apart broken, a machine refunds its whole cost.** The reduction existed so rebuilding would not beat a paid
  repair; with nothing paid it would only punish.
- **A wrench over a broken machine**, blinking in place of the red bolt, on every broken machine, those that draw no
  power too. The bolt goes on meaning short of power, and only that.

## Consequences

- A flare costs time and a walk round the ship, not materials: its pressure is how long things stay stopped.
- Saves need no step: `Repairing` keeps its shape and meaning. A repair paid for in an old save is not refunded.
- We do not bring back a cost for rarer machines: any cost at all reopens the softlock unless the parts are guaranteed.
