namespace GuWorld.Core
{
    /// <summary>Bouts with the academy's partners: three a day, practice (no falling), merit for each won.</summary>
    public partial class Session
    {
        /// <summary>A bout is on (the hero is held above a tenth of their health).</summary>
        public bool Sparring { get; private set; }

        public int BoutsLeft => State.SparDay == State.Day ? Core.Sparring.BoutsADay - State.SparsToday : Core.Sparring.BoutsADay;

        public string WhyNoBout() => BoutsLeft <= 0 ? "На сегодня бои окончены. Приходи завтра." : InSandbox ? "Не в апертуре" : null;

        /// <summary>Starts a bout: the partner for the hero's rank.</summary>
        public EnemyDef BeginBout()
        {
            if (WhyNoBout() != null)
            {
                return null;
            }

            if (State.SparDay != State.Day)
            {
                State.SparDay = State.Day;
                State.SparsToday = 0;
            }

            State.SparsToday++;
            Sparring = true;
            Touch();

            return Core.Sparring.For(Rank);
        }

        /// <summary>The bout ended: won (the partner fell) or not.</summary>
        public void EndBout(bool won)
        {
            if (!Sparring)
            {
                return;
            }

            Sparring = false;

            if (won)
            {
                State.SparsWon++;
                GainMerit(12 + 6 * Rank);
                Say("Наставник кивает: «Неплохо». Заслуги перед кланом растут.");
                Achieve();
            }
            else
            {
                Say("Наставник: «Ещё рано. Отдохни и приходи».");
            }

            Touch();
        }
    }
}
