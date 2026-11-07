// The Gu Yue village's public buildings, assembled from the artist's kit (export.mjs reads RECIPES).
//
// Eight buildings that stand where the old one-piece models stood (each `replaces` names that model): the great
// academy compound, a classroom hall, a long two-storey dormitory, the refectory, the Gu nursery, the courtyard inn
// (which also serves as the Mo and Chi families' manors), the great tavern and the market row. They are built the way
// the artist's houses in kit/houses.js are — stone platforms, plastered walls between dark timber posts and beams,
// lattice windows and doors, tiled gable roofs with upturned eaves — but sized for public use and kept within the
// game's triangle budgets, which shapes several choices here:
//   - the helpers below lay walls, platforms, floors and roofs in modules as wide as the parts allow (a plastered wall
//     is the same few triangles at 1 m or 4 m; platform blocks and floor boards go in cells up to 4 × 4 m);
//   - a tiled slope costs ~250 triangles a metre of ridge whatever its depth, so a roof is paid for by its length:
//     secondary roofs use the plain "low" ridge, main halls the tiled one with swallow-tail ends;
//   - yard walls are plastered walls with a stone coping and a low tiled ridge (the tiled yard-wall part costs ten
//     times as much and is kept for the fronts people see), yards are paved with the slab path module;
//   - chimneys are brick-faced blocks (the kit's chimney lays every brick).
// Everything that makes the building (platforms, floors, walls, doors, stairs, rails, roofs, counters) is in the
// building's layers — solid, walkable, the doors real openings; lanterns, furniture, jars, banners and the training
// gear are decoration (Detail: no shadow, not solid).
// Conventions as in the kit: metres, Y up, the entrance faces +Z, the footprint is centred on the origin at the end
// (centred()), footings sink 0.25 m below the ground so a slope shows no gap.

const D2R = Math.PI / 180;
const ZERO = [0, 0, 0];
const r4 = (v) => Math.round(v * 1e4) / 1e4;
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);

// L split into n equal cells no longer than max: [[centre offset from the middle, size], …]
function cells(L, max = 4) {
  const n = Math.max(1, Math.ceil(L / max - 1e-6)), s = L / n;
  return Array.from({ length: n }, (_, i) => [-L / 2 + (i + 0.5) * s, s]);
}

// ------------------------------------------------------------------ ground: platforms, paving, steps
// A stone platform W × D, top at h, its blocks sunk `sink` below the ground.
function slab(F, o) {
  const { W, D, h } = o, cx = o.cx || 0, cz = o.cz || 0, sink = o.sink ?? 0.25, style = o.style || "ashlar";
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D))
    F.add("found_block", { w: sw, d: sd, h: h + sink, style }, [cx + ux, -sink, cz + uz], ZERO, o.g || "Фундамент");
}
// Stone slabs over W × D (the 2 m path module, up to 3 m wide, its slabs running across X), top at y + 0.06.
function pave(F, o) {
  const { W, D } = o, cx = o.cx || 0, cz = o.cz || 0, y = o.y || 0;
  for (const [ux, sw] of cells(W, 3)) for (const [uz] of cells(D, 2))
    F.add("path_module", { kind: "slab", w: sw }, [cx + ux, y, cz + uz], ZERO, "Фундамент");
}
// A stone flight from the ground up to H whose top step meets z (it comes toward +Z): returns its front z.
function flight(F, o) {
  const n = Math.max(2, Math.round(o.H / (o.rise || 0.16))), rise = o.H / n, tread = o.tread || 0.3;
  const S = F.at(o.x || 0, 0, o.z, o.yaw || 0);
  S.add("stair_flight", { n, w: o.w, tread, rise, kind: o.kind || "stone" }, [0, o.y || 0, n * tread], ZERO, "Лестницы");
  return n * tread;
}
// A wooden flight with one railed side: from (x, y, z) climbing H toward −Z of the frame; returns its run.
function woodStair(F, o) {
  const n = Math.max(2, Math.round(o.H / 0.18)), rise = o.H / n, tread = 0.26, w = o.w || 1.1, S = F.at(o.x, o.y || 0, o.z, o.yaw || 0);
  S.add("stair_flight", { n, w, tread, rise, kind: "wood" }, ZERO, ZERO, "Лестницы");
  for (const s of o.rails || [1]) S.add("rail_panel_slope", { run: n * tread, rise: o.H, h: 0.9, mat: o.mat || "wood_dark", style: "bars" }, [s * (w / 2 + 0.08), 0, 0], ZERO, "Лестницы");
  return n * tread;
}

// ------------------------------------------------------------------ walls
// One run of wall along X of a frame at (x, z, yaw), its tokens laid from −X to +X:
//   w h W X Y — plain wall 1, 0.5, 2, 3, 4 m;  n N — window 1 m (0.6 × 0.8) / 2 m (1.4 × 0.9) with a lattice;
//   a — 1 m window with a propped-open flap;  d D G — door 1 m (plank leaf), 2 m (double), 3 m (great double);
//   c C — doorway 1 m / 2 m with a curtain;  L — 3 m of lattice door-panels (closed);  q — 2 m octagonal window;
//   m — 2 m moon window;  o O — open bay 1 m / 3 m (nothing).
// Door leaves stand open inward (−Z), as the artist's do; the wall_door sill is 0.14 m.
const TOKW = { w: 1, h: 0.5, W: 2, X: 3, Y: 4, n: 1, N: 2, a: 1, d: 1, D: 2, G: 3, c: 1, C: 2, L: 3, q: 2, m: 2, o: 1, O: 3 };
function run(F, spec, o) {
  const toks = spec.replace(/\s+/g, "").split(""), L = toks.reduce((n, t) => n + TOKW[t], 0);
  if (o.L !== undefined && Math.abs(L - o.L) > 1e-6) throw new Error(`wall "${spec}" is ${L} m, not ${o.L}`);
  const { y, h } = o, t = o.t || 0.2, type = o.type || "plaster", R = F.at(o.x || 0, 0, o.z || 0, o.yaw || 0);
  const smooth = { plaster: "plaster", grey: "plaster_grey", ochre: "plaster_ochre", mud: "mud" }[type] || "plaster";
  let u = -L / 2;
  for (const tk of toks) {
    const w = TOKW[tk], c = u + w / 2; u += w;
    if ("whWXY".includes(tk)) R.add("wall_" + type, { w, h, t }, [c, y, 0], ZERO, "Стены");
    else if (tk === "n" || tk === "N" || tk === "a") {
      const ww = tk === "N" ? 1.4 : 0.6, wh = tk === "N" ? 0.9 : 0.8, sill = o.sill ?? 1.0;
      R.add("wall_window", { w, h, t, type, ww, wh, sill }, [c, y, 0], ZERO, "Стены");
      if (tk === "a") R.add("window_awning", { w: ww, h: wh, mat: "wood_dark", open: 40 }, [c, y + sill, t / 2 + 0.02], ZERO, "Окна и двери");
      else R.add("window_lattice", { w: ww, h: wh, pattern: o.win || "wan", mat: "wood_dark" }, [c, y + sill, 0], ZERO, "Окна и двери");
    } else if (tk === "d") {
      R.add("wall_door", { w, h, t, type, dw: 0.8, dh: 2.05 }, [c, y, 0], ZERO, "Стены");
      R.add("door_leaf", { w: 0.8, h: 1.9, style: o.door || "plank" }, [c - 0.4, y + 0.14, -t / 2 + 0.03], [0, 72, 0], "Окна и двери");
    } else if (tk === "D") {
      R.add("wall_door", { w, h, t, type, dw: 1.4, dh: 2.2 }, [c, y, 0], ZERO, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 0.7, h: 2.06, style: o.door2 || "panel" }, [c + s * 0.7, y + 0.14, -t / 2 + 0.03], [0, 90 - s * 10, 0], "Окна и двери");
    } else if (tk === "G") {
      R.add("wall_door", { w, h, t, type, dw: 2.2, dh: 2.75 }, [c, y, 0], ZERO, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 1.1, h: 2.6, style: o.door3 || "black" }, [c + s * 1.1, y + 0.14, -t / 2 + 0.03], [0, 90 - s * 12, 0], "Окна и двери");
    } else if (tk === "c" || tk === "C") {
      const dw = tk === "C" ? 1.6 : 0.8;
      R.add("wall_door", { w, h, t, type, dw, dh: tk === "C" ? 2.3 : 2.05 }, [c, y, 0], ZERO, "Стены");
      R.add("door_curtain", { w: dw, h: tk === "C" ? 2.1 : 1.9, color: o.curtain || "cloth_blue" }, [c, y + 0.14, t / 2 + 0.04], ZERO, "Окна и двери");
    } else if (tk === "L") R.add("wall_lattice_doors", { w, h: Math.min(h, 5), leaves: 4, mat: o.lattice || "wood_dark", pattern: "wan" }, [c, y, 0], ZERO, "Стены");
    else if (tk === "q") R.add("wall_window_shaped", { w, h, t, shape: "octagon", size: 1.1, cy: Math.min(1.5, h - 0.7), mat: smooth === "mud" ? "mud" : smooth, fill: "lattice" }, [c, y, 0], ZERO, "Стены");
    else if (tk === "m") R.add("wall_window_round", { w, h, t, type: ["plaster", "ochre", "mud"].includes(type) ? type : "plaster", dia: 1.2, cy: Math.min(1.55, h - 0.75) }, [c, y, 0], ZERO, "Стены");
  }
  return L;
}
// The four walls of a W × D body (front +Z, back −Z, left −X, right +X; a side's spec runs back → front on the left,
// front → back on the right), corner posts and the beams on top; returns the top of the beams.
function body(F, o) {
  const { W, D, y, h } = o, t = o.t || 0.2, cx = o.cx || 0, cz = o.cz || 0, w = { ...o, t };
  if (o.front) run(F, o.front, { ...w, L: W, x: cx, z: cz + D / 2, yaw: 0 });
  if (o.back) run(F, o.back, { ...w, L: W, x: cx, z: cz - D / 2, yaw: 180 });
  if (o.left) run(F, o.left, { ...w, L: D, x: cx - W / 2, z: cz, yaw: -90 });
  if (o.right) run(F, o.right, { ...w, L: D, x: cx + W / 2, z: cz, yaw: 90 });
  const pm = o.posts === undefined ? "wood_dark" : o.posts, a = t + 0.08;
  if (pm) {
    for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h, a, mat: pm }, [cx + sx * W / 2, y, cz + sz * D / 2], ZERO, "Каркас");
    // posts between the bays of the long faces, a little proud of the wall
    for (const x of o.bays || []) for (const sz of [-1, 1]) F.add("post_square", { h, a: a - 0.02, mat: pm }, [cx + x, y, cz + sz * D / 2], ZERO, "Каркас");
  }
  if (o.beams !== false) {
    const bm = o.beam || "wood_dark";
    for (const sz of [-1, 1]) for (const [u, L] of cells(W + a, 12)) F.add("beam", { L, h: 0.22, d: t + 0.06, mat: bm }, [cx + u, y + h, cz + sz * D / 2], ZERO, "Каркас");
    for (const sx of [-1, 1]) for (const [u, L] of cells(D - a, 12)) F.add("beam", { L, h: 0.22, d: t + 0.06, mat: bm }, [cx + sx * W / 2, y + h, cz + u], [0, 90, 0], "Каркас");
  }
  return y + h + 0.22;
}
// A row of round columns on stone bases from y to y + h, with a beam over them (along X at z).
function colonnade(F, o) {
  const { xs, z, y, h } = o, r = o.r || 0.14, mat = o.mat || "wood_dark";
  for (const x of xs) {
    F.add("post", { h: h - 0.12, r, mat }, [x, y + 0.12, z], ZERO, "Каркас");
    F.add("stone_block", { w: r * 3, h: 0.12, d: r * 3, mat: "stone_light" }, [x, y, z], ZERO, "Каркас");
  }
  if (o.beam !== false) {
    const x0 = Math.min(...xs) - 0.15, x1 = Math.max(...xs) + 0.15;
    for (const [u, L] of cells(x1 - x0, 12)) F.add("beam", { L, h: 0.22, d: 0.2, mat: o.beamMat || "wood_dark" }, [(x0 + x1) / 2 + u, y + h, z], ZERO, "Каркас");
  }
  return y + h + 0.22;
}

// ------------------------------------------------------------------ floors, rails
// An upper floor W × D at y: joists along Z every ~1.5 m, boards over them; returns the top of the boards.
function deck(F, o) {
  const { W, D, y } = o, cx = o.cx || 0, cz = o.cz || 0;
  const nj = Math.max(2, Math.round(W / 1.5) + 1);
  for (let i = 0; i < nj; i++) {
    const x = cx - W / 2 + 0.1 + (W - 0.2) * i / (nj - 1);
    for (const [u, L] of cells(D, 8)) F.add("floor_joist", { L, h: 0.16, d: 0.14 }, [x, y, cz + u], [0, 90, 0], "Пол");
  }
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D)) F.add("floor_plank", { w: sw, d: sd, mat: o.mat || "wood_raw" }, [cx + ux, y + 0.16, cz + uz], ZERO, "Пол");
  return y + 0.21;
}
// Railing from (x0, z0) to (x1, z1) at height y in sections up to 2 m, a newel at the end.
function rail(F, x0, z0, x1, z1, y, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  for (const [u, w] of cells(L, 2)) F.add("railing", { w, h: o.h || 0.95, mat: o.mat || "wood_dark", style: o.style || "cross" }, [x0 + ux * (L / 2 + u), y, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
  if (o.end !== false) F.add("newel", { h: (o.h || 0.95) + 0.1, mat: o.mat || "wood_dark", top: "moon" }, [x1, y, z1], ZERO, "Ограждения");
}

// ------------------------------------------------------------------ roofs
// A gable roof over a W × D body whose beams top out at `top` (ridge along X): tiled slopes in modules up to 4 m,
// the ridge, the verges, the gables and the ridge's ends; a finial in the middle if asked. Returns the ridge's y.
function roof(F, o) {
  const { W, D, top } = o, cx = o.cx || 0, cz = o.cz || 0, pitch = o.pitch || 28, over = o.over ?? 0.75, side = o.side ?? 0.6;
  const kick = o.kick ?? over, mat = o.mat || "tile", tp = Math.tan(pitch * D2R), run = D / 2 + over, ridgeY = top + 0.05 + (D / 2) * tp;
  const span = W + 2 * side;
  for (const [u, w] of cells(span)) {
    for (const ry of [0, 180]) F.add("roof_tile_slope", { w, run, pitch, kick, mat }, [cx + u, ridgeY, cz], [0, ry, 0], "Крыша");
    F.add("roof_ridge", { w, style: o.ridge || "tile", mat }, [cx + u, ridgeY, cz], ZERO, "Крыша");
  }
  for (const sx of [-1, 1]) {
    for (const ry of [0, 180]) F.add("roof_verge", { run, pitch, kick: Math.min(kick, 2) }, [cx + sx * span / 2, ridgeY, cz], [0, ry, 0], "Крыша");
    if (o.ends !== false) F.add("roof_ridge_end", { size: o.endSize || 1, design: o.end || "swallow", mat }, [cx + sx * span / 2, ridgeY + 0.08, cz], [0, sx > 0 ? 0 : 180, 0], "Крыша");
    if (o.gables !== false) F.add("gable", { w: D, h: (D / 2) * tp, t: o.t || 0.2, type: o.gable || "plaster", fish: o.fish || "yes" }, [cx + sx * W / 2, top, cz], [0, sx * 90, 0], "Стены");
  }
  if (o.finial) F.add("roof_finial", { size: o.finial, design: "moon", mat: "silver" }, [cx, ridgeY + 0.28, cz], ZERO, "Крыша");
  return ridgeY;
}
// A lean-to roof against a wall: from x0 to x1 along X, its top edge on the wall at (y, z), falling toward +Z.
function pent(F, o) {
  const { x0, x1, z, y, run } = o, pitch = o.pitch || 25, kick = o.kick ?? 0.5, mat = o.mat || "tile";
  for (const [u, w] of cells(x1 - x0)) F.add("roof_tile_slope", { w, run, pitch, kick, mat }, [(x0 + x1) / 2 + u, y, z], ZERO, "Крыша");
  for (const x of [x0, x1]) F.add("roof_verge", { run, pitch, kick }, [x, y, z], ZERO, "Крыша");
  for (const [u, L] of cells(x1 - x0, 12)) F.add("beam", { L, h: 0.18, d: 0.16, mat: "wood_dark" }, [(x0 + x1) / 2 + u, y - 0.06, z + 0.04], ZERO, "Крыша");
  // the eave's height at a distance d out from the wall (for posts under it)
  return (d) => y - Math.min(d, run - kick) * Math.tan(pitch * D2R) - Math.max(0, d - (run - kick)) * Math.tan(pitch * 0.55 * D2R);
}
// A yard wall from (x0, z0) to (x1, z1): plastered on its stone footing, a dark stone coping and a low tiled ridge.
function yardWall(F, x0, z0, x1, z1, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  const h = o.h || 2.2, t = o.t || 0.34;
  for (const [u, w] of cells(L, 6)) F.add("wall_" + (o.type || "grey"), { w, h, t }, [x0 + ux * (L / 2 + u), 0, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
  for (const [u, w] of cells(L, 3)) {
    const p = [x0 + ux * (L / 2 + u), h, z0 + uz * (L / 2 + u)];
    F.add("stone_block", { w, h: 0.08, d: t + 0.12, mat: "stone_dark" }, p, [0, yaw, 0], "Ограждения");
  }
  for (const [u, w] of cells(L, 4)) F.add("roof_ridge", { w, style: "low", mat: o.mat || "tile" }, [x0 + ux * (L / 2 + u), h + 0.13, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
}

// ------------------------------------------------------------------ the footprint in the middle
// Shift everything so the building's own parts (not what stands about it) are centred on the origin in X and Z.
function centred(items, kit, THREE) {
  const box = new THREE.Box3(), e = new THREE.Euler(), q = new THREE.Quaternion(), one = new THREE.Vector3(1, 1, 1);
  for (const it of items) {
    if (DETAIL.has(it.g)) continue;
    e.set(it.r[0] * D2R, it.r[1] * D2R, it.r[2] * D2R); q.setFromEuler(e);
    const M = new THREE.Matrix4().compose(new THREE.Vector3(...it.p), q, one);
    box.union(kit.buildPart(it.part, it.params).bbox.clone().applyMatrix4(M));
  }
  const mx = (box.min.x + box.max.x) / 2, mz = (box.min.z + box.max.z) / 2;
  for (const it of items) it.p = [r4(it.p[0] - mx), it.p[1], r4(it.p[2] - mz)];
  return items;
}
const make = (fn) => (houses, kit, THREE) => { const items = []; fn(new houses.Frame(items), kit); return centred(items, kit, THREE); };

// ================================================================== the classroom hall (17 × 16 × 8.5)
// A hall 13 × 8 m on a 0.75 m platform, a porch of six columns 2.5 m deep under the same roof, the great door between
// lattice panels, a wide stone flight in front with stone lanterns; inside, rows of benches before the master's desk.
function academy(F) {
  const PL = 0.75, W = 13, D = 8, zf = 1.4, zc = zf - D / 2, zp = zf + 2.5;
  slab(F, { W: 16, D: 13.5, h: PL, cz: -0.85 });
  const top = body(F, { W, D, cz: zc, y: PL, h: 3.9, front: "hNLDLNh", back: "wnWwnwnwWnw", left: "wnWWnw", right: "wnWWnw", bays: [-3.25, 3.25], win: "wan" });
  // over the front wall, up to the roof that runs on to the porch beam
  const roofD = zp - (zc - D / 2), roofZ = (zp + zc - D / 2) / 2, tp = Math.tan(27 * D2R);
  const gap = (zp - zf) * tp;
  for (const [u, w] of cells(W, 6)) F.add("wall_timber", { w, h: gap, t: 0.16 }, [u, top, zf], ZERO, "Стены");
  colonnade(F, { xs: [-6.5, -3.9, -1.3, 1.3, 3.9, 6.5], z: zp, y: PL, h: top - 0.22 - PL, r: 0.15 });
  for (const sx of [-1, 1]) F.add("beam", { L: zp - zf - 0.15, h: 0.22, d: 0.2 }, [sx * W / 2, top - 0.22, (zp + zf) / 2], [0, 90, 0], "Каркас");
  roof(F, { W, D: roofD, cz: roofZ, top, pitch: 27, over: 0.8, side: 0.7, mat: "tile" });
  // the porch rail between the columns, open in the middle bay; the flight before it
  for (const [a, b] of [[-6.5, -1.3], [1.3, 6.5]]) rail(F, a, zp, b, zp, PL, { style: "wan", end: false });
  const zFront = -0.85 + 13.5 / 2;
  flight(F, { H: PL, w: 4, z: zFront, rise: 0.15 });
  for (const sx of [-1, 1]) F.add("stone_lantern", { design: "moon", mat: "stone_light" }, [sx * 3.1, 0, zFront + 0.9], ZERO, "Декор");
  for (const x of [-4.6, -1.9, 1.9, 4.6]) F.add("lantern_hanging", { size: 1.1, color: "paper" }, [x, top - 0.25, zp - 0.05], ZERO, "Декор");
  F.add("sign_board", { w: 1.8, glyphs: 2 }, [0, PL + 2.95, zf + 0.12], ZERO, "Декор");
  F.add("door_couplets", { w: 1.4, h: 2.2 }, [0, PL, zf + 0.11], ZERO, "Декор");
  // inside: the master's desk and chair at the back, four rows of benches with low tables
  const zi = zc - D / 2;
  F.add("f_desk", { w: 1.6, mat: "wood_dark" }, [0, PL, zi + 1.6], [0, 180, 0], "Мебель и утварь");
  F.add("f_chair", { style: "official", mat: "wood_dark" }, [0, PL, zi + 0.9], ZERO, "Мебель и утварь");
  for (const sx of [-1, 1]) F.add("f_candle_stand", { mat: "bronze" }, [sx * 1.6, PL, zi + 1.0], ZERO, "Мебель и утварь");
  for (let r = 0; r < 4; r++) for (const sx of [-1, 1]) {
    F.add("f_table_low", { w: 1.4, mat: "wood_dark" }, [sx * 2.6, PL, zi + 3.2 + r * 1.15], ZERO, "Мебель и утварь");
    F.add("f_bench", { w: 1.6, mat: "wood_raw" }, [sx * 2.6, PL, zi + 3.65 + r * 1.15], ZERO, "Мебель и утварь");
  }
}

export const RECIPES = [
  { id: "pub_academy", label: "Учебный зал", kind: "Academy", clan: "GuYue", section: "Набор · клан", replaces: "prop_academy", build: make(academy) },
];
