"""Shared helpers for the model scripts (Blender as a Python module: pip install bpy)."""
import math
import bpy
from mathutils import Vector


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def load_glb(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path, guess_original_bind_pose=False)
    return [o for o in bpy.data.objects if o not in before]


def armature_of(objects):
    return next(o for o in objects if o.type == 'ARMATURE')


def meshes_of(objects):
    return [o for o in objects if o.type == 'MESH']


def render(path, target=(0, 0, 0.95), distance=3.2, yaw=0, height=1.1, size=(420, 620), samples=12):
    """A quick look at the scene: Cycles on the CPU, a sun and a soft sky. Yaw 0 looks at the model's front."""
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.filepath = path
    world = bpy.data.worlds.new('w') if not scene.world else scene.world
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.6, 0.7, 1)
    world.node_tree.nodes['Background'].inputs[1].default_value = 0.9

    for old in [o for o in bpy.data.objects if o.name.startswith('_look')]:
        bpy.data.objects.remove(old)

    sun = bpy.data.objects.new('_look_sun', bpy.data.lights.new('_look_sun', 'SUN'))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), 0, math.radians(yaw + 30))
    scene.collection.objects.link(sun)
    cam = bpy.data.objects.new('_look_cam', bpy.data.cameras.new('_look_cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    # In Blender the glTF's front (+Z) is −Y.
    a = math.radians(yaw)
    eye = Vector((target[0] + distance * math.sin(a), target[1] - distance * math.cos(a), height))
    cam.location = eye
    look = Vector(target) - eye
    cam.rotation_euler = look.to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = 50
    bpy.ops.render.render(write_still=True)
