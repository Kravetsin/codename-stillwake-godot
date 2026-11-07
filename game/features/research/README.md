# research (client): the tablet and the trade station's screen

The board of what the crew has found and the black market ([the-board](../../../docs/decisions/2026-11-05-the-board.md), [tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md), [market-grid](../../../docs/decisions/2026-11-03-market-grid.md)). T takes the tablet out
anywhere. A trade station is worked as a crate is, through the items feature's panel.

## Files

| File | Holds | Godot |
| --- | --- | --- |
| `ResearchTablet.cs` | the tablet: its two pages and the strip below, T, M, the read timer | yes |
| `ITabletPage.cs` | what the tablet asks of a page on a tab | yes |
| `BoardPage.cs` | the first tab: the board of notes, threads and shapes | yes |
| `BoardFind.cs` | the board's search | yes |
| `TabletKey.cs` | what the research and market keys on the edge tell, read on a timer | yes |
| `ScreenBulb.cs` | the bulb at the screen's top, and the find flying down into the key | yes |
| `MarketPage.cs` | the black market: the order being filled, tabs and a grid of goods | yes |
| `InkDraw.cs` | draws the board's strokes | yes |
| `StripParts.cs` | what pages write with: the strip's controls, a thing's words and pictures | yes |
| `StationScreens.cs` | a trade station's screen, drawn over its picture | yes |
| `CapsuleFlights.cs` | a trade station's capsule going off behind the hull and coming back | yes |
| `logic/Cipher.cs` | The market's garbage | no |
| `logic/MarketReveal.cs` | How far each order's decipher has gone | no |
| `logic/MarketGoods.cs` | The market's goods and what each tile says | no |
| `logic/MarketGrid.cs` | Where the order, the tabs and the tiles lie | no |
| `logic/BoardBox.cs` | The part of the board the camera keeps in bounds | no |
| `logic/BoardFile.cs` | What this machine's player moved on a world's board, in its own file | no |
| `logic/BoardInk.cs` | What the board is written with: a stylus's strokes | no |
| `logic/BoardKeep.cs` | The board as this machine keeps it, for the world played | no |
| `logic/BoardPencil.cs` | How far a silhouette has come towards being understood | no |
| `logic/BoardPile.cs` | The unread entries of one showing of the board | no |
| `logic/BoardSearch.cs` | What the search finds | no |
| `logic/BoardSeen.cs` | What is new on the board for this player | no |
| `logic/BoardSlots.cs` | Where everything on the board stands, from what was found in what order | no |
| `logic/BoardThings.cs` | Every blueprint and craft as the board sees it | no |
| `logic/BoardThreads.cs` | What a note's threads run to | no |
| `logic/BoardView.cs` | What the board shows: a note, a silhouette or nothing, and its threads | no |
| `logic/BulbSignal.cs` | The bulb at the top of this player's screen | no |
| `logic/FindFlight.cs` | The find's way down the research key's beam | no |
| `logic/CapsulePath.cs` | Where a capsule is drawn along its flight | no |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/Research.Ru.cs` | Russian | no |
| `logic/TechCamera.cs` | How the map lies in the view: dragged, zoomed, kept in bounds | no |
| `logic/TechLinks.cs` | How a thing ties to the others, worked out from the recipes | no |

## Guarantees

- The window reads the sim four times a second. It is built once and only restyled. It changes the world only by sending `BuyBlock`. Only the strip's button buys.
- The board reads only behind a key of its `Knowledge`, that module's version and the hero's grid's; positions are set only as the camera moves. It takes no way to send a command, so it cannot change the world. Blank till the first find. Its box only grows, and the first note stands in the middle of the view.
- A shape shows at a thread's end or once every part is noted, and a click adds none: the strip below lists what it takes, goes into and, for a blueprint, its build menu tab. A note runs threads to its three closest to done; a shape with every part noted and no thread gets one from the part noted last.
- A left press on a note or a shape carries it once past the click's slack, and it stays where it is let go; anywhere else the press drags the board. A click still only picks. Each machine keeps its own arrangement, never sent: only what was moved, in `user://boards/<World.Id>.json`, written as the tablet closes and on every way out. A lost or bad file sends things back to their slots, a failed write stops nothing, and a world with id 0 keeps no file.
- The board has edges ([board-room](../../../docs/decisions/2026-11-02-board-room.md)): no slot or carried thing passes them. What the market opens holds a hidden slot by its first part noted and inks in there when bought.
- A bulb at the screen's top says a note has started waiting, once ([hologram-keys](../../../docs/decisions/2026-11-05-hologram-keys.md)): it heats and bursts, the find that lit it hatches and slides down the research key's beam, and the key's number takes what it carried on landing. Only this machine draws it, and only one: a find while it burns rides with it, one after the burst goes onto the number, opening the tablet puts it out, and what waited when a world was opened lights nothing.
- The research key shows from the first note, amber while one is unread and the tablet away, the finds waiting on it; the market's from the first credit or good bought, amber while anything on it can be bought, the credits in a gold readout on the right (hologram-keys). What was read is kept in the same file. With nothing kept, a new world or a late guest has nothing new.
- Its own search, top right ([board-search](../../../docs/decisions/2026-11-07-board-search.md)): what the board shows, by name.
- Unread notes wait beside the board as a pile ([the pile](../../../docs/decisions/2026-11-02-board-pile.md)): the tablet opens on the board with any unread, shows the top one large and the count, and draws nothing of the unread as a note. Only a click lands an entry, shrinking into its note; "Take all" lands the rest. A flip counts at the click: a close mid-pile resumes on the next one. Parts come before what they make; an arrival joins under the top. What landed keeps a hand-drawn loop until pressed, through a close and a restart, kept in the same file ([board-thoughts](../../../docs/decisions/2026-11-02-board-thoughts.md)); its threads stand out only until the tablet closes.
- A find with the board whole lands at once with its loop, the camera left alone. While reading, the board holds still, framing all that will stand on it, or follows the entry if that is too much for words to show; once a pile empties it opens to the whole view. Every shape says the first thing it lacks inside its box, always; what was never had is not named. A pick ties only what is direct: what it takes or is smelted of, where it is made, what takes it or comes out of it; "only found" is said of what nothing makes (`TechLinks.OriginOf`). Strokes are the same on every run and frame, and a canvas draws its ink in one call (`BoardInk.Mesh`). While reading, a click on the board does nothing, and the pile's controls take no keyboard focus.
- The market shows only the order being filled; what orders opened lies in one grid, a tile per kind of upgrade buying its cheapest step opened. It never scrolls: a test fails once the goods outgrow 1280x720.
- Garbage is only a look: what the sim says is reached can be picked and bought mid-decipher, and picking it clears it. The same on every read (`Cipher`); its clock never touches the sim.
- Orders seen are one count (`BoardSeen.SeeOrders`), so lamp and decipher agree: what was new plays after the tablet's rise, or at once if it was up. Closing or leaving the tab shows it clear; a late guest or a lost file plays nothing.
- Orders met are read on a timer, for a sale moves no `Version` ([market-key](../../../docs/decisions/2026-11-03-market-key.md)); how many were seen is kept in the same file. T and its key open the board, M and its key the market; each goes to its page from the other and puts the tablet away from its own.
- A trade station's screen types lines while it holds what sells, and is dark while not. It is read every 100 ms and drawn
  unlit.
