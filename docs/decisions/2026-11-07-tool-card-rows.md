# A tool's card lists its numbers as rows with icons

- Status: accepted
- Date: 2026-11-07

## Context

A player asked why there are several hand tools when their damage a second is about the same. The card led with that
number in amber, so nobody saw that the sledgehammer hits for 12 and breaks a weak wreck in one blow, or that the drill
reaches 8 cells. Its tiles were equal dark boxes with small capital labels, and they ran together. A first pick page tried
bars on a common scale, best and worst marks, role lines and a table of targets. The user rejected all of them as
unintuitive: a card shows the tool's own numbers, not a comparison.

## Decision

- **No damage a second.** A weapon's card shows its damage off a wreck, its damage off rock, the seconds between hits,
  its swing in degrees (`Arc` of a turn's 256) and its reach in cells (`Range`).
- **Rows under groups**, the user's pick («Список с иконками»). Each row has its icon, its label with a small word after
  it, and its value at the right. Rows are ruled faintly. The groups are hit or shot, reach, and special, in that order.
- **One icon and hue per number** (`CardSigns`): damage and pierce hot orange, block stone, time pale cyan, swing steel
  violet, reach and reel steel blue, battery yellow-green, loot green. Icons are drawn in lines on a 24 px square and
  rasterised from SVG at the size shown (`CardIcons`).
- **What is special:** an electric tool's draw comes first, as «Расход батареи» with a bolt by the number, then the
  cutter's loot, «Добыча с обломков +50%» from `CutBonus`. A hit effect is a row now, so `HitEffect` keeps only its id,
  and the effect chips and their words are gone.
- A number the tool lacks is written in words, dimmed, with its icon struck through: the cutter's block, «не режет».
- Guns, ammo, the harpoon and the grapple use the same rows. A gun's draw a shot is a battery row. The harpoon's reel and
  the grapple's pull got icons of their own in the same lines, unpicked.
- Modules still mark what they change in their tech's colour (0098).

## Deliberately not handled

- No comparison between tools anywhere on the card. A player who wants one holds two cards side by side.
- The pierce and reel icons were drawn here, not picked. They are swapped if the user dislikes them.
- A gun's reach is not shown: it has no cell figure today.
