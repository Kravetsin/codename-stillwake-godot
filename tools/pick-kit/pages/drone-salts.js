// Round two of the companion's colour: «Соли в пламени» in the player's colour, five ways to keep its two colours readable on
// space, a room and the day sky, and the set the players' colours grow to. The drone, corners, keys, gauge and thoughts are the
// shipped ones, as on drone-colour.js, here only painted differently. An art-pick page on tools/pick-kit (README.md).
// For a frame: index.html#at=<s into the loop>&colour=<index|-1>&bg=space|room|planet&palette=crew8|salts8|ten&pause
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-salts.js <out-dir> [--break red]
const HD = 'game/art/hd/';

module.exports = {
  title: 'Соли спутника',
  heading: 'Соли спутника: два цвета, которые видно',
  window: false,
  tile: [624, 658],
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">«Соли в пламени» выбраны. Теперь выбираешь <b>как их рисовать</b>, чтобы хоть один из двух цветов всегда был виден, и <b>набор цветов игроков</b>. Нажми «Выбрать этот»: набор сохранится вместе с плиткой.</p>
<p class="lead">У спутника цвет игрока, тот же, что на браслете. <b>Уголь, трещины и глаз остаются угольными</b>, всё, что улетает от угля, разгорается в цвет. Янтаря в наборах больше нет: он сливался с углём.</p>
<p class="lead">В каждой плитке один момент сразу на трёх фонах, в размер игры. Под ними весь набор на героях с браслетами; первые четыре самые разные, их игроки получают первыми. «Фон ряда» меняет фон под ним, щелчок по герою берёт его цвет.</p>
<p class="miss">Не показано: звук; где игрок меняет цвет; отметки на экране; колесо; тревога (она красная, как сейчас); нехватка воздуха, она на другой странице; свет Godot, с ним цвета выйдут чуть иначе.</p>`,
  toggles: [
    { id: 'bgMode', bar: 1, label: 'Фон ряда:', key: 'bg', opts: [['космос', 'space'], ['отсек', 'room'], ['планета днём', 'planet']] },
    { id: 'paletteMode', label: 'Набор:', key: 'palette', opts: [['Восемь экипажа', 'crew8'], ['Восемь: огни металлов', 'salts8'], ['Десять', 'ten']], saved: true, short: 'набор' },
    // the buttons take the names of the set in effect (syncButtons)
    { id: 'colourMode', label: 'Цвет:', key: 'colour', opts: [['по кругу', -1], ...Array.from({ length: 10 }, (_, i) => [String(i + 1), i])] },
  ],
  pick: { doc: 'picks/droneSalts', subject: 'Соли спутника', what: ['раскраску', 'раскраской'] },
  pictures: {
    core: HD + 'drone_core.png', strap: HD + 'hero_tracker.png', wall: HD + 'back_alloy.png', tree: HD + 'planet/acacia_0.png', clouds: HD + 'planet/clouds.png',
    scrap: HD + 'items/scrap.png', ore: HD + 'items/ironOre.png', copper: HD + 'items/copper.png', battery: HD + 'items/battery.png',
  },

  page() {
    const TAU = Math.PI * 2;

    // ---------- the sets, in the order players get them: the first four the farthest apart ----------
    const hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
    const colours = list => list.map(([name, h]) => ({ name, hex: h, rgb: hex(h) }));
    const SETS = {
      crew8: { name: 'Восемь экипажа', list: colours([['лайм', '#d8ec54'], ['бирюза', '#5fc8c0'], ['синий', '#5a8ef0'], ['пурпур', '#dc54cc'], ['голубой', '#8cc4f8'], ['фиолетовый', '#8c64e8'], ['розовый', '#e88cc8'], ['зелёный', '#8cd06a']]) },
      salts8: { name: 'Восемь: огни металлов', list: colours([['барий', '#d4e474'], ['мышьяк', '#7cccf4'], ['цезий', '#7068e8'], ['рубидий', '#d04c8e'], ['медь', '#2ca084'], ['калий', '#d0a0ec'], ['селен', '#3478c0'], ['бор', '#5cbc50']]) },
      ten: { name: 'Десять', list: colours([['лайм', '#d8ec54'], ['бирюза', '#288c7c'], ['синий', '#3c80d8'], ['пурпур', '#d84cc8'], ['зелёный', '#38ac3c'], ['розовый', '#ec88b8'], ['голубой', '#50c8ec'], ['мята', '#8ce4a8'], ['фиолетовый', '#8050d8'], ['сирень', '#c4a8f4']]) },
    };
    const setNow = () => SETS[OPT.palette];

    // ---------- DroneInk's numbers ----------
    const SOOT = [38, 39, 43], PX = .3968, K1 = PX * Z, N = 120, SIDE = .95;
    const ORB = { r: 6.3, more: 1.4, wide: 1.05, flat: .3, tilt: -.3, tiltMore: 1.05, pace: .8, paceMore: .25, hurry: 2.2, line: .4, a: .12 };
    const CRACKS = [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]];
    const NDOT = 26, DOT = { step: 7, side: 3.2, in: .4, out: .4, lag: .4, span: .6, lagOut: .35, spanOut: .65 };

    // ---------- colour ----------
    const lum = c => (.2126 * c[0] + .7152 * c[1] + .0722 * c[2]) / 255, L0 = lum(EMBER);
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    // added light clips to white where it piles up: a stronger hue, paled less, keeps its colour there
    const strong = c => { const m = Math.max(...c); return c.map(v => Math.max(0, m - (m - v) * 1.3)); };
    const sooty = c => mix3(SOOT, strong(c).map(v => v * .5), .4);
    const roomOf = c => lum(c) <= L0 ? 1 : clamp(1 - (lum(c) - L0) * 2.3, .35, 1);
    const gainOf = c => clamp(Math.sqrt(L0 / lum(c)), .85, 1.3);
    const CREAM = pale(EMBER, .65);
    const burning = (C, q) => q < .25 ? EMBER : q < .45 ? mix3(EMBER, CREAM, (q - .25) / .2) : mix3(CREAM, C, sm((q - .45) / .3));   // round one's
    const BURNT = .4;   // else a spark is the coal's until here and the player's beyond: a mix of the two is a third colour, or red

    // ---------- the candidates ----------
    // laid: what lies under the light with normal blend, as wide as it must be to hold an edge on a light ground. fill: the colour
    // itself is laid and only the coal and the heads are added. The rest are multiples of the shipped numbers.
    const TWO = { coreA: .8, coreR: .62, eyeR: .8, eyeA: .55, crackW: 1.8, crackA: 1.5, orbA: 4, orbW: 1.3, lineA: 1.6, tails: [.78, .36, .14] };
    const cands = [
      {
        ...TWO, id: 'soot', name: 'На саже',
        idea: 'Под светом лежит тонкая сажа, чуть подкрашенная цветом. На тёмном виден свет, на светлом тёмная цветная нить. Облачка нет.',
        time: 'В игре недорого: те же линии и искры рисуются ещё раз снизу, обычной краской.',
        laid: { w: 1.2, a: .9, shape: 1.35, frame: 1.8, side: 1.5, tails: [.92, .5, .18] }, tails: [.9, .4, .12], orbA: 5.2, lineA: 1.9,
      },
      {
        ...TWO, id: 'clean', name: 'Чистый свет',
        idea: 'Один свет, под ним ничего. Цвет гуще, чем в первом круге, и без облачка. В космосе чисто, на дневном небе тает.',
        time: 'В игре дешевле всех: меняются только цвета того, что уже рисуется.',
      },
      {
        id: 'flame', name: 'С облачком',
        idea: 'Соли из первого круга без изменений, для сравнения: цветное облачко и бледная середина пламени между углём и цветом.',
        time: 'В игре дёшево: одно свечение на спутник и нити, покрашенные по кускам.',
        round1: true, coreA: 1.3, crackA: 1.3, aura: .22, orbA: 3, orbW: 1.4,
      },
      {
        ...TWO, id: 'smoke', name: 'В дымке',
        idea: 'Вместо цветного облачка маленький тёмный дым. Свету есть на чём гореть и днём, дым идёт и за фигурами, и за уголками окна.',
        time: 'В игре дёшево: одно мягкое пятно на спутник и широкая бледная линия под каждой фигурой.',
        smoke: { r: 14, a: .62, line: .17 },
      },
      {
        ...TWO, id: 'enamel', name: 'Плотный цвет',
        idea: 'Цвет лежит краской и не белеет ни на каком фоне. Светятся только уголь и головы искр. Меньше похоже на огонь, зато цвет самый верный.',
        time: 'В игре недорого: столько же линий, но почти все обычной краской, свет остаётся углю и головам искр.',
        fill: true, laid: { w: .72, a: 1, shape: .85, frame: 1.6, side: 1.2, tails: [1, .62, .25] },
      },
    ];
    // col(role, p, q, o): role core | eye | thread | spark | line | run | glass; p the shipped paleness; q how far out of the coal
    // (a run's: toward its head); o how far along a run
    function inkOf(cand, C) {
      const old = !!cand.round1, lit = old || cand.fill ? C : strong(C), coal = old ? EMBER : strong(EMBER), room = roomOf(C);
      const own = p => p ? pale(lit, p * (old ? room : .4)) : lit, ember = p => p ? pale(coal, p * (old ? 1 : .4)) : coal;
      const far = q => q >= BURNT;
      return {
        cand, C, lit, coal, gain: old || !cand.fill ? gainOf(C) : 1, laid: cand.laid, fill: !!cand.fill, smoke: cand.smoke, far,
        times: key => cand[key] === undefined ? 1 : cand[key],
        // the mark's colour laid under its light
        under: q => cand.fill ? far(q) ? lit : coal : sooty(far(q) ? C : EMBER),
        col(role, p = 0, q = 1, o = 0) {
          if (role === 'core' || role === 'eye') return ember(p);
          if (role === 'run') { const at = o * 1.1 + (1 - q) * .55; return old ? pale(burning(C, at), p * .6) : far(at) ? own(p) : ember(p); }
          if (role === 'spark' || role === 'thread') return old ? pale(burning(C, q), p * room) : far(q) ? own(p) : ember(p);
          return own(p);
        },
      };
    }

    // ---------- paths, the shipped shapes and the keys' icons ----------
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
    const LOVE = mkPath(HEART.map(p => p.map(([x, y]) => [x * 22, y * 22 - 50])));

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
    const RIM_KEY = (() => {
      const p = [], r = .24;
      for (const [x, y, a] of [[1 - r, -1 + r, -Math.PI / 2], [1 - r, 1 - r, 0], [-1 + r, 1 - r, Math.PI / 2], [-1 + r, -1 + r, Math.PI]]) p.push(...arcP(x, y, r, 4, a, a + Math.PI / 2));
      p.push(p[0].slice());
      return mkIcon([p]);
    })();
    const ICON = {
      air: motif(['M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 'M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 'M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2']),
      build: motif([rrD(.4, 1.8, 7.1, 3.5, .55), 'M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z',
        rrD(.4, 6.2, 3.1, 3.5, .5), rrD(4.4, 6.25, 7.2, 3.5, .55), rrD(12.5, 6.2, 3.1, 3.5, .5), rrD(.4, 10.6, 7.1, 3.5, .55),
        'M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z']),
      research: motif(['M 5.3 10.8 C 2.4 9.3 1.8 5.4 3.8 3.0 C 5.8 0.6 10.2 0.6 12.2 3.0 C 14.2 5.4 13.6 9.3 10.7 10.8 L 10.4 12.2 L 5.6 12.2 Z', rrD(5.5, 12.7, 5, 1.6, .3),
        'M 6.7 14.8 L 9.3 14.8 L 8.7 15.8 L 7.3 15.8 Z', 'M 6.5 9.8 L 7 6.6 L 8 8.2 L 9 6.6 L 9.5 9.8']),
    };

    // ---------- the loop, every moment worked out from the clock alone ----------
    const STAGES = [
      { id: 'idle', len: 2.5, say: 'покой' },
      { id: 'lamp', len: 2, say: 'фонарь' },
      { id: 'scan', len: 3.5, say: 'скан и шкала' },
      { id: 'thought', len: 4, say: 'мысль: верность' },
      { id: 'window', len: 3, say: 'уголки окна и кнопки' },
    ];
    const AT = {};
    let LOOP = 0;
    for (const s of STAGES) { s.t0 = LOOP; AT[s.id] = LOOP; LOOP += s.len; }
    const READ = { on: AT.scan + .5, off: AT.scan + 2.4 }, OPEN = { on: AT.window + .15, off: AT.window + 2.5 }, HELD = 3.5;   // a thought: .5 in, 3 held, .5 out
    const SHAPES = [[0, 'idle'], [AT.lamp, 'lamp'], [AT.scan, 'scan'], [AT.thought, 'love'], [AT.thought + HELD, 'idle']];
    function shapeAt(u) {
      let i = 0;
      while (i + 1 < SHAPES.length && u >= SHAPES[i + 1][0]) i++;
      const [t0, cur] = SHAPES[i], prev = i ? SHAPES[i - 1][1] : 'idle', dur = cur === 'love' || prev === 'love' ? .5 : .6;
      return { cur, prev, t0, e: i ? (u - t0) / dur : 9 };
    }
    const openOf = u => u < READ.on ? 0 : u < READ.off ? sm((u - READ.on) / .3) : 1 - sm((u - READ.off - .4) / .4);
    function awayOf(u, j) {
      if (u < READ.on) return 0;
      if (u < READ.off) return sm(((u - READ.on) / DOT.in - hsh(j + 5) * DOT.lag) / DOT.span);
      return 1 - sm(((u - READ.off) / DOT.out - hsh(j + 2) * DOT.lagOut) / DOT.spanOut);
    }

    // ---------- where things are, in a ground's own pane, screen px ----------
    const GROUNDS = [['space', 'космос'], ['room', 'отсек'], ['planet', 'планета днём']];
    const PANE = { w: 208, h: 372, y: 24, floor: 352 }, HX = 130, FACE = -1, F = PANE.floor;
    const PERCH = [HX - FACE * 32, F - 187], WAIST = [HX + FACE * 64, F - 48];
    const HELMET = [HX + FACE * Z, F - (RIG.Neck + 6.2) * Z], PRESSED = [HX - FACE * 4.5 * Z, HELMET[1] - 6.25 * Z];
    const CHUNK = { x: 98, top: 98, k: .5 }, GAUGE = [98, 82], WIN_R = { x: 14, y: 44, w: 180, h: 84 }, KEY_AT = [14, 144];
    const aimTo = (a, b) => Math.atan2(b[1] - a[1], b[0] - a[0]);
    const UP = -Math.PI / 2, LOOK = -2.5, FLOOR_AIM = [20, F], READ_AIM = [CHUNK.x, CHUNK.top];
    const AIMS = [[0, LOOK], [AT.lamp, aimTo(PERCH, FLOOR_AIM)], [AT.scan, aimTo(PERCH, READ_AIM)], [AT.thought, aimTo(PRESSED, HELMET)], [AT.window, UP], [OPEN.off, LOOK]];
    const neck = [HX + FACE * .3 * Z, F - RIG.Neck * Z], headTo = p => clamp(headTurn((p[0] - neck[0]) * FACE, p[1] - neck[1]), -HEAD.Up, HEAD.Down);
    const HEADS = [[0, 0], [AT.lamp, headTo(FLOOR_AIM)], [AT.scan, headTo(READ_AIM)], [AT.thought, 0], [AT.window, headTo([WIN_R.x + WIN_R.w / 2, WIN_R.y + WIN_R.h / 2])], [OPEN.off, 0]];
    function eased(list, u, tau, by) {
      let i = 0;
      while (i + 1 < list.length && u >= list[i + 1][0]) i++;
      return i ? by(list[i - 1][1], list[i][1], 1 - Math.exp(-(u - list[i][0]) / tau)) : list[0][1];
    }
    function stateOf(time) {
      const round = Math.floor(time / LOOP), u = time - round * LOOP;
      let si = 0;
      while (si + 1 < STAGES.length && u >= STAGES[si + 1].t0) si++;
      const set = setNow(), n = set.list.length, index = OPT.colour < 0 ? round % n : OPT.colour % n;
      const lv = u - AT.thought, loved = sm(lv / .5) * (1 - sm((lv - HELD) / .5));
      const wu = u - OPEN.on, wc = u - OPEN.off, flown = sm(wu / .4) * (1 - sm(wc / .4));
      const D = lerp2(lerp2(PERCH, PRESSED, loved), WAIST, flown);
      return {
        time, u, stage: STAGES[si], si, set, index, colour: set.list[index],
        D: [D[0], D[1] + Z * Math.sin(TAU * time / 1.8) + 1.5 * Math.sin(lv * 3) * loved],
        A: eased(AIMS, u, .09, angLerp), head: eased(HEADS, u, .12, lerp),
        sh: shapeAt(u), open: openOf(u), away: Array.from({ length: NDOT }, (_, j) => awayOf(u, j)),
        tilt: -.2 * loved, glow: .2 * loved, op: time - .3 * (round * 4 + clamp(lv, 0, 4)),
        flurry: wu < 0 ? 0 : wc < 0 ? c01(wu / .15) : 1 - c01(wc / .12),
      };
    }
    const atRest = t => ({ time: t, u: 0, A: LOOK, sh: { cur: 'idle', prev: 'idle', t0: 0, e: 9 }, open: 0, away: [], tilt: 0, glow: 0, op: t, flurry: 0 });

    // ---------- the swarm, DroneSwarm's ----------
    const RANK = share => { let m = 0; const r = Array.from({ length: N }, (_, i) => hsh(i * 3 + 1) < share ? m++ : -1); return { r, m }; };
    const LAMP_R = RANK(.8), LOVE_R = RANK(.7);
    const SCAN_R = (() => {
      const raw = [NDOT, .4 * N, N - NDOT - .4 * N].map(Math.round), part = [];
      const order = Array.from({ length: N }, (_, i) => i).sort((a, b) => hsh(a * 3 + 1) - hsh(b * 3 + 1));
      let at = 0;
      raw.forEach((n, p) => { for (let r = 0; r < n && at < N; r++) part[order[at++]] = [p, r]; });
      return { part, count: raw };
    })();
    const takes = (id, i) => id === 'idle' ? false : id === 'scan' ? true : (id === 'lamp' ? LAMP_R : LOVE_R).r[i] >= 0;
    function orbit(i, op, tilt) {
      const P = DR.P[i], o = i % 3, way = o === 1 ? -1 : 1, th = P.ph + op * (ORB.pace + ORB.paceMore * o) * way * ORB.hurry;
      const rx = (ORB.r + o * ORB.more + (P.r1 - .5)) * ORB.wide, tl = ORB.tilt + o * ORB.tiltMore + tilt;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * ORB.flat, tl), v = rot(-Math.sin(th) * way, Math.cos(th) * ORB.flat * way, tl), d = Math.hypot(v[0], v[1]) || 1;
      return [q[0], q[1], v[0] / d, v[1] / d, Math.sin(th)];
    }
    function place(id, i, X) {
      let q;
      if (id === 'scan') {
        const [part, r] = SCAN_R.part[i];
        q = part === 0 ? MARK[r] : part === 1 ? onPath(X.ring, (r + .5) / SCAN_R.count[1] + X.t * 12 / RING_L) : onPath(X.ticks, (r + .5) / SCAN_R.count[2] + X.t * 8 / TICK_L);
      } else if (id === 'lamp') q = onPath(LAMP_P, (LAMP_R.r[i] + .5) / LAMP_R.m + X.t * 30 / LAMP_P.L);
      else return onPath(LOVE, (LOVE_R.r[i] + .5) / LOVE_R.m + X.t * 18 / LOVE.L);
      const a = rot(q[0], q[1], X.A), v = rot(q[2], q[3], X.A);
      return [a[0], a[1], v[0], v[1]];
    }
    const idleHeat = (i, t) => hsh(i + 3) < .5 ? .55 + .3 * Math.sin(t * 11 + i * 7) : .2;
    const heatOn = (id, i, t) => id === 'love' ? .42 + .18 * Math.sin(t * 4 + i * 1.7) : .7 + .25 * Math.sin(t * 13 + i) + (id === 'lamp' ? .12 : 0);
    const flurryHash = (i, n) => frac(Math.sin(i * 127.1 + n * 311.7 + 74.7) * 43758.5453);
    function flurried(i, t, shown) {
      const spot = n => { const a = TAU * flurryHash(i, n + .3), r = .35 + .65 * Math.sqrt(flurryHash(i, n + .6)); return [Math.cos(a) * r * 10, Math.sin(a) * r * 7.5]; };
      const ph = t / (.18 + .32 * flurryHash(i, .5)) + flurryHash(i, .25), n = Math.floor(ph), s = ph - n, a = spot(n), b = spot(n + 1);
      const p = lerp2(a, b, sm(s / .45)), d = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1, rise = sm(shown * 1.4 - .4 * i / 13);
      return { p: [p[0] * rise, p[1] * rise, (b[0] - a[0]) / d, (b[1] - a[1]) / d], heat: (.35 + .65 * flurryHash(i, n + .75)) * rise * (s < .45 ? 1 : .8), shaped: true, front: true, q: 0 };
    }
    const outOf = d => sm((d - 2.8) / 4.6);   // how far out of the coal, from the pick px to it
    function swarmOf(st, seed = 0, fleck = .34) {
      const { cur, prev, e, t0 } = st.sh, X = { t: st.time + seed, A: st.A };
      if (cur === 'scan' || prev === 'scan') { X.ring = ringAt(st.open); X.ticks = ticksAt(st.open); }
      const left = { ...X, t: X.t - (st.u - t0) }, list = [];
      for (let i = 0; i < N; i++) {
        const to = takes(cur, i), was = takes(prev, i);
        if (cur === 'scan' || prev === 'scan') { const [part, j] = SCAN_R.part[i]; if (part === 0 && st.away[j] > 0) continue; }   // out as the gauge's dot
        const O = orbit(i, st.op + seed, st.tilt);
        const k = !to && !was ? 1 : to ? sm((e - hsh(i + 50) * .45) / .55) : sm((e - hsh(i + 50) * .4) / .6);
        let p = O, heat = idleHeat(i, X.t), from = null, shaped = false;
        if (to || k < 1) {
          const T0 = k < 1 ? was ? place(prev, i, prev === 'love' ? left : X) : O : null, T1 = to ? place(cur, i, X) : O;
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
        }
        // born of the coal: the colour by how far out, and on a shape by how long ago
        const d = outOf(Math.hypot(p[0], p[1])), q = shaped ? Math.min(d, sm(frac(X.t * .7 + hsh(i + 9)) / fleck)) : d;
        list.push({ p, heat, shaped, from, q, front: shaped || p[4] === undefined || p[4] >= 0 });
      }
      if (st.flurry > 0) for (let i = 0; i < 14; i++) list.push(flurried(i, X.t, st.flurry));
      return { list, X };
    }

    // ---------- marks: what is laid, then what is added ----------
    function strokeLines(c, at, polys, w, col, a) {
      if (a <= .003 || !polys.length) return;
      c.beginPath();
      for (const p of polys) p.forEach((q, j) => { const r = at(q); if (j) c.lineTo(r[0], r[1]); else c.moveTo(r[0], r[1]); });
      c.lineWidth = w; c.strokeStyle = css(col, a); c.stroke();
    }
    function line2(c, a, b, w, col, al) { if (al <= .003) return; c.beginPath(); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]); c.lineWidth = w; c.strokeStyle = css(col, al); c.stroke(); }
    function square(c, x, y, side, col, a) { if (a <= .003) return; c.fillStyle = css(col, a); c.fillRect(x - side / 2, y - side / 2, side, side); }
    function laidTail(c, K, x, y, dx, dy, side, step, heat, q) {
      const L = K.laid;
      if (!L) return;
      const col = K.under(q), h = K.fill ? Math.min(1, .3 + heat) : Math.min(1, heat * 2.2);
      for (let m = K.fill && !K.far(q) ? 0 : 2; m >= 0; m--) square(c, x - dx * m * step, y - dy * m * step, side * L.side, col, L.tails[m] * h);
    }
    function glowTail(c, K, x, y, dx, dy, side, step, heat, q) {
      const h = Math.min(1, heat);
      if (h <= .02) return;
      if (K.fill) { square(c, x, y, side * .6, K.col('spark', .8, q), .6 * h); return; }
      for (let m = 0; m < 3; m++) {
        const a = (K.cand.tails ? K.cand.tails[m] : .95 - m * .3) * h * K.gain;
        square(c, x - dx * m * step, y - dy * m * step, side, K.col('spark', .3 * (1 - m / 3), q), a);
      }
    }
    // the threads cut where their colour changes
    function threadsOf(K, tilt) {
      const levels = new Map(), steps = K.cand.round1 ? 6 : 1;
      for (let o = 0; o < 3; o++) {
        const rx = (ORB.r + o * ORB.more) * ORB.wide, tl = ORB.tilt + o * ORB.tiltMore + tilt;
        let last = null, lvWas = -1, run = null;
        for (let i = 0; i <= 36; i++) {
          const th = i / 36 * TAU, p = rot(Math.cos(th) * rx, Math.sin(th) * rx * ORB.flat, tl);
          if (last) {
            const q = outOf(Math.hypot((p[0] + last[0]) / 2, (p[1] + last[1]) / 2)), lv = steps > 1 ? Math.round(q * steps) : K.far(q) ? 1 : 0;
            if (lv !== lvWas) { lvWas = lv; run = [last]; if (!levels.has(lv)) levels.set(lv, []); levels.get(lv).push(run); }
            run.push(p);
          }
          last = p;
        }
      }
      return [...levels].map(([lv, polys]) => ({ q: lv / steps, polys }));
    }
    function drawSalts(c, K, st, D, k, swarm) {
      const { list, X } = swarm, { cur, prev, e } = st.sh, A = st.A, cand = K.cand, L = K.laid, gain = K.gain;
      const at = q => [D[0] + q[0] * k, D[1] + q[1] * k], turned = q => at(rot(q[0], q[1], A));
      const shapes = [];
      for (const [id, form] of [[cur, sm(e)], [prev, 1 - sm(e)]]) {
        if (id === 'idle' || form <= .003) continue;
        if (id === 'lamp') shapes.push({ polys: LAMP_P.polys, at: turned, a: form });
        else if (id === 'scan') {
          shapes.push({ polys: [...X.ring.polys, ...X.ticks.polys], at: turned, a: form });
          shapes.push({ polys: MARK_LINES.filter((m, j) => st.away[j] < .5), at: turned, a: form });
        } else shapes.push({ polys: LOVE.polys, at, a: form * .7 });
      }
      const threads = threadsOf(K, st.tilt);

      c.save();
      c.lineCap = 'round'; c.lineJoin = 'round'; c.globalCompositeOperation = 'source-over';
      if (K.smoke) {
        const S = K.smoke, r = S.r * k, g = c.createRadialGradient(D[0], D[1], 0, D[0], D[1], r);
        g.addColorStop(0, css([16, 17, 21], S.a)); g.addColorStop(.55, css([16, 17, 21], S.a * .72)); g.addColorStop(1, css([16, 17, 21], 0));
        c.fillStyle = g; c.beginPath(); c.arc(D[0], D[1], r, 0, TAU); c.fill();
        for (const s of shapes) for (const w of [6, 4, 2.4]) strokeLines(c, s.at, s.polys, w * k, [16, 17, 21], S.line * s.a);
      }
      if (L) {
        for (const th of threads) strokeLines(c, at, th.polys, L.w * k, K.under(th.q), L.a);
        for (const s of shapes) strokeLines(c, s.at, s.polys, L.shape * k, K.under(1), Math.min(1, L.a * s.a / (s.polys === LOVE.polys ? .7 : 1)));
      }
      const side = SIDE * k, step = 1.3 * k;
      const darks = front => {
        for (const s of list) {
          if (s.front !== front) continue;
          const x = D[0] + s.p[0] * k, y = D[1] + s.p[1] * k;
          if (!K.fill) square(c, x, y, side, SOOT, 1);
          laidTail(c, K, x, y, s.p[2], s.p[3], side, step, s.heat, s.q);
        }
      };
      darks(false);
      const R = 5.04 * k;
      if (ready(IMG.core)) c.drawImage(IMG.core, D[0] - R, D[1] - R, 2 * R, 2 * R);
      else { c.beginPath(); c.arc(D[0], D[1], .75 * R, 0, TAU); c.fillStyle = css(SOOT); c.fill(); }
      darks(true);

      c.globalCompositeOperation = 'lighter';
      if (cand.aura) halo(c, D[0], D[1], 17 * k, K.col('line'), cand.aura * gain);
      if (!K.fill) {
        for (const th of threads) strokeLines(c, at, th.polys, ORB.line * K.times('orbW') * k, K.col('thread', 0, th.q), Math.min(1, ORB.a * K.times('orbA') * gain));
        for (const s of shapes) {
          const col = K.col('line'), la = s.a * K.times('lineA') * gain;
          if (!L) strokeLines(c, s.at, s.polys, 1.6 * k, col, .06 * la);   // a wide glow would whiten what is laid
          strokeLines(c, s.at, s.polys, .45 * k, col, .3 * la);
        }
      }
      // on the lamp and the reticle the sparks crowd, and added light would pile up into white
      const short = id => id === 'lamp' || id === 'scan' ? 1 : 0, crowd = cand.round1 || K.fill ? 1 : 1 - .58 * (short(cur) * sm(e) + short(prev) * (1 - sm(e)));
      let shaped = 0;
      for (const s of list) {
        const x = D[0] + s.p[0] * k, y = D[1] + s.p[1] * k;
        if (s.shaped) shaped++;
        if (s.from) line2(c, at(s.from), [x, y], .35 * k, K.col('thread', 0, s.q), .07);
        glowTail(c, K, x, y, s.p[2], s.p[3], side, step, s.shaped ? s.heat * crowd : s.heat, s.q);
      }
      const fl = .75 + .25 * Math.sin(st.time * 7.3), core = K.col('core'), hot = cand.round1 ? gain : 1;
      halo(c, D[0], D[1], 8 * k * K.times('coreR'), core, Math.min(.8, (.35 + .25 * st.glow) * K.times('coreA') * hot));
      c.beginPath();
      for (const [a, b] of CRACKS) { c.moveTo(D[0] + Math.cos(A + a) * .6 * k, D[1] + Math.sin(A + a) * .6 * k); c.lineTo(D[0] + Math.cos(A + b * .5) * 2.6 * k, D[1] + Math.sin(A + b * .5) * 2.6 * k); }
      c.lineWidth = .45 * k * K.times('crackW'); c.strokeStyle = css(core, Math.min(1, .5 * fl * K.times('crackA') * hot)); c.stroke();
      const E = at([Math.cos(A) * 1.386, Math.sin(A) * 1.386]), er = K.times('eyeR');
      c.fillStyle = css(K.col('eye', .5), .9 * fl); c.beginPath(); c.arc(E[0], E[1], 1.2 * k * er, 0, TAU); c.fill();
      halo(c, E[0], E[1], 4 * k * er, K.col('eye'), .5 * fl * K.times('eyeA') * hot);
      c.restore();
      return shaped;
    }

    // ---------- the scanner's gauge, the window's corners and the edge keys, as shipped ----------
    function gauge(c, K, st) {
      const t = st.time, LIT = 16.5, out = [];
      for (let j = 0; j < NDOT; j++) {
        const away = st.away[j];
        if (away <= 0) continue;
        const seat = rot(MARK[j][0], MARK[j][1], st.A), S = [st.D[0] + seat[0] * K1, st.D[1] + seat[1] * K1];
        const H = [GAUGE[0] + (j - (NDOT - 1) / 2) * DOT.step, GAUGE[1]], p = lerp2(S, H, sm(away)).map(Math.round);
        const lit = c01(LIT - j), fl = .82 + .18 * Math.sin(t * 9 + j * 2.3), spark = .7 + .25 * Math.sin(t * 13 + j);
        const way = away >= 1 ? 0 : st.u < READ.off ? 1 : -1, d = Math.hypot(H[0] - S[0], H[1] - S[1]) || 1, side = Math.max(1, Math.round(lerp(SIDE * K1, DOT.side, away)));
        out.push({ j, S, p, lit, way, side, heat: lerp(spark, lit > 0 ? (.3 + .62 * lit) * fl : .04, away), x: p[0] + side / 2 - Math.floor(side / 2), y: p[1] + side / 2 - Math.floor(side / 2), dx: way * (H[0] - S[0]) / d, dy: way * (H[1] - S[1]) / d, q: Math.min(away, sm(frac(t * .5 + hsh(j + 21)) / .2)) });
      }
      const step = 1.3 * K1;
      c.save();
      for (const d of out) {
        if (K.smoke) square(c, d.x, d.y, d.side + 4, [16, 17, 21], .3);
        square(c, d.x, d.y, d.side, SOOT, 1);
        if (K.laid && !(K.fill && !d.way && d.lit <= 0)) laidTail(c, K, d.x, d.y, d.dx, d.dy, d.way ? d.side : d.side / K.laid.side, d.way ? step : 0, d.lit > 0 || d.way ? d.heat : .1, d.q);
      }
      c.globalCompositeOperation = 'lighter';
      let lit = 0, dim = 0;
      for (const d of out) {
        if (d.way && d.j % 2 === 0) line2(c, d.S, d.p, .35 * K1, K.col('line'), .07);
        glowTail(c, K, d.x, d.y, d.dx, d.dy, d.side, step, d.heat, d.q);
        if (d.lit > 0) lit++; else dim++;
      }
      c.restore();
      return { lit, dim };
    }
    function corners(c, K, st) {
      const u = st.u - OPEN.on, uc = st.u - OPEN.off, r = WIN_R;
      if (u < 0 || uc >= .12) return 0;
      const E = [st.D[0] + Math.cos(st.A) * 1.386 * K1, st.D[1] + Math.sin(st.A) * 1.386 * K1], shots = [], set = [], runs = [];
      for (let k = 0; k < 4; k++) {
        const P = [[r.x - 5, r.y - 5], [r.x + r.w + 5, r.y - 5], [r.x + r.w + 5, r.y + r.h + 5], [r.x - 5, r.y + r.h + 5]][k];
        const f = uc < 0 ? c01((u - .01 * k) / .1) : 1 - c01(uc / .12);
        if (f <= 0) continue;
        if (f < 1) { const way = uc < 0 ? 1 : -1, d = Math.hypot(P[0] - E[0], P[1] - E[1]) || 1; shots.push([lerp2(E, P, sm(f)), way * (P[0] - E[0]) / d, way * (P[1] - E[1]) / d]); continue; }
        const sx = k === 0 || k === 3 ? 1 : -1, sy = k < 2 ? 1 : -1, ends = [[P[0] + sx * 22, P[1]], [P[0], P[1] + sy * 22]];
        set.push({ P, ends, landed: c01((u - .1 - .01 * k) / .15) });
        for (const end of ends) for (let m = 0; m < 3; m++) { const q = frac(st.time * 1.3 + m / 3 + k * .17); runs.push({ p: lerp2(P, end, q), dx: (end[0] - P[0]) / 22, dy: (end[1] - P[1]) / 22, q }); }
      }
      const same = q => q, L = K.laid;
      const out = set.flatMap(s => s.ends.map(end => [lerp2(s.P, end, .25), end])), born = set.map(s => [lerp2(s.P, s.ends[0], .25), s.P, lerp2(s.P, s.ends[1], .25)]);
      c.save();
      c.lineCap = 'round'; c.lineJoin = 'round';
      if (K.smoke) for (const w of [9, 6, 3.5]) strokeLines(c, same, set.map(s => [s.ends[0], s.P, s.ends[1]]), w, [16, 17, 21], K.smoke.line);
      if (L) { strokeLines(c, same, out, L.frame, K.under(1), L.a); strokeLines(c, same, born, L.frame, K.under(0), L.a); }
      for (const s of runs) { if (!K.fill) square(c, s.p[0], s.p[1], SIDE, SOOT, 1); laidTail(c, K, s.p[0], s.p[1], s.dx, s.dy, 1.3, 1.3, .9 * (1 - s.q * .7), s.q + .15); }
      if (L) for (const s of set) square(c, s.P[0], s.P[1], 3.2, K.under(0), L.a);
      c.globalCompositeOperation = 'lighter';
      if (!K.fill) {
        const a = K.times('lineA') * K.gain, col = K.col('line'), whole = set.map(s => [s.ends[0], s.P, s.ends[1]]);
        if (K.cand.round1) { strokeLines(c, same, whole, 2.2, col, .07 * a); strokeLines(c, same, whole, .6, col, .45 * a); }
        else {
          if (!L) strokeLines(c, same, whole, 2.2, col, .07 * a);
          strokeLines(c, same, out, 1, col, .55 * a); strokeLines(c, same, born, 1, K.coal, .55 * a);
        }
      }
      for (const s of runs) glowTail(c, K, s.p[0], s.p[1], s.dx, s.dy, SIDE, 1.3, .9 * (1 - s.q * .7), K.cand.round1 ? s.q : s.q + .15);
      for (const s of set) {
        square(c, s.P[0], s.P[1], 2.4, K.col('spark', .225, 0), .855 * (K.cand.round1 ? K.gain : 1));
        if (!K.fill) halo(c, s.P[0], s.P[1], 12, K.col('line'), .6 * (1 - s.landed));
      }
      for (const [p, dx, dy] of shots) glowTail(c, K, p[0], p[1], dx, dy, 1.6, 1.3, 1, 0);
      c.restore();
      return set.length;
    }
    function icon(c, K, I, x, y, r, u, look) {
      const P = look.rim ? I.parts : I.one, t = look.t, sd = look.sd || 0, L = K.laid;
      const lv = look.lv * (look.wait ? .7 + .3 * Math.sin(t * TAU * 1.2) : 1), a = Math.min(1, .3 + .45 * lv), g = K.times('lineA') * K.gain;
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
      if (K.fill) { stroke(1.5 * u, K.lit, Math.min(1, .7 + .3 * lv), false); stroke(.6 * u, K.lit, .3, true); }
      else if (L) { stroke(2 * u, K.under(1), L.a * Math.min(1, .5 + lv), false); stroke(u, K.under(1), .3, true); }
      c.globalCompositeOperation = 'lighter';
      if (!K.fill) {
        if (!L) stroke(3 * u, K.col('line'), .06 * lv * g);
        stroke(u, K.col('line', .12), a * g, false);
        stroke(.6 * u, K.col('line', .12), a * .35 * g, true);
      }
      let s = null;
      if (look.rim) { if (look.hov) s = frac(t * .6 + sd); }
      else if (look.hov) { const q = frac((t + sd) / 1.2) * 1.2; s = q < .9 ? q / .9 : null; }
      else if (look.on || look.wait) s = frac((t + sd * 3) / 2.4);
      if (s !== null) {
        const h = Math.min(1, .5 + .5 * lv) * (look.rim ? .6 : sm(Math.min(s / .12, (1 - s) / .18))), len = look.rim ? .12 : .22, px = q => [x + q[0] * r, y + q[1] * r];
        if (K.fill) c.globalCompositeOperation = 'source-over';
        for (let i = 0; i < 10; i++) {
          const s0 = s - len * (1 - i / 10), s1 = Math.min(.9999, s - len * (1 - (i + 1) / 10));
          if (s0 < 0 || s1 <= s0) continue;
          line2(c, px(onPath(P, s0)), px(onPath(P, s1)), 1.3 * u, K.col('run', .3 + .04 * i, i / 10, s), h * (i + 1) / 10);
        }
        c.globalCompositeOperation = 'lighter';
        if (h > .004) {
          const p = px(onPath(P, Math.min(.9999, s)));
          if (!look.rim && !K.fill) halo(c, p[0], p[1], 3.2 * u, K.col('run', .4, 1, s), .45 * h);
          square(c, p[0], p[1], 1.3 * u, K.col('run', .35, 1, s), h);
        }
      }
      c.restore();
    }
    const KEYS = [{ id: 'air', on: true }, { id: 'build', hov: true }, { id: 'research', wait: true, num: 3 }];
    function edgeKeys(c, K, st) {
      const vis = sm((st.u - OPEN.on) / .15) * (1 - sm((st.u - OPEN.off) / .15));
      if (vis <= .01) return 0;
      KEYS.forEach((key, i) => {
        const x = KEY_AT[0] + i * 40, y = KEY_AT[1], b = (key.hov ? 1.1 : .85) * vis, sd = i * 2.3 + 1;
        rr(c, x + .5, y + .5, 27, 27, 3); c.fillStyle = css(K.col('glass').map(v => v * (key.on ? .3 : .1)), key.on ? .7 * vis : Math.min(.75, .25 + .5 * b)); c.fill();
        icon(c, K, RIM_KEY, x + 14, y + 14, 13.5, .7, { rim: true, lv: b * .5, hov: key.hov, t: st.time, sd });
        icon(c, K, ICON[key.id], x + 14, y + 14, 9, 1, { lv: b, hov: key.hov, on: key.on, wait: key.wait, t: st.time, sd });
        if (!key.num) return;
        const base = y + 33;
        c.fillStyle = 'rgba(4,6,9,.85)'; c.fillRect(x + 6.5, base - 12, 15, 14);
        c.strokeStyle = css(K.col('line'), .55 * b); c.lineWidth = 1; c.strokeRect(x + 7, base - 11.5, 14, 13);
        c.font = '600 13px ui-monospace,Consolas,monospace'; c.textAlign = 'center'; c.textBaseline = 'alphabetic';
        c.fillStyle = css(K.col('line', .45), Math.min(1, b)); c.fillText(String(key.num), x + 14, base); c.textAlign = 'left';
      });
      return KEYS.length;
    }
    // the window's inside: the game's cool screen, never the player's colour
    const ITEMS = { 0: 'scrap', 1: 'ore', 3: 'copper', 4: 'battery' };
    function inside(c, st) {
      const u = st.u - OPEN.on, uc = st.u - OPEN.off, a = u < 0 ? 0 : uc < 0 ? c01((u - .03) / .1) : 1 - c01(uc / .08);
      if (a <= .003) return;
      const r = WIN_R, L = TONES.bag.light;
      c.save(); c.globalAlpha = a;
      c.fillStyle = css(TONES.bag.ground, .93); c.fillRect(r.x, r.y, r.w, r.h);
      c.font = '600 11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.fillStyle = css(pale(L, .5), .92); c.fillText('СУМКА', r.x + 8, r.y + 14);
      for (let i = 0; i < 8; i++) {
        const x = r.x + 6 + (i % 4) * 43, y = r.y + 19 + (i / 4 | 0) * 31;
        rr(c, x + .5, y + .5, 39, 28, 2); c.fillStyle = css([20, 32, 50], .78); c.fill();
        c.strokeStyle = css(L, .36); c.lineWidth = 1; c.stroke();
        if (ITEMS[i] && ready(IMG[ITEMS[i]])) c.drawImage(IMG[ITEMS[i]], x + 7, y + 2, 25, 25);
      }
      c.restore();
    }

    // ---------- the three grounds ----------
    function ground(g, id, w, h, floor, decked) {
      if (id === 'space') {
        const gr = g.createLinearGradient(0, 0, 0, floor); gr.addColorStop(0, '#04060a'); gr.addColorStop(1, '#0a0f17');
        g.fillStyle = gr; g.fillRect(0, 0, w, floor);
        for (const [x, y, s, a] of STARS) { if (x >= w || y >= floor) continue; g.fillStyle = css([220, 228, 240], a); g.fillRect(x, y, s, s); }
        g.fillStyle = '#2f3138'; g.fillRect(0, floor, w, h - floor);
        g.fillStyle = '#4f525b'; g.fillRect(0, floor, w, 3);
      } else if (id === 'room') {
        g.fillStyle = '#1b2327'; g.fillRect(0, 0, w, floor);
        if (ready(IMG.wall)) {
          const laid = () => { for (let x = 0; x < w; x += 512) for (let y = floor - 256; y > -256; y -= 256) g.drawImage(IMG.wall, x, y, 512, 256); };
          g.save(); g.beginPath(); g.rect(0, 0, w, floor); g.clip();
          laid();
          g.globalCompositeOperation = 'lighter'; g.globalAlpha = .65; laid();   // lit
          g.restore();
        }
        g.save(); g.globalCompositeOperation = 'lighter';
        const gr = g.createRadialGradient(w / 2, 8, 8, w / 2, 8, floor + 40);
        gr.addColorStop(0, css([214, 172, 116], .3)); gr.addColorStop(1, css([214, 172, 116], 0));
        g.fillStyle = gr; g.fillRect(0, 0, w, floor);
        g.restore();
        g.fillStyle = '#25282d'; g.fillRect(0, floor, w, h - floor);
        g.fillStyle = '#6a675f'; g.fillRect(0, floor, w, 3);
      } else {
        // the sky picture's colours (planet/sky.png) by the height over the ground, so every drone has the same sky behind it
        const top = floor - 352, sky = g.createLinearGradient(0, top, 0, floor);
        [[0, [94, 112, 128]], [.25, [121, 139, 151]], [.5, [160, 171, 169]], [.75, [209, 203, 181]], [1, [221, 214, 190]]].forEach(([q, col]) => sky.addColorStop(q, css(col)));
        g.fillStyle = sky; g.fillRect(0, 0, w, floor);
        if (decked && ready(IMG.clouds)) { g.save(); g.globalAlpha = .75; g.drawImage(IMG.clouds, -300, 2, 1024, 80); g.restore(); }
        for (const [up, amp, col, f] of [[94, 14, '#8f9c8c', .012], [62, 12, '#66796a', .019], [26, 8, '#3d5246', .031]]) {
          g.beginPath(); g.moveTo(0, floor);
          for (let x = 0; x <= w + 8; x += 8) g.lineTo(x, floor - up + amp * Math.sin(x * f + up) + 2 * Math.sin(x * f * 7.3));
          g.lineTo(w + 8, floor); g.closePath(); g.fillStyle = col; g.fill();
        }
        if (decked && ready(IMG.tree)) g.drawImage(IMG.tree, -104, floor - 228, 200, 228);
        g.fillStyle = '#2b2925'; g.fillRect(0, floor, w, h - floor);
        g.fillStyle = '#3f5b43'; g.fillRect(0, floor, w, 4);
      }
    }
    const PLATE = mk(polyPts([[-84, 78], [-76, 34], [-50, 30], [-40, 4], [8, 0], [30, 18], [64, 22], [70, 46], [84, 60], [86, 78]]), 4242, .8);
    function chunk(c, st) {
      const a = sm((st.u - AT.scan) / .3) * (1 - sm((st.u - AT.thought + .2) / .3));
      if (a <= .01) return;
      c.save(); c.globalAlpha = a; c.translate(CHUNK.x, CHUNK.top); c.scale(CHUNK.k, CHUNK.k); c.lineCap = 'round';
      trace(c, PLATE); c.fillStyle = '#2b2e34'; c.fill();
      c.save(); c.clip(); c.fillStyle = '#3f434a'; c.fillRect(-90, 0, 180, 30); c.fillStyle = '#1f2226'; c.fillRect(-90, 51, 180, 3); c.fillRect(-16, 0, 3, 80); c.restore();
      ink(c, PLATE, 0, 3);
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
    function standing(x, y, seed) { const h = newHero('me', x, seed); h.y = y; h.face = FACE; h.P = poseOf(h); return h; }
    const picsReady = () => Object.values(IMG).filter(ready).length;
    function layer(T, name, w, h) {
      const cw = Math.ceil(w * T.S), ch = Math.ceil(h * T.S);
      if (!T[name] || T[name].width !== cw || T[name].height !== ch) { T[name] = document.createElement('canvas'); T[name].width = cw; T[name].height = ch; }
      const g = T[name].getContext('2d');
      g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over';
      return g;
    }

    // ---------- under the grounds: the whole set at once ----------
    const ROW = { y: PANE.y + PANE.h, x: 4, w: LW - 8, floor: 230, tick: 15 };
    ROW.h = LH - ROW.y;
    const cellAt = (x, n) => clamp(Math.floor((x - ROW.x) / (ROW.w / n)), 0, n - 1);
    function row(T, st) {
      const list = st.set.list, n = list.length, w1 = ROW.w / n;
      const key = `${OPT.bg}|${OPT.palette}|${T.S}|${picsReady()}`;
      if (T.rowKey !== key) {
        T.rowKey = key; T.tickKey = null;
        const g = layer(T, 'rowBack', LW, ROW.h), ink2 = OPT.bg === 'planet' ? '#14171a' : '#d6dbe2';
        ground(g, OPT.bg, LW, ROW.h, ROW.floor, false);
        list.forEach((col, i) => { const h = standing(ROW.x + (i + .5) * w1 - 1, ROW.floor, 1.3 + i); drawHero(g, h); bracelet(g, h, col.rgb); });
        g.textBaseline = 'alphabetic'; g.font = '11px system-ui,"Segoe UI",sans-serif';
        g.textAlign = 'left'; g.fillStyle = ink2; g.fillText(st.set.name, 6, 12);
        g.textAlign = 'center'; g.fillStyle = '#d6dbe2';
        list.forEach((col, i) => g.fillText(col.name, clamp(ROW.x + (i + .5) * w1, 26, LW - 30), ROW.floor + 14 + (i % 2) * 13));
        g.textAlign = 'left';
      }
      const tick = `${Math.floor(st.time * ROW.tick + T.num * .2)}|${st.index}`;
      if (T.tickKey !== tick) {
        T.tickKey = tick;
        const g = layer(T, 'rowLive', LW, ROW.h), rest = atRest(st.time);
        g.drawImage(T.rowBack, 0, 0, LW, ROW.h);
        rr(g, ROW.x + st.index * w1 + 1, 16, w1 - 2, ROW.h - 18, 5); g.strokeStyle = OPT.bg === 'planet' ? 'rgba(20,23,26,.6)' : 'rgba(214,219,226,.6)'; g.lineWidth = 1; g.stroke();
        list.forEach((col, i) => drawSalts(g, inkOf(T.cand, col.rgb), rest, [ROW.x + (i + .5) * w1 + 14, ROW.floor - 198], K1, swarmOf(rest, i * 1.3)));
      }
      T.c.drawImage(T.rowLive, 0, ROW.y, LW, ROW.h);
      return n;
    }

    // ---------- a tile: one moment on the three grounds ----------
    function paneBacks(T) {
      const key = `${T.S}|${picsReady()}`;
      if (T.paneKey === key) return;
      T.paneKey = key;
      const g = layer(T, 'paneBack', LW, PANE.h);
      GROUNDS.forEach(([id, name], i) => {
        g.save(); g.translate(i * PANE.w, 0); g.beginPath(); g.rect(0, 0, PANE.w, PANE.h); g.clip();
        ground(g, id, PANE.w, PANE.h, PANE.floor, true);
        g.fillStyle = 'rgba(0,0,0,.7)'; g.fillRect(0, 0, i ? 2 : 0, PANE.h);
        g.font = '11px system-ui,"Segoe UI",sans-serif'; g.textAlign = 'left'; g.textBaseline = 'alphabetic'; g.fillStyle = '#d6dbe2';
        g.fillText(name, 8, PANE.floor + 15);
        g.restore();
      });
    }
    function draw(T, st) {
      const c = T.c, K = inkOf(T.cand, st.colour.rgb), swarm = swarmOf(st, 0, T.cand.round1 ? .5 : .34), dm = T.dm = { stage: st.stage.id, index: st.index, colour: st.colour, panes: {} };
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      paneBacks(T);
      c.drawImage(T.paneBack, 0, PANE.y, LW, PANE.h);
      const { cur, prev, e } = st.sh, lamp = cur === 'lamp' ? sm(e) : prev === 'lamp' ? 1 - sm(e) : 0;
      const h = T.me; h.x = HX; h.y = F; h.face = FACE; h.look = st.head; h.P = poseOf(h);
      GROUNDS.forEach(([id], i) => {
        const got = dm.panes[id] = {};
        c.save(); c.translate(i * PANE.w, PANE.y); c.beginPath(); c.rect(0, 0, PANE.w, PANE.h); c.clip();
        lampOn(c, st.D[0], st.D[1], HX, F, FACE, .6 * lamp, 2.6);
        chunk(c, st);
        drawHero(c, h);
        T.probe = [id, 'bracelet'];
        bracelet(c, h, st.colour.rgb);
        T.probe = [id, 'drone'];
        got.shaped = drawSalts(c, K, st, st.D, K1, swarm);
        Object.assign(got, gauge(c, K, st));
        T.probe = [id, 'inside'];
        inside(c, st);
        T.probe = [id, 'ui'];
        got.corners = corners(c, K, st);
        got.keys = edgeKeys(c, K, st);
        T.probe = null;
        c.restore();
      });
      dm.row = row(T, st);
      const font = 'system-ui,"Segoe UI",sans-serif';
      c.fillStyle = '#0b0e13'; c.fillRect(0, 0, LW, PANE.y);
      c.textBaseline = 'alphabetic'; c.textAlign = 'left';
      c.font = `600 12px ${font}`; c.fillStyle = '#e3e0d8'; c.fillText(`${st.si + 1}/${STAGES.length} · ${st.stage.say}`, 8, 16);
      c.fillStyle = st.colour.hex; c.fillRect(232, 7, 10, 10);
      c.font = `12px ${font}`; c.fillStyle = 'rgba(206,212,222,.85)';
      c.fillText(`${st.colour.name}, ${st.index + 1} из ${st.set.list.length}${OPT.colour < 0 ? ' · по кругу' : ''}${OPT.slow ? ' · ×¼' : ''}`, 248, 16);
    }

    let shownSet = '';
    function syncButtons() {
      const set = setNow(), n = set.list.length;
      if (OPT.colour >= n) { OPT.colour %= n; REFRESH.forEach(f => f()); }
      if (shownSet === OPT.palette) return;
      shownSet = OPT.palette;
      [...document.getElementById('colourMode').children].filter(b => String(b.tagName || b.tag).toLowerCase() === 'button').slice(1).forEach((b, i) => {
        b.style.display = i < n ? '' : 'none';
        if (i < n) { b.textContent = set.list[i].name; b.style.borderLeft = `10px solid ${set.list[i].hex}`; }
      });
    }

    // a frame opened straight from the address: #at=6.4&colour=2&bg=room&palette=ten&pause
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
      expose: {
        STAGES, LOOP, SETS, GROUNDS: GROUNDS.map(g => g[0]), EMBER, strong, K1,
        seek: t => { clock = t; for (const T of tiles) T.time = t; },
        // the drone at rest on a canvas, for the check to measure it as drawn
        sample(g, cand, C, D, t) { const st = atRest(t); drawSalts(g, inkOf(cand, C), st, D, K1, swarmOf(st)); },
      },
      init(T) {
        T.num = made++; T.time = clock;
        T.cv.addEventListener('pointerdown', e => {
          const r = T.cv.getBoundingClientRect(), y = (e.clientY - r.top) / (r.height || 1) * LH;
          if (y < ROW.y + 16) return;
          OPT.colour = cellAt((e.clientX - r.left) / (r.width || 1) * LW, setNow().list.length);
          REFRESH.forEach(f => f()); dirtyAll();
        });
      },
      onScale(T) { T.paneKey = T.rowKey = null; },
      // one clock for every tile, so a tile scrolled back into view is on the others' stage
      scene(T) {
        clock = Math.max(clock, T.time); T.time = clock;
        syncButtons();
        draw(T, stateOf(T.time));
      },
    };
  },

  // build.js --break <name>: a colour a player could take for the alarm
  breaks: { red: [["['пурпур', '#dc54cc']", "['пурпур', '#f0483c']"]] },

  // the sets as swatches and as drawn (the drone rastered at game scale on each ground); a loop live; every stage in every colour of
  // every set on every ground, read off the colours really set; a pause; a pick saved again as the set changes
  check(K) {
    const X = K.X, SEC = { idle: 1.2, lamp: 1.6, scan: 1.8, thought: 1.8, window: 1.4 };
    const SET_IDS = ['crew8', 'salts8', 'ten'], TAU = Math.PI * 2;
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    const hueOf = ([r, g, b]) => { const M = Math.max(r, g, b), d = M - Math.min(r, g, b); if (!d) return 0; const h = M === r ? (g - b) / d : M === g ? (b - r) / d + 2 : (r - g) / d + 4; return ((h * 60) % 360 + 360) % 360; };
    const apart = (a, b) => Math.abs(((a - b + 540) % 360) - 180);
    const lin = v => { v /= 255; return v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4; };
    const lab = c => { const [r, g, b] = c.map(lin), f = t => t > .008856 ? Math.cbrt(t) : 7.787 * t + 16 / 116, x = f((.4124 * r + .3576 * g + .1805 * b) / .9505), y = f(.2126 * r + .7152 * g + .0722 * b), z = f((.0193 * r + .1192 * g + .9505 * b) / 1.089); return [116 * y - 16, 500 * (x - y), 200 * (y - z)]; };
    const far = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1], a[2] - b[2]);
    const parse = s => { const m = /^rgba?\((\d+),(\d+),(\d+)(?:,([\d.]+))?\)$/.exec(s); return m ? [+m[1], +m[2], +m[3], m[4] === undefined ? 1 : +m[4]] : null; };

    // ---------- the sets ----------
    const MARKS = [['the alarm', [255, 58, 40], 34], ['the hunter', [239, 68, 68], 34], ['the coal', X.EMBER, 34], ['the delivery', [251, 191, 36], 30], ['home', [226, 232, 240], 24]];
    for (const id of SET_IDS) {
      const L = X.SETS[id].list, labs = L.map(c => lab(c.rgb)), n = L.length;
      let near = [1e9], best = 0;
      L.forEach((c, i) => {
        need(apart(hueOf(c.rgb), hueOf(X.EMBER)) >= 40, `${id}: ${c.name} ${c.hex} is near the coal's hue`);
        for (const red of [MARKS[0][1], MARKS[1][1]]) need(apart(hueOf(c.rgb), hueOf(red)) >= 26, `${id}: ${c.name} ${c.hex} could be taken for red`);
        for (const [what, m, min] of MARKS) need(far(labs[i], lab(m)) >= min, `${id}: ${c.name} ${c.hex} could be taken for ${what}`);
        for (let j = i + 1; j < n; j++) { const d = far(labs[i], labs[j]); if (d < near[0]) near = [d, c.name, L[j].name]; }
      });
      const four = q => { let m = 1e9; for (const i of q) for (const j of q) if (i < j) m = Math.min(m, far(labs[i], labs[j])); return m; };
      for (let a = 0; a < n; a++) for (let b = a + 1; b < n; b++) for (let c = b + 1; c < n; c++) for (let d = c + 1; d < n; d++) best = Math.max(best, four([a, b, c, d]));
      console.log(`${id}: ${L.map(c => `${c.name} ${c.hex}`).join(', ')} | closest ${near[0].toFixed(1)} ${near[1]}~${near[2]} | first four ${four([0, 1, 2, 3]).toFixed(1)} of ${best.toFixed(1)}`);
      need(near[0] >= 28, `${id}: ${near[1]} and ${near[2]} are too alike`);
      need(four([0, 1, 2, 3]) >= best * .97, `${id}: the first four are not the farthest apart`);
    }

    // ---------- as drawn: a raster of only what the drone at rest uses ----------
    const SIZE = 40, MID = 20;
    function raster(G) {
      const px = Array.from({ length: SIZE * SIZE }, () => G.slice()), was = [];
      let paths = [];
      const S = { globalCompositeOperation: 'source-over', fillStyle: '', strokeStyle: '', lineWidth: 1 };
      const colourAt = (style, x, y) => {
        if (typeof style === 'string') return parse(style);
        const q = Math.min(1, Math.hypot(x - style.x, y - style.y) / style.r), s = style.stops;
        let i = 1;
        while (i < s.length - 1 && s[i][0] < q) i++;
        const [p0, a] = s[i - 1], [p1, b] = s[i], k = Math.min(1, Math.max(0, (q - p0) / (p1 - p0 || 1)));
        return a.map((v, j) => v + (b[j] - v) * k);
      };
      function put(style, box, cover) {
        const [x0, y0, x1, y1] = box;
        for (let y = Math.max(0, Math.floor(y0)); y < Math.min(SIZE, y1); y++) for (let x = Math.max(0, Math.floor(x0)); x < Math.min(SIZE, x1); x++) {
          const cv = cover(x, y);
          if (cv <= 0) continue;
          const col = colourAt(style, x + .5, y + .5);
          if (!col) continue;
          const a = cv * col[3], p = px[y * SIZE + x];
          for (let j = 0; j < 3; j++) p[j] = S.globalCompositeOperation === 'lighter' ? Math.min(255, p[j] + col[j] * a) : p[j] + (col[j] - p[j]) * a;
        }
      }
      const g = {
        save: () => was.push({ ...S }), restore: () => Object.assign(S, was.pop()),
        beginPath: () => { paths = []; }, moveTo: (x, y) => { paths.push([[x, y]]); }, lineTo: (x, y) => { paths[paths.length - 1].push([x, y]); },
        arc(x, y, r, a0, a1) { if (a1 > a0) paths.push(Object.assign(Array.from({ length: 21 }, (_, i) => [x + Math.cos(i / 20 * TAU) * r, y + Math.sin(i / 20 * TAU) * r]), { disc: [x, y, r] })); },
        createRadialGradient: (x0, y0, r0, x, y, r) => ({ x, y, r, stops: [], addColorStop(q, col) { this.stops.push([q, parse(col)]); } }),
        // the coal's picture: a dark lump
        drawImage(im, x, y, w) { const r = .375 * w; put('rgb(36,36,40)', [x, y, x + w, y + w], (i, j) => Math.max(0, Math.min(1, r - Math.hypot(i + .5 - x - w / 2, j + .5 - y - w / 2) + .5))); },
        fillRect(x, y, w, h) { put(S.fillStyle, [x, y, x + w + 1, y + h + 1], (i, j) => Math.max(0, Math.min(i + 1, x + w) - Math.max(i, x)) * Math.max(0, Math.min(j + 1, y + h) - Math.max(j, y))); },
        fill() { for (const p of paths) if (p.disc) { const [x, y, r] = p.disc; put(S.fillStyle, [x - r - 1, y - r - 1, x + r + 2, y + r + 2], (i, j) => Math.max(0, Math.min(1, r - Math.hypot(i + .5 - x, j + .5 - y) + .5))); } },
        stroke() {
          const w = S.lineWidth, cv = new Float32Array(SIZE * SIZE);
          for (const p of paths) for (let i = 1; i < p.length; i++) {
            const [ax, ay] = p[i - 1], [bx, by] = p[i], ll = (bx - ax) ** 2 + (by - ay) ** 2 || 1, e = w / 2 + 1;
            for (let y = Math.max(0, Math.floor(Math.min(ay, by) - e)); y < Math.min(SIZE, Math.max(ay, by) + e); y++) for (let x = Math.max(0, Math.floor(Math.min(ax, bx) - e)); x < Math.min(SIZE, Math.max(ax, bx) + e); x++) {
              const k = Math.min(1, Math.max(0, ((x + .5 - ax) * (bx - ax) + (y + .5 - ay) * (by - ay)) / ll)), d = Math.hypot(x + .5 - ax - k * (bx - ax), y + .5 - ay - k * (by - ay));
              cv[y * SIZE + x] = Math.max(cv[y * SIZE + x], Math.min(w, 1, Math.max(0, w / 2 - d + .5)));
            }
          }
          put(S.strokeStyle, [0, 0, SIZE, SIZE], (x, y) => cv[y * SIZE + x]);
        },
      };
      const c = new Proxy(g, { get: (o, k) => k in o ? o[k] : S[k], set: (o, k, v) => { S[k] = v; return true; } });
      return { c, px };
    }
    // the two zones' look against the ground: the third of their pixels farthest from it
    function seenAs(cand, C, G) {
      const R = raster(G), g = lab(G), coal = [], out = [];
      X.sample(R.c, cand, C, [MID, MID], 1.2);
      R.px.forEach((p, i) => {
        const d = Math.hypot(i % SIZE + .5 - MID, Math.floor(i / SIZE) + .5 - MID) / X.K1;
        if (d > 11) return;
        const l = lab(p), away = far(l, g);
        if (d < 4) coal.push([away, l]); else if (d > 5 && away > 4) out.push([away, l]);
      });
      const top = list => {
        const t = list.sort((a, b) => b[0] - a[0]).slice(0, Math.max(1, Math.ceil(list.length / 3)));
        return t.length && t[0] ? { away: t.reduce((s, q) => s + q[0], 0) / t.length, lab: [0, 1, 2].map(j => t.reduce((s, q) => s + q[1][j], 0) / t.length) } : { away: 0, lab: g };
      };
      // which colour it is: the more coloured half of its zone, so a smoke or a soot does not count for one
      const vivid = out.map(q => q[1]).sort((a, b) => Math.hypot(b[1], b[2]) - Math.hypot(a[1], a[2])).slice(0, Math.ceil(out.length / 2));
      const all = vivid.reduce((s, q) => [s[0] + q[0], s[1] + q[1], s[2] + q[2]], [0, 0, 0]).map(v => v / (vivid.length || 1));
      return { coal: top(coal), colour: top(out), all };
    }
    const UNDER = { space: [7, 10, 15], room: [112, 116, 104], planet: [160, 171, 169] };
    for (const t of K.tiles) {
      const cand = t.cand, line = [];
      for (const gid of X.GROUNDS) {
        let reads = 1e9, colour = 1e9, near = [1e9];
        for (const id of SET_IDS) {
          const L = X.SETS[id].list, S = L.map(c => seenAs(cand, c.rgb, UNDER[gid]));
          S.forEach((s, i) => {
            reads = Math.min(reads, Math.max(s.coal.away, s.colour.away)); colour = Math.min(colour, s.colour.away);
            for (let j = i + 1; j < S.length; j++) { const d = far(s.all, S[j].all); if (d < near[0]) near = [d, `${id} ${L[i].name}~${L[j].name}`]; }
          });
        }
        line.push(`${gid}: one reads ${reads.toFixed(0)}, colour ${colour.toFixed(0)}, closest ${near[0].toFixed(1)} ${near[1]}`);
        if (cand.id === 'soot' || cand.id === 'enamel') {
          need(reads >= 25 && colour >= 18, `${cand.id} on ${gid}: a colour melts into the ground`);
          need(near[0] >= 5, `${cand.id} on ${gid}: ${near[1]} look alike as drawn`);
        }
      }
      console.log(`drawn ${cand.id} | ${line.join(' | ')}`);
    }

    // ---------- the colours set on each canvas, by ground and by what was being drawn ----------
    const seen = K.tiles.map(() => { const o = {}; for (const g of X.GROUNDS) o[g] = { drone: new Map(), ui: new Map(), inside: new Map(), bracelet: new Map() }; return o; });
    K.tiles.forEach((t, j) => {
      const take = v => { if (!t.probe || typeof v !== 'string') return; const q = parse(v); if (q && q[3] > 0) seen[j][t.probe[0]][t.probe[1]].set(q.slice(0, 3).join(), q); };
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
    let f = L, name = '';
    SET_IDS.forEach((id, si) => ['space', 'room', 'planet'].forEach((bg, bi) => X.SETS[id].list.forEach((col, i) => {
      for (const stage of X.STAGES) {
        const at = stage.t0 + SEC[stage.id];
        K.at(f, () => { K.seg('paletteMode', si); K.seg('bgMode', bi); K.seg('colourMode', i + 1); X.seek(at); });
        plan.set(f, { id, bg, i, stage: stage.id });
        f++;
      }
    })));
    const P = f + 2, E = P + 70, docs = [];
    let status = '', copied = '';
    K.at(L + 1, () => { name = K.byId.colourMode.children.filter(c => c.listeners.click)[1].textContent; });
    K.at(P, () => K.seg('playMode', 1));
    K.at(P + 30, () => K.seg('playMode', 0));
    K.at(P + 36, () => { K.seg('paletteMode', 1); K.click(0, '.pick'); });
    K.at(P + 40, () => { docs.push(K.picked()); K.seg('paletteMode', 2); });
    K.at(P + 44, () => { docs.push(K.picked()); status = K.byId.status.textContent; K.byId.copy.listeners.click[0](); copied = K.byId.fallback.value; });

    // is col the colour C, plain or paled toward white
    const isOf = (col, C) => { const k = [0, 1, 2].map(i => 255 - C[i]), i = k.indexOf(Math.max(...k)), p = (col[i] - C[i]) / (k[i] || 1); return p >= -.02 && p < .9 && [0, 1, 2].every(q => Math.abs(col[q] - (C[q] + k[q] * p)) < 3); };
    const either = (list, C) => { const S = X.strong(C); return list.some(c => isOf(c, C) || isOf(c, S)); };
    const per = K.tiles.map(() => ({ order: [], held: {}, drawn: 0, both: 0, paused: 0, last: -1, inside: null }));
    let apartFrames = 0;
    K.run(E, (t, frame) => {
      const j = K.tiles.indexOf(t), q = per[j], d = t.dm, p = plan.get(frame), id = t.cand.id;
      if (d && frame < L) {
        if (q.order[q.order.length - 1] !== d.stage) q.order.push(d.stage);
        if (frame < L - 5) q.held[d.stage] = (q.held[d.stage] || 0) + 1 / 60;
        if (j && Math.abs(t.time - K.tiles[0].time) > 2 / 60) apartFrames++;
      }
      if (d && p) {
        const C = X.SETS[p.id].list[p.i];
        if (d.stage !== p.stage || d.colour.hex !== C.hex || d.row !== X.SETS[p.id].list.length) K.fail(`${id} ${p.id}/${p.bg}: drew ${d.stage} in ${d.colour.hex}, ${d.row} in the row`);
        for (const g of X.GROUNDS) {
          const at = `${id}: ${p.id} ${C.name} ${p.stage} on ${g}`, got = d.panes[g], S = seen[j][g], drone = [...S.drone.values()];
          q.drawn++;
          if (!either([...S.bracelet.values()], C.rgb)) K.fail(`${at}: the bracelet is not in the colour`);
          if (!either(drone, C.rgb)) K.fail(`${at}: the drone carries none of the player's colour`);
          else if (!either(drone, X.EMBER)) K.fail(`${at}: the coal is gone`);
          else q.both++;
          if (['lamp', 'thought'].includes(p.stage) && got.shaped < 50) K.fail(`${at}: only ${got.shaped} sparks on the shape`);
          if (p.stage === 'scan' && !(got.lit >= 10 && got.dim >= 5)) K.fail(`${at}: the gauge has ${got.lit} lit and ${got.dim} dim`);
          if (p.stage === 'window') {
            const ui = [...S.ui.values()], inside = [...S.inside.keys()].sort().join(';');
            if (!(got.corners === 4 && got.keys === 3)) K.fail(`${at}: corners ${got.corners}, keys ${got.keys}`);
            if (!either(ui, C.rgb) || !either(ui, X.EMBER)) K.fail(`${at}: the corners and keys are not of two colours`);
            if (!inside || (q.inside !== null && q.inside !== inside)) K.fail(`${at}: the window's inside changed`);
            q.inside = inside;
          }
        }
      }
      if (frame > P && frame < P + 29) { if (q.last >= 0 && t.time !== q.last) K.fail(`${id}: time ran while paused`); q.paused++; }
      q.last = t.time;
      for (const g in seen[j]) for (const k in seen[j][g]) seen[j][g][k].clear();
    });

    const all = SET_IDS.reduce((n, id) => n + X.SETS[id].list.length, 0) * X.STAGES.length * 9;
    console.log('stages, s:', X.STAGES.map(s => `${s.id} ${s.t0}..${s.t0 + s.len} (sampled at ${(s.t0 + SEC[s.id]).toFixed(1)})`).join(', '));
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id;
      console.log(`${id}: ${q.drawn} ground frames, both colours ${q.both}, paused ${q.paused}`);
      need(q.order.slice(0, X.STAGES.length).join() === X.STAGES.map(s => s.id).join(), `${id}: the loop in order`);
      need(X.STAGES.every(s => Math.abs((q.held[s.id] || 0) - s.len) < .06), `${id}: each stage as long as planned`);
      need(q.drawn === all && q.both === all, `${id}: both colours at every stage, colour, set and ground`);
      need(q.paused > 20, `${id}: the pause held`);
    });
    need(apartFrames === 0, 'every tile is at the same moment of the loop');
    need(name === 'лайм', `the colour buttons carry the set's names (${name})`);
    const picked = K.tiles[0].cand;
    console.log('docs:', docs.map(d => JSON.stringify(d)).join(' | '), '| copy:', copied);
    ['salts8', 'ten'].forEach((palette, i) => need(docs[i] && docs[i].pick === picked.id && docs[i].palette === palette && Object.keys(docs[i]).sort().join() === 'palette,pick', `pick ${i}: the way and the set saved, nothing else`));
    need(status.includes(`«${picked.name}»`) && status.includes('Десять') && copied.includes(`(${picked.id})`) && copied.includes('Десять'), 'the status and the copy text name both');
    return ok;
  },
};
