# Audio credits

Every sound under `game/audio`: where it came from, who made it and its licence. CC0 only (ADR 0033), but for the team's own music made on Suno, marked as a placeholder (ADR 0035):
the repository is public, so a file must be free to redistribute without attribution. A test
(`game.tests/SoundTests.cs`) fails on a file missing here or under another licence.

Kenney's and Freesound's files are converted to mono WAV, trimmed and normalised to −3 dBFS peak;
Freesound's are its public HQ previews of CC0 sounds. The chop's and the break's variants are then matched by the RMS of
their K-weighted attack (the first 0.15 s of a chop, 0.4 s of a break), the louder turned down. The steps (which the landing plays too) are 0.3 s of each recording from its attack, held 0.08 s and then faded to −60 dB; the jump is the push-off of each `Jump_Metal`, 0.33 s from its attack after 0.1 s in, high-passed at 120 Hz, held 0.1 s; the glove's thump in space, `act-space-1`, is all of `LowHit_01` from its attack (0.14 s), held 0.04 s, raised 21 dB to its peak. The harpoon's shot and ready are cut from their attack, held and faded to −60 dB, and the nailer's four (the user's picks) are cut from 5 ms before their onset to their length here, the last 25–45% faded exponentially; its reel is 2.45 s of its source with short fades, looped. The drill's strike is 50 ms of its source from its attack, held 12 ms, decayed by 40 dB and faded over its last 8 ms, so eighteen a second never smear; it and the charge-done blip are normalised to −3 dBFS peak. The machines' recorded one-shots (0059) are the audition's cuts, decoded to WAV as heard there: the Former's stroke is 0.5 s of its source from the attack after 9.4 s, held 0.12 s, decayed to −60 dB; the drill's whir is three 150 ms grains of a steady stretch (at 6.3, 6.62 and 6.95 s) with 40 ms fades; both levelled so a train of them at their rate peaks at −23 LUFS momentary. The axe's swings (0068) are the audition's cuts: each high-passed at 100 Hz, cut from 5 ms before its attack to its fall to −45 dB, faded out over 25 ms, its loudest 10 ms put 65 ms in with leading silence, and levelled to the drill's whir as played (its grains in a train at 9 a second), −23.4 LUFS momentary, so both sit alike under `Sound.ToolIdleDb`. The drones' one-shots (0062) are the audition's cuts as well: each thud 0.31 s of its source from its attack, held 0.04 s, decayed to −60 dB, a latch's 0.25 s mixed in 60 ms later 6 dB under; each switch 0.25 s from its attack; a set's variants matched by momentary loudness, the louder turned down. The thrust, the drones' whir, the charger's hum and the machines' synthesised loops are rendered offline over two periods of their loop, the second kept, so they close on themselves, encoded as OGG. `chop-1`
keeps its ring, as the team wanted it; `break-2`'s clipped peaks are rebuilt (ffmpeg `adeclip`) and its top cut from 9 kHz. The vent
hum and the fire are 20 s of their sources, its tail crossfaded into their heads, encoded as OGG at
about −20 LUFS. The generated ones
are placeholders made procedurally for Stillwake (band-passed noise, sine tones), the team's own work
released under CC0.

The planet's sounds (0093) are the planet audition's cuts, as heard there: each mono 48 kHz WAV cut from 5 ms before its
onset, or at the point of its source given here, held, decayed to −60 dB by its length and faded over its last 10 ms; each
sound's variants levelled to one max momentary loudness, peaking at −3 dBFS at most (so a few fall short). The lava's rumble
is synthesised as the machines' loops are, its second period kept.

The sentry's sounds are the sentry audition's cuts, as the user picked them there: each mono 48 kHz WAV cut from its attack (or
the point given here), decayed to −60 dB, levelled with the rest of the audition to one loudness (the loudest 100 ms RMS after
a 150 Hz high-pass at −20 dBFS, peaks at −1 dBFS at most), then all turned down 2 dB alike so none peaks over −3 dBFS; its
arc's charge and hit, picked later, matched to them by that loudness, capped at −3 dBFS. Its
death is two sources mixed as the audition mixed them. Its burner is the jetpack's loop, its hit the wreck's chop.

The warden's sounds are the guard audition's picks, cut and levelled as the sentry's: each the length given here from its attack, its last 45% faded out. Its landing is a hero's on alloy and its hit a tool's on alloy, so they are not here.

| File | Source | Author | Licence |
| ---- | ------ | ------ | ------- |
| `atmosphere/hiss-1.ogg` | generated for Stillwake | Stillwake team | CC0 |
| `atmosphere/vent-hum-1.ogg` | Freesound, "Air vent (looped)", https://freesound.org/people/lfyaudio/sounds/848969/ | lfyaudio | CC0 |
| `building/screwdriver-1.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495234/ | CallFlan | CC0 |
| `building/screwdriver-2.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495235/ | CallFlan | CC0 |
| `building/screwdriver-3.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495236/ | CallFlan | CC0 |
| `building/screwdriver-4.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495237/ | CallFlan | CC0 |
| `building/screwdriver-5.wav` | Freesound, "Single_Ratchet (one of five)", https://freesound.org/people/CallFlan/sounds/495238/ | CallFlan | CC0 |
| `building/break-soil-1.wav` | Freesound, "throwing brick on dirt.wav" (0.6 s from 0.34 s), https://freesound.org/people/KrystianPawlowski/sounds/584881/ | KrystianPawlowski | CC0 |
| `building/break-soil-2.wav` | Freesound, "throwing brick on dirt.wav" (0.6 s from 1.39 s), https://freesound.org/people/KrystianPawlowski/sounds/584881/ | KrystianPawlowski | CC0 |
| `building/break-soil-3.wav` | Freesound, "throwing brick on dirt.wav" (0.6 s from 3.55 s), https://freesound.org/people/KrystianPawlowski/sounds/584881/ | KrystianPawlowski | CC0 |
| `building/break-stone-1.wav` | Freesound, "Rocks.wav" (0.9 s from 0.895 s), https://freesound.org/people/adamgryu/sounds/336023/ | adamgryu | CC0 |
| `building/break-stone-2.wav` | Freesound, "Rocks.wav" (0.9 s from 1.17 s), https://freesound.org/people/adamgryu/sounds/336023/ | adamgryu | CC0 |
| `building/break-stone-3.wav` | Freesound, "Rocks.wav" (0.9 s from 1.84 s), https://freesound.org/people/adamgryu/sounds/336023/ | adamgryu | CC0 |
| `building/hit-soil-1.wav` | Freesound, "Shovel_dirt.wav" (0.35 s from 0.316 s), https://freesound.org/people/dr19/sounds/353907/ | dr19 | CC0 |
| `building/hit-soil-2.wav` | Freesound, "Shovel_dirt.wav" (0.35 s from 1.632 s), https://freesound.org/people/dr19/sounds/353907/ | dr19 | CC0 |
| `building/hit-soil-3.wav` | Freesound, "Shovel_dirt.wav" (0.35 s from 3.249 s), https://freesound.org/people/dr19/sounds/353907/ | dr19 | CC0 |
| `building/hit-soil-4.wav` | Freesound, "Shovel_dirt.wav" (0.35 s from 4.983 s), https://freesound.org/people/dr19/sounds/353907/ | dr19 | CC0 |
| `building/hit-soil-5.wav` | Freesound, "Shovel_dirt.wav" (0.35 s from 6.958 s), https://freesound.org/people/dr19/sounds/353907/ | dr19 | CC0 |
| `building/hit-stone-1.wav` | Freesound, "Stone Strike/Hit" (0.4 s from 0.312 s), https://freesound.org/people/Bricklover/sounds/560966/ | Bricklover | CC0 |
| `building/hit-stone-2.wav` | Freesound, "Stone Strike/Hit" (0.4 s from 4.385 s), https://freesound.org/people/Bricklover/sounds/560966/ | Bricklover | CC0 |
| `building/hit-stone-3.wav` | Freesound, "Stone Strike/Hit" (0.4 s from 5.595 s), https://freesound.org/people/Bricklover/sounds/560966/ | Bricklover | CC0 |
| `building/hit-stone-4.wav` | Freesound, "Stone Strike/Hit" (0.4 s from 7.947 s), https://freesound.org/people/Bricklover/sounds/560966/ | Bricklover | CC0 |
| `building/hit-stone-5.wav` | Freesound, "Stone Strike/Hit" (0.4 s from 10.232 s), https://freesound.org/people/Bricklover/sounds/560966/ | Bricklover | CC0 |
| `crafting/former-motor-1.ogg` | generated for Stillwake: 80 Hz at 0.6 and 160 Hz at 0.3 under white noise (mulberry32 seed 14) band-passed at 900 Hz, Q 3, gain 2.2, swaying ±45% at 2.25 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Flywheel whoosh") | Stillwake team | CC0 |
| `crafting/former-stroke-1.wav` | Freesound, "Punch Press, 10 Ton" (0.5 s from the attack after 9.4 s), https://freesound.org/people/dnewtonjr/sounds/232598/ | dnewtonjr | CC0 |
| `crafting/smelter-hum-1.ogg` | generated for Stillwake: 100 Hz and its harmonics at 1, 0.35 and 0.15 under white noise (mulberry32 seed 25) band-passed at 3.5 kHz, Q 0.7, gain 0.1, swaying ±30% at 0.75 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Hum and hiss") | Stillwake team | CC0 |
| `crafting/smelter-crackle-1.wav` | generated for Stillwake: pop 1 of six, white noise (mulberry32 seed 4100) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-2.wav` | generated for Stillwake: pop 2 of six, white noise (mulberry32 seed 4101) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-3.wav` | generated for Stillwake: pop 3 of six, white noise (mulberry32 seed 4102) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-4.wav` | generated for Stillwake: pop 4 of six, white noise (mulberry32 seed 4103) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-5.wav` | generated for Stillwake: pop 5 of six, white noise (mulberry32 seed 4104) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `crafting/smelter-crackle-6.wav` | generated for Stillwake: pop 6 of six, white noise (mulberry32 seed 4105) decaying over 15–40 ms through a band-pass at 300–900 Hz, Q 5 (seed 41 draws each pop's length and band); the six levelled together so two a second have −23 dBFS RMS (the audition's "Molten bubbling") | Stillwake team | CC0 |
| `creatures/death-squish-1.wav` | Freesound, "Gooey Squish", https://freesound.org/people/editor_adp/sounds/269482/: its HQ preview from 5 ms before its attack to its end (0.84 s), faded over its last 30 ms, its loudest 100 ms RMS raised to the death audition's, its peaks rounded off at −1 dBFS by a tanh | editor_adp | CC0 |
| `creatures/warden-wake-1.wav` | Freesound, "Robot Awakening, Power On" (1.17 s), https://freesound.org/people/GammaGool/sounds/746988/ | GammaGool | CC0 |
| `creatures/warden-alert-1.wav` | Freesound, "Synthetic monster growl.wav" (0.9 s), https://freesound.org/people/Wax_vibe/sounds/556613/ | Wax_vibe | CC0 |
| `creatures/warden-windup-1.wav` | Freesound, "hydraulic office chair moving effect 01" (0.78 s), https://freesound.org/people/rubberduck9999/sounds/678452/ | rubberduck9999 | CC0 |
| `creatures/warden-pounce-1.wav` | Freesound, "burst3.wav" (0.6 s), https://freesound.org/people/ReadeOnly/sounds/186926/ | ReadeOnly | CC0 |
| `creatures/warden-punch-1.wav` | Freesound, "PUNCH-BOXING-03.wav" (0.45 s), https://freesound.org/people/newagesoup/sounds/348242/ | newagesoup | CC0 |
| `creatures/warden-death-1.wav` | Freesound, "Computer Breaking Sound" (1.04 s), https://freesound.org/people/thehorriblejoke/sounds/505751/ | thehorriblejoke | CC0 |
| `creatures/warden-shutdown-1.wav` | Freesound, "MachinePowerOff.flac" (1.6 s), https://freesound.org/people/Skylar1146/sounds/395378/ | Skylar1146 | CC0 |
| `creatures/sentry-alert-1.wav` | Freesound, "Computer Chirp.wav", https://freesound.org/people/pointparkcinema/sounds/407244/ | pointparkcinema | CC0 |
| `creatures/sentry-windup-1.wav` | Freesound, "servomotor.wav", https://freesound.org/people/vacuumfan7072/sounds/264592/ | vacuumfan7072 | CC0 |
| `creatures/sentry-unclamp-1.wav` | Freesound, "SqueakyClick1.wav" (from 0.067 s), https://freesound.org/people/BMacZero/sounds/94133/ | BMacZero | CC0 |
| `creatures/sentry-clamp-1.wav` | Freesound, "clunk2.wav", https://freesound.org/people/spukkin/sounds/70880/ | spukkin | CC0 |
| `creatures/sentry-snap-1.wav` | Freesound, "Whip Crack.m4a" (from 0.053 s), https://freesound.org/people/SciFiSounds/sounds/529925/ | SciFiSounds | CC0 |
| `creatures/sentry-death-1.wav` | Freesound, "POWER06.WAV", https://freesound.org/people/mad-monkey/sounds/66692/, mixed with Freesound, "Heavy metal clatter.wav" (from 0.029 s), https://freesound.org/people/EntoClash/sounds/626811/ | mad-monkey, EntoClash | CC0 |
| `creatures/sentry-punch-1.wav` | Freesound, "Metal Gauntlet Punch 4", https://freesound.org/people/Hoggington/sounds/536258/ | Hoggington | CC0 |
| `creatures/sentry-charge-1.wav` | Freesound, "Laser Charge Up" (0.6 s from its attack, the last 0.18 s faded), https://freesound.org/people/magnuswaker/sounds/588242/ | magnuswaker | CC0 |
| `creatures/sentry-arc-hit-1.wav` | Freesound, "Civilian Stun Gun 1/10s zap" (all of it from its attack, the last 0.12 s faded), https://freesound.org/people/AnthonyChan0/sounds/264779/ | AnthonyChan0 | CC0 |
| `crew/air-fill-1.wav` | generated for Stillwake: white noise (mulberry32 seed 47) through two band-passes, Q 1.1, their centre rising from 2.6 to 4.8 kHz, 0.55 s: in over 50 ms, held, falling away over the last 38 %, to −3 dBFS peak (a placeholder till an audition) | Stillwake team | CC0 |
| `crew/alarm-1.wav` | generated for Stillwake | Stillwake team | CC0 |
| `crew/axe-swing-1.wav` | Freesound, "Swinging staff whoosh (strong) 04.wav", https://freesound.org/people/Nightflame/sounds/422513/ | Nightflame | CC0 |
| `crew/axe-swing-2.wav` | Freesound, "Swinging staff whoosh (strong) 06.wav", https://freesound.org/people/Nightflame/sounds/422511/ | Nightflame | CC0 |
| `crew/axe-swing-3.wav` | Freesound, "Swinging staff whoosh (strong) 01.wav", https://freesound.org/people/Nightflame/sounds/422506/ | Nightflame | CC0 |
| `crew/axe-swing-4.wav` | Freesound, "Swinging staff whoosh (strong) 03.wav", https://freesound.org/people/Nightflame/sounds/422508/ | Nightflame | CC0 |
| `crew/harpoon-ready-1.wav` | Freesound, ".22 Bolt Action Long Rifle Rechambering" (0.95 s from its start), https://freesound.org/people/AlexCoover/sounds/706404/ | AlexCoover | CC0 |
| `crew/harpoon-reel-1.ogg` | Freesound, "reeling.wav" (2.45 s, looped), https://freesound.org/people/BranndyBottle/sounds/464701/ | BranndyBottle | CC0 |
| `crew/harpoon-shot-1.wav` | Freesound, "Dart/Arrow Trap." (0.85 s from its attack), https://freesound.org/people/jacobo1304/sounds/866990/ | jacobo1304 | CC0 |
| `crew/nailer-dry-1.wav` | Freesound, "A rifle being dry fired once" (0.25 s from its attack), https://freesound.org/people/ser%C3%B8ut%C5%8Dnin--depriv%C9%99d/sounds/725402/ | serøutōnin--deprivəd | CC0 |
| `crew/nailer-hit-1.wav` | Freesound, "Small_Bullet_Very_Soft_Hit_07" (0.3 s from its attack), https://freesound.org/people/BlondPanda/sounds/778028/ | BlondPanda | CC0 |
| `crew/nailer-ready-1.wav` | Freesound, "ClipLoad2.wav" (all of it from its attack), https://freesound.org/people/BMacZero/sounds/94119/ | BMacZero | CC0 |
| `crew/nailer-shot-1.wav` | Freesound, "air_nailer_single_shot_1.wav" (0.55 s from its attack), https://freesound.org/people/reg7783/sounds/131304/ | reg7783 | CC0 |
| `crew/railgun-charge-1.wav` | Freesound, "Laser Charge Up" (all of it from its attack, the last 0.16 s faded), https://freesound.org/people/magnuswaker/sounds/588242/ | magnuswaker | CC0 |
| `crew/railgun-ready-1.wav` | Freesound, "A relay switch being activated." (all of it), https://freesound.org/people/bassmosphere/sounds/384698/ | bassmosphere | CC0 |
| `crew/railgun-shot-1.wav` | Freesound, "Sci FI Gun.wav" (all of it from its attack), https://freesound.org/people/michael_grinnell/sounds/512480/ | michael_grinnell | CC0 |
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
| `crew/land-hard-1.wav` | Freesound, "Footsteps_Mountain_Boots_Rock_Jump_Sequence_Mono.wav" (0.4 s from 0.463 s), https://freesound.org/people/Nox_Sound/sounds/558477/ | Nox_Sound | CC0 |
| `crew/land-hard-2.wav` | Freesound, "Footsteps_Mountain_Boots_Rock_Jump_Sequence_Mono.wav" (0.4 s from 1.997 s), https://freesound.org/people/Nox_Sound/sounds/558477/ | Nox_Sound | CC0 |
| `crew/land-hard-3.wav` | Freesound, "Footsteps_Mountain_Boots_Rock_Jump_Sequence_Mono.wav" (0.4 s from 6.508 s), https://freesound.org/people/Nox_Sound/sounds/558477/ | Nox_Sound | CC0 |
| `crew/land-soft-1.wav` | Freesound, "land a jump.wav" (0.307 s from 0.001 s), https://freesound.org/people/zepurple/sounds/540272/ | zepurple | CC0 |
| `crew/lava-burn-1.wav` | Freesound, "Pressure Hiss" (0.8 s), https://freesound.org/people/unfa/sounds/211096/ | unfa | CC0 |
| `crew/step-alloy-1.wav` | Freesound, "Boots_On_Hollow_Metal_Light_01" (0.3 s), https://freesound.org/people/BlondPanda/sounds/778413/ | BlondPanda | CC0 |
| `crew/step-alloy-2.wav` | Freesound, "Boots_On_Hollow_Metal_Light_02" (0.3 s), https://freesound.org/people/BlondPanda/sounds/778414/ | BlondPanda | CC0 |
| `crew/step-alloy-3.wav` | Freesound, "Boots_On_Hollow_Metal_Light_04" (0.3 s), https://freesound.org/people/BlondPanda/sounds/778415/ | BlondPanda | CC0 |
| `crew/step-alloy-4.wav` | Freesound, "Boots_On_Hollow_Metal_Light_05" (0.3 s), https://freesound.org/people/BlondPanda/sounds/778416/ | BlondPanda | CC0 |
| `crew/step-alloy-5.wav` | Freesound, "Boots_On_Hollow_Metal_Light_06" (0.3 s), https://freesound.org/people/BlondPanda/sounds/778417/ | BlondPanda | CC0 |
| `crew/step-grass-1.wav` | Freesound, "Hiking Boot Footsteps on Grass" (0.3 s from 0.103 s), https://freesound.org/people/Fission9/sounds/521587/ | Fission9 | CC0 |
| `crew/step-grass-2.wav` | Freesound, "Hiking Boot Footsteps on Grass" (0.3 s from 0.801 s), https://freesound.org/people/Fission9/sounds/521587/ | Fission9 | CC0 |
| `crew/step-grass-3.wav` | Freesound, "Hiking Boot Footsteps on Grass" (0.3 s from 1.491 s), https://freesound.org/people/Fission9/sounds/521587/ | Fission9 | CC0 |
| `crew/step-grass-4.wav` | Freesound, "Hiking Boot Footsteps on Grass" (0.3 s from 2.277 s), https://freesound.org/people/Fission9/sounds/521587/ | Fission9 | CC0 |
| `crew/step-grass-5.wav` | Freesound, "Hiking Boot Footsteps on Grass" (0.3 s from 3.055 s), https://freesound.org/people/Fission9/sounds/521587/ | Fission9 | CC0 |
| `crew/step-grass-6.wav` | Freesound, "Hiking Boot Footsteps on Grass" (0.3 s from 3.796 s), https://freesound.org/people/Fission9/sounds/521587/ | Fission9 | CC0 |
| `crew/step-soil-1.wav` | Freesound, "Footsteps_Dirt_Single_05.wav" (0.3 s), https://freesound.org/people/BenDrain/sounds/487862/ | BenDrain | CC0 |
| `crew/step-soil-2.wav` | Freesound, "Footsteps_Dirt_Single_06.wav" (0.3 s from 0.037 s), https://freesound.org/people/BenDrain/sounds/487863/ | BenDrain | CC0 |
| `crew/step-soil-3.wav` | Freesound, "Footsteps_Dirt_Single_07.wav" (0.3 s from 0.011 s), https://freesound.org/people/BenDrain/sounds/487864/ | BenDrain | CC0 |
| `crew/step-soil-4.wav` | Freesound, "Footsteps_Dirt_Single_04.wav" (0.3 s), https://freesound.org/people/BenDrain/sounds/487869/ | BenDrain | CC0 |
| `crew/step-stone-1.wav` | Freesound, "Hiking Boot Footsteps on Stone" (0.3 s from 0.095 s), https://freesound.org/people/Fission9/sounds/521590/ | Fission9 | CC0 |
| `crew/step-stone-2.wav` | Freesound, "Hiking Boot Footsteps on Stone" (0.3 s from 0.692 s), https://freesound.org/people/Fission9/sounds/521590/ | Fission9 | CC0 |
| `crew/step-stone-3.wav` | Freesound, "Hiking Boot Footsteps on Stone" (0.3 s from 1.255 s), https://freesound.org/people/Fission9/sounds/521590/ | Fission9 | CC0 |
| `crew/step-stone-4.wav` | Freesound, "Hiking Boot Footsteps on Stone" (0.3 s from 1.807 s), https://freesound.org/people/Fission9/sounds/521590/ | Fission9 | CC0 |
| `crew/step-stone-5.wav` | Freesound, "Hiking Boot Footsteps on Stone" (0.3 s from 2.395 s), https://freesound.org/people/Fission9/sounds/521590/ | Fission9 | CC0 |
| `crew/step-stone-6.wav` | Freesound, "Hiking Boot Footsteps on Stone" (0.3 s from 3.014 s), https://freesound.org/people/Fission9/sounds/521590/ | Fission9 | CC0 |
| `debris/break-1.wav` | Freesound, "Car Crash (with Glass)", https://freesound.org/people/magnuswaker/sounds/592388/ | magnuswaker | CC0 |
| `debris/break-2.wav` | Freesound, "Loud Bang Crashing Metal Chunk Dropped in Metal Dumpster", https://freesound.org/people/qubodup/sounds/859154/ | qubodup | CC0 |
| `debris/break-3.wav` | Freesound, "metallic crash.wav", https://freesound.org/people/BehanSean/sounds/422438/ | BehanSean | CC0 |
| `debris/chop-1.wav` | Freesound, "axe hits - 1.wav", https://freesound.org/people/quatricise/sounds/582372/ | quatricise | CC0 |
| `debris/chop-2.wav` | Freesound, "shield guard", https://freesound.org/people/nekoninja/sounds/370203/ | nekoninja | CC0 |
| `debris/chop-3.wav` | Freesound, "slightmetallicthud.mp3", https://freesound.org/people/profoundsounds/sounds/686282/ | profoundsounds | CC0 |
| `debris/drill-whir-1.wav` | Freesound, "DeWalt 18V drill, no-load spins" (150 ms at 6.3 s), https://freesound.org/people/megashroom/sounds/390162/ | megashroom | CC0 |
| `debris/drill-whir-2.wav` | Freesound, "DeWalt 18V drill, no-load spins" (150 ms at 6.62 s), https://freesound.org/people/megashroom/sounds/390162/ | megashroom | CC0 |
| `debris/drill-whir-3.wav` | Freesound, "DeWalt 18V drill, no-load spins" (150 ms at 6.95 s), https://freesound.org/people/megashroom/sounds/390162/ | megashroom | CC0 |
| `flora/bush-1.wav` | Freesound, "bushhit.wav" (0.6 s from 0.003 s), https://freesound.org/people/j1987/sounds/106113/ | j1987 | CC0 |
| `flora/cave-drip-1.wav` | Freesound, "Water Drop 01" (0.187 s from 0.003 s), https://freesound.org/people/LilMati/sounds/348022/ | LilMati | CC0 |
| `flora/cave-drip-2.wav` | Freesound, "Water Drop 02" (0.119 s), https://freesound.org/people/LilMati/sounds/349873/ | LilMati | CC0 |
| `flora/chop-wood-1.wav` | Freesound, "Chopping wood 01" (0.226 s from 0.045 s), https://freesound.org/people/micahlg/sounds/421928/ | micahlg | CC0 |
| `flora/chop-wood-2.wav` | Freesound, "Chopping wood 02" (0.206 s from 0.041 s), https://freesound.org/people/micahlg/sounds/421929/ | micahlg | CC0 |
| `flora/crystal-1.wav` | Freesound, "Breaking icicles 4" (0.7 s from 0.291 s), https://freesound.org/people/AugustSandberg/sounds/677718/ | AugustSandberg | CC0 |
| `flora/crystal-2.wav` | Freesound, "Breaking icicles 4" (0.7 s from 5.132 s), https://freesound.org/people/AugustSandberg/sounds/677718/ | AugustSandberg | CC0 |
| `flora/crystal-3.wav` | Freesound, "Breaking icicles 4" (0.7 s from 7.855 s), https://freesound.org/people/AugustSandberg/sounds/677718/ | AugustSandberg | CC0 |
| `flora/crystal-4.wav` | Freesound, "Breaking icicles 4" (0.7 s from 9.512 s), https://freesound.org/people/AugustSandberg/sounds/677718/ | AugustSandberg | CC0 |
| `flora/glowcap-1.wav` | Freesound, "Squelching SFX [6]" (0.5 s from 0.145 s), https://freesound.org/people/SoundDesignForYou/sounds/649982/ | SoundDesignForYou | CC0 |
| `flora/grass-1.wav` | Freesound, "to cut and drag the grass with a knife and sickle.aif" (0.35 s from 1.255 s), https://freesound.org/people/hadescolossus/sounds/705228/ | hadescolossus | CC0 |
| `flora/grass-2.wav` | Freesound, "to cut and drag the grass with a knife and sickle.aif" (0.35 s from 2.548 s), https://freesound.org/people/hadescolossus/sounds/705228/ | hadescolossus | CC0 |
| `flora/grass-3.wav` | Freesound, "to cut and drag the grass with a knife and sickle.aif" (0.312 s from 3.693 s), https://freesound.org/people/hadescolossus/sounds/705228/ | hadescolossus | CC0 |
| `flora/grass-4.wav` | Freesound, "to cut and drag the grass with a knife and sickle.aif" (0.35 s from 3.995 s), https://freesound.org/people/hadescolossus/sounds/705228/ | hadescolossus | CC0 |
| `flora/tree-fall-1.wav` | Freesound, "A Tree Falling Down.wav" (3 s), https://freesound.org/people/ecfike/sounds/139952/ | ecfike | CC0 |
| `furnace/fire-1.ogg` | Freesound, "Fire - Chimney / Stove", https://freesound.org/people/Vrymaa/sounds/770108/ | Vrymaa | CC0 |
| `grid/door-close-1.wav` | Freesound, "SFX Door Close Big.wav", https://freesound.org/people/Paul368/sounds/264060/ | Paul368 | CC0 |
| `grid/door-open-1.wav` | Freesound, "SFX Door Open.wav", https://freesound.org/people/Paul368/sounds/264061/ | Paul368 | CC0 |
| `grid/valve-1.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-2.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-3.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/lava-bed-1.ogg` | generated for Stillwake: white noise (mulberry32 seed 735) low-passed twice at 70 Hz and high-passed at 25 Hz, swelling ±20% at 0.5 Hz, a 4 s loop rendered over two periods of the same noise and the second kept, −26 LUFS momentary (the audition's "Synth: the rumble alone") | Stillwake team | CC0 |
| `grid/lava-bubble-1.wav` | Freesound, "Hot Bubbling Mud" (0.335 s from 2.01 s), https://freesound.org/people/Breviceps/sounds/456806/ | Breviceps | CC0 |
| `grid/lava-bubble-2.wav` | Freesound, "Hot Bubbling Mud" (0.35 s from 2.675 s), https://freesound.org/people/Breviceps/sounds/456806/ | Breviceps | CC0 |
| `grid/lava-bubble-3.wav` | Freesound, "Hot Bubbling Mud" (0.35 s from 4.965 s), https://freesound.org/people/Breviceps/sounds/456806/ | Breviceps | CC0 |
| `grid/lava-bubble-4.wav` | Freesound, "Hot Bubbling Mud" (0.35 s from 5.97 s), https://freesound.org/people/Breviceps/sounds/456806/ | Breviceps | CC0 |
| `grid/lava-bubble-5.wav` | Freesound, "Hot Bubbling Mud" (0.325 s from 6.77 s), https://freesound.org/people/Breviceps/sounds/456806/ | Breviceps | CC0 |
| `grid/lava-bubble-6.wav` | Freesound, "Hot Bubbling Mud" (0.35 s from 11.025 s), https://freesound.org/people/Breviceps/sounds/456806/ | Breviceps | CC0 |
| `hands/act-1.wav` | Freesound, "Lego Click (short)", https://freesound.org/people/ImmergoMedia/sounds/670000/ | ImmergoMedia | CC0 |
| `hands/act-space-1.wav` | Freesound, "LowHit_01.wav", https://freesound.org/people/Faulkin/sounds/336495/ | Faulkin | CC0 |
| `logistics/drone-dock-1.wav` | Impact Sounds, `impactSoft_medium_000.ogg`, https://kenney.nl/assets/impact-sounds, with Freesound "Latch_04.wav", https://freesound.org/people/Q.K./sounds/56246/, 60 ms after it, 6 dB under | Kenney, Q.K. | CC0 |
| `logistics/drone-dock-2.wav` | Impact Sounds, `impactSoft_medium_002.ogg`, https://kenney.nl/assets/impact-sounds, with Freesound "Latch_04.wav", https://freesound.org/people/Q.K./sounds/56246/, 60 ms after it, 6 dB under | Kenney, Q.K. | CC0 |
| `logistics/drone-dock-3.wav` | Impact Sounds, `impactSoft_medium_004.ogg`, https://kenney.nl/assets/impact-sounds, with Freesound "Latch_04.wav", https://freesound.org/people/Q.K./sounds/56246/, 60 ms after it, 6 dB under | Kenney, Q.K. | CC0 |
| `logistics/drone-grab-1.wav` | UI Audio, `switch10.ogg`, https://kenney.nl/assets/ui-audio | Kenney | CC0 |
| `logistics/drone-grab-2.wav` | UI Audio, `switch20.ogg`, https://kenney.nl/assets/ui-audio | Kenney | CC0 |
| `logistics/drone-grab-3.wav` | UI Audio, `switch1.ogg`, https://kenney.nl/assets/ui-audio | Kenney | CC0 |
| `logistics/drone-whir-1.ogg` | generated for Stillwake: noise band-passed at 450 Hz chopped ±25% 37.5 times a second, noise low-passed at 250 Hz under it and a faint 112.5 Hz tone, a 2 s loop rendered offline to −23 dBFS RMS (the audition's "Fan") | Stillwake team | CC0 |
| `music/space-1.ogg` | generated for Stillwake: the synthesised drone "Wreck" (v2), 80 s looping seamlessly, about −18 LUFS; a placeholder for the bed of space (ADR 0042) until the team writes its own | Stillwake team | CC0 |
| `music/title-1.ogg` | made by the team with Suno (suno.com) on its free plan, 2026-10-16, normalised to about −18 LUFS | Stillwake team | Suno Basic, non-commercial: replace before sale |
| `power/charge-full-1.wav` | Freesound, "UI Confirm" (0.27 s from its attack), https://freesound.org/people/unfa/sounds/584184/ | unfa | CC0 |
| `power/charger-hum-1.ogg` | generated for Stillwake: 50 Hz and its harmonics at 0.7, 1, 0.4 and 0.15, low-passed at 500 Hz, swaying ±4% at 0.5 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Warm 50 Hz") | Stillwake team | CC0 |
| `ruins/music-box-1.wav` | generated for Stillwake (tools/homes-art/export.js): a plucked steel tine, a sine at 1568 Hz with inharmonic partials at 2.76, 5.40 and 8.93 times it, each decaying, 0.9 s, to −3 dBFS peak; played at a tune's pitches (a placeholder till an audition, 0091) | Stillwake team | CC0 |
| `ruins/zap-1.wav` | generated for Stillwake (tools/homes-art/export.js): bursts of smoothed noise a few milliseconds each under a quarter-second fall, to −3 dBFS peak (a placeholder till an audition, 0091) | Stillwake team | CC0 |
| `ruins/ceramic-break-1.wav` | Freesound, "Tile Breaking" (0.737 s), https://freesound.org/people/Fission9/sounds/583310/ | Fission9 | CC0 |
| `ruins/cloth-tear-1.wav` | Freesound, "cloth_rip.mp3" (0.484 s from 0.051 s), https://freesound.org/people/Johnny2810/sounds/507756/ | Johnny2810 | CC0 |
| `ruins/glass-break-1.wav` | Freesound, "Glass Break" (1.2 s from 0.333 s), https://freesound.org/people/unfa/sounds/221528/ | unfa | CC0 |
| `ruins/metal-break-1.wav` | Freesound, "Dropped Metal pot" (1 s from 0.01 s), https://freesound.org/people/gamer500/sounds/687246/ | gamer500 | CC0 |
| `ruins/metal-break-2.wav` | Freesound, "Dropped Metal pot" (1 s from 0.805 s), https://freesound.org/people/gamer500/sounds/687246/ | gamer500 | CC0 |
| `ruins/wood-break-1.wav` | Freesound, "Crate Break 4.wav" (1 s from 0.005 s), https://freesound.org/people/kevinkace/sounds/66780/ | kevinkace | CC0 |
| `ui/click-1.wav` | Interface Sounds, `click_002.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
| `ui/click-2.wav` | Interface Sounds, `click_003.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
