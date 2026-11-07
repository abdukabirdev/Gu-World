// The ordinary houses of the Gu Yue village, assembled from the artist's kit (export.mjs reads RECIPES), 2026-10-31.
//
// About fourteen hundred houses of the village are built from these variants, so they are made to be many and light
// rather than few and rich. They come from six parametric builders of our own, each using the artist's parts and his
// Frame (houses.Frame) the way kit/houses.js does:
//   familyHouse — a clan family's house on a stone plinth: one or two storeys, the upper one jutting forward on
//                 brackets, carried over an arcade on posts, or opening on a railed gallery; a tiled skirt eave between
//                 the floors; a lean-to shed at its side, a walled yard with a gate in front of it;
//   stiltHouse  — a house on stilts with a railed veranda under its roof and a wooden stair, one or two storeys; with
//                 `terrace` its back half stands on a 2 m stone terrace and only its front on stilts (the ground drops
//                 2 m toward +Z: meta.drop; meta.edgeZ is where the terrace's face is, filled in when built);
//   hut         — a poor family's one-room house: mud, plank or bamboo walls, a thatched or old tiled roof, a curtain
//                 or plank door, a lean-to porch or shed;
//   shed        — woodshed, storehouse on stone piers, animal pen, a hut the size of a privy;
//   shopHouse   — familyHouse with a shop front: shop windows with propped flaps, open bays, a counter, an awning or a
//                 skirt eave, the shop's board and a banner.
// What the triangle budgets taught (near triangles, after export.mjs makes heavy parts lighter):
//   - a tiled roof is paid for by its length, ~250 a metre for each slope whatever its depth; the "low" ridge costs
//     52 for 4 m, the tiled one 676, swallow-tail ends 292 a pair-half, the plain ones 30 — so ordinary houses have the
//     low ridge and plain ends, the better ones the tiled ridge and swallow tails;
//   - a plastered (or bamboo, which the exporter makes plain) wall is 24 triangles at any length up to 6 m, a plank wall
//     66 a metre, so plain runs are laid as one module;
//   - windows are lattices (with their paper pane, they are closed panels anyway) set on the face of an unbroken wall
//     with a sill and a lintel of timber, not openings: ~190 instead of ~300-420 each; doors are real openings
//     (wall_door, its leaf standing open inward at 95-100°, out of the way), so every house can be walked into;
//   - interior floors are board ceilings (one board and battens, 120 for 4 × 4 m) where only their top or underside
//     shows; verandas and galleries get real planks;
//   - stilts cost 148 each: they stand 2.5-3 m apart, joists along X carry the floor;
//   - railings are the "cross" or "panel" sections (60 for 4 m), the barred ones only where they are seen close.
// Palettes (PAL): Gu Yue — dark timber, grey tile, pale lanterns (lantern_white); a few houses with blue glazed tile;
// the Chi family — red lacquer posts, beams, brackets, rails and doors, red lanterns; the Mo family — dark blue lacquer
// (wood_blue), blue glazed tile, blue lanterns, black lacquer doors and rails (the railing parts have no blue wood);
// the poor — old grey wood, mud, thatch.
// Two values outside a part's option list are used on purpose: lantern_hanging takes the lantern materials of
// lantern_post (lantern_white, lantern_blue) as its paper — only material names, which the kit builds as they are.
// Layers: everything that makes the house (plinth, floors, walls, windows, doors, stairs, rails, roofs, counters) is in
// the model's layers — solid and walkable; lanterns, couplets, boards, jars, logs, door curtains (so they do not block
// the doorway) are decoration (Detail: no shadow, not solid).
// Conventions as in the kit: metres, Y up, the entrance faces +Z and is reachable from the ground in front (stone or
// wooden steps where the floor is raised), the footprint is centred on the origin at the end (centre()), footings sink
// 0.25 m below the ground so a slope shows no gap. Deterministic: no randomness at all.

const D2R = Math.PI / 180;
const Z0 = [0, 0, 0];
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);

// ------------------------------------------------------------------ palettes
const GY = { post: "wood_dark", beam: "wood_dark", trim: "wood_dark", tile: "tile", lantern: "lantern_white", door: "black", door2: "black", win: "grid", winMat: "wood_dark", rail: "wood_dark", rails: "cross", bracket: "wood_dark", curtain: "cloth_blue", shutter: "wood_old" };
const PAL = {
  gy: GY,
  gyb: { ...GY, tile: "tile_blue", win: "vertical" },
  gyp: { ...GY, door: "plank", door2: "plank", win: "vertical" },
  chi: { ...GY, post: "wood_red", beam: "wood_red", trim: "wood_red", lantern: "lantern", door: "panel", door2: "panel", winMat: "wood_red", rail: "wood_red", rails: "bars", bracket: "wood_red", curtain: "cloth_red" },
  mo: { ...GY, post: "wood_blue", beam: "wood_blue", trim: "wood_blue", tile: "tile_blue", lantern: "lantern_blue", door: "black", door2: "black", win: "vertical", winMat: "wood_blue", rail: "wood_black", rails: "panel", bracket: "wood_blue" },
  poor: { ...GY, post: "wood_old", beam: "wood_old", trim: "wood_old", lantern: "paper", door: "plank", door2: "plank", win: "vertical", winMat: "wood_old", rail: "wood_old", rails: "cross", bracket: "wood_old", curtain: "burlap" },
};

// ------------------------------------------------------------------ small helpers
// L split into n equal cells no longer than max: [[centre offset from the middle, size], …]
function cells(L, max = 4) {
  const n = Math.max(1, Math.ceil(L / max - 1e-6)), s = L / n;
  return Array.from({ length: n }, (_, i) => [-L / 2 + (i + 0.5) * s, s]);
}
// n + 1 evenly spaced positions from a to b, no farther apart than step.
function spread(a, b, step) {
  const n = Math.max(1, Math.ceil((b - a) / step - 1e-6));
  return Array.from({ length: n + 1 }, (_, i) => a + (b - a) * i / n);
}
// How far below its ridge line a tiled slope (run, pitch, kick) is at t from it (the kit's slopeProfile).
function slopeDrop(run, pitch, kick, t) {
  const z1 = Math.max(0.01, run - kick), tp = Math.tan(pitch * D2R);
  return t <= z1 ? t * tp : z1 * tp + (t - z1) * Math.tan(pitch * 0.55 * D2R);
}

// The exporter measures a house over its parts, leaving out the décor that stands away from it (its centre more than
// 0.4 m outside the house itself) unless the recipe keeps its yard. The same here, then the house is moved so that
// box's middle is at the origin in X and Z. Returns the shift.
function centre(items, kit, THREE, yard = false) {
  const boxes = items.map((it) => {
    const r = it.r || Z0, s = it.s || [1, 1, 1];
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
  return [dx, dz];
}

// ------------------------------------------------------------------ walls
// One run of wall along X of a frame at (x, z, yaw), its tokens laid from −X to +X (metres):
//   w 1 / h 0.5 plain;  n 1 window 0.6 × 0.8;  N 2 wide window 1.4 × 0.9;  s 1 small high window 0.45;
//   a 1 window with a propped flap;  t 1 window with closed shutters;  (all of these on an unbroken wall)
//   d 1 door (a leaf standing open inside);  D 2 double door;  c 1 doorway with a curtain;
//   o 2 moon window;  x 2 shaped window (o.shape);  p 1 open bay;  P 1 open bay under a timber transom (from o.lintel);
//   S 2 shop window (a real opening 1.7 × 1.1 with its flap propped up and a sill board for the goods).
// Plain wall between openings is laid as one module (up to 6 m). o.sill is the window sill above the storey's floor.
const TOK = { w: 1, h: 0.5, n: 1, N: 2, s: 1, a: 1, t: 1, d: 1, D: 2, c: 1, o: 2, x: 2, p: 1, P: 1, S: 2 };
function tokens(spec) {
  const out = []; let u = 0;
  for (const k of spec.replace(/\s+/g, "")) { if (!(k in TOK)) throw new Error(`wall token "${k}" in "${spec}"`); out.push({ k, u, w: TOK[k] }); u += TOK[k]; }
  for (const t of out) t.c = t.u + t.w / 2 - u / 2;
  return { list: out, L: u };
}
// The x (along the run, from its middle) of the first door token of a spec, or 0.
function doorAt(spec) {
  const d = tokens(spec).list.find((t) => "dDcpPS".includes(t.k) && t.k !== "S");
  return d ? { x: d.c, k: d.k } : { x: 0, k: "" };
}
function facade(F, spec, o) {
  const { list, L } = tokens(spec);
  if (o.len !== undefined && Math.abs(L - o.len) > 1e-6) throw new Error(`wall "${spec}" is ${L} m long, not ${o.len}`);
  const pal = o.pal || PAL.gy, { y, h } = o, t = o.t ?? 0.18, type = o.type || "plaster", R = F.at(o.x || 0, 0, o.z || 0, o.yaw || 0), face = t / 2;
  const trim = pal.trim, sill0 = o.sill ?? 1.0, lintel = o.lintel ?? 2.3;
  const smooth = { plaster: "plaster", ochre: "plaster_ochre", grey: "plaster_grey", mud: "mud" }[type];
  let run = null;
  const flush = () => { if (!run) return; for (const [c, w] of cells(run.w, 6)) R.add("wall_" + type, { w, h, t }, [run.u - L / 2 + run.w / 2 + c, y, 0], Z0, "Стены"); run = null; };
  const sillBeam = (c, ww, ys) => R.add("beam", { L: ww + 0.16, h: 0.06, d: 0.1, mat: trim }, [c, ys - 0.06, face + 0.03], Z0, "Окна и двери");
  for (const { k, u, w, c } of list) {
    if ("whnNsat".includes(k)) {
      if (run) run.w += w; else run = { u, w };
      if (k === "n" || k === "N" || k === "s") {
        const [ww, wh, sill, pat] = k === "N" ? [1.4, 0.9, sill0 - 0.05, o.winWide || "grid"] : k === "s" ? [0.45, 0.45, sill0 + 0.3, "grid"] : [0.6, 0.8, sill0, o.win || pal.win];
        R.add("window_lattice", { w: ww, h: wh, pattern: pat, mat: pal.winMat }, [c, y + sill, face + 0.015], Z0, "Окна и двери");
        sillBeam(c, ww, y + sill);
        R.add("beam", { L: ww + 0.16, h: 0.07, d: 0.07, mat: trim }, [c, y + sill + wh, face + 0.02], Z0, "Окна и двери");
      } else if (k === "a") {
        R.add("beam", { L: 0.66, h: 0.56, d: 0.02, mat: "wood_black" }, [c, y + sill0 + 0.02, face + 0.005], Z0, "Окна и двери");
        R.add("window_awning", { w: 0.7, h: 0.6, open: 40, mat: trim }, [c, y + sill0, face + 0.02], Z0, "Окна и двери");
        sillBeam(c, 0.7, y + sill0);
      } else if (k === "t") {
        R.add("window_shutter", { w: 0.3, h: 0.7, mat: pal.shutter }, [c - 0.3, y + sill0, face + 0.02], Z0, "Окна и двери");
        R.add("window_shutter", { w: 0.3, h: 0.7, mat: pal.shutter }, [c + 0.3, y + sill0, face + 0.02], [0, 180, 0], "Окна и двери");
        sillBeam(c, 0.6, y + sill0);
      }
      continue;
    }
    flush();
    if (k === "d" || k === "c") {
      R.add("wall_door", { w, h, t, type, dw: 0.8, dh: 2.05 }, [c, y, 0], Z0, "Стены");
      if (k === "c") R.add("door_curtain", { w: 0.8, h: 1.9, color: pal.curtain }, [c, y + 0.14, face + 0.04], Z0, "Декор");
      else R.add("door_leaf", { w: 0.8, h: 1.9, style: o.door || pal.door }, [c - 0.4, y + 0.14, -face + 0.03], [0, 100, 0], "Окна и двери");
    } else if (k === "D") {
      R.add("wall_door", { w, h, t, type, dw: 1.4, dh: 2.2 }, [c, y, 0], Z0, "Стены");
      for (const s of [-1, 1]) R.add("door_leaf", { w: 0.7, h: 2.06, style: o.door2 || pal.door2 }, [c + s * 0.7, y + 0.14, -face + 0.03], [0, s < 0 ? 85 : 95, 0], "Окна и двери");
    } else if (k === "P") {
      for (const [dy, hh] of cells(h - lintel, 0.8)) R.add("beam", { L: w, h: hh, d: t, mat: pal.beam }, [c, y + lintel + (h - lintel) / 2 + dy - hh / 2, 0], Z0, "Стены");
    } else if (k === "o") {
      R.add("wall_window_round", { w, h, t, type: ["plaster", "ochre", "mud"].includes(type) ? type : "plaster", dia: 1.1, cy: sill0 + 0.5 }, [c, y, 0], Z0, "Стены");
    } else if (k === "x") {
      R.add("wall_window_shaped", { w, h, t, shape: o.shape || "hex", size: 0.9, cy: sill0 + 0.45, mat: smooth || "plaster", fill: "lattice" }, [c, y, 0], Z0, "Стены");
    } else if (k === "S") {
      R.add("wall_window", { w, h, t, type, ww: 1.7, wh: 1.1, sill: 0.85 }, [c, y, 0], Z0, "Стены");
      R.add("window_awning", { w: 1.7, h: 0.9, open: 62, mat: trim }, [c, y + 0.85 + 0.2, face + 0.02], Z0, "Окна и двери");
      R.add("beam", { L: 1.9, h: 0.05, d: 0.4, mat: "wood_raw" }, [c, y + 0.8, face + 0.1], Z0, "Каркас");
    }
  }
  flush();
  return L;
}

// Four walls of a W × D box (sides: front +Z, back −Z, left −X its tokens back to front, right +X front to back), posts
// at the corners and o.midPosts along front and back, beams round the top; returns the top of the beams.
// <side>Type overrides the wall type of one side.
function storey(F, o) {
  o = { t: 0.18, type: "plaster", ...o };
  const { W, D, y, h, t } = o, cx = o.cx || 0, cz = o.cz || 0, pal = o.pal || PAL.gy;
  const side = (name, len, x, z, yaw) => { if (o[name]) facade(F, o[name], { ...o, type: o[name + "Type"] || o.type, len, x, z, yaw }); };
  side("front", W, cx, cz + D / 2, 0); side("back", W, cx, cz - D / 2, 180);
  side("left", D, cx - W / 2, cz, -90); side("right", D, cx + W / 2, cz, 90);
  const pm = o.posts === undefined ? pal.post : o.posts;
  if (pm) {
    for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h, a: t + 0.06, mat: pm }, [cx + sx * W / 2, y, cz + sz * D / 2], Z0, "Каркас");
    for (const x of o.midPosts || []) for (const sz of [-1, 1]) F.add("post_square", { h, a: t + 0.04, mat: pm }, [cx + x, y, cz + sz * D / 2], Z0, "Каркас");
  }
  if (o.beams !== false) {
    const bm = o.beamMat || pal.beam;
    for (const sz of [-1, 1]) F.add("beam", { L: W + t + 0.06, h: 0.22, d: t + 0.04, mat: bm }, [cx, y + h, cz + sz * D / 2], Z0, "Каркас");
    for (const sx of [-1, 1]) F.add("beam", { L: (o.sideBeamD || D) - t - 0.06, h: 0.22, d: t + 0.04, mat: bm }, [cx + sx * W / 2, y + h, cz + (o.sideBeamCz || 0)], [0, 90, 0], "Каркас");
  }
  return y + h + 0.22;
}

// ------------------------------------------------------------------ ground, floors, steps, rails
// A plinth of blocks up to 4 × 4 m from y0 (below the ground) to h.
function slab(F, o) {
  const { W, D, h } = o, cx = o.cx || 0, cz = o.cz || 0, y0 = o.y0 ?? -0.25;
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D)) F.add("found_block", { w: sw, d: sd, h: h - y0, style: o.style || "ashlar" }, [cx + ux, y0, cz + uz], Z0, "Фундамент");
}
// Boards over W × D with their top at y: planks (seen from above), or a board ceiling (one board on battens, seen
// from below; its top is a floor too).
function boards(F, o) {
  const { W, D, y } = o, cx = o.cx || 0, cz = o.cz || 0;
  for (const [ux, sw] of cells(W)) for (const [uz, sd] of cells(D)) {
    if (o.ceiling) F.add("ceiling_panel", { w: Math.max(0.5, sw), d: Math.max(0.5, sd), mat: o.mat || "wood_raw" }, [cx + ux, y - 0.015, cz + uz], Z0, "Пол");
    else F.add("floor_plank", { w: sw, d: sd, t: 0.05, mat: o.mat || "wood_raw" }, [cx + ux, y - 0.05, cz + uz], Z0, "Пол");
  }
}
// Stone steps up to H whose top step ends at z (they come toward +Z); returns how far they reach in front of z.
function stoneSteps(F, o) {
  const n = Math.max(1, Math.round(o.H / 0.16)), tread = 0.3;
  F.add("stair_flight", { n, w: o.w || 1.2, tread, rise: o.H / n, kind: "stone" }, [o.x || 0, 0, o.z + n * tread], Z0, "Лестницы");
  return n * tread;
}
// A wooden stair from (x, y, z) of the frame (yaw) climbing H toward its −Z, one railed side (rails: ±1 = local ±X);
// returns its run.
function woodStair(F, o) {
  const n = Math.max(2, Math.round(o.H / 0.18)), rise = o.H / n, tread = o.tread || 0.26, w = o.w || 0.9, S = F.at(o.x, o.y || 0, o.z, o.yaw || 0);
  S.add("stair_flight", { n, w, tread, rise, kind: "wood" }, Z0, Z0, "Лестницы");
  for (const s of o.rails || [1]) S.add("rail_panel_slope", { run: n * tread, rise: o.H, h: 0.9, mat: o.mat || "wood_dark", style: "bars" }, [s * (w / 2 + 0.06), 0, 0], Z0, "Лестницы");
  return n * tread;
}
// Railing sections from (x0, z0) to (x1, z1) at height y (the artist's railRun, sections up to 4 m).
function rail(F, x0, z0, x1, z1, y, pal, style) {
  const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz);
  if (L < 0.3) return;
  const yaw = Math.atan2(-dz, dx) / D2R;
  for (const [u, w] of cells(L)) F.add("railing", { w, h: 0.9, mat: pal.rail, style: style || pal.rails }, [(x0 + x1) / 2 + dx / L * u, y, (z0 + z1) / 2 + dz / L * u], [0, yaw, 0], "Ограждения");
}
// A railing along X at z from x0 to x1 with a gap [g0, g1] left open (for a stair).
function railGap(F, x0, x1, z, y, pal, gap, style) {
  if (!gap) return rail(F, x0, z, x1, z, y, pal, style);
  rail(F, x0, z, gap[0], z, y, pal, style); rail(F, gap[1], z, x1, z, y, pal, style);
}
// Stilts on stone footings at every (x, z) and joists along X on each row; the joists' top at `top`.
function stilts(F, o) {
  const { xs, zs, top } = o, y0 = o.y0 ?? -0.05;
  for (const x of xs) for (const z of zs) F.add("stilt", { h: top - 0.16 - y0, r: o.r || 0.1 }, [x, y0, z], Z0, "Фундамент");
  const x0 = xs[0] - 0.15, x1 = xs[xs.length - 1] + 0.15;
  for (const z of zs) for (const [u, L] of cells(x1 - x0, 8)) {
    F.add("floor_joist", { L, h: 0.16, d: 0.14 }, [(x0 + x1) / 2 + u, top - 0.16, z], Z0, "Фундамент");
    if (o.ties) F.add("floor_joist", { L, h: 0.1, d: 0.08 }, [(x0 + x1) / 2 + u, y0 + (top - y0) * 0.45, z], Z0, "Фундамент");
  }
}

// ------------------------------------------------------------------ roofs
// The verge of a gable end at x: the kit's (dark board and grey roll) on grey tile; on coloured tile a low ridge in
// that tile laid along each part of the slope's profile (the verge part has no tile choice). sides: 1 front, -1 back.
function verge(F, x, ridgeY, cz, run, pitch, kick, mat, sides = [1, -1]) {
  if (mat === "tile") { for (const s of sides) F.add("roof_verge", { run, pitch, kick }, [x, ridgeY, cz], [0, s > 0 ? 0 : 180, 0], "Крыша"); return; }
  const z1 = Math.max(0.01, run - kick), y1 = -z1 * Math.tan(pitch * D2R), y2 = y1 - (run - z1) * Math.tan(pitch * 0.55 * D2R);
  const segs = [[0, 0, z1, y1, pitch]]; if (kick > 0.05) segs.push([z1, y1, run, y2, pitch * 0.55]);
  for (const [za, ya, zb, yb, a] of segs) {
    const L = Math.hypot(zb - za, yb - ya), lift = 0.07 / Math.cos(a * D2R);
    for (const [u, w] of cells(L, 4)) {
      const m = 0.5 + u / L, mz = za + (zb - za) * m, my = ya + (yb - ya) * m;
      for (const s of sides) F.add("roof_ridge", { w, style: "low", mat }, [x, ridgeY + my + lift, cz + s * mz], [s * a, -s * 90, 0], "Крыша");
    }
  }
}
// A gable roof over W × D (ridge along X): the tiled slopes bend into their kick right above the walls, as the artist's
// gableRoof does; style "thatch" for straw. o: pitch, over (eave overhang), side (overhang at the gables), kick, mat,
// ridge (low|tile|ornate), ends (simple|swallow|scroll|fish or false), endSize, gable (wall type or false), fish.
function roof(F, o) {
  const pal = o.pal || PAL.gy, thatch = o.style === "thatch", { W, D, top } = o, cx = o.cx || 0, cz = o.cz || 0;
  const pitch = o.pitch ?? (thatch ? 40 : 28), over = o.over ?? 0.6, side = o.side ?? 0.5, kick = thatch ? 0 : Math.min(o.kick ?? over, over), mat = o.mat || pal.tile;
  const tp = Math.tan(pitch * D2R), run = D / 2 + over, ridgeY = top + (thatch ? 0.04 : 0.05) + (D / 2) * tp, span = W + 2 * side;
  for (const [u, w] of cells(span, 4)) {
    for (const ry of [0, 180]) F.add(thatch ? "roof_thatch_slope" : "roof_tile_slope", thatch ? { w, run, pitch } : { w, run, pitch, kick, mat }, [cx + u, ridgeY, cz], [0, ry, 0], "Крыша");
    F.add("roof_ridge", thatch ? { w, style: "thatch" } : { w, style: o.ridge || "low", mat }, [cx + u, ridgeY, cz], Z0, "Крыша");
  }
  if (!thatch) for (const sx of [-1, 1]) {
    verge(F, cx + sx * span / 2, ridgeY, cz, run, pitch, kick, mat);
    if (o.ends !== false) F.add("roof_ridge_end", { size: o.endSize || 0.55, design: o.ends || "simple", mat }, [cx + sx * span / 2, ridgeY + (o.ridge && o.ridge !== "low" ? 0.1 : 0.03), cz], [0, sx > 0 ? 0 : 180, 0], "Крыша");
  }
  if (o.gable !== false) for (const sx of [-1, 1]) F.add("gable", { w: D, h: Math.max(0.3, (D / 2) * tp), t: o.t || 0.18, type: o.gable || "plaster", fish: o.fish || "no" }, [cx + sx * W / 2, top, cz], [0, sx * 90, 0], "Стены");
  return ridgeY;
}
// A single-pitch eave of length L along X of a frame: its upper edge at (y, z), falling toward +Z by run; a low ridge
// as flashing along the wall, verges at its ends. With posts: a beam under the eave and posts on it down to `floor`.
// Returns the beam's height and z (or the eave's height when there are no posts).
function pent(F, o) {
  const { L, y, z, run } = o, cx = o.cx || 0, pal = o.pal || PAL.gy, thatch = o.style === "thatch";
  const pitch = o.pitch ?? (thatch ? 30 : 22), kick = thatch ? 0 : Math.min(o.kick ?? 0.3, run - 0.2), mat = o.mat || pal.tile;
  for (const [u, w] of cells(L, 4)) {
    F.add(thatch ? "roof_thatch_slope" : "roof_tile_slope", thatch ? { w, run, pitch } : { w, run, pitch, kick, mat }, [cx + u, y, z], Z0, "Крыша");
    if (!thatch && o.cap !== false) F.add("roof_ridge", { w, style: "low", mat }, [cx + u, y, z - 0.05], Z0, "Крыша");
  }
  if (!thatch && o.verges !== false) for (const sx of [-1, 1]) verge(F, cx + sx * L / 2, y, z, run, pitch, kick, mat, [1]);
  const drop = (tt) => (thatch ? tt * Math.tan(pitch * D2R) : slopeDrop(run, pitch, kick, tt));
  if (o.posts) {
    const tp = o.postAt ?? run - Math.max(kick, 0.15) - 0.15, by = y - drop(tp) - (thatch ? 0.06 : 0.07) - 0.18;
    for (const [u, l] of cells(L - 0.2, 12)) F.add("beam", { L: l, h: 0.18, d: 0.16, mat: o.postMat || pal.beam }, [cx + u, by, z + tp], Z0, "Каркас");
    for (const x of o.posts) F.add("post_octagon", { h: by - o.floor, r: 0.09, mat: o.postMat || pal.post }, [cx + x, o.floor, z + tp], Z0, "Каркас");
    return { y: by, z: z + tp };
  }
  return { y: y - drop(run), z: z + run };
}

// ------------------------------------------------------------------ decoration (Detail)
const lantern = (F, x, y, z, color, size = 0.8) => F.add("lantern_hanging", { size, color }, [x, y, z], Z0, "Декор");
const jar = (F, x, y, z, h = 0.75) => F.add("water_jar", { h }, [x, y, z], Z0, "Декор");
function logs(F, x, y, z, n = 5, L = 1.4, yaw = 0) {
  const S = F.at(x, y, z, yaw);
  for (let i = 0; i < n; i++) { const row = i < 3 ? 0 : i < 5 ? 1 : 2, k = row === 0 ? i : row === 1 ? i - 3 : 0; S.add("log", { L, r: 0.09 }, [0, row * 0.16, -0.18 + k * 0.18 + row * 0.09], Z0, "Мебель и утварь"); }
}

// ------------------------------------------------------------------ family houses
// A clan family's house on a stone plinth. o: W, D (the ground storey), pal, plinth (its height), pad, padFront,
// low {h, t, type, front, back, left, right, sill, midPosts}, up (a second storey, same keys, and: over — how far its
// floor reaches in front of the ground storey; enclosed (default true: its walls come forward, a jetty) or false (an
// open gallery with a railing); posts — the overhang stands on posts from the plinth (an arcade) instead of brackets),
// skirt {run, pitch} — a tiled eave along the front between the storeys, roof {…roof()}, steps {w}, shed {side, w, d,
// open, front} — a lean-to at a side, yard {depth, wall, gate} — a walled yard in front, lanterns [x…], extra(F, ctx).
function familyHouse(houses, o) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.gy, { W, D } = o;
  const lo = { h: 2.7, t: 0.2, type: "plaster", ...o.low };
  const up = o.up ? { h: 2.4, t: 0.14, type: "plank", over: 0, ...o.up } : null;
  const ov = up ? up.over : 0, enclosed = up ? up.enclosed !== false : true, arcade = !!(up && up.posts && ov > 0);
  const ph = o.plinth ?? 0.35, pad = o.pad ?? 0.25, padF = o.padFront ?? 0.5;
  const reach = D / 2 + (arcade ? ov : 0) + padF;      // the plinth's front edge
  if (ph > 0) slab(F, { W: W + 2 * pad, D: reach + D / 2 + pad, cz: (reach - D / 2 - pad) / 2, h: ph });
  const y0 = ph, top0 = storey(F, { W, D, y: y0, pal, ...lo });
  let top = top0, rD = D, rcz = 0, y1 = top0, frontZ = D / 2 + lo.t / 2;
  if (up) {
    // the floor of the upper storey: a board ceiling (its battens seen from below) over the ground storey and the
    // overhang; an open gallery gets planks
    if (enclosed || ov === 0) boards(F, { W: W + lo.t, D: D + ov, cz: ov / 2, y: top0 + 0.03, ceiling: true });
    else { boards(F, { W: W + lo.t, D, y: top0 + 0.03, ceiling: true }); boards(F, { W: W + lo.t, D: ov, cz: D / 2 + ov / 2, y: top0 + 0.05 }); }
    y1 = top0 + 0.03;
    const xsF = [-W / 2, ...(lo.midPosts || []), W / 2];
    if (ov > 0 && arcade) {
      for (const x of xsF) F.add("post_square", { h: top0 - y0, a: 0.2, mat: pal.post }, [x, y0, D / 2 + ov - 0.1], Z0, "Каркас");
      F.add("beam", { L: W + 0.3, h: 0.22, d: 0.2, mat: pal.beam }, [0, top0 - 0.22, D / 2 + ov - 0.1], Z0, "Каркас");
      for (const sx of [-1, 1]) F.add("beam", { L: ov, h: 0.22, d: 0.18, mat: pal.beam }, [sx * W / 2, top0 - 0.22, D / 2 + ov / 2], [0, 90, 0], "Каркас");
    } else if (ov > 0) for (const x of xsF) F.add("bracket", { L: Math.max(0.3, ov - 0.05), mat: pal.bracket }, [x, top0 - 0.5, D / 2 + lo.t / 2], Z0, "Каркас");
    const D1 = enclosed ? D + ov : D, cz1 = enclosed ? ov / 2 : 0;
    top = storey(F, { W, D: D1, cz: cz1, y: y1, pal, ...up, sideBeamD: D + ov, sideBeamCz: ov / 2, midPosts: up.midPosts });
    if (!enclosed && ov > 0) {
      const zg = D / 2 + ov - 0.08;
      for (const x of xsF) F.add("post_square", { h: up.h, a: 0.16, mat: pal.post }, [x, y1, zg], Z0, "Каркас");
      F.add("beam", { L: W + 0.2, h: 0.22, d: 0.16, mat: pal.beam }, [0, y1 + up.h, zg], Z0, "Каркас");
      rail(F, -W / 2, zg, W / 2, zg, y1 + 0.02, pal, up.rails || pal.rails);
      for (const sx of [-1, 1]) rail(F, sx * W / 2, D / 2 + up.t / 2 + 0.05, sx * W / 2, zg, y1 + 0.02, pal, up.rails || pal.rails);
    }
    rD = D + ov; rcz = ov / 2; frontZ = cz1 + D1 / 2 + up.t / 2;
  }
  const r = { pitch: 28, over: 0.6, side: 0.5, gable: up ? up.type : lo.type, ...o.roof };
  // an open gallery under the roof: the slope passes above the wall behind it — timber fills the gap
  if (up && !enclosed && ov > 0) { const gh = ov * Math.tan(r.pitch * D2R); for (const [dy, hh] of cells(gh, 0.8)) F.add("beam", { L: W, h: hh, d: up.t, mat: pal.beam }, [0, top + gh / 2 + dy - hh / 2, D / 2], Z0, "Стены"); }
  const ridgeY = roof(F, { W, D: rD, cz: rcz, top, pal, t: up ? up.t : lo.t, ...r });
  let skirtEave = null;
  if (o.skirt && up && ov === 0) skirtEave = pent(F, { L: W + 0.7, y: y1 + (o.skirt.at ?? 0.55), z: frontZ, run: o.skirt.run || 0.95, pitch: o.skirt.pitch || 24, kick: 0.3, pal, mat: r.mat });
  // steps up to the plinth at the front door
  const door = doorAt(lo.front || "");
  if (ph > 0 && o.steps !== false) stoneSteps(F, { H: ph, z: reach, x: (o.steps && o.steps.x) ?? door.x, w: (o.steps && o.steps.w) || (door.k === "D" ? 1.9 : 1.3) });
  const ctx = { F, W, D, pal, y0, top0, top, y1, ridgeY, frontZ: D / 2 + lo.t / 2, door, reach, ov, skirtEave, r };
  if (o.shed) leanShed(F, ctx, o.shed);
  if (o.yard) frontYard(F, ctx, o.yard);
  // lanterns: under the overhang, the skirt eave or the roof's eave
  for (const x of o.lanterns || []) {
    if (ov > 0) lantern(F, x, top0 - 0.08, D / 2 + ov - 0.35, pal.lantern);
    else if (skirtEave) lantern(F, x, skirtEave.y - 0.05, frontZ + 0.55, pal.lantern);
    else lantern(F, x, top - 0.15, frontZ + 0.4, pal.lantern);
  }
  if (o.couplets !== false && door.k && "dD".includes(door.k)) F.add("door_couplets", { w: door.k === "D" ? 1.4 : 0.8, h: door.k === "D" ? 2.2 : 2.05 }, [door.x, y0, ctx.frontZ + 0.01], Z0, "Декор");
  if (o.extra) o.extra(F, ctx);
  return items;
}
// A lean-to shed against the right (side 1) or left (−1) wall of the ground storey, toward the back: plank walls on
// the ground, a tiled or thatched single slope falling away from the house.
function leanShed(F, ctx, s) {
  const { W, D, pal, y0, top0 } = ctx, sx = s.side || 1, sw = s.w || 2, sd = s.d || Math.min(3, D), zc = s.z ?? (-D / 2 + sd / 2);
  // the frame: its X along the house wall (toward −Z on the right side), its +Z away from the house
  const S = F.at(sx * W / 2, 0, zc, sx * 90);
  const ry = top0 - 0.35, run = sw + 0.35, pitch = s.pitch || 20, drop = slopeDrop(run, pitch, 0.25, sw);
  pent(S, { L: sd + 0.5, y: ry, z: 0.12, run, pitch, kick: 0.25, pal, mat: s.mat, style: s.style, verges: s.style !== "thatch" });
  const hOut = ry - drop - 0.12, t = 0.14;
  // walls: the outer one, the back and (unless open) the front, from just below the ground
  facade(S, s.outer || "w".repeat(sd), { x: 0, z: sw, yaw: 0, y: -0.1, h: hOut + 0.1, t, type: s.type || "plank", pal, len: sd });
  const sideH = hOut + 0.1;
  for (const [end, spec] of [[-1, s.back || "w".repeat(sw)], [1, s.open ? null : s.front || "w".repeat(sw)]]) {
    if (!spec) continue;
    // ends of the shed: along its local Z (from the house wall out)
    facade(S.at(-end * sd / 2 * sx * sx, 0, sw / 2, end * sx > 0 ? 90 : -90), spec, { x: 0, z: 0, yaw: 0, y: -0.1, h: sideH, t, type: s.type || "plank", pal, len: sw });
  }
  for (const e of [-1, 1]) F.add("post_square", { h: hOut + 0.1, a: 0.16, mat: pal.post }, ...[[S.o[0], 0, 0], Z0].slice(0, 0), [sx * (W / 2 + sw), -0.1, zc + e * sd / 2], Z0, "Каркас");
  if (s.open) for (let i = 0; i < 2; i++) logs(F, sx * (W / 2 + sw * 0.5), 0, zc + sd / 2 - 0.45 - i * 0.5, 5, sw - 0.3, 90);
}
// A walled yard in front of the house: plastered walls with a tiled coping, a gate in the middle of the front, a path.
function frontYard(F, ctx, y) {
  const { W, pal, reach } = ctx, dep = y.depth || 3, hw = (y.width || W + 0.5) / 2, z0 = reach + (y.gap ?? 0.9), z1 = z0 + dep, h = y.h || 1.7, t = 0.26, mat = y.mat || pal.tile;
  const wall = (x0, za, x1, zb) => {
    const dx = x1 - x0, dz = zb - za, L = Math.hypot(dx, dz), yaw = Math.atan2(-dz, dx) / D2R;
    for (const [u, w] of cells(L, 6)) F.add("wall_" + (y.type || "plaster"), { w, h, t }, [(x0 + x1) / 2 + dx / L * u, 0, (za + zb) / 2 + dz / L * u], [0, yaw, 0], "Ограждения");
    for (const [u, w] of cells(L, 4)) F.add("roof_ridge", { w, style: "low", mat }, [(x0 + x1) / 2 + dx / L * u, h + 0.05, (za + zb) / 2 + dz / L * u], [0, yaw, 0], "Ограждения");
  };
  const zb = ctx.D / 2 - 0.3;   // the side walls start at the house's front corners
  for (const sx of [-1, 1]) wall(sx * hw, zb, sx * hw, z1);
  const gw = 2;
  for (const sx of [-1, 1]) wall(sx * hw, z1, sx * gw / 2, z1);
  F.add("wall_door", { w: gw, h: 2.5, t: 0.3, type: y.type || "plaster", dw: 1.3, dh: 2.1 }, [0, 0, z1], Z0, "Ограждения");
  for (const s of [-1, 1]) F.add("door_leaf", { w: 0.65, h: 1.95, style: y.gate || pal.door2 }, [s * 0.65, 0.14, z1 - 0.12], [0, s < 0 ? 95 : 85, 0], "Ограждения");
  F.add("roof_ridge", { w: 2.6, style: "tile", mat }, [0, 2.55, z1], Z0, "Ограждения");
  for (const s of [-1, 1]) F.add("roof_ridge_end", { size: 0.4, design: "simple", mat }, [s * 1.3, 2.62, z1], [0, s > 0 ? 0 : 180, 0], "Ограждения");
  for (const [u, l] of cells(z1 - reach, 2)) F.add("path_module", { kind: "slab", w: 1.2 }, [0, 0, (reach + z1) / 2 + u], Z0, "Фундамент");
  if (y.extra) y.extra(F, { ...ctx, z0, z1, hw });
}

// ------------------------------------------------------------------ houses on stilts
// o: W, D (the rooms), veranda (its depth in front, under the roof), floor (height of the floor), xs/zs (stilt rows;
// by default 2.5-3 m apart), low {…storey}, up {…storey} (a second storey whose gallery is above the veranda), roof,
// stair {kind: "along" (in front of the veranda, climbing toward +X or −X: dir) | "front" (straight up to it), x, w},
// terrace {back, left, right} — the back half on a stone terrace whose face is at z = 0 and top at `floor`.
function stiltHouse(houses, o, meta) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.gy, { W, D } = o, V = o.veranda ?? 1.0, Hf = o.floor ?? 1.6;
  const lo = { h: 2.6, t: 0.14, type: "bamboo", ...o.low }, up = o.up ? { h: 2.4, t: 0.14, type: "bamboo", ...o.up } : null;
  const zf = D / 2 + V, ter = o.terrace;
  if (ter) slab(F, { W: W + (ter.left ?? 0.6) + (ter.right ?? 0.6), cx: ((ter.right ?? 0.6) - (ter.left ?? 0.6)) / 2, D: D / 2 + (ter.back ?? 1.5), cz: -(D / 2 + (ter.back ?? 1.5)) / 2, h: Hf, style: "rubble", y0: -0.25 });
  const xs = o.xs || spread(-W / 2 + 0.1, W / 2 - 0.1, o.bay || 2.8);
  const zs = o.zs || (ter ? [0.35, ...spread(0.35, zf - 0.15, 2.6).slice(1)] : spread(-D / 2 + 0.1, zf - 0.15, o.bay || 2.8));
  stilts(F, { xs, zs, top: Hf - 0.05, ties: o.ties ?? Hf > 1.4, r: o.stiltR });
  // the rooms' floor a board ceiling (seen from below between the stilts), the veranda real planks
  boards(F, { W: W + 0.1, D: D + 0.1, y: Hf, ceiling: true });
  boards(F, { W: W + 0.1, D: V - 0.05, cz: D / 2 + V / 2 + 0.025, y: Hf });
  const y1 = Hf + 0.02, xsP = o.vPosts || [-W / 2, W / 2], zp = zf - 0.1;
  const top0 = storey(F, { W, D, y: y1, pal, ...lo, sideBeamD: D + V, sideBeamCz: V / 2 });
  for (const x of xsP) F.add("post_square", { h: lo.h, a: 0.16, mat: pal.post }, [x, y1, zp], Z0, "Каркас");
  F.add("beam", { L: W + 0.2, h: 0.22, d: 0.16, mat: pal.beam }, [0, y1 + lo.h, zp], Z0, "Каркас");
  let top = top0, yR = y1, wallT = lo.t;
  const st = { kind: "along", dir: 1, w: 0.9, ...o.stair };
  const n = Math.max(2, Math.round(Hf / 0.18)), runS = n * 0.26;
  let gap = null;
  if (st.kind === "front") gap = [(st.x ?? W / 2 - 0.8) - st.w / 2 - 0.05, (st.x ?? W / 2 - 0.8) + st.w / 2 + 0.05];
  else { const xe = st.dir > 0 ? W / 2 - 0.12 : -W / 2 + 0.12; gap = st.dir > 0 ? [xe - 1.05, xe + 0.02] : [xe - 0.02, xe + 1.05]; }
  railGap(F, -W / 2, W / 2, zp, y1, pal, gap, o.vRails);
  for (const sx of [-1, 1]) rail(F, sx * W / 2, D / 2 + 0.12, sx * W / 2, zp, y1, pal, o.vRails);
  if (up) {
    // the gallery's floor above the veranda, the rooms' ceiling, the second storey and its own posts and rails
    boards(F, { W: W + lo.t, D, y: top0 + 0.03, ceiling: true });
    boards(F, { W: W + lo.t, D: V, cz: D / 2 + V / 2, y: top0 + 0.05 });
    const y2 = top0 + 0.03;
    top = storey(F, { W, D, y: y2, pal, ...up, sideBeamD: D + V, sideBeamCz: V / 2 });
    for (const x of xsP) F.add("post_square", { h: up.h, a: 0.16, mat: pal.post }, [x, y2, zp], Z0, "Каркас");
    F.add("beam", { L: W + 0.2, h: 0.22, d: 0.16, mat: pal.beam }, [0, y2 + up.h, zp], Z0, "Каркас");
    rail(F, -W / 2, zp, W / 2, zp, y2 + 0.02, pal, o.uRails);
    for (const sx of [-1, 1]) rail(F, sx * W / 2, D / 2 + 0.12, sx * W / 2, zp, y2 + 0.02, pal, o.uRails);
    yR = y2; wallT = up.t;
  }
  const r = { pitch: 28, over: 0.6, side: 0.5, gable: (up || lo).type === "bamboo" ? "plank" : (up || lo).type, ...o.roof };
  { const gh = V * Math.tan(r.pitch * D2R); for (const [dy, hh] of cells(gh, 0.8)) F.add("beam", { L: W, h: hh, d: wallT, mat: pal.beam }, [0, top + gh / 2 + dy - hh / 2, D / 2], Z0, "Стены"); }
  const ridgeY = roof(F, { W, D: D + V, cz: V / 2, top, pal, t: wallT, ...r });
  // the stair from the ground
  if (st.kind === "front") woodStair(F, { x: st.x ?? W / 2 - 0.8, z: zf + runS, H: Hf, w: st.w, mat: pal.rail === "wood_black" ? "wood_black" : pal.rail });
  else {
    const xe = st.dir > 0 ? W / 2 - 0.12 : -W / 2 + 0.12;
    woodStair(F, { x: xe - st.dir * runS, z: zf + st.w / 2 + 0.03, H: Hf, w: st.w, yaw: -st.dir * 90, rails: [st.dir], mat: pal.rail === "wood_black" ? "wood_black" : pal.rail });
  }
  const ctx = { F, W, D, V, Hf, pal, y1, top0, top, ridgeY, zf, zp };
  for (const x of o.lanterns || []) lantern(F, x, top - 0.12, zp + 0.25, pal.lantern);
  if (o.extra) o.extra(F, ctx);
  return items;
}

// ------------------------------------------------------------------ the poor
// A one-room house. o: W, D, type (mud|plank|bamboo|grey|plaster), t, h, plinth (rubble, low), posts (exposed timber
// at the corners and o.midPosts, or null), front/back/left/right, roof {…roof(), style thatch}, lean {at: "front" |
// "left" | "right", d, style, closed} — a lean-to porch or shed, extra(F, ctx).
function hut(houses, o) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.poor, { W, D } = o, ph = o.plinth ?? 0.2, h = o.h || 2.3, t = o.t ?? (o.type === "mud" ? 0.28 : 0.14);
  if (ph > 0) slab(F, { W: W + 0.3, D: D + 0.3, h: ph, style: "rubble" });
  const top = storey(F, { W, D, y: ph, h, t, type: o.type || "mud", pal, front: o.front, back: o.back, left: o.left, right: o.right, posts: o.posts === undefined ? pal.post : o.posts, midPosts: o.midPosts, beamMat: pal.beam, sill: o.sill ?? 1.05 });
  const r = { style: "thatch", over: 0.5, side: 0.4, gable: o.type || "mud", ...o.roof };
  const ridgeY = roof(F, { W, D, top, pal, t, ...r });
  const door = doorAt(o.front || "");
  if (ph > 0.12) F.add("stone_slab", { w: 1.0, d: 0.5, h: ph - 0.02 }, [door.x, 0, D / 2 + 0.15 + 0.3], Z0, "Лестницы");
  const ctx = { F, W, D, pal, y0: ph, top, ridgeY, door, frontZ: D / 2 + t / 2 };
  const L = o.lean;
  if (L) {
    const d = L.d || 1.6, style = L.style || r.style;
    if (L.at === "front") {
      const p = pent(F, { L: W + 0.4, y: top - 0.2, z: D / 2 + t / 2, run: d, pitch: style === "thatch" ? 26 : 18, kick: 0.2, style, pal, posts: L.posts || [-W / 2 + 0.1, W / 2 - 0.1], floor: ph > 0 ? 0 : 0, postMat: pal.post });
      ctx.porch = p;
    } else {
      const sx = L.at === "left" ? -1 : 1, S = F.at(sx * W / 2, 0, 0, sx * 90), run = d + 0.3, ry = top - 0.25, pitch = style === "thatch" ? 26 : 18;
      const e = pent(S, { L: Math.min(D, L.len || D) + 0.3, y: ry, z: t / 2, run, pitch, kick: 0.2, style, pal });
      const hOut = ry - (style === "thatch" ? d * Math.tan(pitch * D2R) : slopeDrop(run, pitch, 0.2, d)) - 0.1;
      for (const e2 of [-1, 1]) F.add("post_square", { h: hOut + 0.05, a: 0.14, mat: pal.post }, [sx * (W / 2 + d), -0.05, e2 * (Math.min(D, L.len || D) / 2 - 0.08)], Z0, "Каркас");
      F.add("beam", { L: Math.min(D, L.len || D), h: 0.14, d: 0.14, mat: pal.beam }, [sx * (W / 2 + d), hOut, 0], [0, 90, 0], "Каркас");
      if (L.closed) facade(S, "w".repeat(Math.round(Math.min(D, L.len || D))), { x: 0, z: d, yaw: 0, y: -0.05, h: hOut + 0.05, t: 0.12, type: "plank", pal, len: Math.round(Math.min(D, L.len || D)) });
      ctx.side = { sx, d };
    }
  }
  if (o.extra) o.extra(F, ctx);
  return items;
}

// ------------------------------------------------------------------ sheds
// A woodshed: back and side walls of planks, open front on two posts, a single slope falling to the front; logs.
function woodshed(houses, o) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.poor, { W, D } = o, hb = o.h || 2.2, pitch = o.pitch || 18;
  slab(F, { W: W + 0.1, D: D + 0.1, h: 0.08, y0: -0.2 });
  const run = D + 0.45, back = -D / 2;
  const e = pent(F, { L: W + 0.4, y: hb + 0.1, z: back - 0.12, run, pitch, kick: 0.2, style: o.style, pal, cap: false });
  const hF = hb + 0.1 - (o.style === "thatch" ? (D - 0.1) * Math.tan(pitch * D2R) : slopeDrop(run, pitch, 0.2, D - 0.1)) - 0.12;
  facade(F, "w".repeat(W), { x: 0, z: back, yaw: 180, y: 0.08, h: hb - 0.08, t: 0.12, type: "plank", pal, len: W });
  for (const sx of [-1, 1]) {
    F.add("wall_plank", { w: D - 0.1, h: hF - 0.08, t: 0.12 }, [sx * W / 2, 0.08, 0.05], [0, sx * 90, 0], "Стены");
    F.add("post_square", { h: hF, a: 0.16, mat: pal.post }, [sx * (W / 2 - 0.05), 0, D / 2 - 0.05], Z0, "Каркас");
  }
  F.add("beam", { L: W + 0.1, h: 0.14, d: 0.14, mat: pal.beam }, [0, hF - 0.02, D / 2 - 0.05], Z0, "Каркас");
  for (let i = 0; i < Math.floor(W / 1.1); i++) logs(F, -W / 2 + 0.7 + i * 1.1, 0.08, -0.1, 6, 1.0, 90);
  F.add("stump", { r: 0.25 }, [W / 2 - 0.4, 0, D / 2 + 0.25], Z0, "Мебель и утварь");
  return items;
}
// A storehouse on stone piers: a plank room with a door, two steps up, a small tiled gable roof.
function storehouse(houses, o) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.gyp, { W, D } = o, hf = o.floor || 0.55;
  for (const x of spread(-W / 2 + 0.2, W / 2 - 0.2, 2.6)) for (const z of [-D / 2 + 0.2, D / 2 - 0.2]) F.add("stone_block", { w: 0.45, h: hf - 0.16 + 0.15, d: 0.45, mat: "stone" }, [x, -0.15, z], Z0, "Фундамент");
  for (const z of [-D / 2 + 0.2, D / 2 - 0.2]) F.add("floor_joist", { L: W, h: 0.16, d: 0.16 }, [0, hf - 0.16, z], Z0, "Фундамент");
  boards(F, { W, D, y: hf + 0.03, ceiling: true, mat: "wood_old" });
  const top = storey(F, { W, D, y: hf + 0.03, h: o.h || 2.1, t: 0.14, type: o.type || "plank", pal, front: o.front, back: "w".repeat(W), left: o.left || "w".repeat(D), right: "w".repeat(D), posts: pal.post });
  roof(F, { W, D, top, pal, pitch: o.pitch || 30, over: 0.45, side: 0.35, kick: 0.2, ridge: "low", ends: "simple", endSize: 0.4, gable: "plank", t: 0.14, mat: o.mat });
  const door = doorAt(o.front);
  woodStair(F, { x: door.x, z: D / 2 + 0.08 + 0.26 * Math.max(2, Math.round((hf + 0.03) / 0.18)), H: hf + 0.03, w: 0.9, rails: [] });
  F.add("sack", { kind: "stack" }, [-W / 2 + 0.5, 0, D / 2 + 0.35], [0, 15, 0], "Мебель и утварь");
  return items;
}
// An animal pen: a rail fence round it with an opening in front, a thatched shelter at the back, a trough, animals.
function pen(houses, o) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.poor, { W, D } = o;
  const fence = (x0, z0, x1, z1) => { const dx = x1 - x0, dz = z1 - z0, L = Math.hypot(dx, dz), yaw = Math.atan2(-dz, dx) / D2R; for (const [u, w] of cells(L, 2)) F.add("fence_rail", { w: Math.max(1, w), mat: "wood_old" }, [(x0 + x1) / 2 + dx / L * u, 0, (z0 + z1) / 2 + dz / L * u], [0, yaw, 0], "Ограждения"); };
  const hx = W / 2, hz = D / 2;
  fence(-hx, hz, -0.6, hz); fence(0.6, hz, hx, hz); fence(hx, hz, hx, -hz); fence(-hx, -hz, -hx, hz);
  // the shelter: a plank back wall, two posts, a thatched slope
  const sd = o.shelter || 1.4;
  F.add("wall_plank", { w: W, h: 1.9, t: 0.12 }, [0, -0.05, -hz], Z0, "Стены");
  const e = pent(F, { L: W + 0.3, y: 2.0, z: -hz - 0.1, run: sd + 0.35, pitch: 24, style: o.style || "thatch", pal, posts: [-hx + 0.1, hx - 0.1], floor: -0.05 });
  F.add("trough", { L: 1.4, mat: "wood_old" }, [hx - 1.0, 0, hz - 0.45], Z0, "Каркас");
  for (const [i, a] of (o.animals || [["pig", 30], ["pig", -120]]).entries()) F.add("animal", { kind: a[0] }, [-hx + 0.9 + i * 1.3, 0, -0.2 + (i % 2) * 0.7], [0, a[1], 0], "Мебель и утварь");
  F.add("haystack", { h: 1.2 }, [-hx + 0.7, 0, -hz + 0.7], Z0, "Мебель и утварь");
  return items;
}
// A little hut: four plank walls, a curtain doorway, a thatched or tiled single slope.
function littleHut(houses, o) {
  const items = [], F = new houses.Frame(items), pal = o.pal || PAL.poor, { W, D } = o, hb = o.h || 2.2, pitch = 16;
  slab(F, { W: W + 0.1, D: D + 0.1, h: 0.1, y0: -0.2, style: "rubble" });
  const run = D + 0.5;
  pent(F, { L: W + 0.5, y: hb + 0.12, z: -D / 2 - 0.15, run, pitch, kick: 0.15, style: o.style, pal, cap: false });
  const hF = hb + 0.12 - (o.style === "thatch" ? (D + 0.15) * Math.tan(pitch * D2R) : slopeDrop(run, pitch, 0.15, D + 0.15)) - 0.08;
  facade(F, o.front || "c", { x: 0, z: D / 2, y: 0.1, h: hF - 0.1, t: 0.1, type: o.type || "plank", pal, len: W, door: "plank" });
  facade(F, "w".repeat(Math.round(W * 2) / 2 === W ? 0 : 0) || "h".repeat(W * 2), { x: 0, z: -D / 2, yaw: 180, y: 0.1, h: hb - 0.1, t: 0.1, type: o.type || "plank", pal, len: W });
  for (const sx of [-1, 1]) F.add("wall_" + (o.type || "plank"), { w: D - 0.1, h: hF - 0.1, t: 0.1 }, [sx * W / 2, 0.1, 0], [0, sx * 90, 0], "Стены");
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) F.add("post_square", { h: (sz > 0 ? hF : hb) - 0.1, a: 0.12, mat: pal.post }, [sx * W / 2, 0.1, sz * D / 2], Z0, "Каркас");
  if (o.extra) o.extra(F, { W, D, hF });
  return items;
}

// ------------------------------------------------------------------ shops
// familyHouse with a shop front: o.shop {counter: [x, length] behind the open bays, awning {w, x, color}, sign {x, w},
// flag {x, color}, goods: [[part, params, x, z]…] on the counter or the sills}.
function shopHouse(houses, o) {
  const s = o.shop || {};
  return familyHouse(houses, { ...o, couplets: false, extra: (F, c) => {
    const { y0, D, top0, ov, pal } = c, zf = D / 2 + 0.1;
    if (s.counter) {
      const [x, L] = s.counter, zc = D / 2 - 0.45;
      F.add("beam", { L, h: 0.85, d: 0.5, mat: "wood_dark" }, [x, y0, zc], Z0, "Каркас");
      F.add("beam", { L: L + 0.1, h: 0.05, d: 0.62, mat: "wood_raw" }, [x, y0 + 0.85, zc], Z0, "Каркас");
      F.add("plank", { L: L - 0.12, w: 0.5, t: 0.02, mat: s.counterFront || pal.trim }, [x, y0 + 0.45, zc + 0.25], [90, 0, 0], "Каркас");
    }
    if (s.awning) F.add("awning", { w: s.awning.w, d: s.awning.d || 1.3, h: Math.min(4, top0 - 0.35 - (ov ? 0.3 : 0)), color: s.awning.color || "cloth_blue" }, [s.awning.x || 0, 0, D / 2 + c.F.items.length * 0 + 0.12 + (ov || 0)], Z0, "Крыша");
    if (s.sign) F.add("sign_board", { w: s.sign.w || 1.3, glyphs: s.sign.glyphs || 2 }, [s.sign.x || 0, s.sign.y ?? (top0 - 0.4), zf + 0.02 + (s.sign.ov ? ov : 0)], Z0, "Декор");
    if (s.flag) F.add("shop_flag", { color: s.flag.color || "cloth_white", h: s.flag.h || 5 }, [s.flag.x, 0, s.flag.z ?? D / 2 + 0.7], Z0, "Декор");
    for (const [part, params, x, y, z, yaw] of s.goods || []) F.add(part, params, [x, y0 + y, z], [0, yaw || 0, 0], "Мебель и утварь");
    if (o.extra) o.extra(F, c);
  } });
}

// ------------------------------------------------------------------ recipes
const make = (fn) => (houses, kit, THREE) => { const items = fn(houses); centre(items, kit, THREE); return items; };
const fam = (o) => make((houses) => familyHouse(houses, o));
const stilt = (o) => make((houses) => stiltHouse(houses, o));
const poor = (o) => make((houses) => hut(houses, o));
const shopR = (o) => make((houses) => shopHouse(houses, o));
// a terrace house records where its terrace's face ended up (z, after centring) in its meta
const terrace = (o, meta) => (houses, kit, THREE) => { const items = stiltHouse(houses, o); const [, dz] = centre(items, kit, THREE); meta.edgeZ = +(-dz).toFixed(2); return items; };
const SEC = "Дома из набора";
const TERRACE_A = { drop: 2 }, TERRACE_B = { drop: 2 };

export const RECIPES = [
  // ---------------- Gu Yue families
  { id: "gy_fam_plain", label: "Дом в два этажа", kind: "Family", build: fam({
    W: 5, D: 4, pal: PAL.gy,
    low: { front: "nwdwn", back: "wawaw", left: "wnww", right: "wwnw" },
    up: { front: "nwnwn", back: "wwnww", left: "wwww", right: "wnww" },
    roof: { pitch: 28, over: 0.55, side: 0.45, ridge: "low", ends: "simple" }, lanterns: [-1.3],
    extra: (F, c) => { jar(F, 2.0, c.y0, c.frontZ + 0.35); F.add("f_bench", { w: 1.2 }, [-1.2, c.y0, c.frontZ + 0.3], Z0, "Мебель и утварь"); },
  }) },
  { id: "gy_fam_jetty", label: "Дом с выносом второго этажа", kind: "Family", build: fam({
    W: 6, D: 4, pal: PAL.gy,
    low: { front: "wnDnw", back: "wwaaww", left: "wnww", right: "wwnw", midPosts: [-1, 1] },
    up: { over: 0.5, front: "nwnnwn", back: "wnwwnw", left: "wwnwh", right: "hwnww" },
    roof: { pitch: 26, over: 0.65, side: 0.5, ridge: "tile", ends: "swallow", endSize: 0.5, gable: "plaster" }, lanterns: [-1.6, 1.6],
    extra: (F, c) => { F.add("flower_pot", { kind: "bamboo" }, [-2.6, c.y0, c.frontZ + 0.3], Z0, "Декор"); },
  }) },
  { id: "gy_fam_balcony", label: "Дом с галереей", kind: "Family", build: fam({
    W: 6, D: 5, pal: PAL.gyb,
    low: { front: "wndwnw", back: "wwnnww", left: "wwnww", right: "wnwww" },
    up: { over: 1.0, enclosed: false, type: "bamboo", front: "nwdwnw", back: "wwwnww", left: "wnwww", right: "wwwnw" },
    roof: { pitch: 27, over: 0.6, side: 0.5, ridge: "low", ends: "scroll", endSize: 0.5, gable: "plank" }, lanterns: [-1.2, 1.5],
    extra: (F, c) => { jar(F, -2.4, c.y0, c.frontZ + 0.35, 0.7); F.add("crate", { a: 0.45 }, [2.5, c.y0, c.frontZ + 0.3], [0, 20, 0], "Мебель и утварь"); },
  }) },
  { id: "gy_fam_skirt", label: "Дом с навесом между этажами", kind: "Family", build: fam({
    W: 7, D: 5, pal: PAL.gy,
    low: { type: "grey", front: "wnwDwnw", back: "wawwwaw", left: "wwnww", right: "wwnww", midPosts: [-1.5, 1.5] },
    up: { type: "timber", front: "wnwnwnw", back: "wwnwnww", left: "wwnww", right: "wwnww", midPosts: [-1.5, 1.5] },
    skirt: { run: 0.95, pitch: 24 },
    roof: { pitch: 29, over: 0.6, side: 0.5, ridge: "tile", ends: "simple", endSize: 0.6, gable: "plaster" }, lanterns: [-1.5, 1.5],
    extra: (F, c) => { F.add("f_bench", { w: 1.4 }, [2.4, c.y0, c.frontZ + 0.3], Z0, "Мебель и утварь"); },
  }) },
  { id: "gy_fam_stilt", label: "Бамбуковый дом на сваях", kind: "Family", build: stilt({
    W: 5, D: 4, veranda: 1.0, floor: 1.6, pal: PAL.gy,
    low: { type: "bamboo", front: "wndnw", back: "wwnww", left: "wnnw", right: "wnww" },
    roof: { pitch: 29, over: 0.6, side: 0.5, ridge: "low", ends: "simple" }, stair: { kind: "along", dir: 1 }, lanterns: [-1.2],
    extra: (F, c) => { F.add("back_basket", {}, [-1.4, 0, 0.6], [0, 30, 0], "Мебель и утварь"); jar(F, -1.9, c.y1, c.zp - 0.45, 0.6); },
  }) },
  { id: "gy_fam_stilt_plank", label: "Дощатый дом на высоких сваях", kind: "Family", build: stilt({
    W: 6, D: 4, veranda: 1.2, floor: 2.0, pal: PAL.gyb,
    low: { type: "plank", front: "wnwdnw", back: "wnwwnw", left: "wnww", right: "wwnw" },
    roof: { pitch: 31, over: 0.65, side: 0.55, ridge: "low", ends: "scroll", endSize: 0.5 }, stair: { kind: "front", x: -2.0, w: 0.9 }, lanterns: [0.9],
    extra: (F, c) => { logs(F, 1.2, 0, -0.6, 6, 1.6, 0); F.add("wood_bucket", {}, [-0.4, 0, 0.9], Z0, "Мебель и утварь"); },
  }) },
  { id: "gy_fam_yard", label: "Дом со двором", kind: "Family", build: fam({
    W: 6, D: 4, pal: PAL.gy, plinth: 0.45,
    low: { h: 2.9, front: "nwDwn", back: "wwnnww", left: "wnww", right: "wwnw" },
    up: { h: 1.5, type: "plaster", front: "wswwsw", back: "wwwwww", left: "wwww", right: "wwww", sill: 0.35, posts: undefined },
    roof: { pitch: 30, over: 0.6, side: 0.5, ridge: "tile", ends: "swallow", endSize: 0.5, gable: "plaster", fish: "yes" }, lanterns: [-1.5, 1.5],
    yard: { depth: 2.6, gap: 0.6, extra: (F, c) => { jar(F, -2.3, 0, c.z1 - 0.6, 0.8); F.add("flower_pot", { kind: "flower" }, [2.3, 0, c.z1 - 0.6], Z0, "Декор"); } },
  }) },
  { id: "gy_fam_wide", label: "Большой дом с аркадой", kind: "Family", build: fam({
    W: 8, D: 5, pal: PAL.gy,
    low: { front: "wnwnDnwnw".slice(0, 0) || "nwnwDwnwn".slice(0, 0) || "wnnwDwnnw".slice(0, 0) || "wnwDwnw" + "w", back: "wwawwaww", left: "wwnww", right: "wwnww", midPosts: [-2, 2] },
    up: { over: 1.3, posts: true, front: "wnwnnwnw", back: "wwnwwnww", left: "wwnwwh", right: "hwwnww", midPosts: [-2, 2] },
    roof: { pitch: 28, over: 0.7, side: 0.55, ridge: "tile", ends: "swallow", endSize: 0.6, gable: "plaster", fish: "yes" }, lanterns: [-2.2, 2.2],
    extra: (F, c) => { F.add("f_bench", { w: 1.6 }, [-2.6, c.y0, c.frontZ + 0.35], Z0, "Мебель и утварь"); jar(F, 2.9, c.y0, c.frontZ + 0.4); F.add("sign_board", { w: 1.4, glyphs: 3 }, [c.door.x, c.y0 + 2.55, c.frontZ + 0.01], Z0, "Декор"); },
  }) },
  // ---------------- the Chi family (red lacquer)
  { id: "gy_chi_house", label: "Дом семьи Чи", kind: "Family", clan: "GuYue", build: fam({
    W: 5, D: 4, pal: PAL.chi,
    low: { front: "nwdwn", back: "wawaw", left: "wnww", right: "wwnw" },
    up: { over: 0.4, front: "wnwnw", back: "wwnww", left: "wwnwh", right: "hwnww" },
    roof: { pitch: 27, over: 0.6, side: 0.45, ridge: "low", ends: "swallow", endSize: 0.45 }, lanterns: [-1.4, 1.4],
    extra: (F, c) => { F.add("flower_pot", { kind: "flower" }, [2.0, c.y0, c.frontZ + 0.3], Z0, "Декор"); },
  }) },
  { id: "gy_chi_gallery", label: "Дом Чи с красной галереей", kind: "Family", build: fam({
    W: 6, D: 5, pal: PAL.chi,
    low: { type: "ochre", front: "wnwdnw", back: "wwnnww", left: "wwnww", right: "wwnww" },
    up: { over: 1.1, enclosed: false, front: "nwdwnw".slice(0, 0) || "nwwdwn", back: "wnwwnw", left: "wwnww", right: "wwnww", rails: "bars" },
    roof: { pitch: 28, over: 0.65, side: 0.5, ridge: "tile", ends: "swallow", endSize: 0.55, gable: "ochre" }, lanterns: [-1.6, 1.6],
    extra: (F, c) => { jar(F, -2.5, c.y0, c.frontZ + 0.35); },
  }) },
  { id: "gy_chi_hall", label: "Большой дом семьи Чи", kind: "Family", build: fam({
    W: 8, D: 6, pal: PAL.chi,
    low: { front: "wnwDDwnw".slice(0, 0) || "wnwnDnwnw".slice(0, 0) || "wnnDnnww".slice(0, 0) || "wnwDwnwh".slice(0, 0) || "wnnwDwnnw".slice(1), back: "wwawwaww", left: "wwnnww", right: "wwnnww", midPosts: [-2, 2] },
    up: { front: "nwnwwnwn", back: "wwnwwnww", left: "wwnnww", right: "wwnnww", midPosts: [-2, 2] },
    skirt: { run: 1.0, pitch: 24 },
    shed: { side: 1, w: 2, d: 3, open: true },
    roof: { pitch: 28, over: 0.7, side: 0.55, ridge: "tile", ends: "swallow", endSize: 0.65, gable: "plaster", fish: "yes" }, lanterns: [-2.4, 2.4],
    extra: (F, c) => { F.add("sign_board", { w: 1.4, glyphs: 3 }, [c.door.x, c.y0 + 2.55, c.frontZ + 0.01], Z0, "Декор"); F.add("flower_pot", { kind: "bush" }, [-3.4, c.y0, c.frontZ + 0.35], Z0, "Декор"); },
  }) },
  // ---------------- the Mo family (dark blue lacquer, blue tile)
  { id: "gy_mo_house", label: "Дом семьи Мо", kind: "Family", build: fam({
    W: 5, D: 4, pal: PAL.mo,
    low: { type: "grey", front: "nwdwn", back: "wwaww", left: "wnww", right: "wwnw" },
    up: { front: "wnwnw", back: "wwnww", left: "wwww", right: "wwnw" },
    skirt: { run: 0.85, pitch: 24 },
    roof: { pitch: 29, over: 0.55, side: 0.45, ridge: "low", ends: "simple" }, lanterns: [-1.3, 1.3],
  }) },
  { id: "gy_mo_jetty", label: "Дом Мо с выносом", kind: "Family", build: fam({
    W: 6, D: 5, pal: PAL.mo,
    low: { type: "grey", front: "wnwdnw", back: "wwnnww", left: "wwnww", right: "wnwww" },
    up: { over: 0.5, front: "nwnnwn", back: "wnwwnw", left: "wwnwwh", right: "hwwnww" },
    roof: { pitch: 27, over: 0.65, side: 0.5, ridge: "tile", ends: "scroll", endSize: 0.55, gable: "plaster" }, lanterns: [-1.8, 1.8],
    extra: (F, c) => { F.add("f_bench", { w: 1.3, mat: "wood_dark" }, [1.6, c.y0, c.frontZ + 0.3], Z0, "Мебель и утварь"); },
  }) },
  { id: "gy_mo_court", label: "Дом Мо со двором и сараем", kind: "Family", build: fam({
    W: 6, D: 4, pal: PAL.mo,
    low: { h: 2.9, type: "grey", front: "wnDnw", back: "wwnnww", left: "wnww", right: "wwww" },
    roof: { pitch: 30, over: 0.6, side: 0.5, ridge: "tile", ends: "simple", endSize: 0.6, gable: "grey" }, lanterns: [-1.6, 1.6],
    shed: { side: 1, w: 1.8, d: 3, front: "hdh".replace("hdh", "w") === "w" ? null : null, open: false },
    yard: { depth: 2.4, gap: 0.6, type: "grey", extra: (F, c) => { jar(F, 2.4, 0, c.z1 - 0.6, 0.8); } },
  }) },
  // ---------------- the big bamboo houses (stand in for the old bamboo house)
  { id: "gy_bamboo_house", label: "Большой бамбуковый дом", kind: "Family", replaces: "guyue_bamboo_house", build: stilt({
    W: 8, D: 5, veranda: 1.3, floor: 1.8, pal: PAL.gy, bay: 2.7,
    low: { h: 2.7, type: "bamboo", front: "wnwnDnwn".slice(0, 0) || "wnnwDnnw".slice(0, 0) || "nwnDwnwn".slice(0, 0) || "wnwDwnww".slice(0, 0) || "nwnDnwnw".slice(0, 0) || "wnnDnnww".slice(0, 0) || "wnwDwnnw".slice(0, 0) || "nwnwDwnw".slice(0, 0) || "wnnwDnnw".slice(0, 8), back: "wnwwwwnw", left: "wnwnw", right: "wnwnw" },
    up: { h: 2.4, type: "bamboo", front: "nwnwwnwn", back: "wwnwwnww", left: "wwnww", right: "wwnww" },
    vPosts: [-4, -1.33, 1.33, 4], vRails: "bars", uRails: "bars",
    roof: { pitch: 30, over: 0.7, side: 0.6, ridge: "tile", ends: "swallow", endSize: 0.6, gable: "plank" },
    stair: { kind: "along", dir: -1, w: 1.0 }, lanterns: [-2.6, 2.6],
    extra: (F, c) => { F.add("back_basket", {}, [2.0, 0, 0.4], [0, -20, 0], "Мебель и утварь"); logs(F, -2.2, 0, -1.2, 6, 1.6, 0); F.add("flower_pot", { kind: "bamboo" }, [3.4, c.y1, c.zp - 0.4], Z0, "Декор"); },
  }) },
  { id: "gy_bamboo_house_b", label: "Бамбуковый дом с двумя галереями", kind: "Family", replaces: "guyue_bamboo_house", build: stilt({
    W: 8, D: 5, veranda: 1.2, floor: 2.0, pal: PAL.gyb, bay: 2.7,
    low: { h: 2.6, type: "bamboo", front: "nwwDwwnw".slice(0, 0) || "nwnDwnwn".slice(0, 0) || "wnwDwnwn".slice(0, 0) || "nnwDwwnn".slice(0, 8), back: "wwnwwnww", left: "wnnww", right: "wwnnw" },
    up: { h: 2.4, type: "plank", front: "wnnwwnnw", back: "wwnwwnww", left: "wwnww", right: "wwnww" },
    vPosts: [-4, 0, 4], vRails: "cross", uRails: "lattice",
    roof: { pitch: 33, over: 0.7, side: 0.6, ridge: "low", ends: "scroll", endSize: 0.6, gable: "plank" },
    stair: { kind: "front", x: 2.9, w: 1.0 }, lanterns: [-1.8, 1.8],
    extra: (F, c) => { F.add("wood_bucket", {}, [-2.4, 0, 1.4], Z0, "Мебель и утварь"); F.add("chicken", {}, [-1.4, 0, 0.6], [0, 40, 0], "Мебель и утварь"); },
  }) },
  // ---------------- houses on a terrace edge (the back on a 2 m stone terrace, the front on stilts)
  { id: "gy_terrace_house", label: "Дом на краю террасы", kind: "Family", meta: TERRACE_A, build: terrace({
    W: 6, D: 5, veranda: 1.1, floor: 2.0, pal: PAL.gy, terrace: { back: 1.6, left: 0.6, right: 0.6 },
    low: { type: "plank", front: "wnwdnw", back: "wwnnww", left: "wnwww", right: "wwwnw" },
    roof: { pitch: 28, over: 0.6, side: 0.5, ridge: "low", ends: "simple" }, stair: { kind: "along", dir: 1 }, lanterns: [-1.5],
    extra: (F, c) => { jar(F, -2.4, c.y1, c.zp - 0.45, 0.65); F.add("beehive", {}, [-2.6, 2.0, -3.3], [0, 90, 0], "Мебель и утварь"); },
  }, TERRACE_A) },
  { id: "gy_terrace_house_b", label: "Дом над обрывом с каменной лестницей", kind: "Family", meta: TERRACE_B, build: terrace({
    W: 6, D: 4, veranda: 1.2, floor: 2.0, pal: PAL.gyb, terrace: { back: 1.8, left: 0.5, right: 2.4 },
    low: { type: "plaster", t: 0.18, front: "nwdwnw".slice(0, 0) || "nwnwdw", back: "wwnnww", left: "wnww", right: "wwnw" },
    roof: { pitch: 30, over: 0.6, side: 0.5, ridge: "tile", ends: "simple", endSize: 0.55, gable: "plaster" }, stair: { kind: "along", dir: -1 }, lanterns: [1.2],
    extra: (F, c) => {
      // a stone stair beside the house from the lower ground up to the terrace's top
      const n = 12, tread = 0.3; F.add("stair_flight", { n, w: 1.2, tread, rise: 2.0 / n, kind: "stone" }, [c.W / 2 + 1.2, 0, n * tread], Z0, "Лестницы");
      F.add("garden_bed", { w: 1, d: 2, edge: "stones" }, [c.W / 2 + 1.2, 2.0, -1.6], Z0, "Огород");
      F.add("plant_greens", { L: 1.75 }, [c.W / 2 + 1.2, 2.07, -1.6], Z0, "Огород");
    },
  }, TERRACE_B) },
  // ---------------- the poor
  { id: "gy_poor_mud", label: "Глиняная хижина", kind: "Poor", build: poor({
    W: 4, D: 3, type: "mud", posts: null, front: "wcts".slice(0, 0) || "wctw", back: "wwsw", left: "wsw", right: "www",
    roof: { style: "thatch", pitch: 38, over: 0.5, side: 0.4 },
    extra: (F, c) => { jar(F, 1.5, 0, c.frontZ + 0.4, 0.65); logs(F, -2.45, 0, 0.2, 4, 1.3, 90); },
  }) },
  { id: "gy_poor_plank", label: "Дощатый дом с пристройкой", kind: "Poor", build: poor({
    W: 5, D: 4, type: "plank", front: "wtdtw".slice(0, 0) || "wtdwt", back: "wwsww", left: "wsww", right: "wwsw",
    roof: { style: "tile", pitch: 25, over: 0.45, side: 0.35, kick: 0.2, ridge: "low", ends: false, gable: "plank" },
    lean: { at: "right", d: 1.5, style: "tile", closed: false },
    extra: (F, c) => { logs(F, 3.25, 0, 0.0, 6, 1.2, 90); F.add("wood_bucket", {}, [-1.8, c.y0, c.frontZ + 0.35], Z0, "Мебель и утварь"); },
  }) },
  { id: "gy_poor_wattle", label: "Мазанка с крыльцом", kind: "Poor", build: poor({
    W: 4.5, D: 3.5, type: "mud", posts: "wood_old", front: "hwcwt".slice(0, 0) || "hwcwh".replace("hwcwh", "htcwh"), back: "hwwsh", left: "hswh", right: "hwwh",
    roof: { style: "thatch", pitch: 40, over: 0.45, side: 0.4 },
    lean: { at: "front", d: 1.4, style: "thatch" },
    extra: (F, c) => { F.add("f_bench", { w: 1.2 }, [-1.3, 0, c.frontZ + 0.35], Z0, "Мебель и утварь"); jar(F, 1.7, 0, c.frontZ + 0.4, 0.6); },
  }) },
  { id: "gy_poor_long", label: "Длинная хижина на две семьи", kind: "Poor", build: poor({
    W: 6, D: 3.5, type: "mud", posts: null, front: "wcsdsw".slice(0, 0) || "scwwds", back: "wswwsw", left: "hswh".slice(0, 0) || "hwsh".slice(0, 0) || "hswh", right: "hwwh",
    roof: { style: "thatch", pitch: 37, over: 0.5, side: 0.45 },
    extra: (F, c) => { jar(F, -1.6, 0, c.frontZ + 0.4, 0.6); F.add("sack", { kind: "full" }, [0.4, 0, c.frontZ + 0.35], [0, 20, 0], "Мебель и утварь"); F.add("wood_bucket", {}, [2.6, 0, c.frontZ + 0.35], Z0, "Мебель и утварь"); },
  }) },
  { id: "gy_poor_stilt", label: "Хижина на низких сваях", kind: "Poor", build: make((houses) => stiltHouse(houses, {
    W: 4, D: 3, veranda: 0.9, floor: 0.9, pal: PAL.poor, xs: [-1.9, 0, 1.9], zs: [-1.4, 0.4, 2.25], ties: false,
    low: { h: 2.3, type: "plank", front: "wdwt".slice(0, 0) || "tdww".slice(0, 0) || "wdtw", back: "wwsw", left: "wsw", right: "wsw" },
    roof: { style: "thatch", pitch: 36, over: 0.5, side: 0.4, gable: "plank" }, vRails: "cross", stair: { kind: "front", x: -1.25, w: 0.8 },
    extra: (F, c) => { F.add("back_basket", {}, [0.8, 0, 0.3], [0, 30, 0], "Мебель и утварь"); },
  })) },
  { id: "gy_poor_grey", label: "Бедный дом под старой черепицей", kind: "Poor", build: poor({
    W: 5, D: 4, type: "grey", posts: "wood_old", h: 2.4, front: "wadaw".slice(0, 0) || "awdwt", back: "wwaww", left: "wwaw", right: "wwww",
    roof: { style: "tile", pitch: 23, over: 0.45, side: 0.35, kick: 0.15, ridge: "low", ends: false, gable: "grey" },
    lean: { at: "left", d: 1.4, style: "thatch", closed: true, len: 3 },
    extra: (F, c) => { F.add("crate", { a: 0.45 }, [1.6, c.y0, c.frontZ + 0.3], [0, 10, 0], "Мебель и утварь"); jar(F, 2.2, c.y0, c.frontZ + 0.4, 0.6); },
  }) },
  { id: "gy_poor_bamboo", label: "Бамбуковая хижина", kind: "Poor", build: poor({
    W: 4.5, D: 3.5, type: "bamboo", posts: "wood_old", front: "hsch".slice(0, 0) || "hwcsh", back: "hwswh", left: "hswh", right: "hwsh",
    roof: { style: "thatch", pitch: 39, over: 0.5, side: 0.4, gable: "bamboo" },
    lean: { at: "front", d: 1.2, style: "thatch" },
    extra: (F, c) => { F.add("back_basket", {}, [1.6, 0, c.frontZ + 0.35], [0, -20, 0], "Мебель и утварь"); },
  }) },
  { id: "gy_poor_house", label: "Бедный дом", kind: "Poor", replaces: "prop_poor_house", build: poor({
    W: 5, D: 4, type: "mud", posts: "wood_old", h: 2.3, front: "wtcwt".slice(0, 0) || "wtcwt", back: "wwsww", left: "wsww", right: "wwsw",
    roof: { style: "thatch", pitch: 40, over: 0.5, side: 0.45 },
    lean: { at: "front", d: 1.5, style: "thatch" },
    extra: (F, c) => { logs(F, 2.85, 0, -0.6, 6, 1.3, 90); jar(F, -1.9, 0, c.frontZ + 0.45, 0.65); F.add("f_bench", { w: 1.2 }, [0.9, 0, c.frontZ + 0.4], Z0, "Мебель и утварь"); },
  }) },
  // ---------------- sheds
  { id: "gy_shed_wood", label: "Дровяник", kind: "Shed", build: make((houses) => woodshed(houses, { W: 3, D: 1.8, h: 2.1, style: "tile" })) },
  { id: "gy_shed_store", label: "Кладовая на камнях", kind: "Shed", build: make((houses) => storehouse(houses, { W: 3, D: 2, h: 2.0, front: "wdw" })) },
  { id: "gy_shed_pen", label: "Загон со скотом", kind: "Shed", build: make((houses) => pen(houses, { W: 4, D: 3, shelter: 1.3 })) },
  { id: "gy_shed_hut", label: "Сарайчик", kind: "Shed", build: make((houses) => littleHut(houses, { W: 1.5, D: 1.4, h: 2.2, style: "thatch", front: "c".length ? "hc".slice(0, 0) || "c" : "c" })) },
  // ---------------- shops
  { id: "gy_shop_window", label: "Лавка с откидными окнами", kind: "Shop", build: shopR({
    W: 5, D: 5, pal: PAL.gy,
    low: { front: "SdS".slice(0, 0) || "SdwS".slice(0, 0) || "ShdhS".slice(0, 0) || "SdS" + "", back: "wwaww", left: "wwnww", right: "wwwww" },
    up: { over: 0.5, front: "nwnwn", back: "wwnww", left: "wwnwwh", right: "hwwnww" },
    roof: { pitch: 27, over: 0.6, side: 0.45, ridge: "low", ends: "simple" }, lanterns: [-1.5, 1.5],
    shop: { sign: { w: 1.2, glyphs: 2 }, goods: [["vessel", { kind: "wine_jar" }, -1.6, 0.85, 2.7], ["food", { kind: "buns" }, 1.4, 0.85, 2.7], ["vessel", { kind: "gourd", mat: "ceramic" }, 1.9, 0.85, 2.7]] },
  }) },
  { id: "gy_shop_open", label: "Лавка с открытым прилавком", kind: "Shop", build: shopR({
    W: 6, D: 5, pal: PAL.gy,
    low: { front: "wPPPpw", back: "wwnnww", left: "wwnww", right: "wwnww", midPosts: [-1, 1] },
    up: { front: "nwnnwn", back: "wnwwnw", left: "wwnww", right: "wwnww", midPosts: [-1, 1] },
    roof: { pitch: 28, over: 0.6, side: 0.5, ridge: "tile", ends: "simple", endSize: 0.55, gable: "plank" }, steps: { x: 1.5, w: 1.2 },
    shop: { counter: [-1.0, 2.6], awning: { w: 4.6, d: 1.3, color: "cloth_blue" }, sign: { w: 1.6, glyphs: 3, y: 3.05 + 0.05 }, goods: [["sack", { kind: "open" }, -1.8, 0.9, 2.05, 20], ["basket", { kind: "fruit" }, -0.4, 0.9, 2.05], ["vessel", { kind: "big_jar" }, 2.2, 0, 1.6]] },
  }) },
  { id: "gy_shop_skirt", label: "Лавка с навесом и флагом", kind: "Shop", build: shopR({
    W: 6, D: 6, pal: PAL.gyb,
    low: { front: "SDS", back: "wwaaww", left: "wwnnww", right: "wwwnww", midPosts: [-1, 1] },
    up: { front: "nwnnwn", back: "wnwwnw", left: "wwnnww", right: "wwnnww" },
    skirt: { run: 1.0, pitch: 23 },
    roof: { pitch: 28, over: 0.6, side: 0.5, ridge: "low", ends: "scroll", endSize: 0.5, gable: "plank" }, lanterns: [-1.5, 1.5],
    shop: { sign: { w: 1.4, glyphs: 3, y: 2.95 }, flag: { x: -3.35, z: 3.55, color: "cloth_white", h: 5.2 }, goods: [["crate", { a: 0.4 }, -2.0, 0.85, 3.2], ["vessel", { kind: "meiping", mat: "porcelain_blue" }, 1.6, 0.85, 3.2], ["vessel", { kind: "vase_tall" }, 2.3, 0.85, 3.2]] },
  }) },
  { id: "gy_shop_arcade", label: "Лавка Чи под аркадой", kind: "Shop", build: shopR({
    W: 6, D: 5, pal: PAL.chi,
    low: { type: "ochre", front: "wSdSh".slice(0, 0) || "hSdSh".replace("hSdSh", "SwdS").slice(0, 0) || "SdwS".slice(0, 0) || "wSdS".slice(0, 0) || "SdSw", back: "wwnnww", left: "wwnww", right: "wwnww" },
    up: { over: 1.3, posts: true, front: "wnwwnw", back: "wwnnww", left: "wwnwwh", right: "hwwnww" },
    roof: { pitch: 27, over: 0.65, side: 0.5, ridge: "tile", ends: "swallow", endSize: 0.5, gable: "ochre" }, lanterns: [-1.8, 1.8],
    shop: { sign: { w: 1.3, glyphs: 2, y: 2.75 }, goods: [["food", { kind: "dumplings" }, -2.4, 0.85, 2.7], ["vessel", { kind: "teapot" }, -1.6, 0.85, 2.7], ["vessel", { kind: "wine_jar" }, 0.8, 0.85, 2.7], ["food", { kind: "fruit" }, 1.8, 0.85, 2.7]] },
  }) },
].map((r) => ({ section: SEC, clan: "GuYue", ...r }));
