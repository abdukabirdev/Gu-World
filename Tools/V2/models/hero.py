"""
Makes the hero's game models from the full one (gu-hero.glb, 626 000 triangles):

  gu-hero-game.glb   about 70 000 triangles: the hero on ordinary quality
  gu-npc.glb         about 18 000 triangles: villagers, bandits, the other clans' fighters
  Looks/<look>.jpg   the body's texture dyed for each of them (robe, hair, skin)
  Looks/hero_zones.png  for the hero's own looks (ROADMAP 2.5): where the body's texture is hair,
                     skin, and what keeps its dark colour under any dye (Core.Appearances.DyeBody)

The full model stays as it is for high quality. Same skeleton and bone names in all three,
so one set of animations (anim.py) moves them all.

  python hero.py            (from Tools/V2/models; needs: pip install bpy pillow numpy)
  python hero.py zones      only the hero's zones
  python hero.py preview    Logs/V2/hero_looks.png: every dye, skin and hair style on the model,
                            dyed by the game's own code (Tools/V2/Sim `looks`; needs .NET 8)
"""
import subprocess
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


def smooth(e0, e1, v):
    t = np.clip((v - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def positions(size=2048):
    """Where each texel of the body's texture lies on the model at rest (Blender's axes, metres), rows as in the picture."""
    reset()
    objects = load_glb(SOURCE)
    body = next(o for o in objects if o.name == 'GuHero_Body')
    for other in [o for o in bpy.data.objects if o is not body and o.type == 'MESH']:
        bpy.data.objects.remove(other)
    image = bpy.data.images.new('positions', size, size, float_buffer=True, alpha=True)
    tree = body.data.materials[0].node_tree
    for node in [n for n in tree.nodes if n.type != 'OUTPUT_MATERIAL']:
        tree.nodes.remove(node)
    output = next(n for n in tree.nodes if n.type == 'OUTPUT_MATERIAL')
    geometry = tree.nodes.new('ShaderNodeNewGeometry')
    emission = tree.nodes.new('ShaderNodeEmission')
    tree.links.new(geometry.outputs['Position'], emission.inputs['Color'])
    tree.links.new(emission.outputs['Emission'], output.inputs['Surface'])
    target = tree.nodes.new('ShaderNodeTexImage')
    target.image = image
    tree.nodes.active = target
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 1
    scene.render.bake.margin = 6
    bpy.context.view_layer.objects.active = body
    for o in bpy.data.objects:
        o.select_set(o is body)
    bpy.ops.object.bake(type='EMIT')
    return np.array(image.pixels[:], dtype=np.float32).reshape(size, size, 4)[::-1, :, :3]


def zones():
    """
    Looks/hero_zones.png (1024, 16 levels a channel): red — the model's hair: the hood of it over the head
    and the two locks hanging over the chest; green — where its skin may show: the head and the bare feet
    (the colour then decides, Core.Appearances.Warm); blue — what keeps its own dark colour under any dye:
    the boots, the sash, the bracers. Measured on the model at rest (its head's middle is 3 cm off its axis).
    """
    p = positions()
    covered = np.abs(p).sum(2) > 0
    x, y, z = p[..., 0], p[..., 1], p[..., 2]
    off = x - 0.03
    cap = smooth(1.455, 1.485, z)
    front = 1 - smooth(0.04, 0.08, y)
    left = smooth(0.025, 0.045, -off) * (1 - smooth(0.115, 0.14, -off)) * smooth(1.36, 1.40, z)
    right = smooth(0.025, 0.045, off) * (1 - smooth(0.105, 0.13, off)) * smooth(1.38, 1.42, z)
    hair = np.maximum(cap, np.maximum(left, right) * front)
    skin = np.maximum(smooth(1.44, 1.48, z), 1 - smooth(0.08, 0.12, z))

    def band(low, high, v, soft):
        return smooth(low - soft, low + soft, v) * (1 - smooth(high - soft, high + soft, v))

    boots = 1 - smooth(0.385, 0.425, z)
    sash = band(1.035, 1.11, z, 0.006) * (1 - smooth(0.19, 0.22, np.abs(off)))
    bracers = band(0.92, 1.125, z, 0.008) * smooth(0.17, 0.20, np.abs(off))
    keep = np.maximum(boots, np.maximum(sash, bracers))
    m = np.stack([hair, skin, keep], 2)
    m[~covered] = 0
    half = m.shape[0] // 2
    m = m.reshape(half, 2, half, 2, 3).mean((1, 3))
    m = np.round(m * 255 / 17) * 17
    out = os.path.join(LOOKS, 'hero_zones.png')
    Image.fromarray(m.astype(np.uint8)).save(out, optimize=True)
    print(f'{out}: {os.path.getsize(out) // 1024} KB')


def preview():
    """Logs/V2/hero_looks.png: the hero in each dye (front and back), each skin, each hair style — the texture dyed by Core's own code."""
    import json
    import struct
    import io
    import math
    from mathutils import Vector
    work = os.path.normpath(os.path.join(HERE, '../../../Logs/V2/looks'))
    os.makedirs(work, exist_ok=True)
    game = os.path.join(MODELS, 'gu-hero-game.glb')
    data = open(game, 'rb').read()
    length = struct.unpack('<I', data[12:16])[0]
    gltf = json.loads(data[20:20 + length])
    blob = data[20 + length + 8:]
    material = next(m for m in gltf['materials'] if m['name'] == 'Body')
    view = gltf['bufferViews'][gltf['images'][gltf['textures'][material['pbrMetallicRoughness']['baseColorTexture']['index']]['source']]['bufferView']]
    body = Image.open(io.BytesIO(blob[view.get('byteOffset', 0):view.get('byteOffset', 0) + view['byteLength']])).convert('RGBA')
    zone = Image.open(os.path.join(LOOKS, 'hero_zones.png')).convert('RGBA')
    open(os.path.join(work, 'body.rgba'), 'wb').write(np.asarray(body).tobytes())
    open(os.path.join(work, 'zones.rgba'), 'wb').write(np.asarray(zone).tobytes())
    sim = os.path.normpath(os.path.join(HERE, '../Sim'))
    subprocess.run(['dotnet', 'run', '-c', 'Release', '--project', sim, '--', 'looks', os.path.join(work, 'body.rgba'), str(body.width), str(body.height),
                    os.path.join(work, 'zones.rgba'), str(zone.width), str(zone.height), work], check=True)
    for name in os.listdir(work):
        if name.endswith('.rgba') and name.startswith('skin'):
            raw = np.frombuffer(open(os.path.join(work, name), 'rb').read(), dtype=np.uint8).reshape(body.height, body.width, 4)
            Image.fromarray(raw[..., :3]).save(os.path.join(work, name[:-5] + '.png'))

    # The skins' tints, as Core.Appearances.Tint (the swatch over the first one's), in Blender's linear light.
    swatches = [0xF4E4D6, 0xEBD2BA, 0xD9B28E, 0xC08E66, 0x9A6A48, 0x6E4A32]
    tint = lambda i: [(((swatches[i] >> s) & 255) / ((swatches[0] >> s) & 255)) ** 2.2 for s in (16, 8, 0)]
    reset()
    load_glb(game)
    for extra in [o for o in bpy.data.objects if o.type == 'MESH' and not o.name.startswith('GuHero')]:
        bpy.data.objects.remove(extra)
    hair = {style: load_glb(os.path.join(work, f'hair{style}.glb')) for style in (1, 2, 3)}
    for objects in hair.values():
        for o in objects:
            for m in (o.data.materials if o.type == 'MESH' else []):
                principled = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
                if principled:
                    colours = m.node_tree.nodes.new('ShaderNodeVertexColor')
                    m.node_tree.links.new(colours.outputs['Color'], principled.inputs['Base Color'])
    body_object = bpy.data.objects['GuHero_Body']
    texture_node = next(n for n in body_object.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE')
    own_texture = texture_node.image
    face = bpy.data.objects['GuHero_Face'].data.materials[0].node_tree
    face_texture = next(n for n in face.nodes if n.type == 'TEX_IMAGE')
    multiply = face.nodes.new('ShaderNodeMix')
    multiply.data_type, multiply.blend_type = 'RGBA', 'MULTIPLY'
    multiply.inputs[0].default_value = 1
    face.links.new(face_texture.outputs['Color'], multiply.inputs[6])
    face.links.new(multiply.outputs[2], next(n for n in face.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'])
    hands = next(n for n in bpy.data.objects['GuHero_Hands'].data.materials[0].node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    hands_own = list(hands.inputs['Base Color'].default_value)
    from common import render

    def dress(texture, skin, style):
        texture_node.image = bpy.data.images.load(texture) if texture else own_texture
        k = tint(skin)
        multiply.inputs[7].default_value = (k[0], k[1], k[2], 1)
        hands.inputs['Base Color'].default_value = (hands_own[0] * k[0], hands_own[1] * k[1], hands_own[2] * k[2], 1)
        for s, objects in hair.items():
            for o in objects:
                o.hide_render = s != style

    shots = []
    for robe in range(7):
        dress(os.path.join(work, f'skin0_robe{robe}.png') if robe else None, 0, 0)
        for yaw in (0, 180):
            path = os.path.join(work, f'robe{robe}_{yaw}.png')
            render(path, target=(0.03, 0, 0.95), distance=3.0, yaw=yaw, height=1.15, size=(240, 400), samples=8)
            shots.append((path, 0 if yaw == 0 else 1, robe))
    for skin in range(6):
        dress(os.path.join(work, f'skin{skin}_robe0.png') if skin else None, skin, 0)
        path = os.path.join(work, f'skin{skin}.png')
        render(path, target=(0.03, 0, 1.55), distance=1.2, yaw=20, height=1.6, size=(240, 300), samples=10)
        shots.append((path, 2, skin))
    for style in range(4):
        dress(None, 0, style)
        for i, (yaw, h) in enumerate(((15, 1.68), (160, 1.74))):
            path = os.path.join(work, f'hair{style}_{yaw}.png')
            render(path, target=(0.03, 0, 1.62), distance=0.95, yaw=yaw, height=h, size=(240, 300), samples=10)
            shots.append((path, 3 + i, style))
    sheet = Image.new('RGB', (7 * 240, 2 * 400 + 3 * 300), (40, 44, 50))
    tops = [0, 400, 800, 1100, 1400]
    for path, row, column in shots:
        sheet.paste(Image.open(path).convert('RGB'), (column * 240, tops[row]))
    out = os.path.normpath(os.path.join(HERE, '../../../Logs/V2/hero_looks.png'))
    sheet.save(out)
    print(out)


if __name__ == '__main__':
    what = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if what in ('all', 'game'):
        build(os.path.join(MODELS, 'gu-hero-game.glb'), GAME)
    if what in ('all', 'npc'):
        build(os.path.join(MODELS, 'gu-npc.glb'), NPC)
    if what in ('all', 'looks'):
        looks()
    if what in ('all', 'zones'):
        zones()
    if what == 'preview':
        preview()
