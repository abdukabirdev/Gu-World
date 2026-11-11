using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>Small helpers for the content's lists.</summary>
    public static class Lists
    {
        /// <summary>The arrays one after another (a book's first entries and those added later).</summary>
        public static T[] Join<T>(params T[][] parts)
        {
            var count = 0;

            foreach (var part in parts)
            {
                count += part?.Length ?? 0;
            }

            var all = new T[count];
            var at = 0;

            foreach (var part in parts)
            {
                if (part == null)
                {
                    continue;
                }

                Array.Copy(part, 0, all, at, part.Length);
                at += part.Length;
            }

            return all;
        }

        /// <summary>A lookup by id made once.</summary>
        public static Dictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> id)
        {
            var index = new Dictionary<string, T>();

            foreach (var item in items)
            {
                index[id(item)] = item;
            }

            return index;
        }
    }

    /// <summary>A clan (or a people) the hero's deeds are remembered by.</summary>
    public class ClanDef
    {
        public string Id;
        public string Name;
        public string Seal;
        public int Colour;
        public string Description;
        /// <summary>What the hero is to them at the start of the game, −100…100.</summary>
        public int Start;
        /// <summary>Clans whose friends they dislike: a gain with one of these is a tenth of a loss with this one.</summary>
        public string[] Rivals = new string[0];
        /// <summary>Their guards attack on sight when the hero is hated.</summary>
        public bool Guards = true;
    }

    /// <summary>How a clan thinks of the hero: a name for each band of reputation and what it changes.</summary>
    public enum Standing { Hated, Disliked, Neutral, Friendly, Honoured, Kin }

    /// <summary>The clans of Qing Mao mountain and what reputation means with them.</summary>
    public static class Clans
    {
        public const int Min = -100;
        public const int Max = 100;

        public static readonly ClanDef[] All =
        {
            new ClanDef
            {
                Id = "gu_yue", Name = "Клан Гу Юэ", Seal = "月", Colour = 0xA0D2FF, Start = 10, Rivals = new[] { "bai", "xiong" },
                Description = "Клан Лунного пути у подножия Цинмао. Ваш клан — пока вы ему полезны."
            },
            new ClanDef
            {
                Id = "bai", Name = "Клан Бай", Seal = "白", Colour = 0x9FE8FF, Start = -10, Rivals = new[] { "gu_yue", "xiong" },
                Description = "Ледяной и водный пути, белые стены у водопада за хребтом. Сорок лет спорят с Гу Юэ за источник."
            },
            new ClanDef
            {
                Id = "xiong", Name = "Клан Сюн", Seal = "熊", Colour = 0xE0A070, Start = -5, Rivals = new[] { "gu_yue", "bai" },
                Description = "Путь силы и мёда на передней горе. Грубые, прямые, держат слово — и обиды."
            },
            new ClanDef
            {
                Id = "mortal", Name = "Смертные хутора", Seal = "民", Colour = 0xC9D3D5, Start = 0, Guards = false,
                Description = "Те, у кого нет апертуры. Пашут, платят дань всем трём кланам и помнят, кто им помог."
            },
            new ClanDef
            {
                Id = "bandit", Name = "Чёрный Хвост", Seal = "匪", Colour = 0xF08A74, Start = -60, Rivals = new[] { "gu_yue", "bai", "xiong", "mortal" },
                Description = "Разбойники-мастера Гу из рудника на западе. Платят серебром, режут ножом."
            },
            new ClanDef
            {
                Id = "wanderers", Name = "Странники", Seal = "游", Colour = 0xE8C98A, Start = 0, Guards = false,
                Description = "Одиночки, торговцы и алхимики Южного тракта. Ни клана, ни хозяина."
            },
        };

        static Dictionary<string, ClanDef> byId;

        public static ClanDef Get(string id)
        {
            byId = byId ?? Lists.Index(All, c => c.Id);

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }

        public static Standing StandingOf(int reputation)
        {
            if (reputation <= -50) return Standing.Hated;
            if (reputation <= -15) return Standing.Disliked;
            if (reputation < 20) return Standing.Neutral;
            if (reputation < 50) return Standing.Friendly;
            if (reputation < 80) return Standing.Honoured;

            return Standing.Kin;
        }

        public static string StandingName(Standing standing)
        {
            switch (standing)
            {
                case Standing.Hated: return "Враг";
                case Standing.Disliked: return "Неприязнь";
                case Standing.Neutral: return "Безразличие";
                case Standing.Friendly: return "Дружба";
                case Standing.Honoured: return "Почёт";
                default: return "Свой";
            }
        }

        /// <summary>The lowest reputation of each band (for the bar in the interface).</summary>
        public static int Threshold(Standing standing)
        {
            switch (standing)
            {
                case Standing.Hated: return Min;
                case Standing.Disliked: return -49;
                case Standing.Neutral: return -14;
                case Standing.Friendly: return 20;
                case Standing.Honoured: return 50;
                default: return 80;
            }
        }

        /// <summary>What their shops ask, as a share of the price: dearer for an enemy, cheaper for a friend.</summary>
        public static float PriceFactor(int reputation)
        {
            switch (StandingOf(reputation))
            {
                case Standing.Hated: return 1.6f;
                case Standing.Disliked: return 1.25f;
                case Standing.Neutral: return 1f;
                case Standing.Friendly: return 0.92f;
                case Standing.Honoured: return 0.85f;
                default: return 0.75f;
            }
        }

        /// <summary>Whether they still trade with the hero.</summary>
        public static bool Trades(int reputation) => StandingOf(reputation) != Standing.Hated;

        /// <summary>A settlement's clan (an NPC's home): the clan of the settlement, or Gu Yue for the valley.</summary>
        public static string OfSettlement(string settlement)
        {
            var def = Regions.Settlement(settlement);

            return def != null ? def.Clan : "gu_yue";
        }
    }
}
