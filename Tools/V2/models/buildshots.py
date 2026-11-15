"""
Renders generated buildings in Blender (Cycles) to judge them without the engine. Reads the .glb
the simulator wrote (Sim `gen export only=buildings`: Logs/V2/gen/buildings/<name>_lod<k>.glb, with
the textures in Logs/V2/gen/btex) and builds the materials as the game draws them: the tiling
texture times the vertices' colour, the paper and lanterns glowing, gilding shining.

  python buildshots.py <gen_dir> <out.png> name[,name...] [views] [size] [lod]

views: any of t (three-quarter from the front), f (the front, close, at eye height), d (dusk:
the same three-quarter view at evening, the windows lit), s (the side and back). Default "tf".
"""
import math
import os
import sys

import bpy
from mathutils import Vector
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_glb, reset
import treeshots
from treeshots import attribute, image, shoot


def textured(mat, tex_dir, name):
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.55 if name == 'tile' else 0.85
    uv = nodes.new('ShaderNodeUVMap')
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image(os.path.join(tex_dir, name + '.jpg'))
    links.new(uv.outputs[0], tex.inputs[0])
    mul = nodes.new('ShaderNodeMixRGB')
    mul.blend_type = 'MULTIPLY'
    mul.inputs[0].default_value = 1
    links.new(tex.outputs[0], mul.inputs[1])
    links.new(attribute(nodes, links), mul.inputs[2])
    links.new(mul.outputs[0], bsdf.inputs['Base Color'])
    normal_path = os.path.join(tex_dir, name + '_n.png')
    if os.path.exists(normal_path):
        n = nodes.new('ShaderNodeTexImage')
        n.image = image(normal_path)
        n.image.colorspace_settings.name = 'Non-Color'
        links.new(uv.outputs[0], n.inputs[0])
        nm = nodes.new('ShaderNodeNormalMap')
        nm.inputs['Strength'].default_value = 0.9
        links.new(n.outputs[0], nm.inputs['Color'])
        links.new(nm.outputs[0], bsdf.inputs['Normal'])
    links.new(bsdf.outputs[0], out.inputs[0])


def plain(mat, glow, strength):
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    colour = attribute(nodes, links)
    if glow:
        em = nodes.new('ShaderNodeEmission')
        em.inputs['Strength'].default_value = strength
        links.new(colour, em.inputs['Color'])
        diffuse = nodes.new('ShaderNodeBsdfDiffuse')
        links.new(colour, diffuse.inputs['Color'])
        add = nodes.new('ShaderNodeAddShader')
        links.new(em.outputs[0], add.inputs[0])
        links.new(diffuse.outputs[0], add.inputs[1])
        links.new(add.outputs[0], out.inputs[0])
        return
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.35
    bsdf.inputs['Metallic'].default_value = 0.7
    links.new(colour, bsdf.inputs['Base Color'])
    links.new(bsdf.outputs[0], out.inputs[0])


def dress(objects, tex_dir, glow):
    for obj in objects:
        if obj.type != 'MESH':
            continue
        name = obj.name.split('.')[0]
        kind = name.split('_', 1)[1] if '_' in name else name
        mat = bpy.data.materials.new(name)
        if kind.startswith('tex_'):
            textured(mat, tex_dir, kind[4:])
        elif kind == 'ground':
            mat.use_nodes = True
            nodes, links = mat.node_tree.nodes, mat.node_tree.links
            bsdf = nodes['Principled BSDF']
            bsdf.inputs['Roughness'].default_value = 1
            links.new(attribute(nodes, links), bsdf.inputs['Base Color'])
        else:
            plain(mat, kind == 'glow', glow)
        obj.data.materials.clear()
        obj.data.materials.append(mat)


def bounds(objects):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in objects:
        if o.type != 'MESH':
            continue
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            for i in range(3):
                lo[i] = min(lo[i], w[i])
                hi[i] = max(hi[i], w[i])
    return lo, hi


def dusk():
    scene = bpy.context.scene
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (0.12, 0.14, 0.24, 1)
    bg.inputs[1].default_value = 0.5
    sun = bpy.data.objects.get('_look_sun')
    sun.data.energy = 0.5
    sun.data.color = (0.6, 0.65, 1.0)


def main():
    gen = sys.argv[1]
    out = sys.argv[2]
    names = sys.argv[3].split(',')
    views = sys.argv[4] if len(sys.argv) > 4 else 'tf'
    size = int(sys.argv[5]) if len(sys.argv) > 5 else 640
    lod = int(sys.argv[6]) if len(sys.argv) > 6 else 0
    tex_dir = os.path.join(gen, 'btex')
    tiles = []
    for name in names:
        for view in views:
            reset()
            objects = load_glb(os.path.join(gen, name + '.glb') if '/' in name else os.path.join(gen, 'buildings', f'{name}_lod{lod}.glb'))
            dress(objects, tex_dir, 2.5 if view == 'd' else 0.9)
            lo, hi = bounds(objects)
            treeshots.GROUND_Z = -12 if '/' in name else 0
            mid = (lo + hi) / 2
            r = max(hi.x - lo.x, hi.y - lo.y, hi.z * 0.9)
            front = hi.y + 2.5
            person = (lo.x * 0.25 + 1.2, front - 1.0)
            if '/' in name:
                # A scene: the gate at the origin, the outside toward +Y; `size` of the view from the scene's name.
                reach = 140 if 'stone' in name else 30 if 'bamboo' in name else 32 if 'things' in name else 80
                if 'things' in name:
                    eye = (0, 34, 9) if view != 'f' else (-14, 9, 2.4)
                    target = (0, 3, 0.5) if view != 'f' else (-17, 0, 1.2)
                    lens = 24
                elif view in 'td':
                    eye = (reach * 0.42, reach * 0.62, reach * 0.24)
                    target = (0, reach * 0.05, reach * 0.03)
                    lens = 35
                elif view == 'f':
                    eye = (reach * 0.12, reach * 0.3, 2.2 + reach * 0.01)
                    target = (0, 0, reach * 0.06)
                    lens = 30
                else:
                    eye = (-reach * 0.4, -reach * 0.5, reach * 0.3)
                    target = (0, 0, 2)
                    lens = 35
                person = (3, reach * 0.12)
            elif view in 'td':
                eye = (mid.x + r * 0.95, mid.y + r * 1.35, r * 0.62 + 1)
                target = (mid.x, mid.y, hi.z * 0.4)
                lens = 35
            elif view == 'f':
                eye = (mid.x + 2.5, front + max(7.0, (hi.x - lo.x) * 0.45), 1.65)
                target = (mid.x, hi.y, min(hi.z * 0.4, 4.0))
                lens = 28
            else:
                eye = (mid.x - r * 1.2, mid.y - r * 1.1, r * 0.5 + 1)
                target = (mid.x, mid.y, hi.z * 0.4)
                lens = 35
            path = f'{out}.{name.replace("/", "_")}.{view}.png'
            if view == 'd':
                # dusk() needs the sun that shoot() makes: shoot once to set the scene, then again.
                bpy.context.scene.render.resolution_percentage = 100
            shoot(path, eye, target, (size, int(size * 0.8)), 24 if view != 'd' else 32, lens=lens, sun_yaw=-35, sun_pitch=50, person=person) if view != 'd' else None
            if view == 'd':
                shoot_dusk(path, eye, target, size, lens, person)
            tiles.append(path)
    columns = len(views)
    rows = (len(tiles) + columns - 1) // columns
    w, h = size, int(size * 0.8)
    sheet = Image.new('RGB', (w * columns, h * rows))
    for i, t in enumerate(tiles):
        sheet.paste(Image.open(t), ((i % columns) * w, (i // columns) * h))
        os.remove(t)
    sheet.save(out)
    print('sheet:', out)


def shoot_dusk(path, eye, target, size, lens, person):
    import treeshots
    original = bpy.ops.render.render

    # Set the scene up as shoot() does, but darken it before rendering.
    class _Hold:
        pass
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    treeshots_shoot = treeshots.shoot
    # shoot() renders at its end; render once quickly at 1 sample to build the scene, then darken and render properly.
    scene.cycles.samples = 1
    treeshots_shoot(path, eye, target, (64, 51), 1, lens=lens, sun_yaw=-35, sun_pitch=12, person=person)
    dusk()
    scene.cycles.samples = 40
    scene.render.resolution_x, scene.render.resolution_y = size, int(size * 0.8)
    bpy.ops.render.render(write_still=True)


if __name__ == '__main__':
    main()
