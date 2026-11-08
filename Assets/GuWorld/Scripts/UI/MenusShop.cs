using System.Collections.Generic;
using GuWorld.Cultivation;
using GuWorld.Items;
using GuWorld.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// A shop (the design's "Лавка торговца"): the keeper and what they say on
    /// the left, and under them the thing chosen in the list (a click on its
    /// row) — what it is and what it does; buying or selling in the middle — the
    /// goods by kind, how many are left, the price, and how many to take; the
    /// deal on the right — what is in it, its sum, and the stones left after it.
    /// </summary>
    public partial class Menus
    {
        bool shopSelling;
        int shopFilter = -1;
        readonly Dictionary<string, int> shopLots = new Dictionary<string, int>();
        /// <summary>What is being sold: a bag cell → (the thing's id, how many).</summary>
        readonly Dictionary<int, (string id, int count)> shopSales = new Dictionary<int, (string, int)>();
        /// <summary>The thing whose details show under the keeper (null: the first of the goods).</summary>
        string shopChosen;
        /// <summary>How far down the goods the list is scrolled (the page is built again on every click: it stays there).</summary>
        float shopScroll;

        /// <summary>Opens the shop the hero is at, with an empty deal.</summary>
        public void OpenShop()
        {
            shopSelling = false;
            shopFilter = -1;
            shopLots.Clear();
            shopSales.Clear();
            shopChosen = null;
            shopScroll = 0;
            Show(Page.Shop);
        }

        void ShopPage()
        {
            var shop = host.Shop;

            if (shop == null)
            {
                host.Resume();

                return;
            }

            var gu = host.Cultivator;
            var now = host.Now;
            var discount = shop.ForMerit ? 0 : host.Quests.Discount;
            var purseNow = shop.ForMerit ? host.Quests.Merit : gu.Stones;
            shopSelling &= shop.Buys;
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "商", 460, new Vector2(-60 - 230, -160 - 230));

            // The header: the shop, buying or selling between Q and E, the purse.
            var tabs = new List<(string, string, bool, System.Action)> { ("Купить", null, !shopSelling, () => { shopSelling = false; shopScroll = 0; Show(Page.Shop); }) };

            if (shop.Buys)
            {
                tabs.Add(("Продать", null, shopSelling, () => { shopSelling = true; shopScroll = 0; Show(Page.Shop); }));
            }

            UiKit.Header(stage, "coin", "Лавка", $"{shop.Keeper} · деревня Гу Юэ", tabs, host.Resume, right =>
            {
                var mark = UiKit.Icon(shop.ForMerit ? "jade" : "stone", right, 16, shop.ForMerit ? UiKit.Gold : UiKit.EssenceLit);
                mark.Size(20, 16, 0);
                var count = UiKit.Label(shop.ForMerit ? $"{purseNow} вклада" : $"{purseNow}", right, 15, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                count.horizontalOverflow = HorizontalWrapMode.Overflow;
                count.Size(40, count.preferredWidth + 4, 0);
            });

            if (shop.Buys)
            {
                var keys = stage.gameObject.AddComponent<PageKeys>();
                UnityEngine.InputSystem.Key[] toggles = { UnityEngine.InputSystem.Key.Tab, UnityEngine.InputSystem.Key.Q, UnityEngine.InputSystem.Key.E };

                foreach (var key in toggles)
                {
                    keys.On(key, () =>
                    {
                        shopSelling = !shopSelling;
                        shopScroll = 0;
                        Show(Page.Shop);
                    });
                }
            }

            var body = UiKit.Rect("Body", stage).Place(Vector2.zero, Vector2.one, 56, 76, 56, 108);

            // The keeper, at the left.
            var left = UiKit.Rect("Keeper", body);
            left.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -300, 0);
            left.Stack<VerticalLayoutGroup>(14);
            var portrait = UiKit.Rect("Portrait", left);
            portrait.Size(190);
            var glow = UiKit.Image("Glow", portrait, new Color(1, 190 / 255f, 110 / 255f, 0.28f), UiKit.Radial);
            glow.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 320));
            var flicker = glow.gameObject.AddComponent<Pulse>();
            flicker.Period = 4;
            flicker.Low = 0.7f;
            flicker.High = 1;
            UiKit.Orbit(portrait, 170, UiKit.Gold.A(0.2f), 90, 6);
            var seal = UiKit.Zh("商", portrait, 96, UiKit.Gold.A(0.8f), TextAnchor.MiddleCenter);
            seal.rectTransform.Fill();
            UiKit.Disp(shop.Keeper, left, 28).Size(30);
            UiKit.Lbl($"{shop.Title} · {host.Quests.Standing}", left, 11, UiKit.Muted, 0.24f).Size(14);
            var hello = UiKit.Disp($"«{shop.Hello}»", left, 19, UiKit.Hex(0xE2D9C6), TextAnchor.UpperLeft);
            hello.lineSpacing = 1.05f;
            UiKit.Rect("Space", left).Size(-1).flexibleHeight = 1;
            var chosenBox = UiKit.Rect("Chosen", left);
            chosenBox.Stack<VerticalLayoutGroup>(6, new RectOffset(0, 0, 14, 0));
            UiKit.Image("Rule", chosenBox, UiKit.Gold.A(0.18f), UiKit.Plain).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -1, 0, 0);
            chosenBox.GetChild(0).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Goods(chosenBox, ItemBook.Find(shopChosen ?? (shop.Goods.Length > 0 ? shop.Goods[0].Item : null)));

            // The goods (or the bag, selling), in the middle.
            var centre = UiKit.Rect("Goods", body);
            centre.Place(Vector2.zero, Vector2.one, 334, 0, 360, 0);
            centre.Stack<VerticalLayoutGroup>(10);
            var filters = UiKit.Rect("Filters", centre);
            var filterRow = filters.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            filterRow.childForceExpandWidth = false;
            filters.Size(34);
            var names = new[] { "Все", "Гу", "Корм и еда", "Материалы" };

            for (var i = 0; i < names.Length; i++)
            {
                var index = i;
                var tb = UiKit.Tb(names[i], filters, shopFilter + 1 == i, () =>
                {
                    shopFilter = index - 1;
                    shopScroll = 0;
                    Show(Page.Shop);
                });
                tb.Size(34, tb.Words.preferredWidth + names[i].Length * 12 * 0.16f + 30, 0);
            }

            var header = UiKit.Rect("Header", centre);
            var headerRow = header.Stack<HorizontalLayoutGroup>(16, new RectOffset(66, 6, 0, 0), TextAnchor.MiddleLeft);
            headerRow.childForceExpandWidth = false;
            header.Size(32);
            var headerRule = UiKit.Image("Rule", header, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.12f), UiKit.Plain);
            headerRule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            headerRule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Lbl("Товар", header, 10, UiKit.Muted, 0.28f).Size(32, -1, 1);
            UiKit.Lbl(shopSelling ? "В сумке" : "Есть", header, 10, UiKit.Muted, 0.28f).Size(32, 70, 0);
            UiKit.Lbl("Цена", header, 10, UiKit.Muted, 0.28f).Size(32, 110, 0);
            UiKit.Lbl("", header, 10).Size(32, 110, 0);
            var rows = Scroll(centre, shopScroll, v => shopScroll = v);
            rows.GetComponent<VerticalLayoutGroup>().spacing = 0;

            if (!shopSelling)
            {
                foreach (var goods in shop.Goods)
                {
                    var kind = ItemBook.Find(goods.Item);

                    if (kind != null && Shows(kind) && Trade.OutToday(shop, goods, now))
                    {
                        BuyRow(rows, shop, goods, kind, now, discount);
                    }
                }
            }
            else
            {
                var any = false;

                for (var cell = 0; cell < Bag.Cells; cell++)
                {
                    var stack = gu.Bag[cell];
                    var kind = stack?.Kind;

                    if (kind != null && kind.Id != ItemBook.Stone && Trade.SellPrice(kind, kind.Bundle) > 0 && Shows(kind))
                    {
                        SellRow(rows, cell, stack, kind);
                        any = true;
                    }
                }

                if (!any)
                {
                    UiKit.Label("Продать нечего: лавка берёт только то, что сама продаёт по цене не меньше камня за лот.", rows, 14, UiKit.Muted, TextAnchor.UpperLeft).Size(50);
                }
                else
                {
                    // All the spoils of the hunt (fangs, bones, meat…) into the deal at once.
                    UiKit.Tb("Вся добыча в сделку", rows, false, () =>
                    {
                        for (var cell = 0; cell < Bag.Cells; cell++)
                        {
                            var stack = gu.Bag[cell];
                            var kind = stack?.Kind;

                            if (kind != null && kind.Category == ItemCategory.Material && kind.Id != ItemBook.Stone && Trade.SellPrice(kind, kind.Bundle) > 0)
                            {
                                shopSales[cell] = (stack.id, stack.count / kind.Bundle * kind.Bundle);
                            }
                        }

                        Show(Page.Shop);
                    }, 11).Size(40);
                }
            }

            // The deal, on a panel at the right.
            var right = UiKit.Panel("Deal", body);
            right.rectTransform.Place(new Vector2(1, 0), Vector2.one, -320, 0, 0, 0);
            var deal = UiKit.Rect("Column", right.transform).Fill();
            deal.Stack<VerticalLayoutGroup>(10, new RectOffset(24, 24, 24, 24));
            UiKit.Lbl("Сделка", deal).Size(14);
            var total = 0;
            var empty = true;

            if (!shopSelling)
            {
                foreach (var pair in shopLots)
                {
                    if (pair.Value <= 0)
                    {
                        continue;
                    }

                    var kind = ItemBook.Find(pair.Key);
                    empty = false;
                    DealLine(deal, $"{kind.Name} × {pair.Value * kind.Bundle}", Trade.PriceOf(shop, pair.Key, now, discount) * pair.Value, shop.ForMerit);
                }

                total = Trade.Price(shop, shopLots, now, discount);
            }
            else
            {
                foreach (var pair in shopSales)
                {
                    var kind = ItemBook.Find(pair.Value.id);
                    var got = Trade.SellPrice(kind, pair.Value.count);
                    empty = false;
                    DealLine(deal, $"{kind.Name} × {pair.Value.count}", got);
                    total += got;
                }
            }

            if (empty)
            {
                UiKit.Label("Нажмите «+» у товара — он попадёт в сделку.", deal, 14, UiKit.Muted, TextAnchor.UpperLeft).Size(40);
            }

            UiKit.Rect("Space", deal).Size(-1).flexibleHeight = 1;
            UiKit.Image("Rule", deal, UiKit.Gold.A(0.2f), UiKit.Plain).Size(1);
            var left2 = shopSelling ? purseNow + total : purseNow - total;
            string Money(int n) => shop.ForMerit ? $"{n} вклада" : ItemBook.Price(n);
            var sum = UiKit.Rect("Total", deal);
            sum.Size(46);
            UiKit.Label(shopSelling ? "Получите" : "Итого", sum, 13, UiKit.Muted, TextAnchor.LowerLeft).rectTransform.Fill(4);
            UiKit.Disp(Money(total), sum, 36, UiKit.GoldLit, TextAnchor.LowerRight).rectTransform.Fill();

            if (!shop.ForMerit && !shopSelling)
            {
                Stat(deal, "Скидка клана", discount > 0 ? $"−{discount * 100:0}%" : "нет · растёт с вкладом", discount > 0 ? UiKit.Ok : (Color?)null);
            }

            Stat(deal, "Останется", left2 >= 0 ? Money(left2) : $"не хватает {Money(-left2)}", left2 >= 0 ? (Color?)null : UiKit.Danger);
            UiKit.HexButton(shopSelling ? "Продать" : "Купить", deal, () =>
            {
                if (shopSelling)
                {
                    SellAll();
                }
                else if (host.Buy(shopLots) == Deal.Done)
                {
                    shopLots.Clear();
                }

                Show(Page.Shop);
            }, empty || left2 < 0 ? UiKit.HexKind.Off : UiKit.HexKind.Main, 13, 52).Size(52);
            UiKit.Label(shop.ForMerit
                    ? "Платят вкладом: его дают поручения клана и побочные задания. Товар пополняется в полночь."
                    : shop.GuADay > 0
                        ? $"Покупает за половину цены. Товар — в полночь; Гу в день выставлено {shop.GuADay}, цены на них гуляют."
                        : "Покупает за половину своей цены. Товар пополняется в полночь.", deal, 11, UiKit.Muted, TextAnchor.UpperLeft).Size(30);

            UiKit.Footer(stage, shop.Buys ? new[] { ("ЛКМ", "Товар"), ("+", "Больше"), ("−", "Меньше"), ("Tab", "Купить / продать"), ("Esc", "Уйти") }
                : new[] { ("ЛКМ", "Товар"), ("+", "Больше"), ("−", "Меньше"), ("Esc", "Уйти") });
        }

        bool Shows(ItemKind kind) => shopFilter switch
        {
            0 => kind.Category == ItemCategory.Gu,
            1 => kind.Category == ItemCategory.Food || kind.Category == ItemCategory.Herb || kind.Use == ItemUse.Feed,
            2 => kind.Category == ItemCategory.Material,
            _ => true,
        };

        /// <summary>The chosen thing under the keeper: what kind it is, its name, a few lines about it, and what it gives.</summary>
        static void Goods(Transform parent, ItemKind kind)
        {
            if (kind == null)
            {
                return;
            }

            UiKit.Lbl((kind.Rank > 0 ? $"{kind.Kind} · ранг {Aperture.RankNumeral(kind.Rank)}" : kind.Kind) ?? "", parent, 11,
                kind.Rank > 0 ? Aperture.RankColour(kind.Rank) : UiKit.LabelGold, 0.3f).Size(14);
            UiKit.Disp(kind.Name, parent, 22);
            var about = UiKit.Label(kind.About, parent, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.lineSpacing = 1.1f;
            var gives = kind.Use switch
            {
                ItemUse.Eat when kind.Heal > 0 => $"+{kind.Heal:0} здоровья",
                ItemUse.Feed when kind.AnyGu => "порция корма любому Гу",
                ItemUse.Feed => "корм для Гу",
                ItemUse.Refine => $"очистка за {GuBook.Find(kind.Gu)?.RefineCost:0}% эссенции",
                ItemUse.Activate => GuBook.Find(kind.Gu)?.SkillName,
                _ => null,
            };

            if (gives != null)
            {
                Stat(parent, "Даёт", gives);
            }

            Stat(parent, "Хранится", kind.Keeps > 0 ? Span(kind.Keeps) : "не портится");
        }

        void BuyRow(Transform parent, Shop shop, Goods goods, ItemKind kind, double now, float discount)
        {
            var price = Trade.PriceOf(shop, goods.Item, now, discount);
            var priceText = shop.ForMerit ? $"{price} вклада" : ItemBook.Price(price);
            var left = host.Trade.Left(shop, goods, now);
            shopLots.TryGetValue(kind.Id, out var lots);
            var row = Row(parent, kind, lots > 0, () =>
            {
                shopChosen = kind.Id;
                Show(Page.Shop);
            });
            UiKit.Label(left > 0 ? (kind.Bundle > 1 ? $"{left * kind.Bundle} шт." : $"{left}") : "нет", row, 14, left > 0 ? UiKit.Hex(0xA8B6B8) : UiKit.Danger).Size(-1, 70, 0);
            UiKit.Label(kind.Bundle > 1 ? $"{priceText}\n<size=11><color=#7E9093>за {kind.Bundle} шт.</color></size>" : priceText, row, 14, UiKit.Text).Size(-1, 110, 0);
            Counter(row, kind.Bundle > 1 ? $"{lots * kind.Bundle}" : $"{lots}", lots > 0, () =>
            {
                shopLots[kind.Id] = Mathf.Max(0, lots - 1);
                Show(Page.Shop);
            }, () =>
            {
                shopLots[kind.Id] = Mathf.Min(left, lots + 1);
                Show(Page.Shop);
            });
        }

        void SellRow(Transform parent, int cell, ItemStack stack, ItemKind kind)
        {
            shopSales.TryGetValue(cell, out var sale);
            var count = sale.id == stack.id ? sale.count : 0;
            var row = Row(parent, kind, count > 0, () =>
            {
                shopChosen = kind.Id;
                Show(Page.Shop);
            });
            UiKit.Label($"{stack.count}", row, 14, UiKit.Hex(0xA8B6B8)).Size(-1, 70, 0);
            UiKit.Label($"{ItemBook.Price(Trade.SellPrice(kind, kind.Bundle))}" + (kind.Bundle > 1 ? $"\n<size=11><color=#7E9093>за {kind.Bundle} шт.</color></size>" : ""), row, 14, UiKit.Text).Size(-1, 110, 0);
            var step = kind.Bundle;
            Counter(row, $"{count}", count > 0, () =>
            {
                var next = Mathf.Max(0, count - step);

                if (next > 0) shopSales[cell] = (stack.id, next);
                else shopSales.Remove(cell);

                Show(Page.Shop);
            }, () =>
            {
                var next = Mathf.Min(stack.count / step * step, count + step);

                if (next > 0) shopSales[cell] = (stack.id, next);

                Show(Page.Shop);
            });
        }

        /// <summary>A row of the goods (.gr): the thing's mark, its name and what it is; in the deal, it is lit; a click on it shows it under the keeper.</summary>
        RectTransform Row(Transform parent, ItemKind kind, bool inDeal, System.Action choose)
        {
            var chosen = kind.Id == shopChosen;
            var (row, _) = UiKit.ListRow(parent, 66, chosen || inDeal, choose);
            var line = row.Stack<HorizontalLayoutGroup>(16, new RectOffset(18, 6, 0, 0), TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            var rule = UiKit.Image("Rule", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f), UiKit.Plain);
            rule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            rule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var mark = UiKit.Icon(kind.Icon, row, 44, Icons.Tint(kind.Icon));
            mark.Size(44, 48, 0);
            var words = UiKit.Rect("Words", row);
            words.Stack<VerticalLayoutGroup>(3, null, TextAnchor.MiddleLeft);
            words.Size(66, -1, 1);
            var name = UiKit.Disp(kind.Name, words, 19, chosen ? Color.white : UiKit.Text);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.Size(22);
            UiKit.Lbl((kind.Rank > 0 ? $"ранг {Aperture.RankNumeral(kind.Rank)} · " : "") + (kind.Kind ?? ""), words, 10,
                kind.Rank > 0 ? Aperture.RankColour(kind.Rank) : UiKit.Muted, 0.14f).Size(12);

            return row;
        }

        static void Counter(Transform row, string value, bool lit, System.Action minus, System.Action plus)
        {
            var box = UiKit.Rect("Counter", row);
            var group = box.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleRight);
            group.childForceExpandWidth = false;
            box.Size(66, 110, 0);
            QuantityButton(box, "−", minus, false);
            UiKit.Disp(value, box, 22, lit ? UiKit.GoldLit : UiKit.Muted, TextAnchor.MiddleCenter).Size(30, 30, 0);
            QuantityButton(box, "+", plus, true);
        }

        /// <summary>A small square button to take one more or one less (.qb): the plus is gold.</summary>
        static void QuantityButton(Transform parent, string sign, System.Action click, bool gold)
        {
            var box = UiKit.Image(sign, parent, gold ? UiKit.GoldLit.A(0.08f) : new Color(0, 0, 0, 0.01f), UiKit.Plain);
            box.raycastTarget = true;
            var edge = UiKit.Frame(box.transform, gold ? UiKit.Gold.A(0.5f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f));
            var mark = UiKit.Label(sign, box.transform, 16, gold ? UiKit.GoldLit : UiKit.Text2, TextAnchor.MiddleCenter);
            mark.rectTransform.Fill();
            box.Size(28, 28, 0);
            var clickable = box.gameObject.AddComponent<Clickable>();
            clickable.Click = click;
            clickable.Pointed = over => edge.color = over ? UiKit.GoldLit : gold ? UiKit.Gold.A(0.5f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f);
        }

        static void DealLine(Transform parent, string what, int price, bool merit = false)
        {
            var row = UiKit.Rect(what, parent);
            row.Size(34);
            UiKit.Label(what, row, 14, UiKit.Text).rectTransform.Fill();
            UiKit.Disp(merit ? $"{price} вклада" : ItemBook.Price(price), row, 18, UiKit.Text, TextAnchor.MiddleRight).rectTransform.Fill();
            var rule = UiKit.Image("Rule", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f), UiKit.Plain);
            rule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
        }

        /// <summary>Sells what is in the deal, pile by pile (each checked to be what it was).</summary>
        void SellAll()
        {
            var got = 0;

            foreach (var pair in new List<KeyValuePair<int, (string id, int count)>>(shopSales))
            {
                if (host.Cultivator.Bag[pair.Key]?.id == pair.Value.id)
                {
                    got += host.Sell(pair.Key, pair.Value.count);
                }
            }

            shopSales.Clear();

            if (got > 0)
            {
                Notify($"Продано за {ItemBook.Stones(got)}");
            }
        }

        /// <summary>
        /// A scrolling column (the goods can be more than fit): scrolled `down` points from its top at first, and telling
        /// `scrolled` where it is as it moves.
        /// </summary>
        static RectTransform Scroll(Transform parent, float down = 0, System.Action<float> scrolled = null)
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
            content.Stack<VerticalLayoutGroup>(6);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            // Points from the top, not a share of the length: the list's length is only known once it is laid out.
            content.anchoredPosition = new Vector2(0, down);

            if (scrolled != null)
            {
                scroll.onValueChanged.AddListener(_ => scrolled(content.anchoredPosition.y));
            }

            return content;
        }
    }
}
