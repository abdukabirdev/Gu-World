using System;

namespace GuWorld.Core
{
    /// <summary>What two things on a creature do when they meet.</summary>
    public enum ReactionKind
    {
        None,
        /// <summary>Fire on the chilled or frozen: the ice bursts into steam and takes flesh with it.</summary>
        Steam,
        /// <summary>A stunning blow on the frozen: it cracks like a pond in spring.</summary>
        Shatter,
        /// <summary>Cold on the burning: the fire chokes, and the body jerks still for a moment.</summary>
        Quench,
        /// <summary>Fire on the poisoned: the venom boils into a smoke that eats it from within.</summary>
        Fume,
        /// <summary>A wound on the poisoned, or venom in a wound: the poison runs with the blood.</summary>
        Fester,
        /// <summary>Fire on the bleeding: the wound is seared shut, and the searing hurts more than it.</summary>
        Sear,
    }

    /// <summary>A reaction that happened: what, how much the blow that caused it gains, how long it holds the creature.</summary>
    public struct Reaction
    {
        public ReactionKind Kind;
        /// <summary>A share of the blow that is dealt again on top of it.</summary>
        public float Bonus;
        /// <summary>Seconds the creature is held still (0 for none).</summary>
        public float Hold;
        /// <summary>True when it ended a freeze (the caller lets the creature go).</summary>
        public bool Thawed;

        public bool Happened => Kind != ReactionKind.None;
    }

    /// <summary>
    /// The reactions of statuses: a fighter who carries Gu of different paths can lay one thing on a
    /// creature and set it off with another. This is what pulls against the resonance of one path —
    /// three Gu of the moon cut harder, a moon, a fire and an ice Gu make steam and shatter.
    /// </summary>
    public static class Reactions
    {
        public static readonly ReactionKind[] All = (ReactionKind[])Enum.GetValues(typeof(ReactionKind));

        public static string Name(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Steam: return "Пар";
                case ReactionKind.Shatter: return "Раскол";
                case ReactionKind.Quench: return "Гашение";
                case ReactionKind.Fume: return "Ядовитый дым";
                case ReactionKind.Fester: return "Заражение";
                case ReactionKind.Sear: return "Прижигание";
                default: return "";
            }
        }

        public static string Text(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Steam: return "Огонь по охлаждённому или замёрзшему: лёд взрывается паром (+60% удара), холод сходит.";
                case ReactionKind.Shatter: return "Оглушающий удар по замёрзшему: лёд раскалывается (+90% удара), заморозка сходит.";
                case ReactionKind.Quench: return "Холод по горящему: огонь гаснет, зверь цепенеет на миг (+30% удара).";
                case ReactionKind.Fume: return "Огонь по отравленному: яд вскипает (+25% удара), отрава жжёт сильнее.";
                case ReactionKind.Fester: return "Рана на отравленном или яд в ране: отрава идёт с кровью, сильнее и дольше.";
                case ReactionKind.Sear: return "Огонь по кровоточащему: рана прижжена (+45% удара), кровь больше не идёт.";
                default: return "";
            }
        }

        /// <summary>The colour a reaction's word is shown in.</summary>
        public static int Colour(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Steam: return 0xE8F4FF;
                case ReactionKind.Shatter: return 0x9AE0FF;
                case ReactionKind.Quench: return 0xB8C8E0;
                case ReactionKind.Fume: return 0xC0E070;
                case ReactionKind.Fester: return 0x9AD050;
                case ReactionKind.Sear: return 0xFF9A50;
                default: return 0xFFFFFF;
            }
        }

        /// <summary>The blow's status meets what is on the creature. Changes the statuses and says what happened (the caller deals the bonus).</summary>
        /// <param name="frozen">Held in ice by the caller's own freeze, not only by <paramref name="on"/>.</param>
        public static Reaction Meet(EnemyStatuses on, StatusKind incoming, float power, bool frozen)
        {
            var reaction = new Reaction();
            frozen |= on.FreezeTime > 0;

            switch (incoming)
            {
                case StatusKind.Burn when frozen || on.ChillTime > 0:
                    reaction.Kind = ReactionKind.Steam;
                    reaction.Bonus = 0.6f;
                    reaction.Thawed = frozen;
                    on.ChillTime = 0;
                    on.FreezeTime = 0;
                    break;
                case StatusKind.Burn when on.PoisonTime > 0:
                    reaction.Kind = ReactionKind.Fume;
                    reaction.Bonus = 0.25f;
                    on.Poison *= 1.6f;
                    break;
                case StatusKind.Burn when on.BleedTime > 0:
                    reaction.Kind = ReactionKind.Sear;
                    reaction.Bonus = 0.45f;
                    on.BleedTime = 0;
                    on.Bleed = 0;
                    break;
                case StatusKind.Stun when frozen:
                    reaction.Kind = ReactionKind.Shatter;
                    reaction.Bonus = 0.9f;
                    reaction.Thawed = true;
                    on.FreezeTime = 0;
                    on.ChillTime = 0;
                    break;
                case StatusKind.Chill when on.BurnTime > 0:
                case StatusKind.Freeze when on.BurnTime > 0:
                    reaction.Kind = ReactionKind.Quench;
                    reaction.Bonus = 0.3f;
                    reaction.Hold = 0.8f;
                    on.BurnTime = 0;
                    on.Burn = 0;
                    break;
                case StatusKind.Bleed when on.PoisonTime > 0:
                case StatusKind.Poison when on.BleedTime > 0:
                    reaction.Kind = ReactionKind.Fester;
                    reaction.Bonus = 0.1f;
                    on.Poison = Math.Max(on.Poison, power) * 1.5f;
                    on.PoisonTime = Math.Max(on.PoisonTime, 3) + 3;
                    break;
            }

            return reaction;
        }
    }
}
