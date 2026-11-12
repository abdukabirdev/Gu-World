using System;

namespace GuWorld.Core
{
    /// <summary>How each difficulty changes the numbers of the game.</summary>
    public static class DifficultyRules
    {
        public static string Name(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return "Сказание";
                case Difficulty.Hard: return "Испытание";
                case Difficulty.Demonic: return "Демонический путь";
                default: return "Путь мастера";
            }
        }

        public static string Text(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return "Враги слабее, смерть почти ничего не стоит. Для тех, кто пришёл за историей.";
                case Difficulty.Hard: return "Враги крепче и бьют больнее, лавки дороже.";
                case Difficulty.Demonic: return "Гора не прощает. Враги вдвое опаснее, смерть забирает четверть камней.";
                default: return "Как задумано.";
            }
        }

        /// <summary>Share of each blow on the hero.</summary>
        public static float DamageTaken(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return 0.6f;
                case Difficulty.Hard: return 1.35f;
                case Difficulty.Demonic: return 1.7f;
                default: return 1;
            }
        }

        /// <summary>Share of an enemy's health.</summary>
        public static float EnemyHealth(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return 0.7f;
                case Difficulty.Hard: return 1.3f;
                case Difficulty.Demonic: return 1.6f;
                default: return 1;
            }
        }

        public static float Insight(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return 1.25f;
                case Difficulty.Demonic: return 1.1f;
                default: return 1;
            }
        }

        public static float Prices(Difficulty difficulty) => difficulty >= Difficulty.Hard ? 1.2f : 1;

        /// <summary>The share of the stones a fall costs.</summary>
        public static float DeathLoss(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return 0.03f;
                case Difficulty.Hard: return 0.15f;
                case Difficulty.Demonic: return 0.25f;
                default: return 0.1f;
            }
        }
    }

    public enum Sky { Clear, Cloudy, Rain, Storm, Fog, Snow }

    /// <summary>
    /// The weather, from the day and the hour alone (so every machine sees the same sky on the same day
    /// of a save), and what it changes: the moon's path is stronger at night, lightning in a storm, ice
    /// in snow; fire is weaker in the rain; fog shortens sight.
    /// </summary>
    public static class Weather
    {
        /// <summary>The sky at a day and hour, at a height (snow above the snow line instead of rain).</summary>
        public static Sky At(int day, float hour, float height = 0)
        {
            // Two weather changes a day: one for the first half, one for the second.
            var half = hour < 12 ? 0 : 1;
            var roll = Hash(day * 2 + half) % 100;
            var sky = roll < 46 ? Sky.Clear : roll < 66 ? Sky.Cloudy : roll < 80 ? Sky.Rain : roll < 88 ? Sky.Storm : Sky.Fog;

            // Mornings are foggy in the valleys more often.
            if (hour < 8 && Hash(day * 7 + 3) % 3 == 0 && sky != Sky.Storm)
            {
                sky = Sky.Fog;
            }

            if (height > SnowLine && (sky == Sky.Rain || sky == Sky.Storm || sky == Sky.Cloudy))
            {
                sky = Sky.Snow;
            }

            return sky;
        }

        /// <summary>The height above which the rain falls as snow, metres.</summary>
        public const float SnowLine = 420;

        public static string Name(Sky sky)
        {
            switch (sky)
            {
                case Sky.Cloudy: return "Облачно";
                case Sky.Rain: return "Дождь";
                case Sky.Storm: return "Гроза";
                case Sky.Fog: return "Туман";
                case Sky.Snow: return "Снег";
                default: return "Ясно";
            }
        }

        /// <summary>How much the sky and the hour strengthen (or weaken) a path's Gu.</summary>
        public static float PathFactor(GuPath path, Sky sky, bool night)
        {
            var factor = 1f;

            if (night && (path == GuPath.Moon || path == GuPath.Dark))
            {
                factor += 0.15f;
            }

            if (!night && path == GuPath.Light)
            {
                factor += 0.1f;
            }

            switch (sky)
            {
                case Sky.Storm:
                    if (path == GuPath.Lightning) factor += 0.25f;
                    if (path == GuPath.Fire) factor -= 0.15f;
                    break;
                case Sky.Rain:
                    if (path == GuPath.Water) factor += 0.15f;
                    if (path == GuPath.Fire) factor -= 0.2f;
                    if (path == GuPath.Lightning) factor += 0.1f;
                    break;
                case Sky.Snow:
                    if (path == GuPath.Ice) factor += 0.2f;
                    if (path == GuPath.Fire) factor -= 0.1f;
                    break;
                case Sky.Fog:
                    if (path == GuPath.Dark || path == GuPath.Water) factor += 0.1f;
                    break;
                case Sky.Clear:
                    if (path == GuPath.Fire && !night) factor += 0.05f;
                    break;
            }

            return factor;
        }

        /// <summary>How far enemies see, as a share, in this sky and hour.</summary>
        public static float SightFactor(Sky sky, bool night)
        {
            var factor = night ? 0.7f : 1f;

            if (sky == Sky.Fog)
            {
                factor *= 0.55f;
            }
            else if (sky == Sky.Rain || sky == Sky.Snow)
            {
                factor *= 0.85f;
            }

            return factor;
        }

        /// <summary>Whether lightning may strike the hero in the open (a storm, high up, no roof): the interface rolls the strike.</summary>
        public static bool LightningWeather(Sky sky, float height) => sky == Sky.Storm && height > 120;

        static int Hash(int n)
        {
            unchecked
            {
                var h = (uint)n * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;

                return (int)(h & 0x7FFFFFFF);
            }
        }
    }

    /// <summary>A status on an enemy (the hero's Gu leave them): the same kinds as on the hero, kept by the enemy's body in the engine.</summary>
    public class EnemyStatuses
    {
        public float Poison, PoisonTime;
        public float Burn, BurnTime;
        public float Bleed, BleedTime;
        public float Slow, SlowTime;
        public float ChillTime;
        public float StunTime;
        public float FreezeTime;

        public bool Held => StunTime > 0 || FreezeTime > 0;
        public bool Chilled => ChillTime > 0;

        /// <summary>The share of its speed it keeps.</summary>
        public float SpeedFactor => Held ? 0 : 1 - Math.Min(0.8f, Math.Max(SlowTime > 0 ? Slow : 0, ChillTime > 0 ? 0.3f : 0));

        /// <summary>A hit left something on it. A chill on the chilled freezes; poise stops stuns from weak blows (decided by the caller).</summary>
        public void Apply(StatusKind kind, float power, float seconds)
        {
            switch (kind)
            {
                case StatusKind.Poison: Poison = Math.Max(Poison, power); PoisonTime = Math.Max(PoisonTime, seconds); break;
                case StatusKind.Burn: Burn = Math.Max(Burn, power); BurnTime = Math.Max(BurnTime, seconds); break;
                case StatusKind.Bleed: Bleed = Math.Max(Bleed, power); BleedTime = Math.Max(BleedTime, seconds); break;
                case StatusKind.Slow: Slow = Math.Max(Slow, power); SlowTime = Math.Max(SlowTime, seconds); break;
                case StatusKind.Stun: StunTime = Math.Max(StunTime, seconds); break;
                case StatusKind.Freeze: FreezeTime = Math.Max(FreezeTime, seconds); break;
                case StatusKind.Chill:
                    if (ChillTime > 0)
                    {
                        ChillTime = 0;
                        FreezeTime = Math.Max(FreezeTime, 1.5f);
                    }
                    else
                    {
                        ChillTime = seconds;
                    }

                    break;
            }
        }

        /// <summary>A frame: the damage it takes from its statuses (armour does not stop poison; the caller applies armour to the rest).</summary>
        public float Tick(float dt, float armour)
        {
            var damage = 0f;

            if (PoisonTime > 0) { damage += Poison * dt; PoisonTime -= dt; }
            if (BurnTime > 0) { damage += Burn * dt * (1 - armour); BurnTime -= dt; }
            if (BleedTime > 0) { damage += Bleed * dt * (1 - armour); BleedTime -= dt; }

            SlowTime = Math.Max(0, SlowTime - dt);
            ChillTime = Math.Max(0, ChillTime - dt);
            StunTime = Math.Max(0, StunTime - dt);
            FreezeTime = Math.Max(0, FreezeTime - dt);

            return damage;
        }

        /// <summary>The worst thing on it, for the colour of its health bar.</summary>
        public StatusKind Worst
        {
            get
            {
                if (FreezeTime > 0) return StatusKind.Freeze;
                if (StunTime > 0) return StatusKind.Stun;
                if (BurnTime > 0) return StatusKind.Burn;
                if (PoisonTime > 0) return StatusKind.Poison;
                if (BleedTime > 0) return StatusKind.Bleed;
                if (ChillTime > 0) return StatusKind.Chill;
                if (SlowTime > 0) return StatusKind.Slow;

                return StatusKind.None;
            }
        }
    }

    /// <summary>
    /// How a Gu master among the enemies chooses what to cast: the Gu it carries, each with its own
    /// cooldown, picked by what suits the distance (a buff when the hero is close, a bolt from afar,
    /// a cone or a nova when the hero is in reach), and the distance it wants to keep.
    /// </summary>
    public class CasterMind
    {
        public readonly GuDef[] Gu;
        readonly float[] ready;
        int turn;

        public CasterMind(string[] gu)
        {
            var list = new System.Collections.Generic.List<GuDef>();

            foreach (var id in gu)
            {
                var def = GuBook.Get(id);

                if (def != null && def.Skill != null)
                {
                    list.Add(def);
                }
            }

            Gu = list.ToArray();
            ready = new float[Gu.Length];

            // The killer moves it can make of its Gu: the core and at least one of the supports (a master improvises the rest).
            var moves = new System.Collections.Generic.List<GuDef>();

            foreach (var move in KillerMoves.All)
            {
                if (Array.IndexOf(gu, move.Core) < 0)
                {
                    continue;
                }

                var supported = false;

                foreach (var support in move.Supports)
                {
                    supported |= Array.IndexOf(gu, support) >= 0;
                }

                if (supported)
                {
                    var core = GuBook.Get(move.Core);
                    moves.Add(new GuDef { Id = move.Id, Name = move.Name, Seal = move.Seal, Rank = move.Rank, Path = core.Path, Kind = GuKind.Active, Colour = move.Colour, Skill = move.Skill });
                }
            }

            Moves = moves.ToArray();
            moveReady = new float[Moves.Length];

            for (var i = 0; i < moveReady.Length; i++)
            {
                // Not at once: a master opens with their Gu and keeps the move for later.
                moveReady[i] = 6 + 3 * i;
            }
        }

        /// <summary>The killer moves this master knows, as Gu-like defs (a move's skill, name and colour).</summary>
        public readonly GuDef[] Moves;
        readonly float[] moveReady;

        /// <summary>The distance it likes to fight at: close for melee buffs, the reach of its bolts otherwise.</summary>
        public float PreferredDistance
        {
            get
            {
                var best = 2.2f;

                foreach (var gu in Gu)
                {
                    if (gu.Skill.Shape == SkillShape.Projectile || gu.Skill.Shape == SkillShape.Fan || gu.Skill.Shape == SkillShape.Chain || gu.Skill.Shape == SkillShape.Storm)
                    {
                        best = Math.Max(best, gu.Skill.Range * 0.7f);
                    }
                }

                return best;
            }
        }

        public void Tick(float dt)
        {
            for (var i = 0; i < ready.Length; i++)
            {
                ready[i] = Math.Max(0, ready[i] - dt);
            }

            for (var i = 0; i < moveReady.Length; i++)
            {
                moveReady[i] = Math.Max(0, moveReady[i] - dt);
            }
        }

        /// <summary>A killer move to unleash now (null if none is ready or suits); its wait is long.</summary>
        public GuDef ChooseMove(float distance, bool hurt)
        {
            for (var i = 0; i < Moves.Length; i++)
            {
                if (moveReady[i] <= 0 && Suits(Moves[i].Skill, distance, hurt))
                {
                    moveReady[i] = Moves[i].Skill.Cooldown * 3 + 8;

                    return Moves[i];
                }
            }

            return null;
        }

        /// <summary>The Gu to cast now at a hero this far away (null: none suits or all wait). Starts its cooldown, longer than a hero's.</summary>
        public GuDef Choose(float distance, bool hurt)
        {
            for (var tries = 0; tries < Gu.Length; tries++)
            {
                var index = (turn + tries) % Gu.Length;
                var gu = Gu[index];

                if (ready[index] > 0 || !Suits(gu.Skill, distance, hurt))
                {
                    continue;
                }

                turn = index + 1;
                ready[index] = Math.Max(1.5f, gu.Skill.Cooldown * 1.8f + 0.8f);

                return gu;
            }

            return null;
        }

        static bool Suits(SkillDef skill, float distance, bool hurt)
        {
            switch (skill.Shape)
            {
                case SkillShape.Projectile:
                case SkillShape.Fan:
                case SkillShape.Chain:
                    return distance <= skill.Range && distance > 2;
                case SkillShape.Storm:
                case SkillShape.Pull:
                    return distance <= skill.Range && distance > 4;
                case SkillShape.Cone:
                    return distance <= skill.Range;
                case SkillShape.Nova:
                    return distance <= skill.Radius;
                case SkillShape.Dash:
                case SkillShape.Blink:
                    return distance > 6 && distance <= skill.Range + 4;
                case SkillShape.Heal:
                    return hurt;
                case SkillShape.Buff:
                    return skill.Buff == BuffKind.Shield || skill.Buff == BuffKind.Unstoppable ? hurt || distance < 8 : distance < 10;
                default:
                    return false;
            }
        }
    }
}
