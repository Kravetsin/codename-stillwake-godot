// ---------- the system: data that makes a planet's backdrop ----------
// A biome is a palette and prop weights; a planet type is a sky, three biomes and its weather; a planet is a type and a seed.
// A direction is a composition: how many layers, their parallax and height, which sky events. Art is only the props library.

const BIOMES = {
  sav: { name: 'Саванна', far: '#7f9fb4', near: '#466f6a', rim: '#d8ead8', strata: null, shapeFar: 'round', shapeMesa: 'mesa', glow: ['#9ff0dc', '#ffd27a'], flora: ['#7fe8d4', '#b49aff', '#ffd27a'], props: { acacia: 6, flora: 1 }, leaf: null, moss: ['#b8e2b0', '#9ccf9c', '#78b48a', '#55906a', '#3f6f52'], soil: [1, 1, 1], fg: { tree: 0.09, bush: 0.05, rock: 0.02, bell: 0.15 }, map: '#5f9c74' },
  dry: { name: 'Сухая степь', far: '#a8a89a', near: '#76703e', rim: '#f4e6b0', strata: null, shapeFar: 'round', shapeMesa: 'mesa', glow: ['#ffd27a', '#ffe9a8'], flora: ['#ffd27a', '#ff9a6a'], props: { acacia: 3, bare: 2, flora: 1 }, leaf: ['#2e3a1e', '#4e5a26', '#6e7a30', '#98a040', '#c8c870'], moss: ['#e8e0a0', '#c8c078', '#a89a58', '#7e7440', '#5a5230'], soil: [1.05, 0.98, 0.9], fg: { tree: 0.05, bush: 0.06, rock: 0.04, bell: 0.08 }, map: '#a8a060' },
  bad: { name: 'Бэдлендс', far: '#aa9ca2', near: '#8c5644', rim: '#f6d2a6', strata: '#5e3428', shapeFar: 'mesa', shapeMesa: 'mesa', glow: ['#ffd27a', '#ffb36a'], flora: ['#ffd27a', '#ff9a6a'], props: { hoodoo: 5, acacia: 1, bare: 1 }, leaf: ['#3a2418', '#6a3a22', '#9a5a2a', '#c8803a', '#e8b060'], moss: ['#f0c890', '#d8a070', '#b07a50', '#80583a', '#5a3a28'], soil: [1.08, 0.92, 0.82], fg: { tree: 0.025, bush: 0.03, rock: 0.08, bell: 0.1 }, map: '#b07a5a' },
  cry: { name: 'Кристальные поля', far: '#949ece', near: '#47467c', rim: '#e4e0ff', strata: null, shapeFar: 'jag', shapeMesa: 'jag', glow: ['#9ff0f0', '#d7a8ff'], flora: ['#9ff0f0', '#d7a8ff'], props: { crystal: 6, flora: 1 }, leaf: ['#1e1a3e', '#3a2e6a', '#5a4a9a', '#8a78d0', '#c8b8ff'], moss: ['#d8d0ff', '#b0a8e8', '#8a80c8', '#645aa0', '#463e78'], soil: [0.9, 0.88, 1.05], fg: { tree: 0.02, bush: 0.03, rock: 0.03, bell: 0.16 }, map: '#7a74c0' },
  ice: { name: 'Ледник', far: '#a8c0d8', near: '#557696', rim: '#f4fbff', strata: '#88a6c0', shapeFar: 'jag', shapeMesa: 'mesa', glow: ['#bfe8ff', '#e6fffb'], flora: ['#bfe8ff', '#9ff0f0'], props: { crystal: 3, bare: 3 }, leaf: ['#2a3a4e', '#4a6078', '#7a98b4', '#b4cce0', '#eef6ff'], moss: ['#ffffff', '#e4eef8', '#c4d6e8', '#9ab4cc', '#6e8aa6'], soil: [0.85, 0.92, 1.08], fg: { tree: 0.02, bush: 0.02, rock: 0.05, bell: 0.04 }, map: '#a8c8e0' },
  volc: { name: 'Вулканы', far: '#8a7a80', near: '#3c2c2e', rim: '#ff9a5a', strata: '#24181a', shapeFar: 'cone', shapeMesa: 'cone', glow: ['#ff8a3a', '#ffd27a'], flora: ['#ff8a3a'], props: { hoodoo: 3, bare: 3 }, crater: '#ffb04a', leaf: ['#1a1012', '#3a1a16', '#6a2a1a', '#a8401c', '#ff8a3a'], moss: ['#9a8a84', '#7a6a66', '#5a4c4a', '#403434', '#2a2224'], soil: [0.75, 0.62, 0.62], fg: { tree: 0.02, bush: 0.01, rock: 0.08, bell: 0.05 }, map: '#8a4a3a' },
  ash: { name: 'Пепельные равнины', far: '#9a9494', near: '#55504e', rim: '#e0d6cc', strata: '#3a3434', shapeFar: 'round', shapeMesa: 'mesa', glow: ['#ffb04a'], flora: ['#ffb04a'], props: { bare: 4, hoodoo: 1 }, leaf: ['#1e1a1a', '#3a3432', '#5a524e', '#8a807a', '#b8aea4'], moss: ['#c8c0b8', '#a8a098', '#888078', '#68605a', '#48423e'], soil: [0.8, 0.76, 0.74], fg: { tree: 0.03, bush: 0.02, rock: 0.06, bell: 0.04 }, map: '#6a6460' },
  fung: { name: 'Грибной лес', far: '#a090b8', near: '#5a3a6a', rim: '#ffd8f0', strata: null, shapeFar: 'round', shapeMesa: 'soft', glow: ['#ff9ae0', '#9ff0dc', '#d7a8ff'], flora: ['#ff9ae0', '#9ff0dc'], props: { mushroom: 6, flora: 2 }, leaf: ['#2a1430', '#4e2458', '#7a3a88', '#b060b8', '#f0a0e0'], moss: ['#f0c8f0', '#d0a0d8', '#a878b8', '#7a5090', '#583670'], soil: [0.95, 0.85, 1.0], fg: { tree: 0.04, bush: 0.05, rock: 0.02, bell: 0.2 }, map: '#a060b0' },
};

const SKIES = {
  teal: { ramp: ['#3e6fb0', '#5f93c8', '#93bfdc', '#cfe0dc', '#f1e3c2'], haze: '#e9e2c8' },
  jade: { ramp: ['#2f6a8e', '#4f92aa', '#88c2c0', '#cfe4cc', '#f0e6c0'], haze: '#e2e6c8' },
  ochre: { ramp: ['#5a7aa8', '#8aa0b8', '#c0b8a8', '#e8d0a0', '#f6e0b0'], haze: '#efd8a8' },
  violet: { ramp: ['#3a3a7a', '#5c5aa0', '#8e88c4', '#c8b8dc', '#f0dcd4'], haze: '#e8d4dc' },
  ice: { ramp: ['#4a72b0', '#709cd0', '#a8cce8', '#dcecf4', '#f6f6f0'], haze: '#e8eef0' },
  rust: { ramp: ['#3e3a58', '#6e5468', '#a87468', '#d89870', '#f0bc84'], haze: '#e0ac88' },
};
const GIANTS = { pale: ['#c9d8d4', '#b3c8cc', '#dbe3d6'], warm: ['#e8d0b0', '#d0b090', '#f0e0c8'], cold: ['#d0e4f0', '#b0c8e0', '#e8f0f8'], rose: ['#e8c8d8', '#d0a8c0', '#f0dce8'] };

const PLANET_TYPES = {
  savanna: { name: 'Саванна', sky: 'teal', skies: ['teal', 'jade'], biomes: ['bad', 'sav', 'cry'], pool: ['dry', 'bad', 'cry', 'fung'], giant: 'pale', weather: { low: 'cumulus', aurora: 0, dust: '#d9b88a', motes: ['#fff0c0', '#9ff0dc'] } },
  badlands: { name: 'Бэдлендс', sky: 'ochre', skies: ['ochre', 'teal'], biomes: ['dry', 'bad', 'ash'], pool: ['dry', 'ash', 'sav', 'volc'], giant: 'warm', weather: { low: 'dust', aurora: 0, dust: '#d9a878', motes: ['#ffe0a0'] } },
  crystal: { name: 'Кристальный', sky: 'violet', skies: ['violet', 'ice'], biomes: ['ice', 'cry', 'fung'], pool: ['ice', 'fung', 'sav'], giant: 'rose', weather: { low: 'cumulus', aurora: 1, dust: '#c8b8f0', motes: ['#d7a8ff', '#9ff0f0'] } },
  ice: { name: 'Ледяной', sky: 'ice', skies: ['ice', 'violet'], biomes: ['cry', 'ice', 'ash'], pool: ['cry', 'ash', 'dry'], giant: 'cold', weather: { low: 'cumulus', aurora: 1, dust: '#e8f0f8', motes: ['#ffffff', '#bfe8ff'] } },
  volcanic: { name: 'Вулканический', sky: 'rust', skies: ['rust', 'ochre'], biomes: ['ash', 'volc', 'bad'], pool: ['ash', 'bad', 'dry'], giant: 'warm', weather: { low: 'ash', aurora: 0, dust: '#6a5656', motes: ['#ffb04a', '#ff7a3a'] } },
  fungal: { name: 'Грибной', sky: 'jade', skies: ['jade', 'violet'], biomes: ['sav', 'fung', 'cry'], pool: ['sav', 'cry', 'dry'], giant: 'rose', weather: { low: 'cumulus', aurora: 0, dust: '#e0c0e8', motes: ['#ff9ae0', '#9ff0dc'] } },
};
const TYPE_ORDER = ['savanna', 'badlands', 'crystal', 'ice', 'volcanic', 'fungal'];

function makePlanet(typeKey, seed, over = {}) {
  const T = PLANET_TYPES[typeKey], R = (k) => hash(seed, k, 4242);
  return {
    id: `${typeKey}:${seed}:${over.sky || ''}:${(over.biomes || []).join(',')}`, type: typeKey, typeName: T.name, seed, name: over.name || `${T.name} · ${seed}`,
    sky: over.sky || T.sky, biomes: over.biomes || T.biomes, weather: T.weather, giantBands: GIANTS[over.giant || T.giant],
    borders: [500 + Math.round(R(1) * 220), 1330 + Math.round(R(2) * 220)],
    gscale: 0.8 + R(3) * 0.45, gdx: (R(4) - 0.5) * 0.12, gdy: (R(5) - 0.5) * 0.08, gtilt: (R(6) - 0.5) * 0.4, ring: R(7) < 0.75, moons: Math.floor(R(8) * 3) - 1, sunDx: (R(9) - 0.5) * 0.1,
  };
}
function randomPlanet() {
  const seed = 1 + Math.floor(Math.random() * 99999), type = TYPE_ORDER[Math.floor(Math.random() * TYPE_ORDER.length)], T = PLANET_TYPES[type], R = (k) => hash(seed, k, 777);
  const sky = T.skies[R(1) < 0.65 ? 0 : 1], pick = (k) => T.pool[Math.floor(R(k) * T.pool.length)];
  const left = R(2) < 0.5 ? T.biomes[0] : pick(3), right = R(4) < 0.5 ? T.biomes[2] : pick(5);
  const letters = 'ВГДКЛМНРСТ', name = `${letters[Math.floor(R(6) * 10)]}${letters[Math.floor(R(7) * 10)]}-${100 + Math.floor(R(8) * 900)}`;
  return makePlanet(type, seed, { sky, biomes: [left, T.biomes[1], right === left ? T.biomes[2] : right], name: `${name} · ${T.name.toLowerCase()}` });
}
const PRESETS = Object.fromEntries(TYPE_ORDER.map((k, i) => [k, makePlanet(k, 11 + i * 37)]));

const DIRS = {
  now: { key: 'now', label: 'Сейчас', short: 'Сейчас в игре: три готовые картинки, одни на всю планету' },
  A: {
    key: 'A', label: 'A · Ясный простор', short: 'Развитие нынешнего: больше слоёв, рисунок вместо мягких пятен',
    sky: { giant: { fx: 0.16, fy: 0.24, r: 58, alpha: 0.6, tilt: -0.25 }, moons: [{ fx: 0.31, fy: 0.12, r: 6, a: 0.7 }, { fx: 0.58, fy: 0.07, r: 3, a: 0.6 }, { fx: 0.44, fy: 0.17, r: 4, a: 0.6 }], sun: { fx: 0.8, fy: 0.15, size: 1 }, stars: 0, ship: true },
    clouds: [{ kind: 'cirrus', follows: 0.05, drift: 1.2, y: 0.03, a: 0.55, seed: 41 }, { kind: 'low', follows: 0.075, drift: 2.4, y: 0.14, n: 8, seed: 7 }],
    land: [
      { kind: 'range', name: 'Дальний хребет со шпилями', follows: 0.08, peak: 150, shape: 'auto', haze: 0.72, spires: 2, spireScale: 2 },
      { kind: 'range', name: 'Столовые горы', follows: 0.12, peak: 104, shape: 'mesa', cell: 128, haze: 0.56, hazeBand: 0.22 },
      { kind: 'hills', name: 'Гряда с реквизитом', follows: 0.18, peak: 64, haze: 0.42, props: { n: 22, size: [12, 24] } },
      { kind: 'hills', name: 'Средние холмы, флора', follows: 0.26, peak: 58, haze: 0.28, props: { n: 13, size: [22, 40] }, flora: 6, floraSize: [12, 22], hazeBand: 0.18 },
      { kind: 'hills', name: 'Ближние холмы', follows: 0.36, peak: 52, cell: 256, haze: 0.14, props: { n: 7, size: [38, 64] }, flora: 10, floraSize: [16, 34], outline: true },
    ],
  },
  B: {
    key: 'B', label: 'B · Чужие формы', short: 'Парящие скалы, арки, гигантская флора — сразу видно, что не Земля',
    sky: { giant: { fx: 0.64, fy: 0.5, r: 120, alpha: 0.42, tilt: 0.14 }, moons: [{ fx: 0.2, fy: 0.13, r: 8, a: 0.7 }, { fx: 0.34, fy: 0.07, r: 4, a: 0.6 }], sun: { fx: 0.87, fy: 0.12, size: 1 }, stars: 0, ship: true },
    clouds: [{ kind: 'cirrus', follows: 0.05, drift: 1, y: 0.02, a: 0.45, seed: 43 }, { kind: 'dust', follows: 0.09, drift: 5, y: 0.3, a: 0.32, seed: 5 }],
    land: [
      { kind: 'range', name: 'Дальние плато с арками', follows: 0.07, peak: 132, shape: 'mesa', haze: 0.7, arches: 2, spires: 1, spireScale: 2 },
      { kind: 'floaters', name: 'Дальние парящие скалы', follows: 0.11, haze: 0.58, count: 9, size: [9, 20], ys: [-215, -130] },
      { kind: 'hills', name: 'Арки и останцы', follows: 0.17, peak: 82, haze: 0.44, arches: 3, props: { n: 10, size: [14, 26] }, hazeBand: 0.2 },
      { kind: 'floaters', name: 'Парящие скалы ближе', follows: 0.24, haze: 0.3, count: 5, size: [22, 40], ys: [-205, -120] },
      { kind: 'hills', name: 'Холмы с гигантской флорой', follows: 0.34, peak: 50, cell: 256, haze: 0.15, flora: 16, floraSize: [26, 64], props: { n: 3, size: [40, 60] }, outline: true },
    ],
  },
  C: {
    key: 'C', label: 'C · Небо-событие', short: 'Небо главное: огромный гигант, сияние, корабль; земля низкая', mood: 'deep',
    sky: { giant: { fx: 0.3, fy: 0.28, r: 104, alpha: 0.8, tilt: -0.32, ringShadow: true }, moons: [{ fx: 0.73, fy: 0.1, r: 9, a: 0.85 }, { fx: 0.81, fy: 0.23, r: 4, a: 0.75 }, { fx: 0.6, fy: 0.05, r: 5, a: 0.7 }], sun: { fx: 0.92, fy: 0.08, size: 0.7 }, stars: 0.45, aurora: true, ship: true },
    clouds: [{ kind: 'cirrus', follows: 0.05, drift: 1.4, y: 0.33, a: 0.32, seed: 47 }],
    land: [
      { kind: 'range', name: 'Низкий дальний горизонт', follows: 0.06, peak: 60, shape: 'auto', haze: 0.7 },
      { kind: 'range', name: 'Равнина шпилей', follows: 0.11, peak: 52, shape: 'soft', cell: 128, haze: 0.54, spires: 4, spireScale: 3, hazeBand: 0.2 },
      { kind: 'hills', name: 'Пологие холмы', follows: 0.2, peak: 34, haze: 0.34, props: { n: 16, size: [14, 26] } },
      { kind: 'hills', name: 'Ближний край', follows: 0.32, peak: 36, cell: 256, haze: 0.16, props: { n: 6, size: [30, 50] }, flora: 6, floraSize: [14, 26], outline: true },
    ],
  },
  D: {
    key: 'D', label: 'D · Дымка и столбы света', short: 'Настроение: низкое солнце, лучи, лес силуэтов в слоях тумана', mood: 'warm',
    sky: { giant: { fx: 0.18, fy: 0.18, r: 44, alpha: 0.45, tilt: -0.2 }, moons: [], sun: { fx: 0.7, fy: 0.4, size: 1.6 }, stars: 0, rays: true, ship: true },
    clouds: [{ kind: 'low', follows: 0.06, drift: 1.6, y: 0.06, n: 6, seed: 19, flat: 1.4 }, { kind: 'cirrus', follows: 0.045, drift: 1, y: 0.01, a: 0.4, seed: 49 }],
    land: [
      { kind: 'range', name: 'Призрачный хребет', follows: 0.065, peak: 124, shape: 'auto', haze: 0.84 },
      { kind: 'range', name: 'Хребет со шпилями', follows: 0.095, peak: 100, shape: 'mesa', cell: 128, haze: 0.72, spires: 2, spireScale: 2, hazeBand: 0.3 },
      { kind: 'hills', name: 'Лес в дымке', follows: 0.14, peak: 80, haze: 0.56, props: { n: 40, size: [14, 28] }, hazeBand: 0.32 },
      { kind: 'hills', name: 'Второй лес', follows: 0.21, peak: 60, haze: 0.38, props: { n: 24, size: [22, 40] }, hazeBand: 0.3 },
      { kind: 'hills', name: 'Тёмный ближний план', follows: 0.31, peak: 58, cell: 256, haze: 0.12, props: { n: 10, size: [40, 66] }, flora: 6, floraSize: [16, 30], outline: true, hazeBand: 0.22 },
    ],
  },
};
const DUSK = ['#141a40', '#3b3670', '#8e4f78', '#de7f62', '#f3b77c'];

function skyFor(dir, P, tod) {
  const base = SKIES[P.sky], d = dir.sky;
  let ramp = base.ramp.map(C), haze = C(base.haze);
  if (dir.mood === 'deep') ramp = ramp.map((c, i) => mix(c, '#0e1e48', [0.5, 0.36, 0.2, 0.08, 0][i]));
  if (dir.mood === 'warm') { ramp = ramp.map((c, i) => mix(c, '#f4c27a', [0, 0.05, 0.22, 0.32, 0.36][i])); haze = mix(haze, '#f0d3a2', 0.5); }
  const g = d.giant;
  const s = {
    ramp, haze, stars: d.stars, aurora: d.aurora || !!P.weather.aurora, auroraA: d.aurora ? 1 : 0.55, rays: d.rays, ship: d.ship,
    giant: { ...g, fx: g.fx + P.gdx, fy: g.fy + P.gdy, r: Math.round(g.r * P.gscale), tilt: g.tilt + P.gtilt, ring: P.ring },
    moons: d.moons.slice(0, Math.max(0, d.moons.length - 1 + P.moons)), sun: { ...d.sun, fx: d.sun.fx + P.sunDx },
  };
  if (tod === 'dusk') {
    s.ramp = DUSK.map(C); s.haze = C('#d98a6e'); s.stars = Math.max(0.85, s.stars); s.dusk = true;
    s.sun = { ...s.sun, horizon: true, size: (s.sun.size || 1) * 1.3 }; s.giant = { ...s.giant, alpha: Math.min(0.92, s.giant.alpha + 0.15) };
  }
  return s;
}

// ---------- the world strip: 2048 columns, as the first planet ----------
const CELL = 16, WORLD_W = 2048, SURF = 160, SPAWN = 1024;
const TOPS = new Int16Array(WORLD_W);
(function grow() {
  const raw = new Float32Array(WORLD_W);
  for (let x = 0; x < WORLD_W; x++) {
    let r = (fbm1(x, 256, 11, 3, WORLD_W) - 0.5) * 30 + (fbm1(x, 32, 12, 2, WORLD_W) - 0.5) * 6;
    r *= smooth(8, 40, Math.abs(x - SPAWN)) * 0.85 + 0.15;
    raw[x] = r;
  }
  for (let x = 0; x < WORLD_W; x++) TOPS[x] = SURF + Math.round(raw[x]);
  for (let x = 1; x < WORLD_W - 1; x++) if (TOPS[x - 1] === TOPS[x + 1] && TOPS[x] !== TOPS[x - 1]) TOPS[x] = TOPS[x - 1];
})();
const SPIRE = ['    l    ', '    #    ', '   ###   ', '   ###   ', '   #.#   ', '  ##.##  ', '  #...#  ', '  s...s  ', '  s...s  ', '  s...s  ', '###...###', '#.......#', '#.......#', '####....#', '#.......#'];
const SPIRES = [300, 1170, 1740].map((x) => { let hi = 1e9; for (let q = 0; q < 9; q++) hi = Math.min(hi, TOPS[x + q]); return { x, top: hi - 10 }; });
function zoneIndex(P, cx) { return cx < P.borders[0] ? 0 : cx < P.borders[1] ? 1 : 2; }
function biomeAt(P, cx) { return BIOMES[P.biomes[zoneIndex(P, cx)]]; }
