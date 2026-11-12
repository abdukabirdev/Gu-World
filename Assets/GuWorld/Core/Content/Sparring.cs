namespace GuWorld.Core
{
    /// <summary>
    /// The academy's sparring partners: senior students and teachers of each rank who fight the hero in
    /// the classroom when the teacher allows (three bouts a day). They hit softly (a bout is practice:
    /// the hero cannot fall below a tenth of their health in it) and are worth merit.
    /// </summary>
    public static class Sparring
    {
        public const int BoutsADay = 3;

        static EnemyDef Partner(int rank, string name, string look, float health, float damage, params string[] gu) => new EnemyDef
        {
            Id = "sparring_" + rank, Name = name, Title = "Спарринг", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = look, Level = rank * 5, Health = health, Damage = damage,
            Speed = 4.8f + 0.2f * rank, AttackRange = 14, AttackPeriod = 2.6f, Sight = 30, Insight = 20 * rank, Loot = "sparring", Poise = 10 + 8 * rank, Colour = 0xC0D8FF, Clan = "", Gu = gu
        };

        public static readonly EnemyDef[] Enemies =
        {
            Partner(1, "Старший ученик", "student", 260, 8, "moonlight", "jade_skin"),
            Partner(2, "Лучший ученик года", "student", 700, 15, "moonglow", "storm_step", "rock_skin"),
            Partner(3, "Наставник-помощник", "scribe", 1600, 24, "golden_moon", "moon_shadow", "ice_spike"),
            Partner(4, "Наставник академии", "scribe", 3200, 40, "moon_sickle", "thunder_crown", "golden_moon"),
            Partner(5, "Старейшина-наставник", "elder", 6000, 60, "full_moon", "moon_sickle", "north_wind_ice"),
        };

        public static readonly LootTable[] Tables =
        {
            new LootTable { Id = "sparring", StonesMin = 0, StonesMax = 0, Items = new[] { new LootEntry { Item = "essence_pill", Chance = 0.5f } } },
        };

        /// <summary>The partner for a hero of a rank.</summary>
        public static EnemyDef For(int rank) => Enemies[System.Math.Max(0, System.Math.Min(Enemies.Length - 1, rank - 1))];
    }
}
