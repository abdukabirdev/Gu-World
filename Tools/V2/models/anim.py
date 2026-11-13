"""
Authors the humans' animations in code and bakes them to Resources/Anim/human.bytes, which
the game's own player (Scripts/Actors/RigAnimator.cs) reads. No clips are stored in the GLBs.

A pose is a set of turns of the body's bones about the character's own axes (X to its left,
Y behind it, Z up, as Blender sees the model), each relative to the bone's parent, as if that
parent stood at rest. The twist and helper bones and the skirt follow by the editor's rules.

  python anim.py                 bake
  python anim.py sheet out.png   also render every clip's frames to a contact sheet

The file's format is in gwa.py.
"""
import json
import math
import os
import struct
import sys

import bpy
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gwa
from common import armature_of, load_glb, render, reset

HERE = os.path.dirname(os.path.abspath(__file__))
MODELS = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Models'))
OUT = os.path.normpath(os.path.join(HERE, '../../../Assets/GuWorld/Resources/Anim/human.bytes'))
FPS = 30

X = Vector((1, 0, 0))
Y = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def turn(pitch=0.0, yaw=0.0, roll=0.0):
    """Degrees about the character's left (X), up (Z) and back (Y) axes. For a hanging limb, a
    negative pitch swings it forward; for the spine, a positive pitch bows forward."""
    return (pitch, yaw, roll)


def to_quat(angles):
    pitch, yaw, roll = angles
    return Quaternion(Z, math.radians(yaw)) @ Quaternion(Y, math.radians(roll)) @ Quaternion(X, math.radians(pitch))


def mirror(angles):
    pitch, yaw, roll = angles
    return (pitch, -yaw, -roll)


def lerp(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def ease(t):
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------- Poses

def blank():
    return {'offset': (0.0, 0.0, 0.0), 'fingers_L': 0.15, 'fingers_R': 0.15}


def leg(pose, side, swing, knee, foot=None, spread=0.0, twist=0.0):
    """A leg: the thigh swung forward by `swing` degrees, the knee bent by `knee`, the sole kept level unless told."""
    sign = 1 if side == 'L' else -1
    pose['thigh_' + side] = (-swing, twist * sign, -spread * sign)
    pose['shin_' + side] = (knee, 0, 0)
    pose['foot_' + side] = (swing - knee if foot is None else foot, 0, 0)


def arm(pose, side, swing, out, elbow, twist=0.0, wrist=0.0):
    """An arm: the upper arm swung forward by `swing` and out from the side by `out`, the elbow bent by `elbow`."""
    sign = 1 if side == 'L' else -1
    pose['upperArm_' + side] = (-swing, twist * sign, -out * sign)
    pose['forearm_' + side] = (-elbow, 0, 0)
    pose['hand_' + side] = (-wrist, 0, 0)


# The model was made with its head pushed forward of its chest and the upper back rounded (a
# hunch): every pose straightens it. The upper back opens, the neck comes back over the
# shoulders and the head tips forward again by as much, so the gaze stays where it was.
# Checked side-on in Blender (Docs/V2/rig-posture.png): ears over the shoulders.
POSTURE = {'spine1': -2.0, 'spine2': -4.0, 'chest': -7.0, 'neck': -18.0, 'neck1': -10.0, 'head': 33.0}


def torso(pose, lean=0.0, twist=0.0, side=0.0, head=None):
    """The trunk: the hips lean, the spine carries the twist, the head looks where told (or stays level)."""
    pose['hips'] = (lean * 0.5, twist * 0.25, side * 0.5)
    for name in ('spine', 'spine1', 'spine2', 'chest'):
        pose[name] = (lean * 0.125 + POSTURE.get(name, 0.0), twist * 0.19, side * 0.125)
    pose['neck'] = (POSTURE['neck'], 0, 0)
    pose['neck1'] = (POSTURE['neck1'], 0, 0)
    look = head if head is not None else (-lean * 0.8, -twist * 0.7, -side * 0.6)
    pose['head'] = (look[0] + POSTURE['head'], look[1], look[2])


def stand(crouch=0.0):
    """Standing at ease, knees a little soft; `crouch` (0-1) sinks into a fighting stance."""
    pose = blank()
    bend = 4 + 34 * crouch
    pose['offset'] = (0, 0, -0.012 - 0.11 * crouch)
    torso(pose, lean=2 + 10 * crouch)
    for side in 'LR':
        leg(pose, side, swing=bend * 0.5 + 6 * crouch, knee=bend, spread=2 + 6 * crouch)
        arm(pose, side, swing=4, out=5, elbow=12 + 30 * crouch)
    return pose


def idle(t):
    breath = math.sin(t * 2 * math.pi)
    sway = math.sin(t * 2 * math.pi * 0.5)
    pose = stand()
    torso(pose, lean=2 + 0.8 * breath, side=0.8 * sway)
    pose['offset'] = (0.004 * sway, 0, -0.012 - 0.003 * breath)
    for side in 'LR':
        sign = 1 if side == 'L' else -1
        arm(pose, side, swing=3 + 1.2 * breath, out=5 + 0.6 * breath, elbow=14 + 2 * breath)
    return pose


def gait(t, stride, knee_lift, arm_swing, lean, bounce, elbow, crouch=0.0):
    """One cycle of walking or running: t 0-1, the left foot striking at 0."""
    pose = blank()
    phase = t * 2 * math.pi
    torso(pose, lean=lean, twist=6 * math.sin(phase) * (stride / 30), side=1.5 * math.sin(phase))
    pose['offset'] = (0.012 * math.sin(phase), 0, -0.02 - crouch + bounce * math.cos(2 * phase))
    for side, shift in (('L', 0.0), ('R', math.pi)):
        p = phase + shift
        swing = stride * math.cos(p)
        # The knee folds as the leg comes through from behind and straightens for the strike.
        lift = max(0.0, math.sin(p + math.pi)) ** 1.3
        knee = 6 + knee_lift * lift + 8 * max(0.0, math.cos(p)) ** 6
        foot = None if lift < 0.05 else (swing - knee) + 18 * lift
        leg(pose, side, swing=swing + knee * 0.35, knee=knee, foot=foot, spread=1.5)
        back = -math.cos(p)
        arm(pose, side, swing=arm_swing * back, out=4 + (2 if arm_swing > 30 else 0), elbow=elbow + 0.25 * arm_swing * max(0.0, back), twist=0)
        pose['fingers_' + side] = 0.25 if arm_swing < 30 else 0.7
    return pose


def walk(t):
    return gait(t, stride=24, knee_lift=38, arm_swing=18, lean=3, bounce=0.012, elbow=14)


def run(t):
    return gait(t, stride=38, knee_lift=78, arm_swing=46, lean=13, bounce=0.03, elbow=72, crouch=0.03)


def jump(t):
    pose = blank()
    pose['free'] = True
    torso(pose, lean=6 - 10 * t)
    leg(pose, 'L', swing=42 - 10 * t, knee=70 - 20 * t, foot=-25)
    leg(pose, 'R', swing=8, knee=30, foot=-30)
    arm(pose, 'L', swing=-20 + 60 * t, out=22, elbow=40)
    arm(pose, 'R', swing=30 + 50 * t, out=22, elbow=40)
    return pose


def fall(t):
    wave = math.sin(t * 2 * math.pi)
    pose = blank()
    pose['free'] = True
    torso(pose, lean=4)
    leg(pose, 'L', swing=22 + 4 * wave, knee=38 + 6 * wave, foot=-18)
    leg(pose, 'R', swing=4 - 4 * wave, knee=24 - 4 * wave, foot=-22)
    arm(pose, 'L', swing=18, out=48 + 6 * wave, elbow=28)
    arm(pose, 'R', swing=12, out=52 - 6 * wave, elbow=24)
    pose['fingers_L'] = pose['fingers_R'] = 0.1
    return pose


def roll_over(t):
    """A forward roll, tucked: the body turns once over its own middle."""
    tuck = math.sin(min(1.0, t * 1.15) * math.pi) ** 0.6
    pose = blank()
    pose['free'] = True
    spin = 360 * ease(min(1.0, t / 0.86))
    torso(pose, lean=58 * tuck, head=(34 * tuck, 0, 0))
    pose['hips'] = (pose['hips'][0] + spin, 0, 0)
    pose['offset'] = (0, 0, -0.45 * tuck)
    for side in 'LR':
        leg(pose, side, swing=18 + 92 * tuck, knee=8 + 118 * tuck, foot=-10 * tuck, spread=6)
        arm(pose, side, swing=10 + 54 * tuck, out=10, elbow=20 + 92 * tuck)
        pose['fingers_' + side] = 0.6
    return pose


def climb(t):
    """Hand over hand up a trunk: one arm reaching while the other pulls, the opposite knee lifting, the face turned up."""
    phase = t * 2 * math.pi
    pose = blank()
    pose['free'] = True
    torso(pose, lean=-3, twist=6 * math.sin(phase), head=(-22, 0, 0))
    for side, shift in (('L', 0.0), ('R', math.pi)):
        reach = 0.5 + 0.5 * math.sin(phase + shift)
        arm(pose, side, swing=92 + 62 * reach, out=16, elbow=98 - 66 * reach, wrist=-18)
        leg(pose, side, swing=16 + 54 * (1 - reach), knee=28 + 72 * (1 - reach), foot=-12, spread=11)
        pose['fingers_' + side] = 0.7
    pose['offset'] = (0, 0, 0.02 * math.sin(2 * phase))
    return pose


def keyed(keys):
    """A clip from key poses: [(time 0-1, pose)], eased between."""
    def at(t):
        for (t0, a), (t1, b) in zip(keys, keys[1:]):
            if t <= t1:
                k = ease((t - t0) / max(1e-6, t1 - t0))
                names = set(a) | set(b)
                out = {}
                for name in names:
                    if name == 'free':
                        out[name] = a.get(name, False) or b.get(name, False)
                        continue
                    va = a.get(name, (0, 0, 0) if name not in ('fingers_L', 'fingers_R') else 0.15)
                    vb = b.get(name, (0, 0, 0) if name not in ('fingers_L', 'fingers_R') else 0.15)
                    out[name] = va + (vb - va) * k if isinstance(va, float) else lerp(va, vb, k)
                return out
        return keys[-1][1]
    return at


def guard():
    """The fighting stance blows start from and come back to."""
    pose = stand(0.45)
    torso(pose, lean=8, twist=-14)
    arm(pose, 'L', swing=42, out=8, elbow=96, wrist=-20)
    arm(pose, 'R', swing=18, out=12, elbow=104, wrist=-20)
    leg(pose, 'L', swing=26, knee=30, spread=5)
    leg(pose, 'R', swing=-10, knee=26, spread=9, twist=-20)
    pose['fingers_L'] = pose['fingers_R'] = 0.3
    return pose


def palm(side, reach, twist, lunge=0.0):
    """A palm strike with one hand, the other drawn back to the hip."""
    other = 'R' if side == 'L' else 'L'
    sign = 1 if side == 'L' else -1
    pose = stand(0.5)
    torso(pose, lean=12 + 6 * lunge, twist=-twist * sign)
    arm(pose, side, swing=reach, out=4, elbow=6, wrist=-55)
    arm(pose, other, swing=-18, out=10, elbow=112, wrist=-10)
    front, rear = ('L', 'R')
    leg(pose, front, swing=34 + 10 * lunge, knee=44 + 8 * lunge, spread=5)
    leg(pose, rear, swing=-22 - 8 * lunge, knee=16, spread=8, twist=-18)
    pose['offset'] = (0, -0.06 - 0.1 * lunge, pose['offset'][2] - 0.03 * lunge)
    pose['fingers_' + side] = 0.02
    pose['fingers_' + other] = 0.85
    return pose


def wind(side, twist):
    """Drawing the hand back before a strike."""
    other = 'R' if side == 'L' else 'L'
    sign = 1 if side == 'L' else -1
    pose = guard()
    torso(pose, lean=8, twist=twist * sign)
    arm(pose, side, swing=-26, out=14, elbow=118, wrist=-30)
    arm(pose, other, swing=48, out=6, elbow=70, wrist=-20)
    pose['fingers_' + side] = 0.1
    return pose


def both_palms():
    """The combo's last blow: both palms driven forward with a step."""
    pose = stand(0.6)
    torso(pose, lean=22)
    for side in 'LR':
        arm(pose, side, swing=82, out=-2, elbow=8, wrist=-60)
        pose['fingers_' + side] = 0.02
    leg(pose, 'L', swing=44, knee=56, spread=5)
    leg(pose, 'R', swing=-30, knee=14, spread=8, twist=-16)
    pose['offset'] = (0, -0.2, pose['offset'][2] - 0.04)
    return pose


def gather():
    """Both hands drawn to the hips before the double palm."""
    pose = stand(0.7)
    torso(pose, lean=4)
    for side in 'LR':
        arm(pose, side, swing=-24, out=12, elbow=120, wrist=-20)
        pose['fingers_' + side] = 0.2
    leg(pose, 'L', swing=24, knee=46, spread=5)
    leg(pose, 'R', swing=-4, knee=44, spread=9, twist=-16)
    return pose


def throw_wind():
    """The right hand raised behind the head, a crescent gathering on the fingers."""
    pose = stand(0.35)
    torso(pose, lean=0, twist=34)
    arm(pose, 'R', swing=10, out=96, elbow=104, twist=-40, wrist=10)
    arm(pose, 'L', swing=50, out=14, elbow=40, wrist=-20)
    leg(pose, 'L', swing=22, knee=28, spread=5)
    leg(pose, 'R', swing=-12, knee=24, spread=9, twist=-18)
    pose['fingers_R'] = 0.05
    return pose


def throw_release():
    """The arm cut down and across: the crescent leaves the hand."""
    pose = stand(0.5)
    torso(pose, lean=16, twist=-38)
    arm(pose, 'R', swing=74, out=-12, elbow=14, wrist=-20)
    arm(pose, 'L', swing=-26, out=18, elbow=64)
    leg(pose, 'L', swing=36, knee=46, spread=5)
    leg(pose, 'R', swing=-24, knee=16, spread=8, twist=-18)
    pose['offset'] = (0, -0.08, pose['offset'][2])
    pose['fingers_R'] = 0.0
    return pose


def channel():
    """Hands brought together before the chest: a Gu worked on oneself."""
    pose = stand(0.25)
    torso(pose, lean=4, head=(10, 0, 0))
    for side in 'LR':
        arm(pose, side, swing=52, out=-14, elbow=112, twist=-30, wrist=-10)
        pose['fingers_' + side] = 0.1
    return pose


def channel_open():
    pose = stand(0.15)
    torso(pose, lean=-4, head=(-12, 0, 0))
    for side in 'LR':
        arm(pose, side, swing=20, out=46, elbow=40, wrist=20)
        pose['fingers_' + side] = 0.0
    return pose


def flinch():
    pose = stand(0.3)
    torso(pose, lean=-14, twist=16, head=(-18, 10, 0))
    arm(pose, 'L', swing=34, out=30, elbow=80)
    arm(pose, 'R', swing=-10, out=26, elbow=50)
    pose['offset'] = (0, 0.07, pose['offset'][2])
    return pose


def fallen(share):
    """Falling onto the back: `share` 0 standing, 1 lying."""
    pose = stand(0.2 * (1 - share))
    pose['free'] = True
    back = -90 * ease(share)
    torso(pose, lean=0, head=(8 * share, 12 * share, 0))
    pose['hips'] = (back, 0, 0)
    pose['offset'] = (0, 0.5 * ease(share), -0.80 * ease(share) ** 1.5)
    leg(pose, 'L', swing=30 * math.sin(share * math.pi) + 6 * share, knee=50 * math.sin(share * math.pi) + 14 * share, foot=-20 * share, spread=8 * share)
    leg(pose, 'R', swing=10 * math.sin(share * math.pi), knee=30 * math.sin(share * math.pi) + 4 * share, foot=-24 * share, spread=10 * share)
    arm(pose, 'L', swing=30 * math.sin(share * math.pi), out=20 + 50 * share, elbow=30 - 10 * share)
    arm(pose, 'R', swing=20 * math.sin(share * math.pi), out=24 + 34 * share, elbow=40 - 20 * share)
    pose['fingers_L'] = pose['fingers_R'] = 0.3
    return pose


def sit(t):
    """Kneeling on the heels, hands on the thighs, breathing: the meditation by the spring."""
    breath = math.sin(t * 2 * math.pi)
    pose = blank()
    torso(pose, lean=-1 + 1.2 * breath, head=(5, 0, 0))
    pose['offset'] = (0, 0.1, 0)
    for side in 'LR':
        leg(pose, side, swing=68, knee=150, foot=62, spread=7)
        arm(pose, side, swing=30, out=6, elbow=52 + breath, wrist=24)
        pose['fingers_' + side] = 0.08
    return pose


def slam_up():
    """Both fists raised overhead (a boss's slam, a heavy blow)."""
    pose = stand(0.2)
    torso(pose, lean=-12, head=(-10, 0, 0))
    for side in 'LR':
        arm(pose, side, swing=150, out=14, elbow=40)
        pose['fingers_' + side] = 0.95
    return pose


def slam_down():
    pose = stand(0.95)
    torso(pose, lean=46, head=(-24, 0, 0))
    for side in 'LR':
        arm(pose, side, swing=58, out=2, elbow=12)
        pose['fingers_' + side] = 0.95
    pose['offset'] = (0, -0.1, pose['offset'][2] - 0.05)
    return pose


G = guard()
CLIPS = [
    # name, seconds, loop, pose(t)
    ('idle', 3.0, True, idle),
    ('walk', 1.0, True, walk),
    ('run', 0.64, True, run),
    ('jump', 0.3, False, jump),
    ('fall', 0.8, True, fall),
    ('roll', 0.6, False, roll_over),
    ('attack1', 0.42, False, keyed([(0, G), (0.22, wind('R', 26)), (0.42, palm('R', 78, 30)), (0.6, palm('R', 80, 32)), (1, G)])),
    ('attack2', 0.42, False, keyed([(0, G), (0.22, wind('L', 22)), (0.42, palm('L', 80, 34, 0.3)), (0.6, palm('L', 82, 36, 0.3)), (1, G)])),
    ('attack3', 0.62, False, keyed([(0, G), (0.3, gather()), (0.48, both_palms()), (0.68, both_palms()), (1, G)])),
    ('cast', 0.5, False, keyed([(0, stand(0.2)), (0.32, throw_wind()), (0.52, throw_release()), (0.7, throw_release()), (1, stand(0.1))])),
    ('channel', 0.7, False, keyed([(0, stand(0.1)), (0.3, channel()), (0.55, channel()), (0.75, channel_open()), (1, stand())])),
    ('slam', 0.9, False, keyed([(0, G), (0.4, slam_up()), (0.5, slam_up()), (0.62, slam_down()), (0.8, slam_down()), (1, G)])),
    ('hit', 0.36, False, keyed([(0, stand(0.2)), (0.3, flinch()), (1, stand(0.2))])),
    ('death', 1.1, False, lambda t: fallen(min(1.0, t / 0.8))),
    ('guard', 1.6, True, lambda t: keyed([(0, G), (0.5, dict(G, offset=(0, 0, G['offset'][2] - 0.015))), (1, G)])(t)),
    ('sit', 4.0, True, sit),
    ('climb', 1.0, True, climb),
]

FINGERS = ['index', 'middle', 'ring', 'pinky']
HELPERS = [
    ('upperArmTwist', 'upperArm', True, -0.5), ('elbowHelper', 'forearm', False, 0.5),
    ('forearmTwist1', 'hand', True, 0.33), ('forearmTwist2', 'hand', True, 0.66),
    ('hipHelper', 'thigh', False, 0.5), ('thighTwist', 'thigh', True, -0.5),
    ('kneeHelper', 'shin', False, 0.5), ('shinTwist', 'foot', True, 0.5),
]


# ---------------------------------------------------------------------------- Applying a pose

class Rig:
    def __init__(self, armature):
        self.armature = armature
        self.bones = armature.pose.bones
        self.rest = {b.name: b.bone.matrix_local.to_quaternion() for b in self.bones}
        for bone in self.bones:
            bone.rotation_mode = 'QUATERNION'
        hips = armature.data.bones['hips']
        centre = Vector((hips.head_local.x, hips.head_local.y, 0))
        self.skirt = []
        for bone in self.bones:
            if bone.name.startswith('skirt') and ('_1' in bone.name[-4:] or bone.name.endswith('_1')):
                parts = bone.name.split('_')
                if parts[1] != '1':
                    continue
                away = Vector((bone.bone.head_local.x, bone.bone.head_local.y, 0)) - centre
                if away.length > 1e-4:
                    self.skirt.append((bone.name, away.normalized()))

    def local_delta(self, name, armature_turn):
        """A turn given in the armature's axes, as the bone's own pose rotation."""
        rest = self.rest[name]
        return rest.conjugated() @ armature_turn @ rest

    def apply(self, pose):
        for bone in self.bones:
            bone.rotation_quaternion = Quaternion()
            bone.location = Vector()
        free = pose.get('free', False)
        hips_turn = to_quat(pose.get('hips', (0, 0, 0)))
        for name, value in pose.items():
            if name in ('offset', 'fingers_L', 'fingers_R', 'free') or name not in self.bones:
                continue
            turn_ = to_quat(value)
            if not free and name.startswith('thigh_'):
                # A standing pose gives its legs as the world sees them, whatever the hips do.
                turn_ = hips_turn.conjugated() @ turn_
            self.bones[name].rotation_quaternion = self.local_delta(name, turn_)
        rest = self.rest['hips']
        offset = Vector(pose.get('offset', (0, 0, 0)))
        self.bones['hips'].location = rest.conjugated() @ offset
        if not free:
            # The lower foot stands where it stood at rest: the hips sink or rise to put it there.
            bpy.context.view_layer.update()
            lowest = min(self.bones['foot_' + side].head.z - self.armature.data.bones['foot_' + side].head_local.z for side in 'LR')
            offset.z -= lowest
            pose['offset'] = (offset.x, offset.y, offset.z)
            self.bones['hips'].location = rest.conjugated() @ offset

        # Fingers curl about the axis the hand's knuckles lie on.
        for side in 'LR':
            curl = pose.get('fingers_' + side, 0.15)
            for finger in FINGERS:
                for joint, share in ((1, 70), (2, 80), (3, 60)):
                    name = f'{finger}{joint}_{side}'
                    if name in self.bones:
                        self.bones[name].rotation_quaternion = Quaternion(X, math.radians(share * curl))
            for joint, share in ((1, 20), (2, 35), (3, 40)):
                name = f'thumb{joint}_{side}'
                if name in self.bones:
                    self.bones[name].rotation_quaternion = Quaternion(X, math.radians(share * curl))

        # The editor's drivers for the twist and helper bones.
        for side in 'LR':
            for helper, source, is_roll, share in HELPERS:
                h, s = f'{helper}_{side}', f'{source}_{side}'
                if h not in self.bones or s not in self.bones:
                    continue
                delta = self.bones[s].rotation_quaternion
                if is_roll:
                    twist = Quaternion((delta.w, 0, delta.y, 0))
                    twist = twist.normalized() if twist.magnitude > 1e-5 else Quaternion()
                    self.bones[h].rotation_quaternion = Quaternion().slerp(twist, share) if share >= 0 else Quaternion().slerp(twist.conjugated(), -share)
                else:
                    # A share of the source's bend, seen from the armature, given to the helper.
                    bend = self.rest[s] @ delta @ self.rest[s].conjugated()
                    part = Quaternion().slerp(bend, share)
                    self.bones[h].rotation_quaternion = self.local_delta(h, part)

        # The skirt's panels follow the leg that pushes them out.
        for name, away in self.skirt:
            total = Quaternion()
            for side in 'LR':
                thigh = to_quat(pose.get('thigh_' + side, (0, 0, 0)))
                if not free:
                    thigh = hips_turn.conjugated() @ thigh
                knee = thigh @ Vector((0, 0, -1))
                push = Vector((knee.x, knee.y, 0))
                if push.length < 1e-3:
                    continue
                same = (away.x > 0.02 and side == 'L') or (away.x < -0.02 and side == 'R') or abs(away.x) <= 0.02
                weight = max(0.0, away.dot(push.normalized())) * min(1.0, push.length * 1.6) * (0.9 if same else 0.3)
                total = Quaternion().slerp(thigh, weight) @ total
            self.bones[name].rotation_quaternion = self.local_delta(name, total)
        bpy.context.view_layer.update()


# ---------------------------------------------------------------------------- Baking

def gltf_nodes(path):
    with open(path, 'rb') as file:
        file.read(12)
        length, _ = struct.unpack('<I4s', file.read(8))
        data = json.loads(file.read(length))
    nodes = data['nodes']
    parent = {}
    for index, node in enumerate(nodes):
        for child in node.get('children', []):
            parent[child] = index
    by_name = {node['name']: index for index, node in enumerate(nodes)}

    def rotation(index):
        x, y, z, w = nodes[index].get('rotation', [0, 0, 0, 1])
        return Quaternion((w, x, y, z))

    def world(index):
        chain = Quaternion()
        while index is not None:
            chain = rotation(index) @ chain
            index = parent.get(index)
        return chain

    return nodes, parent, by_name, rotation, world


def to_gltf(q):
    """A rotation in Blender's axes (Z up, −Y front) as glTF's (Y up, +Z front)."""
    return Quaternion((q.w, q.x, q.z, -q.y))


def bake(sheet=None):
    source = os.path.join(MODELS, 'gu-npc.glb')
    reset()
    objects = load_glb(source)
    armature = armature_of(objects)
    rig = Rig(armature)
    nodes, parent, by_name, node_rotation, node_world = gltf_nodes(source)
    names = [b.name for b in rig.bones if b.name in by_name]
    rest_arm = {name: rig.rest[name] for name in names}

    def sample(pose):
        rig.apply(pose)
        # Each bone's turn from rest, in the armature's axes; then as glTF node locals.
        turn_of = {}
        for name in names:
            posed = rig.bones[name].matrix.to_quaternion()
            turn_of[name] = to_gltf(posed @ rest_arm[name].conjugated())
        locals_ = {}
        for name in names:
            index = by_name[name]
            up = parent.get(index)
            up_name = nodes[up]['name'] if up is not None else None
            up_turn = turn_of.get(up_name, Quaternion())
            up_rest = node_world(up) if up is not None else Quaternion()
            locals_[name] = (up_rest.conjugated() @ up_turn.conjugated() @ turn_of[name] @ node_world(index)).normalized()
        hips = by_name['hips']
        offset = Vector(pose.get('offset', (0, 0, 0)))
        offset_gltf = Vector((offset.x, offset.z, -offset.y))
        up_rest = node_world(parent[hips])
        position = Vector(nodes[hips].get('translation', [0, 0, 0])) + up_rest.conjugated() @ offset_gltf
        return locals_, position

    baked = []
    for name, seconds, loop, pose_at in CLIPS:
        frames = max(2, round(seconds * FPS))
        samples = []
        for frame in range(frames):
            t = frame / frames if loop else frame / (frames - 1)
            samples.append(sample(pose_at(t)))
        baked.append((name, loop, samples))

    # Only bones that move in some clip are written.
    rest_local = {name: node_rotation(by_name[name]) for name in names}
    moving = [name for name in names if any(abs(abs(s[0][name].dot(rest_local[name])) - 1) > 1e-5 for _, _, samples in baked for s in samples)]

    bones = [(name, (rest_local[name].x, rest_local[name].y, rest_local[name].z, rest_local[name].w), tuple(nodes[by_name[name]].get('translation', [0, 0, 0]))) for name in moving]
    clips = [(name, FPS, loop, [(tuple(position), [(l[b].x, l[b].y, l[b].z, l[b].w) for b in moving]) for l, position in samples]) for name, loop, samples in baked]
    size = gwa.write(OUT, bones, moving.index('hips'), clips)
    print(f'{os.path.basename(OUT)}: {len(baked)} clips, {len(moving)} bones, {size // 1024} KB')

    if sheet:
        from PIL import Image
        tiles = []
        only = sys.argv[3].split(',') if len(sys.argv) > 3 else None
        for name, seconds, loop, pose_at in CLIPS:
            if only and name not in only:
                continue
            shots = 6 if loop or seconds > 0.5 else 5
            for i in range(shots):
                t = i / shots if loop else i / (shots - 1)
                rig.apply(pose_at(t))
                path = f'{sheet}.{name}.{i}.png'
                render(path, target=(0, -0.1, 0.9), distance=4.6, yaw=62, height=1.2, size=(200, 240), samples=4)
                tiles.append((name, path))
        columns = 6
        rows = (len(tiles) + columns - 1) // columns
        image = Image.new('RGB', (200 * columns, 240 * rows), (40, 40, 44))
        for index, (name, path) in enumerate(tiles):
            image.paste(Image.open(path), ((index % columns) * 200, (index // columns) * 240))
            os.remove(path)
        image.save(sheet)
        print('sheet:', sheet)


if __name__ == '__main__':
    bake(sys.argv[2] if len(sys.argv) > 2 and sys.argv[1] == 'sheet' else None)
