    const PAL_A = { o: '#15181e', b: '#4a5263', hi: '#6a7385', lo: '#3a404d', hi2: '#8a93a8' };
    const BX = 2, BY = 8, W = 333, H = 120;
    function plate(A) {
      const x = BX + 150, y = BY + 1; A.r(x, y, 34, 6, '#15181e'); A.r(x + 1, y + 1, 32, 4, '#5a6274'); A.p(x + 2, y + 2, PAL_A.hi2); A.p(x + 31, y + 2, PAL_A.hi2);
      for (const [a, b] of [[5, 12], [14, 17], [19, 28]]) A.r(x + a, y + 2, b - a, 1, '#3a404d'); A.r(x + 5, y + 3, 9, 1, '#7d8699');
    }
    function stylus(A) {
      const x = BX + W, top = BY - 7, bot = BY + 66;
      for (let y = top; y < bot; y++) { A.p(x, y, '#15181e'); A.p(x + 1, y, '#c3cad6'); A.p(x + 2, y, '#8a93a8'); A.p(x + 3, y, '#5a6274'); A.p(x + 4, y, '#15181e'); }
      A.r(x, top - 1, 5, 1, '#15181e');
      for (let y = top; y < top + 5; y++) { A.p(x + 1, y, '#ebc178'); A.p(x + 2, y, '#e0a030'); A.p(x + 3, y, '#a17323'); }
      A.r(x + 1, top + 5, 3, 1, '#5e4314');
      A.r(x + 1, bot, 3, 2, '#262b35'); A.p(x + 2, bot + 2, '#262b35'); A.p(x, bot, '#15181e'); A.p(x + 4, bot, '#15181e');
      A.p(x + 1, bot + 2, '#15181e'); A.p(x + 3, bot + 2, '#15181e'); A.p(x + 2, bot + 3, '#15181e');
      for (const cy of [BY + 14, BY + 46]) { A.r(x - 3, cy, 9, 4, '#15181e'); A.r(x - 2, cy + 1, 7, 2, '#3a404d'); A.r(x - 2, cy + 1, 7, 1, '#6a7385'); }
    }
    // The concept's variant A frame; bare: the ID plate's spot left as plain bezel and no stylus, for the 9-slice.
    function frame(bare) {
      seed = 7;
      const A = Art(W + 12, H + 8);
      A.shape(BX, BY, W, H, rr(W, H, 3), PAL_A);
      const SX = BX + 7, SY = BY + 8;
      A.r(SX - 2, SY - 2, 323, 106, PAL_A.lo);
      A.r(SX - 1, SY - 1, 321, 1, '#101318'); A.r(SX - 1, SY - 1, 1, 104, '#101318');
      A.r(SX - 1, SY + 102, 321, 1, '#5d6678'); A.r(SX + 319, SY - 1, 1, 104, '#5d6678');
      A.clear(SX, SY, 319, 102);
      for (let y = BY + 22; y < BY + 104; y += 26) { for (const x of [BX + 3, BX + W - 4]) { A.p(x, y, '#262b35'); A.p(x - 1, y - 1, PAL_A.hi); } }
      { const x0 = BX + 21, x1 = BX + 70;
        A.r(x0 - 1, BY + 1, x1 - x0 + 2, 6, '#15181e');
        for (let y = BY + 2; y < BY + 6; y++) for (let x = x0; x < x1; x++) {
          const amber = ((x + y) >> 2) & 1; let col = amber ? '#e0a030' : '#262b35';
          if (amber && rnd() < 0.16) col = '#a17323';
          if (amber && rnd() < 0.05) col = '#6a7385';
          A.p(x, y, col);
        } }
      const under = A.g.getImageData(BX + 150, BY + 1, 34, 6);
      plate(A);
      screw(A, BX + 15, BY + 2, PAL_A); screw(A, BX + W - 18, BY + 2, PAL_A);
      { const x = BX + 268, by = BY + 6;
        for (let i = 0; i < 4; i++) A.r(x + i * 2, by - i - 1, 1, i + 1, i < 3 ? '#4ade80' : '#1f3b2a');
        const bx = BX + 280; A.r(bx, BY + 2, 14, 5, '#15181e'); A.r(bx + 14, BY + 3, 1, 3, '#15181e');
        for (let i = 0; i < 4; i++) A.r(bx + 1 + i * 3, BY + 3, 2, 3, i < 3 ? '#4ade80' : '#1f3b2a'); }
      A.r(BX + 300, BY + 3, 2, 2, '#4ade80'); A.p(BX + 300, BY + 3, '#bbf7d0'); A.r(BX + 299, BY + 5, 4, 1, PAL_A.lo);
      scuff(A, PAL_A.b, { count: 900, scratch: '#7d8699', dent: '#3a404d', hi: '#5d6678', grime: '#434a59' });
      if (bare) A.g.putImageData(under, BX + 150, BY + 1);
      const rub = { o: '#0e1014', b: '#1f2229', hi: '#30343d', lo: '#17191e' };
      for (const [fx, fy] of [[0, 0], [1, 0], [0, 1], [1, 1]]) {
        const s = 14, inside = (x, y) => {
          if (x < 0 || y < 0 || x >= s || y >= s) return false;
          const lx = fx ? s - 1 - x : x, ly = fy ? s - 1 - y : y;
          if (lx + ly > 19) return false;
          return rr(s, s, 4)(lx, ly);
        };
        const ox = fx ? BX + W - s + 1 : BX - 1, oy = fy ? BY + H - s + 1 : BY - 1;
        A.shape(ox, oy, s, s, inside, rub);
        for (const k of [5, 9]) for (let y = 0; y < s; y++) for (let x = 0; x < s; x++) {
          const lx = fx ? s - 1 - x : x, ly = fy ? s - 1 - y : y;
          if (lx + ly === k + 2 && inside(x, y) && inside(x - 1, y) && inside(x + 1, y) && inside(x, y - 1) && inside(x, y + 1)) A.p(ox + x, oy + y, '#121418');
        }
      }
      if (!bare) stylus(A);
      return A;
    }
    const full = frame(false), bare = frame(true);
    save('full.png', full.c);
    // 9-slice source: top rows 0-20, one rivet period of the sides (rows 21-46), bottom rows 114-127.
    const [nine, ng] = cv(345, 21 + 26 + 14);
    ng.drawImage(bare.c, 0, 0, 345, 47, 0, 0, 345, 47);
    ng.drawImage(bare.c, 0, 114, 345, 14, 0, 47, 345, 14);
    save('tablet_frame.png', nine);
    const P = Art(345, 128); plate(P);
    const [pc, pg] = cv(34, 6); pg.drawImage(P.c, BX + 150, BY + 1, 34, 6, 0, 0, 34, 6); save('tablet_plate.png', pc);
    const S = Art(345, 128); stylus(S);
    const [sc, sg] = cv(9, 78); sg.drawImage(S.c, 332, 0, 9, 78, 0, 0, 9, 78); save('tablet_stylus.png', sc);
  } catch (e) { OUT.error = String(e && e.stack || e); }
  document.getElementById('out').textContent = JSON.stringify(OUT);
})();
