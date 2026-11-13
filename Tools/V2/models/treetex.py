"""
Paints the trees' textures (Assets/GuWorld/Resources/Trees): tiling barks with their height
maps, moss, and the foliage atlases — a spray of leaves, needles or blossom on its twig, with
alpha — that the tree generator (Core/Gen/Flora) lays on its trunks and hangs on its twigs.

  python treetex.py [out_dir] [only,names]     (needs: pip install numpy scipy pillow)

Barks: <name>.jpg (colour, 2048², tiles both ways) and <name>_h.jpg (height: white is proud).
With `normals` as a third argument, <name>_n.png normal maps are written too (for the GLB viewer;
the game's shader bumps from the height map itself).
Foliage: <name>.png (RGBA, 2048²). A spray's twig enters at the bottom middle of the picture;
the hanging ones (willow, lichen) hang from the top middle.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage
from scipy.spatial import cKDTree

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Trees'))
SIZE = 2048


# ---------------------------------------------------------------------------- Tiling noise

def noise(rng, gx, gy, size=SIZE):
    """Smooth tiling noise 0-1: a random grid of gx × gy cells, wrapped."""
    grid = rng.random((gy, gx)).astype(np.float32)
    out = ndimage.zoom(grid, (size / gy, size / gx), order=3, mode='grid-wrap', grid_mode=True)
    return np.clip(out, 0, 1)


def fbm(rng, gx, gy, octaves=5, gain=0.5, size=SIZE):
    total = np.zeros((size, size), np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        cx, cy = min(size // 2, gx * 2 ** o), min(size // 2, gy * 2 ** o)
        total += amp * noise(rng, cx, cy, size)
        norm += amp
        amp *= gain
    return total / norm


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def voronoi(rng, count, stretch=1.0, size=SIZE, warp=None):
    """Tiling Worley noise: for every pixel the distances to its nearest two points and the nearest's number.
    `stretch` above 1 makes the cells that many times taller than wide. `warp`: two tiling fields
    (x, y, as shares of the tile) the pixels are moved by first, so the cells' edges wander."""
    pts = rng.random((count, 2))
    tiles = np.concatenate([pts + (dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)])
    ids = np.tile(np.arange(count), 9)
    tree = cKDTree(tiles * (1, 1 / stretch))
    ys, xs = np.mgrid[0:size, 0:size]
    px, py = (xs + 0.5) / size, (ys + 0.5) / size
    if warp is not None:
        px, py = (px + warp[0]) % 1.0, (py + warp[1]) % 1.0
    q = np.stack([px.ravel(), py.ravel() / stretch], axis=1)
    d, i = tree.query(q, k=2, workers=-1)
    return d[:, 0].reshape(size, size).astype(np.float32), d[:, 1].reshape(size, size).astype(np.float32), ids[i[:, 0]].reshape(size, size)


def edge_distance(f1, f2):
    """How far each pixel is from its Worley cell's edge, as a share of the tile — true on the
    picture whatever the cells' stretch or warp (the plain f2 - f1 is several times too wide
    across a stretched cell's ends, which paints blobs where furrows should stay narrow)."""
    e = f2 - f1
    n = e.shape[0]
    gx = (np.roll(e, -1, 1) - np.roll(e, 1, 1)) * (n / 2)
    gy = (np.roll(e, -1, 0) - np.roll(e, 1, 0)) * (n / 2)
    return e / np.maximum(0.25, np.sqrt(gx * gx + gy * gy))


def mix(a, b, t):
    t = t[..., None] if t.ndim == 2 else t
    return a * (1 - t) + b * t


def colour(c):
    return np.array(c, np.float32)


def save_bark(name, rgb, height, out, normals):
    rgb = np.clip(rgb, 0, 1)
    height = np.clip(height, 0, 1)
    Image.fromarray((rgb * 255).astype(np.uint8), 'RGB').save(os.path.join(out, name + '.jpg'), quality=90)
    Image.fromarray((height * 255).astype(np.uint8), 'L').save(os.path.join(out, name + '_h.jpg'), quality=88)
    if normals:
        # Tangent-space normals from the height (wrapping), for viewers that want them.
        k = 9.0
        dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * k
        dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * k
        n = np.stack([-dx, dy, np.ones_like(dx)], axis=2)
        n /= np.linalg.norm(n, axis=2, keepdims=True)
        Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8), 'RGB').save(os.path.join(normals, name + '_n.png'))
    print(f'  {name}: bark {rgb.shape[1]}x{rgb.shape[0]}')


def shade_by_height(rgb, height, strength=0.55):
    """Cavities darker, crests lighter, and a soft light from above across the relief."""
    blurred = ndimage.gaussian_filter(height, 6, mode='wrap')
    cavity = np.clip((height - blurred) * 3.0, -1, 1)
    slope = (np.roll(height, 2, 0) - np.roll(height, -2, 0)) * 2.5
    lit = 1 + strength * (0.6 * cavity + 0.4 * np.clip(slope, -1, 1))
    return rgb * lit[..., None]


# ---------------------------------------------------------------------------- Barks

def ridged(rng, gx, gy, octaves=4, gain=0.55):
    """Interlacing ridges 0-1 (1 on the crests): folded noise, taller than wide when gy is smaller than gx."""
    total = np.zeros((SIZE, SIZE), np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        n = noise(rng, min(SIZE // 2, gx * 2 ** o), min(SIZE // 2, gy * 2 ** o))
        total += amp * (1 - np.abs(2 * n - 1))
        norm += amp
        amp *= gain
    return total / norm


def cracks(rng, gx, gy, width, octaves=2):
    """Thin long cracks 0-1 (1 in the crack): the level lines of stretched noise."""
    n = fbm(rng, gx, gy, octaves)
    return 1 - smooth(0.0, width, np.abs(n - 0.5))


def bark_pine(rng):
    """Old red pine: thick plates like a cracked riverbed, each a stack of thin flakes, deep dark furrows between."""
    f1, f2, ids = voronoi(rng, 420, stretch=2.4)
    warp = fbm(rng, 8, 8, 3)
    edge = (f2 - f1) * (0.75 + 0.5 * warp)
    plate = smooth(0.0012, 0.0065, edge)
    flakes = fbm(rng, 14, 7, 5)
    per = np.random.default_rng(7).random(4096).astype(np.float32)[ids % 4096]
    # Terraces: the flakes' layers step down toward the plate's edge.
    dome = smooth(0.0, 0.03, edge)
    level = (0.5 * dome + 0.5 * flakes) * 7
    layered = np.floor(level) / 7
    rim = np.abs(level % 1 - 0.5) > 0.44
    grain = fbm(rng, 300, 16, 3)
    height = plate * (0.5 + 0.4 * layered + 0.1 * grain) * (0.85 + 0.15 * per)
    warm = colour((0.58, 0.31, 0.19))
    violet = colour((0.43, 0.30, 0.27))
    pale = colour((0.70, 0.47, 0.33))
    base = mix(warm, violet, per)
    base = mix(base, pale, np.clip(layered * 1.2 - 0.4, 0, 1) * 0.55)
    base = base * (0.84 + 0.32 * grain[..., None])
    base[rim] *= 0.74
    crack = colour((0.13, 0.08, 0.06))
    rgb = mix(crack, base, smooth(0.1, 0.7, plate))
    # Grey lichen dust on some plates.
    dust = smooth(0.62, 0.8, fbm(rng, 5, 5, 4)) * plate
    rgb = mix(rgb, colour((0.55, 0.56, 0.50)), dust * 0.4)
    return shade_by_height(rgb, height, 0.45), height


def bark_broad(rng):
    """Camphor, maple, willow and the ancient trees: deeply furrowed bark — long corky ridges that
    part and rejoin like a net pulled downward, each split again by shallower fissures and cracked
    across into blocks; weathered grey on the crests, warm brown on the flanks, dark and a little
    green deep in the furrows; pale lichen on some crests."""
    # The ridges wander sideways as they run, so the furrows between weave.
    wx = (fbm(rng, 5, 3, 3) - 0.5) * 0.05 + (fbm(rng, 16, 5, 2) - 0.5) * 0.012
    wy = (fbm(rng, 4, 4, 2) - 0.5) * 0.08
    f1, f2, ids = voronoi(rng, 105, stretch=7.5, warp=(wx, wy))
    edge = edge_distance(f1, f2)
    # Furrows: wide and deep in places, pinched nearly shut in others.
    gape = 0.5 + 1.0 * fbm(rng, 7, 4, 3)
    furrow_w = 0.0105 * gape
    ridge = smooth(0.0, 1.0, edge / furrow_w)                       # 0 in the furrow's bed, 1 on the ridge's top
    crown = smooth(0.0, 0.021, edge)                                # the ridge's rounded back
    per = np.random.default_rng(11).random(4096).astype(np.float32)[ids % 4096]

    # Each ridge split along its length by shallower fissures...
    jx = (fbm(rng, 46, 46, 2) - 0.5) * 0.007
    jy = (fbm(rng, 46, 46, 2) - 0.5) * 0.007
    g1, g2, ids2 = voronoi(rng, 330, stretch=9.0, warp=(wx * 0.6 + jx, wy))
    fissure = 1 - (1 - smooth(0.0, 0.0034, edge_distance(g1, g2))) * smooth(0.35, 0.6, fbm(rng, 9, 5, 3))
    # ...and cracked across into blocks.
    c1, c2, _ = voronoi(rng, 520, stretch=0.45, warp=(wx + jx, wy * 0.3 + jy))
    cross = 1 - (1 - smooth(0.0, 0.0024, edge_distance(c1, c2))) * smooth(0.5, 0.68, fbm(rng, 10, 10, 3))
    per2 = np.random.default_rng(12).random(4096).astype(np.float32)[ids2 % 4096]

    fibre = fbm(rng, 420, 22, 3)                                    # the cork's long grain
    grit = fbm(rng, 260, 260, 3)
    lumps = fbm(rng, 24, 9, 4)
    top = 0.60 + 0.20 * crown + 0.10 * lumps + 0.16 * (per - 0.5) + 0.08 * (per2 - 0.5)
    top = top * (0.68 + 0.32 * fissure) * (0.84 + 0.16 * cross) + 0.05 * (fibre - 0.5) + 0.03 * (grit - 0.5)
    height = np.clip(ridge ** 0.8 * top, 0, 1)

    grey = colour((0.47, 0.44, 0.39))
    brown = colour((0.36, 0.27, 0.20))
    warm = colour((0.44, 0.31, 0.21))
    deep = colour((0.085, 0.062, 0.048))
    # Crests weather grey; flanks and fresh breaks stay warm.
    weather = smooth(0.35, 0.8, crown * (0.6 + 0.8 * fbm(rng, 6, 6, 4)))
    base = mix(mix(brown, warm, per[..., None]), grey, weather * 0.85)
    base = base * (0.74 + 0.52 * fibre[..., None]) * (0.9 + 0.2 * grit[..., None]) * (0.82 + 0.36 * per[..., None]) * (0.93 + 0.14 * per2[..., None])
    base = base * (0.56 + 0.44 * fissure[..., None]) * (0.7 + 0.3 * cross[..., None])
    rgb = mix(deep, mix(warm * 0.72, base, smooth(0.55, 1.0, ridge)), smooth(0.05, 0.5, ridge))
    # Damp green down in the furrows, here and there.
    damp = (1 - smooth(0.1, 0.7, ridge)) * smooth(0.42, 0.7, fbm(rng, 6, 6, 3))
    rgb = mix(rgb, colour((0.16, 0.22, 0.10)), damp * 0.5)
    # Lichen: pale grey-green crusts on some crests.
    l1, _, lid = voronoi(rng, 170)
    lr = np.random.default_rng(13).random(4096).astype(np.float32)[lid % 4096]
    crust = (1 - smooth(0.004 + 0.012 * lr, 0.007 + 0.016 * lr, l1 + 0.01 * (grit - 0.5))) * (lr > 0.6) * smooth(0.6, 0.9, ridge)
    rgb = mix(rgb, colour((0.58, 0.62, 0.52)) * (0.8 + 0.4 * grit[..., None]), crust * 0.5)
    return shade_by_height(rgb, height, 0.6), height


def bark_smooth(rng):
    """Plum, maple, cherry: tight dark bark with rows of pale raised lenticels and a few shallow splits."""
    body = fbm(rng, 10, 40, 5)
    fine = fbm(rng, 60, 400, 3)
    height = 0.5 + 0.2 * (body - 0.5) + 0.1 * (fine - 0.5)
    rgb = mix(colour((0.20, 0.15, 0.14)), colour((0.36, 0.27, 0.24)), body) * (0.85 + 0.3 * fine[..., None])
    # Lenticels: short horizontal dashes.
    f1, f2, ids = voronoi(rng, 2600, stretch=0.16)
    per = np.random.default_rng(3).random(8192).astype(np.float32)[ids % 8192]
    dash = (1 - smooth(0.0, 0.006, f1)) * (per > 0.45)
    rgb = mix(rgb, colour((0.60, 0.48, 0.38)), dash * 0.85)
    height += 0.25 * dash
    # Splits: long shallow vertical cracks, here and there.
    crack = cracks(rng, 14, 2, 0.012) * smooth(0.45, 0.65, fbm(rng, 5, 3, 3))
    rgb = mix(rgb, colour((0.07, 0.05, 0.05)), crack * 0.9)
    height -= 0.4 * crack
    # A silvery sheen in bands round the trunk.
    band = smooth(0.55, 0.8, fbm(rng, 3, 30, 3))
    rgb = mix(rgb, colour((0.48, 0.44, 0.42)), band * 0.25)
    return shade_by_height(rgb, np.clip(height, 0, 1), 0.4), np.clip(height, 0, 1)


def bark_birch(rng):
    """Birch: chalk-white paper bark, black eyes and dashes, curling edges, dark rough foot."""
    body = fbm(rng, 6, 30, 5)
    fine = fbm(rng, 40, 500, 3)
    rgb = mix(colour((0.86, 0.84, 0.78)), colour((0.70, 0.67, 0.60)), body) * (0.92 + 0.16 * fine[..., None])
    height = 0.6 + 0.1 * (body - 0.5)
    f1, f2, ids = voronoi(rng, 1500, stretch=0.12)
    per = np.random.default_rng(5).random(8192).astype(np.float32)[ids % 8192]
    dash = (1 - smooth(0.0, 0.007, f1)) * (per > 0.5)
    rgb = mix(rgb, colour((0.10, 0.09, 0.08)), dash * 0.9)
    height -= 0.2 * dash
    # Eyes: dark scars where branches fell.
    g1, g2, _ = voronoi(rng, 30, stretch=0.55)
    eye = 1 - smooth(0.004, 0.016, g1)
    rgb = mix(rgb, colour((0.08, 0.07, 0.06)), eye)
    height -= 0.35 * eye
    # Peeling: thin horizontal lines with a shadow under them.
    peel = np.abs(fbm(rng, 3, 60, 4) - 0.5) < 0.012
    rgb[peel] *= 0.55
    height[np.roll(peel, -3, 0)] += 0.12
    return shade_by_height(rgb, np.clip(height, 0, 1), 0.35), np.clip(height, 0, 1)


def bark_dead(rng):
    """Dead standing wood: the bark gone, silvered grain running up and bending round the knots,
    long drying checks that open and close, rot's brown stain, a few scales of old bark still on."""
    # The grain bends: slow sideways drift, and a swirl round each knot.
    wx = (fbm(rng, 3, 2, 3) - 0.5) * 0.06
    wy = np.zeros((SIZE, SIZE), np.float32)
    k1, _, kid = voronoi(rng, 14)
    kr = np.random.default_rng(21).random(64).astype(np.float32)[kid % 64]
    knot = (1 - smooth(0.0, 0.035 + 0.02 * kr, k1)) * (kr > 0.35)
    ys, xs = np.mgrid[0:SIZE, 0:SIZE]
    grain_x = ((xs + 0.5) / SIZE + wx + 0.03 * knot * np.sin(k1 * 240)) % 1.0
    # Fibres: fine lines running up, sampled where the bend puts them.
    lines = fbm(rng, 500, 4, 3)
    col = (grain_x * SIZE).astype(np.int32) % SIZE
    grain = lines[ys, col]
    broad = fbm(rng, 60, 3, 3)[ys, col]
    rings = 0.5 + 0.5 * np.sin(k1 * 520)                              # a knot's rings
    height = 0.62 + 0.22 * (broad - 0.5) + 0.22 * (grain - 0.5) + 0.1 * knot * (rings - 0.5) - 0.12 * smooth(0.6, 1.0, knot)
    silver = colour((0.60, 0.59, 0.56))
    ash = colour((0.45, 0.44, 0.42))
    stain = colour((0.40, 0.30, 0.21))
    weather = fbm(rng, 5, 4, 4)
    rgb = mix(ash, silver, smooth(0.3, 0.7, weather))
    rgb = mix(rgb, stain, smooth(0.55, 0.8, fbm(rng, 6, 3, 4)) * 0.6)
    rgb = rgb * (0.72 + 0.56 * grain[..., None]) * (0.9 + 0.2 * broad[..., None])
    rgb = mix(rgb, colour((0.25, 0.19, 0.14)) * (0.7 + 0.6 * rings[..., None]), knot * 0.75)
    # Checks: long cracks along the grain, widest in their middles, each closing to nothing.
    c1, c2, cid = voronoi(rng, 70, stretch=14.0, warp=(wx, wy))
    cr = np.random.default_rng(22).random(256).astype(np.float32)[cid % 256]
    d = edge_distance(c1, c2)
    open_ = smooth(0.35, 0.75, fbm(rng, 3, 9, 3)) * (0.4 + 0.9 * cr)
    check = (1 - smooth(0.0, 0.0012 + 0.0038 * open_, d)) * smooth(0.02, 0.2, open_)
    hair = cracks(rng, 40, 3, 0.006) * 0.5 * smooth(0.5, 0.7, fbm(rng, 8, 8, 2))
    crack = np.clip(np.maximum(check, hair), 0, 1)
    rgb = mix(rgb, colour((0.045, 0.04, 0.035)), crack)
    height = height - 0.55 * crack
    # Scales of old bark left in a few places, proud of the wood.
    s1, s2, sid = voronoi(rng, 900, stretch=1.6)
    patch = smooth(0.66, 0.72, fbm(rng, 5, 4, 4))
    seam = smooth(0.0, 0.004, edge_distance(s1, s2))
    sr = np.random.default_rng(23).random(1024).astype(np.float32)[sid % 1024]
    rgb = mix(rgb, colour((0.22, 0.17, 0.13)) * (0.7 + 0.6 * sr[..., None]) * (0.35 + 0.65 * seam[..., None]), patch * 0.92)
    height = height + 0.2 * patch * (0.3 + 0.7 * seam)
    height = np.clip(height, 0, 1)
    return shade_by_height(rgb, height, 0.5), height


def bark_bamboo(rng):
    """Bamboo: smooth green culm with fine fibres, a pale waxy bloom, brown specks."""
    fibre = fbm(rng, 500, 4, 3)
    slow = fbm(rng, 6, 6, 4)
    rgb = mix(colour((0.30, 0.42, 0.16)), colour((0.52, 0.60, 0.26)), slow) * (0.9 + 0.2 * fibre[..., None])
    rgb = mix(rgb, colour((0.72, 0.76, 0.62)), smooth(0.6, 0.85, fbm(rng, 5, 12, 4)) * 0.3)
    speck = fbm(rng, 200, 200, 2) > 0.74
    rgb[speck] = rgb[speck] * 0.6 + colour((0.2, 0.12, 0.05)) * 0.4
    height = 0.5 + 0.08 * (fibre - 0.5)
    return rgb, np.clip(height, 0, 1)


def moss(rng):
    """Moss: cushions and fronds, deep green in the hollows, yellow-green on the crowns, a few dead brown tufts."""
    f1, f2, ids = voronoi(rng, 5200)
    cushion = 1 - smooth(0.0, 0.012, f1)
    lumps = fbm(rng, 12, 12, 5)
    height = np.clip(0.25 + 0.5 * cushion * (0.5 + 0.5 * lumps) + 0.25 * lumps, 0, 1)
    star = fbm(rng, 500, 500, 2)
    per = np.random.default_rng(11).random(8192).astype(np.float32)[ids % 8192]
    deep = colour((0.07, 0.15, 0.04))
    mid = colour((0.20, 0.34, 0.08))
    bright = colour((0.42, 0.50, 0.14))
    rgb = mix(deep, mid, smooth(0.2, 0.6, height))
    rgb = mix(rgb, bright, smooth(0.6, 0.95, height) * (0.5 + 0.5 * per))
    rgb = mix(rgb, colour((0.34, 0.26, 0.10)), smooth(0.7, 0.85, fbm(rng, 7, 7, 4)) * 0.5)
    rgb = rgb * (0.8 + 0.4 * star[..., None])
    return shade_by_height(rgb, height, 0.5), height


BARKS = {'bark_pine': bark_pine, 'bark_broad': bark_broad, 'bark_smooth': bark_smooth, 'bark_birch': bark_birch, 'bark_dead': bark_dead, 'bark_bamboo': bark_bamboo, 'moss': moss}


# ---------------------------------------------------------------------------- Foliage

SS = 2            # supersampling
W = SIZE * SS


class Canvas:
    def __init__(self, seed):
        self.image = Image.new('RGBA', (W, W), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)
        self.rng = np.random.default_rng(seed)

    def rgb(self, c, k=1.0):
        return (int(max(0, min(255, c[0] * 255 * k))), int(max(0, min(255, c[1] * 255 * k))), int(max(0, min(255, c[2] * 255 * k))), 255)

    def line(self, pts, width, c, k=1.0):
        self.draw.line([(x * W, y * W) for x, y in pts], fill=self.rgb(c, k), width=max(1, int(width * W)), joint='curve')

    def poly(self, pts, c, k=1.0):
        self.draw.polygon([(x * W, y * W) for x, y in pts], fill=self.rgb(c, k))

    def disc(self, x, y, r, c, k=1.0):
        self.draw.ellipse([(x - r) * W, (y - r) * W, (x + r) * W, (y + r) * W], fill=self.rgb(c, k))

    def leaf(self, x, y, angle, length, width, c, k=1.0, point=1.0, veins=5, serrate=0.0):
        """A leaf from its stem end at (x, y), pointing along `angle` (radians, 0 = up the picture)."""
        dx, dy = math.sin(angle), -math.cos(angle)
        px, py = -dy, dx
        n = 14
        left, right = [], []
        for i in range(n + 1):
            t = i / n
            w = width * 0.5 * (math.sin(math.pi * t ** (0.75 / point)) ** 0.9) * (1 - 0.25 * t)
            if serrate and 0 < i < n:
                w *= 1 + serrate * (0.5 if i % 2 else -0.5)
            cx, cy = x + dx * length * t, y + dy * length * t
            left.append((cx + px * w, cy + py * w))
            right.append((cx - px * w, cy - py * w))
        mid = [(x + dx * length * i / n, y + dy * length * i / n) for i in range(n + 1)]
        # The two halves catch the light differently.
        self.poly(left + mid[::-1], c, k * 1.06)
        self.poly(right + mid[::-1], c, k * 0.9)
        vein = (c[0] * 0.75 + 0.2, c[1] * 0.8 + 0.2, c[2] * 0.7 + 0.1)
        self.line([mid[0], mid[-1]], 0.0009, vein, k)
        for v in range(1, veins + 1):
            t = v / (veins + 1)
            i = int(t * n)
            for edge in (left, right):
                j = min(n, i + 2)
                tip = (mid[i][0] + (edge[j][0] - mid[i][0]) * 0.85, mid[i][1] + (edge[j][1] - mid[i][1]) * 0.85)
                self.line([mid[i], tip], 0.0005, vein, k * 0.95)

    def finish(self, path, speckle=0.06):
        image = self.image.resize((SIZE, SIZE), Image.LANCZOS)
        a = np.asarray(image, np.float32) / 255
        rgb, alpha = a[..., :3], a[..., 3]
        # A little life in the flat fills.
        grain = np.random.default_rng(1).random((SIZE, SIZE)).astype(np.float32)
        grain = ndimage.gaussian_filter(grain, 1.2)
        grain = (grain - grain.mean()) / (grain.std() + 1e-6)
        rgb = np.clip(rgb * (1 + speckle * grain[..., None]), 0, 1)
        # Colour bled out under the transparent pixels, so mip-maps do not fringe dark.
        weight = alpha.copy()
        bleed = rgb * alpha[..., None]
        for sigma in (2, 6, 18, 60):
            bw = ndimage.gaussian_filter(weight, sigma)
            bc = np.stack([ndimage.gaussian_filter(bleed[..., i], sigma) for i in range(3)], axis=2)
            fill = bc / np.maximum(bw, 1e-5)[..., None]
            empty = (weight < 1e-3) & (bw > 1e-4)
            rgb[empty] = fill[empty]
            weight = np.where(empty, 1e-2, weight)
        rgb[weight < 1e-3] = rgb[alpha > 0.5].mean(axis=0) if (alpha > 0.5).any() else 0.3
        out = np.dstack([rgb, alpha])
        # 255 colours with alpha: the fills are flat, and the file is a fifth the size.
        full = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'RGBA')
        full.quantize(colors=255, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).save(path, optimize=True)
        cover = float((alpha > 0.5).mean())
        print(f'  {os.path.basename(path)}: foliage, {cover * 100:.0f}% covered')


def twig_tree(rng, x, y, angle, length, depth, spread=0.5, bend=0.12, fork=3, shrink=0.62, steps=7):
    """A branching twig as a list of (points, width share 0-1, depth); the points run from its root to its tip."""
    out = []
    pts = [(x, y)]
    a = angle
    for s in range(steps):
        a += rng.normal(0, bend)
        x += math.sin(a) * length / steps
        y -= math.cos(a) * length / steps
        pts.append((x, y))
    out.append((pts, 1.0, depth))
    if depth > 0:
        side = 1 if rng.random() < 0.5 else -1
        for f in range(fork):
            t = (f + 1) / (fork + 1) + rng.normal(0, 0.04)
            i = max(1, min(steps - 1, int(t * steps)))
            side = -side
            out += twig_tree(rng, pts[i][0], pts[i][1], angle + side * (spread + rng.normal(0, 0.12)), length * shrink * (1.1 - 0.3 * t), depth - 1, spread, bend, max(2, fork - 1), shrink, steps)
        out += twig_tree(rng, x, y, a + rng.normal(0, 0.2), length * shrink * 0.8, depth - 1, spread, bend, max(2, fork - 1), shrink, steps)
    return out


def inside(x, y, m=0.03):
    return m < x < 1 - m and m < y < 1 - m


def draw_twigs(cv, twigs, base_width, c, k=1.0):
    for pts, share, depth in sorted(twigs, key=lambda t: -t[2]):
        cv.line(pts, base_width * (0.45 + 0.55 * depth / 3), c, k)


def broadleaf(cv, twig_c, greens, leaf_len, leaf_w, per_twig, layers=3, point=1.0, veins=5, serrate=0.0, depth=3, spread=0.55, droop=0.0, flowers=None):
    rng = cv.rng
    twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.46, depth, spread=spread)
    for layer in range(layers):
        k = 0.55 + 0.45 * layer / max(1, layers - 1)
        if layer == layers // 2:
            draw_twigs(cv, twigs, 0.0085, twig_c)
        for pts, share, d in twigs:
            if d > 1:
                continue
            n = per_twig if d == 0 else max(2, per_twig // 2)
            for i in range(n):
                t = (i + rng.random()) / n
                j = min(len(pts) - 2, int(t * (len(pts) - 1)))
                x, y = pts[j]
                heading = math.atan2(pts[j + 1][0] - pts[j][0], -(pts[j + 1][1] - pts[j][1]))
                side = 1 if (i + layer) % 2 else -1
                a = heading + side * rng.uniform(0.5, 1.25) + droop * math.sin(heading) + rng.normal(0, 0.15)
                ln = leaf_len * rng.uniform(0.7, 1.15)
                tip = (x + math.sin(a) * ln, y - math.cos(a) * ln)
                if not inside(*tip):
                    continue
                c = greens[int(rng.integers(len(greens)))]
                cv.leaf(x, y, a, ln, leaf_w * rng.uniform(0.8, 1.15) * ln / leaf_len, c, k * rng.uniform(0.88, 1.1), point, veins, serrate)
        if flowers and layer == layers - 1:
            flowers(cv, twigs)


def atlas_camphor(cv):
    greens = [(0.16, 0.36, 0.13), (0.20, 0.42, 0.14), (0.26, 0.48, 0.16), (0.34, 0.55, 0.18), (0.42, 0.60, 0.22), (0.55, 0.30, 0.14)]
    broadleaf(cv, (0.24, 0.17, 0.11), greens, 0.075, 0.034, 9, layers=3, point=1.25, veins=3)


def atlas_broad(cv):
    greens = [(0.10, 0.26, 0.10), (0.13, 0.31, 0.11), (0.17, 0.36, 0.12), (0.22, 0.42, 0.14), (0.28, 0.46, 0.16)]
    broadleaf(cv, (0.17, 0.12, 0.08), greens, 0.088, 0.046, 10, layers=4, point=0.9, veins=6)


def atlas_birch(cv):
    greens = [(0.36, 0.56, 0.16), (0.46, 0.64, 0.20), (0.56, 0.68, 0.22), (0.76, 0.70, 0.20), (0.84, 0.62, 0.16)]
    broadleaf(cv, (0.20, 0.15, 0.12), greens, 0.05, 0.036, 8, layers=3, point=1.5, veins=5, serrate=0.18, spread=0.45, droop=0.6)


def atlas_ivy(cv):
    greens = [(0.08, 0.22, 0.08), (0.11, 0.28, 0.10), (0.15, 0.33, 0.12)]
    broadleaf(cv, (0.16, 0.13, 0.07), greens, 0.06, 0.06, 8, layers=3, point=0.7, veins=4, depth=3, spread=0.75)


def maple_leaf(cv, x, y, angle, size, c, k):
    """A palmate leaf: five pointed lobes from the stem's end."""
    for lobe, (da, ln) in enumerate(((-1.15, 0.62), (-0.55, 0.9), (0.0, 1.0), (0.55, 0.9), (1.15, 0.62))):
        cv.leaf(x, y, angle + da, size * ln, size * 0.4 * ln, c, k * (1.04 if lobe % 2 else 0.94), point=1.7, veins=2, serrate=0.22)


def atlas_maple(cv, palette):
    rng = cv.rng
    twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.46, 3, spread=0.6)
    for layer in range(3):
        k = 0.6 + 0.4 * layer / 2
        if layer == 1:
            draw_twigs(cv, twigs, 0.008, (0.22, 0.14, 0.12))
        for pts, share, d in twigs:
            if d > 1:
                continue
            n = 6 if d == 0 else 3
            for i in range(n):
                j = min(len(pts) - 2, int((i + rng.random()) / n * (len(pts) - 1)))
                x, y = pts[j]
                heading = math.atan2(pts[j + 1][0] - pts[j][0], -(pts[j + 1][1] - pts[j][1]))
                a = heading + (1 if (i + layer) % 2 else -1) * rng.uniform(0.3, 1.2)
                stem = 0.02
                sx, sy = x + math.sin(a) * stem, y - math.cos(a) * stem
                size = 0.062 * rng.uniform(0.75, 1.15)
                if not inside(sx + math.sin(a) * size, sy - math.cos(a) * size, 0.05):
                    continue
                cv.line([(x, y), (sx, sy)], 0.0012, (0.5, 0.2, 0.12), k)
                maple_leaf(cv, sx, sy, a + rng.normal(0, 0.3), size, palette[int(rng.integers(len(palette)))], k * rng.uniform(0.9, 1.1))


def atlas_maple_red(cv):
    atlas_maple(cv, [(0.72, 0.13, 0.08), (0.82, 0.22, 0.08), (0.88, 0.36, 0.08), (0.90, 0.52, 0.10), (0.60, 0.08, 0.08), (0.80, 0.62, 0.14)])


def atlas_maple_green(cv):
    atlas_maple(cv, [(0.20, 0.42, 0.12), (0.26, 0.50, 0.14), (0.34, 0.56, 0.16), (0.44, 0.60, 0.18)])


def atlas_blossom(cv):
    rng = cv.rng
    twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.47, 3, spread=0.7, bend=0.2)
    draw_twigs(cv, twigs, 0.009, (0.16, 0.10, 0.10))
    petals = [(0.98, 0.80, 0.86), (0.96, 0.66, 0.76), (1.0, 0.92, 0.94), (0.92, 0.52, 0.66)]
    for layer in range(3):
        k = 0.72 + 0.28 * layer / 2
        for pts, share, d in twigs:
            if d > 1:
                continue
            for i in range(9 if d == 0 else 4):
                j = int(rng.integers(1, len(pts)))
                x, y = pts[j][0] + rng.normal(0, 0.012), pts[j][1] + rng.normal(0, 0.012)
                if not inside(x, y, 0.04):
                    continue
                if rng.random() < 0.18:
                    # A bud, or a small new leaf.
                    if rng.random() < 0.5:
                        cv.disc(x, y, 0.006, (0.80, 0.36, 0.50), k)
                    else:
                        cv.leaf(x, y, rng.uniform(-2, 2), 0.03, 0.013, (0.36, 0.52, 0.20), k)
                    continue
                r = 0.017 * rng.uniform(0.75, 1.2)
                c = petals[int(rng.integers(len(petals)))]
                turn = rng.uniform(0, 6.28)
                for p in range(5):
                    a = turn + p * 2 * math.pi / 5
                    cv.disc(x + math.cos(a) * r * 0.62, y + math.sin(a) * r * 0.62, r * 0.56, c, k * (0.94 + 0.03 * p))
                cv.disc(x, y, r * 0.3, (0.86, 0.36, 0.42), k)
                for s in range(6):
                    a = turn + s * 1.05
                    cv.disc(x + math.cos(a) * r * 0.42, y + math.sin(a) * r * 0.42, r * 0.07, (0.98, 0.86, 0.36), k)


def atlas_pine(cv):
    """Pine: tufts of long paired needles bursting from the ends of scaly twigs, a few cones."""
    rng = cv.rng
    twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.44, 3, spread=0.6, bend=0.14)
    greens = [(0.07, 0.20, 0.13), (0.10, 0.26, 0.16), (0.14, 0.32, 0.18), (0.20, 0.40, 0.20), (0.28, 0.46, 0.22)]
    for layer in range(3):
        k = 0.55 + 0.45 * layer / 2
        if layer == 1:
            draw_twigs(cv, twigs, 0.011, (0.30, 0.20, 0.13))
        for pts, share, d in twigs:
            if d > 1:
                continue
            tufts = [len(pts) - 1] + ([len(pts) // 2] if d == 0 else []) + ([len(pts) * 3 // 4] if layer == 2 else [])
            for j in tufts:
                x, y = pts[j]
                heading = math.atan2(pts[j][0] - pts[j - 1][0], -(pts[j][1] - pts[j - 1][1]))
                count = 46 if j == len(pts) - 1 else 26
                for n in range(count):
                    a = heading + rng.normal(0, 0.75)
                    ln = 0.075 * rng.uniform(0.6, 1.1)
                    c = greens[int(rng.integers(len(greens)))]
                    # A needle: a hair-thin curved line, lighter toward its tip.
                    mx, my = x + math.sin(a) * ln * 0.5, y - math.cos(a) * ln * 0.5 + 0.004
                    tx, ty = x + math.sin(a + 0.08) * ln, y - math.cos(a + 0.08) * ln + 0.01
                    if not inside(tx, ty, 0.02):
                        continue
                    cv.line([(x, y), (mx, my)], 0.0016, c, k * 0.9)
                    cv.line([(mx, my), (tx, ty)], 0.0012, c, k * 1.15)
                if j == len(pts) - 1 and layer == 2 and rng.random() < 0.5:
                    cv.disc(x, y, 0.006, (0.62, 0.50, 0.30), 1)
    # Cones: brown scaled ovals hanging near forks.
    for pts, share, d in twigs:
        if d == 2 and rng.random() < 0.6:
            x, y = pts[-2]
            for row in range(5):
                for col in range(-row % 2 - 1, 2):
                    cv.disc(x + col * 0.008 + (row % 2) * 0.004, y + 0.012 + row * 0.007, 0.0065, (0.36, 0.24, 0.14), 0.8 + 0.1 * (row % 2))


def atlas_fir(cv):
    """Fir: flat fronds — every twig feathered on both sides with short needles, paler new growth at the tips."""
    rng = cv.rng
    twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.5, 3, spread=0.62, bend=0.05, fork=4, shrink=0.62)
    for layer in range(2):
        k = 0.65 + 0.35 * layer
        for pts, share, d in twigs:
            for j in range(len(pts) - 1):
                (x0, y0), (x1, y1) = pts[j], pts[j + 1]
                heading = math.atan2(x1 - x0, -(y1 - y0))
                seg = math.hypot(x1 - x0, y1 - y0)
                n = max(2, int(seg / 0.0032))
                for i in range(n):
                    t = (i + rng.random()) / n
                    x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
                    tipness = (j + t) / (len(pts) - 1)
                    new = d == 0 and tipness > 0.72
                    c = (0.30, 0.50, 0.22) if new else ((0.06, 0.20, 0.12) if rng.random() < 0.5 else (0.09, 0.26, 0.15))
                    ln = (0.022 if d == 0 else 0.027) * rng.uniform(0.8, 1.1) * (1 - 0.35 * tipness)
                    for side in (-1, 1):
                        a = heading + side * rng.uniform(0.95, 1.2)
                        tx, ty = x + math.sin(a) * ln, y - math.cos(a) * ln
                        if inside(tx, ty, 0.015):
                            cv.line([(x, y), (tx, ty)], 0.0017, c, k * rng.uniform(0.85, 1.1))
        if layer == 0:
            draw_twigs(cv, twigs, 0.007, (0.26, 0.18, 0.12))


def atlas_cypress(cv):
    """Cypress: fans of scaly, ropy sprays forking again and again."""
    rng = cv.rng
    greens = [(0.08, 0.22, 0.13), (0.11, 0.28, 0.15), (0.15, 0.34, 0.17), (0.22, 0.40, 0.18)]

    def fan(x, y, a, ln, depth, k):
        steps = 4
        px, py = x, y
        for s in range(steps):
            nx, ny = px + math.sin(a) * ln / steps, py - math.cos(a) * ln / steps
            if not inside(nx, ny, 0.015):
                return
            cv.line([(px, py), (nx, ny)], 0.0042 if depth < 2 else 0.006, greens[int(rng.integers(len(greens)))], k * rng.uniform(0.9, 1.1))
            # Scales: little beads along the spray.
            cv.disc(nx, ny, 0.0034, greens[int(rng.integers(len(greens)))], k * 1.08)
            px, py = nx, ny
            if depth > 0 and s > 0:
                side = 1 if s % 2 else -1
                fan(px, py, a + side * rng.uniform(0.35, 0.6), ln * 0.62, depth - 1, k)
        if depth > 0:
            fan(px, py, a + rng.normal(0, 0.15), ln * 0.6, depth - 1, k)

    for layer in range(3):
        k = 0.6 + 0.4 * layer / 2
        twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.3, 2, spread=0.5, bend=0.1)
        draw_twigs(cv, twigs, 0.008, (0.28, 0.18, 0.12), k)
        for pts, share, d in twigs:
            for j in range(2, len(pts), 2):
                heading = math.atan2(pts[j][0] - pts[j - 1][0], -(pts[j][1] - pts[j - 1][1]))
                fan(pts[j][0], pts[j][1], heading + (1 if j % 4 else -1) * 0.5, 0.12, 3, k)


def strands(cv, count, colours, leafy, waviness, width, leaf_len=0.03, top=0.012):
    """Hanging strands from the top middle of the picture: willow twigs with narrow leaves, or beards of lichen."""
    rng = cv.rng
    for layer in range(3):
        k = 0.6 + 0.4 * layer / 2
        for s in range(count):
            x = 0.5 + rng.normal(0, 0.16)
            x = min(0.94, max(0.06, x))
            length = rng.uniform(0.45, 0.95)
            pts = [(0.5 + (x - 0.5) * 0.25, top)]
            px, py = pts[0]
            steps = 26
            drift = rng.normal(0, waviness)
            for i in range(steps):
                t = (i + 1) / steps
                px += (x - pts[0][0]) * (1 / steps) * (2 - 2 * t) + drift * math.sin(t * 9 + s) / steps
                py += length / steps
                pts.append((px, py))
            c = colours[int(rng.integers(len(colours)))]
            cv.line(pts, width, c, k * 0.9)
            if leafy:
                for i in range(2, steps):
                    for side in (-1, 1):
                        if rng.random() < 0.2:
                            continue
                        a = math.pi + side * rng.uniform(0.25, 0.6)
                        cv.leaf(pts[i][0], pts[i][1], a, leaf_len * rng.uniform(0.7, 1.1), leaf_len * 0.2, colours[int(rng.integers(len(colours)))], k * rng.uniform(0.9, 1.1), point=1.6, veins=0)
            else:
                # Lichen forks into finer threads.
                for i in range(3, steps, 3):
                    if rng.random() < 0.6:
                        bx, by = pts[i]
                        fork = [(bx, by)]
                        for j in range(8):
                            bx += rng.normal(0, 0.004)
                            by += length / steps * 0.9
                            fork.append((bx, by))
                        cv.line(fork, width * 0.6, c, k)


def atlas_willow(cv):
    strands(cv, 15, [(0.42, 0.58, 0.18), (0.52, 0.66, 0.22), (0.62, 0.72, 0.26), (0.34, 0.50, 0.16)], True, 0.25, 0.0022, leaf_len=0.036)


def atlas_lichen(cv):
    strands(cv, 46, [(0.60, 0.68, 0.52), (0.50, 0.60, 0.46), (0.70, 0.74, 0.58), (0.42, 0.52, 0.38)], False, 0.5, 0.0016)


def atlas_bamboo(cv):
    """Bamboo: thin wiry twigs with fans of long narrow blades."""
    rng = cv.rng
    twigs = twig_tree(rng, 0.5, 0.985, 0.0, 0.5, 2, spread=0.6, bend=0.1)
    greens = [(0.26, 0.46, 0.14), (0.34, 0.54, 0.16), (0.44, 0.62, 0.20), (0.56, 0.68, 0.24)]
    for layer in range(3):
        k = 0.62 + 0.38 * layer / 2
        if layer == 1:
            draw_twigs(cv, twigs, 0.004, (0.44, 0.50, 0.20))
        for pts, share, d in twigs:
            for j in range(2, len(pts), 2):
                heading = math.atan2(pts[j][0] - pts[j - 1][0], -(pts[j][1] - pts[j - 1][1]))
                for b in range(4):
                    a = heading + rng.uniform(-1.3, 1.3)
                    ln = 0.13 * rng.uniform(0.7, 1.1)
                    if inside(pts[j][0] + math.sin(a) * ln, pts[j][1] - math.cos(a) * ln):
                        cv.leaf(pts[j][0], pts[j][1], a, ln, 0.014, greens[int(rng.integers(len(greens)))], k * rng.uniform(0.9, 1.1), point=1.8, veins=0)


ATLASES = {
    'leaf_camphor': atlas_camphor, 'leaf_broad': atlas_broad, 'leaf_birch': atlas_birch, 'leaf_ivy': atlas_ivy, 'leaf_maple_red': atlas_maple_red,
    'leaf_maple_green': atlas_maple_green, 'blossom_plum': atlas_blossom, 'needle_pine': atlas_pine, 'needle_fir': atlas_fir, 'scale_cypress': atlas_cypress,
    'hang_willow': atlas_willow, 'hang_lichen': atlas_lichen, 'leaf_bamboo': atlas_bamboo,
}


def main():
    out = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] != '-' else OUT
    only = sys.argv[2].split(',') if len(sys.argv) > 2 and sys.argv[2] != '-' else None
    normals = sys.argv[3] if len(sys.argv) > 3 else None
    os.makedirs(out, exist_ok=True)
    if normals:
        os.makedirs(normals, exist_ok=True)
    for n, (name, make) in enumerate(BARKS.items()):
        if only and name not in only:
            continue
        rgb, height = make(np.random.default_rng(100 + n))
        save_bark(name, rgb, height, out, normals)
    for n, (name, make) in enumerate(ATLASES.items()):
        if only and name not in only:
            continue
        cv = Canvas(500 + n)
        make(cv)
        cv.finish(os.path.join(out, name + '.png'))


if __name__ == '__main__':
    main()
