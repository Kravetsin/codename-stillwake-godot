// node tools/ship-art/walls/pick.js [outDir] [artDir]: copies each material's picked candidate (picks.json) from build.js's
// outDir into game/art: wall_<id>, back_<id> and wall-edge_<id>_1-15.
const fs = require('fs'), path = require('path');
const OUT = process.argv[2] || path.join(__dirname, 'out');
const ART = process.argv[3] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const picks = JSON.parse(fs.readFileSync(path.join(__dirname, 'picks.json'), 'utf8'));
let n = 0;
for (const [id, variant] of Object.entries(picks)) {
  const dir = path.join(OUT, id, variant);
  for (const f of fs.readdirSync(dir)) { fs.copyFileSync(path.join(dir, f), path.join(ART, f)); n++; }
}
console.log('picked', n);
