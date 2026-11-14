// Draws the subjects in a headless browser and writes their pictures into game/art/hd. node tools/anime-art/export.js [subject...]
const { spawn, spawnSync } = require('child_process');
const fs = require('fs'), os = require('os'), path = require('path');
const here = __dirname, hd = path.join(here, '..', '..', 'game', 'art', 'hd');
const browser = [process.env.ANIME_BROWSER, 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Google/Chrome/Application/chrome.exe',
  '/usr/bin/chromium', '/usr/bin/google-chrome'].find((p) => p && fs.existsSync(p));
if (!browser) throw new Error('No Edge or Chrome found: set ANIME_BROWSER.');

const SUBJECTS = fs.readdirSync(path.join(here, 'subjects')).filter((f) => f.endsWith('.js')).map((f) => f.slice(0, -3));
const asked = process.argv.slice(2);
for (const s of asked) if (!SUBJECTS.includes(s)) throw new Error(`No subject ${s}: ${SUBJECTS.join(', ')}.`);
const started = Date.now();

/** The boxes and solid pixels of the old pixel pictures a few subjects still place by (logical.json, read once from them): a cave crystal's base stays on its row, a tree's pods where the game lights from. */
const logical = JSON.parse(fs.readFileSync(path.join(here, 'logical.json'), 'utf8'));

const wreckSource = fs.readFileSync(path.join(here, '..', '..', 'sim', 'Content', 'Wreck.cs'), 'utf8');
const mapOf = (name, next) => wreckSource.slice(wreckSource.indexOf(`${name} = Placed(`), wreckSource.indexOf(next)).split('\n').map((l) => /^\s*"(.*)",\s*$/.exec(l)).filter(Boolean).map((m) => m[1]);
const wreck = { rows: mapOf('Rows', 'Materials = Placed('), dress: mapOf('Dress', 'The pipe inside the pod') };

/** Every traced subject's shapes (trace.js) with its palette's materials: drawn from these, the reference is never read here. */
const traced = Object.fromEntries(fs.readdirSync(path.join(here, 'traced')).filter((f) => f.endsWith('.json')).map((f) => {
  const name = f.slice(0, -5), palette = JSON.parse(fs.readFileSync(path.join(here, 'refs', name + '.palette.json'), 'utf8'));
  return [name, { ...JSON.parse(fs.readFileSync(path.join(here, 'traced', f), 'utf8')), materials: palette.materials }];
}));

const url = (f) => 'file:///' + path.join(here, f).replace(/\\/g, '/');
const page = `<!doctype html><meta charset="utf-8"><pre id="out"></pre>
<script>window.LOGICAL=${JSON.stringify(logical)};window.WRECK=${JSON.stringify(wreck)};window.TRACED=${JSON.stringify(traced)};window.Made=[];window.out=(name,c)=>Made.push([name,c]);window.Texts=[];window.writeText=(p,t)=>Texts.push([p,t]);</script>
<script src="${url('engine.js')}"></script>
<script src="${url('wash.js')}"></script>
<script src="${url('traced.js')}"></script>
${SUBJECTS.map((s) => `<script src="${url('subjects/' + s + '.js')}"></script>`).join('\n')}
<script>
try { for (const s of ${JSON.stringify(asked.length ? asked : SUBJECTS)}) Subjects[s](); document.getElementById('out').textContent = String(Made.length); }
catch (e) { document.getElementById('out').textContent = 'ERROR ' + e.stack; }
</script>`;
const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'anime-art-'));
fs.writeFileSync(path.join(dir, 'page.html'), page);

/** The page's pictures, by the browser's debugging port: a browser's own --dump-dom prints nothing on Windows. */
async function draw() {
  const profile = path.join(dir, 'profile');
  // Port 0, the browser's own choice: a port of ours could be another export's, and its pictures land here.
  const child = spawn(browser, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', '--allow-file-access-from-files', `--user-data-dir=${profile}`,
    '--remote-debugging-port=0', 'file:///' + path.join(dir, 'page.html').replace(/\\/g, '/')], { stdio: 'ignore', windowsHide: true });
  try {
    let target = null;
    for (let i = 0; i < 100 && !target; i++) {
      await new Promise((r) => setTimeout(r, 200));
      try {
        const port = fs.readFileSync(path.join(profile, 'DevToolsActivePort'), 'utf8').split('\n')[0].trim();
        target = (await (await fetch(`http://127.0.0.1:${port}/json`)).json()).find((t) => t.type === 'page' && t.url.includes('page.html'));
      } catch { /* not up yet */ }
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
    // On Windows the browser's processes outlive a kill of the one we started, even of its tree: find them all by the profile.
    if (process.platform === 'win32') spawnSync('powershell', ['-NoProfile', '-Command',
      `Get-CimInstance Win32_Process -Filter "Name='${path.basename(browser)}'" | Where-Object { $_.CommandLine -like '*${dir}*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`],
    { stdio: 'ignore', windowsHide: true }); else child.kill();
  }
}

draw().then((n) => {
  try { fs.rmSync(dir, { recursive: true, force: true, maxRetries: 5, retryDelay: 300 }); } catch { /* the browser's last lock on its profile, in a temp folder */ }
  console.log(`${n} pictures in ${((Date.now() - started) / 1000).toFixed(1)} s`);
}).catch((e) => { console.error(e); process.exit(1); });
