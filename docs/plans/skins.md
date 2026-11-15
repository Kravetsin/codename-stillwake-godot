# Design: Skins (cosmetic appearance slots)

Status: designed and decided with the user 2026-11-15; nothing built. Branch `feat/skins`. Once shipped, what stays true
moves into an ADR (`skins`), `sim/Players/README.md`, `game/features/crew/README.md` and `game/features/items/README.md`.

## Problem
A player cannot change how their hero looks without changing what the hero wears, and so what it does. The game also has no way to sell a look later as Steam DLC.

## Player experience
1. The player opens the inventory and presses a wrist key beside the equipment block. The block switches to **Appearance**, which has four slots: helmet, body, cape and weapon effect.
2. Each slot shows the chosen skin's icon, or the slot's ghost when none is chosen.
3. The player clicks the body slot. A strip opens with every body skin and a "none" entry; the ones they do not own show locked and cannot be picked.
4. They pick one. Their hero behind the panel is redrawn in the skin straight away, and so is that hero on every friend's screen.
5. They pick a weapon effect and take the fire axe in hand. The blade fizzes at rest and tears bolts off as it swings. With no struck item in hand, nothing shows.
6. They take the helmet off. The head is bare, even though a helmet skin is chosen.

When it goes wrong: the player chose a DLC skin on a PC that owns it, then joins from a PC that does not. On that PC the strip shows the skin locked, the client does not send it, and the hero is drawn in its item's own pictures.

## Out of scope
- Appearance slots for the mount, back and belt. The belt is not drawn at all.
- Effects on guns, the drill, the grinder and the cutter.
- Inventory icons, item cards, the look editor, and the gear of the creatures called catchers.
- Buying anything inside the game, and any store link.
- Recolours. A skin is always a whole design.
- Skins earned in play.
- Integrating GodotSteam.

## Scale
- World: no cells. A figure is rebuilt only when a pick changes.
- Players: up to 4 or more, each with up to 4 picks.
- Frequency: one command per join, plus one each time a player picks.

## Approach
**Sim side (Players and Crew).**
- `PlayersState` gains `Skins: List<PeerSkins(Peer, IReadOnlyList<string> Ids)>`, kept in peer order.
- The sim gets one new query, `IPlayersQueries.SkinsOf(peer)`, and one new operation, `IPlayersOps.Skin(peer, ids)`.
- The command is `ChooseSkins(IReadOnlyList<string> Ids)`, handled by Crew as `ChooseLook` is. It replaces the sender's whole set and has no refusal reasons, because the sim checks no game rule here.
- `Skins` sits beside `Looks`, not inside `Look`. A look is printed by the pod; a skin applies at once.
- `Skins` is saved and hashed. It is added with a `[]` initializer meaning "none yet", so the save format needs no bump.
- No events.
- `Skin` stores a copy (`[.. ids]`), never the command's list. A peer that left keeps its skins, and bodies show them, as with
  looks. A first `ChooseSkins` before any hero is recorded.
- Terms: an **equipment slot** holds an item (`head`, `suit`, `cape`); a **skin kind** is `head`, `suit`, `cape` or `effect`.
  `Ids` is a set of skin ids, its order meaning nothing to the client: `DressedAs` finds each id's kind through the
  catalogue, the first id of a kind wins. The client sends at most one per kind, in the fixed order head, suit, cape, effect.

**Catalogue.** `crew/logic/Skins.cs` is a client table. Each row has a skin id, its slot kind (`head`, `suit`, `cape` or `effect`) and its DLC app id. It lives on the client like `HeroLooks`, because the sim never reads it.

**The one lookup.**
- Today the lookup is `Suits.WornBy(IItemsQueries items, int peer)` and returns `(Helmet, Body)`.
- It becomes `Suits.DressedAs(items, players, peer)` and returns `Dressed(Helmet, Body, Cape, Effect)`.
- Helmet, body and cape are picture ids: the chosen skin while its equipment slot holds an item, otherwise the item's own, and null when the slot is empty. An unknown id, or one of the wrong kind, falls back to the item.
- Effect is the chosen effect skin, or null.
- `Figure.Wear` takes `Dressed`.
- **The four callers already hold `IPlayersQueries`:**
  - `HeroView.cs:555` (`m.Players`)
  - `HeroView.Others.cs:53` (`m.Players`)
  - `BodyView.cs:108` (`m.Players`)
  - `SeatedHeroes.cs:84` (`_players`)
- `WearsCape` stays the switch that shows the cape, set at `HeroView.cs:562` and `HeroView.Others.cs:93`. The cape's pictures come from `Dressed.Cape`.
- The dev sheets, `HunterView` and `LookEditor` pass a `Dressed` made from items, with no skin.

**What each kind needs to draw.**
- **Body.** Nothing new. `PictureOf` already names a body's pictures `{id}_{part}`, its lights `hero_{id}_{part}_lit` and its arcs and pip by the same id. The bands show only while `SuitGlow` reports the suit's mode on (`Figure.cs:1096`), so a body skin must ship the regen lights, arcs and pip. Then a regen suit under the skin still shows its mode.
- **Helmet.** Small refactors so a skin brings its own fit and mounts:
  - `BodyFit.Fits` is a dictionary keyed by id, so a skin adds one row. That gives its fit, cut, collar, seal and fade, as the catchers' rows already do.
  - `HeadLampArt.OnOf` is an if-chain. It becomes a table of lamp positions keyed by picture id.
  - `Figure.ShowScanner`'s switch (`Figure.cs:691`) and the fixed `ScannerInk.On`/`OnHeavy`/`OnQuilted` become one table keyed by picture id: the scanner's picture and where it sits. `tools/anime-art/subjects/scanner.js` exports a row per helmet and per skin.
  - The goggles' `hero_{id}_lit` is already keyed by id.
- **Cape.** `CapeView`'s four static textures (outside, lining, hood, collar) are picked by the cape's picture id, with `cape` as the item's own.
- **Effect.** Like the arc spear's charge, always on while held: the blade fizzes at rest and tears bolts off as it moves (user's pick).
  - **Step 0, one blade effect first (the user's rule: unify before the effect).** Checked in the code:
    - The blade haze is already one path: `Figure` fills `_edge` from `HeldLook.EdgePoint` for every struck item and `_air.Follow` draws it. Axe, sledgehammer and pickaxe have no code of their own. The spear is the one exception, by design: no trail during its thrust, none with the arc (`Figure.cs:1619`).
    - The arc's charge is NOT general yet. `ArcBurst` and `EffectArea` know no weapon, but they are reached only through `SpearLight.Burst`, behind `Wields == Spear` (`Figure.cs:1631-1633`), and the area comes only from a fitted module's crackle box (`HeldParts.Crackles`).
    - So step 0 pulls `SpearLight.Burst` out into `BladeCharge`: an `ArcBurst` on an area, its sparks, torn bolts, root glows and blade glow on the held stage, with its tint as a parameter. `Figure` runs it for any struck item in hand. `SpearLight` keeps only the spear's own: the thrust's lens and eddies and the arc run down the cable. The arc spear becomes `BladeCharge`'s first user with its crackle area, unchanged on screen.
    - The area for any blade with no module: `HeldArt.EdgeArea(HeldLook)`, which calls `HeldArt.AreaOf` over a box round the item's edge (`EdgeFrom`, `EdgeTo`, `Bow`), so no weapon needs data of its own. If one reads wrong on its frame, it gets an optional box in its `HeldLook` (S-4): data, not code.
    - `Figure` runs `BladeCharge` whenever a struck item is in hand and has a charge to draw: its fitted module's (the arc spear) or the effect skin's. With neither, nothing.
  - The effect skin is `BladeCharge`'s second user: its own tint and numbers (a record in `gc/`), on the edge area of every struck item: fire axe, sledgehammer, pickaxe, spear. Guns, the drill, the grinder and the cutter draw by other paths and are left out.
  - The rule "a skin draws only while its equipment slot holds an item" becomes, for an effect, "only while a struck item is in hand".
  - It draws on top of the haze. It is off while a fitted module draws its own charge (the arc spear), so a fitted module stays visible.
- **Bracelet.** The tracker bracelet is its own sprite, so it stays over any skin.

**Ownership.**
- `platform/Owned.Has(skin)` is the only place that answers "what does this player own".
- A debug build (`OS.IsDebugBuild()`) owns everything.
- A release build asks Steam's DLC check once GodotSteam is in. Until then it owns nothing from DLC: any other answer would give paid skins away in builds already shipped.
- A skin with no app id is free and owned everywhere. The first body skin and effect carry a placeholder app id (`0`, "not on sale"), so the locked state exists from the start.
- The dev flag `--own-none` makes a debug build own no DLC skin, so the user sees the locked strip in the room.
- **Art ships in every build**; ownership gates the pick only. A DLC is never a depot holding pictures, so every peer can draw every skin.
- What the Appearance slot shows for a kept skin the player does not own (another PC, a refund): the locked icon, so the player sees why the hero wears the item's own pictures.

**Settings.** `Settings.Skins()` and `KeepSkins` follow `Look()` and `KeepLook` (`Settings.cs:129-147`): a `skins` section with one key per kind (`head`, `suit`, `cape`, `effect`) and the skin id, or empty for none. A dev run keeps nothing, as with looks.

**The tab.**
- It lives in the wrist's metal like the equipment block, as an electronic screen, and is switched by a wrist key.
- It has one `SlotBox` per kind, in rows: `[head]`, `[suit, cape]`, `[effect]`.
- Clicking a slot opens a strip of that kind's skins and "none": owned ones pickable, the rest locked.

**Next users and their seams.** None of these is built here.
- **Jetpack plume:** a `back` row in the catalogue, read through `Dressed`.
- **Gun effects:** an effect class fed the muzzle instead of the edge.
- **Skins earned in play:** a second source inside `Owned`.

## Determinism and co-op
- Picks reach the sim only through `ChooseSkins`.
- Ids are strings, kept as sent and compared ordinally. No arithmetic is involved.
- The client sends the kept picks it owns (`kept ∩ owned`; ids no longer in the catalogue drop out) on every join, the empty set too, next to `ChooseLook`, and again on every change.
- Builds must match to join (the version and `Protocol.Fingerprint`), so an older peer never meets `ChooseSkins`.
- A player who joins mid-session gets everyone's skins in the snapshot.
- Ownership is checked only on the client, and the host trusts it (principle 6).
- A skin applies at once (confirmed).

## Trade-offs accepted
- **A friend's helmet is skinned.** You cannot tell which helmet it is. The user accepted this.
- **A tampered client sends an unowned skin.** Everyone sees it. We accept this because the game is co-op among friends.
- **DLC is refunded mid-session, or a save carries a DLC skin to a host who lacks it.** The skin shows until that player's next join, which re-sends its set. Friends who own nothing still see it: art ships to all.
- **`Ids` is not capped.** The sim validates no payload shape (CLAUDE.md); our client sends at most four. A tampered client's long list is the trusted-friends case.
- **A guest's pick while the host stalls** applies late, on its stamped tick, as `ChooseLook` does.
- **A peer's skin id is unknown to this build.** The item's own pictures are drawn, as a wrapped look index is.
- **Bodies.** They show the peer's current skins, as with looks.
- **A skin's shape.** It is drawn on the same rig and joints and cannot change the body's proportions.
- **Release builds before GodotSteam.** No DLC skin can be chosen.

## Tests
- **Scenarios:**
  - A second `ChooseSkins` replaces the first set; one before any hero is recorded.
  - An older save without `Skins` loads with none; a saved set loads the same.
  - Crew's `Fuzz` draws `ChooseSkins` from: the empty set, a known id, an unknown id, two of one kind, a wrong-kind id.
- **Client logic tests:**
  - With the helmet off, `DressedAs` returns no helmet even with a helmet skin chosen.
  - `BladeCharge` runs for every struck item in hand, never with the arc fitted under an effect skin; the arc spear draws as before.
  - Catalogue completeness:
    - Every skin has its icon and En/Ru names.
    - A body skin has every part picture, the regen lights, arcs and pip.
    - A helmet skin has a `BodyFit` row, a lamp position and a scanner row.
    - A cape skin has its four pictures.
  - `Owned`: a debug build owns everything, and a release build owns no DLC skin.
- **Frames:** `DressSheet` shows each skin, and a swing run shows the effect.

## Cost
- **Modules changed:** Players (slice, query, operation) and Crew (the command and `Fuzz`). On the client:
  - `crew`: `DressedAs`, `Dressed`, `Skins`, `Figure.Wear`, the effect seam, the cape pictures, the helmet tables
  - `shared`: `HeadLampArt`, `ScannerInk` and its export
  - `items`: the Appearance tab
  - `platform`: `Owned` and `Settings`
- **New module:** none.
- **Content:**
  - One body skin: 6 part pictures, 6 lights, arcs, pip and an icon.
  - One stroke effect and its icon.
  - The user picks both on Artifact pages. Nothing is added to `sim/Content`.
- **Estimated diff:**
  - sim and its tests: about 150 lines
  - lookup, `Figure` and callers: about 120
  - helmet and cape refactors: about 100
  - step 0 (`BladeCharge` out of `SpearLight`) plus the effect skin: about 250
  - tab: about 180
  - `Owned`, `Settings` and client tests: about 180
  - Total: about 980 lines, not counting art.

## Decided with the user
- Four slots: helmet, body, cape, weapon effect. The DLC seam goes in now. A skin applies at once.
- Unowned skins show in the strip locked: the tab advertises the DLC. They cannot be picked.
- First art: one body skin and one weapon effect. Helmet and cape skins come later, through the same slots.
- The effect draws on top of the haze; the arc module wins over it.
- The effect is always on while held, like the arc's charge; and step 0 unifies the blade charge before the effect is built.

## Tasks

Each task leaves `main` green and the game playable. S-0 is a refactor nobody plays: it goes straight to ship as its own
PR. S-1…S-5 are one try on `feat/skins`, played by the user, then shipped together in S-6.

### S-0 — One blade charge (refactor, own PR) ✅ PR 611
- Move `SpearLight.Burst` and its fields (`_burst`, `_sparks`, `_torn`, `_roots`, the glow constants) into
  `game/render/BladeCharge.cs`: `Step(elapsedMs, held, area, tint, stage)`, where `held` maps a point of the held picture into the world (`Figure.HeldAt`, as `SpearLight` gets it now) and the tint (`BoltTint`, flare colours) is a
  parameter. `SpearLight` keeps the lens, the eddies and the cable's run, and calls nothing of the charge.
- `Figure` (`Figure.cs:1631-1633`) runs `BladeCharge` for any struck item in hand with a charge area; today only the arc spear has one
  (`PartsOf(item)` with crackles), so nothing on screen changes.
- Built as above; `HeldArt.EdgeArea` moved to S-4, its first user, so main carries no dead code.
- Frame: the arc spear thrust and at rest, against `main`'s — identical by eye.
- Done: filtered tests green, frame compared, PR merged on green.

### S-1 — Skins in Players (sim) ✅ on feat/skins
- `sim/Players/Players.cs`: `PeerSkins(int Peer, IReadOnlyList<string> Ids)`; `PlayersState.Skins` (`[JsonInclude]`,
  `= []`); `IPlayersQueries.SkinsOf(peer)` (empty for a peer with none); `IPlayersOps.Skin(peer, ids)` replacing the set,
  inserted in peer order. Hashed with the slice as `Looks` is.
- `sim/Crew/Crew.cs`: `ChooseSkins(IReadOnlyList<string> Ids)`, `Handles<ChooseSkins>(_ => null, c => _players.Skin(c.From, c.Ids))`
  next to `ChooseLook` (`Crew.cs:234`); `Fuzz` draws it with ids from a small fixed list.
- Tests (`sim/Tests`, beside `LooksTests`): a second `ChooseSkins` replaces the first; an older save with no `Skins` loads
  with none; two peers' sets kept apart. Generic suites cover save/load and desync.

### S-2 — The client's catalogue, lookup and ownership ✅ on feat/skins (`Figure.Wear` keeps its strings: they are picture ids already)
- `game/features/crew/logic/Skins.cs`: rows `(Id, Kind, DlcAppId?)`, kinds `head`, `suit`, `cape`, `effect`; En/Ru names in
  `logic/Skins.En.cs`, `.Ru.cs`.
- `game/platform/Owned.cs`: `Has(skinId)`: a row with no app id, or a debug build (`OS.IsDebugBuild()`) → owned; a
  release build with an app id → not owned until GodotSteam answers; `--own-none` (in `DevModes`) → a debug build owns no DLC skin. The first two skins carry the placeholder app id `0`. Godot-free part in `platform/logic/`.
- `game/platform/Settings.cs`: `Skins()` / `KeepSkins(...)` beside `Look()` / `KeepLook` (`Settings.cs:129-147`), a
  `skins` section, one key per kind.
- Send: `Main.cs:366` sends `ChooseSkins(kept ∩ owned)` right after `ChooseLook`, and again whenever the tab changes a pick.
- `Suits.WornBy` → `Suits.DressedAs(items, players, peer)` returning `Dressed(Helmet, Body, Cape, Effect)`: helmet, body,
  cape are picture ids (the skin of that kind while its slot holds an item, else the item, null when empty); an unknown
  or wrong-kind id falls back to the item. `Figure.Wear` takes `Dressed`. Callers: `HeroView.cs:555`,
  `HeroView.Others.cs:53`, `BodyView.cs:108`, `SeatedHeroes.cs:84`; `DressSheet`, `HunterView`, `LookEditor` build one
  from items.
- Tests (`game.tests`): helmet off → no helmet with a helmet skin chosen; unknown or wrong-kind id → the item; two clients with the same `Skins` dress a hero alike; `Owned` debug, `--own-none`, release;
  what is sent is kept ∩ owned, the empty set too, on every join.

### S-3 — Helmets and capes drawn by picture id ✅ on feat/skins (lamp mounts in `shared/logic/LampMounts.cs`; `Figure.CapeWorn` replaces `WearsCape`)
- `game/shared/HeadLampArt.cs` `OnOf`: an if-chain → a table keyed by picture id.
- `game/shared/logic/ScannerInk.cs` and `Figure.ShowScanner` (`Figure.cs:691`): the fixed `On`/`OnHeavy`/`OnQuilted` → one
  table keyed by picture id (picture, mount point), written by `tools/anime-art/subjects/scanner.js`.
- `CapeView`: its four textures (`hero_cape`, `_lining`, hood, collar) picked by the cape's picture id; `cape` the item's own.
- No skin of these kinds ships yet: the change is proven by the current helmets and cape drawing as on `main`
  (`DressSheet` frame) and a catalogue test, run on a test-only helmet and cape row, that every such row has its table rows.

### S-4 — The effect skin
- First add `HeldArt.EdgeArea(HeldLook)`: the picture's drawn pixels in a box round `EdgeFrom`–`EdgeTo` bowed by `Bow`, its margin in `gc/`; test that every struck item's box holds its edge points.
- `Dressed.Effect` set → `Figure` runs `BladeCharge` with the skin's look (`gc/SkinEffects.cs`: tint, fizz and bolt
  numbers as data) on `HeldArt.EdgeArea(item)` for the fire axe, sledgehammer, pickaxe and spear, always while in hand.
- Off while the item has a fitted charge of its own (the arc spear): the module wins.
- The look itself is picked by the user on an Artifact page (CLAUDE.md, Art picks): several really different charges
  drawn on a canvas at game scale on the axe and the spear, at rest and swinging; the pick → the `gc/` record.
- Test: with the arc fitted the skin draws nothing; with no effect skin nothing new draws.

### S-5 — The Appearance tab and the body skin
- `game/features/items`: a wrist key beside the equipment block switches it to Appearance: `SlotBox` per kind in rows
  `[head]`, `[suit, cape]`, `[effect]`; a click opens the strip (the kind's skins and "none", unowned locked); a pick keeps
  it in `Settings` and sends `ChooseSkins`. i18n keys in `Items.En.cs`/`.Ru.cs`.
- The first body skin: candidates on an Artifact pick page (ink and watercolour, `tools/anime-art/README.md`, designed
  whole, the bracelet kept); the pick exported through `tools/anime-art` as `{id}_{part}`, `hero_{id}_{part}_lit`, arcs,
  pip and icon. Catalogue test: every body skin has every part, light, arcs, pip, icon and names.
- The room: the plain game; the user takes off and puts on the suit and helmet, picks skins, swings the axe and spear, and
  joins a second client to see the other side.

### S-6 — Ship
- Only after the user's yes on S-1…S-5: full `dotnet test Stillwake.sln`, format check, the ADR `skins`, READMEs of
  Players, Crew (sim), crew, items, platform, `docs/map.md` and `docs/glossary.md` (скин / skin), this plan deleted, PR. (The header's list is the short one; this is the full one.)

## Deliberately not handled
- Everything in "Out of scope" and "Trade-offs accepted" above.
- GodotSteam itself: until it lands, a DLC skin is pickable only in debug builds.
- A store link from a locked skin.
