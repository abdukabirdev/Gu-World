// The Xiong clan's village and the mortals' hamlets, built from the artist's kit (2026-10-31).
//
// The Xiong are the bear clan of the fore mountain: their houses are heavy log walls on rough stone footings and
// boulders, under thick, steep thatch; bear skulls and horns hang on the gables and the posts, claw marks are cut
// beside the doors, the lanterns are the warm fire-red paper ones, the banners red ochre, and there is fire in bronze
// and stone bowls. Here are their families' houses (ten, long houses among them), the poor huts, sheds and shops, the
// two taverns of bear-gall wine, the teahouse, the inn, the smithy, the herbalist, the two granaries (one the honey
// store), the Gu nursery, and the clan's own buildings: the great log pavilion, the hall, the ancestral hall, the
// academy (a hall over a training yard with a wrestling ring) and the hostel.
//
// The mortals live in twelve hamlets under the clans: mud walls on a low rubble footing, thatch, rail-fenced pens,
// mud yard walls capped with straw. Their huts (eight), sheds (a pen, a hay barn, a woodshed, a storehouse), the
// headman's houses with a yard, and the things a hamlet has in common: the earth god's shrine, the threshing floor,
// the water mill, the bee yard and the well.
//
// Everything faces +Z and is centred on the origin (centre() below measures a model the way the exporter does and
// moves it). The helpers are our own, close to the artist's in kit/houses.js but lighter for the game:
//   logWall()  — a log wall laid in horizontal bands between the lines of its openings' sills and heads, so that a
//                window costs a few short logs instead of three whole log walls (the kit's wall_window and wall_door
//                fill each side of the hole with its own full stack of logs: ~900 triangles a window); the walls run
//                past the corners so the log ends cross, the side walls half a log lower than the front and back;
//   kitWall()  — the kit's own wall modules (mud, plank, bamboo, timber…), plain runs joined into modules up to 6 m;
//   thatch()   — the artist's gableRoof() in the thatch style with slope and ridge modules up to 4 m wide (the
//                slope's straw fringe costs the same per metre whatever the module, its rows do not), the gables
//                optionally laid in logs; leanTo() is one such slope against a wall (porches, annexes, sheds);
//   plinth(), boulders(), steps() — the stone footing in blocks up to 4 × 4 m, boulders under the corners, and stone
//                steps from the ground up to every door that stands above it.
// The kit's wattle (fence_wattle, ~4600 triangles a metre) is far too heavy for a hamlet: the mortals' pens have rail
// fences and their yards low mud walls under a straw coping. Some decoration uses a material outside a part's own
// list on purpose (a rock in "bread" for a pale bone skull, a cone of fir in "bread" for a horn, a plank of "fur" for
// a stretched hide): only material names, which the kit builds as they are.

const D2R = Math.PI / 180;
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);
const r4 = (v) => Math.round(v * 1e4) / 1e4;

// a part with a scale (Frame.add always writes 1, 1, 1)
function put(F, part, params, p, r = [0, 0, 0], g = "Декор", s = null) {
  F.add(part, params, p, r, g);
  if (s) F.items[F.items.length - 1].s = s;
}

// ------------------------------------------------------------------ centring
// As the exporter measures: the house is what is not décor; décor more than 0.4 m outside it is left out (unless the
// recipe keeps its yard). The finished items are moved so the measured box's middle is at the origin in X and Z.
function centre(items, kit, THREE, yard = false) {
  const boxes = items.map((it) => {
    const r = it.r || [0, 0, 0], s = it.s || [1, 1, 1];
    const M = new THREE.Matrix4().compose(new THREE.Vector3(...it.p), new THREE.Quaternion().setFromEuler(new THREE.Euler(r[0] * D2R, r[1] * D2R, r[2] * D2R)), new THREE.Vector3(...s));
    return kit.buildPart(it.part, it.params).bbox.clone().applyMatrix4(M);
  });
  const shell = new THREE.Box3();
  items.forEach((it, i) => { if (!DETAIL.has(it.g)) shell.union(boxes[i]); });
  const all = new THREE.Box3(), c = new THREE.Vector3();
  items.forEach((it, i) => {
    if (DETAIL.has(it.g) && !yard) {
      boxes[i].getCenter(c);
      if (!(c.x > shell.min.x - 0.4 && c.x < shell.max.x + 0.4 && c.z > shell.min.z - 0.4 && c.z < shell.max.z + 0.4)) return;
    }
    all.union(boxes[i]);
  });
  const dx = (all.min.x + all.max.x) / 2, dz = (all.min.z + all.max.z) / 2;
  for (const it of items) it.p = [r4(it.p[0] - dx), it.p[1], r4(it.p[2] - dz)];
  return items;
}

// ------------------------------------------------------------------ openings
// An opening: [token, position along the wall (m, from its middle), width for an open bay]. Heights are from the floor.
//   d door, D double door, n window, N wide window, a shop window (propped flap, a counter board), v small vent high
//   up, f a shuttered window that is only shutters on the wall, p an open bay up to the top beam.
// logWall() puts the lines on whole logs (dh = 0.92 t each) so the bands keep their rows.
function openingsOf(spec, o) {
  const k = o.dh || 0.3, out = [];
  for (const [tok, u, wide] of spec || []) {
    const S = {
      d: { w: o.dw ?? 0.95, y0: 0, y1: o.dtop ?? 7 * k },
      D: { w: 1.6, y0: 0, y1: o.Dtop ?? Math.max(o.dtop ?? 7 * k, 2.2) },
      n: { w: o.ww ?? 0.6, y0: o.sill ?? 4 * k, y1: o.wtop ?? 6 * k },
      N: { w: 1.0, y0: o.sill ?? 4 * k, y1: o.wtop ?? 6 * k },
      a: { w: 1.8, y0: 3 * k, y1: o.dtop ?? 7 * k },
      v: { w: 0.45, y0: (o.vsill ?? 6 * k), y1: (o.vsill ?? 6 * k) + Math.max(0.3, k) },
      f: { w: o.ww ?? 0.6, y0: o.sill ?? 4 * k, y1: o.wtop ?? 6 * k },
      p: { w: wide || 2, y0: 0, y1: 99 },
    }[tok];
    if (!S) throw new Error("opening token " + tok);
    out.push({ tok, u, ...S, x0: u - S.w / 2, x1: u + S.w / 2 });
  }
  return out;
}
// the solid stretches of [a, b] outside the given holes, each split into modules of at most `max`
function solids(a, b, holes, max = 6) {
  const hs = holes.map((h) => [Math.max(a, h.x0), Math.min(b, h.x1)]).filter(([x0, x1]) => x1 > x0).sort((p, q) => p[0] - q[0]);
  const out = []; let u = a;
  for (const [x0, x1] of hs) { if (x0 - u > 0.08) out.push([u, x0]); u = Math.max(u, x1); }
  if (b - u > 0.08) out.push([u, b]);
  const mods = [];
  for (const [x0, x1] of out) { const n = Math.ceil((x1 - x0) / max - 1e-6); for (let i = 0; i < n; i++) mods.push([x0 + (x1 - x0) * i / n, x0 + (x1 - x0) * (i + 1) / n]); }
  return mods;
}
// the frames, leaves, shutters and the like of the openings (shared by both kinds of wall); R is the wall's frame
function fillings(R, ops, o) {
  const { floor, t } = o, fm = o.frameMat || "wood_dark", face = t / 2;
  for (const op of ops) {
    const y0 = floor + op.y0, y1 = floor + Math.min(op.y1, o.h);
    if (o.frames) {
      if (op.tok !== "f" && op.tok !== "p") for (const s of [-1, 1]) R.add("beam", { L: y1 - y0 + (op.y0 > 0 ? 0.12 : 0.06), h: 0.1, d: t + 0.06, mat: fm }, [op.u + s * (op.w / 2 + 0.05), y0 - (op.y0 > 0 ? 0.06 : 0), 0.05], [0, 0, 90], "Окна и двери");
      if (op.tok !== "f" && op.tok !== "p") R.add("beam", { L: op.w + 0.3, h: 0.14, d: t + 0.08, mat: fm }, [op.u, y1, 0], [0, 0, 0], "Окна и двери");
      if (op.y0 > 0 && op.tok !== "f" && op.tok !== "p") R.add("beam", { L: op.w + 0.3, h: 0.08, d: t + 0.12, mat: fm }, [op.u, y0 - 0.08, 0.03], [0, 0, 0], "Окна и двери");
      if (op.tok === "d" || op.tok === "D") R.add("beam", { L: op.w, h: 0.12, d: t + 0.04, mat: fm }, [op.u, floor, 0], [0, 0, 0], "Окна и двери");
    }
    const sill = o.frames && (op.tok === "d" || op.tok === "D") ? 0.12 : o.frames ? 0 : 0.14;
    if (op.tok === "d") {
      if (o.curtain) R.add("door_curtain", { w: op.w, h: y1 - y0 - 0.1, color: o.curtain }, [op.u, floor + 0.05, face + 0.04], [0, 0, 0], "Окна и двери");
      else R.add("door_leaf", { w: op.w, h: y1 - y0 - sill - 0.04, style: o.door || "plank" }, [op.u - op.w / 2, floor + sill, -face + 0.04], [0, 72, 0], "Окна и двери");
    } else if (op.tok === "D") {
      for (const s of [-1, 1]) R.add("door_leaf", { w: op.w / 2, h: y1 - y0 - sill - 0.04, style: o.door || "plank" }, [op.u + s * op.w / 2, floor + sill, -face + 0.04], [0, s < 0 ? 80 : 100, 0], "Окна и двери");
    } else if (op.tok === "n" || op.tok === "N" || op.tok === "v") {
      const wh = y1 - y0;
      if (o.win !== "open") R.add("window_lattice", { w: op.w, h: wh, pattern: o.lattice || "vertical", mat: "wood_dark" }, [op.u, y0, 0], [0, 0, 0], "Окна и двери");
      if (o.shutters && op.tok !== "v") for (const s of [-1, 1]) R.add("window_shutter", { w: op.w / 2, h: wh, mat: "wood_old" }, [op.u + s * (op.w / 2 + 0.08), y0, face + 0.03], [0, s < 0 ? 180 : 0, 0], "Окна и двери");
    } else if (op.tok === "f") {
      for (const s of [-1, 1]) R.add("window_shutter", { w: op.w / 2, h: y1 - y0, mat: "wood_old" }, [op.u + s * op.w / 2, y0, face + 0.03], [0, s < 0 ? 0 : 180, 0], "Окна и двери");
      R.add("beam", { L: op.w + 0.2, h: 0.08, d: 0.1, mat: fm }, [op.u, y0 - 0.08, face + 0.04], [0, 0, 0], "Окна и двери");
    } else if (op.tok === "a") {
      R.add("window_awning", { w: op.w, h: Math.min(1.5, (y1 - y0) * 0.75), open: 38, mat: "wood_dark" }, [op.u, y1 - Math.min(1.5, (y1 - y0) * 0.75), face + 0.03], [0, 0, 0], "Окна и двери");
      R.add("plank", { L: op.w + 0.1, w: 0.45, t: 0.05, mat: "wood_raw" }, [op.u, y0, 0.12], [0, 0, 0], "Окна и двери");
    }
  }
}

// ------------------------------------------------------------------ walls
// A log wall along the frame's X from −L/2 to L/2 (its face +Z), its floor at `floor`, its logs from `base` (half a
// log lower on the side walls), the openings cut out band by band.
function logWall(F, o) {
  const R = F.at(o.x, 0, o.z, o.yaw), t = o.t, dh = 0.92 * Math.max(0.16, t);
  const ops = openingsOf(o.open, { ...o, dh });
  const base = o.base ?? o.floor, top = o.floor + o.h;
  const ys = new Set([base, top]);
  for (const op of ops) if (op.tok !== "f") for (const y of [o.floor + op.y0, o.floor + op.y1]) if (y > base + 0.05 && y < top - 0.05) ys.add(y);
  const lines = [...ys].sort((a, b) => a - b);
  for (let i = 0; i + 1 < lines.length; i++) {
    const ya = lines[i], yb = lines[i + 1], holes = ops.filter((op) => op.tok !== "f" && o.floor + op.y0 <= ya + 1e-3 && o.floor + op.y1 >= yb - 1e-3);
    for (const [x0, x1] of solids(-o.L / 2, o.L / 2, holes)) R.add("wall_log", { w: x1 - x0, h: yb - ya, t }, [(x0 + x1) / 2, ya, 0], [0, 0, 0], "Стены");
  }
  fillings(R, ops, { ...o, frames: true });
  return ops;
}
// A wall of the kit's modules (type: mud, plank, bamboo, timber, ochre, grey, stone…).
function kitWall(F, o) {
  const R = F.at(o.x, 0, o.z, o.yaw), t = o.t, type = o.type;
  const ops = openingsOf(o.open, { dw: 0.85, dtop: o.dtop ?? 1.95, Dtop: 2.2, ww: 0.6, sill: o.sill ?? 1.0, wtop: o.wtop ?? 1.6, vsill: o.h - 0.55, dh: 0.3 });
  for (const op of ops) {
    if (op.tok === "p" || op.tok === "f") continue;
    const w = op.w + 0.3;
    if (op.tok === "d" || op.tok === "D") R.add("wall_door", { w, h: o.h, t, type, dw: op.w, dh: op.y1 }, [op.u, o.floor, 0], [0, 0, 0], "Стены");
    else R.add("wall_window", { w, h: o.h, t, type, ww: op.w, wh: op.y1 - op.y0, sill: op.y0 }, [op.u, o.floor, 0], [0, 0, 0], "Стены");
    op.x0 = op.u - w / 2; op.x1 = op.u + w / 2;
  }
  for (const [x0, x1] of solids(-o.L / 2, o.L / 2, ops.filter((op) => op.tok !== "f"))) R.add("wall_" + type, { w: x1 - x0, h: o.h, t }, [(x0 + x1) / 2, o.floor, 0], [0, 0, 0], "Стены");
  // the kit's door and window modules bring their own frames
  for (const op of ops) { op.x0 = op.u - op.w / 2; op.x1 = op.u + op.w / 2; }
  fillings(R, ops, { ...o, frames: false });
  return ops;
}

// ------------------------------------------------------------------ roofs
// A thatched gable roof over a W × D box of walls (ridge along X): slopes and ridge in modules up to 4 m, gables of
// board or of logs. overF / overB: the eave's reach in front and behind. Returns the ridge's height.
function thatch(F, o) {
  const { W, D, top } = o, cx = o.cx || 0, cz = o.cz || 0, pitch = o.pitch ?? 42, side = o.side ?? 0.45;
  const tp = Math.tan(pitch * D2R), ridgeY = top + 0.04 + (D / 2) * tp;
  const span = W + 2 * side, n = Math.max(1, Math.ceil(span / 4 - 1e-6)), mw = span / n;
  for (let i = 0; i < n; i++) {
    const x = cx - span / 2 + (i + 0.5) * mw;
    F.add("roof_thatch_slope", { w: mw, run: D / 2 + (o.overF ?? o.over ?? 0.6), pitch }, [x, ridgeY, cz], [0, 0, 0], "Крыша");
    F.add("roof_thatch_slope", { w: mw, run: D / 2 + (o.overB ?? o.over ?? 0.6), pitch }, [x, ridgeY, cz], [0, 180, 0], "Крыша");
    F.add("roof_ridge", { w: mw, style: "thatch" }, [x, ridgeY, cz], [0, 0, 0], "Крыша");
  }
  const gh = (D / 2) * tp;
  for (const sx of o.gables === false ? [] : o.gables || [-1, 1]) {
    if (o.logGable) {
      // logs laid across the gable, each as long as the slopes above it let it be
      const t = o.t || 0.3, dh = 0.92 * t, G = F.at(cx + sx * W / 2, 0, cz, sx * 90);
      for (let y = top; y < top + gh - 0.25; y += dh) {
        const h = Math.min(dh, top + gh - y), half = D / 2 - (y + h - top) / tp;
        if (half > 0.2) G.add("wall_log", { w: 2 * half, h, t }, [0, y, 0], [0, 0, 0], "Стены");
      }
      G.add("gable", { w: Math.min(D, 2 * Math.max(0.6, 0.35 / tp + 0.3)), h: Math.min(gh, 0.4), t: t * 0.6, type: "log", fish: "no" }, [0, top + gh - Math.min(gh, 0.4), 0], [0, 0, 0], "Стены");
    } else F.add("gable", { w: D, h: gh, t: o.t || 0.2, type: o.gable || "log", fish: "no" }, [cx + sx * W / 2, top, cz], [0, sx * 90, 0], "Стены");
  }
  return ridgeY;
}
// A single thatched slope from a wall: its high edge at (x, y, z) in the frame turned by yaw, falling `run` outward.
function leanTo(F, o) {
  const R = F.at(o.x || 0, 0, o.z || 0, o.yaw || 0), pitch = o.pitch ?? 26, n = Math.max(1, Math.ceil(o.L / 4 - 1e-6)), mw = o.L / n;
  for (let i = 0; i < n; i++) R.add("roof_thatch_slope", { w: mw, run: o.run, pitch }, [-o.L / 2 + (i + 0.5) * mw, o.y, 0], [0, 0, 0], "Крыша");
  return o.y - o.run * Math.tan(pitch * D2R); // the eave
}

// ------------------------------------------------------------------ footings
function plinth(F, o) {
  const { W, D, h } = o, cx = o.cx || 0, cz = o.cz || 0, style = o.style || "rubble", y0 = o.y0 ?? -0.15;
  const nx = Math.ceil(W / 4 - 1e-6), nz = Math.ceil(D / 4 - 1e-6);
  for (let i = 0; i < nx; i++) for (let j = 0; j < nz; j++)
    F.add("found_block", { w: W / nx, d: D / nz, h: h - y0, style }, [cx - W / 2 + (i + 0.5) * W / nx, y0, cz - D / 2 + (j + 0.5) * D / nz], [0, 0, 0], "Фундамент");
}
// boulders at the corners (and along the long sides of long houses)
function boulders(F, o) {
  const { W, D } = o, cx = o.cx || 0, cz = o.cz || 0, size = o.size || 0.9, pts = [];
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) pts.push([sx * W / 2, sz * D / 2]);
  if (o.mid) for (const sz of [-1, 1]) for (const f of o.mid) pts.push([f * W / 2, sz * D / 2]);
  pts.forEach(([x, z], i) => F.add("rock", { size: size * (0.85 + 0.3 * ((i * 37) % 10) / 10), seed: 11 + i * 7 + (o.seed || 0), flat: 0.6, mat: i % 3 ? "stone" : "stone_dark", moss: i % 2 ? "yes" : "no" }, [cx + x, -0.18, cz + z], [0, i * 47, 0], "Фундамент"));
}
// stone steps up from the ground to `H` before an edge: the top step's back at (x, edge) in the frame turned by yaw
function steps(F, o) {
  const H = o.H; if (H < 0.1) return 0;
  const n = Math.max(1, Math.round(H / (o.maxRise || 0.17))), rise = H / n, tread = o.tread || 0.3;
  F.at(o.x || 0, 0, o.z || 0, o.yaw || 0).add("stair_flight", { n, w: o.w || 1.2, tread, rise, kind: o.kind || "stone" }, [0, 0, n * tread], [0, 0, 0], "Лестницы");
  return n * tread;
}

// ------------------------------------------------------------------ Xiong décor
// a bear's skull: the pale cranium, the snout, the dark sockets; facing +Z of the frame turned by yaw
function skull(F, x, y, z, yaw = 0, k = 1, g = "Декор") {
  const S = F.at(x, 0, z, yaw);
  S.add("rock", { size: 0.36 * k, seed: 5, flat: 0.8, mat: "bread", moss: "no" }, [0, y - 0.1 * k, 0], [0, 0, 0], g);
  S.add("rock", { size: 0.22 * k, seed: 9, flat: 0.75, mat: "bread", moss: "no" }, [0, y - 0.12 * k, 0.17 * k], [0, 0, 0], g);
  for (const s of [-1, 1]) S.add("stone_block", { w: 0.07 * k, h: 0.06 * k, d: 0.04 * k, mat: "dark" }, [s * 0.075 * k, y + 0.02 * k, 0.15 * k], [0, s * 15, 0], g);
}
// a pair of horns rising and spreading from (x, y, z), in the plane across the frame's Z
function horns(F, x, y, z, yaw = 0, k = 1, g = "Декор") {
  const S = F.at(x, 0, z, yaw);
  for (const s of [-1, 1]) put(S, "foliage_cone", { r: 0.2, h: 0.3, mat: "bread" }, [s * 0.1 * k, y, 0], [0, 0, -s * 38], g, [0.32 * k, 1.9 * k, 0.32 * k]);
}
// a post with bands, a skull and horns on top: the bear totem the clan sets at its doors
function totem(F, x, z, h = 3.2, yaw = 0) {
  F.add("rock", { size: 0.7, seed: 31, flat: 0.55, mat: "stone_dark", moss: "yes" }, [x, -0.15, z], [0, yaw, 0], "Фундамент");
  F.add("post", { h, r: 0.13, mat: "wood_dark", bands: "dark" }, [x, 0, z], [0, 0, 0], "Каркас");
  skull(F, x, h + 0.16, z + 0.04, yaw, 1.15);
  horns(F, x, h + 0.08, z - 0.04, yaw, 1.2);
}
// three claw marks cut into a wall's face (frame turned by yaw, the face at z)
function claws(F, x, y, z, yaw = 0) {
  const S = F.at(x, 0, z, yaw);
  for (let i = 0; i < 3; i++) S.add("stone_block", { w: 0.035, h: 0.55, d: 0.02, mat: "dark" }, [i * 0.1, y + i * 0.03, 0], [0, 0, 18], "Декор");
}
// a hide stretched on a frame of two poles
function hide(F, x, z, yaw = 0, mat = "fur") {
  const S = F.at(x, 0, z, yaw);
  for (const s of [-1, 1]) S.add("post", { h: 2.0, r: 0.04, mat: "wood_old" }, [s * 0.7, 0, 0], [0, 0, 0], "Декор");
  S.add("beam", { L: 1.6, h: 0.06, d: 0.06, mat: "wood_old" }, [0, 1.85, 0], [0, 0, 0], "Декор");
  S.add("plank", { L: 1.2, w: 1.3, t: 0.02, mat }, [0, 1.15, 0], [90, 0, 0], "Декор");
}

// ------------------------------------------------------------------ a Xiong log house
// W × D: the walls' centre lines; h: the walls' height over the floor; ph: the stone footing's height (the floor).
// front, back, left, right: openings ([token, x or z in the house's axes]); for the sides the position is along Z.
// porch: { kind: "canopy", x, w } a little roof over a door, or { kind: "posts", depth, x0, x1 } a porch on posts.
function logHouse(F, o) {
  const W = o.W, D = o.D, h = o.h ?? 9 * 0.92 * 0.32, t = o.t ?? 0.32, ph = o.ph ?? 0.45, dh = 0.92 * t, e = t / 2 + 0.2;
  const floor = ph, pm = o.pm ?? 0.25, porch = o.porch;
  const pD = porch && porch.kind === "posts" ? porch.depth : 0;
  plinth(F, { W: W + 2 * pm, D: D + 2 * pm + pD, cz: pD / 2, h: ph });
  if (o.boulders !== false) boulders(F, { W: W + 2 * pm - 0.2, D: D + 2 * pm + pD - 0.2, cz: pD / 2, size: o.rock || 0.9, mid: o.mid, seed: o.seed });
  const common = { t, floor, h, door: o.door, shutters: o.shutters, lattice: o.lattice, win: o.win, dtop: o.dtop, Dtop: o.Dtop, sill: o.sill, wtop: o.wtop, vsill: o.vsill };
  const walls = [
    ["front", { x: 0, z: D / 2, yaw: 0, L: W + 2 * e, base: floor }, (u) => u],
    ["back", { x: 0, z: -D / 2, yaw: 180, L: W + 2 * e, base: floor }, (u) => -u],
    ["left", { x: -W / 2, z: 0, yaw: -90, L: D + 2 * e, base: floor - dh / 2 }, (u) => u],
    ["right", { x: W / 2, z: 0, yaw: 90, L: D + 2 * e, base: floor - dh / 2 }, (u) => -u],
  ];
  for (const [k, w, map] of walls) {
    const open = (o[k] || []).map(([tok, u, wd]) => [tok, map(u), wd]);
    logWall(F, { ...common, ...w, open });
    // the doors' steps down to the ground
    for (const [tok, u] of open) if (tok === "d" || tok === "D") {
      const R = F.at(w.x, 0, w.z, w.yaw), out = k === "front" ? pm + pD : pm;
      steps(R, { x: u, z: out, H: ph, w: tok === "D" ? 2.0 : 1.3 });
    }
  }
  const plate = o.plateMat || "wood_dark";
  for (const sz of [-1, 1]) F.add("beam", { L: W + 2 * e - 0.1, h: 0.2, d: t, mat: plate }, [0, floor + h, sz * D / 2], [0, 0, 0], "Каркас");
  for (const sx of [-1, 1]) F.add("beam", { L: D - t, h: 0.2, d: t, mat: plate }, [sx * W / 2, floor + h, 0], [0, 90, 0], "Каркас");
  const top = floor + h + 0.2;
  let overF = o.overF ?? o.over ?? 0.6;
  if (porch && porch.kind === "posts") {
    // a porch on posts along the front: its own slope under the main eave
    const pitch = porch.pitch ?? 24, x0 = porch.x0 ?? -W / 2, x1 = porch.x1 ?? W / 2, L = x1 - x0, cx = (x0 + x1) / 2;
    const yA = floor + h - 0.05, run = pD + 0.45, eave = leanTo(F, { x: cx, z: D / 2 + t / 2, y: yA, L: L + 0.4, run, pitch });
    const zp = D / 2 + pD - 0.15, yb = yA - (zp - D / 2 - t / 2) * Math.tan(pitch * D2R) - 0.28;
    const np = Math.max(2, Math.round(L / 2.6) + 1);
    for (let i = 0; i < np; i++) F.add("post", { h: yb - floor, r: 0.12, mat: o.postMat || "wood_dark", bands: "dark" }, [x0 + 0.1 + i * (L - 0.2) / (np - 1), floor, zp], [0, 0, 0], "Каркас");
    F.add("beam", { L: L + 0.2, h: 0.24, d: 0.2, mat: "wood_dark" }, [cx, yb, zp], [0, 0, 0], "Каркас");
    overF = Math.min(overF, 0.4);
    o.eave = eave;
  } else if (porch && porch.kind === "canopy") {
    // a little roof over the door on two brackets
    const yA = floor + Math.min(h - 0.1, (o.dtop ?? 7 * dh) + 0.55);
    leanTo(F, { x: porch.x || 0, z: D / 2 + t / 2, y: yA, L: porch.w || 1.8, run: 1.05, pitch: 32 });
    for (const s of [-1, 1]) F.add("bracket", { L: 0.7, mat: "wood_dark" }, [(porch.x || 0) + s * ((porch.w || 1.8) / 2 - 0.2), yA - 0.85, D / 2 + t / 2], [0, 0, 0], "Каркас");
  }
  const ridge = thatch(F, { W, D, top, pitch: o.pitch ?? 42, overF, overB: o.overB ?? o.over ?? 0.6, side: o.side ?? 0.45, logGable: o.logGable !== false, t: t * 0.9, gables: o.gables });
  return { floor, top, ridge, e, pm, pD, eave: o.eave, gh: (D / 2) * Math.tan((o.pitch ?? 42) * D2R) };
}

// ------------------------------------------------------------------ a mortal's mud house
// as above, mud walls with corner posts on a low rubble footing (the artist's hut_mud), or another wall type.
function mudHouse(F, o) {
  const W = o.W, D = o.D, h = o.h ?? 2.3, t = o.t ?? 0.3, ph = o.ph ?? 0.25, type = o.type || "mud", pm = o.pm ?? 0.15;
  const floor = ph;
  if (o.stilts) {
    // a floor on stilts: posts on stones, joists, planks; a stair up to the door
    const xs = [], zs = [];
    for (let i = 0; i <= Math.round(W / 1.6); i++) xs.push(-W / 2 + i * W / Math.round(W / 1.6));
    for (let i = 0; i <= Math.round(D / 1.6); i++) zs.push(-D / 2 + i * D / Math.round(D / 1.6));
    for (const x of xs) for (const z of zs) F.add("stilt", { h: ph - 0.16, r: 0.11 }, [x, -0.05, z], [0, 0, 0], "Фундамент");
    for (const z of zs) F.add("floor_joist", { L: W + 0.3, h: 0.16, d: 0.14 }, [0, ph - 0.21, z], [0, 0, 0], "Фундамент");
    const nx = Math.ceil((W + 0.3) / 4 - 1e-6), nz = Math.ceil((D + 0.3 + (o.deck || 0)) / 4 - 1e-6), DD = D + 0.3 + (o.deck || 0);
    for (let i = 0; i < nx; i++) for (let j = 0; j < nz; j++) F.add("floor_plank", { w: (W + 0.3) / nx, d: DD / nz, t: 0.05, mat: "wood_old" }, [-(W + 0.3) / 2 + (i + 0.5) * (W + 0.3) / nx, ph - 0.05, -(D + 0.3) / 2 + (j + 0.5) * DD / nz], [0, 0, 0], "Пол");
  } else plinth(F, { W: W + 2 * pm, D: D + 2 * pm, h: ph, style: o.plinth || "rubble" });
  const common = { t, floor, h, type, door: o.door, curtain: o.curtain, shutters: o.shutters !== false, lattice: o.lattice, win: o.win, sill: o.sill, wtop: o.wtop };
  const walls = [
    ["front", { x: 0, z: D / 2, yaw: 0, L: W }, (u) => u],
    ["back", { x: 0, z: -D / 2, yaw: 180, L: W }, (u) => -u],
    ["left", { x: -W / 2, z: 0, yaw: -90, L: D - t }, (u) => u],
    ["right", { x: W / 2, z: 0, yaw: 90, L: D - t }, (u) => -u],
  ];
  for (const [k, w, map] of walls) {
    const open = (o[k] || []).map(([tok, u, wd]) => [tok, map(u), wd]);
    if (o.stoneBase) {
      // a stone wall to the sill, mud above
      F.at(w.x, 0, w.z, w.yaw).add("wall_stone", { w: w.L, h: o.stoneBase, t: t + 0.06 }, [0, floor, 0], [0, 0, 0], "Стены");
    }
    kitWall(F, { ...common, ...w, open, floor: o.stoneBase ? floor : floor, h });
    if (!o.stilts) for (const [tok, u] of open) if (tok === "d" || tok === "D") {
      const R = F.at(w.x, 0, w.z, w.yaw);
      if (ph > 0.2) R.add("step_natural", { w: 1.0, tread: 0.4, rise: Math.min(0.2, ph * 0.55), seed: 3 + Math.round(u * 7), mat: "stone" }, [u, 0, pm + 0.42], [0, 0, 0], "Лестницы");
    }
  }
  const pmat = o.posts === undefined ? "wood_old" : o.posts;
  if (pmat) for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h, a: t + 0.06, mat: pmat }, [sx * W / 2, floor, sz * D / 2], [0, 0, 0], "Каркас");
  for (const sz of [-1, 1]) F.add("beam", { L: W + t + 0.06, h: 0.2, d: t + 0.04, mat: "wood_old" }, [0, floor + h, sz * D / 2], [0, 0, 0], "Каркас");
  for (const sx of [-1, 1]) F.add("beam", { L: D - t - 0.06, h: 0.2, d: t + 0.04, mat: "wood_old" }, [sx * W / 2, floor + h, 0], [0, 90, 0], "Каркас");
  const top = floor + h + 0.2;
  const ridge = thatch(F, { W, D, top, pitch: o.pitch ?? 38, over: o.over ?? 0.5, overF: o.overF, overB: o.overB, side: o.side ?? 0.4, gable: o.gable || (type === "plank" ? "plank" : type === "bamboo" ? "bamboo" : "mud"), t, gables: o.gables });
  return { floor, top, ridge };
}

// a low mud wall under a straw coping along a line (a mortal's yard)
function mudFence(F, x0, z0, x1, z1, h = 1.1) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), yaw = Math.atan2(-dz, dx) / D2R, n = Math.max(1, Math.ceil(L / 6 - 1e-6));
  const R = F.at((x0 + x1) / 2, 0, (z0 + z1) / 2, yaw);
  for (let i = 0; i < n; i++) R.add("wall_mud", { w: L / n, h: h + 0.15, t: 0.28 }, [-L / 2 + (i + 0.5) * L / n, -0.15, 0], [0, 0, 0], "Ограждения");
  for (let i = 0; i < Math.ceil(L / 4 - 1e-6); i++) { const m = Math.ceil(L / 4 - 1e-6), w = L / m; R.add("roof_ridge", { w: w + 0.1, style: "low", mat: "thatch" }, [-L / 2 + (i + 0.5) * w, h + 0.05, 0], [0, 0, 0], "Ограждения"); }
}
// a rail fence along a line, posts every module
function railFence(F, x0, z0, x1, z1, mat = "wood_old") {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), yaw = Math.atan2(-dz, dx) / D2R, n = Math.max(1, Math.round(L / 2.5));
  const R = F.at((x0 + x1) / 2, 0, (z0 + z1) / 2, yaw);
  for (let i = 0; i < n; i++) R.add("fence_rail", { w: L / n, mat }, [-L / 2 + (i + 0.5) * L / n, 0, 0], [0, 0, 0], "Ограждения");
  R.add("fence_post", { h: 1.25, a: 0.11 }, [L / 2, 0, 0], [0, 0, 0], "Ограждения");
}

// ================================================================== the Xiong
const XS = "Набор · Сюн", MS = "Набор · хутора";
const X = (id, label, kind, build, more = {}) => ({ id: "xiong_" + id, label, kind, clan: "Xiong", section: XS, replaces: "", ...more, build: (houses, kit, THREE) => centre(build(houses, kit, THREE), kit, THREE, more.yard) });
const M = (id, label, kind, build, more = {}) => ({ id: "mortal_" + id, label, kind, clan: "Mortal", section: MS, replaces: "", ...more, build: (houses, kit, THREE) => centre(build(houses, kit, THREE), kit, THREE, more.yard) });
const frame = (houses) => { const items = []; return [items, new houses.Frame(items)]; };

const RECIPES_X = [];

// --- the families' houses
RECIPES_X.push(X("family_a", "Сруб охотника", "Family", (houses) => {
  const [items, F] = frame(houses);
  const r = logHouse(F, { W: 3.6, D: 2.6, front: [["d", -0.4], ["n", 1.0]], back: [["f", 0]], shutters: true });
  skull(F, -0.4, r.floor + 2.5, 1.5); claws(F, 0.45, r.floor + 0.9, 1.48);
  F.add("lantern_hanging", { size: 0.8 }, [0.5, r.top - 0.2, 1.8]);
  F.add("firewood", { kind: "chopping" }, [2.2, 0, 1.0], [0, -40, 0]);
  return items;
}));

RECIPES_X.push(X("pavilion", "Великий бревенчатый зал Сюн", "Pavilion", (houses) => {
  const [items, F] = frame(houses);
  const t = 0.4, dh = 0.92 * t;
  const r = logHouse(F, { W: 17, D: 11, t, h: 13 * dh, ph: 1.0, pitch: 45, overB: 0.9, side: 0.9, rock: 1.4, mid: [-0.5, 0, 0.5],
    porch: { kind: "posts", depth: 2.6, pitch: 22 }, dtop: 7 * dh, Dtop: 8 * dh, sill: 5 * dh, wtop: 8 * dh,
    front: [["D", 0], ["n", -4.5], ["n", 4.5], ["n", -7], ["n", 7]], back: [["n", -4], ["n", 4]], left: [["n", -2], ["n", 2]], right: [["n", -2], ["n", 2]], shutters: true });
  for (const sx of [-1, 1]) { skull(F, sx * (17 / 2 + 0.35), r.top + r.gh * 0.45, 0, sx * 90, 2.2); horns(F, sx * (17 / 2 + 0.2), r.top + r.gh - 0.6, 0, sx * 90, 1.8); }
  skull(F, 0, r.floor + 8 * dh + 0.55, 11 / 2 + 0.3, 0, 1.6);
  for (const x of [-6.5, -2.2, 2.2, 6.5]) skull(F, x, r.eave - 0.15, 11 / 2 + 2.45, 0, 0.9);
  for (const x of [-5.6, 5.6]) F.add("brazier", { kind: "tripod" }, [x, r.floor, 11 / 2 + 1.3]);
  for (const x of [-3, 3]) F.add("lantern_hanging", { size: 1.2 }, [x, r.eave - 0.3, 11 / 2 + 2.45]);
  for (const x of [-9.2, 9.2]) F.add("banner", { h: 5, color: "silk_red", sign: "glyph" }, [x, 0, 7.6]);
  totem(F, -2.3, 8.8, 3.4); totem(F, 2.3, 8.8, 3.4);
  return items;
}));

export const RECIPES = [...RECIPES_X];
