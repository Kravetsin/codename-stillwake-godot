# The tech tree lives on the tablet

- Status: accepted
- Date: 2026-10-30
- Supersedes: the tech station of [trade-and-tech](2026-10-30-trade-and-tech.md) and the window of [tech-graph](2026-10-30-tech-graph.md)

## Context

The tree opened at a machine, in a window that scrolled. A player had to walk to it to see their balance or where to go next,
and could never see the whole tree at once. The station also had nothing of its own to do: it held no items and made nothing,
and a player who took it apart could only buy once they had built it again.

## Decision

- **No tech station.** `BuyBlock(Block)` and `Deposit(Gate)` name no machine and work anywhere, on any surface. The fiction is
  an order placed by radio on the black market; goods still leave physically, through a trade station.
- **The tree is on the tablet.** T takes it out, nearly the whole window, on the frame the build menu and Handbook use. The map
  is dragged and zoomed about the pointer. All the way out the whole tree is in view, seen as pictures and colours; a node's
  words show from a zoom where they can be read.
- **A press is a click only if the pointer did not move:** a drag starting on a node buys nothing.
- **The head of the tablet says where things stand:** credits, how much of the tree is open, and what comes next (the first
  block that can be paid for, or else the first node within reach), which brings it into view when clicked.
- **The wreck keeps a station in the same spot:** a trade station. Selling is what a player must learn first and the tree is
  bought with what it earns.

## Deliberately not done

- **A deposit takes from the bag anywhere,** so a gate's materials do not travel. Asking for a trade station in reach would tie
  the tablet to a place again. To be reconsidered if the playtest finds it odd.
- **No balance on the always-visible HUD:** what is always on screen speaks without words. The tablet and the trade station's
  panel show it.
- **No migration:** worlds older than format 39 no longer load, as with the last format. Nothing has been released on this tree.
