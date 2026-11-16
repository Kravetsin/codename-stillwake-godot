# Design: storms, then rain, on a planet's surface

Status: night is built ([night-chills](../decisions/2026-11-07-night-chills.md)); storms are built as stretches
([surface-storms](../decisions/2026-11-16-surface-storms.md)), their rain a look. What stays here is rain as water.

## Rain, later

A storm that rains water, a field on its `Stretch` added with its user. Today's `Water` module (cisterns, electrolysers) holds
water in machines; flowing water is the module `liquids-design` is designing. Rain needs **no** interface from it: that
module reads `ISkyQueries.Now` and pours water into open-sky columns itself, so Sky stays dependency-free and early in the
order. Nothing more about water is designed here.

## Deliberately not handled

- **Wind pushing anything:** see the ADR.
- **Fog banks**, **a separate cold snap**, **storm damage:** no player need names them (lightning: surface-storms.md).
