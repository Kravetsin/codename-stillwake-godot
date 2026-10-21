'use strict';
// ===== the places the hero is shown in, from the game's own tiles, lit as the game lights them =====
const IMG = {};
const hash = (x, y, s) => { let h = (x * 374761393 + y * 668265263 + s * 2147483647) | 0; h = (h ^ (h >>> 13)) * 1274126177 | 0; return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
// the glow mask of a picture: black where it covers, its own colour where a pixel passes `lit`
function emissiveOf(img, lit) {
  const c = canvasOf(img.width, img.height), g = c.getContext('2d'); g.drawImage(img, 0, 0);
  const d = g.getImageData(0, 0, c.width, c.height);
  for (let i = 0; i < d.data.length; i += 4) { if (!d.data[i + 3]) continue; if (!lit(d.data[i], d.data[i + 1], d.data[i + 2])) { d.data[i] = d.data[i + 1] = d.data[i + 2] = 0; } }
  g.putImageData(d, 0, 0); return c;
}
function blackOf(img) { const c = canvasOf(img.width, img.height), g = c.getContext('2d'); g.drawImage(img, 0, 0); g.globalCompositeOperation = 'source-in'; g.fillStyle = '#000'; g.fillRect(0, 0, c.width, c.height); return c; }
const EM = {};
function prepScenes() {
  EM.lamp = emissiveOf(IMG.machine_lamp, (r, g, b) => r > 220 && g > 220);
  EM.analyzer = emissiveOf(IMG.machine_analyzer, (r, g, b) => b > 200 && r < 200 || r + g + b > 600);
  for (const k of ['machine_crate', 'wall_iron', 'ladder']) EM[k] = blackOf(IMG[k]);
}

// tiles laid by where they are in the world, as ShipTiles lays them
function tileAt(g, img, cx, cy, X, Y) { const n = img.width / CELL, m = img.height / CELL; g.drawImage(img, ((cx % n) + n) % n * CELL, ((cy % m) + m) % m * CELL, CELL, CELL, X, Y, CELL, CELL); }
function edgeOf(open) { return IMG['wall-edge_iron_' + open]; }

// A scene draws the world rect [x0, x0+w) × [y0, y0+h) into the picture, its glow mask, and lists its lights.
// `ground` is the floor's y; climbing scenes are shafts with no floor in view.
class Scene {
  constructor(kind, w, h) { this.kind = kind; this.w = w; this.h = h; this.ground = h - 16; this.t = 0; }
  get ambient() { return { deck: [0.42, 0.41, 0.44], planet: [1, 1, 1], cave: [0.02, 0.024, 0.034], space: [0.74, 0.76, 0.86] }[this.kind]; }
  draw(a, e, x0, y0, shaft) {
    a.setTransform(1, 0, 0, 1, 0, 0); e.setTransform(1, 0, 0, 1, 0, 0);
    e.fillStyle = '#000'; e.fillRect(0, 0, this.w, this.h);
    const lights = [];
    this['draw_' + this.kind](a, e, x0, y0, shaft, lights);
    return lights;
  }
  cells(x0, y0, fn) {
    const c0 = Math.floor(x0 / CELL), r0 = Math.floor(y0 / CELL);
    for (let cy = r0; cy <= r0 + Math.ceil(this.h / CELL) + 1; cy++) for (let cx = c0; cx <= c0 + Math.ceil(this.w / CELL) + 1; cx++) fn(cx, cy, Math.round(cx * CELL - x0), Math.round(cy * CELL - y0));
  }
  solidDeck(cx, cy, shaft) {
    const gr = this.ground / CELL; // the floor's row
    if (shaft) return cx <= 0 || cx >= this.w / CELL - 1;
    return cy >= gr || cy < 1;
  }
  draw_deck(a, e, x0, y0, shaft, lights) {
    a.fillStyle = '#0b0d12'; a.fillRect(0, 0, this.w, this.h);
    const solid = (cx, cy) => this.solidDeck(cx, cy, shaft);
    this.cells(x0, y0, (cx, cy, X, Y) => {
      if (solid(cx, cy)) {
        tileAt(a, IMG.wall_iron, cx, cy, X, Y);
        const open = (!solid(cx, cy - 1) ? 1 : 0) | (!solid(cx + 1, cy) ? 2 : 0) | (!solid(cx, cy + 1) ? 4 : 0) | (!solid(cx - 1, cy) ? 8 : 0);
        if (open) a.drawImage(edgeOf(open), X, Y);
      } else tileAt(a, IMG.back_scrap, cx, cy, X, Y);
    });
    if (shaft) {
      // a lamp on the shaft wall every 6 rows
      this.cells(x0, y0, (cx, cy, X, Y) => { if ((cx === 2 || cx === Math.floor(this.w / CELL) - 3) && ((cy % 6) + 6) % 6 === (cx === 2 ? 0 : 3)) { a.drawImage(IMG.machine_lamp, X, Y - 2); e.drawImage(EM.lamp, X, Y - 2); lights.push({ x: X + 8, y: Y + 7, r: 6 * CELL, c: [1, 0.86, 0.62], k: 0.9 }); } });
      return;
    }
    // props, every 24 cells: a lamp on the ceiling, a crate, an analyzer, another lamp
    const P = 24 * CELL, base = Math.floor(x0 / P) * P;
    for (let px = base - P; px < x0 + this.w + P; px += P) {
      const X = Math.round(px - x0), gY = this.ground;
      for (const lx of [3, 15]) { a.drawImage(IMG.machine_lamp, X + lx * CELL, 16); e.drawImage(EM.lamp, X + lx * CELL, 16); lights.push({ x: X + lx * CELL + 8, y: 16 + 9, r: 9 * CELL, c: [1, 0.84, 0.58], k: 1.3 }); }
      a.drawImage(IMG.machine_crate, X + 7 * CELL, gY - 16); e.drawImage(EM.machine_crate, X + 7 * CELL, gY - 16);
      a.drawImage(IMG.machine_analyzer, X + 18 * CELL, gY - 32); e.drawImage(EM.analyzer, X + 18 * CELL, gY - 32);
      lights.push({ x: X + 18 * CELL + 16, y: gY - 22, r: 3 * CELL, c: [0.45, 0.75, 1], k: 0.5 });
    }
    // top ceiling row is solid at y < 0... draw it at the panel top
  }
  draw_planet(a, e, x0, y0, shaft, lights) {
    if (shaft) return this.draw_shaft(a, e, x0, y0, lights, 'soil');
    const gY = this.ground;
    a.drawImage(IMG.sky, 0, 384 - gY - 90, 1, gY + 90, 0, 0, this.w, gY + 2);
    const lay = (img, k, bottom) => { const ox = ((x0 * k) % img.width + img.width) % img.width; for (let x = -ox; x < this.w; x += img.width) a.drawImage(img, Math.round(x), Math.round(bottom - img.height)); };
    lay(IMG.far, 0.08, gY + 150); lay(IMG.hills, 0.2, gY + 100);
    // the ground: soil, its grassy top, a few plants
    this.cells(x0, y0, (cx, cy, X, Y) => { if (Y >= gY) tileAt(a, IMG.wall_soil, cx, cy, X, Y); });
    a.fillStyle = 'rgba(20,14,8,0.35)'; a.fillRect(0, gY, this.w, 1);
    this.cells(x0, y0, (cx, cy, X, Y) => { if (Y === gY) a.drawImage(IMG.top_soil, (((cx % 4) + 4) % 4) * CELL, 0, CELL, 18, X, gY - 16, CELL, 18); });
    const P = 20 * CELL, base = Math.floor(x0 / P) * P;
    for (let px = base - P; px < x0 + this.w + P; px += P) {
      const X = Math.round(px - x0);
      a.drawImage(IMG.rock_0, X + 2 * CELL, gY - 18);
      a.drawImage(IMG.bush_0, X + 9 * CELL, gY - 21); e.drawImage(IMG.bush_0_glow, X + 9 * CELL, gY - 21);
      for (const bx of [6, 15]) { a.drawImage(IMG.bellTeal_0, X + bx * CELL, gY - 15); e.drawImage(IMG.bellTeal_0_glow, X + bx * CELL, gY - 15); }
      a.drawImage(IMG.tuft_0, X + 13 * CELL, gY - 13); a.drawImage(IMG.tuft_0, X + 18 * CELL + 5, gY - 13);
    }
  }
  draw_shaft(a, e, x0, y0, lights, mat) {
    const last = Math.floor(this.w / CELL) - 1, wall = IMG['wall_' + (mat === 'soil' ? 'soil' : 'stone')], back = IMG['back_' + (mat === 'soil' ? 'soil' : 'stone')];
    this.cells(x0, y0, (cx, cy, X, Y) => {
      const solid = cx <= 0 || cx >= last;
      tileAt(a, solid ? wall : back, cx, cy, X, Y);
      if (solid) { a.fillStyle = 'rgba(10,8,6,0.25)'; a.fillRect(X, Y, CELL, CELL); if (cx === 0) { a.fillStyle = '#16110c'; a.fillRect(X + CELL - 1, Y, 1, CELL); } if (cx === last) { a.fillStyle = '#16110c'; a.fillRect(X, Y, 1, CELL); } }
    });
  }
  draw_cave(a, e, x0, y0, shaft, lights) {
    if (shaft) { this.draw_shaft(a, e, x0, y0, lights, 'stone'); this.caveLights(a, e, x0, lights, true); return; }
    const gY = this.ground, top = 18;
    this.cells(x0, y0, (cx, cy, X, Y) => {
      const solid = Y >= gY || Y + CELL <= top;
      tileAt(a, solid ? IMG.wall_stone : IMG.back_stone, cx, cy, X, Y);
    });
    a.fillStyle = '#0c0e13'; a.fillRect(0, gY, this.w, 1); a.fillRect(0, top - 1, this.w, 1);
    this.caveLights(a, e, x0, lights, false);
  }
  caveLights(a, e, x0, lights, shaft) {
    const gY = this.ground, P = 18 * CELL, base = Math.floor(x0 / P) * P;
    for (let px = base - P; px < x0 + this.w + P; px += P) {
      const X = Math.round(px - x0);
      if (shaft) continue;
      a.drawImage(IMG.crystalIce_0, X + 2 * CELL, gY - 16); glowDraw(e, IMG.crystalIce_0_glow, X + 2 * CELL, gY - 16);
      lights.push({ x: X + 2 * CELL + 11, y: gY - 8, r: 5 * CELL, c: [0.45, 0.8, 1], k: 0.45 });
      a.drawImage(IMG.glowcapTeal_0, X + 11 * CELL, gY - 21); glowDraw(e, IMG.glowcapTeal_0_glow, X + 11 * CELL, gY - 21);
      lights.push({ x: X + 11 * CELL + 12, y: gY - 12, r: 5 * CELL, c: [0.4, 1, 0.85], k: 0.45 });
      a.drawImage(IMG.crystalIceHang_0, X + 7 * CELL, 17); glowDraw(e, IMG.crystalIceHang_0_glow, X + 7 * CELL, 17);
      lights.push({ x: X + 7 * CELL + 11, y: 24, r: 4 * CELL, c: [0.45, 0.8, 1], k: 0.35 });
      for (const sx of [4, 9, 15]) a.drawImage(IMG['stalactite_' + (sx % 4) + '_0'], X + sx * CELL + 5, 17);
    }
  }
  draw_space(a, e, x0, y0, shaft, lights) {
    a.fillStyle = '#05070c'; a.fillRect(0, 0, this.w, this.h);
    for (const [k, n, br] of [[0.05, 90, 0.45], [0.15, 40, 0.85]]) {
      const span = 640, ox = ((x0 * k) % span + span) % span;
      for (let i = 0; i < n; i++) {
        const sx = hash(i, 1, n) * span - ox, sy = hash(i, 2, n) * this.h, b = br * (0.4 + 0.6 * hash(i, 3, n));
        for (const off of [0, span]) { const X = Math.floor(sx + off); if (X < 0 || X >= this.w) continue; const v = Math.round(150 + 105 * b); a.fillStyle = `rgb(${v},${v},${Math.min(255, v + 12)})`; a.fillRect(X, Math.floor(sy), 1, 1); e.fillStyle = `rgba(${v},${v},${v},${b * 0.5})`; e.fillRect(X, Math.floor(sy), 1, 1); }
      }
    }
    // drifting debris far off, dim
    const P = 30 * CELL, base = Math.floor(x0 * 0.5 / P) * P;
    for (let px = base - P; px < x0 * 0.5 + this.w + P; px += P) {
      const X = Math.round(px - x0 * 0.5);
      a.globalAlpha = 0.55; a.drawImage(IMG.asteroid_02, X + 3 * CELL, 14); a.drawImage(IMG.scrap_01, X + 16 * CELL, this.h - 34); a.drawImage(IMG.hull_01, X + 24 * CELL, 40); a.globalAlpha = 1;
    }
  }
}

// ---------- light: ambient, round lights, beams; the lightmap multiplies the picture ----------
function paintLights(g, w, h, ambient, lights) {
  g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'source-over';
  g.fillStyle = rgbCss(ambient.map(v => v * 255)); g.fillRect(0, 0, w, h);
  g.globalCompositeOperation = 'lighter';
  for (const L of lights) {
    const col = (k) => `rgba(${L.c[0] * 255 | 0},${L.c[1] * 255 | 0},${L.c[2] * 255 | 0},${clamp(k, 0, 1)})`;
    if (L.beam) {
      for (const [sp, kk] of [[0.55, 0.35], [0.85, 0.45], [1.05, 0.25], [1.25, 0.08]]) {
        const gr = g.createRadialGradient(L.x, L.y, 0, L.x, L.y, L.r);
        gr.addColorStop(0, col(L.k * kk)); gr.addColorStop(0.55, col(L.k * kk * 0.55)); gr.addColorStop(1, col(0));
        g.fillStyle = gr; g.beginPath(); g.moveTo(L.x, L.y); g.arc(L.x, L.y, L.r, L.ang - L.spread * sp, L.ang + L.spread * sp); g.closePath(); g.fill();
      }
    } else {
      const gr = g.createRadialGradient(L.x, L.y, 0, L.x, L.y, L.r);
      gr.addColorStop(0, col(L.k)); gr.addColorStop(0.4, col(L.k * 0.45)); gr.addColorStop(1, col(0));
      g.fillStyle = gr; g.fillRect(L.x - L.r, L.y - L.r, L.r * 2, L.r * 2);
    }
  }
  g.globalCompositeOperation = 'source-over';
}

// ---------- particles: crisp motes, thin streaks; matter drawn lit, light drawn as glow ----------
class Parts {
  constructor() { this.list = []; }
  add(p) { p.age = 0; this.list.push(p); if (this.list.length > 500) this.list.shift(); }
  step(dt) {
    const s = dt / 1000;
    for (const p of this.list) {
      p.age += dt; if (p.drag) { const k = Math.exp(-p.drag * s); p.vx *= k; p.vy *= k; }
      p.vy += (p.g || 0) * s; p.x += p.vx * s; p.y += p.vy * s;
      if (p.floor != null && p.y > p.floor) { p.y = p.floor; p.vy = 0; p.vx *= 0.8; }
    }
    this.list = this.list.filter(p => p.age < p.life);
  }
  shift(dx, dy) { for (const p of this.list) { p.x -= dx; p.y -= dy; if (p.floor != null) p.floor -= dy; } }
  draw(a, e, lights) {
    for (const p of this.list) {
      const k = 1 - p.age / p.life, al = (p.a ?? 1) * k, g = p.glow ? e : a;
      g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = al; g.fillStyle = g.strokeStyle = p.col;
      if (p.kind === 'mote') g.fillRect(p.x - 0.5, p.y - 0.5, p.size || 1, p.size || 1);
      else { const sp = Math.hypot(p.vx, p.vy) || 1, len = (p.len || 4) * (0.4 + 0.6 * k); g.lineWidth = p.w || 1; g.beginPath(); g.moveTo(p.x, p.y); g.lineTo(p.x - p.vx / sp * len, p.y - p.vy / sp * len); g.stroke(); }
      g.globalAlpha = 1;
      if (p.glow && p.light) lights.push({ x: p.x, y: p.y, r: p.light, c: p.lc || [1, 0.8, 0.5], k: 0.12 * k });
    }
  }
}
const rnd = (a, b) => a + Math.random() * (b - a);

function glowDraw(e, img, x, y) { e.globalAlpha = 0.55; e.drawImage(img, x, y); e.globalAlpha = 1; }
