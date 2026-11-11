"""
Makes the hero's game models from the full one (gu-hero.glb, 626 000 triangles):

  gu-hero-game.glb   about 70 000 triangles: the hero on ordinary quality
  gu-npc.glb         about 18 000 triangles: villagers, bandits, the other clans' fighters
  Looks/<look>.jpg   the body's texture dyed for each of them (robe, hair, skin)

The full model stays as it is for high quality. Same skeleton and bone names in all three,
so one set of animations (anim.py) moves them all.

  python hero.py            (from Tools/V2/models; needs: pip install bpy pillow numpy)
"""
import colorsys
import os
import sys

import bpy
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import armature_of, load_glb, meshes_of, reset

HERE = os.path.dirname(os.path.abspath(__file__))
MODELS = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Models'))
LOOKS = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Looks'))
SOURCE = os.path.join(MODELS, 'gu-hero.glb')

# Triangles to leave in each mesh.
GAME = {'GuHero_Body': 46000, 'GuHero_Face': 14000, 'GuHero_Hands': 10000}
NPC = {'GuHero_Body': 12500, 'GuHero_Face': 3500, 'GuHero_Hands': 2200}

# name: (robe hue 0-1 or None to keep, robe saturation, robe brightness gain, hair colour or None, skin tint)
LOOK_TABLE = {
    'villager': (0.08, 0.30, 1.55, (0.10, 0.08, 0.07), (1.0, 1.0, 1.0)),
    'keeper': (0.36, 0.38, 1.45, (0.09, 0.07, 0.06), (1.0, 1.0, 1.0)),
    'guard': (0.99, 0.60, 1.35, (0.06, 0.05, 0.05), (0.96, 0.94, 0.92)),
    'merchant': (0.11, 0.55, 1.9, (0.12, 0.09, 0.07), (1.0, 0.98, 0.95)),
    'cook': (0.07, 0.25, 2.3, (0.16, 0.14, 0.13), (1.0, 0.97, 0.94)),
    'herbalist': (0.47, 0.40, 1.6, (0.07, 0.06, 0.06), (1.0, 1.0, 1.0)),
    'smith': (0.06, 0.20, 1.0, (0.05, 0.05, 0.05), (0.90, 0.84, 0.80)),
    'gambler': (0.78, 0.40, 1.5, (0.40, 0.40, 0.42), (1.0, 0.97, 0.93)),
    'scribe': (0.60, 0.30, 1.7, (0.07, 0.07, 0.08), (1.0, 1.0, 1.0)),
    'judge': (0.12, 0.35, 2.9, (0.55, 0.55, 0.56), (0.98, 0.96, 0.93)),
    'elder': (0.60, 0.06, 3.6, (0.86, 0.86, 0.88), (0.97, 0.95, 0.93)),
    'bandit': (0.03, 0.35, 0.85, (0.05, 0.04, 0.04), (0.86, 0.80, 0.76)),
    'xiong': (0.05, 0.62, 1.7, (0.16, 0.09, 0.05), (0.95, 0.88, 0.82)),
    'bai': (0.56, 0.22, 3.2, (0.80, 0.84, 0.90), (1.0, 1.0, 1.02)),
    'blood': (0.0, 0.85, 1.25, (0.20, 0.02, 0.02), (0.80, 0.70, 0.70)),
    'stone': (0.10, 0.04, 2.0, (0.42, 0.42, 0.42), None),
}


def decimate(obj, triangles):
    current = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    if current <= triangles:
        return
    bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new('decimate', 'DECIMATE')
    modifier.ratio = triangles / current
    modifier.use_collapse_triangulate = True
    # Applied before the armature, so the weights ride along.
    while obj.modifiers[0] != modifier:
        bpy.ops.object.modifier_move_up(modifier=modifier.name)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.update()


def limit_weights(obj, keep=4):
    """Unity skins with four bones a vertex: the lightest weights go, the rest add up to one."""
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.vertex_group_limit_total(limit=keep)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)


def build(out, budget):
    reset()
    objects = load_glb(SOURCE)
    for extra in [o for o in objects if o.type == 'MESH' and o.name not in budget]:
        bpy.data.objects.remove(extra)
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    for obj in meshes:
        for other in bpy.data.objects:
            other.select_set(other is obj)
        decimate(obj, budget[obj.name])
        limit_weights(obj)
        print(f'  {obj.name}: {len(obj.data.polygons)} triangles')
    for obj in bpy.data.objects:
        obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_animations=False, export_skins=True,
                              export_yup=True, export_image_format='JPEG', export_image_quality=88,
                              export_def_bones=False, export_extras=False, use_selection=False)
    print(f'{os.path.basename(out)}: {os.path.getsize(out) // 1024} KB')


def body_texture_and_masks():
    """The body's texture, and which of its texels are hair (the mesh above the neck that is not skin)."""
    reset()
    objects = load_glb(SOURCE)
    body = next(o for o in objects if o.name == 'GuHero_Body')
    image = body.data.materials[0].node_tree.nodes['Image Texture'].image if 'Image Texture' in body.data.materials[0].node_tree.nodes else None
    if image is None:
        image = next(n.image for n in body.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE')
    width, height = image.size
    pixels = np.array(image.pixels[:], dtype=np.float32).reshape(height, width, 4)[::-1, :, :3]
    texture = Image.fromarray((np.clip(pixels, 0, 1) * 255).astype(np.uint8), 'RGB')

    # Faces whose corners are all above the collar: the head and the hair down the back.
    mesh = body.data
    uv = mesh.uv_layers.active.data
    world = body.matrix_world
    heights = np.array([(world @ v.co).z for v in mesh.vertices])
    backs = np.array([(world @ v.co).y for v in mesh.vertices])
    mask = Image.new('L', (width, height), 0)
    draw = ImageDraw.Draw(mask)
    for polygon in mesh.polygons:
        zs = heights[list(polygon.vertices)]
        ys = backs[list(polygon.vertices)]
        # The head, and the hair that hangs behind the shoulders (the model faces −Y).
        if zs.min() > 1.47 or (zs.min() > 1.18 and ys.min() > 0.075):
            points = [(uv[i].uv[0] * width, (1 - uv[i].uv[1]) * height) for i in polygon.loop_indices]
            draw.polygon(points, fill=255)
    return texture, mask


def dye(texture, head_mask, look):
    hue, saturation, gain, hair, skin_tint = look
    rgb = np.asarray(texture, dtype=np.float32) / 255
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    high = rgb.max(axis=2)
    low = rgb.min(axis=2)
    chroma = high - low
    # Skin: warm, not too dark, clearly red over blue.
    skin = (r > g) & (g > b) & (high > 0.32) & (chroma > 0.07) & ((r - b) > 0.10)
    head = np.asarray(head_mask, dtype=np.float32) / 255 > 0.5
    hair_area = head & ~skin & (high < 0.42)
    cloth = ~skin & ~hair_area
    out = rgb.copy()
    value = high

    if skin_tint is None:
        # Stone: everything grey, lit by its own relief.
        grey = (0.3 * r + 0.59 * g + 0.11 * b)
        stone = np.clip(grey * gain + 0.12, 0, 1)
        out = np.stack([stone * 0.98, stone, stone * 1.03], axis=2)
    else:
        rc, gc, bc = colorsys.hsv_to_rgb(hue % 1, saturation, 1)
        lit = np.clip(value * gain, 0, 1)
        dyed = np.stack([lit * rc, lit * gc, lit * bc], axis=2)
        # The cloth keeps a little of its own colour so trims and seams still read.
        out[cloth] = (dyed * 0.82 + rgb * gain * 0.18)[cloth]
        out[skin] = (rgb * np.array(skin_tint, dtype=np.float32))[skin]
        if hair is not None:
            shade = np.clip(value / 0.16, 0.25, 2.2)[..., None]
            out[hair_area] = (np.array(hair, dtype=np.float32) * shade)[hair_area]

    result = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'RGB')
    return result


def looks():
    os.makedirs(LOOKS, exist_ok=True)
    texture, head = body_texture_and_masks()
    size = 1024
    small = texture.resize((size, size), Image.LANCZOS)
    head_small = head.resize((size, size), Image.BILINEAR).filter(ImageFilter.MaxFilter(3))
    small.save(os.path.join(LOOKS, 'hero.jpg'), quality=88)
    for name, look in LOOK_TABLE.items():
        dye(small, head_small, look).save(os.path.join(LOOKS, name + '.jpg'), quality=86)
    print(f'{len(LOOK_TABLE) + 1} looks in {LOOKS}')


if __name__ == '__main__':
    what = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if what in ('all', 'game'):
        build(os.path.join(MODELS, 'gu-hero-game.glb'), GAME)
    if what in ('all', 'npc'):
        build(os.path.join(MODELS, 'gu-npc.glb'), NPC)
    if what in ('all', 'looks'):
        looks()
