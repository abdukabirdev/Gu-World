"""
Writes Core/Gen/People/Skeleton.cs: the hero's skeleton (the bones the animations move and
their parents) as the generated people are built on — names, parents, each bone's rest turn and
offset from its parent as the GLB has them (glTF's axes), and where each stands at rest in the
model (the engine's axes: x mirrored). The animations (human.bytes) are made for these bones, so
a body built on them, however its proportions are scaled, moves with them.

  python skeleton.py
"""
import json, os, struct

HERE = os.path.dirname(os.path.abspath(__file__))
GLB = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Models/gu-npc.glb'))
ANIM = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Anim/human.bytes'))
OUT = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Core/Gen/People/Skeleton.cs'))


def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return (aw*bx + ax*bw + ay*bz - az*by, aw*by - ax*bz + ay*bw + az*bx, aw*bz + ax*by - ay*bx + az*bw, aw*bw - ax*bx - ay*by - az*bz)


def qrot(q, v):
    x, y, z, w = q
    p = qmul(qmul(q, (v[0], v[1], v[2], 0)), (-x, -y, -z, w))
    return p[:3]


with open(GLB, 'rb') as f:
    f.read(12)
    length, _ = struct.unpack('<I4s', f.read(8))
    data = json.loads(f.read(length))
nodes = data['nodes']
parent = {}
for i, n in enumerate(nodes):
    for c in n.get('children', []):
        parent[c] = i
by_name = {n.get('name'): i for i, n in enumerate(nodes)}

raw = open(ANIM, 'rb').read()
count = struct.unpack('<i', raw[4:8])[0]
o = 8
moved = []
for _ in range(count):
    L = raw[o]; moved.append(raw[o + 1:o + 1 + L].decode()); o += 1 + L + 28

# The moved bones, their ancestors up to 'root', and the skirt chains (the robes hang from them).
need = set()
for name in moved + [n['name'] for n in nodes if n.get('name', '').startswith('skirt')] + ['shoulder_L', 'shoulder_R']:
    i = by_name.get(name)
    while i is not None and nodes[i].get('name') != 'Armature':
        need.add(i)
        i = parent.get(i)


def world(i):
    pos = (0.0, 0.0, 0.0); rot = (0.0, 0.0, 0.0, 1.0)
    chain = []
    while i is not None:
        chain.append(i); i = parent.get(i)
    for j in reversed(chain):
        n = nodes[j]
        t = n.get('translation', [0, 0, 0]); r = n.get('rotation', [0, 0, 0, 1]); s = n.get('scale', [1, 1, 1])
        lt = qrot(rot, (t[0], t[1], t[2]))
        pos = (pos[0] + lt[0], pos[1] + lt[1], pos[2] + lt[2])
        rot = qmul(rot, tuple(r))
    return pos


order = []
def visit(i):
    if i in need:
        order.append(i)
    for c in nodes[i].get('children', []):
        visit(c)
visit(by_name['root'])
index = {i: k for k, i in enumerate(order)}

lines = []
for i in order:
    n = nodes[i]
    t = n.get('translation', [0, 0, 0]); r = n.get('rotation', [0, 0, 0, 1])
    w = world(i)
    p = index.get(parent.get(i), -1)
    lines.append('            new Bone("%s", %d, %sf, %sf, %sf, %sf, %sf, %sf, %sf, %sf, %sf, %sf),' % (
        n['name'], p, *['%.6g' % v for v in r], *['%.6g' % v for v in t], '%.6g' % -w[0], '%.6g' % w[1], '%.6g' % w[2]))

src = '''// Made by Tools/V2/models/skeleton.py from gu-npc.glb and human.bytes — do not edit by hand.
namespace GuWorld.Core.Gen.People
{
    /// <summary>One bone of the hero's skeleton.</summary>
    public readonly struct Bone
    {
        public readonly string Name;
        public readonly int Parent;
        /// <summary>The rest turn and offset from the parent, in glTF's axes, as the GLB (and human.bytes) have them.</summary>
        public readonly float Qx, Qy, Qz, Qw, Tx, Ty, Tz;
        /// <summary>Where the bone stands at rest in the model, in the engine's axes (x mirrored from glTF's).</summary>
        public readonly V3 At;

        public Bone(string name, int parent, float qx, float qy, float qz, float qw, float tx, float ty, float tz, float x, float y, float z)
        {
            Name = name;
            Parent = parent;
            Qx = qx; Qy = qy; Qz = qz; Qw = qw;
            Tx = tx; Ty = ty; Tz = tz;
            At = new V3(x, y, z);
        }
    }

    /// <summary>The hero's skeleton (%d bones), parents before children.</summary>
    public static class Skeleton
    {
        public static readonly Bone[] Bones =
        {
%s
        };
    }
}
''' % (len(order), '\n'.join(lines))
open(OUT, 'w').write(src)
print(f'{OUT}: {len(order)} bones')
