using System;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>The pages that are not the hero's own: the title, loading, pause, the keys, death, the last choice and the ending, and the spring's meditation.</summary>
    public partial class Ui
    {
        const string Credits = "Fox model: PixelMannen (CC0) · rigging and animation: tomkranis (CC BY 4.0) · glTF: AsoboStudio, scurest (CC BY 4.0)";

        RectTransform Centre(RectTransform stage, float width, float top, float height)
        {
            var middle = UiKit.Rect("Middle", stage);
            middle.anchorMin = middle.anchorMax = new Vector2(0.5f, 1);
            middle.pivot = new Vector2(0.5f, 1);
            middle.anchoredPosition = new Vector2(0, -top);
            middle.sizeDelta = new Vector2(width, height);
            middle.Stack<VerticalLayoutGroup>(6, null, TextAnchor.UpperCenter);

            return middle;
        }

        void Title()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "蛊", 620);
            var middle = Centre(stage, 620, 150, 700);
            UiKit.Lbl("Лунная тень Цинмао", middle, 12, UiKit.LabelGold, 0.5f, TextAnchor.MiddleCenter).Size(18);
            var name = UiKit.Disp("GU WORLD", middle, 96, UiKit.Text, TextAnchor.MiddleCenter);
            name.Track(0.18f);
            name.Size(110);
            UiKit.Image("Rule", middle, UiKit.Gold, UiKit.FadeBoth).Size(1);
            Gap(middle, 34);
            var keys = middle.gameObject.AddComponent<MenuKeys>();
            var save = Game.ReadSave();

            if (save != null)
            {
                var minutes = Mathf.RoundToInt(save.PlayedSeconds / 60);
                keys.Add(UiKit.Entry("Продолжить", $"Гу Юэ {save.HeroName} · {Cultivation.Name(save.Step)} · {minutes} мин", middle, () => Game.Instance.Begin(false)).Centre());
            }

            keys.Add(UiKit.Entry("Новая игра", save != null ? $"В ячейке {Game.Slot + 1} — прежнее будет заменено" : null, middle, () => Game.Instance.Begin(true)).Centre());
            keys.Add(UiKit.Entry($"Ячейка сохранения: {Game.Slot + 1} из {Game.Slots}", SlotLine(Game.Slot + 1 < Game.Slots ? Game.Slot + 1 : 0, "Следующая: "), middle, () =>
            {
                Game.Slot = (Game.Slot + 1) % Game.Slots;
                Show(Page.Title);
            }).Centre());
            keys.Add(UiKit.Entry($"Качество: {Quality.Names[Quality.Level]}", Quality.Level == 2 ? "Полная модель героя, дальние тени, свечение" : Quality.Level == 0 ? "Для слабой встроенной графики" : "Для ноутбука без видеокарты", middle, () =>
            {
                Quality.Set((Quality.Level + 1) % 3);
                Game.Rig.Dress();
                Show(Page.Title);
            }).Centre());
            keys.Add(UiKit.Entry("Выйти", null, middle, Game.Quit).Centre());
            var credit = UiKit.Label(Credits, stage, 11, UiKit.Muted.A(0.7f), TextAnchor.MiddleCenter);
            credit.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 0, 60, 0, -78);
            UiKit.Footer(stage, new[] { ("↑↓", "Выбор"), ("Enter", "Открыть") }).GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        }

        /// <summary>A line about what a slot holds.</summary>
        static string SlotLine(int slot, string lead)
        {
            var data = Game.ReadSlot(slot);

            return data == null ? lead + $"ячейка {slot + 1} пуста" : lead + $"{slot + 1} — Гу Юэ {data.HeroName}, {Cultivation.Name(data.Step)}, {Mathf.RoundToInt(data.PlayedSeconds / 60)} мин";
        }

        void LoadingPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "蛊", 620);
            var middle = Centre(stage, 700, 380, 200);
            UiKit.Disp(loadingText, middle, 40, UiKit.GoldText, TextAnchor.MiddleCenter).Size(50);
            UiKit.Lbl("Гора Цинмао · деревня Гу Юэ", middle, 11, UiKit.Muted, 0.4f, TextAnchor.MiddleCenter).Size(16);
        }

        RectTransform Dim(Color tint, string brush)
        {
            var stage = UiKit.Stage(pageRoot);
            var dim = UiKit.Image("Dim", stage, Color.white, UiKit.Plain);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
            dim.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.74f), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.9f));
            UiKit.Cloud(stage, new Vector2(0.5f, 0.5f), new Vector2(1500, 1000), tint);
            UiKit.Image("Vignette", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.9f), UiKit.Vignette).rectTransform.Fill();

            if (brush != null)
            {
                var stroke = UiKit.Brush(brush, stage, 520, UiKit.Gold.A(0.04f));
                stroke.rectTransform.anchorMin = stroke.rectTransform.anchorMax = new Vector2(0.5f, 1);
                stroke.rectTransform.anchoredPosition = new Vector2(0, -330);
            }

            return stage;
        }

        void PausePage()
        {
            var stage = Dim(UiKit.Hex(0x2A3A50, 0.35f), "息");
            var middle = Centre(stage, 520, 150, 640);
            UiKit.Lbl("Время остановлено", middle, 11, UiKit.LabelGold, 0.34f, TextAnchor.MiddleCenter).Size(16);
            var title = UiKit.Disp("ПАУЗА", middle, 72, UiKit.Text, TextAnchor.MiddleCenter);
            title.Track(0.2f);
            title.Size(84);
            UiKit.Image("Rule", middle, UiKit.Gold, UiKit.FadeBoth).Size(1);
            Gap(middle, 24);
            var keys = middle.gameObject.AddComponent<MenuKeys>();
            keys.Add(UiKit.Entry("Продолжить", null, middle, Close).Centre());
            keys.Add(UiKit.Entry("Управление", null, middle, () => Show(Page.Help)).Centre());
            keys.Add(UiKit.Entry("Клавиши", "Сменить клавиши действий", middle, () => Show(Page.Controls)).Centre());
            keys.Add(World.Aperture.Inside
                ? UiKit.Entry("Покинуть апертуру", "Всё вернётся, как было", middle, () => { Close(); World.Aperture.Leave(); }).Centre()
                : UiKit.Entry("Апертура бессмертного", "Полигон: любые Гу, ранги и приёмы, ничего не считается", middle, () => { Close(); World.Aperture.Enter(); }).Centre());
            keys.Add(UiKit.Entry("Настройки игры", $"{DifficultyRules.Name(S.State.Settings.Difficulty)} · голод Гу: {(S.State.Settings.Hunger == HungerRule.Off ? "нет" : S.State.Settings.Hunger == HungerRule.Soft ? "мягкий" : "канон")}", middle, () => Show(Page.Settings)).Centre());
            keys.Add(UiKit.Entry($"Качество: {Quality.Names[Quality.Level]}", "Деревья и модель героя сменятся при следующем входе в игру", middle, () =>
            {
                Quality.Set((Quality.Level + 1) % 3);
                Game.Rig.Dress();
                Show(Page.Pause);
            }).Centre());
            keys.Add(UiKit.Entry("Главное меню", "Игра сохранится", middle, () => Game.Instance.ToTitle()).Centre());
            keys.Add(UiKit.Entry("Выйти из игры", null, middle, Game.Quit).Centre());
            var minutes = Mathf.RoundToInt(S.State.PlayedSeconds / 60);
            var note = UiKit.Label($"Глава {S.Chapter} · {Story.ChapterNames[S.Chapter]} · в игре {minutes} мин · побед {S.State.Kills} · смертей {S.State.Deaths}", stage, 13, UiKit.Muted, TextAnchor.MiddleCenter);
            note.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 0, 64, 0, -84);
            UiKit.Footer(stage, new[] { ("↑↓", "Выбор"), ("Enter", "Открыть"), ("Esc", "Вернуться в игру") }).GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        }

        void HelpPage()
        {
            var body = Frame("术", "Управление", "Как играть", false, () => Show(Page.Pause), null, new[] { ("Esc", "Назад") });
            var left = Column(body, 0.08f, 0.46f, 4);
            var right = Column(body, 0.54f, 0.92f, 4);
            UiKit.Lbl("В мире", left, 11, UiKit.LabelGold).Size(22);

            foreach (var (key, what) in new[]
            {
                ("W A S D", "Идти"), ("Shift", "Бежать быстрее (тратит силы)"), ("Мышь", "Смотреть"), ("Колесо", "Ближе и дальше"),
                ("Пробел", "Прыжок (через кольца молний)"), ("ПКМ / Alt", "Перекат: в нём вас не задеть"), ("Ctrl", "Блок; поставленный в последний миг — парирование"), ("ЛКМ", "Удар ладонью, три подряд"),
                ("Q E R F Z C", "Гу в слотах"), ("V · X", "Убийственный приём · следующий"), ("Tab / СКМ", "Держать цель"), ("H", "Пилюля заживления"), ("G", "Впитать первобытный камень"), ("T", "Говорить, взять, открыть"),
            })
            {
                Fact(left, what, key, UiKit.GoldText);
            }

            UiKit.Lbl("Страницы", right, 11, UiKit.LabelGold).Size(22);

            foreach (var (key, what) in new[] { ("K", "Апертура: Гу и слоты"), ("U", "Убийственные приёмы"), ("P", "Пути: постижение"), ("I", "Сумка"), ("J", "Журнал заданий"), ("M", "Карта"), ("B", "Кодекс"), ("L", "Кланы"), ("Esc", "Пауза") })
            {
                Fact(right, what, key, UiKit.GoldText);
            }

            Gap(right, 14);
            UiKit.Lbl("Как драться", right, 11, UiKit.LabelGold).Size(22);
            Para("Удары ладонью наполняют апертуру эссенцией, Гу её тратят. Красная вспышка или красная фигура на земле — враг бьёт: уходите перекатом, потом отвечайте. Если легли — очнётесь у лунного фонаря, которого касались последним.", right);
            Gap(right, 10);
            UiKit.Lbl("Как расти", right, 11, UiKit.LabelGold).Size(22);
            Para("Победы и задания дают прорыв. У Духовного источника в деревне камни превращаются в прорыв, а на пиковой ступени там же прорываются на новый ранг. Диких Гу очищайте в апертуре, лишних сливайте у алтаря или продавайте.", right);
        }

        void DeathPage()
        {
            var stage = Dim(UiKit.Hex(0x5A1410, 0.5f), "死");
            var middle = Centre(stage, 620, 260, 400);
            UiKit.Lbl("Тьма", middle, 11, UiKit.Danger, 0.5f, TextAnchor.MiddleCenter).Size(16);
            var title = UiKit.Disp("ВЫ ПАЛИ", middle, 78, UiKit.Hex(0xF2D6CE), TextAnchor.MiddleCenter);
            title.Track(0.2f);
            title.Size(90);
            UiKit.Image("Rule", middle, UiKit.Danger, UiKit.FadeBoth).Size(1);
            Gap(middle, 16);
            UiKit.Label(lostStones > 0 ? $"Потеряно первобытных камней: {lostStones}" : "Терять было нечего", middle, 16, UiKit.Text2, TextAnchor.MiddleCenter).Size(24);
            Gap(middle, 26);
            var row = UiKit.Rect("Row", middle);
            row.Size(56);
            UiKit.HexButton("Очнуться у лунного фонаря", row, Game.Wake, UiKit.HexKind.Main, 14, 56, "Enter").GetComponent<RectTransform>().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 56));
        }

        void ChoicePage()
        {
            var step = S.PendingChoice();

            if (step == null)
            {
                Close();
                return;
            }

            var stage = Dim(UiKit.Hex(0x5A1410, 0.45f), "血");
            var middle = Centre(stage, 760, 210, 560);
            UiKit.Lbl("Гу Кровавой луны", middle, 11, UiKit.Danger, 0.5f, TextAnchor.MiddleCenter).Size(16);
            UiKit.Disp("Он ждёт вашего решения", middle, 44, UiKit.Text, TextAnchor.MiddleCenter).Size(56);
            UiKit.Image("Rule", middle, UiKit.Danger, UiKit.FadeBoth).Size(1);
            Gap(middle, 30);

            foreach (var (text, flag) in step.Choices)
            {
                var chosen = flag;
                UiKit.HexButton(text, middle, () =>
                {
                    S.Choose(chosen);
                    Game.Save();
                    Show(Page.Ending);
                }, chosen == "ending_demonic" ? UiKit.HexKind.Bad : UiKit.HexKind.Main, 14, 64);
                Gap(middle, 10);
            }
        }

        void EndingPage()
        {
            var demonic = S.Has("ending_demonic");
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, demonic ? "魔" : "月", 620);
            var middle = Centre(stage, 860, 120, 700);
            UiKit.Lbl(demonic ? "Путь демона" : "Путь клана", middle, 12, demonic ? UiKit.Danger : UiKit.LabelGold, 0.5f, TextAnchor.MiddleCenter).Size(18);
            UiKit.Disp("ЛУННАЯ ТЕНЬ", middle, 64, UiKit.Text, TextAnchor.MiddleCenter).Size(76);
            UiKit.Image("Rule", middle, UiKit.Gold, UiKit.FadeBoth).Size(1);
            Gap(middle, 20);

            foreach (var line in Story.Endings[demonic ? "ending_demonic" : "ending_righteous"])
            {
                var words = UiKit.Disp(line, middle, 22, UiKit.Hex(0xE6DCCB), TextAnchor.UpperCenter);
                words.horizontalOverflow = HorizontalWrapMode.Wrap;
                words.lineSpacing = 1.05f;
                Gap(middle, 4);
            }

            Gap(middle, 20);
            var row = UiKit.Rect("Row", middle);
            row.Size(56);
            UiKit.HexButton("Вернуться в долину", row, Close, UiKit.HexKind.Main, 14, 56).GetComponent<RectTransform>().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 56));
            var credit = UiKit.Label(Credits, stage, 11, UiKit.Muted.A(0.7f), TextAnchor.MiddleCenter);
            credit.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 0, 30, 0, -48);
        }

        // ------------------------------------------------------------------ The spring

        void SpringPage()
        {
            var stage = UiKit.Stage(pageRoot);
            var panel = UiKit.Panel("Spring", stage, UiKit.Panel1, UiKit.Gold.A(0.25f));
            panel.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), -500, -270, 60, -270);
            var column = UiKit.Rect("Column", panel.transform);
            column.Fill(30);
            column.Stack<VerticalLayoutGroup>(8);
            var state = S.State;
            UiKit.Lbl("Духовный источник", column, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(Cultivation.Name(state.Step), column, 28, UiKit.Text).Size(34);
            UiKit.Bar(column, UiKit.BarKind.Stamina, S.InsightShare, 5);
            var top = state.Step >= Cultivation.MaxStep;
            Fact(column, "Прорыв", top ? "вершина третьего ранга" : $"{(int)state.Insight} / {S.InsightNeeded}", UiKit.GoldText);
            Fact(column, "Первобытные камни", state.Stones.ToString(), UiKit.GoldText);
            Fact(column, "Талант", $"{Cultivation.TalentName(state.Talent)} · {Mathf.RoundToInt(state.Talent * 100)}% апертуры");
            Gap(column, 6);
            Para($"Один камень, сожжённый у воды, даёт {Cultivation.StoneInsight:0} прорыва. Победы и задания дают его без камней.", column, 14, UiKit.Muted);
            Gap(column, 6);

            void Burn(string text, int count)
            {
                var can = count > 0 && state.Stones >= count && !top;
                UiKit.HexButton(text, column, () =>
                {
                    S.Meditate(count);
                    Fx.Effects.Wave(Game.Hero.transform.position, new Color(0.5f, 0.95f, 0.85f), 3, 0.5f);
                    Show(Page.Spring);
                }, can ? UiKit.HexKind.Ghost : UiKit.HexKind.Off, 13, 46);
            }

            if (Cultivation.IsRankUp(state.Step) && state.Insight >= Cultivation.Needs[state.Step])
            {
                var why = S.WhyNoBreakthrough();
                UiKit.HexButton($"Прорыв на ранг {GuBook.RankNumerals[S.Rank + 1]} · {Cultivation.StoneCost[state.Step]} камней", column, () =>
                {
                    if (S.Breakthrough())
                    {
                        Game.Save();
                    }

                    Show(Page.Spring);
                }, why == null ? UiKit.HexKind.Solid : UiKit.HexKind.Off, 14, 60);

                if (why != null)
                {
                    Para(why, column, 14, UiKit.Danger);
                }
            }
            else
            {
                var need = top ? 0 : Mathf.CeilToInt((S.InsightNeeded - state.Insight) / Cultivation.StoneInsight);
                Burn("Сжечь 1 камень", 1);
                Burn("Сжечь 10 камней", 10);
                Burn(need > 0 && need <= state.Stones ? $"Сжечь до ступени · {need}" : $"Сжечь все · {state.Stones}", need > 0 && need <= state.Stones ? need : state.Stones);
            }

            UiKit.Footer(stage, new[] { ("Esc", "Встать") });
        }
    }
}
