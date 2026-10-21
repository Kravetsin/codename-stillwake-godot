// Minimal PNG codec (8-bit, non-interlaced; gray/rgb/rgba/palette) using node zlib.
const fs = require('fs'), zlib = require('zlib');
function crc32(buf) { let c, crc = 0xffffffff; for (let n = 0; n < buf.length; n++) { c = (crc ^ buf[n]) & 0xff; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; } return (crc ^ 0xffffffff) >>> 0; }
function decode(file) {
  const b = fs.readFileSync(file); let p = 8, w, h, depth, ct, pal = null, trns = null; const idat = [];
  while (p < b.length) { const len = b.readUInt32BE(p), type = b.toString('ascii', p + 4, p + 8), d = b.slice(p + 8, p + 8 + len);
    if (type === 'IHDR') { w = d.readUInt32BE(0); h = d.readUInt32BE(4); depth = d[8]; ct = d[9]; if (d[12]) throw new Error('interlaced ' + file); }
    else if (type === 'PLTE') pal = d; else if (type === 'tRNS') trns = d; else if (type === 'IDAT') idat.push(d);
    p += 12 + len; }
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const chans = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[ct]; const bpp = Math.max(1, (chans * depth) >> 3); const stride = (w * chans * depth + 7) >> 3;
  const out = Buffer.alloc(w * h * 4); let prev = Buffer.alloc(stride), q = 0;
  for (let y = 0; y < h; y++) { const f = raw[q++]; const line = Buffer.from(raw.slice(q, q + stride)); q += stride;
    for (let i = 0; i < stride; i++) { const a = i >= bpp ? line[i - bpp] : 0, up = prev[i], c = i >= bpp ? prev[i - bpp] : 0; let v = line[i];
      if (f === 1) v += a; else if (f === 2) v += up; else if (f === 3) v += (a + up) >> 1; else if (f === 4) { const pp = a + up - c, pa = Math.abs(pp - a), pb = Math.abs(pp - up), pc = Math.abs(pp - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? up : c; }
      line[i] = v & 255; }
    for (let x = 0; x < w; x++) { const o = (y * w + x) * 4; let r, g, bb, al = 255;
      const samp = (idx) => { if (depth === 8) return line[idx]; const per = 8 / depth, byte = line[Math.floor(idx / per)], sh = 8 - depth * (idx % per + 1); return (byte >> sh) & ((1 << depth) - 1); };
      if (ct === 6) { r = line[x * 4]; g = line[x * 4 + 1]; bb = line[x * 4 + 2]; al = line[x * 4 + 3]; }
      else if (ct === 2) { r = line[x * 3]; g = line[x * 3 + 1]; bb = line[x * 3 + 2]; }
      else if (ct === 0) { r = g = bb = samp(x) * (255 / ((1 << depth) - 1)); }
      else if (ct === 4) { r = g = bb = line[x * 2]; al = line[x * 2 + 1]; }
      else if (ct === 3) { const i = samp(x); r = pal[i * 3]; g = pal[i * 3 + 1]; bb = pal[i * 3 + 2]; al = trns && i < trns.length ? trns[i] : 255; }
      out[o] = r; out[o + 1] = g; out[o + 2] = bb; out[o + 3] = al; }
    prev = line; }
  return { w, h, data: out };
}
function encode(img, file) {
  const { w, h, data } = img; const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; data.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4); }
  const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc32(td)); return Buffer.concat([l, td, c]); };
  const ih = Buffer.alloc(13); ih.writeUInt32BE(w, 0); ih.writeUInt32BE(h, 4); ih[8] = 8; ih[9] = 6;
  fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]));
}
function create(w, h) { return { w, h, data: Buffer.alloc(w * h * 4) }; }
function scale(img, k) { const o = create(img.w * k, img.h * k); for (let y = 0; y < o.h; y++) for (let x = 0; x < o.w; x++) img.data.copy(o.data, (y * o.w + x) * 4, (((y / k) | 0) * img.w + ((x / k) | 0)) * 4, (((y / k) | 0) * img.w + ((x / k) | 0)) * 4 + 4); return o; }
module.exports = { decode, encode, create, scale };
