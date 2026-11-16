// The suits and helmets painted raw (skins): GPT's paint on a sheet of the rig's parts (a suit's four: torso, pack, arm, leg;
// whole-limbs), in ink and watercolour, shrunk by area to the hd size and laid side by side in traced/suit<Name>.png. Each part goes
// out as it is, its margin included: `out` + the part's name. An arm and a leg are drawn straight, the elbow 8.59 and the knee 11.32
// down their boxes.
Subjects.suits = function () {
  for (const [name, t] of Object.entries(TRACED)) {
    if (!t.out || !t.raw) continue;
    const img = Traced.picture(name);
    for (const [part, [x, y, w, h]] of Object.entries(t.parts)) {
      const c = document.createElement('canvas');
      [c.width, c.height] = [w, h];
      c.getContext('2d').drawImage(img, x, y, w, h, 0, 0, w, h);
      out(t.out + part, c);
    }
  }
};
