# Audio credits

Every sound under `game/audio`: where it came from, who made it and its licence. CC0 only (ADR 0033), but for the team's own music made on Suno, marked as a placeholder (ADR 0035):
the repository is public, so a file must be free to redistribute without attribution. A test
(`game.tests/SoundTests.cs`) fails on a file missing here or under another licence.

Kenney's and Freesound's files are converted to mono WAV, trimmed and normalised to −3 dBFS peak;
Freesound's are its public HQ previews of CC0 sounds. The chop's and the break's variants are then matched by the RMS of
their K-weighted attack (the first 0.15 s of a chop, 0.4 s of a break), the louder turned down. The steps (which the landing plays too) are 0.3 s of each recording from its attack, held 0.08 s and then faded to −60 dB; the jump is the push-off of each `Jump_Metal`, 0.33 s from its attack after 0.1 s in, high-passed at 120 Hz, held 0.1 s; the glove's thump in space, `act-space-1`, is all of `LowHit_01` from its attack (0.14 s), held 0.04 s, raised 21 dB to its peak. The harpoon's shot and ready are cut from their attack, held and faded to −60 dB; its reel is 2.45 s of its source with short fades, looped. The drill's strike is 50 ms of its source from its attack, held 12 ms, decayed by 40 dB and faded over its last 8 ms, so eighteen a second never smear; it and the charge-done blip are normalised to −3 dBFS peak. The machines' recorded one-shots (0059) are the audition's cuts, decoded to WAV as heard there: the Former's stroke is 0.5 s of its source from the attack after 9.4 s, held 0.12 s, decayed to −60 dB; the drill's whir is three 150 ms grains of a steady stretch (at 6.3, 6.62 and 6.95 s) with 40 ms fades; both levelled so a train of them at their rate peaks at −23 LUFS momentary. The axe's swings (0068) are the audition's cuts: each high-passed at 100 Hz, cut from 5 ms before its attack to its fall to −45 dB, faded out over 25 ms, its loudest 10 ms put 65 ms in with leading silence, and levelled to the drill's whir as played (its grains in a train at 9 a second), −23.4 LUFS momentary, so both sit alike under `Sound.ToolIdleDb`. The drones' one-shots (0062) are the audition's cuts as well: each thud 0.31 s of its source from its attack, held 0.04 s, decayed to −60 dB, a latch's 0.25 s mixed in 60 ms later 6 dB under; each switch 0.25 s from its attack; a set's variants matched by momentary loudness, the louder turned down. The thrust, the drones' whir, the charger's hum and the machines' synthesised loops are rendered offline over two periods of their loop, the second kept, so they close on themselves, encoded as OGG. `chop-1`
keeps its ring, as the team wanted it; `break-2`'s clipped peaks are rebuilt (ffmpeg `adeclip`) and its top cut from 9 kHz. The vent
hum and the fire are 20 s of their sources, its tail crossfaded into their heads, encoded as OGG at
about −20 LUFS. The generated ones
are placeholders made procedurally for Stillwake (band-passed noise, sine tones), the team's own work
released under CC0.

| File | Source | Author | Licence |
| ---- | ------ | ------ | ------- |
| `atmosphere/hiss-1.ogg` | generated for Stillwake | Stillwake team | CC0 |
| `atmosphere/vent-hum-1.ogg` | Freesound, "Air vent (looped)", https://freesound.org/people/lfyaudio/sounds/848969/ | lfyaudio | CC0 |
| `building/screwdriver-1.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495234/ | CallFlan | CC0 |
| `building/screwdriver-2.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495235/ | CallFlan | CC0 |
| `building/screwdriver-3.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495236/ | CallFlan | CC0 |
| `building/screwdriver-4.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495237/ | CallFlan | CC0 |
| `building/screwdriver-5.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495238/ | CallFlan | CC0 |
| `crafting/former-motor-1.ogg` | generated for Stillwake: 80 Hz at 0.6 and 160 Hz at 0.3 under white noise (mulberry32 seed 14) band-passed at 900 Hz, Q 3, gain 2.2, swaying ±45% at 2.25 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Flywheel whoosh") | Stillwake team | CC0 |
| `crafting/former-stroke-1.wav` | Freesound, "Punch Press, 10 Ton" (0.5 s from the attack after 9.4 s), https://freesound.org/people/dnewtonjr/sounds/232598/ | dnewtonjr | CC0 |
| `crafting/smelter-hum-1.ogg` | generated for Stillwake: 100 Hz and its harmonics at 1, 0.35 and 0.15 under white noise (mulberry32 seed 25) band-passed at 3.5 kHz, Q 0.7, gain 0.1, swaying ±30% at 0.75 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Hum and hiss") | Stillwake team | CC0 |
| `crafting/smelter-crackle-1.wav` | generated for Stillwake: pop 1 of six, white noise (mulberry32 seed 4100) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-2.wav` | generated for Stillwake: pop 2 of six, white noise (mulberry32 seed 4101) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-3.wav` | generated for Stillwake: pop 3 of six, white noise (mulberry32 seed 4102) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-4.wav` | generated for Stillwake: pop 4 of six, white noise (mulberry32 seed 4103) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-5.wav` | generated for Stillwake: pop 5 of six, white noise (mulberry32 seed 4104) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-6.wav` | generated for Stillwake: pop 6 of six, white noise (mulberry32 seed 4105) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crew/air-fill-1.wav` | generated for Stillwake: white noise (mulberry32 seed 47) through two band-passes, Q 1.1, their centre rising from 2.6 to 4.8 kHz, 0.55 s: in over 50 ms, held, falling away over the last 38 %, to −3 dBFS peak (a placeholder till an audition) | Stillwake team | CC0 |
| `crew/alarm-1.wav` | generated for Stillwake | Stillwake team | CC0 |
| `crew/axe-swing-1.wav` | Freesound, "Swinging staff whoosh (strong) 04.wav", https://freesound.org/people/Nightflame/sounds/422513/ | Nightflame | CC0 |
| `crew/axe-swing-2.wav` | Freesound, "Swinging staff whoosh (strong) 06.wav", https://freesound.org/people/Nightflame/sounds/422511/ | Nightflame | CC0 |
| `crew/axe-swing-3.wav` | Freesound, "Swinging staff whoosh (strong) 01.wav", https://freesound.org/people/Nightflame/sounds/422506/ | Nightflame | CC0 |
| `crew/axe-swing-4.wav` | Freesound, "Swinging staff whoosh (strong) 03.wav", https://freesound.org/people/Nightflame/sounds/422508/ | Nightflame | CC0 |
| `crew/harpoon-ready-1.wav` | Freesound, ".22 Bolt Action Long Rifle Rechambering" (0.95 s from its start), https://freesound.org/people/AlexCoover/sounds/706404/ | AlexCoover | CC0 |
| `crew/harpoon-reel-1.ogg` | Freesound, "reeling.wav" (2.45 s, looped), https://freesound.org/people/BranndyBottle/sounds/464701/ | BranndyBottle | CC0 |
| `crew/harpoon-shot-1.wav` | Freesound, "Dart/Arrow Trap." (0.85 s from its attack), https://freesound.org/people/jacobo1304/sounds/866990/ | jacobo1304 | CC0 |
| `crew/inhale-1.wav` | generated for Stillwake: white noise (mulberry32 seed 81) through two band-passes, Q 0.9, their centre rising from 0.9 to 1.7 kHz, 0.5 s under a sine swell, to −3 dBFS peak (a placeholder till an audition) | Stillwake team | CC0 |
| `crew/jump-1.wav` | Freesound, "Jump_Metal_3.wav" (the push-off), https://freesound.org/people/GiocoSound/sounds/421139/ | GiocoSound | CC0 |
| `crew/jump-2.wav` | Freesound, "Jump_Metal_1.wav" (the push-off), https://freesound.org/people/GiocoSound/sounds/421141/ | GiocoSound | CC0 |
| `crew/jump-3.wav` | Freesound, "Jump_Metal_4.wav" (the push-off), https://freesound.org/people/GiocoSound/sounds/421142/ | GiocoSound | CC0 |
| `crew/jump-4.wav` | Freesound, "Jump_Metal_5.wav" (the push-off), https://freesound.org/people/GiocoSound/sounds/421143/ | GiocoSound | CC0 |
| `crew/step-1.wav` | Freesound, "Boots_On_Hollow_Metal_Deep_01", https://freesound.org/people/BlondPanda/sounds/778399/ | BlondPanda | CC0 |
| `crew/step-2.wav` | Freesound, "Boots_On_Hollow_Metal_Deep_02", https://freesound.org/people/BlondPanda/sounds/778400/ | BlondPanda | CC0 |
| `crew/step-3.wav` | Freesound, "Boots_On_Hollow_Metal_Deep_03", https://freesound.org/people/BlondPanda/sounds/778401/ | BlondPanda | CC0 |
| `crew/step-4.wav` | Freesound, "Boots_On_Hollow_Metal_Deep_04", https://freesound.org/people/BlondPanda/sounds/778402/ | BlondPanda | CC0 |
| `crew/step-5.wav` | Freesound, "Boots_On_Hollow_Metal_Deep_05", https://freesound.org/people/BlondPanda/sounds/778403/ | BlondPanda | CC0 |
| `crew/step-6.wav` | Freesound, "Boots_On_Hollow_Metal_Deep_06", https://freesound.org/people/BlondPanda/sounds/778404/ | BlondPanda | CC0 |
| `crew/thrust-1.ogg` | generated for Stillwake: white noise high-passed at 700 Hz and low-passed at 4.8 kHz (24 dB/oct), the filters drifting ±150 cents, a 2 s loop rendered offline to −23 dBFS RMS (the audition's "Bright") | Stillwake team | CC0 |
| `debris/break-1.wav` | Freesound, "Car Crash (with Glass)", https://freesound.org/people/magnuswaker/sounds/592388/ | magnuswaker | CC0 |
| `debris/break-2.wav` | Freesound, "Loud Bang Crashing Metal Chunk Dropped in Metal Dumpster", https://freesound.org/people/qubodup/sounds/859154/ | qubodup | CC0 |
| `debris/break-3.wav` | Freesound, "metallic crash.wav", https://freesound.org/people/BehanSean/sounds/422438/ | BehanSean | CC0 |
| `debris/chop-1.wav` | Freesound, "axe hits - 1.wav", https://freesound.org/people/quatricise/sounds/582372/ | quatricise | CC0 |
| `debris/chop-2.wav` | Freesound, "shield guard", https://freesound.org/people/nekoninja/sounds/370203/ | nekoninja | CC0 |
| `debris/chop-3.wav` | Freesound, "slightmetallicthud.mp3", https://freesound.org/people/profoundsounds/sounds/686282/ | profoundsounds | CC0 |
| `debris/drill-whir-1.wav` | Freesound, "DeWalt 18V drill, no-load spins" (150 ms at 6.3 s), https://freesound.org/people/megashroom/sounds/390162/ | megashroom | CC0 |
| `debris/drill-whir-2.wav` | Freesound, "DeWalt 18V drill, no-load spins" (150 ms at 6.62 s), https://freesound.org/people/megashroom/sounds/390162/ | megashroom | CC0 |
| `debris/drill-whir-3.wav` | Freesound, "DeWalt 18V drill, no-load spins" (150 ms at 6.95 s), https://freesound.org/people/megashroom/sounds/390162/ | megashroom | CC0 |
| `debris/drill-strike-1.wav` | Freesound, "Pickaxe3" (50 ms from its attack, held 12 ms, faded out), https://freesound.org/people/NoisyRedFox/sounds/760567/ | NoisyRedFox | CC0 |
| `furnace/fire-1.ogg` | Freesound, "Fire - Chimney / Stove", https://freesound.org/people/Vrymaa/sounds/770108/ | Vrymaa | CC0 |
| `grid/door-close-1.wav` | Freesound, "SFX Door Close Big.wav", https://freesound.org/people/Paul368/sounds/264060/ | Paul368 | CC0 |
| `grid/door-open-1.wav` | Freesound, "SFX Door Open.wav", https://freesound.org/people/Paul368/sounds/264061/ | Paul368 | CC0 |
| `grid/valve-1.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-2.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-3.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `hands/act-1.wav` | Freesound, "Lego Click (short)", https://freesound.org/people/ImmergoMedia/sounds/670000/ | ImmergoMedia | CC0 |
| `hands/act-space-1.wav` | Freesound, "LowHit_01.wav", https://freesound.org/people/Faulkin/sounds/336495/ | Faulkin | CC0 |
| `logistics/drone-dock-1.wav` | Impact Sounds, `impactSoft_medium_000.ogg`, https://kenney.nl/assets/impact-sounds, with Freesound "Latch_04.wav", https://freesound.org/people/Q.K./sounds/56246/, 60 ms after it, 6 dB under | Kenney, Q.K. | CC0 |
| `logistics/drone-dock-2.wav` | Impact Sounds, `impactSoft_medium_002.ogg`, https://kenney.nl/assets/impact-sounds, with Freesound "Latch_04.wav", https://freesound.org/people/Q.K./sounds/56246/, 60 ms after it, 6 dB under | Kenney, Q.K. | CC0 |
| `logistics/drone-dock-3.wav` | Impact Sounds, `impactSoft_medium_004.ogg`, https://kenney.nl/assets/impact-sounds, with Freesound "Latch_04.wav", https://freesound.org/people/Q.K./sounds/56246/, 60 ms after it, 6 dB under | Kenney, Q.K. | CC0 |
| `logistics/drone-grab-1.wav` | UI Audio, `switch10.ogg`, https://kenney.nl/assets/ui-audio | Kenney | CC0 |
| `logistics/drone-grab-2.wav` | UI Audio, `switch20.ogg`, https://kenney.nl/assets/ui-audio | Kenney | CC0 |
| `logistics/drone-grab-3.wav` | UI Audio, `switch1.ogg`, https://kenney.nl/assets/ui-audio | Kenney | CC0 |
| `logistics/drone-whir-1.ogg` | generated for Stillwake: noise band-passed at 450 Hz chopped ±25% 37.5 times a second, noise low-passed at 250 Hz under it and a faint 112.5 Hz tone, a 2 s loop rendered offline to −23 dBFS RMS (the audition's "Fan") | Stillwake team | CC0 |
| `ruins/music-box-1.wav` | generated for Stillwake (tools/homes-art/export.js): a plucked steel tine, a sine at 1568 Hz with inharmonic partials at 2.76, 5.40 and 8.93 times it, each decaying, 0.9 s, to −3 dBFS peak; played at a tune's pitches (a placeholder till an audition, 0091) | Stillwake team | CC0 |
| `ruins/zap-1.wav` | generated for Stillwake (tools/homes-art/export.js): bursts of smoothed noise a few milliseconds each under a quarter-second fall, to −3 dBFS peak (a placeholder till an audition, 0091) | Stillwake team | CC0 |
| `music/space-1.ogg` | generated for Stillwake: the synthesised drone "Wreck" (v2), 80 s looping seamlessly, about −18 LUFS; a placeholder for the bed of space (ADR 0042) until the team writes its own | Stillwake team | CC0 |
| `music/title-1.ogg` | made by the team with Suno (suno.com) on its free plan, 2026-10-16, normalised to about −18 LUFS | Stillwake team | Suno Basic, non-commercial: replace before sale |
| `power/charge-full-1.wav` | Freesound, "UI Confirm" (0.27 s from its attack), https://freesound.org/people/unfa/sounds/584184/ | unfa | CC0 |
| `power/charger-hum-1.ogg` | generated for Stillwake: 50 Hz and its harmonics at 0.7, 1, 0.4 and 0.15, low-passed at 500 Hz, swaying ±4% at 0.5 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Warm 50 Hz") | Stillwake team | CC0 |
| `ui/click-1.wav` | Interface Sounds, `click_002.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
| `ui/click-2.wav` | Interface Sounds, `click_003.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
