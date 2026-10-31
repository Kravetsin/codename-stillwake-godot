# research (client): the tree on the tablet and the trade station's screen

The tree as the player walks it ([tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md), [tech-rings](../../../docs/decisions/2026-10-30-tech-rings.md)). T takes it out
anywhere. A trade station is worked as a crate is, through the items feature's panel.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `ResearchTablet.cs`  | the tablet: its tabs, the head and the strip below, T, the read timer     | yes   |
| `ITabletPage.cs`     | what the tablet asks of a page on a tab                                   | yes   |
| `BoardPage.cs`       | the first tab: the board of notes, threads and shapes                     | yes   |
| `RingsPage.cs`       | a tab of rings: a hub, nodes, what the strip says of what is picked       | yes   |
| `StripParts.cs`      | what pages write with: the strip's controls, a thing's words and pictures | yes   |
| `StationScreens.cs`  | a trade station's screen, drawn over its picture                          | yes   |
| `CapsuleFlights.cs`  | a trade station's capsule going off behind the hull and coming back       | yes   |
| `logic/BoardBox.cs` | The part of the board the camera keeps in bounds | no |
| `logic/BoardFile.cs` | What this machine's player moved on a world's board, in its own file | no |
| `logic/BoardPencil.cs` | How far a silhouette has come towards being understood | no |
| `logic/BoardSlots.cs` | Where everything on the board stands, from what was found in what order | no |
| `logic/BoardThings.cs` | Every blueprint and craft as the board sees it | no |
| `logic/BoardThreads.cs` | What a note's threads run to | no |
| `logic/BoardView.cs` | What the board shows: a note, a silhouette or nothing, and its threads | no |
| `logic/CapsulePath.cs` | Where a capsule is drawn along its flight | no |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/TechCamera.cs` | How the map lies in the view: dragged, zoomed, kept in bounds | no |
| `logic/TechLinks.cs` | How a thing ties to the others, worked out from the recipes | no |
| `logic/TechRings.cs` | Where each block stands on a tab, from its ring and branch | no |

## Guarantees

- The window reads the sim four times a second. It is built once and only restyled, for a rebuild under the pointer would eat
  a click. It changes the world only by sending `BuyBlock` and `Deposit`. A press picks only if the pointer did not move; only the strip's button buys or deposits.
- Words are set at the size they are seen and never scaled; further out a node keeps only its picture, and from far it is a plain dot coloured by its state, under its branch's name. What a node is, is said by the pointer: an item's card, or a blueprint's picture and words.
- The board reads only behind a key of its `Knowledge`, that module's version and the hero's grid's; positions are set only as the camera moves. It takes no way to send a command, so it cannot change the world. Blank till the first find. Its box only grows, and the first note stands in the middle of the view.
- A left press on a thing on the board, a note or a shape, carries it once past the click's slack, and it stays where it is let go; anywhere else the press drags the board. A click still only picks. Each machine keeps its own arrangement, never sent: only what was moved, in `user://boards/<World.Id>.json`, written as the tablet closes and on every way out. A lost or bad file means things go back to their slots, a failed write stops nothing, and a world with id 0 (made before ids, or a dev run) keeps no file.
- The hub and the strip show what the bag holds over what is still needed.
- A block is green once bought, edged in amber while it can be bought, dim while out of reach. The hub is violet while its gate takes
  a deposit, and the rings its gates opened are lit. It opens on the hub the first time, and after that stays where it was left.
- What is picked ties to others by colour, known or not: teal what it takes, pink where it is made, sky what it goes into; a gate violet to the ring it opens. All else dims.
- A trade station's screen types lines while it holds what sells, and is dark while not. It is read every 100 ms and drawn
  unlit, for a screen gives its own light.
