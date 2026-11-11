using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Fx;
using GuWorld.UI;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>A thing of the plan standing in the world: a lantern, the spring, a chest, a herb, a door, something a quest wants looked at.</summary>
    public class Thing : MonoBehaviour
    {
        public static readonly List<Thing> All = new List<Thing>();

        public ThingDef Def { get; private set; }
        public float Reach { get; private set; }
        GameObject glow, body, lid;
        float regrow;
        float wait;

        public string Verb
        {
            get
            {
                switch (Def.Kind)
                {
                    case ThingKind.Lantern: return "Коснуться";
                    case ThingKind.Spring: return "Медитировать";
                    case ThingKind.FusionAltar: return "Слияние";
                    case ThingKind.Chest: return "Открыть";
                    case ThingKind.Herb: return "Собрать";
                    case ThingKind.Door: return "Войти";
                    default: return "Осмотреть";
                }
            }
        }

        public bool Usable
        {
            get
            {
                var session = Game.Session;

                if (session == null)
                {
                    return false;
                }

                switch (Def.Kind)
                {
                    case ThingKind.Chest: return !session.IsUsed(Def.Id);
                    case ThingKind.Herb: return regrow <= 0;
                    case ThingKind.QuestObject: return session.IsWanted(Def);
                    default: return true;
                }
            }
        }

        public static Thing Make(ThingDef def, Transform parent)
        {
            var root = new GameObject(def.Id);
            root.transform.SetParent(parent, false);
            root.transform.position = Game.Ground(new Vector3(def.At.X, 0, def.At.Z));
            root.transform.rotation = Quaternion.Euler(0, def.Facing, 0);
            var thing = root.AddComponent<Thing>();
            thing.Def = def;
            thing.Dress();

            return thing;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        static void Lamp(Transform on, Color colour, float height, float range, float strength)
        {
            var lamp = new GameObject("Light");
            lamp.transform.SetParent(on, false);
            lamp.transform.localPosition = new Vector3(0, height, 0);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.intensity = strength;
            light.shadows = LightShadows.None;
        }

        GameObject Halo(Color colour, float height, float size)
        {
            var halo = Models.Part("Glow", transform, Models.Quad, Models.Glow(colour), new Vector3(0, height, 0), Vector3.one * size);
            halo.AddComponent<Billboard>();

            return halo;
        }

        void Dress()
        {
            var t = transform;
            var gold = new Color(1f, 0.82f, 0.45f, 0.7f);
            Reach = 0;

            if (!string.IsNullOrEmpty(Def.Model))
            {
                body = Models.Prefab(Def.Model, t, t.position, Def.Facing);
            }

            switch (Def.Kind)
            {
                case ThingKind.Lantern:
                    Lamp(t, new Color(0.6f, 0.78f, 1f), 3.6f, 16, 3);
                    glow = Halo(new Color(0.6f, 0.8f, 1f, 0.6f), 3.6f, 2.4f);
                    break;

                case ThingKind.Spring:
                {
                    Reach = 2.5f;
                    var stone = Models.Lit(new Color(0.42f, 0.44f, 0.46f));

                    for (var i = 0; i < 12; i++)
                    {
                        var angle = i * Mathf.PI * 2 / 12;
                        Models.Shape("Stone", t, PrimitiveType.Sphere, stone, new Vector3(Mathf.Sin(angle) * 3, 0.15f, Mathf.Cos(angle) * 3), new Vector3(1.3f, 0.7f + 0.2f * (i % 3), 1.1f), Quaternion.Euler(0, angle * 57, 0));
                    }

                    Models.Part("Water", t, Models.Disc, Models.Glow(new Color(0.35f, 0.85f, 0.78f, 0.55f), false), new Vector3(0, 0.22f, 0), Vector3.one * 5.6f);
                    Models.GroundGlow("Ring", t, new Color(0.5f, 0.95f, 0.85f, 0.8f), 4, true).AddComponent<Fx.Hover>().Spin = 25;
                    Lamp(t, new Color(0.45f, 0.95f, 0.85f), 1.2f, 14, 2.6f);
                    glow = Halo(new Color(0.5f, 0.95f, 0.85f, 0.35f), 1.4f, 5);
                    break;
                }

                case ThingKind.FusionAltar:
                    Reach = 1.6f;
                    Lamp(t, new Color(1f, 0.6f, 0.3f), 2.4f, 10, 2.2f);
                    glow = Halo(new Color(1f, 0.6f, 0.3f, 0.5f), 2.6f, 2.2f);
                    break;

                case ThingKind.Chest:
                {
                    var wood = Models.Lit(new Color(0.3f, 0.19f, 0.12f));
                    var metal = Models.Lit(new Color(0.75f, 0.6f, 0.3f), 0.6f);
                    Models.Shape("Box", t, PrimitiveType.Cube, wood, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.6f, 0.7f)).AddComponent<BoxCollider>();
                    Models.Shape("Band", t, PrimitiveType.Cube, metal, new Vector3(0, 0.3f, 0), new Vector3(0.16f, 0.62f, 0.72f));
                    lid = new GameObject("Lid");
                    lid.transform.SetParent(t, false);
                    lid.transform.localPosition = new Vector3(0, 0.6f, -0.35f);
                    Models.Shape("Top", lid.transform, PrimitiveType.Cube, wood, new Vector3(0, 0.09f, 0.35f), new Vector3(1.14f, 0.18f, 0.74f));
                    Models.Shape("Clasp", lid.transform, PrimitiveType.Cube, metal, new Vector3(0, 0.05f, 0.72f), new Vector3(0.2f, 0.2f, 0.06f));
                    glow = Halo(gold, 1.2f, 1.6f);
                    glow.AddComponent<Fx.Hover>();
                    break;
                }

                case ThingKind.Herb:
                {
                    var item = ItemBook.Get(Def.Content);
                    var colour = Models.Rgb(item != null ? item.Colour : 0x9FEFC4);
                    body = new GameObject("Plant");
                    body.transform.SetParent(t, false);
                    var leaf = Models.Lit(Color.Lerp(colour, new Color(0.15f, 0.4f, 0.2f), 0.6f));

                    for (var i = 0; i < 4; i++)
                    {
                        Models.Shape("Leaf", body.transform, PrimitiveType.Sphere, leaf, new Vector3(Mathf.Sin(i * 1.6f) * 0.14f, 0.22f, Mathf.Cos(i * 1.6f) * 0.14f), new Vector3(0.1f, 0.5f, 0.24f), Quaternion.Euler(0, i * 90, 24));
                    }

                    Models.Shape("Bloom", body.transform, PrimitiveType.Sphere, Models.Glow(colour), new Vector3(0, 0.5f, 0), Vector3.one * 0.16f);
                    glow = Models.Part("Glow", body.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.45f)), new Vector3(0, 0.5f, 0), Vector3.one * 1.1f);
                    glow.AddComponent<Billboard>();
                    break;
                }

                case ThingKind.Door:
                {
                    var colour = Def.Id.Contains("blood") ? new Color(1f, 0.35f, 0.3f, 0.8f) : new Color(0.6f, 0.8f, 1f, 0.8f);
                    var ring = Models.Part("Seal", t, Models.Ring, Models.Glow(colour), new Vector3(0, 1.5f, 0), Vector3.one * 2.6f, Quaternion.Euler(90, 0, 0));
                    ring.AddComponent<Billboard>();
                    glow = Halo(new Color(colour.r, colour.g, colour.b, 0.3f), 1.5f, 3.4f);
                    Lamp(t, colour, 1.6f, 9, 2);
                    break;
                }

                case ThingKind.QuestObject:
                    Reach = string.IsNullOrEmpty(Def.Model) ? 0 : 1.2f;

                    if (Def.Id == "scout_body")
                    {
                        // The watchman, lying where he fell.
                        var lying = new GameObject("Body");
                        lying.transform.SetParent(t, false);
                        Models.Human(lying.transform, "guard", false, false, out var rig);
                        rig?.Play("death", 0);
                        rig?.Tick(5);
                    }
                    else if (Def.Id.StartsWith("blood_altar") || Def.Id == "blood_gu" || Def.Id == "den_lure")
                    {
                        Models.Shape("Heart", t, PrimitiveType.Sphere, Models.Glow(new Color(1f, 0.2f, 0.15f, 0.9f)), new Vector3(0, Def.Id == "den_lure" ? 0.4f : 2.2f, 0), Vector3.one * (Def.Id == "blood_gu" ? 0.6f : 0.4f)).AddComponent<Fx.Hover>();
                        Lamp(t, new Color(1f, 0.25f, 0.2f), 2.4f, 12, 2.4f);
                    }

                    glow = Models.Part("Beam", t, Models.Quad, Models.Glow(gold), new Vector3(0, 4, 0), new Vector3(0.7f, 8, 1));
                    glow.AddComponent<UprightBillboard>();
                    break;
            }

            Refresh();
        }

        void Update()
        {
            if (regrow > 0)
            {
                regrow -= Time.deltaTime;

                if (regrow <= 0 && body != null)
                {
                    body.SetActive(true);
                }
            }

            wait -= Time.deltaTime;

            if (wait <= 0)
            {
                wait = 0.5f;
                Refresh();
            }
        }

        void Refresh()
        {
            var session = Game.Session;

            if (session == null)
            {
                return;
            }

            switch (Def.Kind)
            {
                case ThingKind.Chest:
                    var open = session.IsUsed(Def.Id);
                    glow.SetActive(!open);
                    lid.transform.localRotation = Quaternion.Euler(open ? -70 : 0, 0, 0);
                    break;
                case ThingKind.QuestObject:
                    glow.SetActive(session.IsWanted(Def));

                    // A broken altar is dark.
                    if (Def.Id.StartsWith("blood_altar") && session.IsUsed(Def.Id))
                    {
                        foreach (var light in GetComponentsInChildren<Light>())
                        {
                            light.enabled = false;
                        }
                    }

                    break;
                case ThingKind.Door:
                    glow.SetActive(session.Has(Def.Needs));
                    break;
                case ThingKind.Lantern:
                    glow.transform.localScale = Vector3.one * (session.State.Lantern == Def.Id ? 3.4f : 2.2f);
                    break;
            }
        }

        public void Use()
        {
            var session = Game.Session;
            var use = session.UseThing(Def.Id);

            if (!use.Ok)
            {
                if (!string.IsNullOrEmpty(use.Message))
                {
                    Game.Say(use.Message);
                }

                return;
            }

            switch (use.Service)
            {
                case "lantern":
                    Game.Say(use.Message);
                    Effects.Pillar(transform.position, new Color(0.6f, 0.8f, 1f), 1.6f, 14, 0.8f);
                    Game.Save();
                    break;
                case "spring":
                    Game.Ui.Show(Ui.Page.Spring);
                    break;
                case "fusion":
                    Game.Ui.Show(Ui.Page.Fusion);
                    break;
                case "door":
                    Game.Travel(Def.To);
                    break;
                case "choose":
                    Game.Ui.Show(Ui.Page.Choice);
                    break;
                default:
                    if (Def.Kind == ThingKind.Chest)
                    {
                        Effects.Pillar(transform.position, new Color(1f, 0.82f, 0.45f), 1.4f, 8, 0.8f);
                    }
                    else if (Def.Kind == ThingKind.Herb)
                    {
                        regrow = Def.Regrow > 0 ? Def.Regrow : 99999;
                        body.SetActive(false);
                        Effects.Burst(transform.position + Vector3.up * 0.5f, new Color(0.7f, 1f, 0.8f), 1.2f, 0.3f);
                    }
                    else if (use.Lines.Length > 0)
                    {
                        Game.Ui.Talk(use.Lines, () =>
                        {
                            use.Finish?.Invoke();

                            // What was read may ask for an answer at once (the bowl under the hall).
                            if (Game.Session.PendingChoice() != null)
                            {
                                Game.Ui.Show(Ui.Page.Choice);
                            }
                        });
                    }
                    else
                    {
                        Effects.Wave(transform.position, new Color(1f, 0.4f, 0.3f), 4, 0.5f);
                        use.Finish?.Invoke();
                    }

                    break;
            }

            Refresh();
        }
    }
}
