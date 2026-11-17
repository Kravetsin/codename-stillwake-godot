'use strict';
// Events that happen to a vessel, stacked in time: explosion, fire, icing, asteroid, break. Each applied to the real cells and parts from
// its own seeded draw, on top of what came before. Geometry on the grid; looks and particles apart, each dulled by its event's age.
(function () {
  const N4 = [[1, 0], [-1, 0], [0, 1], [0, -1]];
  const I = (m, x, y) => y * m.W + x;
  const inb = (m, x, y) => x >= 0 && y >= 0 && x < m.W && y < m.H;
  const cellAt = (m, x, y) => (inb(m, x, y) ? m.cells[I(m, x, y)] : 0);

  function foot(m, mc) {
    const a = [];
    for (let y = mc.y; y < mc.y + mc.h; y++) for (let x = mc.x; x < mc.x + mc.w; x++) if (inb(m, x, y)) a.push(I(m, x, y));
    return a;
  }
  function structCells(m, s) {
    const a = [];
    for (let dy = 0; dy < s.n; dy++) for (let dx = 0; dx < s.n; dx++) if (inb(m, s.x + dx, s.y + dy)) a.push(I(m, s.x + dx, s.y + dy));
    return a;
  }
  function blockedMap(m) {
    const b = new Uint8Array(m.W * m.H);
    for (const s of m.structs) if ((s.k === 'door' || s.k === 'hatch') && !s.open) for (const i of structCells(m, s)) b[i] = 1;
    return b;
  }
  const lamps = m => m.machines.filter(mc => mc.k === 'vesselLamp' && !mc.gone);
  const roomsOf = (m, role) => m.rooms.filter(r => r.role === role);
  const inRoom = (rm, x, y) => x >= rm.x && x < rm.x + rm.w && y >= rm.y && y < rm.y + rm.h;
  const maskOf = (m, test) => { const a = new Uint8Array(m.W * m.H); for (let i = 0; i < a.length; i++) if (test(i, i % m.W, (i / m.W) | 0)) a[i] = 1; return a; };
  const topRow = (m, x) => { let y = 0; while (y < m.H && cellAt(m, x, y) < 2) y++; return y; };

  /** Removes cells (and what stood on them); a skin cell of a rib stays when asked: the frame across the hole. */
  function carve(m, list, opt = {}) {
    const set = new Set();
    for (const i of list) {
      const c = m.cells[i];
      if (!c) continue;
      const x = i % m.W, y = (i / m.W) | 0;
      if (opt.keepRibs && c === 2 && m.skin[i] && SW.ribAt(m, x, y)) continue;
      m.cells[i] = 0; m.dressOf[i] = null; set.add(i);
    }
    m.structs = m.structs.filter(s => !structCells(m, s).some(i => set.has(i)));
    const hit = [];
    for (const mc of m.machines) if (!mc.gone && !mc.outside && foot(m, mc).some(i => set.has(i))) hit.push(mc);
    for (const i of set) m.carved.add(i);
    return { set, hit };
  }

  /** The air that left: every room cell reached from a cell open to space, through open doors and hatches, not through shut ones. */
  function ventAll(m) {
    const blocked = blockedMap(m), seen = new Uint8Array(m.W * m.H), q = [];
    for (let i = 0; i < m.cells.length; i++) {
      if (m.cells[i] !== 1 || blocked[i]) continue;
      const x = i % m.W, y = (i / m.W) | 0;
      if (N4.some(([dx, dy]) => cellAt(m, x + dx, y + dy) === 0)) { seen[i] = 1; q.push(i); }
    }
    while (q.length) {
      const i = q.pop(), x = i % m.W, y = (i / m.W) | 0;
      for (const [dx, dy] of N4) {
        const nx = x + dx, ny = y + dy;
        if (!inb(m, nx, ny)) continue;
        const n = I(m, nx, ny);
        if (!seen[n] && m.cells[n] === 1 && !blocked[n]) { seen[n] = 1; q.push(n); }
      }
    }
    m.vented = seen;
    for (const mc of lamps(m)) if (seen[I(m, mc.x, mc.y)] || seen[I(m, mc.x + 1, mc.y)]) mc.light = 'off';
    return seen;
  }

  function holesOf(m, vented) {
    const holes = new Map();
    for (let i = 0; i < vented.length; i++) {
      if (!vented[i]) continue;
      const x = i % m.W, y = (i / m.W) | 0;
      for (const [dx, dy] of N4) {
        const nx = x + dx, ny = y + dy;
        if (!inb(m, nx, ny) || cellAt(m, nx, ny) !== 0) continue;
        const k = I(m, nx, ny);
        const h = holes.get(k) || { x: nx, y: ny, ux: 0, uy: 0 };
        h.ux += dx; h.uy += dy;
        holes.set(k, h);
      }
    }
    return [...holes.values()].map(h => { const l = Math.hypot(h.ux, h.uy) || 1; h.ux /= l; h.uy /= l; return h; });
  }

  function occupancy(m) {
    const o = new Int32Array(m.W * m.H).fill(-1);
    for (const mc of m.machines) if (!mc.gone && !mc.outside && mc.k !== 'vesselLamp') for (const i of foot(m, mc)) o[i] = mc.id;
    return o;
  }
  function canPlace(m, occ, mc, nx, ny) {
    let floor = false;
    for (let y = ny; y < ny + mc.h; y++) for (let x = nx; x < nx + mc.w; x++) {
      if (!inb(m, x, y) || m.cells[I(m, x, y)] !== 1) return false;
      const o = occ[I(m, x, y)];
      if (o >= 0 && o !== mc.id) return false;
    }
    for (let x = nx; x < nx + mc.w; x++) if (cellAt(m, x, ny + mc.h) >= 2) floor = true;
    return floor;
  }
  function moveBy(m, occ, mc, dx, max) {
    for (const i of foot(m, mc)) if (occ[i] === mc.id) occ[i] = -1;
    let moved = 0;
    while (moved < max && canPlace(m, occ, mc, mc.x + dx, mc.y)) { mc.x += dx; moved++; }
    for (const i of foot(m, mc)) occ[i] = mc.id;
    return moved;
  }

  // ---------- things in space ----------
  function shardPoly(r) {
    const n = 4 + r.int(3), pts = [];
    for (let j = 0; j < n; j++) { const a = (j / n) * Math.PI * 2 + r() * 0.7, rr = 0.35 + r() * 0.7; pts.push([Math.cos(a) * rr, Math.sin(a) * rr * (0.4 + r() * 0.4)]); }
    return pts;
  }
  function rockPoly(r) {
    const n = 7 + r.int(4), pts = [];
    for (let j = 0; j < n; j++) { const a = (j / n) * Math.PI * 2 + r() * 0.4, rr = 0.6 + r() * 0.45; pts.push([Math.cos(a) * rr, Math.sin(a) * rr * (0.65 + r() * 0.3)]); }
    return pts;
  }
  function drifter(m, r, o) {
    const d = Object.assign({ amp: 0.3 + r() * 0.9, om: 0.12 + r() * 0.22, ph: r() * 6.28, rot: r() * 6.28, vr: (r() - 0.5) * 0.35 }, o);
    if (d.kind === 'sprite') { d.vr *= 0.35; d.rot = (r() - 0.5) * 1.2; }
    if (d.fx === undefined) { d.fx = d.x; d.fy = d.y; }
    if (m.old && d.kind !== 'tank') { if (!r.chance(0.3)) return null; d.x = d.fx + (d.x - d.fx) * 1.8; d.y = d.fy + (d.y - d.fy) * 1.8; }
    m.drifters.push(d);
    return d;
  }
  function trail(m, r, x0, y0, ux, uy, n, o = {}) {
    const d0 = o.d0 ?? 2, d1 = o.d1 ?? 30, spread = o.spread ?? 1.4;
    for (let j = 0; j < n; j++) {
      const u = (j + r()) / n, dist = d0 + (d1 - d0) * Math.pow(u, 0.85);
      const perp = (r() - 0.5) * spread * (0.4 + (dist / d1) * 2);
      const x = x0 + ux * dist - uy * perp, y = y0 + uy * dist + ux * perp;
      const roll = r();
      if (o.sprites && roll < (o.spriteShare ?? 0.12)) {
        const k = r.pick(o.sprites), s = m.sizes[k] || { w: 2, h: 2 };
        drifter(m, r, { kind: 'sprite', name: k, w: s.w, h: s.h, x, y, fx: x0, fy: y0, vx: ux, vy: uy, burnt: o.burnt });
      } else if (o.rocks && roll < 0.24) drifter(m, r, { kind: 'rock', poly: rockPoly(r), size: 0.35 + r() * 0.9, x, y, fx: x0, fy: y0, vx: ux, vy: uy });
      else if (roll < 0.72) drifter(m, r, { kind: 'shard', poly: shardPoly(r), size: o.size ?? (0.45 + r() * 1.2), x, y, fx: x0, fy: y0, vx: ux, vy: uy, col: r() < 0.22 ? 'rib' : 'hull', bent: r() < 0.4 });
      else drifter(m, r, { kind: 'bit', size: 0.22 + r() * 0.3, col: r.pick(['#8f9599', '#6e6a63', '#a7a294', '#4f5256']), x, y, fx: x0, fy: y0, vx: ux, vy: uy });
    }
  }
  function eject(m, r, mc, hx, hy, ux, uy, dist, extra = {}) {
    mc.gone = true;
    const x = hx + ux * dist + (r() - 0.5) * 3, y = hy + uy * dist + (r() - 0.5) * 3;
    return drifter(m, r, Object.assign({ kind: 'sprite', name: mc.k, w: mc.w, h: mc.h, x, y, fx: mc.x + mc.w / 2, fy: mc.y + mc.h / 2, vx: ux, vy: uy, burnt: mc.burnt > 0.5, frost: mc.frost }, extra));
  }

  function pull(m, r, holes, reach, out, dir) {
    if (!holes.length) return;
    const occ = occupancy(m);
    // Only rooms a hole opens into lose their things; a room merely vented through a door keeps them.
    const breached = m.rooms.filter(rm => holes.some(h => h.x >= rm.x - 1 && h.x <= rm.x + rm.w && h.y >= rm.y - 1 && h.y <= rm.y + rm.h));
    const loose = m.machines.filter(mc => !mc.gone && !mc.outside && SW.LOOSE.has(mc.k) && foot(m, mc).every(i => m.vented[i])
      && breached.some(rm => inRoom(rm, mc.x + (mc.w >> 1), mc.y + mc.h - 1)));
    for (const mc of loose) {
      const cx = mc.x + mc.w / 2, cy = mc.y + mc.h / 2;
      let best = null, bd = 1e9;
      for (const h of holes) { const d = Math.hypot(h.x + 0.5 - cx, (h.y + 0.5 - cy) * 1.8); if (d < bd) { bd = d; best = h; } }
      if (!best || bd > reach) continue;
      if (bd < reach * 0.35 && r.chance(SW.SMALL.has(mc.k) ? 0.8 : 0.45)) {
        for (const i of foot(m, mc)) if (occ[i] === mc.id) occ[i] = -1;
        const ux = dir ? dir[0] : best.ux, uy = dir ? dir[1] : best.uy;
        eject(m, r, mc, best.x + 0.5, best.y + 0.5, ux, uy, out[0] + r() * (out[1] - out[0]));
        continue;
      }
      const step = Math.sign(best.x + 0.5 - cx);
      if (!step) continue;
      const max = Math.min(Math.max(1, Math.floor(Math.abs(best.x + 0.5 - cx) - mc.w / 2)), 2 + r.int(Math.ceil(reach / 2)));
      if (moveBy(m, occ, mc, step, max)) mc.tilt = step * (0.04 + r() * 0.12);
    }
  }

  function petals(m, r, set, force, prob = 0.55, hot = null) {
    for (const i of set) {
      const x = i % m.W, y = (i / m.W) | 0;
      for (const [dx, dy] of N4) {
        const wx = x - dx, wy = y - dy;
        if (cellAt(m, wx, wy) < 2 || !r.chance(prob)) continue;
        const f = typeof force === 'function' ? force(wx, wy) : force;
        m.looks.push({ t: 'petal', layer: 'top', x: wx, y: wy, sx: dx, sy: dy, fx: f[0], fy: f[1], len: 0.7 + r() * 0.9, seed: r.int(1e9) });
        if (m.skin[I(m, wx, wy)] || cellAt(m, wx, wy + 1) === 1) m.sparkSpots.push({ x: x + 0.5, y: y + 0.5 });
        if (hot && !m.old && Math.hypot(wx - hot.x, wy - hot.y) < hot.r) m.hot.push({ x: wx + 0.5 + dx * 0.5, y: wy + 0.5 + dy * 0.5, ph: r() * 6 });
      }
    }
  }

  function shiftArmour(m, x0, x1, cy, out, near) {
    const occ = occupancy(m);
    for (let x = x0; x <= x1; x++) {
      if (near && !near(x)) continue;
      for (const up of [true, false]) {
        if (!out && !up) continue;
        let y = up ? 0 : m.H - 1; const step = up ? 1 : -1;
        while (inb(m, x, y) && m.cells[I(m, x, y)] === 0) y += step;
        if (!inb(m, x, y) || m.cells[I(m, x, y)] !== 2) continue;
        if (up ? y > cy : y < cy) continue;
        let th = 0;
        while (th < 4 && cellAt(m, x, y + step * th) === 2) th++;
        const inner = y + step * th;
        if (cellAt(m, x, inner) !== 1 || th < 2) continue;
        if (out) {
          const o = I(m, x, y - step), last = I(m, x, y + step * (th - 1));
          if (!inb(m, x, y - step) || m.cells[o] !== 0) continue;
          m.cells[o] = 2; m.dressOf[o] = m.dressOf[I(m, x, y)]; m.skin[o] = 1;
          for (let k = th - 1; k > 0; k--) { const a = I(m, x, y + step * k), b = I(m, x, y + step * (k - 1)); m.dressOf[a] = m.dressOf[b]; }
          m.cells[last] = 1; m.dressOf[last] = null; m.skin[last] = 0;
        } else {
          const ii = I(m, x, inner);
          if (occ[ii] >= 0) continue;
          for (const mc of lamps(m)) if (foot(m, mc).includes(ii)) mc.gone = true;
          for (let k = th; k > 0; k--) { const a = I(m, x, y + k), b = I(m, x, y + k - 1); m.cells[a] = 2; m.dressOf[a] = m.dressOf[b]; m.skin[a] = 1; }
          const t = I(m, x, y); m.cells[t] = 0; m.dressOf[t] = null; m.skin[t] = 0;
        }
      }
    }
  }

  function buckleDecks(m, cx, R, maxShift) {
    const inner = m.decks.slice(0, -1);
    const occ = occupancy(m);
    const structAt = new Set();
    for (const s of m.structs) for (const i of structCells(m, s)) structAt.add(i);
    for (const yd of inner) {
      for (let x = Math.floor(cx - R - 6); x <= cx + R + 6; x++) {
        if (!inb(m, x, yd) || m.cells[I(m, x, yd)] !== 2 || structAt.has(I(m, x, yd))) continue;
        const d = Math.abs(x + 0.5 - cx);
        let sh = d < R + 1.5 ? maxShift : d < R + 6 ? 1 : 0;
        while (sh > 0) {
          let ok = true;
          for (let k = 1; k <= sh; k++) { const i = I(m, x, yd + k); if (cellAt(m, x, yd + k) !== 1 || occ[i] >= 0 || structAt.has(i)) ok = false; }
          if (ok) break;
          sh--;
        }
        if (!sh) continue;
        for (const mc of lamps(m)) for (let k = 1; k <= sh; k++) if (foot(m, mc).includes(I(m, x, yd + k))) mc.gone = true;
        const a = I(m, x, yd), b = I(m, x, yd + sh);
        m.cells[b] = 2; m.dressOf[b] = m.dressOf[a]; m.cells[a] = 1; m.dressOf[a] = null;
        for (const mc of m.machines) if (!mc.gone && !mc.outside && mc.y + mc.h === yd && x >= mc.x && x < mc.x + mc.w && !mc.sunk) { mc.y += 1; mc.sunk = true; mc.tilt = (x - mc.x < mc.w / 2 ? 1 : -1) * 0.08; }
      }
    }
  }

  // ---------- particles ----------
  function P(m, kind) { return m.particles[kind] || (m.particles[kind] = { x: [], y: [], s: [], ax: [], ay: [], w: [], ph: [] }); }
  function dot(m, r, kind, x, y, s, drift = 1) {
    if (m.old) { if (kind === 'ember' || !r.chance(kind === 'rivet' ? 0.4 : 0.16)) return; if (kind === 'ice' || kind === 'fuel') kind = 'iceDull'; }
    if (!inb(m, Math.floor(x), Math.floor(y))) return;
    const p = P(m, kind), a = r() * Math.PI * 2, amp = (0.2 + r() * 1.1) * drift;
    p.x.push(x); p.y.push(y); p.s.push(s); p.ax.push(Math.cos(a) * amp); p.ay.push(Math.sin(a) * amp); p.w.push(0.06 + r() * 0.22); p.ph.push(r() * 6.28);
  }
  const gauss = r => (r() + r() + r() - 1.5) / 1.5;
  function cloud(m, r, kind, n, cx, cy, sx, sy, size, drift) {
    const k = m.old ? 2 : 1;
    for (let j = 0; j < n; j++) dot(m, r, kind, cx + gauss(r) * sx * k, cy + gauss(r) * sy * k, size[0] + r() * (size[1] - size[0]), drift);
  }
  function fan(m, r, kind, n, x, y, ux, uy, spread, d0, d1, size, drift) {
    const base = Math.atan2(uy, ux), k = m.old ? 1.8 : 1;
    for (let j = 0; j < n; j++) {
      const a = base + (r() - 0.5) * spread, d = (d0 + (d1 - d0) * Math.pow(r(), 0.75)) * k;
      dot(m, r, kind, x + Math.cos(a) * d, y + Math.sin(a) * d, size[0] + r() * (size[1] - size[0]), drift);
    }
  }
  function roomDots(m, r, kind, rm, n, size, low) {
    for (let j = 0; j < n; j++) {
      const x = rm.x + r() * rm.w, y = low ? rm.y + rm.h - r() * r() * rm.h : rm.y + r() * rm.h;
      if (cellAt(m, Math.floor(x), Math.floor(y)) === 1) dot(m, r, kind, x, y, size[0] + r() * (size[1] - size[0]), 0.5);
    }
  }

  // ---------- icing ----------
  const WET = /LifeTank|salvageTanks|Pump|Valves|Manifold|Riser|vesselTank|Stove|Bar/;
  function burstPipes(m, r, list, max) {
    for (const mc of list.slice(0, max)) {
      mc.broken = true;
      const x = mc.x + mc.w * (0.3 + r() * 0.4);
      let cy = mc.y - 1; while (cy > 0 && cellAt(m, Math.floor(x), cy) === 1) cy--;
      m.looks.push({ t: 'icicle', layer: 'top', x, y: cy + 1, y1: mc.y, stream: true, seed: r.int(1e9), old: m.old });
      fan(m, r, 'ice', 70, x, mc.y, 0, -1, 2.2, 0.3, 4, [0.05, 0.12], 0.4);
      if (!m.old) m.vents.push({ x, y: mc.y + 0.3, ux: (r() - 0.5) * 0.6, uy: -1, len: 3 + r() * 2, kind: 'ice' });
    }
  }
  /** After a breach: a frozen fan of crystals out of it, rime inside the vented rooms near it, burst tanks and pipes iced. */
  function freeze(m, r, b) {
    const vented = m.vented;
    fan(m, r, 'ice', m.big ? 520 : 360, b.x, b.y, b.ux, b.uy, 1.15, 0.5, b.len, [0.05, 0.15]);
    m.looks.push({ t: 'iceFan', layer: 'top', x: b.x, y: b.y, ux: b.ux, uy: b.uy, len: b.len * 0.7, spread: 1.0, seed: r.int(1e9), old: m.old });
    m.looks.push({ t: 'hullFrost', layer: 'top', cx: b.x, cy: b.y, r: b.r, seed: r.int(1e9), old: m.old, mask: 'walls' });
    const near = (x, y) => Math.hypot(x - b.x, y - b.y) < b.reach;
    m.looks.push({ t: 'hoar', layer: 'mid', mask: vented.slice(), cx: b.x, cy: b.y, reach: b.reach, old: m.old });
    for (const mc of m.machines) if (!mc.gone && !mc.outside && mc.k !== 'vesselLamp' && near(mc.x, mc.y) && foot(m, mc).some(i => vented[i])) mc.frost = true;
    burstPipes(m, r, m.machines.filter(mc => !mc.gone && !mc.outside && WET.test(mc.k) && near(mc.x, mc.y) && foot(m, mc).some(i => vented[i])), 4);
    let n = 0;
    for (let k = 0; k < 400 && n < (m.big ? 8 : 5); k++) {
      const x = Math.floor(b.x + (r() - 0.5) * b.reach * 1.6), y = Math.floor(b.y + (r() - 0.3) * b.reach);
      if (!inb(m, x, y) || !vented[I(m, x, y)] || cellAt(m, x, y - 1) !== 2) continue;
      m.looks.push({ t: 'icicle', layer: 'top', x: x + 0.5, y, len: 0.8 + r() * 1.8, seed: r.int(1e9), old: m.old });
      n++;
    }
    for (const rm of m.rooms) if (near(rm.x + rm.w / 2, rm.y + rm.h / 2) && vented[I(m, rm.x + (rm.w >> 1), rm.y + rm.h - 1)]) roomDots(m, r, 'ice', rm, Math.round(rm.w * rm.h * 0.12), [0.04, 0.1]);
  }
  /** Power lost, hull whole: every room frozen inside, windows frosted, dark. */
  function powerLoss(m, r) {
    const inside = maskOf(m, i => m.cells[i] === 1);
    m.looks.push({ t: 'hoar', layer: 'mid', mask: inside, cx: m.W / 2, cy: m.H / 2, reach: 1e5, old: m.old, even: true });
    for (let i = 0; i < m.cells.length; i++) if (m.cells[i] === 3) m.looks.push({ t: 'frostGlass', layer: 'top', x: i % m.W, y: (i / m.W) | 0, old: m.old });
    for (const mc of m.machines) if (!mc.gone && !mc.outside && mc.k !== 'vesselLamp') mc.frost = true;
    const wet = m.machines.filter(mc => !mc.gone && !mc.outside && WET.test(mc.k));
    for (let k = wet.length - 1; k > 0; k--) { const j = r.int(k + 1); [wet[k], wet[j]] = [wet[j], wet[k]]; }
    burstPipes(m, r, wet, m.big ? 7 : 3);
    let n = 0;
    for (let k = 0; k < 2000 && n < (m.big ? 22 : 8); k++) {
      const i = r.int(m.cells.length);
      if (m.cells[i] !== 1 || cellAt(m, i % m.W, ((i / m.W) | 0) - 1) !== 2) continue;
      m.looks.push({ t: 'icicle', layer: 'top', x: (i % m.W) + 0.5, y: (i / m.W) | 0, len: 0.6 + r() * 1.4, seed: r.int(1e9), old: m.old });
      n++;
    }
    for (const rm of m.rooms) roomDots(m, r, 'ice', rm, Math.round(rm.w * rm.h * 0.05), [0.04, 0.09]);
    for (const mc of lamps(m)) mc.light = 'off';
    m.dark = true;
  }

  // ---------- burning ----------
  function burn(m, r, seeds, D, seat) {
    const blocked = blockedMap(m), dist = new Float32Array(m.W * m.H).fill(-1), q = [];
    const vented = m.vented || new Uint8Array(m.W * m.H);
    for (const rm of seeds) for (let y = rm.y; y < rm.y + rm.h; y++) for (let x = rm.x; x < rm.x + rm.w; x++) {
      const i = I(m, x, y); if (m.cells[i] === 1 && !blocked[i] && !vented[i] && dist[i] < 0) { dist[i] = 0; q.push(i); }
    }
    for (let h = 0; h < q.length; h++) {
      const i = q[h], x = i % m.W, y = (i / m.W) | 0;
      if (dist[i] > D) continue;
      for (const [dx, dy] of N4) {
        const nx = x + dx, ny = y + dy; if (!inb(m, nx, ny)) continue;
        const n = I(m, nx, ny);
        if (dist[n] < 0 && m.cells[n] === 1 && !blocked[n] && !vented[n]) { dist[n] = dist[i] + (dy < 0 ? 0.5 : 1); q.push(n); }
      }
    }
    const burnt = [];
    for (const rm of m.rooms) {
      if (rm.burnt) continue;
      let best = 1e9;
      for (let y = rm.y; y < rm.y + rm.h; y++) for (let x = rm.x; x < rm.x + rm.w; x++) { const d = dist[I(m, x, y)]; if (d >= 0 && d < best) best = d; }
      const I0 = seeds.includes(rm) ? 1 : best < 1e9 ? Math.max(0, 1 - best / D) * 0.92 : 0;
      if (I0 > 0.06) burnt.push({ rm, I: I0 });
    }
    const burntAt = (x, y) => { let v = 0; for (const b of burnt) if (inRoom(b.rm, x, y)) v = Math.max(v, b.I); return v; };
    const tops = m.structs.filter(s => s.k === 'door' && !m.structs.some(o => o.k === 'door' && o.x === s.x && o.y === s.y - 2));
    const height = s => { let h = 0; while (m.structs.some(o => o.k === 'door' && o.x === s.x && o.y === s.y + h)) h += 2; return h; };
    const borders = [], passes = [];
    for (const s of tops) {
      const a = dist[I(m, s.x - 1, s.y + 1)], b = dist[I(m, s.x + 2, s.y + 1)];
      const ia = burntAt(s.x - 1, s.y + 1), ib = burntAt(s.x + 2, s.y + 1);
      if (s.open && a >= 0 && b >= 0) { passes.push({ s, dir: b > a ? 1 : -1, I: Math.max(ia, ib) }); continue; }
      if (!s.open && (ia > 0.06) !== (ib > 0.06)) borders.push({ s, side: ia > ib ? -1 : 1, I: Math.max(ia, ib), h: height(s) });
    }
    const reached = maskOf(m, i => m.cells[i] === 1 && dist[i] >= 0);
    for (const b of burnt) {
      const rm = b.rm;
      rm.burnt = true;
      const doors = passes.filter(p => p.s.x === rm.x - 2 || p.s.x === rm.x + rm.w).map(p => ({ x: p.s.x === rm.x - 2 ? rm.x : rm.x + rm.w, out: (p.s.x === rm.x - 2) === (p.dir < 0) }));
      const mask = maskOf(m, (i, x, y) => reached[i] && x >= rm.x - 2 && x < rm.x + rm.w + 2 && y >= rm.y && y < rm.y + rm.h);
      const st = seat && inRoom(rm, seat.x, seat.y - 1) ? seat : null;
      m.looks.push({ t: 'burnRoom', layer: 'mid', rect: { x: rm.x, y: rm.y, w: rm.w, h: rm.h }, I: b.I, seed: r.int(1e9), mask, seat: st ? { x: st.x, y: st.y } : null, doors, old: m.old });
      if (!m.old) {
        roomDots(m, r, 'ash', rm, Math.round(rm.w * rm.h * 0.35 * b.I), [0.06, 0.2]);
        roomDots(m, r, 'ashLight', rm, Math.round(rm.w * rm.h * 0.12 * b.I), [0.05, 0.14]);
        if (b.I > 0.55) roomDots(m, r, 'ember', rm, Math.round(rm.w * b.I * 1.2), [0.08, 0.16], true);
        if (b.I > 0.4) m.emberLights.push({ x: st ? st.x : rm.x + rm.w / 2, y: rm.y + rm.h - 0.6, rect: { x: rm.x, y: rm.y, w: rm.w, h: rm.h }, r: Math.max(5, Math.min(12, rm.w * 0.5)) });
        m.looks.push({ t: 'smoke', layer: 'top', rect: { x: rm.x, y: rm.y, w: rm.w, h: rm.h }, I: b.I, mask });
      } else roomDots(m, r, 'ash', rm, Math.round(rm.w * 0.8 * b.I), [0.08, 0.2], true);
    }
    for (const p of passes) {
      const s = p.s;
      m.looks.push({ t: 'plume', layer: 'mid', x: p.dir > 0 ? s.x + 2 : s.x, y: s.y, h: height(s), dir: p.dir, I: p.I, mask: reached, old: m.old });
      m.looks.push({ t: 'warpDoor', layer: 'top', x: s.x, y: s.y, h: height(s), I: p.I, seed: r.int(1e9) });
    }
    for (const b of borders) m.looks.push({ t: 'doorScorch', layer: 'top', x: b.s.x, y: b.s.y, h: b.h, side: b.side, I: b.I });
    for (const mc of m.machines) {
      if (mc.outside || mc.gone) continue;
      const v = burntAt(mc.x + (mc.w >> 1), mc.y + mc.h - 1);
      if (v <= 0.1) continue;
      if (mc.k === 'vesselLamp') { mc.light = 'off'; continue; }
      mc.burnt = Math.max(mc.burnt, v);
      if (seat && v > 0.9 && (mc.k === 'crate' || mc.k === 'salvageCrate') && Math.abs(mc.x - seat.x) < 8) {
        const s = m.sizes.pileBits; mc.y = mc.y + mc.h - s.h; mc.k = 'pileBits'; mc.w = s.w; mc.h = s.h;
      }
      if (v > 0.35 && /Chair|Lounger|RoundTable|vesselTable|Vending|Console|vesselBed|vesselBunk|Counter/.test(mc.k)) m.looks.push({ t: 'sag', layer: 'mid', x: mc.x, y: mc.y, w: mc.w, h: mc.h, I: v, seed: r.int(1e9) });
      if (v > 0.6 && /Chair|Lounger|RoundTable/.test(mc.k)) mc.tilt = (r() < 0.5 ? -1 : 1) * (0.08 + r() * 0.25);
    }
    for (let i = 0; i < m.cells.length; i++) {
      if (m.cells[i] !== 3) continue;
      const x = i % m.W, y = (i / m.W) | 0;
      if (N4.some(([dx, dy]) => burntAt(x + dx, y + dy) > 0.3 && dist[I(m, x + dx, y + dy)] >= 0)) m.looks.push({ t: 'melted', layer: 'top', x, y });
    }
    if (borders.length) {
      const b = borders.sort((p, q2) => q2.I - p.I)[0];
      let fy = b.s.y; while (cellAt(m, b.s.x, fy) === 1) fy++;
      m.items.push({ t: 'extinguisher', x: b.side < 0 ? b.s.x - 1.3 : b.s.x + 3.3, y: fy - 0.35, flip: b.side > 0 });
    }
    for (const b of burnt) if (b.I > 0.3) m.sparkSpots.push({ x: b.rm.x + 2 + r() * (b.rm.w - 4), y: b.rm.y + 0.6 });
    return burnt.sort((a, b) => b.I - a.I);
  }
  /** Rooms still holding air and not yet burnt: where a fire can start. */
  function sealedRooms(m) {
    return m.rooms.filter(rm => !rm.burnt && rm.w >= 4 && cellAt(m, rm.x + (rm.w >> 1), rm.y + rm.h - 1) === 1 && !(m.vented && m.vented[I(m, rm.x + (rm.w >> 1), rm.y + rm.h - 1)]));
  }

  // ---------- the events ----------
  const E = {};

  // Explosion: an engine or a fuel tank went up and tore a void; walls bowed out, temper colours, beads, soot streaks, ash.
  E.blast = (m, r) => {
    const body = m.body;
    let cx, cy, R, breach;
    if (m.cls === 'tanker') {
      const bays = roomsOf(m, 'TankBay').filter(b => m.machines.some(mc => !mc.gone && /TankTall|FuelColumn/.test(mc.k) && mc.x >= b.x && mc.x < b.x + b.w));
      const mid = (body.Left + m.bow.Left) / 2;
      bays.sort((a, b) => Math.abs(a.x - mid) - Math.abs(b.x - mid));
      const bay = bays[r.int(Math.min(2, bays.length))] || roomsOf(m, 'Hold')[0] || m.rooms[0];
      const tank = m.machines.find(mc => !mc.gone && /TankTall|FuelColumn/.test(mc.k) && mc.x >= bay.x && mc.x < bay.x + bay.w);
      cx = tank ? tank.x + tank.w / 2 : bay.x + bay.w / 2; cy = tank ? tank.y + tank.h / 2 : bay.y + bay.h / 2; R = 10 + r.int(3);
      if (tank) { tank.gone = true; r.int(1e9); } // the burst tank goes with the blast (the user's pick); the draw kept so the rest stays
    } else {
      const eng = roomsOf(m, 'Engine').sort((a, b) => a.x - b.x)[0] || m.rooms[0];
      const engine = m.machines.find(mc => !mc.gone && /MainEngine|salvageEngine/.test(mc.k) && inRoom(eng, mc.x + 1, mc.y + 1));
      cx = engine ? engine.x + engine.w / 2 : eng.x + eng.w / 2; cy = eng.y + eng.h / 2;
      R = m.cls === 'shuttle' ? 5 : 8;
      const rib = m.ribs.slice().sort((a, b) => a.Left - b.Left)[0];
      if (m.cls === 'freighter' && rib && Math.abs(rib.Left - cx) < 40) {
        // The stern blown off at its seam, ragged band by band; the engines thrown clear.
        const bands = [0, body.Top, ...m.decks, m.H], cut = new Int32Array(m.H);
        for (let b = 0; b < bands.length - 1; b++) { const j = r.int(5) - 1; for (let y = bands[b]; y < bands[b + 1]; y++) cut[y] = rib.Left + j; }
        const away = m.machines.filter(mc => !mc.gone && mc.x + mc.w / 2 < cut[Math.min(m.H - 1, Math.max(0, mc.y + (mc.h >> 1)))]);
        const list = [];
        for (let y = 0; y < m.H; y++) for (let x = 0; x < cut[y] + (r.chance(0.3) ? 1 : 0); x++) list.push(I(m, x, y));
        const { set } = carve(m, list);
        for (const mc of away) {
          if (mc.gone) continue;
          const ax = mc.x + mc.w / 2 - (rib.Left + 1), ay = mc.y + mc.h / 2 - cy, l = Math.hypot(ax, ay) || 1;
          if (mc.k === 'vesselLamp' || (mc.w * mc.h <= 30 && r.chance(0.45))) { mc.gone = true; continue; }
          mc.burnt = 0.7;
          eject(m, r, mc, mc.x + mc.w / 2, mc.y + mc.h / 2, (ax / l) * 0.9 - 0.4, ay / l, 8 + r() * 16);
        }
        petals(m, r, set, (x, y) => [-1, Math.sign(y - cy) * 0.4], 0.5, { x: rib.Left, y: cy, r: 30 });
        cx = rib.Left + 1;
        breach = { x: rib.Left, y: cy, ux: -1, uy: -0.2, len: 16, r: 9, reach: 26 };
      }
    }
    const toTop = cy - body.Top < body.Bottom - cy;
    const lobes = []; for (let k = 0; k < 9; k++) lobes.push(0.75 + r() * 0.5);
    const list = [];
    for (let y = Math.floor(cy - R * 2); y <= cy + R * 2; y++) for (let x = Math.floor(cx - R * 1.6); x <= cx + R * 1.6; x++) {
      if (!inb(m, x, y)) continue;
      const dx = x + 0.5 - cx, dy = (y + 0.5 - cy) * 1.15, a = Math.atan2(dy, dx);
      let rad = R * lobes[Math.floor(((a + Math.PI) / (Math.PI * 2)) * 9) % 9] * (0.88 + 0.24 * SW.h01(m.seed, I(m, x, y), 51));
      if ((toTop ? dy < 0 : dy > 0) && Math.abs(dx) < R * 0.55) rad *= 1.9;
      if (Math.hypot(dx, dy) < rad) list.push(I(m, x, y));
    }
    const { set, hit } = carve(m, list);
    for (const mc of hit) {
      const ax = mc.x + mc.w / 2 - cx, ay = mc.y + mc.h / 2 - cy, l = Math.hypot(ax, ay) || 1;
      if (mc.k === 'vesselLamp' || r.chance(0.4)) { mc.gone = true; continue; }
      mc.burnt = 0.8;
      eject(m, r, mc, cx + (ax / l) * R, cy + (ay / l) * R, ax / l, ay / l, 4 + r() * 18);
    }
    petals(m, r, set, (x, y) => { const ax = x - cx, ay = y - cy, l = Math.hypot(ax, ay) || 1; return [ax / l, ay / l]; }, 0.6, { x: cx, y: cy, r: R * 1.6 });
    for (const i of set) {
      const x = i % m.W, y = (i / m.W) | 0;
      if (m.orig[i] === 2 && m.skin[i] && SW.ribAt(m, x, y) && r.chance(0.35)) {
        const ax = x - cx, ay = y - cy, l = Math.hypot(ax, ay) || 1;
        m.looks.push({ t: 'ribStub', layer: 'top', x: x + 0.5, y: y + 0.5, dx: -ax / l, dy: -ay / l, len: 1.2 + r() * 1.5, bx: (ax / l) * 0.8, by: (ay / l) * 0.8 });
      }
    }
    const beads = [];
    for (const i of set) {
      const x = i % m.W, y = (i / m.W) | 0;
      if (cellAt(m, x, y - 1) >= 2 && r.chance(0.35)) beads.push([x + 0.2 + r() * 0.6, y + 0.05, 0.12 + r() * 0.16, 1]);
      if (cellAt(m, x, y + 1) >= 2 && r.chance(0.25)) beads.push([x + r(), y + 0.85, 0.1 + r() * 0.14, 0]);
    }
    m.looks.push({ t: 'beads', layer: 'top', pts: beads });
    shiftArmour(m, Math.floor(cx - R * 1.8), Math.ceil(cx + R * 1.8), cy, true);
    buckleDecks(m, cx, R * 1.2, 1);
    const occ = occupancy(m);
    for (const mc of m.machines) {
      if (mc.gone || mc.outside || mc.k === 'vesselLamp') continue;
      const ax = mc.x + mc.w / 2 - cx, ay = mc.y + mc.h / 2 - cy, d = Math.hypot(ax, ay * 1.3);
      if (d > R * 2.6) continue;
      const near = 1 - d / (R * 2.6);
      mc.burnt = Math.max(mc.burnt, near * 0.9);
      if (near > 0.25) mc.broken = mc.w * mc.h >= 6 && r.chance(0.6);
      if (SW.LOOSE.has(mc.k) || mc.w * mc.h <= 16) {
        const step = Math.sign(ax) || 1;
        if (moveBy(m, occ, mc, step, 1 + Math.round(near * 8))) mc.tilt = step * (0.08 + r() * 0.25 * near);
      }
    }
    for (const s of m.structs) if (s.k === 'door' && Math.hypot(s.x - cx, s.y - cy) < R * 2.2) s.open = true;
    const vented = ventAll(m);
    const holes = holesOf(m, vented);
    m.looks.push({ t: 'heat', layer: 'top', cx, cy, r: R * 2.3, mask: 'walls', old: m.old });
    m.looks.push({ t: 'scorch', layer: 'top', cx, cy, r: R * 1.8, seed: r.int(1e9), mask: 'walls', old: m.old });
    const near = holes.filter(h => Math.hypot(h.x - cx, h.y - cy) < R * 2.5);
    const streaks = [];
    for (let k = 0; k < (m.big ? 160 : 90); k++) {
      const x = cx + (r() - 0.5) * R * 6, y = cy + (r() - 0.5) * R * 3;
      const xi = Math.floor(x), yi = Math.floor(y);
      if (!inb(m, xi, yi) || !vented[I(m, xi, yi)] || !near.length) continue;
      let best = near[0], bd = 1e9; for (const h of near) { const d = Math.hypot(h.x - x, h.y - y); if (d < bd) { bd = d; best = h; } }
      if (bd > R * 3.5) continue;
      const l = Math.min(bd, 2 + r() * 6);
      streaks.push([x, y, x + ((best.x - x) / bd) * l, y + ((best.y - y) / bd) * l, 0.15 + r() * 0.3]);
    }
    const ventedInside = maskOf(m, i => m.cells[i] === 1 && vented[i]);
    m.looks.push({ t: 'streaks', layer: 'mid', pts: streaks, mask: ventedInside, old: m.old });
    m.looks.push({ t: 'soot', layer: 'mid', cx, cy, r: R * 2.2, strength: 0.9, seed: r.int(1e9), mask: ventedInside, old: m.old });
    pull(m, r, holes, 14, [4, 18]);
    trail(m, r, cx, toTop ? body.Top - 1 : body.Bottom + 1, 0.15, toTop ? -1 : 1, m.big ? 30 : 20, { d0: 2, d1: 30, spread: 10 });
    cloud(m, r, 'ash', m.big ? 700 : 420, cx, toTop ? body.Top - 3 : body.Bottom + 3, R * 1.6, R * 0.9, [0.06, 0.22]);
    cloud(m, r, 'ashLight', 200, cx, cy, R * 1.2, R * 0.9, [0.05, 0.15]);
    cloud(m, r, 'ash', m.big ? 500 : 300, cx, cy, R * 1.8, R * 0.9, [0.05, 0.18], 0.6);
    if (!m.old) cloud(m, r, 'ember', 60, cx, cy, R * 0.9, R * 0.6, [0.08, 0.16], 0.4);
    m.cableSpots.push({ x: cx - R * 0.9, y: cy - R * 0.6 }, { x: cx + R * 0.8, y: cy - R * 0.7 });
    m.blastAt = { x: cx, y: cy, R };
    m.damage = { kind: 'blast', x: cx, y: cy };
    m.lastBreach = breach || { x: cx, y: toTop ? body.Top : body.Bottom, ux: 0.15, uy: toTop ? -1 : 1, len: 16, r: R * 1.8, reach: R * 3 + 8 };
    return { kind: 'blast', x: cx, y: cy, R };
  };

  // Fire: through rooms still holding air, until it starves; stopped crisp at shut doors.
  E.fire = (m, r, ctx) => {
    const sealed = sealedRooms(m);
    let seeds = [], seat = null;
    if (ctx.prev === 'blast' && m.blastAt) {
      const b = m.blastAt;
      seeds = sealed.filter(rm => Math.hypot(rm.x + rm.w / 2 - b.x, rm.y + rm.h / 2 - b.y) < b.R * 3 + rm.w / 2)
        .sort((a, c) => Math.hypot(a.x + a.w / 2 - b.x, a.y - b.y) - Math.hypot(c.x + c.w / 2 - b.x, c.y - b.y)).slice(0, 2);
      if (!seeds.length && sealed.length) seeds = [sealed.sort((a, c) => Math.hypot(a.x - b.x, a.y - b.y) - Math.hypot(c.x - b.x, c.y - b.y))[0]];
    } else {
      const role = m.cls === 'liner' ? 'Galley' : 'Life';
      const pref = sealed.filter(rm => rm.role === role).sort((a, b) => b.w - a.w)[0] || (m.cls === 'liner' ? null : sealed.filter(rm => rm.role === 'Quarters')[0]);
      const origin = pref || (sealed.length ? r.pick(sealed) : null);
      if (origin) {
        seeds = [origin];
        const ds = m.structs.filter(s => s.k === 'door' && (s.x === origin.x - 2 || s.x === origin.x + origin.w) && s.y >= origin.y - 1 && s.y < origin.y + origin.h);
        if (!ds.some(s => s.open)) { const xs = [...new Set(ds.map(s => s.x))]; if (xs.length) { const x = r.pick(xs); for (const s of ds) if (s.x === x) s.open = true; } }
        const stove = m.machines.find(mc => !mc.gone && /Stove|LifeTank|salvageTanks/.test(mc.k) && inRoom(origin, mc.x, mc.y));
        seat = { x: stove ? stove.x + stove.w / 2 : origin.x + origin.w / 2, y: origin.y + origin.h };
      }
    }
    if (!seeds.length) return { kind: 'fire', order: [] };
    const burnt = burn(m, r, seeds, m.big ? 70 : 48, seat);
    return { kind: 'fire', order: burnt.map(b => ({ x: b.rm.x, y: b.rm.y, w: b.rm.w, h: b.rm.h })) };
  };

  // Icing: after a breach, round the latest one; as the first event, power lost and the whole ship frozen inside.
  E.ice = (m, r) => {
    if (m.lastBreach) { ventAll(m); freeze(m, r, m.lastBreach); return { kind: 'ice', x: m.lastBreach.x, y: m.lastBreach.y }; }
    powerLoss(m, r);
    return { kind: 'ice', x: m.W / 2, y: m.H / 2, whole: true };
  };

  // Asteroid: a crater crushed several rooms deep, decks buckled under it, a shower of smaller hits, a field of debris and dust.
  E.rock = (m, r) => {
    const body = m.body, big = m.big;
    const R = { shuttle: 4.5, freighter: 7, liner: 9.5, tanker: 10 }[m.cls];
    const span = m.bow.Left - body.Left - 2 * R - 10;
    let cx = -1;
    for (let t = 0; t < 60; t++) {
      const x = Math.round(body.Left + R + 5 + (0.15 + r() * 0.7) * span);
      let ok = true;
      for (let dx = -Math.ceil(R); dx <= R && ok; dx += 2) if (topRow(m, x + dx) > body.Top + 4) ok = false;
      if (ok && m.rocks.some(p => Math.abs(p - x) < R * 3)) ok = false;
      if (ok) { cx = x; break; }
    }
    if (cx < 0) cx = Math.round(body.Left + R + 5 + r() * span);
    const top = topRow(m, Math.round(cx));
    const side = r.chance(0.5) ? 1 : -1;
    const ang = r.range(0.35, 0.75) * side;
    const ux = Math.sin(ang), uy = Math.cos(ang);
    const ccx = cx + ux * R * 0.3, ccy = top + R * 0.5;
    const lobes = []; for (let k = 0; k < 11; k++) lobes.push(0.78 + r() * 0.42);
    const list = [];
    for (let y = top - 2; y <= top + R * 2.2; y++) for (let x = Math.floor(cx - R * 1.8); x <= cx + R * 1.8; x++) {
      if (!inb(m, x, y)) continue;
      const dx = x + 0.5 - ccx, dy = (y + 0.5 - ccy) / 1.25;
      const lob = lobes[Math.floor(((Math.atan2(dy, dx) + Math.PI) / (Math.PI * 2)) * 11) % 11];
      let hit = Math.hypot(dx, dy) < R * lob * (0.88 + 0.24 * SW.h01(m.seed, I(m, x, y), 61));
      const px = x + 0.5 - ccx, py = y + 0.5 - ccy, t = px * ux + py * uy, p = Math.abs(-px * uy + py * ux);
      if (t > 0 && t < R * 1.3 && p < R * 0.55 * (1 - t / (R * 1.8))) hit = true;
      if (hit) list.push(I(m, x, y));
    }
    const { set, hit } = carve(m, list);
    petals(m, r, set, () => [ux * 0.6, 1], 0.8);
    for (const i of set) {
      const x = i % m.W, y = (i / m.W) | 0;
      if (m.orig[i] === 2 && m.skin[i] && SW.ribAt(m, x, y) && r.chance(0.4)) m.looks.push({ t: 'ribStub', layer: 'top', x: x + 0.5, y: y + 0.5, dx: ux * 0.3, dy: 1, len: 1.5 + r() * 2, bx: (r() - 0.5) * 1.5, by: 0.6 });
    }
    shiftArmour(m, Math.floor(cx - R - 6), Math.ceil(cx + R + 6), ccy, false, x => Math.abs(x - cx) > R * 0.8);
    buckleDecks(m, ccx, R * 0.9, 2);
    const lodge = (x, size) => {
      let y = Math.floor(ccy); while (y < m.H - 1 && cellAt(m, Math.floor(x), y) === 0) y++;
      if (y < m.H - 1) m.items.push({ t: 'rock', x, y: y - size * 0.45, size, poly: rockPoly(r), rot: r() * 6 });
    };
    lodge(ccx + ux * R * 0.6, big ? 2.4 : 1.6);
    lodge(ccx - R * 0.4, big ? 1.4 : 1);
    const shower = [];
    for (let k = 0, n = 1 + r.int(3); k < n; k++) {
      const sx = Math.round(cx + side * (R + 5 + r() * R * 2.6));
      if (sx < body.Left + 3 || sx > m.bow.Left - 3) continue;
      const st = topRow(m, sx);
      if (st > body.Top + 4) continue;
      const rr = 1.4 + r() * 1.4, sl = [];
      for (let y = st - 1; y <= st + rr * 1.6; y++) for (let x = Math.floor(sx - rr - 1); x <= sx + rr + 1; x++)
        if (inb(m, x, y) && Math.hypot(x + 0.5 - sx, (y + 0.5 - st) / 1.3) < rr * (0.85 + 0.3 * SW.h01(m.seed, I(m, x, y), 71))) sl.push(I(m, x, y));
      const res = carve(m, sl);
      petals(m, r, res.set, [ux * 0.5, 1], 0.75);
      for (const mc of res.hit) { mc.broken = true; mc.gone = mc.k === 'vesselLamp' || r.chance(0.5); }
      if (k === 0) { let y = st; while (y < m.H - 1 && cellAt(m, sx, y) === 0) y++; m.items.push({ t: 'rock', x: sx + 0.5, y: y - 0.4, size: 0.8, poly: rockPoly(r), rot: r() * 6 }); }
      shower.push({ x: sx + 0.5, y: st });
    }
    const vented = ventAll(m);
    const holes = holesOf(m, vented);
    let fx = side, fy = -0.38; const fl = Math.hypot(fx, fy); fx /= fl; fy /= fl;
    for (const mc of hit) { if (mc.k === 'vesselLamp') { mc.gone = true; continue; } mc.broken = true; eject(m, r, mc, ccx, top - 1, fx, fy, 4 + r() * 40, { vr: (r() - 0.5) * 0.2 }); }
    pull(m, r, holes, big ? 22 : 16, [4, 30], [fx, fy]);
    trail(m, r, ccx, top - 1, fx, fy, big ? 70 : 46, { d0: 1, d1: big ? 70 : 50, spread: 9, rocks: true, sprites: ['salvageSection', 'salvageSlab', 'crate', 'salvageCrate', 'vesselChair'], spriteShare: 0.08 });
    for (let k = 0; k < 8; k++) drifter(m, r, { kind: 'rock', poly: rockPoly(r), size: 0.3 + r() * 1.2, x: ccx - ux * (4 + r() * 20) + (r() - 0.5) * 6, y: top - uy * (4 + r() * 12), fx: ccx, fy: top, vx: -ux, vy: -uy });
    cloud(m, r, 'dust', big ? 900 : 550, ccx + fx * 4, top - 2, R * 1.4, R * 0.7, [0.06, 0.2]);
    cloud(m, r, 'rockDust', big ? 400 : 260, ccx, top + 1, R * 0.9, R * 0.6, [0.08, 0.22]);
    for (let j = 0; j < (big ? 1100 : 700); j++) {
      const d = 2 + Math.pow(r(), 0.8) * (big ? 72 : 52), sp = (r() - 0.5) * (2 + d * 0.32);
      const x = ccx + fx * d - fy * sp, y = top - 1 + fy * d + fx * sp, roll = r();
      dot(m, r, roll < 0.5 ? 'dust' : roll < 0.75 ? 'paint' : roll < 0.92 ? 'bit' : 'rockDust', x, y, 0.05 + r() * 0.16);
    }
    for (const s of shower) cloud(m, r, 'dust', 90, s.x, s.y - 1, 2.5, 1.5, [0.05, 0.15]);
    m.looks.push({ t: 'haze', layer: 'top', cx: ccx + fx * 6, cy: top - 3, rx: R * 2.2, ry: R * 1.1, col: '150,135,115', a: m.old ? 0.06 : 0.26 });
    m.looks.push({ t: 'haze', layer: 'top', cx: ccx + fx * 24, cy: top - 1 + fy * 24, rx: R * 3, ry: R * 1.2, col: '140,135,125', a: m.old ? 0.04 : 0.15 });
    m.cableSpots.push({ x: ccx - R * 0.7, y: top + 2 }, { x: ccx + R * 0.6, y: top + 3 });
    m.rocks.push(cx);
    m.damage = { kind: 'rock', x: ccx, y: top };
    m.lastBreach = { x: ccx, y: top, ux: fx * 0.55, uy: -1, len: big ? 24 : 16, r: R * 2.2, reach: R * 4 + 10 };
    // A strike always freezes what leaked out of it (the user's pick: a strike alone reads dull).
    ventAll(m); freeze(m, r, m.lastBreach); m.lastBreach.frozen = true;
    return { kind: 'rock', x0: ccx - ux * 80, y0: top - uy * 80, x1: ccx, y1: top, ux, uy, R, shower, fx, fy };
  };

  /** Everything laid down so far moved with its half when the ship breaks. */
  function remapAll(m, mp, mi) {
    const pt = (o, kx, ky) => { if (typeof o[kx] === 'number' && typeof o[ky] === 'number') { const [x, y] = mp(o[kx], o[ky]); o[kx] = x; o[ky] = y; } };
    const remask = a => { const b = new Uint8Array(a.length); for (let i = 0; i < a.length; i++) if (a[i]) { const j = mi(i); if (j >= 0) b[j] = 1; } return b; };
    for (const L of m.looks) {
      if (L.rect) { const [x, y] = mp(L.rect.x + L.rect.w / 2, L.rect.y + L.rect.h / 2); const dx = x - (L.rect.x + L.rect.w / 2); L.rect = { x: L.rect.x + dx, y: y - L.rect.h / 2, w: L.rect.w, h: L.rect.h }; if (L.doors) L.doors = L.doors.map(d => Object.assign({}, d, { x: d.x + dx })); }
      if (typeof L.x === 'number' && typeof L.y === 'number') { const [x, y] = mp(L.x, L.y); if (typeof L.y1 === 'number' && L.x0 === undefined) L.y1 += y - L.y; L.x = x; L.y = y; }
      pt(L, 'cx', 'cy');
      if (L.t === 'cutEdge') { const [x0, y0] = mp(L.x0, L.y0); const dx = x0 - L.x0, dy = y0 - L.y0; L.x0 += dx; L.x1 += dx; L.y0 += dy; L.y1 += dy; }
      if (L.seat) { const [x, y] = mp(L.seat.x, L.seat.y - 1); L.seat = { x, y: y + 1 }; }
      if (L.pts) L.pts = L.pts.map(p => { const [x, y] = mp(p[0], p[1]); const q = p.slice(); q[0] = x; q[1] = y; if (p.length >= 5) { q[2] += x - p[0]; q[3] += y - p[1]; } return q; });
      if (L.mask instanceof Uint8Array) L.mask = remask(L.mask);
    }
    for (const k in m.particles) { const p = m.particles[k]; for (let i = 0; i < p.x.length; i++) { const [x, y] = mp(p.x[i], p.y[i]); p.x[i] = x; p.y[i] = y; } }
    for (const d of m.drifters) { const [x, y] = mp(d.x, d.y); const dx = x - d.x, dy = y - d.y; d.x = x; d.y = y; d.fx += dx; d.fy += dy; }
    for (const list of [m.items, m.vents, m.hot, m.sparks, m.sparkSpots, m.cableSpots, m.emberLights]) for (const o of list) pt(o, 'x', 'y');
    for (const e of m.emberLights) if (e.rect) { const [x, y] = mp(e.rect.x + e.rect.w / 2, e.rect.y + e.rect.h / 2); e.rect = { x: x - e.rect.w / 2, y: y - e.rect.h / 2, w: e.rect.w, h: e.rect.h }; }
    for (const o of [m.lastBreach, m.blastAt, m.damage]) if (o) pt(o, 'x', 'y');
    m.rocks = m.rocks.map(x => mp(x, m.body.Top)[0]);
  }

  // Break: split in two at a module seam — at the damage that caused it, or (first) from overload; the halves drift apart by whole rows.
  E.break = (m, r) => {
    const body = m.body;
    const cause = m.damage ? m.damage.kind : 'hog';
    const target = m.damage ? m.damage.x : (body.Left + m.bow.Left) / 2;
    const ribs = m.ribs.filter(s => s.Left > body.Left + 22 && s.Left < m.bow.Left - 22).sort((a, b) => Math.abs(a.Left - target) - Math.abs(b.Left - target));
    const rib = m.damage ? ribs[0] : ribs[r.int(Math.min(2, ribs.length))];
    if (!rib) return { kind: 'split', cause, x: target, y0: body.Top, y1: body.Bottom, cut: target, top: body.Top };
    const G = { freighter: [8, 11], liner: [11, 16], tanker: [12, 18] }[m.cls] || [6, 10];
    const gap = G[0] + r.int(G[1] - G[0] + 1), Gs = gap >> 1, Gb = gap - Gs;
    const dyB = (r.chance(0.5) ? -1 : 1) * (2 + r.int(2)), dyS = r.chance(0.4) ? -Math.sign(dyB) : 0;
    const bands = [0, body.Top, ...m.decks, m.H];
    const cut = new Int32Array(m.H);
    for (let b = 0; b < bands.length - 1; b++) { const j = r.int(7) - 3; for (let y = bands[b]; y < bands[b + 1]; y++) cut[y] = rib.Left + 1 + j; }
    for (const d of m.decks) if (d < m.H) cut[d] += (r.chance(0.5) ? 1 : -1) * (2 + r.int(3));
    const cutAt = y => cut[Math.max(0, Math.min(m.H - 1, Math.floor(y)))];
    const side = (x, y) => x < cutAt(y);
    const W = m.W, H = m.H;
    const mv = (x, y, stern) => [x + (stern ? -Gs : Gb), y + (stern ? dyS : dyB)];
    const mp = (x, y) => mv(x, y, side(x, y));
    const mi = i => { const x = i % W, y = (i / W) | 0, [nx, ny] = mp(x, y); return inb(m, nx, ny) ? I(m, nx, ny) : -1; };
    remapAll(m, mp, mi);
    const cells = new Uint8Array(W * H), dress = new Array(W * H).fill(null), skin = new Uint8Array(W * H), orig = new Uint8Array(W * H);
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      const i = I(m, x, y);
      if (!m.cells[i] && !m.orig[i]) continue;
      const [nx, ny] = mp(x, y);
      if (!inb(m, nx, ny)) continue;
      const j = I(m, nx, ny);
      cells[j] = m.cells[i]; dress[j] = m.dressOf[i]; skin[j] = m.skin[i]; orig[j] = m.orig[i];
    }
    const gone = [];
    for (const mc of m.machines) {
      if (mc.gone) continue;
      const f = foot(m, mc);
      const s0 = side(mc.x, mc.y), all = f.every(i => side(i % W, (i / W) | 0) === s0);
      if (!all && !mc.outside) { gone.push(mc); continue; }
      const sideOf = mc.outside ? side(mc.x + (mc.w >> 1), mc.y + mc.h - 1) : s0;
      [mc.x, mc.y] = mv(mc.x, mc.y, sideOf);
      mc.half = sideOf ? 'stern' : 'bow';
    }
    m.structs = m.structs.filter(s => {
      const s0 = side(s.x, s.y);
      if (!structCells(m, s).every(i => side(i % W, (i / W) | 0) === s0)) return false;
      [s.x, s.y] = mv(s.x, s.y, s0);
      return true;
    });
    const newRooms = [];
    for (const rm of m.rooms) {
      const c = cutAt(rm.y + (rm.h >> 1));
      const parts = [];
      if (rm.x < c) parts.push({ x: rm.x, w: Math.min(rm.w, c - rm.x), stern: true });
      if (rm.x + rm.w > c) parts.push({ x: Math.max(rm.x, c), w: rm.x + rm.w - Math.max(rm.x, c), stern: false });
      for (const p of parts) { const [nx, ny] = mv(p.x, rm.y, p.stern); newRooms.push(Object.assign({}, rm, { x: nx, y: ny, w: p.w, half: p.stern ? 'stern' : 'bow' })); }
    }
    m.rooms = newRooms;
    m.sections = m.sections.map(s => { const st = s.Left + s.Width / 2 < rib.Left + 1; const dx = st ? -Gs : Gb, dy = st ? dyS : dyB; return Object.assign({}, s, { Left: s.Left + dx, Top: s.Top + dy, Bottom: s.Bottom + dy }); });
    m.ribs = m.sections.filter(s => s.kind === 'Rib');
    const decks0 = m.decks.slice();
    m.decks = m.decks.map(d => d + dyB);
    m.cells = cells; m.dressOf = dress; m.skin = skin; m.orig = orig;
    const faceX = (y, stern) => mv(cutAt(y - (stern ? dyS : dyB)), 0, stern)[0];
    const list = [];
    for (let y = 0; y < H; y++) for (const stern of [true, false]) {
      const fx = faceX(y, stern), depth = r.chance(0.5) ? r.int(3) : 0;
      for (let k = 0; k < depth; k++) { const x = stern ? fx - 1 - k : fx + k; if (inb(m, x, y)) list.push(I(m, x, y)); }
    }
    const { set } = carve(m, list);
    const gx0 = rib.Left + 1 - Gs, gx1 = rib.Left + 1 + Gb, gcx = (gx0 + gx1) / 2;
    const gy0 = body.Top + Math.min(dyS, dyB), gy1 = body.Bottom + Math.max(dyS, dyB), gcy = (gy0 + gy1) / 2;
    for (const stern of [true, false]) {
      const dy = stern ? dyS : dyB;
      for (const d0 of decks0.slice(0, -1)) {
        const yd = d0 + dy;
        let x = stern ? faceX(yd, true) - 1 : faceX(yd, false);
        while (inb(m, x, yd) && cellAt(m, x, yd) === 0 && Math.abs(x - gcx) < gap) x += stern ? -1 : 1;
        for (let k = 0; k < 2; k++) {
          const xx = x + (stern ? -k : k), a = I(m, xx, yd), b = I(m, xx, yd + 1);
          if (cellAt(m, xx, yd) === 2 && cellAt(m, xx, yd + 1) === 1 && cellAt(m, xx, yd + 2) === 1) { m.cells[b] = 2; m.dressOf[b] = m.dressOf[a]; m.cells[a] = 1; m.dressOf[a] = null; }
        }
      }
    }
    for (const mc of gone) {
      mc.gone = true;
      if (mc.k === 'vesselLamp' || r.chance(0.3)) continue;
      drifter(m, r, { kind: 'sprite', name: mc.k, w: mc.w, h: mc.h, x: gcx + (r() - 0.5) * gap * 0.6, y: mc.y + (dyS + dyB) / 2 + (r() - 0.5) * 6, fx: rib.Left + 1, fy: mc.y + mc.h / 2, vx: (r() - 0.5) * 0.3, vy: r() < 0.5 ? -1 : 1, vr: (r() - 0.5) * 0.12, burnt: mc.burnt > 0.5, frost: mc.frost });
    }
    for (let k = 0; k < 22; k++) {
      const x = gx0 + 1 + r() * (gap - 2), y = gy0 - 2 + r() * (gy1 - gy0 + 4);
      if (r() < 0.65) drifter(m, r, { kind: 'shard', poly: shardPoly(r), size: 0.5 + r(), x, y, fx: rib.Left + 1, fy: y, vx: 0.2, vy: r() - 0.5, col: r() < 0.3 ? 'rib' : 'hull', bent: r() < 0.4 });
      else drifter(m, r, { kind: 'bit', size: 0.3, col: '#8f9599', x, y, fx: rib.Left + 1, fy: y, vx: 0.3, vy: 1 });
    }
    cloud(m, r, 'bit', 260, gcx, gcy, gap * 0.4, (gy1 - gy0) * 0.45, [0.05, 0.14], 0.7);
    for (const stern of [true, false]) {
      const dy = stern ? dyS : dyB;
      for (let b = 1; b < bands.length - 1; b++) {
        const y = bands[b] + dy + 1, fx = faceX(y, stern);
        for (let k = 0, n = 1 + r.int(3); k < n; k++) {
          const x = stern ? fx - 0.4 - k * 0.6 : fx + 0.4 + k * 0.6;
          m.cableSpots.push({ x, y: y + r() * 0.5, sway: (stern ? 1 : -1) * (0.6 + r() * 2.2), len: 3 + r() * 6, pipe: r.chance(0.35), stern });
        }
        if (r.chance(0.6)) m.looks.push({ t: 'ribStub', layer: 'top', x: fx, y: bands[b] + dy + 0.5, dx: stern ? 1 : -1, dy: 0.2, len: 1.5 + r() * 2.5, bx: 0, by: 0.4 + r() * 1.4 });
      }
    }
    petals(m, r, set, (x) => [x < gcx ? -0.6 : 0.6, 0.4], 0.45);
    const vented = ventAll(m);
    pull(m, r, holesOf(m, vented), 18, [2, 8]);
    const faceStern = faceX(Math.round(gcy), true), faceBow = faceX(Math.round(gcy), false);
    if (cause === 'hog') {
      m.looks.push({ t: 'folds', layer: 'top', x0: faceStern - 12, x1: faceStern - 1, mask: 'walls', seed: r.int(1e9) }, { t: 'folds', layer: 'top', x0: faceBow + 1, x1: faceBow + 10, mask: 'walls', seed: r.int(1e9) });
      for (const stern of [true, false]) for (let k = 0; k < 4; k++) {
        const y = gy0 + 1 + r() * (gy1 - gy0 - 2), fx = faceX(Math.round(y), stern);
        m.looks.push({ t: 'stress', layer: 'top', x: fx, y, dir: stern ? -1 : 1, len: 6 + r() * 12, seed: r.int(1e9), mask: 'walls' });
      }
      cloud(m, r, 'rivet', 70, gcx, gcy, gap * 0.6, (gy1 - gy0) * 0.5, [0.12, 0.2], 0.8);
      if (m.cls === 'tanker') {
        const bay = m.rooms.filter(rm => (rm.role === 'TankBay' || rm.role === 'PumpRoom') && Math.abs((rm.half === 'stern' ? rm.x + rm.w : rm.x) - (rm.half === 'stern' ? faceStern : faceBow)) < 4)[0];
        const fy = bay ? bay.y + bay.h / 2 : gcy, fstern = bay ? bay.half === 'stern' : true, fx = fstern ? faceStern : faceBow;
        fan(m, r, 'fuel', 460, fx, fy, fstern ? 1 : -1, -0.2, 1.3, 0.5, gap * 1.6, [0.05, 0.14]);
        if (!m.old) m.vents.push({ x: fx, y: fy, ux: fstern ? 1 : -1, uy: -0.1, len: gap * 0.7, kind: 'fuel' });
      }
    } else if (cause === 'rock') {
      const top = Math.min(topRow(m, Math.round(faceStern - 2)), topRow(m, Math.round(faceBow + 2)));
      for (const x of [faceStern - 1.5, faceBow + 1.5]) { let y = Math.max(0, top); while (y < H - 1 && cellAt(m, Math.floor(x), y) === 0) y++; m.items.push({ t: 'rock', x, y: y - 0.6, size: 1.4, poly: rockPoly(r), rot: r() * 6 }); }
      for (let k = 0; k < 10; k++) drifter(m, r, { kind: 'rock', poly: rockPoly(r), size: 0.3 + r() * 1.1, x: gcx + (r() - 0.5) * gap * 1.4, y: gy0 - 2 - r() * 10, fx: gcx, fy: gy0, vx: 0, vy: -1 });
      const spill = m.machines.filter(mc => !mc.gone && !mc.outside && /Piano|Chandelier|Chair|RoundTable|Lounger|vesselTable|Bar|vesselBed|salvageCrate|crate/.test(mc.k) && Math.abs(mc.x + mc.w / 2 - gcx) < 16);
      spill.sort((a, b) => (/Piano|Chandelier/.test(b.k) ? 1 : 0) - (/Piano|Chandelier/.test(a.k) ? 1 : 0));
      for (const mc of spill.slice(0, 9)) eject(m, r, mc, gcx, mc.y, 0, 0, 0, { x: gcx + (r() - 0.5) * gap * 0.8, y: gy0 + r() * (gy1 - gy0), vx: r() - 0.5, vy: 1, vr: (r() - 0.5) * 0.15 });
      cloud(m, r, 'dust', 600, gcx, gy0, 10, 6, [0.06, 0.2]);
      cloud(m, r, 'rockDust', 220, gcx, gy0 + 2, 6, 4, [0.08, 0.2]);
      cloud(m, r, 'bit', 300, gcx, gcy, gap * 0.45, (gy1 - gy0) * 0.45, [0.05, 0.15]);
      m.looks.push({ t: 'haze', layer: 'top', cx: gcx, cy: gy0, rx: 16, ry: 8, col: '150,135,115', a: m.old ? 0.05 : 0.15 });
    } else {
      for (const fx of [faceStern, faceBow]) {
        m.looks.push({ t: 'heat', layer: 'top', cx: fx, cy: gcy, r: 9, mask: 'walls', old: m.old });
        m.looks.push({ t: 'scorch', layer: 'top', cx: fx, cy: gcy, r: 8, seed: r.int(1e9), mask: 'walls', old: m.old });
      }
      m.looks.push({ t: 'soot', layer: 'mid', cx: gcx, cy: gcy, r: 16, strength: 0.9, seed: r.int(1e9), mask: maskOf(m, i => m.cells[i] === 1), old: m.old });
      for (const L of m.looks) if (L.t === 'petal' && Math.abs(L.x - gcx) < gap) { L.fx = L.x < gcx ? -1 : 1; L.fy = 0; }
      const cont = m.machines.filter(mc => !mc.gone && mc.outside && /^vesselContainer/.test(mc.k)).sort((a, b) => Math.abs(a.x - gcx) - Math.abs(b.x - gcx))[0];
      if (cont) eject(m, r, cont, gcx, gy1, 0, 1, 0, { x: gcx, y: gy1 + 3, vx: 0.2, vy: 1, vr: 0.25, amp: 0.6, rot: 0.6, burnt: true });
      cloud(m, r, 'ash', 500, gcx, gcy, gap * 0.6, (gy1 - gy0) * 0.5, [0.06, 0.2]);
      if (!m.old) cloud(m, r, 'ember', 40, gcx, gcy, gap * 0.3, (gy1 - gy0) * 0.3, [0.08, 0.14], 0.4);
    }
    for (const c of m.cableSpots.filter(c => c.pipe).slice(0, 3)) {
      const ex = c.x + c.sway * 1.1, ey = c.y + c.len;
      fan(m, r, 'ice', 90, ex, ey, c.stern ? 0.6 : -0.6, 0.8, 1.2, 0.3, 5, [0.04, 0.12]);
      if (!m.old) m.vents.push({ x: ex, y: ey, ux: c.stern ? 0.7 : -0.7, uy: 0.7, len: 4, kind: 'ice' });
    }
    for (const mc of lamps(m)) if (mc.light !== 'off' || mc.half === 'stern') mc.light = mc.half === 'stern' ? 'blink' : 'off';
    const sealed = m.rooms.filter(rm => !m.vented[I(m, rm.x + (rm.w >> 1), rm.y + rm.h - 1)] && !rm.burnt && lamps(m).some(l => inRoom(rm, l.x + 1, l.y)));
    if (sealed.length && !m.dark) { const rm = r.pick(sealed); for (const l of lamps(m)) if (inRoom(rm, l.x + 1, l.y)) l.light = 'warm'; }
    m.broken = true;
    m.lastBreach = { x: gcx, y: gy0, ux: 0, uy: -1, len: 16, r: 10, reach: 30 };
    return { kind: 'split', cause, x: gcx, y0: gy0 - 2, y1: gy1 + 2, cut: rib.Left + 1, top: body.Top };
  };

  // ---------- age ----------
  /** What an event leaves while it is fresh: sparks, cables sparking, lamps stuttering; old, the cables hang dead. */
  function finishEvent(m, r, age) {
    if (age === 'fresh') {
      for (const mc of lamps(m)) if (mc.light === 'off' && !m.dark && m.vented && m.vented[I(m, mc.x, mc.y)] && r.chance(0.18)) mc.light = 'flicker';
      const spots = m.sparkSpots.slice();
      for (let k = 0; k < 3 && spots.length; k++) { const s = spots.splice(r.int(spots.length), 1)[0]; m.sparks.push({ id: r.int(1e9), x: s.x, y: s.y }); }
    }
    for (const c of m.cableSpots.slice(0, 14)) {
      const len = (c.len ?? 2 + r() * 4) * (age === 'old' ? 0.75 : 1), sway = (c.sway ?? (r() - 0.5) * 1.6) * (age === 'old' ? 0.5 : 1);
      m.looks.push({ t: 'cable', layer: 'top', x: c.x, y: c.y, len, sway, pipe: c.pipe });
      if (age === 'fresh' && !c.pipe && c.stern !== false && r.chance(0.6)) m.sparks.push({ id: r.int(1e9), x: c.x + sway * 1.1, y: c.y + len });
    }
    m.sparkSpots = []; m.cableSpots = [];
  }
  /** The oldest event long ago: pitting, faded paint, scavengers took the engines, panels and anything of value. */
  function strip(m, r, holes0) {
    m.fade = 1;
    m.looks.push({ t: 'pits', layer: 'top' });
    // Engines stay (the user's pick): a whole engine room with no engine reads wrong. Scavengers took skin panels only.
    const ribs = m.ribs.slice();
    for (let k = 0, n = Math.min(ribs.length, 2 + r.int(3)); k < n; k++) {
      const rib = ribs.splice(r.int(ribs.length), 1)[0];
      const x0 = rib.Left - 3 - r.int(3), x1 = rib.Left + rib.Width + 2 + r.int(3), topSide = r.chance(0.6), list = [];
      for (let x = x0; x < x1; x++) {
        let y = topSide ? 0 : m.H - 1;
        while (inb(m, x, y) && m.cells[I(m, x, y)] === 0) y += topSide ? 1 : -1;
        for (let k2 = 0; k2 < 2; k2++) { const yy = y + (topSide ? k2 : -k2); if (cellAt(m, x, yy) === 2 && m.skin[I(m, x, yy)]) list.push(I(m, x, yy)); }
      }
      carve(m, list, { keepRibs: true });
    }
    const holes = holes0.filter(i => m.cells[i] === 0);
    for (let k = 0; k < 2 && holes.length; k++) {
      const i = holes[r.int(holes.length)], x = i % m.W, y = (i / m.W) | 0, cellsP = [];
      for (let dx = -1; dx <= 2; dx++) for (let dy = -1; dy <= 1; dy++) {
        const j = I(m, x + dx, y + dy);
        if (inb(m, x + dx, y + dy) && m.cells[j] === 0 && m.orig[j] === 2 && m.skin[j]) cellsP.push(j);
      }
      if (cellsP.length < 2) continue;
      let minx = 1e9, maxx = -1, miny = 1e9, maxy = -1;
      for (const j of cellsP) { const px = j % m.W, py = (j / m.W) | 0; m.cells[j] = 2; m.dressOf[j] = m.dress0[j] || m.dressOf[j]; minx = Math.min(minx, px); maxx = Math.max(maxx, px); miny = Math.min(miny, py); maxy = Math.max(maxy, py); }
      m.looks.push({ t: 'patch', layer: 'top', x: minx, y: miny, w: maxx - minx + 1, h: maxy - miny + 1, seed: r.int(1e9) });
    }
    // Rooms the events did not reach keep what stood in them (the user's pick): scavengers took only engines and panels.
    ventAll(m);
  }

  // ---------- chains ----------
  const BREACH = ['blast', 'rock', 'break'];
  SW.EVENTS = ['blast', 'fire', 'ice', 'rock', 'break'];
  SW.MAX_EVENTS = { shuttle: 1, freighter: 2, liner: 3, tanker: 3 };
  /** Whether event i of a chain follows its cause directly and so shares its time. */
  SW.dependent = (chain, i) => {
    const e = chain[i] && chain[i].event, p = i > 0 ? chain[i - 1].event : null;
    return !!p && ((e === 'fire' && p === 'blast') || (e === 'ice' && BREACH.includes(p)) || (e === 'break' && (p === 'blast' || p === 'rock')));
  };
  /** Each chain's dependent events take their cause's age. */
  SW.settleAges = chain => { chain.forEach((ev, i) => { if (SW.dependent(chain, i)) ev.age = chain[i - 1].age; else ev.age = ev.age || 'old'; }); return chain; };

  function newModel(v, sizes) {
    const m = SW.makeModel(v, sizes);
    m.sizes = sizes;
    m.big = m.cls === 'liner' || m.cls === 'tanker';
    m.dress0 = m.dressOf.slice();
    m.carved = new Set(); m.sparkSpots = []; m.cableSpots = []; m.particles = {}; m.vents = []; m.hot = []; m.emberLights = []; m.rocks = []; m.replays = [];
    for (const mc of lamps(m)) mc.light = SW.h01(m.seed, mc.id, 17) < 0.45 ? 'red' : 'warm';
    return m;
  }
  function apply(m, chain, i, seed) {
    const ev = chain[i];
    m.old = ev.age === 'old';
    const r = SW.rng(SW.h32(seed >>> 0, i + 1, SW.hashStr(ev.event)));
    m.replays.push(E[ev.event](m, r, { prev: i > 0 ? chain[i - 1].event : null, i }));
    finishEvent(m, r, ev.age);
  }
  /** The events that may come next on this ship after this chain, each with why not when it may not. */
  SW.allowedNext = function (m, chain) {
    const n = e => chain.filter(c => c.event === e).length;
    const breach = chain.some(c => BREACH.includes(c.event));
    const max = SW.MAX_EVENTS[m.cls];
    const out = {};
    for (const e of SW.EVENTS) out[e] = { ok: true, why: '' };
    if (chain.length >= max) { for (const e of SW.EVENTS) out[e] = { ok: false, why: `Не больше ${max} событий для этого класса` }; return out; }
    if (n('blast')) out.blast = { ok: false, why: 'Взрыв бывает один раз' };
    if (!sealedRooms(m).length) out.fire = { ok: false, why: 'Нет отсеков с воздухом — гореть нечему' };
    if (n('ice')) out.ice = { ok: false, why: 'Обледенение бывает один раз' };
    else if (chain.length && !breach) out.ice = { ok: false, why: 'Нужна пробоина раньше — или обледенение первым (отказ питания)' };
    else if (chain.length && m.lastBreach && m.lastBreach.frozen) out.ice = { ok: false, why: 'Удар астероида уже заморозил свою пробоину' };
    if (n('rock') >= (m.big ? 2 : 1)) out.rock = { ok: false, why: m.big ? 'Не больше двух ударов' : 'Один удар на этот класс' };
    if (m.cls === 'shuttle') out.break = { ok: false, why: 'Шаттл не ломается пополам' };
    else if (n('break')) out.break = { ok: false, why: 'Разлом бывает один раз' };
    return out;
  };
  /** A chain drawn from a seed: the count, then each next event uniformly from those allowed; ages last fresh or old, earlier older. */
  SW.drawChain = function (v, sizes, seed) {
    const r = SW.rng(SW.h32(seed >>> 0, 777));
    const max = SW.MAX_EVENTS[v.cls], count = 1 + r.int(max);
    const chain = [], m = newModel(v, sizes);
    for (let i = 0; i < count; i++) {
      const ok = Object.entries(SW.allowedNext(m, chain)).filter(([, a]) => a.ok).map(([e]) => e);
      if (!ok.length) break;
      chain.push({ event: r.pick(ok), age: 'fresh' });
      apply(m, chain, i, seed);
    }
    const last = r.chance(0.5) ? 'fresh' : 'old';
    let group = 0; const groups = chain.map((_, i) => (i && !SW.dependent(chain, i) ? ++group : group));
    chain.forEach((ev, i) => { ev.age = groups[i] === group ? last : 'old'; });
    return SW.settleAges(chain);
  };
  /** The model of a ship after its chain, on one seed. */
  SW.buildChain = function (v, sizes, chain, seed) {
    const m = newModel(v, sizes);
    chain.forEach((_, i) => apply(m, chain, i, seed));
    m.old = false;
    const holes0 = [...m.carved].filter(i => m.orig[i] === 2 && m.skin[i]);
    if (chain.length && chain[0].age === 'old') strip(m, SW.rng(SW.h32(seed >>> 0, 999)), holes0);
    const lastAge = chain.length ? chain[chain.length - 1].age : 'fresh';
    if (lastAge === 'old' || m.dark) for (const mc of lamps(m)) mc.light = 'off';
    const has = e => chain.some(c => c.event === e);
    m.shade = !chain.length ? 0.3 : has('fire') ? (lastAge === 'old' ? 0.4 : 0.22) : has('blast') ? (lastAge === 'old' ? 0.45 : 0.3) : lastAge === 'old' ? 0.56 : 0.42;
    if (m.dark) m.shade = Math.max(m.shade, 0.5);
    m.machines = m.machines.filter(mc => !mc.gone);
    m.lights = [];
    for (const mc of m.machines) {
      if (mc.k !== 'vesselLamp' || !mc.light || mc.light === 'off') continue;
      const room = SW.roomAt(m, mc.x + 1, mc.y) || SW.roomAt(m, mc.x + 1, mc.y + 1);
      const rr = room ? Math.min(11, Math.max(5.5, room.h * 0.95)) : 3.5;
      m.lights.push({ id: mc.id, x: mc.x + 1, y: mc.y + 0.75, room, r: rr, state: mc.light, col: SW.lampColour[mc.light], ph: SW.h01(mc.id, 5) * 4 });
    }
    m.emberLights.forEach((e, k) => m.lights.push({ id: 7000 + k, x: e.x, y: e.y, room: e.rect, r: e.r, state: 'ember', col: SW.lampColour.ember, ph: k * 1.7 }));
    const d = m.particles.dust;
    if (d && m.lights.length) {
      const P2 = m.particles.dustLit || (m.particles.dustLit = { x: [], y: [], s: [], ax: [], ay: [], w: [], ph: [] });
      for (let j = d.x.length - 1; j >= 0; j--) {
        if (!m.lights.some(L => Math.hypot(L.x - d.x[j], L.y - d.y[j]) < L.r * 0.9)) continue;
        for (const k of ['x', 'y', 's', 'ax', 'ay', 'w', 'ph']) { P2[k].push(d[k][j]); d[k].splice(j, 1); }
      }
    }
    m.frost = [];
    for (const k of ['ice', 'fuel']) { const p = m.particles[k]; if (p) for (let j = 0; j < p.x.length; j += 7) m.frost.push({ x: p.x[j] - 0.4, y: p.y[j] - 0.4 }); }
    m.chain = chain;
    return m;
  };
  /** What a chain means for play: the proposal chips. */
  SW.playChips = function (m) {
    const has = e => m.chain.some(c => c.event === e), out = [];
    if (has('blast') || has('rock') || has('break')) out.push(['пробоины', 'новые входы']);
    if (m.vented && m.vented.some(v => v)) out.push(['открытые отсеки', 'невесомость']);
    if (has('fire')) out.push(['пожар', 'меньше добычи']);
    if (has('blast')) out.push(['взрыв', 'больше металлолома']);
    if (has('ice') || has('rock')) out.push(['лёд', 'вода']);
    if (has('rock')) out.push(['камни', 'руда']);
    if (has('break')) out.push(['разлом', 'две половины']);
    return out;
  };
})();
