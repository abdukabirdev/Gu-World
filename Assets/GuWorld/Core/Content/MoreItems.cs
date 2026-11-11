namespace GuWorld.Core
{
    /// <summary>
    /// The things of the wider mountain: what Gu eat, the herbs pills are refined from, the beasts'
    /// parts, the pills themselves, books for the codex, keys, scrolls of the new fusions and the
    /// regions' quest things.
    /// </summary>
    public static class MoreItems
    {
        public static readonly ItemDef[] All =
        {
            // ------------------------------------------------------------------ Gu food

            new ItemDef { Id = "stone_dust", Name = "Каменная крошка", Seal = "砂", Kind = ItemKind.Food, Price = 3, Colour = 0xD0B080, Description = "Толчёный камень с жилками эссенции. Корм Гу пути Земли." },
            new ItemDef { Id = "dew_gourd", Name = "Тыква росы", Seal = "露", Kind = ItemKind.Food, Price = 4, Colour = 0xB0F0E0, Description = "Утренняя роса, собранная в сушёную тыкву. Её пьют Гу ветра, дерева и воды." },
            new ItemDef { Id = "spark_moss", Name = "Грозовой мох", Seal = "苔", Kind = ItemKind.Food, Price = 6, Colour = 0xC0A8FF, Description = "Растёт там, куда била молния. Потрескивает в пальцах. Корм Гу пути Молнии." },
            new ItemDef { Id = "jerky", Name = "Вяленое мясо", Seal = "肉", Kind = ItemKind.Food, Price = 4, Colour = 0xC08060, Description = "Полоски кабанятины в соли. Гу силы, крови и кости едят только мясо." },
            new ItemDef { Id = "honey", Name = "Горный мёд", Seal = "蜜", Kind = ItemKind.Food, Price = 7, Colour = 0xFFD070, Heal = 15, Description = "Тёмный мёд пасек Сюн. Им кормят Гу, а люди — себя: +15 здоровья." },
            new ItemDef { Id = "ember_coal", Name = "Тлеющий уголь", Seal = "炭", Kind = ItemKind.Food, Price = 5, Colour = 0xFF9050, Description = "Уголь из горна, что не гаснет неделю. Корм Гу пути Огня." },
            new ItemDef { Id = "snake_gall", Name = "Змеиная желчь", Seal = "胆", Kind = ItemKind.Food, Price = 8, Colour = 0x9FE070, Description = "Горькая и ядовитая. Гу пути Яда без неё слабеют." },
            new ItemDef { Id = "echo_shell", Name = "Поющая раковина", Seal = "螺", Kind = ItemKind.Food, Price = 9, Colour = 0xE0D0FF, Description = "Речная раковина, в которой шумит эхо. Гу Звука пьют этот шум." },
            new ItemDef { Id = "night_moth", Name = "Ночной мотылёк", Seal = "蛾", Kind = ItemKind.Food, Price = 7, Colour = 0x8A7AC0, Description = "Ловят ночью у фонарей. Корм Гу пути Тьмы." },

            // ------------------------------------------------------------------ Herbs

            new ItemDef { Id = "ginseng", Name = "Горный женьшень", Seal = "参", Kind = ItemKind.Herb, Price = 14, Colour = 0xE8C98A, Description = "Корень, похожий на человечка. Основа пилюль заживления." },
            new ItemDef { Id = "frost_lotus", Name = "Ледяной лотос", Seal = "莲", Kind = ItemKind.Herb, Price = 16, Colour = 0xBFF4FF, Description = "Цветёт у омута Бай, где брызги водопада замерзают. Корм Гу пути Льда и основа согревающих пилюль." },
            new ItemDef { Id = "red_cap", Name = "Красный гриб", Seal = "菌", Kind = ItemKind.Herb, Price = 6, Colour = 0xF06050, Description = "Ядовит. Из яда делают противоядие — так говорят алхимики." },
            new ItemDef { Id = "spirit_moss", Name = "Духовный мох", Seal = "灵", Kind = ItemKind.Herb, Price = 18, Colour = 0x9FEFC4, Description = "Светится в пещерах. Проясняет ум: из него — пилюли ясности." },
            new ItemDef { Id = "bitter_root", Name = "Горький корень", Seal = "根", Kind = ItemKind.Herb, Price = 5, Colour = 0xA08060, Description = "Растёт везде, нужен всем пилюлям — связывает остальное." },
            new ItemDef { Id = "sun_bloom", Name = "Солнечник", Seal = "阳", Kind = ItemKind.Herb, Price = 10, Colour = 0xFFE070, Description = "Жёлтый цветок горных лугов. Согревает и прогоняет холод." },
            new ItemDef { Id = "marsh_reed", Name = "Болотный тростник", Seal = "苇", Kind = ItemKind.Herb, Price = 4, Colour = 0x8FD060, Description = "Пористый стебель из Змеиного болота. Впитывает яд." },
            new ItemDef { Id = "bear_garlic", Name = "Черемша", Seal = "蒜", Kind = ItemKind.Herb, Price = 5, Colour = 0xB0E090, Description = "Медвежий лук. Сюн едят её весной и говорят, что от неё растёт сила." },
            new ItemDef { Id = "cloud_tea", Name = "Облачный чай", Seal = "茶", Kind = ItemKind.Herb, Price = 20, Colour = 0xEAF6FF, Description = "Чайный куст, что растёт только в тумане за хребтом. Бай пьют его на советах." },
            new ItemDef { Id = "blood_vine", Name = "Кровяная лоза", Seal = "藤", Kind = ItemKind.Herb, Price = 30, Colour = 0xD04A3A, Description = "Красная лоза у Великого древа. Сок останавливает кровь — или зовёт её." },

            // ------------------------------------------------------------------ Beasts' parts and ores

            new ItemDef { Id = "bear_claw", Name = "Медвежий коготь", Seal = "爪", Kind = ItemKind.Material, Price = 18, Description = "Коготь бурого медведя длиной с палец." },
            new ItemDef { Id = "bear_gall", Name = "Медвежья желчь", Seal = "胆", Kind = ItemKind.Material, Price = 30, Colour = 0xD0C060, Description = "Дорого стоит у алхимиков: из неё — пилюли ярости." },
            new ItemDef { Id = "bear_king_heart", Name = "Сердце Медвежьего короля", Seal = "心", Kind = ItemKind.Material, Price = 400, Colour = 0xE0A070, Description = "Бьётся до сих пор. Для слияния Гу Великого медведя." },
            new ItemDef { Id = "snake_skin", Name = "Змеиная кожа", Seal = "鳞", Kind = ItemKind.Material, Price = 12, Colour = 0x9FC070, Description = "Сброшенная чешуя болотной змеи. Скользкая и прочная." },
            new ItemDef { Id = "snake_fang", Name = "Змеиный зуб", Seal = "牙", Kind = ItemKind.Material, Price = 14, Colour = 0xE8E8C0, Description = "Полый, с каплей яда внутри." },
            new ItemDef { Id = "python_core", Name = "Ядро Болотного удава", Seal = "核", Kind = ItemKind.Material, Price = 380, Colour = 0x8FD060, Description = "Зелёный шар размером с кулак. В нём яд целого болота." },
            new ItemDef { Id = "spider_silk", Name = "Паучий шёлк", Seal = "丝", Kind = ItemKind.Material, Price = 16, Colour = 0xF0F0F0, Description = "Крепче верёвки, тоньше волоса." },
            new ItemDef { Id = "spider_eye", Name = "Паучий глаз", Seal = "目", Kind = ItemKind.Material, Price = 20, Colour = 0x60E0A0, Description = "Смотрит на вас даже из сумки." },
            new ItemDef { Id = "spider_queen_eye", Name = "Глаз Паучьей матери", Seal = "瞳", Kind = ItemKind.Material, Price = 420, Colour = 0x60E0A0, Description = "Восемь зрачков в одном. Видит звук." },
            new ItemDef { Id = "bat_wing", Name = "Крыло нетопыря", Seal = "翅", Kind = ItemKind.Material, Price = 9, Colour = 0x806878, Description = "Тонкая кожа на костях. Беззвучное." },
            new ItemDef { Id = "centipede_shell", Name = "Панцирь сколопендры", Seal = "甲", Kind = ItemKind.Material, Price = 22, Colour = 0xC07040, Description = "Пластина с ядовитыми шипами по краю." },
            new ItemDef { Id = "eagle_feather", Name = "Орлиное перо", Seal = "羽", Kind = ItemKind.Material, Price = 15, Colour = 0xE0D0B0, Description = "Маховое перо горного орла. Ветер его любит." },
            new ItemDef { Id = "bone_shard", Name = "Осколок кости", Seal = "骨", Kind = ItemKind.Material, Price = 8, Colour = 0xF0E8D8, Description = "Из старых могил и пещер. Гу Кости растут на таких." },
            new ItemDef { Id = "jade_shard", Name = "Осколок нефрита", Seal = "玉", Kind = ItemKind.Material, Price = 25, Colour = 0x8FE0B0, Description = "Зелёный камень из рудника. Мастера слияния ценят его." },
            new ItemDef { Id = "iron_ore", Name = "Железная руда", Seal = "铁", Kind = ItemKind.Material, Price = 10, Colour = 0x9A9AA0, Description = "Тяжёлая, рыжая от ржавчины." },
            new ItemDef { Id = "ancient_coin", Name = "Древняя монета", Seal = "币", Kind = ItemKind.Material, Price = 60, Colour = 0xE8C98A, Description = "Монета царства, которого нет уже тысячу лет." },
            new ItemDef { Id = "ancient_relic", Name = "Реликвия Древнего мастера", Seal = "宝", Kind = ItemKind.Material, Price = 600, Colour = 0xC0A8FF, Description = "Кость пальца в нефритовом футляре. Гудит, когда рядом Гу." },
            new ItemDef { Id = "toad_venom", Name = "Жабий яд", Seal = "蟾", Kind = ItemKind.Material, Price = 11, Colour = 0xA0C060, Description = "Белая пена с кожи болотной жабы." },
            new ItemDef { Id = "silver_ingot", Name = "Серебряный слиток", Seal = "银", Kind = ItemKind.Material, Price = 80, Colour = 0xE0E8F0, Description = "Слиток с клеймом Чёрного Хвоста. Его не берут в лавках кланов, но берут на тракте." },

            // ------------------------------------------------------------------ Pills

            new ItemDef { Id = "big_heal_pill", Name = "Большая пилюля заживления", Seal = "丹", Kind = ItemKind.Consumable, Price = 40, Heal = 140, Colour = 0xF06A54, Description = "Возвращает 140 здоровья." },
            new ItemDef { Id = "great_essence_pill", Name = "Пилюля полной апертуры", Seal = "满", Kind = ItemKind.Consumable, Price = 45, Essence = 1f, Colour = 0x5FD0B8, Description = "Наполняет апертуру до краёв." },
            new ItemDef { Id = "antidote", Name = "Противоядие", Seal = "解", Kind = ItemKind.Consumable, Price = 14, Cures = StatusKind.Poison, Heal = 10, Colour = 0x9FE070, Description = "Снимает яд и немного лечит." },
            new ItemDef { Id = "burn_salve", Name = "Мазь от ожогов", Seal = "膏", Kind = ItemKind.Consumable, Price = 12, Cures = StatusKind.Burn, Heal = 15, Colour = 0xFFB080, Description = "Гасит огонь на коже." },
            new ItemDef { Id = "bandage", Name = "Повязка с лозой", Seal = "带", Kind = ItemKind.Consumable, Price = 10, Cures = StatusKind.Bleed, Heal = 20, Colour = 0xE8D8C8, Description = "Останавливает кровь." },
            new ItemDef { Id = "warming_pill", Name = "Согревающая пилюля", Seal = "暖", Kind = ItemKind.Consumable, Price = 16, Cures = StatusKind.Chill, Colour = 0xFFE070, Description = "Прогоняет холод и иней." },
            new ItemDef { Id = "clarity_pill", Name = "Пилюля ясности", Seal = "明", Kind = ItemKind.Consumable, Price = 90, Insight = 60, Colour = 0xA0F0E0, Description = "Ум чист, как вода в омуте: +60 прозрения." },
            new ItemDef { Id = "iron_pill", Name = "Железная пилюля", Seal = "铁", Kind = ItemKind.Consumable, Price = 35, Buff = BuffKind.Shield, BoostValue = 0.2f, BoostSeconds = 60, Colour = 0xA0A8B0, Description = "Минуту кожа как железо: урон на пятую часть меньше." },
            new ItemDef { Id = "fury_pill", Name = "Пилюля ярости", Seal = "怒", Kind = ItemKind.Consumable, Price = 40, Buff = BuffKind.MeleeUp, BoostValue = 0.4f, BoostSeconds = 45, Colour = 0xF08060, Description = "Сорок пять секунд ладонь бьёт сильнее." },
            new ItemDef { Id = "wind_pill", Name = "Пилюля ветра", Seal = "疾", Kind = ItemKind.Consumable, Price = 30, Buff = BuffKind.Haste, BoostValue = 0.25f, BoostSeconds = 60, Colour = 0xCFF5E8, Description = "Минуту ноги несут быстрее." },
            new ItemDef { Id = "shadow_pill", Name = "Пилюля тени", Seal = "影", Kind = ItemKind.Consumable, Price = 60, Buff = BuffKind.Stealth, BoostValue = 1.5f, BoostSeconds = 20, Colour = 0x8A7AC0, Description = "Двадцать секунд вас не видят." },
            new ItemDef { Id = "regen_pill", Name = "Пилюля жизни", Seal = "生", Kind = ItemKind.Consumable, Price = 35, Buff = BuffKind.Regen, BoostValue = 3, BoostSeconds = 40, Colour = 0x9FEFC4, Description = "Сорок секунд раны затягиваются." },
            new ItemDef { Id = "focus_pill", Name = "Пилюля сосредоточения", Seal = "专", Kind = ItemKind.Consumable, Price = 55, BoostStat = StatKind.Power, BoostValue = 0.2f, BoostSeconds = 120, Colour = 0xC8E4FF, Description = "Две минуты Гу бьют на пятую часть сильнее." },
            new ItemDef { Id = "thrift_pill", Name = "Пилюля бережливости", Seal = "俭", Kind = ItemKind.Consumable, Price = 50, BoostStat = StatKind.Cost, BoostValue = -0.2f, BoostSeconds = 120, Colour = 0x7FE0C8, Description = "Две минуты Гу берут на пятую часть меньше эссенции." },

            // ------------------------------------------------------------------ Books

            new ItemDef { Id = "book_apertures", Name = "«О пяти апертурах»", Seal = "书", Kind = ItemKind.Book, Price = 30, Lore = "lore_apertures", Description = "Учебник академии Гу Юэ, переписанный небрежной рукой." },
            new ItemDef { Id = "book_feeding", Name = "«Как кормить Гу»", Seal = "书", Kind = ItemKind.Book, Price = 20, Lore = "lore_feeding", Description = "Тонкая тетрадь смотрителя питомника Бай." },
            new ItemDef { Id = "book_alchemy", Name = "«Котёл и огонь»", Seal = "书", Kind = ItemKind.Book, Price = 60, Lore = "lore_alchemy", Description = "Записки странствующего алхимика. Половина страниц прожжена." },
            new ItemDef { Id = "book_three_clans", Name = "«Хроника трёх кланов»", Seal = "书", Kind = ItemKind.Book, Price = 40, Lore = "lore_three_clans", Description = "Кто, когда и за что резал друг друга на Цинмао." },
            new ItemDef { Id = "book_beasts", Name = "«Звери Цинмао»", Seal = "书", Kind = ItemKind.Book, Price = 30, Lore = "lore_beasts", Description = "Охотничий справочник Сюн Лин, с её же пометками." },
            new ItemDef { Id = "book_ancient", Name = "«Дневник гробокопателя»", Seal = "书", Kind = ItemKind.Book, Price = 50, Lore = "lore_ancient_master", Description = "Найден в руднике. Последняя запись обрывается на полуслове." },
            new ItemDef { Id = "book_paths", Name = "«Десять путей»", Seal = "书", Kind = ItemKind.Book, Price = 45, Lore = "lore_paths", Description = "Трактат о путях Гу: луна, земля, огонь, яд и другие." },
            new ItemDef { Id = "book_spring_autumn", Name = "«Цикада Весны и Осени»", Seal = "书", Kind = ItemKind.Book, Price = 0, Lore = "lore_cicada", Description = "Пять строк без подписи. Кто-то очень хотел, чтобы это забыли." },

            // ------------------------------------------------------------------ Keys

            new ItemDef { Id = "tomb_key", Name = "Нефритовая печать гробницы", Seal = "印", Kind = ItemKind.Key, Description = "Открывает внутренние двери Гробницы Древнего мастера." },
            new ItemDef { Id = "mine_key", Name = "Ключ штейгера", Seal = "钥", Kind = ItemKind.Key, Description = "Ржавый ключ от нижнего штрека рудника." },
            new ItemDef { Id = "fort_key", Name = "Ключ от погреба Чёрного Хвоста", Seal = "钥", Kind = ItemKind.Key, Description = "Им запирают пленных и серебро." },
            new ItemDef { Id = "bai_vault_key", Name = "Ключ хранилища Бай", Seal = "钥", Kind = ItemKind.Key, Description = "Ключ из белого нефрита. Дают только своим." },

            // ------------------------------------------------------------------ Scrolls of the new fusions

            new ItemDef { Id = "scroll_flame_burst", Name = "Свиток: Гу Огненного взрыва", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "flame_burst", Price = 90, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_fire_robe", Name = "Свиток: Гу Огненной одежды", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "fire_robe", Price = 110, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_toxic_mist", Name = "Свиток: Гу Ядовитого тумана", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "toxic_mist", Price = 110, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_stealth_scales", Name = "Свиток: Гу Скрытой чешуи", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "stealth_scales", Price = 140, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_bear_roar", Name = "Свиток: Гу Медвежьего рыка", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "bear_roar", Price = 110, Description = "Рецепт клана Сюн." },
            new ItemDef { Id = "scroll_white_boar", Name = "Свиток: Гу Белого вепря", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "white_boar", Price = 150, Description = "Рецепт клана Сюн." },
            new ItemDef { Id = "scroll_river_serpent", Name = "Свиток: Гу Речного змея", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "river_serpent", Price = 120, Description = "Рецепт клана Бай." },
            new ItemDef { Id = "scroll_white_cloud", Name = "Свиток: Гу Белого облака", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "white_cloud", Price = 130, Description = "Рецепт клана Бай." },
            new ItemDef { Id = "scroll_moon_shadow", Name = "Свиток: Гу Лунной тени", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "moon_shadow", Price = 140, Description = "Рецепт клана Гу Юэ." },
            new ItemDef { Id = "scroll_earth_spikes", Name = "Свиток: Гу Каменных шипов", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "earth_spikes", Price = 110, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_bone_spear", Name = "Свиток: Гу Костяного копья", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "bone_spear", Price = 120, Description = "Рецепт, нацарапанный на кости." },
            new ItemDef { Id = "scroll_steel_bone", Name = "Свиток: Гу Стальной кости", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "steel_bone", Price = 150, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_blood_skull", Name = "Свиток: Гу Кровавого черепа", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "blood_skull", Price = 200, Description = "Рецепт демонического пути. Хранить такой — уже преступление." },
            new ItemDef { Id = "scroll_gold_silkworm", Name = "Свиток: Золотой шелкопряд", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "gold_silkworm", Price = 200, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_thunder_wing", Name = "Свиток: Гу Громового крыла", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "thunder_wing", Price = 130, Description = "Рецепт слияния. Прочтите в сумке." },
            new ItemDef { Id = "scroll_blazing_feather", Name = "Свиток: Гу Пылающего пера", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "blazing_feather", Price = 420, Description = "Рецепт третьего ранга." },
            new ItemDef { Id = "scroll_ten_thousand_needles", Name = "Свиток: Гу Десяти тысяч игл", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "ten_thousand_needles", Price = 420, Description = "Рецепт третьего ранга, пахнущий болотом." },
            new ItemDef { Id = "scroll_great_bear", Name = "Свиток: Гу Великого медведя", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "great_bear", Price = 450, Description = "Тайный рецепт клана Сюн." },
            new ItemDef { Id = "scroll_ice_lotus", Name = "Свиток: Гу Ледяного лотоса", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "ice_lotus", Price = 450, Description = "Тайный рецепт клана Бай." },
            new ItemDef { Id = "scroll_heaven_echo", Name = "Свиток: Гу Небесного отзвука", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "heaven_echo", Price = 400, Description = "Рецепт третьего ранга." },
            new ItemDef { Id = "scroll_ancient_bone", Name = "Свиток: Гу Древней кости", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "ancient_bone", Price = 500, Description = "Высечен на стене гробницы, вы перерисовали его." },
            new ItemDef { Id = "scroll_moon_lake", Name = "Свиток: Гу Лунного озера", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "moon_lake", Price = 480, Description = "Тайный рецепт клана Гу Юэ." },
            new ItemDef { Id = "scroll_dark_tide", Name = "Свиток: Гу Тёмного прилива", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "dark_tide", Price = 450, Description = "Рецепт, который не должен был покинуть паучьи пещеры." },

            // ------------------------------------------------------------------ Quest things of the regions

            new ItemDef { Id = "bai_letter", Name = "Письмо клану Бай", Seal = "信", Kind = ItemKind.Quest, Description = "Запечатано лунной печатью Гу Юэ. Тяжелее, чем кажется." },
            new ItemDef { Id = "xiong_letter", Name = "Письмо клану Сюн", Seal = "信", Kind = ItemKind.Quest, Description = "Запечатано лунной печатью Гу Юэ." },
            new ItemDef { Id = "lost_hairpin", Name = "Серебряная шпилька", Seal = "簪", Kind = ItemKind.Quest, Description = "Шпилька с жемчужиной в виде капли. Бай Мэй ищет её третий день." },
            new ItemDef { Id = "honey_pot", Name = "Горшок мёда", Seal = "罐", Kind = ItemKind.Quest, Description = "Мёд для старейшины Сюн. Медведи о нём тоже знают." },
            new ItemDef { Id = "mill_stone", Name = "Жернов мельницы", Seal = "磨", Kind = ItemKind.Quest, Description = "Каменный круг с дыркой. Удивительно, что вы его несёте." },
            new ItemDef { Id = "stolen_rice", Name = "Мешок риса", Seal = "米", Kind = ItemKind.Quest, Description = "Дань хуторов, украденная Чёрным Хвостом." },
            new ItemDef { Id = "miner_tag", Name = "Бирка рудокопа", Seal = "牌", Kind = ItemKind.Quest, Description = "Медная бирка с именем. В руднике таких много." },
            new ItemDef { Id = "eagle_egg", Name = "Орлиное яйцо", Seal = "卵", Kind = ItemKind.Quest, Description = "Тёплое. Внутри кто-то шевелится." },
            new ItemDef { Id = "spirit_seed", Name = "Семя Великого древа", Seal = "种", Kind = ItemKind.Quest, Description = "Семя с кулак. Ведунья говорит, древу без него не жить." },
            new ItemDef { Id = "tomb_rubbing", Name = "Оттиск надписи", Seal = "拓", Kind = ItemKind.Quest, Description = "Тушь на рисовой бумаге: письмена, которых никто не читает." },
            new ItemDef { Id = "black_tail_seal", Name = "Печать Чёрного Хвоста", Seal = "印", Kind = ItemKind.Quest, Description = "Медная печать атамана. С ней в руках вас примут за своего — или убьют быстрее." },
            new ItemDef { Id = "peace_treaty", Name = "Договор о водах", Seal = "约", Kind = ItemKind.Quest, Description = "Три печати: луна, снег, медведь. Пока одной не хватает." },
        };
    }
}
