using System;
using GuWorld.Cultivation;
using GuWorld.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The admin panel (F10): everything a tester needs at a click — the hero's health and sea, ranks, talent, the
    /// body's traits, Gu, the hour and the weather, places to go to, beasts and Gu Masters to call up. It works only
    /// with the admin mode on (switched here, kept between games); with it off the chat's cheating commands refuse too.
    /// Most buttons run the chat's own commands, so both do the same.
    /// </summary>
    public partial class Menus
    {
        void AdminPage()
        {
            var stage = UiKit.Stage(pageRoot);
            var dim = UiKit.Image("Dim", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.6f), UiKit.Plain);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;

            // The panel: dark glass, a coral edge — this is not the game, it is its workshop.
            var frame = UiKit.Image("Admin", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.72f), UiKit.Plain);
            frame.rectTransform.Place(Vector2.zero, Vector2.one, 40, 32, 40, 32);
            UiKit.Frame(frame.transform, UiKit.Danger.A(0.35f));
            UiKit.Glow(frame.transform, UiKit.Danger.A(0.12f), 40);
            var top = UiKit.Image("Top", frame.transform, UiKit.Danger.A(0.06f), UiKit.Plain).rectTransform;
            top.Place(new Vector2(0, 1), Vector2.one, 0, -60, 0, 0);
            UiKit.Image("Rule", top, UiKit.Danger.A(0.25f), UiKit.Plain).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            var head = top.Stack<HorizontalLayoutGroup>(16, new RectOffset(22, 22, 0, 0), TextAnchor.MiddleLeft);
            head.childForceExpandWidth = false;
            var gem = UiKit.Image("Gem", top, UiKit.Danger, UiKit.Diamond);
            gem.Size(8, 8, 0);
            var title = UiKit.Disp("АДМИН-ПАНЕЛЬ", top, 22);
            title.Track(0.16f);
            title.Size(60);
            UiKit.Lbl(host.Admin ? "Режим администратора · включён" : "Режим администратора · выключен", top, 11, host.Admin ? UiKit.Danger : UiKit.Muted, 0.24f).Size(60);
            UiKit.Rect("Space", top).Size(60, -1, 1);
            UiKit.Label("Кнопки выполняют команды чата — результат одинаковый", top, 12, UiKit.Muted).Size(60);
            UiKit.Tb("Закрыть · F10 / Esc", top, false, host.Resume, 11).Size(34, 190, 0);
            var body = UiKit.Rect("Body", frame.transform).Place(Vector2.zero, Vector2.one, 16, 16, 16, 76);

            var columns = new Transform[3];

            for (var i = 0; i < 3; i++)
            {
                var panel = UiKit.Image("Column", body, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.02f), UiKit.Plain);
                UiKit.Frame(panel.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f));
                panel.rectTransform.anchorMin = new Vector2(i / 3f, 0);
                panel.rectTransform.anchorMax = new Vector2((i + 1) / 3f, 1);
                panel.rectTransform.offsetMin = new Vector2(i == 0 ? 0 : 6, 0);
                panel.rectTransform.offsetMax = new Vector2(i == 2 ? 0 : -6, 0);
                var holder = UiKit.Rect("Holder", panel.transform).Fill(12);
                holder.Stack<VerticalLayoutGroup>(0);
                var column = Scroll(holder, 0);
                column.GetComponent<VerticalLayoutGroup>().spacing = 2;
                columns[i] = column;
            }

            var hero = columns[0];
            Heading(hero, "РЕЖИМ АДМИНИСТРАТОРА");
            UiKit.Choices(hero, new[] { "Включён", "Выключен" }, host.Admin ? 0 : 1, i =>
            {
                host.Admin = i == 0;
                Notify(host.Admin ? "Режим администратора включён: чит-команды чата и эта панель работают" : "Режим администратора выключен");
                Show(Page.Admin);
            }, 15);

            if (!host.Admin)
            {
                UiKit.Label("Пока режим выключен, панель и чит-команды чата (/give, /gu, /rank, /tp, /time…) не работают — " +
                            "игра идёт честно. Включите его для проверок; он запоминается между играми.", hero, 16, UiKit.Muted).Size(120);

                return;
            }

            var gu = host.Cultivator;
            var a = gu.Aperture;
            var p = host.Physique;

            void Toggle(Transform column, string name, bool on, Action<bool> set)
            {
                Heading(column, name);
                UiKit.Choices(column, new[] { "Да", "Нет" }, on ? 0 : 1, i =>
                {
                    set(i == 0);
                    Show(Page.Admin);
                }, 14);
            }

            void Buttons(Transform column, string name, params (string label, Action act)[] buttons)
            {
                Heading(column, name);
                UiKit.Choices(column, Array.ConvertAll(buttons, b => b.label), -1, i =>
                {
                    buttons[i].act();
                    Show(Page.Admin);
                }, 14);
            }

            Action Run(string line) => () =>
            {
                var said = host.Commands?.Run(line);

                if (!string.IsNullOrEmpty(said))
                {
                    Notify(said.TrimStart('!'));
                }
            };

            // ── The hero ──
            Toggle(hero, "БЕССМЕРТИЕ", host.Health.Immortal, on => host.Health.Immortal = on);
            Toggle(hero, "БЕСКОНЕЧНАЯ ЭССЕНЦИЯ", gu.EndlessEssence, on => gu.EndlessEssence = on);
            Toggle(hero, "ГУ НЕ УСТАЮТ", gu.Untiring, on => gu.Untiring = on);
            Toggle(hero, "ГУ НЕ ГОЛОДАЮТ", gu.Sated, on => gu.Sated = on);
            Buttons(hero, "ЗДОРОВЬЕ · МОРЕ · СИЛЫ",
                ("Исцелить", Run("/heal")),
                ("Полное море", Run("/essence max")),
                ("Выносливость", () => p.Refill()));
            Buttons(hero, "ПЕРВОБЫТНЫЕ КАМНИ",
                ("+10", Run("/give primeval_stone 10")),
                ("+100", Run("/give primeval_stone 100")),
                ("+500", Run("/give primeval_stone 500")));
            Buttons(hero, "ГУ В СУМКУ (ВСЕ ГУ РАНГА)",
                ("I", Run("/gu 1")), ("II", Run("/gu 2")), ("III", Run("/gu 3")), ("IV", Run("/gu 4")), ("V", Run("/gu 5")));
            Buttons(hero, "ГУ ИЗ СУМКИ",
                ("Очистить все в апертуру", Run("/use refine")),
                ("Отдохнуть всем", () => gu.RestAll()));

            // ── Cultivation and the body ──
            var realm = columns[1];
            Heading(realm, $"РАНГ · {a.Rank}, {Aperture.Name(a.Stage)}");
            UiKit.Choices(realm, new[] { "I", "II", "III", "IV", "V" }, a.Rank - 1, i =>
            {
                gu.SetRealm(i + 1, gu.Aperture.Stage);
                Show(Page.Admin);
            }, 14);
            UiKit.Choices(realm, new[] { "Начальная", "Средняя", "Верхняя", "Пик" }, (int)a.Stage, i =>
            {
                gu.SetRealm(gu.Aperture.Rank, (Stage)i);
                Show(Page.Admin);
            }, 13);
            Buttons(realm, $"ПРОРЫВ · шанс {gu.BreakChance * 100:0.##}%",
                ("Стенки 100%", () => gu.Aperture.Progress = 1),
                ("Закалка 100%", () => gu.Aperture.Readiness = 1),
                ("Прорыв наверняка", () =>
                {
                    gu.Aperture.Progress = 1;
                    gu.Aperture.Essence = gu.Aperture.Cap;
                    gu.BreakThrough(0);
                }));
            Heading(realm, $"ТАЛАНТ · {a.TalentName}");
            UiKit.Choices(realm, new[] { "30", "50", "70", "88", "99", "100" }, -1, i =>
            {
                gu.SetTalent(new[] { 30f, 50, 70, 88, 99, 100 }[i]);
                Show(Page.Admin);
            }, 14);

            Heading(realm, $"ТЕЛО · уровень {p.Overall}");

            foreach (Trait trait in Enum.GetValues(typeof(Trait)))
            {
                var t = trait;
                UiKit.Choices(realm, new[] { $"{Physique.Name(t)} {p.Level(t)}", "−5", "+5", "+20" }, 0, i =>
                {
                    if (i > 0)
                    {
                        p.Set(t, p.Level(t) + (i == 1 ? -5 : i == 2 ? 5 : 20));
                    }

                    Show(Page.Admin);
                }, 13);
            }

            Buttons(realm, "ВСЁ ТЕЛО",
                ("Всем 30", () => SetAll(p, 30)),
                ("Всем 60", () => SetAll(p, 60)),
                ("Сбросить", () => p.Renew()));

            // ── The world ──
            var world = columns[2];
            Buttons(world, "ВРЕМЯ",
                ("Рассвет", Run("/time set dawn")), ("Утро", Run("/time set morning")), ("День", Run("/time set noon")),
                ("Вечер", Run("/time set evening")), ("Ночь", Run("/time set night")), ("+1 день", Run("/time add 24")));
            Buttons(world, "ПОГОДА",
                ("Ясно", Run("/weather clear")), ("Облачно", Run("/weather cloudy")), ("Дождь", Run("/weather rain")), ("Своя", Run("/weather auto")));
            Heading(world, "ТЕЛЕПОРТ");
            var places = new[] { ("gu_yue", "Гу Юэ"), ("bai", "Бай"), ("xiong", "Сюн"), ("peak", "Вершина"), ("spring", "Источник"),
                ("cave", "Пещера"), ("grove", "Роща"), ("pool", "Озеро"), ("waterfall", "Водопад"), ("fore_peak", "Перед. гора") };

            for (var i = 0; i < places.Length; i += 5)
            {
                var row = new (string, string)[Mathf.Min(5, places.Length - i)];
                Array.Copy(places, i, row, 0, row.Length);
                UiKit.Choices(world, Array.ConvertAll(row, r => r.Item2), -1, k =>
                {
                    Run("/tp " + row[k].Item1)();
                    host.Resume();
                }, 13);
            }

            Buttons(world, "ЗВЕРИ",
                ("Волки ×3", Run("/wolves 3 grey")), ("Молниевые ×3", Run("/wolves 3 lightning")), ("Кабаны ×3", Run("/wolves 3 boar")),
                ("Медведь", Run("/wolves 1 bear")), ("Змея", Run("/wolves 1 snake")));
            Buttons(world, "КОРОЛИ ЗВЕРЕЙ",
                ("Сотни", Run("/wolves 1 bold")), ("Тысячи", Run("/wolves 1 frenzy")), ("Мириады", Run("/wolves 1 thunder_crown")),
                ("Кабанов", Run("/wolves 1 boar_king")), ("Обезьян", Run("/wolves 1 monkey_king")));
            Buttons(world, "ВОЛЧИЙ ПРИЛИВ", ("Прилив", Run("/tide")), ("Великий прилив", Run("/tide great")));
            Buttons(world, "ГУ-МАСТЕРА БАЙ", ("Ранг I", Run("/masters bai 1 2")), ("Ранг II", Run("/masters bai 2 1")), ("Ранг III", Run("/masters bai 3 1")));
            Buttons(world, "ГУ-МАСТЕРА СЮН", ("Ранг I", Run("/masters xiong 1 2")), ("Ранг II", Run("/masters xiong 2 1")), ("Ранг III", Run("/masters xiong 3 1")));
            Buttons(world, $"КЛАНЫ · Бай {GuWorld.Creatures.Clans.Hostility("bai"):0} · Сюн {GuWorld.Creatures.Clans.Hostility("xiong"):0}",
                ("Бай +25", () => GuWorld.Creatures.Clans.Anger("bai", 25)), ("Сюн +25", () => GuWorld.Creatures.Clans.Anger("xiong", 25)),
                ("Мир", () => GuWorld.Creatures.Clans.Load(null)), ("Разбойники", Run("/masters demonic 2 2")));
            Buttons(world, "ОТЛАДКА",
                ("Убрать всех врагов", host.ClearWolves),
                ("Хитбоксы", Run("/hitboxes")),
                ("Задание выполнить", Run("/quest done")));
        }

        static void SetAll(Physique p, int level)
        {
            foreach (Trait trait in Enum.GetValues(typeof(Trait)))
            {
                p.Set(trait, level);
            }
        }
    }
}
