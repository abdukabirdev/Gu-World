"""Prepares the rigged GU hero (exported from the Gu Rig editor) for the game.

    python prepare_glb.py gu_hero_rigged.glb ../../Assets/GuWorld/Resources/Models/gu-hero.glb

- names the meshes in English (GuHero_Face, GuHero_Body, GuHero_Hands);
- the face decal (the portrait laid over the face) is see-through at its edges: alphaMode BLEND;
- drops the animation clips (the game poses the body itself, from the Mannequin) and every
  buffer view nothing refers to any more, so the file shrinks.
"""
import json
import struct
import sys

src, dst = sys.argv[1], sys.argv[2]
data = open(src, 'rb').read()
json_len = struct.unpack('<I', data[12:16])[0]
gltf = json.loads(data[20:20 + json_len])
bin_start = 20 + json_len
bin_len = struct.unpack('<I', data[bin_start:bin_start + 4])[0]
blob = data[bin_start + 8:bin_start + 8 + bin_len]

names = {'Лицо (фото)': 'GuHero_Face', 'AI-персонаж (TRELLIS)': 'GuHero_Body', 'Кисти с пальцами': 'GuHero_Hands'}
for node in gltf['nodes']:
    if node.get('name') in names:
        node['name'] = names[node['name']]
        gltf['meshes'][node['mesh']]['name'] = node['name']
        if node['name'] == 'GuHero_Face':
            material = gltf['materials'][gltf['meshes'][node['mesh']]['primitives'][0]['material']]
            material['alphaMode'] = 'BLEND'
            material['name'] = 'GuHero_Face'
        if node['name'] == 'GuHero_Hands':
            gltf['materials'][gltf['meshes'][node['mesh']]['primitives'][0]['material']]['name'] = 'GuHero_Hands'
gltf['scenes'][0]['name'] = 'GuHero'
gltf.pop('animations', None)

# Keep only the accessors and buffer views still referred to, packed again.
used_acc = set()
for mesh in gltf['meshes']:
    for p in mesh['primitives']:
        used_acc.update(p['attributes'].values())
        if 'indices' in p:
            used_acc.add(p['indices'])
for skin in gltf.get('skins', []):
    if 'inverseBindMatrices' in skin:
        used_acc.add(skin['inverseBindMatrices'])
acc_map = {}
accessors = []
for i, a in enumerate(gltf['accessors']):
    if i in used_acc:
        acc_map[i] = len(accessors)
        accessors.append(a)
used_bv = {a['bufferView'] for a in accessors} | {im['bufferView'] for im in gltf.get('images', [])}
bv_map = {}
views = []
out = bytearray()
for i, bv in enumerate(gltf['bufferViews']):
    if i not in used_bv:
        continue
    while len(out) % 4:
        out.append(0)
    start = bv.get('byteOffset', 0)
    chunk = blob[start:start + bv['byteLength']]
    nv = dict(bv)
    nv['byteOffset'] = len(out)
    out += chunk
    bv_map[i] = len(views)
    views.append(nv)
for a in accessors:
    a['bufferView'] = bv_map[a['bufferView']]
for im in gltf.get('images', []):
    im['bufferView'] = bv_map[im['bufferView']]
for mesh in gltf['meshes']:
    for p in mesh['primitives']:
        p['attributes'] = {k: acc_map[v] for k, v in p['attributes'].items()}
        if 'indices' in p:
            p['indices'] = acc_map[p['indices']]
for skin in gltf.get('skins', []):
    if 'inverseBindMatrices' in skin:
        skin['inverseBindMatrices'] = acc_map[skin['inverseBindMatrices']]
gltf['accessors'] = accessors
gltf['bufferViews'] = views
while len(out) % 4:
    out.append(0)
gltf['buffers'] = [{'byteLength': len(out)}]

js = json.dumps(gltf, ensure_ascii=False, separators=(',', ':')).encode('utf-8')
while len(js) % 4:
    js += b' '
total = 12 + 8 + len(js) + 8 + len(out)
with open(dst, 'wb') as f:
    f.write(struct.pack('<III', 0x46546C67, 2, total))
    f.write(struct.pack('<II', len(js), 0x4E4F534A))
    f.write(js)
    f.write(struct.pack('<II', len(out), 0x004E4942))
    f.write(out)
print(dst, total, 'bytes')
