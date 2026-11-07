// The Gu Yue clan's great buildings, assembled from the artist's kit (export.mjs reads RECIPES).
//
// Six buildings for the clan's own quarter: the great five-storey hall at the heart of the village (with its terraces,
// side halls and galleries), the hall of the elders, the ancestral hall, the many-sided tea pavilion, a court gate and a
// bell tower. They stand on pads the village planner cuts, so each keeps the footprint of the old model it replaces.
//
// The look follows the art direction for the Gu Yue: dark timber columns, beams painted blue-green with gold, white
// plaster walls on a stone skirting, blue-grey tile roofs with upturned eaves, cold pale lanterns, the crescent moon as
// the clan's sign. What makes these buildings different from the houses is the roof: hips, hip-and-gable roofs and
// tiered eaves. The kit's own hip corner and pavilion facet are far too heavy for the game (4–6 thousand triangles a
// corner), so the roofs here are made from the plain tiled slope only:
//   - an eave ring (eaveRing) runs slopes round an inner polygon, the pivot of every slope on that polygon; where two
//     faces meet, the corner is filled by narrow strips of slope, each starting where its face's half of the corner
//     begins (on the hip line) and ending on the common eave — the strips step along the hip, and a long low ridge cap
//     laid down the hip hides the steps; an upturned tile (ицзяо) ends every hip at the eave;
//   - a hip roof is such a ring round its own ridge, a pyramid round a point, a tier of a tower is a ring round the
//     next storey's walls, and a hip-and-gable roof is a ring round an inset rectangle with a gable roof standing on it;
//   - the slopes' tiles are stretched across (the slope modules scaled along their ridge), so a big roof is not paid
//     for at the tile's real size: ~120 triangles a metre of eave instead of ~250.
// Everything that is the building (platforms, floors, walls, doors, stairs, balustrades, roofs) is in the building's
// layers — solid and walkable, the doors real openings; lanterns, banners, burners, furniture are decoration.
// Conventions as in the kit: metres, Y up, the entrance faces +Z, the footprint centred on the origin (centred()),
// footings sunk 0.3 m below the ground so a slope shows no gap.

const D2R = Math.PI / 180;
const ZERO = [0, 0, 0];
const r4 = (v) => Math.round(v * 1e4) / 1e4;
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);
const SINK = 0.3;

// L split into n equal cells no longer than max: [[centre offset from the middle, size], …]
function cells(L, max = 4) {
  const n = Math.max(1, Math.ceil(L / max - 1e-6)), s = L / n;
  return Array.from({ length: n }, (_, i) => [-L / 2 + (i + 0.5) * s, s]);
}
// F.add with a scale (the kit's Frame always writes [1, 1, 1])
function put(F, part, params, p, r = ZERO, g = "Декор", s = null) {
  F.add(part, params, p, r, g);
  if (s) F.items[F.items.length - 1].s = s.map(r4);
}

// ------------------------------------------------------------------ ground: blocks, stairs, balustrades
// A stone block W × D whose top is at y + h (bottom sunk to y − sink), in pieces up to 8 × 8 (the landing part: one box
// and its cap, 24 triangles whatever its size).
function block(F, o) {
  const { W, D, h } = o, cx = o.cx || 0, cz = o.cz || 0, y = o.y || 0, sink = o.sink ?? SINK, mat = o.mat || "ashlar";
  for (const [ux, sw] of cells(W, 8)) for (const [uz, sd] of cells(D, 8))
    F.add("stair_landing", { w: sw, d: sd, h: h + sink, mat }, [cx + ux, y - sink, cz + uz + sd / 2], ZERO, o.g || "Фундамент");
}
// A wooden deck W × D, its top at y (a floor or a ceiling of boards).
function deck(F, o) {
  const { W, D, y } = o, cx = o.cx || 0, cz = o.cz || 0, t = o.t || 0.2;
  for (const [ux, sw] of cells(W, 8)) for (const [uz, sd] of cells(D, 8))
    F.add("stair_landing", { w: sw, d: sd, h: t, mat: "wood_raw" }, [cx + ux, y - t, cz + uz + sd / 2], ZERO, "Пол");
}
// A stone flight from y up H whose top step meets z (it climbs toward −Z of the frame); returns its run.
function flight(F, o) {
  const n = Math.max(2, Math.round(o.H / (o.rise || 0.15))), rise = o.H / n, tread = o.tread || 0.32;
  const S = F.at(o.x || 0, 0, o.z, o.yaw || 0);
  S.add("stair_flight", { n, w: o.w, tread, rise, kind: o.kind || "stone" }, [0, o.y || 0, n * tread], ZERO, "Лестницы");
  return n * tread;
}
// A stone balustrade from (x0, z0) to (x1, z1) on y: solid panels up to 3 m, a post at every joint.
function balustrade(F, x0, z0, x1, z1, y, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  const mat = o.mat || "stone_light", cs = cells(L, o.max || 3);
  for (const [u, w] of cs) F.add("balustrade_stone", { w: w - 0.2, h: 0.9, mat, pattern: o.pattern || "solid" }, [x0 + ux * (L / 2 + u), y, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
  const posts = [0, ...cs.map(([u, w]) => L / 2 + u + w / 2)];
  for (const d of posts) F.add("stone_post", { h: 1.15, mat, top: o.top || "plain" }, [x0 + ux * d, y, z0 + uz * d], ZERO, "Ограждения");
}
// A low stone parapet (dressed blocks) from (x0, z0) to (x1, z1) on y.
function parapet(F, x0, z0, x1, z1, y, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  for (const [u, w] of cells(L, 3)) F.add("stone_block", { w, h: o.h || 0.55, d: o.d || 0.42, mat: o.mat || "stone_light" }, [x0 + ux * (L / 2 + u), y, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
}
// A wooden railing from (x0, z0) to (x1, z1) on y in sections up to 2 m.
function rail(F, x0, z0, x1, z1, y, o = {}) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), ux = dx / L, uz = dz / L, yaw = Math.atan2(-dz, dx) / D2R;
  for (const [u, w] of cells(L, 2)) F.add("railing", { w, h: o.h || 0.9, mat: o.mat || "wood_dark", style: o.style || "cross" }, [x0 + ux * (L / 2 + u), y, z0 + uz * (L / 2 + u)], [0, yaw, 0], "Ограждения");
  if (o.end !== false) F.add("newel", { h: (o.h || 0.9) + 0.1, mat: o.mat || "wood_dark", top: "moon" }, [x1, y, z1], ZERO, "Ограждения");
}

// ------------------------------------------------------------------ walls, columns, beams
// One run of wall along X of a frame at (x, z, yaw), its tokens laid from −X to +X:
//   h w W X Y — plain wall 0.5 … 4 m (runs of plain wall are laid as one module up to 6 m);
//   n N — window 1 m (0.6 × 0.8) / 2 m (1.4 × 1.0) with a lattice;  q — 2 m octagonal window (open, framed);
//   d D G — door 1 m (plank), 2 m (double, 1.4 m), 3 m (great double, 2.2 m);  o O — open bay 1 / 3 m (nothing).
// If o.L is given and the tokens fall short, the difference is shared by two plain pieces at the ends.
// Door leaves stand open inward (−Z), as the artist's do; the wall_door sill is 0.14 m.
const TOKW = { h: 0.5, w: 1, W: 2, X: 3, Y: 4, n: 1, N: 2, q: 2, d: 1, D: 2, G: 3, o: 1, O: 3 };
function run(F, spec, o) {
  let toks = spec.replace(/\s+/g, "").split("");
  const L0 = toks.reduce((n, t) => n + TOKW[t], 0), L = o.L ?? L0, pad = (L - L0) / 2;
  if (pad < -1e-6) throw new Error(`wall "${spec}" is ${L0} m, longer than ${L}`);
  const { y, h } = o, t = o.t || 0.24, type = o.type || "plaster", R = F.at(o.x || 0, 0, o.z || 0, o.yaw || 0);
  const smooth = { plaster: "plaster", grey: "plaster_grey", ochre: "plaster_ochre", mud: "mud" }[type] || "plaster";
  const seq = []; if (pad > 1e-6) seq.push(["p", pad]); for (const tk of toks) seq.push([tk, TOKW[tk]]); if (pad > 1e-6) seq.push(["p", pad]);
  let u = -L / 2, plain0 = null;
  const flush = (end) => { if (plain0 === null) return; const len = end - plain0; for (const [c, w] of cells(len, 6)) R.add("wall_" + type, { w, h, t }, [plain0 + len / 2 + c, y, 0], ZERO, "Стены"); plain0 = null; };
  for (const [tk, w] of seq) {
    const c = u + w / 2;
    if (tk === "p" || "hwWXY".includes(tk)) { if (plain0 === null) plain0 = u; u += w; continue; }
    flush(u); u += w;
    if (tk === "n" || tk === "N") {
      const ww = tk === "N" ? 1.4 : 0.6, wh = tk === "N" ? 1.0 : 0.8, sill = o.sill ?? 1.0;
      R.add("wall_window", { w, h, t, type, ww, wh, sill }, [c, y, 0], ZERO, "Стены");
      R.add("window_lattice", { w: ww, h: wh, pattern: o.win || "wan", mat: o.winMat || "wood_dark" }, [c, y + sill, 0], ZERO, "Окна и двери");
    } else if (tk === "q") {
      R.add("wall_window_shaped", { w, h, t, shape: "octagon", size: 1.1, cy: Math.min(o.qy ?? 1.55, h - 0.7), mat: smooth, fill: "open" }, [c, y, 0], ZERO, "Стены");
    } else if (tk === "d") {
      R.add("wall_door", { w, h, t, type, dw: 0.8, dh: 2.05 }, [c, y, 0], ZERO, "Стены");
      R.add("door_leaf", { w: 0.8, h: 1.9, style: o.door || "plank" }, [c - 0.4, y + 0.14, -t / 2 + 0.03], [0, 72, 0], "Окна и двери");
    } else if (tk === "D") {
      R.add("wall_door", { w, h, t, type, dw: 1.4, dh: 2.3 }, [c, y, 0], ZERO, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 0.7, h: 2.16, style: o.door2 || "panel" }, [c + s * 0.7, y + 0.14, -t / 2 + 0.03], [0, 90 - s * 10, 0], "Окна и двери");
    } else if (tk === "G") {
      const dh = Math.min(3, h - 0.3);
      R.add("wall_door", { w, h, t, type, dw: 2.2, dh }, [c, y, 0], ZERO, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 1.1, h: dh - 0.15, style: o.door3 || "panel" }, [c + s * 1.1, y + 0.14, -t / 2 + 0.03], [0, 90 - s * 12, 0], "Окна и двери");
    }
  }
  flush(u);
  return L;
}
// The four walls of a W × D body (front +Z, back −Z, left −X, right +X), posts at the corners (and at o.bays along the
// long faces), beams on top unless beams: false; returns the top of the beams.
function body(F, o) {
  const { W, D, y, h } = o, t = o.t || 0.24, cx = o.cx || 0, cz = o.cz || 0, w = { ...o, t };
  if (o.front) run(F, o.front, { ...w, L: W, x: cx, z: cz + D / 2, yaw: 0 });
  if (o.back) run(F, o.back, { ...w, L: W, x: cx, z: cz - D / 2, yaw: 180 });
  if (o.left) run(F, o.left, { ...w, L: D, x: cx - W / 2, z: cz, yaw: -90 });
  if (o.right) run(F, o.right, { ...w, L: D, x: cx + W / 2, z: cz, yaw: 90 });
  const pm = o.posts === undefined ? "wood_dark" : o.posts, a = t + 0.1;
  if (pm) {
    for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h, a, mat: pm }, [cx + sx * W / 2, y, cz + sz * D / 2], ZERO, "Каркас");
    for (const x of o.bays || []) for (const sz of [-1, 1]) F.add("post_square", { h, a: a - 0.02, mat: pm }, [cx + x, y, cz + sz * D / 2], ZERO, "Каркас");
  }
  if (o.beams === false) return y + h;
  return beamRing(F, { W, D, cx, cz, y: y + h, h: o.beamH || 0.32, style: o.beamStyle || "hexi", plain: o.plainBeam });
}
// Beams round a W × D rectangle at y (painted unless plain); returns their top.
function beamRing(F, o) {
  const { W, D, y, h } = o, cx = o.cx || 0, cz = o.cz || 0, d = h * 0.7;
  const one = (L, p, yaw) => o.plain ? F.add("beam", { L, h, d: Math.max(0.16, d), mat: o.plain }, p, [0, yaw, 0], "Каркас") : F.add("beam_painted", { L: Math.max(1, L), h, style: o.style || "hexi" }, p, [0, yaw, 0], "Каркас");
  for (const sz of [-1, 1]) for (const [u, L] of cells(W + d, 12)) one(L, [cx + u, y, cz + sz * D / 2], 0);
  for (const sx of [-1, 1]) for (const [u, L] of cells(D - d, 12)) one(L, [cx + sx * W / 2, y, cz + u], 90);
  return y + h;
}
// Round columns on square stone bases from y to y + h at the given points.
function columns(F, pts, y, h, o = {}) {
  const r = o.r || 0.2, mat = o.mat || "wood_dark";
  for (const [x, z] of pts) {
    F.add("stone_footing", { r: r * 1.15, design: o.base || "square", mat: "stone_light" }, [x, y, z], ZERO, "Каркас");
    F.add(o.oct ? "post_octagon" : "post", { h: h - 0.2, r, mat }, [x, y + 0.2, z], ZERO, "Каркас");
  }
}
// Points round a W × D rectangle: the four corners, nx − 1 between them along X and nz − 1 along Z.
function ringPts(W, D, nx, nz, cx = 0, cz = 0) {
  const pts = [];
  for (let i = 0; i <= nx; i++) for (const sz of [-1, 1]) pts.push([cx - W / 2 + (W * i) / nx, cz + sz * D / 2]);
  for (let j = 1; j < nz; j++) for (const sx of [-1, 1]) pts.push([cx + sx * W / 2, cz - D / 2 + (D * j) / nz]);
  return pts;
}
// Bracket sets (dougong) on the points, their levers pointing out of the W × D rectangle; returns their top.
function brackets(F, pts, y, W, D, o = {}) {
  const size = o.size || 1, tiers = o.tiers || 1, mat = o.mat || "painted";
  for (const [x, z] of pts) {
    const ex = Math.abs(Math.abs(x) - W / 2) < 0.05 ? Math.sign(x) : 0, ez = Math.abs(Math.abs(z) - D / 2) < 0.05 ? Math.sign(z) : 0;
    const yaw = Math.atan2(ex, ez) / D2R;
    F.add("dougong", { size, tiers, mat }, [x, y, z], [0, yaw, 0], "Каркас");
  }
  return y + size * (0.18 + 0.18 * tiers + 0.12) + 0.01;
}

// ------------------------------------------------------------------ roofs
// The height of the kit's tiled slope below its ridge at a distance t out (main pitch, then the flatter kick).
function prof(R, pitch, kick) {
  const tp = Math.tan(pitch * D2R), tk = Math.tan(pitch * 0.55 * D2R), z1 = Math.max(0.01, R - kick);
  return (t) => (t <= z1 ? -t * tp : -z1 * tp - (t - z1) * tk);
}
// One tile slope of width w on its ridge at p (falling toward +Z turned by yaw), its tiles stretched sx times across.
function slope(F, p, yaw, w, o) {
  const sx = Math.max(1, Math.min(o.sx || 2, w / 0.25));
  put(F, "roof_tile_slope", { w: w / sx, run: o.run, pitch: o.pitch, kick: o.kick, mat: o.mat || "tile" }, p, [0, yaw, 0], "Крыша", [sx, 1, 1]);
}
// The faces of a ring round a rectangle with half sides a (X) and b (Z): each face's pivot line at c, turned by yaw
// (its outward normal), half as long as L, the corners on both its ends cut at half-angle h.
const rectFaces = (a, b) => [
  { c: [0, b], yaw: 0, L: a, h: 45 }, { c: [a, 0], yaw: 90, L: b, h: 45 },
  { c: [0, -b], yaw: 180, L: a, h: 45 }, { c: [-a, 0], yaw: -90, L: b, h: 45 },
];
// … and round a regular n-gon whose sides lie at the distance r from the middle (one side facing +Z).
const polyFaces = (n, r) => Array.from({ length: n }, (_, k) => {
  const phi = (k * 360) / n;
  return { c: [r * Math.sin(phi * D2R), r * Math.cos(phi * D2R)], yaw: phi, L: r * Math.tan(Math.PI / n), h: 180 / n };
});
// An eave ring: tiled slopes round the faces at height y, run R out from every face's line. Each face lays its straight
// stretch and its halves of the corners: strips `strip` wide, each starting where the face's half of the corner begins
// (t ≥ u / tan h) and ending on the common eave; on each corner (at the face's +L end) a low ridge cap down the hip
// and an upturned end tile.
function eaveRing(F, o) {
  const { y, R, pitch, kick } = o, mat = o.mat || "tile", P = prof(R, pitch, kick), knee = Math.max(0.01, R - kick);
  const strip = o.strip || 0.6, sx = o.sx || 2;
  for (const f of o.faces) {
    const Fk = F.at(f.c[0], y, f.c[1], f.yaw);
    if (f.L > 0.05) for (const [u, w] of cells(2 * f.L, 4 * sx)) slope(Fk, [u, 0, 0], 0, w, { run: R, pitch, kick, mat, sx });
    const tg = Math.tan(f.h * D2R), umax = R * tg, n = Math.max(1, Math.ceil(umax / strip - 1e-6)), sw = umax / n;
    for (const side of [-1, 1]) for (let i = 0; i < n; i++) {
      const um = (i + 0.5) * sw, t0 = um / tg, run = R - t0;
      if (run < 0.12) continue;
      const inKick = t0 >= knee - 1e-6;
      put(Fk, "roof_tile_slope", { w: 0.25, run, pitch: inKick ? pitch * 0.55 : pitch, kick: inKick ? 0 : kick, mat }, [side * (f.L + um), P(t0), t0], ZERO, "Крыша", [sw / 0.25, 1, 1]);
    }
    if (o.hips !== false) hip(Fk, f.L, f.h, { R, pitch, kick, mat, cap: o.cap, upturn: o.upturn });
  }
}
// The hip at the +L end of a face (its frame, ring pivot at y = 0): two cap pieces down the hip line (the main pitch
// and the kick) and the upturned end tile at the eave.
function hip(Fk, L, h, o) {
  const { R, pitch, kick } = o, P = prof(R, pitch, kick), knee = Math.max(0.01, R - kick), tg = Math.tan(h * D2R), ch = Math.cos(h * D2R);
  const at = (t) => [L + t * tg, P(t), t];
  const capS = o.cap || [1, 1.6, 2.2], lift = 0.05 * capS[1];
  for (const [ta, tb] of [[0, knee], [knee, R]]) {
    if (tb - ta < 0.05) continue;
    const A = at(ta), B = at(tb), horiz = (tb - ta) / ch, drop = A[1] - B[1], len = Math.hypot(horiz, drop), alpha = Math.atan2(drop, horiz) / D2R;
    const n = Math.ceil(len / 4 - 1e-6);
    for (let i = 0; i < n; i++) {
      const f = (i + 0.5) / n;
      put(Fk, "roof_ridge", { w: Math.max(0.25, len / n), style: "low", mat: o.mat }, [A[0] + (B[0] - A[0]) * f, A[1] + (B[1] - A[1]) * f + lift, A[2] + (B[2] - A[2]) * f], [0, h - 90, -alpha], "Крыша", capS);
    }
  }
  if (o.upturn) {
    const u = o.upturn, t = R - (u.back ?? 0.25), p = at(t), sc = u.s || 1.8;
    put(Fk, "tile_corner_upturn", { L: u.L || 1.2, lift: (u.lift || 0.9) / sc, mat: o.mat }, [p[0], p[1] + lift, p[2]], [0, h, 0], "Крыша", [sc, sc, 1]);
  }
}
// A ridge along X from x0 to x1 at y (its tiles stretched sx times), with ends of the given design.
function ridge(F, x0, x1, y, o = {}) {
  const sx = o.sx || 2, cz = o.cz || 0;
  for (const [u, w] of cells(x1 - x0, 4 * sx)) put(F, "roof_ridge", { w: w / sx, style: o.style || "tile", mat: o.mat || "tile" }, [(x0 + x1) / 2 + u, y, cz], ZERO, "Крыша", [sx, 1, 1]);
  if (o.ends !== false) for (const [x, ry] of [[x1, 0], [x0, 180]]) F.add("roof_ridge_end", { size: o.endSize || 1, design: o.end || "swallow", mat: o.endMat || o.mat || "tile" }, [x, y + 0.08, cz], [0, ry, 0], "Крыша");
}
// A gable roof over a W × D body whose beams top out at `top` (ridge along X); returns the ridge's y.
function gableRoof(F, o) {
  const { W, D, top } = o, cx = o.cx || 0, cz = o.cz || 0, pitch = o.pitch || 28, over = o.over ?? 0.8, side = o.side ?? 0.6;
  const kick = o.kick ?? Math.min(2, over * 0.9), mat = o.mat || "tile", tp = Math.tan(pitch * D2R), run = D / 2 + over, ridgeY = top + 0.05 + (D / 2) * tp;
  const span = W + 2 * side, sx = o.sx || 2;
  for (const [u, w] of cells(span, 4 * sx)) for (const ry of [0, 180]) slope(F, [cx + u, ridgeY, cz], ry, w, { run, pitch, kick, mat, sx });
  ridge(F, cx - span / 2, cx + span / 2, ridgeY, { cz, style: o.ridge || "tile", mat, end: o.end, endSize: o.endSize, ends: o.ends, sx: o.ridgeSx });
  for (const sx2 of [-1, 1]) for (const ry of [0, 180]) F.add("roof_verge", { run, pitch, kick }, [cx + sx2 * span / 2, ridgeY, cz], [0, ry, 0], "Крыша");
  if (o.gables !== false) for (const sx2 of [-1, 1]) F.add("gable", { w: D, h: (D / 2) * tp, t: o.t || 0.2, type: o.gable || "plaster", fish: o.fish || "yes" }, [cx + sx2 * W / 2, top, cz], [0, sx2 * 90, 0], "Стены");
  return ridgeY;
}
// A hip-and-gable roof (歇山) over a body whose column line has half sides A × B and whose brackets top out at `top`:
// an eave ring round the rectangle inset by g (its slopes resting on the column line), and on that rectangle a gable
// roof a little steeper (pitchUp) with gables, verges and the main ridge. Returns the ridge's y.
function hipGable(F, o) {
  const { A, B, top, g } = o, pitch = o.pitch || 27, up = o.pitchUp || pitch + 6, kick = o.kick ?? 0.7, mat = o.mat || "tile", sx = o.sx || 2;
  const a = A - g, b = B - g, yP = top + g * Math.tan(pitch * D2R) + 0.02, R = g + o.over;
  eaveRing(F, { faces: rectFaces(a, b), y: yP, R, pitch, kick, mat, sx, strip: o.strip, cap: o.cap, upturn: o.upturn });
  const tu = Math.tan(up * D2R), yR = yP + b * tu, side = o.side ?? 0.45, span = 2 * (a + side);
  for (const [u, w] of cells(span, 4 * sx)) for (const ry of [0, 180]) slope(F, [u, yR, 0], ry, w, { run: b + 0.15, pitch: up, kick: 0, mat, sx });
  ridge(F, -span / 2, span / 2, yR, { style: o.ridge || "tile", mat, end: o.end || "chiwen", endSize: o.endSize || 0.9, endMat: o.endMat, sx: o.ridgeSx });
  for (const s of [-1, 1]) {
    for (const ry of [0, 180]) F.add("roof_verge", { run: b + 0.15, pitch: up, kick: 0 }, [s * span / 2, yR, 0], [0, ry, 0], "Крыша");
    F.add("gable", { w: 2 * b, h: b * tu, t: 0.2, type: o.gable || "plaster", fish: "yes" }, [s * (a - 0.1), yP, 0], [0, s * 90, 0], "Стены");
  }
  return yR;
}

// ------------------------------------------------------------------ the footprint in the middle
// Shift everything so the building's own parts (not what stands about it) are centred on the origin in X and Z.
function centred(items, kit, THREE) {
  const box = new THREE.Box3(), e = new THREE.Euler(), q = new THREE.Quaternion();
  for (const it of items) {
    if (DETAIL.has(it.g)) continue;
    e.set(it.r[0] * D2R, it.r[1] * D2R, it.r[2] * D2R); q.setFromEuler(e);
    const M = new THREE.Matrix4().compose(new THREE.Vector3(...it.p), q, new THREE.Vector3(...(it.s || [1, 1, 1])));
    box.union(kit.buildPart(it.part, it.params).bbox.clone().applyMatrix4(M));
  }
  const mx = (box.min.x + box.max.x) / 2, mz = (box.min.z + box.max.z) / 2;
  for (const it of items) it.p = [r4(it.p[0] - mx), it.p[1], r4(it.p[2] - mz)];
  return items;
}
const make = (fn) => (houses, kit, THREE) => { const items = []; fn(new houses.Frame(items), kit); return centred(items, kit, THREE); };

// ================================================================== the hall of the elders (15.9 × 12.1 × 9.4)
// A hall 11.6 × 7.4 m (column line) on a 0.9 m stone podium: a porch of five red columns before the front wall, a
// double door between lattice windows, the hip-and-gable roof on painted bracket sets; a stone flight to the porch.
// Inside, the elders' chairs in a row before a screen, the long table, banners of the moon.
function council(F) {
  const PH = 0.9, A = 5.8, B = 3.7, H = 4.2, zw = B - 1.5;
  block(F, { W: 13.4, D: 9.0, h: PH });
  flight(F, { H: PH, w: 3.6, z: 4.5, rise: 0.15, tread: 0.3 });
  // walls: front set back behind the porch, the sides from the back to it
  const wall = { y: PH, h: H, t: 0.24, type: "plaster", win: "wan", sill: 1.0 };
  run(F, "wNwDwNw", { ...wall, L: 2 * A, z: zw });
  run(F, "WNWNWNW", { ...wall, L: 2 * A, z: -B, yaw: 180 });
  for (const s of [-1, 1]) run(F, "wNwNw", { ...wall, L: B + zw, x: s * A, z: (zw - B) / 2, yaw: s * 90 });
  // columns: the porch row and the back row on stone bases, beams and brackets round the column line
  const red = { r: 0.2, mat: "wood_red" };
  columns(F, ringPts(2 * A, 2 * B, 4, 2), PH, H, red);
  columns(F, [[-A, zw], [A, zw]], PH, H, red);
  const bt = beamRing(F, { W: 2 * A, D: 2 * B, y: PH + H, h: 0.36, style: "red" });
  // over the walls up to the roof, behind the brackets: a frieze of plaster
  for (const [z, yaw, L] of [[zw, 0, 2 * A], [-B, 180, 2 * A]]) for (const [u, w] of cells(L, 6)) F.add("wall_plaster", { w, h: 0.8, t: 0.16 }, [u, bt, z], [0, yaw, 0], "Стены");
  const top = brackets(F, ringPts(2 * A, 2 * B, 4, 2), bt, 2 * A, 2 * B, { size: 0.85, tiers: 1, mat: "painted" });
  hipGable(F, { A, B, top, g: 1.8, over: 1.45, pitch: 27, pitchUp: 33, kick: 0.7, mat: "tile", end: "chiwen", endSize: 0.85, strip: 0.6,
    upturn: { L: 1.1, lift: 0.75 } });
  // the porch: its floor is the podium; a sign over the door, couplets, lanterns under the eaves
  F.add("sign_board", { w: 1.8, glyphs: 3 }, [0, PH + 2.95, zw + 0.13], ZERO, "Декор");
  F.add("door_couplets", { w: 1.4, h: 2.3 }, [0, PH, zw + 0.13], ZERO, "Декор");
  for (const x of [-2.9, 2.9]) F.add("lantern_hanging", { size: 1.1, color: "paper" }, [x, PH + H - 0.05, B], ZERO, "Декор");
  for (const x of [-1.6, 1.6]) F.add("stone_lantern", { design: "moon", mat: "stone_light" }, [x * 3.4, 0, 6.2], ZERO, "Декор");
  // inside: five elders' chairs before a screen, the head's in the middle, the long table before them
  const zi = -B + 0.9;
  F.add("f_screen", { panels: 6, mat: "wood_dark", art: "landscape" }, [0, PH, zi - 0.45], ZERO, "Мебель и утварь");
  for (let i = -2; i <= 2; i++) F.add("f_chair", { style: i ? "official" : "throne", mat: "wood_dark" }, [i * 1.5, PH, zi + 0.4], ZERO, "Мебель и утварь");
  F.add("f_altar_table", { w: 3.2, mat: "wood_dark" }, [0, PH, zi + 1.8], ZERO, "Мебель и утварь");
  for (const x of [-4.6, 4.6]) F.add("banner", { h: 3.4, color: "cloth_white", sign: "moon" }, [x, PH, zi], ZERO, "Декор");
  for (const x of [-3.6, 3.6]) F.add("f_candle_stand", { mat: "bronze" }, [x, PH, zi + 0.5], ZERO, "Мебель и утварь");
}

export const RECIPES = [
  { id: "hall_council", label: "Зал старейшин", kind: "Hall", clan: "GuYue", section: "Набор · клан", replaces: "prop_council_hall", build: make(council) },
];
