// node tools/ship-art/economy/pick.js [outDir] [artDir]: copies the picked candidates (picks.json) from build.js's outDir
// into game/art: each machine's picture and its work pictures, each item's icon into items/<id>.png.
const fs = require('fs'), path = require('path');
const OUT = process.argv[2] || path.join(__dirname, 'out');
const ART = process.argv[3] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const picks = JSON.parse(fs.readFileSync(path.join(__dirname, 'picks.json'), 'utf8'));
let n = 0;
for (const [kind, id] of Object.entries(picks.machines)) {
  const dir = path.join(OUT, kind, id);
  for (const f of fs.readdirSync(dir)) { fs.copyFileSync(path.join(dir, f), path.join(ART, f)); n++; }
}
for (const [id, k] of Object.entries(picks.items)) { fs.copyFileSync(path.join(OUT, 'items', `${id}-${k}.png`), path.join(ART, 'items', id + '.png')); n++; }
console.log('picked', n);
