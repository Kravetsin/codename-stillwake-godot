// node export.cjs <outDir>: draws the concept's tablet frame (variant A) in headless Chrome and writes its slices.
const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
const D = __dirname, outDir = process.argv[2] || path.join(D, 'out');
const js = '(async function () { const OUT = {}; try {\n' + fs.readFileSync(path.join(D, 'helpers-head.txt'), 'utf8') +
  fs.readFileSync(path.join(D, 'helpers-art.txt'), 'utf8') + fs.readFileSync(path.join(D, 'tail.js'), 'utf8');
fs.writeFileSync(path.join(D, 'export.html'), `<!doctype html><meta charset="utf-8"><body><pre id="out"></pre><script>${js}</script>`);
const chrome = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const dom = execFileSync(chrome, ['--headless=new', '--disable-gpu', '--no-first-run', '--user-data-dir=' + path.join(require('os').tmpdir(), 'stillwake-tablet-export'),
  '--virtual-time-budget=20000', '--dump-dom', 'file:///' + path.join(D, 'export.html').split(path.sep).join('/')], { maxBuffer: 1 << 28, encoding: 'utf8' });
const txt = dom.match(/<pre id="out">([\s\S]*?)<\/pre>/)[1].replace(/&quot;/g, '"').replace(/&amp;/g, '&').replace(/&lt;/g, '<').replace(/&gt;/g, '>');
const out = JSON.parse(txt);
if (out.error) { console.error(out.error); process.exit(1); }
fs.mkdirSync(outDir, { recursive: true });
for (const [name, val] of Object.entries(out)) { fs.writeFileSync(path.join(outDir, name), Buffer.from(val.split(',')[1], 'base64')); console.log(name); }
