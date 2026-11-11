using System;
using System.Globalization;

namespace GuWorld.Core
{
    /// <summary>
    /// The little language of conditions and effects the content is written in (DialogueDefs has the
    /// list). A plain word with no colon is a flag, so the old content's Needs read the same.
    /// </summary>
    public partial class Session
    {
        /// <summary>A conversation's effect asked the interface to do something: open a shop, start a fight.</summary>
        public event Action<string, string> Requested;

        /// <summary>The hour the interface last told the session (night and day conditions).</summary>
        public float Hour => State.Hour;

        public bool Night => State.Hour < 5.5f || State.Hour > 19.8f;

        /// <summary>True if every condition holds.</summary>
        public bool MeetsAll(string[] conditions)
        {
            if (conditions == null)
            {
                return true;
            }

            foreach (var condition in conditions)
            {
                if (!Meets(condition))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>True if a condition holds ("" always does).</summary>
        public bool Meets(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return true;
            }

            if (condition[0] == '!')
            {
                return !Meets(condition.Substring(1));
            }

            switch (condition)
            {
                case "night": return Night;
                case "day": return !Night;
            }

            var colon = condition.IndexOf(':');

            if (colon < 0)
            {
                // "stones>=50", "step>=4", "rank>=2", "chapter>=3", "skill>=2", or a bare flag.
                if (SplitComparison(condition, out var name, out var op, out var number))
                {
                    switch (name)
                    {
                        case "stones": return Compare(State.Stones, op, number);
                        case "step": return Compare(State.Step, op, number);
                        case "rank": return Compare(Rank, op, number);
                        case "chapter": return Compare(Chapter, op, number);
                        case "skill": return Compare(State.Refining, op, number);
                        case "day": return Compare(State.Day, op, number);
                        case "kills": return Compare(State.Kills, op, number);
                    }
                }

                return Has(condition);
            }

            var kind = condition.Substring(0, colon);
            var rest = condition.Substring(colon + 1);

            switch (kind)
            {
                case "flag":
                    return Has(rest);

                case "quest":
                    return IsActive(rest);

                case "done":
                    return IsDone(rest);

                case "gu":
                    return State.Gu.Exists(g => g.Def == rest);

                case "visited":
                    return State.Lanterns.Contains(rest);

                case "codex":
                    return State.Codex.Contains(rest);

                case "pill":
                    return KnowsPill(rest);

                case "rep":
                {
                    if (SplitComparison(rest, out var clan, out var op, out var number))
                    {
                        return Compare(ReputationOf(clan), op, number);
                    }

                    return false;
                }

                case "item":
                {
                    if (SplitComparison(rest, out var item, out var op, out var number))
                    {
                        return Compare(Count(item), op, number);
                    }

                    return Count(rest) > 0;
                }

                case "kills":
                {
                    if (SplitComparison(rest, out var enemy, out var op, out var number))
                    {
                        return Compare(KillsOf(enemy), op, number);
                    }

                    return KillsOf(rest) > 0;
                }

                case "standing":
                {
                    // "standing:bai=friendly"
                    var equals = rest.IndexOf('=');

                    if (equals > 0 && Enum.TryParse(rest.Substring(equals + 1), true, out Standing standing))
                    {
                        return Clans.StandingOf(ReputationOf(rest.Substring(0, equals))) >= standing;
                    }

                    return false;
                }
            }

            return Has(condition);
        }

        /// <summary>Why a condition fails, in words for a greyed answer ("Нужно: дружба с кланом Бай").</summary>
        public string Explain(string condition)
        {
            if (string.IsNullOrEmpty(condition) || Meets(condition))
            {
                return "";
            }

            var colon = condition.IndexOf(':');
            var kind = colon > 0 ? condition.Substring(0, colon) : "";
            var rest = colon > 0 ? condition.Substring(colon + 1) : condition;

            if (kind == "rep" && SplitComparison(rest, out var clan, out _, out var need))
            {
                return $"Нужно: {Clans.StandingName(Clans.StandingOf((int)need))} с «{Clans.Get(clan)?.Name ?? clan}»";
            }

            if (kind == "item" && SplitComparison(rest, out var item, out _, out var count))
            {
                return $"Нужно: {ItemBook.Get(item)?.Name ?? item} × {count}";
            }

            if (SplitComparison(condition, out var name, out _, out var number))
            {
                switch (name)
                {
                    case "stones": return $"Нужно {number} камней";
                    case "skill": return $"Нужно мастерство алхимии {number}";
                    case "rank": return $"Нужен {number}-й ранг";
                    case "step": return $"Нужна ступень {number}";
                }
            }

            return "Пока недоступно";
        }

        static bool SplitComparison(string text, out string name, out string op, out float number)
        {
            name = text;
            op = "";
            number = 0;

            foreach (var candidate in new[] { ">=", "<=", "==", "!=", ">", "<", "=" })
            {
                var at = text.IndexOf(candidate, StringComparison.Ordinal);

                if (at > 0)
                {
                    name = text.Substring(0, at);
                    op = candidate;

                    return float.TryParse(text.Substring(at + candidate.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
                }
            }

            return false;
        }

        static bool Compare(float value, string op, float number)
        {
            switch (op)
            {
                case ">=": return value >= number;
                case "<=": return value <= number;
                case ">": return value > number;
                case "<": return value < number;
                case "!=": return Math.Abs(value - number) > 0.001f;
                default: return Math.Abs(value - number) < 0.001f;
            }
        }

        // ------------------------------------------------------------------ Effects

        public void ApplyAll(string[] effects, string speaker = "")
        {
            if (effects == null)
            {
                return;
            }

            foreach (var effect in effects)
            {
                Apply(effect, speaker);
            }
        }

        /// <summary>Does what an effect says. The ones the interface must carry out ("shop", "fight:x") are raised as Requested.</summary>
        public void Apply(string effect, string speaker = "")
        {
            if (string.IsNullOrEmpty(effect))
            {
                return;
            }

            var parts = effect.Split(':');
            var kind = parts[0];
            string Part(int i) => parts.Length > i ? parts[i] : "";
            int Number(int i, int otherwise) => parts.Length > i && int.TryParse(parts[i].TrimStart('+'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : otherwise;

            switch (kind)
            {
                case "flag":
                    SetFlag(Part(1));
                    break;

                case "unflag":
                    State.Flags.Remove(Part(1));
                    break;

                case "give":
                    if (GuBook.Get(Part(1)) != null)
                    {
                        AddGu(MakeGu(Part(1), 0, true));
                    }
                    else
                    {
                        AddItem(Part(1), Number(2, 1));
                    }

                    break;

                case "take":
                    RemoveItem(Part(1), Number(2, 1));
                    break;

                case "stones":
                {
                    var change = Number(1, 0);

                    if (change >= 0)
                    {
                        AddStones(change);
                    }
                    else
                    {
                        State.Stones = Math.Max(0, State.Stones + change);
                        Touch();
                    }

                    break;
                }

                case "rep":
                    ChangeReputation(Part(1), Number(2, 0), false);
                    break;

                case "quest":
                    StartQuest(Part(1));
                    break;

                case "learn":
                    if (Part(1) == "pill")
                    {
                        LearnPill(Part(2));
                    }
                    else if (Part(1) == "fusion" && !State.Recipes.Contains(Part(2)))
                    {
                        State.Recipes.Add(Part(2));
                        Say($"Рецепт слияния: {GuBook.Get(Recipes.Get(Part(2))?.Result)?.Name ?? Part(2)}");
                    }

                    break;

                case "insight":
                    GainInsight(Number(1, 0));
                    break;

                case "gu":
                    AddGu(MakeGu(Part(1), Number(2, 0), true));
                    break;

                case "codex":
                    OpenCodex(Part(1));
                    break;

                case "heal":
                    Restore();
                    Touch();
                    break;

                case "sleep":
                    Sleep(Number(1, 8));
                    break;

                default:
                    // "shop", "gamble", "board", "fight:encounter", "alchemy", "travel"…
                    Requested?.Invoke(kind, parts.Length > 1 ? effect.Substring(kind.Length + 1) : speaker);
                    break;
            }
        }
    }
}
