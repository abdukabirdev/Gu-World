"""
Renders generated trees in Blender (Cycles) to judge them without the engine: the whole tree, its
foot, and up among its limbs. Reads the .glb the simulator wrote (Sim `gen export`:
Logs/V2/gen/trees/<name>_raw.glb with Logs/V2/gen/tex/) and builds the materials as the game's
shaders do: bark with moss laid over it by the vertices' weight and the moss's own height, shaded
by the vertices; foliage cut out by its alpha, tinted by the vertices, a little light through it.

  python treeshots.py <gen_dir> <out.png> name[,name...] [views] [size]

views: any of w (whole), f (foot), c (crown), u (looking up from the ground); default "wfc".
"""
import math
import os
import sys

import bpy
from mathutils import Vector
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_glb, reset


def image(path):
    return bpy.data.images.load(path, check_existing=True)


def attribute(nodes, links):
    """The vertices' colour as the generator wrote it (the file holds it linear; this undoes that)."""
    attr = nodes.new('ShaderNodeVertexColor')
    attr.layer_name = 'Color'
    gamma = nodes.new('ShaderNodeGamma')
    gamma.inputs[1].default_value = 1 / 2.2
    links.new(attr.outputs['Color'], gamma.inputs[0])
    return gamma.outputs[0]


def bark_material(mat, tex_dir, name):
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.95
    uv = nodes.new('ShaderNodeUVMap')
    bark = nodes.new('ShaderNodeTexImage')
    bark.image = image(os.path.join(tex_dir, name + '.jpg'))
    links.new(uv.outputs[0], bark.inputs[0])
    scale = nodes.new('ShaderNodeMapping')
    scale.inputs['Scale'].default_value = (1.7, 1.7, 1.7)
    links.new(uv.outputs[0], scale.inputs[0])
    moss = nodes.new('ShaderNodeTexImage')
    moss.image = image(os.path.join(tex_dir, 'moss.jpg'))
    links.new(scale.outputs[0], moss.inputs[0])
    moss_h = nodes.new('ShaderNodeTexImage')
    moss_h.image = image(os.path.join(tex_dir, 'moss_h.jpg'))
    moss_h.image.colorspace_settings.name = 'Non-Color'
    links.new(scale.outputs[0], moss_h.inputs[0])
    sep = nodes.new('ShaderNodeSeparateColor')
    links.new(attribute(nodes, links), sep.inputs[0])
    # factor = clamp((weight * 2 - 1 + height) * 2.5)
    a = nodes.new('ShaderNodeMath'); a.operation = 'MULTIPLY_ADD'; a.inputs[1].default_value = 2; a.inputs[2].default_value = -1
    links.new(sep.outputs[0], a.inputs[0])
    b = nodes.new('ShaderNodeMath'); b.operation = 'ADD'
    links.new(a.outputs[0], b.inputs[0]); links.new(moss_h.outputs[0], b.inputs[1])
    c = nodes.new('ShaderNodeMath'); c.operation = 'MULTIPLY'; c.inputs[1].default_value = 2.5; c.use_clamp = True
    links.new(b.outputs[0], c.inputs[0])
    blend = nodes.new('ShaderNodeMixRGB')
    links.new(c.outputs[0], blend.inputs[0]); links.new(bark.outputs[0], blend.inputs[1]); links.new(moss.outputs[0], blend.inputs[2])
    shade = nodes.new('ShaderNodeMixRGB'); shade.blend_type = 'MULTIPLY'; shade.inputs[0].default_value = 1
    links.new(blend.outputs[0], shade.inputs[1]); links.new(sep.outputs[1], shade.inputs[2])
    links.new(shade.outputs[0], bsdf.inputs['Base Color'])
    normal_path = os.path.join(tex_dir, name + '_n.png')
    if os.path.exists(normal_path):
        n = nodes.new('ShaderNodeTexImage')
        n.image = image(normal_path)
        n.image.colorspace_settings.name = 'Non-Color'
        links.new(uv.outputs[0], n.inputs[0])
        nm = nodes.new('ShaderNodeNormalMap')
        nm.inputs[0].default_value = 1.6
        links.new(n.outputs[0], nm.inputs[1])
        links.new(nm.outputs[0], bsdf.inputs['Normal'])
    links.new(bsdf.outputs[0], out.inputs[0])


def leaf_material(mat, tex_dir, name):
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image(os.path.join(tex_dir, name + '.png'))
    tint = nodes.new('ShaderNodeMixRGB'); tint.blend_type = 'MULTIPLY'; tint.inputs[0].default_value = 1
    links.new(tex.outputs[0], tint.inputs[1]); links.new(attribute(nodes, links), tint.inputs[2])
    diffuse = nodes.new('ShaderNodeBsdfDiffuse')
    through = nodes.new('ShaderNodeBsdfTranslucent')
    links.new(tint.outputs[0], diffuse.inputs[0]); links.new(tint.outputs[0], through.inputs[0])
    both = nodes.new('ShaderNodeMixShader'); both.inputs[0].default_value = 0.3
    links.new(diffuse.outputs[0], both.inputs[1]); links.new(through.outputs[0], both.inputs[2])
    clear = nodes.new('ShaderNodeBsdfTransparent')
    cut = nodes.new('ShaderNodeMath'); cut.operation = 'GREATER_THAN'; cut.inputs[1].default_value = 0.45
    links.new(tex.outputs['Alpha'], cut.inputs[0])
    final = nodes.new('ShaderNodeMixShader')
    links.new(cut.outputs[0], final.inputs[0]); links.new(clear.outputs[0], final.inputs[1]); links.new(both.outputs[0], final.inputs[2])
    links.new(final.outputs[0], out.inputs[0])


def plain_material(mat, glow):
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    if glow:
        e = nodes.new('ShaderNodeEmission')
        e.inputs[0].default_value = (0.25, 1, 0.7, 1)
        e.inputs[1].default_value = 6
        links.new(e.outputs[0], out.inputs[0])
        return
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.8
    links.new(attribute(nodes, links), bsdf.inputs['Base Color'])
    links.new(bsdf.outputs[0], out.inputs[0])


def dress(objects, tex_dir):
    for obj in objects:
        if obj.type != 'MESH':
            continue
        name = obj.name.split('.')[0]
        kind = name.split('_', 1)[1] if '_' in name else name
        mat = bpy.data.materials.new(name)
        if kind.startswith('bark_'):
            bark_material(mat, tex_dir, kind[5:])
        elif kind.startswith('leaf_'):
            leaf_material(mat, tex_dir, kind[5:])
        else:
            plain_material(mat, kind == 'glow')
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        for poly in obj.data.polygons:
            poly.use_smooth = True


def shoot(path, eye, target, size, samples, lens=35, sun_yaw=35, sun_pitch=48, person=None):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 4
    scene.cycles.transparent_max_bounces = 24
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.filepath = path
    scene.view_settings.view_transform = 'Standard'
    world = scene.world or bpy.data.worlds.new('w')
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (0.5, 0.62, 0.8, 1)
    bg.inputs[1].default_value = 1.0
    for old in [o for o in bpy.data.objects if o.name.startswith('_look')]:
        bpy.data.objects.remove(old)
    sun = bpy.data.objects.new('_look_sun', bpy.data.lights.new('_look_sun', 'SUN'))
    sun.data.energy = 4.0
    sun.data.angle = 0.03
    sun.rotation_euler = (math.radians(sun_pitch), 0, math.radians(sun_yaw))
    scene.collection.objects.link(sun)
    # The ground: a wide dark meadow.
    bpy.ops.mesh.primitive_plane_add(size=900, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = '_look_ground'
    gm = bpy.data.materials.new('_look_ground')
    gm.use_nodes = True
    gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.09, 0.13, 0.05, 1)
    gm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1
    ground.data.materials.append(gm)
    # A person for scale: 1.75 m, standing by the foot.
    if person is not None:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.5, location=(person[0], person[1], 0.75))
        body = bpy.context.active_object
        body.name = '_look_person'
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.14, location=(person[0], person[1], 1.62))
        head = bpy.context.active_object
        head.name = '_look_person_head'
        pm = bpy.data.materials.new('_look_person')
        pm.use_nodes = True
        pm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.75, 0.1, 0.08, 1)
        body.data.materials.append(pm)
        head.data.materials.append(pm)
    cam = bpy.data.objects.new('_look_cam', bpy.data.cameras.new('_look_cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = eye
    cam.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = lens
    cam.data.clip_end = 3000
    bpy.ops.render.render(write_still=True)


def main():
    gen = sys.argv[1]
    out = sys.argv[2]
    names = sys.argv[3].split(',')
    views = sys.argv[4] if len(sys.argv) > 4 else 'wfc'
    size = (int(sys.argv[5]), int(int(sys.argv[5]) * 1.25)) if len(sys.argv) > 5 else (480, 600)
    tex_dir = os.path.join(gen, 'tex')
    tiles = []
    for name in names:
        reset()
        path = os.path.join(gen, 'trees', name + ('' if name.endswith('.glb') else '_raw.glb'))
        objects = load_glb(path)
        dress(objects, tex_dir)
        top = max((obj.matrix_world @ Vector(c)).z for obj in objects if obj.type == 'MESH' for c in obj.bound_box)
        wide = max(max(abs((obj.matrix_world @ Vector(c)).x), abs((obj.matrix_world @ Vector(c)).y)) for obj in objects if obj.type == 'MESH' for c in obj.bound_box)
        h = top
        reach = max(h * 0.5, wide)
        # glTF's front (+Z) is Blender's −Y; the tree's +X (its lean) is Blender's −X after the mirror.
        for view in views:
            shot = f'{out}.{name}.{view}.png'
            if view == 'w':
                d = max(h * 1.5, reach * 2.6)
                shoot(shot, (d * 0.35, -d, h * 0.42), (0, 0, h * 0.48), size, 20, 35, person=(wide * 0.25 + 2, -wide * 0.25 - 2))
            elif view == 'f':
                d = max(6, h * 0.22)
                shoot(shot, (d * 0.55, -d, 1.7 + h * 0.01), (0, 0, h * 0.075 + 1), size, 24, 28, person=(d * 0.42, -d * 0.45))
            elif view == 'c':
                shoot(shot, (reach * 0.45, -reach * 0.7, h * 0.5), (0, 0, h * 0.62), size, 24, 28)
            elif view == 'u':
                shoot(shot, (h * 0.07 + 2, -h * 0.07 - 2, 1.7), (0, 0, h * 0.75), size, 24, 20)
            tiles.append(shot)
    columns = len(views)
    rows = len(names)
    sheet = Image.new('RGB', (size[0] * columns, size[1] * rows))
    for i, shot in enumerate(tiles):
        sheet.paste(Image.open(shot), ((i % columns) * size[0], (i // columns) * size[1]))
        os.remove(shot)
    sheet.save(out)
    print('sheet:', out)


if __name__ == '__main__':
    main()
