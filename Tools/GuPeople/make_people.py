"""
GU World's people: a young man and a young woman (and their grown-up selves) made
in Blender from MakeHuman's base mesh (MPFB 2, CC0 assets), with a game skeleton
(MakeHuman's "game_engine" rig, 53 bones) and everything they wear as separate
rigged meshes on that skeleton: the clothes, the hair, the beards.

Run (Blender 5.2, MPFB 2 installed, MakeHuman's system assets loaded into MPFB):

    blender --background --python make_people.py -- <out_dir> <log> <mpfb_data> [<blend_dir>]

<mpfb_data> is a copy of MPFB's own data folder at a short path (Windows cannot
open MPFB's longest target names under the Microsoft Store's package folder).

It writes <out_dir>/<kit>/<kit>.gltf + .bin for each kit (male_teen, male_adult,
female_teen, female_adult) and their textures in <out_dir>/textures. In each kit:

    Rig              the skeleton (pelvis, spine_01..03, neck_01, head, clavicle,
                     upperarm, lowerarm, hand, fingers, thigh, calf, foot, ball)
    Body             the skin (material "Skin")
    Eyes, Eyebrows, Eyelashes
    Hair_Short, Hair_Parted, Hair_Buns, Hair_Bob, Hair_Ponytail, Hair_Long
    Beard_Stubble, Beard_Mustache, Beard_Goatee, Beard_Full    (the men's)
    Cloth_Under      always worn (a loincloth and, for the women, a chest band)
    Cloth_Trousers, Cloth_Wraps, Cloth_Shoes
    Cloth_Robe       the long hanfu (materials Robe, Inner, Trim)
    Cloth_Tunic      the short one, to the thigh or the knee (the same materials)
    Cloth_Belt       the sash (Belt)
    Cloth_Mark       the clan's crescent on the back (Mark)

The game tints the cloth by material name (Robe, Trim, Belt, Trousers, Wraps,
Shoes) and the hair and beards by the person's hair colour.
"""

import bpy
import bmesh
import math
import os
import sys
import traceback

import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = os.path.abspath(ARGS[0])
LOG_PATH = os.path.abspath(ARGS[1])
MPFB_DATA = os.path.abspath(ARGS[2])
BLEND_DIR = os.path.abspath(ARGS[3]) if len(ARGS) > 3 else None
ONLY = os.environ.get("GU_PEOPLE_ONLY", "")

_log = open(LOG_PATH, "w", encoding="utf-8")


def log(*parts):
    _log.write(" ".join(str(p) for p in parts) + "\n")
    _log.flush()


KITS = {
    "male_teen": dict(gender=1.0, age=0.32, muscle=0.5, weight=0.45, proportions=0.85, height=0.55, cupsize=0.5, firmness=0.5),
    "male_adult": dict(gender=1.0, age=0.5, muscle=0.55, weight=0.5, proportions=0.85, height=0.55, cupsize=0.5, firmness=0.5),
    "female_teen": dict(gender=0.0, age=0.32, muscle=0.45, weight=0.45, proportions=0.85, height=0.5, cupsize=0.38, firmness=0.6),
    "female_adult": dict(gender=0.0, age=0.5, muscle=0.45, weight=0.48, proportions=0.85, height=0.5, cupsize=0.5, firmness=0.55),
}

# The game's hairstyles (Body/Looks.cs HairStyle) and the MakeHuman hair each is made from.
HAIRS = {
    "Short": "short02",
    "Parted": "short03",
    "Buns": "short01",
    "Bob": "bob02",
    "Ponytail": "ponytail01",
    "Long": "long01",
}

SIDES = ("l", "r")
FINGERS = [f"{f}_0{k}_{s}" for f in ("thumb", "index", "middle", "ring", "pinky") for k in (1, 2, 3) for s in SIDES]
HAND = set(FINGERS) | {"hand_l", "hand_r"}
ARM = HAND | {f"{b}_{s}" for b in ("upperarm", "lowerarm") for s in SIDES}
LEG = {f"{b}_{s}" for b in ("thigh", "calf", "foot", "ball") for s in SIDES}
HEAD = {"head", "neck_01"}


def clear_scene():
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):
        for block in list(collection):
            collection.remove(block)


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


class Kit:
    """One person being made: the MakeHuman body, its rig and what is measured on it."""

    def __init__(self, name, macro):
        from bl_ext.user_default.mpfb.services.humanservice import HumanService
        from bl_ext.user_default.mpfb.services.locationservice import LocationService
        from bl_ext.user_default.mpfb.services.targetservice import TargetService

        self.name = name
        self.male = macro["gender"] > 0.5
        info = TargetService.get_default_macro_info_dict()
        info.update(macro)
        info["race"] = {"asian": 1.0, "caucasian": 0.0, "african": 0.0}
        self.body = HumanService.create_human(mask_helpers=True, detailed_helpers=True, extra_vertex_groups=True,
                                              feet_on_ground=True, scale=0.1, macro_detail_dict=info)
        self.rig = HumanService.add_builtin_rig(self.body, "game_engine", import_weights=True)
        self.bones = [b.name for b in self.rig.data.bones]
        data = LocationService.get_user_data()
        self.assets = {}

        def add(kind, rel):
            obj = HumanService.add_mhclo_asset(os.path.join(data, rel), self.body, asset_type=kind, subdiv_levels=0, material_type="MAKESKIN")
            return obj

        self.assets["Eyes"] = add("Eyes", "eyes/low-poly/low-poly.mhclo")
        self.assets["Eyebrows"] = add("Eyebrows", "eyebrows/eyebrow001/eyebrow001.mhclo" if self.male else "eyebrows/eyebrow010/eyebrow010.mhclo")
        self.assets["Eyelashes"] = add("Eyelashes", "eyelashes/eyelashes01/eyelashes01.mhclo")
        self.hair_sources = {}
        for style, source in HAIRS.items():
            self.assets["Hair_" + style] = add("Hair", f"hair/{source}/{source}.mhclo")
            self.hair_sources["Hair_" + style] = source

        TargetService.bake_targets(self.body)
        self.data_root = data
        self.measure()

    # --- what the body is like -------------------------------------------------------------

    def measure(self):
        me = self.body.data
        names = {g.index: g.name for g in self.body.vertex_groups}
        self.weights = [{names[g.group]: g.weight for g in v.groups if g.weight > 0} for v in me.vertices]
        self.co = [v.co.copy() for v in me.vertices]
        bone_set = set(self.bones)
        self.bone_set = bone_set

        def at(name, tail=False):
            b = self.rig.data.bones[name]
            return self.rig.matrix_world @ (b.tail_local if tail else b.head_local)

        self.at = at
        self.neck_z = at("neck_01").z
        self.knee_z = at("calf_l").z
        self.ankle_z = at("foot_l").z
        self.hip_z = at("thigh_l").z
        skirt = [i for i, w in enumerate(self.weights) if w.get("helper-skirt", 0) > 0.5]
        self.waist_z = max(self.co[i].z for i in skirt) - 0.025
        self.height = max(self.co[i].z for i, w in enumerate(self.weights) if w.get("body", 0) > 0.5)
        self.scale = self.height / 1.6
        lips = [self.co[i] for i, w in enumerate(self.weights) if w.get("lips", 0) > 0.5]
        self.mouth = sum(lips, Vector()) / len(lips)
        nipples = [self.co[i] for i, w in enumerate(self.weights) if w.get("nipple", 0) > 0.5]
        self.chest_z = sum((v.z for v in nipples), 0) / max(1, len(nipples)) if nipples else at("spine_03").z + 0.05
        ears = [self.co[i] for i, w in enumerate(self.weights) if w.get("ears", 0) > 0.5]
        self.ear_front_y = min(v.y for v in ears)
        self.ear_inner_x = min(abs(v.x) for v in ears)
        self.ear_mid_z = sum((v.z for v in ears), 0) / len(ears)
        log(f"  {self.name}: height {self.height:.3f} neck {self.neck_z:.3f} waist {self.waist_z:.3f} hips {self.hip_z:.3f} "
            f"knee {self.knee_z:.3f} ankle {self.ankle_z:.3f} chest {self.chest_z:.3f} mouth {tuple(round(c, 3) for c in self.mouth)}")

    def in_group(self, i, group, at_least=0.5):
        return self.weights[i].get(group, 0) > at_least

    def bone_share(self, i, bones):
        return sum(self.weights[i].get(b, 0) for b in bones)

    def face_centre(self, poly):
        return sum((self.co[v] for v in poly.vertices), Vector()) / len(poly.vertices)

    # --- pieces cut from the base mesh --------------------------------------------------------

    def extract(self, name, keep):
        """A new object of the base mesh's faces `keep(poly, centre)` says yes to, its bone weights kept."""
        polys = self.body.data.polygons
        bm = bmesh.new()
        bm.from_mesh(self.body.data)
        bm.faces.ensure_lookup_table()
        kill = [f for f in bm.faces if not keep(polys[f.index], self.face_centre(polys[f.index]))]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        obj = link(bpy.data.objects.new(name, mesh))
        for group in self.body.vertex_groups:
            obj.vertex_groups.new(name=group.name)
        log(f"  {name}: {len(mesh.vertices)} vertices cut from the base mesh")
        return obj

    def all_in(self, poly, group):
        return all(self.in_group(v, group) for v in poly.vertices)

    def mean_share(self, poly, bones):
        return sum(self.bone_share(v, bones) for v in poly.vertices) / len(poly.vertices)

    def tights(self, poly):
        return self.all_in(poly, "helper-tights")


# --- mesh helpers -------------------------------------------------------------------------------


def bvh(obj):
    obj.data.update()
    bpy.context.view_layer.update()
    return BVHTree.FromObject(obj, bpy.context.evaluated_depsgraph_get())


def offset(obj, distance):
    mesh = obj.data
    mesh.update()
    normals = [v.normal.copy() for v in mesh.vertices]
    for v, n in zip(mesh.vertices, normals):
        v.co += n * distance


def subdivide(obj, levels=1):
    mod = obj.modifiers.new("Subdivision", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=mod.name)


def bisect(obj, height, keep_above=True):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, height), plane_no=(0, 0, 1 if keep_above else -1), clear_inner=True)
    bm.to_mesh(obj.data)
    bm.free()


def join(objects, name):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    objects[0].name = name
    objects[0].data.name = name
    return objects[0]


def weights_of(obj):
    names = {g.index: g.name for g in obj.vertex_groups}
    return [{names[g.group]: g.weight for g in v.groups if g.weight > 0} for v in obj.data.vertices]


def copy_weights_nearest(target, source, bones):
    """Each of the target's vertices takes the bone weights of the source's nearest vertex."""
    src = weights_of(source)
    tree = KDTree(len(source.data.vertices))
    for v in source.data.vertices:
        tree.insert(source.matrix_world @ v.co, v.index)
    tree.balance()
    for name in bones:
        if name not in target.vertex_groups:
            target.vertex_groups.new(name=name)
    for v in target.data.vertices:
        _, index, _ = tree.find(target.matrix_world @ v.co)
        for bone, weight in src[index].items():
            if bone in bones:
                target.vertex_groups[bone].add([v.index], weight, "REPLACE")


def only_bones(obj, bones):
    for group in list(obj.vertex_groups):
        if group.name not in bones:
            obj.vertex_groups.remove(group)
    # Every vertex weighed to some bone, the weights summing to one.
    weights = weights_of(obj)
    loose = [i for i, w in enumerate(weights) if sum(w.values()) < 1e-4]
    if loose:
        tree = KDTree(len(weights))
        for i, v in enumerate(obj.data.vertices):
            if i not in loose:
                tree.insert(v.co, i)
        tree.balance()
        for i in loose:
            _, j, _ = tree.find(obj.data.vertices[i].co)
            for bone, weight in weights[j].items():
                obj.vertex_groups[bone].add([i], weight, "REPLACE")
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    if obj.vertex_groups:
        bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
        bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    return len(loose)


def rig_up(obj, rig):
    obj.parent = rig
    obj.matrix_parent_inverse.identity()
    for mod in list(obj.modifiers):
        if mod.type == "ARMATURE":
            obj.modifiers.remove(mod)
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = rig


def ribbon(name, rows, normals):
    """A strip between two rows of points (each row a list of Vectors), double-sided."""
    mesh = bpy.data.meshes.new(name)
    left, right = rows
    verts = left + right
    n = len(left)
    faces = [(i, i + 1, n + i + 1, n + i) for i in range(n - 1)]
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    return link(bpy.data.objects.new(name, mesh))


# --- materials --------------------------------------------------------------------------------


def load_image(path, name, size, grey=False):
    image = bpy.data.images.load(path)
    image.name = name
    if image.size[0] > size:
        image.scale(size, size)
    if grey:
        # Hair: its colour taken out, light (the game multiplies in the person's own hair colour).
        px = np.empty(len(image.pixels), dtype=np.float32)
        image.pixels.foreach_get(px)
        px = px.reshape(-1, 4)
        lum = px[:, 0] * 0.3 + px[:, 1] * 0.59 + px[:, 2] * 0.11
        seen = px[:, 3] > 0.5
        mean = float(lum[seen].mean()) if seen.any() else 0.5
        lum = np.clip(lum * (0.82 / max(0.05, mean)), 0, 1)
        px[:, 0] = px[:, 1] = px[:, 2] = lum
        image.pixels.foreach_set(px.ravel())
    image.filepath_raw = os.path.join(OUT, "textures", name + ".png")
    image.file_format = "PNG"
    image.save()
    return image


def material(name, colour=(0.8, 0.8, 0.8), image=None, clip=False, two_sided=True):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    mat = bpy.data.materials.new(name)
    if not mat.node_tree:
        mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    output = nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = (*colour, 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    if image is not None:
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = image
        mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if clip:
            cut = nodes.new("ShaderNodeMath")
            cut.operation = "GREATER_THAN"
            cut.inputs[1].default_value = 0.5
            mat.node_tree.links.new(tex.outputs["Alpha"], cut.inputs[0])
            mat.node_tree.links.new(cut.outputs[0], bsdf.inputs["Alpha"])
    mat.use_backface_culling = not two_sided
    return mat


def set_material(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def mhmat_texture(mhmat_path, key="diffuseTexture"):
    folder = os.path.dirname(mhmat_path)
    for line in open(mhmat_path, encoding="utf-8", errors="ignore"):
        parts = line.strip().split(None, 1)
        if len(parts) == 2 and parts[0] == key:
            return os.path.join(folder, os.path.basename(parts[1].strip()))
    return None


# --- the clothes ------------------------------------------------------------------------------


def make_clothes(kit):
    rig = kit.rig
    body = kit.body
    s = kit.scale
    out = {}

    not_hand = lambda p: kit.mean_share(p, HAND) < 0.5
    not_head = lambda p: kit.mean_share(p, HEAD) < 0.5
    not_arm = lambda p: kit.mean_share(p, ARM) < 0.3

    # Under: a loincloth from the waist to the top of the thighs; the women's also a band over the breasts.
    crotch = min(c.z for p in body.data.polygons if kit.tights(p) for c in [kit.face_centre(p)] if abs(c.x) < 0.012 and c.z < kit.hip_z + 0.05)
    log(f"  crotch {crotch:.3f}")

    def under(p, c):
        if not kit.tights(p) or not not_arm(p):
            return False
        if crotch - 0.07 * s < c.z < kit.waist_z + 0.01:
            return True
        return not kit.male and abs(c.z - kit.chest_z) < 0.07 * s and kit.mean_share(p, LEG) < 0.1

    out["Cloth_Under"] = kit.extract("Cloth_Under", under)
    offset(out["Cloth_Under"], 0.003)

    trousers = kit.extract("Cloth_Trousers", lambda p, c: kit.tights(p) and not_arm(p) and kit.ankle_z + 0.012 < c.z < kit.waist_z + 0.015)
    offset(trousers, 0.008)
    out["Cloth_Trousers"] = trousers

    wraps = kit.extract("Cloth_Wraps", lambda p, c: kit.tights(p) and kit.ankle_z - 0.005 < c.z < kit.knee_z - 0.08 * s)
    offset(wraps, 0.015)
    out["Cloth_Wraps"] = wraps

    shoes = kit.extract("Cloth_Shoes", lambda p, c: kit.tights(p) and c.z < kit.ankle_z + 0.02)
    offset(shoes, 0.009)
    out["Cloth_Shoes"] = shoes

    # The robe's top: the tights from below the waist up to the neck, the arms to the wrists; then its sleeves widened.
    def top(p, c):
        return kit.tights(p) and not_hand(p) and not_head(p) and kit.waist_z - 0.05 < c.z < kit.neck_z + 0.004 and kit.mean_share(p, LEG) < 0.5

    def robe(name, hem):
        upper = kit.extract(name + "_top", top)
        offset(upper, 0.013)
        widen_sleeves(kit, upper)
        subdivide(upper)
        skirt = kit.extract(name + "_skirt", lambda p, c: kit.all_in(p, "helper-skirt"))
        bisect(skirt, hem)
        offset(skirt, 0.006)
        subdivide(skirt)
        obj = join([upper, skirt], name)
        moved = push_out(obj, trousers, 0.006) + push_out(obj, wraps, 0.006) + push_out(obj, out["Cloth_Under"], 0.008)
        log(f"  {name}: {moved} vertices moved out over the trousers")
        dress_collar(kit, obj)
        return obj

    long_hem = kit.knee_z + (kit.ankle_z - kit.knee_z) * (0.45 if kit.male else 0.75)
    short_hem = (kit.hip_z + kit.knee_z) / 2 if kit.male else kit.knee_z - 0.05 * s
    out["Cloth_Robe"] = robe("Cloth_Robe", long_hem)
    out["Cloth_Tunic"] = robe("Cloth_Tunic", short_hem)

    # The sash over the robe at the waist.
    belt = kit.extract("Cloth_Belt", lambda p, c: kit.tights(p) and not_arm(p) and abs(c.z - kit.waist_z) < 0.035 * s)
    sit_outside(belt, out["Cloth_Robe"], 0.008)
    out["Cloth_Belt"] = belt

    out["Cloth_Mark"] = crescent(kit, out["Cloth_Robe"])

    # One plain material per piece, named for the game to dye (the robe's three are set by dress_collar).
    for name, obj in out.items():
        if not obj.data.materials:
            piece = name.split("_", 1)[1]
            set_material(obj, material(piece, (0.86, 0.84, 0.78) if piece == "Under" else (0.8, 0.8, 0.8)))
    return out


def widen_sleeves(kit, obj):
    """The hanfu's sleeves: wider from the elbow to the wrist (half again and more), around the arm's own line."""
    weights = weights_of(obj)
    for side in SIDES:
        shoulder = kit.at(f"upperarm_{side}")
        elbow = kit.at(f"lowerarm_{side}")
        wrist = kit.at(f"hand_{side}")
        arm_bones = {f"upperarm_{side}", f"lowerarm_{side}", f"hand_{side}"}
        for v, w in zip(obj.data.vertices, weights):
            if sum(w.get(b, 0) for b in arm_bones) < 0.5:
                continue
            p = v.co
            # The nearest point on the arm's line, and how far from the elbow toward the wrist it is.
            fore = wrist - elbow
            t = max(0.0, min(1.0, (p - elbow).dot(fore) / fore.length_squared))
            on_fore = elbow + fore * t
            upper = elbow - shoulder
            u = max(0.0, min(1.0, (p - shoulder).dot(upper) / upper.length_squared))
            on_upper = shoulder + upper * u
            if (p - on_fore).length < (p - on_upper).length:
                centre, along = on_fore, t
            else:
                centre, along = on_upper, 0.0
            factor = 1.12 + 1.5 * along ** 1.4
            v.co = centre + (p - centre) * factor


def sit_outside(obj, over, gap):
    """Moves each vertex out along its normal until it is `gap` outside `over`'s surface."""
    tree = bvh(over)
    obj.data.update()
    for v in obj.data.vertices:
        n = v.normal.copy()
        hit = tree.ray_cast(v.co - n * 0.01, n, 0.15)
        v.co = (hit[0] + n * gap) if hit[0] is not None else v.co + n * 0.03


def push_out(cover, under, gap):
    """Moves each of the cover's vertices out along its normal until `under`'s surface is at least `gap` inside it."""
    tree = bvh(under)
    cover.data.update()
    moved = 0
    for v in cover.data.vertices:
        n = v.normal.copy()
        start = v.co + n * 0.03
        hit = tree.ray_cast(start, -n, 0.1)
        if hit[0] is None:
            continue
        inside = (start - hit[0]).length - 0.03
        if inside < gap:
            v.co += n * (gap - inside)
            moved += 1
    return moved


def dress_collar(kit, robe):
    """The crossed collar: the left panel over the right, a band of trim along each edge and round the neck,
    the inner robe's white showing in the V between them."""
    s = kit.scale
    tree = bvh(robe)
    zn = kit.neck_z - 0.008
    zw = kit.waist_z + 0.03
    a0 = Vector((0.055 * s, zn))
    a1 = Vector((-0.11 * s, zw))
    b0 = Vector((-0.055 * s, zn))
    # The inner edge mirrors the outer one; they cross at the V's point.
    d_a = a1 - a0
    d_b = Vector((-d_a.x, d_a.y))
    # Solve a0 + d_a t = b0 + d_b k.
    det = d_a.x * (-d_b.y) - d_a.y * (-d_b.x)
    rhs = b0 - a0
    t = (rhs.x * (-d_b.y) - rhs.y * (-d_b.x)) / det
    apex = a0 + d_a * t

    def on_front(x, z):
        hit = tree.ray_cast(Vector((x, -1.0, z)), Vector((0, 1, 0)), 2.0)
        return hit[0], hit[1]

    def band(start, end, width, steps):
        left, right = [], []
        prev = None
        points = []
        for i in range(steps + 1):
            q = start.lerp(end, i / steps)
            loc, nrm = on_front(q.x, q.y)
            if loc is None:
                continue
            points.append((loc, nrm))
        for i, (loc, nrm) in enumerate(points):
            a = points[max(0, i - 1)][0]
            b = points[min(len(points) - 1, i + 1)][0]
            tangent = (b - a).normalized()
            side = nrm.cross(tangent).normalized()
            for row, sign in ((left, 1), (right, -1)):
                edge = loc + side * (width / 2 * sign)
                near = tree.find_nearest(edge)
                base = near[0] if near[0] is not None else edge
                row.append(base + (near[1] if near[1] is not None else nrm) * 0.004)
        return left, right

    pieces = []
    width = 0.034 * s
    pieces.append(ribbon("collar_a", band(a0, a1, width, 28), None))
    pieces.append(ribbon("collar_b", band(b0, apex, width, 12), None))

    # Round the back of the neck: from the outer edge's start, behind, to the inner edge's start.
    centre = kit.at("neck_01")
    low, high = [], []
    start_loc, _ = on_front(a0.x, a0.y)
    end_loc, _ = on_front(b0.x, b0.y)
    if start_loc is not None and end_loc is not None:
        th0 = math.atan2(start_loc.y - centre.y, start_loc.x - centre.x)
        th1 = math.atan2(end_loc.y - centre.y, end_loc.x - centre.x)
        # Go round through the back (+Y, angle +90°).
        if th1 < th0:
            th1 += 2 * math.pi
        steps = 20
        for i in range(steps + 1):
            th = th0 + (th1 - th0) * i / steps
            direction = Vector((math.cos(th), math.sin(th), 0))
            origin = Vector((centre.x, centre.y, zn - 0.004)) + direction * 0.4
            hit = tree.ray_cast(origin, -direction, 0.5)
            radius = (hit[0] - Vector((centre.x, centre.y, zn - 0.004))).length if hit[0] is not None else 0.07 * s
            base = Vector((centre.x, centre.y, zn - 0.006)) + direction * (radius + 0.004)
            low.append(base)
            high.append(Vector((centre.x, centre.y, zn + 0.022 * s)) + direction * (radius * 0.93 + 0.004))
        pieces.append(ribbon("collar_neck", (low, high), None))

    # The V: the robe's front faces above both edges are the inner robe's (material 1).
    robe.data.materials.clear()
    robe.data.materials.append(material("Robe"))
    robe.data.materials.append(material("Inner", (0.9, 0.88, 0.82)))
    robe.data.materials.append(material("Trim"))
    spine_y = kit.at("spine_03").y

    def line_z(p0, p1, x):
        return p0.y + (x - p0.x) * (p1.y - p0.y) / (p1.x - p0.x)

    inner = 0
    for poly in robe.data.polygons:
        c = poly.center
        if c.y > spine_y or c.z < apex.y - 0.01 or c.z > zn + 0.02 or abs(c.x) > 0.09 * s:
            continue
        if c.z > line_z(a0, a1, c.x) and c.z > line_z(b0, apex, c.x):
            poly.material_index = 1
            inner += 1

    for piece in pieces:
        piece.data.materials.append(material("Trim"))
        copy_weights_nearest(piece, robe, set(robe.vertex_groups.keys()))
        # The bands keep the robe's group order (join matches groups by name).
    joined = join([robe] + pieces, robe.name)
    log(f"  {robe.name}: the V {inner} faces, apex {apex.y:.3f}, {len(joined.data.vertices)} vertices")


def crescent(kit, robe):
    """The Gu Yue's crescent moon on the robe's back, between the shoulders."""
    s = kit.scale
    tree = bvh(robe)
    z = kit.at("spine_03").z + 0.02 * s
    hit = tree.ray_cast(Vector((0, 1.0, z)), Vector((0, -1, 0)), 2.0)
    loc, nrm = hit[0], hit[1]
    up = Vector((0, 0, 1))
    right = up.cross(nrm).normalized()
    up = nrm.cross(right).normalized()
    R = 0.05 * s
    r = 0.04 * s
    d = 0.022 * s
    x = (R * R - r * r + d * d) / (2 * d)
    y = math.sqrt(max(0.0, R * R - x * x))
    t_top = math.atan2(y, x)
    t_bottom = 2 * math.pi - t_top
    p_top = math.atan2(y, x - d)
    p_bottom = -p_top
    outline = []
    n = 24
    for i in range(n + 1):
        t = t_top + (t_bottom - t_top) * i / n
        outline.append((R * math.cos(t), R * math.sin(t)))
    for i in range(1, n):
        # The inner arc back from the bottom to the top, round the inner circle's far side.
        p = (p_bottom + 2 * math.pi) - ((p_bottom + 2 * math.pi) - p_top) * i / n
        outline.append((d + r * math.cos(p), r * math.sin(p)))
    bm = bmesh.new()
    verts = [bm.verts.new(loc + right * u + up * v + nrm * 0.005) for u, v in outline]
    face = bm.faces.new(verts)
    bmesh.ops.triangulate(bm, faces=[face], quad_method="BEAUTY", ngon_method="EAR_CLIP")
    mesh = bpy.data.meshes.new("Cloth_Mark")
    bm.to_mesh(mesh)
    bm.free()
    obj = link(bpy.data.objects.new("Cloth_Mark", mesh))
    copy_weights_nearest(obj, robe, set(robe.vertex_groups.keys()))
    set_material(obj, material("Mark"))
    return obj


# --- the beards -------------------------------------------------------------------------------


def make_beards(kit):
    body = kit.body
    mouth = kit.mouth
    head_centre = (kit.at("head") + kit.at("head", tail=True)) / 2
    head = lambda i: kit.bone_share(i, {"head"}) > 0.6 and kit.in_group(i, "body")
    chin_z = min(kit.co[i].z for i in range(len(kit.co)) if head(i) and abs(kit.co[i].x) < 0.015 and kit.co[i].y < mouth.y + 0.02)
    log(f"  chin {chin_z:.3f}")

    def outward(poly, c):
        return poly.normal.dot((c - head_centre).normalized()) > 0.2

    def face_ok(poly, c, region):
        return all(head(v) and not kit.in_group(v, "lips", 0.3) for v in poly.vertices) and outward(poly, c) and region(c)

    def full_region(c):
        if c.y > kit.ear_front_y - 0.004 or abs(c.x) > kit.ear_inner_x:
            return False
        lift = max(0.0, min(1.0, (abs(c.x) - 0.03) / 0.03))
        top = mouth.z + 0.016 + lift * (kit.ear_mid_z - mouth.z - 0.03)
        return chin_z - 0.028 < c.z < top

    regions = {
        "Beard_Mustache": (lambda c: abs(c.x) < 0.026 and mouth.z + 0.003 < c.z < mouth.z + 0.017 and c.y < mouth.y + 0.012, 0.004),
        "Beard_Goatee": (lambda c: (abs(c.x) < 0.026 and mouth.z + 0.003 < c.z < mouth.z + 0.017 and c.y < mouth.y + 0.012)
                                    or (abs(c.x) < 0.021 and chin_z - 0.006 < c.z < mouth.z - 0.011 and c.y < mouth.y + 0.03), 0.005),
        "Beard_Full": (full_region, 0.007),
        "Beard_Stubble": (full_region, 0.0012),
    }
    out = {}
    for name, (region, lift) in regions.items():
        obj = kit.extract(name, lambda p, c, region=region: face_ok(p, c, region))
        offset(obj, lift)
        if name in ("Beard_Full", "Beard_Goatee"):
            for v in obj.data.vertices:
                if v.co.z < mouth.z - 0.012 and abs(v.co.x) < 0.03:
                    v.co.z -= 0.012 * (1 - abs(v.co.x) / 0.03)
        set_material(obj, material("Stubble" if name == "Beard_Stubble" else "Beard", (0.85, 0.85, 0.85)))
        out[name] = obj
    return out


# --- the buns: a topknot with a pin -----------------------------------------------------------


def add_bun(kit, hair):
    # The crown, toward the back of the head (the figure looks along -Y).
    top = max(hair.data.vertices, key=lambda v: v.co.z + 0.4 * v.co.y).co.copy()
    s = kit.scale
    centre = top + Vector((0, 0.028 * s, -0.012 * s))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.036 * s)
    bmesh.ops.translate(bm, verts=bm.verts, vec=centre)
    pin = bmesh.new()
    bmesh.ops.create_cone(pin, cap_ends=True, segments=6, radius1=0.0035 * s, radius2=0.0025 * s, depth=0.13 * s)
    bmesh.ops.rotate(pin, verts=pin.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, "Y"))
    bmesh.ops.translate(pin, verts=pin.verts, vec=centre + Vector((0, 0, 0.006 * s)))
    mesh = bpy.data.meshes.new("bun")
    bm.to_mesh(mesh)
    bm.free()
    pin_mesh = bpy.data.meshes.new("pin")
    pin.to_mesh(pin_mesh)
    pin.free()
    bun = link(bpy.data.objects.new("bun", mesh))
    stick = link(bpy.data.objects.new("pin", pin_mesh))
    for poly in bun.data.polygons:
        poly.use_smooth = True
    bun.data.materials.append(material("HairSolid", (0.82, 0.82, 0.82)))
    stick.data.materials.append(material("Pin", (0.42, 0.62, 0.5)))
    for piece in (bun, stick):
        piece.vertex_groups.new(name="head").add(list(range(len(piece.data.vertices))), 1.0, "REPLACE")
    return join([hair, bun, stick], hair.name)


# --- one kit ----------------------------------------------------------------------------------


def make_kit(name, macro):
    clear_scene()
    log(f"== {name}")
    kit = Kit(name, macro)
    rig = kit.rig
    log(f"  rig at {tuple(rig.matrix_world.translation)} scale {tuple(rig.matrix_world.to_scale())}, body parent "
        f"{kit.body.parent.name if kit.body.parent else None}, body at {tuple(kit.body.matrix_world.translation)} scale {tuple(kit.body.matrix_world.to_scale())}")
    rig.name = "Rig"
    rig.data.name = "Rig"
    bones = kit.bone_set
    sex = "male" if kit.male else "female"

    pieces = {}
    pieces.update(make_clothes(kit))
    if kit.male:
        pieces.update(make_beards(kit))

    # The skin: the base mesh without its helpers.
    body = kit.body
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.verts.ensure_lookup_table()
    gone = [bm.verts[i] for i in range(len(bm.verts)) if not kit.in_group(i, "body")]
    bmesh.ops.delete(bm, geom=gone, context="VERTS")
    bm.to_mesh(body.data)
    bm.free()
    for mod in list(body.modifiers):
        if mod.type == "MASK":
            body.modifiers.remove(mod)
    body.name = "Body"
    body.data.name = "Body"

    data = kit.data_root
    skin = load_image(mhmat_texture(os.path.join(data, f"skins/young_asian_{sex}/young_asian_{sex}.mhmat")), f"skin_{sex}", 1024)
    set_material(body, material("Skin", image=skin, two_sided=False))
    set_material(kit.assets["Eyes"], material("Eyes", image=load_image(os.path.join(data, "eyes/materials/brown_eye.png"), "eye_brown", 256), two_sided=False))
    brow_src = "eyebrow001" if kit.male else "eyebrow010"
    set_material(kit.assets["Eyebrows"], material("Eyebrows", image=load_image(os.path.join(data, f"eyebrows/{brow_src}/{brow_src}.png"), f"brows_{sex}", 512), clip=True))
    set_material(kit.assets["Eyelashes"], material("Eyelashes", image=load_image(os.path.join(data, "eyelashes/eyelashes01/eyelashes01.png"), "lashes", 512), clip=True))
    for key in ("Eyes", "Eyebrows", "Eyelashes"):
        kit.assets[key].name = key
        pieces[key] = kit.assets[key]

    for hair_name, source in kit.hair_sources.items():
        obj = kit.assets[hair_name]
        tex_path = mhmat_texture(os.path.join(data, f"hair/{source}/{source}.mhmat"))
        set_material(obj, material(f"Hair_{source}", image=load_image(tex_path, f"hair_{source}", 1024, grey=True), clip=True))
        obj.name = hair_name
        if hair_name == "Hair_Buns":
            obj = add_bun(kit, obj)
        pieces[hair_name] = obj

    # Everything on the skeleton, weighed to its bones only.
    pieces["Body"] = body
    for key, obj in pieces.items():
        loose = only_bones(obj, bones)
        rig_up(obj, rig)
        obj.data.name = key
        for poly in obj.data.polygons:
            poly.use_smooth = True
        log(f"  {key}: {len(obj.data.vertices)} vertices, {len(obj.data.polygons)} faces, materials "
            f"{[m.name for m in obj.data.materials]}, {loose} unweighted vertices given their neighbour's")

    # Anything MPFB left behind (proxies, empties) goes.
    for obj in list(bpy.data.objects):
        if obj != rig and obj not in pieces.values():
            log("  removing", obj.name, obj.type)
            bpy.data.objects.remove(obj)

    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    for o in bpy.context.selected_objects:
        o.select_set(False)
    if BLEND_DIR:
        os.makedirs(BLEND_DIR, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND_DIR, name + ".blend"), copy=True)
    bpy.ops.export_scene.gltf(
        filepath=os.path.join(folder, name + ".gltf"),
        export_format="GLTF_SEPARATE",
        export_texture_dir="../textures",
        export_skins=True,
        export_animations=False,
        export_morph=False,
        export_yup=True,
        export_apply=False,
        export_def_bones=False,
        export_image_format="AUTO",
        use_selection=False,
    )
    log(f"  written {folder}")


def main():
    import addon_utils
    try:
        bpy.ops.preferences.addon_enable(module="bl_ext.user_default.mpfb")
    except Exception as error:
        log("could not enable MPFB:", error)
    from bl_ext.user_default.mpfb.services.locationservice import LocationService
    LocationService._mpfb_data = MPFB_DATA
    os.makedirs(os.path.join(OUT, "textures"), exist_ok=True)
    for name, macro in KITS.items():
        if ONLY and name not in ONLY.split(","):
            continue
        try:
            make_kit(name, macro)
        except Exception:
            log("FAILED", name, traceback.format_exc())
    log("done")


main()
_log.close()
