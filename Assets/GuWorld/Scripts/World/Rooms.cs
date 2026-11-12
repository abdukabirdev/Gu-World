using System.Collections;
using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The insides of the great buildings (Core's <see cref="Interiors"/>): a room is built the first time
    /// the hero steps through its door — its shell made in code, the artist's furniture, warm lights under
    /// the beams — high above the world's corner, and kept. The way out is the step before its door.
    /// </summary>
    public class Rooms : MonoBehaviour
    {
        static Rooms instance;
        readonly Dictionary<string, Transform> built = new Dictionary<string, Transform>();
        InteriorDef inside;
        Vector3 back;
        float backYaw;

        static Rooms Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("Rooms").AddComponent<Rooms>();
                }

                return instance;
            }
        }

        public static bool Inside => instance != null && instance.inside != null;

        static Vector3 OriginOf(InteriorDef room)
        {
            var (x, y, z) = Interiors.Origin(room.Id);

            return new Vector3(x, y, z);
        }

        static Vector3 DoorStep(InteriorDef room) => OriginOf(room) + new Vector3(0, 0.15f, -room.Depth / 2 + 1.6f);

        Enemy partner;

        /// <summary>A bout in the academy's room: the partner steps out at the far end.</summary>
        public static void BeginBout()
        {
            if (!Inside || instance.partner != null)
            {
                return;
            }

            var def = Game.Session.BeginBout();

            if (def == null)
            {
                return;
            }

            var room = instance.inside;
            var at = OriginOf(room) + new Vector3(0, 0.3f, room.Depth / 2 - 4);
            instance.partner = Enemy.Spawn(def, at, 180, "bout");
            Game.Ui.Banner("Учебный бой", def.Name);
            Sound.AudioDirector.Play("quest", null, 0.8f, 0);
        }

        void Update()
        {
            if (partner == null && Game.Session != null && Game.Session.Sparring && !Inside)
            {
                Game.Session.EndBout(false);
            }

            if (partner == null)
            {
                return;
            }

            // Won: the partner fell. Lost: the rules ended the bout (the hero was brought low), or the hero left.
            if (!partner.Alive)
            {
                Game.Session.EndBout(true);
                Destroy(partner.gameObject, 2);
                partner = null;
            }
            else if (!Game.Session.Sparring || !Inside)
            {
                Game.Session.EndBout(false);
                Destroy(partner.gameObject);
                partner = null;
            }
        }

        /// <summary>The person of the room nearest the hero, within reach.</summary>
        public static RoomPerson NearestPerson()
        {
            if (!Inside || Game.Hero == null)
            {
                return null;
            }

            RoomPerson best = null;
            var bestDistance = 2.6f;

            foreach (var person in RoomPerson.All)
            {
                var d = Vector3.Distance(person.transform.position, Game.Hero.transform.position);

                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = person;
                }
            }

            return best;
        }

        /// <summary>The prompt by the door inside, or null.</summary>
        public static string Prompt => Inside && Game.Hero != null && Vector3.Distance(Game.Hero.transform.position, DoorStep(instance.inside)) < 2.2f ? "Выйти · " + instance.inside.Name : null;

        public static void Enter(string id, Vector3 from, float facing)
        {
            var room = Interiors.Get(id);

            if (room == null || Game.Hero == null)
            {
                return;
            }

            Game.Run(Instance.Entering(room, from, facing));
        }

        public static void Leave()
        {
            if (Inside)
            {
                Game.Run(instance.Leaving());
            }
        }

        IEnumerator Entering(InteriorDef room, Vector3 from, float facing)
        {
            yield return Game.Ui.FadeOut(0.3f);
            Build(room);
            inside = room;
            // Back out a step before the outer door, turned away from it.
            back = from + Quaternion.Euler(0, facing, 0) * Vector3.forward * 1.6f;
            backYaw = facing;
            Sound.AudioDirector.Play("door", null, 0.8f, 0);
            Game.Hero.Teleport(DoorStep(room) + Vector3.forward * 0.8f);
            Game.Rig.Follow(Game.Hero, 0);
            yield return null;
            yield return Game.Ui.FadeIn(0.4f);
            Game.Say(room.Name);
        }

        IEnumerator Leaving()
        {
            yield return Game.Ui.FadeOut(0.3f);
            inside = null;
            Sound.AudioDirector.Play("door", null, 0.8f, 0);
            Streamer.Instance?.LoadNow(back);
            Game.Hero.Teleport(Game.Ground(back) + Vector3.up * 0.2f);
            Game.Rig.Follow(Game.Hero, backYaw);
            yield return null;
            yield return Game.Ui.FadeIn(0.4f);
        }

        void Build(InteriorDef room)
        {
            if (built.ContainsKey(room.Id))
            {
                return;
            }

            var root = new GameObject(room.Name).transform;
            root.SetParent(transform, false);
            root.position = OriginOf(room);
            built[room.Id] = root;

            var mesh = Meshes.From(Props.Room(room.Width, room.Depth, room.Height, room.Style), room.Id);
            var shell = new GameObject("Shell");
            shell.transform.SetParent(root, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh;
            shell.AddComponent<MeshRenderer>().sharedMaterial = Models.VertexLit(Color.white, 0);
            shell.AddComponent<MeshCollider>().sharedMesh = mesh;

            foreach (var (model, x, z, yaw) in room.Furniture)
            {
                Models.Prefab(model, root, root.position + new Vector3(x, 0, z), yaw);
            }

            // Its people, standing at their places.
            foreach (var person in room.People)
            {
                var body = new GameObject(person.name);
                body.transform.SetParent(root, false);
                body.transform.position = root.position + new Vector3(person.x, 0.02f, person.z);
                body.transform.rotation = Quaternion.Euler(0, person.yaw, 0);
                Models.Human(body.transform, person.look, false, false, out var rig, People.Seed(person.name + person.title));
                rig?.Play("idle", 0);
                body.AddComponent<RoomPerson>().Set(rig, person.name, person.title, person.line);
                var solid = body.AddComponent<CapsuleCollider>();
                solid.radius = 0.35f;
                solid.height = 1.8f;
                solid.center = new Vector3(0, 0.9f, 0);
            }

            // Lanterns under the beams: warm lights down the room's middle (two rows in a wide room).
            var rows = room.Width > 15 ? 2 : 1;

            for (var i = 0; i < room.Lights; i++)
            {
                var z = -room.Depth / 2 + (i / rows + 0.5f) * room.Depth / Mathf.Ceil(room.Lights / (float)rows);
                var x = rows == 1 ? 0 : (i % 2 == 0 ? -room.Width / 4 : room.Width / 4);
                var at = root.position + new Vector3(x, room.Height - 1.2f, z);
                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.position = at;
                light.type = LightType.Point;
                light.range = Mathf.Max(8, room.Height * 2.2f);
                light.intensity = 1.4f;
                light.color = new Color(1f, 0.78f, 0.55f);
                Models.Part("Lantern", root, Models.Quad, Models.Glow(new Color(1f, 0.55f, 0.3f, 0.8f)), at, Vector3.one * 0.7f).AddComponent<Fx.Billboard>();
            }
        }
    }

    /// <summary>Someone standing in a room: idles, turns to the hero, says their line.</summary>
    public class RoomPerson : MonoBehaviour
    {
        public static readonly List<RoomPerson> All = new List<RoomPerson>();

        RigAnimator rig;
        Quaternion rest;
        public string Name { get; private set; }
        public string Title { get; private set; }
        public string Line { get; private set; }

        public void Set(RigAnimator animator, string name, string title, string line)
        {
            rig = animator;
            Name = name;
            Title = title;
            Line = line;
            rest = transform.rotation;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var hero = Game.Hero;

            if (hero != null)
            {
                var to = hero.transform.position - transform.position;
                to.y = 0;
                var want = to.magnitude < 5 && to.sqrMagnitude > 0.1f ? Quaternion.LookRotation(to) : rest;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 160 * Time.deltaTime);
            }

            if (!Game.Frozen)
            {
                rig?.Tick(Time.deltaTime);
            }
        }

        /// <summary>Says its line; the clan's treasurer then opens the treasury.</summary>
        public void Talk()
        {
            System.Action then = null;

            if (Name.StartsWith("Казначей"))
            {
                then = () => Game.Ui.Show(UI.Ui.Page.Treasury);
            }
            else if (Name.StartsWith("Наставник академии"))
            {
                // The teacher offers a bout with a partner of the hero's rank.
                var why = Game.Session.WhyNoBout();
                var lines = why == null
                    ? new[] { new Core.Line("~" + Name + "|" + Title, Line), new Core.Line("~" + Name + "|" + Title, $"Хочешь проверить себя? Против тебя выйдет {Core.Sparring.For(Game.Session.Rank).Name.ToLowerInvariant()}. Боёв на сегодня: {Game.Session.BoutsLeft}.") }
                    : new[] { new Core.Line("~" + Name + "|" + Title, why) };
                Game.Ui.Talk(lines, why == null ? () => Rooms.BeginBout() : (System.Action)null);

                return;
            }

            Game.Ui.Talk(new[] { new Core.Line("~" + Name + "|" + Title, Line) }, then);
        }
    }
}
