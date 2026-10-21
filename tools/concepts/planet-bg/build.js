const fs = require('fs'), path = require('path');
const S = __dirname, A = path.join(S, 'assets');
let html = fs.readFileSync(path.join(S, 'src/page.html'), 'utf8');
const assets = {};
for (const f of fs.readdirSync(A)) {
  const uri = `data:image/${f.endsWith('.webp') ? 'webp' : 'png'};base64,` + fs.readFileSync(path.join(A, f)).toString('base64');
  if (f.endsWith('.png')) assets[f.replace('.png', '')] = uri;
  html = html.split(`{{${f}}}`).join(uri);
}
const app = ['core', 'art', 'specs', 'surface', 'under', 'system-ui', 'ui'].map((n) => fs.readFileSync(path.join(S, 'src', n + '.js'), 'utf8')).join('\n');
html = html.replace('/*ASSETS*/', () => JSON.stringify(assets)).replace('/*APP*/', () => app);
fs.writeFileSync(path.join(S, 'planet-bg.html'), html);
// a local wrapper with a doctype for headless checks
fs.writeFileSync(path.join(S, 'check.html'), '<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"></head><body>' + html + '</body></html>');
console.log('built', (html.length / 1024).toFixed(0), 'KB');
