# Liquids in tanks

- Status: accepted
- Date: 2026-11-08

## Context

A slot held 20 canisters, 400 units of a hero's air against a helmet's 45, and 50 water bottles. Air never ran short on a
sortie or a planet, crates were the ship's oxygen store, and a cistern of 8 bottles was only a source for pipes. Ice
came one a vein or crystal, so the electrolyser's water was slow to gather.

## Decision

- **A few a slot:** a canister and a water bottle stack to 5, ice to 50, so water is not kept as ice either.
- **Tanks and cisterns hold the store:** a tank 50 canisters (`Air.TankCapacity` = 150 000), a cistern 50 bottles
  (`Water.CisternBottles`, 225 000, three hours of an electrolyser). More room only: oxygen still comes of canisters and
  electrolysis.
- **Ice is farmed:** an ice vein and an ice crystal give 4. The user asked for 3 to 5; nothing a wall or a plant gives is
  rolled, so the middle.

## Consequences

- A sortie's air takes bag room; a tank is where canisters are drawn (`DrawCanister`).
- A drone carries 5 a trip; a bay's fuel slot holds 5 canisters, 25 minutes of trips.
- A stack saved larger loads as it was, moves whole and is used from: no save step.

## Not done

- A rolled yield for veins and crystals.
- Bigger tanks or cisterns as machines of their own.
