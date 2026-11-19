using System;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
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

        // ------------------------------------------------------------------ Loading (ROADMAP 2.10)

        RectTransform loadingFill;
        Text loadingStep, loadingTip;
        float loadingShare, loadingShown, tipTime;
        int tipAt;

        /// <summary>Lines shown while the world is made: advice, and the valley's own sayings.</summary>
        static readonly string[] LoadingTips =
        {
            "Ладонь копит эссенцию, Гу её тратят. Кончилась эссенция — бейте ладонью.",
            "Красная фигура на земле — враг сейчас ударит туда. Перекат в последний миг — и он промахнётся.",
            "Блок, поставленный в самый последний миг, становится парированием.",
            "Голодный Гу слабеет. Корм — в сумке; на рассвете Гу берут его сами, если вы разрешили.",
            "Лунный фонарь, которого вы коснулись, — место, где вы очнётесь. И дорога назад.",
            "Первобытный камень — и деньги, и эссенция, и прорыв. Каждый можно потратить только один раз.",
            "Три Гу одного пути звучат вместе — созвучие апертуры. Пять разных путей спорят друг с другом.",
            "Огонь по замёрзшему зверю даёт пар. Удар по замёрзшему — раскол.",
            "Звери одних путей боятся, к другим стойки. Кодекс запоминает это после третьей победы над видом.",
            "У Духовного источника камни становятся прорывом. На пике ступени там же прорываются на новый ранг.",
            "Компас наверху и круг карты внизу справа можно убрать в настройках, вкладка «Интерфейс».",
            "Клан помнит тех, кто полезен. Доски поручений есть в каждой деревне.",
            "В деревне говорят: талант — глубина колодца, но воду из него носят руками.",
            "Старики Гу Юэ говорят: Гу не нюхает, чей ты сын. Он слушается воли.",
            "Охотники Сюн говорят: волк, который воет, ещё не голоден. Бойся тихого.",
            "Речные люди Бай говорят: вода не спорит с камнем — она его обходит, а потом точит.",
        };

        public void LoadingProgress(float share, string step)
        {
            loadingShare = Mathf.Max(loadingShare, Mathf.Clamp01(share));

            if (loadingStep != null)
            {
                loadingStep.text = step;
            }
        }

        void LoadingPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "蛊", 620);
            loadingShare = loadingShown = 0;
            var middle = Centre(stage, 760, 330, 300);
            UiKit.Disp(loadingText, middle, 40, UiKit.GoldText, TextAnchor.MiddleCenter).Size(50);
            UiKit.Lbl("Гора Цинмао · деревня Гу Юэ", middle, 11, UiKit.Muted, 0.4f, TextAnchor.MiddleCenter).Size(16);
            Gap(middle, 26);

            // The bar: a hairline and a gold fill growing to the share made.
            var bar = UiKit.Rect("Bar", middle);
            bar.Size(6);
            var track = UiKit.Image("Track", bar, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f), UiKit.Plain);
            track.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -1, 0, -1);
            var fill = UiKit.Image("Fill", bar, Color.white, UiKit.Plain);
            fill.Gradient(UiKit.GoldDeep, UiKit.GoldLit, true);
            UiKit.Glow(fill.transform, UiKit.Gold.A(0.4f), 8);
            loadingFill = fill.rectTransform;
            loadingFill.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, -1.5f, 0, -1.5f);
            Gap(middle, 10);
            loadingStep = UiKit.Label("", middle, 13, UiKit.Muted, TextAnchor.MiddleCenter);
            loadingStep.Size(20);
            Gap(middle, 34);
            loadingTip = UiKit.Label(LoadingTips[tipAt % LoadingTips.Length], middle, 16, UiKit.Text2, TextAnchor.UpperCenter);
            loadingTip.horizontalOverflow = HorizontalWrapMode.Wrap;
            loadingTip.lineSpacing = 1.15f;
            loadingTip.Size(60);
            tipTime = 0;
        }

        /// <summary>The loading page as it waits: the bar eases to the share made, the advice turns every six seconds.</summary>
        void TickLoading()
        {
            if (Open != Page.Loading || loadingFill == null)
            {
                return;
            }

            loadingShown = Mathf.MoveTowards(loadingShown, loadingShare, Time.unscaledDeltaTime * 0.8f);
            loadingFill.anchorMax = new Vector2(loadingShown, 0.5f);
            tipTime += Time.unscaledDeltaTime;

            if (tipTime > 6)
            {
                tipTime = 0;
                tipAt++;
                loadingTip.text = LoadingTips[tipAt % LoadingTips.Length];
            }
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
            var stage = Dim(UiKit.Hex(0x2A3A50, 0.35f), "靜");
            UiKit.Orbit(stage, 620, UiKit.Gold.A(0.08f), 140, 10);
            UiKit.Orbit(stage, 820, UiKit.Gold.A(0.05f), -220, 0);

            // The title and the menu.
            var middle = Centre(stage, 480, 120, 700);
            UiKit.Lbl("Время остановлено", middle, 11, UiKit.LabelGold, 0.34f, TextAnchor.MiddleCenter).Size(16);
            var title = UiKit.Disp("ПАУЗА", middle, 72, UiKit.Text, TextAnchor.MiddleCenter);
            title.Track(0.2f);
            title.Size(84);
            UiKit.Image("Rule", middle, UiKit.Gold, UiKit.FadeBoth).Size(1);
            Gap(middle, 18);
            var keys = middle.gameObject.AddComponent<MenuKeys>();
            keys.Add(UiKit.Entry("Продолжить", null, middle, Close).Restyle(28, 62).Centre());

            if (Game.SandboxOnly)
            {
                keys.Add(UiKit.Entry("Настройки", null, middle, () => OpenSettings(Page.Pause)).Centre());
                keys.Add(UiKit.Entry("Управление", null, middle, () => Show(Page.Help)).Centre());
                keys.Add(UiKit.Entry("Покинуть апертуру", "В главное меню · ничего не сохраняется", middle,
                    () => Ask("Покинуть апертуру?", "Всё, что было в полигоне, исчезнет; вы выйдете в главное меню.", "Покинуть", () => Game.Instance.ToTitle(false))).Centre());
            }
            else
            {
                keys.Add(UiKit.Entry("Сохранить", "Игра и так сохраняется сама", middle, () => { Game.Save(); Toast("Сохранено"); Close(); }).Centre());
                keys.Add(UiKit.Entry("Настройки", null, middle, () => OpenSettings(Page.Pause)).Centre());
                keys.Add(UiKit.Entry("Управление", "Клавиши и как играть", middle, () => Show(Page.Help)).Centre());
                keys.Add(World.Aperture.Inside
                    ? UiKit.Entry("Покинуть апертуру", "Всё вернётся, как было", middle, () => { Close(); World.Aperture.Leave(); }).Centre()
                    : UiKit.Entry("Апертура бессмертного", "Полигон: любые Гу, ранги и приёмы", middle, () => { Close(); World.Aperture.Enter(); }).Centre());
                keys.Add(UiKit.Entry("Главное меню", "Игра сохранится", middle,
                    () => Ask("В главное меню?", "Игра сохранится — продолжить можно с титула, «Продолжить».", "В меню", () => Game.Instance.ToTitle())).Centre());
            }

            keys.Add(UiKit.Entry("Выйти из игры", null, middle,
                () => Ask("Выйти из игры?", Game.SandboxOnly ? "Апертура не сохраняется." : "Игра сохранится перед выходом.", "Выйти", Game.Quit)).Quiet(50).Centre());

            // The hero, at the left.
            var state = S.State;
            var hero = UiKit.Rect("Hero", stage);
            hero.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 72, -170, -420, -170);
            hero.Stack<VerticalLayoutGroup>(8);
            UiKit.Lbl("Герой", hero, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp($"Гу Юэ {state.HeroName}", hero, 30, UiKit.GoldText).Size(38);
            UiKit.Label($"{Cultivation.Name(state.Step)} · талант {Cultivation.TalentName(state.Talent)}", hero, 13, UiKit.Hex(0xA8B6B8)).Size(18);
            UiKit.Label($"{DifficultyRules.Name(state.Settings.Born)} · {DeathRules.Name(state.Settings.Death)}", hero, 13, UiKit.Muted).Size(18);
            Gap(hero, 6);
            PauseBar(hero, "Здоровье", state.Health / Mathf.Max(1, S.Stats.MaxHealth), UiKit.BarKind.Health);
            PauseBar(hero, "Эссенция", state.Essence / Mathf.Max(1, S.Stats.MaxEssence), UiKit.BarKind.Essence);
            PauseBar(hero, "Прорыв", S.InsightShare, UiKit.BarKind.System);
            Gap(hero, 4);
            var hour = World.Atmosphere.Hour;
            UiKit.Label($"День {state.Day} · {(int)hour:00}:{(int)(hour % 1 * 60):00} · {Story.ChapterNames[S.Chapter]}", hero, 13, UiKit.Muted).Size(18);

            // The title won by deeds, if any (ROADMAP 12.4).
            if (S.Title != "")
            {
                UiKit.Label($"Титул: «{S.Title}»", hero, 13, UiKit.GoldText).Size(18);
            }

            // What is in hand, at the right.
            var now = UiKit.Rect("Now", stage);
            now.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), -420, -170, 72, -170);
            now.Stack<VerticalLayoutGroup>(8);
            UiKit.Lbl("Сейчас", now, 11, UiKit.LabelGold, 0.34f, TextAnchor.MiddleRight).Size(16);
            var main = S.MainQuest;

            if (Game.SandboxOnly || World.Aperture.Inside)
            {
                UiKit.Disp("Апертура бессмертного", now, 26, UiKit.Text, TextAnchor.MiddleRight).Size(34);
                Para("Полигон: ничего не сохраняется и не считается.", now, 14, UiKit.Muted).alignment = TextAnchor.UpperRight;
            }
            else if (main != null)
            {
                var def = Story.Get(main.Quest);
                UiKit.Lbl($"Глава {def.Chapter} · задание", now, 11, UiKit.Muted, 0.3f, TextAnchor.MiddleRight).Size(16);
                var name = UiKit.Disp(def.Title, now, 26, UiKit.Text, TextAnchor.MiddleRight);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.Size(36);
                Para(S.StepText(main), now, 15, UiKit.Text2).alignment = TextAnchor.UpperRight;
            }
            else
            {
                UiKit.Disp("Сюжет пройден", now, 26, UiKit.Text, TextAnchor.MiddleRight).Size(34);
                Para("Поручения — у досок в деревнях.", now, 14, UiKit.Muted).alignment = TextAnchor.UpperRight;
            }

            Gap(now, 8);
            var minutes = Mathf.RoundToInt(state.PlayedSeconds / 60);
            UiKit.Label($"В игре {minutes} мин · побед {state.Kills} · смертей {state.Deaths}{(state.Rebirths > 0 ? $" · перерождений {state.Rebirths}" : "")}", now, 12, UiKit.Muted, TextAnchor.MiddleRight).Size(18);
            UiKit.Label(Game.SandboxOnly ? "Без сохранения" : $"Игра сохраняется сама каждые {(Prefs.Autosave >= 60 ? $"{Prefs.Autosave / 60} мин" : $"{Prefs.Autosave} с")}", now, 12, UiKit.Muted, TextAnchor.MiddleRight).Size(18);

            UiKit.Footer(stage, new[] { ("↑↓", "Выбор"), ("Enter", "Открыть"), ("Esc", "Вернуться в игру") }).GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        }

        /// <summary>A line of the pause's hero: a name and a thin bar filled to a share.</summary>
        static void PauseBar(RectTransform parent, string name, float share, UiKit.BarKind kind)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(30);
            UiKit.Label(name, row, 12, UiKit.Muted).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 1));
            UiKit.Label($"{Mathf.RoundToInt(Mathf.Clamp01(share) * 100)}%", row, 12, UiKit.Text2, TextAnchor.MiddleRight).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 1));
            var bar = UiKit.Rect("Bar", row);
            bar.Place(Vector2.zero, new Vector2(1, 0.4f));
            UiKit.Bar(bar, kind, Mathf.Clamp01(share), 4);
        }

        void HelpPage()
        {
            var body = Frame("术", "Управление", "Как играть", false, () => Show(Page.Pause), null, new[] { ("Esc", "Назад") });
            var left = Column(body, 0.08f, 0.46f, 4);
            var right = Column(body, 0.54f, 0.92f, 4);
            UiKit.Lbl("В мире", left, 11, UiKit.LabelGold).Size(22);

            // The fixed ones, then every action with the key it has now (Settings → Управление changes them).
            foreach (var (key, what) in new[] { ("W A S D", "Идти"), ("Мышь", "Смотреть"), ("F5", "Вид: плечо · глаза героя · другое плечо") })
            {
                Fact(left, what, key, UiKit.GoldText);
            }

            foreach (var action in Actors.Bindings.All)
            {
                Fact(left, action.Id == "strike" ? action.Name + ", три подряд" : action.Name, Actors.Bindings.LabelsOf(action.Id), UiKit.GoldText);
            }

            UiKit.Lbl("Страницы", right, 11, UiKit.LabelGold).Size(22);

            foreach (var (key, what) in new[] { ("K", "Апертура: Гу и слоты"), ("U", "Убийственные приёмы"), ("P", "Пути: постижение"), ("I", "Сумка"), ("J", "Журнал заданий"), ("M", "Карта"), ("B", "Кодекс"), ("L", "Кланы"), ("O", "Статус и тело"), ("Esc", "Пауза") })
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
            var rule = S.State.Settings.Death;
            var stage = Dim(UiKit.Hex(0x5A1410, 0.5f), "死");
            var middle = Centre(stage, 760, 220, 520);
            var hardcore = rule == DeathRule.Hardcore && !Game.SandboxOnly;
            UiKit.Lbl(hardcore ? "Одна жизнь" : rule == DeathRule.Rebirth ? "Перерождение" : "Тьма", middle, 11, UiKit.Danger, 0.5f, TextAnchor.MiddleCenter).Size(16);
            var title = UiKit.Disp(hardcore ? "ПУТЬ ОКОНЧЕН" : "ВЫ ПАЛИ", middle, 78, UiKit.Hex(0xF2D6CE), TextAnchor.MiddleCenter);
            title.Track(0.2f);
            title.Size(90);
            UiKit.Image("Rule", middle, UiKit.Danger, UiKit.FadeBoth).Size(1);
            Gap(middle, 16);
            var state = S.State;
            var row = UiKit.Rect("Row", middle);

            if (hardcore)
            {
                var minutes = Mathf.RoundToInt(state.PlayedSeconds / 60);
                Para($"Гу Юэ {state.HeroName} · {Cultivation.Name(state.Step)} · глава {S.Chapter}", middle, 18, UiKit.Text2).alignment = TextAnchor.MiddleCenter;
                Para($"{DifficultyRules.Name(state.Settings.Born)} · {minutes} мин в пути · побед {state.Kills}. Сохранение удалено.", middle, 15, UiKit.Muted).alignment = TextAnchor.MiddleCenter;
                Gap(middle, 22);
                row = UiKit.Rect("Row", middle);
                row.Size(56);
                UiKit.HexButton("Главное меню", row, () => Game.Instance.ToTitle(false), UiKit.HexKind.Main, 14, 56, "Enter").GetComponent<RectTransform>().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 56));
                stage.gameObject.AddComponent<PageKeys>().On(Key.Enter, () => Game.Instance.ToTitle(false));

                return;
            }

            if (rule == DeathRule.Rebirth && !Game.SandboxOnly)
            {
                var arc = DeathRules.RebirthArc(S.Chapter);
                Para($"Вы родитесь заново в начале главы {arc} — «{Story.ChapterNames[arc]}». С собой — только память: кодекс, достижения, подсказки.", middle, 16, UiKit.Text2).alignment = TextAnchor.MiddleCenter;
                Gap(middle, 22);
                row = UiKit.Rect("Row", middle);
                row.Size(56);
                UiKit.HexButton("Переродиться", row, () => Game.Instance.Reborn(), UiKit.HexKind.Main, 14, 56, "Enter").GetComponent<RectTransform>().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 56));
                stage.gameObject.AddComponent<PageKeys>().On(Key.Enter, () => Game.Instance.Reborn());

                return;
            }

            // What felled the hero, where they wake, what is lost and what stays (ROADMAP 2.x «Вы пали»).
            if (!string.IsNullOrEmpty(deathBane) || !string.IsNullOrEmpty(deathWhere))
            {
                var how = string.IsNullOrEmpty(deathBane) ? deathWhere : string.IsNullOrEmpty(deathWhere) ? deathBane : deathBane + " · " + deathWhere;
                UiKit.Label(char.ToUpperInvariant(how[0]) + how.Substring(1), middle, 16, UiKit.Hex(0xE8B4A8), TextAnchor.MiddleCenter).Size(24);
            }

            UiKit.Label(lostStones > 0 ? $"Потеряно первобытных камней: {lostStones}" : "Терять было нечего", middle, 16, UiKit.Text2, TextAnchor.MiddleCenter).Size(24);
            var lantern = Atlas.Thing(state.Lantern) ?? Valley.Thing("lantern_village");

            if (lantern != null)
            {
                UiKit.Label("Очнётесь у лунного фонаря: " + Atlas.NameAt(lantern.At), middle, 14, UiKit.Muted, TextAnchor.MiddleCenter).Size(22);
            }

            var stays = state.Wounds != null && state.Wounds.Exists(w => w.Kind == WoundKind.Burn || w.Kind == WoundKind.Fracture);
            UiKit.Label(stays ? "Ушибы и порезы затянулись; ожоги и переломы остались" : "Свет фонаря затянул ушибы и порезы; действие пилюль и Гу развеялось", middle, 13, UiKit.Muted, TextAnchor.MiddleCenter).Size(20);
            Gap(middle, 20);
            row = UiKit.Rect("Row", middle);
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
                var words = UiKit.Disp(Words.Fill(line, S.State), middle, 22, UiKit.Hex(0xE6DCCB), TextAnchor.UpperCenter);
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
