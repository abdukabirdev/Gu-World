// The Bai clan's village, assembled from the artist's kit (export.mjs reads RECIPES).
//
// The Bai live by a deep pool under a waterfall, and their buildings are the cold, orderly kind: whitewashed walls on
// grey stone plinths, steep straight roofs of dark grey tile with only a little upturn at the eaves (pitch 32–38°),
// small high windows with crisp geometric lattices, black-lacquered doors under little tiled door hoods, stepped
// "horse-head" gable walls rising above the roofs of the better houses, grey stone quoins at the corners of the
// plainer ones, blue and white lanterns, white banners. Their trade is white grain wine, so the wine shop, the tavern
// and the distillery (with its stills) stand among the houses, the smithy, the herbalist, the granary and the Gu farm.
// The clan's own buildings are the three-storey pavilion facing the waterfall, the clan hall, the ancestral hall
// with its gate range and court, the academy around its yard and the long two-storey hostel.
//
// Everything is built the way the artist's houses in kit/houses.js are, but with helpers of our own (below) tuned to
// the game's triangle budgets:
//   - a plastered wall is the same few triangles at 1 m or 6 m, so runs of plain wall are merged into wide modules;
//     windows and doors keep their own modules (a run's nominal metres are stretched to the wall's real length);
//   - a tiled slope costs ~250 triangles a metre of ridge whatever its depth (~175 without the eave kick), the tiled
//     ridge ~170 a metre, the "low" ridge almost nothing: big roofs get the tiled ridge, small ones and yard walls the
//     low one; the cheap "wan" lattice is the default, the ice-crack one is kept for the clan's halls;
//   - platform blocks go in cells up to 4 × 4 m, stairs are whole flights (stair_flight), not single steps;
//   - lanterns, jars, furniture and goods are decoration (Detail: no shadow, not solid) and kept few.
// The stepped horse-head walls (wall_horsehead) are the side walls of a house from its plinth up: horseWalls() picks
// the number of steps so that every step stands above the roof slope behind it and the middle one above the ridge.
// Conventions as in the kit: metres, Y up, the entrance faces +Z, footings sink 0.25 m below the ground, and centred()
// moves every model so its footprint (eaves and steps included) is centred on the origin.

const ZERO = [0, 0, 0];
const D2R = Math.PI / 180;
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);
const r2 = (v) => Math.round(v * 100) / 100;

// L split into n equal cells no longer than max: [[centre offset from the middle, size], …]
function cells(L, max = 4) {
  const n = Math.max(1, Math.ceil(L / max - 1e-6)), s = L / n;
  return Array.from({ length: n }, (_, i) => [-L / 2 + (i + 0.5) * s, s]);
}

// ------------------------------------------------------------------ ground: plinths, steps, paving
// A stone plinth W × D with its top at h, sunk 0.25 m into the ground.
function plinth(F, o) {
  const { W, D, h } = o, cx = o.cx || 0, cz = o.cz || 0, sink = o.sink ?? 0.25, y = o.y || 0;
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D))
    F.add("found_block", { w: sw, d: sd, h: h + sink, style: o.style || "ashlar" }, [cx + ux, y - sink, cz + uz], ZERO, o.g || "Фундамент");
}
// A stone flight from y up H that ends at the edge z (it comes toward +Z of the frame); returns its run.
function steps(F, o) {
  const tread = o.tread || 0.3, S = F.at(o.x || 0, 0, o.z, o.yaw || 0);
  if (o.H < 0.22) { S.add("step_stone", { w: o.w, tread: 0.35, rise: Math.max(0.08, o.H) }, [0, o.y || 0, 0.35], ZERO, "Лестницы"); return 0.35; }
  const n = Math.max(2, Math.round(o.H / (o.rise || 0.16))), rise = o.H / n;
  S.add("stair_flight", { n, w: o.w, tread, rise, kind: o.kind || "stone" }, [0, o.y || 0, n * tread], ZERO, "Лестницы");
  return n * tread;
}
// Stone slabs (the 2 m path module, up to 3 m wide) over W × D, top at y + 0.06.
function pave(F, o) {
  const { W, D } = o, cx = o.cx || 0, cz = o.cz || 0, y = o.y || 0;
  for (const [ux, sw] of cells(W, 3)) for (const [uz] of cells(D, 2)) F.add("path_module", { kind: "slab", w: sw }, [cx + ux, y, cz + uz], ZERO, "Фундамент");
}

// ------------------------------------------------------------------ walls
// One run of wall along X of a frame at (x, z, yaw), its tokens laid from −X to +X, each nominally so many metres and
// all stretched together to the run's real length L:
//   w h W X Y — plain wall 1, 0.5, 2, 3, 4 m (runs of them are merged into modules up to 6 m);
//   n — 1 m small high window (0.5 × 0.6) with a lattice;  N — 2 m wide window (1.2 × 0.8);  a — 1 m window with a
//   propped flap;  o — 2 m octagonal lattice window;  q — 2 m octagonal opening;
//   d — 1 m door (plank leaf);  D — 2 m double door (black lacquer);  G — 3 m great double door;  c — 1 m doorway with a
//   curtain;  k — 1 m doorway;  s S — 2 m / 3 m open shop front;  p P — 1 m / 2 m open bay (no wall at all).
// Doors stand open inward (−Z) as the artist's do; wall_door has a 0.14 m sill.
const TW = { w: 1, h: 0.5, W: 2, X: 3, Y: 4, n: 1, N: 2, a: 1, o: 2, q: 2, d: 1, D: 2, G: 3, c: 1, k: 1, s: 2, S: 3, p: 1, P: 2 };
const SMOOTH = { plaster: "plaster", grey: "plaster_grey", ochre: "plaster_ochre", mud: "mud" };
function wall(F, spec, o) {
  const toks = spec.replace(/\s+/g, "").split(""), L0 = toks.reduce((n, t) => n + TW[t], 0), k = o.L ? o.L / L0 : 1;
  const { y, h } = o, t = o.t ?? 0.24, type = o.type || "plaster", R = F.at(o.x || 0, 0, o.z || 0, o.yaw || 0);
  const win = o.win || "wan", lat = o.lattice || "wood_dark", leaf = o.leaf || "black";
  let u = -L0 * k / 2, plain = 0, p0 = 0;
  const flush = () => { if (plain > 1e-6) for (const [c, w] of cells(plain, 6)) R.add("wall_" + type, { w, h, t }, [p0 + plain / 2 + c, y, 0], ZERO, "Стены"); plain = 0; };
  for (const tk of toks) {
    const w = TW[tk] * k, c = u + w / 2;
    if ("whWXY".includes(tk)) { if (plain === 0) p0 = u; plain += w; u += w; continue; }
    flush(); u += w;
    if (tk === "n" || tk === "N" || tk === "a") {
      const big = tk === "N", ww = big ? 1.2 : o.ww || (tk === "a" ? 0.6 : 0.5), wh = big ? 0.8 : o.wh || 0.6;
      const sill = Math.min(big ? o.sillN ?? 1.0 : o.sill ?? 1.5, h - wh - 0.15);
      R.add("wall_window", { w, h, t, type, ww, wh, sill }, [c, y, 0], ZERO, "Стены");
      if (tk === "a") R.add("window_awning", { w: ww, h: wh, mat: "wood_dark", open: 35 }, [c, y + sill, t / 2 + 0.02], ZERO, "Окна и двери");
      else R.add("window_lattice", { w: ww, h: wh, pattern: big ? o.winN || win : win, mat: lat }, [c, y + sill, 0], ZERO, "Окна и двери");
    } else if (tk === "o" || tk === "q") {
      R.add("wall_window_shaped", { w, h, t, shape: o.shape || "octagon", size: 0.9, cy: Math.min(1.5, h - 0.7), mat: SMOOTH[type] || "plaster", fill: tk === "o" ? "lattice" : "open" }, [c, y, 0], ZERO, "Стены");
    } else if (tk === "d") {
      R.add("wall_door", { w, h, t, type, dw: 0.8, dh: 2.05 }, [c, y, 0], ZERO, "Стены");
      R.add("door_leaf", { w: 0.8, h: 1.9, style: o.door || "plank" }, [c - 0.4, y + 0.14, -t / 2 + 0.03], [0, 72, 0], "Окна и двери");
    } else if (tk === "D") {
      R.add("wall_door", { w, h, t, type, dw: 1.3, dh: 2.2 }, [c, y, 0], ZERO, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 0.65, h: 2.06, style: leaf }, [c + s * 0.65, y + 0.14, -t / 2 + 0.03], [0, 90 - s * 10, 0], "Окна и двери");
    } else if (tk === "G") {
      R.add("wall_door", { w, h, t, type, dw: 2.0, dh: 2.6 }, [c, y, 0], ZERO, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 1.0, h: 2.46, style: leaf }, [c + s * 1.0, y + 0.14, -t / 2 + 0.03], [0, 90 - s * 12, 0], "Окна и двери");
    } else if (tk === "c" || tk === "k") {
      R.add("wall_door", { w, h, t, type, dw: tk === "k" ? 0.9 : 0.8, dh: 2.05 }, [c, y, 0], ZERO, "Стены");
      if (tk === "c") R.add("door_curtain", { w: 0.8, h: 1.9, color: o.curtain || "cloth_blue" }, [c, y + 0.14, t / 2 + 0.04], ZERO, "Окна и двери");
    } else if (tk === "s" || tk === "S") {
      R.add("wall_door", { w, h, t, type, dw: tk === "S" ? Math.min(2.6, w - 0.3) : Math.min(1.7, w - 0.3), dh: Math.min(2.4, h - 0.3) }, [c, y, 0], ZERO, "Стены");
    }
  }
  flush();
  return L0 * k;
}
// The centre (along the run) of the i-th token ch of a spec stretched to L.
function tokX(spec, L, ch, i = 0) {
  const toks = spec.replace(/\s+/g, "").split(""), k = L / toks.reduce((n, t) => n + TW[t], 0);
  let u = -L / 2;
  for (const tk of toks) { if (tk === ch && i-- === 0) return u + TW[tk] * k / 2; u += TW[tk] * k; }
  throw new Error(`no ${ch} in ${spec}`);
}
// The four walls of a W × D body (front +Z, back −Z, left −X, right +X; a side's spec runs back → front on the left,
// front → back on the right), corner posts (grey stone quoins by default) and beams on top; returns the beams' top.
// Without posts the front and back runs reach over the side walls' ends so the corners close.
function body(F, o) {
  const { W, D, y, h } = o, t = o.t ?? 0.24, cx = o.cx || 0, cz = o.cz || 0, pm = o.posts === undefined ? "stone_light" : o.posts;
  const LW = pm ? W : W + t, w = { ...o, t };
  if (o.front) wall(F, o.front, { ...w, L: LW, x: cx, z: cz + D / 2, yaw: 0 });
  if (o.back) wall(F, o.back, { ...w, L: LW, x: cx, z: cz - D / 2, yaw: 180 });
  if (o.left) wall(F, o.left, { ...w, L: D, x: cx - W / 2, z: cz, yaw: -90 });
  if (o.right) wall(F, o.right, { ...w, L: D, x: cx + W / 2, z: cz, yaw: 90 });
  if (pm) for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h, a: t + 0.08, mat: pm }, [cx + sx * W / 2, y, cz + sz * D / 2], ZERO, "Каркас");
  if (o.beams !== false) {
    const bm = o.beam || "wood_dark";
    for (const sz of [-1, 1]) if (sz > 0 ? o.front !== null || o.beamFront : o.back !== null) for (const [u, L] of cells(W + t + 0.06, 12)) F.add("beam", { L, h: 0.22, d: t + 0.04, mat: bm }, [cx + u, y + h, cz + sz * D / 2], ZERO, "Каркас");
    for (const sx of [-1, 1]) for (const [u, L] of cells(D - t - 0.06, 12)) F.add("beam", { L, h: 0.22, d: t + 0.04, mat: bm }, [cx + sx * W / 2, y + h, cz + u], [0, 90, 0], "Каркас");
  }
  return y + h + 0.22;
}
// Round columns on square stone bases from y to y + h along X at z, a beam over them.
function columns(F, o) {
  const { xs, z, y, h } = o, r = o.r || 0.13, mat = o.mat || "wood_dark";
  for (const x of xs) {
    F.add("stone_footing", { r: r * 1.05, design: "square", mat: "stone_light" }, [x, y, z], ZERO, "Каркас");
    F.add("post", { h: h - 0.22, r, mat }, [x, y + 0.22, z], ZERO, "Каркас");
  }
  if (o.beam !== false) {
    const x0 = Math.min(...xs) - 0.2, x1 = Math.max(...xs) + 0.2;
    for (const [u, L] of cells(x1 - x0, 12)) F.add("beam", { L, h: 0.22, d: 0.22, mat: o.beamMat || "wood_dark" }, [(x0 + x1) / 2 + u, y + h, z], ZERO, "Каркас");
  }
  return y + h + 0.22;
}
// An upper floor W × D at y: joists along Z, boards over them; returns the boards' top.
function deck(F, o) {
  const { W, D, y } = o, cx = o.cx || 0, cz = o.cz || 0, nj = Math.max(2, Math.round(W / 1.6) + 1);
  for (let i = 0; i < nj; i++) for (const [u, L] of cells(D, 8)) F.add("floor_joist", { L, h: 0.16, d: 0.14, mat: "wood_dark" }, [cx - W / 2 + 0.1 + (W - 0.2) * i / (nj - 1), y, cz + u], [0, 90, 0], "Пол");
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D)) F.add("floor_plank", { w: sw, d: sd, mat: o.mat || "wood_old" }, [cx + ux, y + 0.16, cz + uz], ZERO, "Пол");
  return y + 0.21;
}
// A railing from (x0, z0) to (x1, z1) at height y in sections up to 2 m.
function rail(F, x0, z0, x1, z1, y, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  for (const [u, w] of cells(L, 2)) F.add("railing", { w, h: o.h || 0.95, mat: o.mat || "wood_dark", style: o.style || "cross" }, [x0 + ux * (L / 2 + u), y, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
  if (o.end !== false) F.add("newel", { h: (o.h || 0.95) + 0.1, mat: o.mat || "wood_dark", top: "pyramid" }, [x1, y, z1], ZERO, "Ограждения");
}

// ------------------------------------------------------------------ roofs
// The stepped horse-head walls at both ends of a gable roof: from the plinth (y0) up, each step above the slope behind
// it, the middle one above the ridge. w = the body's depth + the wall thickness.
function horseWalls(F, o) {
  const { W, w, y0, top, ridgeY, tp } = o, cx = o.cx || 0, cz = o.cz || 0;
  let n = 2; while (n < 4 && (w / (2 * n + 1)) * tp > 0.6) n++;
  const sw = w / (2 * n + 1);
  let h = top - y0 + 0.15 + sw * tp;
  h = Math.max(h, ridgeY - y0 + 0.45 - 0.8 * n);
  for (const sx of [-1, 1]) F.add("wall_horsehead", { w: r2(w), h: r2(Math.min(10, h)), steps: n, t: 0.3, mat: o.mat || "plaster" }, [cx + sx * W / 2, y0, cz], [0, 90, 0], "Стены");
  return y0 + h + 0.8 * n + 0.33;
}
// A gable roof over a W × D body whose beams top out at `top` (ridge along X): slopes in modules up to 4 m, the ridge,
// and either plaster gables with verges and ridge ends (side overhang `side`) or horse-head walls (horse: their base y).
// Returns the ridge's y.
function roof(F, o) {
  const { W, D, top } = o, cx = o.cx || 0, cz = o.cz || 0, pitch = o.pitch ?? 34, tp = Math.tan(pitch * D2R), over = o.over ?? 0.55;
  const kick = Math.min(o.kick ?? 0.25, over), mat = o.mat || "tile", run = D / 2 + over, runF = D / 2 + (o.overFront ?? over), ridgeY = top + 0.05 + (D / 2) * tp;
  const horse = o.horse !== undefined, side = horse ? 0 : o.side ?? 0.35, span = W + 2 * side;
  for (const [u, w] of cells(span)) {
    for (const ry of [0, 180]) F.add("roof_tile_slope", { w, run: ry ? run : runF, pitch, kick, mat }, [cx + u, ridgeY, cz], [0, ry, 0], "Крыша");
    F.add("roof_ridge", { w, style: o.ridge || "tile", mat }, [cx + u, ridgeY, cz], ZERO, "Крыша");
  }
  if (horse) horseWalls(F, { W, w: D + (o.t ?? 0.24), y0: o.horse, top, ridgeY, tp, cx, cz, mat: o.horseMat });
  else for (const sx of [-1, 1]) {
    for (const ry of [0, 180]) F.add("roof_verge", { run: ry ? run : runF, pitch, kick: Math.min(kick, 2) }, [cx + sx * span / 2, ridgeY, cz], [0, ry, 0], "Крыша");
    if (o.end !== null) F.add("roof_ridge_end", { size: o.endSize || 1, design: o.end || "simple", mat }, [cx + sx * (span / 2 - 0.12), ridgeY + 0.08, cz], [0, sx > 0 ? 0 : 180, 0], "Крыша");
    if (o.gable !== null) F.add("gable", { w: D, h: r2((D / 2) * tp), t: o.t ?? 0.24, type: o.gable || "plaster", fish: o.fish || "no" }, [cx + sx * W / 2, top, cz], [0, sx * 90, 0], "Стены");
  }
  if (o.finial) F.add("roof_finial", { size: o.finial, design: o.finialDesign || "gourd", mat: o.finialMat || "tile" }, [cx, ridgeY + 0.3, cz], ZERO, "Крыша");
  return ridgeY;
}
// A lean-to against a wall, from x0 to x1 along X of the frame, its top edge at (y, z), falling toward +Z (run out,
// straight unless kick); a low ridge seals it to the wall. Returns the eave's height at a distance d from the wall.
function pent(F, o) {
  const { x0, x1, z, y, run } = o, pitch = o.pitch ?? 30, kick = Math.min(o.kick ?? 0, run - 0.1), mat = o.mat || "tile", tp = Math.tan(pitch * D2R);
  const S = F;
  for (const [u, w] of cells(x1 - x0)) {
    S.add("roof_tile_slope", { w, run, pitch, kick, mat }, [(x0 + x1) / 2 + u, y, z], ZERO, "Крыша");
    S.add("roof_ridge", { w, style: "low", mat }, [(x0 + x1) / 2 + u, y + 0.03, z + 0.04], ZERO, "Крыша");
  }
  if (o.verges !== false) for (const x of [x0, x1]) S.add("roof_verge", { run, pitch, kick }, [x, y, z], ZERO, "Крыша");
  return (d) => y - Math.min(d, run - kick) * tp - Math.max(0, d - (run - kick)) * Math.tan(pitch * 0.55 * D2R);
}
// The little tiled hood over a door: a short straight lean-to w wide on the wall face (z), its top at y.
function hood(F, o) {
  pent(F, { x0: o.x - o.w / 2, x1: o.x + o.w / 2, z: o.z, y: o.y, run: o.run || 0.7, pitch: 30, kick: 0, verges: false });
  F.add("beam", { L: o.w - 0.1, h: 0.14, d: 0.12, mat: "wood_dark" }, [o.x, o.y - 0.32, o.z + 0.08], ZERO, "Каркас");
}
// A courtyard wall from (x0, z0) to (x1, z1): plastered on its stone skirting, a low tiled ridge on top.
function yardWall(F, x0, z0, x1, z1, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  const h = o.h || 2.3, t = o.t || 0.3;
  if (o.spec) wall(F.at((x0 + x1) / 2, 0, (z0 + z1) / 2, yaw), o.spec, { y: o.y || 0, h, t, L, type: o.type || "plaster", leaf: o.leaf, win: o.win });
  else for (const [u, w] of cells(L, 6)) F.add("wall_" + (o.type || "plaster"), { w, h, t }, [x0 + ux * (L / 2 + u), o.y || 0, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
  for (const [u, w] of cells(L + t, 4)) F.add("roof_ridge", { w, style: "low", mat: "tile" }, [x0 + ux * (L / 2 + u), (o.y || 0) + h + 0.05, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
}

// ------------------------------------------------------------------ decoration
const lamp = (F, x, z, y = 0, h = 2.2) => F.add("lantern_post", { h, color: "lantern_blue" }, [x, y, z], ZERO, "Декор");
const hang = (F, x, y, z, size = 0.8) => F.add("lantern_hanging", { size, color: "paper" }, [x, y, z], ZERO, "Декор");
const jar = (F, x, z, y = 0, kind = "wine_jar", size = 1, mat = "ceramic_brown") => F.add("vessel", { kind, mat, size }, [x, y, z], ZERO, "Мебель и утварь");

// ------------------------------------------------------------------ the footprint in the middle
// Shift everything so the model (the parts the exporter keeps: the building and what stands on or against it) is
// centred on the origin in X and Z, as the exporter measures it.
function centred(items, kit, THREE, yard) {
  const e = new THREE.Euler(), q = new THREE.Quaternion(), one = new THREE.Vector3(1, 1, 1);
  const boxOf = (it) => {
    const r = it.r || ZERO, M = new THREE.Matrix4().compose(new THREE.Vector3(...it.p), q.setFromEuler(e.set(r[0] * D2R, r[1] * D2R, r[2] * D2R)), one);
    return kit.buildPart(it.part, it.params).bbox.clone().applyMatrix4(M);
  };
  const shell = new THREE.Box3();
  for (const it of items) if (!DETAIL.has(it.g)) shell.union(boxOf(it));
  const all = new THREE.Box3(), c = new THREE.Vector3();
  for (const it of items) {
    const b = boxOf(it);
    if (DETAIL.has(it.g) && !yard) { b.getCenter(c); if (c.x < shell.min.x - 0.4 || c.x > shell.max.x + 0.4 || c.z < shell.min.z - 0.4 || c.z > shell.max.z + 0.4) continue; }
    all.union(b);
  }
  const mx = (all.min.x + all.max.x) / 2, mz = (all.min.z + all.max.z) / 2;
  for (const it of items) it.p = [Math.round((it.p[0] - mx) * 1e4) / 1e4, it.p[1], Math.round((it.p[2] - mz) * 1e4) / 1e4];
  return items;
}

// One recipe: its id (bai_…), label, kind and the function that builds it on a frame.
function R(id, label, kind, fn, extra = {}) {
  return {
    id: "bai_" + id, label, kind, clan: "Bai", section: "Набор · Бай", replaces: "", ...extra,
    build(houses, kit, THREE) { const items = []; fn(new houses.Frame(items), kit); return centred(items, kit, THREE, extra.yard); },
  };
}

// ------------------------------------------------------------------ a whitewashed house
// Plinth (front margin mf for the stoop), storeys of walls, the gable roof (plaster gables or horse-head walls), the
// steps up to each door and the door hoods. o.storeys: [{h, front, back, left, right, type?, …wall options}].
function house(F, o) {
  const { W, D } = o, cx = o.cx || 0, cz = o.cz || 0, t = o.t ?? 0.24, ph = o.plinth ?? 0.45, m = o.margin ?? 0.15, mf = o.mf ?? 0.35;
  const horse = o.gable === "horse";
  if (ph > 0) plinth(F, { W: W + 2 * m + (horse ? 0.1 : 0), D: D + m + mf, h: ph, cx, cz: cz + (mf - m) / 2, style: o.stone || "ashlar" });
  let y = ph, top = ph;
  for (const s of o.storeys) {
    top = body(F, { win: o.win, leaf: o.leaf, door: o.door, lattice: o.lattice, ...s, W, D, y, t, cx, cz, type: s.type || o.type || "plaster",
      left: horse ? null : s.left, right: horse ? null : s.right, posts: horse ? null : o.posts, beam: o.beam });
    y = top;
  }
  const ridgeY = roof(F, { W, D, top, cx, cz, t, ...(o.roof || {}), horse: horse ? ph : undefined, gable: horse ? undefined : o.gable });
  for (const s of o.steps ?? [{ x: 0, w: 1.3 }]) steps(F, { x: cx + s.x, z: cz + D / 2 + mf, H: ph, w: s.w });
  for (const hd of o.hoods || []) hood(F, { x: cx + hd.x, w: hd.w || 1.8, y: ph + (hd.y || 2.75), z: cz + D / 2 + t / 2 });
  return { top, ridgeY, y0: ph, front: cz + D / 2 + mf };
}

// ================================================================== Family houses
const FAMILY = [
  // 4 × 3: one room, plaster gables, grey stone quoins, a plank door and one window.
  R("house_small", "Белёный домик", "Family", (F) => {
    const H = house(F, { W: 3.3, D: 2.1, plinth: 0.3, mf: 0.3, roof: { over: 0.4, side: 0.25, kick: 0.15, ridge: "low", end: "simple" },
      storeys: [{ h: 2.7, front: "wndw", back: "wnw", left: "w", right: "w" }], steps: [{ x: 0.1, w: 1.0 }] });
    F.add("water_jar", { h: 0.6 }, [-1.25, H.y0, 1.2]);
  }),
  // 5 × 4: one storey between horse-head walls, a door hood over the black double door.
  R("house_horse", "Дом с конскими стенами", "Family", (F) => {
    const H = house(F, { W: 4.4, D: 2.8, gable: "horse", plinth: 0.45, roof: { over: 0.5, kick: 0.2 },
      storeys: [{ h: 3.0, front: "wnDnw", back: "wnwnw" }], hoods: [{ x: 0, w: 2.0 }], steps: [{ x: 0, w: 1.6 }] });
    lamp(F, 1.6, H.front + 0.3);
  }),
];

export const RECIPES = [...FAMILY];
