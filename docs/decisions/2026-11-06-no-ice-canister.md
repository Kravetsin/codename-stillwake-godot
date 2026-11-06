# Canisters are salvaged, not frozen of ice

- Status: accepted; supersedes [water-to-oxygen](2026-11-02-water-to-oxygen.md) on the Former's 3 ice → canister
- Date: 2026-11-06

## Context

The Former froze 3 ice into a canister. Ice is had only on a planet, so the craft sat in the Former's rows, unreachable, through the
whole time aboard, and the playtest of 2026-11-06 asked what it was for. The user decided to remove it: an electrolyser will fill
canisters later, from water.

## Decision

- **The craft is gone** from `Crafts` and the Former's rows. Canisters come from debris: supply pods, oxygen tanks, cargo holds and
  pod clusters (`Loot`). Ice still melts into water for the cistern.
- **Its worth is priced by hand** (`Market.Raw`, 117), what its craft came to, so the jetpack, the heavy helmet and the plasma
  cutter cost what they did, and two canisters still understand what takes one.
- **Saves: format 51.** The step drops a queued canister entry at any Former, the queue's work restarting when it was the first,
  and the craft's grant. The ice paid for it is not given back: a few units of a planet's ice, against a refund through
  inventories by hand in a JSON step.

## Deliberately not handled

- Filling canisters at the electrolyser is a later change; until then air aboard is what is salvaged and what the lifepod makes.
