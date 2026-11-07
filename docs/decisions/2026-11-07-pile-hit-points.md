# The hold's piles have hit points

- Status: accepted
- Date: 2026-11-07
- Amends [hold-piles](2026-10-31-hold-piles.md)

## Context

A pile in the hold had a `Strength` in block damage, as a wall has, so every tool took about the same number of hits: the axe 4
a swing, the sledgehammer 6 a blow every three seconds. In play it looked as though a pile had no hit points and only counted
blows, and the sledgehammer, a wreck tool, was the slowest way through it. The user asked for hit points.

## Decision

- **A pile has hit points (`Machine.Hp`), as wreckage does:** every hit takes the weapon's `Damage`, not its `BlockDamage`, in
  the same crack the hold-piles ADR kept. The axe's swings stay what they were (6, 5, 5 and 3: hit points 12, 10, 10 and 6); the
  sledgehammer's 12 breaks any pile at a blow, the spear's 3 twice the axe's.
- **The scanner reads a pile** in the hold as it reads a wreck: its hit points less its crack, over the middle of its top.

## Deliberately not handled

- The plasma cutter still breaks no pile: it deals no block damage, and its hits on what is not a wreck are never sent.
- A pile's crack is drawn in stages of its hit points, as before; no new art.
