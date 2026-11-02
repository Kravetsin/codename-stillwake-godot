# research (client): the tablet and the trade station's screen

The board of what the crew has found and the black market's rings ([board](../../../docs/plans/board.md), [tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md)). T takes the tablet out
anywhere. A trade station is worked as a crate is, through the items feature's panel.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `ResearchTablet.cs`  | the tablet: its tabs, the head and the strip below, T, the read timer     | yes   |
| `ITabletPage.cs`     | what the tablet asks of a page on a tab                                   | yes   |
| `BoardPage.cs`       | the first tab: the board of notes, threads and shapes                     | yes   |
| `TabletKey.cs`       | the tablet's key on the HUD and its lamp                                  | yes   |
| `RingsPage.cs`       | a tab of rings: a hub, nodes, what the strip says of what is picked       | yes   |
| `InkDraw.cs`         | draws the board's strokes                                                 | yes   |
| `StripParts.cs`      | what pages write with: the strip's controls, a thing's words and pictures | yes   |
| `StationScreens.cs`  | a trade station's screen, drawn over its picture                          | yes   |
| `CapsuleFlights.cs`  | a trade station's capsule going off behind the hull and coming back       | yes   |
| `logic/BoardBox.cs` | The part of the board the camera keeps in bounds | no |
| `logic/BoardFile.cs` | What this machine's player moved on a world's board, in its own file | no |
| `logic/BoardInk.cs` | What the board is written with: a stylus's strokes | no |
| `logic/BoardKeep.cs` | The board as this machine keeps it, for the world played | no |
| `logic/BoardPencil.cs` | How far a silhouette has come towards being understood | no |
| `logic/BoardPile.cs` | The unread entries of one showing of the board | no |
| `logic/BoardSeen.cs` | What is new on the board for this player | no |
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
  a click. It changes the world only by sending `BuyBlock`. A press picks only if the pointer did not move; only the strip's button buys.
- Words are set at the size they are seen and never scaled; further out a node keeps only its picture, and from far it is a plain dot coloured by its state, under its branch's name. What a node is, is said by the pointer: an item's card, or a blueprint's picture and words.
- The board reads only behind a key of its `Knowledge`, that module's version and the hero's grid's; positions are set only as the camera moves. It takes no way to send a command, so it cannot change the world. Blank till the first find. Its box only grows, and the first note stands in the middle of the view.
- A shape shows at a thread's end or once every part is noted, and a click adds none: the strip below lists what it takes and goes into. A note runs threads to its three closest to done; a shape with every part noted and no thread gets one from the part noted last, so nothing makeable is lost behind a plate's many uses.
- A left press on a thing on the board, a note or a shape, carries it once past the click's slack, and it stays where it is let go; anywhere else the press drags the board. A click still only picks. Each machine keeps its own arrangement, never sent: only what was moved, in `user://boards/<World.Id>.json`, written as the tablet closes and on every way out. A lost or bad file means things go back to their slots, a failed write stops nothing, and a world with id 0 (made before ids, or a dev run) keeps no file.
- A lamp in the metal of the tablet's key lights while a note is unread and the tablet is away ([tablet-key](../../../docs/decisions/2026-10-31-tablet-key.md)). What was read is kept in the same file. With nothing kept it starts as things stand at the first read, so a new world or a late guest has nothing new. Only notes count: more of a known item or a bought upgrade lights nothing.
- Unread notes wait beside the board as a pile ([the pile](../../../docs/decisions/2026-11-02-board-pile.md)): the tablet opens on the board with any unread, shows the top one large and the count, and draws nothing of the unread as a note. Only the player's click lands an entry, the whole card shrinking into its note and its thread drawing; nothing lands by itself, and "Take all" lands the rest at once. A flip counts at the click, so a close mid-pile resumes on the next one. Parts come before what they make, and an arrival joins under the top, which never changes. What landed keeps a hand-drawn loop until the tablet closes.
- A find with the board whole lands at once with its loop, the camera left alone. While reading, the board holds still, framing all that will stand on it, or follows the entry if that is too much for words to show; once a pile empties it opens to the whole view, and the first thing the closest shape lacks is written beside it; what was never had is not named. Strokes are the same for the same thing on every run and every frame. While reading, a click on the board does nothing, and the pile's controls take no keyboard focus: the hero still jumps under the open tablet.
- The hub and the strip show what has been sold over what is ordered, with no button, for an order fills by selling. The head's count of what is open shows on the market alone: an empty board starts with no sum to fill.
- An order met lights the key's lamp and a lamp on the market's tab until the market is chosen, tablet opened and closed or not. How many were seen is kept in the same file. The key reads them on its timer, for a sale moves no `Version`.
- A block is green once bought, edged in amber while it can be bought, dim while out of reach. The hub is violet while its order is open,
  and the rings its orders opened are lit. It opens on the hub the first time, and after that stays where it was left.
- What is picked ties to others by colour, known or not: teal what it takes, pink where it is made, sky what it goes into; a gate violet to the ring it opens. All else dims.
- A trade station's screen types lines while it holds what sells, and is dark while not. It is read every 100 ms and drawn
  unlit, for a screen gives its own light.
