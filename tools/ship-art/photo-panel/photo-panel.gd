extends SceneTree
# The photosensitive panel's icon (2026-10-29-photo-panel): a violet light-sensitive sheet in a silver frame, an amber
# sheen across it, two copper contacts below; darker than the solar cell's blue, so the two are told apart in a slot.
const PAL = {
	"D": Color("1a1e25"), "F": Color("9aa3ad"), "G": Color("6b7486"), "V": Color("2e2252"),
	"P": Color("5a43a0"), "L": Color("8a6fd0"), "A": Color("e0a640"), "W": Color("f4e2a8"), "C": Color("c07a3a"),
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
	var art = OS.get_cmdline_user_args()[0]
	paint([
		"................",
		"................",
		".DDDDDDDDDDDDDD.",
		".DFFFFFFFFFFFFD.",
		".DFVVPPLWAPVVGD.",
		".DFVPPLWAPPVVGD.",
		".DFPPLWAPPVVVGD.",
		".DFPLWAPPVVVPGD.",
		".DFLWAPPVVVPPGD.",
		".DFWAPPVVVPPLGD.",
		".DFAPPVVVPPLWGD.",
		".DFGGGGGGGGGGGD.",
		".DDDDDDDDDDDDDD.",
		"....CC....CC....",
		"....CC....CC....",
		"................",
	], art + "/items/photoPanel.png")
	quit()
