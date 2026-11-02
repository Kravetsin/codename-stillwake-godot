# Tanks in a line

- Status: accepted
- Date: 2026-11-02
- Builds on [liquid oxygen](2026-10-30-liquid-oxygen.md) and [ports left to right](2026-11-02-ports-left-to-right.md)

## Context

Play-testing, the user piped one tank's output to another's input and expected the liquid to move: tanks in a line should
fill the last one, as water runs downhill. A tank only held what a canister or an electrolyser put in it.

## Decision

- **A tank pours on.** Each tick, before the vaporizers make, each tank holding liquid, in id order, gives up to
  `Air.TankFlow` (20, a canister in some 22 seconds) to the tanks whose input is on its output's network, in id order, as
  much as each has room for. A later tank pours on in the same tick, so a line A → B → C fills C first, then B. Nothing is
  made or lost: liquid only moves, and a shut valve splits the network, so it stops the line.
- **Cisterns pour the same way** (`Water.CisternFlow`, a tank's flow), in the water module before the electrolysers draw.
  An electrolyser takes from the cisterns behind it that hold water, skipping an empty one.
- **A vaporizer draws only the tanks whose output is on its input's network.** Before, any port counted, so a tank whose
  input shared that line was drawn as well; with lines it would empty a tank meant to be filled.
- **No new state:** the liquid is the tanks' `Tanks` as before, so saves keep their format.
- **The pointer tells** (the shared `MachineTips` in `game/shared`): a tank its liquid of its capacity, a vaporizer or the
  pod how fast it makes air (`IAtmosphereQueries.MakesAt`, a second to a tenth), the cistern its bottles and the electrolyser what it does, as before.

## Consequences

- Deliberately not handled: a pump's power. Liquid moves by itself, as a tank's own pressure would push it.
- A loop of tanks passes liquid round and round at the flow, evening out nothing; nobody builds one on purpose.
- A tank's flow is shared among the tanks it feeds in id order, not evenly: the first fills, then the next.
