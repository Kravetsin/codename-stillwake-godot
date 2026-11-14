# Food and gear cards list what they guarantee, and food sells

- Status: accepted
- Date: 2026-11-14

## Context

Hovering food or gear showed only a line, «Паёк · Еда · ПКМ»; the cape and the helmet lamp had one large number. The HUD's
meal slot gave plain text lines. A player could not see what a ration does, how much air a helmet holds, or what the
jetpack costs, without trying them. Food was also unsold, though rations and tins pile up in fridges.

## Decision

- **Food and gear get the tool cards' rows** ([tool-card-rows](2026-11-07-tool-card-rows.md)): a picture in its window,
  the name and kind, sign rows under group titles, the key cap with words, and for food the price.
- **A card states only what the item guarantees:** capacities, rates a second, resistances in percent, durations and
  price. It never projects: no «воздуха на 45 с», no «батареи хватит на 2:00», no breaths. The player never sees a breath,
  so air is in O₂ units, the units the HUD shows; the HUD's air tip says them too, «Шлем: 45 O₂».
- **Food** (coral edge `#a8705f`, kind `#f0997a`), under «Эффект»: «Расход на дыхание −N%» (calm), «Скорость работы +N%»
  (work), «Баллон даёт больше +N%» (deep), from `Diet`'s thousandths rounded down to whole percent, then «Длительность
  m:ss». A food with no rates, the seed, has a dimmed row with its sign struck, «Эффект · только утоляет голод». Then
  «ПКМ съесть» and the price.
- **The HUD's meal slot** shows the same card, «Осталось» with «из m:ss» in place of the duration, no key and no price.
  Pod paste, no item, is named and kinded by the crew's table.
- **Gear** (green edge): a helmet «Запас воздуха N O₂»; a body «Выдерживает до −40°» (2026-11-14-temperature-in-degrees); the regen suit also, under «Особое»,
  «Вырабатывает воздух до 1 O₂/с» and «Расход заряда · за 1 O₂ · ⚡15», key V «вкл / выкл»; the cape «Урон от ударов −20%»
  and «Тепло +5°»; the jetpack «Скорость до 30 кл/с» and «Расход воздуха при тяге 2 O₂/с», key Shift «в
  космосе»; the helmet lamp «Дальность 18 кл», «Луч 60°» and its battery draw, 0; the scanner its line. Every number is
  computed from `sim/Content` and the light's constants, none written on the card.
- **Signs: set A, «Вещи»**, the user's pick on the [pick page](https://claude.ai/artifact/FnYZertfjnwM14YFssX8Ba)
  (`cards_A`): lungs with an arrow down (calm), a canister with a plus (deep), a hammer with an arrow up (work), two
  bottles (tank), a snowflake (cold), a shield (guard), arrows round a bolt (regen), a nozzle with its flame (speed), a
  bulb (light). Duration reuses the stopwatch of «Удар раз в»; the regen's charge reuses the battery, the lamp's beam the
  swing. New groups: «Эффект», «Воздух», «Защита», «Движение», «Свет».
- **Food sells.** Ration 30 and canned meat 40 are priced by hand; fruit and glowberries keep their 20; dishes keep
  their worked-out worth (porridge 100, hash 113, broth 113, roast 94, stew 132). Gear and tools stay unsold.

## Deliberately not handled

- The cap on meals, up to three dishes and their summed rates, is undecided, so no card says it.
- The bodies' and the cape's cold rows are in degrees, from [temperature](2026-11-14-temperature-in-degrees.md): the coldest a
  body withstands and what the cape adds.
- Pod paste is no item and has no price.
- The seed keeps its kind, «Семя», though the pick page called it food: it is planted as often as eaten.
