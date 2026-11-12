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
