# Audio credits

Every sound under `game/audio`: where it came from, who made it and its licence. CC0 only (ADR 0033), but for the team's own music made on Suno, marked as a placeholder (ADR 0035):
the repository is public, so a file must be free to redistribute without attribution. A test
(`game.tests/SoundTests.cs`) fails on a file missing here or under another licence.

Kenney's and Freesound's files are converted to mono WAV, trimmed and normalised to −3 dBFS peak;
Freesound's are its public HQ previews of CC0 sounds. The chop's and the break's variants are then matched by the RMS of
their K-weighted attack (the first 0.15 s of a chop, 0.4 s of a break), the louder turned down. `chop-1`
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
| `crew/land-1.wav` | Impact Sounds, `impactSoft_heavy_000.ogg`, https://kenney.nl/assets/impact-sounds | Kenney | CC0 |
| `crew/land-2.wav` | Impact Sounds, `impactSoft_heavy_002.ogg`, https://kenney.nl/assets/impact-sounds | Kenney | CC0 |
| `crew/step-1.wav` | Impact Sounds, `footstep_concrete_000.ogg`, https://kenney.nl/assets/impact-sounds | Kenney | CC0 |
| `crew/step-2.wav` | Impact Sounds, `footstep_concrete_001.ogg`, https://kenney.nl/assets/impact-sounds | Kenney | CC0 |
| `crew/step-3.wav` | Impact Sounds, `footstep_concrete_002.ogg`, https://kenney.nl/assets/impact-sounds | Kenney | CC0 |
| `crew/step-4.wav` | Impact Sounds, `footstep_concrete_003.ogg`, https://kenney.nl/assets/impact-sounds | Kenney | CC0 |
| `debris/break-1.wav` | Freesound, "Car Crash (with Glass)", https://freesound.org/people/magnuswaker/sounds/592388/ | magnuswaker | CC0 |
| `debris/break-2.wav` | Freesound, "Loud Bang Crashing Metal Chunk Dropped in Metal Dumpster", https://freesound.org/people/qubodup/sounds/859154/ | qubodup | CC0 |
| `debris/break-3.wav` | Freesound, "metallic crash.wav", https://freesound.org/people/BehanSean/sounds/422438/ | BehanSean | CC0 |
| `debris/chop-1.wav` | Freesound, "axe hits - 1.wav", https://freesound.org/people/quatricise/sounds/582372/ | quatricise | CC0 |
| `debris/chop-2.wav` | Freesound, "shield guard", https://freesound.org/people/nekoninja/sounds/370203/ | nekoninja | CC0 |
| `debris/chop-3.wav` | Freesound, "slightmetallicthud.mp3", https://freesound.org/people/profoundsounds/sounds/686282/ | profoundsounds | CC0 |
| `furnace/fire-1.ogg` | Freesound, "Fire - Chimney / Stove", https://freesound.org/people/Vrymaa/sounds/770108/ | Vrymaa | CC0 |
| `grid/door-close-1.wav` | Freesound, "SFX Door Close Big.wav", https://freesound.org/people/Paul368/sounds/264060/ | Paul368 | CC0 |
| `grid/door-open-1.wav` | Freesound, "SFX Door Open.wav", https://freesound.org/people/Paul368/sounds/264061/ | Paul368 | CC0 |
| `grid/valve-1.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-2.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `grid/valve-3.wav` | Freesound, "Squeak Rusty Metal Water Tap", https://freesound.org/people/brunoboselli/sounds/441096/ | brunoboselli | CC0 |
| `hands/act-1.wav` | Freesound, "Lego Click (short)", https://freesound.org/people/ImmergoMedia/sounds/670000/ | ImmergoMedia | CC0 |
| `music/space-1.ogg` | generated for Stillwake: the synthesised drone "Wreck" (v2), 80 s looping seamlessly, about −18 LUFS; a placeholder for the bed of space (ADR 0041) until the team writes its own | Stillwake team | CC0 |
| `music/title-1.ogg` | made by the team with Suno (suno.com) on its free plan, 2026-10-16, normalised to about −18 LUFS | Stillwake team | Suno Basic, non-commercial: replace before sale |
| `ui/click-1.wav` | Interface Sounds, `click_002.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
| `ui/click-2.wav` | Interface Sounds, `click_003.ogg`, https://kenney.nl/assets/interface-sounds | Kenney | CC0 |
