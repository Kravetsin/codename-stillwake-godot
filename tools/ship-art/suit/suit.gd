extends SceneTree
# Placeholder icon for the basic suit (2026-10-27-suits-are-worn): the suit seen from the front, empty, its helmet
# with a blue visor, the air pack's hoses at the shoulders, boots under it.
const PAL = {
	"D": Color("1a1e25"), "W": Color("d9dde3"), "G": Color("a9b0ba"), "M": Color("6b7486"),
	"L": Color("7fc4f0"), "B": Color("3a8fd0"), "O": Color("e08a3c"), "K": Color("262b35"),
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
		"......DDDD......",
		".....DWWWWD.....",
		"....DWLLBBWD....",
		"....DWLBBBWD....",
		"....DGWWWWGD....",
		"..DDDMDGGDMDDD..",
		".DWWGMWWWWMGWWD.",
		".DWGDWWOOWWDGWD.",
		".DWGDWWWWWWDGWD.",
		".DGDDGWWWWGDDGD.",
		"..DD.DGMMGD.DD..",
		".....DWGDWD.....",
		".....DWGDWD.....",
		"....DKKDDKKD....",
		"................",
	], root + "/art/items/suit.png")
	quit()
