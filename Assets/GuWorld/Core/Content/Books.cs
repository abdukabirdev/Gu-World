using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>Every item: materials, pills, scrolls, quest things, rocks to gamble on.</summary>
    public static class ItemBook
    {
        public static readonly ItemDef[] All =
        {
            new ItemDef { Id = "fox_pelt", Name = "Лисья шкура", Seal = "皮", Kind = ItemKind.Material, Price = 6, Description = "Рыжая шкура горного лиса. Берут скорняки и мастера слияния." },
            new ItemDef { Id = "bamboo_shoot", Name = "Побег бамбука", Seal = "筍", Kind = ItemKind.Material, Price = 4, Description = "Молодой побег из западной рощи. Тётушка Хуа кладёт их в лапшу." },
            new ItemDef { Id = "wolf_fang", Name = "Волчий клык", Seal = "牙", Kind = ItemKind.Material, Price = 10, Description = "Клык серого волка. Твёрже железа, годится в слияние." },
            new ItemDef { Id = "wolf_pelt", Name = "Волчья шкура", Seal = "裘", Kind = ItemKind.Material, Price = 14, Description = "Густая серая шкура." },
            new ItemDef { Id = "storm_core", Name = "Грозовое ядро", Seal = "核", Kind = ItemKind.Material, Price = 40, Colour = 0xC0A8FF, Description = "Сгусток молнии из тела молниевого волка. Покалывает пальцы." },
            new ItemDef { Id = "moon_orchid", Name = "Лунная орхидея", Seal = "蘭", Kind = ItemKind.Material, Price = 8, Colour = 0xA0D2FF, Description = "Цветёт на опушке леса Цинмао и светится в сумерках. Корм Гу Лунного света." },
            new ItemDef { Id = "wine_jar", Name = "Кувшин вина", Seal = "酒", Kind = ItemKind.Material, Price = 20, Description = "Бамбуковое вино. Его любят и люди, и Винный червь." },
            new ItemDef { Id = "blood_crystal", Name = "Кристалл крови", Seal = "晶", Kind = ItemKind.Material, Price = 60, Colour = 0xF05A4A, Description = "Застывшая кровь, из которой вытянули жизнь. Тёплый на ощупь." },
            new ItemDef { Id = "king_core", Name = "Сердцевина короля зверей", Seal = "王", Kind = ItemKind.Material, Price = 200, Colour = 0xFFE08A, Description = "Ядро сотенного вожака. За такое слияние платят золотом." },
            new ItemDef { Id = "crown_core", Name = "Сердцевина Громовой Короны", Seal = "冠", Kind = ItemKind.Material, Price = 500, Colour = 0xD0B8FF, Description = "Ядро тысячного короля. В нём до сих пор гремит." },

            new ItemDef { Id = "heal_pill", Name = "Пилюля заживления", Seal = "丹", Kind = ItemKind.Consumable, Price = 12, Heal = 50, Colour = 0xF08A74, Description = "Возвращает 50 здоровья. Клавиша H." },
            new ItemDef { Id = "essence_pill", Name = "Пилюля эссенции", Seal = "元", Kind = ItemKind.Consumable, Price = 15, Essence = 0.6f, Colour = 0x7FE0C8, Description = "Наполняет апертуру на 60%." },

            new ItemDef { Id = "rock_1", Name = "Самородок с прожилкой", Seal = "礦", Kind = ItemKind.Rock, Tier = 1, Price = 25, Description = "Что внутри — не знает и продавец. Расколите в сумке." },
            new ItemDef { Id = "rock_2", Name = "Самородок с пятном", Seal = "礦", Kind = ItemKind.Rock, Tier = 2, Price = 90, Colour = 0x6FB8FF, Description = "Знатоки говорят: пятно — к Гу. Знатоки часто нищие." },
            new ItemDef { Id = "rock_3", Name = "Самородок с глазом", Seal = "礦", Kind = ItemKind.Rock, Tier = 3, Price = 300, Colour = 0xE8C98A, Description = "На сколе что-то шевелится. Или кажется." },

            new ItemDef { Id = "scroll_moonglow", Name = "Свиток: Гу Лунного сияния", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "moonglow", Price = 50, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_four_wines", Name = "Свиток: Червь Четырёх вин", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "four_wines", Price = 120, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_rock_skin", Name = "Свиток: Гу Каменной кожи", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "rock_skin", Price = 80, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_storm_step", Name = "Свиток: Гу Грозового шага", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "storm_step", Price = 90, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_white_jade", Name = "Свиток: Гу Белого нефрита", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "white_jade", Price = 90, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_golden_moon", Name = "Свиток: Гу Золотой луны", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "golden_moon", Price = 400, Description = "Рецепт слияния из хранилища клана." },
            new ItemDef { Id = "scroll_thunder_crown", Name = "Свиток: Гу Громовой короны", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "thunder_crown", Price = 400, Description = "Рецепт, выжженный молнией на волчьей шкуре." },
            new ItemDef { Id = "scroll_sky_canopy", Name = "Свиток: Гу Небесного покрова", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "sky_canopy", Price = 300, Description = "Рецепт слияния. Прочтите в сумке." },

            new ItemDef { Id = "scout_token", Name = "Жетон дозорного", Seal = "令", Kind = ItemKind.Quest, Description = "Нефритовый жетон Гу Юэ Тяня. Расколот надвое." },
            new ItemDef { Id = "hermit_will", Name = "Завещание Отшельника", Seal = "遺", Kind = ItemKind.Quest, Description = "«Тому, кого клан счёл слабым. Значит, ты ещё жив»." },
            new ItemDef { Id = "unsealed_letter", Name = "Письмо без печати", Seal = "信", Kind = ItemKind.Quest, Description = "«Сорок камней за луну, пока тракт закрыт. Учеников со слабой апертурой не трогать — они нужны»." },
            new ItemDef { Id = "blood_lure", Name = "Приманка пути крови", Seal = "餌", Kind = ItemKind.Quest, Description = "Мёртвый Гу в воске. На воске — лунная печать академии." },
        };

        static Dictionary<string, ItemDef> byId;

        public static ItemDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, ItemDef>();

                foreach (var item in All)
                {
                    byId[item.Id] = item;
                }
            }

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }
    }

    /// <summary>Every enemy and what it drops.</summary>
    public static class Bestiary
    {
        public static readonly EnemyDef[] All =
        {
            new EnemyDef { Id = "dummy", Name = "Манекен", Body = EnemyBody.Dummy, Mind = EnemyMind.Still, Health = 60, Damage = 0, Speed = 0, Insight = 2, Sight = 0, Poise = 999 },

            new EnemyDef { Id = "fox", Name = "Горный лис", Body = EnemyBody.Canid, Mind = EnemyMind.Skirmisher, Look = "fox", Scale = 1f, Level = 1, Health = 80, Damage = 7, Speed = 5.4f, AttackPeriod = 2.4f, Insight = 9, Loot = "fox", Poise = 8 },
            new EnemyDef { Id = "wolf", Name = "Серый волк", Body = EnemyBody.Canid, Mind = EnemyMind.PackHunter, Look = "grey", Scale = 1.35f, Level = 3, Health = 170, Damage = 11, Speed = 6f, AttackPeriod = 2.6f, Sight = 22, Insight = 16, Loot = "wolf", Poise = 14 },
            new EnemyDef
            {
                Id = "ragged_ear", Name = "Рваное Ухо", Title = "Вожак стаи", Body = EnemyBody.Canid, Mind = EnemyMind.Boss, Look = "grey", Scale = 1.8f, Level = 5, Health = 1300, Damage = 16, Speed = 6.4f,
                AttackRange = 2.6f, Sight = 30, Insight = 90, Boss = true, Loot = "ragged_ear", Poise = 40, Moves = new[] { BossMove.Lunge, BossMove.Lunge, BossMove.Howl, BossMove.Lunge }, Summons = "wolf"
            },
            new EnemyDef { Id = "storm_wolf", Name = "Молниевый волк", Body = EnemyBody.Canid, Mind = EnemyMind.Shooter, Look = "storm", Scale = 1.4f, Level = 6, Health = 330, Damage = 15, Speed = 6.2f, AttackRange = 14, AttackPeriod = 3f, Sight = 26, Insight = 26, Loot = "storm_wolf", Poise = 16, Colour = 0xC0A8FF },
            new EnemyDef
            {
                Id = "hundred_king", Name = "Сотенный вожак", Title = "Король сотни зверей", Body = EnemyBody.Canid, Mind = EnemyMind.Boss, Look = "grey", Scale = 2.4f, Level = 8, Health = 4000, Damage = 22, Speed = 6.6f,
                AttackRange = 3.4f, Sight = 40, Insight = 220, Boss = true, Loot = "hundred_king", Poise = 80, Moves = new[] { BossMove.Charge, BossMove.Lunge, BossMove.Howl, BossMove.Lunge, BossMove.Charge }, Summons = "wolf"
            },
            new EnemyDef
            {
                Id = "thunder_crown_wolf", Name = "Громовая Корона", Title = "Король тысячи зверей", Body = EnemyBody.Canid, Mind = EnemyMind.Boss, Look = "storm", Scale = 3.1f, Level = 10, Health = 7200, Damage = 28, Speed = 7f,
                AttackRange = 4.2f, Sight = 50, Insight = 520, Boss = true, Loot = "thunder_crown_wolf", Poise = 140, Colour = 0xD0B8FF,
                Moves = new[] { BossMove.Rings, BossMove.Lunge, BossMove.Leap, BossMove.Bolts, BossMove.Charge, BossMove.Howl }, Summons = "storm_wolf"
            },
            new EnemyDef { Id = "blood_wolf", Name = "Кровавый волк", Body = EnemyBody.Canid, Mind = EnemyMind.PackHunter, Look = "blood", Scale = 1.5f, Level = 11, Health = 620, Damage = 26, Speed = 6.8f, AttackPeriod = 2.2f, Sight = 26, Insight = 48, Loot = "blood_wolf", Poise = 20, Colour = 0xF05A4A },

            new EnemyDef { Id = "cave_gu", Name = "Дикий Гу", Body = EnemyBody.Bug, Mind = EnemyMind.Swarm, Look = "glow", Scale = 1f, Level = 4, Health = 90, Damage = 9, Speed = 5f, AttackPeriod = 2f, Sight = 16, Insight = 14, Loot = "cave_gu", Poise = 4, Colour = 0x7FE0C8 },
            new EnemyDef
            {
                Id = "stone_guard", Name = "Каменный страж", Title = "Страж наследия", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "stone", Scale = 1.35f, Level = 6, Health = 1900, Damage = 19, Speed = 3.6f,
                AttackRange = 2.6f, Sight = 30, Insight = 160, Boss = true, Loot = "stone_guard", Armour = 0.15f, Poise = 999, Moves = new[] { BossMove.Slam, BossMove.Lunge, BossMove.Rings, BossMove.Lunge }
            },

            new EnemyDef { Id = "bandit", Name = "Разбойник", Body = EnemyBody.Human, Mind = EnemyMind.Brawler, Look = "bandit", Level = 5, Health = 330, Damage = 14, Speed = 4.6f, AttackPeriod = 2.4f, Sight = 20, Insight = 24, Loot = "bandit", Poise = 18 },
            new EnemyDef { Id = "bandit_thrower", Name = "Разбойник-метатель", Body = EnemyBody.Human, Mind = EnemyMind.Shooter, Look = "bandit", Level = 5, Health = 250, Damage = 13, Speed = 4.4f, AttackRange = 15, AttackPeriod = 3.2f, Sight = 24, Insight = 26, Loot = "bandit", Poise = 12, Colour = 0x9FE8FF },
            new EnemyDef
            {
                Id = "crooked_fang", Name = "Кривой Клык", Title = "Атаман тракта", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "bandit", Scale = 1.15f, Level = 7, Health = 2800, Damage = 24, Speed = 4.8f,
                AttackRange = 2.4f, Sight = 30, Insight = 240, Boss = true, Loot = "crooked_fang", Poise = 60, Colour = 0xD0B080, Moves = new[] { BossMove.Lunge, BossMove.Slam, BossMove.Shield, BossMove.Bolts, BossMove.Lunge }
            },
            new EnemyDef
            {
                Id = "xiong_fighter", Name = "Сюн Ли", Title = "Боец клана Сюн", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "xiong", Scale = 1.1f, Level = 7, Health = 2400, Damage = 22, Speed = 5f,
                AttackRange = 2.4f, Sight = 40, Insight = 150, Boss = true, Loot = "duelist", Poise = 70, Colour = 0xE0A070, Moves = new[] { BossMove.Charge, BossMove.Lunge, BossMove.Slam, BossMove.Lunge }
            },
            new EnemyDef
            {
                Id = "bai_fighter", Name = "Бай Нин", Title = "Боец клана Бай", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "bai", Level = 7, Health = 2500, Damage = 20, Speed = 5.2f,
                AttackRange = 14, Sight = 40, Insight = 150, Boss = true, Loot = "duelist", Poise = 40, Colour = 0x9FE8FF, Moves = new[] { BossMove.Bolts, BossMove.Bolts, BossMove.Shield, BossMove.Rings }
            },

            new EnemyDef { Id = "blood_puppet", Name = "Кровавая марионетка", Body = EnemyBody.Human, Mind = EnemyMind.Brawler, Look = "blood", Level = 11, Health = 700, Damage = 24, Speed = 5.2f, AttackPeriod = 2f, Sight = 22, Insight = 52, Loot = "blood_puppet", Poise = 30, Colour = 0xF05A4A },
            new EnemyDef
            {
                Id = "elder", Name = "Гу Юэ Чанмин", Title = "Старейшина академии", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "elder", Scale = 1.08f, Level = 13, Health = 11500, Damage = 32, Speed = 5.4f,
                AttackRange = 16, Sight = 60, Insight = 1200, Boss = true, Loot = "elder", Poise = 160, Colour = 0xF05A4A,
                Moves = new[] { BossMove.Blades, BossMove.Blades, BossMove.Leap, BossMove.Summon, BossMove.BloodMoon, BossMove.Shield, BossMove.Rings }, Summons = "blood_puppet"
            },
        };

        public static readonly LootTable[] Tables =
        {
            new LootTable { Id = "fox", StonesMin = 1, StonesMax = 3, Items = new[] { new LootEntry { Item = "fox_pelt", Chance = 0.7f } }, GuChance = 0.03f, GuPool = new[] { "tailwind", "little_light" } },
            new LootTable { Id = "wolf", StonesMin = 2, StonesMax = 6, Items = new[] { new LootEntry { Item = "wolf_fang", Chance = 0.6f }, new LootEntry { Item = "wolf_pelt", Chance = 0.35f } }, GuChance = 0.06f, GuPool = new[] { "tailwind", "stone_beetle", "life_grass", "little_light", "jade_skin" } },
            new LootTable
            {
                Id = "ragged_ear", StonesMin = 30, StonesMax = 45, Items = new[] { new LootEntry { Item = "wolf_fang", Min = 3, Max = 5 }, new LootEntry { Item = "wolf_pelt", Min = 2, Max = 3 }, new LootEntry { Item = "heal_pill", Min = 2, Max = 2 } },
                GuChance = 1, GuPool = new[] { "tailwind", "stone_beetle" }, GuMinQuality = 1
            },
            new LootTable { Id = "storm_wolf", StonesMin = 5, StonesMax = 12, Items = new[] { new LootEntry { Item = "storm_core", Chance = 0.45f }, new LootEntry { Item = "wolf_fang", Chance = 0.5f } }, GuChance = 0.08f, GuPool = new[] { "spark", "tailwind", "storm_step" } },
            new LootTable
            {
                Id = "hundred_king", StonesMin = 90, StonesMax = 130, Items = new[] { new LootEntry { Item = "king_core" }, new LootEntry { Item = "wolf_pelt", Min = 4, Max = 6 }, new LootEntry { Item = "essence_pill", Min = 2, Max = 3 } },
                GuChance = 1, GuPool = new[] { "bear_strength", "whirl", "rock_skin" }, GuMinQuality = 1
            },
            new LootTable
            {
                Id = "thunder_crown_wolf", StonesMin = 220, StonesMax = 300, Items = new[] { new LootEntry { Item = "crown_core" }, new LootEntry { Item = "storm_core", Min = 5, Max = 7 }, new LootEntry { Item = "scroll_thunder_crown" } },
                GuChance = 1, GuPool = new[] { "storm_step", "spark" }, GuMinQuality = 2
            },
            new LootTable { Id = "blood_wolf", StonesMin = 10, StonesMax = 22, Items = new[] { new LootEntry { Item = "blood_crystal", Chance = 0.3f }, new LootEntry { Item = "wolf_fang", Chance = 0.5f } }, GuChance = 0.08f, GuPool = new[] { "ice_spike", "whirl", "bear_strength", "storm_step" } },
            new LootTable { Id = "cave_gu", StonesMin = 2, StonesMax = 7, GuChance = 0.14f, GuPool = new[] { "little_light", "stone_beetle", "spark", "life_grass", "jade_skin" } },
            new LootTable
            {
                Id = "stone_guard", StonesMin = 50, StonesMax = 70, Items = new[] { new LootEntry { Item = "heal_pill", Min = 2, Max = 3 }, new LootEntry { Item = "scroll_moonglow" } },
                GuChance = 1, GuPool = new[] { "stone_beetle", "jade_skin" }, GuMinQuality = 1
            },
            new LootTable { Id = "bandit", StonesMin = 6, StonesMax = 14, Items = new[] { new LootEntry { Item = "wine_jar", Chance = 0.3f }, new LootEntry { Item = "heal_pill", Chance = 0.2f } }, GuChance = 0.07f, GuPool = new[] { "jade_skin", "stone_beetle", "spark", "life_grass", "ice_spike" } },
            new LootTable
            {
                Id = "crooked_fang", StonesMin = 110, StonesMax = 150, Items = new[] { new LootEntry { Item = "unsealed_letter" }, new LootEntry { Item = "wine_jar", Min = 2, Max = 4 }, new LootEntry { Item = "scroll_rock_skin" } },
                GuChance = 1, GuPool = new[] { "rock_skin" }, GuMinQuality = 1
            },
            new LootTable { Id = "duelist", StonesMin = 40, StonesMax = 60, Items = new[] { new LootEntry { Item = "essence_pill", Min = 1, Max = 2 } } },
            new LootTable { Id = "blood_puppet", StonesMin = 12, StonesMax = 26, Items = new[] { new LootEntry { Item = "blood_crystal", Chance = 0.4f } }, GuChance = 0.08f, GuPool = new[] { "rock_skin", "ice_spike", "whirl", "white_jade" } },
            new LootTable { Id = "elder", StonesMin = 500, StonesMax = 500, Items = new[] { new LootEntry { Item = "blood_crystal", Min = 3, Max = 3 } } },

            // Chests.
            new LootTable { Id = "chest_grove", StonesMin = 12, StonesMax = 18, Items = new[] { new LootEntry { Item = "heal_pill", Min = 2, Max = 2 } }, GuChance = 1, GuPool = new[] { "tailwind" } },
            new LootTable { Id = "chest_forest", StonesMin = 25, StonesMax = 35, Items = new[] { new LootEntry { Item = "moon_orchid", Min = 2, Max = 3 }, new LootEntry { Item = "rock_1" } }, GuChance = 1, GuPool = new[] { "life_grass", "little_light" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_river", StonesMin = 30, StonesMax = 40, Items = new[] { new LootEntry { Item = "essence_pill", Min = 2, Max = 2 }, new LootEntry { Item = "wine_jar", Min = 1, Max = 2 } } },
            new LootTable { Id = "chest_hermit", StonesMin = 60, StonesMax = 80, Items = new[] { new LootEntry { Item = "wine_jar", Min = 2, Max = 2 }, new LootEntry { Item = "rock_2" } }, GuChance = 1, GuPool = new[] { "spark", "stone_beetle" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_camp", StonesMin = 70, StonesMax = 100, Items = new[] { new LootEntry { Item = "wine_jar", Min = 2, Max = 3 }, new LootEntry { Item = "scroll_storm_step" } }, GuChance = 1, GuPool = new[] { "ice_spike", "bear_strength" } },
            new LootTable { Id = "chest_ridge", StonesMin = 90, StonesMax = 120, Items = new[] { new LootEntry { Item = "storm_core", Min = 2, Max = 3 }, new LootEntry { Item = "rock_2" } }, GuChance = 1, GuPool = new[] { "whirl", "storm_step" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_blood", StonesMin = 150, StonesMax = 200, Items = new[] { new LootEntry { Item = "blood_crystal", Min = 2, Max = 3 }, new LootEntry { Item = "rock_3" } }, GuChance = 1, GuPool = new[] { "frost_demon" }, GuMinQuality = 1 },
        };

        static Dictionary<string, EnemyDef> byId;
        static Dictionary<string, LootTable> tables;

        public static EnemyDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, EnemyDef>();

                foreach (var enemy in All)
                {
                    byId[enemy.Id] = enemy;
                }
            }

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }

        public static LootTable Table(string id)
        {
            if (tables == null)
            {
                tables = new Dictionary<string, LootTable>();

                foreach (var table in Tables)
                {
                    tables[table.Id] = table;
                }
            }

            return id != null && tables.TryGetValue(id, out var found) ? found : null;
        }
    }

    /// <summary>What can be fused from what, and what the shops keep.</summary>
    public static class Recipes
    {
        public static readonly RecipeDef[] All =
        {
            new RecipeDef { Id = "moonglow", Result = "moonglow", Gu = new[] { "moonlight", "little_light" }, Items = new[] { ("moon_orchid", 3) }, Stones = 30 },
            new RecipeDef { Id = "four_wines", Result = "four_wines", Gu = new[] { "liquor_worm" }, Items = new[] { ("wine_jar", 4) }, Stones = 80 },
            new RecipeDef { Id = "rock_skin", Result = "rock_skin", Gu = new[] { "jade_skin", "stone_beetle" }, Items = new[] { ("wolf_fang", 4) }, Stones = 40 },
            new RecipeDef { Id = "storm_step", Result = "storm_step", Gu = new[] { "tailwind", "spark" }, Items = new[] { ("storm_core", 2) }, Stones = 60 },
            new RecipeDef { Id = "white_jade", Result = "white_jade", Gu = new[] { "jade_skin", "jade_skin" }, Items = new[] { ("fox_pelt", 6) }, Stones = 60 },
            new RecipeDef { Id = "golden_moon", Result = "golden_moon", Gu = new[] { "moonglow" }, Items = new[] { ("king_core", 1), ("storm_core", 5) }, Stones = 200 },
            new RecipeDef { Id = "thunder_crown", Result = "thunder_crown", Gu = new[] { "spark", "storm_step" }, Items = new[] { ("crown_core", 1) }, Stones = 250 },
            new RecipeDef { Id = "sky_canopy", Result = "sky_canopy", Gu = new[] { "rock_skin", "white_jade" }, Items = new[] { ("blood_crystal", 3) }, Stones = 250 },
        };

        public static RecipeDef Get(string id)
        {
            foreach (var recipe in All)
            {
                if (recipe.Id == id)
                {
                    return recipe;
                }
            }

            return null;
        }
    }

    public static class Shops
    {
        /// <summary>Baoshan's shop in the village.</summary>
        public static readonly ShopEntry[] Shop =
        {
            new ShopEntry { Item = "heal_pill", Price = 12 },
            new ShopEntry { Item = "essence_pill", Price = 15 },
            new ShopEntry { Item = "wine_jar", Price = 20 },
            new ShopEntry { Gu = "jade_skin", Price = 60 },
            new ShopEntry { Gu = "tailwind", Price = 60 },
            new ShopEntry { Gu = "life_grass", Price = 70 },
            new ShopEntry { Gu = "stone_beetle", Price = 70, Chapter = 2 },
            new ShopEntry { Gu = "spark", Price = 70, Chapter = 2 },
            new ShopEntry { Gu = "little_light", Price = 55, Chapter = 2 },
            new ShopEntry { Item = "scroll_moonglow", Price = 50, Chapter = 2 },
            new ShopEntry { Item = "scroll_rock_skin", Price = 80, Chapter = 3 },
            new ShopEntry { Item = "scroll_storm_step", Price = 90, Chapter = 3 },
            new ShopEntry { Item = "scroll_white_jade", Price = 90, Chapter = 3 },
            new ShopEntry { Item = "scroll_four_wines", Price = 120, Chapter = 3 },
            new ShopEntry { Gu = "whirl", Price = 220, Chapter = 3 },
            new ShopEntry { Gu = "ice_spike", Price = 220, Chapter = 4 },
            new ShopEntry { Item = "scroll_sky_canopy", Price = 300, Chapter = 5 },
        };

        /// <summary>The gambling tent's rocks.</summary>
        public static readonly ShopEntry[] Gamble =
        {
            new ShopEntry { Item = "rock_1", Price = 25 },
            new ShopEntry { Item = "rock_2", Price = 90, Chapter = 2 },
            new ShopEntry { Item = "rock_3", Price = 300, Chapter = 3 },
        };
    }
}
