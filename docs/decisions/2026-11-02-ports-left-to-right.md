# Ports read left to right

- Status: accepted
- Date: 2026-11-02
- Builds on [typed ports](2026-10-29-typed-ports.md)
- Supersedes, in part, [liquid oxygen](2026-10-30-liquid-oxygen.md) (the tank's output on its left) and
  [water to oxygen](2026-11-02-water-to-oxygen.md) (the electrolyser's water in on its right, the tank's input on its right)

## Context

Play-testing the water chain, the user found the ports hard to read: the tank gave on its left, the electrolyser took
on its right, the pod gave on its left. Which way a line flows had to be learnt machine by machine. The overlay's arrow
also stood after every socket (o >), into or out of the machine, so an input and an output looked alike at a glance.

## Decision

- **Every machine takes on its left and gives on its right**: an input in its footprint's left column, an output in
  its right one; a machine with only an output gives on its right. `ContentTests` holds every row of `Machines.All` to it.
  The tank (`(0, 2) In`, `(3, 2) Out`), the electrolyser (`(0, 5) In water`, `(3, 5) Out liquidOxygen`) and the lifepod
  (`(5, 5) Out air`) moved; the cistern, the vaporizer and the vent already kept it. A line reads left to right.
- **The overlay's arrow shows the way the fluid comes**: an input's head before its socket, pointing at it (> o); an
  output's after it, pointing away (o >). `Sockets.TipOf` places it.
- **Saves move with the ports**: format 45's step moves every span end at a moved port's old cell to its new one, the
  tank's and the electrolyser's swapped, so a world piped before the change is piped the same after it. The wreck's
  pod pipe is drawn from its new port.

## Consequences

- A tank's liquid now leaves on its right: the wreck and test maps drawn a character a 16 px cell reach that odd cell
  only through a pipe under the block past it, as the vaporizer's air already did.
- Deliberately not handled: mirroring a machine as it is placed. A line from right to left runs its pipe round the
  machine; nobody has asked for more.
