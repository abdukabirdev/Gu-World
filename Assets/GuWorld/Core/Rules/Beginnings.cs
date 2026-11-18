using System;
using System.Text;

namespace GuWorld.Core
{
    /// <summary>A way to begin a new game: the path, which is both the difficulty and the talent one is born with.</summary>
    public class Beginning
    {
        public Difficulty Path;
        /// <summary>The share of the aperture the hero is born with (0…1).</summary>
        public float Talent;
        /// <summary>The chance (0…1) to be born with the Ten Extreme Physique instead (talent 100%).</summary>
        public float ExtremeChance;
        /// <summary>How hard, 1…5 (the dots on the path's card).</summary>
        public int Dots;
        public string About;

        public string Name => DifficultyRules.Name(Path);
        public string Grade => Cultivation.TalentName(Talent);
    }

    /// <summary>
    /// The five ways to begin (main's "path" page, lore: sys_aptitude, sys_ten_extreme_physique). The
    /// talent sets how much essence the aperture holds — and so how often the Gu can be used and which
    /// Gu can be refined at all (Session.WhyNoRefine). The story raises the talent twice (the Hermit's
    /// legacy, the Thunder Crown's core): by the same amount on every path, up to 100%.
    /// </summary>
    public static class Beginnings
    {
        /// <summary>The path the game's numbers were balanced on, and what Session.NewGame(name) plays.</summary>
        public const Difficulty Reference = Difficulty.Hard;

        /// <summary>The path the new game page offers first.</summary>
        public const Difficulty Default = Difficulty.Normal;

        public static readonly Beginning[] All =
        {
            new Beginning
            {
                Path = Difficulty.Easy, Talent = 0.88f, ExtremeChance = 0.1f, Dots = 1,
                About = "Талант A, 88% — гордость семьи. Море велико, враги мягче, смерть почти ничего не стоит. " +
                        "Один шанс из десяти родиться с Телосложением Десяти Крайностей: 100%.",
            },
            new Beginning
            {
                Path = Difficulty.Normal, Talent = 0.62f, Dots = 2,
                About = "Талант B, 62%. Крепкий ученик: моря хватает на честный бой, ошибки прощаются.",
            },
            new Beginning
            {
                Path = Difficulty.Hard, Talent = 0.44f, Dots = 3,
                About = "Талант C, 44% — как у большинства детей клана. Игра такая, какой её задумали: каждый камень и каждый Гу на счету.",
            },
            new Beginning
            {
                Path = Difficulty.Unbearable, Talent = 0.36f, Dots = 4,
                About = "Талант D, 36%. Пять-шесть приёмов — и море пусто. Враги крепче, лавки дороже, смерть забирает больше.",
            },
            new Beginning
            {
                Path = Difficulty.Impossible, Talent = 0.30f, Dots = 5,
                About = "Талант D, 30%. Клан махнул на вас рукой, а гора не прощает: враги почти вдвое опаснее, смерть забирает четверть камней. Выжить — уже победа.",
            },
        };

        public static Beginning Of(Difficulty path)
        {
            foreach (var way in All)
            {
                if (way.Path == path)
                {
                    return way;
                }
            }

            return All[1];
        }

        /// <summary>The essence a rank one aperture holds on a path (its sea, for the card).</summary>
        public static float Sea(Difficulty path) => Cultivation.EssenceScale(1) * Of(path).Talent;
    }

    /// <summary>The hero's name as the player types it, cleaned.</summary>
    public static class Look
    {
        public const int NameLength = 12;
        public const string DefaultName = "Лань";

        static readonly string[] Given = { "Лань", "Фэн", "Мин", "Юань", "Шань", "Цин", "Хуа", "Мэй", "Тао", "Сюэ", "Лин", "Чэнь", "Бо", "Жуй", "Юнь", "Сун", "Ань", "Лэй", "Нин", "Сяо Юй" };

        /// <summary>A name of the valley's sort, by chance.</summary>
        public static string RandomName(Random random) => Given[random.Next(Given.Length)];

        /// <summary>Letters, digits, spaces, hyphens and apostrophes only; spaces squeezed; at most NameLength; empty → DefaultName.</summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return DefaultName;
            }

            var clean = new StringBuilder();
            var space = false;

            foreach (var c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '\'')
                {
                    clean.Append(c);
                    space = false;
                }
                else if (char.IsWhiteSpace(c) && !space && clean.Length > 0)
                {
                    clean.Append(' ');
                    space = true;
                }

                if (clean.Length >= NameLength)
                {
                    break;
                }
            }

            var result = clean.ToString().Trim();

            return result.Length == 0 ? DefaultName : char.ToUpperInvariant(result[0]) + result.Substring(1);
        }
    }
}
