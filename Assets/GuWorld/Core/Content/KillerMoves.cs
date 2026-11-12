namespace GuWorld.Core
{
    /// <summary>
    /// A killer move: several Gu urged at once to one end, so that what comes out is more than the sum
    /// of them. It is cast through its core Gu (which must sit in an active slot, and goes on its
    /// cooldown), and the supporting Gu must be slotted too (active or passive). A master comes to a
    /// move by comprehension — spending insight once the Gu are together — or is taught it.
    /// </summary>
    public class KillerMoveDef
    {
        public string Id;
        public string Name;
        /// <summary>One hanzi for its seal.</summary>
        public string Seal;
        public string Description;
        /// <summary>The hero's least rank to wield it.</summary>
        public int Rank = 1;
        /// <summary>The Gu it is cast through (an active Gu).</summary>
        public string Core;
        /// <summary>The Gu that must be slotted with it.</summary>
        public string[] Supports = new string[0];
        public SkillDef Skill;
        /// <summary>Insight comprehending it takes (0: it cannot be comprehended, only taught).</summary>
        public int Insight;
        public int Colour = 0xFFFFFF;
    }

    public static class KillerMoves
    {
        public static readonly KillerMoveDef[] All =
        {
            // ------------------------------------------------------------------ Rank 1

            new KillerMoveDef
            {
                Id = "moon_on_wind", Name = "Луна на ветру", Seal = "飘", Rank = 1, Core = "moonlight", Supports = new[] { "tailwind" }, Insight = 60, Colour = 0xBFE0FF,
                Description = "Ветер подхватывает лунный клинок и разносит его на три: широкий веер, летящий вдвое быстрее.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 30, Count = 3, Cost = 22, Cooldown = 3, Range = 18, Radius = 0.8f, Speed = 44 }
            },
            new KillerMoveDef
            {
                Id = "jade_charge", Name = "Нефритовый натиск", Seal = "冲", Rank = 1, Core = "tailwind", Supports = new[] { "jade_skin", "black_boar" }, Insight = 80, Colour = 0xB8F0C8,
                Description = "Тело в нефрите, сила вепря и ветер за спиной: рывок, что сшибает и оглушает всех на пути.",
                Skill = new SkillDef { Shape = SkillShape.Dash, Power = 34, Range = 10, Radius = 2, Cost = 25, Cooldown = 7, Status = StatusKind.Stun, StatusTime = 1.2f }
            },
            new KillerMoveDef
            {
                Id = "venom_spark", Name = "Ядовитая искра", Seal = "螫", Rank = 1, Core = "spark", Supports = new[] { "poison_needle" }, Insight = 70, Colour = 0xB0FF90,
                Description = "Искра несёт яд иглы и прыгает от врага к врагу. Каждый, кого она коснулась, травится.",
                Skill = new SkillDef { Shape = SkillShape.Chain, Power = 20, Range = 14, Radius = 7, Count = 4, Cost = 20, Cooldown = 4, Status = StatusKind.Poison, StatusPower = 6, StatusTime = 5 }
            },

            // ------------------------------------------------------------------ Rank 2

            new KillerMoveDef
            {
                Id = "frost_moon", Name = "Морозная луна", Seal = "霜", Rank = 2, Core = "moonglow", Supports = new[] { "ice_spike" }, Insight = 220, Colour = 0xD0F4FF,
                Description = "Лунный свет, остуженный льдом: серп пробивает всех на линии и выстуживает их.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 70, Range = 26, Radius = 1.2f, Speed = 36, Pierce = true, Cost = 40, Cooldown = 5, Status = StatusKind.Chill, StatusPower = 0.35f, StatusTime = 4 }
            },
            new KillerMoveDef
            {
                Id = "thunder_stride", Name = "Громовая поступь", Seal = "踏", Rank = 2, Core = "storm_step", Supports = new[] { "whirl" }, Insight = 240, Colour = 0xD8D0FF,
                Description = "Прыжок молнией в вихре: где вы приземлились, всё оглушено.",
                Skill = new SkillDef { Shape = SkillShape.Blink, Power = 62, Range = 14, Radius = 4.5f, Cost = 42, Cooldown = 8, Status = StatusKind.Stun, StatusTime = 1.5f }
            },
            new KillerMoveDef
            {
                Id = "bear_quake", Name = "Медвежья дрожь", Seal = "撼", Rank = 2, Core = "bear_strength", Supports = new[] { "earth_spikes", "white_boar" }, Insight = 260, Colour = 0xE0B880,
                Description = "Удар медведя в землю: из неё вокруг встают каменные шипы, враги валятся с ног.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 80, Radius = 6, Cost = 48, Cooldown = 10, Status = StatusKind.Stun, StatusTime = 1.8f }
            },
            new KillerMoveDef
            {
                Id = "shadow_moon", Name = "Удар из лунной тени", Seal = "影", Rank = 2, Core = "moon_shadow", Supports = new[] { "stealth_scales" }, Insight = 250, Colour = 0x9EA8E0,
                Description = "Шаг в тень и удар луной из неё — из скрытности он втрое сильнее.",
                Skill = new SkillDef { Shape = SkillShape.Blink, Power = 90, Range = 12, Radius = 2.5f, Cost = 44, Cooldown = 9, Status = StatusKind.Bleed, StatusPower = 10, StatusTime = 4 }
            },
            new KillerMoveDef
            {
                Id = "poison_fire", Name = "Ядовитое пламя", Seal = "瘴", Rank = 2, Core = "toxic_mist", Supports = new[] { "flame_burst" }, Insight = 230, Colour = 0xC8E060,
                Description = "Ядовитый туман, подожжённый изнутри: облако горит и травит пять секунд.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 26, Range = 16, Radius = 5, Duration = 5, Count = 8, Cost = 46, Cooldown = 12, Status = StatusKind.Poison, StatusPower = 9, StatusTime = 5 }
            },
            new KillerMoveDef
            {
                Id = "bone_forest", Name = "Костяной лес", Seal = "林", Rank = 2, Core = "bone_spear", Supports = new[] { "bone_needle", "steel_bone" }, Insight = 270, Colour = 0xF0E8D8,
                Description = "Семь костяных копий веером, каждое пробивает насквозь и оставляет рану.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 44, Count = 7, Range = 20, Radius = 0.9f, Speed = 32, Pierce = true, Cost = 50, Cooldown = 7, Status = StatusKind.Bleed, StatusPower = 6, StatusTime = 4 }
            },
            new KillerMoveDef
            {
                Id = "serpent_cloud", Name = "Змей в облаках", Seal = "蛟", Rank = 2, Core = "river_serpent", Supports = new[] { "white_cloud", "water_shield" }, Insight = 240, Colour = 0x90D0F0,
                Description = "Речной змей уходит в облако и падает на врагов дождём, что не даёт им бежать.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 34, Range = 18, Radius = 6, Duration = 4, Count = 9, Cost = 45, Cooldown = 11, Status = StatusKind.Slow, StatusPower = 0.4f, StatusTime = 3 }
            },

            // ------------------------------------------------------------------ Rank 3

            new KillerMoveDef
            {
                Id = "blood_eclipse", Name = "Кровавое затмение", Seal = "蚀", Rank = 3, Core = "blood_moon", Supports = new[] { "golden_moon", "blood_skull" }, Insight = 700, Colour = 0xFF6070,
                Description = "Золотая луна затмевается кровавой: пять серпов, и пятая часть урона возвращается кровью.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 120, Count = 5, Range = 24, Radius = 1.4f, Speed = 34, Pierce = true, Cost = 80, Cooldown = 9, Leech = 0.2f }
            },
            new KillerMoveDef
            {
                Id = "storm_crown", Name = "Корона бурь", Seal = "冕", Rank = 3, Core = "thunder_crown", Supports = new[] { "storm_step", "thunder_wing" }, Insight = 750, Colour = 0xD0C0FF,
                Description = "Корона поднимает над целью грозу на шесть секунд — молнии бьют в каждого, кто под ней.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 70, Range = 22, Radius = 8, Duration = 6, Count = 14, Cost = 90, Cooldown = 16, Status = StatusKind.Stun, StatusTime = 0.6f }
            },
            new KillerMoveDef
            {
                Id = "demon_lotus", Name = "Лотос морозного демона", Seal = "莲", Rank = 3, Core = "ice_lotus", Supports = new[] { "frost_demon" }, Insight = 720, Colour = 0xE0FFFF,
                Description = "Лотос распускается льдом демона вокруг вас: всё в семи шагах замерзает.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 130, Radius = 7, Cost = 85, Cooldown = 14, Status = StatusKind.Freeze, StatusTime = 2.5f }
            },
            new KillerMoveDef
            {
                Id = "phoenix_feather", Name = "Перо феникса", Seal = "凰", Rank = 3, Core = "blazing_feather", Supports = new[] { "fire_robe", "ember" }, Insight = 700, Colour = 0xFF8040,
                Description = "Пылающее перо, одетое в огонь: широкий конус пламени, после которого горят ещё шесть секунд.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 125, Range = 11, Radius = 60, Cost = 80, Cooldown = 10, Status = StatusKind.Burn, StatusPower = 18, StatusTime = 6 }
            },
            new KillerMoveDef
            {
                Id = "thousand_echoes", Name = "Тысяча отзвуков", Seal = "响", Rank = 3, Core = "heaven_echo", Supports = new[] { "echo" }, Insight = 680, Colour = 0xF0E0B0,
                Description = "Небесный отзвук, повторённый эхом: удар звука на десять шагов, что оглушает.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 95, Radius = 10, Cost = 78, Cooldown = 13, Status = StatusKind.Stun, StatusTime = 2 }
            },
            new KillerMoveDef
            {
                Id = "black_deluge", Name = "Чёрный потоп", Seal = "渊", Rank = 3, Core = "dark_tide", Supports = new[] { "river_serpent", "night_eye" }, Insight = 740, Colour = 0x6060A0,
                Description = "Тёмный прилив накрывает место волной за волной — враги вязнут, и тьма пьёт их.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 60, Range = 20, Radius = 8, Duration = 5, Count = 12, Cost = 88, Cooldown = 15, Status = StatusKind.Slow, StatusPower = 0.5f, StatusTime = 3, Leech = 0.1f }
            },
            new KillerMoveDef
            {
                Id = "ten_thousand_stings", Name = "Десять тысяч жал", Seal = "蜂", Rank = 3, Core = "ten_thousand_needles", Supports = new[] { "poison_needle", "toxic_mist" }, Insight = 720, Colour = 0xA0E060,
                Description = "Туча игл, напоённых туманом: двенадцать отравленных жал веером.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 50, Count = 12, Range = 20, Radius = 0.6f, Speed = 40, Cost = 82, Cooldown = 9, Status = StatusKind.Poison, StatusPower = 12, StatusTime = 6 }
            },

            // ------------------------------------------------------------------ Rank 4

            new KillerMoveDef
            {
                Id = "sickle_tide", Name = "Прилив лунных серпов", Seal = "潮", Rank = 4, Core = "moon_sickle", Supports = new[] { "moon_lake", "golden_moon" }, Insight = 1800, Colour = 0xE0F0FF,
                Description = "Озеро луны выплёскивается серпами: девять, и ничто их не останавливает.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 150, Count = 9, Range = 28, Radius = 1.8f, Speed = 36, Pierce = true, Cost = 130, Cooldown = 10 }
            },
            new KillerMoveDef
            {
                Id = "giant_step", Name = "Шаг великана", Seal = "震", Rank = 4, Core = "stone_giant", Supports = new[] { "great_bear", "golden_bell" }, Insight = 1900, Colour = 0xD8B078,
                Description = "Каменный великан топает: земля в девяти шагах проваливается, враги оглушены.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 200, Radius = 9, Cost = 140, Cooldown = 16, Status = StatusKind.Stun, StatusTime = 2.5f }
            },
            new KillerMoveDef
            {
                Id = "myriad_storm", Name = "Буря мириад", Seal = "万", Rank = 4, Core = "myriad_lightning", Supports = new[] { "thunder_crown", "ten_thousand_li" }, Insight = 2000, Colour = 0xD8C8FF,
                Description = "Молния, которой некуда кончиться: четырнадцать прыжков, и каждый оглушает.",
                Skill = new SkillDef { Shape = SkillShape.Chain, Power = 120, Range = 24, Radius = 11, Count = 14, Cost = 135, Cooldown = 9, Status = StatusKind.Stun, StatusTime = 0.8f }
            },
            new KillerMoveDef
            {
                Id = "north_blizzard", Name = "Метель с севера", Seal = "雪", Rank = 4, Core = "north_wind_ice", Supports = new[] { "ice_lotus", "whirl" }, Insight = 1900, Colour = 0xE8FFFF,
                Description = "Северный ветер гонит лотосы льда: буря на семь секунд, и кто в ней — замерзает.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 90, Range = 24, Radius = 10, Duration = 7, Count = 16, Cost = 140, Cooldown = 20, Status = StatusKind.Freeze, StatusTime = 1.2f }
            },
            new KillerMoveDef
            {
                Id = "crimson_wings", Name = "Багровые крылья", Seal = "翼", Rank = 4, Core = "crimson_flame", Supports = new[] { "blazing_feather", "ten_thousand_li" }, Insight = 1900, Colour = 0xFF5030,
                Description = "Рывок на крыльях багрового пламени: путь за вами горит.",
                Skill = new SkillDef { Shape = SkillShape.Dash, Power = 170, Range = 18, Radius = 3, Cost = 130, Cooldown = 9, Status = StatusKind.Burn, StatusPower = 28, StatusTime = 5 }
            },

            // ------------------------------------------------------------------ Rank 5

            new KillerMoveDef
            {
                Id = "spring_rain", Name = "Весенний дождь", Seal = "霖", Rank = 4, Core = "spring_life", Supports = new[] { "honey_dew", "life_grass" }, Insight = 1700, Colour = 0xA0F0A0,
                Description = "Весна проливается дождём: здоровье возвращается целиком за пять секунд.",
                Skill = new SkillDef { Shape = SkillShape.Heal, Power = 900, Duration = 5, Cost = 140, Cooldown = 40 }
            },
            new KillerMoveDef
            {
                Id = "venom_flood", Name = "Ядовитый потоп", Seal = "瘟", Rank = 4, Core = "venom_dragon", Supports = new[] { "toxic_mist", "water_dragon" }, Insight = 1900, Colour = 0x80E060,
                Description = "Ядовитый дракон тянет за собой реку: туча яда над местом на шесть секунд.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 70, Range = 24, Radius = 9, Duration = 6, Count = 14, Cost = 135, Cooldown = 16, Status = StatusKind.Poison, StatusPower = 34, StatusTime = 8 }
            },
            new KillerMoveDef
            {
                Id = "lion_roar", Name = "Рык льва", Seal = "獅", Rank = 4, Core = "thunder_roar", Supports = new[] { "bear_roar", "echo" }, Insight = 1800, Colour = 0xF0D080,
                Description = "Громовой рык, умноженный эхом: всё вокруг на двенадцать шагов падает оглушённым.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 170, Radius = 12, Cost = 130, Cooldown = 18, Status = StatusKind.Stun, StatusTime = 3 }
            },
            new KillerMoveDef
            {
                Id = "blood_bloom", Name = "Цветение крови", Seal = "綻", Rank = 4, Core = "blood_lotus", Supports = new[] { "blood_moon", "blood_skull" }, Insight = 2000, Colour = 0xFF3050,
                Description = "Кровавый лотос раскрывается трижды, и половина урона возвращается кровью.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 110, Range = 6, Radius = 8, Duration = 2, Count = 6, Cost = 140, Cooldown = 14, Leech = 0.5f }
            },
            new KillerMoveDef
            {
                Id = "mountain_fall", Name = "Обрушение горы", Seal = "崩", Rank = 5, Core = "mountain", Supports = new[] { "golden_bell", "earth_spikes" }, Insight = 5000, Colour = 0xD0B080,
                Description = "Не одна гора — хребет. Три удара земли с неба, и под ними никто не стоит.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 320, Range = 26, Radius = 11, Duration = 3, Count = 6, Cost = 300, Cooldown = 34, Status = StatusKind.Stun, StatusTime = 3 }
            },
            new KillerMoveDef
            {
                Id = "great_sun", Name = "Великое солнце", Seal = "陽", Rank = 5, Core = "sun_flame", Supports = new[] { "crimson_flame", "fire_robe" }, Insight = 5100, Colour = 0xFFC060,
                Description = "Солнце ростом с дом. Взрыв на четырнадцать шагов, и всё в нём горит восемь секунд.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 600, Range = 32, Radius = 14, Speed = 14, Cost = 320, Cooldown = 36, Status = StatusKind.Burn, StatusPower = 60, StatusTime = 8 }
            },
            new KillerMoveDef
            {
                Id = "heaven_gale", Name = "Небесный шквал", Seal = "飆", Rank = 5, Core = "sky_wind", Supports = new[] { "ten_thousand_li", "whirl" }, Insight = 4900, Colour = 0xE8FFF8,
                Description = "Небесный ветер и десять тысяч ли в нём: буря на девять секунд, из которой не выйти.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 190, Range = 30, Radius = 13, Duration = 9, Count = 22, Cost = 300, Cooldown = 38, Status = StatusKind.Slow, StatusPower = 0.6f, StatusTime = 3 }
            },
            new KillerMoveDef
            {
                Id = "moonfall", Name = "Лунопад", Seal = "坠", Rank = 5, Core = "full_moon", Supports = new[] { "moon_sickle", "moon_lake" }, Insight = 5000, Colour = 0xFFFFFF,
                Description = "Полная луна падает на землю. Восемь секунд в двенадцати шагах не остаётся ничего живого.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 200, Range = 30, Radius = 12, Duration = 8, Count = 22, Cost = 300, Cooldown = 40 }
            },
            new KillerMoveDef
            {
                Id = "frozen_heaven", Name = "Застывшие небеса", Seal = "凝", Rank = 5, Core = "heaven_ice", Supports = new[] { "north_wind_ice", "ice_lotus" }, Insight = 5000, Colour = 0xF0FFFF,
                Description = "Небо застывает: всё в четырнадцати шагах — лёд на пять секунд.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 320, Radius = 14, Cost = 300, Cooldown = 36, Status = StatusKind.Freeze, StatusTime = 5 }
            },
            new KillerMoveDef
            {
                Id = "heaven_judgement", Name = "Суд небес", Seal = "罚", Rank = 5, Core = "heaven_thunder", Supports = new[] { "myriad_lightning", "storm_step" }, Insight = 5200, Colour = 0xF0E8FF,
                Description = "Громы небес падают на место столбами, восемь секунд.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 240, Range = 28, Radius = 10, Duration = 8, Count = 20, Cost = 310, Cooldown = 38, Status = StatusKind.Stun, StatusTime = 1 }
            },
            new KillerMoveDef
            {
                Id = "immortal_fist", Name = "Кулак бессмертного тела", Seal = "拳", Rank = 5, Core = "stone_giant", Supports = new[] { "immortal_body", "great_bear" }, Insight = 4800, Colour = 0xFFE8C0,
                Description = "Удар тела, которое не берёт ни меч, ни яд: всё перед вами сметено, и вы неуязвимы ещё мгновение.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 380, Range = 9, Radius = 80, Cost = 280, Cooldown = 30, Status = StatusKind.Stun, StatusTime = 3 }
            },
        };

        public static KillerMoveDef Get(string id)
        {
            foreach (var move in All)
            {
                if (move.Id == id)
                {
                    return move;
                }
            }

            return null;
        }

        /// <summary>Every Gu a move needs, the core first.</summary>
        public static string[] GuOf(KillerMoveDef move)
        {
            var all = new string[move.Supports.Length + 1];
            all[0] = move.Core;
            move.Supports.CopyTo(all, 1);

            return all;
        }
    }
}
