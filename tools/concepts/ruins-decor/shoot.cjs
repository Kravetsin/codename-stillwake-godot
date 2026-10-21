// node shoot.cjs [width]: screenshots of the built page in the installed Chrome: full page, the scenes over a few frames,
// some catalogue cards, at the given width (1280 by default)
const path = require('path');
const { chromium } = require('C:/Users/Kravets/Documents/GitHub/codename-stillwake/node_modules/.pnpm/playwright-core@1.64.0/node_modules/playwright-core');
const W = +(process.argv[2] || 1280), OUT = path.join(__dirname, 'shots'); require('fs').mkdirSync(OUT, { recursive: true });
(async () => {
  const b = await chromium.launch({ channel: 'chrome' });
  const pg = await b.newPage({ viewport: { width: W, height: 900 }, deviceScaleFactor: 1 });
  const errs = []; pg.on('pageerror', (e) => errs.push(e.message)); pg.on('console', (m) => { if (m.type() === 'error') errs.push(m.text()); });
  await pg.goto('file:///' + path.join(__dirname, 'site', 'ruins-homes.html').replace(/\\/g, '/'));
  await pg.waitForTimeout(800);
  const sw = await pg.evaluate(() => [document.documentElement.scrollWidth, window.innerWidth]);
  console.log('scrollWidth', sw, 'errors', errs);
  await pg.screenshot({ path: path.join(OUT, `full-${W}.png`), fullPage: true });
  if (W >= 1000) {
    const scenes = await pg.$$('.scene .frame');
    for (let i = 0; i < scenes.length; i++) {
      await scenes[i].scrollIntoViewIfNeeded(); await pg.waitForTimeout(300);
      for (let f = 0; f < 3; f++) { await pg.waitForTimeout(170); await scenes[i].screenshot({ path: path.join(OUT, `scene${i}-f${f}.png`) }); }
    }
    for (const id of ['ceilLamp', 'hearth', 'arcDevice', 'holoPortrait', 'cradle', 'brokenLamp', 'musicBox', 'plantAlive']) {
      const c = await pg.$('#card-' + id); await c.scrollIntoViewIfNeeded(); await pg.waitForTimeout(250);
      for (let f = 0; f < 2; f++) { await pg.waitForTimeout(200); await c.screenshot({ path: path.join(OUT, `card-${id}-f${f}.png`) }); }
    }
  }
  await b.close();
})();
