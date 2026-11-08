using System.Collections.Generic;
using GuWorld.Cultivation;
using GuWorld.Items;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The Gu codex (from the main menu, UI-Codex.dc.html): every Gu the game knows as cards in
    /// a grid, filtered by rank; the chosen one at the right — its mark floating in its glow
    /// inside a dashed gold ring, its rank and path, its name, what the lore says it does, what
    /// it eats and where to get it, its skill and the fusions it goes into.
    /// </summary>
    public partial class Menus
    {
        float codexScroll;

        void Codex()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "蛊", 420, new Vector2(-30 - 210, -100 - 210));
            UiKit.Header(stage, null, "Кодекс Гу", $"{GuBook.Count} Гу в книге", null, () => Show(Page.Title), null, "蛊");
            var body = UiKit.Rect("Body", stage).Place(Vector2.zero, Vector2.one, 56, 76, 56, 108);

            // The cards, at the left.
            var left = UiKit.Rect("All", body);
            left.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -760, 0);
            left.Stack<VerticalLayoutGroup>(14);
            var filters = UiKit.Rect("Filters", left);
            var filterRow = filters.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            filterRow.childForceExpandWidth = false;
            filters.Size(34);
            UiKit.Lbl("Ранг", filters).Size(34, 56, 0);

            for (var rank = 0; rank <= 5; rank++)
            {
                var r = rank;
                UiKit.Tb(rank == 0 ? "Все" : Aperture.RankNumeral(rank), filters, rank == codexRank, () =>
                {
                    codexRank = r;
                    codexChosen = null;
                    codexScroll = 0;
                    Show(Page.Codex);
                }).Size(34, rank == 0 ? 64 : 48, 0);
            }

            var shown = new List<GuKind>();

            foreach (var kind in GuBook.All)
            {
                if (codexRank == 0 || kind.Rank == codexRank)
                {
                    shown.Add(kind);
                }
            }

            GuKind chosen = shown.Count > 0 ? shown[0] : null;

            foreach (var kind in shown)
            {
                if (kind.Id == codexChosen)
                {
                    chosen = kind;
                }
            }

            UiKit.Rect("Space", filters).Size(34, -1, 1);
            UiKit.Label($"{shown.Count} из {GuBook.Count}", filters, 12, UiKit.Muted, TextAnchor.MiddleRight).Size(34, 90, 0);

            var grid = CardScroll(left, codexScroll, y => codexScroll = y);

            foreach (var kind in shown)
            {
                var k = kind;
                var on = kind == chosen;
                var card = UiKit.Image(kind.Name, grid, on ? UiKit.GoldLit.A(0.08f) : new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.6f), UiKit.Plain);
                card.raycastTarget = true;
                var edge = UiKit.Frame(card.transform, on ? UiKit.GoldLit.A(0.6f) : new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));
                var rankColour = Aperture.RankColour(kind.Rank);
                var gem = UiKit.Image("Rank", card.transform, rankColour, UiKit.Diamond);
                gem.rectTransform.At(new Vector2(0, 1), new Vector2(14, -14), new Vector2(8, 8));
                var mark = UiKit.Icon(kind.Icon, card.transform, 56, kind.Colour);
                mark.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(56, 56));
                var name = UiKit.Disp(GuSlot.Short(kind.Name), card.transform, 14, on ? Color.white : UiKit.Text2, TextAnchor.UpperCenter);
                name.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 6, 8, 6, -34);
                var click = card.gameObject.AddComponent<Clickable>();
                click.Click = () =>
                {
                    codexChosen = k.Id;
                    Show(Page.Codex);
                };

                if (!on)
                {
                    click.Pointed = over => edge.color = over ? UiKit.Gold.A(0.4f) : new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f);
                }
            }

            // The chosen Gu, at the right.
            if (chosen != null)
            {
                CodexCard(body, chosen);
            }

            UiKit.Footer(stage, new[] { ("ЛКМ", "Открыть"), ("Колесо", "Листать"), ("Esc", "Назад") });
        }

        void CodexCard(RectTransform body, GuKind chosen)
        {
            var item = ItemBook.Find(chosen.Item);
            var food = ItemBook.Find(chosen.Food);
            var right = UiKit.Rect("Chosen", body);
            right.Place(Vector2.zero, Vector2.one, 804, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(10);

            var art = UiKit.Rect("Art", right);
            art.Size(250);
            var holder = UiKit.Rect("Holder", art);
            holder.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 240));
            UiKit.Orbit(holder, 240, UiKit.Gold.A(0.2f), 80, 6);
            UiKit.Image("Glow", holder, chosen.Colour.A(0.45f), UiKit.Radial).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300));
            var mark = UiKit.Icon(chosen.Icon, holder, 150, chosen.Colour);
            mark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            var bob = mark.gameObject.AddComponent<Bob>();
            bob.Period = 6;
            bob.Rise = 10;

            UiKit.Lbl($"Ранг {Aperture.RankNumeral(chosen.Rank)} · {chosen.Line}", right, 11, Aperture.RankColour(chosen.Rank), 0.3f).Size(14);
            var name = UiKit.Disp(chosen.Name, right, 40);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            var about = UiKit.Label(item?.About ?? chosen.About ?? "", right, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.lineSpacing = 1.15f;

            var boxes = UiKit.Rect("Boxes", right);
            var boxRow = boxes.Stack<HorizontalLayoutGroup>(10);
            boxRow.childForceExpandWidth = true;
            boxes.Size(58);
            Fact(boxes, "Вид", chosen.Kind ?? "—");
            Fact(boxes, "Ест", food != null ? $"{food.Name.ToLowerInvariant()} ×{chosen.Meal}" : "не ест");
            Fact(boxes, "Где взять", item != null && item.Price > 0 ? $"лавка · {ItemBook.Stones(item.Price)}" : "не продаётся");

            var stats = UiKit.Rect("Stats", right);
            stats.Stack<VerticalLayoutGroup>(0);
            Stat(stats, "Навык", chosen.SkillName, UiKit.GoldLit);
            UiKit.Label(SkillLine(chosen), right, 12, UiKit.Muted, TextAnchor.UpperLeft);

            if (food != null && chosen.MealEvery > 0)
            {
                Stat(stats, "Кормить", MealLine(chosen.MealEvery));
            }

            if (!chosen.OneUse)
            {
                Stat(stats, "Очищение", $"{chosen.RefineCost:0}% эссенции · усталость {chosen.Tire:0} за раз");
            }

            foreach (var recipe in GuBook.RecipesFor(chosen.Id))
            {
                UiKit.Label(RecipeLine(recipe), right, 12, UiKit.EssenceLit, TextAnchor.UpperLeft);
            }

            UiKit.Rect("Space", right).Size(-1).flexibleHeight = 1;
            UiKit.Label($"Запись лор-базы: {chosen.LoreRef}", right, 12, UiKit.Hex(0x4E5E61)).Size(16);
        }

        /// <summary>A small framed fact (the codex's "Вид", "Ест", "Где взять").</summary>
        static void Fact(Transform parent, string name, string value)
        {
            var box = UiKit.Image(name, parent, new Color(0, 0, 0, 0), UiKit.Plain);
            UiKit.Frame(box.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f));
            box.Size(58, -1, 1);
            UiKit.Lbl(name, box.transform, 10, UiKit.Muted, 0.24f).rectTransform.Place(new Vector2(0, 1), Vector2.one, 12, -24, 12, 10);
            var text = UiKit.Label(value, box.transform, 13, UiKit.Text);
            text.rectTransform.Place(Vector2.zero, new Vector2(1, 1), 12, 8, 12, 26);
        }

        /// <summary>A scrolling grid of cards, five across, scrolled `down` points from its top at first.</summary>
        static RectTransform CardScroll(Transform parent, float down, System.Action<float> scrolled)
        {
            var view = UiKit.Rect("View", parent);
            view.Size(-1).flexibleHeight = 1;
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var content = UiKit.Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(144, 132);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            content.anchoredPosition = new Vector2(0, down);
            scroll.onValueChanged.AddListener(_ => scrolled(content.anchoredPosition.y));

            return content;
        }
    }
}
