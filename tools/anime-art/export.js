// Draws the subjects in a headless browser and writes their pictures into game/art/hd. node tools/anime-art/export.js [subject...]
const { spawn, spawnSync } = require('child_process');
const fs = require('fs'), os = require('os'), path = require('path'), zlib = require('zlib');
const here = __dirname, art = path.join(here, '..', '..', 'game', 'art'), hd = path.join(art, 'hd');
const browser = [process.env.ANIME_BROWSER, 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Google/Chrome/Application/chrome.exe',
  '/usr/bin/chromium', '/usr/bin/google-chrome'].find((p) => p && fs.existsSync(p));
if (!browser) throw new Error('No Edge or Chrome found: set ANIME_BROWSER.');

const SUBJECTS = fs.readdirSync(path.join(here, 'subjects')).filter((f) => f.endsWith('.js')).map((f) => f.slice(0, -3));
const asked = process.argv.slice(2);
for (const s of asked) if (!SUBJECTS.includes(s)) throw new Error(`No subject ${s}: ${SUBJECTS.join(', ')}.`);
const started = Date.now();

/** The box a logical PNG uses, its size, the rows and columns it draws on and its solid pixels: a cave picture's base must stay on its row, a tree's pods where the game lights from. RGBA8 or palette, as the art is. */
function usedBox(file) {
  const b = fs.readFileSync(file);
  let p = 8, w, h, ct, trns; const idat = [];
  while (p < b.length) {
    const l = b.readUInt32BE(p), t = b.toString('latin1', p + 4, p + 8), d = b.subarray(p + 8, p + 8 + l); p += 12 + l;
    if (t === 'IHDR') { w = d.readUInt32BE(0); h = d.readUInt32BE(4); ct = d[9]; } else if (t === 'tRNS') trns = d; else if (t === 'IDAT') idat.push(d);
  }
  const raw = zlib.inflateSync(Buffer.concat(idat)), ch = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[ct], st = w * ch;
  let prev = Buffer.alloc(st), q = 0, x0 = w, y0 = h, x1 = -1, y1 = -1;
  const pts = [];
  for (let y = 0; y < h; y++) {
    const f = raw[q++], cur = Buffer.from(raw.subarray(q, q + st)); q += st;
    for (let i = 0; i < st; i++) {
      const a = i >= ch ? cur[i - ch] : 0, u = prev[i], c = i >= ch ? prev[i - ch] : 0; let v = 0;
      if (f === 1) v = a; else if (f === 2) v = u; else if (f === 3) v = (a + u) >> 1;
      else if (f === 4) { const pp = a + u - c, pa = Math.abs(pp - a), pb = Math.abs(pp - u), pc = Math.abs(pp - c); v = pa <= pb && pa <= pc ? a : pb <= pc ? u : c; }
      cur[i] = (cur[i] + v) & 255;
    }
    for (let x = 0; x < w; x++) {
      const al = ct === 6 ? cur[x * 4 + 3] : ct === 4 ? cur[x * 2 + 1] : ct === 3 && trns && cur[x] < trns.length ? trns[cur[x]] : 255;
      if (al > 0) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); if (al >= 153) pts.push(x, y); }
    }
    prev = cur;
  }
  return { w, h, x0, y0, x1, y1, pts };
}
const logical = {};
for (const f of fs.readdirSync(path.join(art, 'caves'))) if (/^crystalIce(Hang)?_\d\.png$/.test(f)) logical[f.slice(0, -4)] = usedBox(path.join(art, 'caves', f));
for (const f of fs.readdirSync(path.join(art, 'planet'))) if (/^acacia_\d_glow\.png$/.test(f)) logical[f.slice(0, -4)] = usedBox(path.join(art, 'planet', f));

const url = (f) => 'file:///' + path.join(here, f).replace(/\\/g, '/');
const page = `<!doctype html><meta charset="utf-8"><pre id="out"></pre>
<script>window.LOGICAL=${JSON.stringify(logical)};window.Made=[];window.out=(name,c)=>Made.push([name,c]);window.Texts=[];window.writeText=(p,t)=>Texts.push([p,t]);</script>
<script src="${url('engine.js')}"></script>
<script src="${url('wash.js')}"></script>
${SUBJECTS.map((s) => `<script src="${url('subjects/' + s + '.js')}"></script>`).join('\n')}
<script>
try { for (const s of ${JSON.stringify(asked.length ? asked : SUBJECTS)}) Subjects[s](); document.getElementById('out').textContent = String(Made.length); }
catch (e) { document.getElementById('out').textContent = 'ERROR ' + e.stack; }
</script>`;
const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'anime-art-'));
fs.writeFileSync(path.join(dir, 'page.html'), page);

/** The page's pictures, by the browser's debugging port: a browser's own --dump-dom prints nothing on Windows. */
async function draw() {
  const port = 9300 + Math.floor(Math.random() * 500);
  const child = spawn(browser, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', '--allow-file-access-from-files', `--user-data-dir=${path.join(dir, 'profile')}`,
    `--remote-debugging-port=${port}`, 'file:///' + path.join(dir, 'page.html').replace(/\\/g, '/')], { stdio: 'ignore' });
  try {
    let target = null;
    for (let i = 0; i < 100 && !target; i++) {
      await new Promise((r) => setTimeout(r, 200));
      try { target = (await (await fetch(`http://127.0.0.1:${port}/json`)).json()).find((t) => t.type === 'page' && t.url.includes('page.html')); } catch { /* not up yet */ }
    }
    if (!target) throw new Error('The browser never opened the page.');
    const ws = new WebSocket(target.webSocketDebuggerUrl);
    await new Promise((r, e) => { ws.onopen = r; ws.onerror = e; });
    let id = 0;
    const ask = (expression) => new Promise((resolve, reject) => {
      const me = ++id;
      ws.onmessage = (m) => { const r = JSON.parse(m.data); if (r.id === me) (r.error ? reject(new Error(r.error.message)) : resolve(r.result.result.value)); };
      ws.send(JSON.stringify({ id: me, method: 'Runtime.evaluate', params: { expression, awaitPromise: true, returnByValue: true } }));
    });
    const told = await ask('new Promise(r=>{const t=()=>{const x=document.getElementById("out").textContent;x?r(x):setTimeout(t,100)};t()})');
    if (told.startsWith('ERROR')) throw new Error(told);
    const n = Number(told);
    for (let i = 0; i < n; i++) {
      const [name, data] = await ask(`[Made[${i}][0], Made[${i}][1].toDataURL('image/png')]`);
      const file = path.join(hd, name + '.png');
      fs.mkdirSync(path.dirname(file), { recursive: true });
      fs.writeFileSync(file, Buffer.from(data.split(',')[1], 'base64'));
    }
    for (const [p, t] of await ask('Texts')) fs.writeFileSync(path.join(hd, p), t);
    ws.close();
    return n;
  } finally {
    // On Windows the browser's children outlive a plain kill.
    if (process.platform === 'win32') spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore' }); else child.kill();
  }
}

draw().then((n) => {
  try { fs.rmSync(dir, { recursive: true, force: true, maxRetries: 5, retryDelay: 300 }); } catch { /* the browser's last lock on its profile, in a temp folder */ }
  console.log(`${n} pictures in ${((Date.now() - started) / 1000).toFixed(1)} s`);
}).catch((e) => { console.error(e); process.exit(1); });
