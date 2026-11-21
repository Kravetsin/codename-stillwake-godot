// The drone's hologram in the planner sheet's place: an art-pick page on tools/pick-kit (its README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-holo.js <out-dir> [--break still|open|up]
const YN = [['есть', true], ['нет', false]];
module.exports = {
  title: 'Голограмма дрона',
  heading: 'Голограмма дрона на месте листа',
  lead: `
<p class="lead">Окно (здесь — сумка) открывается сразу: без своей анимации и без ничего между ним и миром. А в мире, пока окно открыто, дрон проецирует перед героем маленькую схематичную голограмму — на месте и в наклоне листа-планшета, который герой сейчас держит при стройке: около 18×11 px мира у передней руки, повёрнута к глазам. Внутри — только сетка тусклых квадратиков, ничего настоящего. Её видит каждый игрок: в кооперативе она говорит друзьям «он сейчас в окне»; вид один для всех окон дрона и для стройки — она заменяет лист. Голова смотрит на указатель, но не выше 0,6 рад и не ниже 0,5 рад, как в игре; голограмма и рука поворачиваются за взглядом, как лист. Появляется мерцанием за <b>0,1 с</b>, гаснет за <b>0,08 с</b>, дальше неподвижна — только лёгкое мерцание и поворот за взглядом; бежит и прыгает вместе с героем.</p>
<p class="lead">Масштаб игры: клетка 8 px мира, вид ×4, герой ~42 px мира (~170 px). Свет — от жестовой голограммы дрона: бледно-голубой #a8eeff, непрозрачность 0,85, сложением, каждая вторая строка экрана темнее на 35 %, мерцание 0,18. Слева стоит друг со своим дроном; его окно открывается по тому же расписанию на секунду позже — так голограмма читается издали.</p>
<p class="miss">Не показано: звук; отрисовка Godot (строки шейдера, свет и тени выйдут чуть иначе); содержимое других окон — меню стройки, Справочник, доска, рынок — у них та же голограмма; сама стройка. Герой — упрощённая замена фигуры игры, с теми же суставами и хватом листа. Наведи указатель на картинку: голова повернётся к нему; щелчок по ячейкам берёт и кладёт стопку.</p>`,
  toggles: [
    { id: 'winMode', bar: 1, label: 'Окно:', key: 'hideWindow', opts: [['показано', false], ['скрыто', true]] },
    { id: 'armsMode', label: 'Руки:', short: 'руки', key: 'arms', saved: true, opts: [['держат как лист', 'hold'], ['свободны', 'free']] },
    { id: 'followMode', label: 'Голограмма:', short: 'голограмма', key: 'follow', saved: true, opts: [['за взглядом', 'look'], ['неподвижно чуть вниз', 'fixed']] },
    { id: 'coneMode', label: 'Конус от линзы:', short: 'конус', key: 'cone', saved: true, opts: YN },
    { id: 'lightMode', label: 'Свет на лицо:', short: 'свет на лицо', key: 'light', saved: true, opts: YN },
    { id: 'flickMode', label: 'Мерцание:', short: 'мерцание', key: 'flicker', saved: true, opts: YN },
  ],
  pick: { doc: 'picks/droneHolo', subject: 'Голограмма дрона', what: ['голограмму', 'голограммой'] },

  page() {
    // DroneHolo / DroneSign: on in 0.1 s, off in 0.08 s, alpha .85, every other screen row 35 % dimmer, flicker .18, the cone .45 → .12
    const OPEN_S = .1, CLOSE_S = .08, ALPHA = .85, FLICK = .18, SCAN = .35, CONE = [.45, .12];
    const FIXED_LOOK = .15;     // «неподвижно чуть вниз»: the sheet's place for a head looking a little down
    const SH_RECT = [[0, -5], [18, -5], [18, 6], [0, 6]];   // the sheet's picture, 18×11 world px about its grip (0, 5)
    const FRIEND_X = 300, FRIEND_LAG = 1, HB = [360, 300];
    const openShape = u => u >= 1 ? 1 : u < .25 ? .85 : u < .45 ? .15 : u < .7 ? 1 : u < .85 ? .4 : 1;   // the flicker-on
    const noise = (t, s) => c01(.5 + .3 * Math.sin(t * 31.7 + s) + .22 * Math.sin(t * 13.1 + s * 2.3) + .18 * Math.sin(t * 77.3 + s * .7));
    // where the hologram sits: the sheet's grip at PlanHand from the shoulder, turned SheetTurn; squashed flat as it goes out
    function holoGeom(h) {
      const P = h.P, hs = h.hs, hand = planHand(h.hl), O = [P.sh[0] + hand[0], P.sh[1] + hand[1]], turn = sheetTurn(h.hl);
      const sq = hs.target ? 1 : .2 + .8 * hs.hp;
      const toH = (x, y) => { const q = rot(x, (y - .5) * sq + .5, turn); return [O[0] + q[0], O[1] + q[1]]; };
      return { O, turn, sq, toH, eye: P.eye, c: toH(9, .5), scr: p => toS(h, p) };
    }
    const sheetOutline = G => SH_RECT.map(p => G.toH(p[0], p[1]));
    function squares(g, x0, y0, nx, ny, step, s, sy = step) { for (let j = 0; j < ny; j++) for (let i = 0; i < nx; i++) g.fillRect(x0 + i * step, y0 + j * sy, s, s); }
    const TILES = (() => { const r = rng(17), order = [0, 4, 2, 5, 1, 3], out = []; for (let j = 0; j < 2; j++) for (let i = 0; i < 3; i++) out.push({ x: .35 + i * 5.95 + (r() - .5) * .6, y: -4.75 + j * 5.65 + (r() - .5) * .6, a: (r() - .5) * .09, at: order[j * 3 + i] * .12 }); return out; })();
    const FAN_C = [[.6, -4.4], [17.4, -4.4], [17.4, 5.4], [.6, 5.4]];
    function arcGeo(G) { const E = G.eye, C = G.c, R = Math.hypot(C[0] - E[0], C[1] - E[1]); return { E, R, th: Math.atan2(C[1] - E[1], C[0] - E[0]), half: 8.5 / R, t2: 2.8 * G.sq }; }

    // ---------- the candidates: each in the sheet's place and size; light(g, G, K) draws additively in world px ----------
    const TIME = 'Появляется мерцанием за 0,1 с · гаснет за 0,08 с, сплющиваясь · дальше неподвижна: мерцание 0,18 и поворот за взглядом.';
    const cands = [
      {
        id: 'sheet', name: 'Лист', time: TIME, outline: sheetOutline,
        idea: 'Сам лист-планшет, ставший светом: тот же прямоугольник 18×11 в руке, светлая рамка, экранная полоса вверху слева с сеткой квадратиков, огонёк-метка справа и две строки ниже. Ближе всего к тому, что есть: игрок узнаёт планшет.',
        light(g, G, K) {
          const A = K.A; K.sheet();
          rr(g, 0, -5, 18, 11, 1.1); g.fillStyle = css(SIGN, .09 * A); g.fill(); g.strokeStyle = css(SIGN, .8 * A); g.lineWidth = .3; g.stroke();
          rr(g, .9, -4.1, 11, 3.9, .4); g.fillStyle = css(SIGN, .14 * A); g.fill(); g.strokeStyle = css(SIGN, .45 * A); g.lineWidth = .2; g.stroke();
          g.fillStyle = css(SIGN, .42 * A); squares(g, 1.5, -3.55, 6, 2, 1.75, 1.2);
          g.fillStyle = css(pale(SIGN, .5), .95 * A); g.beginPath(); g.arc(15.4, -2.2, .65, 0, 6.283); g.fill();
          g.strokeStyle = css(SIGN, .3 * A); g.lineWidth = .22; g.beginPath(); g.moveTo(1.2, 1.9); g.lineTo(16.6, 1.9); g.moveTo(1.2, 3.8); g.lineTo(11.5, 3.8); g.stroke();
        },
      },
      {
        id: 'tiles', name: 'Плитки', outline: sheetOutline,
        idea: 'Шесть отдельных плиток в два ряда, чуть вразнобой, как разложенные карточки; каждая — тусклая рамка с четырьмя квадратиками. Вместе занимают ровно место листа; при открытии вспыхивают по одной.',
        time: 'Плитки вспыхивают по одной за 0,1 с · гаснут вместе за 0,08 с · дальше неподвижны: мерцание 0,18 и поворот за взглядом.',
        light(g, G, K) {
          K.sheet();
          for (const q of TILES) {
            const k = !K.opening || K.hp >= q.at + .12 ? 1 : K.hp >= q.at ? .45 : 0; if (!k) continue;
            const A = K.A * k;
            g.save(); g.translate(q.x + 2.6, q.y + 2.3); g.rotate(q.a);
            rr(g, -2.6, -2.3, 5.2, 4.6, .5); g.fillStyle = css(SIGN, .1 * A); g.fill(); g.strokeStyle = css(SIGN, .7 * A); g.lineWidth = .25; g.stroke();
            g.fillStyle = css(SIGN, .4 * A); squares(g, -1.75, -1.65, 2, 2, 2.2, 1.3, 2);
            g.restore();
          }
        },
      },
      {
        id: 'arc', name: 'Дуга', time: TIME,
        idea: 'Изогнутая полоса вокруг глаз героя, как экран, обёрнутый вокруг шлема: середина на месте листа, длина та же, внутри два ряда квадратиков. Обращена к лицу, а не к зрителю.',
        outline(G) { const a = arcGeo(G), out = []; for (const r of [a.R - a.t2, a.R + a.t2]) for (const u of [-1, -.5, 0, .5, 1]) { const t = a.th + u * a.half; out.push([a.E[0] + Math.cos(t) * r, a.E[1] + Math.sin(t) * r]); } return out; },
        light(g, G, K) {
          const a = arcGeo(G), A = K.A, E = a.E; K.hero();
          g.beginPath(); g.arc(E[0], E[1], a.R + a.t2, a.th - a.half, a.th + a.half); g.arc(E[0], E[1], a.R - a.t2, a.th + a.half, a.th - a.half, true); g.closePath();
          g.fillStyle = css(SIGN, .1 * A); g.fill(); g.strokeStyle = css(SIGN, .75 * A); g.lineWidth = .3; g.stroke();
          g.fillStyle = css(SIGN, .42 * A);
          for (let i = 0; i < 6; i++) for (let j = 0; j < 2; j++) {
            const t = a.th - a.half + (i + .5) * (2 * a.half / 6), r = a.R + (j ? 1.25 : -1.25) * G.sq;
            g.save(); g.translate(E[0] + Math.cos(t) * r, E[1] + Math.sin(t) * r); g.rotate(t + Math.PI / 2); g.fillRect(-.6, -.6 * G.sq, 1.2, 1.2 * G.sq); g.restore();
          }
          const t = a.th + a.half * .86; g.fillStyle = css(pale(SIGN, .5), .9 * A); g.beginPath(); g.arc(E[0] + Math.cos(t) * a.R, E[1] + Math.sin(t) * a.R, .5, 0, 6.283); g.fill();
        },
      },
      {
        id: 'fan', name: 'Веер', plates: true, outline: sheetOutline,
        idea: 'Четыре пластины дрона слетают и встают по углам листа, между ними натянут экран с сеткой. Пока окно открыто, дрон голый — видно, откуда голограмма; на закрытии пластины возвращаются.',
        time: 'Пластины долетают за 0,1 с, экран загорается к концу · гаснет за 0,08 с, пластины домой за то же время · дальше неподвижна: мерцание 0,18 и поворот за взглядом.',
        light(g, G, K) {
          const A = K.A * (K.opening ? c01((K.hp - .4) / .6) : 1); K.sheet();
          if (A > .003) {
            g.beginPath(); FAN_C.forEach((p, i) => i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1])); g.closePath();
            g.fillStyle = css(SIGN, .08 * A); g.fill(); g.strokeStyle = css(SIGN, .45 * A); g.lineWidth = .22; g.stroke();
            g.fillStyle = css(SIGN, .4 * A); squares(g, 2.6, -3, 6, 3, 2.25, 1.15, 2.3);
          }
          for (const p of FAN_C) halo(g, p[0], p[1], 2.4, SIGN, .45 * K.A * K.hp);
        },
      },
      {
        id: 'brackets', name: 'Скобы', time: TIME, outline: sheetOutline,
        idea: 'Только четыре угловые скобки и сетка мелких квадратиков между ними — без рамки и заливки. Самый лёгкий вариант: мир виден насквозь, место листа читается по углам.',
        light(g, G, K) {
          const A = K.A; K.sheet();
          g.strokeStyle = css(SIGN, .85 * A); g.lineWidth = .38; g.lineCap = 'square'; g.beginPath();
          for (const [x, y, dx, dy] of [[0, -5, 1, 1], [18, -5, -1, 1], [18, 6, -1, -1], [0, 6, 1, -1]]) { g.moveTo(x + dx * 3, y); g.lineTo(x, y); g.lineTo(x, y + dy * 3); }
          g.stroke();
          g.fillStyle = css(SIGN, .45 * A); squares(g, 2.25, -3.25, 7, 4, 2.13, .75, 2.27);
        },
      },
    ];

    // «Веер»: the drone's four plates on their way to the sheet's corners, or there
    function fanPlates(T, h) {
      const G = holoGeom(h), e = sm(h.hs.hp), c = T.c, k = DS * DSC, d = h.d;
      DR.sh.forEach((s, i) => {
        const I = plateIdle(i, T.time + h.seed), from = [d.p[0] + I[0] * k, d.p[1] + I[1] * k], to = G.scr(G.toH(FAN_C[i][0], FAN_C[i][1]));
        const p = lerp2(from, to, e); p[1] -= Math.sin(e * Math.PI) * 8;
        c.save(); c.translate(p[0], p[1]); c.scale(lerp(k, k * .7, e), lerp(k, k * .7, e));
        plate(c, { x: 0, y: 0, a: angLerp(I[2], G.turn * h.face + i * 1.5708, e) }, s, s.col, lerp(I[3], 1, e));
        c.restore();
      });
    }
    function cone(g, L, pts, A) {
      let cx = 0, cy = 0; for (const p of pts) { cx += p[0]; cy += p[1]; } cx /= pts.length; cy /= pts.length;
      const base = Math.atan2(cy - L[1], cx - L[0]); if (Math.hypot(cx - L[0], cy - L[1]) < 2) return;
      let lo = 1e9, hi = -1e9, pl = pts[0], ph = pts[0], far = 0;
      for (const p of pts) {
        let a = Math.atan2(p[1] - L[1], p[0] - L[0]) - base; while (a > Math.PI) a -= 2 * Math.PI; while (a < -Math.PI) a += 2 * Math.PI;
        if (a < lo) { lo = a; pl = p; } if (a > hi) { hi = a; ph = p; }
        far = Math.max(far, (p[0] - L[0]) * Math.cos(base) + (p[1] - L[1]) * Math.sin(base));
      }
      const F = [L[0] + Math.cos(base) * far, L[1] + Math.sin(base) * far], gr = g.createLinearGradient(L[0], L[1], F[0], F[1]);
      gr.addColorStop(0, css(SIGN, CONE[0] * A)); gr.addColorStop(1, css(SIGN, CONE[1] * A));
      g.fillStyle = gr; g.beginPath(); g.moveTo(L[0], L[1]); g.lineTo(pl[0], pl[1]); g.lineTo(ph[0], ph[1]); g.closePath(); g.fill();
    }
    // the hologram: drawn into a buffer, its odd screen rows dimmed, then added onto the world
    function drawHolo(T, h) {
      const hs = h.hs; hs.drawn = false;
      if (hs.hp <= 0) return;
      const G = holoGeom(h), S = T.S;
      const A = ALPHA * (hs.target ? openShape(hs.hp) : hs.hp) * (OPT.flicker ? 1 - FLICK * noise(T.time, h.seed) : 1);
      const pts = T.cand.outline(G).map(G.scr), lensP = lensAt(h.d);
      let x0 = 1e9, y0 = 1e9, x1 = -1e9, y1 = -1e9;
      for (const p of OPT.cone ? pts.concat([lensP]) : pts) { x0 = Math.min(x0, p[0]); y0 = Math.min(y0, p[1]); x1 = Math.max(x1, p[0]); y1 = Math.max(y1, p[1]); }
      const ox = Math.floor((x0 - 10) * S), oy = Math.floor((y0 - 10) * S);
      const bw = Math.min(T.hb.width, Math.ceil((x1 + 10) * S) - ox), bh = Math.min(T.hb.height, Math.ceil((y1 + 10) * S) - oy);
      if (bw <= 0 || bh <= 0) return;
      const g = T.hbg;
      g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, bw + 2, bh + 2);
      g.globalCompositeOperation = 'lighter'; g.lineCap = 'butt'; g.lineJoin = 'miter';
      const hero = () => g.setTransform(S * h.face * Z, 0, 0, S * Z, S * h.x - ox, S * h.y - oy);
      const sheet = () => { hero(); g.translate(G.O[0], G.O[1]); g.rotate(G.turn); g.translate(0, .5); g.scale(1, G.sq); g.translate(0, -.5); };
      if (OPT.cone) { g.setTransform(S, 0, 0, S, -ox, -oy); cone(g, lensP, pts, A); }
      T.cand.light(g, G, { A, hp: hs.hp, opening: hs.target, hero, sheet });
      g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'destination-out'; g.fillStyle = T.pat[oy & 1]; g.fillRect(0, 0, bw, bh); g.globalCompositeOperation = 'source-over';
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0, bw, bh, ox, oy, bw, bh); c.restore();
      hs.drawn = true; hs.geom = { c: G.scr(G.c), eye: G.scr(G.eye), pts };
      if (OPT.light) faceLight(T, h, G, A);
    }
    // its light on the helmet: a glow before the visor, a rim on the side it is on
    function faceLight(T, h, G, A) {
      const c = T.c, eye = G.scr(G.eye), hc = toS(h, h.P.helm), cc = G.scr(G.c), a = Math.atan2(cc[1] - hc[1], cc[0] - hc[0]);
      halo(c, eye[0], eye[1], 30, SIGN, .28 * A);
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round';
      c.strokeStyle = css(SIGN, .45 * A); c.lineWidth = 2; c.beginPath(); c.arc(hc[0], hc[1], HELM_R * Z - 1.5, a - .75, a + .75); c.stroke();
      c.fillStyle = css(pale(SIGN, .3), .3 * A); c.beginPath(); c.ellipse(eye[0] - h.face * 3, eye[1] + 1, 7, 5, 0, 0, 6.283); c.fill();
      c.restore();
    }

    return {
      cands, expose: { OPEN_S, CLOSE_S, HEAD },
      init(T) {
        T.fr = newHero('friend', FRIEND_X, 4.1); T.heroes = [T.fr, T.me];
        for (const h of T.heroes) h.hs = { target: false, hp: 0, openT: -10, closeT: -10, drawn: false, geom: null };
      },
      // the buffer and its scanlines: odd screen rows lose 35 %, whatever row the buffer starts on
      onScale(T) {
        const cv = document.createElement('canvas'); cv.width = Math.ceil(HB[0] * T.S); cv.height = Math.ceil(HB[1] * T.S);
        T.hb = cv; T.hbg = cv.getContext('2d');
        const pat = odd => { const p = document.createElement('canvas'); p.width = 1; p.height = 2; const g = p.getContext('2d'); g.fillStyle = `rgba(0,0,0,${SCAN})`; g.fillRect(0, odd ? 0 : 1, 1, 1); return T.hbg.createPattern(p, 'repeat'); };
        T.pat = [pat(0), pat(1)];
      },
      // the hologram with the window, the friend's a second later; the arms round its grip while «держат как лист»
      step(T, dt) {
        turnHead(T.fr, .1 + .22 * Math.sin(.7 * T.time) + .1 * Math.sin(1.9 * T.time + 1), dt);
        for (const [h, o] of [[T.me, T.win.on], [T.fr, scheduledOn(T, FRIEND_LAG)]]) {
          const hs = h.hs;
          if (o !== hs.target) { hs.target = o; if (o) hs.openT = T.time; else hs.closeT = T.time; }
          if (!OPT.playing) hs.hp = o ? 1 : 0;
          else hs.hp = o ? Math.min(1, hs.hp + dt / OPEN_S + 1e-9) : Math.max(0, hs.hp - dt / CLOSE_S - 1e-9);
          const tg = OPT.arms === 'hold' && (hs.target || hs.hp > 0) ? 1 : 0;
          h.e = !OPT.playing ? tg : tg > h.e ? Math.min(1, h.e + dt / .1) : Math.max(0, h.e - dt / .12);
          h.hl = OPT.follow === 'look' ? h.look : FIXED_LOOK;
        }
      },
      aim: (T, h) => h.hs.target || h.hs.hp > 0 ? (G => G.scr(G.c))(holoGeom(h)) : null,
      platesAway: (T, h) => !!T.cand.plates && h.hs.hp > 0,
      overHero(T, h) {
        if (h === T.fr) { T.c.font = '12px system-ui,"Segoe UI",sans-serif'; T.c.textAlign = 'center'; T.c.fillStyle = 'rgba(200,205,214,.55)'; T.c.fillText('друг: его окно на 1 с позже', FRIEND_X, FLOOR_Y + 56); T.c.textAlign = 'left'; }
        if (T.cand.plates && h.hs.hp > 0) fanPlates(T, h);
      },
      overDrone: drawHolo,
      hud(T) { const up = -T.me.look; return `голова: ${up >= 0 ? 'вверх' : 'вниз'} ${Math.abs(up).toFixed(2).replace('.', ',')} рад (пределы 0,6 вверх, 0,5 вниз)` + (T.time - T.clickAt < 2.2 ? ' · призрак взял стопку' : ''); },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    still: [['turn = sheetTurn(h.hl);', 'turn = sheetTurn(h.hl) + Math.sin(Date.now()) * .02;']],
    open: [['hs.hp + dt / OPEN_S', 'hs.hp + dt / (2 * OPEN_S)']],
    up: [['const sq = hs.target ? 1', 'O[1] -= 30; const sq = hs.target ? 1']],
  },

  // every combination of the look's toggles over the three open modes, a quarter with the window hidden; forced turns
  // at every moment; a still hero under a still pointer; slow; pause; a pick
  check(K) {
    const { OPT: O, X } = K, IDS = ['armsMode', 'followMode', 'coneMode', 'lightMode', 'flickMode'];
    const setCombo = i => IDS.forEach((id, j) => K.seg(id, (i >> j) & 1));
    for (let i = 0; i < 32; i++) K.at(20 + i * 170, () => { setCombo(i); K.seg('openMode', i % 3); K.seg('winMode', i % 4 === 3 ? 1 : 0); });
    K.at(5480, () => { setCombo(0); K.seg('openMode', 0); K.seg('winMode', 0); }); K.forceSeq(5500);
    K.at(5790, () => setCombo(27)); K.forceSeq(5800);
    K.at(6090, () => { setCombo(12); K.seg('heroMode', 1); }); K.forceSeq(6100);
    K.at(6400, () => { setCombo(0); O.force = true; K.ptrAll([300, 120]); });
    K.at(6560, () => K.ptrAll([980, 430]));
    K.at(6720, () => { K.seg('followMode', 1); K.ptrAll([60, 60]); });
    K.at(6880, () => { K.ptrAll(null); O.force = null; K.seg('heroMode', 0); K.seg('followMode', 0); });
    K.at(6900, () => K.seg('speedMode', 1)); K.forceSeq(7000); K.at(7300, () => K.seg('speedMode', 0));
    K.at(7350, () => K.seg('playMode', 1)); K.at(7370, () => K.ptrAll([200, 80])); K.at(7380, () => K.ptrAll([900, 460]));
    K.at(7400, () => K.ptrAll(null)); K.at(7420, () => K.seg('playMode', 0));
    K.at(7500, () => K.click(2, '.pick'));
    const st = { toFull: 0, toGone: 0, opens: 0, maxUp: -9, lookMin: 9, lookMax: -9, statics: 0, combos: new Set(), hidden: 0, size: {}, lags: new Set() };
    const per = K.tiles.map(() => ({ me: {}, fr: {}, still: 0 }));
    const keyOf = () => `${O.arms}/${O.follow}/${O.cone}/${O.light}/${O.flicker}`;
    K.run(7600, (t, i) => {
      const p = per[K.tiles.indexOf(t)];
      if (t.win.on !== t.me.hs.target) K.fail(`${t.cand.id}: window ${t.win.on}, hologram ${t.me.hs.target}, frame ${i}`);
      for (const nm of ['me', 'fr']) {
        const h = t[nm], hs = h.hs, q = p[nm];
        if (hs.target && !q.target) { st.opens++; q.full = false; if (nm === 'fr' && O.force === null && O.mode !== 'always' && t.me.hs.target) st.lags.add((hs.openT - t.me.hs.openT).toFixed(2)); }
        if (!hs.target && q.target) q.gone = false;
        q.target = hs.target;
        if (!hs.target && hs.hp === 0 && hs.drawn) K.fail(`${t.cand.id} ${nm}: drawn while closed, frame ${i}`);
        if (hs.target && !q.full && hs.hp >= 1 && hs.drawn) { q.full = true; st.toFull = Math.max(st.toFull, t.time - hs.openT); }
        if (!hs.target && !q.gone && hs.hp === 0 && hs.closeT > 0) { q.gone = true; st.toGone = Math.max(st.toGone, t.time - hs.closeT); }
        if (hs.target && t.time - hs.openT >= X.OPEN_S - 1e-9 && (hs.hp < 1 || !hs.drawn)) K.fail(`${t.cand.id} ${nm}: hp ${hs.hp.toFixed(3)} at +${(t.time - hs.openT).toFixed(3)} s, frame ${i}`);
        if (!hs.target && t.time - hs.closeT >= X.CLOSE_S - 1e-9 && (hs.hp > 0 || hs.drawn)) K.fail(`${t.cand.id} ${nm}: shown +${(t.time - hs.closeT).toFixed(3)} s after close, frame ${i}`);
        if (h.look < -X.HEAD.Up - 1e-9 || h.look > X.HEAD.Down + 1e-9) K.fail(`${t.cand.id} ${nm}: look ${h.look}`);
        st.lookMin = Math.min(st.lookMin, h.look); st.lookMax = Math.max(st.lookMax, h.look);
        if (!hs.drawn) continue;
        const g = hs.geom, up = Math.atan2(g.eye[1] - g.c[1], Math.abs(g.c[0] - g.eye[0]));
        st.maxUp = Math.max(st.maxUp, up);
        if (up > .6 + 1e-6) K.fail(`${t.cand.id} ${nm}: centre ${up.toFixed(3)} rad above the eye, frame ${i}`);
        if (nm === 'me') { st.combos.add(keyOf()); if (O.hideWindow) st.hidden++; }
        if (hs.hp >= 1) { let d = 0; for (const a of g.pts) for (const b of g.pts) d = Math.max(d, Math.hypot(a[0] - b[0], a[1] - b[1])); const s = st.size[t.cand.id] || (st.size[t.cand.id] = [1e9, 0]); s[0] = Math.min(s[0], d); s[1] = Math.max(s[1], d); }
      }
      // still: hero standing, pointer resting, window open: the hologram must not move against the eye
      const k = `${keyOf()}${O.slow}${t.real ? t.ptr.join(',') : 'ghost'}`, g = t.me.hs.geom;
      p.still = O.stand && t.real && O.playing && t.me.hs.target && k === p.key ? p.still + 1 : 0; p.key = k;
      if (p.still > 40 && g && p.geo) {
        st.statics++;
        let mv = Math.hypot(g.eye[0] - p.geo.eye[0], g.eye[1] - p.geo.eye[1]);
        g.pts.forEach((q, j) => { mv = Math.max(mv, Math.hypot(q[0] - g.eye[0] - p.geo.rel[j][0], q[1] - g.eye[1] - p.geo.rel[j][1])); });
        if (mv > .01) K.fail(`${t.cand.id}: hologram moves ${mv.toFixed(3)} px on a still hero, frame ${i}`);
      }
      p.geo = g ? { eye: g.eye.slice(), rel: g.pts.map(q => [q[0] - g.eye[0], q[1] - g.eye[1]]) } : null;
    });
    console.log(`opens ${st.opens}; full by +${st.toFull.toFixed(3)} s, gone by +${st.toGone.toFixed(3)} s; friend opens after me by ${[...st.lags].join(' ')} s`);
    console.log(`centre above the eye at most ${st.maxUp.toFixed(3)} rad (limit 0.6); head look ${st.lookMin.toFixed(3)}..${st.lookMax.toFixed(3)}`);
    console.log(`still frames ${st.statics}; look combinations drawn ${st.combos.size}/32; frames drawn with the window hidden ${st.hidden}`);
    console.log('outline span, px:', Object.entries(st.size).map(([c, v]) => `${c} ${v[0].toFixed(0)}..${v[1].toFixed(0)}`).join(', '));
    return st.statics > 100 && st.combos.size === 32 && st.hidden > 0 && K.clicks.length > 0 && st.toFull <= X.OPEN_S + 1e-6;
  },
};
