// Inlines the pictures and the emitters into page.src.html: site/ruins-homes.html
const fs = require('fs'), path = require('path');
const out = path.join(__dirname, 'out'), uri = (f) => 'data:image/png;base64,' + fs.readFileSync(f).toString('base64');
let h = fs.readFileSync(path.join(__dirname, 'page.src.html'), 'utf8');
h = h.replace(/\{\{img:([^}]+)\}\}/g, (_, n) => uri(path.join(out, n + '.png')));
const meta = JSON.parse(fs.readFileSync(path.join(out, 'cards.json'), 'utf8'));
for (const id of Object.keys(meta)) meta[id].src = uri(path.join(out, 'cards', id + '.png'));
const scenes = JSON.parse(fs.readFileSync(path.join(out, 'scenes.json'), 'utf8'));
for (const id of Object.keys(scenes)) scenes[id].src = uri(path.join(out, 'scene-' + id + '.png'));
h = h.replace('{{json:cards}}', JSON.stringify(meta)).replace('{{json:scenes}}', JSON.stringify(scenes));
fs.mkdirSync(path.join(__dirname, 'site'), { recursive: true });
fs.writeFileSync(path.join(__dirname, 'site', 'ruins-homes.html'), h);
console.log((h.length / 1e6).toFixed(2), 'MB');
