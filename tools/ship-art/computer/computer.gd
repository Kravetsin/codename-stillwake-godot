extends SceneTree
# Placeholder art for the ship's computer and the radar antenna on the roof, in the ship machines' palette.
# The computer, 48x32 (6x4 cells): a monitor on a desk over a cabinet, a control panel with the antenna's jack on the
# right, a rack of lights on the left. Overlays: its screen idle (4 frames) and on radar (8 frames), the rack's lights
# (2 frames). The antenna, 32x32 (4x4 cells): a mast on a rotator bolted to the hull, its dish turning (8 frames,
# drawn as a 3D paraboloid seen from the side), a beacon. Also writes a preview sheet at 6x when given a path.
#   godot --headless --script tools/ship-art/computer/computer.gd -- <artDir> [preview.png]

const C = {
	"line": Color("1a1e25"), "deep": Color("10161d"), "glass": Color("1f2b36"),
	"hi": Color("9aa0ab"), "steel": Color("8f99ad"), "mid": Color("7f848f"), "mid2": Color("6b7385"),
	"dark": Color("5b6170"), "dark2": Color("474c57"), "white": Color("d8dde6"),
	"cu": Color("b07a50"), "cu2": Color("8a5a3a"), "cuHi": Color("c98a55"),
	"greenDim": Color("2a5a3a"), "green": Color("6fe08a"), "amberDim": Color("6a5426"), "amber": Color("f0c050"),
	"cyanDim": Color("2f5f78"), "cyan": Color("7fc4f0"), "cyanHi": Color("a8e6ff"),
	"redDim": Color("5a2424"), "red": Color("ff5a4a"),
	"termBg": Color("0c1a24"), "radarOut": Color("08140f"), "radarBg": Color("0b1f16"), "radarGrid": Color("1d4a30"),
	"sweep0": Color("7dffa8"), "sweep1": Color("3fbf6e"), "sweep2": Color("26804a"), "blip": Color("ffe08a"),
}
const CLEAR = Color(0, 0, 0, 0)

var img: Image

func blank(w: int, h: int) -> void:
	img = Image.create_empty(w, h, false, Image.FORMAT_RGBA8)
	img.fill(CLEAR)

func setp(x: int, y: int, c: Color) -> void:
	if x >= 0 and y >= 0 and x < img.get_width() and y < img.get_height():
		img.set_pixel(x, y, c)

func filled(x: int, y: int) -> bool:
	return x >= 0 and y >= 0 and x < img.get_width() and y < img.get_height() and img.get_pixel(x, y).a > 0

func rect(x0: int, y0: int, x1: int, y1: int, c: Color) -> void:
	for y in range(y0, y1 + 1):
		for x in range(x0, x1 + 1):
			setp(x, y, c)

func box(x0: int, y0: int, x1: int, y1: int, fill: Color) -> void:
	rect(x0, y0, x1, y1, C.line)
	rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill)

func save(dir: String, name: String) -> Image:
	img.save_png(dir + "/" + name + ".png")
	print("wrote ", name)
	return img

# The computer --------------------------------------------------------------------------------------------------------

# The screen's inside, where the overlays draw.
const SX0 = 10
const SY0 = 3
const SX1 = 29
const SY1 = 11

func computer() -> void:
	blank(48, 32)
	# Feet, the cabinet lit from above and the left.
	box(4, 29, 9, 31, C.dark2)
	box(38, 29, 43, 31, C.dark2)
	box(2, 19, 45, 29, C.mid)
	rect(3, 20, 44, 20, C.hi)
	rect(3, 21, 3, 27, C.steel)
	rect(44, 21, 44, 27, C.mid2)
	rect(3, 28, 44, 28, C.dark)
	# The rack on the left: a dark window with three rows of lights, unlit.
	box(5, 21, 12, 27, C.deep)
	for row in [22, 24, 26]:
		for x in [7, 9, 11]:
			setp(x, row, C.amberDim if x == 11 else C.greenDim)
	# Two doors in the middle.
	box(15, 21, 30, 27, C.mid2)
	rect(16, 22, 29, 22, C.steel)
	rect(22, 22, 22, 26, C.line)
	setp(20, 24, C.hi)
	setp(24, 24, C.hi)
	# Vents on the right.
	for y in [22, 24, 26]:
		rect(33, y, 42, y, C.line)
		rect(33, y + 1, 42, y + 1, C.dark)
	# The stand, then the desk's slab over the cabinet.
	box(17, 13, 22, 18, C.dark2)
	rect(19, 14, 20, 17, C.dark)
	box(0, 17, 47, 19, C.mid)
	rect(1, 18, 46, 18, C.hi)
	# The monitor, its glass dark with a glint.
	box(6, 0, 33, 14, C.mid)
	rect(7, 1, 32, 1, C.hi)
	rect(7, 2, 7, 12, C.steel)
	rect(32, 2, 32, 12, C.mid2)
	rect(7, 13, 32, 13, C.dark)
	box(SX0 - 1, SY0 - 1, SX1 + 1, SY1 + 1, C.deep)
	for i in 3:
		setp(SX1 - 3 + i, SY0 + i, C.glass)
	setp(30, 13, C.greenDim)
	# The control panel on the right: keys and knobs, and the antenna's jack in copper.
	box(35, 13, 46, 17, C.dark2)
	for x in range(37, 45, 2):
		setp(x, 15, C.mid)
	setp(37, 15, C.amberDim)
	setp(43, 15, C.cyanDim)
	setp(38, 14, C.hi)
	setp(42, 14, C.hi)
	box(41, 9, 46, 13, C.dark2)
	rect(42, 10, 45, 12, C.cu2)
	rect(43, 11, 44, 11, C.cu)
	setp(43, 10, C.cuHi)

## The screen idle: lines of text scrolling up, a cursor blinking at the last.
func computer_screen(frame: int) -> void:
	blank(48, 32)
	rect(SX0, SY0, SX1, SY1, C.termBg)
	var lengths = [12, 7, 15, 9, 13, 5, 11, 8]
	var last = 0
	for row in 4:
		var n = lengths[(frame + row) % lengths.size()]
		var y = SY0 + 1 + row * 2
		for i in n:
			# Words: a gap every few letters.
			if (i + row * 3) % 5 != 4:
				setp(SX0 + 1 + i, y, C.cyan if row < 3 else C.cyanHi)
		last = n
	if frame % 2 == 0:
		rect(SX0 + 2 + last, SY0 + 6, SX0 + 3 + last, SY0 + 7, C.cyanHi)

## The radar: a half disc on the screen's floor, rings, a beam sweeping across it, blips lit as it passes.
func computer_radar(frame: int) -> void:
	blank(48, 32)
	var cx = (SX0 + SX1 + 1) / 2.0
	var cy = SY1 + 1.0
	var r_max = (SX1 - SX0 + 1) / 2.0
	var beam = PI * (frame + 0.5) / 8.0
	for y in range(SY0, SY1 + 1):
		for x in range(SX0, SX1 + 1):
			var dx = x + 0.5 - cx
			var dy = cy - (y + 0.5)
			var r = sqrt(dx * dx + dy * dy)
			if r > r_max:
				setp(x, y, C.radarOut)
				continue
			var col = C.radarBg
			if abs(r - r_max / 2.0) < 0.5 or r > r_max - 0.8 or y == SY1:
				col = C.radarGrid
			var behind = beam - atan2(dy, dx)
			if behind >= 0:
				if behind < 0.18:
					col = C.sweep0
				elif behind < 0.45:
					col = C.sweep1
				elif behind < 0.9 and col == C.radarBg:
					col = C.sweep2
			setp(x, y, col)
	for b in [Vector2i(14, 6), Vector2i(25, 8), Vector2i(22, 4)]:
		var since = beam - atan2(cy - (b.y + 0.5), b.x + 0.5 - cx)
		if since >= 0 and since < 1.6:
			setp(b.x, b.y, C.blip)
			if since < 0.8:
				setp(b.x + 1, b.y, C.blip)

## The rack's lights blinking, two patterns.
func computer_lights(frame: int) -> void:
	blank(48, 32)
	var on = [[1, 0, 1, 1, 1, 0, 0, 1, 1], [0, 1, 1, 1, 0, 1, 1, 1, 0]][frame]
	var i = 0
	for row in [22, 24, 26]:
		for x in [7, 9, 11]:
			if on[i] == 1:
				setp(x, row, C.amber if x == 11 else C.green)
			i += 1
	setp(30, 13, C.green)

# The antenna --------------------------------------------------------------------------------------------------------

# Where the dish turns: its vertex sits on the mast's top.
const DX = 16.0
const DY = 11.0
const R = 8.0
const F = 4.5

func antenna_mast() -> void:
	blank(32, 32)
	# The flange bolted to the hull, the rotator with its copper ring, the beacon unlit, the mast.
	box(7, 28, 24, 31, C.mid)
	rect(8, 29, 23, 29, C.hi)
	for x in [9, 13, 18, 22]:
		setp(x, 30, C.dark2)
	box(11, 23, 20, 28, C.dark2)
	rect(12, 24, 19, 24, C.mid2)
	rect(12, 26, 19, 26, C.cu)
	setp(13, 26, C.cuHi)
	setp(18, 24, C.redDim)
	box(14, int(DY), 17, 23, C.mid2)
	rect(15, int(DY) + 1, 15, 22, C.steel)

## The dish turned by theta about the mast: 0 faces right, PI/2 the viewer, PI left, 3PI/2 away.
func antenna_dish(theta: float) -> void:
	blank(32, 32)
	var z = []
	z.resize(32 * 32)
	z.fill(-1e9)
	var light = Vector3(-0.45, 0.65, 0.6).normalized()
	var front = [C.dark, C.mid2, C.mid, C.steel, C.hi]
	var back = [C.dark2, C.dark, C.mid2, C.mid]
	var ct = cos(theta)
	var st = sin(theta)
	var u = 0.0
	while u <= R:
		var phi = 0.0
		while phi < TAU:
			var p = Vector3(u * u / (4.0 * F) - 1.0, u * cos(phi), u * sin(phi))
			var n = Vector3(1.0, -p.y / (2.0 * F), -p.z / (2.0 * F)).normalized()
			var rp = Vector3(p.x * ct - p.z * st, p.y, p.x * st + p.z * ct)
			var rn = Vector3(n.x * ct - n.z * st, n.y, n.x * st + n.z * ct)
			var x = int(floor(DX + rp.x))
			var y = int(floor(DY - rp.y))
			if x >= 0 and y >= 0 and x < 32 and y < 32 and rp.z > z[y * 32 + x]:
				z[y * 32 + x] = rp.z
				var col: Color
				if u > R - 0.6:
					col = C.dark2
				elif rn.z >= 0:
					col = front[clampi(int((rn.dot(light) * 0.5 + 0.5) * front.size()), 0, front.size() - 1)]
				else:
					col = back[clampi(int(((-rn).dot(light) * 0.5 + 0.5) * back.size()), 0, back.size() - 1)]
				setp(x, y, col)
			phi += 0.01
		u += 0.05
	# The silhouette outlined.
	var edge = []
	for y in 32:
		for x in 32:
			if not filled(x, y) and (filled(x - 1, y) or filled(x + 1, y) or filled(x, y - 1) or filled(x, y + 1)):
				edge.append(Vector2i(x, y))
	for e in edge:
		setp(e.x, e.y, C.line)
	# The boom from the dish's vertex to its focus, and the feed horn there, each hidden behind the dish where it is.
	var t = 0.0
	while t <= 1.0:
		var lx = -1.0 + (F - 1.0 + 1.0) * t
		var bx = lx * ct
		var bz = lx * st
		var x = int(floor(DX + bx))
		var y = int(floor(DY))
		if x >= 0 and x < 32 and (bz > z[y * 32 + x] or not filled(x, y)):
			setp(x, y, C.dark2 if t < 0.9 else C.cuHi)
			if t >= 0.9:
				setp(x, y + 1, C.cu)
				if not filled(x, y - 1):
					setp(x, y - 1, C.line)
				if not filled(x, y + 2):
					setp(x, y + 2, C.line)
		t += 0.02

func antenna_beacon() -> void:
	blank(32, 32)
	var halo = Color(1.0, 0.35, 0.3, 0.4)
	setp(18, 24, C.red)
	for d in [Vector2i(1, 0), Vector2i(-1, 0), Vector2i(0, 1), Vector2i(0, -1)]:
		setp(18 + d.x, 24 + d.y, halo)

# The parts' icons, 16x16 ----------------------------------------------------------------------------------------------

## The processor: a dark chip with copper pins round it and a lit die in its middle.
func processor_icon() -> void:
	blank(16, 16)
	for i in range(4, 12, 2):
		for p in [Vector2i(i, 1), Vector2i(i, 14), Vector2i(1, i), Vector2i(14, i)]:
			setp(p.x, p.y, C.cu)
		rect(i, 2, i, 2, C.cu2)
		rect(i, 13, i, 13, C.cu2)
		rect(2, i, 2, i, C.cu2)
		rect(13, i, 13, i, C.cu2)
	box(3, 3, 12, 12, C.dark2)
	rect(4, 4, 11, 4, C.dark)
	box(6, 6, 9, 9, C.deep)
	rect(7, 7, 8, 8, C.cyanDim)
	setp(7, 7, C.cyan)
	setp(4, 11, C.greenDim)

## The display: a small screen in a steel frame on a stand, lines of text lit on it.
func display_icon() -> void:
	blank(16, 16)
	box(5, 13, 10, 15, C.dark2)
	rect(7, 11, 8, 13, C.dark)
	box(0, 1, 15, 12, C.mid)
	rect(1, 2, 14, 2, C.hi)
	box(2, 3, 13, 10, C.termBg)
	rect(4, 5, 9, 5, C.cyan)
	rect(4, 7, 11, 7, C.cyanDim)
	rect(4, 9, 7, 9, C.cyanDim)
	setp(12, 4, C.glass)

## The transceiver: a cased coil of copper with a rod up its side, a red tip and the signal's arcs.
func transceiver_icon() -> void:
	blank(16, 16)
	box(1, 7, 12, 15, C.dark2)
	rect(2, 8, 11, 8, C.mid2)
	box(3, 10, 8, 13, C.cu2)
	for x in [4, 6, 8]:
		rect(x, 11, x, 12, C.cuHi)
	setp(10, 11, C.greenDim)
	setp(10, 13, C.amberDim)
	rect(11, 2, 11, 6, C.mid)
	setp(11, 1, C.red)
	setp(13, 1, C.cyanDim)
	setp(14, 2, C.cyanDim)
	setp(14, 3, C.cyanDim)
	setp(13, 4, C.cyanDim)
	setp(9, 1, C.cyanDim)
	setp(8, 2, C.cyanDim)
	setp(8, 3, C.cyanDim)
	setp(9, 4, C.cyanDim)

# Preview ------------------------------------------------------------------------------------------------------------

func over(base: Image, top: Image) -> Image:
	var out = base.duplicate()
	out.blend_rect(top, Rect2i(0, 0, top.get_width(), top.get_height()), Vector2i.ZERO)
	return out

func preview(path: String, pc: Image, idle: Array, radar: Array, lights: Array, mast: Image, dishes: Array, beacon: Image) -> void:
	const S = 6
	var sheet = Image.create_empty(64 * 8 + 8, 200, false, Image.FORMAT_RGBA8)
	sheet.fill(Color("1c2230"))
	var put = func(src: Image, x: int, y: int) -> void:
		sheet.blend_rect(src, Rect2i(0, 0, src.get_width(), src.get_height()), Vector2i(x, y))
	# Row 1: the computer off, idle, and on radar.
	var x = 4
	for frame in [pc, over(over(pc, idle[0]), lights[0]), over(over(pc, idle[1]), lights[1]), over(pc, radar[1]), over(pc, radar[3]), over(over(pc, radar[5]), lights[0]), over(pc, radar[7])]:
		put.call(frame, x, 4)
		x += 52
	# Row 2: the antenna turning, its beacon on every other frame.
	x = 4
	for i in dishes.size():
		var a = over(mast, dishes[i])
		if i % 2 == 0:
			a = over(a, beacon)
		put.call(a, x, 44)
		x += 40
	# Row 3: aboard, the antenna on the roof over a room with the computer beside the assembler for scale.
	var y0 = 84
	sheet.fill_rect(Rect2i(4, y0 + 32, 200, 6), C.line)
	sheet.fill_rect(Rect2i(4, y0 + 33, 200, 4), C.mid)
	sheet.fill_rect(Rect2i(4, y0 + 33, 200, 1), C.hi)
	sheet.fill_rect(Rect2i(4, y0 + 38, 200, 40), Color("2a303c"))
	sheet.fill_rect(Rect2i(4, y0 + 78, 200, 6), C.line)
	sheet.fill_rect(Rect2i(4, y0 + 79, 200, 4), C.mid)
	sheet.fill_rect(Rect2i(4, y0 + 79, 200, 1), C.hi)
	put.call(over(over(mast, dishes[1]), beacon), 30, y0)
	put.call(over(over(pc, radar[4]), lights[0]), 20, y0 + 46)
	var assembler = Image.load_from_file("res://game/art/machine_assembler.png")
	if assembler != null:
		put.call(assembler, 90, y0 + 46)
	sheet.resize(sheet.get_width() * S, sheet.get_height() * S, Image.INTERPOLATE_NEAREST)
	sheet.save_png(path)
	print("wrote ", path)

func _init():
	var args = OS.get_cmdline_user_args()
	var dir = args[0]
	processor_icon()
	save(dir + "/items", "processor")
	display_icon()
	save(dir + "/items", "display")
	transceiver_icon()
	save(dir + "/items", "transceiver")
	computer()
	var pc = save(dir, "machine_computer")
	var idle = []
	for i in 4:
		computer_screen(i)
		idle.append(save(dir, "computer_screen_%d" % i))
	var radar = []
	for i in 8:
		computer_radar(i)
		radar.append(save(dir, "computer_radar_%d" % i))
	var lights = []
	for i in 2:
		computer_lights(i)
		lights.append(save(dir, "computer_lights_%d" % i))
	antenna_mast()
	var mast = save(dir, "radarAntenna_mast")
	var dishes = []
	for i in 8:
		antenna_dish(i * TAU / 8.0)
		dishes.append(save(dir, "radarAntenna_dish_%d" % i))
	antenna_beacon()
	var beacon = save(dir, "radarAntenna_beacon")
	# The menu's picture: the whole antenna, the dish three quarters to the viewer.
	img = over(mast, dishes[1])
	save(dir, "machine_radarAntenna")
	if args.size() > 1:
		preview(args[1], pc, idle, radar, lights, mast, dishes, beacon)
	quit()
