    // The wrist computer (inventory-look variant B) in the build tablet's metal, cut into 9-slices: each piece is drawn round a
    // nominal window, then its corners, one stretch of each edge and nothing of the window are kept.
    const PAL_A = { o: '#15181e', b: '#4a5263', hi: '#6a7385', lo: '#3a404d', hi2: '#8a93a8' };
    const SCUFF_A = { scratch: '#7d8699', dent: '#3a404d', hi: '#5d6678', grime: '#434a59' };
    const PAD = 1; // a transparent pixel round the shape, where the rubber bumpers stand out
    const box = (w, h, r) => (x, y) => { // r: the corners' radii, [tl, tr, bl, br]
      if (x < 0 || y < 0 || x >= w || y >= h) return false;
      const k = (x < w / 2 ? 0 : 1) + (y < h / 2 ? 0 : 2), rad = r[k];
      if (!rad) return true;
      const cx = x < rad ? rad : x >= w - rad ? w - rad : null, cy = y < rad ? rad : y >= h - rad ? h - rad : null;
      if (cx === null || cy === null) return true;
      const dx = x + 0.5 - cx, dy = y + 0.5 - cy;
      return dx * dx + dy * dy <= rad * rad;
    };
    function rubber(A, x, y, w, h, fx, fy, s, thick) {
      const rub = { o: '#0e1014', b: '#1f2229', hi: '#30343d', lo: '#17191e' };
      const inside = (i, j) => {
        if (i < 0 || j < 0 || i >= s || j >= s) return false;
        const lx = fx ? s - 1 - i : i, ly = fy ? s - 1 - j : j;
        return lx + ly <= s + 2 && rr(s, s, 3)(lx, ly) && (lx < thick || ly < thick);
      };
      A.shape(fx ? x + w - s + 1 : x - 1, fy ? y + h - s + 1 : y - 1, s, s, inside, rub);
    }
    const led = (A, x, y, size, on) => { A.r(x, y, size, size, on); if (size > 1) A.p(x, y, '#9fe0b4'); };
    function piece({ ww, wh, l, t, r, b, rad, rubbers = [], rs = 9, rt = 4, scuffs = 0, deco }) {
      const sw = ww + l + r, sh = wh + t + b, W = sw + 2 * PAD, H = sh + 2 * PAD;
      const A = Art(W, H);
      A.shape(PAD, PAD, sw, sh, box(sw, sh, rad), PAL_A);
      const wx = PAD + l, wy = PAD + t;
      A.r(wx - 1, wy - 1, ww + 2, 1, '#101318'); A.r(wx - 1, wy - 1, 1, wh + 2, '#101318');
      A.r(wx - 1, wy + wh, ww + 2, 1, '#5d6678'); A.r(wx + ww, wy - 1, 1, wh + 2, '#5d6678');
      if (scuffs) scuff(A, PAL_A.b, { count: scuffs, ...SCUFF_A });
      A.clear(wx, wy, ww, wh);
      for (const [fx, fy] of rubbers) rubber(A, PAD, PAD, sw, sh, fx, fy, rs, rt);
      if (deco) deco(A, W, H);
      return A;
    }
    // corners L x T etc. kept whole, one MW-wide stretch of the top and bottom and one MH-tall stretch of the sides
    function nine(A, L, T, R, B, MW, MH) {
      const [c, g] = cv(L + MW + R, T + MH + B);
      const xs = [[0, 0, L], [L, L, MW], [A.w - R, L + MW, R]], ys = [[0, 0, T], [T, T, MH], [A.h - B, T + MH, B]];
      for (const [sx, dx, w] of xs) for (const [sy, dy, h] of ys) g.drawImage(A.c, sx, sy, w, h, dx, dy, w, h);
      return c;
    }
    const stripe = (A, x, y, w, h) => {
      for (let j = y; j < y + h; j++) for (let i = x; i < x + w; i++) {
        const am = ((i + j) >> 1) & 1; let col = am ? '#e0a030' : '#262b35';
        if (am && rnd() < 0.16) col = '#a17323';
        A.p(i, j, col);
      }
    };
    const LED_DIM = '#3d8a55', LED_ON = '#4ade80', PIP_OFF = '#1f3b2a';

    // The cuff round the hotbar: thin, quiet, two small bumpers, a dim status light; the gauges' tabs hang under it.
    seed = 7;
    const cuff = piece({ ww: 80, wh: 30, l: 3, t: 3, r: 3, b: 3, rad: [2, 2, 2, 2], rubbers: [[0, 0], [1, 0]], rs: 7, rt: 3,
      deco: (A, W) => A.p(W - PAD - 10, PAD + 1, LED_DIM) });
    save('wrist_cuff.png', nine(cuff, 7, 7, 12, 4, 24, 8));
    // A gauge's tab: its top the cuff's bottom edge, which it is drawn over.
    seed = 11;
    const gauge = piece({ ww: 60, wh: 15, l: 3, t: 2, r: 3, b: 3, rad: [0, 0, 2, 2] });
    save('wrist_gauge.png', nine(gauge, 4, 3, 4, 4, 24, 1));
    // The forearm display round the bag: bumpers, a worn warning stripe, screws, signal pips and its light.
    const display = (docked) => {
      seed = 7;
      return piece({ ww: 140, wh: 60, l: 4, t: 5, r: 4, b: docked ? 3 : 4, rad: docked ? [3, 3, 0, 0] : [3, 3, 3, 3],
        rubbers: docked ? [[0, 0], [1, 0]] : [[0, 0], [1, 0], [0, 1], [1, 1]], scuffs: 500,
        deco: (A, W) => {
          screw(A, PAD + 9, PAD + 1, PAL_A); stripe(A, PAD + 14, PAD + 1, 36, 3);
          for (let i = 0; i < 4; i++) A.r(W - PAD - 30 + i * 3, PAD + 2, 2, 2, i < 3 ? LED_ON : PIP_OFF);
          led(A, W - PAD - 16, PAD + 2, 2, LED_ON); screw(A, W - PAD - 12, PAD + 1, PAL_A);
        } });
    };
    save('wrist_display.png', nine(display(false), 54, 9, 34, 9, 24, 8));
    save('wrist_display_docked.png', nine(display(true), 54, 9, 34, 4, 24, 8));
    // The open machine's module under the display: square on top, where it meets the display's bottom edge.
    seed = 23;
    const dock = piece({ ww: 120, wh: 60, l: 4, t: 2, r: 4, b: 4, rad: [0, 0, 3, 3], rubbers: [[0, 1], [1, 1]], scuffs: 400,
      deco: (A, W, H) => { screw(A, PAD + 9, H - PAD - 4, PAL_A); screw(A, W - PAD - 12, H - PAD - 4, PAL_A); } });
    save('wrist_dock.png', nine(dock, 13, 3, 13, 9, 24, 8));
    // The equipment's unit at the right edge.
    seed = 31;
    const body = piece({ ww: 68, wh: 100, l: 4, t: 5, r: 4, b: 4, rad: [3, 3, 3, 3], rubbers: [[0, 0], [1, 0], [0, 1], [1, 1]], scuffs: 300,
      deco: (A, W) => { screw(A, PAD + 9, PAD + 1, PAL_A); led(A, W - PAD - 16, PAD + 2, 2, LED_ON); screw(A, W - PAD - 12, PAD + 1, PAL_A); } });
    save('wrist_body.png', nine(body, 13, 9, 19, 9, 24, 8));
    // whole pieces, scaled up, for looking at
    for (const [n, A] of [['cuff', cuff], ['gauge', gauge], ['display', display(false)], ['docked', display(true)], ['dock', dock], ['body', body]]) save('look_' + n + '.png', up(A.c));
  } catch (e) { OUT.error = String(e && e.stack || e); }
  document.getElementById('out').textContent = JSON.stringify(OUT);
})();
