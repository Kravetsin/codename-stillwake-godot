// The bulb, hung at the screen's top (the hologram-keys ADR), as picked: straight ink on a canvas, the finish left off. Its origin is (20, 24) of 40 by 48.
(function () {
'use strict';
const { canvas } = Anime;
const INK = '#0a0d13', AMBER_RGB = [242, 184, 75], HOT = '#fff6ce';
const rgba = (rgb, a) => `rgba(${rgb[0]},${rgb[1]},${rgb[2]},${a})`;
const clamp = (v) => Math.min(1, Math.max(0, v));
const hot = (k) => (k < .2 ? '#4a2012' : k < .45 ? '#8a3c17' : k < .7 ? '#c9701f' : k < .95 ? '#f2b84b' : '#ffe6a8');

const spline = (p) => {
  const n = p.length - 1;
  return (t) => {
    const f = clamp(t) * n, i = Math.min(n - 1, Math.floor(f)), u = f - i;
    const a = p[Math.max(0, i - 1)], b = p[i], c = p[i + 1], d = p[Math.min(n, i + 2)];
    const q = (k) => .5 * (2 * b[k] + (c[k] - a[k]) * u + (2 * a[k] - 5 * b[k] + 4 * c[k] - d[k]) * u * u + (3 * b[k] - a[k] - 3 * c[k] + d[k]) * u * u * u);
    return [q(0), q(1)];
  };
};
/** The board's hand along a curve: it wanders a little and swells. */
const along = (fn, n, seed, amp) => {
  const out = [];
  for (let i = 0; i <= n; i++) {
    const t = i / n, [x, y] = fn(t), [x1, y1] = fn(Math.min(1, t + .01)), [x0, y0] = fn(Math.max(0, t - .01));
    const dx = x1 - x0, dy = y1 - y0, l = Math.hypot(dx, dy) || 1, w = (Math.sin(t * 7 + seed) * 1.1 + Math.sin(t * 19 + seed * 1.7) * .6) * Math.min(1, t * 6, (1 - t) * 6) * amp;
    out.push({ x: x - (dy / l) * w, y: y + (dx / l) * w, w: .62 + .38 * Math.sin(t * 11 + seed * 2.3) });
  }
  return out;
};
const ribbon = (c, pts, p, width, colour) => {
  const n = pts.length - 1, last = Math.min(n, p * n), L = [], R = [];
  if (last <= 0) return;
  for (let i = 0; i <= Math.ceil(last); i++) {
    const f = Math.min(i, last), i0 = Math.floor(f), u = f - i0, a = pts[i0], b = pts[Math.min(n, i0 + 1)], s = pts[Math.max(0, i0 - 1)], e = pts[Math.min(n, i0 + 1)];
    const q = [a.x + (b.x - a.x) * u, a.y + (b.y - a.y) * u], dx = e.x - s.x, dy = e.y - s.y, l = Math.hypot(dx, dy) || 1, h = (width / 2) * a.w * Math.min(1, (f + .4) / 2.4, (n - f + .4) / 2.4);
    L.push([q[0] - (dy / l) * h, q[1] + (dx / l) * h]);
    R.push([q[0] + (dy / l) * h, q[1] - (dx / l) * h]);
  }
  c.fillStyle = colour;
  c.beginPath();
  L.forEach((q, i) => (i ? c.lineTo(q[0], q[1]) : c.moveTo(q[0], q[1])));
  for (let i = R.length - 1; i >= 0; i--) c.lineTo(R[i][0], R[i][1]);
  c.closePath();
  c.fill();
};
const line = (c, pts, close) => {
  c.beginPath();
  pts.forEach((q, i) => (i ? c.lineTo(q[0], q[1]) : c.moveTo(q[0], q[1])));
  if (close) c.closePath();
};

const PEAR = along(spline([[6, 10], [8, 4], [13, -4], [14, -11], [10, -18], [0, -21], [-10, -18], [-14, -11], [-13, -4], [-8, 4], [-6, 10]]), 44, 2.1, .45);
const EDGE = along(spline([[-11, -13], [-8, -17], [-2, -19.5], [4, -19]]), 12, 1.2, .2);
const pear = (c) => line(c, PEAR.map((q) => [q.x, q.y]), true);

const cap = (c) => {
  c.fillStyle = '#2c313b'; c.fillRect(-6.5, 9.5, 13, 8);
  c.fillStyle = '#0d1016'; c.fillRect(-6.5, 12, 13, 1); c.fillRect(-6.5, 15, 13, 1);
  c.fillStyle = '#7a828f'; c.fillRect(-6.5, 9.5, 13, 1);
  c.fillStyle = '#14171d'; c.fillRect(-3, 17.5, 6, 2.5);
};
const glass = (c, p) => {
  if (p >= .6) {
    pear(c); c.fillStyle = '#2f3541'; c.fill();
    c.save(); pear(c); c.clip(); c.fillStyle = '#4f5664'; c.beginPath(); c.arc(-5, -12, 13, 0, Math.PI * 2); c.fill(); c.restore();
    cap(c);
  }
  ribbon(c, PEAR, p, 3, INK);
  if (p >= 1) ribbon(c, EDGE, 1, 1.8, '#aeb8c8');
};
const fire = (c, k) => {
  c.save();
  pear(c); c.clip();
  c.fillStyle = rgba(AMBER_RGB, .34 * k); c.fillRect(-16, -22, 32, 34);
  if (k > .6) { c.fillStyle = rgba(AMBER_RGB, .55 * k); c.beginPath(); c.arc(0, -5, 7.5, 0, Math.PI * 2); c.fill(); }
  c.restore();
  c.lineJoin = 'miter';
  c.strokeStyle = hot(k); c.lineWidth = 2.2;
  line(c, [[-3, 10], [-4, -2], [-2, -7], [0, -2], [2, -7], [4, -2], [3, 10]]); c.stroke();
  if (k > .9) { c.strokeStyle = HOT; c.lineWidth = 1; line(c, [[-4, -2], [-2, -7], [0, -2], [2, -7], [4, -2]]); c.stroke(); }
};
const stump = (c) => {
  cap(c);
  c.fillStyle = '#2f3541';
  line(c, [[-6.5, 10], [-8, 4], [-5, 1], [-3.5, 5], [-1, -1], [2, 4], [5, 0], [8, 5], [6.5, 10]], true);
  c.fill();
  c.strokeStyle = INK; c.lineWidth = 2; c.lineJoin = 'miter'; c.stroke();
  c.strokeStyle = '#8b93a1'; c.lineWidth = 1;
  line(c, [[-3, 10], [-3.5, 3], [-1.5, 1]]); c.stroke();
  line(c, [[3, 10], [3.5, 4]]); c.stroke();
};
const picture = (draw) => {
  const made = canvas(40, 48), c = made.getContext('2d');
  c.translate(20, 24);
  c.rotate(-.1);
  draw(c);
  return made;
};

Subjects.bulb = function () {
  [.35, .65, .85].forEach((p, i) => out(`bulb_drawing_${i}`, picture((c) => glass(c, p))));
  out('bulb_glass', picture((c) => glass(c, 1)));
  [.12, .33, .58, .82, 1].forEach((k, i) => out(`bulb_fire_${i}`, picture((c) => fire(c, k))));
  out('bulb_stump', picture(stump));
};
})();
