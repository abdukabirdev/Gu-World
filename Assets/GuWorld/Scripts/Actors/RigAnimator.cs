using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// The clips of one kind of body (Resources/Anim/*.bytes, made by Tools/V2/models): for
    /// every frame, the local rotation of each bone that moves and the hips' position. The
    /// file's numbers are glTF's; they are turned to Unity's when a rig is bound.
    /// </summary>
    public class ClipSet
    {
        public class Clip
        {
            public string Name;
            public float Fps;
            public int Frames;
            public bool Loop;
            /// <summary>Frames × bones.</summary>
            public Quaternion[] Rotations;
            public Vector3[] Positions;
            public float Length => Frames / Fps;
        }

        public string[] Bones;
        public Quaternion[] RestRotations;
        public Vector3[] RestPositions;
        public int Mover;
        public readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>();

        static readonly Dictionary<string, ClipSet> loaded = new Dictionary<string, ClipSet>();

        public static ClipSet Load(string name)
        {
            if (loaded.TryGetValue(name, out var set))
            {
                return set;
            }

            var asset = Resources.Load<TextAsset>("Anim/" + name);
            set = asset != null ? Parse(asset.bytes) : null;

            if (set == null)
            {
                Debug.LogError($"GU World: no animations at Resources/Anim/{name}");
            }

            loaded[name] = set;

            return set;
        }

        static string ReadName(BinaryReader reader) => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));

        static ClipSet Parse(byte[] bytes)
        {
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "GWA2")
                {
                    return null;
                }

                var set = new ClipSet();
                var bones = reader.ReadInt32();
                set.Bones = new string[bones];
                set.RestRotations = new Quaternion[bones];
                set.RestPositions = new Vector3[bones];

                for (var i = 0; i < bones; i++)
                {
                    set.Bones[i] = ReadName(reader);
                    set.RestRotations[i] = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    set.RestPositions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }

                set.Mover = reader.ReadInt32();
                var clips = reader.ReadInt32();

                for (var c = 0; c < clips; c++)
                {
                    var clip = new Clip { Name = ReadName(reader), Fps = reader.ReadSingle(), Frames = reader.ReadInt32(), Loop = reader.ReadByte() != 0 };
                    clip.Rotations = new Quaternion[clip.Frames * bones];
                    clip.Positions = new Vector3[clip.Frames];

                    for (var f = 0; f < clip.Frames; f++)
                    {
                        clip.Positions[f] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                        for (var b = 0; b < bones; b++)
                        {
                            const float unit = 1f / 32767f;
                            clip.Rotations[f * bones + b] = new Quaternion(reader.ReadInt16() * unit, reader.ReadInt16() * unit, reader.ReadInt16() * unit, reader.ReadInt16() * unit);
                        }
                    }

                    set.Clips[clip.Name] = clip;
                }

                return set;
            }
        }
    }

    /// <summary>
    /// Plays a ClipSet on a model's bones (found by name): one clip at a time, each new one
    /// faded in from wherever the bones were. The owner calls Tick once a frame.
    ///
    /// How the importer turned glTF's right-handed numbers into Unity's is found out, not
    /// assumed: the file's rest pose is tried both ways against the model's own, and the way
    /// that fits is used (`Mirrored` tells the owner if the model faces backwards for it).
    /// </summary>
    public class RigAnimator
    {
        readonly ClipSet set;
        readonly Transform[] bones;
        readonly Quaternion[] from;
        readonly Quaternion[] correction;
        Vector3 fromPosition;
        readonly bool flipZ;
        readonly Vector3 moverRest;
        readonly float positionScale = 1;

        ClipSet.Clip clip;
        float time;
        float fade, fadeLeft;
        public float Speed = 1;

        public string Current => clip?.Name;
        public bool Bound { get; }
        /// <summary>The importer mirrored Z, not X: a model that should face +Z faces −Z.</summary>
        public bool Mirrored => flipZ;
        /// <summary>A clip that does not loop has played to its end.</summary>
        public bool Finished => clip != null && !clip.Loop && time >= clip.Length;
        public float Progress => clip == null ? 1 : Mathf.Clamp01(time / Mathf.Max(0.0001f, clip.Length));

        public RigAnimator(Transform root, string clips)
        {
            set = ClipSet.Load(clips);

            if (set == null || root == null)
            {
                return;
            }

            var byName = new Dictionary<string, Transform>();

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!byName.ContainsKey(child.name))
                {
                    byName[child.name] = child;
                }
            }

            bones = new Transform[set.Bones.Length];
            from = new Quaternion[bones.Length];
            correction = new Quaternion[bones.Length];
            var found = 0;
            float errorX = 0, errorZ = 0;

            for (var i = 0; i < bones.Length; i++)
            {
                if (!byName.TryGetValue(set.Bones[i], out bones[i]))
                {
                    continue;
                }

                found++;
                var rest = bones[i].localRotation;
                errorX += Quaternion.Angle(rest, Turn(set.RestRotations[i], false));
                errorZ += Quaternion.Angle(rest, Turn(set.RestRotations[i], true));
            }

            Bound = found > bones.Length / 2;

            if (!Bound)
            {
                Debug.LogWarning($"GU World: animations '{clips}' found only {found} of {bones.Length} bones under {root.name}");
                return;
            }

            flipZ = errorZ < errorX;
            var error = Mathf.Min(errorX, errorZ) / found;

            if (error > 3)
            {
                Debug.LogWarning($"GU World: the rest pose of {root.name} is {error:0.0}° off the animations' on average; they are fitted to it bone by bone");
            }

            for (var i = 0; i < bones.Length; i++)
            {
                // What is left between the model's rest pose and the file's goes into every frame.
                correction[i] = bones[i] != null ? bones[i].localRotation * Quaternion.Inverse(Turn(set.RestRotations[i], flipZ)) : Quaternion.identity;
                from[i] = bones[i] != null ? bones[i].localRotation : Quaternion.identity;
            }

            var mover = bones[set.Mover];

            if (mover != null)
            {
                moverRest = mover.localPosition;
                var fileRest = Turn(set.RestPositions[set.Mover], flipZ);
                // A model imported at another scale than the file's units.
                positionScale = fileRest.magnitude > 1e-4f ? moverRest.magnitude / fileRest.magnitude : 1;
                fromPosition = moverRest;
            }
        }

        static Quaternion Turn(Quaternion q, bool flipZ) => flipZ ? new Quaternion(-q.x, -q.y, q.z, q.w) : new Quaternion(q.x, -q.y, -q.z, q.w);
        static Vector3 Turn(Vector3 p, bool flipZ) => flipZ ? new Vector3(p.x, p.y, -p.z) : new Vector3(-p.x, p.y, p.z);

        public bool Has(string name) => set != null && set.Clips.ContainsKey(name);

        public float LengthOf(string name) => set != null && set.Clips.TryGetValue(name, out var found) ? found.Length : 0;

        /// <summary>Starts a clip (or goes on with it if it is already playing and `restart` is not asked).</summary>
        public void Play(string name, float fadeSeconds = 0.15f, bool restart = false, float speed = 1)
        {
            if (!Bound || !set.Clips.TryGetValue(name, out var next))
            {
                return;
            }

            Speed = speed;

            if (clip == next && !restart)
            {
                return;
            }

            // The fade starts from where the bones are now.
            for (var i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    from[i] = bones[i].localRotation;
                }
            }

            if (bones[set.Mover] != null)
            {
                fromPosition = bones[set.Mover].localPosition;
            }

            clip = next;
            time = 0;
            fade = fadeLeft = Mathf.Max(0, fadeSeconds);
        }

        public void Tick(float dt)
        {
            if (!Bound || clip == null)
            {
                return;
            }

            time += dt * Speed;
            fadeLeft = Mathf.Max(0, fadeLeft - dt);
            var length = clip.Length;
            var at = clip.Loop ? Mathf.Repeat(time, length) : Mathf.Min(time, length - 0.0001f);
            var frame = at * clip.Fps;
            var a = Mathf.Clamp(Mathf.FloorToInt(frame), 0, clip.Frames - 1);
            var b = clip.Loop ? (a + 1) % clip.Frames : Mathf.Min(a + 1, clip.Frames - 1);
            var t = frame - Mathf.Floor(frame);
            var blend = fade <= 0 ? 1 : 1 - fadeLeft / fade;
            blend = blend * blend * (3 - 2 * blend);
            var count = bones.Length;

            for (var i = 0; i < count; i++)
            {
                var bone = bones[i];

                if (bone == null)
                {
                    continue;
                }

                var rotation = correction[i] * Turn(Quaternion.SlerpUnclamped(clip.Rotations[a * count + i], Nearest(clip.Rotations[a * count + i], clip.Rotations[b * count + i]), t), flipZ);
                bone.localRotation = blend >= 1 ? rotation : Quaternion.Slerp(from[i], rotation, blend);
            }

            var mover = bones[set.Mover];

            if (mover != null)
            {
                var offset = Turn(Vector3.LerpUnclamped(clip.Positions[a], clip.Positions[b], t) - set.RestPositions[set.Mover], flipZ) * positionScale;
                var position = moverRest + offset;
                mover.localPosition = blend >= 1 ? position : Vector3.Lerp(fromPosition, position, blend);
            }
        }

        static Quaternion Nearest(Quaternion a, Quaternion b) => Quaternion.Dot(a, b) < 0 ? new Quaternion(-b.x, -b.y, -b.z, -b.w) : b;
    }
}
