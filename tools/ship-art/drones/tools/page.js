// The page's runtime, inlined by build.js after `const DATA = {...}`. Plain browser JS, no libraries.
// The scene emulates the sim (cell paths at four cells a second, drones docked in bay slots) and draws it two ways:
// as the game does now (LogisticsView.DrawDrone at cell centres) and as proposed (sprites, the motion model below).
(function () {
  'use strict';
  var C = 16, Z = 2, SPEED = 64; // a cell in pixels; the scene's zoom; four cells a second
  var DEFAULTS = { lift: 8, bobAmp: 1, bobPeriod: 1.4, tiltMax: 14, tiltCruise: 7, springHz: 2.5, damping: 0.65, swing: 18 };
  var PRESETS = {
    calm: { lift: 8, bobAmp: 0.6, bobPeriod: 2, tiltMax: 9, tiltCruise: 4, springHz: 2, damping: 0.9, swing: 10 },
    lively: { lift: 8, bobAmp: 1.5, bobPeriod: 1.1, tiltMax: 20, tiltCruise: 9, springHz: 3.2, damping: 0.5, swing: 28 },
  };
  var P = Object.assign({}, DEFAULTS);
  var picks = { drone: 'A', bay: 'A', mode: 'new', grid: false, slow: 1 };

  // ---------- images ----------
  var IMG = {};
  function load() {
    var names = Object.keys(DATA.img);
    return Promise.all(names.map(function (n) {
      return new Promise(function (ok) { var i = new Image(); i.onload = function () { IMG[n] = i; ok(); }; i.onerror = ok; i.src = DATA.img[n]; });
    }));
  }

  // ---------- the motion model: one rig a drone ----------
  // Follows a target (the sim's smoothed position, lifted) with a spring that also tracks the target's speed, so it
  // eases off and in, leans past a stop a little and rounds a corner; leans into its acceleration; bobs; swings its cargo.
  function Rig(id) {
    this.id = id; this.x = 0; this.y = 0; this.vx = 0; this.vy = 0; this.ax = 0;
    this.tilt = 0; this.face = 1; this.swing = 0; this.swingV = 0; this.spin = 0; this.spinPhase = id * 1.7; this.t = id * 0.37;
  }
  Rig.prototype.place = function (x, y) { this.x = x; this.y = y; this.vx = this.vy = this.ax = 0; this.tilt = 0; this.swing = this.swingV = 0; };
  Rig.prototype.step = function (dt, tx, ty, tvx, tvy, spinTo) {
    var w = 2 * Math.PI * P.springHz, z = P.damping;
    var ax = w * w * (tx - this.x) + 2 * z * w * (tvx - this.vx);
    var ay = w * w * (ty - this.y) + 2 * z * w * (tvy - this.vy);
    this.vx += ax * dt; this.vy += ay * dt; this.x += this.vx * dt; this.y += this.vy * dt; this.ax = ax;
    // lean: a little into its speed while it cruises, more into its acceleration as it sets off or brakes
    var deg = this.vx / SPEED * P.tiltCruise + ax * 0.012 * (P.tiltMax / 14);
    deg = Math.max(-P.tiltMax, Math.min(P.tiltMax, deg));
    this.tilt += (deg * Math.PI / 180 - this.tilt) * (1 - Math.exp(-dt / 0.08));
    if (this.vx > 6) this.face = 1; else if (this.vx < -6) this.face = -1;
    // the cargo, a pendulum pushed back by the acceleration
    var wc = 2 * Math.PI * 1.4, kc = (P.swing * Math.PI / 180) * wc * wc / 500;
    this.swingV += (-wc * wc * this.swing - 2 * 0.3 * wc * this.swingV - kc * ax) * dt;
    this.swing = Math.max(-0.6, Math.min(0.6, this.swing + this.swingV * dt));
    this.spin += (spinTo - this.spin) * (1 - Math.exp(-dt / 0.12));
    this.spinPhase += dt * 20 * this.spin; this.t += dt;
  };
  Rig.prototype.bob = function () {
    var moving = Math.min(1, Math.hypot(this.vx, this.vy) / SPEED);
    return P.bobAmp * Math.sin(2 * Math.PI * this.t / P.bobPeriod + this.id * 2.1) * (1 - 0.5 * moving) * this.spin;
  };

  // ---------- drawing a proposed drone ----------
  function look() { return DATA.drones[picks.drone]; }
  function px(v) { return Math.round(v * Z) / Z; }
  // where its parts are: the body turned by its tilt, mirrored to face its way
  function drawDrone(ctx, rig, cargo, pass, opts) {
    var L = look(), o = opts || {}, x = px(rig.x), y = px(rig.y + rig.bob());
    var frame = rig.spin < 0.05 ? -1 : Math.floor(rig.spinPhase) % 4;
    ctx.save(); ctx.translate(x, y); ctx.rotate(rig.tilt); ctx.scale(rig.face, 1);
    if (pass === 'lit') {
      ctx.drawImage(IMG[frame < 0 ? L.fly[0] : L.fly[frame]], -L.anchor[0], -L.anchor[1]);
    } else {
      ctx.drawImage(IMG[L.eye], -L.anchor[0], -L.anchor[1]);
      if (o.low) { if (blinkOn(rig.t)) ctx.drawImage(IMG[L.lampLow], -L.anchor[0], -L.anchor[1]); }
      else if (rig.spin > 0.5) ctx.drawImage(IMG[L.lampWork], -L.anchor[0], -L.anchor[1]);
    }
    ctx.restore();
    if (cargo && pass === 'lit') {
      // the clamp turns with the body; the stack hangs from it on a short tether, swinging, kept upright
      var cl = L.clamp, cx = x - Math.sin(rig.tilt) * cl, cy = y + Math.cos(rig.tilt) * cl, tl = 6;
      var ix = px(cx + Math.sin(rig.swing) * tl), iy = px(cy + Math.cos(rig.swing) * tl);
      ctx.fillStyle = '#7f848f';
      ctx.fillRect(px(cx + (ix - cx) * 0.2) - 0.5, px(cy), 1, 1);
      ctx.drawImage(IMG['icon_' + cargo], ix - 4, iy - 4, 8, 8);
      // the clamp's jaws over the stack's top corners
      ctx.fillStyle = '#8f99ad'; ctx.fillRect(ix - 3, iy - 5, 6, 1); ctx.fillStyle = '#474c57'; ctx.fillRect(ix - 3, iy - 4, 1, 1); ctx.fillRect(ix + 2, iy - 4, 1, 1);
    }
  }
  function blinkOn(t) { return Math.sin(t * 1.5 * 2 * Math.PI) > -0.3; }

  // ---------- drawing a drone as the game does now (LogisticsView.DrawDrone) ----------
  function drawNow(ctx, x, y, id, clock, cargo) {
    function r(a, b, w, h, c) { ctx.fillStyle = c; ctx.fillRect(x + a, y + b, w, h); }
    r(-5, -3, 10, 5, '#1a1e25'); r(-4, -2, 8, 3, '#cbd5e1'); r(-4, 0, 8, 1, '#8fa0b2'); r(1, -1, 2, 1, '#60a5fa');
    for (var side = -1; side <= 1; side += 2) {
      var hx = x + side * 5, hy = y - 4;
      ctx.fillStyle = '#8fa0b2'; ctx.fillRect(hx - 0.5, hy, 1, 2);
      var blade = 2 + 2 * Math.abs(Math.sin((clock * 12 + id * 0.37 + (side + 1) * 0.25) * Math.PI));
      ctx.fillStyle = '#e2e8f0'; ctx.fillRect(hx - blade, hy - 1, 2 * blade, 1);
    }
    if (cargo) ctx.drawImage(IMG['icon_' + cargo], x - 4, y + 3, 8, 8);
  }

  // ---------- the room ----------
  var MAP = DATA.map, MW = MAP[0].length, MH = MAP.length;
  function solid(cx, cy) { return cy < 0 || cy >= MH || cx < 0 || cx >= MW || MAP[cy][cx] === '#'; }
  var BAY = DATA.bayAt; // its top-left cell
  var bayX = BAY[0] * C, bayY = BAY[1] * C;

  function background() {
    var c = document.createElement('canvas'); c.width = MW * C; c.height = MH * C; var g = c.getContext('2d'); g.imageSmoothingEnabled = false;
    for (var y = 0; y < MH; y++) for (var x = 0; x < MW; x++) {
      var back = IMG.back_scrap; g.drawImage(back, (x * C) % back.width, (y * C) % back.height, C, C, x * C, y * C, C, C);
    }
    for (y = 0; y < MH; y++) for (x = 0; x < MW; x++) {
      if (MAP[y][x] === '#') {
        var wall = IMG.wall_iron; g.drawImage(wall, (x * C) % wall.width, (y * C) % wall.height, C, C, x * C, y * C, C, C);
        var open = (solid(x, y - 1) ? 0 : 1) | (solid(x + 1, y) ? 0 : 2) | (solid(x, y + 1) ? 0 : 4) | (solid(x - 1, y) ? 0 : 8);
        if (open) g.drawImage(IMG['edge_' + open], x * C, y * C);
      } else if (MAP[y][x] === 'H') g.drawImage(IMG.ladder, x * C, y * C);
    }
    DATA.machines.forEach(function (m) { if (m.art !== 'bay') g.drawImage(IMG[m.art], m.at[0] * C, m.at[1] * C); });
    return c;
  }

  // ---------- the drones' jobs, as the sim would fly them: cell paths, waits, a dock ----------
  // Each step: a path of corner cells (flown at four cells a second), a wait (taking or dropping a stack), or a rest docked.
  var JOBS = DATA.jobs;
  function plan(job) {
    var t = 0, steps = [];
    job.steps.forEach(function (s) {
      if (s.path) {
        for (var i = 1; i < s.path.length; i++) {
          var a = s.path[i - 1], b = s.path[i], d = (Math.abs(b[0] - a[0]) + Math.abs(b[1] - a[1])) * C;
          steps.push({ kind: 'fly', t0: t, t1: t + d / SPEED, a: a, b: b, cargo: s.cargo || null, low: !!s.low }); t += d / SPEED;
        }
      } else if (s.wait) { steps.push({ kind: 'wait', t0: t, t1: t + s.wait, at: s.at, cargo: s.cargo || null, low: !!s.low }); t += s.wait; }
      else if (s.rest) { steps.push({ kind: 'rest', t0: t, t1: t + s.rest, from: s.from, to: s.to }); t += s.rest; }
    });
    return { steps: steps, period: t, slot: job.slot, offset: job.offset };
  }
  var PLANS = JOBS.map(plan);
  function centre(c) { return [c[0] * C + C / 2, c[1] * C + C / 2]; }
  // the sim's drone at time t: where it is, how fast it goes, what it carries; or docked with its charge
  function simAt(pl, t) {
    var tt = ((t + pl.offset) % pl.period + pl.period) % pl.period;
    for (var i = 0; i < pl.steps.length; i++) {
      var s = pl.steps[i];
      if (tt < s.t1 || i === pl.steps.length - 1) {
        if (s.kind === 'fly') {
          var a = centre(s.a), b = centre(s.b), f = (tt - s.t0) / (s.t1 - s.t0), dx = Math.sign(b[0] - a[0]), dy = Math.sign(b[1] - a[1]);
          return { fly: true, x: a[0] + (b[0] - a[0]) * f, y: a[1] + (b[1] - a[1]) * f, vx: dx * SPEED, vy: dy * SPEED, cargo: s.cargo, low: s.low, cell: f < 0.5 ? s.a : s.b, since: tt - pl.steps[0].t0 };
        }
        if (s.kind === 'wait') { var c = centre(s.at); return { fly: true, x: c[0], y: c[1], vx: 0, vy: 0, cargo: s.cargo, low: s.low, cell: s.at, since: tt }; }
        var k = (tt - s.t0) / (s.t1 - s.t0);
        return { fly: false, charge: s.from + (s.to - s.from) * Math.min(1, k * 1.4), restT: tt - s.t0 };
      }
    }
  }

  // ---------- the bay ----------
  function padCentre(slot) { var p = DATA.pads[slot]; return [bayX + p[0] + DATA.dockCentre[0], bayY + p[1] + DATA.dockCentre[1]]; }
  // what each slot shows: a drone docked (with its charge, charging or not) or nothing
  function slots(t, landing) {
    var s = [null, null, null, null];
    DATA.parked.forEach(function (p) { s[p.slot] = { charge: p.charge, charging: false }; });
    PLANS.forEach(function (pl, i) {
      var st = simAt(pl, t);
      if (!st.fly && !(picks.mode === 'new' && landing[i])) s[pl.slot] = { charge: st.charge, charging: st.charge < 4 };
    });
    return s;
  }

  // ---------- the scene ----------
  var canvas, ctx, bg, rigs = [], landing = [], wasFlying = [], clock = 0, last = 0;
  function initScene() {
    canvas = document.getElementById('room');
    canvas.width = MW * C * Z; canvas.height = MH * C * Z;
    ctx = canvas.getContext('2d'); ctx.imageSmoothingEnabled = false;
    bg = background();
    rigs = PLANS.map(function (_, i) { return new Rig(i); });
    landing = PLANS.map(function () { return null; });
    wasFlying = PLANS.map(function () { return false; });
    requestAnimationFrame(frame);
  }

  function liftAt(cell) { return solid(cell[0], cell[1] - 1) ? 2 : P.lift; }

  function stepRigs(dt) {
    PLANS.forEach(function (pl, i) {
      var st = simAt(pl, clock), rig = rigs[i], pad = padCentre(pl.slot);
      if (st.fly) {
        if (!wasFlying[i]) {
          // just launched: off the pad, rotors spinning up; met mid-flight (the view switched): where the sim is
          if (st.since < 0.3) { rig.place(pad[0], pad[1]); rig.spin = 0; rig.hold = 0.22; } else { rig.place(st.x, st.y - liftAt(st.cell)); rig.spin = 1; rig.hold = 0; }
          landing[i] = null;
        }
        wasFlying[i] = true;
        if (rig.hold > 0) { rig.hold -= dt; rig.step(dt, pad[0], pad[1] - 1, 0, 0, 1); return; }
        rig.step(dt, st.x, st.y - liftAt(st.cell), st.vx, st.vy, 1);
        rig.cargo = st.cargo; rig.low = st.low;
      } else {
        if (wasFlying[i]) { landing[i] = { t: 0 }; wasFlying[i] = false; rig.cargo = null; }
        var L = landing[i];
        if (!L) return;
        L.t += dt;
        // over the pad first, then down onto it as the rotors slow
        if (L.t < 0.55) rig.step(dt, pad[0], pad[1] - 4, 0, 0, 1);
        else if (L.t < 1.0) { var k = (L.t - 0.55) / 0.45, e = 1 - (1 - k) * (1 - k); rig.step(dt, pad[0], pad[1] - 4 + 4 * e, 0, 0, 1 - k); }
        else landing[i] = null;
      }
    });
  }

  function glow(g, x, y, r, rgb, a) {
    var gr = g.createRadialGradient(x, y, 0, x, y, r);
    gr.addColorStop(0, 'rgba(' + rgb + ',' + a + ')'); gr.addColorStop(1, 'rgba(' + rgb + ',0)');
    g.fillStyle = gr; g.fillRect(x - r, y - r, 2 * r, 2 * r);
  }

  function frame(ts) {
    var dt = Math.min(0.05, (ts - (last || ts)) / 1000) * picks.slow; last = ts;
    var n = Math.max(1, Math.ceil(dt / (1 / 240)));
    for (var k = 0; k < n; k++) { clock += dt / n; if (picks.mode === 'new') stepRigs(dt / n); }
    if (picks.mode !== 'new') PLANS.forEach(function (_, i) { var st = simAt(PLANS[i], clock); wasFlying[i] = false; landing[i] = null; });
    draw();
    requestAnimationFrame(frame);
  }

  function draw() {
    var g = ctx; g.setTransform(Z, 0, 0, Z, 0, 0); g.imageSmoothingEnabled = false; g.globalCompositeOperation = 'source-over';
    g.drawImage(bg, 0, 0);
    var now = picks.mode !== 'new', bay = now ? null : DATA.bays[picks.bay], sl = slots(clock, landing), L = look();
    // ---- lit: the bay and its docked drones, the drones in flight with their cargo
    g.drawImage(IMG[now ? 'bay_cur' : bay.base], bayX, bayY);
    if (!now) sl.forEach(function (s, i) { if (s) g.drawImage(IMG[L.dock], bayX + DATA.pads[i][0], bayY + DATA.pads[i][1]); });
    var flying = [];
    PLANS.forEach(function (pl, i) {
      var st = simAt(pl, clock);
      if (now) { if (st.fly) flying.push({ i: i, st: st }); }
      else if (st.fly || landing[i]) flying.push({ i: i, st: st });
    });
    flying.forEach(function (f) {
      if (now) drawNow(g, Math.round(f.st.x * Z) / Z, Math.round(f.st.y * Z) / Z, f.i, clock, f.st.cargo);
      else drawDrone(g, rigs[f.i], f.st.fly ? f.st.cargo : null, 'lit');
    });
    // ---- the room's light: dimmed, the lamps and the fire adding theirs
    g.globalCompositeOperation = 'multiply'; g.fillStyle = '#7d8596'; g.fillRect(0, 0, MW * C, MH * C);
    g.globalCompositeOperation = 'lighter';
    DATA.lights.forEach(function (l) { glow(g, l.at[0], l.at[1], l.r, l.rgb, l.a); });
    // the drones' and the pads' light
    if (!now) {
      sl.forEach(function (s, i) {
        if (!s) return; var gp = DATA.glows[i], pulse = s.charging ? 0.7 + 0.3 * Math.cos(clock * 0.8 * 2 * Math.PI) : 0.55;
        glow(g, bayX + gp[0] + 4, bayY + gp[1], 10, s.charge < 1 ? '242,184,75' : '126,227,107', 0.22 * pulse);
      });
      flying.forEach(function (f) {
        var r = rigs[f.i]; if (r.spin < 0.5 && !f.st.low) return;
        if (f.st.low) { if (blinkOn(r.t)) glow(g, r.x, r.y - 5, 12, '242,184,75', 0.4); }
        else glow(g, r.x, r.y - 5, 9, '125,211,252', 0.22);
      });
    } else {
      sl.forEach(function (s, i) { if (s) { var p = DATA.curPads[i]; glow(g, bayX + p[0] + 4, bayY + p[1] + 1, 10, '125,211,252', 0.22); } });
      flying.forEach(function (f) { if (f.st.low && blinkOn(clock)) glow(g, f.st.x, f.st.y - 3, 12, '242,184,75', 0.4); });
    }
    // ---- unlit: lamps' bulbs, the pads' lights and charge, the drones' eyes and lamps
    g.globalCompositeOperation = 'source-over';
    DATA.machines.forEach(function (m) { if (m.art === 'lamp') g.drawImage(IMG.lamp, m.at[0] * C, m.at[1] * C); });
    if (now) {
      sl.forEach(function (s, i) { if (s) { var p = DATA.curPads[i]; g.fillStyle = '#7dd3fc'; g.fillRect(bayX + p[0], bayY + p[1], 8, 2); } });
      flying.forEach(function (f) { if (f.st.low && blinkOn(clock)) { g.fillStyle = '#f2b84b'; g.fillRect(Math.round(f.st.x * Z) / Z - 1, Math.round(f.st.y * Z) / Z - 4, 2, 2); } });
    } else {
      sl.forEach(function (s, i) {
        if (!s) return; var gp = DATA.glows[i], lp = DATA.lamps[i];
        if (s.charging || s.charge >= 4) { g.globalAlpha = s.charging ? 0.6 + 0.4 * Math.cos(clock * 0.8 * 2 * Math.PI) : 1; g.drawImage(IMG.pad_glow, bayX + gp[0], bayY + gp[1]); g.globalAlpha = 1; }
        var lv = Math.floor(s.charge + 1e-6);
        // while it charges the next lamp blinks in, as a battery's bar fills
        if (s.charging && lv < 4 && Math.sin(clock * 2 * Math.PI) > 0) lv += 1;
        g.drawImage(IMG[s.charge < 1 ? 'charge_low' : 'charge_' + Math.min(4, lv)], bayX + lp[0], bayY + lp[1]);
      });
      if (bay.beacon) { g.fillStyle = '#7dd3fc'; g.fillRect(bayX + bay.beacon[0], bayY + bay.beacon[1], 2, 1); }
      sl.forEach(function (s, i) { if (s) g.drawImage(IMG[L.dockEye], bayX + DATA.pads[i][0], bayY + DATA.pads[i][1]); });
      flying.forEach(function (f) { drawDrone(g, rigs[f.i], null, 'unlit', { low: f.st.fly ? f.st.low : rigs[f.i].low }); });
    }
    if (picks.grid) drawGrid(g, now, flying);
  }

  // cell lines, the sim's position (a dot at the cell centre path) and the drawn one
  function drawGrid(g, now, flying) {
    g.globalAlpha = 0.35; g.fillStyle = '#9fb3c8';
    for (var x = 0; x <= MW; x++) g.fillRect(x * C, 0, 0.5, MH * C);
    for (var y = 0; y <= MH; y++) g.fillRect(0, y * C, MW * C, 0.5);
    g.globalAlpha = 1;
    flying.forEach(function (f) {
      if (!f.st.fly) return;
      g.fillStyle = '#ff5d73'; g.fillRect(f.st.x - 1, f.st.y - 1, 2, 2);
    });
  }

  // ---------- small previews: one drone flying a few cells to and fro with a stack, at x4 ----------
  function Mini(el, id) {
    this.el = el; this.id = id; this.g = el.getContext('2d'); this.rig = new Rig(3); this.t = 0;
    this.W = el.width / 4; this.H = el.height / 4; this.rig.place(this.W / 2 - 24, this.H / 2); this.rig.spin = 1;
  }
  Mini.prototype.frame = function (dt) {
    var r = this.rig, cyc = 4.4, tt = this.t % cyc, a = this.W / 2 - 24, b = this.W / 2 + 24, y0 = this.H / 2 + 2, tx, tv;
    // stop 0.8 s, fly 48 px right (0.75 s), stop, fly back
    if (tt < 0.8) { tx = a; tv = 0; } else if (tt < 1.55) { tx = a + (tt - 0.8) * SPEED; tv = SPEED; }
    else if (tt < 2.95) { tx = b; tv = 0; } else if (tt < 3.7) { tx = b - (tt - 2.95) * SPEED; tv = -SPEED; } else { tx = a; tv = 0; }
    var n = Math.ceil(dt / (1 / 240)); for (var k = 0; k < n; k++) r.step(dt / n, tx, y0 - P.lift, tv, 0, 1);
    this.t += dt;
    var g = this.g; g.setTransform(4, 0, 0, 4, 0, 0); g.imageSmoothingEnabled = false;
    g.fillStyle = '#151a22'; g.fillRect(0, 0, this.W, this.H);
    var back = IMG.back_scrap; for (var bx = 0; bx < this.W; bx += 64) for (var by = 0; by < y0 + 8; by += 32) g.drawImage(back, 0, 0, 64, 32, bx, by, 64, 32);
    g.fillStyle = 'rgba(10,13,18,.35)'; g.fillRect(0, 0, this.W, y0 + 8);
    for (var x = 0; x < this.W; x += 16) { g.drawImage(IMG.wall_iron, x % 64, 0, 16, 16, x, y0 + 8, 16, 16); g.drawImage(IMG.edge_1, x, y0 + 8); }
    var keep = picks.drone; picks.drone = this.id;
    drawDrone(g, r, 'scrapChunk', 'lit'); drawDrone(g, r, null, 'unlit', {});
    picks.drone = keep;
    // the cell's centre the sim flies along, faint
    g.fillStyle = 'rgba(255,93,115,.6)'; g.fillRect(0, y0 - 0.25, this.W, 0.5);
  };
  var minis = [], mLast = 0;
  function miniLoop(ts) { var dt = Math.min(0.05, (ts - (mLast || ts)) / 1000) * picks.slow; mLast = ts; minis.forEach(function (m) { m.frame(dt); }); requestAnimationFrame(miniLoop); }

  // ---------- controls and the answer line ----------
  function fmt(v) { return Math.round(v * 100) / 100; }
  function sliders() {
    document.querySelectorAll('[data-p]').forEach(function (inp) {
      var key = inp.getAttribute('data-p'), out = document.querySelector('[data-out="' + key + '"]');
      inp.value = P[key]; if (out) out.textContent = fmt(P[key]);
      inp.addEventListener('input', function () {
        P[key] = parseFloat(inp.value); if (out) out.textContent = fmt(P[key]);
        var r = document.querySelector('input[name="pick-motion"][value="sliders"]'); if (r) r.checked = true; answer();
      });
    });
  }
  function setP(src) {
    Object.assign(P, src);
    document.querySelectorAll('[data-p]').forEach(function (inp) { var k = inp.getAttribute('data-p'); inp.value = P[k]; var o = document.querySelector('[data-out="' + k + '"]'); if (o) o.textContent = fmt(P[k]); });
  }
  function answer() {
    var parts = [];
    document.querySelectorAll('.pickrow').forEach(function (f) {
      var c = f.querySelector('input:checked'); if (!c || !c.value) return;
      var label = c.getAttribute('data-say') || c.value;
      if (c.value === 'sliders') label = 'по ползункам (подъём ' + fmt(P.lift) + ' px, качание ' + fmt(P.bobAmp) + ' px / ' + fmt(P.bobPeriod) + ' с, наклон ' + fmt(P.tiltMax) + '° макс / ' + fmt(P.tiltCruise) + '° в полёте, пружина ' + fmt(P.springHz) + ' Гц / ' + fmt(P.damping) + ', груз ' + fmt(P.swing) + '°)';
      parts.push(f.getAttribute('data-group') + ': ' + label);
    });
    document.getElementById('line').textContent = parts.length ? parts.join(' · ') : 'Ничего не выбрано';
  }
  function controls() {
    document.addEventListener('change', function (e) {
      var t = e.target;
      if (t.name === 'pick-drone' && t.value && t.value !== 'now') picks.drone = t.value;
      if (t.name === 'pick-bay' && t.value && t.value !== 'now') picks.bay = t.value;
      if (t.name === 'pick-motion') { if (t.value === 'mock') setP(DEFAULTS); else if (t.value === 'calm') setP(PRESETS.calm); else if (t.value === 'lively') setP(PRESETS.lively); }
      if (t.name === 'view') { picks.mode = t.value; document.body.classList.toggle('mode-now', t.value === 'now'); }
      if (t.name === 'scene-drone') picks.drone = t.value;
      if (t.name === 'scene-bay') picks.bay = t.value;
      if (t.id === 'grid') picks.grid = t.checked;
      if (t.name === 'slow') picks.slow = parseFloat(t.value);
      if (t.matches('.pickrow input')) answer();
      syncSceneRadios();
    });
    document.getElementById('reset').addEventListener('click', function () { setP(DEFAULTS); var r = document.querySelector('input[name="pick-motion"][value="mock"]'); if (r) r.checked = true; answer(); });
    document.getElementById('copy').addEventListener('click', function () {
      var b = this, text = document.getElementById('line').textContent;
      var done = function () { b.textContent = 'Скопировано'; setTimeout(function () { b.textContent = 'Копировать'; }, 1400); };
      if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text).then(done, done); else done();
    });
  }
  // the scene's own drone and bay switches follow the picks below and the other way round
  function syncSceneRadios() {
    var d = document.querySelector('input[name="scene-drone"][value="' + picks.drone + '"]'); if (d) d.checked = true;
    var b = document.querySelector('input[name="scene-bay"][value="' + picks.bay + '"]'); if (b) b.checked = true;
  }

  // for checking the page while its tab is in the background: advance everything by `sec` and draw once
  window.__drones = { tick: function (sec) { var n = Math.ceil(sec * 240); for (var k = 0; k < n; k++) { clock += sec / n; if (picks.mode === 'new') stepRigs(sec / n); } draw(); minis.forEach(function (m) { m.frame(sec); }); } };

  load().then(function () {
    initScene(); sliders(); controls(); answer(); syncSceneRadios();
    document.querySelectorAll('canvas.mini').forEach(function (el) { minis.push(new Mini(el, el.getAttribute('data-look'))); });
    requestAnimationFrame(miniLoop);
  });
})();
