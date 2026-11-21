# research (client): the tablet and the trade station's screen

The board of what this player has found ([own-knowledge](../../../docs/decisions/2026-11-21-own-knowledge.md)) and the crew's black market ([board-islands](../../../docs/decisions/2026-11-18-board-islands.md), [the-board](../../../docs/decisions/2026-11-05-the-board.md), [tablet-tree](../../../docs/decisions/2026-10-30-tablet-tree.md), [market-grid](../../../docs/decisions/2026-11-03-market-grid.md)). T takes the tablet out
anywhere once paired, in the drone's corners ([drone-windows](../../../docs/decisions/2026-11-21-drone-windows.md)). A trade station is worked as a crate is, through the items feature's panel.

## Files

| File | Holds | Godot |
| --- | --- | --- |
| `ResearchTablet.cs` | the tablet: its two pages and the strip below, T, M, the read timer | yes |
| `ITabletPage.cs` | what the tablet asks of a page on a tab | yes |
| `BoardPage.cs` | the first tab: the district islands, the finds cards, the goal | yes |
| `BoardFind.cs` | the board's search | yes |
| `TabletKey.cs` | what the research and market keys on the edge tell, read on a timer | yes |
| `FindScanView.cs` | the drone scanning a find over the hero's head, its ember flying to the research key | yes |
| `FormPictures.cs` | a drone's form as a picture: its thread icon in ember, made once a size | yes |
| `MarketPage.cs` | the black market: what the mafia wants, tabs and a grid of goods | yes |
| `StripParts.cs` | what pages write with: the strip's controls, a thing's words and pictures | yes |
| `StationScreens.cs` | a trade station's screen, drawn over its picture | yes |
| `CapsuleFlights.cs` | a trade station's capsule flying off and back | yes |
| `logic/Cipher.cs` | The market's garbage | no |
| `logic/MarketReveal.cs` | How far each level's prices have deciphered | no |
| `logic/MarketGoods.cs` | The market's goods and what each tile says | no |
| `logic/MarketGrid.cs` | Where the wanted card, the tabs and the tiles lie | no |
| `logic/BoardFile.cs` | What this machine's player saw and pinned on a world's board, in its own file | no |
| `logic/BoardInk.cs` | A stylus's strokes, for the radar | no |
| `logic/BoardKeep.cs` | The board as this machine keeps it, for the world played | no |
| `logic/BoardPencil.cs` | How far a silhouette has come towards being understood | no |
| `logic/BoardPictures.cs` | The side a picture is baked at | no |
| `logic/BoardSearch.cs` | What the search finds | no |
| `logic/BoardSeen.cs` | What is new for this player: batches of finds, each with its cause | no |
| `logic/BoardLayout.cs` | Where every frame and district block stands, from Content alone | no |
| `logic/BoardDistricts.cs` | The districts in flow order, each with its things | no |
| `logic/BoardChain.cs` | A goal's chain down to raw, and what it still lacks | no |
| `logic/BoardThings.cs` | Every blueprint and craft as the board sees it | no |
| `logic/BoardView.cs` | What a frame shows: unknown, almost or known, and what it lacks | no |
| `logic/FindScan.cs` | The find scan's and a lesson's stages and times, a batch at a time | no |
| `logic/FormIcons.cs` | The thread icon each of the drone's forms is shown by | no |
| `logic/CapsulePath.cs` | Where a capsule is drawn along its flight | no |
| `logic/Research.En.cs` | Its player text (`En`) | no |
| `logic/Research.Ru.cs` | Russian | no |
| `logic/TechCamera.cs` | How the map lies in the view: dragged, zoomed, kept in bounds | no |
| `logic/TechLinks.cs` | How a thing ties to the others, worked out from the recipes | no |

## Guarantees

- The window reads the sim four times a second, built once and only restyled. It sends only `BuyBlock`, from the strip's button.
- The board reads only behind a key of `Knowledge.Version` and every open surface's grid version; a machine standing on any open surface counts. It takes no way to send a command. Frames are made once on open; a read restyles only a frame whose state changed.
- Every blueprint, craft, item and drone form has one frame in one of eight districts from the start (`BoardDistricts`, a test names one listed twice or left out); columns are steps from raw (`BoardLayout`), unlabelled. A frame is 3 by 3 cells of the background's 30 px grid on a 120 px pitch; blocks and lanes keep it. Nothing is dragged; a find never moves a frame.
- A frame is unknown («?», «ещё не открыто», no name, cannot be picked), almost (a silhouette, a bar of parts had, «не хватает: N» counting every part never had and the machine to stand, listed on the pointer; «можно получить» with nothing missing) or known. Market goods are tagged «рынок», never almost. Hover or a pick ties only what is direct, in four colours; "whole chain" lights it down to raw. Zoomed out the blocks veil their frames under the district names.
- Finds are cards beside the board: a read that made something known is a batch, its cause fixed then (obtained, else built, else opened); hovering never reshuffles one. A card shows what opened, what came closer (almost, more parts had than before) and its cause; each picture flies to its frame. The newest 40 are kept in `user://boards/<World.Id>.json` with what was seen and the pin; with nothing kept, all that is known is seen, so a late guest has nothing new.
- A pinned goal lights its chain and lists what it still lacks; it clears itself once known.
- **The «Спутник» district** (`companion`, [drone-lessons](../../../docs/decisions/2026-11-21-drone-lessons.md)) holds the drone's forms: a known one its thread icon (`FormPictures`), an unknown one a plain «?», never pencilled in, no hint or count.
- **The drone scans each find** ([find-scan](../../../docs/decisions/2026-11-21-find-scan.md)): an item first held shows its icon over the head, the scan sweeps it, it folds into an ember that flies to the research key, which flares as its number takes the batch; a batch's items one after another, 1.35 s each to the key. A form learned is a lesson first: the drone visits what taught it, fills the scanner's dots over it, shows the form, then its icon flies in, the key flaring longer. Only this machine draws it; with no drone a find lands at once; opening the tablet puts out finds, not lessons; what waited at a world's opening plays nothing.
- The research key shows from the first find, amber while a card is unread and the tablet away, the finds on it; the market's from the first credit or good bought, amber while anything on it can be bought, credits in gold on the right (hologram-keys). What was read is kept in the same file. With nothing kept, a new world or a late guest has nothing new.
- Its own search, top right ([board-search-related](../../../docs/decisions/2026-11-07-board-search-related.md)): what is known or almost, by word starts in either language, a slip forgiven; few named bring their ties, lit while listed.
- The market shows what the mafia wants next; every good in one grid, its price garbage until its level (mafia-reputation), a tile per kind of upgrade buying its cheapest step opened. A good bought is delivered (mafia-deliveries), its tile bought meanwhile; gear and the container stay buyable. It never scrolls (tested at 1280x720).
- Garbage is only a look: what the sim says is reached is picked and bought mid-decipher, picking clears it. Alike on every read (`Cipher`); its clock never touches the sim.
- Levels seen are one count (`BoardSeen.SeeOrders`), so lamp and decipher agree: what was new plays after the tablet fades in, or at once if it was up. Closing or leaving the tab shows it clear; a late guest or a lost file plays nothing.
- Levels met are read on a timer, for a sale moves no `Version` ([market-key](../../../docs/decisions/2026-11-03-market-key.md)); how many were seen is kept in the same file. T and its key open the board, M and its key the market; each switches from the other, puts away from its own.
- A trade station's screen types while it holds what sells; read every 100 ms, unlit.
