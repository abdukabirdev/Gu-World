using System.Collections.Generic;
using GuWorld.Cultivation;
using GuWorld.Items;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The fusion page (U, or from the aperture): every recipe the hero knows on the left — what Gu it makes and from
    /// what, ready ones marked — filtered by what can be fused now or by the result's rank; on the right the chosen one:
    /// the Gu it makes and what it does, each Gu and material it takes against what the hero has (the Gu from the bag
    /// and the ring, never the vital one), the essence it costs at once and the chance it holds — failed, all that was
    /// put in is lost — and the button that fuses them.
    /// </summary>
    public partial class Menus
    {
        int fusionFilter;
        int fusionChosen;
        float fusionScroll;

        static readonly Color Enough = UiKit.Hex(0x8FD0A0);

        /// <summary>How many of a Gu the hero has to put into a fusion: in the bag, and in the ring besides the vital one.</summary>
        int HaveForFusion(string id)
        {
            var gu = host.Cultivator;
            var kind = GuBook.Find(id);

            return kind == null ? 0 : gu.Bag.Count(kind.Item) + gu.Gu.FindAll(g => g.kind == id && !g.vital).Count;
        }

        static Dictionary<string, int> Needed(Recipe recipe)
        {
            var counts = new Dictionary<string, int>();

            foreach (var id in recipe.Gu)
            {
                counts[id] = counts.TryGetValue(id, out var n) ? n + 1 : 1;
            }

            return counts;
        }

        bool Shown(Recipe recipe)
        {
            var result = GuBook.Find(recipe.Result);

            return fusionFilter switch
            {
                1 => host.Cultivator.Lacks(recipe) == null,
                2 => result.Rank == 2,
                3 => result.Rank == 3,
                4 => result.Rank >= 4,
                _ => true,
            };
        }

        void FusionPage()
        {
            var gu = host.Cultivator;
            var body = BookFrame("ring", "Слияние", Page.Fusion);
            var brush = UiKit.Brush("煉", bookStage, 620, UiKit.Gold.A(0.04f));
            brush.rectTransform.anchorMin = brush.rectTransform.anchorMax = new Vector2(0.5f, 1);
            brush.rectTransform.anchoredPosition = new Vector2(0, -60 - 310);
            brush.transform.SetSiblingIndex(1);

            // The recipe book, at the left.
            var left = UiKit.Rect("Recipes", body);
            left.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -330, 0);
            left.Stack<VerticalLayoutGroup>(10);
            var ready = System.Array.FindAll(GuBook.Recipes, r => gu.Lacks(r) == null).Length;
            var bookHead = UiKit.Rect("Head", left);
            bookHead.Size(22);
            UiKit.Lbl("Книга рецептов", bookHead).rectTransform.Fill();
            UiKit.Disp($"<color=#9FEFC4>{ready}</color> / {GuBook.Recipes.Length}", bookHead, 20, UiKit.Muted, TextAnchor.MiddleRight).rectTransform.Fill();
            var filters = UiKit.Rect("Filters", left);
            var filterRow = filters.Stack<HorizontalLayoutGroup>(16, new RectOffset(4, 4, 0, 0), TextAnchor.MiddleLeft);
            filterRow.childForceExpandWidth = false;
            filters.Size(30);
            var names = new[] { "Все", "Готовы", "II", "III", "IV+" };

            for (var i = 0; i < names.Length; i++)
            {
                var index = i;
                TextTab(filters, names[i], i == fusionFilter, () =>
                {
                    fusionFilter = index;
                    fusionScroll = 0;
                    Show(Page.Fusion);
                });
            }

            UiKit.Image("Rule", left, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), UiKit.Plain).Size(1);
            var list = Scroll(left, fusionScroll, y => fusionScroll = y);
            list.GetComponent<VerticalLayoutGroup>().spacing = 2;
            var shown = new List<int>();

            for (var i = 0; i < GuBook.Recipes.Length; i++)
            {
                if (Shown(GuBook.Recipes[i]))
                {
                    shown.Add(i);
                }
            }

            if (shown.Count > 0 && !shown.Contains(fusionChosen))
            {
                fusionChosen = shown[0];
            }

            foreach (var index in shown)
            {
                var recipe = GuBook.Recipes[index];
                var result = GuBook.Find(recipe.Result);
                var can = gu.Lacks(recipe) == null;
                var i = index;
                var on = index == fusionChosen;
                var (row, _) = UiKit.ListRow(list, 62, on, () =>
                {
                    fusionChosen = i;
                    Show(Page.Fusion);
                });
                var line = row.Stack<HorizontalLayoutGroup>(12, new RectOffset(22, 12, 0, 0), TextAnchor.MiddleLeft);
                line.childForceExpandWidth = false;
                var words = UiKit.Rect("Words", row);
                words.Stack<VerticalLayoutGroup>(4, null, TextAnchor.MiddleLeft);
                words.Size(62, -1, 1);
                var title = UiKit.Disp(result.Name, words, 20, on ? Color.white : UiKit.Text2);
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                title.Size(22);
                UiKit.Lbl(result.Kind ?? "", words, 10, UiKit.Muted, 0.18f).Size(12);
                UiKit.Disp(Aperture.RankNumeral(result.Rank), row, 18, Aperture.RankColour(result.Rank), TextAnchor.MiddleRight).Size(62, 30, 0);
                var dot = UiKit.Image("Ready", row, can ? UiKit.Hex(0x8FE0B0) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.15f), UiKit.Disc);
                dot.Size(8, 8, 0);
            }

            if (shown.Count == 0)
            {
                UiKit.Label(fusionFilter == 1 ? "Пока ни одно слияние не готово: соберите Гу и материалы." : "Здесь пусто.", list, 14, UiKit.Muted, TextAnchor.UpperLeft).Size(40);
                UiKit.Footer(bookStage, new[] { ("Q+E", "Страница"), ("Esc", "Закрыть") });

                return;
            }

            var chosen = GuBook.Recipes[fusionChosen];
            var made = GuBook.Find(chosen.Result);
            var lacks = gu.Lacks(chosen);
            var cost = gu.FuseCost(chosen);
            var essence = gu.EndlessEssence || gu.Aperture.Essence >= cost;
            var able = lacks == null && essence;
            var chance = gu.FuseChance(chosen);

            // The altar, in the middle: slow gold rings, the chance as a gold arc, the Gu to be born floating in its glow, what goes in round it.
            var altar = UiKit.Rect("Altar", body);
            altar.anchorMin = altar.anchorMax = new Vector2(0.5f, 1);
            altar.pivot = new Vector2(0.5f, 1);
            altar.anchoredPosition = new Vector2(0, 12);
            altar.sizeDelta = new Vector2(500, 500);
            var outer = UiKit.Orbit(altar, 500, UiKit.Gold.A(0.2f), 160, 0);
            var rider = UiKit.Image("Rider", outer.transform, UiKit.Gold, UiKit.Diamond);
            rider.rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(9, 9));
            UiKit.Orbit(altar, 400, UiKit.Gold.A(0.16f), -90, 4);
            var arcTrack = UiKit.Image("Track", altar, new Color(1, 1, 1, 0.05f), UiKit.ArcRing(455, 3));
            arcTrack.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(455, 455));
            var arc = UiKit.Image("Chance", altar, UiKit.Gold, UiKit.ArcRing(455, 3));
            Radial(arc);
            arc.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(455, 455));
            arc.fillAmount = Mathf.Clamp01(chance);
            var aura = UiKit.Image("Aura", altar, made.Colour.A(0.5f), UiKit.Radial);
            aura.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 460));
            var breathe = aura.gameObject.AddComponent<Pulse>();
            breathe.Period = 5;
            breathe.Low = 0.5f;
            breathe.High = 0.95f;
            breathe.Grow = 0.06f;
            var art = UiKit.Icon(made.Icon, altar, 180, made.Colour);
            art.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(180, 180));
            var bob = art.gameObject.AddComponent<Bob>();
            bob.Period = 6;
            bob.Rise = 10;

            var inputs = new List<(string icon, Color colour, string name, int need, int have)>();

            foreach (var pair in Needed(chosen))
            {
                var kind = GuBook.Find(pair.Key);
                inputs.Add((kind.Icon, kind.Colour, kind.Name, pair.Value, HaveForFusion(pair.Key)));
            }

            foreach (var (item, count) in chosen.Materials)
            {
                var what = ItemBook.Find(item);
                inputs.Add((what?.Icon ?? "bag", Icons.Tint(what?.Icon), what?.Name ?? item, count, gu.Bag.Count(item)));
            }

            for (var k = 0; k < inputs.Count; k++)
            {
                // From the left round the bottom to the right, then the top.
                var angle = (180 + k * 360f / Mathf.Max(2, inputs.Count)) * Mathf.Deg2Rad;
                var at = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 196;
                var (icon, colour, name, need, have) = inputs[k];
                var enough = have >= need;
                var disc = UiKit.Node(altar, 70, enough ? UiKit.Gold : UiKit.Danger);
                var node = (RectTransform)disc.transform.parent;
                node.anchoredPosition = at;
                UiKit.HaloAt(node, colour.A(0.4f), Vector2.zero, 110).transform.SetAsFirstSibling();
                var mark = UiKit.Icon(icon, node, 44, colour);
                mark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
                var counts = UiKit.Label($"{Mathf.Min(have, need)}/{need}", node, 11, enough ? UiKit.Ok : UiKit.Danger, TextAnchor.UpperCenter, FontStyle.Bold).Shadowed();
                counts.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(80, 14));
                var label = UiKit.Label(GuSlot.Short(name), node, 11, UiKit.Text2, TextAnchor.UpperCenter).Shadowed();
                label.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -64), new Vector2(110, 14));
            }

            // What is born and the fusing, under the altar.
            var born = UiKit.Rect("Born", body);
            born.anchorMin = born.anchorMax = new Vector2(0.5f, 1);
            born.pivot = new Vector2(0.5f, 1);
            born.anchoredPosition = new Vector2(0, -496);
            born.sizeDelta = new Vector2(600, 200);
            var bornName = UiKit.Disp(made.Name, born, 44, Color.white, TextAnchor.UpperCenter);
            bornName.Gradient(Color.white, UiKit.Hex(0xC9B590), false, UiKit.Hex(0xF2EADB), 0.45f);
            bornName.rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -50, 0, 0);
            UiKit.Lbl($"Ранг {Aperture.RankNumeral(made.Rank)} · {made.Kind} · {made.Line}", born, 11, Aperture.RankColour(made.Rank), 0.24f, TextAnchor.MiddleCenter)
                .rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -70, 0, 54);
            var recipeNow = chosen;
            var fuse = UiKit.HexButton(able ? "Слить" : lacks != null ? "Не хватает" : "Не хватает эссенции", born, () =>
            {
                var done = host.Cultivator.Fuse(recipeNow);
                Show(Page.Fusion);
                Banner(done ? "Слияние удалось" : "Слияние сорвалось", done ? $"{made.Name} — в сумке" : "Гу и материалы погибли");
            }, able ? UiKit.HexKind.Solid : UiKit.HexKind.Off, 13, 56, able ? "Пробел" : null);
            ((RectTransform)fuse.transform).At(new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(300, 56));

            if (able)
            {
                fuse.Held(1.2f, UnityEngine.InputSystem.Key.Space);
                UiKit.Label("Удерживайте — слияние необратимо", born, 11, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -174, 0, 156);
            }
            else if (lacks != null)
            {
                UiKit.Label($"Не хватает: {lacks}", born, 12, UiKit.Danger, TextAnchor.MiddleCenter).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -178, 0, 154);
            }

            // The rite's numbers, at the right.
            var right = UiKit.Rect("Rite", body);
            right.Place(new Vector2(1, 0), Vector2.one, -320, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(20);
            var odds = UiKit.Rect("Chance", right);
            odds.Stack<VerticalLayoutGroup>(4);
            odds.Size(116);
            UiKit.Lbl("Шанс успеха", odds).Size(14);
            var number = UiKit.Disp($"{chance * 100:0}<size=40>%</size>", odds, 92, UiKit.GoldLit, TextAnchor.LowerLeft);
            number.Gradient(UiKit.Hex(0xFFF6E2), UiKit.Hex(0x9C7A44), false, UiKit.Gold, 0.6f);
            number.Size(96);

            if (chance >= 1)
            {
                UiKit.Label("Гу уверенности: слияние не сорвётся", right, 12, UiKit.Ok).Size(16);
            }

            var sea = UiKit.Rect("Essence", right);
            sea.Stack<VerticalLayoutGroup>(10);
            sea.Size(44);
            var seaHead = UiKit.Rect("Head", sea);
            seaHead.Size(18);
            UiKit.Lbl("Эссенция", seaHead).rectTransform.Fill();
            var left2 = gu.Aperture.Essence - cost;
            UiKit.Label($"<color={(essence ? "#FFE6B0" : "#F08A74")}>−{cost:0.#}%</color> · останется {Mathf.Max(0, left2):0}%", seaHead, 14, UiKit.Text, TextAnchor.MiddleRight).rectTransform.Fill();
            Segments(sea, gu.Aperture.Essence / 100, cost / 100);

            var aboutBox = UiKit.Rect("About", right);
            aboutBox.Stack<VerticalLayoutGroup>(8);
            aboutBox.Size(160);
            UiKit.Lbl("О Гу", aboutBox).Size(14);
            var about = UiKit.Label($"{ItemBook.Find(made.Item)?.About ?? made.About ?? ""} <color=#E2D9C6>{made.SkillName}</color> — {SkillLine(made)}.", aboutBox, 13, UiKit.Hex(0xA39B89), TextAnchor.UpperLeft);
            about.lineSpacing = 1.1f;
            about.Size(130);

            var warn = UiKit.Image("Warning", right, UiKit.Danger.A(0.05f), UiKit.Plain);
            UiKit.Frame(warn.transform, UiKit.Danger.A(0.3f));
            warn.Size(56);
            UiKit.Label("Сорвётся — погибнут и Гу, и материалы. Рождённый Гу ляжет в сумку: очистите его в апертуру.", warn.transform, 12, UiKit.Hex(0xE8B0A4)).rectTransform.Fill(12);

            UiKit.Footer(bookStage, new[] { ("ЛКМ", "Рецепт"), ("Пробел", "Удерживать — слить"), ("Q+E", "Страница"), ("Esc", "Закрыть") });
        }

        /// <summary>A text tab (the recipe book's filters): spaced capitals; chosen — warm gold with a gold line under it.</summary>
        static void TextTab(Transform parent, string name, bool on, System.Action click)
        {
            var tab = UiKit.Label(name.ToUpperInvariant(), parent, 12, on ? UiKit.GoldText : UiKit.Muted, TextAnchor.MiddleCenter);
            tab.Track(0.16f);
            tab.raycastTarget = true;
            tab.Size(30);

            if (on)
            {
                var under = UiKit.Image("Under", tab.transform, UiKit.Gold, UiKit.Plain);
                under.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            }

            var clickable = tab.gameObject.AddComponent<Clickable>();
            clickable.Click = click;

            if (!on)
            {
                clickable.Pointed = over => tab.color = over ? UiKit.Text : UiKit.Muted;
            }
        }

        /// <summary>The sea as 25 segments: full ones essence-green, the ones the fusion would take warm gold, the empty faint.</summary>
        static void Segments(Transform parent, float full, float take)
        {
            var row = UiKit.Rect("Segments", parent);
            var group = row.Stack<HorizontalLayoutGroup>(3);
            group.childForceExpandWidth = true;
            row.Size(10);
            const int count = 25;

            for (var i = 0; i < count; i++)
            {
                var at = (i + 0.5f) / count;
                var colour = at <= full - take ? UiKit.Essence : at <= full ? UiKit.Hex(0xFFE6B0) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f);
                UiKit.Image("Segment", row, colour, UiKit.Plain).Size(10, -1, 1);
            }
        }
    }
}
