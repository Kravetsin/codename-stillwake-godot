// node tools/ship-art/wrecks/install.js [artDir, default game/art]: copies the wreck kinds from wrecks/out and their icons
// from wrecks/icons under the names the game uses. The hull section's wreck is the debris' own section_01, read, not drawn.
const fs = require('fs'), path = require('path');
const ART = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const OUT = process.env.WRECKS_OUT || path.join(__dirname, 'out'), ICONS = process.env.WRECKS_ICONS || path.join(__dirname, 'icons');
const DEBRIS = { cargoContainer: 'container_01', machineBlock: 'machine_01', cableRack: 'rack_01', shieldSlab: 'slab_01', tankCluster: 'tanks_01' };
for (const d of ['debris', 'items']) fs.mkdirSync(path.join(ART, d), { recursive: true });
for (const [id, name] of Object.entries(DEBRIS)) {
  fs.copyFileSync(path.join(OUT, `wreck_${id}.png`), path.join(ART, 'debris', `${name}.png`));
  fs.copyFileSync(path.join(ICONS, `${id}.png`), path.join(ART, 'items', `${id}.png`));
}
fs.copyFileSync(path.join(OUT, 'item_circuit.png'), path.join(ART, 'items', 'circuit.png'));
console.log('installed', Object.keys(DEBRIS).length * 2 + 1, 'files');
