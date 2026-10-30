extends SceneTree
# The cistern (2026-10-29-typed-ports): a steel cylinder lying on two saddles over a base plate, a hatch on top, a sight
# glass showing teal water, pipe stubs at its ports, the input left and the output right, in the tank's palette.
const OUTLINE = Color("1a1e25")
const BANDS = [Color("9aa0ab"), Color("8f99ad"), Color("7f848f"), Color("6b7385"), Color("5b6170"), Color("474c57")]
const RIB = Color("474c57")
const DARK = Color("22262c")
const GLASS = Color("10161d")
const GLINT = Color("2a3d4f")
const WATER = Color("2fa3b8")
const WATER_TOP = Color("7fd8e6")
const WATER_DEEP = Color("1f6f80")
const BRASS = [Color("c98a55"), Color("b07a50"), Color("6a4430")]
const STUB = [Color("2f353d"), Color("7b838e"), Color("434b56")]

var img: Image

func put(x: int, y: int, c: Color) -> void:
	if x >= 0 and y >= 0 and x < 32 and y < 32:
		img.set_pixel(x, y, c)

func rect(x0: int, y0: int, x1: int, y1: int, c: Color) -> void:
	for y in range(y0, y1 + 1):
		for x in range(x0, x1 + 1):
			put(x, y, c)

func band(y: int) -> Color:
	# Lit from above: the top rows lightest, the belly darkest.
	return BANDS[clampi((y - 7) * BANDS.size() / 16, 0, BANDS.size() - 1)]

func body() -> void:
	# A lying cylinder from x 3 to 28, y 7 to 22, its ends rounded by two pixels.
	for y in range(7, 23):
		var inset = [2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2][y - 7]
		for x in range(3 + inset, 29 - inset):
			var c = band(y)
			if x == 3 + inset or x == 28 - inset:
				c = c.darkened(0.25)
			put(x, y, c)
		put(2 + inset, y, OUTLINE)
		put(29 - inset, y, OUTLINE)
	rect(6, 6, 25, 6, OUTLINE)
	rect(6, 23, 25, 23, OUTLINE)
	for x in [9, 22]:
		for y in range(7, 23):
			put(x, y, RIB if y < 15 else DARK)
			put(x + 1, y, band(y).lightened(0.12))

func hatch() -> void:
	rect(12, 5, 19, 5, OUTLINE)
	rect(13, 5, 18, 5, BANDS[0])
	rect(14, 3, 17, 4, RIB)
	put(13, 3, OUTLINE)
	put(13, 4, OUTLINE)
	put(18, 3, OUTLINE)
	put(18, 4, OUTLINE)
	rect(13, 1, 18, 1, OUTLINE)
	rect(12, 2, 19, 2, OUTLINE)
	rect(13, 2, 18, 2, BRASS[0])
	put(17, 2, BRASS[1])
	put(18, 2, BRASS[2])

func sight_glass() -> void:
	rect(12, 10, 19, 19, OUTLINE)
	rect(13, 11, 18, 18, GLASS)
	rect(13, 14, 18, 18, WATER)
	rect(13, 14, 18, 14, WATER_TOP)
	rect(13, 18, 18, 18, WATER_DEEP)
	put(14, 12, GLINT)
	put(15, 12, GLINT)
	put(14, 16, WATER_TOP)
	for corner in [Vector2i(12, 10), Vector2i(19, 10), Vector2i(12, 19), Vector2i(19, 19)]:
		put(corner.x, corner.y, BRASS[1])

func saddles() -> void:
	for x0 in [6, 20]:
		rect(x0, 23, x0 + 5, 26, OUTLINE)
		rect(x0 + 1, 24, x0 + 4, 26, BANDS[3])
		rect(x0 + 1, 24, x0 + 4, 24, BANDS[1])
		put(x0 + 2, 25, RIB)

func base() -> void:
	rect(1, 26, 30, 30, OUTLINE)
	rect(2, 27, 29, 27, BANDS[0])
	rect(2, 28, 29, 28, BANDS[2])
	rect(2, 29, 29, 29, RIB)
	for x in [4, 27]:
		put(x, 28, DARK)

func stub(x0: int, x1: int, flange: int) -> void:
	# A pipe end at its port's cell, rows 16 to 23: three tones across, a flange where it meets the shell.
	rect(x0, 17, x1, 21, OUTLINE)
	rect(x0, 18, x1, 18, STUB[0])
	rect(x0, 19, x1, 19, STUB[1])
	rect(x0, 20, x1, 20, STUB[2])
	rect(flange, 16, flange + 1, 22, OUTLINE)
	rect(flange, 17, flange + 1, 21, STUB[0])
	put(flange, 19, STUB[1])

func _init():
	img = Image.create(32, 32, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))
	body()
	hatch()
	sight_glass()
	saddles()
	base()
	stub(0, 3, 2)
	stub(28, 31, 28)
	var path = OS.get_cmdline_user_args()[0]
	img.save_png(path)
	print("wrote ", path)
	quit()
