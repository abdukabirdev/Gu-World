using System;
using GuWorld.Cultivation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The pause (UI-Pause.dc.html) and the conversation (UI-Dialogue.dc.html). The pause:
    /// the world dimmed to the void under slow gold rings and the brush mark 靜 (stillness),
    /// "ПАУЗА" in the middle with its menu, the hero at the left (rank, health, essence, how
    /// far to the next realm, the hour), the task in hand at the right. The conversation: the
    /// world goes on behind black bars, darkened from the foot; who speaks and their words at
    /// the left, the answers at the right with their keys 1–4.
    /// </summary>
    public partial class Menus
    {
        void Pause()
        {
            var stage = UiKit.Stage(pageRoot);
            var dim = UiKit.Image("Dim", stage, Color.white, UiKit.Plain);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
            dim.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.72f), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.86f));
            UiKit.Cloud(stage, new Vector2(0.3f, 0.6f), new Vector2(1400, 1000), UiKit.Hex(0x2A3A50, 0.35f));
            UiKit.Cloud(stage, new Vector2(0.7f, 0.4f), new Vector2(1400, 1000), UiKit.Hex(0x3A2A20, 0.3f));
            var vignette = UiKit.Image("Vignette", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.9f), UiKit.Vignette);
            vignette.rectTransform.Fill();
            var rings = UiKit.Rect("Rings", stage);
            rings.At(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(10, 10));
            UiKit.Orbit(rings, 760, UiKit.Gold.A(0.1f), 200, 0);
            UiKit.Orbit(rings, 600, UiKit.Gold.A(0.1f), -160, 4);
            var brush = UiKit.Brush("靜", stage, 520, UiKit.Gold.A(0.04f));
            brush.rectTransform.anchorMin = brush.rectTransform.anchorMax = new Vector2(0.5f, 1);
            brush.rectTransform.anchoredPosition = new Vector2(0, -70 - 260);

            // The title and the menu.
            var middle = UiKit.Rect("Middle", stage);
            middle.anchorMin = middle.anchorMax = new Vector2(0.5f, 1);
            middle.pivot = new Vector2(0.5f, 1);
            middle.anchoredPosition = new Vector2(0, -130);
            middle.sizeDelta = new Vector2(460, 640);
            UiKit.Lbl("Время остановлено", middle, 11, UiKit.LabelGold, 0.34f, TextAnchor.MiddleCenter).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -16, 0, 0);
            var title = UiKit.Disp("ПАУЗА", middle, 72, UiKit.Text, TextAnchor.MiddleCenter);
            title.Track(0.2f);
            title.rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -100, 0, 24);
            UiKit.Image("Rule", middle, UiKit.Gold, UiKit.FadeBoth).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), -100, -121, -100, 120);

            var menu = UiKit.Rect("Menu", middle);
            menu.Place(new Vector2(0, 1), Vector2.one, 0, -600, 0, 154);
            menu.Stack<VerticalLayoutGroup>(4, null, TextAnchor.UpperCenter);
            var keys = menu.gameObject.AddComponent<MenuKeys>();
            keys.Add(UiKit.Entry("Продолжить", null, menu, host.Resume).Restyle(28, 62).Centre());

            if (host.Sandbox)
            {
                keys.Add(UiKit.Entry("Режимы и все Гу", null, menu, () => host.OpenPage(Page.Sandbox)).Restyle(24, 54).Centre());
            }

            keys.Add(UiKit.Entry("Настройки", null, menu, () => OpenSettings(Page.Pause)).Restyle(24, 54).Centre());
            keys.Add(UiKit.Entry(host.Sandbox ? "Покинуть апертуру" : "Главное меню", null, menu, () => FadeThrough(host.BackToTitle)).Restyle(24, 54).Centre());

            if (!host.Sandbox)
            {
                var again = UiKit.Entry("Начать заново", null, menu, () =>
                    Ask("Начать заново?", "Сохранение будет удалено. Игра начнётся с выбора пути и создания героя. Настройки останутся.",
                        "Начать заново", host.ResetGame, Page.Pause)).Restyle(24, 54).Centre();
                again.Tint(UiKit.Hex(0xC9A69E));
                keys.Add(again);
            }

            UiKit.Rect("Gap", menu).Size(6);
            keys.Add(UiKit.Entry("Выйти из игры", null, menu, host.Quit).Quiet(50).Centre());

            // The hero.
            var gu = host.Cultivator;
            var a = gu.Aperture;
            var hero = UiKit.Rect("Hero", stage);
            hero.TopLeft(72, 300, 300, 260);
            hero.Stack<VerticalLayoutGroup>(12);
            UiKit.Lbl("Герой", hero).Size(14);
            UiKit.Disp(host.Look.FullName, hero, 30).Size(32);
            UiKit.Label($"Ранг {Aperture.RankNumeral(a.Rank)} · {Aperture.Name(a.Stage)} · {a.TalentName}", hero, 13, UiKit.Hex(0xA8B6B8)).Size(36);
            var health = host.Health;
            PauseBar(hero, "Здоровье", health.Current / health.Max, UiKit.Danger);
            PauseBar(hero, "Эссенция", a.Essence / 100, UiKit.Essence);
            PauseBar(hero, "Прорыв", a.Progress, UiKit.GoldLit);
            UiKit.Label(host.TimeLine, hero, 12, UiKit.Muted).Size(18);

            // The task in hand.
            var now = UiKit.Rect("Now", stage);
            now.TopRight(72, 300, 300, 220);
            now.Stack<VerticalLayoutGroup>(12, null, TextAnchor.UpperRight);
            UiKit.Lbl("Сейчас", now, 11, UiKit.LabelGold, 0.34f, TextAnchor.MiddleRight).Size(14);
            var quests = host.Quests;
            var current = host.Sandbox ? null : quests.Current;
            var side = host.Sandbox || quests.Sides.Count == 0 ? null : quests.Sides[0];
            var (taskTitle, taskLine) = current != null
                ? (current.Title, $"Задание академии · {(quests.Ready ? "готово" : $"{quests.Progress} / {current.Target}")}")
                : side != null ? (side.Title, $"Поручение · {(quests.IsReady(side) ? "готово" : $"{quests.ProgressOf(side)} / {side.Target}")}")
                : host.Sandbox ? ("Апертура бессмертного", "Полигон: ничего не сохраняется") : ("Заданий нет", "Загляните в академию");
            UiKit.Disp(taskTitle, now, 24, UiKit.Text, TextAnchor.MiddleRight).Size(54);
            UiKit.Label(taskLine, now, 13, UiKit.Hex(0xA8B6B8), TextAnchor.MiddleRight).Size(18);
            var rule = UiKit.Rect("Rule", now);
            rule.Size(12);
            var line = UiKit.Image("Line", rule, UiKit.Gold.A(0.4f), UiKit.SoftEdge);
            line.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), -120, 0, 0, -1);
            line.rectTransform.localScale = new Vector3(-1, 1, 1);
            UiKit.Label(host.Sandbox ? "Без сохранения" : "Игра сохраняется сама каждые 30 с", now, 12, UiKit.Muted, TextAnchor.MiddleRight).Size(18);

            var foot = UiKit.Footer(stage, new[] { ("↑↓", "Выбор"), ("Enter", "Открыть"), ("Esc", "Вернуться в игру") });
            foot.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        }

        /// <summary>A line of the pause's hero: a name and a thin bar, filled to a share, in a colour.</summary>
        static void PauseBar(Transform parent, string name, float share, Color colour)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(14);
            UiKit.Label(name, row, 12, UiKit.Muted).rectTransform.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -100, 0);
            var track = UiKit.Image("Track", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), UiKit.Plain);
            track.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 112, -1.5f, 0, -1.5f);
            var fill = UiKit.Image("Fill", track.transform, colour, UiKit.Plain);
            fill.rectTransform.Place(Vector2.zero, new Vector2(Mathf.Clamp01(share), 1));
        }

        /// <summary>The conversation: black bars, the world darkened from the foot; the words at the left, the answers at the right.</summary>
        void Dialogue()
        {
            var stage = UiKit.Stage(pageRoot);
            var shade = UiKit.Image("Shade", stage, Color.white, UiKit.Plain);
            shade.rectTransform.Fill();
            shade.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.98f), false, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.12f), 0.42f);
            var vignette = UiKit.Image("Vignette", stage, new Color(0, 0, 0, 0.6f), UiKit.Vignette);
            vignette.rectTransform.Fill();
            UiKit.Image("Top", stage, Color.black, UiKit.Plain).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -44, 0, 0);
            UiKit.Image("Foot", stage, Color.black, UiKit.Plain).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -44);

            // Who speaks and what they say.
            var words = UiKit.Rect("Words", stage);
            words.TopLeft(96, 560, 680, 290);
            words.Stack<VerticalLayoutGroup>(14);
            var who = UiKit.Rect("Who", words);
            var whoRow = who.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft);
            whoRow.childForceExpandWidth = false;
            who.Size(36);
            UiKit.Image("Line", who, UiKit.Gold, UiKit.SoftEdge).rectTransform.localScale = new Vector3(-1, 1, 1);
            who.GetChild(0).Size(1, 40, 0);
            UiKit.Disp(speaker ?? "", who, 32).Size(36);
            UiKit.Lbl(speakerTitle ?? "", who, 11, UiKit.Muted, 0.3f).Size(36);
            var said = UiKit.Disp($"«{spoken}»", words, 28, UiKit.Hex(0xF2EADB), TextAnchor.UpperLeft);
            said.lineSpacing = 1.05f;
            said.horizontalOverflow = HorizontalWrapMode.Wrap;
            said.Size(-1).flexibleHeight = 1;

            // The answers.
            var replies = UiKit.Rect("Answers", stage);
            replies.TopRight(96, 560, 480, 300);
            replies.Stack<VerticalLayoutGroup>(4);
            var head = UiKit.Lbl("Ответ", replies);
            head.Size(22);
            head.rectTransform.GetComponent<LayoutElement>().minWidth = 0;
            var pageKeys = stage.gameObject.AddComponent<PageKeys>();
            var digits = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };

            for (var i = 0; i < answers.Count; i++)
            {
                var (text, act) = answers[i];
                var last = i == answers.Count - 1 && answers.Count > 1;
                var (row, click) = UiKit.ListRow(replies, 54, false, act);
                var line = row.Stack<HorizontalLayoutGroup>(14, new RectOffset(26, 18, 0, 0), TextAnchor.MiddleLeft);
                line.childForceExpandWidth = false;
                UiKit.Key($"{i + 1}", row);
                var reply = UiKit.Label(text, row, 16, last ? UiKit.Muted : UiKit.Text2);
                reply.Size(54, -1, 1);
                click.Pointed += over => reply.color = over ? Color.white : last ? UiKit.Muted : UiKit.Text2;

                if (i < digits.Length)
                {
                    pageKeys.On(digits[i], act);
                }
            }

            // The world does not wait.
            var note = UiKit.Rect("Note", stage);
            note.TopRight(96, 64, 400, 20);
            var noteRow = note.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleRight);
            noteRow.childForceExpandWidth = false;
            var dot = UiKit.Image("Dot", note, UiKit.Ok, UiKit.Disc);
            dot.Size(6, 6, 0);
            UiKit.Lbl("Мир не ждёт ·", note, 11, UiKit.Muted, 0.2f).Size(20);
            UiKit.Key("Esc", note);
            UiKit.Lbl("уйти", note, 11, UiKit.Muted, 0.2f).Size(20);
        }
    }
}
