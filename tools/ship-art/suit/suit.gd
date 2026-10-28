extends SceneTree
# Icons for the basic suit (2026-10-27-suits-are-worn) in the hero's suit ramp as drawn (tools/hero-art/parts.js), its
# boots, gloves and glass: the helmet a lit dome, its dark visor turned right as the hero's is, glinting, on its collar
# ring; the body from the front, empty, a collar ring for the helmet, a chest panel, a belt, dark gloves and boots. The helmet lamp, a torch on a
# clamp, from the side: its icon, and the picture fixed on the hero's helmet, its lens where the light leaves. The cape
# (2026-10-27-the-cape-is-worn) from behind, in the wool and lining of hero_cape (tools/hero-art/parts.js), its folds down the back.
const PAL = {
	"D": Color("1a1e25"), "W": Color("d9dde3"), "G": Color("a9b0ba"), "M": Color("6b7486"),
	"L": Color("7fc4f0"), "B": Color("3a8fd0"), "O": Color("e08a3c"), "K": Color("262b35"),
	"o": Color("14171e"), "l": Color("c0cad8"), "m": Color("96a0b0"), "n": Color("747e90"), "t": Color("5a6474"),
	"s": Color("3e4656"), "Y": Color("f0be5a"), "Z": Color("fff6ce"),
	"P": Color("c49a68"), "Q": Color("9a7148"), "R": Color("6e4f33"), "T": Color("3b2a1b"), "A": Color("6f9e98"),
	"e": Color("ebc178"), "f": Color("e0a030"), "g": Color("a17323"), "h": Color("5e4314"), "q": Color("3d4452"),
	"v": Color("1d2836"), "c": Color("8fb6d6"), "w": Color("cfe6f5"),
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
		".....hhhhhh.....",
		"...hheeefffhh...",
		"..heeeffffffgh..",
		".heeffffKKKKKgh.",
		".hefffKKvvvvvKh.",
		".heffKvwcvvvvKh.",
		".heffKvcvvvvvKh.",
		".hfffKvvvvvvcKh.",
		".hfffgKvvvvvKgh.",
		"..hgffgKKKKKgh..",
		"...hggggggggh...",
		"..KMMMMMMMMMMK..",
		"..KqqqqqqqqqqK..",
		"...KKKKKKKKKK...",
		"................",
	], root + "/art/items/helmet.png")
	paint([
		"................",
		"................",
		"..hhhhKMMKhhhh..",
		".heeeeefffffggh.",
		".hefheffqqfhfgh.",
		".hefhfffcqghfgh.",
		".hefhfffgfghfgh.",
		".hefhKqqMqKhfgh.",
		".hefheffgfghfgh.",
		".hqqhefgffghqqh.",
		".hKKhefgffghKKh.",
		"..hhhefgffghhh..",
		"....hefhhfgh....",
		"....hefhhfgh....",
		"...KMqqKKMqqK...",
		"...KKKKKKKKKK...",
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
