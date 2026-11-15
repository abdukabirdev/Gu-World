"""
Renders the clan's underground (Core/Gen/Buildings/DepthsGen) from inside, as the game lights it:
no sky, only its own lights (the Sim writes them beside the model: Logs/V2/gen/depths/<name>.json)
and the glow of the spring, the orchids, the jade plates and the blood lake.

  python depthshots.py <gen_dir> <out.png> [size] [samples]

Views (the game's coordinates, the entry at the origin, the way in toward +Z): the stair, the spring
from the Dark Hall's post, the orchid sea, a Gu room, the corridor to the forbidden door, the Secret
Hall, the shaft, the blood pond.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_glb, reset
from buildshots import dress

VIEWS = [
    ('stair', (0, 1.1, 2.4), (0, -12, 28)),
    ('spring', (0, -13.2, 37.5), (0, -15.5, 56)),
    ('orchids', (-17, -12.5, 63), (4, -15.8, 50)),
    ('gu room', (32, -14.2, 55.2), (32, -14.8, 63)),
    ('forbidden door', (48, -14.3, 52), (64, -14.6, 52)),
    ('secret hall', (-55.5, -13.4, 57), (-67, -15.6, 54)),
    ('shaft', (1.5, -25.5, 103.5), (0, -40, 106.5)),
    ('blood pond', (0, -46.5, 131), (0, -49, 166)),
]


def b(p):
    """The game's (x, y, z) in Blender's frame (the .glb mirrors x; the importer turns y up to z up)."""
    return Vector((-p[0], -p[2], p[1]))


def main():
    gen = sys.argv[1]
    out = sys.argv[2]
    size = int(sys.argv[3]) if len(sys.argv) > 3 else 520
    samples = int(sys.argv[4]) if len(sys.argv) > 4 else 32
    base = os.path.join(gen, 'depths', 'gu_yue_depths')
    info = json.load(open(base + '.json'))
    tiles = []

    reset()
    objects = load_glb(base + '.glb')
    dress(objects, os.path.join(gen, 'btex'), 1.3)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 4
    scene.view_settings.view_transform = 'Standard'
    scene.render.resolution_x, scene.render.resolution_y = size, int(size * 0.75)
    world = scene.world or bpy.data.worlds.new('w')
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (0.05, 0.06, 0.08, 1)
    bg.inputs[1].default_value = 0.15

    for i, light in enumerate(info['lights']):
        data = bpy.data.lights.new(f'_l{i}', 'POINT')
        data.color = light['colour']
        data.energy = light['intensity'] * light['range'] ** 2 * 5
        data.shadow_soft_size = 0.4
        obj = bpy.data.objects.new(f'_l{i}', data)
        obj.location = b(light['at'])
        scene.collection.objects.link(obj)

    cam = bpy.data.objects.new('_cam', bpy.data.cameras.new('_cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.lens = 18
    cam.data.clip_start = 0.05

    for name, eye, target in VIEWS:
        cam.location = b(eye)
        cam.rotation_euler = (b(target) - b(eye)).to_track_quat('-Z', 'Y').to_euler()
        path = f'{out}.{name.replace(" ", "_")}.png'
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        tiles.append((path, name))

    w, h = size, int(size * 0.75)
    columns = 2
    rows = (len(tiles) + columns - 1) // columns
    sheet = Image.new('RGB', (w * columns, h * rows))

    for i, (t, _) in enumerate(tiles):
        sheet.paste(Image.open(t), ((i % columns) * w, (i // columns) * h))
        os.remove(t)

    sheet.save(out)
    print('sheet:', out)


if __name__ == '__main__':
    main()
