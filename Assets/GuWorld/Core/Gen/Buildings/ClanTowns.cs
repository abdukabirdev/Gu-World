using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>
    /// A faction of a clan: a branch family (the Gu Yue's Chi and Mo), a department that grew into
    /// one (the medicine faction of the Yao), the clan head's own camp. It holds a district of the
    /// village: its branch manor, its members' houses, its kitchen, storehouse, training ground and
    /// fields, all in its colour (lacquer, banners, lanterns).
    /// </summary>
    public sealed class Faction
    {
        public string Id, Name, Clan;
        /// <summary>The lacquer of its halls and gates, its banners.</summary>
        public Tint Accent;
        /// <summary>The paper of its lanterns.</summary>
        public Tint Lantern;
        /// <summary>Its share of the factions' ring.</summary>
        public float Weight = 1;
        /// <summary>What its fields grow: "rice", "herbs" (the medicine faction's beds), "vegetables", "bees" (hives), "hides" (racks of pelts), "fish" (ponds).</summary>
        public string Fields = "vegetables";
        /// <summary>Its people, as the novel names them (for the codex and the people placed there).</summary>
        public string[] People = new string[0];
        /// <summary>Where it comes from: the novel, or made for the game.</summary>
        public string Note = "";

        public string Tag => Clan + "." + Id;
    }

    /// <summary>
    /// The tag a building carries for its colours: a clan ("gu_yue") or a clan and one of its
    /// factions ("gu_yue.chi"). Everything that colours a building reads it through here.
    /// </summary>
    public static class Factions
    {
        public static readonly Tint GuYueRed = new Tint(0.52f, 0.085f, 0.065f);
        public static readonly Tint XiongOchre = new Tint(0.46f, 0.27f, 0.11f);
        public static readonly Tint BaiWhite = new Tint(0.86f, 0.86f, 0.84f);
        public static readonly Tint RedPaper = new Tint(1f, 0.26f, 0.1f);

        /// <summary>The clan of a tag: "gu_yue.chi" → "gu_yue".</summary>
        public static string Clan(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return "";
            }

            var dot = tag.IndexOf('.');

            return dot < 0 ? tag : tag.Substring(0, dot);
        }

        /// <summary>The faction of a tag, or null.</summary>
        public static Faction Of(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tag.IndexOf('.') < 0)
            {
                return null;
            }

            return ByTag.TryGetValue(tag, out var faction) ? faction : null;
        }

        /// <summary>The lacquer for a tag: the faction's, else the clan's.</summary>
        public static Tint Accent(string tag)
        {
            var faction = Of(tag);

            if (faction != null)
            {
                return faction.Accent;
            }

            switch (Clan(tag))
            {
                case "gu_yue": return GuYueRed;
                case "xiong": return XiongOchre;
                case "bai": return BaiWhite;
                default: return new Tint(0.3f, 0.13f, 0.09f);
            }
        }

        /// <summary>The paper of the lanterns for a tag.</summary>
        public static Tint Lantern(string tag) => Of(tag)?.Lantern ?? RedPaper;

        static readonly Dictionary<string, Faction> ByTag = new Dictionary<string, Faction>();

        static Factions()
        {
            foreach (var town in ClanTown.All)
            {
                foreach (var faction in town.Factions)
                {
                    ByTag[faction.Tag] = faction;
                }

                foreach (var faction in town.Halls)
                {
                    ByTag[faction.Tag] = faction;
                }
            }
        }
    }

    /// <summary>One ring of a village, from the protected heart out: who lives there and on what lots.</summary>
    public sealed class Tier
    {
        public int Index;
        public string Name;
        /// <summary>Its outer edge: the share of the village's buildable land inside it, counted from the middle out.</summary>
        public float UpTo;
        public LotKind[] Lots;
        /// <summary>The lots on its main streets (shops), or null.</summary>
        public LotKind[] Main;
    }

    /// <summary>A building of a village that is placed by name, not on a lot: the clan's pavilion, a hall, a tavern.</summary>
    public sealed class Landmark
    {
        public string Style;
        /// <summary>Its role in the plan (the game finds its halls and inns by it).</summary>
        public string Role;
        public string Name;
        public float Width, Depth;
        public int Wealth = 7, Wear = 2;
        /// <summary>The tier it stands in (0 the heart).</summary>
        public int Tier;
        /// <summary>Whose colours: a faction of the clan or a hall ("dark"), or "" for the clan's.</summary>
        public string Faction = "";
        /// <summary>How many of it.</summary>
        public int Count = 1;
        /// <summary>Its things round it (thing kinds of ThingGen).</summary>
        public string[] Things = new string[0];
        /// <summary>Stands on a pad levelled for it (a great building) rather than down to the slope.</summary>
        public bool Pad = true;

        public Landmark(string style, string role, string name, float width, float depth, int tier, int wealth = 7)
        {
            Style = style;
            Role = role;
            Name = name;
            Width = width;
            Depth = depth;
            Tier = tier;
            Wealth = wealth;
        }
    }

    /// <summary>
    /// How a clan's village is laid out in rings by how protected they are (Docs/V2/PLAN-TOWNS.md):
    /// the heart with the clan's pavilion over its spring and its halls; the factions' districts; the
    /// Gu Masters' houses; the commoners; the poor at the edge; then the wall and the fields. Each
    /// clan its own: the Gu Yue's green bamboo on massive stakes, the Xiong's timber and hides, the
    /// Bai's white walls by the water.
    /// </summary>
    public sealed class ClanTown
    {
        public string Clan;
        /// <summary>The rings from the heart out (Tier 0 the heart).</summary>
        public Tier[] Tiers;
        public Faction[] Factions;
        /// <summary>The clan's departments with colours of their own (the Dark Hall's black): not districts.</summary>
        public Faction[] Halls = new Faction[0];
        /// <summary>The great building of the heart (it stands over the spring and the way down to it).</summary>
        public Landmark Main;
        /// <summary>The rest of the heart, then the buildings of the other tiers placed by name (taverns, inns).</summary>
        public Landmark[] Landmarks;
        /// <summary>What makes up a faction's district besides its houses: its manor and the rest.</summary>
        public Landmark[] District;
        /// <summary>The heart behind its own wall.</summary>
        public bool HeartWall = true;
        /// <summary>The rampart of the heart's wall.</summary>
        public string HeartRampart = "plaster";

        public Tier TierAt(float share)
        {
            foreach (var tier in Tiers)
            {
                if (share <= tier.UpTo)
                {
                    return tier;
                }
            }

            return Tiers[Tiers.Length - 1];
        }

        public static ClanTown For(string clan)
        {
            foreach (var town in All)
            {
                if (town.Clan == clan)
                {
                    return town;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ The Gu Yue

        /// <summary>
        /// The Gu Yue at the mountain's waist (ch. 1–4, 21, 33, 57, 90, 110, 149, 152–186): the clan's
        /// five-storey pavilion in the middle over the spring, the halls round it; the clan head's camp,
        /// the Chi and Mo branches, the medicine faction and the small Fang family; Gu Masters' two-storey
        /// bamboo houses; tall green bamboo houses on massive stakes, three taverns, an inn and a shabby
        /// lodging let by the day; four gates.
        /// </summary>
        public static readonly ClanTown GuYue = new ClanTown
        {
            Clan = "gu_yue",
            Factions = new[]
            {
                new Faction { Id = "head", Clan = "gu_yue", Name = "Фракция главы клана", Accent = new Tint(0.5f, 0.07f, 0.06f), Lantern = new Tint(1f, 0.3f, 0.12f), Weight = 1.3f, Fields = "rice",
                    People = new[] { "Гу Юэ Бо", "Гу Юэ Цин Шу", "Гу Юэ Фан Чжэн" }, Note = "новелла: глава клана растит своих людей (гл. 4, 33, 152)" },
                new Faction { Id = "chi", Clan = "gu_yue", Name = "Ветвь Чи", Accent = new Tint(0.8f, 0.2f, 0.06f), Lantern = new Tint(1f, 0.42f, 0.08f), Weight = 1.15f, Fields = "rice",
                    People = new[] { "Гу Юэ Чи Лянь", "Гу Юэ Чи Шань", "Гу Юэ Чи Чэн", "Гу Юэ Чи Чжун" }, Note = "новелла: усадьба ветви Чи (гл. 21, 29)" },
                new Faction { Id = "mo", Clan = "gu_yue", Name = "Ветвь Мо", Accent = new Tint(0.06f, 0.06f, 0.075f), Lantern = new Tint(0.95f, 0.9f, 0.78f), Weight = 1.15f, Fields = "vegetables",
                    People = new[] { "Гу Юэ Мо Чэнь", "Гу Юэ Мо Бэй", "Гу Юэ Мо Янь" }, Note = "новелла: усадьба ветви Мо, склад фракции Мо (гл. 21, 176)" },
                new Faction { Id = "yao", Clan = "gu_yue", Name = "Фракция медицины (Яо)", Accent = new Tint(0.12f, 0.36f, 0.24f), Lantern = new Tint(0.55f, 1f, 0.45f), Weight = 1f, Fields = "herbs",
                    People = new[] { "Гу Юэ Яо Цзи", "Гу Юэ Яо Лэ", "Гу Юэ Яо Хун", "Гу Юэ Ли Чэнь" }, Note = "новелла: фракция поддержки, самый доходный отдел (гл. 110, 152–178)" },
                new Faction { Id = "fang", Clan = "gu_yue", Name = "Семья Фан", Accent = new Tint(0.5f, 0.32f, 0.11f), Lantern = new Tint(1f, 0.75f, 0.3f), Weight = 0.8f, Fields = "vegetables",
                    People = new[] { "Гу Юэ Дун Ту", "тётя" }, Note = "новелла: не фракция, а семья родителей Фан Юаня; её дома, земли и слуг держат дядя Дун Ту и тётя (гл. 2, 9, 101); район — для игры" },
            },
            Halls = new[]
            {
                new Faction { Id = "dark", Clan = "gu_yue", Name = "Тёмный зал", Accent = new Tint(0.08f, 0.06f, 0.1f), Lantern = new Tint(0.6f, 0.55f, 1f), Note = "новелла: охраняет пещеру источника (гл. 156)" },
            },
            Tiers = new[]
            {
                new Tier { Index = 0, Name = "Сердце клана", UpTo = 0.1f,
                    Lots = new[] { new LotKind("courtyard", 3, 22, 25, 7, 9), new LotKind("townhouse", 2, 12, 12, 7, 9) } },
                new Tier { Index = 1, Name = "Районы фракций", UpTo = 0.36f,
                    Lots = new[] { new LotKind("courtyard", 4, 21, 24, 5, 8), new LotKind("townhouse", 3, 12, 12, 5, 8), new LotKind("bamboo", 3, 11, 10, 5, 7) } },
                new Tier { Index = 2, Name = "Дома Гу-мастеров", UpTo = 0.56f,
                    Lots = new[] { new LotKind("courtyard", 3, 18, 20, 3, 6), new LotKind("bamboo", 5, 11, 10, 4, 6), new LotKind("townhouse", 2, 11, 12, 4, 6) },
                    Main = new[] { new LotKind("shop", 4, 11, 11, 4, 7, true), new LotKind("market_row", 1, 26, 12, 4, 7, true), new LotKind("townhouse", 2, 12, 12, 5, 7) } },
                new Tier { Index = 3, Name = "Простые дома", UpTo = 0.78f,
                    Lots = new[] { new LotKind("bamboo", 7, 10, 9, 2, 4), new LotKind("house", 2, 11, 9, 2, 4), new LotKind("cottage", 1, 10, 10, 1, 3) },
                    Main = new[] { new LotKind("shop", 4, 11, 11, 3, 5, true), new LotKind("bamboo", 2, 10, 9, 3, 5) } },
                new Tier { Index = 4, Name = "Бедные окраины", UpTo = 1.01f,
                    Lots = new[] { new LotKind("bamboo", 5, 9, 8, 0, 2), new LotKind("hut", 3, 7.5f, 7, 0, 2), new LotKind("stilt", 2, 9, 9, 0, 2) } },
            },
            // The clan's pavilion: five storeys, sharp tilted roofs, a square before it (ch. 1, 4).
            Main = new Landmark("clan_pavilion", "hall", "Павильон клана Гу Юэ", 30, 26, 0, 9) { Things = new[] { "lantern_post", "banner_pole" } },
            Landmarks = new[]
            {
                new Landmark("hall", "hall", "Зал внутренних дел", 28, 18, 0, 8),
                new Landmark("hall", "hall", "Зал внешних дел", 24, 16, 0, 8),
                new Landmark("hall", "herbalist", "Зал медицины", 26, 17, 0, 8) { Faction = "yao", Things = new[] { "jars", "drying_rack" } },
                new Landmark("hall", "hall", "Тёмный зал", 22, 15, 0, 8) { Faction = "dark", Things = new[] { "weapon_rack", "banner_pole" } },
                new Landmark("academy", "academy", "Академия клана", 46, 52, 0, 9) { Pad = false },
                new Landmark("dorm", "house", "Общежитие академии", 54, 14, 0, 7),
                new Landmark("granary", "granary", "Хранилище клана", 20, 12, 0, 8) { Things = new[] { "barrels" } },
                new Landmark("nursery", "nursery", "Питомник Гу клана", 22, 12, 0, 8),
                new Landmark("temple", "hall", "Зал предков", 22, 18, 0, 9),
                // The town's buildings placed by name: the three taverns (rice, muddy and green bamboo wine, ch. 12), the inn (ch. 57), the market.
                new Landmark("tavern", "inn", "Таверна зелёного бамбукового вина", 16, 13, 2, 7) { Things = new[] { "jars", "table" }, Pad = false },
                new Landmark("tavern", "inn", "Таверна рисового вина", 14, 12, 3, 5) { Things = new[] { "jars", "barrels" }, Pad = false },
                new Landmark("tavern", "inn", "Таверна мутного вина", 13, 11, 3, 3) { Things = new[] { "jars", "table" }, Pad = false, Wear = 6 },
                new Landmark("inn", "inn", "Постоялый двор", 22, 16, 2, 7) { Things = new[] { "cart" } },
                new Landmark("market_row", "shop", "Торговый ряд", 40, 12, 2, 6) { Count = 3 },
                new Landmark("smithy", "smithy", "Кузня", 14, 11, 3, 4) { Count = 2, Things = new[] { "woodpile" }, Pad = false },
                new Landmark("granary", "granary", "Амбар", 16, 11, 4, 4) { Count = 3, Pad = false },
                // The shabby bamboo building let by the day, where Fang Yuan lived after leaving home (ch. 90).
                new Landmark("lodging", "inn", "Ветхий бамбуковый дом (посуточно)", 12, 10, 4, 1) { Pad = false, Wear = 8, Things = new[] { "washing" } },
            },
            // A faction's district: its branch manor (the Chi's, the Mo's), kitchen, storehouse, training ground.
            District = new[]
            {
                new Landmark("manor", "hall", "Усадьба", 40, 46, 1, 8) { Pad = false },
                new Landmark("kitchen", "inn", "Кухня", 16, 10, 1, 5) { Things = new[] { "jars", "woodpile" }, Pad = false },
                new Landmark("granary", "granary", "Склад", 16, 10, 1, 6) { Things = new[] { "barrels" }, Pad = false },
                new Landmark("training", "yard", "Тренировочная площадка", 24, 18, 1, 6),
            },
        };

        // ------------------------------------------------------------------ The Xiong

        /// <summary>
        /// The Xiong on the fore mountain (ch. 22, 65, 117, 123): a house of bears, arrogant, strong and
        /// robust; Bear Strength Gu that eats honey; bear gall wine. Their quarters are the game's: the
        /// head's line, the hunters, the beekeepers who brew.
        /// </summary>
        public static readonly ClanTown Xiong = new ClanTown
        {
            Clan = "xiong",
            HeartRampart = "palisade",
            Factions = new[]
            {
                new Faction { Id = "head", Clan = "xiong", Name = "Линия главы клана Сюн", Accent = new Tint(0.42f, 0.2f, 0.08f), Lantern = new Tint(1f, 0.45f, 0.15f), Weight = 1.2f, Fields = "vegetables",
                    Note = "для игры: в новелле глава клана Сюн без имени" },
                new Faction { Id = "hunters", Clan = "xiong", Name = "Охотники (род Сюн Ли)", Accent = new Tint(0.3f, 0.1f, 0.05f), Lantern = new Tint(1f, 0.3f, 0.1f), Weight = 1f, Fields = "hides",
                    People = new[] { "Сюн Ли", "Сюн Линь", "Сюн Цзян", "Сюн Цзяо Мань" }, Note = "новелла: отряд Сюн Ли (гл. 117, 123); район — для игры" },
                new Faction { Id = "honey", Clan = "xiong", Name = "Медовары", Accent = new Tint(0.72f, 0.46f, 0.1f), Lantern = new Tint(1f, 0.7f, 0.2f), Weight = 0.9f, Fields = "bees",
                    Note = "новелла: Гу Медвежьей силы ест мёд, вино из медвежьей желчи (гл. 22, 65); район — для игры" },
            },
            Tiers = new[]
            {
                new Tier { Index = 0, Name = "Сердце клана", UpTo = 0.12f, Lots = new[] { new LotKind("courtyard", 2, 18, 20, 6, 8), new LotKind("cottage", 1, 11, 10, 5, 7) } },
                new Tier { Index = 1, Name = "Дворы родов", UpTo = 0.42f, Lots = new[] { new LotKind("courtyard", 3, 18, 20, 4, 7), new LotKind("cottage", 3, 11, 10, 3, 6), new LotKind("house", 1, 12, 10, 3, 6) } },
                new Tier { Index = 2, Name = "Дома Гу-мастеров", UpTo = 0.62f, Lots = new[] { new LotKind("cottage", 3, 11, 10, 3, 5), new LotKind("house", 2, 12, 10, 3, 5), new LotKind("courtyard", 1, 17, 19, 3, 5) },
                    Main = new[] { new LotKind("shop", 2, 11, 11, 2, 5, true), new LotKind("cottage", 2, 11, 10, 2, 5) } },
                new Tier { Index = 3, Name = "Простые дома", UpTo = 0.82f, Lots = new[] { new LotKind("cottage", 3, 10, 10, 1, 3), new LotKind("hut", 2, 8, 7.5f, 1, 3) } },
                new Tier { Index = 4, Name = "Бедные окраины", UpTo = 1.01f, Lots = new[] { new LotKind("hut", 3, 8, 7.5f, 0, 2), new LotKind("stilt", 1, 9, 9, 0, 2) } },
            },
            Main = new Landmark("great_hall", "hall", "Медвежий зал клана Сюн", 30, 22, 0, 8) { Things = new[] { "hide_rack", "banner_pole" } },
            Landmarks = new[]
            {
                new Landmark("academy", "academy", "Двор учеников", 38, 42, 0, 6),
                new Landmark("hall", "hall", "Зал старейшин", 20, 14, 0, 7),
                new Landmark("tavern", "inn", "Винокурня медвежьего вина", 15, 12, 2, 5) { Things = new[] { "barrels", "jars" }, Pad = false },
                new Landmark("smithy", "smithy", "Кузня", 13, 10, 3, 3) { Things = new[] { "woodpile" }, Pad = false },
                new Landmark("lodging", "inn", "Ночлежка", 11, 9, 4, 1) { Pad = false, Wear = 7 },
            },
            District = new[]
            {
                new Landmark("manor", "hall", "Двор рода", 30, 34, 1, 7) { Pad = false },
                new Landmark("training", "yard", "Борцовский круг", 18, 18, 1, 5),
            },
        };

        // ------------------------------------------------------------------ The Bai

        /// <summary>
        /// The Bai by the deep pool under the waterfall behind the mountain (ch. 14, 117, 123): pale
        /// skin and a yin aura from living by the falls, Stream Gu, white grain wine; a rising clan.
        /// Their quarters are the game's: the head's line, the rising line of Bai Ning Bing, the river folk.
        /// </summary>
        public static readonly ClanTown Bai = new ClanTown
        {
            Clan = "bai",
            Factions = new[]
            {
                new Faction { Id = "head", Clan = "bai", Name = "Линия главы клана Бай", Accent = new Tint(0.82f, 0.82f, 0.8f), Lantern = new Tint(1f, 0.9f, 0.75f), Weight = 1.2f, Fields = "rice",
                    Note = "для игры: в новелле глава клана Бай без имени" },
                new Faction { Id = "ning", Clan = "bai", Name = "Восходящий род Бай Нин Бина", Accent = new Tint(0.42f, 0.58f, 0.78f), Lantern = new Tint(0.7f, 0.85f, 1f), Weight = 1f, Fields = "rice",
                    People = new[] { "Бай Нин Бин" }, Note = "новелла: Бай Нин Бин — гений клана (гл. 1, 117); род — для игры" },
                new Faction { Id = "river", Clan = "bai", Name = "Речной род", Accent = new Tint(0.2f, 0.45f, 0.5f), Lantern = new Tint(0.6f, 1f, 0.95f), Weight = 0.9f, Fields = "fish",
                    Note = "новелла: Гу Потока, вино «белое зерно» (гл. 117, 123); род — для игры" },
            },
            Tiers = new[]
            {
                new Tier { Index = 0, Name = "Сердце клана", UpTo = 0.12f, Lots = new[] { new LotKind("courtyard", 3, 19, 22, 6, 8), new LotKind("townhouse", 1, 11, 12, 6, 8) } },
                new Tier { Index = 1, Name = "Дворы родов", UpTo = 0.42f, Lots = new[] { new LotKind("courtyard", 4, 19, 22, 4, 7), new LotKind("townhouse", 2, 11, 12, 4, 7) } },
                new Tier { Index = 2, Name = "Дома Гу-мастеров", UpTo = 0.62f, Lots = new[] { new LotKind("courtyard", 2, 17, 19, 3, 5), new LotKind("house", 3, 11, 10, 3, 5) },
                    Main = new[] { new LotKind("shop", 3, 11, 11, 4, 6, true), new LotKind("townhouse", 1, 12, 12, 4, 6) } },
                new Tier { Index = 3, Name = "Простые дома", UpTo = 0.82f, Lots = new[] { new LotKind("house", 3, 11, 10, 2, 4), new LotKind("cottage", 1, 10, 10, 1, 3) } },
                new Tier { Index = 4, Name = "Бедные окраины", UpTo = 1.01f, Lots = new[] { new LotKind("cottage", 2, 10, 10, 0, 2), new LotKind("stilt", 2, 9, 9, 0, 2) } },
            },
            Main = new Landmark("great_hall", "hall", "Белый зал клана Бай", 28, 21, 0, 8) { Things = new[] { "lantern_post", "banner_pole" } },
            Landmarks = new[]
            {
                new Landmark("academy", "academy", "Двор учеников", 38, 42, 0, 7),
                new Landmark("hall", "hall", "Зал старейшин", 20, 14, 0, 7),
                new Landmark("tavern", "inn", "Винокурня «Белое зерно»", 15, 12, 2, 6) { Things = new[] { "jars", "barrels" }, Pad = false },
                new Landmark("lodging", "inn", "Постоялый дом", 12, 10, 4, 2) { Pad = false, Wear = 6 },
            },
            District = new[]
            {
                new Landmark("manor", "hall", "Двор рода", 30, 34, 1, 7) { Pad = false },
                new Landmark("granary", "granary", "Склад", 14, 10, 1, 5) { Pad = false },
            },
        };

        public static readonly ClanTown[] All = { GuYue, Xiong, Bai };
    }
}
