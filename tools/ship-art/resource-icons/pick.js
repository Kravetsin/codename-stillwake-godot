// node tools/ship-art/resource-icons/pick.js [outDir] [itemsDir]: copies the picked candidates (picks.json) from build.js's
// outDir/png into game/art/items/<id>.png.
const fs = require('fs'), path = require('path');
const OUT = process.argv[2] || path.join(__dirname, 'out');
const ITEMS = process.argv[3] || path.join(__dirname, '..', '..', '..', 'game', 'art', 'items');
const picks = JSON.parse(fs.readFileSync(path.join(__dirname, 'picks.json'), 'utf8'));
for (const [id, file] of Object.entries(picks)) fs.copyFileSync(path.join(OUT, 'png', file + '.png'), path.join(ITEMS, id + '.png'));
console.log('picked', Object.keys(picks).length);
