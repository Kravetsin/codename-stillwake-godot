# Plan: Food, hunger and the canister's cooldown

Status: designed with the user 2026-11-12, nothing built. Stage A is playable alone; stage B follows it.

## Why

The game grew in length (craft chains) but not in breadth. Food is a semi-mandatory side loop, as in Valheim: a crew
can play on without cooking, but plays better and builds faster fed. It also ends the endless canister: a hero with a
stack of canisters breathes them back to back and never runs dry, so air, the only health, stops mattering short of the
farthest ring.

**Decided with the user** (2026-11-12):
- **The shell/body rule.** Gear owns capacities and protection; food owns the body's rates. The canister upgrade
  (`canister1`, bought on the board) goes; more air a canister now comes from food.
- **A hard canister cooldown**, per hero, its mark by the air meter (canisters are breathed with R).
- **Three food slots.** The same food is refused except in its last minute, which refreshes it. Death clears them: the
  pod prints a new person.
- **Hunger = every slot empty**, no meter. A hungry hero never dies of it: building is slower and sprinting costs more
  air. Walking, rest breath, hits and the jetpack are untouched (a walking slowdown reads as lag).
- **The pod prints a hero fed** with pod paste: hunger kept off, no rates.
- **Body rates, the player picks.** A food gives one or more rates; nobody prescribes a "fight" or "long outing" set.
  This stage: Calm, Deep, Work. Named next: legs (run and jump), strike (melee), harvest yield (clashes with the plasma
  cutter's loot bonus; one owner chosen then).
- **Named dishes, not free mixing.** A dish's ingredients are its recipe, its cost; its rates are its own, set by hand.
  Water and fat have no rates: they are not eaten raw. One rule ties them: a dish is stronger than any raw food in its
  recipe.
- **Space food comes from debris**, endless as the stream: a drifting fridge, a vending machine in derelicts' quarters,
  rations in supply pods. Seeds may be eaten as a snack: eat or plant is the player's choice.
- **The farm grows fruit, not biomass.** Fruit is eaten, cooked, or crushed at the crusher into biomass for fuel:
  fruit → biomass → fuel reads better to players than fruit straight into the fermenter. A seed comes back by chance when
  its fruit is eaten raw or crushed.
- **The board ignores quantities**: one of each ingredient obtained and a machine that makes it (PR 540), dishes too.
- **No save care:** release breaks saves (fields added with none-defaults; no bump, no refund).

## Words

A **food** is anything eaten: a raw food (an item eaten as it is) or a **dish** (cooked at the galley). A **meal** is
a food in one of a hero's slots, with the ticks it has left. **Pod paste** is a meal the pod gives, never an item.

## Rules

**Hero fields**, saved, omitted at defaults like `Hurts`: `CanisterIn` (ticks until the next canister), `Owed` (breath
thousandths), `Worked` (work thousandths), `Meals` (up to three `Meal(string Food, int Left)`, a record with
properties, never a tuple: the save writes no fields). Time is kept as ticks left, never as a `Clock` tick: each
surface has its own clock, and a hero carries these across `Depart` and `Arrive`. Crew counts `CanisterIn` and every
`Left` down each tick, for every hero, creative too, and drops a meal at 0; nothing reads an expired meal.

**Rates** (`Content.Diet`) in thousandths, summed over a hero's meals, then capped. A hungry hero has no meals and so no
rates; the Hungry column is its base instead.

| Rate | Scales | Base | Hungry | Cap of food |
| --- | --- | --- | --- | --- |
| Calm | air a breath costs: base × (1000 − calm)/1000 | rest 1000, sprint 2000 (thousandths of a unit) | sprint 2500 | 400 |
| Deep | a canister's air: 20 × (1000 + deep)/1000 | 20 units | – | 800 (→ 36) |
| Work | progress per `Work` command: (base + work)/1000 ticks | 1000 | 750 | 500 |

**Raw foods** (`Content.Foods`; `Lasts` in ticks at 20 a second):

| Food | Where from | Stage | Lasts | Calm | Deep | Work |
| --- | --- | --- | --- | --- | --- | --- |
| pod paste | printed; not an item | A | 24 000 | – | – | – |
| seed | supply pods, grazing creatures | A | 6 000 | – | – | – |
| ration | supply pods, fridge, vending machine | A | 18 000 | 50 | – | 300 |
| canned meat | fridge, vending machine | A | 18 000 | – | 300 | – |
| fruit | the seed's crop | B | 12 000 | – | – | 100 |
| glowberry | the spore's crop | B | 12 000 | 120 | – | – |

**Dishes** (stage B, the galley; each lasts 18 000):

| Dish | Recipe | Calm | Deep | Work |
| --- | --- | --- | --- | --- |
| Builder's porridge | 2 fruit, 1 ration | 50 | – | 400 |
| Spacer's hash | 1 canned meat, 2 fruit | – | 300 | 200 |
| Glow broth | 2 glowberry, 1 water | 300 | – | – |
| Roast | 2 fat | – | 500 | – |
| Hunter's stew | 1 fat, 1 glowberry, 1 water | 150 | 300 | – |

No loadout fills every cap: roast + stew reach deep 800 with a slot free; porridge + ration reach work 500 capped;
broth + stew + roast give calm 400 and deep 800 but no work. Every number is a first guess for the playtest.

1. **Breath** (words as in `sim/Crew/README.md` › Rules). Each breath adds its base × (1000 − calm)/1000, rounded
   down, to `Owed`; `Owed / 1000` whole units replace `breathes` in the pod term, `want`, `net` and the regen suit's
   give-back, the remainder stays. **Thin air** (`Rules.ThinAir`) drops an off-beat shortfall as today; such a breath
   also leaves `Owed` as it was. So in vacuum `Owed` moves only on the beat and a calm hero pays 60% of today's there,
   while in an aired room every breath draws as today. (Accruing every breath would alias: calm 400 pays units
   0,1,0,1,1, period 5, against a beat every 5th breath.)
2. **Deep.** An applied `Refill` gives `CanisterRefill × (1000 + deep)/1000`, rounded down. `ICrewQueries.CanisterRefill`
   gives way to `RefillOf(peer)`, which the client's air cue reads.
3. **Work speed.** The `Work` command: one per builder a tick, two with a power driver (Building README). Building
   already holds `ICrewOps`; a new op `Works(peer)` adds the hero's work rate to `Worked` once per command and returns
   the whole ticks it is worth, 0, 1 or 2. Building applies the first as today; a second never finishes the plan nor
   starts an unpaid part, and is lost if it would, so the last-tick checks (`building.heroInside`, `notEmpty`,
   `holdsMachine`, `SeatProblem`) keep running on a tick of their own. A `Work` pays at most one part. No hero, a hero
   passed out, or a creative one: 1 tick, `Worked` untouched. Repair and taking apart are `Work`. Hungry, every fourth
   command moves nothing.
4. **The cooldown.** `Refill` is refused in this order: `crew.noHero` (a passed-out hero too), `crew.suitFull`,
   `crew.canisterCooling` (while `CanisterIn > 0`), `items.notEnough`. Only an applied `Refill` sets
   `CanisterIn = Crew.CanisterCooldown` (500 ticks, 25 s). Creative skips it.
5. **Eating.** `Eat(Item)` takes one and adds `Meal(food, Lasts)`, or refreshes the meal of that food: its slot stays,
   `Left` back to `Lasts`. Refused: `crew.noHero` (a passed-out hero too), `crew.notFood`, `items.notEnough` (never in
   creative), `crew.sameFood` (while its `Left > RefreshWithin`, 1 200), `crew.mealsFull`. Eating a fruit draws its seed
   from the world's `Rng` in the apply only, never in the check the client calls (stage B). A seed is eaten by using it
   from the hotbar or inventory; it is planted by putting it in a grow capsule, as today.
6. **Printing.** Passing out clears `Meals`, `Owed`, `Worked` and `CanisterIn`. A printed hero (on `Enter` and on
   waking) gets paste. A creative hero may eat, its meals tick down, but it is never hungry and its rates are not read,
   as its air is endless; switching creative off keeps what it has.
7. **Space food.** Item `ration` joins the supply pod's loot (`OneIn: 2`, step A2). The fridge (A4) is a new debris
   kind beside the supply pod, weight 10 in the ship tier and 12 in each far tier, in `NetCatches`; loot rations 1–3,
   canned meat 0–2. The vending machine (A4) is a `Vessels.Salvage` row whose piece is the fridge, in derelicts'
   quarters, as `salvagePod` is the supply pod. Ration, canned meat, fruit, glowberry and every dish go into
   `Market.Unsold`, so the crush-to-fuel economy has no market bypass.
8. **Fruits** (stage B). The grow capsule's yields become fruit and `Returns` goes: a seed grows 4 fruit, a spore 3
   glowberries. Fruit's `Process` is `crush`, short (`SmeltTicks` ≈ 40): 1 biomass and its seed back as a byproduct.
   Fruit: a seed one time in 5 (0.8 a seed, as today). Glowberry: two rolls, a spore one time in 3 and another one time
   in 30 (1.1 a spore, as today). Eating either raw draws the same rolls; cooking draws none. Fuel per seed stays 4
   biomass. Biomass stays the wild drop of creatures and cave caps.
9. **The galley** (stage B). A new unpowered queued station like the workbench
   ([0057](../decisions/0057-stations-one-model-for-every-crafting-machine.md)): one `Machines` entry, one `Stations`
   row, queue 4, built at the grow capsule's tier. The board understands a dish with one of each ingredient and a
   galley standing.

## Steps, one PR each, in order

Art is picked on published Artifacts as CLAUDE.md says, one page per subject, before the step that draws it: the
canister mark before A1; ration, canned meat and pod paste before A2; the fridge and the vending machine before A4;
fruit and glowberry before B1; the five dishes and the galley before B2.

### A1: the canister's cooldown, the upgrade removed
Rule 4; `CanisterIn` and its countdown; death clears it. `canister1` leaves `Upgrades.All` and the board's tabs in
`Content.Research` with `CanisterUp` and `Upgrades.Canister`; `CanisterRefill` becomes the constant, read through
`RefillOf(peer)`. Fix `ResearchTests`, `SuitsTests`, `LastBreathTests`, `FieldTests`, `JetpackTests`,
`ContentTests.Hero`, `game.tests/AirCuesTests`. Client: the canister mark by the air meter, greyed with a sweep,
blinking on R while cooling; a canister on the hotbar greys too.

### A2: meals, hunger and the breath rates
`Content.Diet`, `Content.Foods`, the `ration` item and its supply-pod loot; `Meals`, `Owed`, `Eat`, paste on print;
rules 1, 2, 5, 6. Scenario heroes are pasted on entry; a test that outruns 24 000 ticks with a hero feeds it
explicitly, and hunger is tested on purpose. Client: three slots by the air meter (icon flat, pulsing in the last
minute, empty when hungry), eating by using a food from the hotbar or inventory, `Report.EatOf` guarding a second send
as `Report.RefillOf` does, rates on hover, i18n En and Ru. ADR `food-and-the-body`: the shell/body rule, rates, hunger,
cooldown, superseding the upgrade part of [the heavy helmet](../decisions/2026-11-03-the-heavy-helmet.md). Crew README,
concept, glossary.
As built: `Content.Diet` is named so for `Render.Body`; canned meat came in A2, in supply pods at
`OneIn: 4` till A4 brings the fridge; the HUD slots are a stand-in (`MealSlots`) till the gauges' redesign places food ([hud-vitals](hud-vitals.md), H2).

### A3: work speed
`Worked`, `ICrewOps.Works`, rule 3 in Building's `ApplyWork`. Building README.
As built: a repair takes both ticks of a `Work` worth two, its last mending, since it has no part to pay nor last-tick check.

### A4: the fridge, the vending machine, canned meat, seeds eaten
The rest of rule 7, canned meat, the seed as a snack. Debris README.

### B1: fruit
Rule 8: fruit and glowberry items, the capsule's yields, the crusher's byproducts, the seed draw on eating. ADR
`fruit-not-biomass` amending [0060](../decisions/0060-the-farm-a-grow-capsule-is-a-fed-station.md). Fix the farm and
biofuel tests that read biomass off a capsule.
As built: eating draws any raw food's `Byproducts`, given or dropped at the hero; fruit and glowberries are hand-priced 20
as biomass is, and unsold. "A cooked one never does" is tested with the galley in B2.

### B2: the galley and its dishes
Rule 9 and the dish table. The galley panel is the workbench's.

## Tests

- **A1:** refused then accepted after 500 ticks; per hero; creative skips; death clears it; refusal order; a refused
  `Refill` starts nothing; a hero cooling who lands on a planet and comes back waits exactly the ticks left.
- **A2:** calm 400 in vacuum costs 3 over 5 breaths, `Owed` carrying; a full-air hero in an aired room draws 60%;
  calm 400 under `ThinAir = 5` in vacuum pays 60% of today's, and an aired room there draws as today; hungry sprint
  costs 5 over 2 breaths, a pasted hero today's; deep 800 gives 36; every refusal of `Eat`; the same food refreshed in
  its last minute keeps its slot; a printed hero holds only paste; death clears and wakes with paste; a meal crosses a
  landing with its ticks left; a hero holding a meal saves and loads alike.
- **A3:** a hungry 120-tick blueprint takes 160 `Work`s, on a ration 93; a two-tick `Work` stops at an unpaid part;
  a two-tick `Work` on a wall's last tick with a hero inside finishes nothing; repair and taking apart scale alike; a
  `Work` from a peer with no hero is worth 1.
- **A4:** a fridge and a vending machine give rations and canned meat; the net catches a fridge; a seed ends hunger
  for its 6 000 ticks and adds no rate.
- **B1:** the capsule grows fruit; the crusher gives biomass and its seed by chance, and its outputs take every
  entry's most; eating a fruit can give its seed; a cooked one never does.
- **B2:** the galley cooks each dish; the board understands porridge with fruit, a ration and a galley, not roast.
- **Invariants (FsCheck):** no two applied canisters closer than the cooldown; at most three meals, all different,
  none with `Left` ≤ 0; air within 0..capacity, `CapacityOf` the same at any rates; every rate within its cap; every
  dish stronger than each raw food of its recipe (no rate lower, the sum higher); over many grown seeds and spores,
  fuel and seeds back average today's.
- **Fuzz:** `Eat` drawn with every refusal; `Refill` drawn soon after another, so `crew.canisterCooling` is reached.

## Not done

- Death or damage from hunger; a hunger meter; walking slower.
- Free mixing or computed dishes.
- Legs, strike and harvest yield: named next rates.
- Black-market recipes.
- The jetpack's cost, rest breath, base air: playtest tuning.
- Feeding another hero.
- Deep breath on tank canisters or drone trips.

**Accepted:**
- Water cooked is oxygen not made.
- Paste takes a slot.
- In stage A deep breath comes only from canned meat.
- A cooked fruit gives no seed back.
- With canned meat and a stack of canisters a resting hero breathes indefinitely; canisters stay scarce
  ([scarce-oxygen](../decisions/2026-11-07-scarce-oxygen.md)).
- A fed hero's extra `Work` tick is lost where it would finish a plan or start a part.
- Leave and rejoin wipes meals and the cooldown and prints paste.
