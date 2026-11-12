namespace GuWorld.Core
{
    /// <summary>A room the hero can step into: its size, its look, and the furniture in it (the artist's props, placed from the room's middle; the door is at −z).</summary>
    public sealed class InteriorDef
    {
        public string Id;
        public string Name;
        public float Width;
        public float Depth;
        public float Height;
        /// <summary>"hall" (lacquered pillars, a high ceiling), "house" (plaster and timber), "inn", "nursery".</summary>
        public string Style;
        public (string model, float x, float z, float yaw)[] Furniture = new (string, float, float, float)[0];
        /// <summary>Warm lights hung from the beams, evenly.</summary>
        public int Lights = 2;
        /// <summary>Who is in the room: a look, where they stand, their name and title, what they say.</summary>
        public (string look, float x, float z, float yaw, string name, string title, string line)[] People = new (string, float, float, float, string, string, string)[0];
    }

    /// <summary>
    /// The insides of the Gu Yue town's great buildings: the clan's main hall with its ancestral altar,
    /// the academy's classroom, the inn's room, the Gu nursery's jar halls. Their doors stand at the
    /// buildings' own doors; inside, the rooms float far above the mountain (they need no ground).
    /// </summary>
    public static class Interiors
    {
        public static readonly InteriorDef[] All =
        {
            new InteriorDef
            {
                Id = "clan_hall", Name = "Главный зал клана Гу Юэ", Width = 22, Depth = 30, Height = 9, Style = "hall", Lights = 6,
                People = new[]
                {
                    ("elder", -2.5f, 10f, 180f, "Старейшина зала", "Хранитель предков", "Здесь висят имена тех, кто держал клан три сотни лет. Твоё имя тоже может здесь оказаться. Или нет."),
                    ("scribe", 7f, 11.5f, 225f, "Казначей клана", "Сокровищница", "Заслуги перед кланом не пропадают. Покажи, что ты заработал, — и сокровищница откроется."),
                    ("guard", -3f, -12.5f, 0f, "Страж зала", "Гу Юэ", "В зале не обнажают Гу. Даже если очень хочется."),
                    ("guard", 3f, -12.5f, 0f, "Страж зала", "Гу Юэ", "Проходи. Старейшины сегодня в духе."),
                },
                Furniture = new[]
                {
                    ("prop_ancestral_altar", 0f, 12.5f, 180f), ("prop_bronze_ding", 0f, 7.5f, 0f), ("prop_folding_screen", 0f, 14.2f, 180f),
                    ("prop_luohan_bed", -6f, 9f, 90f), ("prop_luohan_bed", 6f, 9f, -90f),
                    ("prop_bench_wood", -6f, 2f, 90f), ("prop_bench_wood", -6f, -2f, 90f), ("prop_bench_wood", -6f, -6f, 90f),
                    ("prop_bench_wood", 6f, 2f, -90f), ("prop_bench_wood", 6f, -2f, -90f), ("prop_bench_wood", 6f, -6f, -90f),
                    ("prop_lotus_vat", -8.5f, -12f, 0f), ("prop_lotus_vat", 8.5f, -12f, 0f), ("prop_weapon_rack", -10.2f, 6f, 90f), ("prop_weapon_rack", 10.2f, 6f, -90f),
                }
            },
            new InteriorDef
            {
                Id = "academy", Name = "Класс академии Гу Юэ", Width = 16, Depth = 20, Height = 6, Style = "house", Lights = 4,
                People = new[]
                {
                    ("scribe", 0f, 8.6f, 180f, "Наставник академии", "Гу Юэ", "Сядь. Сегодня формула очищения: эссенция идёт по кругу, а не туда, куда хочется тебе."),
                    ("student", -4f, 0.8f, 0f, "Ученик", "Академия Гу Юэ", "Тсс. Наставник смотрит. Он всегда смотрит."),
                    ("student", 4f, -3.7f, 0f, "Ученица", "Академия Гу Юэ", "Я опять уронила кувшин с Гу. Он выжил. Я — не уверена."),
                },
                Furniture = new[]
                {
                    ("prop_writing_desk", 0f, 7.5f, 180f), ("prop_curio_shelf", -4f, 9.3f, 180f), ("prop_curio_shelf", 4f, 9.3f, 180f),
                    ("prop_writing_desk", -4f, 2f, 0f), ("prop_writing_desk", 0f, 2f, 0f), ("prop_writing_desk", 4f, 2f, 0f),
                    ("prop_writing_desk", -4f, -2.5f, 0f), ("prop_writing_desk", 0f, -2.5f, 0f), ("prop_writing_desk", 4f, -2.5f, 0f),
                    ("prop_gu_jar_shelf", -7.4f, 4f, 90f), ("prop_gu_jar_shelf", 7.4f, 4f, -90f), ("prop_bonsai_pine", 6.5f, -8.5f, 0f),
                }
            },
            new InteriorDef
            {
                Id = "inn_room", Name = "Зал постоялого двора", Width = 18, Depth = 16, Height = 5.5f, Style = "inn", Lights = 5,
                People = new[]
                {
                    ("cook", 6f, 4f, -90f, "Хозяин двора", "Постоялый двор", "Комната за камни, ужин за улыбку. Улыбка, впрочем, тоже стоит камней."),
                    ("gambler", -4f, 3.2f, 180f, "Игрок в го", "Завсегдатай", "Ещё партию? Ставлю три камня, что ты не видишь, куда я пойду."),
                    ("villager", 4f, -4.5f, 0f, "Путник", "С южной дороги", "У Чёрного Хвоста снова новые люди. Ходите по дороге днём."),
                },
                Furniture = new[]
                {
                    ("prop_go_table", -4f, 2f, 0f), ("prop_go_table", 4f, 2f, 0f), ("prop_go_table", -4f, -3f, 0f), ("prop_go_table", 4f, -3f, 0f),
                    ("prop_bench_wood", -4f, 3.6f, 0f), ("prop_bench_wood", 4f, 3.6f, 0f), ("prop_bench_wood", -4f, -1.4f, 0f), ("prop_bench_wood", 4f, -1.4f, 0f),
                    ("prop_barrels", 6.2f, 5.5f, 0f), ("prop_curio_shelf", -6f, 7.4f, 180f), ("prop_folding_screen", 0f, 7.2f, 180f), ("prop_canopy_bed", -6.5f, -5.5f, 90f),
                }
            },
            new InteriorDef
            {
                Id = "nursery_hall", Name = "Питомник Гу", Width = 14, Depth = 22, Height = 6, Style = "nursery", Lights = 4,
                People = new[]
                {
                    ("keeper", 0f, 8.4f, 180f, "Смотрительница питомника", "Гу Юэ", "Не стучи по кувшинам. Гу Лунного света обижается, а потом три дня не светит."),
                    ("herbalist", -3f, -6f, 90f, "Помощник", "Питомник", "Орхидеи — Лунному свету, камень — Нефриту. Перепутаешь — будешь кормить с ладони."),
                },
                Furniture = new[]
                {
                    ("prop_gu_jar_shelf", -5.6f, 6f, 90f), ("prop_gu_jar_shelf", -5.6f, 1f, 90f), ("prop_gu_jar_shelf", -5.6f, -4f, 90f),
                    ("prop_gu_jar_shelf", 5.6f, 6f, -90f), ("prop_gu_jar_shelf", 5.6f, 1f, -90f), ("prop_gu_jar_shelf", 5.6f, -4f, -90f),
                    ("prop_medicine_cabinet", 0f, 10.3f, 180f), ("prop_writing_desk", 0f, 7f, 180f), ("prop_lotus_vat", 0f, 0f, 0f),
                    ("prop_moonlight_gu", 0.3f, 7.1f, 0f), ("prop_white_jade_gu", -0.3f, 7.2f, 0f),
                }
            },
        };

        public static InteriorDef Get(string id)
        {
            foreach (var room in All)
            {
                if (room.Id == id)
                {
                    return room;
                }
            }

            return null;
        }

        /// <summary>Where the rooms hang: far above the north-east corner of the world, side by side.</summary>
        public static (float x, float y, float z) Origin(string id)
        {
            var index = 0;

            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    index = i;
                }
            }

            return (7300 - index * 80, 2600, 7300);
        }
    }
}
