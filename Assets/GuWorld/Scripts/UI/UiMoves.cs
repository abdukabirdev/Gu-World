using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The page of killer moves (U): the moves known and those in sight, the Gu each needs and whether
    /// they sit in the aperture now, comprehension for insight, and the one put on V.
    /// </summary>
    public partial class Ui
    {
        string pickedMove;

        void MovesPage()
        {
            var session = S;
            var state = session.State;
            var ready = session.ReadyMoves();
            var body = Frame("招", "Убийственные приёмы", $"Постигнуто {state.Moves.Count} из {KillerMoves.All.Length} · готово сейчас {ready.Count}", true, null, null,
                new[] { ("V", "Приём"), ("X", "Следующий"), ("Q E", "Страницы"), ("Esc", "Закрыть") });

            var left = Column(body, 0, 0.42f, 6);
            UiKit.Lbl($"Прозрение · {(int)state.Insight}", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));
            var moves = session.MovesInSight();

            if (!moves.Exists(m => m.Id == pickedMove))
            {
                pickedMove = moves.Count > 0 ? moves[0].Id : null;
            }

            var selected = session.SelectedMove();

            foreach (var move in moves)
            {
                var id = move.Id;
                var known = session.KnowsMove(id);
                var isReady = ready.Contains(move);
                var right = selected == move ? "на V" : isReady ? "готов" : known ? "постигнут" : session.WhyNoDevise(move) == null ? "можно постичь" : $"ранг {GuBook.RankNumerals[move.Rank]}";
                var colour = known ? UiKit.Text : UiKit.Muted;
                ListLine(list, move.Seal, move.Name, right, colour, id == pickedMove, () => { pickedMove = id; Redraw(); },
                    selected == move ? UiKit.GoldText : isReady ? UiKit.Essence : UiKit.Text2);
            }

            if (moves.Count == 0)
            {
                Para("Убийственный приём — это несколько Гу, разбуженных разом ради одного удара. Соберите в апертуре Гу, что подходят друг другу, и постигайте приёмы здесь.", list, 15, UiKit.Muted);
            }

            var column = Column(body, 0.48f, 1, 6);
            var picked = KillerMoves.Get(pickedMove);

            if (picked != null)
            {
                MoveDetail(column, picked);
            }
        }

        void MoveDetail(RectTransform column, KillerMoveDef move)
        {
            var session = S;
            var known = session.KnowsMove(move.Id);
            var head = UiKit.Rect("Head", column);
            head.Size(84);
            var seal = UiKit.Zh(move.Seal, head, 64, UiKit.Hex(move.Colour), TextAnchor.MiddleLeft);
            seal.rectTransform.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -80, 0);
            var name = UiKit.Disp(move.Name, head, 26, known ? UiKit.Text : UiKit.Text2);
            name.rectTransform.Place(Vector2.zero, Vector2.one, 92, 34, 0, 8);
            var kind = UiKit.Lbl($"Ранг {GuBook.RankNumerals[move.Rank]} · {ShapeName(move.Skill.Shape)}", head, 11, UiKit.LabelGold, 0.2f);
            kind.rectTransform.Place(Vector2.zero, Vector2.one, 92, 12, 0, 54);
            UiKit.Rule(column);
            Para(move.Description, column, 15);
            Gap(column, 4);

            // Its Gu: each with whether it is in the aperture now.
            UiKit.Lbl("Гу приёма", column, 11, UiKit.LabelGold).Size(18);
            var missing = new HashSet<string>(session.MissingFor(move));

            foreach (var id in KillerMoves.GuOf(move))
            {
                var def = GuBook.Get(id);
                var owned = session.State.Gu.Exists(g => g.Def == id && g.Refined);
                var here = !missing.Contains(def?.Name ?? id);
                var what = id == move.Core ? "ядро · в активный слот" : def != null && def.Kind == GuKind.Passive ? "в пассивный слот" : "в любой слот";
                Fact(column, (def?.Seal ?? "蛊") + "  " + (def?.Name ?? id), here ? "в апертуре" : owned ? what : "нет", here ? UiKit.Essence : owned ? UiKit.Hunger : UiKit.Danger);
            }

            Gap(column, 6);
            var skill = move.Skill;
            var slot = session.CoreSlot(move);
            var core = slot >= 0 ? session.ActiveGu(slot) : null;

            if (skill.Power > 0)
            {
                var power = core != null ? $"{session.PowerOf(core, skill.Power):0}" : $"{skill.Power:0}";
                Fact(column, "Сила", power + (skill.Count > 1 && skill.Shape == SkillShape.Fan ? $" × {skill.Count}" : skill.Shape == SkillShape.Storm ? $" · {skill.Count} ударов" : skill.Shape == SkillShape.Chain ? $" · {skill.Count} прыжков" : ""), UiKit.GoldText);
            }

            Fact(column, "Расход эссенции", $"{session.MoveCostOf(move):0}", UiKit.Essence);
            Fact(column, "Перезарядка", $"{session.MoveCooldownOf(move):0.0} с");

            if (skill.Range > 0)
            {
                Fact(column, "Дальность", $"{skill.Range * session.Stats.Range:0} м");
            }

            if (skill.Status != StatusKind.None)
            {
                Fact(column, "Оставляет", $"{Session.StatusName(skill.Status)} · {skill.StatusTime:0.#} с", UiKit.Danger);
            }

            Gap(column, 12);

            // In the immortal aperture any move can be tried at once.
            if (session.InSandbox)
            {
                var trial = move.Id;
                UiKit.HexButton("Испытать: одолжить Гу и вставить", column, () => { session.SandboxTryMove(trial); Redraw(); }, UiKit.HexKind.Main, 12, 42);
            }

            if (known)
            {
                var why = session.WhyNoMove(move);

                if (session.SelectedMove() == move)
                {
                    Para("Приём на клавише V.", column, 14, UiKit.GoldText);
                }
                else
                {
                    var id = move.Id;
                    UiKit.HexButton("Поставить на V", column, () => { session.SelectMove(id); Redraw(); }, why == null || why == "Приём ещё не готов" || why == "Не хватает эссенции" ? UiKit.HexKind.Main : UiKit.HexKind.Ghost, 13, 46);
                }

                if (why != null && why != "Приём ещё не готов")
                {
                    Para(why, column, 14, UiKit.Danger);
                }

                Fact(column, "Применён раз", $"{session.State.MovesCast}");

                return;
            }

            var blocked = session.WhyNoDevise(move);
            var moveId = move.Id;
            UiKit.HexButton($"Постичь · {session.DeviseCost(move)} прозрения", column, () =>
            {
                if (session.Devise(moveId))
                {
                    session.SelectMove(moveId);
                }

                Redraw();
            }, blocked == null ? UiKit.HexKind.Main : UiKit.HexKind.Off, 13, 52);

            if (blocked != null)
            {
                Para(blocked, column, 14, UiKit.Danger);
            }
            else
            {
                Para("Прозрение уйдёт на приём, а не на ступень — но приём останется с вами навсегда.", column, 13, UiKit.Muted);
            }
        }

        static string ShapeName(SkillShape shape)
        {
            switch (shape)
            {
                case SkillShape.Projectile: return "снаряд";
                case SkillShape.Fan: return "веер";
                case SkillShape.Cone: return "конус";
                case SkillShape.Nova: return "волна вокруг";
                case SkillShape.Dash: return "рывок";
                case SkillShape.Blink: return "прыжок";
                case SkillShape.Buff: return "усиление";
                case SkillShape.Heal: return "лечение";
                case SkillShape.Pull: return "притяжение";
                case SkillShape.Storm: return "буря";
                case SkillShape.Chain: return "цепь";
                default: return "приём";
            }
        }
    }
}
