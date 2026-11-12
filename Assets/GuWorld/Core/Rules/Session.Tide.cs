using System;

namespace GuWorld.Core
{
    /// <summary>
    /// The wolf tide: every seventh night the packs of the mountain come down on the Gu Yue town together.
    /// From the second rank on, on such a night (from nine till four) the tide is up — the townsfolk go
    /// in, the wolves gather at the old north gate — and a hero who stands there fights its waves. Holding
    /// the town pays in stones, insight and the clan's regard; a tide not met simply ebbs at dawn.
    /// </summary>
    public partial class Session
    {
        public const string Tide = "wolf_tide";
        public const int TideEvery = 7;

        /// <summary>Raised when the tide rises or ebbs (for the interface's horn and line).</summary>
        public event Action<bool> TideChanged;

        public bool TideUp => Has(Tide);

        /// <summary>Whether this is a tide's night and its hour (the tide is not up if it was held tonight already).</summary>
        public bool TideNight(float hour)
        {
            var night = hour >= 21 || hour < 4;
            // The night belongs to the day it began on.
            var day = hour < 4 ? State.Day - 1 : State.Day;

            return night && Rank >= 2 && day > 0 && day % TideEvery == 0 && !Has("tide_held_" + day) && !InSandbox;
        }

        /// <summary>Called with the clock: raises the tide or lets it ebb.</summary>
        void UpdateTide(float hour)
        {
            var should = TideNight(hour);

            if (should && !TideUp)
            {
                SetFlag(Tide);
                // The encounter may be fought again (each tide is its own).
                State.Flags.Remove(Tide + "_won");
                Say("Волчий прилив! Стаи спускаются к деревне — к северным воротам!");
                TideChanged?.Invoke(true);
            }
            else if (!should && TideUp)
            {
                State.Flags.Remove(Tide);
                TideChanged?.Invoke(false);
            }
        }

        /// <summary>The tide's last wave fell: the town was held.</summary>
        void TideHeld()
        {
            var day = State.Hour < 4 ? State.Day - 1 : State.Day;
            SetFlag("tide_held_" + day);
            State.Flags.Remove(Tide);
            State.Tides++;
            var stones = 60 + 40 * Rank;
            State.Stones += stones;
            GainInsight(80 * Rank);
            ChangeReputation("gu_yue", 8, false);
            GainMerit(40 + 20 * Rank);
            Say($"Прилив отбит. Клан благодарит: {stones} камней.");
            TideChanged?.Invoke(false);
            Achieve();
        }
    }
}
