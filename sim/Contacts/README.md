# Contacts

## Files

| File | Purpose |
| --- | --- |
| `Contacts.cs` | The radar's contacts (2026-11-08-radar-contacts): places that come up on the ship's orbit now and then and go after a while |

The places the radar shows ([radar-contacts](../../docs/decisions/2026-11-08-radar-contacts.md)): now and then one comes up
somewhere on the ship's orbit and goes again after a while. A global, as `players` and `knowledge`: one per world, made by
the ship's registry and shared by every surface's, saved in the ship's `slices` after `knowledge`. The radar watches space,
whichever surface a hero stands on. Nothing acts on a contact yet: the jump will.

- **Depends on:** nothing.
- **State** (`ContactsState`, members internal, saved): `NextIn` — ticks till the next contact, 0 before the first is
  counted down; `NextId` — the id the next takes; `Contacts` — `Contact(Id, Kind, Offset, Left)` for every contact up, in
  id order.
- **Tunables:** `c/Contacts.cs` — `First`, `EveryMin`, `EveryMax`, `Lasts`, `Reach`, `Nearest`, `Kinds`.

## Rules

- **Each tick, in simulation:** every contact's `Left` goes down by one, and one reaching 0 is gone. Then the countdown:
  at 0 it starts at `First` (a quarter of an hour, fixed, so a short run never draws from the world's `Rng`); counted
  down to 0, a contact comes up and the next `NextIn` is drawn evenly in `EveryMin..EveryMax`.
- **A contact drawn:** its kind evenly of `Kinds` (`wreck` only, now), its side evenly, its distance evenly in
  `Nearest..Reach`; `Offset` is the distance signed, minus left of the ship. It stays `Lasts` ticks, its id `NextId++`.
- **One at a time:** `Lasts` is no more than `EveryMin`, and a contact goes before the next is added on the same tick, so
  two drawn contacts are never up at once.
- **`ContactNow(kind, offset)`** (dev) adds one at once, `Lasts` ticks to stay, the countdown untouched. Refused with
  `contacts.unknownKind` for a kind not in `Kinds`, `contacts.outOfReach` for `|offset| > Reach`.
- An older save (format 56) loads with no contact up and the countdown starting over: the first a quarter of an hour on.

## API

- `All` — the contacts up, in id order, read-only. `Find(id)` — one, or null once it is gone. `NextIn` — ticks till the next.
- Command: `ContactNow`. Its `Fuzz` draws it now and then, refused kinds and offsets past reach too.

## Invariants

- Every contact up has `1 ≤ Left ≤ Lasts` and `|Offset| ≤ Reach`; a drawn one has `|Offset| ≥ Nearest`.
- With no `ContactNow`, at most one contact is up (`EveryContactIsInReachWithTimeLeftAndTheDrawnOnesComeOneAtATime`).

## Deliberately not handled

- **The jump.** A contact is only seen and named; flying there comes next and reads a contact's id and kind.
- **Contacts come with or without a radar.** The sim does not know which programs a computer has (f/computer); a player
  who builds the radar late finds the contact up already there.
- **No overlap check.** Drawn ones are one at a time; a `ContactNow` beside another is a developer's own doing.
- **Contacts do not move.** The ship and the place drift round the planet together; the offset stays.
- **One kind.** Debris clusters and events join `Kinds` when they have a place to fly to.
