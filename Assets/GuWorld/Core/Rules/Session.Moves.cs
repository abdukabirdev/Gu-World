using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>Killer moves: knowing them, seeing whether the Gu for one are slotted, comprehending one, and casting it.</summary>
    public partial class Session
    {
        /// <summary>The cooldowns of the moves themselves (each also puts its core Gu on its own).</summary>
        readonly Dictionary<string, float> moveCooldowns = new Dictionary<string, float>();

        /// <summary>A killer move was comprehended or taught (for the interface's sound and line).</summary>
        public event Action<KillerMoveDef> MoveLearnt;

        public bool KnowsMove(string id) => State.Moves.Contains(id);

        public void LearnMove(string id)
        {
            var move = KillerMoves.Get(id);

            if (move == null || KnowsMove(id))
            {
                return;
            }

            State.Moves.Add(id);

            if (string.IsNullOrEmpty(State.Move))
            {
                State.Move = id;
            }

            Say($"Убийственный приём: {move.Name}");
            MoveLearnt?.Invoke(move);
            Achieve();
            Touch();
        }

        /// <summary>The Gu slotted now (active and passive), by their kind.</summary>
        HashSet<string> SlottedKinds()
        {
            var kinds = new HashSet<string>();

            for (var i = 0; i < State.Active.Length && i < Stats.ActiveSlots; i++)
            {
                var gu = FindGu(State.Active[i]);

                if (gu != null && gu.Refined)
                {
                    kinds.Add(gu.Def);
                }
            }

            for (var i = 0; i < State.Passive.Length && i < Stats.PassiveSlots; i++)
            {
                var gu = FindGu(State.Passive[i]);

                if (gu != null && gu.Refined)
                {
                    kinds.Add(gu.Def);
                }
            }

            return kinds;
        }

        /// <summary>The slot of the move's core Gu, or -1 if it is not in an active slot.</summary>
        public int CoreSlot(KillerMoveDef move)
        {
            for (var i = 0; i < State.Active.Length && i < Stats.ActiveSlots; i++)
            {
                var gu = FindGu(State.Active[i]);

                if (gu != null && gu.Refined && gu.Def == move.Core)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The Gu a move lacks in the slots now (by name), empty if it has them all.</summary>
        public List<string> MissingFor(KillerMoveDef move)
        {
            var missing = new List<string>();
            var kinds = SlottedKinds();

            if (CoreSlot(move) < 0)
            {
                missing.Add(GuBook.Get(move.Core)?.Name ?? move.Core);
            }

            foreach (var support in move.Supports)
            {
                if (!kinds.Contains(support))
                {
                    missing.Add(GuBook.Get(support)?.Name ?? support);
                }
            }

            return missing;
        }

        /// <summary>Whether the hero owns (refined) every Gu a move needs, slotted or not.</summary>
        public bool OwnsGuFor(KillerMoveDef move)
        {
            foreach (var id in KillerMoves.GuOf(move))
            {
                if (!State.Gu.Exists(g => g.Def == id && g.Refined))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Why a move cannot be comprehended now, or null if it can.</summary>
        public string WhyNoDevise(KillerMoveDef move)
        {
            if (move == null)
            {
                return "Такого приёма нет";
            }

            if (KnowsMove(move.Id))
            {
                return "Приём уже постигнут";
            }

            if (move.Insight <= 0)
            {
                return "Этот приём можно только перенять";
            }

            if (Rank < move.Rank)
            {
                return $"Нужен {move.Rank}-й ранг";
            }

            var missing = MissingFor(move);

            if (missing.Count > 0)
            {
                return "Вставьте в апертуру: " + string.Join(", ", missing);
            }

            return State.Insight < DeviseCost(move) ? $"Нужно {DeviseCost(move)} прозрения" : null;
        }

        /// <summary>Comprehends a move: the Gu are together in the aperture, insight is spent, the move is known.</summary>
        public bool Devise(string id)
        {
            var move = KillerMoves.Get(id);

            if (WhyNoDevise(move) != null)
            {
                return false;
            }

            State.Insight -= DeviseCost(move);
            GainAttainment(GuBook.Get(move.Core).Path, 60 * move.Rank);
            LearnMove(id);

            return true;
        }

        /// <summary>The moves the hero could come to: known ones first, then those whose Gu they own, then the rest of their rank.</summary>
        public List<KillerMoveDef> MovesInSight()
        {
            var known = new List<KillerMoveDef>();
            var owned = new List<KillerMoveDef>();
            var rest = new List<KillerMoveDef>();

            foreach (var move in KillerMoves.All)
            {
                if (KnowsMove(move.Id))
                {
                    known.Add(move);
                }
                else if (OwnsGuFor(move))
                {
                    owned.Add(move);
                }
                else if (move.Rank <= Rank && (State.Gu.Exists(g => g.Def == move.Core) || HasAnyOf(move.Supports)))
                {
                    rest.Add(move);
                }
            }

            known.AddRange(owned);
            known.AddRange(rest);

            return known;
        }

        bool HasAnyOf(string[] ids)
        {
            foreach (var id in ids)
            {
                if (State.Gu.Exists(g => g.Def == id))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The known moves whose Gu are all slotted now.</summary>
        public List<KillerMoveDef> ReadyMoves()
        {
            var ready = new List<KillerMoveDef>();

            foreach (var id in State.Moves)
            {
                var move = KillerMoves.Get(id);

                if (move != null && MissingFor(move).Count == 0 && Rank >= move.Rank)
                {
                    ready.Add(move);
                }
            }

            return ready;
        }

        /// <summary>The move on V: the one chosen if it is ready, else the first ready one.</summary>
        public KillerMoveDef SelectedMove()
        {
            var ready = ReadyMoves();

            foreach (var move in ready)
            {
                if (move.Id == State.Move)
                {
                    return move;
                }
            }

            return ready.Count > 0 ? ready[0] : null;
        }

        public void SelectMove(string id)
        {
            if (KnowsMove(id))
            {
                State.Move = id;
                Touch();
            }
        }

        /// <summary>The next ready move on V (Shift + V turns through them).</summary>
        public KillerMoveDef NextMove()
        {
            var ready = ReadyMoves();

            if (ready.Count == 0)
            {
                return null;
            }

            var at = ready.FindIndex(m => m.Id == State.Move);
            var next = ready[(at + 1) % ready.Count];
            State.Move = next.Id;
            Touch();

            return next;
        }

        public float MoveCooldownLeft(KillerMoveDef move) => move != null && moveCooldowns.TryGetValue(move.Id, out var left) ? left : 0;

        public float MoveCooldownOf(KillerMoveDef move)
        {
            var slot = move == null ? -1 : CoreSlot(move);
            var gu = slot >= 0 ? ActiveGu(slot) : null;

            return move == null ? 0 : move.Skill.Cooldown * Stats.Cooldown * (gu == null ? 1 : 1 - AffixOf(gu, AffixKind.Quick));
        }

        /// <summary>What a move costs in essence: its own cost, made cheaper by the hero and the core Gu, dearer if a Gu of it starves.</summary>
        public float MoveCostOf(KillerMoveDef move)
        {
            var slot = CoreSlot(move);
            var gu = slot >= 0 ? ActiveGu(slot) : null;
            var cost = move.Skill.Cost * Stats.Cost;

            if (gu != null)
            {
                cost *= AttainmentThrift(GuBook.Get(gu.Def).Path);
                cost *= 1 - AffixOf(gu, AffixKind.Thrifty);
                cost *= IsStarving(gu) ? 1.3f : 1;
            }

            return cost;
        }

        /// <summary>Why a move cannot be cast now, or null if it can.</summary>
        public string WhyNoMove(KillerMoveDef move)
        {
            if (move == null)
            {
                return "Нет готового приёма";
            }

            if (!KnowsMove(move.Id))
            {
                return "Приём не постигнут";
            }

            if (Rank < move.Rank)
            {
                return $"Нужен {move.Rank}-й ранг";
            }

            var missing = MissingFor(move);

            if (missing.Count > 0)
            {
                return "Не хватает: " + string.Join(", ", missing);
            }

            if (moveCooldowns.ContainsKey(move.Id))
            {
                return "Приём ещё не готов";
            }

            if (Held)
            {
                return "Вы не можете пошевелиться";
            }

            return State.Essence < MoveCostOf(move) ? "Не хватает эссенции" : null;
        }

        /// <summary>
        /// Casts a move through its core Gu: takes the essence, starts the move's cooldown and the core's,
        /// and gives the cast with the move's skill and the power the core Gu would lend it (its mastery,
        /// quality, affixes, path and hunger), raised a little for every supporting Gu's mastery.
        /// </summary>
        public Cast CastMove(KillerMoveDef move)
        {
            if (WhyNoMove(move) != null)
            {
                return null;
            }

            var slot = CoreSlot(move);
            var gu = ActiveGu(slot);
            var def = GuBook.Get(gu.Def);
            var skill = move.Skill;
            State.Essence -= MoveCostOf(move);
            var cooldown = MoveCooldownOf(move);
            moveCooldowns[move.Id] = cooldown;
            cooldowns[gu.Uid] = Math.Max(cooldowns.TryGetValue(gu.Uid, out var left) ? left : 0, Math.Min(cooldown, CooldownOf(gu) * 1.5f));

            var power = PowerOf(gu, skill.Power) * SupportFactor(move);
            var cast = new Cast
            {
                Gu = gu, Def = def, Skill = skill, Power = power, Move = move,
                Range = skill.Range * Stats.Range * (1 + AffixOf(gu, AffixKind.Far)),
                Leech = skill.Leech + Stats.Leech + AffixOf(gu, AffixKind.Leech)
            };

            if (skill.Shape == SkillShape.Buff)
            {
                ApplyBuff(skill.Buff, skill.BuffValue, skill.Duration, skill.Radius, skill.Status, skill.StatusPower);
            }
            else if (skill.Shape == SkillShape.Heal)
            {
                healLeft += cast.Power;
                healTime = Math.Max(0.1f, skill.Duration);
            }
            else
            {
                cast.Power *= BreakStealth();
            }

            // Every Gu of the move learns from it.
            foreach (var id in KillerMoves.GuOf(move))
            {
                var part = State.Gu.Find(g => g.Def == id && g.Refined);

                if (part != null)
                {
                    GainMastery(part, 2);
                }
            }

            State.MovesCast++;

            if (State.MovesCast % 10 == 0)
            {
                Achieve();
            }

            Touch();

            return cast;
        }

        /// <summary>Each supporting Gu adds a share of power by its mastery (a master's Gu work together better).</summary>
        float SupportFactor(KillerMoveDef move)
        {
            var factor = 1f;

            foreach (var id in move.Supports)
            {
                var part = State.Gu.Find(g => g.Def == id && g.Refined);

                if (part != null)
                {
                    factor += 0.04f * Cultivation.MasteryLevel(part.MasteryXp) + 0.03f * part.Quality;
                }
            }

            return factor;
        }

        void TickMoves(float dt)
        {
            if (moveCooldowns.Count == 0)
            {
                return;
            }

            var keys = new List<string>(moveCooldowns.Keys);

            foreach (var key in keys)
            {
                var left = moveCooldowns[key] - dt;

                if (left <= 0)
                {
                    moveCooldowns.Remove(key);
                }
                else
                {
                    moveCooldowns[key] = left;
                }
            }
        }
    }
}
