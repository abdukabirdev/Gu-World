using System;
using System.Collections.Generic;
using GuWorld.Cultivation;
using GuWorld.Items;
using GuWorld.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The hero's pages share a frame (UI-Bag and its siblings): the starry void, the header
    /// — a gold hexagon with the page's mark, its title and the rank under it, the pages'
    /// tabs between Q and E, the stones carried and a close button. This part holds the frame
    /// and the bag (UI-Bag.dc.html): forty octagon cells under the filters, the weight under
    /// them, and on a panel at the right the chosen thing — its mark floating in a glow, what
    /// it is, its numbers, and what can be done with it.
    /// </summary>
    public partial class Menus
    {
        const float BagCell = 72;
        const float BagGapX = 14;
        const float BagGapY = 12;

        readonly List<Cell> pageCells = new List<Cell>();
        int bagChosen = -1;
        /// <summary>The bag's filter: -1 all, else an ItemCategory.</summary>
        int bagFilter = -1;

        /// <summary>The stage of the hero's page on screen (its footer goes on it).</summary>
        RectTransform bookStage;

        /// <summary>The tabs of the hero's pages (the immortal's aperture has its own), each with its key.</summary>
        List<(string name, string key, Page page)> PageTabs() => host.Sandbox
            ? new List<(string, string, Page)> { ("Все Гу", "Tab", Page.Sandbox), ("Статус", "O", Page.Status), ("Карта", "M", Page.Map), ("Тело", "B", Page.Body) }
            : new List<(string, string, Page)>(Array.ConvertAll(UiKit.HeroPages, p => (p.name, p.key, p.page)));

        /// <summary>
        /// The frame of the hero's pages, on a stage in the design's units; returns the body to fill (1328 × 708 on
        /// the design's frame, wider on wider screens: 56 in from the sides, 116 from the top, 76 from the foot).
        /// </summary>
        RectTransform BookFrame(string icon, string title, Page page, string subtitle = null)
        {
            var stage = UiKit.Stage(pageRoot);
            bookStage = stage;
            UiKit.Backdrop(stage);
            var a = host.Cultivator.Aperture;
            var pages = PageTabs();
            var tabs = new List<(string, string, bool, Action)>();

            foreach (var (name, key, target) in pages)
            {
                var to = target;
                tabs.Add((name, key, target == page, () => host.OpenPage(to)));
            }

            UiKit.Header(stage, icon, title, subtitle ?? $"Ранг {Aperture.RankNumeral(a.Rank)} · {Aperture.Name(a.Stage)}", tabs, host.Resume, right =>
            {
                var stone = UiKit.Icon("stone", right, 16, UiKit.EssenceLit);
                stone.Size(20, 16, 0);
                var count = UiKit.Label($"{host.Cultivator.Stones}", right, 15, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                count.horizontalOverflow = HorizontalWrapMode.Overflow;
                count.Size(40, count.preferredWidth + 4, 0);
            });

            // Q and E: the page before and after.
            var index = pages.FindIndex(p => p.page == page);
            var keys = stage.gameObject.AddComponent<PageKeys>();

            if (index >= 0)
            {
                keys.On(Key.Q, () => host.OpenPage(pages[(index + pages.Count - 1) % pages.Count].page));
                keys.On(Key.E, () => host.OpenPage(pages[(index + 1) % pages.Count].page));
            }

            return UiKit.Rect("Body", stage).Place(Vector2.zero, Vector2.one, 56, 76, 56, 116);
        }

        /// <summary>Keys on the aperture page: 1–4 put the chosen Gu on that key.</summary>
        void UpdatePages()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null || (Current != Page.Bag && Current != Page.Aperture))
            {
                return;
            }

            var digits = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key };

            for (var i = 0; i < digits.Length; i++)
            {
                if (!digits[i].wasPressedThisFrame)
                {
                    continue;
                }

                var entry = ChosenEntry();

                if (entry != null)
                {
                    host.Cultivator.Bind(i, entry);
                    Notify($"На клавише {i + 1}: {EntryName(entry)}");
                    Show(Current);
                }
            }
        }

        /// <summary>What is chosen on the page, as an action bar entry (null: nothing that can go there).</summary>
        string ChosenEntry()
        {
            var gu = host.Cultivator;

            // Only Gu go on the keys (things are used from the bag; the stone has its own key).
            if (Current == Page.Bag)
            {
                return null;
            }

            var owned = gu.At(ringChosen);

            return owned != null && owned.Kind.Skill != SkillKind.None ? "gu:" + owned.kind : null;
        }

        static string EntryName(string entry) =>
            entry.StartsWith("gu:") ? GuBook.Find(entry.Substring(3))?.SkillName : ItemBook.Find(entry.Substring(5))?.Name;

        void BagPage()
        {
            var gu = host.Cultivator;
            var bag = gu.Bag;
            var body = BookFrame("bag", "Сумка", Page.Bag);

            if (bag[bagChosen] == null)
            {
                // Nothing chosen: the first thing in the bag.
                bagChosen = -1;

                for (var i = 0; i < Bag.Cells && bagChosen < 0; i++)
                {
                    if (bag[i] != null) bagChosen = i;
                }
            }

            // The filters, the cells and the weight, at the left.
            var gridWidth = Bag.Across * BagCell + (Bag.Across - 1) * BagGapX;
            var rows = Mathf.CeilToInt(Bag.Cells / (float)Bag.Across);
            var gridHeight = rows * BagCell + (rows - 1) * BagGapY;
            var left = UiKit.Rect("Things", body);
            left.TopLeft(0, 0, 700, 600);

            var filters = UiKit.Rect("Filters", left);
            filters.TopLeft(0, 0, 700, 34);
            var filterRow = filters.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            filterRow.childForceExpandWidth = false;
            var names = new[] { "Все", "Гу", "Еда", "Травы", "Материалы" };

            for (var i = 0; i < names.Length; i++)
            {
                var index = i;
                var tb = UiKit.Tb(names[i], filters, bagFilter + 1 == i, () =>
                {
                    bagFilter = index - 1;
                    Show(Page.Bag);
                });
                tb.Size(34, tb.Words.preferredWidth + names[i].Length * 12 * 0.16f + 30, 0);
            }

            UiKit.Rect("Space", filters).Size(34, -1, 1);
            UiKit.Tb("Упорядочить", filters, false, () =>
            {
                host.Cultivator.Bag.Sort();
                bagChosen = -1;
                Show(Page.Bag);
            }).Size(34, 150, 0);

            var grid = UiKit.Rect("Grid", left);
            grid.TopLeft(0, 50, gridWidth, gridHeight);
            var used = 0;

            for (var i = 0; i < Bag.Cells; i++)
            {
                var index = i;
                var cell = UiKit.Cell(grid, BagCell, () =>
                {
                    bagChosen = host.Cultivator.Bag[index] != null ? index : -1;
                    Show(Page.Bag);
                });
                cell.Back.rectTransform.anchorMin = cell.Back.rectTransform.anchorMax = new Vector2(0, 1);
                cell.Back.rectTransform.anchoredPosition = new Vector2(BagCell / 2 + i % Bag.Across * (BagCell + BagGapX), -(BagCell / 2 + i / Bag.Across * (BagCell + BagGapY)));
                ShowStack(cell, bag[i], i == bagChosen, host.Now);
                pageCells.Add(cell);
                used += bag[i] != null ? 1 : 0;
            }

            var weight = UiKit.Rect("Weight", left);
            weight.TopLeft(0, 50 + gridHeight + 22, gridWidth, 24);
            var weightRow = weight.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft);
            weightRow.childForceExpandWidth = false;
            UiKit.Lbl("Вес", weight).Size(24, 40, 0);
            var track = UiKit.Rect("Track", weight);
            track.Size(24, -1, 1);
            var bar = UiKit.Bar(track, UiKit.BarKind.Stamina, bag.Limit > 0 ? bag.Weight / bag.Limit : 0);
            bar.transform.parent.GetComponent<RectTransform>().Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -2, 0, -2);
            bar.transform.parent.GetComponent<LayoutElement>().ignoreLayout = true;
            var load = UiKit.Disp($"{bag.Weight:0.#} <size=14><color=#7E9093>/ {bag.Limit:0} кг</color></size>", weight, 18, bag.Weight > bag.Limit ? UiKit.Danger : UiKit.Text);
            load.horizontalOverflow = HorizontalWrapMode.Overflow;
            load.Size(24, 110, 0);
            UiKit.Label($"{used} из {Bag.Cells} ячеек", weight, 12, UiKit.Muted).Size(24, 110, 0);

            // The chosen thing, on a panel at the right.
            var panel = UiKit.Panel("Details", body);
            panel.rectTransform.Place(new Vector2(1, 0), Vector2.one, -560, 0, 0, 0);
            var details = UiKit.Rect("Column", panel.transform).Fill();
            details.Stack<VerticalLayoutGroup>(14, new RectOffset(28, 28, 26, 26));
            var chosen = bag[bagChosen];

            UiKit.Footer(bookStage, new[] { ("ЛКМ", "Выбрать"), ("Q+E", "Страница"), ("Esc", "Закрыть") });

            if (chosen == null)
            {
                UiKit.Disp("Сумка пуста", details, 30, UiKit.Text2).Size(40);
                UiKit.Label("Собирайте травы, покупайте в лавках, берите с поверженных зверей.", details, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(40);

                return;
            }

            var kind = chosen.Kind;
            var rankColour = kind.Rank > 0 ? Aperture.RankColour(kind.Rank) : UiKit.Gold;
            var head = UiKit.Rect("Head", details);
            head.Size(150);
            var art = UiKit.Rect("Art", head);
            art.TopLeft(0, 0, 150, 150);
            ShowArt(art, kind.Icon, rankColour);
            var title = UiKit.Rect("Title", head).Place(Vector2.zero, Vector2.one, 172, 0, 0, 0);
            title.Stack<VerticalLayoutGroup>(8, null, TextAnchor.MiddleLeft);
            UiKit.Lbl((kind.Rank > 0 ? $"{kind.Kind} · ранг {Aperture.RankNumeral(kind.Rank)}" : kind.Kind) ?? "", title, 11, rankColour, 0.3f).Size(14);
            var name = UiKit.Disp(kind.Name, title, 34);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiKit.Label($"Количество · <color=#EAF0F2>{chosen.count}</color>", title, 13, UiKit.Muted).Size(18);

            var about = UiKit.Label(kind.About, details, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.lineSpacing = 1.15f;
            about.Size(66);
            var stats = UiKit.Rect("Stats", details);
            stats.Stack<VerticalLayoutGroup>(0);
            Stat(stats, "Вес", $"{chosen.count * kind.Weight:0.##} кг");
            Stat(stats, "Цена", kind.Price > 0 ? (kind.Bundle > 1 ? $"{kind.Bundle} шт. — {ItemBook.Stones(kind.Price)}" : ItemBook.Stones(kind.Price)) : "—");
            Stat(stats, "Свежесть", chosen.spoils > 0 ? $"завянет через {Span(chosen.spoils - host.Now)}" : "не портится",
                chosen.spoils > 0 && chosen.spoils - host.Now < 24 * 60 ? UiKit.Hunger : (Color?)null);

            if (kind.Use == ItemUse.Feed)
            {
                Stat(stats, "Кто ест", kind.AnyGu ? "любой Гу — вместо одной порции" : FeedsWhom(kind.Id));
            }
            else if (kind.Use == ItemUse.Eat && kind.Heal > 0)
            {
                Stat(stats, "Даёт", $"+{kind.Heal:0} здоровья", UiKit.Ok);
            }
            else if (GuBook.Find(kind.Gu) is GuKind gk)
            {
                Stat(stats, "Навык", gk.SkillName);

                if (!gk.Passive)
                {
                    Stat(stats, "Вам стоит", $"{host.Cultivator.CostOf(gk):0.#}% эссенции", UiKit.EssenceLit);
                }
            }

            UiKit.Rect("Space", details).Size(-1).flexibleHeight = 1;
            var cellIndex = bagChosen;
            var actions = UiKit.Rect("Actions", details);
            var actionRow = actions.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft);
            actionRow.childForceExpandWidth = false;
            actions.Size(48);

            if (kind.Use != ItemUse.None)
            {
                var act = ActionName(kind, host.Cultivator);
                UiKit.HexButton(act, actions, () =>
                {
                    // A stone is drawn in over three seconds in the world: back to it.
                    if (host.Cultivator.UseCell(cellIndex) && kind.Use == ItemUse.Absorb)
                    {
                        host.Resume();
                    }
                    else
                    {
                        Show(Page.Bag);
                    }
                }, UiKit.HexKind.Solid, 12, 48).Size(48, Mathf.Min(300, 90 + act.Length * 10), 0);
            }

            UiKit.HexButton("Разделить", actions, () =>
            {
                host.Cultivator.Bag.Split(cellIndex);
                Show(Page.Bag);
            }, UiKit.HexKind.Ghost, 12, 48).Size(48, 150, 0);
            UiKit.HexButton("Выбросить", actions, () =>
                Ask("Выбросить?", $"{kind.Name} ×{chosen.count} исчезнет из сумки.", "Выбросить", () =>
                {
                    host.Drop(cellIndex);
                    bagChosen = -1;
                    Show(Page.Bag);
                }, Page.Bag, false), UiKit.HexKind.Bad, 12, 48).Size(48, 150, 0);
            UiKit.Label("Гу ставятся на клавиши 1–4 в апертуре (K). Камни поглощаются клавишей V.", details, 12, UiKit.Muted).Size(18);
        }

        /// <summary>A thing's mark, big: a glow of its colour, a dashed gold ring turning, the mark floating in it (150 across).</summary>
        static void ShowArt(RectTransform holder, string icon, Color colour)
        {
            UiKit.Image("Glow", holder, Icons.Tint(icon).A(0.45f), UiKit.Halo).rectTransform.Fill();
            var ring = UiKit.Image("Ring", holder, UiKit.Gold.A(0.25f), UiKit.RingLine(138, 6));
            ring.rectTransform.Fill(6);
            ring.gameObject.AddComponent<Spin>().Period = 60;
            var mark = UiKit.Icon(icon, holder, 84, Icons.Tint(icon));
            mark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
            var bob = mark.gameObject.AddComponent<Bob>();
            bob.Period = 6;
            bob.Rise = 8;
            var strip = UiKit.Image("Rank", holder, colour, UiKit.Diamond);
            strip.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(10, 10));
        }

        static string ActionName(ItemKind kind, Cultivator gu) => kind.Use switch
        {
            ItemUse.Refine => $"Очистить · {gu.RefineCostOf(GuBook.Find(kind.Gu)):0.#}%",
            ItemUse.Activate => $"{kind.Verb ?? "Применить"} · {gu.CostOf(GuBook.Find(kind.Gu)):0.#}%",
            ItemUse.Absorb => "Поглотить · V",
            _ when kind.Verb != null => kind.Verb,
            ItemUse.Eat => "Съесть",
            ItemUse.Feed => kind.AnyGu ? "Подкормить Гу" : "Накормить Гу",
            _ => "",
        };

        static string FeedsWhom(string food)
        {
            var who = new List<string>();

            foreach (var kind in GuBook.All)
            {
                if (kind.Food == food)
                {
                    who.Add(kind.Name);
                }
            }

            return who.Count > 0 ? string.Join(", ", who) : "—";
        }

        /// <summary>A pile in a cell: its icon, its count, its rank's strip, whether it is about to spoil; chosen or filtered out.</summary>
        void ShowStack(Cell cell, ItemStack stack, bool chosen, double now)
        {
            var kind = stack?.Kind;
            cell.SetIcon(kind?.Icon);
            cell.Count.text = stack != null && stack.count > 1 ? $"{stack.count}" : "";
            cell.SetRank(kind?.Rank ?? 0);
            cell.Dot.enabled = stack != null && stack.spoils > 0 && stack.spoils - now < 24 * 60;
            cell.Dot.color = UiKit.Hunger;
            cell.SetRim(chosen ? UiKit.Gold : UiKit.Line);

            if (chosen)
            {
                UiKit.Glow(cell.transform, UiKit.Gold.A(0.12f), 10);
                var inner = UiKit.Image("Inner", cell.transform, UiKit.Gold.A(0.16f), UiKit.Halo);
                inner.rectTransform.Fill(-4);
                inner.transform.SetSiblingIndex(1);
            }

            var shown = kind == null || bagFilter < 0 || (int)kind.Category == bagFilter;
            var alpha = shown ? 1 : 0.25f;
            cell.Mark.color = Icons.Tint(kind?.Icon).A(alpha);
            cell.Count.color = UiKit.Text2.A(alpha);
            cell.Strip.color = cell.Strip.color.A(alpha);
        }

        /// <summary>A line of numbers: a quiet name at the left, the value at the right, a hairline under it.</summary>
        static void Stat(Transform parent, string name, string value, Color? colour = null)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(36);
            UiKit.Label(name, row, 13, UiKit.Muted).rectTransform.Place(Vector2.zero, new Vector2(0.4f, 1));
            var text = UiKit.Label(value, row, 13, colour ?? UiKit.Text, TextAnchor.MiddleRight);
            text.rectTransform.Place(new Vector2(0.3f, 0), Vector2.one);
            var rule = UiKit.Image("Rule", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f), UiKit.Plain);
            rule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
        }

        /// <summary>A stretch of game time in words: "3 дн 4 ч", "5 ч 20 мин", "12 мин".</summary>
        public static string Span(double minutes)
        {
            if (minutes <= 0)
            {
                return "сейчас";
            }

            var total = (long)System.Math.Ceiling(minutes);
            var days = total / (24 * 60);
            var hours = total / 60 % 24;
            var mins = total % 60;

            return days > 0 ? (hours > 0 ? $"{days} дн {hours} ч" : $"{days} дн") : hours > 0 ? (mins > 0 ? $"{hours} ч {mins} мин" : $"{hours} ч") : $"{mins} мин";
        }
    }
}
