// The walls' candidates, by material: the armour (scrap, iron) redrawn and the sealing walls (glass, mica, hide) new. A
// candidate paints its wall sheet (128x64, laid over the world a cell at a time) and its back wall, and names the look of
// its edges; `current` is the art as it is in game/art, to keep.
const fs = require('fs'), path = require('path');
const { read } = require('../part-icons/png.js');
const { rng, recede, edge } = require('./lib.js');
const seal = require('./seal.js'), armour = require('./armour.js');
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art');

/** An edge's look from five colours (outline, accent, lit rim, shadowed rim, unused) and a trim (lib.js TRIMS). */
const look = ([outline, accent, lit, shade], trim) => ({ outline, accent, lit, shade, trim });
const current = { id: 'current', name: 'Как сейчас', what: 'оставить нынешний рисунок', current: true };

const MATERIALS = [
  {
    id: 'scrap', name: 'Лом', what: 'броня: залатанный лом, щели на стыках (течёт)',
    variants: [
      current,
      { id: 'patched', name: 'Заплаты', what: 'листы разных тонов рядами, клёпки, щели на стыках, заплаты поверх дыр, ржавые потёки', paint: armour.patched, edge: look([0x121418, 0x8a5a3a, 0x8a8f9a, 0x3a3e47], 'notch') },
      { id: 'sheets', name: 'Листы', what: 'ровные и гофрированные листы, болты, не сходятся: щели по краю, ржавчина', paint: armour.sheets, edge: look([0x121418, 0x8a5a3a, 0x8e9198, 0x3c3f46], 'notch') },
    ],
  },
  {
    id: 'iron', name: 'Железо', what: 'броня: клёпаные плиты (течёт меньше лома)', backs: true,
    variants: [
      current,
      { id: 'riveted', name: 'Клёпаные плиты', what: 'плиты 4×2 вперевязку, фаска, по два ряда клёпок на торцах; задник: панель и решётка', paint: armour.riveted, back: 'riveted', edge: look([0x2b2e34, 0xd3d8e0, 0xb4bcca, 0x595f6b], 'rivet') },
      { id: 'heavy', name: 'Тяжёлые плиты', what: 'квадратные плиты 4×4 потемнее, двойной шов, кольцо клёпок, потёртости; задник: утопленная панель и сетка', paint: armour.heavy, back: 'heavy', edge: look([0x24272d, 0xc4cad4, 0xa7afbe, 0x4c5260], 'rivet') },
    ],
  },
  {
    id: 'glass', name: 'Стекло', what: 'герметик: матовое, из гравия на корабле; непрозрачное',
    variants: [
      { id: 'panes', name: 'Матовые панели', what: 'панели 4×2 клетки в тонкой стальной раме, болт на стыках; полупрозрачные: сквозь них звёзды', paint: seal.panes, edge: look([0x2b3a40, 0xd3e6e6, 0xb2cdcf, 0x5e7c85]) },
      { id: 'blocks', name: 'Стеклоблоки', what: 'блок на клетку, на светлом растворе; подушка и вдавленное кольцо', paint: seal.blocks, edge: look([0x2e3b40, 0xd5e7e6, 0xb4cfd0, 0x5c7880]) },
      { id: 'opal', name: 'Молочное стекло', what: 'опаловая плитка 2×2 клетки вперевязку, сиреневато-белая, облачко в каждой', paint: seal.opal, edge: look([0x342f3c, 0xebe6ef, 0xcbc3d6, 0x6c6479]) },
    ],
  },
  {
    id: 'mica', name: 'Слюда', what: 'герметик: добыта на планетах; слоистая, серебро → янтарь',
    variants: [
      { id: 'flakes', name: 'Чешуя', what: 'тонкие пластинки внахлёст, серебро с шампанским и янтарём; без рамы', paint: seal.flakes, edge: look([0x2e271c, 0xf3ecd6, 0xc9b88c, 0x6e5c40], 'glint') },
      { id: 'framed', name: 'Янтарь в раме', what: 'янтарные листы 4×2 в клёпаной железной раме, как окошко печки, но глухое; сколы-ступеньки', paint: seal.framed, edge: look([0x26241f, 0xd3cec2, 0x908a7f, 0x45413b], 'rivet') },
      { id: 'laminate', name: 'Серебряный пласт', what: 'серебристые слои на ребре, листы по 4 клетки со сдвигом', paint: seal.laminate, edge: look([0x2a2923, 0xf0ecdc, 0xc2bda9, 0x6a675b], 'glint') },
    ],
  },
  {
    id: 'hide', name: 'Шкура', what: 'герметик: с планетных зверей, швы залиты жиром; ранняя, лоскутная',
    variants: [
      { id: 'patchwork', name: 'Лоскуты', what: 'куски кожи разных тонов, швы с жиром и стежками', paint: seal.patchwork, edge: look([0x2a1c14, 0xddcc95, 0xa47a50, 0x5a3a26], 'stitch') },
      { id: 'drum', name: 'На шестах', what: 'бледная сыромять натянута на раму из шестов 4×4, шнуровка, перевязь на стыках', paint: seal.drum, edge: look([0x261c15, 0xd6c48e, 0x7a624e, 0x3e2f24], 'stitch') },
      { id: 'beast', name: 'Зверь', what: 'оливково-серая пупырчатая шкура полосами, швы жиром', paint: seal.beast, edge: look([0x23251d, 0xddcc95, 0x8f9480, 0x45493b], 'stitch') },
    ],
  },
];

const img = (s) => ({ w: s.w, h: s.h, data: s.rgba() });
const art = (name) => read(path.join(ART, name + '.png'));
const edgeName = (id, open) => `wall-edge_${id}_${open}`;

/**
 * A candidate's pictures as { files: name → { w, h, data } } at the names the tileset loads, and the room's view of them:
 * the wall, the back wall (a sheet) or iron's back tiles, the edges by open sides (index 1..15).
 */
function paint(material, variant) {
  const id = material.id, files = {};
  if (variant.current) {
    for (const f of fs.readdirSync(ART)) {
      const m = f.match(/^(wall_|wall-edge_|back_)(.+)\.png$/);
      if (m && (m[2] === id || m[2].startsWith(id + '_'))) files[f.slice(0, -4)] = art(f.slice(0, -4));
    }
  } else {
    const seed = [...(id + variant.id)].reduce((a, ch) => a * 31 + ch.charCodeAt(0), 7);
    files[`wall_${id}`] = img(variant.paint(rng(seed), false));
    if (variant.back) {
      const { plain, grille } = armour.backs(variant.back);
      files[`back_${id}_plain`] = img(plain);
      files[`back_${id}_grille`] = img(grille);
    } else files[`back_${id}`] = img(recede(variant.paint(rng(seed + 1), true), id === 'scrap' ? 50 : 64));
    for (let open = 1; open < 16; open++) files[edgeName(id, open)] = edge(open, variant.edge);
  }
  return {
    files,
    wall: files[`wall_${id}`],
    back: files[`back_${id}`] || null,
    tiles: files[`back_${id}_plain`] ? { plain: files[`back_${id}_plain`], grille: files[`back_${id}_grille`] } : null,
    edges: [null, ...Array.from({ length: 15 }, (_, k) => files[edgeName(id, k + 1)])],
  };
}

module.exports = { MATERIALS, paint };
