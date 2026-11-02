extends SceneTree
# Water's pictures (water), in the cistern's palette: the bottle, a glass bottle with a cap and a drop on it; and the
# electrolyser, variant A, three glass columns over a base: liquid oxygen in the left, hydrogen bubbling in the right and
# out at its top, the water reservoir between, water piped in on the right, oxygen out on the left.
const OUTLINE = Color("1a1e25")
const STEEL = [Color("9aa0ab"), Color("8f99ad"), Color("7f848f"), Color("6b7385"), Color("5b6170"), Color("474c57")]
const GLASS = Color("10161d")
const GLASS_EDGE = Color("3d5566")
const GLINT = Color("c8e4ee")
const WATER = Color("2fa3b8")
const WATER_TOP = Color("7fd8e6")
const WATER_DEEP = Color("1f6f80")
const LIQUID = Color("a8d8ff")
const LIQUID_DEEP = Color("6fa8d8")
const BUBBLE = Color("e8f6fb")
const BRASS = [Color("c98a55"), Color("b07a50"), Color("6a4430")]
const STUB = [Color("2f353d"), Color("7b838e"), Color("434b56")]
const LAMP = Color("7fe08a")
const LAMP_OFF = Color("2f3a33")
const LAMP_STALLED = Color("e05555")
# Where bubbles rise: the left column's liquid, the reservoir's water, the right column above its water, by x and the rows they span.
const RISES = [[5, 19, 36], [8, 19, 36], [14, 17, 36], [17, 17, 36], [23, 8, 29], [26, 8, 29]]

var img: Image

func put(x: int, y: int, c: Color) -> void:
	if x >= 0 and y >= 0 and x < img.get_width() and y < img.get_height():
		img.set_pixel(x, y, c)

func rect(x0: int, y0: int, x1: int, y1: int, c: Color) -> void:
	for y in range(y0, y1 + 1):
		for x in range(x0, x1 + 1):
			put(x, y, c)

func fresh(w: int, h: int) -> void:
	img = Image.create(w, h, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))

func bottle(path: String) -> void:
	fresh(16, 16)
	# The cap, the neck, then the body: glass round a teal fill, a white drop on it.
	rect(6, 0, 9, 2, OUTLINE)
	rect(7, 1, 8, 1, BRASS[0])
	rect(6, 3, 9, 5, OUTLINE)
	rect(7, 3, 8, 5, GLASS_EDGE)
	rect(4, 5, 11, 15, OUTLINE)
	rect(5, 6, 10, 14, GLASS_EDGE)
	rect(5, 8, 10, 14, WATER)
	rect(5, 8, 10, 8, WATER_TOP)
	rect(5, 14, 10, 14, WATER_DEEP)
	put(5, 6, GLINT)
	put(5, 7, GLINT)
	# The drop: a point on top, a round below.
	put(8, 9, BUBBLE)
	rect(7, 10, 9, 10, BUBBLE)
	rect(6, 11, 10, 12, BUBBLE)
	rect(7, 13, 9, 13, BUBBLE)
	put(7, 11, WATER_TOP)
	img.save_png(path)
	print("wrote ", path)

func column(x0: int, x1: int, top: int, bottom: int, fill: Color, fill_top: Color, level: int) -> void:
	# A glass tube from top to bottom, filled from its level down, a glint up its left side.
	rect(x0, top, x1, bottom, OUTLINE)
	rect(x0 + 1, top + 1, x1 - 1, bottom - 1, GLASS)
	rect(x0 + 1, level, x1 - 1, bottom - 1, fill)
	rect(x0 + 1, level, x1 - 1, level, fill_top)
	for y in range(top + 2, level - 1, 3):
		put(x0 + 1, y, GLASS_EDGE)

func cap(x0: int, x1: int, y: int) -> void:
	rect(x0 - 1, y, x1 + 1, y + 2, OUTLINE)
	rect(x0, y + 1, x1, y + 1, BRASS[0])
	put(x1, y + 1, BRASS[2])

func stub(x0: int, x1: int, flange: int, ring: Color) -> void:
	# A pipe end at its port's cell, rows 40 to 47, a ring of its fluid's colour by the shell.
	rect(x0, 41, x1, 45, OUTLINE)
	rect(x0, 42, x1, 42, STUB[0])
	rect(x0, 43, x1, 43, STUB[1])
	rect(x0, 44, x1, 44, STUB[2])
	rect(flange, 40, flange + 1, 46, OUTLINE)
	rect(flange, 41, flange + 1, 45, ring)

func electrolyser(path: String) -> void:
	fresh(32, 48)
	# The base: a steel box with a lamp, the columns standing in it.
	rect(1, 36, 30, 47, OUTLINE)
	for y in range(37, 47):
		rect(2, y, 29, y, STEEL[clampi((y - 37) * STEEL.size() / 10, 0, STEEL.size() - 1)])
	rect(2, 37, 29, 37, STEEL[0])
	rect(13, 40, 18, 44, OUTLINE)
	rect(14, 41, 17, 43, STEEL[4])
	put(15, 42, LAMP_OFF)
	put(16, 42, LAMP_OFF)
	# Liquid oxygen on the left, the reservoir of water between, hydrogen on the right.
	column(3, 10, 6, 37, LIQUID, BUBBLE, 18)
	column(12, 19, 10, 37, WATER, WATER_TOP, 16)
	column(21, 28, 6, 37, WATER_DEEP, WATER, 30)
	rect(22, 30, 27, 36, WATER)
	rect(22, 30, 27, 30, WATER_TOP)
	cap(4, 9, 3)
	cap(13, 18, 7)
	cap(22, 27, 3)
	# The hydrogen's vent out of the right column's top.
	rect(24, 0, 25, 3, OUTLINE)
	put(24, 1, STEEL[1])
	put(24, 2, STEEL[2])
	# Electrodes down the outer columns, and bubbles rising off them.
	for y in range(7, 34):
		put(7, y, STEEL[2])
		put(24, y, STEEL[2])
	# The pipes across the bottom joining the columns to the reservoir.
	rect(10, 33, 12, 35, OUTLINE)
	rect(19, 33, 21, 35, OUTLINE)
	put(11, 34, WATER)
	put(20, 34, WATER)
	stub(0, 3, 2, LIQUID_DEEP)
	stub(28, 31, 28, WATER)
	img.save_png(path)
	print("wrote ", path)

func cistern_fill(path: String, level: int) -> void:
	# The cistern's sight glass over its picture, its water up a quarter a level, 0 the dark glass alone.
	fresh(32, 32)
	rect(13, 11, 18, 18, GLASS)
	put(14, 12, GLASS_EDGE)
	put(15, 12, GLASS_EDGE)
	if level > 0:
		var top = 19 - 2 * level
		rect(13, top, 18, 18, WATER)
		rect(13, top, 18, top, WATER_TOP)
		if top < 18:
			rect(13, 18, 18, 18, WATER_DEEP)
	img.save_png(path)
	print("wrote ", path)

func electrolyser_lamp(lit: Color) -> void:
	put(15, 42, lit)
	put(16, 42, lit)

func electrolyser_work(path: String, frame: int) -> void:
	# Over its picture while it works: the lamp green, bubbles up each column, two rows a frame, four frames a cycle.
	fresh(32, 48)
	electrolyser_lamp(LAMP)
	for i in RISES.size():
		var r = RISES[i]
		var span = r[2] - r[1]
		for k in 2:
			var y = r[2] - ((i * 5 + k * span / 2 + frame * 2) % span)
			put(r[0], y, BUBBLE)
	img.save_png(path)
	print("wrote ", path)

func electrolyser_stalled(path: String) -> void:
	fresh(32, 48)
	electrolyser_lamp(LAMP_STALLED)
	img.save_png(path)
	print("wrote ", path)

func _init():
	var art = OS.get_cmdline_user_args()[0]
	bottle(art + "/items/water.png")
	electrolyser(art + "/machine_electrolyser.png")
	for level in 5:
		cistern_fill(art + "/cistern_fill_%d.png" % level, level)
	for frame in 4:
		electrolyser_work(art + "/electrolyser_work_%d.png" % frame, frame)
	electrolyser_stalled(art + "/electrolyser_stalled.png")
	quit()
