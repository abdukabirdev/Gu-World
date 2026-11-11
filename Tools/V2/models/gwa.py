"""
The game's own animation file (read by Scripts/Actors/RigAnimator.cs), little endian:

  "GWA2"
  int bones, bones × { name, rest rotation xyzw (float), rest position xyz (float) }
  int mover                      the bone whose position is animated (the hips)
  int clips, clips × { name, float fps, int frames, byte loop,
                       frames × { mover position xyz (float), bones × rotation xyzw (int16 / 32767) } }

Rotations and positions are glTF node locals (right-handed, Y up); the game turns them to
Unity's. A name is a byte of length and UTF-8.
"""
import os
import struct


def _text(value):
    raw = value.encode('utf-8')
    return struct.pack('<B', len(raw)) + raw


def write(path, bones, mover, clips):
    """bones: [(name, (x, y, z, w), (x, y, z))]; clips: [(name, fps, loop, [(position, [quaternion xyzw per bone])])]."""
    out = bytearray(b'GWA2')
    out += struct.pack('<i', len(bones))
    for name, rotation, position in bones:
        out += _text(name) + struct.pack('<4f3f', *rotation, *position)
    out += struct.pack('<i', mover)
    out += struct.pack('<i', len(clips))
    for name, fps, loop, frames in clips:
        out += _text(name) + struct.pack('<fiB', fps, len(frames), 1 if loop else 0)
        for position, rotations in frames:
            out += struct.pack('<3f', *position)
            for q in rotations:
                out += struct.pack('<4h', *(int(round(max(-1.0, min(1.0, c)) * 32767)) for c in q))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'wb') as file:
        file.write(out)
    return len(out)
