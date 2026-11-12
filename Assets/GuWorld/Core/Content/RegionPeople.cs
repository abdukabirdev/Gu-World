namespace GuWorld.Core
{
    /// <summary>
    /// The people of the wider mountain. Those who live in a settlement stand at one of its spots
    /// (Home and Role — the spot is found when the world is laid out); those at a place stand where
    /// they are put. Their days move them between spots (Day), and Teaches lists what they refine.
    /// </summary>
    public static class RegionPeople
    {
        static readonly (float, string)[] Shopkeeping = { (7, "shop"), (20, "house") };
        static readonly (float, string)[] Elders = { (8, "hall"), (12, "plaza"), (14, "hall"), (21, "house") };
        static readonly (float, string)[] Innkeeping = { (6, "inn") };
        static readonly (float, string)[] Fields = { (6, "plaza"), (9, "house"), (18, "plaza"), (21, "house") };

        public static readonly NpcDef[] All =
        {
            // ------------------------------------------------------------------ The Bai

            new NpcDef
            {
                Id = "bai_yun", Name = "Бай Юнь", Title = "Старейшина клана Бай", Look = "bai_elder", Home = "bai_village", Role = "hall", Clan = "bai", Dialogue = "bai_yun", Day = Elders,
                Barks = new[] { "Вода помнит, кто её пил.", "Гу Юэ всегда приходят с улыбкой. И уходят с нашим.", "Говори, раз пришёл." }
            },
            new NpcDef
            {
                Id = "bai_qing", Name = "Бай Цин", Title = "Лавочница", Look = "bai_villager", Home = "bai_village", Role = "shop", Clan = "bai", Service = "shop", Shop = "bai_shop", Day = Shopkeeping,
                Barks = new[] { "Пилюли, лёд, рис — всё по честной цене. Для своих.", "Лунный фонарик на поясе? Цена выше." }
            },
            new NpcDef
            {
                Id = "bai_lin", Name = "Бай Лин", Title = "Травница", Look = "herbalist", Home = "bai_village", Role = "herbalist", Clan = "bai", Dialogue = "bai_lin",
                Teaches = new[] { "bandage", "burn_salve", "big_heal_pill", "thrift_pill" }, Day = new[] { (6f, "plaza"), (8f, "herbalist"), (20f, "house") },
                Barks = new[] { "Ледяной лотос растёт только там, где брызги водопада замерзают.", "Не трогайте красные грибы руками." }
            },
            new NpcDef
            {
                Id = "bai_he", Name = "Бай Хэ", Title = "Смотритель питомника", Look = "keeper", Home = "bai_village", Role = "nursery", Clan = "bai", Service = "shop", Shop = "bai_gu", Dialogue = "bai_he",
                Barks = new[] { "Голодный Гу — мёртвый Гу. Кормите.", "Водяной щит пьёт росу. Не воду из реки — росу." }
            },
            new NpcDef
            {
                Id = "bai_shu", Name = "Бай Шу", Title = "Хозяин чайного дома", Look = "merchant", Home = "bai_village", Role = "inn", Clan = "bai", Service = "inn", Day = Innkeeping,
                Barks = new[] { "Облачный чай? Две чашки — и вы видите сквозь туман.", "Комната — пять камней. Сон бесплатно." }
            },
            new NpcDef
            {
                Id = "bai_gatekeeper", Name = "Бай Ту", Title = "Страж ворот", Look = "bai_guard", Home = "bai_village", Role = "gate", Clan = "bai",
                Barks = new[] { "Стой. Откуда?", "За воротами смотри под ноги — скалы скользкие.", "Ночью ворота не открываем. Никому." }
            },
            new NpcDef
            {
                Id = "bai_mei", Name = "Бай Мэй", Title = "Дочь рыбака", Look = "child", Home = "bai_village", Role = "plaza", Clan = "bai", Day = new[] { (7f, "plaza"), (19f, "house") },
                Barks = new[] { "Вы правда из Гу Юэ? А у вас все такие хмурые?", "Папа говорит, в омуте живёт змей." }
            },
            new NpcDef
            {
                Id = "bai_lao", Name = "Старик Лао", Title = "Рыбак", Look = "villager", Home = "bai_village", Role = "house", Clan = "bai",
                Barks = new[] { "Рыба ушла в глубину. Значит, будет холодная зима.", "Сорок лет ловлю в омуте. Змея не видел. Но слышал." }
            },
            new NpcDef
            {
                Id = "bai_scribe", Name = "Бай Вэнь", Title = "Писарь клана", Look = "scribe", Home = "bai_village", Role = "plaza", Clan = "bai", Service = "board",
                Barks = new[] { "Поручения клана Бай. Платим камнями и честью — честью чаще." }
            },

            // ------------------------------------------------------------------ The Xiong

            new NpcDef
            {
                Id = "xiong_jian", Name = "Сюн Цзянь", Title = "Глава клана Сюн", Look = "xiong_elder", Home = "xiong_village", Role = "hall", Clan = "xiong", Dialogue = "xiong_jian", Day = Elders,
                Barks = new[] { "Ха! Гу Юэ на нашей горе. Пришёл драться или пить?", "Сила без мёда — злость. Мёд без силы — сладость." }
            },
            new NpcDef
            {
                Id = "xiong_meng", Name = "Сюн Мэн", Title = "Кузнец", Look = "smith", Home = "xiong_village", Role = "smithy", Clan = "xiong", Service = "shop", Shop = "xiong_smith", Dialogue = "xiong_meng",
                Teaches = new[] { "iron_pill" }, Day = new[] { (6f, "smithy"), (21f, "inn") },
                Barks = new[] { "Руда! Мне нужна руда! Рудник занят крысами Чёрного Хвоста.", "Железная пилюля — та же поковка, только в брюхе." }
            },
            new NpcDef
            {
                Id = "xiong_bao", Name = "Толстый Бао", Title = "Трактирщик", Look = "cook", Home = "xiong_village", Role = "inn", Clan = "xiong", Service = "inn", Day = Innkeeping,
                Barks = new[] { "Медовуха, мясо, кровать. В таком порядке.", "Кто не пьёт у Бао, тот не Сюн." }
            },
            new NpcDef
            {
                Id = "xiong_hua", Name = "Сюн Хуа", Title = "Пасечница и смотрительница Гу", Look = "keeper", Home = "xiong_village", Role = "nursery", Clan = "xiong", Service = "shop", Shop = "xiong_gu", Dialogue = "xiong_hua",
                Teaches = new[] { "fury_pill", "regen_pill" },
                Barks = new[] { "Пчёлы злые этой весной. Что-то их беспокоит.", "Гу Медовой росы любит только наш мёд." }
            },
            new NpcDef
            {
                Id = "xiong_ling", Name = "Сюн Лин", Title = "Охотница", Look = "hunter", Home = "xiong_village", Role = "gate", Clan = "xiong", Dialogue = "xiong_ling", Day = new[] { (5f, "gate"), (20f, "inn") },
                Barks = new[] { "Медведи вышли из лога. Раньше такого не было.", "Хочешь научиться ходить по следу — молчи." }
            },
            new NpcDef
            {
                Id = "xiong_dan", Name = "Сюн Дань", Title = "Лавочник", Look = "merchant", Home = "xiong_village", Role = "shop", Clan = "xiong", Service = "shop", Shop = "xiong_shop", Day = Shopkeeping,
                Barks = new[] { "Мясо, мёд, пилюли. Сюн не жадничают — Сюн торгуются." }
            },
            new NpcDef
            {
                Id = "xiong_scribe", Name = "Сюн Шу", Title = "Писарь клана", Look = "scribe", Home = "xiong_village", Role = "plaza", Clan = "xiong", Service = "board",
                Barks = new[] { "Работа для крепких рук. Читать умеешь?" }
            },
            new NpcDef
            {
                Id = "xiong_xiao", Name = "Сюн Сяо", Title = "Мальчишка", Look = "child", Home = "xiong_village", Role = "plaza", Clan = "xiong", Day = new[] { (7f, "plaza"), (12f, "granary"), (15f, "plaza"), (19f, "house") },
                Barks = new[] { "Я тоже буду как Сюн Ба! Смотри — рррр!", "Ты дрался с Сюн Ли на турнире? Он потом неделю хромал!" }
            },

            // ------------------------------------------------------------------ The hamlets

            new NpcDef
            {
                Id = "willow_head", Name = "Староста Ли", Title = "Ивовый хутор", Look = "farmer", Home = "willow", Role = "plaza", Clan = "mortal", Day = Fields,
                Barks = new[] { "Мастер Гу! Простите, что без поклона, — спина.", "Волки режут овец. Кланы говорят: сами виноваты." }
            },
            new NpcDef
            {
                Id = "terraces_head", Name = "Староста Ван", Title = "Хутор Террасы", Look = "farmer", Home = "terraces", Role = "plaza", Clan = "mortal", Day = Fields,
                Barks = new[] { "Рис дань трём кланам. Четвёртому — Чёрному Хвосту — тоже, хоть его никто не звал.", "Террасы строил мой прадед. Камень к камню." }
            },
            new NpcDef
            {
                Id = "mill_head", Name = "Мельник Чжоу", Title = "Хутор у Мельницы", Look = "farmer", Home = "mill", Role = "granary", Clan = "mortal",
                Barks = new[] { "Без жернова мельница — сарай с колесом.", "Мука — это хлеб. Хлеб — это жизнь. Жернов — это всё." }
            },
            new NpcDef
            {
                Id = "pine_head", Name = "Лесоруб Ма", Title = "Сосновый хутор", Look = "farmer", Home = "pine", Role = "plaza", Clan = "mortal",
                Barks = new[] { "Мой брат ушёл к перевалу за кедром. Третий день нет.", "Сосна у перевала — лучшая на горе. И самая опасная." }
            },
            new NpcDef
            {
                Id = "honey_head", Name = "Пасечник Цао", Title = "Медовый хутор", Look = "farmer", Home = "honey", Role = "plaza", Clan = "mortal", Service = "shop", Shop = "hamlet",
                Barks = new[] { "Пчёлы с ума сошли — жалят всё, что движется.", "Мёд продам. Пчёл — нет, даже не просите." }
            },
            new NpcDef
            {
                Id = "stone_head", Name = "Каменотёс Ши", Title = "Каменный хутор", Look = "farmer", Home = "stone", Role = "plaza", Clan = "mortal",
                Barks = new[] { "По ночам у гробницы огни. Синие. Мои парни туда больше не ходят.", "Камень не врёт. Люди врут." }
            },
            new NpcDef
            {
                Id = "misty_head", Name = "Охотник У", Title = "Туманный хутор", Look = "hunter", Home = "misty", Role = "plaza", Clan = "mortal",
                Barks = new[] { "Орлы с Орлиных скал носят ягнят. Скоро начнут детей.", "В тумане не стреляй. Можешь попасть в соседа." }
            },
            new NpcDef
            {
                Id = "bamboo_head", Name = "Староста Тан", Title = "Бамбуковый хутор", Look = "farmer", Home = "bamboo", Role = "plaza", Clan = "mortal",
                Barks = new[] { "Змеи из болота ползут в огороды. Раньше их удерживало что-то большое.", "Бамбук растёт за ночь на ладонь. А змеи — быстрее." }
            },
            new NpcDef
            {
                Id = "ford_head", Name = "Перевозчик Хэ", Title = "Хутор у Брода", Look = "villager", Home = "ford", Role = "plaza", Clan = "mortal", Service = "shop", Shop = "hamlet",
                Barks = new[] { "Брод мелкий, но течение злое. Держитесь камней.", "На тракте опять бродячие мастера. Берут, что хотят." }
            },
            new NpcDef
            {
                Id = "willow_girl", Name = "Сяо Юй", Title = "Пастушка", Look = "child", Home = "willow", Role = "house", Clan = "mortal",
                Barks = new[] { "У меня было двенадцать овец. Теперь девять.", "Когда я вырасту, у меня тоже будет Гу!" }
            },

            // ------------------------------------------------------------------ The south road

            new NpcDef
            {
                Id = "trade_captain", Name = "Капитан Лу", Title = "Начальник заставы", Look = "guard", Home = "trade_post", Role = "plaza", Clan = "wanderers", Dialogue = "trade_captain",
                Barks = new[] { "Тракт держим втроём. Втроём, понимаешь?", "Чёрный Хвост сидит в старом руднике на западе. Все знают. Никто не идёт." }
            },
            new NpcDef
            {
                Id = "trade_merchant", Name = "Купец Цзинь", Title = "Торговец с юга", Look = "merchant", Home = "trade_post", Role = "shop", Clan = "wanderers", Service = "shop", Shop = "wander_shop",
                Barks = new[] { "С юга, из-за гор. Товар, какого у вас не видели.", "Серебро Чёрного Хвоста? Беру. Молча." }
            },
            new NpcDef
            {
                Id = "trade_innkeeper", Name = "Хозяйка Мэй", Title = "Постоялый двор", Look = "cook", Home = "trade_post", Role = "inn", Clan = "wanderers", Service = "inn", Day = Innkeeping,
                Barks = new[] { "Ночлег, похлёбка, сплетни. Сплетни бесплатно.", "Алхимик у костра опять что-то взорвал." }
            },
            new NpcDef
            {
                Id = "wandering_alchemist", Name = "Алхимик Фань", Title = "Странствующий алхимик", Look = "alchemist", Home = "trade_post", Role = "house", Clan = "wanderers", Dialogue = "alchemist",
                Teaches = new[] { "warming_pill", "great_essence_pill", "clarity_pill", "wind_pill", "shadow_pill", "focus_pill" },
                Barks = new[] { "Котёл, огонь и терпение. Терпения у меня нет — поэтому брови тоже.", "Хочешь варить пилюли? Начни с противоядия. Пригодится." }
            },
            new NpcDef
            {
                Id = "black_marketeer", Name = "Безухий", Title = "Скупщик", Look = "rogue", Home = "bandit_fort", Role = "house", Clan = "bandit", Service = "shop", Shop = "black_market",
                Barks = new[] { "Тихо. Ты меня не видел.", "Демонический Гу? Есть. Цена — не спрашивай." }
            },

            // ------------------------------------------------------------------ The places

            new NpcDef
            {
                Id = "old_witch", Name = "Бабушка Гуй", Title = "Ведунья Великого древа", Look = "witch", At = new MapPoint(-326, -1880), Facing = 200, Clan = "mortal", Dialogue = "old_witch",
                Barks = new[] { "Древо старше кланов. Оно умирает, внучек.", "Корни шепчут про пауков. Про пауков и семя." }
            },
            new NpcDef
            {
                Id = "tomb_scholar", Name = "Учёный Юань", Title = "Знаток древности", Look = "scribe", At = new MapPoint(2302, 896), Facing = 317, Clan = "wanderers", Dialogue = "tomb_scholar",
                Barks = new[] { "Письмена на стелах старше всех трёх кланов.", "Внутри — огни. Я туда не пойду. Вы — пойдёте?" }
            },
            new NpcDef
            {
                Id = "pass_monk", Name = "Монах Цзин", Title = "Отшельник перевала", Look = "monk", At = new MapPoint(-1136, 1772), Facing = 160, Clan = "wanderers", Dialogue = "pass_monk",
                Barks = new[] { "Перевал разделяет кланы. Вершина — объединяет.", "Я сижу здесь двадцать лет. Ветер меня знает." }
            },
            new NpcDef
            {
                Id = "mine_survivor", Name = "Рудокоп Ся", Title = "Беглец из рудника", Look = "miner", At = new MapPoint(-2066, 2178), Facing = 10, Clan = "mortal", Dialogue = "mine_survivor",
                Barks = new[] { "Они заставили нас копить камни. А потом из глубины пришла она.", "Тысяча ног. Тысяча. Я считал." }
            },
            new NpcDef
            {
                Id = "lian_prisoner", Name = "Сяо Лянь", Title = "Пленница", Look = "villager", Home = "bandit_fort", Role = "house", Clan = "mortal", GoneAfter = "lian_freed",
                Barks = new[] { "Тише! Они вернутся!", "Ключ от погреба у атамана. Всегда при нём." }
            },
        };
    }

    /// <summary>The shelves of every shop on the mountain.</summary>
    public static class Markets
    {
        public static readonly ShopEntry[] BaiShop =
        {
            new ShopEntry { Item = "heal_pill", Price = 13 },
            new ShopEntry { Item = "essence_pill", Price = 16 },
            new ShopEntry { Item = "warming_pill", Price = 18 },
            new ShopEntry { Item = "frost_lotus", Price = 18 },
            new ShopEntry { Item = "cloud_tea", Price = 22 },
            new ShopEntry { Item = "dew_gourd", Price = 4 },
            new ShopEntry { Item = "book_feeding", Price = 20 },
            new ShopEntry { Item = "scroll_river_serpent", Price = 120, Chapter = 3 },
            new ShopEntry { Item = "scroll_white_cloud", Price = 130, Chapter = 4 },
        };

        public static readonly ShopEntry[] BaiGu =
        {
            new ShopEntry { Gu = "water_shield", Price = 65 },
            new ShopEntry { Gu = "frost_breath", Price = 70 },
            new ShopEntry { Gu = "ice_spike", Price = 210, Chapter = 3 },
            new ShopEntry { Gu = "river_serpent", Price = 260, Chapter = 4 },
            new ShopEntry { Gu = "white_cloud", Price = 280, Chapter = 4 },
            new ShopEntry { Item = "scroll_north_wind_ice", Price = 1200, Needs = new[] { "rank>=4", "standing:bai=friendly" } },
            new ShopEntry { Item = "scroll_water_dragon", Price = 1200, Needs = new[] { "rank>=4", "standing:bai=friendly" } },
            new ShopEntry { Item = "scroll_heaven_ice", Price = 4000, Needs = new[] { "rank>=5", "standing:bai=honoured" } },
        };

        public static readonly ShopEntry[] XiongShop =
        {
            new ShopEntry { Item = "heal_pill", Price = 12 },
            new ShopEntry { Item = "jerky", Price = 4 },
            new ShopEntry { Item = "honey", Price = 7 },
            new ShopEntry { Item = "bear_garlic", Price = 5 },
            new ShopEntry { Item = "bandage", Price = 11 },
            new ShopEntry { Item = "fury_pill", Price = 42, Chapter = 3 },
            new ShopEntry { Item = "book_beasts", Price = 30 },
        };

        public static readonly ShopEntry[] XiongSmith =
        {
            new ShopEntry { Item = "iron_ore", Price = 12 },
            new ShopEntry { Item = "stone_dust", Price = 3 },
            new ShopEntry { Item = "ember_coal", Price = 6 },
            new ShopEntry { Item = "iron_pill", Price = 38, Chapter = 3 },
            new ShopEntry { Item = "jade_shard", Price = 30, Chapter = 2 },
            new ShopEntry { Item = "scroll_earth_spikes", Price = 110, Chapter = 3 },
            new ShopEntry { Item = "scroll_golden_bell", Price = 1300, Needs = new[] { "rank>=4", "standing:xiong=friendly" } },
        };

        public static readonly ShopEntry[] XiongGu =
        {
            new ShopEntry { Gu = "honey_dew", Price = 70 },
            new ShopEntry { Gu = "black_boar", Price = 140 },
            new ShopEntry { Gu = "bear_strength", Price = 200, Chapter = 3 },
            new ShopEntry { Gu = "bear_roar", Price = 240, Chapter = 4 },
            new ShopEntry { Item = "scroll_white_boar", Price = 150, Chapter = 4 },
            new ShopEntry { Item = "scroll_stone_giant", Price = 1250, Needs = new[] { "rank>=4", "standing:xiong=friendly" } },
            new ShopEntry { Item = "scroll_thunder_roar", Price = 1200, Needs = new[] { "rank>=4", "standing:xiong=friendly" } },
        };

        public static readonly ShopEntry[] Hamlet =
        {
            new ShopEntry { Item = "jerky", Price = 4 },
            new ShopEntry { Item = "honey", Price = 6 },
            new ShopEntry { Item = "dew_gourd", Price = 3 },
            new ShopEntry { Item = "bitter_root", Price = 5 },
            new ShopEntry { Item = "heal_pill", Price = 14 },
        };

        public static readonly ShopEntry[] Wanderer =
        {
            new ShopEntry { Item = "antidote", Price = 15 },
            new ShopEntry { Item = "burn_salve", Price = 13 },
            new ShopEntry { Item = "big_heal_pill", Price = 44, Chapter = 3 },
            new ShopEntry { Item = "great_essence_pill", Price = 48, Chapter = 3 },
            new ShopEntry { Item = "ember_coal", Price = 6 },
            new ShopEntry { Item = "echo_shell", Price = 10 },
            new ShopEntry { Item = "night_moth", Price = 8 },
            new ShopEntry { Item = "snake_gall", Price = 9 },
            new ShopEntry { Item = "book_alchemy", Price = 60 },
            new ShopEntry { Item = "book_paths", Price = 45 },
            new ShopEntry { Gu = "ember", Price = 70 },
            new ShopEntry { Gu = "echo", Price = 70 },
            new ShopEntry { Gu = "light_steps", Price = 60 },
            new ShopEntry { Gu = "deep_breath", Price = 130, Chapter = 2 },
            new ShopEntry { Gu = "night_eye", Price = 120, Chapter = 3 },
            new ShopEntry { Item = "scroll_thunder_wing", Price = 130, Chapter = 3 },
            new ShopEntry { Item = "scroll_flame_burst", Price = 90, Chapter = 3 },
            new ShopEntry { Item = "scroll_myriad_lightning", Price = 1250, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "scroll_ten_thousand_li", Price = 1150, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "scroll_spring_life", Price = 1200, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "scroll_radiant_lotus", Price = 1400, Needs = new[] { "rank>=4" } },
        };

        public static readonly ShopEntry[] BlackMarket =
        {
            new ShopEntry { Gu = "poison_needle", Price = 80 },
            new ShopEntry { Gu = "bone_needle", Price = 80 },
            new ShopEntry { Gu = "blood_skull", Price = 600, Chapter = 4 },
            new ShopEntry { Item = "shadow_pill", Price = 70 },
            new ShopEntry { Item = "scroll_blood_skull", Price = 260, Chapter = 4 },
            new ShopEntry { Item = "scroll_stealth_scales", Price = 160, Chapter = 3 },
            new ShopEntry { Item = "scroll_crimson_flame", Price = 1200, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "scroll_immortal_shadow", Price = 1400, Needs = new[] { "rank>=4", "standing:bandit=friendly" } },
            new ShopEntry { Item = "scroll_venom_dragon", Price = 1250, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "scroll_blood_lotus", Price = 1400, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "rock_2", Price = 80 },
            new ShopEntry { Item = "blood_crystal", Price = 95, Chapter = 3 },
            new ShopEntry { Item = "relic_r1", Price = 450, Chapter = 2, Needs = new[] { "rank<=1" } },
            new ShopEntry { Item = "scroll_pure_wine", Price = 1300, Needs = new[] { "rank>=4" } },
            new ShopEntry { Item = "relic_r2", Price = 1100, Chapter = 3, Needs = new[] { "rank==2" } },
        };

        // The town's market stalls (their traders, standing behind them, sell these).
        public static readonly ShopEntry[] StallFood =
        {
            new ShopEntry { Item = "jerky", Price = 4 }, new ShopEntry { Item = "honey", Price = 6 }, new ShopEntry { Item = "dew_gourd", Price = 3 },
            new ShopEntry { Item = "bitter_root", Price = 5 }, new ShopEntry { Item = "stolen_rice", Price = 5 },
        };

        public static readonly ShopEntry[] StallHerbs =
        {
            new ShopEntry { Item = "ginseng", Price = 14 }, new ShopEntry { Item = "spirit_moss", Price = 16 }, new ShopEntry { Item = "bitter_root", Price = 5 },
            new ShopEntry { Item = "cloud_tea", Price = 12 }, new ShopEntry { Item = "bear_garlic", Price = 5 },
        };

        public static readonly ShopEntry[] StallPills =
        {
            new ShopEntry { Item = "heal_pill", Price = 13 }, new ShopEntry { Item = "essence_pill", Price = 16 }, new ShopEntry { Item = "bandage", Price = 11 },
            new ShopEntry { Item = "antidote", Price = 15 }, new ShopEntry { Item = "big_heal_pill", Price = 46, Chapter = 3 },
        };

        public static readonly ShopEntry[] StallGuFood =
        {
            new ShopEntry { Item = "moon_orchid", Price = 9 }, new ShopEntry { Item = "ember_coal", Price = 6 }, new ShopEntry { Item = "night_moth", Price = 8 },
            new ShopEntry { Item = "spark_moss", Price = 9 }, new ShopEntry { Item = "stone_dust", Price = 3 }, new ShopEntry { Item = "frost_lotus", Price = 14, Chapter = 3 },
        };

        /// <summary>A shelf by its id ("" or "valley": Baoshan's).</summary>
        public static ShopEntry[] Get(string id)
        {
            switch (id)
            {
                case "bai_shop": return BaiShop;
                case "bai_gu": return BaiGu;
                case "xiong_shop": return XiongShop;
                case "xiong_smith": return XiongSmith;
                case "xiong_gu": return XiongGu;
                case "hamlet": return Hamlet;
                case "wander_shop": return Wanderer;
                case "black_market": return BlackMarket;
                case "stall_food": return StallFood;
                case "stall_herbs": return StallHerbs;
                case "stall_pills": return StallPills;
                case "stall_gu_food": return StallGuFood;
                case "gamble": return Shops.Gamble;
                default: return Shops.Shop;
            }
        }

        public static readonly string[] Ids = { "valley", "bai_shop", "bai_gu", "xiong_shop", "xiong_smith", "xiong_gu", "hamlet", "wander_shop", "black_market", "gamble", "stall_food", "stall_herbs", "stall_pills", "stall_gu_food" };
    }
}
