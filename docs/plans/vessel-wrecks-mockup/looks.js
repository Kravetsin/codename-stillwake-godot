'use strict';
// Looks: what a wreck shows over its cells and never changes on the grid — soot, frost, bent edges, pitting, patches, cables.
(function () {
  const D = SW.drawLook, hex = SW.hex, mixN = SW.mixN;
  const INK = '#101216';

  // A plate torn at a hole's edge, bent the way the force went: into the ship from outside, out of it from inside.
  D.petal = (g, m, L, liv) => {
    const r = SW.rng(L.seed);
    const ex = L.x + 0.5 + L.sx * 0.5, ey = L.y + 0.5 + L.sy * 0.5;
    const px = -L.sy, py = L.sx;
    const w = 0.4 + r() * 0.1;
    const b1 = [ex - px * w, ey - py * w], b2 = [ex + px * w, ey + py * w];
    const len = L.len;
    const tx = ex + L.sx * len * 0.55 + L.fx * len * 0.75 + px * (r() - 0.5) * 0.3;
    const ty = ey + L.sy * len * 0.55 + L.fy * len * 0.75 + py * (r() - 0.5) * 0.3;
    const mx = (b1[0] + tx) / 2 + L.fx * 0.12, my = (b1[1] + ty) / 2 + L.fy * 0.12;
    g.beginPath(); g.moveTo(b1[0], b1[1]); g.lineTo(mx, my); g.lineTo(tx, ty); g.lineTo(b2[0], b2[1]); g.closePath();
    g.fillStyle = hex(mixN(liv.hull, 0xffffff, 0.12)); g.fill();
    g.beginPath(); g.moveTo(b2[0], b2[1]); g.lineTo(tx, ty); g.lineTo((b1[0] + b2[0]) / 2, (b1[1] + b2[1]) / 2); g.closePath();
    g.fillStyle = hex(mixN(liv.hull, 0x000000, 0.45)); g.fill();
    g.beginPath(); g.moveTo(b1[0], b1[1]); g.lineTo(mx, my); g.lineTo(tx, ty); g.lineTo(b2[0], b2[1]);
    g.lineWidth = 0.07; g.strokeStyle = INK; g.stroke();
  };

  function blob(g, cx, cy, rad, seed, sq = 1) {
    const r = SW.rng(seed), n = 14;
    g.beginPath();
    for (let j = 0; j < n; j++) {
      const a = (j / n) * Math.PI * 2, rr = rad * (0.72 + r() * 0.42);
      const x = cx + Math.cos(a) * rr, y = cy + Math.sin(a) * rr * sq;
      j ? g.lineTo(x, y) : g.moveTo(x, y);
    }
    g.closePath();
  }

  // Soot round a blast: three hard steps of tone, darkest at the source, only where the region allows.
  D.soot = (g, m, L) => {
    g.save();
    g.clip(SW.maskPath(m, L.mask));
    for (let s = 0; s < 3; s++) {
      blob(g, L.cx, L.cy, L.r * (1 - s * 0.3), L.seed + s, 0.75);
      g.fillStyle = `rgba(16,11,8,${0.3 * L.strength})`;
      g.fill();
    }
    g.restore();
  };

  // Smoke out of an open door: a fan over its head along the next room's ceiling.
  D.plume = (g, m, L) => {
    g.save();
    g.clip(SW.maskPath(m, L.mask));
    const d = L.dir, x = L.x, y = L.y;
    g.beginPath();
    g.moveTo(x, y + L.h * 0.5);
    g.lineTo(x + d * 2, y - 0.2);
    g.lineTo(x + d * (6 + 6 * L.I), y - 0.2);
    g.lineTo(x + d * (5 + 3 * L.I), y + 1.6);
    g.lineTo(x + d * 2, y + L.h * 0.55);
    g.closePath();
    g.fillStyle = `rgba(14,10,8,${0.35 + 0.3 * L.I})`;
    g.fill();
    g.restore();
  };

  D.scorch = (g, m, L) => {
    g.save(); g.clip(SW.maskPath(m, L.mask));
    for (let s = 0; s < 2; s++) { blob(g, L.cx, L.cy, L.r * (1 - s * 0.4), L.seed + s, 0.7); g.fillStyle = 'rgba(20,14,10,0.42)'; g.fill(); }
    g.restore();
  };

  // A window the fire got to: dark glass, a sag of melt below, a crack.
  D.melted = (g, m, L) => {
    g.fillStyle = 'rgba(30,20,13,0.62)';
    g.fillRect(L.x, L.y, 1, 1);
    g.fillStyle = '#5a3c22';
    g.beginPath(); g.moveTo(L.x + 0.15, L.y + 0.85); g.quadraticCurveTo(L.x + 0.5, L.y + 1.25, L.x + 0.85, L.y + 0.85); g.closePath(); g.fill();
    g.strokeStyle = INK; g.lineWidth = 0.05;
    g.beginPath(); g.moveTo(L.x + 0.2, L.y + 0.2); g.lineTo(L.x + 0.55, L.y + 0.5); g.lineTo(L.x + 0.45, L.y + 0.8); g.stroke();
  };

  // Micrometeor pitting over every outer plate still there.
  D.pits = (g, m) => {
    for (let i = 0; i < m.cells.length; i++) {
      if (m.cells[i] !== 2 || !m.skin[i]) continue;
      const x = i % m.W, y = (i / m.W) | 0;
      const n = 2 + (SW.h32(i, 41) % 5);
      for (let j = 0; j < n; j++) {
        const px = x + SW.h01(i, j, 1), py = y + SW.h01(i, j, 2), rr = 0.035 + SW.h01(i, j, 3) * 0.06;
        g.fillStyle = 'rgba(22,22,24,0.75)';
        g.beginPath(); g.arc(px, py, rr, 0, Math.PI * 2); g.fill();
        g.fillStyle = 'rgba(235,235,230,0.35)';
        g.beginPath(); g.arc(px - rr * 0.6, py - rr * 0.6, rr * 0.5, 0, Math.PI * 2); g.fill();
      }
      if (SW.h32(i, 43) % 9 === 0) { g.fillStyle = 'rgba(40,36,30,0.18)'; g.fillRect(x, y, 1, 1); }
    }
  };

  // A crude plate riveted over a hole: a tone off the hull, askew, rivets round it.
  D.patch = (g, m, L, liv) => {
    const r = SW.rng(L.seed);
    g.save();
    g.translate(L.x + L.w / 2, L.y + L.h / 2);
    g.rotate((r() - 0.5) * 0.06);
    const w = L.w + 0.5, h = L.h + 0.4;
    g.fillStyle = hex(mixN(mixN(liv.hull, 0x5a4a3a, 0.35), 0x000000, 0.15));
    g.fillRect(-w / 2, -h / 2, w, h);
    g.fillStyle = 'rgba(140,80,40,0.35)';
    g.fillRect(-w / 2, h / 2 - 0.25, w, 0.25);
    g.lineWidth = 0.07; g.strokeStyle = INK; g.strokeRect(-w / 2, -h / 2, w, h);
    g.fillStyle = '#2a2a2a';
    for (let x = -w / 2 + 0.2; x < w / 2; x += 0.55) { g.fillRect(x, -h / 2 + 0.12, 0.09, 0.09); g.fillRect(x, h / 2 - 0.21, 0.09, 0.09); }
    g.restore();
  };

  // A tank burst from inside: its skin opened in petals curled back, its heart black.
  D.tankPetals = (g, m, L) => {
    const r = SW.rng(L.seed);
    g.fillStyle = 'rgba(10,8,6,0.85)';
    g.beginPath(); g.ellipse(L.cx, L.cy, L.rw * 0.55, L.rh * 0.5, 0, 0, Math.PI * 2); g.fill();
    const n = 7;
    for (let j = 0; j < n; j++) {
      const a = (j / n) * Math.PI * 2 + r() * 0.4;
      const bx = L.cx + Math.cos(a) * L.rw * 0.5, by = L.cy + Math.sin(a) * L.rh * 0.45;
      const len = (0.6 + r() * 0.6) * Math.min(L.rw, L.rh) * 0.5;
      const ca = a + (r() < 0.5 ? 1 : -1) * 0.5;
      const tx = bx + Math.cos(ca) * len, ty = by + Math.sin(ca) * len;
      const pa = a + Math.PI / 2, wv = 1.1 + r() * 0.6;
      const p1 = [bx + Math.cos(pa) * wv, by + Math.sin(pa) * wv], p2 = [bx - Math.cos(pa) * wv, by - Math.sin(pa) * wv];
      g.beginPath(); g.moveTo(p1[0], p1[1]);
      g.quadraticCurveTo(tx + Math.cos(pa) * 0.5, ty + Math.sin(pa) * 0.5, tx, ty);
      g.quadraticCurveTo(tx - Math.cos(pa) * 0.3, ty - Math.sin(pa) * 0.3, p2[0], p2[1]);
      g.closePath();
      g.fillStyle = j % 2 ? '#7f857b' : '#6d7268'; g.fill();
      g.beginPath(); g.moveTo(p2[0], p2[1]); g.quadraticCurveTo((bx + tx) / 2, (by + ty) / 2, tx, ty); g.lineTo(bx, by); g.closePath();
      g.fillStyle = '#2f2b26'; g.fill();
      g.beginPath(); g.moveTo(p1[0], p1[1]);
      g.quadraticCurveTo(tx + Math.cos(pa) * 0.5, ty + Math.sin(pa) * 0.5, tx, ty);
      g.quadraticCurveTo(tx - Math.cos(pa) * 0.3, ty - Math.sin(pa) * 0.3, p2[0], p2[1]);
      g.lineWidth = 0.09; g.strokeStyle = INK; g.stroke();
    }
  };

  // A frame bent where the hull tore away from it.
  D.ribStub = (g, m, L, liv) => {
    const col = hex(mixN(liv.hull, 0x303030, 0.4));
    g.lineCap = 'butt';
    g.strokeStyle = INK; g.lineWidth = 0.75;
    const path = () => { g.beginPath(); g.moveTo(L.x, L.y); g.quadraticCurveTo(L.x + L.dx * L.len * 0.6, L.y + L.dy * L.len * 0.6, L.x + L.dx * L.len + L.bx, L.y + L.dy * L.len + L.by); };
    path(); g.stroke();
    g.strokeStyle = col; g.lineWidth = 0.6; path(); g.stroke();
  };

  // Cracks over a smashed machine: its face darkened and split from one blow.
  D.cracks = (g, m, L) => {
    const r = SW.rng(L.seed * 13 + 7);
    const cx = L.x + L.w * (0.3 + r() * 0.4), cy = L.y + L.h * (0.25 + r() * 0.35);
    g.fillStyle = 'rgba(10,10,12,0.35)';
    g.fillRect(L.x + L.w * 0.12, L.y + L.h * 0.1, L.w * 0.76, L.h * 0.45);
    g.strokeStyle = INK; g.lineWidth = 0.07;
    for (let j = 0; j < 5; j++) {
      let x = cx, y = cy, a = r() * Math.PI * 2;
      g.beginPath(); g.moveTo(x, y);
      for (let s = 0; s < 3; s++) { x += Math.cos(a) * 0.45; y += Math.sin(a) * 0.45; a += (r() - 0.5) * 0.9; g.lineTo(x, y); }
      g.stroke();
    }
  };

  // A crate forced open: its inside dark, its lid leant against it.
  D.lid = (g, m, L, liv) => {
    g.fillStyle = 'rgba(12,10,8,0.7)';
    g.fillRect(L.x + 0.2, L.y + 0.12, L.w - 0.4, L.h * 0.38);
    g.save();
    g.translate(L.x + L.w + 0.05, L.y + L.h);
    g.rotate(-1.25);
    g.fillStyle = '#6f6a5c'; g.fillRect(0, 0, L.w * 0.9, 0.35);
    g.lineWidth = 0.06; g.strokeStyle = INK; g.strokeRect(0, 0, L.w * 0.9, 0.35);
    g.restore();
  };

  // A clean cut: straight faces, a band of heat tint round it.
  D.cutEdge = (g, m, L) => {
    g.strokeStyle = L.old ? 'rgba(160,160,155,0.7)' : 'rgba(150,110,170,0.55)';
    g.lineWidth = 0.14;
    g.beginPath();
    g.moveTo(L.x0, L.y0); g.lineTo(L.x0, L.y1);
    g.moveTo(L.x1, L.y0); g.lineTo(L.x1, L.y1);
    g.stroke();
    if (!L.old) {
      g.strokeStyle = 'rgba(190,140,60,0.45)'; g.lineWidth = 0.18;
      g.beginPath(); g.moveTo(L.x0 - 0.35, L.y0); g.lineTo(L.x0 - 0.35, L.y1); g.moveTo(L.x1 + 0.35, L.y0); g.lineTo(L.x1 + 0.35, L.y1); g.stroke();
    }
  };

  // Burnt room: smoke laid from the ceiling in tongues reaching along it toward the doors it went through (fire goes up and along),
  // a V up the wall from the seat, the floor charred, blistered and peeling paint where it was hot but not black. Hard steps of tone.
  D.burnRoom = (g, m, L) => {
    const r = SW.rng(L.seed), R = L.rect, I0 = L.I;
    g.save();
    g.clip(SW.maskPath(m, L.mask));
    const tongue = x => { let d = 0; for (const dr of L.doors) d += Math.max(0, 1 - Math.abs(x - dr.x) / (R.w * 0.55)) * R.h * (dr.out ? 0.35 : 0.2); return d; };
    for (const [f, a] of [[0.62, 0.2], [0.42, 0.28], [0.22, 0.45]]) {
      const base = R.h * f * (0.3 + 0.7 * I0);
      g.beginPath(); g.moveTo(R.x - 2, R.y - 1); g.lineTo(R.x + R.w + 2, R.y - 1);
      const n = Math.max(8, Math.round(R.w * 1.4));
      for (let j = n; j >= 0; j--) {
        const x = R.x - 2 + ((R.w + 4) * j) / n;
        const wob = (Math.sin(x * 1.3 + (L.seed % 97)) * 0.5 + Math.sin(x * 0.47 + (L.seed % 13))) * 0.7 * f + (r() - 0.5) * 0.6;
        g.lineTo(x, R.y + Math.min(R.h + 1, Math.max(0.3, base + tongue(x) * f + wob)));
      }
      g.closePath();
      g.fillStyle = `rgba(10,8,6,${a * (0.6 + 0.6 * I0)})`;
      g.fill();
    }
    if (L.seat) {
      const sx = L.seat.x, fy = R.y + R.h;
      for (const [w, a] of [[1, 0.42], [0.62, 0.5], [0.28, 0.72]]) {
        g.beginPath(); g.moveTo(sx - 0.8 * w - 0.3, fy); g.lineTo(sx - R.h * 0.8 * w - 0.5, R.y - 0.5); g.lineTo(sx + R.h * 0.8 * w + 0.5, R.y - 0.5); g.lineTo(sx + 0.8 * w + 0.3, fy); g.closePath();
        g.fillStyle = `rgba(7,5,4,${a})`; g.fill();
      }
    }
    g.fillStyle = `rgba(9,7,5,${0.3 + 0.4 * I0})`;
    g.fillRect(R.x - 2, R.y + R.h - 0.3 - I0 * 0.5, R.w + 4, 1.2);
    if (L.old) {
      // Long since: the soot settled and dusted over.
      for (let j = 0; j < R.w * R.h * 0.6; j++) { g.fillStyle = 'rgba(120,114,106,0.28)'; g.fillRect(R.x + r() * R.w, R.y + r() * R.h, 0.12, 0.08); }
    }
    const nb = Math.round(R.w * R.h * 0.45 * (1 - Math.abs(I0 - 0.55)));
    for (let j = 0; j < nb; j++) {
      const x = R.x + r() * R.w, y = R.y + R.h * (0.3 + r() * 0.6), rad = 0.07 + r() * 0.15;
      g.fillStyle = 'rgba(150,136,118,0.55)'; g.beginPath(); g.arc(x, y, rad, 0, Math.PI * 2); g.fill();
      g.strokeStyle = 'rgba(18,12,9,0.7)'; g.lineWidth = 0.04; g.beginPath(); g.arc(x + 0.02, y + 0.03, rad, 0.2, Math.PI * 1.2); g.stroke();
      if (j % 6 === 0) { g.strokeStyle = 'rgba(170,158,140,0.6)'; g.lineWidth = 0.06; g.beginPath(); g.arc(x + 0.3, y, 0.22, Math.PI, Math.PI * 1.8); g.stroke(); }
    }
    g.restore();
  };

  // A window frosted from inside when the power went: a white film, crystal ferns.
  D.frostGlass = (g, m, L) => {
    const r = SW.rng(L.x * 7919 + L.y);
    g.save();
    g.beginPath(); g.rect(L.x, L.y, 1, 1); g.clip();
    g.fillStyle = L.old ? 'rgba(180,190,196,0.5)' : 'rgba(214,234,246,0.55)';
    g.fillRect(L.x, L.y, 1, 1);
    g.strokeStyle = L.old ? 'rgba(210,214,216,0.6)' : 'rgba(250,253,255,0.9)';
    g.lineWidth = 0.05;
    for (let j = 0; j < 5; j++) {
      let x = L.x + r(), y = L.y + (r() < 0.5 ? 1 : r()), a = -Math.PI / 2 + (r() - 0.5) * 1.6;
      g.beginPath(); g.moveTo(x, y);
      for (let s = 0; s < 3; s++) {
        const nx = x + Math.cos(a) * 0.22, ny = y + Math.sin(a) * 0.22;
        g.lineTo(nx, ny);
        g.moveTo(nx, ny); g.lineTo(nx + Math.cos(a + 0.9) * 0.1, ny + Math.sin(a + 0.9) * 0.1);
        g.moveTo(nx, ny); g.lineTo(nx + Math.cos(a - 0.9) * 0.1, ny + Math.sin(a - 0.9) * 0.1);
        g.moveTo(nx, ny);
        x = nx; y = ny; a += (r() - 0.5) * 0.6;
      }
      g.stroke();
    }
    g.restore();
  };

  // Smoke still hanging in a sealed burnt room (fresh): a grey film, thicker under the ceiling.
  D.smoke = (g, m, L) => {
    const R = L.rect;
    g.save(); g.clip(SW.maskPath(m, L.mask));
    g.fillStyle = `rgba(78,72,68,${0.12 + 0.12 * L.I})`; g.fillRect(R.x - 2, R.y, R.w + 4, R.h);
    g.fillStyle = `rgba(60,56,54,${0.12 + 0.1 * L.I})`; g.fillRect(R.x - 2, R.y, R.w + 4, R.h * 0.45);
    g.restore();
  };

  // A doorway the heat went through: its edges warped and heat-tinted, its face sooted.
  D.warpDoor = (g, m, L) => {
    const r = SW.rng(L.seed);
    g.fillStyle = `rgba(14,10,8,${0.25 + 0.35 * L.I})`; g.fillRect(L.x, L.y, 2, L.h);
    for (const ex of [L.x + 0.12, L.x + 1.88]) {
      g.beginPath();
      for (let y = 0; y <= L.h; y += 0.25) { const x = ex + Math.sin(y * 2.3 + r() * 0.6) * 0.12 * L.I; y ? g.lineTo(x, L.y + y) : g.moveTo(x, L.y); }
      g.strokeStyle = 'rgba(176,112,52,0.55)'; g.lineWidth = 0.14; g.stroke();
      g.strokeStyle = INK; g.lineWidth = 0.05; g.stroke();
    }
  };

  // A shut door that stopped the fire: black on its burnt side to a crisp line, clean on the other.
  D.doorScorch = (g, m, L) => {
    const x = L.side < 0 ? L.x : L.x + 1;
    g.fillStyle = `rgba(9,7,5,${0.55 + 0.3 * L.I})`; g.fillRect(x, L.y, 1, L.h);
    g.fillStyle = 'rgba(160,100,45,0.35)'; g.fillRect(L.side < 0 ? L.x + 0.9 : L.x + 1, L.y, 0.12, L.h);
    g.strokeStyle = INK; g.lineWidth = 0.06; g.beginPath(); g.moveTo(L.x + 1, L.y); g.lineTo(L.x + 1, L.y + L.h); g.stroke();
  };

  // Plastic that sagged: its top edge slumped, drips hung and set below it.
  D.sag = (g, m, L) => {
    const r = SW.rng(L.seed);
    g.strokeStyle = 'rgba(20,14,10,0.8)'; g.lineWidth = 0.1;
    g.beginPath();
    for (let j = 0; j <= 8; j++) { const x = L.x + (L.w * j) / 8, y = L.y + 0.15 + Math.sin((j / 8) * Math.PI) * 0.35 * L.I; j ? g.lineTo(x, y) : g.moveTo(x, y); }
    g.stroke();
    for (let j = 0, n = 2 + r.int(3); j < n; j++) {
      const x = L.x + 0.3 + r() * (L.w - 0.6), y = L.y + L.h * (0.45 + r() * 0.5), len = 0.25 + r() * 0.7 * L.I;
      g.fillStyle = '#2a1f17';
      g.beginPath(); g.moveTo(x - 0.1, y); g.quadraticCurveTo(x - 0.12, y + len, x, y + len + 0.12); g.quadraticCurveTo(x + 0.12, y + len, x + 0.1, y); g.closePath(); g.fill();
      g.fillStyle = 'rgba(200,170,130,0.4)'; g.fillRect(x - 0.03, y + len * 0.4, 0.04, len * 0.4);
    }
  };

  // Temper colours on the metal round a blast: blue nearest, bronze, then straw; hard steps, only on walls.
  D.heat = (g, m, L) => {
    g.save(); g.clip(SW.maskPath(m, L.mask));
    for (const [f, col] of [[1, 'rgba(206,176,96,0.32)'], [0.68, 'rgba(158,94,42,0.38)'], [0.38, 'rgba(72,88,168,0.42)']]) {
      blob(g, L.cx, L.cy, L.r * f, Math.round(L.cx * 31 + f * 7), 0.8); g.fillStyle = col; g.fill();
    }
    g.restore();
  };

  // Metal that ran and froze: beads hanging under torn edges, drops set on the floor.
  D.beads = (g, m, L) => {
    for (const [x, y, s, hang] of L.pts) {
      g.fillStyle = '#9ea2a6';
      g.beginPath();
      if (hang) { g.moveTo(x - s * 0.5, y); g.quadraticCurveTo(x - s, y + s * 1.6, x, y + s * 2); g.quadraticCurveTo(x + s, y + s * 1.6, x + s * 0.5, y); }
      else g.ellipse(x, y, s, s * 0.6, 0, 0, Math.PI * 2);
      g.closePath(); g.fill();
      g.lineWidth = 0.04; g.strokeStyle = INK; g.stroke();
      g.fillStyle = 'rgba(255,255,255,0.7)'; g.fillRect(x - s * 0.3, y + (hang ? s : -s * 0.2), s * 0.3, s * 0.3);
    }
  };

  // Soot dragged by the air rushing out: streaks pointing at the breach, thick where they start.
  D.streaks = (g, m, L) => {
    g.save(); g.clip(SW.maskPath(m, L.mask));
    for (const [x0, y0, x1, y1, w] of L.pts) {
      const dx = x1 - x0, dy = y1 - y0, l = Math.hypot(dx, dy) || 1, px = (-dy / l) * w, py = (dx / l) * w;
      g.beginPath(); g.moveTo(x0 + px, y0 + py); g.lineTo(x1, y1); g.lineTo(x0 - px, y0 - py); g.closePath();
      g.fillStyle = 'rgba(12,9,7,0.55)'; g.fill();
    }
    g.restore();
  };

  // A fan of frozen spray out of a breach: fine rays and a pale wedge.
  D.iceFan = (g, m, L) => {
    const r = SW.rng(L.seed), base = Math.atan2(L.uy, L.ux), a0 = L.old ? 0.5 : 1;
    const gr = g.createRadialGradient(L.x, L.y, 0, L.x, L.y, L.len);
    gr.addColorStop(0, `rgba(225,240,250,${0.22 * a0})`); gr.addColorStop(1, 'rgba(225,240,250,0)');
    g.fillStyle = gr;
    g.beginPath(); g.moveTo(L.x, L.y); g.arc(L.x, L.y, L.len, base - L.spread / 2, base + L.spread / 2); g.closePath(); g.fill();
    g.lineWidth = 0.05;
    for (let j = 0; j < 30; j++) {
      const a = base + (r() - 0.5) * L.spread, l = L.len * (0.3 + r() * 0.7);
      g.strokeStyle = `rgba(235,247,255,${(0.15 + r() * 0.25) * a0})`;
      g.beginPath(); g.moveTo(L.x + Math.cos(a) * 0.5, L.y + Math.sin(a) * 0.5); g.lineTo(L.x + Math.cos(a) * l, L.y + Math.sin(a) * l); g.stroke();
    }
  };

  // Frost on the plates round a breach: white specks thinning out.
  D.hullFrost = (g, m, L) => {
    const r = SW.rng(L.seed);
    g.save(); g.clip(SW.maskPath(m, L.mask));
    const n = Math.round(L.r * L.r * 3);
    for (let j = 0; j < n; j++) {
      const a = r() * Math.PI * 2, d = Math.pow(r(), 0.6) * L.r;
      const x = L.cx + Math.cos(a) * d, y = L.cy + Math.sin(a) * d * 0.8, s = 0.05 + r() * 0.12;
      g.fillStyle = L.old ? 'rgba(170,178,184,0.5)' : `rgba(232,244,252,${0.85 - (d / L.r) * 0.5})`;
      g.fillRect(x, y, s, s);
    }
    g.restore();
  };

  // Rime inside the vented rooms near the breach: crust on floors, hoarfrost spikes under ceilings, specks on walls.
  D.hoar = (g, m, L) => {
    const col = L.old ? '168,176,182' : '226,240,250';
    const x0 = Math.max(0, Math.floor(L.cx - L.reach)), x1 = Math.min(m.W - 1, Math.ceil(L.cx + L.reach));
    const y0 = Math.max(0, Math.floor(L.cy - L.reach)), y1 = Math.min(m.H - 1, Math.ceil(L.cy + L.reach));
    const wall = (x, y) => x >= 0 && y >= 0 && x < m.W && y < m.H && m.cells[y * m.W + x] >= 2;
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const i = y * m.W + x;
      if (!L.mask[i] || m.cells[i] !== 1) continue;
      const f = L.even ? 0.7 : 1 - Math.hypot(x - L.cx, y - L.cy) / L.reach;
      if (f <= 0) continue;
      const a = (L.old ? 0.35 : 0.7) * (0.35 + 0.65 * f);
      g.fillStyle = `rgba(${col},${a})`;
      if (wall(x, y + 1)) {
        g.fillRect(x, y + 0.8, 1, 0.2);
        for (let k = 0; k < 3; k++) { const px = x + SW.h01(i, k, 3); g.beginPath(); g.moveTo(px - 0.12, y + 0.8); g.lineTo(px, y + 0.6 - SW.h01(i, k, 4) * 0.15); g.lineTo(px + 0.12, y + 0.8); g.fill(); }
      }
      if (wall(x, y - 1)) for (let k = 0; k < 4; k++) { const px = x + SW.h01(i, k, 5); g.beginPath(); g.moveTo(px - 0.07, y); g.lineTo(px, y + 0.15 + SW.h01(i, k, 6) * 0.3 * f); g.lineTo(px + 0.07, y); g.fill(); }
      if (wall(x - 1, y) || wall(x + 1, y)) { const ex = wall(x - 1, y) ? x : x + 0.82; for (let k = 0; k < 3; k++) g.fillRect(ex + SW.h01(i, k, 7) * 0.18, y + SW.h01(i, k, 8), 0.08, 0.08); }
      if (SW.h01(i, 9) < 0.35 * f) g.fillRect(x + SW.h01(i, 10), y + SW.h01(i, 11), 0.07, 0.07);
    }
  };

  // Icicles from a burst pipe in the ceiling; a stream frozen from a burst tank.
  D.icicle = (g, m, L) => {
    const r = SW.rng(L.seed), fill = L.old ? 'rgba(170,180,186,0.7)' : 'rgba(220,238,250,0.88)';
    if (L.stream) {
      const h = L.y1 - L.y;
      if (h <= 0.2) return;
      g.fillStyle = fill;
      g.beginPath(); g.moveTo(L.x - 0.2, L.y);
      for (let k = 0; k <= 6; k++) g.lineTo(L.x - 0.15 - Math.sin(k * 1.7 + (L.seed % 7)) * 0.12 - (k / 6) * 0.1, L.y + (h * k) / 6);
      for (let k = 6; k >= 0; k--) g.lineTo(L.x + 0.15 + Math.sin(k * 1.3 + (L.seed % 5)) * 0.12 + (k / 6) * 0.1, L.y + (h * k) / 6);
      g.closePath(); g.fill();
      g.strokeStyle = 'rgba(60,80,95,0.6)'; g.lineWidth = 0.04; g.stroke();
      g.fillStyle = 'rgba(255,255,255,0.6)'; g.fillRect(L.x - 0.05, L.y + 0.2, 0.06, Math.max(0, h - 0.4));
      return;
    }
    for (let k = 0, n = 3 + r.int(3); k < n; k++) {
      const x = L.x + (r() - 0.5) * 1.4, w = 0.1 + r() * 0.16, len = L.len * (0.4 + r() * 0.6);
      g.fillStyle = fill;
      g.beginPath(); g.moveTo(x - w, L.y); g.lineTo(x, L.y + len); g.lineTo(x + w, L.y); g.closePath(); g.fill();
      g.strokeStyle = 'rgba(60,80,95,0.55)'; g.lineWidth = 0.03; g.stroke();
    }
  };

  // Accordion folds where the keel buckled before it broke.
  D.folds = (g, m, L) => {
    g.save(); g.clip(SW.maskPath(m, L.mask));
    for (let x = L.x0; x < L.x1; x += 1.3) {
      for (const [col, off] of [['rgba(16,16,18,0.6)', 0], ['rgba(240,238,230,0.35)', 0.1]]) {
        g.strokeStyle = col; g.lineWidth = 0.08; g.beginPath();
        for (let y = 0, k = 0; y < m.H; y += 0.7, k++) g.lineTo(x + off + (k % 2 ? 0.45 : 0), y);
        g.stroke();
      }
    }
    g.restore();
  };

  // Stress cracks branching from the break across the plates.
  D.stress = (g, m, L) => {
    const r = SW.rng(L.seed);
    g.save(); g.clip(SW.maskPath(m, L.mask));
    const crack = (x, y, a, len, depth) => {
      g.beginPath(); g.moveTo(x, y);
      const pts = [];
      for (let s = 0; s < len; s += 0.5) { a += (r() - 0.5) * 0.7; x += Math.cos(a) * 0.5; y += Math.sin(a) * 0.5; g.lineTo(x, y); pts.push([x, y, a]); }
      g.strokeStyle = 'rgba(14,14,16,0.85)'; g.lineWidth = 0.07 + depth * 0.03; g.stroke();
      if (depth > 0) for (const p of pts) if (r() < 0.12) crack(p[0], p[1], p[2] + (r() < 0.5 ? 0.8 : -0.8), len * 0.4, depth - 1);
    };
    crack(L.x, L.y, L.dir > 0 ? 0 : Math.PI, L.len, 2);
    g.restore();
  };

  // A soft haze of dust.
  D.haze = (g, m, L) => {
    g.save(); g.translate(L.cx, L.cy); g.scale(1, L.ry / L.rx);
    const gr = g.createRadialGradient(0, 0, 0, 0, 0, L.rx);
    gr.addColorStop(0, `rgba(${L.col},${L.a})`); gr.addColorStop(0.6, `rgba(${L.col},${L.a * 0.45})`); gr.addColorStop(1, `rgba(${L.col},0)`);
    g.fillStyle = gr; g.fillRect(-L.rx, -L.rx, L.rx * 2, L.rx * 2);
    g.restore();
  };

  // Cables (two wires) and pipes hanging from torn decks; a pipe's end a flange.
  D.cable = (g, m, L) => {
    const ex = L.x + L.sway * 1.1, ey = L.y + L.len;
    const path = off => { g.beginPath(); g.moveTo(L.x + off, L.y); g.bezierCurveTo(L.x + off + L.sway * 0.3, L.y + L.len * 0.4, L.x + off + L.sway, L.y + L.len * 0.7, ex + off, ey); };
    if (L.pipe) {
      path(0); g.lineCap = 'butt'; g.strokeStyle = INK; g.lineWidth = 0.42; g.stroke();
      path(0); g.strokeStyle = '#7a7e82'; g.lineWidth = 0.3; g.stroke();
      path(-0.06); g.strokeStyle = 'rgba(230,230,225,0.35)'; g.lineWidth = 0.06; g.stroke();
      g.fillStyle = '#55595d'; g.fillRect(ex - 0.28, ey - 0.08, 0.56, 0.16);
      return;
    }
    for (let k = 0; k < 2; k++) { path(k * 0.22); g.strokeStyle = k ? '#7a2420' : '#15171a'; g.lineWidth = 0.16; g.stroke(); }
    g.strokeStyle = '#c4823e'; g.lineWidth = 0.05;
    g.beginPath(); g.moveTo(ex, ey); g.lineTo(ex - 0.12, ey + 0.25); g.moveTo(ex, ey); g.lineTo(ex + 0.1, ey + 0.28); g.stroke();
  };

  // A stone of the asteroid: dark grey-brown, faceted, lit from above.
  SW.drawRock = (g, poly, size) => {
    const outline = () => { g.beginPath(); poly.forEach(([x, y], j) => (j ? g.lineTo(x * size, y * size) : g.moveTo(x * size, y * size))); g.closePath(); };
    outline(); g.fillStyle = '#5c5045'; g.fill();
    g.save(); outline(); g.clip();
    g.fillStyle = '#3d342d'; g.beginPath(); g.moveTo(-size, size * 0.1); g.lineTo(size, -size * 0.3); g.lineTo(size, size); g.lineTo(-size, size); g.fill();
    g.fillStyle = 'rgba(150,130,108,0.45)'; g.beginPath(); g.moveTo(-size * 0.7, -size); g.lineTo(size * 0.1, -size); g.lineTo(-size * 0.2, -size * 0.1); g.fill();
    g.restore();
    outline(); g.lineWidth = Math.max(0.06, size * 0.06); g.strokeStyle = INK; g.stroke();
  };

  // Things lying about, drawn in both views: they are on the grid.
  SW.drawItem = (g, it) => {
    if (it.t === 'rock') { g.save(); g.translate(it.x, it.y); g.rotate(it.rot); SW.drawRock(g, it.poly, it.size); g.restore(); return; }
    if (it.t === 'extinguisher') {
      g.save();
      g.translate(it.x, it.y);
      g.rotate(it.flip ? -1.45 : 1.45);
      g.fillStyle = '#b3261e'; g.fillRect(-0.32, -1.5, 0.64, 1.5);
      g.fillStyle = '#1a1a1a'; g.fillRect(-0.18, -1.75, 0.36, 0.28);
      g.lineWidth = 0.07; g.strokeStyle = INK; g.strokeRect(-0.32, -1.5, 0.64, 1.5);
      g.strokeStyle = '#1a1a1a'; g.lineWidth = 0.1;
      g.beginPath(); g.moveTo(0.1, -1.65); g.quadraticCurveTo(0.9, -1.4, 0.7, -0.6); g.stroke();
      g.fillStyle = 'rgba(255,255,255,0.25)'; g.fillRect(-0.22, -1.35, 0.1, 1.1);
      g.restore();
    }
  };
})();
