const { decode, encode } = require(process.argv[5]);
const a = decode(process.argv[2]), b = decode(process.argv[3]);
for (let i = 0; i < a.w * a.h; i++) if (b.rgba[i*4+3]) b.rgba.copy(a.rgba, i*4, i*4, i*4+4);
encode(process.argv[4], a.w, a.h, a.rgba);
