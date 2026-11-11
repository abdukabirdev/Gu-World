"""
The wolves and foxes: the Khronos sample Fox (model CC0 by PixelMannen; rigging and animation
CC BY 4.0 by tomkranis; glTF conversion CC BY 4.0 by AsoboStudio and scurest).

Copies the model to Resources/Models/canid.glb, bakes its three clips (Survey, Walk, Run) to
Resources/Anim/canid.bytes in the game's format (gwa.py) straight from the file's own
samplers, and dyes its texture for the grey, storm and blood wolves (Resources/Looks).

  python canid.py path/to/Fox.glb
"""
import io
import json
import math
import os
import shutil
import struct
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gwa

HERE = os.path.dirname(os.path.abspath(__file__))
RESOURCES = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources'))
FPS = 30
NAMES = {'Survey': 'idle', 'Walk': 'walk', 'Run': 'run'}


def read_glb(path):
    with open(path, 'rb') as file:
        file.read(12)
        length, _ = struct.unpack('<I4s', file.read(8))
        data = json.loads(file.read(length))
        length, _ = struct.unpack('<I4s', file.read(8))
        binary = file.read(length)
    return data, binary


def accessor(data, binary, index):
    acc = data['accessors'][index]
    view = data['bufferViews'][acc['bufferView']]
    width = {'SCALAR': 1, 'VEC3': 3, 'VEC4': 4}[acc['type']]
    assert acc['componentType'] == 5126
    start = view.get('byteOffset', 0) + acc.get('byteOffset', 0)
    stride = view.get('byteStride', width * 4)
    out = np.zeros((acc['count'], width), dtype=np.float32)
    for i in range(acc['count']):
        out[i] = struct.unpack_from(f'<{width}f', binary, start + i * stride)
    return out


def slerp(a, b, t):
    dot = float(np.dot(a, b))
    if dot < 0:
        b, dot = -b, -dot
    if dot > 0.9995:
        q = a + (b - a) * t
    else:
        angle = math.acos(dot)
        q = (math.sin((1 - t) * angle) * a + math.sin(t * angle) * b) / math.sin(angle)
    return q / np.linalg.norm(q)


def sample(times, values, t, rotation):
    if t <= times[0]:
        return values[0]
    if t >= times[-1]:
        return values[-1]
    i = int(np.searchsorted(times, t)) - 1
    k = (t - times[i]) / max(1e-9, times[i + 1] - times[i])
    return slerp(values[i], values[i + 1], k) if rotation else values[i] + (values[i + 1] - values[i]) * k


def bake(source):
    data, binary = read_glb(source)
    nodes = data['nodes']
    joints = data['skins'][0]['joints']
    names = [nodes[j]['name'] for j in joints]
    rest_q = [np.array(nodes[j].get('rotation', [0, 0, 0, 1]), dtype=np.float32) for j in joints]
    rest_t = [np.array(nodes[j].get('translation', [0, 0, 0]), dtype=np.float32) for j in joints]
    mover = names.index('b_Hip_01')
    clips = []
    for animation in data['animations']:
        tracks = {}
        length = 0.0
        for channel in animation['channels']:
            sampler = animation['samplers'][channel['sampler']]
            assert sampler.get('interpolation', 'LINEAR') == 'LINEAR'
            times = accessor(data, binary, sampler['input'])[:, 0]
            values = accessor(data, binary, sampler['output'])
            tracks[(channel['target']['node'], channel['target']['path'])] = (times, values)
            length = max(length, float(times[-1]))
        count = max(2, round(length * FPS))
        frames = []
        for frame in range(count):
            t = frame / count * length
            position = rest_t[mover]
            track = tracks.get((joints[mover], 'translation'))
            if track:
                position = sample(track[0], track[1], t, False)
            rotations = []
            for index, joint in enumerate(joints):
                track = tracks.get((joint, 'rotation'))
                rotations.append(tuple(sample(track[0], track[1], t, True)) if track else tuple(rest_q[index]))
            frames.append((tuple(float(v) for v in position), rotations))
        clips.append((NAMES.get(animation['name'], animation['name'].lower()), FPS, True, frames))
    bones = [(name, tuple(float(v) for v in rest_q[i]), tuple(float(v) for v in rest_t[i])) for i, name in enumerate(names)]
    out = os.path.join(RESOURCES, 'Anim', 'canid.bytes')
    size = gwa.write(out, bones, mover, clips)
    print(f'canid.bytes: {[c[0] + " " + str(len(c[3])) for c in clips]}, {len(bones)} bones, {size // 1024} KB')

    # The texture and its dyes.
    image = data['images'][0]
    view = data['bufferViews'][image['bufferView']]
    texture = Image.open(io.BytesIO(binary[view.get('byteOffset', 0):view.get('byteOffset', 0) + view['byteLength']])).convert('RGB')
    looks = os.path.join(RESOURCES, 'Looks')
    os.makedirs(looks, exist_ok=True)
    rgb = np.asarray(texture, dtype=np.float32) / 255
    grey = (0.3 * rgb[..., 0] + 0.59 * rgb[..., 1] + 0.11 * rgb[..., 2])[..., None]
    warm = np.clip(rgb[..., 0:1] - rgb[..., 2:3], 0, 1) * 2.2   # where the fox is orange (not its white or black)

    def save(name, colour):
        Image.fromarray((np.clip(colour, 0, 1) * 255).astype(np.uint8), 'RGB').save(os.path.join(looks, name + '.png'))

    save('canid_fox', rgb)
    save('canid_grey', grey * (1 - warm) + grey * np.array([0.62, 0.64, 0.70]) * 0.9 * warm)
    save('canid_storm', grey * (1 - warm) * np.array([0.9, 0.92, 1.0]) + grey * np.array([0.42, 0.40, 0.95]) * 1.25 * warm)
    save('canid_blood', grey * (1 - warm) * np.array([0.75, 0.45, 0.45]) + grey * np.array([0.85, 0.10, 0.08]) * 1.2 * warm)
    shutil.copyfile(source, os.path.join(RESOURCES, 'Models', 'canid.glb'))
    print('canid.glb and 4 looks written')


if __name__ == '__main__':
    bake(sys.argv[1])
