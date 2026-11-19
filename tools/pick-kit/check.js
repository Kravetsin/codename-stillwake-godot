// The stub check (README.md): runs a built page's script in Node against a stub DOM and a canvas that throws on a
// non-finite number, a bad colour or a source rectangle outside its image, then hands the page's check its tools.
const calls = { n: 0, by: {} };
const bad = m => { throw new Error(m); };
const ctx = () => new Proxy({ canvas: null, globalAlpha: 1 }, {
  get(t, k) {
    if (k in t) return t[k];
    if (k === 'createLinearGradient' || k === 'createRadialGradient') return (...a) => {
      for (const v of a) if (!isFinite(v)) bad('non-finite gradient arg ' + a.join(','));
      if (k === 'createRadialGradient' && (a[2] < 0 || a[5] < 0)) bad('negative gradient radius ' + a.join(','));
      return { addColorStop(u, c) { if (!(u >= 0 && u <= 1)) bad('bad stop ' + u); if (/NaN|undefined|e-/.test(c)) bad('bad colour ' + c); } };
    };
    if (k === 'createPattern') return () => ({ pattern: true });
    if (k === 'measureText') return () => ({ width: 10 });
    if (k === 'createImageData') return (w, h) => ({ width: w, height: h, data: new Uint8ClampedArray(w * h * 4) });
    return (...a) => {
      calls.n++; calls.by[k] = (calls.by[k] || 0) + 1;
      for (const v of a) if (typeof v === 'number' && !isFinite(v)) bad('non-finite arg to ' + String(k) + ': ' + a.join(','));
      if (k === 'arc' && a[2] < 0) bad('negative arc radius');
      if (k === 'ellipse' && (a[2] < 0 || a[3] < 0)) bad('negative ellipse radius');
      if (k === 'drawImage' && a.length === 9 && a[0].width) { const [im, sx, sy, sw, sh] = a; if (sx < -.01 || sy < -.01 || sx + sw > im.width + .01 || sy + sh > im.height + .01) bad('source rect outside ' + [sx, sy, sw, sh, im.width, im.height].join(',')); }
    };
  },
  set(t, k, v) { if (typeof v === 'string' && /NaN|undefined|\de-\d/.test(v)) bad('bad ' + String(k) + ' ' + v); if (typeof v === 'number' && !isFinite(v)) bad('bad ' + String(k)); if (v === undefined || v === null) bad('unset ' + String(k)); t[k] = v; return true; },
});
const el = tag => {
  const e = { tag, style: {}, children: [], hidden: false, textContent: '', value: '', checked: false, width: 300, height: 150, clientWidth: 620,
    classList: { s: new Set(), toggle(c, on) { if (on === undefined) on = !this.s.has(c); on ? this.s.add(c) : this.s.delete(c); return on; }, add(c) { this.s.add(c); } },
    listeners: {}, addEventListener(k, f) { (this.listeners[k] = this.listeners[k] || []).push(f); },
    appendChild(c) { this.children.push(c); return c; }, getContext() { const c = ctx(); c.canvas = e; return c; },
    getBoundingClientRect: () => ({ left: 0, top: 0, width: 620, height: 334.8 }), scrollIntoView() {}, focus() {}, select() {},
    _q: {}, querySelector(s) { return this._q[s] || (this._q[s] = el(s)); },
  };
  let ih = '';
  Object.defineProperty(e, 'innerHTML', { get: () => ih, set(v) { ih = v; } });
  return e;
};

/** Runs the script, then page.check(K); true when the page's check passed and nothing threw. */
function run(script, page) {
  const byId = {};
  let raf = null, errs = 0, kit = null;
  global.document = { getElementById: id => byId[id] || (byId[id] = el(id)), createElement: el };
  global.window = { devicePixelRatio: 2, matchMedia: () => ({ matches: false }), addEventListener() {}, claude: undefined, __pickCheck: k => { kit = k; } };
  global.navigator = {};
  global.Image = class { constructor() { this.complete = true; this.naturalWidth = 64; } set src(v) { this._s = v; } };
  global.requestAnimationFrame = f => { raf = f; };
  global.setTimeout = () => 0;
  const seen = new Set();
  console.error = e => { errs++; const m = String(e && e.stack || e).split('\n').slice(0, 3).join(' | '); if (!seen.has(m)) { seen.add(m); console.log('ERR', m); } };
  new Function(script)();
  if (!kit) bad('the page never started');
  const plan = {}, clicks = [], lastClick = kit.tiles.map(() => -10), why = [];
  const toC = p => ({ clientX: p[0] * .62, clientY: p[1] * .62, preventDefault() {} });
  const K = {
    ...kit, byId, calls, clicks,
    /** Clicks the i-th button of a toggle bar. */
    seg: (id, i) => byId[id].children.filter(c => c.listeners.click)[i].listeners.click[0](),
    /** Clicks a selector inside the i-th candidate's tile. */
    click: (i, sel) => byId.cands.children[i].querySelector(sel).listeners.click[0](),
    /** Moves the real pointer to p (logical px) on every tile, or takes it off them with null. */
    ptrAll: p => kit.tiles.forEach(t => { if (p) t.cv.listeners.pointermove[0](toC(p)); else t.cv.listeners.pointerleave[0](); }),
    at: (f, fn) => { (plan[f] = plan[f] || []).push(fn); },
    /** Forced open-close turns from frame f0 at every moment of an opening and a closing, then the schedule again. */
    forceSeq: f0 => {
      for (const [d, v] of [[0, false], [40, true], [43, false], [46, true], [52, false], [55, true], [62, false], [64, true], [68, false], [72, true], [100, false], [104, true], [150, false], [153, true], [230, null]])
        K.at(f0 + d, () => { kit.OPT.force = v; });
    },
    fail: m => { if (why.length < 10) why.push(m); },
    /** Runs frames 0..end at 60 fps, the plan's clicks before each, each(tile, frame) after it. */
    run(end, each) {
      let now = 0;
      for (let i = 0; i < end; i++) {
        for (const fn of plan[i] || []) fn();
        now += 1000 / 60; const f = raf; raf = null; f(now);
        kit.tiles.forEach((t, ti) => {
          if (t.clickAt !== lastClick[ti]) { lastClick[ti] = t.clickAt; if (t.clickAt > 0) clicks.push(t.clickAfter); }
          each(t, i);
        });
      }
    },
  };
  const ok = page.check(K);
  console.log('errors', errs, 'canvas calls', calls.n, 'drawImage', calls.by.drawImage || 0, '| ghost clicks', clicks.length, clicks.length ? `${Math.min(...clicks).toFixed(3)}..${Math.max(...clicks).toFixed(3)} s after Tab` : '');
  console.log('status:', byId.status.textContent, '| chosen', kit.tiles.map(t => t.sec.classList.s.has('chosen') ? 1 : 0).join(''));
  for (const m of why) console.log('  ', m);
  return !!ok && errs === 0 && why.length === 0;
}
module.exports = { run };
