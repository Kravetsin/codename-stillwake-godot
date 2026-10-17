# Audio credits

Every sound under `game/audio`: where it came from, who made it and its licence. CC0 only (ADR 0033), but for the team's own music made on Suno, marked as a placeholder (ADR 0035):
the repository is public, so a file must be free to redistribute without attribution. A test
(`game.tests/SoundTests.cs`) fails on a file missing here or under another licence.

Kenney's and Freesound's files are converted to mono WAV, trimmed and normalised to −3 dBFS peak;
Freesound's are its public HQ previews of CC0 sounds. The chop's and the break's variants are then matched by the RMS of
their K-weighted attack (the first 0.15 s of a chop, 0.4 s of a break), the louder turned down. The steps (which the landing plays too) are 0.3 s of each recording from its attack, held 0.08 s and then faded to −60 dB; the jump is the push-off of each `Jump_Metal`, 0.33 s from its attack after 0.1 s in, high-passed at 120 Hz, held 0.1 s; the glove's thump in space, `act-space-1`, is all of `LowHit_01` from its attack (0.14 s), held 0.04 s, raised 21 dB to its peak. The harpoon's shot and ready are cut from their attack, held and faded to −60 dB; its reel is 2.45 s of its source with short fades, looped. The drill's strike is 50 ms of its source from its attack, held 12 ms, decayed by 40 dB and faded over its last 8 ms, so eighteen a second never smear; it and the charge-done blip are normalised to −3 dBFS peak. The thrust and the charger's hum are rendered offline over two periods of their loop, the second kept, so they close on themselves, encoded as OGG. `chop-1`
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
| `crew/alarm-1.wav` | generated for Stillwake | Stillwake team | CC0 |
| `crew/breathe-1.ogg` | the first 0.7 s of `atmosphere/hiss-1.ogg`, faded out (a placeholder till an audition) | Stillwake team | CC0 |
| `crew/harpoon-ready-1.wav` | Freesound, ".22 Bolt Action Long Rifle Rechambering" (0.95 s from its start), https://freesound.org/people/AlexCoover/sounds/706404/ | AlexCoover | CC0 |
| `crew/harpoon-reel-1.ogg` | Freesound, "reeling.wav" (2.45 s, looped), https://freesound.org/people/BranndyBottle/sounds/464701/ | BranndyBottle | CC0 |
| `crew/harpoon-shot-1.wav` | Freesound, "Dart/Arrow Trap." (0.85 s from its attack), https://freesound.org/people/jacobo1304/sounds/866990/ | jacobo1304 | CC0 |
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
| `debris/drill-strike-1.wav` | Freesound, "Pickaxe3" (50 ms from its attack, held 12 ms, faded out), https://freesound.org/people/NoisyRedFox/sounds/760567/ | NoisyRedFox | CC0 |
| `furnace/fire-1.ogg` | Freesound, "Fire - Chimney / Stove", https://freesound.org/people/Vrymaa/sounds/770108/ | Vrymaa | CC0 |
| `grid/door-close-1.wav` | Freesound, "SFX Door Close Big.wav", https://freesound.org/people/Paul368/sounds/264060/ | Paul368 | CC0 |
| `grid/door-open-1.wav` | Freesound, "SFX Door Open.wav", https://freesound.org/people/Paul368/sounds/264061/ | Paul368 | CC0 |
| `grid/valve-1.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-2.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-3.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `hands/act-1.wav` | Freesound, "Lego Click (short)", https://freesound.org/people/ImmergoMedia/sounds/670000/ | ImmergoMedia | CC0 |
| `hands/act-space-1.wav` | Freesound, "LowHit_01.wav", https://freesound.org/people/Faulkin/sounds/336495/ | Faulkin | CC0 |
| `music/space-1.ogg` | generated for Stillwake: the synthesised drone "Wreck" (v2), 80 s looping seamlessly, about −18 LUFS; a placeholder for the bed of space (ADR 0042) until the team writes its own | Stillwake team | CC0 |
| `music/title-1.ogg` | made by the team with Suno (suno.com) on its free plan, 2026-10-16, normalised to about −18 LUFS | Stillwake team | Suno Basic, non-commercial: replace before sale |
| `power/charge-full-1.wav` | Freesound, "UI Confirm" (0.27 s from its attack), https://freesound.org/people/unfa/sounds/584184/ | unfa | CC0 |
| `power/charger-hum-1.ogg` | generated for Stillwake: 50 Hz and its harmonics at 0.7, 1, 0.4 and 0.15, low-passed at 500 Hz, swaying ±4% at 0.5 Hz, a 4 s loop rendered offline to −23 dBFS RMS (the audition's "Warm 50 Hz") | Stillwake team | CC0 |
| `ui/click-1.wav` | Interface Sounds, `click_002.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
| `ui/click-2.wav` | Interface Sounds, `click_003.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
