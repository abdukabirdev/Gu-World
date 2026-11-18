using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The three ways a fall can go (the author, 18.11): the lantern (wake where the hero last touched one,
    /// a share of the stones lost — the path decides how much); rebirth (as if born again: the game goes
    /// back to the start of the arc before this one, and only memories come along — the codex, the
    /// achievements, the hints); hardcore (one life: a fall ends the playthrough and its save).
    /// </summary>
    public static class DeathRules
    {
        public static readonly DeathRule[] All = { DeathRule.Lantern, DeathRule.Rebirth, DeathRule.Hardcore };

        public static string Name(DeathRule rule)
        {
            switch (rule)
            {
                case DeathRule.Rebirth: return "Перерождение";
                case DeathRule.Hardcore: return "Одна жизнь";
                default: return "Лунный фонарь";
            }
        }

        public static string Text(DeathRule rule)
        {
            switch (rule)
            {
                case DeathRule.Rebirth: return "Павший рождается заново в начале прошлой арки. С собой — только память: кодекс, достижения, подсказки.";
                case DeathRule.Hardcore: return "Одна жизнь на всё прохождение. Смерть — конец пути: сохранение удаляется.";
                default: return "Очнуться у фонаря, которого касались последним. Путь решает, сколько камней заберёт смерть.";
            }
        }

        /// <summary>The arc a rebirth goes back to from a chapter: the one before it (the first arc goes back to its own start).</summary>
        public static int RebirthArc(int chapter) => chapter <= 1 ? 1 : chapter - 1;

        /// <summary>
        /// The save a rebirth starts from: the arc's start as it was then, with the life just lost's
        /// memories (the codex, the achievements, the hints shown), its time played, its wins and its
        /// falls (one more), and one rebirth more.
        /// </summary>
        public static SaveData Reborn(SaveData arcStart, SaveData life)
        {
            var reborn = arcStart;
            reborn.Codex = Union(arcStart.Codex, life.Codex);
            reborn.Achievements = Union(arcStart.Achievements, life.Achievements);
            reborn.Hints = Union(arcStart.Hints, life.Hints);
            reborn.PlayedSeconds = life.PlayedSeconds;
            reborn.Kills = life.Kills;
            reborn.Deaths = life.Deaths + 1;
            reborn.Rebirths = life.Rebirths + 1;
            reborn.Settings = life.Settings;

            return reborn;
        }

        static List<string> Union(List<string> a, List<string> b)
        {
            var all = new List<string>(a ?? new List<string>());

            foreach (var item in b ?? new List<string>())
            {
                if (!all.Contains(item))
                {
                    all.Add(item);
                }
            }

            return all;
        }
    }
}
