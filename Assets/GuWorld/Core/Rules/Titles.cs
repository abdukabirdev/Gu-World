using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// Titles won by deeds (ROADMAP 12.4: «титулы за достижения — показывает Статус, видят NPC»): some
    /// achievements give the hero a name people call them by. The highest won is the one worn; people say it
    /// in their words ({title}); without one, the hero is what the clan calls its young — «ученик».
    /// </summary>
    public static class Titles
    {
        /// <summary>The achievements that give a title, the highest first, and the title.</summary>
        public static readonly (string achievement, string title)[] Ordered =
        {
            ("rank_five", "Вершина смертных"),
            ("path_grandmaster", "Великий мастер"),
            ("clan_elder", "Старейшина"),
            ("all_bosses", "Прошедший гору"),
            ("slayer", "Гроза горы"),
            ("rank_four", "Мастер четвёртого ранга"),
            ("treaty", "Миротворец"),
            ("tides", "Сторож деревни"),
            ("mortal_kin", "Заступник"),
            ("bai_kin", "Белая кровь"),
            ("xiong_kin", "Медвежья лапа"),
            ("tide", "Стена против прилива"),
            ("raid", "Защитник хутора"),
            ("path_master", "Мастер пути"),
            ("wolfbane", "Волчья погибель"),
            ("fuser", "Мастер слияния"),
            ("alchemist", "Алхимик"),
            ("traveller", "Странник"),
            ("hunter", "Охотник"),
            ("clan_member", "Свой в клане"),
        };

        /// <summary>What people call one without a title.</summary>
        public const string None = "ученик";

        /// <summary>The highest title won ("" if none).</summary>
        public static string Of(SaveData state)
        {
            if (state?.Achievements == null)
            {
                return "";
            }

            foreach (var (achievement, title) in Ordered)
            {
                if (state.Achievements.Contains(achievement))
                {
                    return title;
                }
            }

            return "";
        }

        /// <summary>Every title won, the highest first.</summary>
        public static List<string> All(SaveData state)
        {
            var list = new List<string>();

            foreach (var (achievement, title) in Ordered)
            {
                if (state?.Achievements != null && state.Achievements.Contains(achievement))
                {
                    list.Add(title);
                }
            }

            return list;
        }

        /// <summary>How people address the hero: by the title, in lower case as in speech; else «ученик».</summary>
        public static string Address(SaveData state)
        {
            var title = Of(state);

            return title == "" ? None : title;
        }
    }

    public partial class Session
    {
        /// <summary>The title the hero wears ("" if none yet).</summary>
        public string Title => Titles.Of(State);
    }
}
