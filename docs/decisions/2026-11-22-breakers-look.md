# Breakers are robots of painted pieces, a pack all of one kind

- Status: accepted
- Date: 2026-11-22

## Context

The breakers ([companion-fights](2026-11-22-companion-fights.md)) were small sentries, and the user found they had the
sentry's look and habits. A first page of five bodies drawn after real things (a saw, pliers, a train, an auger, a porter)
was rejected whole: a creature here is an invented robot, never an object or a held tool.

## Decision

- **The bodies.** Twelve invented robots from one GPT sheet, nine kept for a still pick
  ([page](https://claude.ai/artifact/PtGn9TdnjP4Z4VzMJ4eae3)), then each cut into pieces GPT painted in our style and used
  raw, 4 px a world px (`tools/pick-kit/pages/breaker-rigs`: sheets, cuts, one rig engine). On the live page
  ([page](https://claude.ai/artifact/XAwovCwqsn7zgRPXsLpPrJ), `breaker-packs.js`) the user kept five: Латаный, Саван,
  Сросток, Лик, Клубок. Picked in chat; the page's doc `picks/breakerPacks` is empty.
- **One kind a pack**, never mixed. Zones of space may give each kind its own later; the far field has «Клубок» (Knot).
- **One rig in the game.** `export.js` writes a kind's pictures and its bone table (`BreakerInk.cs`); `BreakerRig` hangs
  each piece on its parent's joint, `IBreakerMoves` turns and shifts them (`KnotMoves`), `CreatureLooks.Breakers` names a
  look's kind. Another kind is an export and a line there.
- **Nests at work, not asleep.** A breaker would be busy: two of a nest pry at its piece's rim (grab, three pries, a
  yank, picking by turns), the rest unseen under it. All of a nest on view read as a tumour on the wreck (the user). As
  the pack wakes the pair leaves the rim and the rest fly out from under the piece one after another, 70 ms apart.
- **It turns whole** to its hero, never mirrored: a mirror reads as a sheet of paper turning (played).
- **Drawn small:** the Knot is 10 px across, a tick wanting to latch on (played), inside its 2×2-cell box.
- **Two packs at a hero:** a hero's keep box holds 16 of a nest's kind, so a second pack wakes while one chases him;
  a third waits. One nest ignoring a hero beside it looked wrong; a whole field following him was the other edge.
- **Killed, it comes apart** into its own pieces, drifting from the hero over 1.7 s, with sparks and dust; no pixel crumble.

## Rejected

- Bodies after real things; all nine bodies mixed in each pack; nests asleep or all on view.

## Deliberately not handled

- **The box is bigger than the ball:** a blow at the box's edge lands where no picture is. A one-cell box breaks the
  flight and knock numbers worked out for two; played as it is.
- The sim lets a pack out at once: those still coming out from under the piece can be struck before they are seen.
- Sounds are the sentry's stand-ins; habits are the swarm's. Patched, Shroud, Twin and Visage wait for their zones.
- The pieces are AI-painted (GPT), to be stated in Steam's AI disclosure.
