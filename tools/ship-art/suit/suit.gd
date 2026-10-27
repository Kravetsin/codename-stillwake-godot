extends SceneTree
# Placeholder icons for the basic suit (2026-10-27-suits-are-worn), seen from the front: the helmet, its blue visor and
# its collar ring; the body, empty, the air pack's hoses at the shoulders, boots under it. The cape (2026-10-27-the-cape-is-worn)
# from behind, in the wool and lining of hero_cape (tools/hero-art/parts.js), its folds down the back.
const PAL = {
	"D": Color("1a1e25"), "W": Color("d9dde3"), "G": Color("a9b0ba"), "M": Color("6b7486"),
	"L": Color("7fc4f0"), "B": Color("3a8fd0"), "O": Color("e08a3c"), "K": Color("262b35"),
	"P": Color("c49a68"), "Q": Color("9a7148"), "R": Color("6e4f33"), "T": Color("3b2a1b"), "A": Color("6f9e98"),
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
		"......TTTT......",
		".....TAAAAT.....",
		"....TPPPPPPT....",
		"...TPPQPPQPPT...",
		"...TPQPPPQPPT...",
		"..TPPQPPPQPPPT..",
		"..TPQPPPPQPPPT..",
		"..TPQPPPQPPPRT..",
		".TPPQPPPQPPPPRT.",
		".TPQPPPPQPPPPRT.",
		".TAQPPPQPPPPRAT.",
		".TTAARRRRRRAATT.",
		"..TTTTTTTTTTTT..",
		"................",
	], root + "/art/items/cape.png")
	quit()
