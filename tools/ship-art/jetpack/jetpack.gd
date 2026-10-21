extends SceneTree
# Placeholder art for the jetpack (0047): its icon, two oxygen canisters strapped side by side with
# nozzles under them, and the pack as worn on the hero's back, seen from the side.
const PAL = {
	"D": Color("1a1e25"), "G": Color("9aa3ad"), "L": Color("7fc4f0"), "B": Color("3a8fd0"),
	"S": Color("24608f"), "M": Color("6b7486"), "P": Color("3f4552"), "K": Color("262b35"),
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
		"....DD....DD....",
		"....GG....GG....",
		"..DDDDDDDDDDDD..",
		"..DLBBSDDLBBSD..",
		"..DLBBSDDLBBSD..",
		".PMMMMMMMMMMMMP.",
		"..DLBBSDDLBBSD..",
		"..DLBBSDDLBBSD..",
		"..DLBBSDDLBBSD..",
		".PMMMMMMMMMMMMP.",
		"..DLBBSDDLBBSD..",
		"..DDDDDDDDDDDD..",
		"....PP....PP....",
		"...KKKK..KKKK...",
		"................",
	], root + "/art/items/jetpack.png")
	paint([
		".DDDD.",
		".DGGD.",
		"DDDDDD",
		"DLBBSD",
		"DLBBSD",
		"MMMMMM",
		"DLBBSD",
		"DLBBSD",
		"DLBBSD",
		"MMMMMM",
		"DLBBSD",
		"DDDDDD",
		"..PP..",
		".KKKK.",
	], root + "/art/hero_jetpack.png")
	quit()
