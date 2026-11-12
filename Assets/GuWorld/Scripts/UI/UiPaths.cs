using System;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.UI
{
    /// <summary>The page of the paths (P): the hero's attainment on each path, its grade and what the grade gives, the Gu they own of it.</summary>
    public partial class Ui
    {
        GuPath pickedPath = GuPath.Moon;

        void PathsPage()
        {
            var session = S;
            var known = session.Attainments();
            var body = Frame("道", "Пути", known.Count > 0 ? $"Лучший: {GuBook.PathName(known[0].path)} · {Attainment.Grades[Attainment.Grade(known[0].points)].ToLowerInvariant()}" : "Постижение путей приходит с каждым применённым Гу", true, null, null,
                new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });

            var left = Column(body, 0, 0.42f, 6);
            var list = Scroll(Rest(left));

            foreach (GuPath path in Enum.GetValues(typeof(GuPath)))
            {
                var p = path;
                var points = session.AttainmentOf(path);
                var grade = Attainment.Grade(points);
                var owned = session.State.Gu.FindAll(g => GuBook.Get(g.Def).Path == path).Count;

                if (points == 0 && owned == 0)
                {
                    continue;
                }

                ListLine(list, PathSeal(path), GuBook.PathName(path), Attainment.Grades[grade], grade >= 2 ? UiKit.GoldText : points > 0 ? UiKit.Text : UiKit.Muted, path == pickedPath, () => { pickedPath = p; Redraw(); },
                    grade >= 2 ? UiKit.GoldText : UiKit.Text2);
            }

            var right = Column(body, 0.48f, 1, 8);
            var mine = session.AttainmentOf(pickedPath);
            var level = Attainment.Grade(mine);
            UiKit.Zh(PathSeal(pickedPath), right, 54, UiKit.Gold).Size(62);
            UiKit.Disp(GuBook.PathName(pickedPath), right, 30, UiKit.Text).Size(38);
            UiKit.Lbl(Attainment.Grades[level], right, 11, UiKit.LabelGold).Size(18);
            UiKit.Bar(right, UiKit.BarKind.Stamina, Attainment.Share(mine), 4);
            Fact(right, "Постижение", level >= Attainment.Needs.Length - 1 ? $"{mine} · вершина" : $"{mine} / {Attainment.Needs[level + 1]}", UiKit.GoldText);
            Fact(right, "Сила Гу пути", $"+{Mathf.RoundToInt(Attainment.Power[level] * 100)}%", UiKit.Essence);
            Fact(right, "Расход эссенции", $"−{Mathf.RoundToInt(Attainment.Thrift[level] * 100)}%", UiKit.Essence);
            Fact(right, "Постижение приёмов", $"−{level * 10}% прозрения");
            Gap(right, 6);
            Para("Постижение растёт само: каждый раз, когда Гу пути набирается мастерства. Слияния и постигнутые приёмы дают больше.", right, 14, UiKit.Muted);
            Gap(right, 6);
            UiKit.Lbl("Гу этого пути у вас", right, 11, UiKit.LabelGold).Size(18);

            foreach (var gu in session.State.Gu)
            {
                var def = GuBook.Get(gu.Def);

                if (def.Path == pickedPath)
                {
                    Fact(right, def.Seal + "  " + def.Name, $"ранг {GuBook.RankNumerals[def.Rank]} · мастерство {Cultivation.MasteryLevel(gu.MasteryXp)}", QualityColour(gu.Quality));
                }
            }
        }

        static string PathSeal(GuPath path)
        {
            switch (path)
            {
                case GuPath.Moon: return "月";
                case GuPath.Earth: return "土";
                case GuPath.Wind: return "风";
                case GuPath.Wood: return "木";
                case GuPath.Lightning: return "雷";
                case GuPath.Light: return "光";
                case GuPath.Wine: return "酒";
                case GuPath.Ice: return "冰";
                case GuPath.Strength: return "力";
                case GuPath.Blood: return "血";
                case GuPath.Fire: return "火";
                case GuPath.Water: return "水";
                case GuPath.Poison: return "毒";
                case GuPath.Sound: return "音";
                case GuPath.Dark: return "暗";
                default: return "骨";
            }
        }
    }
}
