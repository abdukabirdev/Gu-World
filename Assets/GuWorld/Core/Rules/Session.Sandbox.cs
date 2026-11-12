using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The immortal aperture: a place outside the world for trying Gu. Inside it the hero may take any Gu
    /// of the book on loan, at any quality, be set to any step, know every killer move, and have essence
    /// without end; nothing done there counts (no kills, no insight, no loot, no death). Leaving gives back
    /// everything as it was: the loaned Gu go, the step, the slots, the moves, health and essence return.
    /// </summary>
    public partial class Session
    {
        sealed class SandboxKeep
        {
            public int Step;
            public float Insight;
            public int[] Active;
            public int[] Passive;
            public List<string> Moves;
            public string Move;
            public float Health;
            public float Essence;
        }

        SandboxKeep sandbox;

        public bool InSandbox => sandbox != null;

        /// <summary>In the aperture: essence refills itself.</summary>
        public bool SandboxEssence { get; set; } = true;

        /// <summary>In the aperture: the hero cannot fall below one point of health.</summary>
        public bool SandboxUndying { get; set; } = true;

        public void EnterSandbox()
        {
            if (sandbox != null)
            {
                return;
            }

            sandbox = new SandboxKeep
            {
                Step = State.Step,
                Insight = State.Insight,
                Active = (int[])State.Active.Clone(),
                Passive = (int[])State.Passive.Clone(),
                Moves = new List<string>(State.Moves),
                Move = State.Move,
                Health = State.Health,
                Essence = State.Essence,
            };

            Say("Апертура бессмертного: здесь ничего не считается");
            Touch();
        }

        public void LeaveSandbox()
        {
            if (sandbox == null)
            {
                return;
            }

            var keep = sandbox;
            sandbox = null;
            DropBorrowed();
            State.Step = keep.Step;
            State.Insight = keep.Insight;
            State.Active = keep.Active;
            State.Passive = keep.Passive;
            State.Moves = keep.Moves;
            State.Move = keep.Move;
            cooldowns.Clear();
            moveCooldowns.Clear();
            statuses.Clear();
            Recompute();
            State.Health = Math.Min(Stats.MaxHealth, Math.Max(1, keep.Health));
            State.Essence = Math.Min(Stats.MaxEssence, keep.Essence);
            Touch();
        }

        /// <summary>Lends a Gu of the book at a quality (refined, ready), and puts it in a free slot of its kind.</summary>
        public GuInstance SandboxGive(string id, int quality)
        {
            if (sandbox == null || GuBook.Get(id) == null)
            {
                return null;
            }

            var gu = MakeGu(id, Math.Max(0, Math.Min(2, quality)), true);
            gu.Borrowed = true;
            gu.Quality = Math.Max(0, Math.Min(GuBook.MaxQuality, quality));
            AddGu(gu, true);

            if (GuBook.Get(id).Rank <= Rank)
            {
                EquipAuto(gu.Uid);
            }

            return gu;
        }

        /// <summary>Sets the hero's step in the aperture (the rank, the slots and the aperture's size follow).</summary>
        public void SandboxStep(int step)
        {
            if (sandbox == null)
            {
                return;
            }

            State.Step = Math.Max(0, Math.Min(Cultivation.MaxStep, step));
            Recompute();
            Restore();

            // Gu above the new rank cannot stay in their slots.
            for (var i = 0; i < State.Active.Length; i++)
            {
                var gu = FindGu(State.Active[i]);

                if (gu != null && GuBook.Get(gu.Def).Rank > Rank || i >= Stats.ActiveSlots)
                {
                    State.Active[i] = 0;
                }
            }

            for (var i = 0; i < State.Passive.Length; i++)
            {
                var gu = FindGu(State.Passive[i]);

                if (gu != null && GuBook.Get(gu.Def).Rank > Rank || i >= Stats.PassiveSlots)
                {
                    State.Passive[i] = 0;
                }
            }

            Recompute();
            Touch();
        }

        /// <summary>Knows every killer move while in the aperture.</summary>
        public void SandboxAllMoves()
        {
            if (sandbox == null)
            {
                return;
            }

            foreach (var move in KillerMoves.All)
            {
                if (!State.Moves.Contains(move.Id))
                {
                    State.Moves.Add(move.Id);
                }
            }

            Touch();
        }

        /// <summary>Lends every Gu a move needs and slots them, so it can be cast at once.</summary>
        public bool SandboxTryMove(string id)
        {
            var move = KillerMoves.Get(id);

            if (sandbox == null || move == null)
            {
                return false;
            }

            if (Rank < move.Rank)
            {
                SandboxStep((move.Rank - 1) * 4);
            }

            Array.Clear(State.Active, 0, State.Active.Length);
            Array.Clear(State.Passive, 0, State.Passive.Length);
            Recompute();

            foreach (var gu in KillerMoves.GuOf(move))
            {
                var owned = State.Gu.Find(g => g.Def == gu && g.Refined) ?? SandboxGive(gu, 1);

                if (owned != null && !IsSlotted(owned.Uid))
                {
                    EquipAuto(owned.Uid);
                }
            }

            if (!State.Moves.Contains(id))
            {
                State.Moves.Add(id);
            }

            State.Move = id;
            cooldowns.Clear();
            moveCooldowns.Clear();
            Restore();
            Touch();

            return MissingFor(move).Count == 0;
        }

        /// <summary>Clears the cooldowns of everything (a button in the aperture).</summary>
        public void SandboxReady()
        {
            if (sandbox != null)
            {
                cooldowns.Clear();
                moveCooldowns.Clear();
                Restore();
            }
        }

        /// <summary>Takes away every loaned Gu (also on loading a save, should the game have stopped in the aperture).</summary>
        void DropBorrowed()
        {
            var borrowed = State.Gu.FindAll(g => g.Borrowed);

            foreach (var gu in borrowed)
            {
                Unslot(gu.Uid);
                State.Gu.Remove(gu);
            }
        }

        void TickSandbox()
        {
            if (sandbox != null && SandboxEssence)
            {
                State.Essence = Stats.MaxEssence;
            }
        }
    }
}
