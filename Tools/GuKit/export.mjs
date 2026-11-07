// Gu Yue kit houses → game-ready glTF models (2026-10-31).
//
// The artist's workshop (GLB/glb_masterskaya_source: kit/*.js on three.js) builds every part of the kit from its
// parameters, and kit/houses.js assembles whole houses from them — roofs included, whose slopes and ridges are not
// among the 1102 exported part files. This script runs that code in Node (three@0.160.0 installed beside it, the
// canvas it paints its patterns on stubbed out: the patterns come as PNG files from the part library) and writes each
// house the way the village's other models are kept (Models/Village/<id>/<id>.gltf, VillageModels.cs):
//   Near   — the house itself, one primitive per material (the pattern PNG × the material's colour);
//   Detail — what stands about it (lanterns, jars, pots, couplets): no shadow;
//   Far    — boxes of its big parts in its colours (one material, colours in the vertices), walls and slopes joined.
// It also writes kit_models.json beside them: each house's label, footprint, height and the footprint's middle, in the
// game's axes (glTFast imports a glTF mirrored in X, as it does the artist's other models).
//
// Usage: node Tools/GuKit/export.mjs <kit dir> <pattern PNG dir> <out dir> [--recipes file.mjs]… [--only regex] [--report file.json]
//   The recipes: recipes.mjs and every recipes/*.mjs beside this script (each exports RECIPES), or only the --recipes
//   given; --only keeps the ids matching; --report writes each model's numbers as JSON.
// A recipe: { id, label, kind, clan?, section?, yard?, far?: "boxes" | "nature", detail?: false, meta?: {…},
//   build(houses, kit, THREE) → items [{part, params, p, r, s, g}] } — houses: the artist's helpers and HOUSES; kit: kit.js.
import fs from "node:fs";
import path from "node:path";
import { pathToFileURL, fileURLToPath } from "node:url";

const argv = process.argv.slice(2);
const flags = { recipes: [], only: null, report: null };
const positional = [];
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === "--recipes") flags.recipes.push(path.resolve(argv[++i]));
  else if (argv[i] === "--only") flags.only = new RegExp(argv[++i]);
  else if (argv[i] === "--report") flags.report = path.resolve(argv[++i]);
  else positional.push(path.resolve(argv[i]));
}
const [kitDir, patternDir, outDir] = positional;
if (!kitDir || !patternDir || !outDir) throw new Error("usage: export.mjs <kit dir> <pattern PNG dir> <out dir> [--recipes file]… [--only regex] [--report file]");
const here = path.dirname(fileURLToPath(import.meta.url));
if (!flags.recipes.length) {
  flags.recipes.push(path.join(here, "recipes.mjs"));
  const dir = path.join(here, "recipes");
  if (fs.existsSync(dir)) for (const f of fs.readdirSync(dir).sort()) if (f.endsWith(".mjs")) flags.recipes.push(path.join(dir, f));
}

// The workshop paints its patterns on a canvas; here nothing is painted (the PNGs are the library's).
const stub = () => new Proxy(function () {}, { get: (t, k) => (k === Symbol.toPrimitive ? () => 0 : stub()), apply: () => stub(), set: () => true });
globalThis.document = { createElement: () => ({ width: 256, height: 256, getContext: () => stub() }) };

const kit = await import(pathToFileURL(path.join(kitDir, "kit.js")));
const THREE = await import(pathToFileURL(path.join(kitDir, "..", "node_modules", "three", "build", "three.module.js")));

// houses.js keeps its helpers (wallRun, box, gableRoof…) to itself: a copy beside it that also exports them, so the
// recipes here build with the artist's own hands. Written next to the original (GLB/ is not in git), never over it.
const housesSource = fs.readFileSync(path.join(kitDir, "houses.js"), "utf8");
const helpersPath = path.join(kitDir, `_gw_houses_${process.pid}.js`);
fs.writeFileSync(helpersPath, housesSource + "\nexport { Frame, wallRun, box, gableRoof, platform, floorGrid, stiltDeck, stairs, railRun, fenceRun };\n");
let houses;
try { houses = await import(pathToFileURL(helpersPath)); } finally { fs.rmSync(helpersPath, { force: true }); }

const RECIPES = [];
for (const file of flags.recipes) {
  const mod = await import(pathToFileURL(file));
  for (const r of mod.RECIPES || []) RECIPES.push({ ...r, source: path.basename(file) });
}
{
  const seen = new Map();
  for (const r of RECIPES) {
    if (seen.has(r.id)) throw new Error(`recipe id ${r.id} twice: ${seen.get(r.id)} and ${r.source}`);
    seen.set(r.id, r.source);
  }
}

const D2R = Math.PI / 180;

// ------------------------------------------------------------------ lighter roofs
// The tiled slope (516 triangles a metre) as the workshop makes it, its round tiles in 3 facets instead of 7 and its
// eave discs in 6 instead of 10 (≈280): the same silhouette and rows. A skirt slope (t0 > 0) stays the artist's.
{
  const original = kit.PARTS.roof_tile_slope;
  const lite = (b, p) => {
    const S = kit.segs(kit.slopeProfile(p.run, p.pitch, p.kick)), step = 0.25, n = Math.max(1, Math.round(p.w / step)), X = [-1, 0, 0], tm = p.mat;
    for (const s of S) {
      const mid = [0, (s.y0 + s.y1) / 2, (s.z0 + s.z1) / 2];
      b.basis("wood_dark", new THREE.BoxGeometry(p.w, s.L + 0.01, 0.05), [0, mid[1] - s.up[1] * 0.025, mid[2] - s.up[2] * 0.025], X, s.dir, s.up);
      for (let i = 0; i < n; i++) {
        const xc = -p.w / 2 + (i + 0.5) * (p.w / n), xe = -p.w / 2 + i * (p.w / n);
        b.basis(tm, new THREE.BoxGeometry(p.w / n - 0.03, s.L, 0.018), [xc, mid[1] + s.up[1] * 0.012, mid[2] + s.up[2] * 0.012], X, s.dir, s.up);
        b.basis(tm, kit.halfCyl(0.055, s.L, 3), [xe, mid[1] + s.up[1] * 0.025, mid[2] + s.up[2] * 0.025], X, s.dir, s.up);
      }
    }
    const last = S[S.length - 1];
    for (let i = 0; i < n; i++) {
      const xe = -p.w / 2 + i * (p.w / n);
      b.basis(tm, new THREE.CylinderGeometry(0.06, 0.06, 0.025, 6), [xe, last.y1 + last.up[1] * 0.025, last.z1 + last.up[2] * 0.025], X, last.dir, last.up);
    }
    b.box("wood_dark", p.w, 0.12, 0.04, 0, last.y1 - 0.08, last.z1 - 0.03);
  };
  const build = original.build;
  original.build = (b, p) => (p.t0 > 0 ? build(b, p) : lite(b, p));
}

// ------------------------------------------------------------------ lighter stand-ins
// Walls the workshop lays brick by brick or culm by culm (a metre of brick wall ~2400 triangles, of bamboo ~1000):
// for the game the same wall plain, in that material's colour and pattern. Rubble footings likewise as dressed stone.
const PLAIN = { brick: "brick", brick_red: "brick_red", bamboo: "bamboo", wattle: "mud" };
function lighter(part, params) {
  const remap = {};
  let p = { ...params };
  let id = part;
  const wall = /^wall_(brick|brick_red|bamboo|wattle)$/.exec(part);
  if (wall) {
    id = wall[1] === "wattle" ? "wall_mud" : "wall_plaster";
    if (wall[1] !== "wattle") remap.plaster = PLAIN[wall[1]];
  } else if (typeof p.type === "string" && PLAIN[p.type] && kit.PARTS[part]?.params.type) {
    if (p.type === "wattle") p.type = "mud";
    else { remap.plaster = PLAIN[p.type]; p.type = "plaster"; }
  }
  if (part === "found_block" && p.style === "rubble") { p.style = "ashlar"; remap.ashlar = "stone"; }
  if (part === "baluster" && (p.style === undefined || p.style === "turned")) p.style = "square";
  return { id, params: p, remap };
}

// ------------------------------------------------------------------ colours
// The game's colour floor (QingMaoMaterials.cs, ColourFloor): no surface darker than 0x48 at its brightest channel
// (sRGB), or it reads as a hole in the sun. The workshop's woods are far below it (dark wood #3c2b20).
const FLOOR = 0x48 / 255;
const toLinear = (c) => (c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4));
function srgbOf(name) {
  const c = kit.material(name).color.clone();
  const hex = c.getHex(THREE.SRGBColorSpace);
  return [(hex >> 16) / 255, ((hex >> 8) & 255) / 255, (hex & 255) / 255];
}
function liftedLinear(name) {
  const s = srgbOf(name);
  const top = Math.max(...s);
  const k = top > 0.001 && top < FLOOR ? FLOOR / top : 1;
  return s.map((v) => toLinear(Math.min(1, v * k)));
}

// ------------------------------------------------------------------ layers
// What stands about the house (no shadow, Detail) and what makes the house (Near; its collider too).
const DETAIL = new Set(["Декор", "Мебель и утварь", "Природа", "Огород", "Транспорт", "Оружие"]);

// ------------------------------------------------------------------ geometry
class Bucket {
  constructor() { this.byMat = new Map(); }
  add(mat, pos, nor, uv) {
    if (!this.byMat.has(mat)) this.byMat.set(mat, { pos: [], nor: [], uv: [] });
    const b = this.byMat.get(mat);
    for (const v of pos) b.pos.push(v);
    for (const v of nor) b.nor.push(v);
    for (const v of uv) b.uv.push(v);
  }
  // Shared corners: the workshop's meshes have three corners for every triangle.
  welded() {
    const out = [];
    for (const [mat, b] of this.byMat) {
      const map = new Map(), pos = [], nor = [], uv = [], idx = [];
      const n = b.pos.length / 3;
      for (let i = 0; i < n; i++) {
        const key = [b.pos[i * 3], b.pos[i * 3 + 1], b.pos[i * 3 + 2]].map((v) => Math.round(v * 2000)).join(",") + "|" +
          [b.nor[i * 3], b.nor[i * 3 + 1], b.nor[i * 3 + 2]].map((v) => Math.round(v * 100)).join(",") + "|" +
          [b.uv[i * 2], b.uv[i * 2 + 1]].map((v) => Math.round(v * 1000)).join(",");
        let k = map.get(key);
        if (k === undefined) {
          k = pos.length / 3; map.set(key, k);
          pos.push(b.pos[i * 3], b.pos[i * 3 + 1], b.pos[i * 3 + 2]);
          nor.push(b.nor[i * 3], b.nor[i * 3 + 1], b.nor[i * 3 + 2]);
          uv.push(b.uv[i * 2], b.uv[i * 2 + 1]);
        }
        idx.push(k);
      }
      out.push({ mat, pos, nor, uv, idx });
    }
    return out;
  }
}

function transformInto(bucket, P, M, remap) {
  const N = new THREE.Matrix3().getNormalMatrix(M), v = new THREE.Vector3();
  for (const m of P.meshes) {
    const g = m.geometry, p = g.attributes.position, n = g.attributes.normal, t = g.attributes.uv;
    const pos = new Array(p.count * 3), nor = new Array(p.count * 3), uv = new Array(p.count * 2);
    for (let i = 0; i < p.count; i++) {
      v.fromBufferAttribute(p, i).applyMatrix4(M); pos[i * 3] = v.x; pos[i * 3 + 1] = v.y; pos[i * 3 + 2] = v.z;
      v.fromBufferAttribute(n, i).applyMatrix3(N).normalize(); nor[i * 3] = v.x; nor[i * 3 + 1] = v.y; nor[i * 3 + 2] = v.z;
      uv[i * 2] = t.getX(i); uv[i * 2 + 1] = t.getY(i);
    }
    // A mirrored transform turns the triangles inside out: wind them back.
    if (M.determinant() < 0) for (let a = 1; a < p.count; a += 3) {
      const b = a + 1;
      for (let k = 0; k < 3; k++) { [pos[a * 3 + k], pos[b * 3 + k]] = [pos[b * 3 + k], pos[a * 3 + k]]; [nor[a * 3 + k], nor[b * 3 + k]] = [nor[b * 3 + k], nor[a * 3 + k]]; }
      for (let k = 0; k < 2; k++) [uv[a * 2 + k], uv[b * 2 + k]] = [uv[b * 2 + k], uv[a * 2 + k]];
    }
    bucket.add(remap[m.mat] || m.mat, pos, nor, uv);
  }
}

// ------------------------------------------------------------------ far boxes
// Every big part of the house as a box (a roof slope as a slab from its ridge to its eave), in the colour of most of
// it; boxes in a row (a wall's metres, a slope's modules, a footing's blocks) joined into one.
const FAR_LAYERS = new Set(["Фундамент", "Стены", "Крыша", "Каркас", "Лестницы", "Ограждения", "Пол"]);
function farBoxes(items, nature) {
  const boxes = [];
  for (const it of items) {
    if (!FAR_LAYERS.has(it.layer) && !(nature && DETAIL.has(it.layer))) continue;
    const bb = it.P.bbox, size = new THREE.Vector3(), mid = new THREE.Vector3();
    bb.getSize(size); bb.getCenter(mid);
    if (Math.max(size.x, size.y, size.z) < 0.6 && it.layer !== "Фундамент") continue;
    if (it.layer === "Каркас" && Math.min(size.x, size.z) < 0.3 && size.y > 1) continue; // posts: hidden in walls far off
    let tris = 0, mat = null;
    for (const m of it.P.meshes) { const t = m.geometry.attributes.position.count; if (t > tris) { tris = t; mat = it.remap[m.mat] || m.mat; } }
    const colour = liftedLinear(mat);
    let local = new THREE.Matrix4(), half;
    if (/_slope$/.test(it.id)) {
      // ridge at the back top, eave at the front bottom
      const dz = size.z, dy = size.y, L = Math.hypot(dz, dy), a = Math.atan2(dy, dz);
      local.compose(mid, new THREE.Quaternion().setFromEuler(new THREE.Euler(a, 0, 0)), new THREE.Vector3(1, 1, 1));
      half = [size.x / 2, 0.08, L / 2];
    } else {
      local.makeTranslation(mid.x, mid.y, mid.z);
      half = [size.x / 2, size.y / 2, size.z / 2];
    }
    const M = it.M.clone().multiply(local);
    const o = new THREE.Vector3(), q = new THREE.Quaternion(), s = new THREE.Vector3();
    M.decompose(o, q, s);
    const ax = [new THREE.Vector3(1, 0, 0), new THREE.Vector3(0, 1, 0), new THREE.Vector3(0, 0, 1)].map((v) => v.applyQuaternion(q));
    boxes.push({ o, ax, h: [half[0] * Math.abs(s.x), half[1] * Math.abs(s.y), half[2] * Math.abs(s.z)], colour });
  }
  // join boxes that continue one another along their own X
  const key = (b) => [...b.ax.flatMap((v) => [v.x, v.y, v.z]).map((v) => Math.round(v * 100)), Math.round(b.h[1] * 100), Math.round(b.h[2] * 100),
    Math.round(b.o.dot(b.ax[1]) * 50), Math.round(b.o.dot(b.ax[2]) * 50), ...b.colour.map((c) => Math.round(c * 200))].join(",");
  const groups = new Map();
  for (const b of boxes) { const k = key(b); if (!groups.has(k)) groups.set(k, []); groups.get(k).push(b); }
  const joined = [];
  for (const list of groups.values()) {
    list.sort((a, b) => a.o.dot(a.ax[0]) - b.o.dot(b.ax[0]));
    let cur = null;
    for (const b of list) {
      const lo = b.o.dot(b.ax[0]) - b.h[0], hi = b.o.dot(b.ax[0]) + b.h[0];
      if (cur && lo <= cur.hi + 0.05) { cur.hi = Math.max(cur.hi, hi); continue; }
      if (cur) joined.push(cur);
      cur = { ...b, lo, hi };
    }
    if (cur) joined.push(cur);
  }
  const pos = [], nor = [], col = [], idx = [];
  for (const b of joined) {
    const along = (b.lo + b.hi) / 2, o = b.o.clone().add(b.ax[0].clone().multiplyScalar(along - b.o.dot(b.ax[0])));
    const h = [(b.hi - b.lo) / 2, b.h[1], b.h[2]];
    for (let f = 0; f < 6; f++) {
      const a = f >> 1, sgn = f & 1 ? -1 : 1, u = (a + 1) % 3, w = (a + 2) % 3;
      const n = b.ax[a].clone().multiplyScalar(sgn), base = pos.length / 3;
      for (const [su, sw] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) {
        const p = o.clone().add(b.ax[a].clone().multiplyScalar(sgn * h[a])).add(b.ax[u].clone().multiplyScalar(su * h[u])).add(b.ax[w].clone().multiplyScalar(sw * h[w]));
        pos.push(p.x, p.y, p.z); nor.push(n.x, n.y, n.z); col.push(...b.colour, 1);
      }
      // outward winding (counter-clockwise seen from outside, as glTF wants)
      if (sgn > 0) idx.push(base, base + 1, base + 2, base, base + 2, base + 3);
      else idx.push(base, base + 2, base + 1, base, base + 3, base + 2);
    }
  }
  return { pos, nor, col, idx, boxes: joined.length };
}

// ------------------------------------------------------------------ glTF
function writeGltf(dir, id, near, detail, far) {
  const json = {
    asset: { version: "2.0", generator: "Gu World kit export" }, scene: 0, scenes: [{ nodes: [] }], nodes: [], meshes: [], materials: [],
    textures: [], images: [], samplers: [{ magFilter: 9729, minFilter: 9987, wrapS: 10497, wrapT: 10497 }], accessors: [], bufferViews: [], buffers: [],
  };
  const chunks = []; let offset = 0;
  const view = (typed, target) => {
    const pad = (4 - (offset % 4)) % 4; if (pad) { chunks.push(Buffer.alloc(pad)); offset += pad; }
    const buf = Buffer.from(typed.buffer, typed.byteOffset, typed.byteLength);
    json.bufferViews.push({ buffer: 0, byteOffset: offset, byteLength: buf.byteLength, ...(target ? { target } : {}) });
    chunks.push(buf); offset += buf.byteLength; return json.bufferViews.length - 1;
  };
  const acc = (arr, type, comp = 5126, minmax = false) => {
    const typed = comp === 5126 ? new Float32Array(arr) : comp === 5123 ? new Uint16Array(arr) : new Uint32Array(arr);
    const n = { SCALAR: 1, VEC2: 2, VEC3: 3, VEC4: 4 }[type];
    const a = { bufferView: view(typed, comp === 5126 ? 34962 : 34963), componentType: comp, count: arr.length / n, type };
    if (minmax) {
      a.min = [Infinity, Infinity, Infinity]; a.max = [-Infinity, -Infinity, -Infinity];
      for (let i = 0; i < arr.length; i++) { a.min[i % 3] = Math.min(a.min[i % 3], arr[i]); a.max[i % 3] = Math.max(a.max[i % 3], arr[i]); }
    }
    json.accessors.push(a); return json.accessors.length - 1;
  };
  const images = new Map(), mats = new Map();
  const material = (name) => {
    if (mats.has(name)) return mats.get(name);
    const m = kit.material(name), pattern = m.map.name;
    if (!images.has(pattern)) {
      json.images.push({ name: pattern, uri: `../kit_textures/${pattern}.png`, mimeType: "image/png" });
      json.textures.push({ sampler: 0, source: json.images.length - 1 });
      images.set(pattern, json.textures.length - 1);
    }
    const md = { name, pbrMetallicRoughness: { baseColorFactor: [...liftedLinear(name), 1], baseColorTexture: { index: images.get(pattern) }, metallicFactor: m.metalness, roughnessFactor: m.roughness } };
    if (m.emissiveIntensity && m.emissive && (m.emissive.r || m.emissive.g || m.emissive.b)) {
      const e = m.emissive.clone().multiplyScalar(m.emissiveIntensity);
      md.emissiveFactor = [Math.min(1, e.r), Math.min(1, e.g), Math.min(1, e.b)];
    }
    if (m.transparent) { md.alphaMode = "BLEND"; md.pbrMetallicRoughness.baseColorFactor[3] = m.opacity; }
    json.materials.push(md); mats.set(name, json.materials.length - 1); return json.materials.length - 1;
  };
  const meshNode = (name, parts) => {
    if (!parts.length) return;
    const prims = parts.map((p) => {
      const big = p.pos.length / 3 > 65535;
      return { attributes: { POSITION: acc(p.pos, "VEC3", 5126, true), NORMAL: acc(p.nor, "VEC3"), TEXCOORD_0: acc(p.uv, "VEC2") }, indices: acc(p.idx, "SCALAR", big ? 5125 : 5123), material: material(p.mat), mode: 4 };
    });
    json.meshes.push({ name, primitives: prims });
    json.nodes.push({ name, mesh: json.meshes.length - 1 });
    json.scenes[0].nodes.push(json.nodes.length - 1);
  };
  meshNode("Near", near);
  meshNode("Detail", detail);
  json.materials.push({ name: "Far", doubleSided: true, pbrMetallicRoughness: { metallicFactor: 0, roughnessFactor: 0.9 } });
  const farMat = json.materials.length - 1;
  json.meshes.push({ name: "Far", primitives: [{ attributes: { POSITION: acc(far.pos, "VEC3", 5126, true), NORMAL: acc(far.nor, "VEC3"), COLOR_0: acc(far.col, "VEC4") }, indices: acc(far.idx, "SCALAR", 5123), material: farMat, mode: 4 }] });
  json.nodes.push({ name: "Far", mesh: json.meshes.length - 1 });
  json.scenes[0].nodes.push(json.nodes.length - 1);
  const bin = Buffer.concat(chunks);
  json.buffers.push({ byteLength: bin.byteLength, uri: `${id}.bin` });
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, `${id}.gltf`), JSON.stringify(json));
  fs.writeFileSync(path.join(dir, `${id}.bin`), bin);
  return [...images.keys()];
}

// ------------------------------------------------------------------ houses
const report = [], manifest = [], numbers = [], patterns = new Set();
for (const recipe of RECIPES) {
  if (flags.only && !flags.only.test(recipe.id)) continue;
  let raw;
  try { raw = recipe.build(houses, kit, THREE); } catch (e) { console.error(`${recipe.id} (${recipe.source}): ${e.stack || e}`); process.exitCode = 1; continue; }
  const items = [];
  for (const it of raw) {
    const { id, params, remap } = lighter(it.part, it.params || {});
    let P;
    try { P = kit.buildPart(id, params); } catch (e) { console.error(`${recipe.id}: part ${id} ${JSON.stringify(params)}: ${e.message}`); process.exitCode = 1; continue; }
    const r = it.r || [0, 0, 0], sc = it.s || [1, 1, 1];
    const M = new THREE.Matrix4().compose(new THREE.Vector3(...(it.p || [0, 0, 0])), new THREE.Quaternion().setFromEuler(new THREE.Euler(r[0] * D2R, r[1] * D2R, r[2] * D2R)), new THREE.Vector3(...(Array.isArray(sc) ? sc : [sc, sc, sc])));
    items.push({ id, P, M, remap, layer: recipe.detail === false ? "Стены" : it.g || "Декор" });
  }
  // the house's own footprint: everything that is not about it
  const shellBox = new THREE.Box3();
  for (const it of items) if (!DETAIL.has(it.layer)) shellBox.union(it.P.bbox.clone().applyMatrix4(it.M));
  const keep = items.filter((it) => {
    if (!DETAIL.has(it.layer) || recipe.yard) return true;
    const c = new THREE.Vector3(); it.P.bbox.clone().applyMatrix4(it.M).getCenter(c);
    return c.x > shellBox.min.x - 0.4 && c.x < shellBox.max.x + 0.4 && c.z > shellBox.min.z - 0.4 && c.z < shellBox.max.z + 0.4;
  });
  const near = new Bucket(), detail = new Bucket();
  const all = new THREE.Box3();
  for (const it of keep) {
    transformInto(DETAIL.has(it.layer) ? detail : near, it.P, it.M, it.remap);
    all.union(it.P.bbox.clone().applyMatrix4(it.M));
  }
  const nearParts = near.welded(), detailParts = detail.welded(), far = farBoxes(keep, recipe.far === "nature");
  const id = "kit_" + recipe.id;
  for (const p of writeGltf(path.join(outDir, id), id, nearParts, detailParts, far)) patterns.add(p);
  const tris = (parts) => parts.reduce((n, p) => n + p.idx.length / 3, 0);
  const verts = (parts) => parts.reduce((n, p) => n + p.pos.length / 3, 0);
  // in the game's axes: glTFast mirrors X
  const meta = recipe.meta || {};
  manifest.push({
    id, label: recipe.label, kind: recipe.kind, clan: recipe.clan || "GuYue", section: recipe.section || "Дома из набора", replaces: recipe.replaces || "",
    width: +(all.max.x - all.min.x).toFixed(2), depth: +(all.max.z - all.min.z).toFixed(2), height: +all.max.y.toFixed(2),
    middleX: +(-(all.min.x + all.max.x) / 2).toFixed(2), middleZ: +((all.min.z + all.max.z) / 2).toFixed(2),
    rise: meta.rise ?? 0, run: meta.run ?? 0, profile: (meta.profile || []).flat(),
  });
  numbers.push({ id, source: recipe.source, kind: recipe.kind, replaces: recipe.replaces || "", near: tris(nearParts), detail: tris(detailParts), far: far.idx.length / 3,
    nearMaterials: nearParts.length, width: +(all.max.x - all.min.x).toFixed(2), depth: +(all.max.z - all.min.z).toFixed(2), height: +all.max.y.toFixed(2),
    minY: +all.min.y.toFixed(2), middleX: +((all.min.x + all.max.x) / 2).toFixed(2), middleZ: +((all.min.z + all.max.z) / 2).toFixed(2) });
  report.push(`${id}: ${keep.length}/${items.length} parts, near ${tris(nearParts)} tris ${verts(nearParts)} verts ${nearParts.length} mats, detail ${tris(detailParts)} tris ${detailParts.length} mats, far ${far.idx.length / 3} tris (${far.boxes} boxes), ${(all.max.x - all.min.x).toFixed(1)}×${(all.max.z - all.min.z).toFixed(1)}×${all.max.y.toFixed(1)} m`);
}

// the patterns, once, beside the houses
fs.mkdirSync(path.join(outDir, "kit_textures"), { recursive: true });
for (const p of patterns) fs.copyFileSync(path.join(patternDir, `${p}.png`), path.join(outDir, "kit_textures", `${p}.png`));
fs.writeFileSync(path.join(outDir, "kit_models.json"), JSON.stringify({ models: manifest }, null, 1));
if (flags.report) fs.writeFileSync(flags.report, JSON.stringify(numbers, null, 1));
console.log(report.join("\n"));
console.log(`${manifest.length} houses, ${patterns.size} patterns → ${outDir}`);
