"""
Paints the buildings' tiling textures (Assets/GuWorld/Resources/Buildings), each with a height
map: the frame's wood, planks, lime plaster, mud, stone, grey brick, bamboo, roof tiles, thatch
and shingles. The building generator (Core/Gen/Buildings) lays them by the metre and tints them
in its vertices (the clan's lacquer on the wood, the wall's colour on the plaster), so wood,
planks and plaster are painted light and neutral; stone, brick, tile and thatch carry their own
colour.

  python buildtex.py [out_dir] [only,names] [normals_dir]

Orientation, as the generator lays them: u runs along a wall (or a beam, or a roof's eave), v up
the wall (or across the beam, or down the roof's slope). One repeat covers (metres): wood 1.4,
plank 2.2, plaster 3.2, mud 2.6, stone 2.4, brick 1.6, bamboo 1.5, tile/thatch/shingle 2.24 (eight
tiles of 0.28 m: the cover tiles' rows fall on u = k/8).
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.spatial import cKDTree

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Buildings'))
N = 1024


def noise(rng, gx, gy):
    grid = rng.random((gy, gx)).astype(np.float32)
    return np.clip(ndimage.zoom(grid, (N / gy, N / gx), order=3, mode='grid-wrap', grid_mode=True), 0, 1)


def fbm(rng, gx, gy, octaves=4, gain=0.5):
    total = np.zeros((N, N), np.float32)
    amp = norm = 0.0
    amp = 1.0
    for o in range(octaves):
        total += amp * noise(rng, min(N // 2, gx * 2 ** o), min(N // 2, gy * 2 ** o))
        norm += amp
        amp *= gain
    return total / norm


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a * (1 - t) + b * t


def col(c):
    return np.array(c, np.float32)


def voronoi(rng, count, sx=1.0, sy=1.0, pts=None):
    """Tiling cells: distance to the nearest point and the second, and the nearest's number; cells sx wide per sy tall."""
    if pts is None:
        pts = rng.random((count, 2))
    count = len(pts)
    tiles = np.concatenate([pts + (dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)])
    ids = np.tile(np.arange(count), 9)
    tree = cKDTree(tiles * (1 / sx, 1 / sy))
    ys, xs = np.mgrid[0:N, 0:N]
    q = np.stack([((xs + 0.5) / N).ravel() / sx, ((ys + 0.5) / N).ravel() / sy], axis=1)
    d, i = tree.query(q, k=2, workers=-1)
    return d[:, 0].reshape(N, N).astype(np.float32), d[:, 1].reshape(N, N).astype(np.float32), ids[i[:, 0]].reshape(N, N)


def grid():
    ys, xs = np.mgrid[0:N, 0:N]
    return (xs + 0.5) / N, (ys + 0.5) / N


def per(ids, seed, count=8192):
    return np.random.default_rng(seed).random(count).astype(np.float32)[ids % count]


def shade(rgb, h, k=0.5):
    blurred = ndimage.gaussian_filter(h, 5, mode='wrap')
    cavity = np.clip((h - blurred) * 3, -1, 1)
    slope = (np.roll(h, 2, 0) - np.roll(h, -2, 0)) * 3
    return rgb * (1 + k * (0.6 * cavity + 0.4 * np.clip(slope, -1, 1)))[..., None]


def save(name, rgb, h, out, normals):
    # Image rows run down; the generator's v runs up: flip so v=0 is the picture's bottom.
    rgb = np.clip(rgb, 0, 1)[::-1]
    h = np.clip(h, 0, 1)[::-1]
    Image.fromarray((rgb * 255).astype(np.uint8)).save(os.path.join(out, name + '.jpg'), quality=90)
    Image.fromarray((h * 255).astype(np.uint8), 'L').save(os.path.join(out, name + '_h.jpg'), quality=88)
    if normals:
        k = 6.0
        dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * k
        dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * k
        n = np.stack([-dx, dy, np.ones_like(dx)], 2)
        n /= np.linalg.norm(n, axis=2, keepdims=True)
        Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(os.path.join(normals, name + '_n.png'))
    print(' ', name)


# ---------------------------------------------------------------------- Wood

def wood(rng):
    """The frame's timber: long grain along u, a few knots, light so the vertices' tint (lacquer, dark stain) shows."""
    u, v = grid()
    wobble = (fbm(rng, 3, 6, 3) - 0.5) * 0.08
    rings = np.sin((v + wobble + 0.02 * np.sin(u * 6.283 * 2)) * 6.283 * 26 + 3 * fbm(rng, 4, 30, 2))
    fine = fbm(rng, 6, 160, 3)
    grain = 0.5 + 0.5 * rings * (0.35 + 0.4 * fbm(rng, 3, 8, 2))
    k1, _, kid = voronoi(rng, 7)
    knot = (1 - smooth(0.0, 0.03, k1)) * (per(kid, 3) > 0.4)
    h = 0.55 + 0.12 * (grain - 0.5) + 0.2 * (fine - 0.5) - 0.2 * knot
    base = col((0.86, 0.8, 0.74)) * (0.82 + 0.25 * grain[..., None]) * (0.85 + 0.3 * fine[..., None])
    base = mix(base, col((0.45, 0.38, 0.33)), knot * 0.7)
    # Cracks along the grain, and wear on the edges is the vertices' business.
    crack = (1 - smooth(0.0, 0.004, np.abs(fbm(rng, 2, 40, 2) - 0.5))) * smooth(0.55, 0.75, fbm(rng, 6, 3, 2))
    base = mix(base, col((0.25, 0.2, 0.18)), crack * 0.8)
    h -= 0.3 * crack
    return shade(base, h, 0.35), h


def plank(rng):
    """Boards standing up (doors, walls, floors seen from above run the same way): seams, nail heads, grain up the board."""
    u, v = grid()
    boards = 9
    edges = np.cumsum(np.concatenate([[0], 0.7 + 0.6 * rng.random(boards)]))
    edges = edges / edges[-1]
    idx = np.searchsorted(edges, u[0], side='right') - 1
    left = edges[idx]
    right = edges[idx + 1]
    across = (u - left[None, :]) / (right - left)[None, :]
    bid = np.broadcast_to(idx[None, :], (N, N))
    tone = per(bid, 5, 64)
    seam = 1 - smooth(0.0, 0.05, np.minimum(across, 1 - across))
    wob = (fbm(rng, 10, 2, 3) - 0.5) * 0.15
    grain = 0.5 + 0.5 * np.sin((u * 60 + wob * 8 + tone[...] * 9) * 6.283)
    fine = fbm(rng, 140, 6, 3)
    h = 0.62 - 0.45 * seam + 0.08 * (grain - 0.5) + 0.12 * (fine - 0.5)
    base = col((0.84, 0.78, 0.7)) * (0.8 + 0.3 * tone[..., None]) * (0.86 + 0.18 * grain[..., None]) * (0.85 + 0.3 * fine[..., None])
    base = mix(base, col((0.12, 0.1, 0.08)), seam * 0.85)
    # Battens across, nailed.
    for vb in (0.18, 0.82):
        band = 1 - smooth(0.0, 0.006, np.abs(np.abs(v - vb) - 0.03))
        base = base * (1 - 0.25 * band[..., None])
        nail = (1 - smooth(0.004, 0.008, np.hypot(across - 0.5, (v - vb) * 8))) if False else None
    # Stains from the eave's drip.
    drip = smooth(0.5, 0.85, fbm(rng, 12, 2, 3)) * smooth(0.6, 1.0, v)
    base = base * (1 - 0.3 * drip[..., None])
    return shade(base, h, 0.4), h


# ---------------------------------------------------------------------- Walls

def plaster(rng):
    """Lime plaster: near white, trowel marks, hairline cracks, damp creeping up and run-marks down."""
    u, v = grid()
    trowel = fbm(rng, 14, 10, 4)
    broad = fbm(rng, 3, 3, 4)
    f1, f2, _ = voronoi(rng, 40)
    crack = (1 - smooth(0.0, 0.0012, f2 - f1)) * smooth(0.62, 0.8, fbm(rng, 5, 5, 3))
    h = 0.6 + 0.1 * (trowel - 0.5) - 0.3 * crack
    base = col((0.93, 0.92, 0.88)) * (0.9 + 0.12 * trowel[..., None]) * (0.92 + 0.1 * broad[..., None])
    damp = smooth(0.35, 0.0, v + 0.15 * (fbm(rng, 8, 3, 3) - 0.5)) * 0.35
    base = mix(base, col((0.62, 0.62, 0.55)), damp)
    runs = smooth(0.6, 0.85, fbm(rng, 30, 2, 3)) * smooth(0.3, 1.0, v) * 0.25
    base = mix(base, col((0.55, 0.53, 0.5)), runs)
    # Where the plaster has fallen off: the mud and laths behind.
    spall = smooth(0.74, 0.78, fbm(rng, 4, 4, 5))
    base = mix(base, col((0.55, 0.45, 0.34)) * (0.8 + 0.3 * trowel[..., None]), spall)
    h -= 0.2 * spall
    base = mix(base, col((0.4, 0.38, 0.35)), crack * 0.5)
    return shade(base, h, 0.3), h


def mud(rng):
    """Mud and straw: warm earth, chopped straw, the marks of hands, cracks as it dried."""
    u, v = grid()
    lumps = fbm(rng, 12, 12, 5)
    f1, f2, _ = voronoi(rng, 70)
    crack = (1 - smooth(0.0, 0.0025, f2 - f1)) * smooth(0.45, 0.65, fbm(rng, 6, 6, 3))
    straw = np.zeros((N, N), np.float32)
    s = np.random.default_rng(9)
    img = Image.new('L', (N, N), 0)
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)
    for _ in range(2600):
        x, y = s.random() * N, s.random() * N
        a = s.random() * 6.283
        ln = 8 + s.random() * 22
        d.line([(x, y), (x + np.cos(a) * ln, y + np.sin(a) * ln)], fill=int(120 + s.random() * 135), width=2)
    straw = np.asarray(img, np.float32) / 255
    h = 0.55 + 0.25 * (lumps - 0.5) - 0.35 * crack + 0.08 * straw
    base = col((0.56, 0.44, 0.31)) * (0.82 + 0.35 * lumps[..., None])
    base = mix(base, col((0.78, 0.66, 0.42)), straw * 0.6)
    base = mix(base, col((0.2, 0.15, 0.1)), crack * 0.8)
    damp = smooth(0.3, 0.0, v) * 0.3
    base = base * (1 - damp[..., None])
    return shade(base, h, 0.5), h


def stone(rng):
    """Rough ashlar of grey stone: courses of blocks of uneven length, chiselled faces, dark mortar, lichen."""
    u, v = grid()
    courses = 8
    row = np.floor(v * courses).astype(int)
    rv = v * courses - row
    lengths = np.random.default_rng(31)
    h = np.zeros((N, N), np.float32)
    bid = np.zeros((N, N), np.int64)
    joint = np.zeros((N, N), np.float32)
    for r in range(courses):
        cuts = np.sort(np.concatenate([[0], lengths.random(lengths.integers(3, 6))]))
        shift = lengths.random()
        uu = (u[0] + shift) % 1.0
        k = np.searchsorted(cuts, uu, side='right') - 1
        l = cuts[k]
        rr = np.where(k + 1 < len(cuts), cuts[np.minimum(k + 1, len(cuts) - 1)], 1.0)
        dist = np.minimum(uu - l, rr - uu)
        sel = row == r
        joint[sel] = np.broadcast_to(dist[None, :], (N, N))[sel]
        bid[sel] = np.broadcast_to((k + r * 10)[None, :], (N, N))[sel]
    vj = np.minimum(rv, 1 - rv) / courses
    edge = np.minimum(joint, vj)
    chisel = fbm(rng, 40, 40, 3)
    face = fbm(rng, 8, 8, 4)
    tone = per(bid, 4, 512)
    block = smooth(0.003, 0.012 + 0.006 * face, edge)
    h = block * (0.55 + 0.15 * face + 0.1 * chisel + 0.1 * tone)
    base = col((0.47, 0.47, 0.45)) * (0.75 + 0.4 * tone[..., None]) * (0.85 + 0.3 * chisel[..., None]) * (0.85 + 0.25 * face[..., None])
    base = mix(col((0.12, 0.12, 0.11)), base, smooth(0.0, 0.5, block))
    lichen = smooth(0.66, 0.74, fbm(rng, 10, 10, 4)) * block
    base = mix(base, col((0.55, 0.6, 0.45)), lichen * 0.45)
    return shade(base, h, 0.55), h


def brick(rng):
    """Grey fired brick in running bond (the clan's dados and storehouses): crisp joints, chipped arrises, salt bloom."""
    u, v = grid()
    rows = 22
    per_row = 6
    row = np.floor(v * rows).astype(int)
    rv = v * rows - row
    uu = (u * per_row + (row % 2) * 0.5) % per_row
    col_i = np.floor(uu).astype(int)
    ru = uu - col_i
    bid = row * 13 + col_i
    tone = per(bid, 8, 4096)
    dx = np.minimum(ru, 1 - ru) / per_row
    dy = np.minimum(rv, 1 - rv) / rows
    edge = np.minimum(dx, dy)
    chip = fbm(rng, 60, 60, 2)
    brickness = smooth(0.0015, 0.004 + 0.002 * chip, edge)
    face = fbm(rng, 50, 50, 3)
    h = brickness * (0.6 + 0.12 * face + 0.08 * tone)
    base = col((0.42, 0.43, 0.44)) * (0.78 + 0.35 * tone[..., None]) * (0.88 + 0.22 * face[..., None])
    base = mix(col((0.62, 0.61, 0.57)), base, smooth(0.0, 0.6, brickness))
    bloom = smooth(0.62, 0.8, fbm(rng, 6, 6, 4)) * brickness
    base = mix(base, col((0.7, 0.7, 0.66)), bloom * 0.4)
    return shade(base, h, 0.45), h


def bamboo(rng):
    """Split bamboo culms side by side, standing up: nodes, sheen, weathered grey-green."""
    u, v = grid()
    count = 12
    cu = u * count
    i = np.floor(cu).astype(int)
    fu = cu - i
    tone = per(i, 6, 64)
    round_ = np.sqrt(np.clip(1 - (2 * fu - 1) ** 2, 0, 1))
    node_at = (v * 2.5 + tone * 0.9) % 1.0
    node = 1 - smooth(0.0, 0.025, np.abs(node_at - 0.5))
    fine = fbm(rng, 4, 200, 2)
    h = 0.2 + 0.6 * round_ + 0.12 * node
    base = mix(col((0.6, 0.58, 0.42)), col((0.5, 0.55, 0.38)), tone) * (0.65 + 0.4 * round_[..., None]) * (0.9 + 0.15 * fine[..., None])
    base = mix(base, col((0.36, 0.33, 0.22)), node * 0.6)
    grey = smooth(0.4, 0.8, fbm(rng, 4, 6, 3)) * 0.5
    base = mix(base, col((0.55, 0.53, 0.5)), grey)
    return shade(base, h, 0.4), h


# ---------------------------------------------------------------------- Roofs

def tile(rng):
    """Grey clay tiles: eight channels of pan tiles across, each row lapping the one below, cover tiles on the ribs (u = k/8)."""
    u, v = grid()
    lanes = 8
    lu = u * lanes
    lane = np.floor(lu + 0.5).astype(int)
    fu = lu + 0.5 - lane                           # 0-1 across a lane, the rib at 0 and 1
    rows = 14
    row = np.floor(v * rows + (lane % 2) * 0.37).astype(int)
    fv = v * rows + (lane % 2) * 0.37 - row       # 0 at a tile's lower edge
    tid = row * 17 + lane
    tone = per(tid, 12, 4096)
    rib = np.minimum(fu, 1 - fu)                   # distance to the cover-tile rib
    cover = smooth(0.17, 0.11, rib)                # the round cover tile
    pan = 1 - (2 * fu - 1) ** 2                    # the pan's hollow seen from above (high at the lane's middle? no: low)
    hollow = 1 - pan
    lap = smooth(0.0, 0.12, fv)                    # each tile's lower edge laps the one below
    h = 0.35 + 0.25 * hollow * 0.5 + 0.2 * (fv * 0.6) * (1 - cover)
    h = np.where(cover > 0.01, 0.55 + 0.4 * np.sqrt(np.clip(1 - (rib / 0.17) ** 2, 0, 1)) * cover + 0.05 * fv, h)
    h = h * (0.85 + 0.15 * lap)
    grit = fbm(rng, 90, 90, 2)
    base = col((0.27, 0.29, 0.31)) * (0.75 + 0.4 * tone[..., None]) * (0.88 + 0.22 * grit[..., None])
    base = base * (0.55 + 0.45 * lap[..., None])
    base = base * (0.7 + 0.3 * hollow[..., None] * (1 - cover[..., None])) + cover[..., None] * base * 0.15
    # Dark gaps beside the ribs, lichen spots, a lighter weathered bloom.
    gap = smooth(0.2, 0.17, rib) * (1 - cover)
    base = base * (1 - 0.6 * gap[..., None])
    bloom = smooth(0.55, 0.85, fbm(rng, 5, 5, 4))
    base = mix(base, col((0.48, 0.5, 0.5)), bloom * 0.3)
    spots = smooth(0.78, 0.84, fbm(rng, 24, 24, 3))
    base = mix(base, col((0.58, 0.6, 0.42)), spots * 0.6)
    return shade(base, h, 0.5), h


def thatch(rng):
    """Rice-straw thatch: strands down the slope, laid in courses, grey and dark where it has weathered."""
    u, v = grid()
    img = Image.new('L', (N, N), 0)
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)
    s = np.random.default_rng(17)
    for _ in range(26000):
        x = s.random() * N
        y = s.random() * N
        ln = 30 + s.random() * 90
        dx = (s.random() - 0.5) * 10
        g = int(90 + s.random() * 165)
        for ox in (-N, 0, N):
            for oy in (-N, 0, N):
                d.line([(x + ox, y + oy), (x + dx + ox, y + ln + oy)], fill=g, width=2)
    strands = np.asarray(img, np.float32) / 255
    courses = 6
    cv = (v * courses) % 1.0
    course = smooth(0.0, 0.25, cv)
    broad = fbm(rng, 6, 4, 4)
    h = 0.3 + 0.45 * strands * (0.6 + 0.4 * course) + 0.15 * course
    base = col((0.62, 0.52, 0.33)) * (0.55 + 0.6 * strands[..., None]) * (0.6 + 0.4 * course[..., None])
    grey = smooth(0.35, 0.75, broad)
    base = mix(base, col((0.42, 0.4, 0.35)) * (0.6 + 0.6 * strands[..., None]), grey * 0.6)
    moss = smooth(0.72, 0.8, fbm(rng, 8, 8, 4))
    base = mix(base, col((0.3, 0.36, 0.17)), moss * 0.5)
    return shade(base, h, 0.6), h


def shingle(rng):
    """Split wooden shingles in lapping rows down the slope, staggered, silvered by weather."""
    u, v = grid()
    rows = 9
    row = np.floor(v * rows).astype(int)
    fv = v * rows - row
    widths = 7
    off = per(row, 21, 64)
    uu = (u * widths + off * 3) % widths
    k = np.floor(uu).astype(int)
    fu = uu - k
    sid = row * 31 + k
    tone = per(sid, 22, 4096)
    gap = 1 - smooth(0.0, 0.03, np.minimum(fu, 1 - fu))
    lap = smooth(0.0, 0.1, fv)
    grain = fbm(rng, 140, 6, 3)
    h = (0.3 + 0.5 * fv * 0.7 + 0.15 * grain) * (1 - 0.6 * gap) * (0.7 + 0.3 * lap)
    base = col((0.55, 0.5, 0.45)) * (0.7 + 0.45 * tone[..., None]) * (0.8 + 0.35 * grain[..., None])
    base = base * (0.45 + 0.55 * lap[..., None]) * (1 - 0.7 * gap[..., None])
    return shade(base, h, 0.45), h


def rammed(rng):
    """Rammed earth: courses the height of the shuttering, each pressed in layers, the boards' seams and the tie-holes left in rows."""
    u, v = grid()
    courses = 6
    cv = v * courses
    course = np.floor(cv).astype(int)
    fv = cv - course
    layers = 0.5 + 0.5 * np.sin((v * 60 + 0.4 * fbm(rng, 12, 3, 3)) * 6.283)
    lumps = fbm(rng, 16, 10, 4)
    seam = 1 - smooth(0.0, 0.03, np.minimum(fv, 1 - fv))
    tone = per(course, 33, 64)
    ys = np.zeros((N, N), np.float32)
    holes = np.zeros((N, N), np.float32)
    for k in range(courses):
        cy = (k + 0.08) / courses
        for j in range(4):
            cx = (j + 0.5 * (k % 2) + 0.25) / 4
            d = np.hypot(((u - cx + 0.5) % 1.0) - 0.5, (v - cy) * 1.0)
            holes = np.maximum(holes, 1 - smooth(0.006, 0.011, d))
    f1, f2, _ = voronoi(rng, 50, 1.0, 0.6)
    crack = (1 - smooth(0.0, 0.002, f2 - f1)) * smooth(0.6, 0.78, fbm(rng, 6, 6, 3))
    h = 0.6 + 0.12 * (layers - 0.5) + 0.2 * (lumps - 0.5) - 0.3 * seam - 0.4 * holes - 0.3 * crack
    base = col((0.6, 0.48, 0.34)) * (0.82 + 0.25 * tone[..., None]) * (0.85 + 0.2 * layers[..., None]) * (0.85 + 0.3 * lumps[..., None])
    base = mix(base, col((0.3, 0.23, 0.16)), seam * 0.5)
    base = mix(base, col((0.08, 0.06, 0.05)), np.maximum(holes, crack * 0.8))
    rain = smooth(0.55, 0.8, fbm(rng, 24, 2, 3)) * 0.25
    base = mix(base, col((0.42, 0.36, 0.3)), rain)
    damp = smooth(0.25, 0.0, v) * 0.35
    base = base * (1 - damp[..., None])
    return shade(base, h, 0.5), h


def log(rng):
    """Round logs with their bark on (palisades, cartwheels' hubs, woodpiles): ridged bark running along u, pale where it has peeled."""
    u, v = grid()
    w = (fbm(rng, 3, 30, 3) - 0.5) * 0.06
    ridges = 0.5 + 0.5 * np.sin((v + w) * 6.283 * 22 + 2 * fbm(rng, 30, 4, 2))
    ridges = ridges ** 1.6
    breaks = 1 - (1 - smooth(0.0, 0.03, np.abs(fbm(rng, 40, 3, 2) - 0.5))) * 0.6
    fine = fbm(rng, 120, 30, 3)
    h = 0.35 + 0.45 * ridges * breaks + 0.15 * (fine - 0.5)
    base = col((0.4, 0.33, 0.26)) * (0.55 + 0.6 * ridges[..., None]) * (0.85 + 0.3 * fine[..., None])
    peel = smooth(0.66, 0.72, fbm(rng, 6, 8, 4))
    base = mix(base, col((0.72, 0.6, 0.45)) * (0.85 + 0.3 * fine[..., None]), peel)
    h = np.where(peel > 0.5, 0.3 + 0.1 * fine, h)
    lichen = smooth(0.7, 0.8, fbm(rng, 10, 10, 3)) * (1 - peel)
    base = mix(base, col((0.55, 0.58, 0.45)), lichen * 0.5)
    return shade(base, h, 0.45), h


ALL = {'wood': wood, 'plank': plank, 'plaster': plaster, 'mud': mud, 'stone': stone, 'brick': brick, 'bamboo': bamboo, 'tile': tile, 'thatch': thatch, 'shingle': shingle, 'rammed': rammed, 'log': log}


def main():
    out = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] != '-' else OUT
    only = sys.argv[2].split(',') if len(sys.argv) > 2 and sys.argv[2] != '-' else None
    normals = sys.argv[3] if len(sys.argv) > 3 else None
    os.makedirs(out, exist_ok=True)
    if normals:
        os.makedirs(normals, exist_ok=True)
    for n, (name, make) in enumerate(ALL.items()):
        if only and name not in only:
            continue
        rgb, h = make(np.random.default_rng(300 + n))
        save(name, rgb, h, out, normals)


if __name__ == '__main__':
    main()
