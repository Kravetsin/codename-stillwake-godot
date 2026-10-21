// node tools/planet-art/icons/pick.js [outDir]: copies the candidates picked on the planet icons page (picks.json) from
// build.js's outDir into game/art/items/<id>.png.
const fs = require('fs'), path = require('path');
const OUT = process.argv[2] || path.join(__dirname, 'out');
const ITEMS = path.join(__dirname, '..', '..', '..', 'game', 'art', 'items');
const picks = JSON.parse(fs.readFileSync(path.join(__dirname, 'picks.json'), 'utf8'));
for (const [id, key] of Object.entries(picks)) fs.copyFileSync(path.join(OUT, id, `${id}_${key}.png`), path.join(ITEMS, `${id}.png`));
console.log('picked', Object.keys(picks).length);
