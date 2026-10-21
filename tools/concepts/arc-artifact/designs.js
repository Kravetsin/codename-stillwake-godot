// Seven alien relics, each in the cradle the arc makers built round it. piece(S) draws it at scale S (1 = the placed piece,
// 32 x 32 px, two cells) and returns where its arcs leave from (src), the metal they jump to (metal) and its heart (core).
const L = require('./lib');
const { Spr, poly, ell, line, tone, rod, insulator, OLS, VC, VG, AM, IRON, CHIT, CU, CER, A, ST, mix, hash, inPoly } = L;

const sc = (S) => (v) => v * S;
const P2 = (S, pts) => pts.map(([x, y]) => [x * S, y * S]);
function veins(s, pts, S, o = {}) {
  // a glowing crack: a polyline of points; nodes brighter; the matter beside it lit faintly from within
  for (let i = 0; i < pts.length - 1; i++) line(pts[i][0] * S, pts[i][1] * S, pts[i + 1][0] * S, pts[i + 1][1] * S, (x, y) => {
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const X = x + dx, Y = y + dy; if (s.has(X, Y) && s.mat(X, Y) !== 'glow') { const c = s.c[Y * s.w + X]; s.set(X, Y, mix(c, VC.lo, 0.45), 'glow', 0.28); } }
    s.glow(x, y, o.col || VC.hi, 0.95); if (S >= 1.6) s.glow(x + 1, y, VC.mid, 0.8);
  });
  for (const [x, y] of pts.slice(1, -1)) s.glow(x * S, y * S, VC.core, 1.25);
}
function plate(s, x0, y0, w, h) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) s.set(x0 + i, y0 + j, j === 0 ? A[4] : i === 0 ? A[4] : j === h - 1 ? A[1] : A[3], 'al'); }

const DESIGNS = [
  {
    id: 'shard', name: 'Небесный осколок', short: 'Осколок',
    what: 'Чёрное стекло, которое не плавится: щепа с неба, упавшая остриём вниз. Внутри бегут белые трещины, и в каждой гудит свет.',
    tamed: 'Медный хомут на трёх ногах держит осколок стоймя; под ним фарфоровая стопка, чтобы дуги уходили в хомут, а не в пол.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W);
      const sh = P2(S, [[12, 27], [11, 19], [12.5, 10], [15, 1.5], [17.5, 6], [19.5, 13], [21, 21], [20, 27]]);
      poly(s, sh, (x, y) => {
        const xm = k(15) + (k(16) - k(15)) * (y - k(1.5)) / k(25.5), left = x + 0.5 < xm;
        const edgeL = !inPoly(sh, x - 0.5, y + 0.5), edgeR = !inPoly(sh, x + 1.5, y + 0.5);
        if (left) return edgeL ? VG[4] : (y < k(9) && x + 0.5 > xm - 1.5) ? VG[5] : VG[3];
        return edgeR ? VG[1] : VG[2];
      }, 'vg');
      veins(s, [[16, 26], [15, 20], [17, 15], [15.5, 10], [16, 4.5]], S);
      veins(s, [[15, 20], [13, 17]], S); veins(s, [[17, 15], [19, 18]], S);
      // the collar, its legs and the porcelain under it
      const y0 = Math.round(k(19)), hC = Math.max(3, Math.round(k(3.5)));
      for (let j = 0; j < hC; j++) for (let x = Math.round(k(9)); x <= Math.round(k(23)); x++) s.set(x, y0 + j, j === 0 ? CU[4] : j === hC - 1 ? CU[1] : (x % Math.round(k(4)) === 0 && j === 1) ? CU[4] : CU[3], 'cu');
      rod(s, k(10), y0 + hC, k(5), W - 2); rod(s, k(22), y0 + hC, k(27), W - 2);
      insulator(s, Math.round(k(16)), Math.round(k(27)), Math.max(2, Math.round(2 * S)), Math.round(k(7)) | 1);
      plate(s, Math.round(k(3)), W - 2, Math.round(k(5)), 2); plate(s, Math.round(k(24.5)), W - 2, Math.round(k(5)), 2);
      plate(s, Math.round(k(12)), W - 1, Math.round(k(9)), 1);
      s.outline(OLS);
      return { spr: s, core: [k(16), k(15)], src: P2(S, [[15, 2], [19.5, 13], [11.5, 15], [21, 21], [12.5, 10]]), metal: P2(S, [[9, 20], [23, 20], [7, 27], [25, 27], [5, 30], [27, 30]]), r: 6.5 };
    },
    icon: [
      '...........H....', '.......o........', '......o5o.......', '......o4Ho......', '.....o4H2o......', '.M...o3M2o......', '.....o4H2o......', '....o43W2o......',
      '....o4M22o......', '...ooo3H2ooo....', '..oCCCCCCCCCo...', '..okkkkkkkkqo...', '...ooo3M2ooo....', '..oco.o22o.oco..', '.oco..oppo..oco.', '.ooo..oooo..ooo.',
    ],
  },
  {
    id: 'heart', name: 'Сердце-кристалл', short: 'Сердце',
    what: 'Восьмигранник, который светится сам и не касается земли: висит над чашей на ладонь, будто его держит что-то невидимое.',
    tamed: 'Медная чаша под ним и четыре пальца с фарфоровыми бусинами по краям: кристалл бьёт в бусины, чаша собирает ток.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W);
      const cx = k(16), cy = k(11), cr = P2(S, [[16, 2], [22.5, 11], [16, 20], [9.5, 11]]);
      poly(s, cr, (x, y) => {
        const X = x + 0.5, Y = y + 0.5, ridge = Math.abs(X - cx) < 0.6, gird = Math.abs(Y - cy) < 0.6;
        if (Math.abs(X - cx) < k(1.6) && Math.abs(Y - cy) < k(1.6)) return [VC.core, 1.25];
        if (gird) return [VC.hi, 1]; if (ridge) return [Y < cy ? VC.hi : VC.mid, 0.95];
        if (Y < cy) return X < cx ? [VC.hi, 0.8] : [VC.mid, 0.72];
        return X < cx ? [VC.mid, 0.62] : [VC.lo, 0.58];
      }, 'cry');
      // the dish, its stem and plate
      const dy = Math.round(k(24));
      for (let x = Math.round(k(8)); x <= Math.round(k(24)); x++) s.set(x, dy, CU[4], 'cu');
      for (let j = 1; j <= Math.max(2, Math.round(k(2.5))); j++) { const inset = Math.round(j * 1.6 * S); for (let x = Math.round(k(8)) + inset; x <= Math.round(k(24)) - inset; x++) s.set(x, dy + j, x < cx ? CU[3] : CU[2], 'cu'); }
      for (let y = dy + Math.round(k(3)); y < W - 3; y++) for (let x = Math.round(k(14.5)); x <= Math.round(k(17.5)); x++) s.set(x, y, x === Math.round(k(14.5)) ? A[4] : A[2], 'al');
      plate(s, Math.round(k(10)), W - 3, Math.round(k(13)), 3);
      // the fingers and their beads
      const bead = (x, y) => { for (let j = 0; j < 2; j++) for (let i = 0; i < 3; i++) s.set(x - 1 + i, y + j, j === 0 ? (i === 0 ? CER[4] : CER[3]) : CER[1], 'cer'); };
      line(k(8), dy, k(7), k(18), (x, y) => s.set(x, y, CU[3], 'cu')); line(k(7), k(18), k(8), k(13.5), (x, y) => s.set(x, y, CU[4], 'cu')); bead(k(8), k(11.5));
      line(k(24), dy, k(25), k(18), (x, y) => s.set(x, y, CU[2], 'cu')); line(k(25), k(18), k(24), k(13.5), (x, y) => s.set(x, y, CU[3], 'cu')); bead(k(24), k(11.5));
      line(k(12), dy, k(12), k(22.5), (x, y) => s.set(x, y, CU[3], 'cu')); bead(k(12), k(21));
      line(k(20), dy, k(20), k(22.5), (x, y) => s.set(x, y, CU[3], 'cu')); bead(k(20), k(21));
      s.outline({ ...OLS, cry: VC.dk });
      return { spr: s, core: [cx, cy], src: P2(S, [[16, 2.5], [22, 11], [10, 11], [16, 19.5], [13, 7], [19, 15]]), metal: P2(S, [[8, 11.5], [24, 11.5], [12, 21], [20, 21], [16, 24]]), r: 7, hover: true };
    },
    icon: [
      '.......oo.......', '......oHMo......', 'oo...oHHMMo...oo', 'Pr..oHHWHMMo..Pr', 'oo.oHHWWWMMLo.oo', '.c..oMMWMLLo..c.', '.c...oMMLLo...c.', '.c....oMLo....c.',
      '.c.....oo.....c.', '.c.....H......c.', '.oCCCCCCCCCCCCo.', '..okkkkkkkkkko..', '...oqqqqqqqqo...', '......osto......', '....ottsssso....', '....oooooooo....',
    ],
  },
  {
    id: 'core', name: 'Чужое ядро', short: 'Ядро',
    what: 'Идеальный чёрный шар из металла, которого нет в их таблицах. По нему идёт светящийся шов и три точки знака, не похожего ни на одну их букву.',
    tamed: 'Медный обруч с обмоткой по бокам и фарфор под ним: шар висит в клетке, обмотка снимает с него ток.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W), cx = k(16), cy = k(15), R = k(7);
      ell(s, cx, cy, R, R, (x, y, nx, ny, d) => {
        if (nx > -0.62 && nx < -0.22 && ny > -0.66 && ny < -0.3) return AM[5];
        return d > 0.9 ? AM[1] : tone(AM, nx, ny, 1);
      }, 'am');
      // the seam: a ring round its middle, tilted towards us; the mark: three points and an arc
      for (let x = -R + 0.5; x <= R - 0.5; x += 0.5) { const y = cy + 0.36 * Math.sqrt(Math.max(0, R * R - x * x)); s.glow(cx + x, y, Math.abs(x) < R * 0.35 ? VC.hi : VC.mid, Math.abs(x) < R * 0.35 ? 1.05 : 0.85); }
      for (const [dx, dy2] of [[0, -4.2], [-2.2, -1.2], [2.2, -1.2]]) { s.glow(cx + k(dx), cy + k(dy2), VC.core, 1.2); if (S > 1.4) s.glow(cx + k(dx) + 1, cy + k(dy2), VC.hi, 1); }
      // the cage: a hoop round it, wound at either side, a hook at the top, porcelain and a plate below
      const hr = k(10.6), hry = k(11.4);
      for (let a = 0; a < Math.PI * 2; a += 0.01) {
        const x = cx + Math.cos(a) * hr, y = cy + Math.sin(a) * hry; if (s.mat(Math.round(x), Math.round(y)) === 'am') continue;
        s.set(x, y, Math.cos(a) < -0.3 || Math.sin(a) < -0.6 ? CU[4] : Math.cos(a) > 0.4 ? CU[2] : CU[3], 'cu');
      }
      for (const side of [-1, 1]) for (let y = Math.round(cy - k(3.5)); y <= Math.round(cy + k(3.5)); y++) { const x = Math.round(cx + side * hr); const c = y % 2 ? CU[4] : CU[1]; s.set(x - 1, y, c, 'cu'); s.set(x, y, c, 'cu'); s.set(x + 1, y, c, 'cu'); }
      for (let y = Math.round(cy - hry - k(2)); y <= Math.round(cy - hry); y++) s.set(cx, y, A[4], 'al');
      insulator(s, Math.round(cx), Math.round(cy + hry + 1), Math.max(1, Math.round(S)), 5);
      plate(s, Math.round(k(10)), W - 3, Math.round(k(13)), 3);
      for (let y = Math.round(cy + hry + 1 + 2 * Math.max(1, Math.round(S))); y < W - 3; y++) s.set(cx, y, A[3], 'al');
      s.outline(OLS);
      return { spr: s, core: [cx, cy], src: P2(S, [[9.2, 15.5], [22.8, 15.5], [16, 8.5], [13, 21], [19, 21], [16, 10.8]]), metal: P2(S, [[5.4, 15], [26.6, 15], [16, 3.6], [16, 26.4], [8, 7], [24, 23]]), r: 7 };
    },
    icon: [
      '............H...', '.o...oooooo...o.', 'oC..oeeffdbo..Co', 'oq.oeefHddbbo.qo', 'oC.oedddddbao.Co', 'oqMoMMHWWHMMoMqo', 'oC.odddddbbao.Co', 'oq.obdddbbaao.qo',
      'oC..obbbbaao..Co', '.o...oooooo...o.', '......oPPo......', '......orro......', '.....oPPPPo.....', '.....orrrro.....', '...ottsssssso...', '...oooooooooo...',
    ],
  },
  {
    id: 'seed', name: 'Звёздное семя', short: 'Семя',
    what: 'Ребристая скорлупа, похожая на семя или кокон, треснувшая вдоль. Из щели льётся свет, а корни, которые оно пустило при падении, вросли в камень.',
    tamed: 'Медная плетёная корзина держит нижнюю половину, фарфоровый барабан под ней. Корни так и оставили: их не оторвать.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W), cx = k(16), cy = k(14), ry = k(11);
      const half = (y) => { const t = (y + 0.5 - cy) / ry; if (Math.abs(t) >= 1) return 0; return k(6.2) * Math.sqrt(1 - t * t) * (t < 0 ? 1 + 0.42 * t : 1); };
      // the pedestal first: the roots grip it
      for (let y = Math.round(k(27)); y < W; y++) for (let x = Math.round(k(10)); x <= Math.round(k(22)); x++) s.set(x, y, y === Math.round(k(27)) ? CER[4] : y === W - 1 ? CER[1] : x === Math.round(k(10)) ? CER[4] : x >= Math.round(k(20)) ? CER[2] : CER[3], 'cer');
      for (let y = Math.round(cy - ry); y <= Math.round(cy + ry); y++) { const hw = half(y); if (hw <= 0.3) continue;
        for (let x = Math.floor(cx - hw); x <= Math.ceil(cx + hw) - 1; x++) {
          const nx = (x + 0.5 - cx) / hw, t = (y + 0.5 - cy) / ry; if (Math.abs(nx) > 1) continue;
          const split = Math.max(0, 1.5 * S * (1 - Math.pow(Math.abs(t + 0.05), 1.6))), inSplit = t > -0.78 && t < 0.7 && Math.abs(x + 0.5 - cx) < split;
          if (inSplit) { const m = Math.abs(x + 0.5 - cx) / Math.max(0.6, split); s.glow(x, y, m < 0.4 ? VC.core : m < 0.8 ? VC.hi : VC.mid, m < 0.4 ? 1.25 : 0.95); continue; }
          let c = tone(CHIT, nx * 0.9, t * 0.55, 1); if (Math.abs(Math.abs(nx) - 0.55) < 0.12) c = CHIT[1]; if (Math.abs(nx) > 0.9) c = CHIT[1];
          if (t > -0.78 && t < 0.7 && Math.abs(x + 0.5 - cx) < split + 1.1) s.set(x, y, mix(c, VC.lo, 0.5), 'glow', 0.32); else s.set(x, y, c, 'chit');
        } }
      // the basket over its lower half
      const yb = Math.round(k(18)), ye = Math.round(k(25));
      for (let y = yb; y <= ye; y++) { const hw = half(y) + 1; for (let x = Math.floor(cx - hw); x <= Math.ceil(cx + hw) - 1; x++) {
        if (s.mat(x, y) === 'glow' && s.g[y * s.w + x] > 0.5) continue;
        const lat = ((x + y) % 4 === 0) || (((x - y) % 4) + 4) % 4 === 0;
        if (y === yb) s.set(x, y, CU[4], 'cu'); else if (y === ye) s.set(x, y, CU[2], 'cu'); else if (lat) s.set(x, y, x < cx ? CU[3] : CU[2], 'cu');
      } }
      // roots, after: they hold the drum
      for (const r of [[[13, 25], [10, 27], [9, 30], [8, 31.5]], [[19, 25], [22, 27.5], [23, 31.5]], [[16, 25.5], [15, 27.5]]]) for (let i = 0; i < r.length - 1; i++) line(k(r[i][0]), k(r[i][1]), k(r[i + 1][0]), k(r[i + 1][1]), (x, y) => { s.set(x, y, CHIT[3], 'chit'); s.set(x + 1, y, CHIT[1], 'chit'); });
      s.outline(OLS);
      return { spr: s, core: [cx, k(13)], src: P2(S, [[16, 4.5], [16, 22], [16, 12], [15.5, 8], [16.5, 17]]), metal: P2(S, [[9.5, 18], [22.5, 18], [11, 22], [21, 22], [10, 27], [22, 27]]), r: 6.5 };
    },
    icon: [
      '.......oo...M...', '......oyxo......', '.....oyzHxo.....', '....oyzMWxwo....', '....ozyHWLxo....', '...oyzyMWHxwo...', '...ozyzHWMxwo...', '..oCCCCHWCCCCo..',
      '...ocxcMHcxco...', '....oxcLMcxo....', '.....okccko.....', '...owwoPPowwo...', '..ow.oPppro.wo..', '.ow..oprrro..wo.', '.oo..oooooo..oo.', '................',
    ],
  },
  {
    id: 'meteor', name: 'Звёздное железо', short: 'Метеорит',
    what: 'Ком оплавленного железа с ямками, как от пальцев: метеорит, расколотый трещинами, из которых сочится фиолетовый свет.',
    tamed: 'Его не подняли: обвязали двумя медными полосами, утопили в каменную чашу и воткнули сверху громоотвод, чтобы собирать разряды.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W), cx = k(16), cy = k(18), R0 = k(9.2), sq = 0.74;
      const rad = (a) => R0 * (1 + 0.11 * Math.sin(3 * a + 1) + 0.07 * Math.sin(5 * a + 2) + 0.04 * Math.sin(9 * a));
      // the socket
      for (let y = Math.round(k(25)); y < W; y++) { const t = (y - k(25)) / (W - k(25)); const x0 = Math.round(k(6.5) + t * k(3)), x1 = Math.round(k(25.5) - t * k(3));
        for (let x = x0; x <= x1; x++) s.set(x, y, y === Math.round(k(25)) ? ST[4] : x <= x0 + 1 ? ST[3] : x >= x1 - 1 ? ST[1] : ST[2], 'st'); }
      for (let y = Math.round(cy - R0); y <= Math.round(cy + R0); y++) for (let x = Math.round(cx - R0 * 1.3); x <= Math.round(cx + R0 * 1.3); x++) {
        const dx = x + 0.5 - cx, dy = (y + 0.5 - cy) / sq, a = Math.atan2(dy, dx), r = rad(a), d = Math.sqrt(dx * dx + dy * dy); if (d > r) continue;
        if (y > Math.round(k(26))) continue;
        let c = d > r - 1 ? IRON[1] : tone(IRON, dx / r, dy / r, 1);
        for (const [px, py] of [[12, 15], [19, 14], [15, 21], [21, 20], [10, 20], [17, 18]]) { const ex = x + 0.5 - k(px), ey = y + 0.5 - k(py); if (ex * ex + ey * ey < k(1.9) * k(1.9)) c = ex + ey < 0 ? IRON[1] : IRON[4]; }
        s.set(x, y, c, 'iron');
      }
      veins(s, [[8.5, 18], [11, 17], [13, 19.5], [16, 17], [19, 18.5], [22, 16.5], [24, 18]], S);
      veins(s, [[16, 17], [15, 13.5], [17, 11.5]], S); veins(s, [[13, 19.5], [12.5, 22.5]], S);
      // the straps and the rod
      for (const sx of [11.5, 20.5]) for (let y = 0; y < W; y++) { const x = Math.round(k(sx)); if (!['iron', 'glow'].includes(s.mat(x, y))) continue; for (let i = 0; i < Math.max(2, Math.round(2 * S)); i++) s.set(x + i, y, i === 0 ? CU[4] : CU[2], 'cu'); }
      let top = 0; while (top < W && !['iron', 'cu'].includes(s.mat(Math.round(cx), top))) top++;
      for (let y = Math.round(k(1.5)); y < top + 1; y++) s.set(cx, y, y < k(3) ? CU[4] : CU[3], 'cu');
      for (let x = Math.round(k(13)); x <= Math.round(k(19)); x++) { s.set(x, Math.round(k(7)), CER[x < cx ? 4 : 3], 'cer'); s.set(x, Math.round(k(7)) + 1, CER[1], 'cer'); }
      s.outline(OLS);
      return { spr: s, core: [cx, k(18)], src: P2(S, [[8.5, 18], [24, 18], [17, 11.5], [13, 19.5], [12.5, 22.5], [19, 18.5]]), metal: P2(S, [[16, 1.5], [16, 5], [11.5, 12.5], [20.5, 12], [6.5, 25], [25.5, 25]]), r: 6.5 };
    },
    icon: [
      '.......C........', '.......c....H...', '......oPo.......', '.......c........', '....oooqoooo....', '...ouCuuujCjo...', '..ojuCjjMjCiio..', '..oMHkWHMjkMio..',
      '..ojikjjHjkiio..', '...oiknjMnkio...', '....oknnnnko....', '.oEEEEEEEEEEEEo.', '..oFFFFFFFFFFo..', '...oGGGGGGGGo...', '....oooooooo....', '................',
    ],
  },
  {
    id: 'ring', name: 'Висящее кольцо', short: 'Кольцо',
    what: 'Кольцо из тёмного металла, которое висит в воздухе и медленно вращает свет в своей дыре. На ободе вырезаны метки, ни одна не повторяется.',
    tamed: 'Два медных электрода с фарфором по бокам, концы почти касаются обода: ток прыгает через зазор, как в искровом разряднике.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W), cx = k(16), cy = k(13), Ro = k(8), Ri = k(4);
      ell(s, cx, cy, Ro, Ro, (x, y, nx, ny, d) => {
        const dd = d * Ro, a = Math.atan2(ny, nx);
        if (dd <= Ri) { if (dd < k(1.6)) return [VC.core, 0.9]; const sw = Math.sin(a * 2 + dd * 1.4 / S); return sw > 0.35 ? [VC.hi, 0.6] : sw > -0.3 ? [VC.mid, 0.5] : [VC.lo, 0.42]; }
        if (dd < Ri + 0.9) return [mix(AM[2], VC.lo, 0.55), 0.35];
        const u = (dd - (Ro + Ri) / 2) / ((Ro - Ri) / 2);
        for (let n = 0; n < 7; n++) { const an = n * 0.9 + 0.3; let da = Math.abs(((a - an) % (Math.PI * 2) + Math.PI * 3) % (Math.PI * 2) - Math.PI); if (da < 0.1 && u > -0.3 && u < 0.55) return [VC.mid, 0.75]; }
        return dd > Ro - 0.9 ? AM[1] : tone(AM, nx / Math.max(0.3, d) * u, ny / Math.max(0.3, d) * u, 1);
      }, 'am');
      for (const side of [-1, 1]) {
        const px = Math.round(cx + side * k(12));
        insulator(s, px, Math.round(k(22)), Math.max(3, Math.round(4 * S)), 5);
        for (let y = Math.round(k(8)); y < Math.round(k(22)); y++) s.set(px, y, side < 0 ? CU[3] : CU[2], 'cu');
        for (let j = -1; j <= 1; j++) for (let i = -1; i <= 1; i++) s.set(px + i, Math.round(k(7)) + j, i + j < 0 ? CU[4] : CU[3], 'cu');
        for (let x = Math.min(px, Math.round(cx + side * (Ro + 1.5))); x <= Math.max(px, Math.round(cx + side * (Ro + 1.5))); x++) s.set(x, Math.round(cy), CU[4], 'cu');
      }
      plate(s, Math.round(k(1)), W - 2, Math.round(k(30)), 2);
      s.outline(OLS);
      return { spr: s, core: [cx, cy], src: P2(S, [[8.2, 13], [23.8, 13], [16, 5.2], [16, 20.8], [10.5, 7.5], [21.5, 18.5]]), metal: P2(S, [[6.5, 13], [25.5, 13], [4, 7], [28, 7], [4, 21], [28, 21]]), r: 7, orbit: { x: cx, y: cy, r: k(2.6) } };
    },
    icon: [
      '........H.......', 'oCo..........oCo', '.c...oooooo...c.', '.c..oefddbbo..c.', '.c.oefoooodbo.c.', '.c.oeoMHHMoao.c.', '.cCodoHWWHoaoCc.', '.c.odoHWWHoao.c.',
      '.c.odoMHHLoao.c.', '.c.obdooooabo.c.', '.c..obbbaaao..c.', '.c...oooooo...c.', 'oPo..........oPo', 'oro....M.....oro', 'osssssssssssssso', 'oooooooooooooooo',
    ],
  },
  {
    id: 'obelisk', name: 'Обломок обелиска', short: 'Обелиск',
    what: 'Кусок чёрной призмы, сломанной сверху: в изломе виден свет. По грани сбегает столбец знаков, нарезанных не их инструментом.',
    tamed: 'Три медных пояса с обмоткой, сверху венец из двух фарфоровых рогов, по бокам две стальные распорки, чтобы не упал.',
    piece(S = 1) {
      const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W);
      const sh = P2(S, [[11.5, 31], [20, 31], [21.5, 4], [20, 2], [18.5, 4.5], [17, 1.5], [15, 4], [13, 3], [12.6, 6]]);
      const split = (y) => k(18) + (k(19.6) - k(18)) * (1 - y / W);
      poly(s, sh, (x, y) => {
        const X = x + 0.5; if (X > split(y)) return !inPoly(sh, x + 1.5, y + 0.5) ? AM[1] : AM[2];
        if (!inPoly(sh, x - 0.5, y + 0.5)) return AM[4]; if (!inPoly(sh, x + 0.5, y - 0.5)) return [VC.hi, 0.9]; if (!inPoly(sh, x + 0.5, y - 1.5)) return [mix(AM[3], VC.lo, 0.5), 0.35];
        return AM[3];
      }, 'am');
      // the column of marks down its face
      const marks = [[7, 2], [10, 1], [11, 3], [13.5, 1], [17, 2], [19.5, 1], [21, 3], [25, 2], [27.5, 1]];
      for (const [my, len] of marks) for (let i = 0; i < len; i++) s.glow(k(14) + (i % 2 ? k(1.5) : 0) + (len === 3 ? i : 0), k(my) + (i > 0 && len < 3 ? 1 : 0), i === 0 ? VC.hi : VC.mid, i === 0 ? 1.05 : 0.85);
      // the bands, the crown, the struts
      for (const by of [9, 16, 23]) { const y = Math.round(k(by)); for (let j = 0; j < Math.max(2, Math.round(2 * S)); j++) for (let x = Math.round(k(10.5)); x <= Math.round(k(21.5)); x++) s.set(x, y + j, j === 0 ? (x % 2 ? CU[4] : CU[3]) : (x % 2 ? CU[2] : CU[1]), 'cu'); }
      const bead = (x, y) => { for (let j = 0; j < 2; j++) for (let i = 0; i < 3; i++) s.set(x - 1 + i, y + j, j === 0 ? (i === 0 ? CER[4] : CER[3]) : CER[1], 'cer'); };
      line(k(10.5), k(9), k(7.5), k(6.5), (x, y) => s.set(x, y, CU[4], 'cu')); bead(k(7), k(5));
      line(k(21.5), k(9), k(24.5), k(6.5), (x, y) => s.set(x, y, CU[3], 'cu')); bead(k(25), k(5));
      line(k(11), k(17.5), k(5), W - 2, (x, y) => { s.set(x, y, A[4], 'al'); s.set(x + 1, y, A[2], 'al'); });
      line(k(21), k(17.5), k(27), W - 2, (x, y) => { s.set(x, y, A[3], 'al'); s.set(x + 1, y, A[1], 'al'); });
      plate(s, Math.round(k(3)), W - 2, Math.round(k(5)), 2); plate(s, Math.round(k(25)), W - 2, Math.round(k(5)), 2);
      s.outline(OLS);
      return { spr: s, core: [k(16), k(12)], src: P2(S, [[17, 2], [20, 2.5], [13, 3.5], [14.5, 11], [15, 21], [14.5, 27.5]]), metal: P2(S, [[7, 5], [25, 5], [10.5, 16], [21.5, 16], [10.5, 23], [21.5, 23], [5, 30], [27, 30]]), r: 6.5 };
    },
    icon: [
      '......o.o....H..', '.....oHoHo......', '.....oHHWHo.....', '.....oedMao.....', '..oPoCCCCCCoPo..', '.....qkkkkq.....', '.....oedHao.....', '.....oeddao.....',
      '.....oeMdao.....', '.....CCCCCC.....', '....oqkkkkqo....', '.....oedHao.....', '....toedHaot....', '...t.oedMao.t...', '..t..oeddao..t..', '..oo.oooooo.oo..',
    ],
  },
];

// the icons' legend
const ICOL = {
  o: L.hex('#100c18'), C: CU[4], c: CU[3], k: CU[2], q: CU[1],
  W: VC.core, H: VC.hi, M: VC.mid, L: VC.lo, D: VC.dk,
  1: VG[1], 2: VG[2], 3: VG[3], 4: VG[4], 5: VG[5],
  a: AM[1], b: AM[2], d: AM[3], e: AM[4], f: AM[5],
  P: CER[4], p: CER[3], r: CER[2],
  n: IRON[1], i: IRON[2], j: IRON[3], u: IRON[4],
  w: CHIT[1], x: CHIT[2], y: CHIT[3], z: CHIT[4],
  s: A[3], t: A[4], g: A[1],
  E: ST[4], F: ST[3], G: ST[2],
};
const IGLOW = new Set(['W', 'H', 'M', 'L']);
function icon(d) {
  const s = new Spr(16, 16);
  d.icon.forEach((row, y) => { if (row.length !== 16) throw new Error(d.id + ' icon row ' + y + ' is ' + row.length); for (let x = 0; x < 16; x++) { const ch = row[x]; if (ch === '.') continue; if (!ICOL[ch]) throw new Error(d.id + ' icon char ' + ch); s.set(x, y, ICOL[ch], IGLOW.has(ch) ? 'glow' : 'x', IGLOW.has(ch) ? 1 : 0); } });
  return s;
}

module.exports = { DESIGNS, icon, veins, plate };
