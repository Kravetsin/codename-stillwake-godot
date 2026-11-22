// The player's colour in the light drone «Нити»: five ways it lives in the embers, three palettes to choose from and where it
// comes from. The drone, the corners, the icons, the gauge and the thoughts are the shipped ones (DroneInk, DroneSwarm, ScanDots,
// CornerFrame, ThreadIcon), here only recoloured; an alarm is the shipped red in every way. An art-pick page on tools/pick-kit (README.md).
// For a frame: index.html#at=<s into the loop>&colour=<index|-1>&bg=space|room|planet&palette=salts|crew|ash&source=own|bracelet&pause
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-colour.js <out-dir> [--break red]
const HD = 'game/art/hd/';

module.exports = {
  title: 'Цвет дрона',
  heading: 'Цвет игрока в дроне «Нити»',
  window: false,
  tile: [1000, 620],
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Выбираешь три вещи: <b>как цвет живёт в дроне</b> (пять плиток), <b>палитру</b>, из которой выбирают игроки, и <b>откуда цвет</b>: свой у спутника или тот же, что на браслете игрока. Палитра и «откуда цвет» сохраняются вместе с плиткой. «Цвет» и «Фон» нужны только для просмотра.</p>
<p class="lead">Цвет меняет угли дрона (искры, нити, фигуры, глаз, шкалу скана), уголки его окон и значки его кнопок. <b>Красный остаётся тревоге</b>: в тревоге дрон краснеет при любом цвете. <b>Нутро окон остаётся как есть</b>, свет фонаря тоже.</p>
<p class="lead">Все плитки идут по одному кругу в 24 секунды: покой, фонарь, скан, окно с кнопками и колесом, мысль, тревога, обратно. Справа лупа, внизу вся палитра сразу, в этом же способе: щёлкни по дрону, чтобы взять его цвет. Посмотри на всех трёх фонах, а размер игры даёт «Крупно 1:1».</p>
<p class="miss">Не показано: звук; где игрок выбирает цвет в игре; встреча с дроном и видения; свет Godot (цвета и свечение выйдут чуть иначе).</p>
<p class="miss">Учти: «как у игрока» даёт шесть цветов браслета, нынешнего оранжевого среди них нет. В прицеле и на светлом небе любой цвет белеет, как и оранжевый сейчас: сравни, выбрав «уголь».</p>`,
  toggles: [
    { id: 'bgMode', bar: 1, label: 'Фон:', key: 'bg', opts: [['космос', 'space'], ['отсек', 'room'], ['планета днём', 'planet']] },
    { id: 'paletteMode', label: 'Палитра:', key: 'palette', opts: [['Огни металлов', 'salts'], ['Цвета экипажа', 'crew'], ['Пепельные', 'ash']], saved: true, short: 'палитра' },
    { id: 'sourceMode', label: 'Откуда цвет:', key: 'source', opts: [['свой у спутника', 'own'], ['как у игрока', 'bracelet']], saved: true, short: 'откуда цвет' },
    // the buttons take the names of the palette in effect (syncButtons)
    { id: 'colourMode', label: 'Цвет:', key: 'colour', opts: [['по кругу', -1], ...Array.from({ length: 8 }, (_, i) => [String(i + 1), i])] },
  ],
  pick: { doc: 'picks/droneColour', subject: 'Цвет дрона', what: ['раскраску', 'раскраской'] },
  pictures: {
    core: HD + 'drone_core.png', strap: HD + 'hero_tracker.png', wall: HD + 'back_alloy.png',
    tree: HD + 'planet/acacia_0.png', bush: HD + 'planet/bush_0.png', clouds: HD + 'planet/clouds.png',
    scrap: HD + 'items/scrap.png', ore: HD + 'items/ironOre.png', copper: HD + 'items/copper.png', battery: HD + 'items/battery.png', fruit: HD + 'items/fruit.png',
  },

  page() {
    const TAU = Math.PI * 2;

    // ---------- the palettes, ember first; the crew's are Suits.All exactly ----------
    const hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
    const colours = list => list.map(([name, h]) => ({ name, hex: h, rgb: hex(h) }));
    const SUIT = colours([['янтарь', '#e8b04a'], ['бирюза', '#5fc8c0'], ['лиловый', '#a08cf0'], ['зелёный', '#8cd06a'], ['синий', '#5a8ef0'], ['розовый', '#e88cc8']]);
    const COAL = { name: 'уголь', hex: '#ff8a3a', rgb: EMBER };
    const PALS = {
      salts: { name: 'Огни металлов', list: [COAL, ...colours([['натрий', '#e8c84a'], ['бор', '#a4d47a'], ['медь', '#38b8a0'], ['мышьяк', '#94c8e8'], ['цезий', '#6c7ce8'], ['калий', '#c888d8'], ['магний', '#e6e0d0']])] },
      crew: { name: 'Цвета экипажа', list: [COAL, ...SUIT] },
      ash: { name: 'Пепельные', list: [COAL, ...colours([['охра', '#bca65c'], ['лишайник', '#8fa678'], ['патина', '#58988c'], ['сланец', '#8eacc4'], ['сумерки', '#7878b8'], ['вереск', '#b088a8'], ['зола', '#c8c2b4']])] },
    };
    const palNow = () => OPT.source === 'bracelet' ? { name: 'Как у игрока', list: SUIT } : PALS[OPT.palette];
    // a bracelet of its own colour: pink, and blue only while the drone itself is that pink
    const braceletFor = C => OPT.source === 'bracelet' ? C : C.hex === SUIT[5].hex ? SUIT[4] : SUIT[5];

    // ---------- DroneInk's numbers ----------
    const RED = [255, 58, 40], SOOT = [38, 39, 43];
    const PX = .3968, K1 = PX * Z, N = 120, SIDE = .95;   // screen px a pick px about the drone; over the world one is one
    const ORB = { r: 6.3, more: 1.4, wide: 1.05, flat: .3, tilt: -.3, tiltMore: 1.05, pace: .8, paceMore: .25, hurry: 2.2, line: .4, a: .12 };
    const CRACKS = [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]];
    const NDOT = 26, DOT = { step: 7, side: 3.2, above: 20, in: .4, out: .4, lag: .4, span: .6, lagOut: .35, spanOut: .65 };

    // ---------- the colour as the added light takes it ----------
    const lum = c => (.2126 * c[0] + .7152 * c[1] + .0722 * c[2]) / 255, L0 = lum(EMBER);
    const mix3 = (a, b, k) => k <= 0 ? a : k >= 1 ? b : [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    // added, a light colour clips to white and a dark one sinks: the first is paled less, the second lit a little stronger
    const roomOf = c => lum(c) <= L0 ? 1 : clamp(1 - (lum(c) - L0) * 2.3, .35, 1);
    const gainOf = c => clamp(Math.sqrt(L0 / lum(c)), .85, 1.3);
    // a colour's deep shade: its hue, stronger and darker
    const deepOf = c => { const m = Math.max(...c); return c.map(v => Math.max(0, m - (m - v) * 1.35) * .58); };
    const HEAT = [1, .7, .45, .2, 0];   // «Накал»: how hot each square of a tail is, from the head
    const ramp = (K, h) => h < .45 ? mix3(K.deep, K.C, h / .45) : mix3(K.C, K.white, (h - .45) / .55);
    // «Соли»: ember, through the flame's pale middle, into the colour: straight to a lilac or a pink it would pass the alarm's red
    const CREAM = pale(EMBER, .65);
    const flame = (K, q) => q < .25 ? EMBER : q < .45 ? mix3(EMBER, CREAM, (q - .25) / .2) : mix3(CREAM, K.C, sm((q - .45) / .3));

    // ---------- the candidates: col(K, role, p, q, o, m, h) is a mark's colour ----------
    // role: core (halo, cracks) | eye | thread | spark | line | key (a lit key's line) | run | glass; p the shipped paleness; q how far out
    // of the coal (a run's: toward its head); o the orbit (a run's: how far along); m the tail's square; h the heat.
    // The rest are multiples of the shipped numbers, and all of it gives way to the shipped red as an alarm comes.
    const own = (K, p) => p ? pale(K.C, p * K.room) : K.C, coal = p => p ? pale(EMBER, p) : EMBER;
    const cands = [
      {
        id: 'whole', name: 'Угли целиком',
        idea: 'Самое простое: всё, что сейчас тлеет углём, берёт цвет, и глаз с трещинами тоже. Хвосты искр светлеют к белому, как сейчас. Опорный вариант для сравнения.',
        time: 'Цветом: всё. Углём: ничего.',
        col: (K, role, p) => own(K, p),
      },
      {
        id: 'heart', name: 'Живое ядро',
        idea: 'Сердце дрона остаётся живым углём: тёплое зарево, трещины и глаз. Вокруг него цвет игрока: нити, искры, фигуры, уголки окон, значки.',
        time: 'Цветом: всё вокруг ядра. Углём: ядро и глаз, они горят сильнее.',
        coreA: 1.2, coreR: .62, eyeR: 1.15, crackW: 1.6, orbA: () => 3.4, orbW: () => 1.6,   // a small heart: added to the colour, a wide one whitens it
        col: (K, role, p) => role === 'core' || role === 'eye' ? coal(p) : own(K, p),
      },
      {
        id: 'heat', name: 'Накал',
        idea: 'Цвет как температура металла. Голова искры почти добела, дальше чистый цвет, конец хвоста густой и тёмный. Нити, уголки и значки лежат тёмным густым оттенком, а жар бежит по ним.',
        time: 'Цветом: всё, в трёх накалах. Хвосты длиннее: пять шагов вместо трёх.',
        coreR: .8, eyeR: 1.3, orbA: () => 1.4, lineA: .55, under: true, tail: 5,
        col(K, role, p, q, o, m, h) {
          if (role === 'spark') return ramp(K, HEAT[m] * (h < .3 ? .4 : Math.min(1, .62 + .45 * h)));
          if (role === 'run') return ramp(K, q);
          if (role === 'eye') return p ? K.white : K.C;
          return role === 'glass' ? K.deep : K.C;
        },
      },
      {
        id: 'salts', name: 'Соли в пламени',
        idea: 'Как соли металлов в костре: искра рождается у ядра оранжевым углём и по пути вдоль нити разгорается в цвет. Вокруг дрона цветной язык пламени. В уголках и значках линия цветная, а бегущая искра рождается углём.',
        time: 'Углём: ядро, глаз, рождение каждой искры. Цветом: всё, что успело разгореться.',
        coreA: 1.3, aura: .22, orbA: () => 3, orbW: () => 1.4, grad: true,
        col(K, role, p, q, o) {
          if (role === 'core' || role === 'eye') return coal(p);
          if (role === 'run') return pale(flame(K, o * 1.1 + (1 - q) * .55), p * .6);
          if (role === 'spark' || role === 'thread') return pale(flame(K, q), p * K.room);
          return own(K, p);
        },
      },
      {
        id: 'vein', name: 'Одна жила',
        idea: 'Самый тихий вариант: дрон угольный, а цвет в одной жиле. Это средняя орбита и её искры, глаз и главные метки: горящие точки шкалы, бегущая искра, включённая кнопка.',
        time: 'Цветом: треть искр, одна орбита, глаз, метки. Углём: остальное.',
        eyeR: 1.5, eyeA: 1.5, orbA: o => o === 1 ? 5 : 1, orbW: o => o === 1 ? 1.8 : 1,
        col(K, role, p, q, o) {
          const marked = role === 'eye' || role === 'run' || role === 'key' || ((role === 'spark' || role === 'thread') && o === 1);
          return marked ? own(K, p) : coal(p);
        },
      },
    ];
    // how the colour is drawn now: gate is how far the shipped look has taken over for an alarm, tint its red
    function inkOf(cand, C, gate = 0, tint = 0) {
      const hot = mix3(EMBER, RED, Math.min(1, tint * 1.4));
      const K = { cand, C, gate, deep: deepOf(C), white: pale(C, .88), room: roomOf(C), under: cand.under ? 1 - gate : 0, gain: lerp(gainOf(C), 1, gate) };
      K.col = (role, p = 0, q = 1, o = 0, m = 0, h = 1) => {
        const to = () => p ? pale(hot, p) : hot;
        return gate >= 1 ? to() : gate > 0 ? mix3(cand.col(K, role, p, q, o, m, h), to(), gate) : cand.col(K, role, p, q, o, m, h);
      };
      K.times = (v, ...a) => v === undefined ? 1 : lerp(typeof v === 'function' ? v(...a) : v, 1, gate);
      return K;
    }

    // ---------- paths, and the shipped shapes and icons ----------
    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const frac = v => v - Math.floor(v);
    const arcP = (cx, cy, r, n, a0 = 0, a1 = TAU) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]); } return p; };
    function mkPath(polys, bridges) {
      const seg = []; let L = 0;
      polys.forEach((p, pi) => {
        for (let i = 1; i < p.length; i++) { const a = p[i - 1], b = p[i], d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 1e-6) continue; seg.push([a[0], a[1], b[0], b[1], d, L, bridges ? bridges[pi][i] : 0]); L += d; }
      });
      return { seg, L, polys };
    }
    function onPath(P, s) {
      const d = frac(s) * P.L; let lo = 0, hi = P.seg.length - 1;
      while (lo < hi) { const m = (lo + hi + 1) >> 1; if (P.seg[m][5] <= d) lo = m; else hi = m - 1; }
      const q = P.seg[lo], e = c01((d - q[5]) / q[4]);
      return [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e, (q[2] - q[0]) / q[4], (q[3] - q[1]) / q[4]];
    }
    const para = (back, half, cv, n) => Array.from({ length: n + 1 }, (_, i) => { const v = -half + 2 * half * i / n; return [-back + v * v / cv, v]; });
    const LAMP_P = mkPath([para(9, 12, 14, 14), para(7.2, 10, 15, 12)]);
    const MARK = Array.from({ length: NDOT }, (_, j) => { const a = (j + .5) / NDOT * TAU; return [Math.cos(a) * 10.2, Math.sin(a) * 10.2, -Math.sin(a), Math.cos(a)]; });
    const MARK_LINES = MARK.map(([x, y, dx, dy]) => [[x - dx * .22, y - dy * .22], [x + dx * .22, y + dy * .22]]);
    const ringAt = L => mkPath([arcP(0, 0, lerp(7.6, 7.1, L), 26)]);
    const ticksAt = L => mkPath([0, 1, 2, 3].map(q => [[lerp(5.4, 4.3, L), 0], [lerp(12.5, 13.5, L), 0]].map(p => rot(p[0], p[1], q * Math.PI / 2))));
    const RING_L = ringAt(0).L, TICK_L = ticksAt(0).L;
    const HEART = [Array.from({ length: 37 }, (_, i) => { const a = i / 36 * TAU; return [16 * Math.pow(Math.sin(a), 3) / 17, -(13 * Math.cos(a) - 5 * Math.cos(2 * a) - 2 * Math.cos(3 * a) - Math.cos(4 * a)) / 17 - .15]; })];
    const FLAKE = [];
    for (let i = 0; i < 3; i++) { const a = Math.PI / 2 + i * Math.PI / 3; FLAKE.push([[-Math.cos(a), -Math.sin(a)], [Math.cos(a), Math.sin(a)]]); }
    for (let i = 0; i < 6; i++) { const a = Math.PI / 2 + i * Math.PI / 3, P = [Math.cos(a) * .56, Math.sin(a) * .56], b = q => [P[0] + Math.cos(q) * .32, P[1] + Math.sin(q) * .32]; FLAKE.push([b(a - .9), P, b(a + .9)]); }
    const stood = polys => mkPath(polys.map(p => p.map(([x, y]) => [x * 22, y * 22 - 50])));
    const THOUGHT = { love: stood(HEART), cold: stood(FLAKE) };

    // the icons as thread-icons.js draws them (IconInk)
    function joinOne(polys) {
      const left = polys.slice(1).map(p => p.slice()), cur = polys[0].slice(), fl = cur.map(() => 0);
      while (left.length) {
        const e = cur[cur.length - 1]; let best = 0, rev = false, bd = 1e9;
        left.forEach((p, i) => { const d0 = Math.hypot(e[0] - p[0][0], e[1] - p[0][1]), d1 = Math.hypot(e[0] - p[p.length - 1][0], e[1] - p[p.length - 1][1]); if (d0 < bd) { bd = d0; best = i; rev = false; } if (d1 < bd) { bd = d1; best = i; rev = true; } });
        let p = left.splice(best, 1)[0]; if (rev) p = p.slice().reverse();
        p.forEach((q, j) => { cur.push(q); fl.push(j === 0 ? 1 : 0); });
      }
      return mkPath([cur], [fl]);
    }
    const mkIcon = polys => ({ parts: mkPath(polys), one: joinOne(polys) });
    function svg(d, n = 8) {
      const tk = d.match(/[MLCQZ]|-?\d*\.?\d+/g), out = []; let cur = null, p = [0, 0], st = [0, 0], i = 0, cmd = '';
      const num = () => +tk[i++];
      while (i < tk.length) {
        if (/[MLCQZ]/.test(tk[i])) cmd = tk[i++];
        if (cmd === 'M') { p = [num(), num()]; st = p; cur = [p]; out.push(cur); cmd = 'L'; }
        else if (cmd === 'L') { p = [num(), num()]; cur.push(p); }
        else if (cmd === 'Q') { const a = [num(), num()], b = [num(), num()]; for (let k = 1; k <= n; k++) cur.push(bez(p, a, b, k / n)); p = b; }
        else if (cmd === 'C') {
          const a = [num(), num()], b = [num(), num()], e = [num(), num()];
          for (let k = 1; k <= n; k++) { const s = k / n, m = 1 - s; cur.push([0, 1].map(j => m * m * m * p[j] + 3 * m * m * s * a[j] + 3 * m * s * s * b[j] + s * s * s * e[j])); }
          p = e;
        } else if (cmd === 'Z') { cur.push(st.slice()); p = st; }
      }
      return out.map(q => q.map(([x, y]) => [(x - 8) / 8, (y - 8) / 8]));
    }
    const motif = ds => mkIcon(ds.flatMap(d => svg(d)));
    const rrD = (x, y, w, h, r) => `M ${x + r} ${y} L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
    const squareRim = (() => {
      const p = [], r = .24;
      for (const [x, y, a] of [[1 - r, -1 + r, -Math.PI / 2], [1 - r, 1 - r, 0], [-1 + r, 1 - r, Math.PI / 2], [-1 + r, -1 + r, Math.PI]]) p.push(...arcP(x, y, r, 4, a, a + Math.PI / 2));
      p.push(p[0].slice());
      return mkIcon([p]);
    })();
    const ICON = {
      circle: mkIcon([arcP(0, 0, 1, 48)]), square: squareRim,
      lamp: mkIcon([Array.from({ length: 17 }, (_, k) => { const a = k / 16 * TAU - Math.PI / 2, r = k % 2 ? .52 : .95; return [Math.cos(a) * r, Math.sin(a) * r]; }), arcP(0, 0, .24, 16)]),
      scan: mkIcon([arcP(0, 0, .88, 36), [[0, 0], rot(.88, 0, -Math.PI / 4)], arcP(0, 0, .58, 10, -Math.PI / 4 - 1.1, -Math.PI / 4), arcP(0, 0, .16, 12)]),
      air: motif(['M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 'M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 'M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2']),
      build: motif([rrD(.4, 1.8, 7.1, 3.5, .55), 'M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z',
        rrD(.4, 6.2, 3.1, 3.5, .5), rrD(4.4, 6.25, 7.2, 3.5, .55), rrD(12.5, 6.2, 3.1, 3.5, .5), rrD(.4, 10.6, 7.1, 3.5, .55),
        'M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z']),
      research: motif(['M 5.3 10.8 C 2.4 9.3 1.8 5.4 3.8 3.0 C 5.8 0.6 10.2 0.6 12.2 3.0 C 14.2 5.4 13.6 9.3 10.7 10.8 L 10.4 12.2 L 5.6 12.2 Z', rrD(5.5, 12.7, 5, 1.6, .3),
        'M 6.7 14.8 L 9.3 14.8 L 8.7 15.8 L 7.3 15.8 Z', 'M 6.5 9.8 L 7 6.6 L 8 8.2 L 9 6.6 L 9.5 9.8']),
    };

    // ---------- the loop, every moment of it worked out from the clock alone: a frame can be opened at any second ----------
    const STAGES = [
      { id: 'idle', len: 2.5, say: 'покой: искры на трёх орбитах' },
      { id: 'lamp', len: 2.5, say: 'фонарь: зеркало из нитей' },
      { id: 'scan', len: 4.5, say: 'скан: прицел и шкала прочности' },
      { id: 'window', len: 4.5, say: 'окно: уголки, кнопки края, колесо' },
      { id: 'love', len: 4, say: 'мысль: верность' },
      { id: 'alarm', len: 4, say: 'тревога: холод, всегда красная' },
      { id: 'back', len: 2, say: 'обратно к покою' },
    ];
    const AT = {};
    let LOOP = 0;
    for (const s of STAGES) { s.t0 = LOOP; AT[s.id] = LOOP; LOOP += s.len; }
    const READ = { on: AT.scan + .7, off: AT.scan + 3.7 }, OPEN = { on: AT.window + .15, off: AT.window + 4 }, HELD = 3.5;   // a thought: 0.5 in, 3 held, 0.5 out
    const SHAPES = [[0, 'idle'], [AT.lamp, 'lamp'], [AT.scan, 'scan'], [AT.window, 'idle'], [AT.love, 'love'], [AT.love + HELD, 'idle'], [AT.alarm, 'cold'], [AT.alarm + HELD, 'idle']];
    function shapeAt(u) {
      let i = 0;
      while (i + 1 < SHAPES.length && u >= SHAPES[i + 1][0]) i++;
      const [t0, cur] = SHAPES[i], prev = i ? SHAPES[i - 1][1] : 'idle', dur = THOUGHT[cur] || THOUGHT[prev] ? .5 : .6;
      return { cur, prev, t0, e: i ? (u - t0) / dur : 9 };
    }
    const openOf = u => u < READ.on ? 0 : u < READ.off ? sm((u - READ.on) / .3) : 1 - sm((u - READ.off - .4) / .4);
    function awayOf(u, j) {
      if (u < READ.on) return 0;
      if (u < READ.off) return sm(((u - READ.on) / DOT.in - hsh(j + 5) * DOT.lag) / DOT.span);
      return 1 - sm(((u - READ.off) / DOT.out - hsh(j + 2) * DOT.lagOut) / DOT.spanOut);
    }

    // ---------- where things are, screen px ----------
    const HX = 560, FACE = -1, SCENE_H = 522;
    const PERCH = [HX - FACE * 32, FLOOR_Y - 187], WAIST = [HX + FACE * 16 * Z, FLOOR_Y - 12 * Z];   // SheetHoloLook: 16 ahead, 12 over the feet
    const HELMET = [HX + FACE * Z, FLOOR_Y - (RIG.Neck + 6.2) * Z], PRESSED = [HX - FACE * 4.5 * Z, HELMET[1] - 6.25 * Z];
    const WRECK = { x: 250, top: 390 }, WIN_R = { x: 64, y: 70, w: 308, h: 164 }, WHEEL = [444, 148], KEY_AT = [70, 479];
    const LP = { x: 752, y: 12, w: 236, h: 368, cx: 870, cy: 310, k: 2.5 };
    const aimTo = (a, b) => Math.atan2(b[1] - a[1], b[0] - a[0]);
    const UP = -Math.PI / 2, LOOK = -2.5, FLOOR_AIM = [380, FLOOR_Y], READ_AIM = [WRECK.x, WRECK.top - 20];
    const AIMS = [[0, LOOK], [AT.lamp, aimTo(PERCH, FLOOR_AIM)], [AT.scan, aimTo(PERCH, READ_AIM)], [AT.window, UP], [AT.love, aimTo(PRESSED, HELMET)], [AT.love + HELD, LOOK], [AT.alarm, UP], [AT.alarm + HELD, LOOK]];
    const neck = [HX + FACE * .3 * Z, FLOOR_Y - RIG.Neck * Z], headTo = p => clamp(headTurn((p[0] - neck[0]) * FACE, p[1] - neck[1]), -HEAD.Up, HEAD.Down);
    const HEADS = [[0, 0], [AT.lamp, headTo(FLOOR_AIM)], [AT.scan, headTo(READ_AIM)], [AT.window, headTo([WIN_R.x + WIN_R.w / 2, WIN_R.y + WIN_R.h / 2])], [AT.love, 0]];
    function eased(list, u, tau, by) {
      let i = 0;
      while (i + 1 < list.length && u >= list[i + 1][0]) i++;
      return i ? by(list[i - 1][1], list[i][1], 1 - Math.exp(-(u - list[i][0]) / tau)) : list[0][1];
    }

    function stateOf(time) {
      const round = Math.floor(time / LOOP), u = time - round * LOOP;
      let si = 0;
      while (si + 1 < STAGES.length && u >= STAGES[si + 1].t0) si++;
      const pal = palNow(), n = pal.list.length, index = OPT.colour < 0 ? round % n : OPT.colour % n;
      const lv = u - AT.love, loved = sm(lv / .5) * (1 - sm((lv - HELD) / .5));
      const wu = u - OPEN.on, wc = u - OPEN.off, flown = sm(wu / .4) * (1 - sm(wc / .4));
      let D = lerp2(lerp2(PERCH, PRESSED, loved), WAIST, flown);
      D = [D[0], D[1] + Z * Math.sin(TAU * time / 1.8) + 1.5 * Math.sin(lv * 3) * loved];
      const al = u - AT.alarm, formed = sm(al / .5) * (1 - sm((al - HELD) / .5));
      return {
        time, u, stage: STAGES[si], si, pal, index, colour: pal.list[index], D,
        A: eased(AIMS, u, .09, angLerp), head: eased(HEADS, u, .12, lerp),
        sh: shapeAt(u), open: openOf(u), away: Array.from({ length: NDOT }, (_, j) => awayOf(u, j)),
        tilt: -.2 * loved, glow: .2 * loved, op: time - .3 * (round * 4 + clamp(lv, 0, 4)),
        flurry: wu < 0 ? 0 : wc < 0 ? c01(wu / .15) : 1 - c01(wc / .12),
        gate: sm(formed * 2.5), tint: formed > 0 ? formed * (.6 + .4 * Math.sin(al * TAU * 2.4)) : 0,
      };
    }

    // ---------- the swarm, DroneSwarm's ----------
    const RANK = share => { let m = 0; const r = Array.from({ length: N }, (_, i) => hsh(i * 3 + 1) < share ? m++ : -1); return { r, m }; };
    const LAMP_R = RANK(.8), THOUGHT_R = RANK(.7);
    const SCAN_R = (() => {
      const raw = [NDOT, .4 * N, N - NDOT - .4 * N].map(Math.round), part = [];
      const order = Array.from({ length: N }, (_, i) => i).sort((a, b) => hsh(a * 3 + 1) - hsh(b * 3 + 1));
      let at = 0;
      raw.forEach((n, p) => { for (let r = 0; r < n && at < N; r++) part[order[at++]] = [p, r]; });
      return { part, count: raw };
    })();
    const takes = (id, i) => id === 'idle' ? false : id === 'scan' ? true : (id === 'lamp' ? LAMP_R : THOUGHT_R).r[i] >= 0;
    function orbit(i, op, tilt) {
      const P = DR.P[i], o = i % 3, way = o === 1 ? -1 : 1, th = P.ph + op * (ORB.pace + ORB.paceMore * o) * way * ORB.hurry;
      const rx = (ORB.r + o * ORB.more + (P.r1 - .5)) * ORB.wide, tl = ORB.tilt + o * ORB.tiltMore + tilt;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * ORB.flat, tl), v = rot(-Math.sin(th) * way, Math.cos(th) * ORB.flat * way, tl), d = Math.hypot(v[0], v[1]) || 1;
      return [q[0], q[1], v[0] / d, v[1] / d, Math.sin(th)];
    }
    function place(id, i, X) {
      const t = X.t;
      let q;
      if (id === 'scan') {
        const [part, r] = SCAN_R.part[i];
        q = part === 0 ? MARK[r] : part === 1 ? onPath(X.ring, (r + .5) / SCAN_R.count[1] + t * 12 / RING_L) : onPath(X.ticks, (r + .5) / SCAN_R.count[2] + t * 8 / TICK_L);
      } else if (id === 'lamp') q = onPath(LAMP_P, (LAMP_R.r[i] + .5) / LAMP_R.m + t * 30 / LAMP_P.L);
      else return onPath(THOUGHT[id], (THOUGHT_R.r[i] + .5) / THOUGHT_R.m + t * (id === 'cold' ? 42 : 18) / THOUGHT[id].L);
      const a = rot(q[0], q[1], X.A), v = rot(q[2], q[3], X.A);
      return [a[0], a[1], v[0], v[1]];
    }
    const idleHeat = (i, t) => hsh(i + 3) < .5 ? .55 + .3 * Math.sin(t * 11 + i * 7) : .2;
    function heatOn(id, i, t) {
      if (id === 'cold') return .75 + .25 * Math.sin(t * 13 + i);
      if (id === 'love') return .42 + .18 * Math.sin(t * 4 + i * 1.7);
      return .7 + .25 * Math.sin(t * 13 + i) + (id === 'lamp' ? .12 : 0);
    }
    const flurryHash = (i, n) => frac(Math.sin(i * 127.1 + n * 311.7 + 74.7) * 43758.5453);
    function flurried(i, t, shown) {
      const spot = n => { const a = TAU * flurryHash(i, n + .3), r = .35 + .65 * Math.sqrt(flurryHash(i, n + .6)); return [Math.cos(a) * r * 10, Math.sin(a) * r * 7.5]; };
      const ph = t / (.18 + .32 * flurryHash(i, .5)) + flurryHash(i, .25), n = Math.floor(ph), s = ph - n, a = spot(n), b = spot(n + 1);
      const p = lerp2(a, b, sm(s / .45)), d = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1, rise = sm(shown * 1.4 - .4 * i / 13);
      return { p: [p[0] * rise, p[1] * rise, (b[0] - a[0]) / d, (b[1] - a[1]) / d], heat: (.35 + .65 * flurryHash(i, n + .75)) * rise * (s < .45 ? 1 : .8), o: i % 3 };
    }
    // every spark about the core, pick px: where, how hot, whose orbit it is, whether on a shape, where a flight left from
    function swarmOf(st, t, seed = 0) {
      const { cur, prev, e, t0 } = st.sh, X = { t: t + seed, A: st.A };
      if (cur === 'scan' || prev === 'scan') { X.ring = ringAt(st.open); X.ticks = ticksAt(st.open); }
      const left = { ...X, t: X.t - (st.u - t0) }, list = [];
      for (let i = 0; i < N; i++) {
        const to = takes(cur, i), was = takes(prev, i);
        if (cur === 'scan' || prev === 'scan') {
          const [part, j] = SCAN_R.part[i];
          if (part === 0 && st.away[j] > 0) continue;   // out as the gauge's dot
        }
        const O = orbit(i, st.op + seed, st.tilt);
        const k = !to && !was ? 1 : to ? sm((e - hsh(i + 50) * .45) / .55) : sm((e - hsh(i + 50) * .4) / .6);
        let p = O, heat = idleHeat(i, X.t), from = null, shaped = false, o = i % 3;
        if (to || k < 1) {
          const T0 = k < 1 ? was ? place(prev, i, THOUGHT[prev] ? left : X) : O : null, T1 = to ? place(cur, i, X) : O;
          if (k >= 1) p = T1;
          else if (k <= 0) p = T0;
          else {
            const q = lerp2(T0, T1, sm(k)), d = Math.hypot(T1[0] - T0[0], T1[1] - T0[1]) || 1;
            p = [q[0], q[1], (T1[0] - T0[0]) / d, (T1[1] - T0[1]) / d];
            if (i % 4 === 0) from = T0;
          }
          const f = lerp(was ? 1 : 0, to ? 1 : 0, k);
          shaped = f > 0;
          if (shaped) heat = lerp(idleHeat(i, X.t), heatOn(to ? cur : prev, i, X.t), f) + .3 * Math.sin(k * Math.PI);
          // «Одна жила»: the reticle's marks are its key marks
          if (f > .5 && (to ? cur : prev) === 'scan' && SCAN_R.part[i][0] === 0) o = 1;
        }
        list.push({ p, heat, o, shaped, from, age: shaped ? frac(X.t * .7 + hsh(i + 9)) : 1 });
      }
      if (st.flurry > 0) for (let i = 0; i < 14; i++) list.push(flurried(i, X.t, st.flurry));
      return { list, X };
    }

    // ---------- drawing the drone: the darks and the core, then everything added ----------
    function strokeLines(c, at, polys, w, col, a) {
      if (a <= .003) return;
      c.beginPath();
      for (const p of polys) p.forEach((q, j) => { const r = at(q); if (j) c.lineTo(r[0], r[1]); else c.moveTo(r[0], r[1]); });
      c.lineWidth = w; c.strokeStyle = css(col, a); c.stroke();
    }
    function line2(c, a, b, w, col, al) { if (al <= .003) return; c.beginPath(); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]); c.lineWidth = w; c.strokeStyle = css(col, al); c.stroke(); }
    const outOf = d => sm((d - 2.8) / 4.6);   // how far out of the coal, from the pick px to it
    // a spark's glow: its square and the ones behind it
    function tail(c, K, x, y, dx, dy, side, step, heat, q, o) {
      const h = Math.min(1, heat);
      if (h <= .02) return;
      const n = K.gate >= 1 ? 3 : K.cand.tail || 3;
      for (let m = 0; m < n; m++) {
        let a;
        if (n === 3) a = .95 - m * .3;
        else a = (.95 - m * .16) * (m < 3 ? 1 : 1 - K.gate);
        a *= h * K.gain;
        if (a <= .003) continue;
        c.fillStyle = css(K.col('spark', .3 * Math.max(0, 1 - m / 3), q, o, m, h), a);
        c.fillRect(x - dx * m * step - side / 2, y - dy * m * step - side / 2, side, side);
      }
    }
    function drawDrone2(c, K, st, D, k, seed = 0) {
      const { list, X } = swarmOf(st, st.time, seed), { cur, prev, e } = st.sh, A = st.A;
      const at = q => [D[0] + q[0] * k, D[1] + q[1] * k], turned = q => at(rot(q[0], q[1], A));
      const shapes = [];
      for (const [id, form] of [[cur, sm(e)], [prev, 1 - sm(e)]]) {
        if (id === 'idle' || form <= .003) continue;
        const a = form * (id === 'love' ? .7 : 1);
        if (id === 'lamp') shapes.push({ polys: LAMP_P.polys, at: turned, a, role: 'line' });
        else if (id === 'scan') {
          shapes.push({ polys: [...X.ring.polys, ...X.ticks.polys], at: turned, a, role: 'line' });
          shapes.push({ polys: MARK_LINES.filter((m, j) => st.away[j] < .5), at: turned, a, role: 'key' });
        } else shapes.push({ polys: THOUGHT[id].polys, at, a, role: 'line' });
      }
      const rings = [0, 1, 2].map(o => ({ o, rx: (ORB.r + o * ORB.more) * ORB.wide, tilt: ORB.tilt + o * ORB.tiltMore + st.tilt }));
      const ellipse = r => { c.beginPath(); c.ellipse(D[0], D[1], r.rx * k, r.rx * ORB.flat * k, r.tilt, 0, TAU); c.stroke(); };

      c.save();
      c.lineCap = 'round'; c.lineJoin = 'round'; c.globalCompositeOperation = 'source-over';
      if (K.under > .003) {   // «Накал»'s deep threads, laid, not added
        c.strokeStyle = css(K.deep, .85 * K.under); c.lineWidth = .65 * k;
        rings.forEach(ellipse);
        for (const s of shapes) strokeLines(c, s.at, s.polys, .95 * k, K.deep, .85 * s.a * K.under);
      }
      const darks = front => {
        for (const s of list) {
          if ((s.shaped || s.p[4] === undefined || s.p[4] >= 0) !== front) continue;
          const p = at(s.p);
          c.fillRect(p[0] - SIDE * k / 2, p[1] - SIDE * k / 2, SIDE * k, SIDE * k);
        }
      };
      c.fillStyle = css(SOOT); darks(false);
      const R = 5.04 * k;
      if (ready(IMG.core)) c.drawImage(IMG.core, D[0] - R, D[1] - R, 2 * R, 2 * R);
      else { c.beginPath(); c.arc(D[0], D[1], .75 * R, 0, TAU); c.fill(); }
      c.fillStyle = css(SOOT); darks(true);

      c.globalCompositeOperation = 'lighter';
      const cand = K.cand, gain = K.gain;
      if (cand.aura && K.gate < 1) halo(c, D[0], D[1], 17 * k, K.col('line'), cand.aura * (1 - K.gate) * gain);
      for (const r of rings) {
        const a = Math.min(1, ORB.a * K.times(cand.orbA, r.o) * gain), w = ORB.line * K.times(cand.orbW, r.o) * k;
        c.lineWidth = w;
        if (!cand.grad || K.gate >= 1) { c.strokeStyle = css(K.col('thread', 0, 1, r.o), a); ellipse(r); continue; }
        // the thread ember by the coal, the colour at its ends: pieces by how far out
        const levels = new Map();
        let last = null;
        for (let i = 0; i <= 28; i++) {
          const th = i / 28 * TAU, p = rot(Math.cos(th) * r.rx, Math.sin(th) * r.rx * ORB.flat, r.tilt);
          if (last) { const lv = Math.round(outOf(Math.hypot((p[0] + last[0]) / 2, (p[1] + last[1]) / 2)) * 6); if (!levels.has(lv)) levels.set(lv, []); levels.get(lv).push([last, p]); }
          last = p;
        }
        for (const [lv, pieces] of levels) strokeLines(c, at, pieces, w, K.col('thread', 0, lv / 6, r.o), a);
      }
      for (const s of shapes) {
        const col = K.col(s.role), la = s.a * K.times(cand.lineA) * gain;
        strokeLines(c, s.at, s.polys, 1.6 * k, col, .06 * la);
        strokeLines(c, s.at, s.polys, .45 * k, col, .3 * la);
      }
      let shaped = 0;
      for (const s of list) {
        const p = at(s.p), d = Math.hypot(s.p[0], s.p[1]);
        const q = s.shaped ? Math.min(outOf(d), sm(s.age / .5)) : outOf(d);
        if (s.from) line2(c, at(s.from), p, .35 * k, K.col('thread', 0, q, s.o), .07);
        if (s.shaped) shaped++;
        tail(c, K, p[0], p[1], s.p[2], s.p[3], SIDE * k, 1.3 * k, s.heat + st.tint * .55, q, s.o);
      }
      const fl = .75 + .25 * Math.sin(st.time * 7.3), core = K.col('core');
      halo(c, D[0], D[1], 8 * k * K.times(cand.coreR), core, Math.min(.8, (.35 + .25 * st.glow) * K.times(cand.coreA) * gain));
      c.beginPath();
      for (const [a, b] of CRACKS) { c.moveTo(D[0] + Math.cos(A + a) * .6 * k, D[1] + Math.sin(A + a) * .6 * k); c.lineTo(D[0] + Math.cos(A + b * .5) * 2.6 * k, D[1] + Math.sin(A + b * .5) * 2.6 * k); }
      c.lineWidth = .45 * k * K.times(cand.crackW); c.strokeStyle = css(core, .5 * fl * gain * K.times(cand.coreA)); c.stroke();
      const E = at([Math.cos(A) * 1.386, Math.sin(A) * 1.386]), er = K.times(cand.eyeR);
      c.fillStyle = css(K.col('eye', .5), .9 * fl); c.beginPath(); c.arc(E[0], E[1], 1.2 * k * er, 0, TAU); c.fill();
      halo(c, E[0], E[1], 4 * k * er, K.col('eye'), .5 * fl * K.times(cand.eyeA) * gain);
      c.restore();
      return shaped;
    }

    // ---------- the scanner's gauge, ScanDots' and ScannerView's ----------
    function gauge(c, K, st) {
      const t = st.time, LIT = 16.5, step = 1.3 * K1, out = [];
      for (let j = 0; j < NDOT; j++) {
        const away = st.away[j];
        if (away <= 0) continue;
        const seat = rot(MARK[j][0], MARK[j][1], st.A), S = [st.D[0] + seat[0] * K1, st.D[1] + seat[1] * K1];
        const H = [WRECK.x + (j - (NDOT - 1) / 2) * DOT.step, WRECK.top - DOT.above], e = sm(away), p = lerp2(S, H, e).map(Math.round);
        const lit = c01(LIT - j), fl = .82 + .18 * Math.sin(t * 9 + j * 2.3), spark = .7 + .25 * Math.sin(t * 13 + j);
        const heat = lerp(spark, lit > 0 ? (.3 + .62 * lit) * fl : .04, away), d = Math.hypot(H[0] - S[0], H[1] - S[1]) || 1;
        const way = away >= 1 ? 0 : st.u < READ.off ? 1 : -1;
        out.push({ j, p, S, lit, heat, way, side: Math.max(1, Math.round(lerp(SIDE * K1, DOT.side, away))), dx: way * (H[0] - S[0]) / d, dy: way * (H[1] - S[1]) / d, away });
      }
      c.save();
      c.fillStyle = css(SOOT);
      for (const d of out) c.fillRect(d.p[0] - Math.floor(d.side / 2), d.p[1] - Math.floor(d.side / 2), d.side, d.side);
      if (K.under > .003) for (const d of out) {   // «Накал»: the dim dots lie in the deep shade
        if (d.lit >= 1) continue;
        c.fillStyle = css(K.deep, .8 * d.away * K.under); c.fillRect(d.p[0] - Math.floor(d.side / 2), d.p[1] - Math.floor(d.side / 2), d.side, d.side);
      }
      c.globalCompositeOperation = 'lighter';
      let lit = 0, dim = 0;
      for (const d of out) {
        if (d.way && d.j % 2 === 0) line2(c, d.S, d.p, .35 * K1, K.col('line'), .07);
        const q = Math.min(d.away, sm(frac(t * .5 + hsh(d.j + 21)) / .2)), half = d.side / 2 - Math.floor(d.side / 2);
        tail(c, K, d.p[0] + half, d.p[1] + half, d.dx, d.dy, d.side, step, d.heat, q, d.lit > 0 || d.way ? 1 : 0);
        if (d.lit > 0) lit++; else dim++;
      }
      c.restore();
      return { lit, dim };
    }

    // ---------- the window's corners (CornerFrame), the edge keys and the wheel (ThreadIcon) ----------
    function corners(c, K, st) {
      const u = st.u - OPEN.on, uc = st.u - OPEN.off, r = WIN_R;
      if (u < 0 || uc >= .12) return 0;
      const E = [st.D[0] + Math.cos(st.A) * 1.386 * K1, st.D[1] + Math.sin(st.A) * 1.386 * K1], shots = [], set = [];
      for (let k = 0; k < 4; k++) {
        const P = [[r.x - 5, r.y - 5], [r.x + r.w + 5, r.y - 5], [r.x + r.w + 5, r.y + r.h + 5], [r.x - 5, r.y + r.h + 5]][k];
        const f = uc < 0 ? c01((u - .01 * k) / .1) : 1 - c01(uc / .12);
        if (f <= 0) continue;
        if (f < 1) { const way = uc < 0 ? 1 : -1, d = Math.hypot(P[0] - E[0], P[1] - E[1]) || 1; shots.push([lerp2(E, P, sm(f)), way * (P[0] - E[0]) / d, way * (P[1] - E[1]) / d]); continue; }
        const sx = k === 0 || k === 3 ? 1 : -1, sy = k < 2 ? 1 : -1;
        set.push({ k, P, ends: [[P[0] + sx * 22, P[1]], [P[0], P[1] + sy * 22]], landed: c01((u - .1 - .01 * k) / .15) });
      }
      const runs = [];
      for (const s of set) for (const E1 of s.ends) for (let m = 0; m < 3; m++) {
        const q = frac(st.time * 1.3 + m / 3 + s.k * .17);
        runs.push({ p: lerp2(s.P, E1, q), dx: (E1[0] - s.P[0]) / 22, dy: (E1[1] - s.P[1]) / 22, q });
      }
      const polys = set.map(s => [s.ends[0], s.P, s.ends[1]]), same = q => q;
      c.save();
      c.lineCap = 'round'; c.lineJoin = 'round';
      if (K.under > .003) strokeLines(c, same, polys, 1.5, K.deep, .9 * K.under);
      c.fillStyle = css(SOOT);
      for (const s of runs) c.fillRect(s.p[0] - SIDE / 2, s.p[1] - SIDE / 2, SIDE, SIDE);
      c.globalCompositeOperation = 'lighter';
      const col = K.col('line'), a = K.times(K.cand.lineA) * K.gain;
      strokeLines(c, same, polys, 2.2, col, .07 * a);
      strokeLines(c, same, polys, .6, col, .45 * a);
      for (const s of runs) tail(c, K, s.p[0], s.p[1], s.dx, s.dy, SIDE, 1.3, .9 * (1 - s.q * .7), s.q, 1);
      for (const s of set) {
        c.fillStyle = css(K.col('spark', .225, 0, 1, 0, .9), .855 * K.gain); c.fillRect(s.P[0] - 1.2, s.P[1] - 1.2, 2.4, 2.4);
        halo(c, s.P[0], s.P[1], 12, col, .6 * (1 - s.landed));
      }
      for (const [p, dx, dy] of shots) tail(c, K, p[0], p[1], dx, dy, 1.6, 1.3, 1, 0, 1);
      c.restore();
      return set.length;
    }
    function icon(c, K, I, x, y, r, u, look) {
      const P = look.rim ? I.parts : I.one, t = look.t, sd = look.sd || 0, fl = look.flare || 0;
      const lv = look.lv * (look.wait ? .7 + .3 * Math.sin(t * TAU * 1.2) : 1), role = look.key ? 'key' : 'line', a = Math.min(1, .3 + .45 * lv + .4 * fl);
      const stroke = (w, col, al, bridge) => {
        if (al <= .004) return;
        c.beginPath();
        let last = null, any = false;
        for (const s of P.seg) {
          if (bridge !== undefined && !!s[6] !== bridge) { last = null; continue; }
          if (!last || last[0] !== s[0] || last[1] !== s[1]) c.moveTo(x + s[0] * r, y + s[1] * r);
          c.lineTo(x + s[2] * r, y + s[3] * r); last = [s[2], s[3]]; any = true;
        }
        if (!any) return;
        c.lineWidth = w; c.strokeStyle = css(col, al); c.stroke();
      };
      c.save();
      c.lineCap = 'round'; c.lineJoin = 'round';
      if (K.under > .003) { stroke(1.5 * u, K.deep, Math.min(1, .5 + .4 * lv) * K.under, false); stroke(.9 * u, K.deep, .35 * K.under, true); }
      c.globalCompositeOperation = 'lighter';
      const g = K.times(K.cand.lineA) * K.gain;
      stroke(3 * u, K.col(role), (.06 + .1 * fl) * lv * g);
      stroke(u, K.col(role, .12 + .4 * fl), a * g, false);
      stroke(.6 * u, K.col(role, .12), a * .35 * g, true);
      let s = null;
      if (look.rim) { if (look.hov) s = frac(t * .6 + sd); }
      else if (fl > 0) s = 1 - fl;
      else if (look.hov) { const q = frac((t + sd) / 1.2) * 1.2; s = q < .9 ? q / .9 : null; }
      else if (look.on || look.wait) s = frac((t + sd * 3) / 2.4);
      if (s !== null) {
        const h = Math.min(1, .5 + .5 * lv) * (look.rim ? .6 : fl > 0 ? 1 : sm(Math.min(s / .12, (1 - s) / .18))), len = look.rim ? .12 : .22;
        const px = q => [x + q[0] * r, y + q[1] * r];
        for (let i = 0; i < 10; i++) {
          const s0 = s - len * (1 - i / 10), s1 = Math.min(.9999, s - len * (1 - (i + 1) / 10));
          if (s0 < 0 || s1 <= s0) continue;
          line2(c, px(onPath(P, s0)), px(onPath(P, s1)), 1.3 * u, K.col('run', .3 + .04 * i, i / 10, s), h * (i + 1) / 10);
        }
        if (h > .004) {
          const p = px(onPath(P, Math.min(.9999, s)));
          if (!look.rim) halo(c, p[0], p[1], 3.2 * u, K.col('run', .4, 1, s), .45 * h);
          c.fillStyle = css(K.col('run', .35, 1, s), h); c.fillRect(p[0] - .65 * u, p[1] - .65 * u, 1.3 * u, 1.3 * u);
        }
      }
      c.restore();
    }
    const KEYS = [{ id: 'air', on: true }, { id: 'build', hov: true }, { id: 'research', wait: true, num: 3 }];
    function edgeKeys(c, K, st) {
      const vis = sm((st.u - OPEN.on) / .15) * (1 - sm((st.u - OPEN.off) / .15));
      if (vis <= .01) return 0;
      KEYS.forEach((key, i) => {
        const [x, y] = [KEY_AT[0] + i * 40, KEY_AT[1]], b = (key.hov ? 1.1 : .85) * vis, sd = i * 2.3 + 1;
        const deep = K.col(key.on ? 'key' : 'glass').map(v => v * (key.on ? .3 : .1));
        rr(c, x + .5, y + .5, 27, 27, 3); c.fillStyle = css(deep, key.on ? .7 * vis : Math.min(.75, .25 + .5 * b)); c.fill();
        icon(c, K, ICON.square, x + 14, y + 14, 13.5, .7, { rim: true, lv: b * .5, hov: key.hov, key: key.on, t: st.time, sd });
        icon(c, K, ICON[key.id], x + 14, y + 14, 9, 1, { lv: b, hov: key.hov, on: key.on, wait: key.wait, key: key.on, t: st.time, sd });
        if (!key.num) return;
        const base = y + 33;
        c.fillStyle = 'rgba(4,6,9,.85)'; c.fillRect(x + 6.5, base - 12, 15, 14);
        c.strokeStyle = css(K.col('line'), .55 * b); c.lineWidth = 1; c.strokeRect(x + 7, base - 11.5, 14, 13);
        c.font = '600 13px ui-monospace,Consolas,monospace'; c.textAlign = 'center'; c.textBaseline = 'alphabetic';
        c.fillStyle = css(K.col('line', .45), Math.min(1, b)); c.fillText(String(key.num), x + 14, base); c.textAlign = 'left';
      });
      return KEYS.length;
    }
    function wheel(c, K, st) {
      const u = st.u - AT.window - .3, go = 3.3;
      if (u < 0 || u > go + .45) return 0;
      const vis = u < go ? c01(u / .12) : 1 - c01((u - go) / .2), flare = u >= go ? 1 - (u - go) / .45 : 0, toward = u < .6 ? -1 : u < 1.9 ? 0 : 1;
      const [x, y] = WHEEL, t = st.time;
      let n = 0;
      c.save();
      c.globalAlpha = vis;
      if (vis > .01) icon(c, K, ICON.circle, x, y, 18, .8, { rim: true, lv: .32, t, sd: 1 });
      ['lamp', 'scan'].forEach((id, i) => {
        const fl = i === toward ? flare : 0, hov = i === toward && u < go, lit = hov || fl > 0, grow = lit ? 1.15 : 1;
        const py = y + (i ? 54 : -54), r = 21 * grow;
        c.globalAlpha = fl > 0 ? 1 : vis;
        if (c.globalAlpha <= .01) return;
        c.beginPath(); c.arc(x, py, r, 0, TAU); c.fillStyle = css([5, 8, 14], lit ? .9 : .62); c.fill();
        icon(c, K, ICON.circle, x, py, r, .9, { rim: true, lv: lit ? .9 : .38, hov, flare: fl, key: lit, t, sd: i * 1.7 });
        icon(c, K, ICON[id], x, py, 12.5 * grow, 1.25 * grow, { lv: lit ? 1 : .6, hov, flare: fl, key: lit, t, sd: i * 1.7 });
        n++;
      });
      c.restore();
      return n;
    }
    // the window's inside: the game's cool screen, never the drone's colour
    const ITEMS = { 0: 'scrap', 1: 'ore', 2: 'copper', 4: 'battery', 7: 'fruit' };
    function inside(c, st) {
      const u = st.u - OPEN.on, uc = st.u - OPEN.off, a = u < 0 ? 0 : uc < 0 ? c01((u - .03) / .1) : 1 - c01(uc / .08);
      if (a <= .003) return;
      const r = WIN_R, L = TONES.bag.light;
      c.save(); c.globalAlpha = a;
      c.fillStyle = css(TONES.bag.ground, .93); c.fillRect(r.x, r.y, r.w, r.h);
      c.font = '600 12px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.fillStyle = css(pale(L, .5), .92); c.fillText('СУМКА', r.x + 12, r.y + 20);
      for (let i = 0; i < 18; i++) {
        const x = r.x + 10 + (i % 6) * 49, y = r.y + 28 + (i / 6 | 0) * 44;
        rr(c, x + .5, y + .5, 43, 40, 2); c.fillStyle = css([20, 32, 50], .78); c.fill();
        c.strokeStyle = css(L, .36); c.lineWidth = 1; c.stroke();
        if (ITEMS[i] && ready(IMG[ITEMS[i]])) c.drawImage(IMG[ITEMS[i]], x + 4, y + 2, 36, 36);
      }
      c.restore();
    }

    // ---------- the world: three backdrops, the wreck the scanner reads, the bracelet ----------
    function backSpace(g, S) { drawBack({ c: g, S }); }
    function backRoom(g) {
      g.fillStyle = '#1b2327'; g.fillRect(0, 0, LW, LH);
      if (ready(IMG.wall)) {
        const tiles2 = () => { for (let x = 0; x < LW; x += 512) for (let y = -32; y < FLOOR_Y; y += 256) g.drawImage(IMG.wall, x, y, 512, 256); };
        tiles2();
        g.save(); g.globalCompositeOperation = 'lighter'; g.globalAlpha = .65; tiles2(); g.restore();   // lit
      }
      g.save(); g.globalCompositeOperation = 'lighter';
      for (const x of [260, 650]) { const gr = g.createRadialGradient(x, 18, 8, x, 18, 470); gr.addColorStop(0, css([214, 172, 116], .32)); gr.addColorStop(1, css([214, 172, 116], 0)); g.fillStyle = gr; g.fillRect(0, 0, LW, FLOOR_Y); }
      g.restore();
      for (const x of [260, 650]) { rr(g, x - 24, 0, 48, 14, 3); g.fillStyle = '#25282d'; g.fill(); g.strokeStyle = INK; g.lineWidth = 2; g.stroke(); g.fillStyle = '#e9cfa4'; g.fillRect(x - 17, 11, 34, 5); }
      g.fillStyle = '#25282d'; g.fillRect(0, FLOOR_Y, LW, LH - FLOOR_Y);
      g.fillStyle = '#6a675f'; g.fillRect(0, FLOOR_Y, LW, 3);
      for (let x = 0; x < LW; x += 64) { g.fillStyle = INK; g.fillRect(x, FLOOR_Y + 3, 2, LH - FLOOR_Y); g.fillStyle = '#4b4d50'; g.fillRect(x + 8, FLOOR_Y + 10, 2, 2); }
    }
    function backPlanet(g) {
      // the sky's picture (game/art/hd/planet/sky.png) by its colours down the middle
      const sky = g.createLinearGradient(0, 0, 0, FLOOR_Y);
      [[0, [94, 112, 128]], [.25, [121, 139, 151]], [.5, [160, 171, 169]], [.75, [209, 203, 181]], [1, [221, 214, 190]]].forEach(([q, col]) => sky.addColorStop(q, css(col)));
      g.fillStyle = sky; g.fillRect(0, 0, LW, FLOOR_Y);
      if (ready(IMG.clouds)) { g.save(); g.globalAlpha = .75; g.drawImage(IMG.clouds, -300, 12, 2048, 160); g.restore(); }
      for (const [y0, h, col, f] of [[338, 24, '#8f9c8c', .006], [382, 22, '#66796a', .011], [432, 12, '#3d5246', .023]]) {
        g.beginPath(); g.moveTo(0, FLOOR_Y);
        for (let x = 0; x <= LW; x += 10) g.lineTo(x, y0 + h * Math.sin(x * f + y0) + 3 * Math.sin(x * f * 7.3));
        g.lineTo(LW, FLOOR_Y); g.closePath(); g.fillStyle = col; g.fill();
      }
      if (ready(IMG.tree)) g.drawImage(IMG.tree, -46, FLOOR_Y - 418, 370, 422);
      if (ready(IMG.bush)) { g.drawImage(IMG.bush, 648, FLOOR_Y - 88, 180, 92); g.drawImage(IMG.bush, 876, FLOOR_Y - 62, 126, 64); }
      g.fillStyle = '#2b2925'; g.fillRect(0, FLOOR_Y, LW, LH - FLOOR_Y);
      g.fillStyle = '#3f5b43'; g.fillRect(0, FLOOR_Y, LW, 5);
      g.beginPath();
      for (let i = 0; i < 190; i++) { const x = hsh(i) * LW, h = 3 + hsh(i + 200) * 7; g.moveTo(x - 2, FLOOR_Y + 1); g.lineTo(x + (hsh(i + 400) - .5) * 6, FLOOR_Y - h); g.lineTo(x + 2, FLOOR_Y + 1); }
      g.fill();
    }
    const BACKS = { space: backSpace, room: backRoom, planet: backPlanet };
    const picsReady = () => Object.values(IMG).filter(ready).length;
    function backdrop(T) {
      const key = `${OPT.bg}|${T.S}|${picsReady()}`;
      if (T.bgKey !== key) {
        const w = Math.ceil(LW * T.S), h = Math.ceil(LH * T.S);
        if (!T.bgc || T.bgc.width !== w || T.bgc.height !== h) { T.bgc = document.createElement('canvas'); T.bgc.width = w; T.bgc.height = h; }
        const g = T.bgc.getContext('2d');
        g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over';
        BACKS[OPT.bg](g, T.S);
        T.bgKey = key;
      }
      return T.bgc;
    }
    const PLATE = mk(polyPts([[-84, 78], [-76, 34], [-50, 30], [-40, 4], [8, 0], [30, 18], [64, 22], [70, 46], [84, 60], [86, 78]]), 4242, .8);
    const RUST = [[-40, 44, 6], [22, 36, 5], [52, 62, 4], [-8, 64, 4]].map(([x, y, s], i) => blobAt(rng(4300 + i), x, y, s));
    function wreck(c) {
      c.save(); c.translate(WRECK.x, WRECK.top); c.lineCap = 'round';
      trace(c, PLATE); c.fillStyle = '#2b2e34'; c.fill();
      c.save(); c.clip();
      c.fillStyle = '#3f434a'; c.fillRect(-90, 0, 180, 30); c.fillStyle = '#353940'; c.fillRect(-90, 30, 180, 22);
      c.fillStyle = '#1f2226'; c.fillRect(-90, 51, 180, 2); c.fillRect(-16, 0, 2, 80); c.fillRect(40, 0, 2, 80);
      for (const b of RUST) rust(c, b);
      c.restore();
      ink(c, PLATE, 0, 2.2);
      c.restore();
    }
    // the tracker on the near shin: its picture's box about the knee, its light where hero_tracker_light lies
    function bracelet(c, h, C) {
      const k = h.P.legF[1], f = h.P.legF[2];
      c.save();
      c.translate(h.x, h.y); c.scale(h.face, 1); c.translate(k[0] * Z, k[1] * Z); c.rotate(Math.atan2(f[1] - k[1], f[0] - k[0]) - Math.PI / 2);
      c.translate(-24, -20.72);
      if (ready(IMG.strap)) c.drawImage(IMG.strap, 0, 0, 60, 68);
      rr(c, 15, 29, 22, 6, 2); c.fillStyle = css(C); c.fill();
      halo(c, 26, 32, 20, C, .3);
      c.restore();
    }

    // ---------- the strip: the whole palette at once ----------
    const ST = { y: SCENE_H, h: LH - SCENE_H, x: 8, w: LW - 16, cy: 50, tick: 15 };
    const cellAt = (x, n) => clamp(Math.floor((x - ST.x) / (ST.w / n)), 0, n - 1);
    const STRIP_GROUND = { space: '#070a10', room: '#2c3539', planet: '#a0aba9' };
    function strip(T, st) {
      const n = st.pal.list.length, tick = Math.floor(st.time * ST.tick + T.num * .2);
      const key = `${tick}|${st.index}|${OPT.bg}|${OPT.source}|${OPT.palette}|${T.S}|${picsReady()}`;
      if (T.stripKey !== key) {
        T.stripKey = key;
        const w = Math.ceil(LW * T.S), h = Math.ceil(ST.h * T.S);
        if (!T.stc || T.stc.width !== w || T.stc.height !== h) { T.stc = document.createElement('canvas'); T.stc.width = w; T.stc.height = h; }
        const g = T.stc.getContext('2d'), day = OPT.bg === 'planet', txt = day ? '#14171a' : '#d6dbe2';
        g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over';
        g.fillStyle = STRIP_GROUND[OPT.bg]; g.fillRect(0, 0, LW, ST.h);
        g.fillStyle = 'rgba(0,0,0,.6)'; g.fillRect(0, 0, LW, 2);
        g.font = '12px system-ui,"Segoe UI",sans-serif'; g.textAlign = 'left'; g.textBaseline = 'alphabetic'; g.fillStyle = txt;
        g.fillText(OPT.source === 'bracelet'
          ? 'Как у игрока: шесть цветов экипажа, браслет того же цвета. Переключатель «Палитра» сейчас не действует.'
          : `Палитра «${st.pal.name}». У спутника свой цвет, у браслета свой.`, 12, 18);
        st.pal.list.forEach((col, i) => {
          const w1 = ST.w / n, x0 = ST.x + i * w1, mx = x0 + w1 / 2, on = i === st.index;
          if (on) { rr(g, x0 + 2, 24, w1 - 4, 70, 6); g.strokeStyle = txt; g.lineWidth = 1.5; g.stroke(); }
          const idle = { time: st.time, u: 0, A: LOOK, sh: { cur: 'idle', prev: 'idle', t0: 0, e: 9 }, open: 0, away: [], tilt: 0, glow: 0, op: st.time, flurry: 0, tint: 0 };
          drawDrone2(g, inkOf(T.cand, col.rgb), idle, [mx, ST.cy], K1, i * 1.3);
          g.fillStyle = css(col.rgb); g.fillRect(x0 + 10, 82, 7, 7);
          g.font = `${on ? '700 ' : ''}12px system-ui,"Segoe UI",sans-serif`; g.textAlign = 'left'; g.fillStyle = txt;
          g.fillText(col.name, x0 + 22, 89);
        });
      }
      T.c.drawImage(T.stc, 0, ST.y, LW, ST.h);
      return n;
    }

    // ---------- the tile ----------
    function label(c, st) {
      const font = 'system-ui,"Segoe UI",sans-serif';
      rr(c, 12, 10, 384, 42, 6); c.fillStyle = 'rgba(5,7,11,.78)'; c.fill();
      c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.font = `600 14px ${font}`; c.fillStyle = '#e3e0d8'; c.fillText(`${st.si + 1}/${STAGES.length} · ${st.stage.say}`, 22, 28);
      c.fillStyle = st.colour.hex; c.fillRect(22, 36, 9, 9);
      c.font = `12px ${font}`; c.fillStyle = 'rgba(206,212,222,.8)';
      c.fillText(`${st.colour.name}, ${st.index + 1} из ${st.pal.list.length}${OPT.colour < 0 ? ' · по кругу' : ''}`, 37, 45);
    }
    function loupe(T, st, K) {
      const c = T.c;
      c.save();
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip();
      c.drawImage(T.bgc, LP.cx - PERCH[0] * LP.k, LP.cy - PERCH[1] * LP.k, LW * LP.k, LH * LP.k);
      T.probe = 'drone';
      drawDrone2(c, K, st, [LP.cx, LP.cy + Math.sin(TAU * st.time / 1.8) * Z], K1 * LP.k);
      T.probe = null;
      c.restore();
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.strokeStyle = 'rgba(20,24,30,.8)'; c.lineWidth = 2; c.stroke();
      rr(c, LP.x + 6, LP.y + 6, 62, 18, 4); c.fillStyle = 'rgba(5,7,11,.7)'; c.fill();
      c.font = '11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.fillStyle = '#c8cdd6'; c.fillText('лупа ×2,5', LP.x + 11, LP.y + 19);
    }

    // the colour buttons take the palette's names; the palette's own buttons dim while the bracelet decides
    let shownBar = '';
    function syncButtons() {
      const pal = palNow(), n = pal.list.length;
      if (OPT.colour >= n) { OPT.colour %= n; REFRESH.forEach(f => f()); }
      const key = OPT.source + OPT.palette;
      if (shownBar === key) return;
      shownBar = key;
      document.getElementById('paletteMode').style.opacity = OPT.source === 'bracelet' ? '.35' : '1';
      [...document.getElementById('colourMode').children].filter(b => String(b.tagName || b.tag).toLowerCase() === 'button').slice(1).forEach((b, i) => {
        b.style.display = i < n ? '' : 'none';
        if (i < n) { b.textContent = pal.list[i].name; b.style.borderLeft = `10px solid ${pal.list[i].hex}`; }
      });
    }

    // a frame opened straight from the address: #at=6.5&colour=3&bg=room&palette=ash&source=bracelet&pause
    let clock = 0;
    const asked = typeof location === 'undefined' ? '' : location.hash.slice(1);
    for (const part of asked.split('&')) {
      const [key, v] = part.split('=');
      if (key === 'pause') OPT.playing = false;
      else if (key === 'at' && isFinite(+v) && +v >= 0) clock = +v;
      else {
        const t = PAGE.toggles.find(q => q.key === key), opt = t && t.opts.find(o => String(o[1]) === v);
        if (opt) OPT[key] = opt[1];
      }
    }

    let made = 0;
    return {
      cands,
      expose: { STAGES, LOOP, PALS, SUIT, RED, palNow, seek: t => { clock = t; for (const T of tiles) T.time = t; } },
      init(T) {
        T.num = made++; T.time = clock; T.me.face = FACE; T.st = null;
        T.cv.addEventListener('pointerdown', e => {
          const r = T.cv.getBoundingClientRect(), y = (e.clientY - r.top) / (r.height || 1) * LH;
          if (y < ST.y + 22) return;
          OPT.colour = cellAt((e.clientX - r.left) / (r.width || 1) * LW, palNow().list.length);
          REFRESH.forEach(f => f()); dirtyAll();
        });
      },
      onScale(T) { T.bgKey = T.stripKey = null; },
      // one clock for every tile, so a tile scrolled back into view is on the others' stage
      step(T) {
        clock = Math.max(clock, T.time); T.time = clock;
        syncButtons();
        const st = T.st = stateOf(T.time), me = T.me;
        T.K = inkOf(T.cand, st.colour.rgb, st.gate, st.tint);
        me.x = HX; me.face = FACE; me.look = st.head;
      },
      ownDrone: () => true,
      back(T) {
        const c = T.c, st = T.st;
        c.drawImage(backdrop(T), 0, 0, LW, LH);
        wreck(c);
        const { cur, prev, e } = st.sh, lamp = cur === 'lamp' ? sm(e) : prev === 'lamp' ? 1 - sm(e) : 0;
        lampOn(c, st.D[0], st.D[1], HX, FLOOR_Y, FACE, .6 * lamp, 2.6);
      },
      overHero(T, h) {
        T.band = braceletFor(T.st.colour);
        T.probe = 'bracelet';
        bracelet(T.c, h, T.band.rgb);
        T.probe = null;
      },
      overDrone(T) {
        const c = T.c, K = T.K, st = T.st, dm = T.dm = { stage: st.stage.id, index: st.index, colour: st.colour, gate: st.gate, band: T.band };
        T.probe = 'drone';
        dm.shaped = drawDrone2(c, K, st, st.D, K1);
        Object.assign(dm, gauge(c, K, st));
        T.probe = null;
      },
      screen(T) {
        const c = T.c, st = T.st, K = T.K, dm = T.dm;
        T.probe = 'inside';
        inside(c, st);
        T.probe = 'ui';
        dm.corners = corners(c, K, st);
        dm.keys = edgeKeys(c, K, st);
        dm.wheel = wheel(c, K, st);
        T.probe = null;
        loupe(T, st, K);
        dm.strip = strip(T, st);
        label(c, st);
      },
    };
  },

  // build.js --break <name>: a colour a player could take for the alarm
  breaks: { red: [["['калий', '#c888d8']", "['калий', '#f0483c']"]] },

  // a whole loop live; then every stage in every colour of every palette, source and backdrop, read off the colours really set on the
  // canvas; a pause; a pick, saved again as palette and source change
  check(K) {
    const X = K.X, SEC = { idle: 1.2, lamp: 1.5, scan: 2.2, window: 2, love: 1.6, alarm: 1.6, back: 1.5 };
    const PALETTES = ['salts', 'crew', 'ash'], SOURCES = ['own', 'bracelet'], BGS = ['space', 'room', 'planet'];
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };

    // ---------- the palettes ----------
    const hueOf = ([r, g, b]) => { const M = Math.max(r, g, b), d = M - Math.min(r, g, b); if (!d) return 0; const h = M === r ? (g - b) / d : M === g ? (b - r) / d + 2 : (r - g) / d + 4; return ((h * 60) % 360 + 360) % 360; };
    const satOf = c => (Math.max(...c) - Math.min(...c)) / (Math.max(...c) || 1), apart = (a, b) => Math.abs(((a - b + 540) % 360) - 180);
    const lin = v => { v /= 255; return v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4; };
    const labOf = c => { const [r, g, b] = c.map(lin), f = t => t > .008856 ? Math.cbrt(t) : 7.787 * t + 16 / 116, x = f((.4124 * r + .3576 * g + .1805 * b) / .9505), y = f(.2126 * r + .7152 * g + .0722 * b), z = f((.0193 * r + .1192 * g + .9505 * b) / 1.089); return [116 * y - 16, 500 * (x - y), 200 * (y - z)]; };
    const dE = (a, b) => { const p = labOf(a), q = labOf(b); return Math.hypot(p[0] - q[0], p[1] - q[1], p[2] - q[2]); };
    const redHue = hueOf(X.RED);
    need(X.SUIT.map(c => c.hex).join() === '#e8b04a,#5fc8c0,#a08cf0,#8cd06a,#5a8ef0,#e88cc8', 'the crew colours are Suits.All exactly');
    for (const name of PALETTES) {
      const L = X.PALS[name].list;
      console.log(`${name}: ${L.map(c => `${c.name} ${c.hex}`).join(', ')}`);
      need(L[0].hex === '#ff8a3a', `${name}: today's ember comes first`);
      L.forEach((c, i) => {
        if (i) need(satOf(c.rgb) < .2 || apart(hueOf(c.rgb), redHue) >= 28, `${name}: ${c.name} ${c.hex} could be taken for the alarm's red`);
        L.forEach((d, j) => { if (j > i) need(dE(c.rgb, d.rgb) >= (j === i + 1 ? 22 : 14), `${name}: ${c.name} and ${d.name} are too alike (${dE(c.rgb, d.rgb).toFixed(1)})`); });
      });
    }

    // ---------- the colours set on each canvas, by what was being drawn ----------
    const parse = s => { const m = /^rgba?\((\d+),(\d+),(\d+)(?:,([\d.]+))?\)$/.exec(s); return m && (m[4] === undefined || +m[4] > 0) ? [+m[1], +m[2], +m[3]] : null; };
    const seen = K.tiles.map(() => ({ drone: new Map(), ui: new Map(), inside: new Map(), bracelet: new Map() }));
    K.tiles.forEach((t, j) => {
      const take = v => {
        if (!t.probe || typeof v !== 'string') return;
        const q = parse(v);
        if (q) seen[j][t.probe].set(q.join(), q);
      };
      t.c = new Proxy(t.c, {
        get(o, k) {
          if (k === 'createLinearGradient' || k === 'createRadialGradient') return (...a) => { const g = o[k](...a); return { addColorStop(s, col) { take(col); g.addColorStop(s, col); } }; };
          return o[k];
        },
        set(o, k, v) { if (k === 'fillStyle' || k === 'strokeStyle') take(v); o[k] = v; return true; },
      });
    });

    // ---------- the plan ----------
    const L = Math.round(X.LOOP * 60) + 4, plan = new Map();
    let f = L;
    PALETTES.forEach((pal, pi) => SOURCES.forEach((src, si) => BGS.forEach((bg, bi) => {
      const n = src === 'bracelet' ? X.SUIT.length : X.PALS[pal].list.length;
      for (let i = 0; i < n; i++) for (const stage of X.STAGES) {
        const at = stage.t0 + SEC[stage.id];
        K.at(f, () => { K.seg('paletteMode', pi); K.seg('sourceMode', si); K.seg('bgMode', bi); K.seg('colourMode', i + 1); X.seek(at); });
        plan.set(f, { pal, src, bg, i, n, stage: stage.id, at });
        f++;
      }
    })));
    const SWEPT = f, P = f + 2, E = P + 70;
    let dimmed = '', name = '';
    K.at(L + 1, () => { name = K.byId.colourMode.children.filter(c => c.listeners.click)[2].textContent; });
    K.at(SWEPT - 1, () => { dimmed = K.byId.paletteMode.style.opacity; });
    K.at(P, () => K.seg('playMode', 1));
    K.at(P + 30, () => K.seg('playMode', 0));
    const docs = [];
    let status = '', copied = '';
    K.at(P + 36, () => { K.seg('paletteMode', 0); K.seg('sourceMode', 0); K.click(2, '.pick'); });
    K.at(P + 40, () => { docs.push(K.picked()); K.seg('paletteMode', 2); });
    K.at(P + 44, () => { docs.push(K.picked()); K.seg('sourceMode', 1); });
    K.at(P + 48, () => { docs.push(K.picked()); status = K.byId.status.textContent; K.byId.copy.listeners.click[0](); copied = K.byId.fallback.value; });

    // is col the colour C, plain or paled toward white
    const isOf = (col, C) => { const k = [0, 1, 2].map(i => 255 - C[i]); let i = k.indexOf(Math.max(...k)); const p = (col[i] - C[i]) / (k[i] || 1); return p >= -.02 && p < .9 && [0, 1, 2].every(q => Math.abs(col[q] - (C[q] + k[q] * p)) < 3); };
    const reddish = c => Math.max(...c) > 120 && satOf(c) > .45 && apart(hueOf(c), redHue) < 15;
    const per = K.tiles.map(() => ({ order: [], held: {}, stages: new Set(), drawn: 0, alarms: 0, coloured: 0, paused: 0, last: -1, inside: null }));
    const alarmRef = { set: null }, idleSets = new Map();
    let apartFrames = 0;
    K.run(E, (t, frame) => {
      const j = K.tiles.indexOf(t), q = per[j], d = t.dm, id = t.cand.id, S = seen[j], p = plan.get(frame);
      if (d) {
        if (frame < L) {
          if (q.order[q.order.length - 1] !== d.stage) q.order.push(d.stage);
          if (frame < Math.round(X.LOOP * 60) - 1) q.held[d.stage] = (q.held[d.stage] || 0) + 1 / 60;
          if (j && Math.abs(t.time - K.tiles[0].time) > 2 / 60) apartFrames++;
        }
        if (p) {
          const at = `${id}: ${p.pal}/${p.src}/${p.bg} colour ${p.i} ${p.stage}`, pal = p.src === 'bracelet' ? X.SUIT : X.PALS[p.pal].list, C = pal[p.i];
          q.drawn++;
          q.stages.add(`${p.pal}|${p.src}|${p.bg}|${p.stage}`);
          if (d.stage !== p.stage || d.colour.hex !== C.hex || d.strip !== p.n) K.fail(`${at}: drew ${d.stage} in ${d.colour.hex}, ${d.strip} in the strip`);
          if (p.stage === 'scan' && !(d.lit >= 10 && d.dim >= 5)) K.fail(`${at}: the gauge has ${d.lit} lit and ${d.dim} dim`);
          if (p.stage === 'window' && !(d.corners === 4 && d.keys === 3 && d.wheel === 2)) K.fail(`${at}: corners ${d.corners}, keys ${d.keys}, wheel ${d.wheel}`);
          if (['lamp', 'love', 'alarm'].includes(p.stage) && d.shaped < 50) K.fail(`${at}: only ${d.shaped} sparks on the shape`);
          if (p.src === 'bracelet' ? d.band.hex !== C.hex : d.band.hex === C.hex) K.fail(`${at}: the bracelet is ${d.band.hex}`);
          if (!S.bracelet.has(d.band.rgb.join())) K.fail(`${at}: the bracelet is not drawn in its colour`);
          const drone = [...S.drone.values()], ui = [...S.ui.values()], lit = drone.concat(ui);
          if (p.stage === 'alarm') {
            for (const c of drone) if (satOf(c) > .2 && apart(hueOf(c), redHue) > 15) { K.fail(`${at}: ${c} is not the alarm's red`); break; }
            const set = [...S.drone.keys()].sort().join(';');
            if (alarmRef.set === null) alarmRef.set = set;
            else if (set !== alarmRef.set) K.fail(`${at}: the alarm differs from the shipped one`);
            q.alarms++;
          } else {
            for (const c of lit) if (reddish(c)) { K.fail(`${at}: ${c} reads as the alarm's red`); break; }
            if (!drone.some(c => isOf(c, C.rgb))) K.fail(`${at}: the drone carries none of its colour`); else q.coloured++;
            if (p.stage === 'window') {
              if (!ui.some(c => isOf(c, C.rgb))) K.fail(`${at}: the frames and icons carry none of its colour`);
              const inside = [...S.inside.keys()].sort().join(';');
              if (!inside || (q.inside !== null && q.inside !== inside)) K.fail(`${at}: the window's inside changed`);
              q.inside = inside;
            }
            if (p.stage === 'idle' && p.pal === 'salts' && p.src === 'own' && p.bg === 'space' && p.i === 4) idleSets.set(id, [...S.drone.keys()].sort().join(';'));
          }
        }
      }
      if (frame > P && frame < P + 29) { if (q.last >= 0 && t.time !== q.last) K.fail(`${id}: time ran while paused`); q.paused++; }
      q.last = t.time;
      for (const k in S) S[k].clear();
    });

    console.log('stages, s:', X.STAGES.map(s => `${s.id} ${s.t0}..${s.t0 + s.len} (sampled at ${s.t0 + SEC[s.id]})`).join(', '));
    const colours = PALETTES.reduce((n, p) => n + 3 * (X.PALS[p].list.length + X.SUIT.length), 0);
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id;
      console.log(`${id}: ${q.drawn} sampled frames, alarms ${q.alarms}, in its colour ${q.coloured}, combinations ${q.stages.size}, paused ${q.paused}`);
      need(q.order.slice(0, X.STAGES.length).join() === X.STAGES.map(s => s.id).join(), `${id}: the loop in order`);
      need(X.STAGES.every(s => Math.abs((q.held[s.id] || 0) - s.len) < .05), `${id}: each stage as long as planned`);
      need(q.drawn === colours * X.STAGES.length, `${id}: every colour of every palette, source and backdrop at every stage`);
      need(q.stages.size === 18 * X.STAGES.length, `${id}: every stage in every palette, source and backdrop`);
      need(q.alarms === colours && q.paused > 20, `${id}: the alarm in every colour; the pause held`);
    });
    need(apartFrames === 0, 'every tile is at the same moment of the loop');
    need(new Set(idleSets.values()).size === K.tiles.length, 'the five ways differ at rest');
    need(name === 'уголь' || name === 'натрий', `the colour buttons carry the palette's names (${name})`);
    need(dimmed === '.35', 'the palette is dimmed while the bracelet decides');

    const id = K.tiles[2].cand.id, want = [['salts', 'own'], ['ash', 'own'], ['ash', 'bracelet']];
    console.log('docs:', docs.map(d => JSON.stringify(d)).join(' | '), '| copy:', copied);
    want.forEach(([palette, source], i) => {
      const d = docs[i];
      need(d && d.pick === id && d.palette === palette && d.source === source && Object.keys(d).sort().join() === 'palette,pick,source', `pick ${i}: the way, the palette and the source saved, nothing else`);
    });
    need(status.includes(`«${K.tiles[2].cand.name}»`) && status.includes('Пепельные') && status.includes('как у игрока'), 'the status names all three');
    need(copied.includes(`(${id})`) && copied.includes('Пепельные') && copied.includes('как у игрока'), 'the copy text names all three');
    return ok;
  },
};
