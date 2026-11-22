// How a drone window (the bag, the plan, a machine's window) opens from the light drone «Нити»: the screen readable at once,
// the drone before the hero's waist projecting it with its own ember sparks. An art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-windows.js <out-dir> [--break slow|skip|stuck|waist|dark]
module.exports = {
  title: 'Окна дрона',
  heading: 'Как дрон открывает окно: сумка, план, окно машины',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Окно дрона открывается почти сразу: экран читается за <b>0,08–0,15 с</b>, а не за секунду, как сейчас, когда искры сперва выкладывают рамку. Пока окно открыто, дрон «Нити» слетает с плеча к поясу героя (за 0,22 с; место из игры: 16 px мира впереди, 12 над ступнями) и проецирует окно своими угольными искрами — как раньше голограмма, только угли вместо голубых точек. Здесь пять способов этой проекции и того, как с ней связана рамка. Каждая плитка по кругу открывает и закрывает три окна: <b>сумку</b>, <b>план постройки</b> и <b>окно машины</b>, по 3,6 с на окно: закрыто 0,7 с → открыто 2,3 с → закрывается, дрон возвращается к плечу за 0,35 с.</p>
<p class="lead">Масштаб игры: клетка 8 px мира, вид ×4, герой ~42 px мира. Цвет проекции — уголь нитей; внутри окон — тёплый экран-заглушка. В лупе справа вверху — дрон у пояса ×2,5. Над каждой плиткой — её тайминги; «Медленно ×¼» показывает открытие по кадрам.</p>
<p class="miss">Не показано: звук; что видят друзья; отрисовка Godot (свет и цвета выйдут чуть иначе); что внутри окон — заглушки; окно, пока герой бежит; другие окна дрона (справочник, доска, рынок) — проекция у них та же.</p>`,
  toggles: [
    { id: 'showMode', bar: 1, label: 'Окно:', key: 'show', opts: [['все три по очереди', 'all'], ['сумка', 'bag'], ['план', 'plan'], ['машина', 'machine']] },
    { id: 'loupeMode', label: 'Лупа:', key: 'loupe', opts: [['есть', true], ['нет', false]] },
  ],
  pick: { doc: 'picks/droneWindows', subject: 'Окна дрона', what: ['проекцию', 'проекцией'] },

  page() {
    const TAU = Math.PI * 2;
    // ---------- the loop: a slot per window, shut, opened at .7 s, closed at 3 s ----------
    const ORDER = ['bag', 'plan', 'machine'];
    const SL = { open: .7, close: 3, len: 3.6 };
    const FLY = { in: .22, out: .35 };     // the drone from the shoulder to the waist, and back
    const WAIST = [16, -12];               // SheetHoloLook: OverAhead past the hero's middle, OverUp over its feet, world px
    const HX = 760, LOUPE_K = 2.5;
    const WINS = {
      bag: { id: 'bag', name: 'сумка', r: { x: 196, y: 66, w: 404, h: 218 } },
      plan: { id: 'plan', name: 'план', r: { x: 236, y: 44, w: 354, h: 276 } },
      machine: { id: 'machine', name: 'окно машины', r: { x: 250, y: 94, w: 340, h: 192 } },
    };
    const LOOP = ORDER.length * SL.len;

    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const noise = (t, s) => c01(.5 + .3 * Math.sin(t * 31.7 + s) + .22 * Math.sin(t * 13.1 + s * 2.3) + .18 * Math.sin(t * 77.3 + s * .7));
    const unit = (a, b) => { const dx = b[0] - a[0], dy = b[1] - a[1], d = Math.hypot(dx, dy) || 1; return [dx / d, dy / d]; };
    const P170 = DR.P, NP = DR.P.length;

    // ---------- the threads' look: a spark with its tail, a glowing square, a thread ----------
    function glowSq(g, x, y, s, h) { if (h <= .02) return; h = Math.min(1, h); g.fillStyle = css(pale(EMBER, .25 * h), .95 * h); g.fillRect(x - s / 2, y - s / 2, s, s); }
    function spark(c, g, p, h, s = .95, a = 1) {
      const dx = p[2] || 0, dy = p[3] || 0;
      if (a > .003) { c.fillStyle = css([38, 39, 43], a); c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s); }
      if (h <= .02) return; h = Math.min(1, h);
      for (let m = 0; m < 3; m++) { const b = m * 1.3; g.fillStyle = css(pale(EMBER, .3 * (1 - m / 3)), (.95 - m * .3) * h); g.fillRect(p[0] - dx * b - s / 2, p[1] - dy * b - s / 2, s, s); }
    }
    function line2(g, a, b, w, al) { if (al <= .003) return; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.lineWidth = w; g.strokeStyle = css(EMBER, al); g.stroke(); }
    const along = (a, b, e) => { const u = unit(a, b); return [lerp(a[0], b[0], e), lerp(a[1], b[1], e), u[0], u[1]]; };

    // ---------- the drone «Нити»: the coal with its ember eye, three orbit threads, sparks running round them ----------
    function coal(c, g, X) {
      const x = X.D[0], y = X.D[1], s = DS * DSC, A = X.A, fl = .75 + .25 * Math.sin(X.t * 7.3);
      halo(g, x, y, 8, EMBER, .35);
      c.save(); c.translate(x, y); c.scale(s, s); paint(c, DR.core, A, DARK, .8, 2.6); ink(c, DR.core, A, 1.7); c.restore();
      g.strokeStyle = css(EMBER, .5 * fl); g.lineWidth = .45; g.beginPath();
      for (const [a, b] of [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]]) { g.moveTo(x + Math.cos(A + a) * .6, y + Math.sin(A + a) * .6); g.lineTo(x + Math.cos(A + b * .5) * 2.6, y + Math.sin(A + b * .5) * 2.6); }
      g.stroke();
      const e = eyeOf(X);
      g.fillStyle = css(pale(EMBER, .5), .9 * fl); g.beginPath(); g.arc(e[0], e[1], 1.2, 0, TAU); g.fill();
      halo(g, e[0], e[1], 4, EMBER, .5 * fl);
    }
    const eyeOf = X => [X.D[0] + Math.cos(X.A) * 2.2 * DS * DSC, X.D[1] + Math.sin(X.A) * 2.2 * DS * DSC];
    function orbit(i, X) {
      const P = P170[i % NP], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + X.t * (.8 + .25 * o) * dir * 2.2, rx = (6.3 + o * 1.4 + (P.r1 - .5)) * 1.05, tl = -.3 + o * 1.05;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tl), v = rot(-Math.sin(th) * dir, Math.cos(th) * .3 * dir, tl), vl = Math.hypot(v[0], v[1]) || 1;
      return [X.D[0] + q[0], X.D[1] + q[1], v[0] / vl, v[1] / vl, Math.sin(th)];
    }
    const CH = {};
    function chosen(n, share) {
      const key = n + ':' + share; if (CH[key]) return CH[key];
      const rank = []; let m = 0;
      for (let i = 0; i < n; i++) rank.push(hsh(i * 3 + 1) < share ? m++ : -1);
      return (CH[key] = { rank, m });
    }
    // the body: a candidate may take a share of the sparks off the orbits into a shape of its own (the disc, the lens); returns how
    // many sparks are in that shape, as part of the projection
    function drawDrone(c, g, cand, X, W) {
      const n = 120, own = W && cand.body ? cand.body(X, W) : null, pick = own && own.form > 0 ? chosen(n, own.share) : null, list = [];
      let shaped = 0;
      for (let i = 0; i < n; i++) {
        const I = orbit(i, X);
        let p = I, z = I[4], h = hsh(i + 3) < .5 ? .55 + .3 * Math.sin(X.t * 11 + i * 7) : .2;
        const j = pick ? pick.rank[i] : -1;
        if (j >= 0) {
          const S = own.at((j + .5) / pick.m), q = sm(c01((own.form - hsh(i + 50) * .3) / .7));
          if (q > 0) {
            p = [lerp(I[0], S[0], q), lerp(I[1], S[1], q), q > .5 ? S[2] : I[2], q > .5 ? S[3] : I[3]];
            if (q > .5) { z = 1; shaped++; }
            h = lerp(h, .8 + .15 * Math.sin(X.t * 13 + i), q);
          }
        }
        list.push([p, z, h]);
      }
      const keep = own ? 1 - own.form * own.hide : 1;
      g.lineWidth = .4; g.strokeStyle = css(EMBER, .12 * keep);
      for (let o = 0; o < 3; o++) { const rx = (6.3 + o * 1.4) * 1.05; g.beginPath(); g.ellipse(X.D[0], X.D[1], rx, rx * .3, -.3 + o * 1.05, 0, TAU); g.stroke(); }
      if (own && own.form > 0) own.under(g);
      for (const e of list) if (e[1] < 0) spark(c, g, e[0], e[2]);
      coal(c, g, X);
      for (const e of list) if (e[1] >= 0) spark(c, g, e[0], e[2]);
      return shaped;
    }

    // ---------- a window: where it is, its shimmering frame of ember squares, the screen inside ----------
    function frameOf(w) {
      if (w.FR) return w.FR;
      const sq = [], r = w.r;
      for (const [row, o] of [[0, 3], [1, 6.4]]) {
        const x0 = r.x - o, y0 = r.y - o, ww = r.w + 2 * o, hh = r.h + 2 * o, per = 2 * (ww + hh), n = Math.round(per / 3.4);
        for (let j = 0; j < n; j++) { let d = j / n * per, p; if (d < ww) p = [x0 + d, y0]; else if ((d -= ww) < hh) p = [x0 + ww, y0 + d]; else if ((d -= hh) < ww) p = [x0 + ww - d, y0 + hh]; else { d -= ww; p = [x0, y0 + hh - d]; } sq.push({ p, per: j / n, row }); }
      }
      return (w.FR = sq);
    }
    // the frame's squares at alpha a; mod(sq, j) gives a square its own place and alpha
    function drawFrame(c, g, X, W, a, mod) {
      if (a <= .003 && !mod) return;
      W.FR.forEach((sq, j) => {
        let p = sq.p, al = a;
        if (mod) { const m = mod(sq, j); if (!m) return; p = m[0]; al = m[1]; }
        if (al <= .003) return;
        c.fillStyle = css([38, 39, 43], al); c.fillRect(p[0] - .9, p[1] - .9, 1.8, 1.8);
        glowSq(g, p[0], p[1], 1.6, al * (.14 + .7 * Math.pow(.5 + .5 * Math.sin(sq.per * TAU * 6 - X.t * 4 + sq.row), 6) + .1 * noise(X.t * .4, j)));
      });
    }
    // a corner of the window grown by o (0 top left, clockwise), and the point s of the way round from corner k
    const cornerAt = (r, o, k) => [[r.x - o, r.y - o], [r.x + r.w + o, r.y - o], [r.x + r.w + o, r.y + r.h + o], [r.x - o, r.y + r.h + o]][k];
    function around(r, o, k, s) {
      const L = [r.w + 2 * o, r.h + 2 * o, r.w + 2 * o, r.h + 2 * o], per = 2 * (L[0] + L[1]);
      let d = (((s % 1) + 1) % 1) * per;
      for (let m = 0; m < 4; m++) {
        const i = (k + m) % 4;
        if (d <= L[i] || m === 3) { const a = cornerAt(r, o, i), b = cornerAt(r, o, (i + 1) % 4); return along(a, b, Math.min(1, d / L[i])); }
        d -= L[i];
      }
    }
    // the window seen from the eye: its silhouette corners, the nearest, and a point v of the way along its near edges
    function winCtx(st, X) {
      const w = WINS[st.id], r = w.r, E = eyeOf(X), C = [0, 1, 2, 3].map(k => cornerAt(r, 0, k)), mid = [r.x + r.w / 2, r.y + r.h / 2];
      const th = Math.atan2(mid[1] - E[1], mid[0] - E[0]);
      let lo = 0, hi = 0, dl = 9, dh = -9, near = 0, dn = 1e9;
      C.forEach((p, i) => {
        let d = Math.atan2(p[1] - E[1], p[0] - E[0]) - th;
        while (d > Math.PI) d -= TAU; while (d < -Math.PI) d += TAU;
        if (d < dl) { dl = d; lo = i; } if (d > dh) { dh = d; hi = i; }
        const q = Math.hypot(p[0] - E[0], p[1] - E[1]); if (q < dn) { dn = q; near = i; }
      });
      return { ...st, w, r, C, E, mid, sil: [C[lo], C[hi]], near, FR: frameOf(w) };
    }
    function edgePt(W, v) {
      const a = W.sil[0], n = W.C[W.near], b = W.sil[1], l1 = Math.hypot(n[0] - a[0], n[1] - a[1]), l2 = Math.hypot(b[0] - n[0], b[1] - n[1]), d = v * (l1 + l2);
      return d < l1 ? lerp2(a, n, d / l1) : lerp2(n, b, l2 > 1e-6 ? (d - l1) / l2 : 0);
    }
    // a fill from the eye's side up to the window, never over the screen
    function outsideWin(g, r) { g.beginPath(); g.rect(0, 0, LW, LH); g.rect(r.x, r.y, r.w, r.h); g.clip('evenodd'); }

    // ---------- the screens inside: placeholders, warm electronic screens ----------
    const TXT = css(pale(EMBER, .5), .92), DIM = css(EMBER, .5), LINE = css(EMBER, .3), CELL = 'rgba(64,32,15,.55)', FONT = 'system-ui,"Segoe UI",sans-serif';
    function title(c, r, a, b) {
      c.textBaseline = 'alphabetic'; c.textAlign = 'left'; c.font = `600 12px ${FONT}`; c.fillStyle = TXT; c.fillText(a, r.x + 12, r.y + 19);
      c.textAlign = 'right'; c.font = `11px ${FONT}`; c.fillStyle = DIM; c.fillText(b, r.x + r.w - 12, r.y + 19); c.textAlign = 'left';
    }
    function foot(c, r, s) { c.font = `11px ${FONT}`; c.fillStyle = DIM; c.textAlign = 'left'; c.fillText(s, r.x + 12, r.y + r.h - 9); }
    function thing(c, x, y, kind, n) {
      c.fillStyle = '#5d5550'; c.strokeStyle = 'rgba(20,14,10,.9)'; c.lineWidth = 1;
      if (kind === 0) {
        c.beginPath(); c.moveTo(x - 9, y + 3); c.lineTo(x - 5, y - 7); c.lineTo(x + 4, y - 8); c.lineTo(x + 9, y - 1); c.lineTo(x + 6, y + 7); c.lineTo(x - 4, y + 8); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = css(EMBER, .55); c.fillRect(x - 2, y - 3, 3, 3); c.fillRect(x + 3, y + 2, 2, 2);
      } else if (kind === 1) {
        c.beginPath(); c.moveTo(x - 10, y + 6); c.lineTo(x - 6, y - 4); c.lineTo(x + 6, y - 4); c.lineTo(x + 10, y + 6); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = '#857a70'; c.fillRect(x - 5, y - 3, 10, 2);
      } else if (kind === 2) { rr(c, x - 11, y - 4, 22, 8, 2); c.fill(); c.stroke(); c.fillStyle = '#857a70'; c.fillRect(x - 10, y - 3, 20, 2); }
      else { c.beginPath(); c.arc(x, y, 8, 0, TAU); c.fill(); c.stroke(); c.beginPath(); c.arc(x, y, 3, 0, TAU); c.fillStyle = '#0d0805'; c.fill(); }
      if (n > 1) { c.font = '600 10px ui-monospace,Consolas,monospace'; c.textAlign = 'right'; c.fillStyle = TXT; c.fillText(String(n), x + 16, y + 16); c.textAlign = 'left'; }
    }
    function slot(c, x, y, on) { rr(c, x + .5, y + .5, 36, 36, 3); c.fillStyle = CELL; c.fill(); c.strokeStyle = on ? css(pale(EMBER, .3), .85) : LINE; c.lineWidth = 1; c.stroke(); }
    function gauge(c, x, y, w, f) { rr(c, x + .5, y + .5, w, 8, 2); c.strokeStyle = LINE; c.lineWidth = 1; c.stroke(); c.fillStyle = css(EMBER, .7); c.fillRect(x + 2, y + 2, Math.max(0, (w - 3) * f), 5); }
    const BAG_IN = { 0: [0, 24], 1: [1, 6], 2: [2, 3], 4: [3, 1], 9: [0, 8], 11: [1, 12], 13: [2, 40], 20: [3, 2], 27: [0, 5] };
    const INSIDE = {
      bag(c, r) {
        title(c, r, 'СУМКА', '9 / 35');
        for (let q = 0; q < 36; q++) { const x = r.x + 22 + (q % 9) * 40, y = r.y + 30 + (q / 9 | 0) * 40; slot(c, x, y, q === 11); const it = BAG_IN[q]; if (it) thing(c, x + 18, y + 18, it[0], it[1]); }
        foot(c, r, 'Tab — закрыть · ПКМ — разделить стопку');
      },
      plan(c, r) {
        title(c, r, 'ПОСТРОЙКА', 'чертежи');
        ['Стена', 'Дверь', 'Труба', 'Насос', 'Печь', 'Верстак', 'Лампа', 'Бак'].forEach((s, i) => {
          const y = r.y + 30 + i * 28, on = i === 4;
          if (on) { rr(c, r.x + 8, y, 160, 24, 3); c.fillStyle = 'rgba(255,138,58,.14)'; c.fill(); c.strokeStyle = css(EMBER, .5); c.lineWidth = 1; c.stroke(); }
          rr(c, r.x + 14.5, y + 5.5, 14, 14, 2); c.strokeStyle = LINE; c.lineWidth = 1; c.stroke();
          c.font = `12px ${FONT}`; c.fillStyle = on ? TXT : DIM; c.fillText(s, r.x + 36, y + 16);
        });
        const px = r.x + 180, py = r.y + 30;
        rr(c, px + .5, py + .5, 160, 120, 3); c.fillStyle = CELL; c.fill(); c.strokeStyle = LINE; c.stroke();
        c.strokeStyle = css(EMBER, .08); c.beginPath();
        for (let x = px + 10; x < px + 160; x += 10) { c.moveTo(x, py + 2); c.lineTo(x, py + 118); }
        for (let y = py + 10; y < py + 120; y += 10) { c.moveTo(px + 2, y); c.lineTo(px + 158, y); }
        c.stroke();
        c.strokeStyle = css(pale(EMBER, .3), .75); c.lineWidth = 1.2;
        c.strokeRect(px + 50.5, py + 30.5, 60, 70); c.strokeRect(px + 62.5, py + 62.5, 36, 26); c.strokeRect(px + 90.5, py + 12.5, 12, 18); c.lineWidth = 1;
        c.font = `600 12px ${FONT}`; c.fillStyle = TXT; c.fillText('Печь', px, py + 142);
        c.font = `11px ${FONT}`; c.fillStyle = DIM; c.fillText('железо ×6 · камень ×10', px, py + 160); c.fillText('место: 3×4 клетки', px, py + 176);
        foot(c, r, 'ЛКМ — поставить · Q — закрыть');
      },
      machine(c, r, t) {
        title(c, r, 'ПЕЧЬ', 'плавит');
        const x = r.x + 24, y = r.y + 40;
        slot(c, x, y, false); thing(c, x + 18, y + 18, 0, 12);
        gauge(c, x + 56, y + 14, 150, (t * .4) % 1);
        c.fillStyle = css(EMBER, .6); c.beginPath(); c.moveTo(x + 210, y + 12); c.lineTo(x + 216, y + 18); c.lineTo(x + 210, y + 24); c.fill();
        slot(c, x + 222, y, false); thing(c, x + 240, y + 18, 1, 3);
        c.font = `11px ${FONT}`; c.fillStyle = DIM; c.fillText('топливо', x, y + 78); c.fillText('жар', x, y + 100);
        gauge(c, x + 64, y + 70, 180, .6); gauge(c, x + 64, y + 92, 180, .85);
        c.textAlign = 'right'; c.fillStyle = TXT; c.fillText('840°', r.x + r.w - 12, y + 100); c.textAlign = 'left';
        foot(c, r, 'E — закрыть');
      },
    };
    // the screen at alpha a, s of its size about O
    function screenIn(c, W, a, s, O, t) {
      if (a <= .003) return;
      const r = W.r;
      c.save(); c.globalAlpha = Math.min(1, a);
      if (s !== 1) { c.translate(O[0], O[1]); c.scale(s, s); c.translate(-O[0], -O[1]); }
      c.fillStyle = 'rgba(13,8,5,.93)'; c.fillRect(r.x, r.y, r.w, r.h);
      c.fillStyle = 'rgba(0,0,0,.2)'; for (let y = r.y + 1; y < r.y + r.h; y += 3) c.fillRect(r.x, y, r.w, 1);
      c.strokeStyle = css(EMBER, .16); c.lineWidth = 1; c.strokeRect(r.x + .5, r.y + .5, r.w - 1, r.h - 1);
      INSIDE[W.id](c, r, t);
      c.restore();
    }

    // ---------- the candidates: draw(c, g, X, W) draws the screen, the frame and the projection, returns { a, s, proj }; body(X, W)
    // takes a share of the drone's sparks into a shape { form, share, hide, at(s), under(g) } ----------
    const dirOf = X => [Math.cos(X.A), Math.sin(X.A)];
    function discOf(X, W) {
      const form = W.ph === 'open' ? sm(W.u / .12) : 1 - sm(W.uc / .15), d = dirOf(X), n = [-d[1], d[0]], O = [X.D[0] + d[0] * 3, X.D[1] + d[1] * 3], R = 9, Rt = 2.2;
      return {
        form, share: .7, hide: 1,
        at(s) { const th = s * TAU + X.t * 5, c = Math.cos(th), si = Math.sin(th), v = [-si * R * n[0] + c * Rt * d[0], -si * R * n[1] + c * Rt * d[1]], l = Math.hypot(v[0], v[1]) || 1; return [O[0] + c * R * n[0] + si * Rt * d[0], O[1] + c * R * n[1] + si * Rt * d[1], v[0] / l, v[1] / l]; },
        under(g) { for (const [w, a] of [[1.8, .07], [.45, .32]]) { g.beginPath(); g.ellipse(O[0], O[1], R, Rt, Math.atan2(n[1], n[0]), 0, TAU); g.lineWidth = w; g.strokeStyle = css(EMBER, a * form); g.stroke(); } },
      };
    }
    function lensOf(X) { const d = dirOf(X), n = [-d[1], d[0]]; return { d, n, L: [X.D[0] + d[0] * 16, X.D[1] + d[1] * 16], R: 8, Rt: 2.6 }; }
    const cands = [
      {
        id: 'beam', name: 'Луч',
        idea: 'Из глаза дрона бьёт широкий тёплый луч — конус света до ближних краёв окна, по нему непрерывно бегут искры, каждая к своей точке рамки. На открытии луч вспыхивает и оседает в слабое свечение. Рамка — тлеющие квадратики, как сейчас, но загорается сразу целиком. Закрываясь, луч втягивается обратно в глаз.',
        time: 'Вспышка луча 0,05 с · рамка за 0,08 с · экран читается через 0,13 с · искра летит по лучу 0,6 с · закрытие: экран гаснет за 0,08 с, луч втягивается за 0,15 с.',
        draw(c, g, X, W) {
          const { u, uc, E, r } = W, open = W.ph === 'open';
          const a = open ? c01((u - .04) / .1) : 1 - c01(uc / .08), fr = open ? c01((u - .02) / .08) : 1 - c01(uc / .1);
          const reach = open ? 1 : 1 - sm(uc / .15), glow = open ? .35 + .65 * Math.exp(-u / .08) * c01(u / .03) : .35 * reach;
          screenIn(c, W, a, 1, null, X.t);
          drawFrame(c, g, X, W, fr);
          let proj = 0;
          if (reach > .003) {
            const A = lerp2(E, W.sil[0], reach), B = lerp2(E, W.sil[1], reach);
            g.save(); outsideWin(g, r);
            const gr = g.createLinearGradient(E[0], E[1], W.mid[0], W.mid[1]); gr.addColorStop(0, css(EMBER, .22 * glow)); gr.addColorStop(1, css(EMBER, .03 * glow));
            g.fillStyle = gr; g.beginPath(); g.moveTo(E[0], E[1]); g.lineTo(A[0], A[1]); g.lineTo(B[0], B[1]); g.closePath(); g.fill();
            g.restore();
            line2(g, E, A, .5, .25 * glow); line2(g, E, B, .5, .25 * glow);
            for (let j = 0; j < 28; j++) {
              const life = (X.t / .6 + hsh(j + 60)) % 1;
              if (open && u < life * .6) continue;   // only sparks sent since the open
              const T0 = edgePt(W, hsh(j + 70)), p = along(E, T0, life * reach), h = .9 * (1 - c01((life - .85) / .15)) * (open ? 1 : reach);
              spark(c, g, p, h, .95, h > .05 ? 1 : 0); if (h > .05) proj++;
            }
          }
          return { a, s: 1, proj };
        },
      },
      {
        id: 'rise', name: 'Всплытие',
        idea: 'Луча нет: искры отрываются от орбит дрона и всплывают вверх, к ближнему краю окна, где тают в рамке. На открытии — один выдох из 24 искр, рамка загорается под ним; потом искры поднимаются по одной, пока окно открыто. Закрываясь, рамка осыпается, и её квадратики падают обратно в дрон.',
        time: 'Выдох искр 0,18 с · рамка за 0,1 с · экран читается через 0,15 с · по искре каждые ~0,07 с, путь 1,2 с · закрытие: экран за 0,08 с, рамка осыпается за 0,3 с.',
        draw(c, g, X, W) {
          const { u, uc } = W, open = W.ph === 'open', a = open ? c01((u - .05) / .11) : 1 - c01(uc / .08);
          screenIn(c, W, a, 1, null, X.t);
          if (open) drawFrame(c, g, X, W, c01((u - .06) / .1));
          else if (uc < .32) drawFrame(c, g, X, W, 1, (sq, j) => {
            const q = c01((uc - hsh(j + 5) * .1) / .22); if (q <= 0) return [sq.p, 1];
            return [bez(sq.p, [sq.p[0], Math.max(sq.p[1], X.D[1]) + 20], X.D, q * q), 1 - q];
          });
          let proj = 0;
          const rising = (S, T0, e, wob, j) => { const p = bez(S, [S[0] + wob, T0[1]], T0, e), p2 = bez(S, [S[0] + wob, T0[1]], T0, Math.min(1, e + .03)), d = unit(p, p2); return [p[0], p[1], d[0], d[1]]; };
          if (open && u < .3) for (let j = 0; j < 24; j++) {   // the puff
            const e = c01((u - hsh(j + 31) * .04) / .16); if (e <= 0 || e >= 1) continue;
            spark(c, g, rising(orbit(j * 5, X), edgePt(W, hsh(j + 32)), sm(e), (hsh(j + 33) - .5) * 30, j), .95); proj++;
          }
          const fade = open ? 1 : 1 - c01(uc / .1);
          if (fade > 0) for (let j = 0; j < 18; j++) {   // the steady rise
            const life = (X.t / 1.2 + hsh(j + 90)) % 1;
            if (open && u < life * 1.2) continue;
            const T0 = edgePt(W, hsh(j + 91)), end = c01((life - .75) / .25), h = .85 * (1 - end) * fade;
            const p = rising(orbit(j * 7 + 1, X), T0, life, (hsh(j + 92) - .5) * 40 + 6 * Math.sin(X.t * 3 + j), j);
            spark(c, g, p, h, .95 + 1.4 * end, (1 - end) * fade);
            if (end > 0) halo(g, T0[0], T0[1], 7, EMBER, .35 * (1 - end) * fade);
            if (h > .05) proj++;
          }
          return { a, s: 1, proj };
        },
      },
      {
        id: 'stroke', name: 'Росчерк',
        idea: 'Рамка и экран появляются разом, а украшение — одна яркая искра: она вылетает из глаза к ближнему углу и один раз обегает рамку с хвостом. Проекция — сам дрон: три орбиты сплющиваются в плоский диск, повёрнутый к окну, искры носятся по нему; от глаза к углу тянется тонкая нить. Закрываясь, искра возвращается в глаз.',
        time: 'Экран читается через 0,08 с · искра долетает до угла за 0,08 с и обегает рамку за 0,42 с · диск собирается за 0,12 с · закрытие: экран за 0,08 с, искра к глазу 0,12 с, диск обратно 0,15 с.',
        body: discOf,
        draw(c, g, X, W) {
          const { u, uc, E, r } = W, open = W.ph === 'open', a = open ? c01(u / .08) : 1 - c01(uc / .08), k = W.near, Cn = cornerAt(r, 4.7, k);
          screenIn(c, W, a, 1, null, X.t);
          drawFrame(c, g, X, W, a);
          const at = tm => tm < .08 ? along(E, Cn, tm / .08) : around(r, 4.7, k, Math.min(.9999, (tm - .08) / .42));
          if (open && u < .62) {
            const f = 1 - c01((u - .5) / .12);
            for (let m = 20; m >= 0; m--) { const tm = u - m * .01; if (tm < 0) continue; spark(c, g, at(tm), f * (m ? .85 * (1 - m / 20) : 1), m ? .95 : 1.6, m ? f : 1); }
            const hp = at(u); halo(g, hp[0], hp[1], 6, EMBER, .5 * f);
          }
          if (!open && uc < .12) spark(c, g, along(Cn, E, sm(uc / .12)), 1, 1.6);
          let proj = 0;
          const tf = open ? c01(u / .1) : 1 - c01(uc / .1);
          if (tf > 0) { line2(g, E, Cn, .4, .14 * tf); spark(c, g, along(E, Cn, (X.t / .7) % 1), .8 * tf, .95, tf); proj++; }
          return { a, s: 1, proj };
        },
      },
      {
        id: 'lens', name: 'Линза',
        idea: 'Половина искр слетается в кольцо-линзу перед глазом, со стороны окна; линза вращается. Из её краёв к дальним углам окна расходятся две нити с бегущими искрами, между ними — слабый свет, а окно вырастает из ближнего угла с 90 % до полного размера. Закрываясь, окно сжимается обратно, линза рассыпается в орбиты.',
        time: 'Линза собирается за 0,1 с · окно растёт 0,04–0,16 с, читается через 0,13 с · закрытие: окно сжимается за 0,1 с, линза — за 0,15 с.',
        body(X, W) {
          const form = W.ph === 'open' ? sm(W.u / .1) : 1 - sm(W.uc / .15), { d, n, L, R, Rt } = lensOf(X);
          return {
            form, share: .55, hide: .6,
            at(s) { const th = s * TAU + X.t * 3.2, c = Math.cos(th), si = Math.sin(th), v = [-si * R * n[0] + c * Rt * d[0], -si * R * n[1] + c * Rt * d[1]], l = Math.hypot(v[0], v[1]) || 1; return [L[0] + c * R * n[0] + si * Rt * d[0], L[1] + c * R * n[1] + si * Rt * d[1], v[0] / l, v[1] / l]; },
            under(g) {
              halo(g, L[0], L[1], 10, EMBER, .25 * form);
              for (const [w, a] of [[1.8, .08], [.45, .35]]) { g.beginPath(); g.ellipse(L[0], L[1], R, Rt, Math.atan2(n[1], n[0]), 0, TAU); g.lineWidth = w; g.strokeStyle = css(EMBER, a * form); g.stroke(); }
            },
          };
        },
        draw(c, g, X, W) {
          const { u, uc, r } = W, open = W.ph === 'open';
          const e = open ? sm(c01((u - .04) / .12)) : 1 - sm(c01(uc / .1)), a = open ? c01((u - .04) / .1) : 1 - c01(uc / .1), s = .9 + .1 * e, O = W.C[W.near];
          screenIn(c, W, a, s, O, X.t);
          drawFrame(c, g, X, W, a, sq => [[O[0] + (sq.p[0] - O[0]) * s, O[1] + (sq.p[1] - O[1]) * s], a]);
          const { n, L, R } = lensOf(X), R1 = [L[0] + n[0] * R, L[1] + n[1] * R], R2 = [L[0] - n[0] * R, L[1] - n[1] * R];
          const sc = p => [O[0] + (p[0] - O[0]) * s, O[1] + (p[1] - O[1]) * s], S0 = sc(W.sil[0]), S1 = sc(W.sil[1]);
          const pairs = Math.hypot(R1[0] - S0[0], R1[1] - S0[1]) + Math.hypot(R2[0] - S1[0], R2[1] - S1[1]) <= Math.hypot(R1[0] - S1[0], R1[1] - S1[1]) + Math.hypot(R2[0] - S0[0], R2[1] - S0[1]) ? [[R1, S0], [R2, S1]] : [[R1, S1], [R2, S0]];
          let proj = 0;
          if (e > .003) {
            g.save(); outsideWin(g, r);
            const gr = g.createLinearGradient(L[0], L[1], W.mid[0], W.mid[1]); gr.addColorStop(0, css(EMBER, .1 * e)); gr.addColorStop(1, css(EMBER, .02 * e));
            g.fillStyle = gr; g.beginPath(); g.moveTo(pairs[0][0][0], pairs[0][0][1]); g.lineTo(pairs[0][1][0], pairs[0][1][1]); g.lineTo(pairs[1][1][0], pairs[1][1][1]); g.lineTo(pairs[1][0][0], pairs[1][0][1]); g.closePath(); g.fill();
            g.restore();
            pairs.forEach(([A, B], side) => {
              line2(g, A, B, .45, .24 * e);
              for (let j = 0; j < 5; j++) { const life = (X.t * 1.1 + j / 5 + side * .1) % 1; spark(c, g, along(A, B, life), .8 * e * (1 - life * .5), .95, e); proj++; }
            });
          }
          return { a, s, proj };
        },
      },
      {
        id: 'corners', name: 'Уголки',
        idea: 'Рамки нет: четыре искры выстреливают из глаза в углы окна и встают там уголками-скобками, по которым бегают искры. Экран между ними — без каймы. Проекция — четыре тонкие нити от глаза к уголкам, по каждой ползёт искра. Закрываясь, уголки втягиваются обратно в глаз.',
        time: 'Искры долетают до углов за 0,1 с · экран читается через 0,13 с · закрытие: экран за 0,08 с, уголки в глаз за 0,12 с.',
        draw(c, g, X, W) {
          const { u, uc, E, r } = W, open = W.ph === 'open', a = open ? c01((u - .03) / .1) : 1 - c01(uc / .08), ARM = 22;
          screenIn(c, W, a, 1, null, X.t);
          let proj = 0;
          for (let k = 0; k < 4; k++) {
            const P0 = cornerAt(r, 5, k), sx = k === 0 || k === 3 ? 1 : -1, sy = k < 2 ? 1 : -1;
            if (open) { const q = c01((u - .01 * k) / .1); if (q < 1) { const p = along(E, P0, sm(q)); line2(g, E, p, .4, .2); spark(c, g, p, 1, 1.6); continue; } }
            else { const q = c01(uc / .12); if (q >= 1) continue; const p = along(P0, E, sm(q)); line2(g, p, E, .4, .2 * (1 - q)); spark(c, g, p, 1, 1.6); continue; }
            const land = c01((u - .1 - .01 * k) / .15), Ax = [P0[0] + sx * ARM, P0[1]], Ay = [P0[0], P0[1] + sy * ARM];
            for (const [w, al] of [[2.2, .07], [.6, .45]]) { g.beginPath(); g.moveTo(Ax[0], Ax[1]); g.lineTo(P0[0], P0[1]); g.lineTo(Ay[0], Ay[1]); g.lineWidth = w; g.strokeStyle = css(EMBER, al); g.stroke(); }
            for (const B of [Ax, Ay]) for (let m = 0; m < 3; m++) { const s = (X.t * 1.3 + m / 3 + k * .17) % 1; spark(c, g, along(P0, B, s), .9 * (1 - s * .7)); }
            glowSq(g, P0[0], P0[1], 2.4, .9);
            halo(g, P0[0], P0[1], 12, EMBER, .6 * (1 - land));
            proj += cornerThread(c, g, X, E, P0, k);
          }
          return { a, s: 1, proj };
        },
      },
    ];
    // the corners' projection: a faint thread from the eye to a corner, a spark creeping along it
    function cornerThread(c, g, X, E, P0, k) { line2(g, E, P0, .35, .08); spark(c, g, along(E, P0, (X.t * .9 + k * .25) % 1), .55); return 1; }

    // ---------- the clock, the drone's flight ----------
    function stateOf(T) {
      const ids = OPT.show === 'all' ? ORDER : [OPT.show], L = ids.length * SL.len, s = ((T.time - T.t0) % L + L) % L, k = Math.min(ids.length - 1, Math.floor(s / SL.len)), v = s - k * SL.len, id = ids[k];
      if (v < SL.open) return { id, ph: 'shut', u: 0, uc: 0 };
      if (v < SL.close) return { id, ph: 'open', u: v - SL.open, uc: 0 };
      return { id, ph: 'close', u: v - SL.open, uc: v - SL.close };
    }
    const flyOf = st => st.ph === 'open' ? sm(st.u / FLY.in) : st.ph === 'close' ? 1 - sm(st.uc / FLY.out) : 0;
    const waistOf = (h, t) => { const p = toS(h, WAIST); return [p[0], p[1] + Math.sin(t * 1.7 + h.seed) * 1.5]; };

    // ---------- the light buffer: everything that glows, added onto the picture ----------
    function lightStart(T) {
      const g = T.hbg; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.hb.width, T.hb.height);
      g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalCompositeOperation = 'lighter'; g.lineCap = 'round'; g.lineJoin = 'round';
      return g;
    }
    function lightEnd(T) {
      const g = T.hbg; g.globalCompositeOperation = 'source-over';
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0); c.restore();
    }
    // the loupe: the picture round the drone's place at the waist, ×2.5
    const LP = { x: 786, y: 12, w: 200, h: 200 };
    function loupe(T) {
      if (!OPT.loupe) return false;
      const c = T.c, S = T.S, Wp = waistOf(T.me, T.time), half = LP.w / LOUPE_K / 2, sw = 2 * half * S;
      const sx = clamp((Wp[0] - 18 - half) * S, 0, Math.max(0, T.cv.width - sw)), sy = clamp((Wp[1] - 16 - half) * S, 0, Math.max(0, T.cv.height - sw));
      const lg = T.lbg; lg.setTransform(1, 0, 0, 1, 0, 0); lg.globalCompositeOperation = 'source-over'; lg.clearRect(0, 0, T.lb.width, T.lb.height);
      lg.drawImage(T.cv, sx, sy, sw, sw, 0, 0, T.lb.width, T.lb.height);
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.fillStyle = '#05070b'; c.fill();
      c.save(); rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip(); c.drawImage(T.lb, LP.x, LP.y, LP.w, LP.h); c.restore();
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.strokeStyle = 'rgba(140,150,165,.35)'; c.lineWidth = 1; c.stroke();
      c.font = `11px ${FONT}`; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(200,205,214,.6)'; c.fillText('лупа ×2,5: у пояса', LP.x + 8, LP.y + LP.h - 8);
      return true;
    }

    return {
      cands, expose: { LOOP, SL, FLY, WAIST, ORDER, HIP: RIG.Hip, Z },
      init(T) { T.me.face = -1; T.t0 = 0; T.showWas = OPT.show; T.st = null; T.dp = null; },
      onScale(T) {
        const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S); T.hb = cv; T.hbg = cv.getContext('2d');
        const lb = document.createElement('canvas'); lb.width = Math.ceil(LP.w * T.S); lb.height = Math.ceil(LP.h * T.S); T.lb = lb; T.lbg = lb.getContext('2d');
      },
      // the hero stands facing the window, his head on it while it is out
      step(T, dt) {
        const me = T.me; me.x = HX; me.face = -1;
        if (T.showWas !== OPT.show) { T.showWas = OPT.show; T.t0 = T.time; }
        const st = T.st = stateOf(T), r = WINS[st.id].r, nk = toS(me, NECK());
        const look = st.ph === 'open' || (st.ph === 'close' && st.uc < .2);
        turnHead(me, look ? headTurn((r.x + r.w / 2 - nk[0]) * me.face, r.y + r.h / 2 - nk[1]) : 0, dt);
      },
      aim: () => null,
      ownDrone: () => true,
      // the drone leaves the kit's place over the shoulder for the waist while a window is out, turning its eye to it
      back(T) {
        drawBack(T);
        const h = T.me, st = T.st; if (!st) return;
        const f = flyOf(st), Wp = waistOf(h, T.time), H = h.d.p;
        T.dp = bez(H, [Wp[0], H[1]], Wp, f);
        const r = WINS[st.id].r;
        T.da = angLerp(h.d.ang, Math.atan2(r.y + r.h / 2 - T.dp[1], r.x + r.w / 2 - T.dp[0]), c01(f * 1.6));
        lampOn(T.c, T.dp[0], T.dp[1], h.x, h.y, h.face, .6, 2.6);
      },
      overDrone(T, h) {
        const st = T.st, cand = T.cand, c = T.c; if (!st || !T.dp) return;
        const g = lightStart(T), X = { c, g, D: T.dp.slice(), A: T.da, t: T.time + h.seed };
        const dm = T.dm = { id: st.id, ph: st.ph, u: st.u, uc: st.uc, a: 0, s: 1, proj: 0, D: X.D, hero: [h.x, h.y, h.face], loupe: false };
        let W = null;
        if (st.ph !== 'shut') { W = winCtx(st, X); const q = cand.draw(c, g, X, W); dm.a = q.a; dm.s = q.s; dm.proj = q.proj; }
        dm.proj += drawDrone(c, g, cand, X, W);
        lightEnd(T);
      },
      screen(T) { if (T.dm) T.dm.loupe = loupe(T); },
      hud(T) {
        const st = T.st; if (!st) return '';
        const n = WINS[st.id].name;
        if (st.ph === 'shut') return 'окно закрыто · дрон над плечом';
        if (st.ph === 'open') return st.u < .3 ? `открывается: ${n} · ${st.u.toFixed(2).replace('.', ',')} с` : `открыто: ${n} · дрон у пояса проецирует`;
        return st.uc < .35 ? `закрывается: ${n}` : 'окно закрыто · дрон летит к плечу';
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    slow: [['const a = open ? c01((u - .04) / .1) : 1 - c01(uc / .08), fr', 'const a = open ? c01((u - .04) / .4) : 1 - c01(uc / .08), fr']],
    skip: [["const ORDER = ['bag', 'plan', 'machine'];", "const ORDER = ['bag', 'plan'];"]],
    stuck: [['const SL = { open: .7, close: 3, len: 3.6 };', 'const SL = { open: .7, close: 3.7, len: 3.6 };']],
    waist: [['const WAIST = [16, -12];', 'const WAIST = [-9, -44];']],
    dark: [['proj += cornerThread(', 'proj += 0 * cornerThread(']],
  },

  // one whole loop of the three windows; then only the plan; then all again with the loupe off; a pick
  check(K) {
    const X = K.X, L = Math.round(X.LOOP * 60), A = L + 40, B = A + 240, E = B + 120, READ = .25;
    K.at(A, () => K.seg('showMode', 2));
    K.at(B, () => { K.seg('showMode', 0); K.seg('loupeMode', 1); });
    let st = '', doc = null;
    K.at(E - 20, () => K.click(3, '.pick'));
    K.at(E - 10, () => { st = K.byId.status.textContent; doc = K.picked(); });
    const per = K.tiles.map(() => ({ wins: {}, cur: null, read: null, late: false, waist: 0, away: 0, onlyPlan: 0, lOn: 0, lOff: 0, band: [1e9, -1e9, 1e9, -1e9] }));
    K.run(E, (t, i) => {
      const q = per[K.tiles.indexOf(t)], d = t.dm; if (!d) return;
      const id = t.cand.id, w = q.wins[d.id] || (q.wins[d.id] = { opens: 0, closes: 0, first: [] });
      const readable = d.a >= .9 && d.s >= .97;
      const dx = (d.D[0] - d.hero[0]) * d.hero[2] / X.Z, dy = (d.hero[1] - d.D[1]) / X.Z;
      if (d.ph === 'open') {
        if (q.cur !== d.id + ':open') { q.cur = d.id + ':open'; q.read = null; q.late = false; }
        if (readable && q.read === null) { q.read = d.u; w.first.push(d.u); }
        if (q.read !== null && !readable) K.fail(`${id}: ${d.id} unreadable at ${d.u.toFixed(3)} s, after it was readable`);
        if (q.read === null && d.u > READ && !q.late) { q.late = true; K.fail(`${id}: ${d.id} not readable ${d.u.toFixed(3)} s after it opened`); }
        if (d.u >= X.FLY.in + .08) {
          q.band = [Math.min(q.band[0], dx), Math.max(q.band[1], dx), Math.min(q.band[2], dy), Math.max(q.band[3], dy)];
          if (dx < 12 || dx > 20 || dy < X.HIP - 8 || dy > X.HIP + 8) K.fail(`${id}: the drone ${dx.toFixed(1)} ahead, ${dy.toFixed(1)} up (world px) while ${d.id} is out, frame ${i}`);
          else if (d.proj <= 0) K.fail(`${id}: no projection sparks while ${d.id} is out, frame ${i}`);
          else q.waist++;
        }
      } else if (d.ph === 'close') {
        if (q.cur === d.id + ':open') { q.cur = d.id + ':close'; w.opens++; }
        if (d.uc > .35) {
          if (d.a > 0 || d.proj > 0) K.fail(`${id}: ${d.id} still shown ${d.uc.toFixed(3)} s after closing`);
          else if (q.cur === d.id + ':close') { q.cur = null; w.closes++; }
        }
      } else {
        if (d.a > 0 || d.proj > 0) K.fail(`${id}: a window drawn while shut, frame ${i}`);
        if (dx < 0 && dy > 35) q.away++;
      }
      if (i > A + 1 && i < B) { if (d.id !== 'plan') K.fail(`${id}: ${d.id} with only the plan on`); else if (d.ph === 'open') q.onlyPlan++; }
      if (i < B) { if (d.loupe) q.lOn++; } else if (i > B + 1) { if (d.loupe) K.fail(`${id}: the loupe drawn while off`); else q.lOff++; }
    });
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id, f = k => q.wins[k] ? q.wins[k] : { opens: 0, closes: 0, first: [] };
      console.log(`${id}: ${X.ORDER.concat(['machine']).filter((k, n, a) => a.indexOf(k) === n).map(k => `${k} opened ${f(k).opens} closed ${f(k).closes} readable at ${f(k).first.map(v => v.toFixed(3)).join('/') || '-'} s`).join(' | ')} | drone at the waist ${q.waist} frames (${q.band.map(v => v.toFixed(1)).join('..')}), over the shoulder ${q.away} | only the plan ${q.onlyPlan} | loupe ${q.lOn}/${q.lOff}`);
      for (const k of ['bag', 'plan', 'machine']) {
        need(f(k).opens >= 1 && f(k).closes >= 1, `${id}: ${k} opens and closes`);
        need(f(k).first.length >= 1 && f(k).first.every(v => v <= READ), `${id}: ${k} readable within ${READ} s`);
      }
      need(q.waist > 300 && q.away > 60, `${id}: the drone before the waist while projecting, over the shoulder while shut`);
      need(q.onlyPlan > 100 && f('plan').opens >= 2, `${id}: only the plan when asked`);
      need(q.lOn > 100 && q.lOff > 30, `${id}: the loupe on and off`);
    });
    const cand = K.tiles[3].cand;
    console.log('after the pick:', st, '| doc', JSON.stringify(doc));
    need(doc && doc.pick === cand.id, 'the pick is in the db doc');
    need(st.includes(`«${cand.name}»`), 'the status names the pick');
    return ok;
  },
};
