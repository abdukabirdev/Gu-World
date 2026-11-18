using System;

namespace GuWorld.Core
{
    /// <summary>
    /// The story's words that depend on who the hero is: their name, and the talent they were born with
    /// (the elder reads it off the ancestors' stone, the instructor teases them about it, the ending
    /// remembers it). The texts carry tokens in braces; Fill puts the hero in.
    /// </summary>
    public static class Words
    {
        /// <summary>The talent the hero was born with (the path's, or 100% with the Ten Extreme Physique).</summary>
        public static float BornTalent(SaveData state) =>
            state.ExtremePhysique ? 1f : Beginnings.Of(state.Settings?.Born ?? Beginnings.Reference).Talent;

        /// <summary>
        /// The text with its tokens filled: {name}; {shares} / {Shares} — the born talent as "сорок четыре доли";
        /// {n} — as "сорок четыре"; {grade} — its letter; {bowl}, {elder_eyes}, {elder_well}, {instructor} —
        /// the ceremony's words for that talent. Text without a brace comes back as it is.
        /// </summary>
        public static string Fill(string text, SaveData state)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0 || state == null)
            {
                return text;
            }

            var talent = BornTalent(state);
            var percent = (int)Math.Round(talent * 100);
            var grade = Cultivation.TalentName(talent);
            var high = talent >= 0.6f;
            var shares = Shares(percent);

            return text
                .Replace("{name}", Look.CleanName(state.HeroName))
                .Replace("{Shares}", Capital(shares))
                .Replace("{shares}", shares)
                .Replace("{n}", Number(percent, false))
                .Replace("{grade}", grade)
                .Replace("{bowl}", Bowl(talent))
                .Replace("{elder_eyes}", ElderEyes(talent, state.ExtremePhysique))
                .Replace("{elder_well}", high ? "Глубокий колодец — ещё не вода. Носить её придётся, как всем." : "Колодец мелок — значит, будешь носить воду чаще других.")
                .Replace("{instructor}", high ? "Не задирай нос." : "Ничего.");
        }

        static string Bowl(float talent) =>
            talent >= 0.99f ? "полная чаша, и свет бьётся о края, будто ему тесно"
            : talent >= 0.8f ? "почти полная чаша"
            : talent >= 0.6f ? "чуть больше половины чаши"
            : talent >= 0.4f ? "меньше половины чаши"
            : "едва треть чаши, на самом донышке";

        static string ElderEyes(float talent, bool extreme) =>
            extreme ? "Телосложение Десяти Крайностей. Старейшины побледнели, ты видел? За стенами академии — ни слова об этом."
            : talent >= 0.8f ? "Старейшины зашевелились, ты видел? Теперь на тебя будут смотреть. Не дай им себя сломать."
            : talent >= 0.6f ? "Старейшины кивнули — и забыли о тебе. Это лучше, чем кажется."
            : talent >= 0.4f ? "Старейшины уже отвернулись, ты видел? Не смотри на них. Смотри на меня."
            : "Старейшины даже не подняли глаз. Не смотри на них. Смотри на меня.";

        /// <summary>"сорок четыре доли", "шестьдесят две доли", "тридцать долей", "сто долей".</summary>
        public static string Shares(int count)
        {
            var last = count % 10;
            var teen = count % 100 >= 11 && count % 100 <= 14;
            var noun = !teen && last == 1 ? "доля" : !teen && last >= 2 && last <= 4 ? "доли" : "долей";

            return Number(count, true) + " " + noun;
        }

        static readonly string[] Units = { "ноль", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять",
                                           "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
                                           "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };

        static readonly string[] Tens = { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };

        /// <summary>0…100 in words; feminine: "одна", "две" (for "доля").</summary>
        public static string Number(int n, bool feminine)
        {
            n = Math.Max(0, Math.Min(100, n));

            if (n == 100)
            {
                return "сто";
            }

            string Unit(int u) => feminine && u == 1 ? "одна" : feminine && u == 2 ? "две" : Units[u];

            if (n < 20)
            {
                return Unit(n);
            }

            return n % 10 == 0 ? Tens[n / 10] : Tens[n / 10] + " " + Unit(n % 10);
        }

        static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
