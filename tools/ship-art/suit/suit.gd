extends SceneTree
# Placeholder icons for the basic suit (2026-10-27-suits-are-worn), seen from the front: the helmet, its blue visor and
# its collar ring; the body, empty, the air pack's hoses at the shoulders, boots under it. The helmet lamp, a torch on a
# clamp, from the side: its icon, and the picture fixed on the hero's helmet, its lens where the light leaves.
const PAL = {
	"D": Color("1a1e25"), "W": Color("d9dde3"), "G": Color("a9b0ba"), "M": Color("6b7486"),
	"L": Color("7fc4f0"), "B": Color("3a8fd0"), "O": Color("e08a3c"), "K": Color("262b35"),
	"o": Color("14171e"), "l": Color("c0cad8"), "m": Color("96a0b0"), "n": Color("747e90"), "t": Color("5a6474"),
	"s": Color("3e4656"), "Y": Color("f0be5a"), "Z": Color("fff6ce"),
}

func paint(rows: Array, path: String) -> void:
	var w = rows[0].length()
	var img = Image.create(w, rows.size(), false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))
	for y in rows.size():
		for x in w:
			var ch = rows[y][x]
			if PAL.has(ch):
				img.set_pixel(x, y, PAL[ch])
	img.save_png(path)
	print("wrote ", path)

func _init():
	var root = OS.get_cmdline_user_args()[0]
	paint([
		"................",
		"................",
		".....DDDDDD.....",
		"....DWWWWWWD....",
		"...DWWLLBBWWD...",
		"...DWLLBBBBWD...",
		"...DWLBBBBBWD...",
		"...DWWBBBBWWD...",
		"...DGWWWWWWGD...",
		"....DGGGGGGD....",
		".....DMMMMD.....",
		"....DGGGGGGD....",
		"....DDDDDDDD....",
		"................",
		"................",
		"................",
	], root + "/art/items/helmet.png")
	paint([
		"................",
		"................",
		"......DMMD......",
		"..DDDDGGGGDDDD..",
		".DWWGMWWWWMGWWD.",
		".DWGDWWOOWWDGWD.",
		".DWGDWWWWWWDGWD.",
		".DGDDGWWWWGDDGD.",
		"..DD.DGMMGD.DD..",
		".....DWGDWD.....",
		".....DWGDWD.....",
		".....DWGDWD.....",
		"....DKKDDKKD....",
		"................",
		"................",
		"................",
	], root + "/art/items/suit.png")
	paint([
		"................",
		"................",
		"................",
		"................",
		"............ooo.",
		"....oooooooooYZo",
		"...olllllllloYZo",
		"...ommmmmmmmoYZo",
		"...onnnnnnnnoYZo",
		"....oooooooooYZo",
		"......osso..ooo.",
		"....oosttsoo....",
		"...ossooooosso..",
		"...oso.....oso..",
		"....o.......o...",
		"................",
	], root + "/art/items/helmetLamp.png")
	paint([".oooo.", "ollllZ", "onnnnY", ".osso."], root + "/art/hero_headLamp.png")
	quit()
