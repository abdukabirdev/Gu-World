using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// Attainment on the paths: the understanding a Gu master gathers of a path by using its Gu. Every
    /// bit of mastery a Gu earns is also the path's; fusing a Gu of a path and comprehending a killer move
    /// through it add more. The grades (ordinary, quasi-master, master, quasi-grandmaster, grandmaster,
    /// great grandmaster) make that path's Gu stronger and cheaper, and its killer moves easier to
    /// comprehend.
    /// </summary>
    public static class Attainment
    {
        public static readonly string[] Grades = { "Обычный", "Почти мастер", "Мастер", "Почти великий мастер", "Великий мастер", "Величайший мастер" };
        /// <summary>Points to reach each grade.</summary>
        public static readonly int[] Needs = { 0, 150, 600, 1800, 4500, 11000 };
        /// <summary>Power of the path's Gu added at each grade.</summary>
        public static readonly float[] Power = { 0, 0.05f, 0.1f, 0.16f, 0.24f, 0.34f };
        /// <summary>Essence the path's Gu take less at each grade.</summary>
        public static readonly float[] Thrift = { 0, 0.02f, 0.05f, 0.08f, 0.12f, 0.16f };

        public static int Grade(int points)
        {
            var grade = 0;

            for (var i = 1; i < Needs.Length; i++)
            {
                if (points >= Needs[i])
                {
                    grade = i;
                }
            }

            return grade;
        }

        /// <summary>How far to the next grade, 0–1 (1 at the top).</summary>
        public static float Share(int points)
        {
            var grade = Grade(points);

            return grade >= Needs.Length - 1 ? 1 : (points - Needs[grade]) / (float)(Needs[grade + 1] - Needs[grade]);
        }
    }

    public partial class Session
    {
        /// <summary>Raised when a path's grade rises.</summary>
        public event Action<GuPath, int> AttainmentRose;

        public int AttainmentOf(GuPath path)
        {
            var key = path.ToString();

            foreach (var tally in State.Attainment)
            {
                if (tally.Id == key)
                {
                    return tally.Count;
                }
            }

            return 0;
        }

        public int GradeOf(GuPath path) => Attainment.Grade(AttainmentOf(path));

        public void GainAttainment(GuPath path, int points)
        {
            if (points <= 0 || InSandbox)
            {
                return;
            }

            var key = path.ToString();
            var tally = State.Attainment.Find(t => t.Id == key);

            if (tally == null)
            {
                tally = new Tally { Id = key };
                State.Attainment.Add(tally);
            }

            var before = Attainment.Grade(tally.Count);
            tally.Count += points;
            var after = Attainment.Grade(tally.Count);

            if (after > before)
            {
                Say($"{GuBook.PathName(path)}: {Attainment.Grades[after].ToLowerInvariant()}");
                AttainmentRose?.Invoke(path, after);
                Achieve();
                Touch();
            }
        }

        /// <summary>The paths the hero has any attainment on, the highest first.</summary>
        public List<(GuPath path, int points)> Attainments()
        {
            var list = new List<(GuPath, int)>();

            foreach (GuPath path in Enum.GetValues(typeof(GuPath)))
            {
                var points = AttainmentOf(path);

                if (points > 0)
                {
                    list.Add((path, points));
                }
            }

            list.Sort((a, b) => b.Item2.CompareTo(a.Item2));

            return list;
        }

        /// <summary>The best grade on any path (for the achievements).</summary>
        public int BestGrade()
        {
            var best = 0;

            foreach (var (_, points) in Attainments())
            {
                best = Math.Max(best, Attainment.Grade(points));
            }

            return best;
        }

        float AttainmentPower(GuPath path) => 1 + Attainment.Power[GradeOf(path)];

        float AttainmentThrift(GuPath path) => 1 - Attainment.Thrift[GradeOf(path)];

        /// <summary>The insight a killer move takes to comprehend: a tenth less for each grade of its core's path.</summary>
        public int DeviseCost(KillerMoveDef move)
        {
            var core = GuBook.Get(move.Core);
            var grade = core != null ? GradeOf(core.Path) : 0;

            return (int)Math.Round(move.Insight * (1 - 0.1f * grade));
        }
    }
}
