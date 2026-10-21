// Inlines the pictures, the emitters and the designs into page.src.html: site/arc-relic.html
const fs = require('fs'), path = require('path');
const out = path.join(__dirname, 'out'), uri = (f) => 'data:image/png;base64,' + fs.readFileSync(f).toString('base64');
const { DESIGNS } = require('./designs');
let h = fs.readFileSync(path.join(__dirname, 'page.src.html'), 'utf8');
h = h.replace(/\{\{img:([^}]+)\}\}/g, (_, n) => uri(path.join(out, n + '.png')));
const meta = JSON.parse(fs.readFileSync(path.join(out, 'meta.json'), 'utf8')), imgs = {};
for (const f of fs.readdirSync(out)) if (/^(card|spire|home|icon|today)-.*\.png$/.test(f)) imgs[f.slice(0, -4)] = uri(path.join(out, f));
const designs = DESIGNS.map((d) => ({ id: d.id, name: d.name, short: d.short, what: d.what, tamed: d.tamed }));
h = h.replace('{{json:meta}}', JSON.stringify(meta)).replace('{{json:imgs}}', JSON.stringify(imgs)).replace('{{json:designs}}', JSON.stringify(designs));
fs.mkdirSync(path.join(__dirname, 'site'), { recursive: true });
fs.writeFileSync(path.join(__dirname, 'site', 'arc-relic.html'), h);
console.log((h.length / 1e6).toFixed(2), 'MB');
