# Helmets and suits each add their degrees; the heated and heat helmets

- Status: accepted; supersedes in part [temperature-in-degrees](2026-11-14-temperature-in-degrees.md) and
  [heat-limits](2026-11-14-heat-limits.md) (a suit's `Low` and `High` as the whole limit),
  [quilted-helmet-and-traced-art](2026-11-14-quilted-helmet-and-traced-art.md) (the quilted helmet),
  [stretches-of-space](2026-11-15-stretches-of-space.md) (what opens the ice belt) and
  [helmets-fit-the-suit](2026-11-14-helmets-fit-the-suit.md) (each helmet turning its own way)
- Date: 2026-11-21

## Context

Only the suit set the temperature limits. Every helmet stood to −110° as the basic one did, so a helmet decided air alone:
the heavy helmet beat the quilted one everywhere, and nothing in the head slot traded air against warmth. The quilted
helmet could not be made before the planet either: its cloth comes from the planet's ruins. The helmets' pictures were
whole GPT paintings fitted by hand, and their bottoms went wrong again and again.

## Decision

- **The limits add up.** `ItemSpec.Low` and `High` are a worn piece's signed part, a helmet's or a body's.
  `CrewModule.LowOf` is `Temperature.BareLow` plus the worn helmet's `Low` plus the worn body's `Low`, with
  `Cape.Warmth` taken off when the cape is worn. `HighOf` is the same with `High` and `BareHigh`. Bare: 0° / +30°.

  | Piece | Tank | Cold | Heat | With the basic other piece | Made |
  | --- | --- | --- | --- | --- | --- |
  | basic helmet | 25 | −50 | +10 | −110 / +50 | free at the pod |
  | heavy helmet | 80 | −50 | +10 | −110 / +50 | black market |
  | heated helmet (`heatedHelmet`) | 50 | −90 | 0 | −150 / +40 | workbench: basic helmet, 2 coils, 2 wire, 2 plates |
  | heat helmet (`heatHelmet`) | 25 | −30 | +30 | −90 / +70 | workbench: basic helmet, 2 mica, 2 alloy scrap, 1 glass |
  | basic suit, regen suit | — | −60 | +10 | −110 / +50 | free at the pod; black market |
  | heat suit | — | −50 | +20 | −100 / +60 | workbench |

  Every number lives in `sim/Content` (`Items`, `Crafts`). Air stays a helmet's `Tank`.
- **Air or warmth in the head slot.** The heavy helmet holds air; the heated helmet, made of space parts, holds the cold
  off. The heat helmet pairs with the heat suit on the planet: together −80° / +80°, colder in space than the basic pair.
- **The quilted helmet is removed**, with its art and craft. Space gets its own warm helmet; the planet gets the heat one.
- **The ice belt opens with the heated helmet alone** (`Stretches.IceBelt`). The heavy helmet no longer helps against cold.
  Nothing checks what a hero wears there: the cold decides who can work.
- **Cards** read what a piece adds: `card.cold` «Холод −90°», `card.heat` «Жар 0°». The dial's hover shows the totals.
- **Helmets are bolted still** on their rubber collars (`BodyFit`: every helmet `Bolted`). The hero's own head turns
  inside, seen through the glass, its hair and beard too; a still helmet's upper body leans a share of the look instead
  (`BodyAnswers.LookLean`). The lamp and the scanner left the helmet for the light drone ([light-drone](2026-11-21-light-drone.md)).
- **New helmets are socket helmets** (`tools/anime-art/socket/socket.js`): GPT paints only the dome over a template of the
  basic helmet; the tool crops it to the basic shell's zones and lays on the basic neck, seal and glass, which may be
  tinted (the heat helmet's is gold). A cell painting less than 98% of the shell is refused. The heated and heat
  helmets were made this way, and so were three spare looks kept as skins: the dust wrap, hide and bone, and the boiler.

## Deliberately not handled

- No quilted suit yet: without weight it would beat the basic suit in space outright. It comes with weight.
- The thermometer ring keeps the body's scale; the place's limits stay in its hover.
- The cape's warmth stays `Cape.Warmth`, outside the item rows.
- Weight, set bonuses and coolants are not in.
- A save whose ice belt the heavy helmet opened is not handled: saves break at release.
- Socket helmets have one glass, the basic one, and nothing hangs below the rim line: no fringe, jaw or beak. The bell
  and the catchers keep their own fits, and the nine older skins are not redrawn.
- The tool checks shape, not style; a seam that looks glued on is caught on the pick page.
