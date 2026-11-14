# Liquid volumes

- Status: accepted; supersedes the bottle of [scarce-oxygen](2026-11-07-scarce-oxygen.md) and the cistern of [liquids-in-tanks](2026-11-08-liquids-in-tanks.md)
- Date: 2026-11-09

## Context

A player could not tell how much liquid a canister, a bottle or a stack of them held: no card said, a canister was 3 litres
and a bottle 4.5, a tank 150 and a cistern 225, and machines read to the millilitre ("87.000/150.000 l"). A canister
breathed gave the suit 20 units of air, a second amount in no unit the player saw.

## Decision

- **A bottle is a canister:** `Water.Bottle` = `Air.Canister`, 3 litres, and a cistern holds 50 of them, 150 litres, a
  tank's. The user's pick: one container, one volume. Ice now gives two thirds of the oxygen it gave.
- **A card says its litres:** a canister's and a bottle's card "3 l", a stack's of more "3 l · ×4 = 12 l" (`ItemCards.VolumeOf`).
- **Litres to the hundredth, rounded down** (`Litres.Of`): "87.99/150.00 l". A tank and a cistern also say the whole canisters
  or bottles they hold, on the pointer's tip and the draw key: "· 29 canisters".
- **A canister's card is a bottle's:** its name, kind and litres, no key: the user took the RMB cap off it, and with it
  the suit's air it gives.

## Consequences

- No save step: amounts stay in millilitres. A cistern saved over 150 litres keeps its water, refuses pours, takes none from
  its line and shows full until it drains below.
- Text on the slot itself stays a count: a slot speaks without words, its litres are on the card.

## Not done

- Biofuel keeps its count: the user left it as it is.
- The suit's air a canister breathed gives is not written anywhere: the wrist gauge shows it filling.
- Rates (leaks, a vaporizer's making) keep their tenth of a millilitre: a wall leaks 0.0015 l a minute.
