using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The status (O; main's UI-Status.dc.html and UI-Body.dc.html in one page, improved): the way a
    /// "system" shows one — cold blue panels with bright corners and a light scanning down them. At the
    /// left the hero: the body's level and title, health, stamina and the sea. In the middle the body:
    /// a figure whose parts glow by their wounds, and the wounds listed with what they weigh on and how
    /// long they will take. At the right the four traits: level, how far to the next, what each gives
    /// and what trains it.
    /// </summary>
    public partial class Ui
    {
        static readonly Color SystemEdge = UiKit.SystemBlue.A(0.45f);

        void StatusPage()
        {
            var session = S;
            var state = session.State;
            var body = Frame("身", "Статус", $"{Body.Title(session.BodyLevel)} · уровень тела {session.BodyLevel}", true, null, null, new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });

            // The hero.
            var left = SystemPanel(body, 0, 0.29f);
            UiKit.Lbl("Система · герой", left, 11, UiKit.SystemBlue).Size(16);
            UiKit.Disp($"Гу Юэ {state.HeroName}", left, 30, UiKit.Text).Size(38);
            UiKit.Label(Body.Title(session.BodyLevel).ToUpperInvariant(), left, 13, UiKit.Moon).Track(0.2f).Size(20);

            // The title won by deeds (what people call the hero), and how many there are.
            var titles = Titles.All(state);

            if (titles.Count > 0)
            {
                UiKit.Label($"Титул: «{titles[0]}»{(titles.Count > 1 ? $" · ещё {titles.Count - 1}" : "")}", left, 13, UiKit.GoldText).Size(20);
            }
            var level = UiKit.Rect("Level", left);
            level.Size(86);
            var number = UiKit.Disp($"{session.BodyLevel}", level, 72, UiKit.Moon, TextAnchor.MiddleLeft);
            number.Shadowed();
            number.rectTransform.Place(Vector2.zero, new Vector2(0.45f, 1));
            UiKit.Label("уровень тела\nиз 40", level, 13, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(new Vector2(0.45f, 0), Vector2.one);
            StatusBar(left, "Здоровье", state.Health, session.Stats.MaxHealth, UiKit.BarKind.Health);
            StatusBar(left, "Выносливость", session.Stamina, session.Stats.StaminaMax, UiKit.BarKind.Stamina);
            StatusBar(left, "Море эссенции", state.Essence, session.Stats.MaxEssence, UiKit.BarKind.Essence);
            Gap(left, 6);
            Fact(left, "Ступень", Cultivation.Name(state.Step));
            Fact(left, "Талант", $"{Cultivation.TalentName(state.Talent)} · {state.Talent * 100:0}%");
            Fact(left, "Путь", DifficultyRules.Name(state.Settings.Born));
            Fact(left, "Побед · смертей", $"{state.Kills} · {state.Deaths}");

            // The body.
            var middle = SystemPanel(body, 0.31f, 0.6f);
            UiKit.Lbl("Тело · раны", middle, 11, UiKit.SystemBlue).Size(16);
            var figure = UiKit.Rect("Figure", middle);
            figure.Size(330);
            Figure(figure, session);

            if (session.Wounds.Count == 0)
            {
                Para("Ран нет. Тяжёлые удары оставляют раны: на руках — слабее удар, на ногах — медленнее бег, на теле — меньше здоровья, на голове — эссенция течёт медленнее.", middle, 13, UiKit.Muted);
            }
            else
            {
                foreach (var wound in session.Wounds)
                {
                    var row = UiKit.Rect("Wound", middle);
                    row.Size(40);
                    var name = UiKit.Label($"{Capital(Body.Name(wound.Part))} — {Body.Name(wound.Kind, wound.Severity)}", row, 15, WoundColour(wound.Severity));
                    name.rectTransform.Place(new Vector2(0, 0.5f), Vector2.one);
                    UiKit.Label($"{Body.Weighs(wound.Part)} · заживёт через {Mathf.CeilToInt(wound.Hours)} ч", row, 12, UiKit.Muted).rectTransform.Place(Vector2.zero, new Vector2(1, 0.5f));
                }

                Para("Раны заживают с часами игры, во сне — быстрее. Пилюля заживления закрывает лёгкую рану сразу. Лунный фонарь закрывает ушибы и порезы.", middle, 12, UiKit.Muted);
            }

            // The traits.
            var right = SystemPanel(body, 0.62f, 1);
            UiKit.Lbl("Черты тела · растут от дела", right, 11, UiKit.SystemBlue).Size(16);

            foreach (var trait in Body.Traits)
            {
                var xp = session.TraitXpOf(trait);
                var at = Body.Level(xp);
                var row = UiKit.Rect(Body.Name(trait), right);
                row.Size(30);
                UiKit.Disp(Body.Name(trait), row, 22, UiKit.Text).rectTransform.Fill();
                UiKit.Disp($"{at}", row, 26, UiKit.Moon, TextAnchor.MiddleRight).rectTransform.Fill();
                var bar = UiKit.Rect("Bar", right);
                bar.Size(8);
                var share = at >= Body.MaxLevel ? 1 : (xp - Body.Needs[at]) / Mathf.Max(1, Body.Needs[at + 1] - Body.Needs[at]);
                UiKit.Bar(bar, UiKit.BarKind.System, Mathf.Clamp01(share), 4);
                UiKit.Label(at >= Body.MaxLevel ? $"Даёт: {Body.Gives(trait, at)} · предел" : $"Даёт: {Body.Gives(trait, at)} · до {at + 1}: {Mathf.CeilToInt(Body.Needs[at + 1] - xp)}",
                    right, 13, UiKit.Text2).Size(18);
                UiKit.Label($"Растёт: {Body.TrainedBy(trait)}", right, 12, UiKit.Muted).Size(18);
                Gap(right, 6);
            }
        }

        /// <summary>A "system" panel between two shares of the body's width: dark glass, a blue edge, bright corners, a light scanning down.</summary>
        static RectTransform SystemPanel(RectTransform body, float from, float to)
        {
            var panel = UiKit.Image("Panel", body, new Color(6 / 255f, 14 / 255f, 26 / 255f, 0.78f), UiKit.Plain);
            panel.rectTransform.Place(new Vector2(from, 0), new Vector2(to, 1));
            UiKit.Frame(panel.transform, SystemEdge.A(0.25f));
            UiKit.GoldCorners(panel.transform, 16, UiKit.Moon);
            var clip = UiKit.Rect("Clip", panel.transform);
            clip.Fill();
            clip.gameObject.AddComponent<RectMask2D>();
            var scan = UiKit.Image("Scan", clip, UiKit.SystemBlue.A(0.18f), UiKit.FadeBoth);
            scan.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -1, 0, -1);
            var move = scan.gameObject.AddComponent<Bob>();
            move.Period = 7;
            move.Rise = 330;
            var column = UiKit.Rect("Column", panel.transform);
            column.Fill();
            column.Stack<VerticalLayoutGroup>(6, new RectOffset(22, 22, 18, 18));

            return column;
        }

        static void StatusBar(RectTransform parent, string name, float value, float most, UiKit.BarKind kind)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(34);
            UiKit.Label(name, row, 12, UiKit.Muted).rectTransform.Place(new Vector2(0, 0.45f), Vector2.one);
            UiKit.Label($"{Mathf.RoundToInt(value)} / {Mathf.RoundToInt(most)}", row, 12, UiKit.Text2, TextAnchor.MiddleRight).rectTransform.Place(new Vector2(0, 0.45f), Vector2.one);
            var bar = UiKit.Rect("Bar", row);
            bar.Place(Vector2.zero, new Vector2(1, 0.4f));
            UiKit.Bar(bar, kind, Mathf.Clamp01(value / Mathf.Max(1, most)), 4);
        }

        static Color WoundColour(int severity) => severity >= 3 ? UiKit.Danger : severity == 2 ? UiKit.Hex(0xF0A070) : UiKit.Hex(0xF0C868);

        /// <summary>The body as a figure of six parts, each lit by its wound (cold blue whole, amber, orange, red).</summary>
        static void Figure(RectTransform box, Session session)
        {
            var middle = UiKit.Rect("Middle", box);
            middle.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 320));

            void Part(BodyPart part, Vector2 centre, Vector2 size, Sprite shape)
            {
                var wound = session.WoundOn(part);
                var colour = wound == null ? UiKit.Moon.A(0.22f) : WoundColour(wound.Severity).A(0.75f);
                var piece = UiKit.Image(part.ToString(), middle, colour, shape);
                piece.rectTransform.At(new Vector2(0.5f, 1), centre, size);

                if (wound != null)
                {
                    UiKit.Glow(piece.transform, WoundColour(wound.Severity).A(0.4f), 14);
                    var pulse = piece.gameObject.AddComponent<Pulse>();
                    pulse.Period = 2.4f - 0.5f * wound.Severity;
                    pulse.Low = 0.55f;
                    pulse.High = 1;
                }

                UiKit.Frame(piece.transform, UiKit.Moon.A(wound == null ? 0.35f : 0.6f));
            }

            Part(BodyPart.Head, new Vector2(0, -24), new Vector2(40, 44), UiKit.Disc);
            Part(BodyPart.Torso, new Vector2(0, -112), new Vector2(72, 118), UiKit.Rounded);
            Part(BodyPart.RightArm, new Vector2(-54, -112), new Vector2(22, 114), UiKit.Rounded);
            Part(BodyPart.LeftArm, new Vector2(54, -112), new Vector2(22, 114), UiKit.Rounded);
            Part(BodyPart.RightLeg, new Vector2(-18, -244), new Vector2(30, 140), UiKit.Rounded);
            Part(BodyPart.LeftLeg, new Vector2(18, -244), new Vector2(30, 140), UiKit.Rounded);
            UiKit.Label("правая", box, 11, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(new Vector2(0, 0), new Vector2(0.3f, 0.1f));
            UiKit.Label("левая", box, 11, UiKit.Muted, TextAnchor.MiddleRight).rectTransform.Place(new Vector2(0.7f, 0), new Vector2(1, 0.1f));
        }

        static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        // ------------------------------------------------------------------ The small figure on the HUD (ROADMAP 12.4)

        RectTransform hudFigure;
        readonly Image[] hudParts = new Image[6];

        static readonly (BodyPart part, Vector2 centre, Vector2 size)[] SmallBody =
        {
            (BodyPart.Head, new Vector2(0, -6), new Vector2(10, 11)),
            (BodyPart.Torso, new Vector2(0, -28), new Vector2(18, 30)),
            (BodyPart.RightArm, new Vector2(-14, -28), new Vector2(6, 29)),
            (BodyPart.LeftArm, new Vector2(14, -28), new Vector2(6, 29)),
            (BodyPart.RightLeg, new Vector2(-5, -61), new Vector2(8, 35)),
            (BodyPart.LeftLeg, new Vector2(5, -61), new Vector2(8, 35)),
        };

        /// <summary>A little body by the hero's panel, shown only while a wound is worse than light: which part, how bad.</summary>
        void BuildHudFigure(RectTransform stage)
        {
            hudFigure = UiKit.Rect("Wounds", stage);
            hudFigure.TopLeft(52, 32 + 222 + 14, 52, 84);

            for (var i = 0; i < SmallBody.Length; i++)
            {
                var (part, centre, size) = SmallBody[i];
                hudParts[i] = UiKit.Image(part.ToString(), hudFigure, UiKit.Moon.A(0.2f), part == BodyPart.Head ? UiKit.Disc : UiKit.Rounded);
                hudParts[i].rectTransform.At(new Vector2(0.5f, 1), centre, size);
            }

            var label = UiKit.Label("раны · O", hudFigure, 10, UiKit.Muted, TextAnchor.UpperCenter);
            label.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), -20, -16, -20, 2);
            hudFigure.gameObject.SetActive(false);
        }

        void TickHudFigure(Session session)
        {
            if (hudFigure == null)
            {
                return;
            }

            var worst = 0;

            foreach (var wound in session.Wounds)
            {
                worst = Mathf.Max(worst, wound.Severity);
            }

            hudFigure.gameObject.SetActive(worst >= 2 && !Game.Busy);

            if (worst < 2)
            {
                return;
            }

            for (var i = 0; i < SmallBody.Length; i++)
            {
                var wound = session.WoundOn(SmallBody[i].part);
                hudParts[i].color = wound == null ? UiKit.Moon.A(0.2f) : WoundColour(wound.Severity).A(0.85f);
            }
        }
    }
}
