# Skins: how a hero looks, apart from what it wears

- Status: accepted; amends [blade-trail](2026-11-10-blade-trail.md): an effect skin draws the trail in place of the haze
- Date: 2026-11-16

## Context

A player could not change how their hero looks without changing what it wears, and so what it does; and the game had no
way to sell a look later as Steam DLC. The design was decided with the user, built on `feat/skins` and played before it
shipped. The user picked the first art on pick pages: four body skins painted raw, and two weapon effects, «Пламя» and
«Разлом». The first effects tried were the arc's charge recoloured; the user turned them down for effects of their own.

## Decision

- **Four kinds of skin:** `head`, `suit`, `cape` and `effect` (a weapon effect). A skin is always a whole design, never a
  recolour, drawn on the same rig and joints as the item it covers.
- **The sim keeps ids, nothing else.** `PlayersState.Skins` holds `PeerSkins(Peer, Ids)` in peer order: saved, hashed, added
  empty, so no format bump. `ChooseSkins(Ids)`, handled by Crew as `ChooseLook` is, replaces the sender's whole set at once
  (`IPlayersOps.Skin`), with a hero or none yet. It is never refused: the sim knows no skin and checks no id. A skin sits
  beside `Look`, not inside it: a look is printed by the pod, a skin applies at once. `IPlayersQueries.SkinsOf(peer)` reads it.
- **The catalogue is the client's** (`crew/logic/Skins.cs`): a `Skin` row is an id, its kind and its DLC app id, none
  for a free one. The id is also the picture id the gear is drawn by.
- **One lookup.** Every figure of a peer's hero (own, others', seated, bodies) is dressed through
  `Suits.DressedAs(items, players, peer)`, giving `Dressed(Helmet, Body, Cape, Effect)`. Helmet, body and cape are picture
  ids: the chosen skin while its equipment slot holds an item, else the item's own, none for an empty slot. An unknown id,
  or one of another kind, draws the item's own; the first known id of a kind wins. A bare head stays bare under a helmet skin.
- **Drawn by picture id.** A body skin's parts are `hero_<id>_<part>` and its card `skins/<id>.png`. A helmet's lamp
  (`LampMounts`), scanner (`ScannerInk`) and fit (`BodyFit`) are tables keyed by picture id, so a helmet skin is rows,
  not code; a cape's four pictures are `hero_<id>`, `_lining`, `_hood`, `_collar`, worn through `Figure.CapeWorn`.
- **A weapon effect is a drawer of the blade's trail.** On a struck blade in hand (fire axe, sledgehammer, pickaxe, spear)
  the effect (`BladeFx`: `FireFx`, `RiftFx`; drawn by `BladeFxView`; numbers in gc/`SkinEffects`) draws the swing's
  `BladeTrail` in place of the haze, and lives on the blade at rest, on its lip (`EdgeLip`, round `HeldLook.EdgeBox`). With no
  blade in hand nothing shows. A fitted arc module wins: its charge and the haze draw as before, the effect not at all.
- **Ownership is asked in one place**, `platform/logic/Owned`: a skin with no app id is owned everywhere; a DLC's in a debug
  build (all of them, unless `--own-none`), never in a release build until GodotSteam answers, since any other answer gives
  paid skins away in builds already shipped. Every skin so far carries the placeholder app id `0`, so the locked state
  exists from the start.
- **Art ships in every build.** Owning gates only the pick; a DLC is never a depot of pictures, so every peer can draw every
  skin.
- **The client sends what it owns of what it kept** (`Owned.Sendable`): one a kind, the empty set too, on every join and
  every pick. Picks are kept in `Settings` (`[skins]`, one key a kind); a dev run keeps none.
- **The Appearance tab**: a wrist key on the equipment's left edge (items' `LooksButton`) turns its screen to crew's
  `AppearanceView`, a slot a kind in rows `[head]`, `[suit, cape]`, `[effect]`. A slot opens its kind's strip, "none" and
  every skin, the unowned shown locked so the tab advertises the DLC; a kept skin not owned on this PC shows locked too.

## Consequences

- A new skin is a catalogue row, its art and its names; a helmet skin adds its table rows. Next users named: a jetpack plume
  (a `back` kind), effects on guns (fed the muzzle, not the edge), skins earned in play (a second source inside `Owned`).
- Until GodotSteam is in, a release build can pick no skin.

## Deliberately not handled

- Slots for the mount, back and belt; effects on guns, the drill, the grinder and the cutter; inventory icons, item cards,
  the look editor and catchers' gear stay the items' own. No purchase inside the game, no store link.
- A friend's skinned helmet hides which helmet it is: accepted.
- A tampered client can send an unowned skin, or any number of ids; everyone sees it. Co-op among friends (principle 6).
- A refund mid-session, or a save brought to a PC that lacks the DLC, keeps the skin shown until that player's next join.
- A peer that left keeps its skins, and its bodies show its current ones, as with looks.
- The body skins shipped have no `_lit` pictures or pip of their own: a regen suit under one does not show its mode's lights.
