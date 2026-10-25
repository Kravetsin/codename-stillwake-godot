extends SceneTree
# The engine (2026-10-25-the-engine-through-the-hull), in the ship machines' palette: a salvaged engine block, 72x32. Its
# footprint is the right 48 px (6x4 cells); the cone nozzle reaches the left 24 px (3 cells), drawn over the stern's wall
# and out into space. A firebox with a grille, a pressure gauge, a filler cap, a short stack, a hazard band, a copper hose
# to the nozzle's collar. Also the icons of its parts: the nozzle, the fuel pump, the igniter. With a preview path, a
# sheet at 4x: the engine idle, burning (its glow and flame, for when it runs), and aboard at the stern.
#   godot --headless --script tools/ship-art/engine/engine.gd -- <artDir> [sheet.png]

const C = {
	"line": Color("1a1e25"), "deep": Color("10161d"), "glass": Color("1f2b36"),
	"hi": Color("9aa0ab"), "steel": Color("8f99ad"), "mid": Color("7f848f"), "mid2": Color("6b7385"),
	"dark": Color("5b6170"), "dark2": Color("474c57"), "white": Color("d8dde6"),
	"cu": Color("b07a50"), "cu2": Color("8a5a3a"), "cuHi": Color("c98a55"),
	"amber": Color("f0c050"), "red": Color("ff5a4a"),
	"cyan": Color("7fc4f0"), "cyanHi": Color("e0ffff"), "cyanDim": Color("2f5f78"),
}
const CLEAR = Color(0, 0, 0, 0)
const DIRS = [Vector2i(1, 0), Vector2i(-1, 0), Vector2i(0, 1), Vector2i(0, -1)]
# The nozzle's reach left of the footprint, in pixels: Machines' Exhaust, 3 cells.
const REACH = 24
# The nozzle's axis and its exit, for the flame.
const AXIS = 15
const EXIT_R = 10

var STEEL = [C.hi, C.steel, C.mid, C.mid2, C.dark]
var DARK = [C.mid, C.mid2, C.dark, C.dark2, Color("3a3e48")]
var COPPER = [C.cuHi, C.cu, C.cu, C.cu2, Color("6a4430")]
var LIP = [C.white, C.hi, C.steel, C.mid, C.mid2]
var SCORCH = [Color("8a8f9c"), Color("6e7383"), Color("5a5f6d"), Color("474b57"), Color("363a44")]
var HOT = [Color("8a2a1a"), Color("d8532a"), Color("ff9a3a"), Color("ffd870")]
var FIRE = [Color("fff6d0"), Color("ffd870"), Color("ff9a3a"), Color("d8532a"), Color("8a2a1a")]

var img: Image

func blank(w: int, h: int) -> void:
	img = Image.create_empty(w, h, false, Image.FORMAT_RGBA8)
	img.fill(CLEAR)

func setp(x: int, y: int, c: Color) -> void:
	if x >= 0 and y >= 0 and x < img.get_width() and y < img.get_height():
		img.set_pixel(x, y, c)

func getp(x: int, y: int) -> Color:
	if x >= 0 and y >= 0 and x < img.get_width() and y < img.get_height():
		return img.get_pixel(x, y)
	return CLEAR

func rect(x0: int, y0: int, x1: int, y1: int, c: Color) -> void:
	for y in range(y0, y1 + 1):
		for x in range(x0, x1 + 1):
			setp(x, y, c)

func box(x0: int, y0: int, x1: int, y1: int, fill: Color) -> void:
	rect(x0, y0, x1, y1, C.line)
	rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill)

func same(a: Color, b: Color) -> bool:
	return a.is_equal_approx(b)

func shade(pal: Array, t: float) -> Color:
	if t < 0.12: return pal[0]
	if t < 0.32: return pal[1]
	if t < 0.62: return pal[2]
	if t < 0.84: return pal[3]
	return pal[4]

# A shape of pixels, filled, its rim outlined.
func solid(pix: Dictionary) -> void:
	for p in pix:
		var edge = false
		for d in DIRS:
			if not pix.has(p + d):
				edge = true
		setp(p.x, p.y, C.line if edge else pix[p])

# Columns [x, top, bottom], each shaded top to bottom as a round body lit from above.
func columns(cols: Array, pal: Array) -> void:
	var pix = {}
	for c in cols:
		for y in range(c[1], c[2] + 1):
			pix[Vector2i(c[0], y)] = shade(pal, float(y - c[1]) / maxi(1, c[2] - c[1]))
	solid(pix)

func capsule(x0: int, x1: int, y0: int, y1: int, k: int, pal: Array) -> void:
	var cols = []
	for x in range(x0, x1 + 1):
		var e = mini(x - x0, x1 - x)
		var inset = 0
		if e < k:
			inset = roundi(k - sqrt(k * k - (k - e) * (k - e)))
		cols.append([x, y0 + inset, y1 - inset])
	columns(cols, pal)

# A nozzle from its throat (xt, half-height r0) to its exit (xe, r1); p > 1 bells it.
func bell(xt: int, xe: int, ay: int, r0: int, r1: int, p: float, pal: Array) -> void:
	var cols = []
	for x in range(mini(xe, xt), maxi(xe, xt) + 1):
		var t = float(absi(xt - x)) / absi(xt - xe)
		cols.append([x, ay - roundi(r0 + (r1 - r0) * pow(t, p)), ay + roundi(r0 + (r1 - r0) * pow(t, p))])
	columns(cols, pal)

# Recolours the inside of the shape's column at x (found from y), as a band round it.
func band(x: int, y: int, pal: Array) -> void:
	var top = y
	var bot = y
	while getp(x, top - 1).a > 0:
		top -= 1
	while getp(x, bot + 1).a > 0:
		bot += 1
	for yy in range(top + 1, bot):
		if not same(getp(x, yy), C.line):
			setp(x, yy, shade(pal, float(yy - top) / maxi(1, bot - top)))

func seam(x: int, y0: int, y1: int) -> void:
	for y in range(y0, y1 + 1):
		if getp(x, y).a > 0:
			setp(x, y, C.line)

func pipe_h(x0: int, x1: int, y: int, pal: Array) -> void:
	rect(x0, y, x1, y, C.line)
	rect(x0, y + 1, x1, y + 1, pal[0])
	rect(x0, y + 2, x1, y + 2, pal[3])
	rect(x0, y + 3, x1, y + 3, C.line)

func pipe_v(x: int, y0: int, y1: int, pal: Array) -> void:
	rect(x, y0, x, y1, C.line)
	rect(x + 1, y0, x + 1, y1, pal[0])
	rect(x + 2, y0, x + 2, y1, pal[3])
	rect(x + 3, y0, x + 3, y1, C.line)

func rivet(x: int, y: int) -> void:
	setp(x, y, C.hi)
	setp(x + 1, y + 1, C.dark2)

func save(dir: String, name: String) -> Image:
	img.save_png(dir + "/" + name + ".png")
	print("wrote ", name)
	return img

# The engine ----------------------------------------------------------------------------------------------------

# The firebox's grille, dark till it burns.
const GX0 = 57
const GX1 = 66
const GY0 = 10
const GY1 = 16

func engine() -> void:
	blank(72, 32)
	# The skid and its feet.
	box(37, 28, 42, 31, C.dark2)
	box(64, 28, 69, 31, C.dark2)
	box(35, 26, 71, 29, C.dark)
	rect(36, 27, 70, 27, C.mid)
	# The block, lit from above and the left; a hazard band along its foot.
	box(38, 5, 71, 26, C.mid)
	rect(39, 6, 70, 6, C.hi)
	rect(39, 7, 39, 24, C.steel)
	rect(70, 7, 70, 24, C.mid2)
	rect(39, 25, 70, 25, C.dark)
	for y in range(21, 24):
		for x in range(39, 71):
			setp(x, y, C.amber if ((x + y) / 2) % 2 == 0 else C.line)
	rect(39, 20, 70, 20, C.line)
	rect(39, 24, 70, 24, C.line)
	seam(53, 7, 19)
	# The firebox door.
	box(55, 8, 68, 18, C.dark2)
	for y in range(GY0, GY1 + 1):
		for x in range(GX0, GX1 + 1):
			setp(x, y, C.line if x % 2 == 1 else C.deep)
	for p in [[41, 8], [50, 8], [41, 17], [50, 17], [55, 7], [67, 7]]:
		rivet(p[0], p[1])
	# A pressure gauge.
	var dial = {}
	for y in range(9, 16):
		for x in range(43, 50):
			if (x - 46) * (x - 46) + (y - 12) * (y - 12) <= 10:
				dial[Vector2i(x, y)] = C.white
	solid(dial)
	setp(46, 12, C.line)
	setp(47, 11, C.red)
	setp(48, 10, C.red)
	# The filler cap and a short stack.
	box(61, 3, 64, 5, C.dark2)
	box(60, 0, 65, 3, C.cu)
	rect(61, 1, 64, 1, C.cuHi)
	box(47, 0, 51, 5, C.dark2)
	rect(48, 1, 50, 1, C.deep)
	# A copper hose from the block to the nozzle's collar inside.
	pipe_h(29, 44, 1, COPPER)
	pipe_v(29, 1, 6, COPPER)
	box(29, 1, 32, 4, C.cu)
	# The cone nozzle out through the wall: a flange at the throat, a collar inside, one at its tip.
	bell(37, 1, AXIS, 5, EXIT_R, 1.7, SCORCH)
	columns([[35, 6, 24], [36, 6, 24], [37, 6, 24]], DARK)
	for x in [30, 31, 5, 6]:
		var r = roundi(5 + (EXIT_R - 5) * pow(float(37 - x) / 36.0, 1.7)) + 1
		columns([[x, AXIS - r, AXIS + r]], STEEL)
	band(2, AXIS, LIP)

# For when it runs: the grille and the throat lit.
func glow(base: Image) -> void:
	blank(72, 32)
	for y in range(GY0, GY1 + 1):
		for x in range(GX0, GX1 + 1):
			if x % 2 == 0:
				setp(x, y, HOT[clampi((y - GY0 + 1) / 2, 0, 3)])
	for y in range(6, 25):
		for x in range(32, 38):
			var c = base.get_pixel(x, y)
			if c.a > 0 and not same(c, C.line):
				setp(x, y, HOT[clampi(int(4.0 * (x - 32) / 6.0), 0, 3)])

func noise(x: int, y: int, f: int) -> float:
	var v = sin(x * 12.9898 + y * 78.233 + f * 37.719) * 43758.5453
	return v - floor(v)

# A smoky flame leaving the nozzle at its right edge, at row EXIT_R + 3, burning to the left.
func flame(frame: int) -> Image:
	var r = EXIT_R
	var ay = r + 3
	blank(52, 2 * r + 7)
	var n = 40 + [0, 3, -2][frame % 3]
	for d in range(n):
		var z = float(d) / n
		var hh = r * pow(1.0 - z, 0.55) * (1.0 + 0.1 * sin(d * 0.8 + frame * 2.3))
		var x = 51 - d
		for y in range(ay - ceili(hh), ay + ceili(hh) + 1):
			var q = absf(y - ay) / maxf(hh, 0.6)
			if q > 1.0:
				continue
			var k = noise(x, y, frame)
			if q > 0.75 and k < 0.35:
				continue
			var h = (1.0 - q) * (1.0 - 0.9 * z) + (k - 0.5) * 0.14
			var i = 4
			if h > 0.72: i = 0
			elif h > 0.52: i = 1
			elif h > 0.34: i = 2
			elif h > 0.18: i = 3
			var c = FIRE[i]
			c.a = [1.0, 1.0, 0.95, 0.85, 0.6][i]
			if z > 0.6 and i >= 3:
				c = Color(0.42, 0.45, 0.52, 0.55 - 0.4 * (z - 0.6))
			setp(x, y, c)
	return img

# The parts' icons, 16x16 ----------------------------------------------------------------------------------------

func nozzle_icon() -> void:
	blank(16, 16)
	bell(12, 1, 8, 3, 6, 1.3, SCORCH)
	columns([[12, 3, 13], [13, 3, 13], [14, 3, 13]], DARK)
	band(7, 8, STEEL)
	band(2, 8, LIP)
	rivet(13, 5)

func pump_icon() -> void:
	blank(16, 16)
	pipe_v(2, 1, 6, STEEL)
	pipe_v(10, 1, 6, STEEL)
	capsule(1, 14, 5, 14, 3, STEEL)
	for x in [5, 6, 9, 10]:
		band(x, 9, COPPER)
	box(2, 0, 5, 2, C.mid2)
	box(10, 0, 13, 2, C.mid2)

func igniter_icon() -> void:
	blank(16, 16)
	# A copper holder, a crystal in it, and its spark.
	var holder = []
	for x in range(3, 13):
		holder.append([x, 10, 14])
	columns(holder, COPPER)
	var gem = {}
	for y in range(2, 12):
		var half = mini(y - 2, 11 - y) / 2 + 1
		for x in range(8 - half, 8 + half):
			gem[Vector2i(x, y)] = C.cyan if x < 8 else C.cyanDim
	solid(gem)
	setp(7, 5, C.cyanHi)
	for p in [[12, 2], [13, 1], [3, 3], [2, 2]]:
		setp(p[0], p[1], C.amber)

# Preview --------------------------------------------------------------------------------------------------------

func over(base: Image, top: Image) -> Image:
	var out = base.duplicate()
	out.blend_rect(top, Rect2i(0, 0, top.get_width(), top.get_height()), Vector2i.ZERO)
	return out

func put_engine(dst: Image, eng: Image, fire: Image, x: int, y: int) -> void:
	if fire != null:
		dst.blend_rect(fire, Rect2i(0, 0, fire.get_width(), fire.get_height()), Vector2i(x + 1 - fire.get_width(), y + AXIS - EXIT_R - 3))
	dst.blend_rect(eng, Rect2i(0, 0, 72, 32), Vector2i(x, y))

func preview(path: String, eng: Image, lit: Image, fires: Array, icons: Array) -> void:
	var sheet = Image.create_empty(4 + 3 * 128 + 172, 64, false, Image.FORMAT_RGBA8)
	sheet.fill(Color("11141b"))
	put_engine(sheet, eng, null, 56, 4)
	put_engine(sheet, lit, fires[0], 56 + 128, 4)
	put_engine(sheet, lit, fires[1], 56 + 256, 4)
	for i in icons.size():
		sheet.blend_rect(icons[i], Rect2i(0, 0, 16, 16), Vector2i(60 + 24 * i, 42))
	# Aboard: the stern's wall two cells thick, the engine against it, space past it.
	var s = Image.create_empty(168, 56, false, Image.FORMAT_RGBA8)
	s.fill(Color("05070b"))
	s.fill_rect(Rect2i(40, 0, 128, 56), Color("2a303c"))
	s.fill_rect(Rect2i(24, 0, 144, 4), C.mid)
	s.fill_rect(Rect2i(24, 48, 144, 8), C.mid)
	s.fill_rect(Rect2i(24, 48, 144, 1), C.hi)
	s.fill_rect(Rect2i(24, 0, 16, 56), C.mid2)
	s.fill_rect(Rect2i(24, 0, 1, 56), C.line)
	put_engine(s, lit, fires[2], 40 - REACH, 16)
	sheet.blend_rect(s, Rect2i(0, 0, 168, 56), Vector2i(4 + 3 * 128, 4))
	sheet.resize(sheet.get_width() * 4, sheet.get_height() * 4, Image.INTERPOLATE_NEAREST)
	sheet.save_png(path)
	print("wrote ", path)

func _init():
	var args = OS.get_cmdline_user_args()
	var dir = args[0]
	engine()
	var eng = save(dir, "machine_engine")
	var icons = []
	nozzle_icon()
	icons.append(save(dir + "/items", "nozzle"))
	pump_icon()
	icons.append(save(dir + "/items", "fuelPump"))
	igniter_icon()
	icons.append(save(dir + "/items", "igniter"))
	if args.size() > 1:
		glow(eng)
		var lit = over(eng, img)
		var fires = []
		for f in 3:
			fires.append(flame(f))
		preview(args[1], eng, lit, fires, icons)
	quit()
