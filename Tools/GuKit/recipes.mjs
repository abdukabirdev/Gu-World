// The kit houses the game takes (export.mjs): the artist's eight from kit/houses.js as they are, and kin of theirs in
// other walls, woods, tiles and cloths. `kind` is what the village plans use them as; `yard`: keep what the artist set
// about the house (logs, haystacks, baskets) — else only what stands on or against it.

// Swap values throughout a house: a wall type (and its "wall_<type>" parts), a wood, a tile, a cloth.
function swap(items, map) {
  return items.map((it) => {
    const params = {};
    for (const [k, v] of Object.entries(it.params)) params[k] = typeof v === "string" && map[v] ? map[v] : v;
    const wall = /^wall_(.+)$/.exec(it.part);
    const part = wall && map[wall[1]] ? "wall_" + map[wall[1]] : it.part;
    return { ...it, part, params };
  });
}

const artist = (id) => (houses) => houses.HOUSES.find((h) => h.id === id).build();
const kin = (id, map) => (houses) => swap(artist(id)(houses), map);

export const RECIPES = [
  { id: "hut_mud", label: "Глинобитная хижина", kind: "Poor", build: artist("hut_mud") },
  { id: "hut_mud_ochre", label: "Хижина в охре", kind: "Poor", build: kin("hut_mud", { mud: "ochre", wood_old: "wood_raw" }) },
  { id: "stilt_bamboo", label: "Бамбуковый дом на сваях", kind: "Family", build: artist("stilt_bamboo") },
  { id: "stilt_plank", label: "Дощатый дом на сваях", kind: "Family", build: kin("stilt_bamboo", { bamboo: "plank", tile: "tile_blue" }) },
  { id: "artisan", label: "Дом ремесленника", kind: "Family", build: artist("artisan") },
  { id: "artisan_ochre", label: "Дом ремесленника в охре", kind: "Family", build: kin("artisan", { plaster: "ochre", wood_red: "wood_dark", tile: "tile_blue" }) },
  { id: "two_storey", label: "Двухэтажный дом", kind: "Family", build: artist("two_storey") },
  { id: "two_storey_stone", label: "Двухэтажный дом на камне", kind: "Family", build: kin("two_storey", { brick: "stone", plank: "timber" }) },
  { id: "l_kitchen", label: "Г-образный дом с кухней", kind: "Family", build: artist("l_kitchen") },
  { id: "l_kitchen_grey", label: "Г-образный дом, серый", kind: "Family", build: kin("l_kitchen", { ochre: "grey", brick: "stone", wood_red: "wood_dark" }) },
  { id: "courtyard", label: "Дом с двором", kind: "Family", build: artist("courtyard") },
  { id: "courtyard_blue", label: "Дом с двором, синий лак", kind: "Family", build: kin("courtyard", { wood_red: "wood_blue", tile: "tile_blue" }) },
  { id: "slope_terrace", label: "Дом на краю террасы", kind: "Family", build: artist("slope_terrace") },
  { id: "shop_house", label: "Дом с лавкой", kind: "Shop", build: artist("shop_house") },
  { id: "shop_house_plaster", label: "Дом с лавкой, белёный", kind: "Shop", build: kin("shop_house", { brick: "plaster", cloth_blue: "cloth_red" }) },
];
