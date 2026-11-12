using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// What a creature is weak to and what it shrugs off, read from what it is: the cold beasts fear fire
    /// and laugh at ice, the burning ones fear water and ice, venomous things fear fire and are proof to
    /// poison, the dead fear light and drink darkness, serpents slow in the cold, birds fall to lightning,
    /// stone and bone shrug off wind and blades of moonlight… A blow of a weakness lands a third harder,
    /// of a resistance a third softer. Nothing in the content needs to say it: the rules see it.
    /// </summary>
    public static class Affinity
    {
        public const float Weak = 1.35f;
        public const float Strong = 0.65f;

        static readonly Dictionary<string, (GuPath[] weak, GuPath[] strong)> cache = new Dictionary<string, (GuPath[], GuPath[])>();

        public static (GuPath[] weak, GuPath[] strong) Of(EnemyDef enemy)
        {
            if (enemy == null)
            {
                return (new GuPath[0], new GuPath[0]);
            }

            var key = enemy.Id + "|" + enemy.Elite;

            lock (cache)
            {
                if (cache.TryGetValue(key, out var known))
                {
                    return known;
                }
            }

            var weak = new List<GuPath>();
            var strong = new List<GuPath>();

            void W(params GuPath[] paths)
            {
                foreach (var p in paths)
                {
                    if (!weak.Contains(p) && !strong.Contains(p))
                    {
                        weak.Add(p);
                    }
                }
            }

            void S(params GuPath[] paths)
            {
                foreach (var p in paths)
                {
                    if (!strong.Contains(p) && !weak.Contains(p))
                    {
                        strong.Add(p);
                    }
                }
            }

            // What it inflicts tells what it is made of.
            switch (enemy.Inflicts)
            {
                case StatusKind.Chill:
                case StatusKind.Freeze:
                    W(GuPath.Fire);
                    S(GuPath.Ice, GuPath.Water);
                    break;
                case StatusKind.Burn:
                    W(GuPath.Water, GuPath.Ice);
                    S(GuPath.Fire);
                    break;
                case StatusKind.Poison:
                    W(GuPath.Fire);
                    S(GuPath.Poison);
                    break;
                case StatusKind.Bleed:
                    S(GuPath.Blood);
                    break;
            }

            // What its body is.
            var id = enemy.Id;
            var look = enemy.Look ?? "";

            if (enemy.Body == EnemyBody.Serpent)
            {
                W(GuPath.Ice);
            }

            if (look == "eagle" || id.Contains("roc") || id.Contains("eagle") || look == "bat")
            {
                W(GuPath.Lightning);
                S(GuPath.Wind);
            }

            if (look == "spider" || look == "spider_queen" || look == "bee" || look == "centipede" || id.Contains("spider") || id.Contains("centipede"))
            {
                W(GuPath.Fire);
            }

            if (look == "shade" || id.Contains("bone") || id.Contains("tomb") || id.Contains("shade") || id.Contains("ghoul") || look == "wisp")
            {
                W(GuPath.Light, GuPath.Moon);
                S(GuPath.Dark, GuPath.Poison);
            }

            if (id.Contains("stone") || id.Contains("guardian") || id.Contains("golem"))
            {
                W(GuPath.Strength, GuPath.Earth);
                S(GuPath.Wind, GuPath.Moon);
            }

            if (look == "bear" || look == "boar")
            {
                S(GuPath.Strength);
            }

            if (id.Contains("blood"))
            {
                W(GuPath.Light);
                S(GuPath.Blood);
            }

            if (id.Contains("storm") || id.Contains("thunder"))
            {
                S(GuPath.Lightning);
                W(GuPath.Earth);
            }

            if (id.Contains("lake") || id.Contains("marsh") || id.Contains("toad"))
            {
                W(GuPath.Lightning, GuPath.Ice);
                S(GuPath.Water);
            }

            // An elite brings its own: a frosty one fears fire, a burning one water…
            switch (enemy.Elite)
            {
                case "frost": W(GuPath.Fire); S(GuPath.Ice); break;
                case "burning": W(GuPath.Water); S(GuPath.Fire); break;
                case "venom": S(GuPath.Poison); break;
                case "stone": S(GuPath.Wind, GuPath.Moon); W(GuPath.Strength); break;
            }

            var result = (weak.ToArray(), strong.ToArray());

            lock (cache)
            {
                cache[key] = result;
            }

            return result;
        }

        /// <summary>The factor a blow of a path makes against a creature (and +1 weak, −1 strong, 0).</summary>
        public static float Factor(EnemyDef enemy, GuPath path, out int sign)
        {
            var (weak, strong) = Of(enemy);
            sign = Array.IndexOf(weak, path) >= 0 ? 1 : Array.IndexOf(strong, path) >= 0 ? -1 : 0;

            return sign > 0 ? Weak : sign < 0 ? Strong : 1;
        }

        public static string Names(GuPath[] paths)
        {
            var names = new List<string>();

            foreach (var path in paths)
            {
                names.Add(GuBook.PathName(path).Replace("Путь ", "").ToLowerInvariant());
            }

            return string.Join(", ", names);
        }
    }
}
