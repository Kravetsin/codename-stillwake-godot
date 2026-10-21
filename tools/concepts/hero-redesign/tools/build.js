// builds the page: the game's own pictures inlined, the scripts joined
const fs = require('fs'), path = require('path');
const ROOT = path.join(__dirname, '..'), A = path.join(ROOT, '..', '..', '..', 'game', 'art') + '/';
const files = [
  'hero_helmet', 'hero_torso', 'hero_pack', 'hero_upperArm', 'hero_forearm', 'hero_thigh', 'hero_shin', 'hero_jetpack',
  'hero_drill', 'hero_drill_spin1', 'hero_drill_spin2', 'hero_drill_spin3',
  'floor', 'wall_iron', 'back_scrap', 'ladder', 'machine_lamp', 'machine_crate', 'machine_analyzer',
  ...Array.from({ length: 15 }, (_, i) => 'wall-edge_iron_' + (i + 1)),
  'planet/sky', 'planet/far', 'planet/hills', 'planet/top_soil', 'planet/wall_soil', 'planet/back_soil', 'planet/wall_stone', 'planet/back_stone',
  'planet/rock_0', 'planet/bush_0', 'planet/bush_0_glow', 'planet/bellTeal_0', 'planet/bellTeal_0_glow', 'planet/tuft_0',
  'caves/crystalIce_0', 'caves/crystalIce_0_glow', 'caves/crystalIceHang_0', 'caves/crystalIceHang_0_glow', 'caves/glowcapTeal_0', 'caves/glowcapTeal_0_glow',
  'caves/stalactite_0_0', 'caves/stalactite_1_0', 'caves/stalactite_2_0', 'caves/stalactite_3_0',
  'debris/asteroid_02', 'debris/scrap_01', 'debris/hull_01',
];
const art = {};
for (const f of files) art[f.split('/').pop()] = 'data:image/png;base64,' + fs.readFileSync(A + f + '.png').toString('base64');
const strip = s => s.replace(/if \(typeof module[\s\S]*$/, '');
const code = ['art.js', 'fig.js', 'scene.js', 'app.js'].map(f => strip(fs.readFileSync(path.join(ROOT, 'src', f), 'utf8'))).join('\n');
const shot = f => 'data:image/png;base64,' + fs.readFileSync(path.join(ROOT, f)).toString('base64');
let page = fs.readFileSync(path.join(ROOT, 'src', 'page.html'), 'utf8');
page = page.replace('/*ART*/', () => JSON.stringify(art)).replace('/*CODE*/', () => code)
  .replace('/*SHOT_DECK*/', () => shot('shot-deck.png')).replace('/*SHOT_PLANET*/', () => shot('shot-planet.png')).replace('/*CMP_REAL*/', () => shot('cmp-real.png')).replace('/*CMP_MINE*/', () => shot('cmp-mine.png'));
fs.writeFileSync(path.join(ROOT, 'hero-redesign.html'), page);
fs.writeFileSync(path.join(ROOT, 'local.html'), '<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"></head><body>' + page + '</body></html>');
console.log('ok', (page.length / 1024).toFixed(0) + 'KB');
