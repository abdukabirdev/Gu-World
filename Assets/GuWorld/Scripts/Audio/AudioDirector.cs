using System.Collections.Generic;
using System.Threading.Tasks;
using GuWorld.Core;
using GuWorld.Core.Audio;
using UnityEngine;

namespace GuWorld.Sound
{
    /// <summary>
    /// The game's sound, made in code at start-up (Core/Audio) and played here: effects from a pool of
    /// sources, in the world or flat for the interface; beds of ambience faded in and out by where the
    /// hero is (woods, water, caves, rain, wind on the heights, settlements, night); music that follows
    /// the mood (the place, the hour, a fight, a boss), crossfading from piece to piece.
    /// The sounds are made on worker threads; the clips are made on the main thread a few per frame.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        // The volumes the player set (0–1), kept between games.
        public static float Master = 0.9f, Effects = 0.9f, MusicVolume = 0.55f, Beds = 0.7f;

        readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, AudioClip> beds = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> pieces = new Dictionary<string, AudioClip>();
        readonly Queue<(string id, int variant, Wave wave)> madeEffects = new Queue<(string, int, Wave)>();
        readonly Queue<(string id, Wave wave)> madeBeds = new Queue<(string, Wave)>();
        readonly Queue<(string id, Wave wave)> madePieces = new Queue<(string, Wave)>();
        readonly HashSet<string> piecesAsked = new HashSet<string>();
        readonly object gate = new object();

        readonly List<AudioSource> pool = new List<AudioSource>();
        int next;
        readonly Dictionary<string, (AudioSource source, float want)> bedSources = new Dictionary<string, (AudioSource, float)>();
        AudioSource musicA, musicB;
        bool musicOnA = true;
        string musicNow = "";
        float moodWait;
        Mood mood = Mood.Title;
        float lastFlash;

        public static void Make(Transform parent)
        {
            if (Instance != null)
            {
                return;
            }

            var root = new GameObject("Audio");
            root.transform.SetParent(parent, false);
            Instance = root.AddComponent<AudioDirector>();
        }

        void Awake()
        {
            Master = PlayerPrefs.GetFloat("gw2.volume.master", Master);
            Effects = PlayerPrefs.GetFloat("gw2.volume.effects", Effects);
            MusicVolume = PlayerPrefs.GetFloat("gw2.volume.music", MusicVolume);
            Beds = PlayerPrefs.GetFloat("gw2.volume.beds", Beds);

            for (var i = 0; i < 20; i++)
            {
                var source = new GameObject("Voice " + i).AddComponent<AudioSource>();
                source.transform.SetParent(transform, false);
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 3;
                source.maxDistance = 60;
                source.dopplerLevel = 0;
                pool.Add(source);
            }

            musicA = Looping("Music A", 0);
            musicB = Looping("Music B", 0);

            // Everything is made away from the main thread: the effects first, then the beds.
            Task.Run(() =>
            {
                foreach (var id in Sounds.Ids)
                {
                    for (var v = 0; v < Sounds.Variants; v++)
                    {
                        var wave = Sounds.Make(id, v);

                        lock (gate)
                        {
                            madeEffects.Enqueue((id, v, wave));
                        }
                    }
                }

                foreach (var id in Core.Audio.Ambience.Ids)
                {
                    var wave = Core.Audio.Ambience.Make(id);

                    lock (gate)
                    {
                        madeBeds.Enqueue((id, wave));
                    }
                }
            });

            Ask(Mood.Title, 1);
        }

        AudioSource Looping(string name, float spatial)
        {
            var source = new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = spatial;
            source.volume = 0;

            return source;
        }

        static AudioClip Clip(string name, Wave wave)
        {
            var clip = AudioClip.Create(name, wave.Length, 1, Synth.Rate, false);
            clip.SetData(wave.Data, 0);

            return clip;
        }

        /// <summary>A piece for a mood, made on a worker thread the first time it is wanted.</summary>
        void Ask(Mood wanted, int seed)
        {
            var key = wanted + "/" + seed;

            if (!piecesAsked.Add(key))
            {
                return;
            }

            Task.Run(() =>
            {
                var wave = Music.Make(wanted, seed);

                lock (gate)
                {
                    madePieces.Enqueue((key, wave));
                }
            });
        }

        public static void SetVolumes(float master, float effects, float music, float ambience)
        {
            Master = master;
            Effects = effects;
            MusicVolume = music;
            Beds = ambience;
            PlayerPrefs.SetFloat("gw2.volume.master", master);
            PlayerPrefs.SetFloat("gw2.volume.effects", effects);
            PlayerPrefs.SetFloat("gw2.volume.music", music);
            PlayerPrefs.SetFloat("gw2.volume.beds", ambience);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------ Effects

        /// <summary>Plays an effect: in the world at a point (null for the interface's flat sound), a variant at random.</summary>
        public static void Play(string id, Vector3? at = null, float volume = 1, float pitchSpread = 0.06f)
        {
            var director = Instance;

            if (director == null || !director.clips.TryGetValue(id, out var variants) || variants.Length == 0)
            {
                return;
            }

            var source = director.pool[director.next];
            director.next = (director.next + 1) % director.pool.Count;
            // A variant at random (one not yet made is skipped).
            var clip = variants[Random.Range(0, variants.Length)];

            for (var i = 0; clip == null && i < variants.Length; i++)
            {
                clip = variants[i];
            }

            if (clip == null)
            {
                return;
            }

            source.Stop();
            source.clip = clip;
            source.volume = volume * Effects * Master;
            source.pitch = 1 + Random.Range(-pitchSpread, pitchSpread);
            source.spatialBlend = at.HasValue ? 1 : 0;

            if (at.HasValue)
            {
                source.transform.position = at.Value;
            }

            source.Play();
        }

        public static bool Ready => Instance != null && Instance.clips.Count >= 20;

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            // The clips that have been made, a few a frame.
            lock (gate)
            {
                for (var n = 0; n < 6 && madeEffects.Count > 0; n++)
                {
                    var (id, variant, wave) = madeEffects.Dequeue();

                    if (!clips.TryGetValue(id, out var list))
                    {
                        clips[id] = list = new AudioClip[Sounds.Variants];
                    }

                    list[variant] = Clip(id + variant, wave);
                }

                if (madeBeds.Count > 0)
                {
                    var (id, wave) = madeBeds.Dequeue();
                    beds[id] = Clip("bed " + id, wave);
                }

                if (madePieces.Count > 0)
                {
                    var (id, wave) = madePieces.Dequeue();
                    pieces[id] = Clip("music " + id, wave);
                }
            }

            var listener = Game.Rig != null ? Game.Rig.Camera : null;

            if (listener != null && listener.GetComponent<AudioListener>() == null)
            {
                listener.gameObject.AddComponent<AudioListener>();
            }

            Moods();
            Ambience();
            MusicFade();

            // Thunder after the flash, a little later the farther the storm.
            if (World.Atmosphere.Flash > 0.9f && Time.time - lastFlash > 2)
            {
                lastFlash = Time.time;
                Invoke(nameof(Thunder), Random.Range(0.6f, 2.5f));
            }
        }

        void Thunder() => Play("thunder", null, 0.8f, 0.1f);

        // ------------------------------------------------------------------ The music

        void Moods()
        {
            moodWait -= Time.unscaledDeltaTime;

            if (moodWait > 0)
            {
                return;
            }

            moodWait = 1.5f;
            var wanted = Mood.Title;
            var hero = Game.Hero;
            var session = Game.Session;

            if (Game.Playing && hero != null && session != null)
            {
                var point = new MapPoint(hero.transform.position.x, hero.transform.position.z);
                var settlement = Atlas.SettlementAt(point);
                var place = Atlas.PlaceAt(point);
                var boss = false;

                foreach (var enemy in Actors.Enemy.Living)
                {
                    boss |= enemy.Def.Boss && enemy.Aware && hero.FlatDistance(enemy.transform.position) < 60;
                }

                var sacred = place != null && (place.Id == "peak" || place.Id == "great_tree" || place.Id == "spring" || place.Id == "hermit_cave");
                var forest = place != null && (place.Id == "forest" || place.Id == "grove");
                wanted = Music.MoodOf(settlement?.Clan ?? "", place != null && place.Cave, World.Atmosphere.Night, Game.Fighting, boss, sacred, forest);
            }

            if (wanted != mood || musicNow == "")
            {
                mood = wanted;
                var day = session != null ? session.State.Day : 1;
                Ask(mood, 1 + day % 3);
            }

            var key = mood + "/" + (1 + (session != null ? session.State.Day : 1) % 3);

            if (key != musicNow && pieces.TryGetValue(key, out var clip))
            {
                musicNow = key;
                musicOnA = !musicOnA;
                var source = musicOnA ? musicA : musicB;
                source.clip = clip;
                source.time = 0;
                source.Play();
            }
        }

        void MusicFade()
        {
            var speed = mood == Mood.Combat || mood == Mood.Boss ? 0.8f : 0.3f;
            var on = musicOnA ? musicA : musicB;
            var off = musicOnA ? musicB : musicA;
            var want = MusicVolume * Master * (Game.Frozen && Game.Playing ? 0.5f : 1);
            on.volume = Mathf.MoveTowards(on.volume, want, Time.unscaledDeltaTime * speed);
            off.volume = Mathf.MoveTowards(off.volume, 0, Time.unscaledDeltaTime * speed);

            if (off.volume <= 0 && off.isPlaying)
            {
                off.Stop();
            }
        }

        // ------------------------------------------------------------------ The beds

        void Ambience()
        {
            var hero = Game.Hero;
            var wanted = new Dictionary<string, float>();

            if (Game.Playing && hero != null)
            {
                var at = hero.transform.position;
                var point = new MapPoint(at.x, at.z);
                var place = Atlas.PlaceAt(point);
                var settlement = Atlas.SettlementAt(point);
                var cave = place != null && place.Cave;
                var sky = World.Atmosphere.Sky;
                var night = World.Atmosphere.Night;

                if (cave)
                {
                    wanted["cave"] = 1;
                }
                else
                {
                    wanted[at.y > 380 ? "wind_high" : at.y > 160 ? "wind" : "wind_soft"] = 0.7f;
                    wanted[night ? "night" : "forest_day"] = 0.6f;

                    if (sky == Sky.Rain)
                    {
                        wanted["rain"] = World.Atmosphere.Gloom;
                    }

                    if (sky == Sky.Storm)
                    {
                        wanted["storm"] = World.Atmosphere.Gloom;
                    }

                    if (settlement != null)
                    {
                        wanted["settlement"] = night ? 0.3f : 0.6f;

                        // The town's market: loud on the square and round it, by day.
                        if (settlement.City && !night)
                        {
                            var fromMiddle = new Vector2(at.x - settlement.At.X, at.z - settlement.At.Z).magnitude;
                            wanted["market"] = Mathf.Clamp01(1 - (fromMiddle - settlement.Core) / 120) * 0.75f;
                        }
                    }

                    if (place != null && place.Id == "marsh")
                    {
                        wanted["marsh"] = 0.8f;
                    }
                }

                // Water: a river near, louder the nearer; a waterfall louder still.
                var river = World.Ground.RiverDistance(at.x, at.z);

                if (river < 60)
                {
                    wanted["river"] = Mathf.Clamp01(1 - river / 60);
                }

                var fall = World.Ground.FallDistance(at.x, at.z);

                if (fall < 120)
                {
                    wanted["waterfall"] = Mathf.Clamp01(1 - fall / 120);
                }
            }

            foreach (var id in Core.Audio.Ambience.Ids)
            {
                wanted.TryGetValue(id, out var level);

                if (!bedSources.TryGetValue(id, out var bed))
                {
                    if (level <= 0 || !beds.TryGetValue(id, out var clip))
                    {
                        continue;
                    }

                    var source = Looping("Bed " + id, 0);
                    source.clip = clip;
                    source.time = Random.Range(0, clip.length);
                    source.Play();
                    bed = (source, 0);
                }

                var volume = Mathf.MoveTowards(bed.source.volume, level * Beds * Master, Time.unscaledDeltaTime * 0.35f);
                bed.source.volume = volume;
                bedSources[id] = (bed.source, level);

                if (volume <= 0 && level <= 0)
                {
                    Destroy(bed.source.gameObject);
                    bedSources.Remove(id);
                }
            }
        }
    }
}
