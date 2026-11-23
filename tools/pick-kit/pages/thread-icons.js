// The drone wheel's modes (held F), the gestures' wheel (held H) and the edge's keys, drawn in the drone's «Нити» look: five ways to
// build a thread icon and make it alive, every real mode and key in every state, at the game's size and in loupes; one hue plus
// brightness, recoloured by a viewing control. An art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/thread-icons.js <out-dir> [--break <name>]
module.exports = {
  title: 'Значки нитями',
  heading: 'Значки колеса и кнопок в стиле «Нити»',
  window: false,
  tile: [1200, 690],
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Значки колеса дрона (держать <b>F</b>) и кнопок у нижнего края, нарисованные как сам дрон «Нити»: светящиеся нити на тёмном, по ним бегут искры. Пять способов отличаются тем, как собрана нить значка и насколько он живой. В каждой карточке всё сразу, в размере игры: колесо дрона во всех состояниях (покой, наведено, выбор, ещё не изучено), колесо жестов (держать <b>H</b>, у него тот же вид) и шесть кнопок края во всех состояниях; справа две лупы, где те же значки проходят состояния по кругу.</p>
<p class="lead">Режимы и кнопки те же, что в игре: в колесе дрона <b>фонарь</b> (сверху) и <b>скан</b> (снизу); в колесе жестов «да», «нет», «помахать»; на краю по порядку <b>Воздух, Стройка, Планшет, Рынок, Костюм, Справочник</b>. Всё нарисовано одним оттенком и его яркостью: цвет нитей потом может стать цветом игрока. Переключатель «Оттенок» вверху перекрашивает всю страницу — он только для просмотра и в выбор не входит. «Крупно 1:1» показывает точный размер игры.</p>
<p class="miss">Не показано: звук; отрисовка Godot (свет выйдет чуть иначе); как кнопка мерцает, появляясь и исчезая; вспышка числа при счёте; подсказка с названием и клавишей под указателем. Янтарь «ждёт» (Планшет, Рынок) здесь заменён пульсом того же оттенка. В игре колесо при отпускании просто исчезает — вспышка выбранного значка здесь предложение. Жесты в игре — рисунок перчатки, здесь они тоже нитью.</p>`,
  toggles: [
    { id: 'hueMode', bar: 1, label: 'Оттенок (только посмотреть):', key: 'hue', opts: [['угли', 'ember'], ['циан', 'cyan'], ['зелёный', 'green'], ['фиолетовый', 'violet']], saved: false },
  ],
  pick: { doc: 'picks/threadIcons', subject: 'Значки нитями', what: ['отрисовку', 'отрисовкой'] },

  page() {
    const TAU = Math.PI * 2;
    // ---------- what the game has ----------
    const MODES = ['lamp', 'scan'];             // DroneModes.Sides, from the top clockwise
    const GESTURES = ['yes', 'no', 'wave'];     // Gestures.WheelSides; a tap points
    const KEYS = ['air', 'build', 'research', 'market', 'suit', 'journal'];   // Main.cs puts them on the edge in this order
    const WAITS = ['research', 'market'];       // the keys that tell of something waiting (amber in the game)
    const WHEEL = { middle: 18, radius: 54, disc: 21, grow: 1.15, pic: 12.5, hand: 13 };   // Wheel.cs, WheelView.cs at the normal slot size
    const KEY = { side: 28, icon: 9 };          // TabletPeek.KeySide; EdgeKey's motif 18 px inside a 5 px inset
    const LEVEL = { rest: .34, near: .85, hover: 1.1, held: .77, on: .85, wait: .85 };   // EdgeKey.Level per state
    const GROUPS = [
      ['rest', 'покой (тускло)'],
      ['near', 'указатель у края'],
      ['hover', 'под указателем'],
      ['held', 'нажата'],
      ['on', 'включена'],
      ['wait', 'ждёт: Планшет (3), Рынок'],
    ];
    const PULSE = 1.2;   // a waiting key throbs this many times a second
    const HUES = { ember: { rgb: [255, 138, 58] }, cyan: { rgb: [80, 214, 255] }, green: { rgb: [120, 236, 128] }, violet: { rgb: [178, 124, 255] } };

    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const AC = () => HUES[OPT.hue].rgb;
    const hot = k => pale(AC(), k);
    const deep = k => { const a = AC(); return [a[0] * k, a[1] * k, a[2] * k]; };

    // ---------- paths: polylines in a box -1..1 (y down), walked by a share of their length ----------
    function mkPath(polys, br) {
      const seg = []; let L = 0;
      polys.forEach((p, pi) => { for (let i = 1; i < p.length; i++) { const a = p[i - 1], b = p[i], d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 1e-6) continue; seg.push([a[0], a[1], b[0], b[1], d, L, br ? br[pi][i] : 0]); L += d; } });
      return { seg, L, polys };
    }
    function onPath(P, s) {
      const d = (((s % 1) + 1) % 1) * P.L; let lo = 0, hi = P.seg.length - 1;
      while (lo < hi) { const m = (lo + hi + 1) >> 1; if (P.seg[m][5] <= d) lo = m; else hi = m - 1; }
      const q = P.seg[lo], e = c01((d - q[5]) / q[4]);
      return [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e, (q[2] - q[0]) / q[4], (q[3] - q[1]) / q[4]];
    }
    // one thread through every part: each next part from the nearer of its ends, the step between marked a bridge
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
    const mkIcon = polys => ({ P: mkPath(polys), one: joinOne(polys) });
    const arc = (cx, cy, r, n = 28, a0 = 0, a1 = TAU) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]); } return p; };
    function roundSq(rc) {
      const p = [];
      for (const [cx, cy, a0] of [[1 - rc, -1 + rc, -Math.PI / 2], [1 - rc, 1 - rc, 0], [-1 + rc, 1 - rc, Math.PI / 2], [-1 + rc, -1 + rc, Math.PI]])
        for (let i = 0; i <= 4; i++) { const a = a0 + i / 4 * Math.PI / 2; p.push([cx + Math.cos(a) * rc, cy + Math.sin(a) * rc]); }
      p.push(p[0].slice()); return p;
    }
    // the edge keys' motifs, as tools/anime-art/subjects/edgekeys.js draws them in a 16-unit square: outlines become threads
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
          for (let k = 1; k <= n; k++) { const s = k / n, m = 1 - s; cur.push([m * m * m * p[0] + 3 * m * m * s * a[0] + 3 * m * s * s * b[0] + s * s * s * e[0], m * m * m * p[1] + 3 * m * m * s * a[1] + 3 * m * s * s * b[1] + s * s * s * e[1]]); }
          p = e;
        } else if (cmd === 'Z') { cur.push(st.slice()); p = st; }
      }
      return out.map(q => q.map(([x, y]) => [(x - 8) / 8, (y - 8) / 8]));
    }
    const rrD = (x, y, w, h, r) => `M ${x + r} ${y} L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
    const motif = ds => ds.flatMap(d => svg(d));
    const THUMB = [[-.5, .85], [-.5, .05], [-.22, -.02], [-.2, -.55], [-.12, -.85], [.02, -.92], [.14, -.82], [.12, -.35], [.1, -.05], [.55, -.05], [.68, .08], [.62, .25], [.7, .38], [.63, .55], [.68, .7], [.55, .85], [-.5, .85]];
    const FIST = [THUMB, [[.1, .25], [.6, .25]], [[.1, .45], [.62, .45]], [[.1, .65], [.6, .65]], [[-.85, 0], [-.62, 0], [-.62, .88], [-.85, .88], [-.85, 0]]];
    const ICONS = {
      // the lamp: a lens ringed by rays, its rays one zigzag; the scan: a ring a line sweeps round, a trail behind it (WheelView.Drone)
      lamp: mkIcon([Array.from({ length: 17 }, (_, k) => { const a = k / 16 * TAU - Math.PI / 2, r = k % 2 ? .52 : .95; return [Math.cos(a) * r, Math.sin(a) * r]; }), arc(0, 0, .24, 16)]),
      scan: mkIcon([arc(0, 0, .88, 36), [[0, 0], [Math.cos(-Math.PI / 4) * .88, Math.sin(-Math.PI / 4) * .88]], arc(0, 0, .58, 10, -Math.PI / 4 - 1.1, -Math.PI / 4), arc(0, 0, .16, 12)]),
      yes: mkIcon(FIST),
      no: mkIcon(FIST.map(p => p.map(([x, y]) => [x, -y]))),
      wave: mkIcon([[[-.42, .92], [-.48, .3], [-.86, -.08], [-.78, -.22], [-.4, .02], [-.38, -.68], [-.26, -.8], [-.14, -.68], [-.12, -.16], [-.08, -.86], [.04, -.96], [.17, -.86], [.15, -.16], [.22, -.74], [.34, -.82], [.45, -.7], [.38, -.08], [.48, -.46], [.58, -.52], [.68, -.42], [.56, .3], [.36, .92], [-.42, .92]],
        arc(.1, -.3, .8, 6, -.9, -.1), arc(.1, -.3, .98, 6, -.85, -.15)]),
      air: mkIcon(motif(['M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 'M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 'M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2'])),
      build: mkIcon(motif([rrD(0.4, 1.8, 7.1, 3.5, .55), 'M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z',
        rrD(0.4, 6.2, 3.1, 3.5, .5), rrD(4.4, 6.25, 7.2, 3.5, .55), rrD(12.5, 6.2, 3.1, 3.5, .5), rrD(0.4, 10.6, 7.1, 3.5, .55),
        'M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z'])),
      research: mkIcon(motif(['M 5.3 10.8 C 2.4 9.3 1.8 5.4 3.8 3.0 C 5.8 0.6 10.2 0.6 12.2 3.0 C 14.2 5.4 13.6 9.3 10.7 10.8 L 10.4 12.2 L 5.6 12.2 Z', rrD(5.5, 12.7, 5, 1.6, .3),
        'M 6.7 14.8 L 9.3 14.8 L 8.7 15.8 L 7.3 15.8 Z', 'M 6.5 9.8 L 7 6.6 L 8 8.2 L 9 6.6 L 9.5 9.8'])),
      market: mkIcon(motif(['M 8 0.8 Q 8.5 2.6 9.7 3.7 Q 14.9 6.5 14.3 11.0 Q 13.5 15.2 8 15.4 Q 2.5 15.2 1.7 11.0 Q 1.1 6.5 6.3 3.7 Q 7.5 2.6 8 0.8 Z', 'M 8 4.6 Q 4.0 7.4 4.6 11.2 Q 5.2 14.0 8 14.4', 'M 8 4.6 Q 12.0 7.4 11.4 11.2 Q 10.8 14.0 8 14.4'])),
      suit: mkIcon([...motif(['M 5.5 1.7 Q 8 5.3 10.5 1.7 L 12.9 2.2 Q 15.5 2.9 15.7 5.6 L 15.6 9.3 Q 14.6 9.7 13.6 9.4 L 13.25 6.9 Q 12.3 7.5 12.4 9.2 L 11.95 12.4 Q 12.5 14.0 12.35 15.2 Q 8 15.9 3.65 15.2 Q 3.5 14.0 4.05 12.4 L 3.6 9.2 Q 3.7 7.5 2.75 6.9 L 2.4 9.4 Q 1.4 9.7 0.4 9.3 L 0.3 5.6 Q 0.5 2.9 3.1 2.2 Z']), arc(0, -.0375, 1.95 / 8, 14)]),
      journal: mkIcon(motif(['M 7.75 4.6 Q 4.8 2.7 0.6 3.3 L 0.4 13.4 Q 4.6 12.7 7.75 14.7 Z', 'M 8.25 4.6 Q 11.2 2.7 15.4 3.3 L 15.6 13.4 Q 11.4 12.7 8.25 14.7 Z',
        'M 2.6 6.4 Q 4.6 6.0 6.6 6.8', 'M 2.6 9.0 Q 4.6 8.6 6.6 9.4', 'M 9.4 6.8 Q 11.4 6.0 13.4 6.4', 'M 9.4 9.4 Q 11.4 8.6 13.4 9.0'])),
    };
    const RIM = { circle: mkIcon([arc(0, 0, 1, 48)]), square: mkIcon([roundSq(.24)]), cross: mkIcon([[[-.42, -.42], [.42, .42]], [[.42, -.42], [-.42, .42]]]) };

    // ---------- drawing a thread: F = { x, y, R (px per unit), u (a thread's px) } ----------
    const P2 = (F, q) => [F.x + q[0] * F.R, F.y + q[1] * F.R];
    function pathStroke(c, P, F, w, col, a, pick) {
      if (a <= .004) return 0;
      c.beginPath(); let last = null, any = false;
      for (const s of P.seg) {
        if (pick && !pick(s)) { last = null; continue; }
        const A = P2(F, s), B = P2(F, [s[2], s[3]]);
        if (!last || last[0] !== s[0] || last[1] !== s[1]) c.moveTo(A[0], A[1]);
        c.lineTo(B[0], B[1]); last = [s[2], s[3]]; any = true;
      }
      if (!any) return 0;
      c.lineWidth = w; c.strokeStyle = css(col, a); c.stroke(); return 1;
    }
    // squares gathered by how bright they are, one fill a level: hundreds of embers cost a few fills, not hundreds
    const SB = new Map();
    function sq(key, x, y, s) { let a = SB.get(key); if (!a) SB.set(key, a = []); a.push(x, y, s); }
    function flushSq(c, colOf) {
      let n = 0;
      for (const [key, a] of SB) { c.beginPath(); for (let i = 0; i < a.length; i += 3) c.rect(a[i] - a[i + 2] / 2, a[i + 1] - a[i + 2] / 2, a[i + 2], a[i + 2]); const [col, al] = colOf(key); c.fillStyle = css(col, al); c.fill(); n++; }
      SB.clear(); return n;
    }
    // a spark: a hot square and two fading ones behind it, as the drone's threads draw theirs (gathered; flushSparks fills them)
    function spark(F, q, s, h, tail) {
      if (h <= .02) return 0;
      const p = P2(F, q), hq = Math.min(10, Math.max(1, Math.round(h * 10)));
      for (let m = 0; m < 3; m++) { const b = m * tail; sq(hq * 3 + m, p[0] - q[2] * b, p[1] - q[3] * b, s); }
      return 1;
    }
    const flushSparks = c => flushSq(c, key => { const m = key % 3; return [hot(.35 * (1 - m / 3)), (.95 - m * .3) * Math.floor(key / 3) / 10]; });
    // a run along an open thread: the stretch behind s brightening to its head
    function runTail(c, P, F, s, len, w, h, n = 10) {
      let k = 0;
      for (let i = 0; i < n; i++) {
        const s0 = s - len * (1 - i / n), s1 = Math.min(.9999, s - len * (1 - (i + 1) / n));
        if (s0 < 0 || s1 <= s0) continue;
        const a = P2(F, onPath(P, s0)), b = P2(F, onPath(P, s1));
        c.beginPath(); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]); c.lineWidth = w; c.strokeStyle = css(hot(.3 + .4 * i / n), h * (i + 1) / n); c.stroke(); k++;
      }
      return k;
    }
    const pulseOf = st => st.wait ? .7 + .3 * Math.sin(st.t * TAU * PULSE) : 1;

    // st: lv how bright (EdgeKey's level), hov under the pointer, on, held, wait, shut, flare (1 → 0 after a pick), rim (a disc's or key's edge), t, sd
    const cands = [
      {
        id: 'oneThread', name: 'Одна нить', flareS: .45,
        idea: 'Каждый значок — одна непрерывная нить: где частей несколько, она переходит между ними тонкой перемычкой. В покое нить тихо тлеет. Под указателем по ней бежит одна искра с хвостом — от начала до конца. Включено — искра ходит медленно. Нажата — искра замирает. Не изучено — нить порвана (пунктир).',
        time: 'Искра проходит значок за 0,9 с, пауза 0,3 с · включено: проход за 2,4 с · выбор: нить вспыхивает и гаснет за 0,45 с, колесо уходит за 0,2 с · ждёт: пульс 1,2 раза в секунду.',
        draw(c, I, F, st) {
          const P = st.rim ? I.P : I.one, u = F.u, t = st.t, pulse = pulseOf(st), fl = st.flare || 0, lv = st.lv * pulse * (st.shut ? .35 : 1);
          let n = 0;
          c.globalCompositeOperation = 'lighter';
          n += pathStroke(c, P, F, 3 * u, AC(), (.06 + .1 * fl) * lv);
          if (st.shut) { c.setLineDash([1.6 * u, 1.8 * u]); n += pathStroke(c, P, F, .9 * u, hot(.05), .55 * lv); c.setLineDash([]); }
          else {
            const a = Math.min(1, .3 + .45 * lv + .4 * fl) * (st.held ? .8 : 1);
            n += pathStroke(c, P, F, (st.held ? .8 : 1) * u, hot(.12 + .4 * fl), a, s => !s[6]);
            n += pathStroke(c, P, F, .6 * u, hot(.12), a * .35, s => s[6]);
            let s = null;
            if (st.rim) { if (st.hov > .01) s = (t * .6 + st.sd) % 1; }
            else if (fl > 0) s = 1 - fl;
            else if (st.held) s = .55 + .2 * hsh(st.sd);
            else if (st.hov > .01) { const q = (t + st.sd) % 1.2; s = q < .9 ? q / .9 : null; }
            else if (st.on || st.wait) s = ((t + st.sd * 3) / 2.4) % 1;
            if (s != null) {
              const h = Math.min(1, .5 + .5 * lv) * (st.held ? .6 : 1) * (st.rim ? .6 : 1);
              n += runTail(c, P, F, s, st.rim ? .12 : .22, 1.3 * u, h);
              const q = onPath(P, Math.min(.9999, s)), p = P2(F, q);
              if (!st.rim) halo(c, p[0], p[1], 3.2 * u, hot(.4), .45 * h);
              n += spark(F, q, 1.3 * u, h, .01); flushSparks(c);
            }
          }
          c.globalCompositeOperation = 'source-over';
          return n;
        },
      },
      {
        id: 'smoulder', name: 'Тлеющий контур', flareS: .5,
        idea: 'Как сам дрон «Нити»: контур — тонкая нить с мягким ореолом, стоит неподвижно. Искры с хвостиками бегут по контуру, только пока значок жив: под указателем, включён, ждёт. В покое — один тусклый контур. Нажата — искры замирают. Выбор — искры разгоняются втрое. Не изучено — тусклый контур без искр.',
        time: 'Искры по контуру 30 px/с, как у дрона; включено — 14 px/с · выбор: 90 px/с и вспышка за 0,5 с · колесо уходит за 0,2 с · ждёт: пульс 1,2 раза в секунду.',
        draw(c, I, F, st) {
          const P = I.P, u = F.u, t = st.t, pulse = pulseOf(st), fl = st.flare || 0, lv = st.lv * pulse * (st.shut ? .3 : 1);
          let n = 0;
          c.globalCompositeOperation = 'lighter';
          n += pathStroke(c, P, F, 2.4 * u, AC(), (.05 + .08 * fl) * lv);
          n += pathStroke(c, P, F, .75 * u, hot(.1 + .3 * fl), Math.min(1, .25 + .38 * lv + .4 * fl));
          const act = st.shut ? 0 : Math.max(st.hov || 0, st.on ? .75 : 0, st.wait ? .75 : 0, fl, st.held ? .5 : 0);
          if (act > .01 && !(st.rim && !(st.hov > .01))) {
            const len = P.L * F.R, cnt = clamp(Math.round(len / (7 * u)), 3, 40);
            const v = st.held ? 0 : (fl > 0 ? 90 : st.hov > .01 ? 30 : 14) * u;
            for (let i = 0; i < cnt; i++) {
              const s = i / cnt + hsh(i + st.sd * 10) * .5 / cnt + t * v / len;
              const h = act * (.55 + .35 * Math.sin(t * 11 + i * 7.1)) * (st.held ? .55 : 1) * pulse * (st.rim ? .6 : 1);
              n += spark(F, onPath(P, s), 1.05 * u, h, 1.25 * u);
            }
            flushSparks(c);
          }
          c.globalCompositeOperation = 'source-over';
          return n;
        },
      },
      {
        id: 'dotted', name: 'Пунктир углей', flareS: .4,
        idea: 'Контур собран из отдельных тлеющих точек, и каждая мерцает сама по себе. Под указателем по точкам бежит волна жара. Включено — все точки горят ровно. Нажата — точки сжимаются. Не изучено — остаётся каждая третья, пепельная.',
        time: 'Точки через 2,3 px · волна жара обегает значок за 0,7 с · выбор: точки вспыхивают добела и остывают за 0,4 с · колесо уходит за 0,2 с · ждёт: пульс 1,2 раза в секунду.',
        draw(c, I, F, st) {
          const P = I.P, u = F.u, t = st.t, pulse = pulseOf(st), fl = st.flare || 0, lv = st.lv;
          const cnt = Math.max(6, Math.round(P.L * F.R / (2.3 * u))), s = (st.held ? .8 : 1.15) * u;
          let n = 0;
          c.globalCompositeOperation = 'lighter';
          for (let j = 0; j < cnt; j++) {
            if (st.shut && j % 3) continue;
            const p = P2(F, onPath(P, (j + .5) / cnt));
            let h = (.22 + .42 * lv) * (.72 + .28 * Math.sin(t * (2 + hsh(j) * 3) + j * 1.7));
            if (st.hov > .01 && !st.rim) { const w = .5 + .5 * Math.cos(TAU * (j / cnt - t / .7)); h += st.hov * .65 * Math.pow(w, 6); }
            if (st.on) h = Math.max(h, .8 + .12 * Math.sin(t * 9 + j));
            h = Math.min(1, h * pulse + fl);
            if (st.shut) h = .16;
            const hq = Math.max(1, Math.round(h * 12));
            sq(hq, p[0], p[1], s);
            if (h > .55) sq(100 + hq, p[0], p[1], 2.8 * u);
            n++;
          }
          flushSq(c, key => key >= 100 ? [AC(), .12 * (key - 100) / 12] : [hot(.15 + .4 * fl + .3 * c01(key / 12 - .7)), key / 12]);
          c.globalCompositeOperation = 'source-over';
          return n;
        },
      },
      {
        id: 'orbit', name: 'Орбита', flareS: .5,
        idea: 'Значок — тихий контур, а вокруг него наклонная орбита-нить, как кольца у ядра дрона; задняя половина орбиты тусклее. В покое по ней медленно ходит одна искра, под указателем — три и быстрее, включено — две. Нажата — орбита сжимается. Выбор — орбита стягивается в значок. Не изучено — без орбиты.',
        time: 'Покой: оборот за 4 с · под указателем: 3 искры, оборот за 1,1 с · включено: 2 искры, 2,2 с · выбор: орбита стягивается за 0,5 с · колесо уходит за 0,2 с · ждёт: пульс 1,2 раза в секунду.',
        draw(c, I, F, st) {
          const P = I.P, u = F.u, t = st.t, pulse = pulseOf(st), fl = st.flare || 0, lv = st.lv * pulse * (st.shut ? .45 : 1);
          let n = 0;
          c.globalCompositeOperation = 'lighter';
          n += pathStroke(c, P, F, 2.2 * u, AC(), .05 * lv);
          n += pathStroke(c, P, F, .75 * u, hot(.1 + .3 * fl), Math.min(1, .24 + .4 * lv + .4 * fl));
          if (!st.rim && !st.shut) {
            const e = fl > 0 ? 1 - fl : 0, rad = (st.held ? .86 : 1) * (1 - .8 * sm(e));
            const rx = 1.3 * rad, ry = .46 * rad, tl = -.38 + .08 * Math.sin(t * .4 + st.sd), ca = Math.cos(tl), sa = Math.sin(tl);
            const at = a => { const x = Math.cos(a) * rx, y = Math.sin(a) * ry; return [x * ca - y * sa, x * sa + y * ca]; };
            const hv = st.hov > .01, spd = fl > 0 ? 2.5 : hv ? 1 / 1.1 : st.on || st.wait ? 1 / 2.2 : 1 / 4;
            const oa = (.1 + .22 * lv + .25 * (st.hov || 0)) * (1 - e * .5);
            const back = [], front = [];
            for (let i = 0; i <= 40; i++) { const a = i / 40 * TAU, q = at(a); (Math.sin(a) < 0 ? back : front).push(q); }
            n += pathStroke(c, mkPath([back]), F, .6 * u, hot(.1), oa * .45);
            n += pathStroke(c, mkPath([front]), F, .7 * u, hot(.15), oa);
            const k = hv || fl > 0 ? 3 : st.on || st.wait ? 2 : 1;
            for (let m = 0; m < k; m++) {
              const a = (t * spd + m / k + st.sd) * TAU, q = at(a), v = [-Math.sin(a) * rx, Math.cos(a) * ry], vr = [v[0] * ca - v[1] * sa, v[0] * sa + v[1] * ca], vl = Math.hypot(vr[0], vr[1]) || 1;
              const h = (.55 + .35 * Math.min(1, lv)) * (Math.sin(a) < 0 ? .4 : 1) * (fl > 0 ? 1.3 : 1) * (st.held ? .6 : 1);
              n += spark(F, [q[0], q[1], vr[0] / vl, vr[1] / vl], 1.15 * u, h, 1.4 * u);
            }
            flushSparks(c);
          }
          c.globalCompositeOperation = 'source-over';
          return n;
        },
      },
      {
        id: 'warm', name: 'Жаркий штрих', flareS: .45,
        idea: 'Толстый тёплый штрих, как раскалённая проволока: тёмное тело и светлое пятно жара, которое ползёт по нему. В покое жар ползёт медленно, под указателем быстро, включено — два пятна. Нажата — штрих тоньше, жар стоит. Выбор — весь штрих раскаляется добела. Не изучено — остывший штрих без жара.',
        time: 'Пятно жара: покой — проход за 4,5 с, под указателем — за 1 с, включено — два пятна по 2 с · выбор: добела и остывает за 0,45 с · колесо уходит за 0,2 с · ждёт: пульс 1,2 раза в секунду.',
        draw(c, I, F, st) {
          const P = I.P, u = F.u, t = st.t, pulse = pulseOf(st), fl = st.flare || 0, lv = st.lv;
          const w = (st.held ? 1.5 : 1.9) * u * (st.rim ? .6 : 1);
          let n = 0;
          n += pathStroke(c, P, F, w, deep(st.shut ? .28 : .45 + .2 * Math.min(1, lv)), st.shut ? .45 : .55 + .35 * Math.min(1, lv));
          c.globalCompositeOperation = 'lighter';
          if (fl > 0) n += pathStroke(c, P, F, w, hot(.55), fl);
          if (!st.shut) {
            const heads = [];
            if (st.held) heads.push(.4 + .2 * hsh(st.sd));
            else if (st.hov > .01) heads.push(((t + st.sd) / 1.0) % 1.25 - .12);
            else if (st.on || st.wait) { const q = ((t + st.sd) / 2) % 1; heads.push(q, (q + .5) % 1); }
            else heads.push(((t + st.sd * 4) / 4.5) % 1.25 - .12);
            const amp = (st.rim ? .4 : 1) * (.35 + .55 * Math.min(1, lv)) * pulse, lev = [];
            for (const s of P.seg) {
              const m = (s[5] + s[4] / 2) / P.L; let g = 0;
              for (const h of heads) { let d = Math.abs(m - h); if (st.rim) d = Math.min(d, 1 - d); d /= .13; g = Math.max(g, Math.exp(-d * d)); }
              const gq = Math.round(g * 8); if (gq < 1) continue;
              (lev[gq] = lev[gq] || []).push(s);
            }
            // the glow by level: a path per level, so a long stroke costs a few strokes
            lev.forEach((segs, gq) => { if (!segs) return; const g = gq / 8; c.beginPath(); for (const s of segs) { const A = P2(F, s), B = P2(F, [s[2], s[3]]); c.moveTo(A[0], A[1]); c.lineTo(B[0], B[1]); } c.lineWidth = w * .75; c.strokeStyle = css(hot(.3 + .3 * g), g * amp); c.stroke(); n++; });
            n += pathStroke(c, P, F, w * 2.6, AC(), .05 * lv * pulse);
          }
          c.globalCompositeOperation = 'source-over';
          return n;
        },
      },
    ];

    // ---------- what is shown: the draw of every icon this frame, for the check ----------
    const see = (T, key, n) => { const m = T.seenNow; m.set(key, Math.max(m.get(key) || 0, n)); };
    // Wheel.SideOf: the side an offset points to, from the top clockwise; none within the middle
    function sideOf(dx, dy, middle, n) {
      if (dx * dx + dy * dy < middle * middle) return null;
      const share = TAU / n, s = Math.floor((Math.atan2(dx, -dy) + share / 2) / share);
      return ((s % n) + n) % n;
    }
    // ws: { vis, ptr: the pointer from the middle, shut: [..], chosen: { i, f } }
    function drawWheel(T, x, y, set, ws) {
      const c = T.c, cand = T.cand, ids = set === 'drone' ? MODES : GESTURES, n = ids.length, t = T.time, shut = ws.shut || [];
      const tw = ws.ptr ? sideOf(ws.ptr[0], ws.ptr[1], WHEEL.middle, n) : null;
      if (ws.vis > .01) { c.globalAlpha = ws.vis; cand.draw(c, RIM.circle, { x, y, R: WHEEL.middle, u: .8 }, { lv: .32, rim: 1, t, sd: 1, hov: 0 }); }
      for (let i = 0; i < n; i++) {
        const id = ids[i], a = i * TAU / n, sh = !!shut[i], lit = tw === i && !sh, ch = ws.chosen && ws.chosen.i === i ? ws.chosen.f : 0;
        const vis = ch > 0 ? 1 : ws.vis; if (vis <= .01) continue;
        const grow = lit || ch > 0 ? WHEEL.grow : 1, px = x + Math.sin(a) * WHEEL.radius, py = y - Math.cos(a) * WHEEL.radius, r = WHEEL.disc * grow;
        c.globalAlpha = vis;
        c.beginPath(); c.arc(px, py, r, 0, TAU); c.fillStyle = lit || ch > 0 ? 'rgba(5,8,14,.9)' : 'rgba(5,8,14,.62)'; c.fill();
        const sd = i * 1.7 + (set === 'drone' ? 0 : 5), hov = lit ? 1 : 0;
        cand.draw(c, RIM.circle, { x: px, y: py, R: r, u: .9 }, { lv: lit || ch > 0 ? .9 : .38, hov, rim: 1, t, sd, flare: ch, shut: sh });
        const k = cand.draw(c, ICONS[id], { x: px, y: py, R: (set === 'drone' ? WHEEL.pic : WHEEL.hand) * grow, u: 1.25 * grow }, { lv: lit || ch > 0 ? 1 : .6, hov, flare: ch, shut: sh, t, sd });
        if (sh) cand.draw(c, RIM.cross, { x: px, y: py, R: r, u: .9 }, { lv: .5, rim: 1, t, sd });
        see(T, `${set}:${id}:${sh ? 'shut' : ch > 0 ? 'chosen' : lit ? 'hover' : 'idle'}`, k);
      }
      c.globalAlpha = 1;
      if (ws.ptr) drawCursor(c, x + ws.ptr[0], y + ws.ptr[1]);
    }
    const toward = (i, n) => { const a = i * TAU / n; return [Math.sin(a) * 30, -Math.cos(a) * 30]; };
    const MID = [2, 3];
    // let go: the picked side flares where it was, the rest of the wheel gone in 0.2 s
    function release(d, i, cand, shut, ptr) { const f = 1 - d / cand.flareS; return { vis: 1 - c01(d / .2), ptr, shut, chosen: f > 0 ? { i, f } : null }; }
    // the chosen panel: held toward a side, let go; each round the next side
    function chosenCycle(u, n, cand, order) {
      const L = 2.6, q = u % L, tr = 1.3, i = order[Math.floor(u / L) % order.length], P = toward(i, n);
      if (q < tr) return { vis: c01(q / .12), ptr: lerp2(MID, P, sm(c01((q - .35) / .25))) };
      return release(q - tr, i, cand, null, P);
    }
    // the wheel's loupe: open, the lamp, the scan, let go; open again with the scan not known, on it, the lamp, let go
    function loupeWheel(u, cand) {
      const q = u % 8.4, UP = [0, -30], DN = [0, 30], mv = (a, b, t0) => lerp2(a, b, sm(c01((q - t0) / .25)));
      if (q < 3.4) return { ws: { vis: c01(q / .12), ptr: q < 1 ? MID : q < 2.2 ? mv(MID, UP, 1) : mv(UP, DN, 2.2) }, say: q < 1 ? 'колесо открылось, указатель в середине' : q < 2.2 ? 'наведено: фонарь' : 'наведено: скан' };
      if (q < 4.6) return { ws: release(q - 3.4, 1, cand, null, DN), say: 'отпустил F: выбран скан' };
      if (q < 7.6) return { ws: { vis: c01((q - 4.6) / .12), shut: [false, true], ptr: q < 5.6 ? MID : q < 7 ? mv(MID, DN, 5.6) : mv(DN, UP, 7) }, say: q < 5.6 ? 'скан ещё не изучен' : q < 7 ? 'на скане: не изучен — не горит и не выбирается' : 'наведено: фонарь' };
      return { ws: release(q - 7.6, 0, cand, [false, true], UP), say: 'отпустил F: выбран фонарь' };
    }
    const KL = [['rest', 1.2], ['near', 1.2], ['hover', 1.2], ['held', .8], ['on', 1.6], ['wait', 1.6]], KLL = KL.reduce((a, b) => a + b[1], 0);
    function loupeKey(u) { let q = u % KLL; for (const [s, d] of KL) { if (q < d) return s; q -= d; } return 'rest'; }

    function drawKey(T, x, y, id, state, num) {
      const c = T.c, cand = T.cand, t = T.time, b = LEVEL[state], held = state === 'held' ? 1 : 0, on = state === 'on' ? 1 : 0, wait = state === 'wait' ? 1 : 0;
      const sunk = held, cx = x + KEY.side / 2, cy = y + KEY.side / 2 + sunk, hov = state === 'hover' ? 1 : 0, sd = KEYS.indexOf(id) * 2.3 + 1;
      rr(c, x + .5, y + .5 + sunk, KEY.side - 1, KEY.side - 1, 3);
      c.fillStyle = on ? css(deep(.3), .7) : css(deep(.1), Math.min(.75, .25 + .5 * b)); c.fill();
      cand.draw(c, RIM.square, { x: cx, y: cy, R: KEY.side / 2 - .5, u: .7 }, { lv: b * .5, hov, rim: 1, t, sd, on, held });
      const k = cand.draw(c, ICONS[id], { x: cx, y: cy, R: KEY.icon, u: 1 }, { lv: b, hov, held, on, wait, t, sd });
      if (num != null) {
        const base = y + KEY.side + 5 + sunk, wd = 15;
        c.fillStyle = 'rgba(4,6,9,.85)'; c.fillRect(cx - wd / 2, base - 12, wd, 14);
        c.strokeStyle = css(AC(), .55 * b); c.lineWidth = 1; c.strokeRect(cx - wd / 2 + .5, base - 11.5, wd - 1, 13);
        c.font = '600 13px ui-monospace,Consolas,monospace'; c.textAlign = 'center'; c.textBaseline = 'alphabetic';
        c.fillStyle = css(hot(.45), b); c.fillText(String(num), cx, base); c.textAlign = 'left';
      }
      see(T, `key:${id}:${state}`, k);
    }

    // ---------- the tile ----------
    const LX = 830, LWID = 355, WL = { y: 10, h: 332, k: 2 }, KLB = { y: 352, h: 326, k: 4 };
    const DRONE_X = [95, 255, 415, 575, 735], GEST_X = DRONE_X, GX = [20, 285, 550], GY = [478, 574];
    function text(c, s, x, y, size, a, align) { c.font = `${size}px system-ui,"Segoe UI",sans-serif`; c.textAlign = align || 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = `rgba(206,212,222,${a})`; c.fillText(s, x, y); c.textAlign = 'left'; }
    function box(c, x, y, w, h) { rr(c, x, y, w, h, 8); c.fillStyle = '#020306'; c.fill(); c.strokeStyle = 'rgba(140,150,165,.35)'; c.lineWidth = 1; c.stroke(); }
    function scene(T) {
      const c = T.c, u = T.time, cand = T.cand;
      T.seenNow = new Map();
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      c.fillStyle = '#05070b'; c.fillRect(0, 0, LW, LH);

      text(c, 'Колесо дрона — держать F · размер игры', 20, 24, 14, .85);
      const dronePanels = [
        [{ vis: 1, ptr: MID }, 'покой'],
        [{ vis: 1, ptr: toward(0, 2) }, 'наведено: фонарь'],
        [{ vis: 1, ptr: toward(1, 2) }, 'наведено: скан'],
        [chosenCycle(u, 2, cand, [1, 0]), 'выбор (отпустил F)'],
        [{ vis: 1, ptr: toward(1, 2), shut: [false, true] }, 'скан ещё не изучен'],
      ];
      dronePanels.forEach(([ws, say], i) => { drawWheel(T, DRONE_X[i], 122, 'drone', ws); text(c, say, DRONE_X[i], 224, 12, .6, 'center'); });

      text(c, 'Колесо жестов — держать H · тот же вид', 20, 252, 14, .85);
      const gestPanels = [
        [{ vis: 1, ptr: MID }, 'покой'],
        [{ vis: 1, ptr: toward(0, 3) }, 'наведено: «да»'],
        [{ vis: 1, ptr: toward(1, 3) }, 'наведено: «нет»'],
        [{ vis: 1, ptr: toward(2, 3) }, 'наведено: «помахать»'],
        [chosenCycle(u, 3, cand, [2, 0, 1]), 'выбор (отпустил H)'],
      ];
      gestPanels.forEach(([ws, say], i) => { drawWheel(T, GEST_X[i], 340, 'gesture', ws); text(c, say, GEST_X[i], 424, 12, .6, 'center'); });

      text(c, 'Кнопки у края, 28 px: Воздух · Стройка · Планшет · Рынок · Костюм · Справочник', 20, 462, 14, .85);
      GROUPS.forEach(([state, say], gi) => {
        const x0 = GX[gi % 3], y0 = GY[gi / 3 | 0];
        KEYS.forEach((id, j) => {
          const s = state === 'wait' && !WAITS.includes(id) ? 'near' : state;
          drawKey(T, x0 + j * 34, y0, id, s, s === 'wait' && id === 'research' ? 3 : null);
        });
        text(c, say, x0, y0 + 52, 12, .6);
      });

      // the wheel's loupe ×2
      const lw = loupeWheel(u, cand);
      box(c, LX, WL.y, LWID, WL.h);
      c.save(); rr(c, LX, WL.y, LWID, WL.h, 8); c.clip();
      c.translate(LX + LWID / 2, WL.y + 158); c.scale(WL.k, WL.k); drawWheel(T, 0, 0, 'drone', lw.ws);
      c.restore();
      text(c, `лупа ×2 · ${lw.say}`, LX + 10, WL.y + WL.h - 8, 12, .6);
      see(T, 'loupe:wheel', 1);

      // the keys' loupe ×4: the air, which never waits, and the tablet, with its number
      const ks = loupeKey(u);
      box(c, LX, KLB.y, LWID, KLB.h);
      c.save(); rr(c, LX, KLB.y, LWID, KLB.h, 8); c.clip();
      c.translate(LX + LWID / 2 - 34 * KLB.k, KLB.y + 40); c.scale(KLB.k, KLB.k);
      drawKey(T, 0, 0, 'air', ks === 'wait' ? 'near' : ks, null);
      drawKey(T, 40, 0, 'research', ks, ks === 'wait' ? 3 : null);
      c.restore();
      const say = { rest: 'покой (тускло)', near: 'указатель у края', hover: 'под указателем', held: 'нажата', on: 'включена', wait: 'ждёт: Планшет с числом; Воздух не ждёт' }[ks];
      text(c, `лупа ×4 · Воздух и Планшет · ${say}`, LX + 10, KLB.y + KLB.h - 10, 12, .6);
      see(T, 'loupe:key', 1);

      if (OPT.slow) text(c, '×¼', LW - 40, LH - 10, 12, .8);
    }

    return {
      cands, expose: { MODES, GESTURES, KEYS, WAITS, HUES, GROUPS: GROUPS.map(g => g[0]) },
      init(T) { T.seenNow = new Map(); },
      scene,
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    key: [["const KEYS = ['air', 'build', 'research', 'market', 'suit', 'journal'];", "const KEYS = ['air', 'build', 'research', 'suit', 'journal'];"]],
    state: [["['held', 'нажата']", "['hover', 'нажата']"]],
    hue: [['c.fillStyle = css(hot(.45), b);', 'c.fillStyle = css(pale(EMBER, .45), b);']],
    blind: [['const k = cand.draw(c, ICONS[id], { x: cx, y: cy, R: KEY.icon', 'const k = 0 * cand.draw(c, ICONS[id], { x: cx, y: cy, R: KEY.icon']],
    save: [['"saved":false', '"saved":true,"short":"оттенок"']],
  },

  // every mode, gesture and key in every state on every candidate, each one lit; every chromatic colour drawn in the chosen hue under
  // each of the four; a pause; the pick saved without the hue
  check(K) {
    const X = K.X;
    const WANT = { modes: ['lamp', 'scan'], gestures: ['yes', 'no', 'wave'], keys: ['air', 'build', 'research', 'market', 'suit', 'journal'], waits: ['research', 'market'] };
    const want = [];
    for (const m of WANT.modes) for (const s of ['idle', 'hover', 'chosen']) want.push(`drone:${m}:${s}`);
    want.push('drone:scan:shut');
    for (const g of WANT.gestures) for (const s of ['idle', 'hover', 'chosen']) want.push(`gesture:${g}:${s}`);
    for (const k of WANT.keys) for (const s of ['rest', 'near', 'hover', 'held', 'on']) want.push(`key:${k}:${s}`);
    for (const k of WANT.waits) want.push(`key:${k}:wait`);
    want.push('loupe:wheel', 'loupe:key');
    const still = want.filter(k => !k.endsWith(':chosen'));

    // every colour a tile sets, through a recorder on its canvas: a chromatic one must be the hue chosen now
    const hueOf = (r, g, b) => { const mx = Math.max(r, g, b), mn = Math.min(r, g, b), d = mx - mn; if (d < 30) return null; const h = mx === r ? ((g - b) / d + 6) % 6 : mx === g ? (b - r) / d + 2 : (r - g) / d + 4; return h * 60; };
    const parse = s => { let m = /^rgba?\((\d+),(\d+),(\d+)(?:,([\d.]+))?\)$/.exec(s.replace(/\s/g, '')); if (m) return [+m[1], +m[2], +m[3], m[4] == null ? 1 : +m[4]]; m = /^#([0-9a-f]{6})$/i.exec(s); if (m) { const v = parseInt(m[1], 16); return [v >> 16, (v >> 8) & 255, v & 255, 1]; } return null; };
    const rec = K.tiles.map(() => ({ n: {}, bad: 0, ex: '', calls: 0 }));
    K.tiles.forEach((t, j) => {
      const take = v => {
        if (typeof v !== 'string') return;
        const q = parse(v); if (!q || q[3] <= .02) return;
        const h = hueOf(q[0], q[1], q[2]); if (h == null) return;
        const hue = K.OPT.hue, w = hueOf(...X.HUES[hue].rgb), d = Math.abs(((h - w + 540) % 360) - 180), r = rec[j];
        r.n[hue] = (r.n[hue] || 0) + 1;
        if (d > 10) { r.bad++; if (!r.ex) r.ex = `${v} under ${hue}`; }
      };
      const real = t.c;
      t.c = new Proxy(real, {
        get(o, k) {
          if (k === 'createLinearGradient' || k === 'createRadialGradient') return (...a) => { const gr = o[k](...a); return { addColorStop(s, col) { take(col); gr.addColorStop(s, col); } }; };
          const v = o[k]; if (typeof v === 'function') rec[j].calls++; return v;
        },
        set(o, k, v) { if (k === 'fillStyle' || k === 'strokeStyle' || k === 'shadowColor') take(v); o[k] = v; return true; },
      });
    });

    const SEG = [['ember', 0, 600], ['cyan', 600, 780], ['green', 780, 960], ['violet', 960, 1140]];
    K.at(600, () => K.seg('hueMode', 1)); K.at(780, () => K.seg('hueMode', 2)); K.at(960, () => K.seg('hueMode', 3)); K.at(1140, () => K.seg('hueMode', 0));
    K.at(1180, () => K.seg('playMode', 1));
    K.at(1220, () => { K.seg('playMode', 0); K.seg('speedMode', 1); });
    K.at(1260, () => K.seg('speedMode', 0));
    let st1 = '', doc1 = null, doc2 = null, fb = '';
    K.at(1280, () => K.click(2, '.pick'));
    K.at(1290, () => { st1 = K.byId.status.textContent; doc1 = K.picked(); K.byId.copy.listeners.click[0](); fb = K.byId.fallback.value; });
    K.at(1295, () => K.seg('hueMode', 2));
    K.at(1300, () => { doc2 = K.picked(); });
    const all = K.tiles.map(() => new Map()), segSeen = K.tiles.map(() => SEG.map(() => new Set())), paused = K.tiles.map(() => ({ n: 0, ran: 0, last: -1 }));
    K.run(1310, (t, i) => {
      const j = K.tiles.indexOf(t);
      for (const [k, n] of t.seenNow) { all[j].set(k, Math.max(all[j].get(k) || 0, n)); SEG.forEach(([, a, b], s) => { if (i >= a && i < b) segSeen[j][s].add(k); }); }
      const p = paused[j];
      if (i > 1181 && i < 1219) { p.n++; if (p.last >= 0 && t.time !== p.last) p.ran++; }
      p.last = t.time;
    });

    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    const same = (a, b) => JSON.stringify(a) === JSON.stringify(b);
    need(same(X.MODES, WANT.modes), `the drone wheel has the game's modes, lamp then scan (DroneModes.Sides): ${X.MODES}`);
    need(same(X.GESTURES, WANT.gestures), `the gestures' wheel has the game's sides (Gestures.WheelSides): ${X.GESTURES}`);
    need(same(X.KEYS, WANT.keys), `the edge has the game's six keys in Main.cs's order: ${X.KEYS}`);
    need(same(X.WAITS, WANT.waits), 'research and market are the keys that wait');
    need(same(X.GROUPS, ['rest', 'near', 'hover', 'held', 'on', 'wait']), `the key groups are every EdgeKey state: ${X.GROUPS}`);
    need(same(Object.keys(X.HUES), ['ember', 'cyan', 'green', 'violet']), 'the hues are ember, cyan, green, violet');
    K.tiles.forEach((t, j) => {
      const id = t.cand.id, miss = want.filter(k => !all[j].has(k)), dark = want.filter(k => all[j].has(k) && !(all[j].get(k) > 0));
      const segMiss = SEG.map((s, si) => still.filter(k => !segSeen[j][si].has(k)).length + ([...segSeen[j][si]].some(k => k.endsWith(':chosen')) ? 0 : 1));
      const r = rec[j];
      console.log(`${id}: shown ${want.length - miss.length}/${want.length} | unlit ${dark.length} | per hue missing ${segMiss.join(' ')} | colours ${SEG.map(s => `${s[0]} ${r.n[s[0]] || 0}`).join(', ')} | off-hue ${r.bad}${r.ex ? ' e.g. ' + r.ex : ''} | paused ${paused[j].n} ran ${paused[j].ran} | ${Math.round(r.calls / 1310)} canvas calls a frame`);
      need(!miss.length, `${id}: every mode, gesture and key in every state is drawn${miss.length ? ' — missing ' + miss.slice(0, 6).join(' ') : ''}`);
      need(!dark.length, `${id}: every one of them lights something${dark.length ? ' — dark ' + dark.slice(0, 6).join(' ') : ''}`);
      need(segMiss.every(v => v === 0), `${id}: all of it is shown again under each hue`);
      need(SEG.every(s => (r.n[s[0]] || 0) > 2000), `${id}: plenty of coloured strokes under every hue`);
      need(r.bad === 0, `${id}: every coloured stroke is the hue chosen, nothing kept its old colour`);
      need(paused[j].n > 30 && paused[j].ran === 0, `${id}: the pause holds`);
    });
    const cand = K.tiles[2].cand;
    console.log('after the pick:', st1, '| doc', JSON.stringify(doc1), '| copy:', fb, '| after a hue change:', JSON.stringify(doc2));
    need(doc1 && same(Object.keys(doc1), ['pick']) && doc1.pick === cand.id, 'the db doc holds the pick alone, not the hue');
    need(doc2 && same(doc2, doc1), 'changing the hue leaves the pick as it was');
    need(st1.includes(`«${cand.name}»`) && !/оттен/.test(st1), 'the status names the pick and not the hue');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the copy text names the pick');
    return ok;
  },
};
