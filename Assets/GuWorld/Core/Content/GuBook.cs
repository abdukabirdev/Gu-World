using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>Every Gu of the game.</summary>
    public static class GuBook
    {
        public static readonly GuDef[] All =
        {
            // Rank 1.
            new GuDef
            {
                Id = "moonlight", Name = "Гу Лунного света", Seal = "月", Rank = 1, Path = GuPath.Moon, Kind = GuKind.Active, Colour = 0xA0D2FF, Price = 30,
                Description = "Первый Гу каждого ученика Гу Юэ. Бросает лунный клинок на десять шагов.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 22, Cost = 9, Cooldown = 0.7f, Range = 14, Radius = 0.9f, Speed = 28 }
            },
            new GuDef
            {
                Id = "jade_skin", Name = "Гу Нефритовой кожи", Seal = "玉", Rank = 1, Path = GuPath.Earth, Kind = GuKind.Active, Colour = 0x8FE0B0, Price = 40,
                Description = "Кожа отливает нефритом: шесть секунд удары вязнут в ней.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Shield, BuffValue = 0.4f, Duration = 6, Cost = 22, Cooldown = 14 }
            },
            new GuDef
            {
                Id = "tailwind", Name = "Гу Попутного ветра", Seal = "風", Rank = 1, Path = GuPath.Wind, Kind = GuKind.Active, Colour = 0xBFEFE0, Price = 40,
                Description = "Рывок на семь шагов сквозь врагов. В рывке вас не задеть.",
                Skill = new SkillDef { Shape = SkillShape.Dash, Power = 12, Range = 7, Radius = 1.2f, Cost = 12, Cooldown = 4 }
            },
            new GuDef
            {
                Id = "life_grass", Name = "Гу Травы жизни", Seal = "草", Rank = 1, Path = GuPath.Wood, Kind = GuKind.Active, Colour = 0x9FEFC4, Price = 45,
                Description = "Затягивает раны: тридцать пять здоровья за три секунды.",
                Skill = new SkillDef { Shape = SkillShape.Heal, Power = 35, Duration = 3, Cost = 20, Cooldown = 12 }
            },
            new GuDef
            {
                Id = "stone_beetle", Name = "Гу Каменного жука", Seal = "石", Rank = 1, Path = GuPath.Earth, Kind = GuKind.Active, Colour = 0xD0B080, Price = 45,
                Description = "Удар о землю: всё вокруг получает урон и на миг теряет равновесие.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 30, Radius = 4.5f, Cost = 22, Cooldown = 7, Status = StatusKind.Stun, StatusTime = 1.2f }
            },
            new GuDef
            {
                Id = "spark", Name = "Гу Искры", Seal = "電", Rank = 1, Path = GuPath.Lightning, Kind = GuKind.Active, Colour = 0xB8A0FF, Price = 45,
                Description = "Молния бьёт в цель и перескакивает ещё на двоих.",
                Skill = new SkillDef { Shape = SkillShape.Chain, Power = 17, Range = 13, Radius = 6, Count = 3, Cost = 14, Cooldown = 3 }
            },
            new GuDef
            {
                Id = "little_light", Name = "Гу Малого света", Seal = "光", Rank = 1, Path = GuPath.Light, Kind = GuKind.Passive, Colour = 0xFFF1CF, Price = 35,
                Description = "Светится в апертуре и усиливает Гу пути Луны на пятнадцать процентов.",
                Passive = new[] { new StatBonus(StatKind.MoonPower, 0.15f) }
            },
            new GuDef
            {
                Id = "liquor_worm", Name = "Винный червь", Seal = "酒", Rank = 1, Path = GuPath.Wine, Kind = GuKind.Passive, Colour = 0xF0B060, Price = 300,
                Description = "Очищает эссенцию, как вино — воду. Эссенция восстанавливается в полтора раза быстрее.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 0.5f) }
            },

            // Rank 2.
            new GuDef
            {
                Id = "moonglow", Name = "Гу Лунного сияния", Seal = "芒", Rank = 2, Path = GuPath.Moon, Kind = GuKind.Active, Colour = 0xC8E4FF, Price = 140,
                Description = "Клинок шире и острее: проходит сквозь всех на пути.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 46, Cost = 16, Cooldown = 0.8f, Range = 18, Radius = 1.3f, Speed = 30, Pierce = true }
            },
            new GuDef
            {
                Id = "ice_spike", Name = "Гу Ледяного шипа", Seal = "冰", Rank = 2, Path = GuPath.Ice, Kind = GuKind.Active, Colour = 0x9FE8FF, Price = 130,
                Description = "Шип клана Бай: ранит и сковывает холодом на три секунды.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 40, Cost = 15, Cooldown = 1.6f, Range = 20, Radius = 0.8f, Speed = 34, Status = StatusKind.Slow, StatusPower = 0.4f, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "bear_strength", Name = "Гу Медвежьей силы", Seal = "熊", Rank = 2, Path = GuPath.Strength, Kind = GuKind.Active, Colour = 0xE0A070, Price = 130,
                Description = "Сила бурого медведя клана Сюн: десять секунд ладонь бьёт почти вдвое сильнее.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.MeleeUp, BuffValue = 0.8f, Duration = 10, Cost = 28, Cooldown = 20 }
            },
            new GuDef
            {
                Id = "rock_skin", Name = "Гу Каменной кожи", Seal = "岩", Rank = 2, Path = GuPath.Earth, Kind = GuKind.Active, Colour = 0xB8B0A0, Price = 150,
                Description = "Восемь секунд вы — скала: урон вдвое меньше, с ног не сбить.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Unstoppable, BuffValue = 0.55f, Duration = 8, Cost = 30, Cooldown = 18 }
            },
            new GuDef
            {
                Id = "storm_step", Name = "Гу Грозового шага", Seal = "雷", Rank = 2, Path = GuPath.Lightning, Kind = GuKind.Active, Colour = 0xC0A8FF, Price = 150,
                Description = "Скачок на двенадцать шагов. Там, откуда ушли, и там, куда пришли, бьёт молния.",
                Skill = new SkillDef { Shape = SkillShape.Blink, Power = 34, Range = 12, Radius = 3.5f, Cost = 20, Cooldown = 6 }
            },
            new GuDef
            {
                Id = "whirl", Name = "Гу Вихря", Seal = "旋", Rank = 2, Path = GuPath.Wind, Kind = GuKind.Active, Colour = 0xA8F0DC, Price = 130,
                Description = "Вихрь в точке прицела стягивает врагов в кучу и держит две секунды.",
                Skill = new SkillDef { Shape = SkillShape.Pull, Power = 18, Range = 14, Radius = 7, Duration = 2, Cost = 24, Cooldown = 11, Status = StatusKind.Slow, StatusPower = 0.6f, StatusTime = 2 }
            },
            new GuDef
            {
                Id = "white_jade", Name = "Гу Белого нефрита", Seal = "白", Rank = 2, Path = GuPath.Earth, Kind = GuKind.Passive, Colour = 0xEAF0F2, Price = 160,
                Description = "Тело крепнет, как белый нефрит: любой урон меньше на пятнадцать процентов.",
                Passive = new[] { new StatBonus(StatKind.DamageTaken, -0.15f) }
            },
            new GuDef
            {
                Id = "four_wines", Name = "Червь Четырёх вин", Seal = "醇", Rank = 2, Path = GuPath.Wine, Kind = GuKind.Passive, Colour = 0xFFC870, Price = 500,
                Description = "Винный червь, вскормленный четырьмя винами. Эссенция восстанавливается вдвое быстрее, Гу берут её меньше.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 1f), new StatBonus(StatKind.Cost, -0.15f) }
            },

            // Rank 3.
            new GuDef
            {
                Id = "golden_moon", Name = "Гу Золотой луны", Seal = "金", Rank = 3, Path = GuPath.Moon, Kind = GuKind.Active, Colour = 0xFFE08A, Price = 600,
                Description = "Три золотых клинка веером. Вблизи в одну цель попадают все три.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 62, Count = 3, Cost = 26, Cooldown = 1.1f, Range = 20, Radius = 1.2f, Speed = 32, Pierce = true }
            },
            new GuDef
            {
                Id = "frost_demon", Name = "Гу Морозного демона", Seal = "霜", Rank = 3, Path = GuPath.Ice, Kind = GuKind.Active, Colour = 0xBFF4FF, Price = 550,
                Description = "Дыхание стужи: всё перед вами получает урон и замерзает на две секунды.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 70, Range = 9, Radius = 50, Cost = 34, Cooldown = 10, Status = StatusKind.Freeze, StatusTime = 2 }
            },
            new GuDef
            {
                Id = "thunder_crown", Name = "Гу Громовой короны", Seal = "冠", Rank = 3, Path = GuPath.Lightning, Kind = GuKind.Active, Colour = 0xD0B8FF, Price = 700,
                Description = "Гроза в точке прицела: четыре секунды молнии бьют всё, что там стоит.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 38, Range = 18, Radius = 6, Duration = 4, Count = 8, Cost = 40, Cooldown = 14 }
            },
            new GuDef
            {
                Id = "sky_canopy", Name = "Гу Небесного покрова", Seal = "天", Rank = 3, Path = GuPath.Earth, Kind = GuKind.Passive, Colour = 0xFFFFFF, Price = 700,
                Description = "Раз в тридцать секунд покров целиком поглощает один удар.",
                Passive = new[] { new StatBonus(StatKind.WardPeriod, 30f), new StatBonus(StatKind.DamageTaken, -0.1f) }
            },
            new GuDef
            {
                Id = "blood_moon", Name = "Гу Кровавой луны", Seal = "血", Rank = 3, Path = GuPath.Blood, Kind = GuKind.Active, Colour = 0xF05A4A, Price = 0,
                Description = "Двести лет его кормили чужой жизнью. Его клинки возвращают хозяину треть нанесённого урона.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 74, Count = 3, Cost = 22, Cooldown = 1f, Range = 20, Radius = 1.2f, Speed = 32, Pierce = true, Leech = 0.3f }
            },
        };

        static Dictionary<string, GuDef> byId;

        public static GuDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, GuDef>();

                foreach (var gu in All)
                {
                    byId[gu.Id] = gu;
                }
            }

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }

        public static readonly string[] QualityNames = { "обычный", "редкий", "отменный" };
        public static readonly int[] QualityColours = { 0xC9D3D5, 0x6FB8FF, 0xE8C98A };
        public static readonly string[] RankNumerals = { "", "I", "II", "III", "IV", "V" };

        public static string PathName(GuPath path)
        {
            switch (path)
            {
                case GuPath.Moon: return "Путь Луны";
                case GuPath.Earth: return "Путь Земли";
                case GuPath.Wind: return "Путь Ветра";
                case GuPath.Wood: return "Путь Дерева";
                case GuPath.Lightning: return "Путь Молнии";
                case GuPath.Light: return "Путь Света";
                case GuPath.Wine: return "Путь Вина";
                case GuPath.Ice: return "Путь Льда";
                case GuPath.Strength: return "Путь Силы";
                default: return "Путь Крови";
            }
        }

        public static string AffixName(AffixKind kind)
        {
            switch (kind)
            {
                case AffixKind.Sharp: return "Острый";
                case AffixKind.Thrifty: return "Бережливый";
                case AffixKind.Quick: return "Быстрый";
                case AffixKind.Far: return "Дальний";
                case AffixKind.Leech: return "Кровопийца";
                case AffixKind.Sturdy: return "Крепкий";
                default: return "Источник";
            }
        }

        public static string AffixText(Affix affix)
        {
            var percent = (int)System.Math.Round(affix.Value * 100);

            switch (affix.Kind)
            {
                case AffixKind.Sharp: return $"Острый: сила +{percent}%";
                case AffixKind.Thrifty: return $"Бережливый: расход −{percent}%";
                case AffixKind.Quick: return $"Быстрый: перезарядка −{percent}%";
                case AffixKind.Far: return $"Дальний: дальность +{percent}%";
                case AffixKind.Leech: return $"Кровопийца: {percent}% урона в здоровье";
                case AffixKind.Sturdy: return $"Крепкий: здоровье +{(int)affix.Value}";
                default: return $"Источник: эссенция +{percent}%";
            }
        }
    }
}
