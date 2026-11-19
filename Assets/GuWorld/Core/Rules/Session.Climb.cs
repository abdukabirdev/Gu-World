using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>The hero's body beyond the fight (ROADMAP 12.3, 12.6): climbing, falls, the tops of great trees. The numbers are Movement's.</summary>
    public partial class Session
    {
        /// <summary>
        /// A moment on the bark or hanging from a ledge: stamina goes (less for a hardy body) and does not
        /// come back meanwhile; the arms and the wind grow. False when it is spent: the hands let go.
        /// </summary>
        public bool Climb(float dt, bool moving, bool fast)
        {
            staminaWait = 0.6f;

            if (Godmode)
            {
                return true;
            }

            var cost = Movement.Cost(moving, fast) * (1 - Math.Min(0.3f, 0.03f * TraitLevel(Trait.Endurance))) * dt;

            if (Stamina <= cost)
            {
                Stamina = 0;

                return false;
            }

            Stamina -= cost;

            if (moving)
            {
                Train(Trait.Strength, dt * 0.2f);
                Train(Trait.Endurance, dt * 0.3f);
            }

            return true;
        }

        /// <summary>A fall onto something solid: health by the drop, and from 7 m a leg is hurt. The damage done.</summary>
        public float Fall(float drop)
        {
            var share = Movement.FallShare(drop);

            if (share <= 0)
            {
                return 0;
            }

            var damage = TakeDamage(share * Stats.MaxHealth);
            var wound = Movement.FallWound(drop, State.Settings.Difficulty);

            if (wound != null && damage > 0 && State.Health > 0)
            {
                AddWound(Random.Next(2) == 0 ? BodyPart.LeftLeg : BodyPart.RightLeg, wound.Value.kind, wound.Value.severity);
            }

            return damage;
        }

        /// <summary>The most tops kept (the oldest forgotten: a tree climbed long ago gives its view again, not its nest — the nest comes back too).</summary>
        public const int MostTreetops = 400;

        /// <summary>
        /// The top of a great tree, reached (ROADMAP 12.6): once a tree, the land far round goes on the
        /// map, and the nest up there gives what it holds. The words to show, or null — not a great tree,
        /// or its top was reached before.
        /// </summary>
        public string Treetop(string key, float x, float z, float treeHeight)
        {
            if (treeHeight < Movement.GreatTree || string.IsNullOrEmpty(key))
            {
                return null;
            }

            State.Treetops = State.Treetops ?? new List<string>();

            if (State.Treetops.Contains(key))
            {
                return null;
            }

            State.Treetops.Add(key);

            if (State.Treetops.Count > MostTreetops)
            {
                State.Treetops.RemoveAt(0);
            }

            var seen = Explore(x, z, Movement.TopSight(treeHeight));
            var found = new List<string>();

            foreach (var (item, count) in Movement.Nest(Random, treeHeight))
            {
                if (item == "stones")
                {
                    State.Stones += count;
                    found.Add($"{count} первобытных камней");
                }
                else if (ItemBook.Get(item) != null)
                {
                    AddItem(item, count, true);
                    found.Add(count > 1 ? $"{ItemBook.Get(item).Name} ×{count}" : ItemBook.Get(item).Name);
                }
            }

            Touch();

            return $"Вершина в {treeHeight:0} м: округа как на ладони" + (seen > 0 ? " — на карте открылось больше" : "") + (found.Count > 0 ? $". В гнезде: {string.Join(", ", found)}." : ".");
        }
    }
}
