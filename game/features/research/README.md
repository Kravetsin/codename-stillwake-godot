# research (client): the tablet and the trade station's screen

The board of what the crew has found and the black market ([board](../../../docs/plans/board.md), [tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md), [market-decrypt](../../../docs/decisions/2026-11-02-market-decrypt.md)). T takes the tablet out
anywhere. A trade station is worked as a crate is, through the items feature's panel.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `ResearchTablet.cs`  | the tablet: its tabs, the head and the strip below, T, the read timer     | yes   |
| `ITabletPage.cs`     | what the tablet asks of a page on a tab                                   | yes   |
| `BoardPage.cs`       | the first tab: the board of notes, threads and shapes                     | yes   |
| `TabletKey.cs`       | the tablet's key, on its edge, its cap a lens that lights                 | yes   |
| `HeroBulb.cs`        | the bulb over this player's hero as a note starts waiting                 | yes   |
| `MarketPage.cs`      | the black market: a row per group, its order and its goods as cards       | yes   |
| `MarketFaults.cs`    | the hack's marks over the screen while the market shows                   | yes   |
| `InkDraw.cs`         | draws the board's strokes                                                 | yes   |
| `StripParts.cs`      | what pages write with: the strip's controls, a thing's words and pictures | yes   |
| `StationScreens.cs`  | a trade station's screen, drawn over its picture                          | yes   |
| `CapsuleFlights.cs`  | a trade station's capsule going off behind the hull and coming back       | yes   |
| `logic/Cipher.cs` | The market's garbage, and how its lines break | no |
| `logic/MarketReveal.cs` | How far each row of the market has deciphered | no |
| `logic/MarketRows.cs` | Where the market's rows and cards lie, and a card's state | no |
| `logic/BoardBox.cs` | The part of the board the camera keeps in bounds | no |
| `logic/BoardFile.cs` | What this machine's player moved on a world's board, in its own file | no |
| `logic/BoardInk.cs` | What the board is written with: a stylus's strokes | no |
| `logic/BoardKeep.cs` | The board as this machine keeps it, for the world played | no |
| `logic/KeyGlow.cs` | How lit the lens on the tablet's key is | no |
| `logic/BoardPencil.cs` | How far a silhouette has come towards being understood | no |
| `logic/BoardPile.cs` | The unread entries of one showing of the board | no |
| `logic/BoardSeen.cs` | What is new on the board for this player | no |
| `logic/BoardSlots.cs` | Where everything on the board stands, from what was found in what order | no |
| `logic/BoardThings.cs` | Every blueprint and craft as the board sees it | no |
| `logic/BoardThreads.cs` | What a note's threads run to | no |
| `logic/BoardView.cs` | What the board shows: a note, a silhouette or nothing, and its threads | no |
| `logic/BulbSignal.cs` | The bulb over this player's hero | no |
| `logic/CapsulePath.cs` | Where a capsule is drawn along its flight | no |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/Research.Ru.cs` | Russian | no |
| `logic/TechCamera.cs` | How the map lies in the view: dragged, zoomed, kept in bounds | no |
| `logic/TechLinks.cs` | How a thing ties to the others, worked out from the recipes | no |

## Guarantees

- The window reads the sim four times a second. It is built once and only restyled, for a rebuild under the pointer would eat
  a click. It changes the world only by sending `BuyBlock`. Only the strip's button buys.
- The board reads only behind a key of its `Knowledge`, that module's version and the hero's grid's; positions are set only as the camera moves. It takes no way to send a command, so it cannot change the world. Blank till the first find. Its box only grows, and the first note stands in the middle of the view.
- A shape shows at a thread's end or once every part is noted, and a click adds none: the strip below lists what it takes and goes into. A note runs threads to its three closest to done; a shape with every part noted and no thread gets one from the part noted last, so nothing makeable is lost behind a plate's many uses.
- A left press on a thing on the board, a note or a shape, carries it once past the click's slack, and it stays where it is let go; anywhere else the press drags the board. A click still only picks. Each machine keeps its own arrangement, never sent: only what was moved, in `user://boards/<World.Id>.json`, written as the tablet closes and on every way out. A lost or bad file sends things back to their slots, a failed write stops nothing, and a world with id 0 keeps no file.
- The board has edges ([board-room](../../../docs/decisions/2026-11-02-board-room.md)): no slot or carried thing passes them. What the market opens holds a hidden slot by its first part noted and inks in there when bought.
- A bulb over this player's hero says a note has started waiting, once, about 2.5 s: it heats, overheats and bursts, the glass flying ([bulb-signal](../../../docs/decisions/2026-11-02-bulb-signal.md)). Only this machine draws it, and it is only ever one: a find while it burns makes it burn on, a find after the burst adds nothing, opening the tablet puts it out, and what waited when a world was opened lights nothing.
- The lens on the tablet's key lights while a note is unread and the tablet is away ([key-lens](../../../docs/decisions/2026-11-02-key-lens.md)). What was read is kept in the same file. With nothing kept it starts as things stand at the first read, so a new world or a late guest has nothing new.
- Unread notes wait beside the board as a pile ([the pile](../../../docs/decisions/2026-11-02-board-pile.md)): the tablet opens on the board with any unread, shows the top one large and the count, and draws nothing of the unread as a note. Only the player's click lands an entry, the whole card shrinking into its note and its thread drawing; nothing lands by itself, and "Take all" lands the rest at once. A flip counts at the click: a close mid-pile resumes on the next one. Parts come before what they make; an arrival joins under the top. What landed keeps a hand-drawn loop until the player presses it, through a close and a restart, kept in the same file ([board-thoughts](../../../docs/decisions/2026-11-02-board-thoughts.md)); its threads stand out only until the tablet closes.
- A find with the board whole lands at once with its loop, the camera left alone. While reading, the board holds still, framing all that will stand on it, or follows the entry if that is too much for words to show; once a pile empties it opens to the whole view. Every shape says the first thing it lacks inside its box, always; what was never had is not named. A pick ties only what is direct: what it takes, where it is made, what takes it or comes out of it. Strokes are the same for the same thing on every run and every frame. While reading, a click on the board does nothing, and the pile's controls take no keyboard focus.
- The market never scrolls: at most 6 rows of 5 goods, held by a Content test. A head shows what is sold over what is ordered. A click picks a card; a sealed one picks nothing and says so.
- Garbage is only a look: what the sim says is reached can be picked and bought mid-decipher, and picking it clears it. The same on every read (`Cipher`); its clock never touches the sim.
- Orders seen are one count (`BoardSeen.SeeOrders`), so lamp and decipher agree: what was new plays after the tablet's rise, or at once if it was up. Closing or leaving the tab shows it clear; a late guest or a lost file plays nothing.
- An order met lights the key and the market tab's lamp until the market is chosen; the count is kept in the same file. The key reads them on its timer: a sale moves no `Version`.
- A trade station's screen types lines while it holds what sells, and is dark while not. It is read every 100 ms and drawn
  unlit, for a screen gives its own light.
