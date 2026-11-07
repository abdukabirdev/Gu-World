// The trades of a Gu Yue village, built from the artist's kit (2026-10-31): the shops big and small, the herbalist,
// the smithy, the clan granary, the noodle eatery, the teahouse, the tavern, the inn and the caravan's gambling tent.
// Each stands in for one of the old models (`replaces`) at the size the village plans place it, its entrance facing +Z
// and its footprint centred on the origin (centre() below shifts the finished house the way the exporter measures it).
//
// The houses are put together from the artist's parts with helpers of our own, close to his in kit/houses.js but
// lighter for the game: walls() lays a wall from a token string as his wallRun() does, but runs of plain wall become one
// module (up to 6 m) instead of one per metre, and it knows open bays, open bays under a transom, propped shop windows,
// moon and octagonal windows; gable() is his gableRoof() with the tile's colour, the ridge's kind and the ridge ends as
// choices; pent() is a single-pitch eave against a wall (the skirt roof over a shopfront or under a balcony) with posts
// under its eave; plinth() and planks() use blocks and boards up to 4 m. Interior floors are board ceilings
// (ceiling_panel: one board and battens) where only their top shows; the balconies and decks get real planks.
// Style: dark timber posts and beams, white plaster, blue-grey tile (some tile_blue), upturned eaves and swallow-tail
// ridge ends, pale paper lanterns (lantern_white), moon windows on the teahouse and the herbalist; the tavern alone has
// warm red lanterns, and the caravan's tent is striped red and gold cloth.
//
// Two values outside a part's option list are used on purpose: lantern_hanging takes the lantern materials of
// lantern_post (lantern_white) as its paper, and the gambling tent's cloth is made of beams in silk materials (beam's
// own materials are woods; the kit has no large cloth sheet). Both are only material names, which the kit builds as is.

const D2R = Math.PI / 180;
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);

// ------------------------------------------------------------------ centring
// The exporter measures a house over its parts, leaving out the décor that stands away from it (its centre more than
// 0.4 m outside the house itself) unless the recipe keeps its yard. The same here, then the house is moved so that
// box's middle is at the origin in X and Z.
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
  const r4 = (v) => Math.round(v * 1e4) / 1e4;
  for (const it of items) it.p = [r4(it.p[0] - dx), it.p[1], r4(it.p[2] - dz)];
  return items;
}

// ------------------------------------------------------------------ walls
// Tokens (metres): w 1 plain, h 0.5 plain, p 1 open, q 0.5 open, P 1 open under a transom (from o.lintel up),
// n 1 window (lattice), s 1 small high window, a 2 shop window (wide opening, a propped flap and a sill board),
// d 1 door (a leaf ajar inside), c 1 doorway with a curtain, D 2 double door, o 2 moon window, x 2 octagonal window.
const TW = { w: 1, h: 0.5, p: 1, q: 0.5, P: 1, n: 1, s: 1, a: 2, d: 1, c: 1, D: 2, o: 2, x: 2 };
function walls(F, spec, o) {
  const toks = spec.replace(/\s+/g, "").split("");
  const L = toks.reduce((n, t) => { if (!(t in TW)) throw new Error("wall token " + t); return n + TW[t]; }, 0);
  if (o.len !== undefined && Math.abs(L - o.len) > 1e-6) throw new Error(`wall "${spec}" is ${L} m long, not ${o.len}`);
  const { y, h, t } = o, type = o.type || "plaster", R = F.at(o.x, 0, o.z, o.yaw);
  const lintel = o.lintel ?? 2.4, plasterish = ["plaster", "ochre", "grey", "mud"].includes(type);
  let u = -L / 2, run = null, top = null;
  const flush = () => { if (run) R.add("wall_" + type, { w: run.w, h, t }, [run.u + run.w / 2, y, 0], [0, 0, 0], "Стены"); run = null; };
  const flushTop = () => { if (top && h - lintel >= 0.3) R.add("wall_" + type, { w: top.w, h: h - lintel, t }, [top.u + top.w / 2, y + lintel, 0], [0, 0, 0], "Стены"); top = null; };
  for (const tk of toks) {
    const w = TW[tk], c = u + w / 2;
    if (tk === "w" || tk === "h") { flushTop(); if (run && run.w + w <= 6) run.w += w; else { flush(); run = { u, w }; } u += w; continue; }
    if (tk === "P") { flush(); if (top && top.w + w <= 6) top.w += w; else { flushTop(); top = { u, w }; } u += w; continue; }
    flush(); flushTop();
    if (tk === "n" || tk === "s") {
      const small = tk === "s", ww = small ? 0.45 : o.ww || 0.6, wh = small ? 0.45 : o.wh || 0.8, sill = small ? o.highSill || 1.25 : o.sill || 1.0;
      R.add("wall_window", { w, h, t, type, ww, wh, sill }, [c, y, 0], [0, 0, 0], "Стены");
      R.add("window_lattice", { w: ww, h: wh, pattern: o.win || "vertical", mat: "wood_dark" }, [c, y + sill, 0], [0, 0, 0], "Окна и двери");
    } else if (tk === "a") {
      R.add("wall_window", { w, h, t, type, ww: 1.6, wh: 1.0, sill: 0.9 }, [c, y, 0], [0, 0, 0], "Стены");
      if (o.flap !== false) R.add("window_awning", { w: 1.6, h: 1.0, open: 55, mat: "wood_dark" }, [c, y + 0.9, t / 2 + 0.01], [0, 0, 0], "Окна и двери");
      R.add("beam", { L: 1.9, h: 0.06, d: 0.42, mat: "wood_raw" }, [c, y + 0.84, t / 2 + 0.08], [0, 0, 0], "Каркас");
    } else if (tk === "d" || tk === "c") {
      R.add("wall_door", { w, h, t, type, dw: 0.8 }, [c, y, 0], [0, 0, 0], "Стены");
      if (tk === "c") R.add("door_curtain", { w: 0.8, h: 1.9, color: o.curtain || "cloth_blue" }, [c, y + 0.14, t / 2 + 0.04], [0, 0, 0], "Окна и двери");
      else R.add("door_leaf", { w: 0.8, h: 1.9, style: o.door || "plank" }, [c - 0.4, y + 0.14, -t / 2 + 0.03], [0, 72, 0], "Окна и двери");
    } else if (tk === "D") {
      const dh = o.dh || 2.2; // the leaves stand on the 0.14 m sill and fill the opening above it
      R.add("wall_door", { w, h, t, type, dw: 1.4, dh }, [c, y, 0], [0, 0, 0], "Стены");
      R.add("door_leaf", { w: 0.7, h: dh - 0.14, style: o.door2 || "panel" }, [c - 0.7, y + 0.14, -t / 2 + 0.03], [0, 80, 0], "Окна и двери");
      R.add("door_leaf", { w: 0.7, h: dh - 0.14, style: o.door2 || "panel" }, [c + 0.7, y + 0.14, -t / 2 + 0.03], [0, 100, 0], "Окна и двери");
    } else if (tk === "o") {
      R.add("wall_window_round", { w, h, t, type: plasterish && type !== "grey" ? type : "plaster", dia: o.dia || 1.2, cy: o.cy || 1.5 }, [c, y, 0], [0, 0, 0], "Стены");
    } else if (tk === "x") {
      R.add("wall_window_shaped", { w, h, t, shape: o.shape || "octagon", size: 1.0, cy: o.cy || 1.5, mat: type === "grey" ? "plaster_grey" : type === "ochre" ? "plaster_ochre" : "plaster", fill: "lattice" }, [c, y, 0], [0, 0, 0], "Стены");
    }
    u += w;
  }
  flush(); flushTop();
  return L;
}

// A beam along X, in pieces of at most 12 m (the part's longest).
function beamX(F, L, x, y, z, o = {}) {
  const n = Math.max(1, Math.ceil(L / 12 - 1e-6)), l = L / n;
  for (let i = 0; i < n; i++) F.add("beam", { L: l, h: o.h || 0.22, d: o.d || 0.2, mat: o.mat || "wood_dark" }, [x - L / 2 + (i + 0.5) * l, y, z], [0, 0, 0], o.g || "Каркас");
}

// Four walls of a W×D box on posts, with beams round the top; returns the top of the beams. Sides: front +Z, back −Z,
// left −X (its tokens run back to front), right +X (front to back). <side>Type overrides the wall type for a side.
function hall(F, o) {
  o = { t: 0.18, type: "plaster", ...o };
  const { W, D, y, h, t } = o, cx = o.cx || 0, cz = o.cz || 0;
  const side = (name, len, x, z, yaw) => o[name] && walls(F, o[name], { ...o, type: o[name + "Type"] || o.type, len, x, z, yaw });
  side("front", W, cx, cz + D / 2, 0); side("back", W, cx, cz - D / 2, 180);
  side("left", D, cx - W / 2, cz, -90); side("right", D, cx + W / 2, cz, 90);
  const pm = o.posts === undefined ? "wood_dark" : o.posts;
  if (pm) for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h, a: t + 0.06, mat: pm }, [cx + sx * W / 2, y, cz + sz * D / 2], [0, 0, 0], "Каркас");
  for (const x of o.frontPosts || []) F.add("post_square", { h, a: 0.2, mat: pm || "wood_dark" }, [cx + x, y, cz + D / 2], [0, 0, 0], "Каркас");
  const bm = o.beamMat || "wood_dark";
  if (o.beams !== false) {
    for (const sz of [-1, 1]) beamX(F, W + t + 0.06, cx, y + h, cz + sz * D / 2, { d: t + 0.04, mat: bm });
    for (const sx of [-1, 1]) F.add("beam", { L: (o.sideBeamD || D) - t - 0.06, h: 0.22, d: t + 0.04, mat: bm }, [cx + sx * W / 2, y + h, cz + (o.sideBeamCz || 0)], [0, 90, 0], "Каркас");
  }
  return y + h + 0.22;
}

// ------------------------------------------------------------------ roofs
// How far below its ridge line a tiled slope (run, pitch, kick) is at t from it.
function slopeDrop(run, pitch, kick, t) {
  const z1 = Math.max(0.01, run - kick), tp = Math.tan(pitch * D2R);
  return t <= z1 ? t * tp : z1 * tp + (t - z1) * Math.tan(pitch * 0.55 * D2R);
}
// A gable roof over W×D (ridge along X): the slopes bend into their kick right above the walls (kick = over), so they
// meet the wall top there as the artist's gableRoof does. Options: mat, ridge (tile|low|ornate), ends (a design or
// false), endSize, gable (wall type of the triangles or false), fish, side (overhang at the gables), mod (module width).
function gable(F, o) {
  const { W, D, top } = o, cx = o.cx || 0, cz = o.cz || 0, pitch = o.pitch || 28, over = o.over ?? 0.6, side = o.side ?? 0.5, mat = o.mat || "tile";
  const tp = Math.tan(pitch * D2R), run = D / 2 + over, ridgeY = top + 0.05 + (D / 2) * tp;
  const span = W + 2 * side, n = Math.max(1, Math.ceil(span / (o.mod || 2) - 1e-6)), mw = span / n;
  for (let i = 0; i < n; i++) {
    const x = cx - span / 2 + (i + 0.5) * mw;
    for (const ry of [0, 180]) F.add("roof_tile_slope", { w: mw, run, pitch, kick: over, mat }, [x, ridgeY, cz], [0, ry, 0], "Крыша");
    F.add("roof_ridge", { w: mw, style: o.ridge || "tile", mat }, [x, ridgeY, cz], [0, 0, 0], "Крыша");
  }
  for (const sx of [-1, 1]) {
    for (const ry of [0, 180]) F.add("roof_verge", { run, pitch, kick: over }, [cx + sx * span / 2, ridgeY, cz], [0, ry, 0], "Крыша");
    if (o.ends !== false) F.add("roof_ridge_end", { size: o.endSize || 0.5, design: o.ends || "swallow", mat }, [cx + sx * span / 2, ridgeY + (o.ridge === "low" ? 0 : 0.08), cz], [0, sx > 0 ? 0 : 180, 0], "Крыша");
    if (o.gable !== false) F.add("gable", { w: D, h: (D / 2) * tp, t: o.t || 0.18, type: o.gable || "plaster", fish: o.fish || "no" }, [cx + sx * W / 2, top, cz], [0, sx * 90, 0], "Стены");
  }
  return ridgeY;
}
// A single-pitch eave of length L against a wall: its upper edge along X at (y, z), falling toward +Z by run, a low
// ridge as flashing along the wall, verges at the ends. With posts: a beam under the eave and posts on it down to floor.
function pent(F, o) {
  const { L, y, z, run } = o, cx = o.cx || 0, pitch = o.pitch || 22, kick = o.kick ?? 0.35, mat = o.mat || "tile";
  const n = Math.max(1, Math.ceil(L / 2 - 1e-6)), mw = L / n;
  for (let i = 0; i < n; i++) {
    const x = cx - L / 2 + (i + 0.5) * mw;
    F.add("roof_tile_slope", { w: mw, run, pitch, kick, mat }, [x, y, z], [0, 0, 0], "Крыша");
    if (o.cap !== false) F.add("roof_ridge", { w: mw, style: "low", mat }, [x, y, z - 0.06], [0, 0, 0], "Крыша");
  }
  if (o.verges !== false) for (const sx of [-1, 1]) F.add("roof_verge", { run, pitch, kick }, [cx + sx * L / 2, y, z], [0, 0, 0], "Крыша");
  if (o.posts) {
    const tp = o.postAt ?? run - kick - 0.2, by = y - slopeDrop(run, pitch, kick, tp) - 0.07 - 0.18;
    beamX(F, L - 0.2, cx, by, z + tp, { h: 0.18, d: 0.16, mat: o.postMat || "wood_dark" });
    for (const x of o.posts) {
      if (o.footing !== false) F.add("stone_footing", { r: 0.18, design: "square", mat: "stone" }, [cx + x, o.floor, z + tp], [0, 0, 0], "Фундамент");
      const f0 = o.floor + (o.footing !== false ? 0.22 : 0);
      F.add("post_octagon", { h: by - f0, r: 0.1, mat: o.postMat || "wood_dark" }, [cx + x, f0, z + tp], [0, 0, 0], "Каркас");
    }
    return { beamY: by, postZ: z + tp };
  }
  return null;
}

// ------------------------------------------------------------------ floors, steps
// A plinth of blocks up to 4×4 m from y0 (a little below the ground, so a slope shows no gap) to y0 + h.
function plinth(F, o) {
  const { W, D, h } = o, cx = o.cx || 0, cz = o.cz || 0, y0 = o.y0 ?? -0.2, style = o.style || "ashlar";
  const cells = (L) => { const n = Math.max(1, Math.ceil(L / 4 - 1e-6)), s = L / n; return Array.from({ length: n }, (_, i) => [(i + 0.5) * s, s]); };
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D)) F.add("found_block", { w: sw, d: sd, h: h - y0, style }, [cx - W / 2 + ux, y0, cz - D / 2 + uz], [0, 0, 0], "Фундамент");
}
// Boards (the planks show) or a board ceiling (one board with battens under it) over W×D on joists whose top is at y;
// either way the floor's top is at y + 0.05 (ceiling_panel reaches 0.01 above its origin).
function planks(F, o) {
  const { W, D, y } = o, cx = o.cx || 0, cz = o.cz || 0;
  const cells = (L) => { const n = Math.max(1, Math.ceil(L / 4 - 1e-6)), s = L / n; return Array.from({ length: n }, (_, i) => [(i + 0.5) * s, s]); };
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D)) {
    const p = [cx - W / 2 + ux, y, cz - D / 2 + uz];
    if (o.ceiling) F.add("ceiling_panel", { w: Math.max(0.5, sw), d: Math.max(0.5, sd), mat: o.mat || "wood_raw" }, [p[0], y + 0.04, p[2]], [0, 0, 0], "Пол");
    else F.add("floor_plank", { w: sw, d: sd, t: 0.05, mat: o.mat || "wood_raw" }, p, [0, 0, 0], "Пол");
  }
}
// A railing from (x0, z0) to (x1, z1) on a floor at y, in pieces of up to 2 m (a railing piece costs the same at any
// width, its pattern stretched), with no newel at the end.
function rails(F, x0, z0, x1, z1, y, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), n = Math.max(1, Math.ceil(L / 2 - 1e-6)), w = L / n, yaw = Math.atan2(-dz, dx) / D2R;
  for (let i = 0; i < n; i++) F.add("railing", { w, h: 0.9, mat: o.mat || "wood_dark", style: o.style || "wan" }, [x0 + dx * (i + 0.5) / n, y, z0 + dz * (i + 0.5) / n], [0, yaw, 0], "Ограждения");
}
// A plain strip of wall along X (no stone skirting, which wall_plaster puts at its foot): the band of wall that closes
// the space between a wall's top beam and the roof slope above a balcony or veranda. Beams of a wall material, ≤0.8 m.
function band(F, L, h, x, y, z, mat = "plaster", t = 0.18) {
  const n = Math.max(1, Math.ceil(h / 0.8 - 1e-6));
  for (let i = 0; i < n; i++) beamX(F, L, x, y + (i * h) / n, z, { h: h / n, d: t, mat, g: "Стены" });
}
// Stone steps up to a platform whose front edge is at z (they climb toward −Z and end at that edge).
function steps(houses, F, o) {
  const tread = o.tread || 0.3, n = Math.max(1, Math.round(o.H / 0.16));
  houses.stairs(F, { x: o.x || 0, z: o.z + n * tread, H: o.H, w: o.w || 1.2, kind: "stone", tread, cheeks: o.cheeks });
}

// ------------------------------------------------------------------ fittings
const lantern = (F, x, y, z, color = "lantern_white", size = 0.8) => F.add("lantern_hanging", { size, color }, [x, y, z], [0, 0, 0], "Декор");
// A shop counter: a dark body, a light top, a lacquered front panel (solid: it stands in the house).
function counter(F, x, y, z, L, yaw = 0, front = "wood_red") {
  const C = F.at(x, y, z, yaw);
  C.add("beam", { L, h: 0.85, d: 0.5, mat: "wood_dark" }, [0, 0, 0], [0, 0, 0], "Каркас");
  C.add("beam", { L: L + 0.1, h: 0.05, d: 0.62, mat: "wood_raw" }, [0, 0.85, 0], [0, 0, 0], "Каркас");
  C.add("plank", { L: L - 0.12, w: 0.5, t: 0.02, mat: front }, [0, 0.45, 0.25], [90, 0, 0], "Каркас");
}
// Shelves against a wall: a back board and n shelves (facing +Z of their frame).
function shelves(F, x, y, z, L, yaw = 0, n = 3, H = 1.8) {
  const S = F.at(x, y, z, yaw);
  S.add("beam", { L, h: H, d: 0.04, mat: "wood_dark" }, [0, 0, -0.16], [0, 0, 0], "Мебель и утварь");
  for (let i = 0; i < n; i++) S.add("beam", { L, h: 0.04, d: 0.34, mat: "wood_raw" }, [0, 0.35 + i * (H - 0.4) / Math.max(1, n - 1), 0], [0, 0, 0], "Мебель и утварь");
  return S;
}
// A medicine chest of drawers: a dark body and rows of light drawer fronts (cheaper than the kit's, whose drawers have
// knobs and labels each).
function drawers(F, x, y, z, yaw = 0, W = 1.4, H = 1.8, cols = 4, rows = 5) {
  const S = F.at(x, y, z, yaw), cw = W / cols, rh = (H - 0.15) / rows;
  S.add("beam", { L: W, h: H, d: 0.45, mat: "wood_dark" }, [0, 0, 0], [0, 0, 0], "Мебель и утварь");
  for (let i = 0; i < cols; i++) for (let j = 0; j < rows; j++) S.add("plank", { L: cw - 0.05, w: rh - 0.05, t: 0.02, mat: (i + j) % 3 ? "wood_raw" : "wood_pale" }, [-W / 2 + (i + 0.5) * cw, 0.1 + (j + 0.5) * rh, 0.225], [90, 0, 0], "Мебель и утварь");
}
// Bunches of herbs hung head down from a rod at height y along X from x0 to x1.
function herbs(F, x0, x1, y, z, n, seed) {
  const mats = ["grass_dry", "leaf_light", "grass_dry", "grass"];
  F.add("beam", { L: Math.abs(x1 - x0) + 0.2, h: 0.04, d: 0.04, mat: "wood_raw" }, [(x0 + x1) / 2, y, z], [0, 0, 0], "Декор");
  for (let i = 0; i < n; i++) F.add("grass_tuft", { h: 0.42, n: 9, mat: mats[(i + seed) % mats.length] }, [x0 + (x1 - x0) * (i + 0.5) / n, y + 0.02, z], [180, (i * 53 + seed * 31) % 360, 0], "Декор");
}
// A drying rack: two posts, three rails, bunches hung on them.
function rack(F, x, z, yaw, L = 1.8, seed = 1) {
  const R = F.at(x, 0, z, yaw);
  for (const s of [-1, 1]) R.add("post_square", { h: 1.9, a: 0.08, mat: "wood_raw" }, [s * L / 2, 0, 0], [0, 0, 0], "Декор");
  for (const [i, y] of [1.85, 1.35].entries()) herbs(R, -L / 2 + 0.1, L / 2 - 0.1, y, 0, 5, seed + i);
}

// ------------------------------------------------------------------ the houses
const make = (fn, yard = false) => (houses, kit, THREE) => {
  const items = [];
  fn(new houses.Frame(items), houses, kit);
  return centre(items, kit, THREE, yard);
};

// 1. Лавка — a tall shop: the open front with a counter under a tiled skirt roof on posts, a loft above with two windows
//    and the shop's board, a white flag on a bamboo pole. 5 × 6 m, the loft walls 1.75 m.
function shop(F, houses) {
  plinth(F, { W: 5.4, D: 8.1, cz: 0.85, h: 0.3 });
  const y0 = 0.3;
  const t1 = hall(F, { W: 5, D: 6, y: y0, h: 2.6, front: "hpppph", back: "wwnww", left: "wwnwww", right: "wwwwdw", frontPosts: [-0.7, 0.7] });
  const t2 = hall(F, { W: 5, D: 6, y: t1, h: 1.75, front: "wnwnw", back: "wwwww", left: "wwwwww", right: "wwwwww", sill: 0.55, wh: 0.75, backType: "timber", leftType: "timber", rightType: "timber" });
  gable(F, { W: 5, D: 6, top: t2, pitch: 27, over: 0.6, side: 0.5, endSize: 0.45, gable: "timber", fish: "yes" });
  const p = pent(F, { L: 5.4, y: t1 + 0.18, z: 3.12, run: 1.85, pitch: 20, kick: 0.35, posts: [-2.5, 2.5], floor: y0 });
  steps(houses, F, { x: 1.35, z: 4.9, H: y0, w: 1.2, cheeks: false });
  counter(F, -0.75, y0, 2.55, 2.4);
  shelves(F, 0, y0, -2.7, 3.6, 0, 3);
  F.add("vessel", { kind: "wine_jar", mat: "ceramic_brown" }, [-1.4, y0, -2.65], [0, 20, 0], "Мебель и утварь");
  F.add("vessel", { kind: "gourd", mat: "glaze_celadon" }, [0.2, y0 + 0.93, -2.7], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "meiping", mat: "porcelain_blue" }, [1.1, y0 + 1.63, -2.7], [0, 0, 0], "Мебель и утварь");
  F.add("sack", { kind: "open" }, [1.6, y0, 1.9], [0, 30, 0], "Мебель и утварь");
  F.add("crate", { a: 0.5, kind: "plain" }, [-2.0, y0, -1.2], [0, 10, 0], "Мебель и утварь");
  F.add("item", { kind: "scale", size: 1 }, [-1.3, y0 + 0.9, 2.55], [0, 0, 0], "Мебель и утварь");
  for (let i = 0; i < 5; i++) F.add("plank", { L: 2.5, w: 0.26, t: 0.03, mat: "wood_old" }, [-2.86 - i * 0.035, y0 + 1.22, 0.9 + i * 0.27], [0, 0, 78], "Окна и двери");
  F.add("sign_board", { w: 1.15, glyphs: 3 }, [0, t1 + 0.95, 3.09], [0, 0, 0], "Декор"); // between the loft windows
  for (const x of [-1.6, 1.6]) lantern(F, x, p.beamY, p.postZ + 0.15);
  // the flag's pole just outside the skirt roof's end (it spans x ±2.7), its cloth in front of the loft
  F.add("shop_flag", { color: "cloth_white", h: 6.2 }, [-2.95, 0, 4.85], [0, 0, 0], "Декор");
}

// 2. Лавочка А — a small whitewashed shop: a wide counter window under a blue cloth awning, a plank door, a board over it.
function shopSmallA(F, houses) {
  plinth(F, { W: 4.4, D: 4.9, h: 0.25 });
  const y0 = 0.25;
  const top = hall(F, { W: 4, D: 4.5, y: y0, h: 2.7, front: "hahd", back: "wwww", left: "wwnwh", right: "wwwwh", flap: false });
  gable(F, { W: 4, D: 4.5, top, pitch: 33, over: 0.55, side: 0.45, endSize: 0.4, fish: "yes" });
  F.add("awning", { w: 2.5, d: 1.0, h: 2.8, color: "cloth_blue" }, [-0.5, 0, 2.36], [0, 0, 0], "Крыша");
  steps(houses, F, { x: 1.5, z: 2.45, H: y0, w: 1.0, cheeks: false });
  counter(F, -0.5, y0, 1.6, 1.9);
  shelves(F, -0.4, y0, -2.0, 2.6, 0, 3);
  F.add("vessel", { kind: "wine_jar" }, [-1.6, y0 + 0.9, -2.0], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "gourd", mat: "ceramic" }, [0.6, y0 + 0.9, 1.6], [0, 0, 0], "Мебель и утварь");
  F.add("sack", { kind: "full" }, [-1.7, 0, 2.75], [0, 40, 0], "Мебель и утварь");
  F.add("crate", { a: 0.45, kind: "slats" }, [-1.15, 0, 2.95], [0, -15, 0], "Мебель и утварь");
  F.add("sign_board", { w: 1.0, glyphs: 2 }, [1.5, y0 + 2.42, 2.34], [0, 0, 0], "Декор");
  lantern(F, 2.2, top - 0.1, 2.55, "lantern_white", 0.7);
}

// 3. Лавочка Б — a plank shop with its gable to the street: an open front under a transom and a small tiled skirt roof
//    on brackets, the board on the gable. Walls and roof are laid in a frame turned 90° (its left side is the front).
function shopSmallB(F, houses) {
  plinth(F, { W: 4.4, D: 5.4, h: 0.2 });
  const y0 = 0.2, G = F.at(0, 0, 0, 90);
  const top = hall(G, { W: 5, D: 4, y: y0, h: 2.9, t: 0.14, type: "plank", left: "hPPPh", front: "wwnww", back: "wwwww", right: "wwdw", lintel: 2.35 });
  gable(G, { W: 5, D: 4, top, pitch: 33, over: 0.6, side: 0.4, endSize: 0.45, mat: "tile_blue", gable: "plank", t: 0.14, fish: "yes" });
  for (const x of [-1.5, 1.5]) F.add("post_square", { h: 2.35, a: 0.16, mat: "wood_dark" }, [x, y0, 2.5], [0, 0, 0], "Каркас");
  // the skirt roof meets the plank wall under the gable's overhang: no flashing ridge needed there
  pent(F, { L: 4.3, y: y0 + 2.62, z: 2.6, run: 1.1, pitch: 22, kick: 0.3, mat: "tile_blue", cap: false });
  for (const x of [-1.6, 1.6]) F.add("bracket", { L: 0.8, mat: "wood_dark" }, [x, y0 + 1.95, 2.57], [0, 0, 0], "Каркас");
  steps(houses, F, { x: 1.0, z: 2.7, H: y0, w: 1.0, cheeks: false });
  counter(F, -0.5, y0, 2.0, 1.9);
  shelves(F, 0, y0, -2.2, 3.2, 0, 3);
  F.add("vessel", { kind: "wine_jar", mat: "ceramic" }, [-1.0, y0 + 0.9, 2.0], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "big_jar" }, [1.5, y0, -1.5], [0, 0, 0], "Мебель и утварь");
  F.add("crate", { a: 0.5, kind: "plain" }, [-1.6, y0, -1.3], [0, 20, 0], "Мебель и утварь");
  F.add("sack", { kind: "full", mat: "cloth_white" }, [-1.9, 0, 2.95], [0, -20, 0], "Мебель и утварь");
  F.add("sign_board", { w: 1.3, glyphs: 2 }, [0, top + 0.4, 2.57], [0, 0, 0], "Декор");
  for (const x of [-1.45, 1.45]) lantern(F, x, y0 + 2.3, 3.35, "lantern_white", 0.7);
}

// 4. Лавочка В — a half-timbered booth: a counter opening under a transom, a curtained door, a deep tiled porch on two
//    posts with goods set out on a bench.
function shopSmallC(F, houses) {
  plinth(F, { W: 4.4, D: 3.9, h: 0.2 });
  const y0 = 0.2;
  const top = hall(F, { W: 4, D: 3.5, y: y0, h: 2.7, type: "timber", front: "hPPhc", back: "wwww", left: "wwwh", right: "wwwh", lintel: 2.3, curtain: "cloth_blue", frontType: "plaster" });
  gable(F, { W: 4, D: 3.5, top, pitch: 30, over: 0.55, side: 0.4, endSize: 0.4, gable: "timber" });
  const p = pent(F, { L: 4.4, y: y0 + 2.62, z: 1.87, run: 1.6, pitch: 18, kick: 0.3, posts: [-2.0, 2.0], floor: 0 });
  counter(F, -0.5, y0, 1.4, 1.9);
  shelves(F, -0.3, y0, -1.45, 2.8, 0, 3);
  F.add("f_bench", { w: 1.6, mat: "wood_raw" }, [-0.7, 0, 2.55], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "wine_jar" }, [-1.2, 0.47, 2.55], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "gourd", mat: "glaze_celadon" }, [-0.6, 0.47, 2.55], [0, 0, 0], "Мебель и утварь");
  F.add("crate", { a: 0.4, kind: "plain" }, [-0.05, 0.47, 2.55], [0, 10, 0], "Мебель и утварь");
  F.add("vessel", { kind: "big_jar" }, [1.7, 0, 2.45], [0, 0, 0], "Мебель и утварь");
  F.add("sack", { kind: "full" }, [1.35, y0, -1.1], [0, 0, 0], "Мебель и утварь");
  F.add("sign_board", { w: 1.2, glyphs: 2 }, [-0.5, y0 + 2.62 - 0.4, 1.85], [0, 0, 0], "Декор"); // on the transom
  for (const x of [-1.2, 1.2]) lantern(F, x, p.beamY, p.postZ, "lantern_white", 0.7);
}

// 5. Лавка травника — a plastered hall on a stone podium: moon windows either side of the door, a veranda on four posts
//    whose beam is hung with drying herbs, two drying racks by the steps; inside a chest of medicine drawers and a counter.
function herbalist(F, houses) {
  const y0 = 0.45, veranda = 1.8;
  // the podium from 0.15 m behind the back wall (z −2.65) to 0.15 m before the veranda posts (z 4.45)
  plinth(F, { W: 6.6, D: 7.1, cz: 0.9, h: y0 });
  const top = hall(F, { W: 6, D: 5, y: y0, h: 2.9, front: "oDo", back: "wwnwww", left: "wnwww", right: "wwwnw", door2: "panel", sideBeamD: 5 + veranda, sideBeamCz: veranda / 2 });
  const zf = 2.5 + veranda, pitch = 26;
  for (const x of [-3, -1, 1, 3]) {
    F.add("stone_footing", { r: 0.16, design: "drum", mat: "stone_light" }, [x, y0, zf], [0, 0, 0], "Фундамент");
    F.add("post", { h: 2.9 - 0.22, r: 0.1, mat: "wood_dark" }, [x, y0 + 0.22, zf], [0, 0, 0], "Каркас");
  }
  beamX(F, 6.3, 0, y0 + 2.9, zf, { d: 0.2 });
  F.add("wall_plaster", { w: 6, h: veranda * Math.tan(pitch * D2R), t: 0.18 }, [0, top, 2.5], [0, 0, 0], "Стены");
  gable(F, { W: 6, D: 5 + veranda, cz: veranda / 2, top, pitch, over: 0.7, side: 0.5, endSize: 0.5, fish: "yes" });
  steps(houses, F, { x: 0, z: zf + 0.15, H: y0, w: 2.0 });
  herbs(F, -2.85, -1.15, y0 + 2.88, zf, 5, 0); herbs(F, 1.15, 2.85, y0 + 2.88, zf, 5, 2);
  herbs(F, -0.85, 0.85, y0 + 2.88, zf, 4, 1);
  rack(F, -2.4, zf + 1.05, 0, 1.6, 0); rack(F, 2.4, zf + 1.05, 0, 1.6, 3);
  drawers(F, 0, y0, -2.25, 0, 1.6, 1.9);
  F.add("item", { kind: "medicine", size: 1.5 }, [-0.4, y0 + 1.9, -2.2], [0, 0, 0], "Мебель и утварь");
  counter(F, 0.9, y0, 0.2, 2.0, 0, "wood_dark");
  F.add("item", { kind: "scale", size: 1 }, [0.4, y0 + 0.9, 0.2], [0, 0, 0], "Мебель и утварь");
  F.add("item", { kind: "medicine", size: 1.2 }, [1.3, y0 + 0.9, 0.25], [0, 30, 0], "Мебель и утварь");
  F.add("sack", { kind: "open" }, [-2.3, y0, -1.6], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "big_jar", mat: "glaze_celadon" }, [2.4, y0, -1.9], [0, 0, 0], "Мебель и утварь");
  F.add("sign_board", { w: 1.4, glyphs: 3 }, [0, y0 + 2.62, 2.6], [0, 0, 0], "Декор");
  for (const x of [-2, 2]) lantern(F, x, y0 + 2.88, zf - 0.3);
}

// 6. Кузница — an open forge shed on heavy posts against a stone back wall: the hearth with its glowing coals, a hood
//    and a chimney through the roof, the anvil, the quench trough, the bellows, a weapon rack; a plank store room at
//    the right end.
function smithy(F, houses) {
  const y0 = 0.15, W = 8, D = 6.5, hz = D / 2, H = 3.0, ty = y0 + H, zb = -hz + 0.25; // zb: the back wall's inner face
  plinth(F, { W: W + 0.4, D: D + 0.6, h: y0, style: "rammed" });
  // the stone back wall: two blocks of 4 m (the part's widest), from the floor up to the beams
  for (const x of [-2, 2]) F.add("found_block", { w: 4, d: 0.5, h: H, style: "ashlar" }, [x, y0, -hz], [0, 0, 0], "Стены");
  for (const x of [-4, -1.5, 1.5, 4]) {
    F.add("stone_footing", { r: 0.22, design: "square", mat: "stone" }, [x, y0, hz], [0, 0, 0], "Фундамент");
    F.add("post_octagon", { h: H - 0.22, r: 0.15, mat: "wood_dark" }, [x, y0 + 0.22, hz], [0, 0, 0], "Каркас");
  }
  for (const z of [-hz, hz]) beamX(F, W + 0.4, 0, ty, z, { h: 0.26, d: 0.26 });
  for (const x of [-4, 4]) F.add("beam", { L: D - 0.3, h: 0.26, d: 0.26, mat: "wood_dark" }, [x, ty, 0], [0, 90, 0], "Каркас");
  F.add("beam", { L: D - 0.3, h: 0.22, d: 0.2, mat: "wood_dark" }, [0, ty, 0], [0, 90, 0], "Каркас");
  // the store room: x 1.5..4, plank walls, a door in front
  walls(F, "wdh", { x: 2.75, z: hz, yaw: 0, y: y0, h: H, t: 0.14, type: "plank", len: 2.5 });
  walls(F, "wwwwwwh", { x: 1.5, z: 0, yaw: -90, y: y0, h: H, t: 0.14, type: "plank", len: D });
  walls(F, "wwwwwwh", { x: 4.0, z: 0, yaw: 90, y: y0, h: H, t: 0.14, type: "plank", len: D });
  const ridgeY = gable(F, { W, D, top: ty + 0.26, pitch: 27, over: 0.8, side: 0.5, endSize: 0.53, gable: "timber", fish: "yes" });
  // the forge against the back wall: a hearth with the fire at its front, a flue behind the fire, a hood over it on
  // the flue, and the chimney from the hood up through the roof (≈0.8 m above the ridge)
  const fx = -2.2;
  F.add("found_block", { w: 1.8, d: 1.3, h: 0.8, style: "ashlar" }, [fx, y0, zb + 0.65], [0, 0, 0], "Каркас");
  F.add("brazier", { kind: "campfire" }, [fx, y0 + 0.8, zb + 0.85], [0, 0, 0], "Декор");
  F.add("found_block", { w: 1.0, d: 0.45, h: 2.0, style: "ashlar" }, [fx, y0 + 0.8, zb + 0.225], [0, 0, 0], "Каркас");
  F.add("found_block", { w: 1.8, d: 1.1, h: 0.6, style: "ashlar" }, [fx, y0 + 2.2, zb + 0.55], [0, 0, 0], "Каркас");
  const stackTop = ridgeY + 0.8;
  F.add("found_block", { w: 0.8, d: 0.8, h: stackTop - (y0 + 2.8), style: "ashlar" }, [fx, y0 + 2.8, zb + 0.4], [0, 0, 0], "Крыша");
  F.add("stone_slab", { w: 1.0, d: 1.0, h: 0.12 }, [fx, stackTop, zb + 0.4], [0, 0, 0], "Крыша");
  // the work floor: two anvils with the quench trough between them and the wall, the rack of finished weapons on the
  // store room's wall, coal sacks and the leather chest by the hearth, iron bars on a crate, the chopping block
  F.add("f_chest", { w: 0.8, mat: "leather" }, [-3.5, y0, zb + 0.5], [0, 90, 0], "Мебель и утварь");
  F.add("f_anvil", {}, [-1.0, y0, -0.7], [0, -20, 0], "Мебель и утварь");
  F.add("f_anvil", {}, [0.2, y0, 0.6], [0, 35, 0], "Мебель и утварь");
  F.add("trough", { L: 1.4, mat: "stone" }, [-0.2, y0, zb + 0.35], [0, 0, 0], "Мебель и утварь");
  F.add("weapon_rack", { w: 1.6, load: "mixed" }, [1.15, y0, -0.3], [0, -90, 0], "Оружие");
  F.add("w_hammer", { mat: "iron", size: 0.8 }, [-0.85, y0 + 0.74, -0.65], [90, 0, 70], "Оружие");
  F.add("sack", { kind: "stack" }, [-3.4, y0, -1.0], [0, 90, 0], "Мебель и утварь");
  F.add("firewood", { kind: "chopping" }, [-3.2, y0, 1.9], [0, 30, 0], "Мебель и утварь");
  F.add("crate", { a: 0.5, kind: "plain", mat: "wood_old" }, [-1.6, y0, 1.1], [0, 15, 0], "Мебель и утварь");
  for (let i = 0; i < 4; i++) F.add("beam", { L: 0.6, h: 0.05, d: 0.07, mat: "iron" }, [-1.6, y0 + 0.53 + (i > 2 ? 0.05 : 0), 0.98 + (i % 3) * 0.1], [0, 15, 0], "Мебель и утварь");
  lantern(F, 2.75, ty - 0.02, hz + 0.3);
}

// 7. Амбар клана — a granary of boards raised on staddle stones against damp and rats, a deck in front with a railing,
//    a wooden stair to it, a black lacquered double door, vents high up, the clan's crescent on the ridge.
function granary(F, houses) {
  const fy = 1.39, W = 7, D = 5.5, deck = 1.0;
  for (const x of [-3.4, 0, 3.4]) for (const z of [-2.65, 0, 2.65, 2.65 + deck - 0.15]) {
    F.add("found_block", { w: 0.45, d: 0.45, h: 1.3, style: "ashlar" }, [x, -0.2, z], [0, 0, 0], "Фундамент");
    F.add("stone_slab", { w: 0.75, d: 0.75, h: 0.08 }, [x, 1.1, z], [0, 0, 0], "Фундамент");
  }
  for (const z of [-2.65, 0, 2.65, 2.65 + deck - 0.15]) F.add("floor_joist", { L: 7.3, h: 0.16, d: 0.14, mat: "wood_dark" }, [0, 1.18, z], [0, 0, 0], "Фундамент");
  planks(F, { W: 7.2, D: D, y: 1.34, ceiling: true });
  planks(F, { W: 7.2, D: deck, cz: D / 2 + deck / 2, y: 1.34 });
  // the door 2.0 m high, so the board fits on the 2.7 m wall above its lintel; the sides blind (air comes in at the vents)
  const top = hall(F, { W, D, y: fy, h: 2.7, t: 0.14, type: "plank", front: "wshDhsw", back: "wwwwwww", left: "wwwwwh", right: "wwwwwh", door2: "black", dh: 2.0, highSill: 1.8 });
  gable(F, { W, D, top, pitch: 30, over: 0.7, side: 0.6, endSize: 0.5, mat: "tile", gable: "plank", t: 0.14, fish: "yes" });
  F.add("roof_finial", { design: "moon", size: 0.5, mat: "silver" }, [0, top + 0.05 + (D / 2) * Math.tan(30 * D2R) + 0.28, 0], [0, 0, 0], "Крыша");
  const zr = D / 2 + deck;
  houses.railRun(F, -3.6, zr, -0.7, zr, fy, { mat: "wood_dark", style: "cross" });
  houses.railRun(F, 0.7, zr, 3.6, zr, fy, { mat: "wood_dark", style: "cross" });
  for (const sx of [-1, 1]) houses.railRun(F, sx * 3.6, D / 2, sx * 3.6, zr, fy, { mat: "wood_dark", style: "cross", end: false });
  houses.stairs(F, { x: 0, z: zr + 8 * 0.24, H: fy, w: 1.2, kind: "wood", tread: 0.24, railMat: "wood_dark" });
  F.add("sack", { kind: "stack" }, [-2.3, fy + 0.05, D / 2 + 0.5], [0, 0, 0], "Мебель и утварь");
  F.add("sack", { kind: "full" }, [2.1, fy + 0.05, D / 2 + 0.45], [0, 20, 0], "Мебель и утварь");
  F.add("sack", { kind: "open" }, [2.7, fy + 0.05, D / 2 + 0.55], [0, -30, 0], "Мебель и утварь");
  F.add("sign_board", { w: 1.4, glyphs: 2 }, [0, fy + 2.41, D / 2 + 0.07], [0, 0, 0], "Декор");
  lantern(F, -1.3, top - 0.05, D / 2 + 0.4);
}

// 8. Харчевня — a long noodle house open along its front under a tiled skirt roof on posts: tables and benches inside
//    and out, the kitchen at the back right with its stove, wok and steamers, a chimney, the white noodle flag.
function eatery(F, houses) {
  const y0 = 0.3;
  plinth(F, { W: 9.6, D: 8.0, cz: 0.85, h: y0 });
  const t1 = hall(F, { W: 9, D: 6, y: y0, h: 3.0, front: "hPPPPPPPPh", back: "wwnwwwnww", left: "wwnwww", right: "wwwwww", lintel: 2.5, frontPosts: [-2, 0, 2] });
  gable(F, { W: 9, D: 6, top: t1, pitch: 29, over: 0.6, side: 0.5, endSize: 0.5, fish: "yes" });
  const p = pent(F, { L: 9.6, y: y0 + 2.86, z: 3.12, run: 1.6, pitch: 20, kick: 0.3, posts: [-4.6, -1.55, 1.55, 4.6], floor: y0 });
  steps(houses, F, { x: 0, z: 4.85, H: y0, w: 2.4, cheeks: false });
  // kitchen
  F.add("found_block", { w: 2.0, d: 0.9, h: 0.8, style: "ashlar" }, [3.2, y0, -2.4], [0, 0, 0], "Каркас");
  F.add("wok_stove_set", {}, [3.2, y0 + 0.8, -2.4], [0, 0, 0], "Мебель и утварь");
  F.add("found_block", { w: 0.7, d: 0.6, h: 2.8, style: "ashlar" }, [4.05, y0, -2.55], [0, 0, 0], "Каркас");
  F.add("found_block", { w: 0.7, d: 0.6, h: 2.9, style: "ashlar" }, [4.05, y0 + 2.8, -2.55], [0, 0, 0], "Крыша");
  F.add("stone_slab", { w: 0.9, d: 0.8, h: 0.12 }, [4.05, y0 + 5.7, -2.55], [0, 0, 0], "Крыша");
  counter(F, 2.9, y0, -1.1, 2.6, 0, "wood_dark");
  F.add("vessel", { kind: "big_jar" }, [1.2, y0, -2.5], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "wine_jar" }, [1.9, y0, -2.55], [0, 0, 0], "Мебель и утварь");
  F.add("firewood", { kind: "bundle" }, [4.0, y0, -1.5], [0, 0, 0], "Мебель и утварь");
  // tables
  const table = (x, z, rot = 0) => {
    F.add("f_table_square", { a: 0.9, mat: "wood_dark" }, [x, y0, z], [0, rot, 0], "Мебель и утварь");
    for (const s of [-1, 1]) F.add("f_bench", { w: 1.2, mat: "wood_raw" }, [x + s * 0.7 * Math.cos(rot * D2R + Math.PI / 2), y0, z + s * 0.7 * Math.sin(rot * D2R + Math.PI / 2) * -1], [0, rot + 90, 0], "Мебель и утварь");
  };
  table(-3.0, -0.4); table(-0.8, -0.6); table(-3.0, 1.9); table(-0.6, 1.6);
  table(-2.6, 4.0, 90); table(2.6, 4.0, 90);
  F.add("food", { kind: "noodles" }, [-0.8, y0 + 0.82, -0.6], [0, 0, 0], "Мебель и утварь");
  F.add("food", { kind: "buns" }, [2.3, y0 + 0.9, -1.1], [0, 0, 0], "Мебель и утварь");
  // the board hangs on the face of the skirt roof's beam, its top just under the tiles (1.8 m clear above the floor)
  F.add("sign_board", { w: 2.0, glyphs: 3 }, [0, p.beamY - 0.08, p.postZ + 0.09], [0, 0, 0], "Декор");
  for (const x of [-3.1, 3.1]) lantern(F, x, p.beamY, p.postZ - 0.1);
  // the noodle flag's pole beside the skirt roof's left end (its verge board is at x −4.8)
  F.add("shop_flag", { color: "cloth_white", h: 6.6 }, [-5.05, 0, 4.85], [0, 0, 0], "Декор");
}

// 9. Чайная — a two-storey teahouse: moon windows either side of the door below, a deep balcony with a lattice railing
//    above it carried on posts, a tiled skirt roof under the balcony, tea tables on the balcony and on the porch.
//    The upper floor has no stair inside (its rooms are not entered).
function teahouse(F, houses) {
  const y0 = 0.5, W = 8, D = 7, bal = 1.0;
  plinth(F, { W: 8.6, D: D + bal + 0.6, cz: bal / 2 + 0.3 - 0.3, h: y0 });
  const t1 = hall(F, { W, D, cz: -0.5, y: y0, h: 3.0, front: "woDow", back: "wwwnnwww", left: "wwnwwnw", right: "wnwwnww", door2: "red" });
  const zb = -0.5 + D / 2 + bal; // the balcony's edge (posts) z = 4.0
  for (const x of [-4, -1.4, 1.4, 4]) {
    F.add("stone_footing", { r: 0.16, design: "drum", mat: "stone_light" }, [x, y0, zb], [0, 0, 0], "Фундамент");
    F.add("post", { h: t1 - y0 - 0.22, r: 0.1, mat: "wood_dark" }, [x, y0 + 0.22, zb], [0, 0, 0], "Каркас");
  }
  beamX(F, 8.3, 0, t1 - 0.22, zb, { d: 0.2 });
  for (const z of [-3.2, -1.2, 0.8, 2.8]) F.add("floor_joist", { L: 8.2, h: 0.16, d: 0.14, mat: "wood_dark" }, [0, t1, z], [0, 0, 0], "Пол");
  planks(F, { W: 8.2, D: D, cz: -0.5, y: t1 + 0.16, ceiling: true });
  planks(F, { W: 8.2, D: bal + 0.1, cz: zb - bal / 2 + 0.05, y: t1 + 0.16, mat: "wood_raw" });
  const y1 = t1 + 0.21;
  const t2 = hall(F, { W, D, cz: -0.5, y: y1, h: 2.6, t: 0.16, type: "plank", front: "wnwDwnw", back: "wwwwwwww", left: "wwwnwww", right: "wwwnwww", door2: "lattice", sideBeamD: D + bal, sideBeamCz: bal / 2 });
  for (const x of [-4, -1.4, 1.4, 4]) F.add("post", { h: 2.6, r: 0.09, mat: "wood_dark" }, [x, y1, zb], [0, 0, 0], "Каркас");
  beamX(F, 8.3, 0, y1 + 2.6, zb, { d: 0.18 });
  const pitch = 25;
  F.add("wall_plank", { w: W, h: Math.max(0.3, bal * Math.tan(pitch * D2R)), t: 0.16 }, [0, t2, -0.5 + D / 2], [0, 0, 0], "Стены");
  gable(F, { W, D: D + bal, cz: bal / 2 - 0.5 + 0.5 - 0.5 + 0.0, top: t2, pitch, over: 0.7, side: 0.5, endSize: 0.4, gable: "plank", t: 0.16, fish: "yes" });
  houses.railRun(F, -4, zb, 4, zb, y1, { mat: "wood_dark", style: "wan", end: false });
  for (const sx of [-1, 1]) houses.railRun(F, sx * 4, zb - bal, sx * 4, zb, y1, { mat: "wood_dark", style: "wan", end: false });
  pent(F, { L: 8.6, y: t1 - 0.1, z: zb + 0.08, run: 1.0, pitch: 22, kick: 0.25 });
  steps(houses, F, { x: 0, z: zb + 0.3, H: y0, w: 2.2 });
  // tea tables
  for (const x of [-2.6, 2.6]) {
    F.add("f_table_round", { r: 0.45, mat: "wood_dark" }, [x, y1, zb - 0.45], [0, 0, 0], "Мебель и утварь");
    for (const s of [-1, 1]) F.add("f_stool", { style: "square", mat: "wood_dark" }, [x + s * 0.7, y1, zb - 0.45], [0, 0, 0], "Мебель и утварь");
  }
  F.add("vessel", { kind: "teapot", mat: "porcelain_blue" }, [-2.6, y1 + 0.81, zb - 0.45], [0, 30, 0], "Мебель и утварь");
  for (const x of [-2.5, 2.5]) {
    F.add("f_table_square", { a: 0.8, mat: "wood_dark" }, [x, y0, zb - 0.9], [0, 0, 0], "Мебель и утварь");
    F.add("f_bench", { w: 1.2, mat: "wood_dark" }, [x, y0, zb - 0.25], [0, 0, 0], "Мебель и утварь");
  }
  F.add("vessel", { kind: "teapot", mat: "glaze_celadon" }, [2.5, y0 + 0.82, zb - 0.9], [0, -40, 0], "Мебель и утварь");
  F.add("sign_board", { w: 1.6, glyphs: 3 }, [0, y0 + 2.7, -0.5 + D / 2 + 0.1], [0, 0, 0], "Декор");
  for (const x of [-2.7, 2.7]) lantern(F, x, y1 + 2.58, zb + 0.05);
  for (const x of [-2.7, 2.7]) lantern(F, x, t1 - 0.25, zb - 0.1, "lantern_white", 0.7);
}

// 10. Таверна — two storeys: below two double doors between lattice windows, the upper floor jutting out on brackets
//     with a row of windows and the tavern's board, a deep tiled porch on posts with tables and benches, wine jars
//     stacked by the door, a tall wine flag; warm red lanterns.
function tavern(F, houses) {
  const y0 = 0.3, W = 8, D = 7.5, jut = 0.5;
  plinth(F, { W: 8.6, D: D + 2.6, cz: 1.3 - 0.3 + 0.3, h: y0 });
  const t1 = hall(F, { W, D, y: y0, h: 2.8, front: "wnDDnw", back: "wwwdwwww", left: "wwnwwnwh", right: "wwwwwwwh", door2: "red" });
  for (const x of [-3.5, -1.2, 1.2, 3.5]) F.add("floor_joist", { L: D + jut + 0.2, h: 0.16, d: 0.14, mat: "wood_dark" }, [x, t1, jut / 2], [0, 90, 0], "Пол");
  planks(F, { W: W + 0.2, D: D + jut, cz: jut / 2, y: t1 + 0.16, ceiling: true });
  for (const x of [-3, -1, 1, 3]) F.add("bracket", { L: 0.55, mat: "wood_dark" }, [x, t1 - 0.5, D / 2 + 0.1], [0, 0, 0], "Каркас");
  const y1 = t1 + 0.21;
  const t2 = hall(F, { W, D: D + jut, cz: jut / 2, y: y1, h: 2.3, t: 0.16, type: "plank", front: "wnwwwwnw", back: "wwwwwwww", left: "wwwnwwww", right: "wwwwnwww" });
  gable(F, { W, D: D + jut, cz: jut / 2, top: t2, pitch: 22, over: 0.6, side: 0.5, endSize: 0.5, gable: "plank", t: 0.16, fish: "yes" });
  const zp = D / 2 + jut + 0.06;
  const p = pent(F, { L: 8.6, y: t1 - 0.02, z: zp, run: 1.6, pitch: 20, kick: 0.3, posts: [-4.2, -1.4, 1.4, 4.2], floor: y0 });
  steps(houses, F, { x: 0, z: D / 2 + 2.3 - 0.0, H: y0, w: 2.4, cheeks: false });
  // jars by the doors, tables on the porch
  for (const [x, z, k] of [[-3.55, D / 2 + 0.45, "big_jar"], [-2.85, D / 2 + 0.4, "big_jar"], [-3.2, D / 2 + 1.05, "wine_jar"], [3.55, D / 2 + 0.45, "big_jar"], [2.9, D / 2 + 0.4, "wine_jar"], [3.3, D / 2 + 1.0, "wine_jar"]])
    F.add("vessel", { kind: k, mat: "ceramic_brown" }, [x, y0, z], [0, x * 40, 0], "Мебель и утварь");
  for (const x of [-1.9, 1.9]) {
    F.add("f_table_square", { a: 0.85, mat: "wood_dark" }, [x, y0, D / 2 + 1.25], [0, 0, 0], "Мебель и утварь");
    for (const s of [-1, 1]) F.add("f_bench", { w: 1.2, mat: "wood_raw" }, [x + s * 0.75, y0, D / 2 + 1.25], [0, 90, 0], "Мебель и утварь");
  }
  F.add("vessel", { kind: "ewer", mat: "ceramic_brown" }, [-1.9, y0 + 0.82, D / 2 + 1.25], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "bowl", mat: "ceramic" }, [1.75, y0 + 0.82, D / 2 + 1.2], [0, 0, 0], "Мебель и утварь");
  F.add("sign_board", { w: 2.0, glyphs: 3 }, [0, y1 + 1.35, zp - 0.04], [0, 0, 0], "Декор");
  for (const x of [-2.8, 0, 2.8]) lantern(F, x, p.beamY, p.postZ - 0.12, "lantern", 0.85);
  F.add("shop_flag", { color: "cloth_white", h: 7.6 }, [4.6, 0, D / 2 + 1.95], [0, 180, 0], "Декор");
}

// 11. Постоялый двор — a long two-storey inn: the common hall below with its double door, four guest rooms above opening
//     onto a gallery on posts, an outside stair up to the gallery at the right end; a hitching rail with a trough and a
//     cart at the left, the inn's board and flag.
function inn(F, houses) {
  const y0 = 0.4, W = 14, D = 8, gal = 1.3;
  plinth(F, { W: 14.6, D: D + gal + 0.5, cz: gal / 2 + 0.25 - 0.25 + 0.0, h: y0 });
  const t1 = hall(F, { W, D, y: y0, h: 3.0, front: "wnwnwwDwwnwnw", back: "wwwwwwdwwwwwww", left: "wwnwwnww", right: "wwwwwwdw", door2: "panel" });
  const zg = D / 2 + gal;
  for (const x of [-7, -3.5, 0, 3.5, 7]) {
    F.add("stone_footing", { r: 0.16, design: "square", mat: "stone" }, [x, y0, zg], [0, 0, 0], "Фундамент");
    F.add("post_octagon", { h: t1 - y0 - 0.22 - 0.22, r: 0.11, mat: "wood_dark" }, [x, y0 + 0.22, zg], [0, 0, 0], "Каркас");
  }
  beamX(F, W + 0.3, 0, t1 - 0.22, zg, { d: 0.2 });
  planks(F, { W: W + 0.2, D, y: t1 + 0.16, ceiling: true });
  planks(F, { W: W + 1.0, D: gal, cx: 0.4, cz: D / 2 + gal / 2, y: t1 + 0.16, mat: "wood_raw" });
  for (const z of [-2.5, 0, 2.5]) beamX(F, W + 0.2, 0, t1, z, { h: 0.16, d: 0.14, mat: "wood_dark", g: "Пол" });
  beamX(F, W + 1.0, 0.4, t1, zg - 0.1, { h: 0.16, d: 0.14, mat: "wood_dark", g: "Пол" });
  const y1 = t1 + 0.21;
  const t2 = hall(F, { W, D, y: y1, h: 2.6, front: "hdnwhdnwhdnwhdnw", back: "wwwwwwwwwwwwww", left: "wwwnwwww", right: "wwwwnwww", door: "black", sideBeamD: D + gal, sideBeamCz: gal / 2 });
  for (const x of [-7, -3.5, 0, 3.5, 7]) F.add("post_octagon", { h: 2.6, r: 0.1, mat: "wood_dark" }, [x, y1, zg], [0, 0, 0], "Каркас");
  beamX(F, W + 0.3, 0, y1 + 2.6, zg, { d: 0.18 });
  const pitch = 24;
  F.add("wall_plaster", { w: 6, h: gal * Math.tan(pitch * D2R), t: 0.18 }, [-4, t2, D / 2], [0, 0, 0], "Стены");
  F.add("wall_plaster", { w: 6, h: gal * Math.tan(pitch * D2R), t: 0.18 }, [2, t2, D / 2], [0, 0, 0], "Стены");
  F.add("wall_plaster", { w: 2, h: gal * Math.tan(pitch * D2R), t: 0.18 }, [6, t2, D / 2], [0, 0, 0], "Стены");
  gable(F, { W, D: D + gal, cz: gal / 2, top: t2, pitch, over: 0.7, side: 0.6, endSize: 0.6, ridge: "low", fish: "yes" });
  // gallery railing, the stair at the right end climbing toward the front
  houses.railRun(F, -7, zg, 7.55, zg, y1, { mat: "wood_dark", style: "cross", end: false });
  houses.railRun(F, -7, D / 2, -7, zg, y1, { mat: "wood_dark", style: "cross", end: false });
  const sx = 7.6, n = Math.round((y1 - y0) / 0.18), run = n * 0.25;
  houses.stairs(F, { x: sx, z: zg - 0.95 - run, y: y0, H: y1 - y0, w: 0.9, kind: "wood", tread: 0.25, yaw: 180, rails: [-1], railMat: "wood_dark" });
  F.add("floor_plank", { w: 1.0, d: 1.0, t: 0.05, mat: "wood_raw" }, [sx, y1 - 0.05, zg - 0.5], [0, 0, 0], "Пол");
  F.add("post_octagon", { h: y1 - 0.05 - y0, r: 0.08, mat: "wood_dark" }, [sx + 0.45, y0, zg - 0.05], [0, 0, 0], "Каркас");
  steps(houses, F, { x: 0, z: zg + 0.25, H: y0, w: 2.4 });
  // the yard side: hitching rail, trough, cart
  F.add("fence_rail", { w: 3, mat: "wood_old" }, [-5.0, 0, zg + 1.25], [0, 0, 0], "Ограждения");
  F.add("trough", { L: 1.6, mat: "wood_old" }, [-5.0, 0, zg + 0.75], [0, 0, 0], "Мебель и утварь");
  F.add("ox_cart", { L: 2.4, cargo: "sacks" }, [-2.3, 0, zg + 1.4], [0, -90, 0], "Транспорт");
  F.add("sign_board", { w: 2.2, glyphs: 3 }, [0, y0 + 2.75, D / 2 + 0.1], [0, 0, 0], "Декор");
  for (const x of [-5.25, -1.75, 1.75, 5.25]) lantern(F, x, y1 + 2.58, zg + 0.05);
  F.add("shop_flag", { color: "cloth_white", h: 7.0 }, [6.6, 0, zg + 0.7], [0, 180, 0], "Декор");
}

// 12. Игорный шатёр — the caravan's gambling tent: a long ridge tent of red and gold cloth on lacquered poles, a white
//     back wall and a white gable over the open front, curtains closing its back half, a fringe along the eaves; inside
//     gaming tables and stools, the banker's counter with a chest of silver, red lanterns from the ridge.
function gambling(F, houses) {
  const y0 = 0.15, hx = 5.3, Lz = 10.6, ridge = 5.6, eave = 2.55, a = Math.atan2(ridge - eave, hx) / D2R, sl = Math.hypot(hx, ridge - eave) + 0.2;
  plinth(F, { W: 10.4, D: 10.4, h: y0, style: "rammed" });
  // poles
  for (const x of [-5, 5]) for (const z of [-5, -2.5, 0, 2.5, 5]) F.add("post_octagon", { h: eave - 0.1 - y0, r: 0.09, mat: "wood_red" }, [x, y0, z], [0, 0, 0], "Каркас");
  for (const z of [-5, 0, 5]) F.add("post_octagon", { h: ridge - y0 - 0.02, r: 0.11, mat: "wood_red" }, [0, y0, z], [0, 0, 0], "Каркас");
  for (const x of [-5, 5]) F.add("beam", { L: 10.4, h: 0.12, d: 0.12, mat: "wood_red" }, [x, eave - 0.22, 0], [0, 90, 0], "Каркас");
  F.add("beam", { L: Lz + 0.2, h: 0.16, d: 0.16, mat: "wood_dark" }, [0, ridge - 0.02, 0], [0, 90, 0], "Каркас");
  // the cloth: strips down each slope, red and gold in turn
  const n = 20, sw = Lz / n;
  for (const s of [-1, 1]) for (let i = 0; i < n; i++) {
    const mx = s * hx / 2, my = (ridge + eave) / 2 + 0.06;
    F.add("beam", { L: sl, h: 0.06, d: sw, mat: i % 2 ? "silk_yellow" : "silk_red" }, [mx, my, -Lz / 2 + (i + 0.5) * sw], [0, 0, -s * a], "Крыша");
  }
  for (const s of [-1, 1]) F.add("beam", { L: Lz, h: 0.38, d: 0.03, mat: "silk_yellow" }, [s * (hx + 0.08), eave - 0.4, 0], [0, 90, 0], "Крыша");
  F.add("gable", { w: 2 * hx - 0.1, h: ridge - eave, t: 0.05, type: "plaster", fish: "no" }, [0, eave, -Lz / 2 + 0.05], [0, 0, 0], "Стены");
  const g2 = 0.62; // the front gable closes only the upper part of the triangle
  F.add("gable", { w: (2 * hx - 0.1) * g2, h: (ridge - eave) * g2, t: 0.05, type: "plaster", fish: "no" }, [0, ridge - (ridge - eave) * g2, Lz / 2 - 0.05], [0, 0, 0], "Стены");
  F.add("beam", { L: (2 * hx - 0.1) * g2, h: 0.14, d: 0.1, mat: "wood_red" }, [0, ridge - (ridge - eave) * g2 - 0.14, Lz / 2 - 0.05], [0, 0, 0], "Каркас");
  // curtains: the back half closed, the back wall under its gable
  for (const s of [-1, 1]) for (const z of [-3.75, -1.25]) F.add("door_curtain", { w: 2.0, h: eave - 0.3 - y0, color: "cloth_red" }, [s * 5.02, y0, z], [0, 90, 0], "Окна и двери");
  for (const x of [-3.75, -1.25, 1.25, 3.75]) F.add("door_curtain", { w: 2.0, h: eave - 0.3 - y0, color: "cloth_red" }, [x, y0, -5.02], [0, 0, 0], "Окна и двери");
  for (const s of [-1, 1]) F.add("door_curtain", { w: 0.9, h: eave - 0.3 - y0, color: "cloth_white" }, [s * 4.45, y0, 5.05], [0, s * 12, 0], "Окна и двери");
  // gaming tables
  for (const [x, z] of [[-2.6, -2.2], [2.6, -2.2], [-2.6, 1.0], [2.6, 1.0]]) {
    F.add("f_table_square", { a: 1.0, mat: "wood_red" }, [x, y0, z], [0, 0, 0], "Мебель и утварь");
    for (const [dx, dz] of [[-0.75, 0], [0.75, 0], [0, -0.75]]) F.add("f_stool", { style: "square", mat: "wood_dark" }, [x + dx, y0, z + dz], [0, 0, 0], "Мебель и утварь");
  }
  F.add("f_table_round", { r: 0.6, mat: "wood_dark" }, [0, y0, 2.6], [0, 0, 0], "Мебель и утварь");
  for (const a2 of [0, 90, 180, 270]) F.add("f_stool", { style: "drum", mat: "wood_red" }, [Math.cos(a2 * D2R) * 0.95, y0, 2.6 + Math.sin(a2 * D2R) * 0.95], [0, 0, 0], "Мебель и утварь");
  for (const [x, z] of [[-2.6, -2.2], [2.6, 1.0]]) F.add("item", { kind: "coins", size: 1.5 }, [x, y0 + 0.82, z], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "bowl", mat: "porcelain_blue" }, [2.6, y0 + 0.82, -2.2], [0, 0, 0], "Мебель и утварь");
  F.add("vessel", { kind: "cup", mat: "porcelain_blue" }, [-2.6, y0 + 0.82, 1.0], [0, 0, 0], "Мебель и утварь");
  counter(F, 0, y0, -3.9, 3.0, 0, "wood_red");
  F.add("f_chest", { w: 0.8, mat: "wood_red", open: "yes" }, [1.0, y0 + 0.9, -3.9], [0, 0, 0], "Мебель и утварь");
  F.add("item", { kind: "scale", size: 1 }, [-0.9, y0 + 0.9, -3.9], [0, 0, 0], "Мебель и утварь");
  F.add("crate", { a: 0.6, kind: "plain" }, [-4.3, y0, -4.4], [0, 15, 0], "Мебель и утварь");
  F.add("crate", { a: 0.5, kind: "plain" }, [-4.3, y0 + 0.6, -4.4], [0, -10, 0], "Мебель и утварь");
  F.add("sack", { kind: "full" }, [4.3, y0, -4.3], [0, 0, 0], "Мебель и утварь");
  for (const z of [-2.5, 0.5, 3.0]) lantern(F, 0, ridge - 0.12, z, "lantern", 0.9);
  F.add("sign_board", { w: 2.0, glyphs: 3 }, [0, ridge - (ridge - eave) * g2 + 0.55, Lz / 2 - 0.02], [0, 0, 0], "Декор");
  for (const s of [-1, 1]) F.add("banner", { h: 4.6, color: s < 0 ? "silk_yellow" : "silk_red", sign: "glyph" }, [s * 4.75 - 0.4, 0, 5.55], [0, 0, 0], "Декор");
}

const S = "Набор · лавки";
export const RECIPES = [
  { id: "trade_shop", label: "Лавка с мансардой", kind: "Shop", clan: "GuYue", section: S, replaces: "prop_shop", build: make(shop) },
  { id: "trade_shop_small_a", label: "Лавочка с навесом", kind: "Shop", clan: "GuYue", section: S, replaces: "prop_shop_small_a", build: make(shopSmallA) },
  { id: "trade_shop_small_b", label: "Лавочка с фронтоном", kind: "Shop", clan: "GuYue", section: S, replaces: "prop_shop_small_b", build: make(shopSmallB) },
  { id: "trade_shop_small_c", label: "Лавочка с крыльцом", kind: "Shop", clan: "GuYue", section: S, replaces: "prop_shop_small_c", build: make(shopSmallC) },
  { id: "trade_herbalist", label: "Лавка травника", kind: "Herbalist", clan: "GuYue", section: S, replaces: "prop_herbalist", build: make(herbalist) },
  { id: "trade_smithy", label: "Кузница", kind: "Smithy", clan: "GuYue", section: S, replaces: "prop_smithy", build: make(smithy) },
  { id: "trade_granary", label: "Амбар клана", kind: "Granary", clan: "GuYue", section: S, replaces: "prop_granary", build: make(granary) },
  { id: "trade_eatery", label: "Лапшичная", kind: "Tavern", clan: "GuYue", section: S, replaces: "prop_eatery", build: make(eatery) },
  { id: "trade_teahouse", label: "Чайная с балконом", kind: "Teahouse", clan: "GuYue", section: S, replaces: "prop_teahouse", build: make(teahouse) },
  { id: "trade_tavern", label: "Таверна", kind: "Tavern", clan: "GuYue", section: S, replaces: "prop_tavern", build: make(tavern) },
  { id: "trade_inn", label: "Постоялый двор", kind: "Inn", clan: "GuYue", section: S, replaces: "prop_inn", build: make(inn) },
  { id: "trade_gambling", label: "Игорный шатёр", kind: "Gambling", clan: "GuYue", section: S, replaces: "prop_gambling_tent", build: make(gambling) },
];
