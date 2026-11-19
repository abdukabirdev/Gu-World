using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>What a clan's patrol does when it meets the hero.</summary>
    public enum PatrolStance
    {
        /// <summary>Lets them by, with a nod.</summary>
        Pass,
        /// <summary>Calls out and gives time to leave; strikes if they stay, come close or strike first.</summary>
        Warn,
        /// <summary>A shout, and then the fight.</summary>
        Fight,
    }

    /// <summary>
    /// The clans' patrols (ROADMAP 12.9): posts at the gates and patrols on the roads around the villages of
    /// Bai and Xiong (and the hero's own Gu Yue). What they do comes from what the clan thinks of the hero:
    /// friends are let by, the indifferent and the disliked are told to leave, the hated are attacked. A
    /// clan's own man (Gu Yue for the hero) is let by unless the clan has turned on them. Fallen men of a
    /// post come back after a while of the game's time.
    /// </summary>
    public static class Patrols
    {
        /// <summary>The hero's own clan: its patrols are kinder to them.</summary>
        public const string HomeClan = "gu_yue";

        /// <summary>Hours of the game's time until a post's fallen men are replaced.</summary>
        public const float ReturnHours = 6;

        public static PatrolStance StanceOf(string clan, Standing standing)
        {
            if (standing == Standing.Hated)
            {
                return PatrolStance.Fight;
            }

            if (standing >= Standing.Friendly || (clan == HomeClan && standing >= Standing.Neutral))
            {
                return PatrolStance.Pass;
            }

            return PatrolStance.Warn;
        }

        /// <summary>Seconds a patrol gives the hero to go before it strikes.</summary>
        public static float WarnSeconds(PatrolStance stance) => stance == PatrolStance.Warn ? 8 : stance == PatrolStance.Fight ? 2 : 0;

        /// <summary>Closer than this, a warning patrol strikes at once.</summary>
        public const float TooClose = 7;

        /// <summary>What a patrol calls out (the clan's name in the words).</summary>
        public static string Line(PatrolStance stance, string clanName)
        {
            switch (stance)
            {
                case PatrolStance.Pass: return string.IsNullOrEmpty(clanName) ? "«Проходи.»" : $"«Проходи, друг клана {Short(clanName)}.»";
                case PatrolStance.Fight: return "«Это он! Взять!»";
                default: return string.IsNullOrEmpty(clanName) ? "«Уходи, пока цел!»" : $"«Земля клана {Short(clanName)}. Уходи, пока цел!»";
            }
        }

        /// <summary>What a patrol says to a hero it lets by, who has a title: by the title (ROADMAP 12.4: «видят NPC»).</summary>
        public static string Greeting(string clanName, string title) =>
            string.IsNullOrEmpty(title) ? Line(PatrolStance.Pass, clanName) : $"«Проходи, {title}. Слава о тебе дошла и до нас.»";

        /// <summary>"Клан Бай" → "Бай".</summary>
        static string Short(string clanName) => clanName.StartsWith("Клан ") ? clanName.Substring(5) : clanName;

        /// <summary>What striking a patrol that let the hero by costs with its clan.</summary>
        public const int StruckFriendCost = 6;

        // ------------------------------------------------------------------ Roads walked

        /// <summary>The length of a route, metres.</summary>
        public static float Length(MapPoint[] route)
        {
            var total = 0f;

            for (var i = 1; i < route.Length; i++)
            {
                total += route[i - 1].DistanceTo(route[i]);
            }

            return total;
        }

        /// <summary>How far along a route (metres from its start) its point nearest `p` lies, and how far `p` is from it.</summary>
        public static float Closest(MapPoint[] route, MapPoint p, out float distance)
        {
            distance = float.MaxValue;
            var best = 0f;
            var walked = 0f;

            if (route == null || route.Length == 0)
            {
                return 0;
            }

            if (route.Length == 1)
            {
                distance = route[0].DistanceTo(p);

                return 0;
            }

            for (var i = 1; i < route.Length; i++)
            {
                var a = route[i - 1];
                var b = route[i];
                var dx = b.X - a.X;
                var dz = b.Z - a.Z;
                var length = (float)Math.Sqrt(dx * dx + dz * dz);
                var t = length < 0.001f ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Z - a.Z) * dz) / (length * length)));
                var on = new MapPoint(a.X + dx * t, a.Z + dz * t);
                var far = on.DistanceTo(p);

                if (far < distance)
                {
                    distance = far;
                    best = walked + length * t;
                }

                walked += length;
            }

            return best;
        }

        /// <summary>The point `metres` from a route's start (clamped to its ends).</summary>
        public static MapPoint At(MapPoint[] route, float metres)
        {
            if (route == null || route.Length == 0)
            {
                return new MapPoint();
            }

            if (metres <= 0 || route.Length == 1)
            {
                return route[0];
            }

            for (var i = 1; i < route.Length; i++)
            {
                var length = route[i - 1].DistanceTo(route[i]);

                if (metres <= length)
                {
                    var t = length < 0.001f ? 0 : metres / length;

                    return new MapPoint(route[i - 1].X + (route[i].X - route[i - 1].X) * t, route[i - 1].Z + (route[i].Z - route[i - 1].Z) * t);
                }

                metres -= length;
            }

            return route[route.Length - 1];
        }

        /// <summary>
        /// Where on its route a patrol that comes out now should stand: on the road near the hero, but not on
        /// top of them — moved along the road until it is at least `apart` metres away.
        /// </summary>
        public static float ComeOutAt(MapPoint[] route, MapPoint hero, float apart)
        {
            var along = Closest(route, hero, out var distance);

            if (distance >= apart)
            {
                return along;
            }

            var length = Length(route);
            var shift = (float)Math.Sqrt(Math.Max(0, apart * apart - distance * distance)) + 1;
            // Ahead if the road goes on that way far enough, else behind.
            return along + shift <= length ? along + shift : Math.Max(0, along - shift);
        }
    }

    /// <summary>Where a blow lands on a man: the head hurts more, the legs slow them (ROADMAP 12.4).</summary>
    public enum HitZone { Body, Head, Legs }

    public static class HitZones
    {
        /// <summary>Only those built like people have a head and legs to aim at (beasts and bugs are hit as one).</summary>
        public static bool Applies(EnemyBody body) => body == EnemyBody.Human;

        /// <summary>The zone at a height on the body, as a share of its height (0 = feet, 1 = the top of the head).</summary>
        public static HitZone Of(float share) => share >= 0.82f ? HitZone.Head : share <= 0.38f ? HitZone.Legs : HitZone.Body;

        public static float Damage(HitZone zone) => zone == HitZone.Head ? 1.5f : zone == HitZone.Legs ? 0.85f : 1;

        /// <summary>How much slower a leg hit makes them, and for how long.</summary>
        public const float LegSlow = 0.35f;
        public const float LegSlowSeconds = 3;

        /// <summary>A head hit counts as a critical one (the larger number).</summary>
        public static bool Critical(HitZone zone) => zone == HitZone.Head;
    }

    /// <summary>Something coming at a place, to show on the compass and the map: a raid, a tide.</summary>
    public struct Threat
    {
        public string Name;
        public MapPoint At;
        /// <summary>Already on (not only foretold).</summary>
        public bool Now;
    }

    public partial class Session
    {
        /// <summary>What a clan's patrol does when it meets the hero now.</summary>
        public PatrolStance PatrolStanceOf(string clan) => Patrols.StanceOf(clan, StandingWith(clan));

        /// <summary>The hero struck a patrol that was letting them by: the clan takes it ill.</summary>
        public void StruckFriendlyPatrol(string clan)
        {
            if (string.IsNullOrEmpty(clan))
            {
                return;
            }

            ChangeReputation(clan, -Patrols.StruckFriendCost, false);
        }

        // ------------------------------------------------------------------ Foretold

        /// <summary>Raised once when a raid or a tide is foretold, hours before it comes: a title and what is seen or heard.</summary>
        public event Action<string, string> Forewarned;

        /// <summary>The hours before a raid when its smoke and birds are seen.</summary>
        public bool RaidSoon(float hour) => State.Day % 9 == 4 && hour >= 7 && hour < 10 && !Has("black_tail_beaten") && !Has("raid_held_" + State.Day) && !InSandbox;

        /// <summary>The evening before a tide's night, when the wolves are heard on the slopes.</summary>
        public bool TideSoon(float hour) => hour >= 18 && hour < 21 && Rank >= 2 && State.Day > 0 && State.Day % TideEvery == 0 && !Has("tide_held_" + State.Day) && !InSandbox;

        void UpdateForewarnings(float hour)
        {
            if (RaidSoon(hour) && !Has("raid_warned_" + State.Day))
            {
                SetFlag("raid_warned_" + State.Day);
                Forewarned?.Invoke("Дым на западе", "Над лесом у Ивового хутора — дым, птицы с криком летят прочь. Разбойники близко.");
            }

            if (TideSoon(hour) && !Has("tide_warned_" + State.Day))
            {
                SetFlag("tide_warned_" + State.Day);
                Forewarned?.Invoke("Вой на склонах", "Волки воют со всех сторон горы. Этой ночью — прилив у северных ворот.");
            }
        }

        /// <summary>The raids and tides that are on or foretold now, for the compass and the map.</summary>
        public List<Threat> Threats()
        {
            var list = new List<Threat>();
            var hour = State.Hour;
            var raid = Atlas.Encounter(Raid);
            var tide = Atlas.Encounter(Tide);

            if (raid != null && (RaidUp || RaidSoon(hour)))
            {
                list.Add(new Threat { Name = raid.Name, At = raid.At, Now = RaidUp });
            }

            if (tide != null && (TideUp || TideSoon(hour)))
            {
                list.Add(new Threat { Name = tide.Name, At = tide.At, Now = TideUp });
            }

            return list;
        }
    }
}
