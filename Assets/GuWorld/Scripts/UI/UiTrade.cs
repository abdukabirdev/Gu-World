using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>The pages opened in the village: a shop (to buy and to sell), the board of errands, the fusion altar.</summary>
    public partial class Ui
    {
        bool selling;
        int pickedGoods;
        string pickedSale;
        string pickedErrand;
        string pickedRecipe;

        // ------------------------------------------------------------------ Shops

        void ShopPage(ShopEntry[] shop, string title, string hanzi, Page page)
        {
            var session = S;
            var body = Frame(hanzi, title, selling ? "Продать" : "Купить", false, null, Purse, new[] { ("Tab", "Купить / продать"), ("Esc", "Уйти") });
            body.gameObject.AddComponent<PageKeys>().On(UnityEngine.InputSystem.Key.Tab, () => { selling = !selling; Show(page); });
            var left = Column(body, 0, 0.5f, 6);
            var tabs = UiKit.Rect("Tabs", left);
            tabs.Stack<HorizontalLayoutGroup>(24, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            tabs.Size(40);
            UiKit.Tb("Купить", tabs, !selling, () => { selling = false; Show(page); });
            UiKit.Tb("Продать", tabs, selling, () => { selling = true; Show(page); });
            var list = Scroll(Rest(left));
            var right = Column(body, 0.56f, 1, 8);

            if (!selling)
            {
                var shelf = session.Shelf(shop);
                pickedGoods = Mathf.Clamp(pickedGoods, 0, Mathf.Max(0, shelf.Count - 1));

                for (var i = 0; i < shelf.Count; i++)
                {
                    var entry = shelf[i];
                    var index = i;
                    var gu = entry.Gu != null ? GuBook.Get(entry.Gu) : null;
                    var item = entry.Item != null ? ItemBook.Get(entry.Item) : null;
                    var price = session.PriceFor(entry, shopClan);
                    var afford = session.State.Stones >= price;
                    ListLine(list, gu != null ? gu.Seal : item.Seal, gu != null ? gu.Name : item.Name, $"{price} камней", gu != null ? UiKit.Hex(gu.Colour) : UiKit.Hex(item.Colour), i == pickedGoods,
                        () => { pickedGoods = index; Redraw(); }, afford ? UiKit.GoldText : UiKit.Danger);
                }

                if (shelf.Count == 0)
                {
                    return;
                }

                var picked = shelf[pickedGoods];

                if (picked.Gu != null)
                {
                    GuDetail(right, session.MakeGu(picked.Gu, 0, true), false);
                }
                else
                {
                    var item = ItemBook.Get(picked.Item);
                    UiKit.Zh(item.Seal, right, 64, UiKit.Hex(item.Colour)).Size(76);
                    UiKit.Disp(item.Name, right, 28, UiKit.Text).Size(34);
                    UiKit.Lbl($"{KindName(item.Kind)} · в сумке {session.Count(item.Id)}", right, 11, UiKit.LabelGold).Size(16);
                    UiKit.Rule(right);
                    Para(item.Description, right, 16);
                }

                Gap(right, 12);
                var cost = session.PriceFor(picked, shopClan);
                var can = session.State.Stones >= cost;
                UiKit.HexButton($"Купить · {cost} камней", right, () =>
                {
                    session.Buy(picked, shopClan);
                    Redraw();
                }, can ? UiKit.HexKind.Main : UiKit.HexKind.Off, 13, 54);

                if (!can)
                {
                    Para("Не хватает первобытных камней.", right, 14, UiKit.Danger);
                }

                return;
            }

            // Selling: what is in the bag, and the Gu that are in no slot.
            var sales = new List<(string key, string seal, string name, int price, Color colour)>();

            foreach (var stack in session.State.Items)
            {
                var item = ItemBook.Get(stack.Item);

                if (session.SellPrice(stack.Item) > 0)
                {
                    sales.Add(("i:" + stack.Item, item.Seal, $"{item.Name} × {stack.Count}", session.SellPrice(stack.Item), UiKit.Hex(item.Colour)));
                }
            }

            foreach (var gu in session.State.Gu)
            {
                var def = GuBook.Get(gu.Def);

                if (!session.IsSlotted(gu.Uid) && def.Price > 0)
                {
                    sales.Add(("g:" + gu.Uid, def.Seal, def.Name + (gu.Refined ? "" : " (дикий)"), session.SellPrice(gu), QualityColour(gu.Quality)));
                }
            }

            if (!sales.Exists(s => s.key == pickedSale))
            {
                pickedSale = sales.Count > 0 ? sales[0].key : null;
            }

            foreach (var sale in sales)
            {
                var key = sale.key;
                ListLine(list, sale.seal, sale.name, $"{sale.price} камней", sale.colour, key == pickedSale, () => { pickedSale = key; Redraw(); }, UiKit.GoldText);
            }

            if (pickedSale == null)
            {
                Para("Продать нечего. Гу в слотах не продаются — сначала снимите их в апертуре.", right, 15, UiKit.Muted);
                return;
            }

            var chosen = sales.Find(s => s.key == pickedSale);

            if (pickedSale.StartsWith("g:"))
            {
                var uid = int.Parse(pickedSale.Substring(2));
                GuDetail(right, session.FindGu(uid), false);
                Gap(right, 12);
                UiKit.HexButton($"Продать · {chosen.price} камней", right, () =>
                {
                    session.SellGu(uid);
                    Redraw();
                }, UiKit.HexKind.Main, 13, 54);
            }
            else
            {
                var id = pickedSale.Substring(2);
                var item = ItemBook.Get(id);
                UiKit.Zh(item.Seal, right, 64, UiKit.Hex(item.Colour)).Size(76);
                UiKit.Disp(item.Name, right, 28, UiKit.Text).Size(34);
                UiKit.Rule(right);
                Para(item.Description, right, 16);
                Gap(right, 12);
                UiKit.HexButton($"Продать одну · {chosen.price} камней", right, () =>
                {
                    session.Sell(id);
                    Redraw();
                }, UiKit.HexKind.Main, 13, 54);
                var all = session.Count(id);

                if (all > 1)
                {
                    UiKit.HexButton($"Продать все · {chosen.price * all} камней", right, () =>
                    {
                        session.Sell(id, all);
                        Redraw();
                    }, UiKit.HexKind.Ghost, 13, 46);
                }
            }
        }

        // ------------------------------------------------------------------ The board

        void BoardPage()
        {
            var session = S;
            var body = Frame("族", "Доска поручений", "Клан платит по исполнении", false, null, Purse);
            var left = Column(body, 0, 0.46f, 6);
            UiKit.Lbl("Поручения", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));
            var errands = new List<QuestDef>();

            foreach (var quest in Story.All)
            {
                if (quest.Giver == "scribe" && session.Has(quest.Needs))
                {
                    errands.Add(quest);
                }
            }

            if (!errands.Exists(e => e.Id == pickedErrand))
            {
                pickedErrand = errands.Count > 0 ? errands[0].Id : null;
            }

            foreach (var errand in errands)
            {
                var id = errand.Id;
                var taken = session.IsActive(id);
                ListLine(list, "守", errand.Title.Replace("Поручение: ", ""), taken ? "взято" : $"{errand.Reward.Stones} камней", taken ? UiKit.Essence : UiKit.Text2, id == pickedErrand, () => { pickedErrand = id; Redraw(); },
                    taken ? UiKit.Essence : UiKit.GoldText);
            }

            var right = Column(body, 0.52f, 1, 8);

            if (pickedErrand == null)
            {
                Para("Доска пуста. Писарь вывесит поручения, когда клан увидит, что вы справляетесь с охотой.", right, 16, UiKit.Muted);
                return;
            }

            var picked = Story.Get(pickedErrand);
            UiKit.Disp(picked.Title, right, 30, UiKit.Text).Size(38);
            UiKit.Rule(right);
            Para(picked.Summary, right, 16);
            Gap(right, 6);
            Fact(right, "Плата", $"{picked.Reward.Stones} камней", UiKit.GoldText);
            Fact(right, "Прорыв", $"{picked.Reward.Insight}", UiKit.GoldText);
            Gap(right, 12);
            var state = session.StateOf(picked.Id);

            if (state != null && !state.Done)
            {
                Para(session.StepText(state), right, 17, UiKit.GoldText);
            }
            else
            {
                UiKit.HexButton("Взять поручение", right, () =>
                {
                    session.StartQuest(picked.Id);
                    Redraw();
                }, UiKit.HexKind.Main, 13, 54);
            }
        }

        // ------------------------------------------------------------------ Fusion

        void FusionPage()
        {
            var session = S;
            var body = Frame("炼", "Алтарь слияния", "Два Гу и их корм становятся одним, рангом выше", false, null, Purse);
            var left = Column(body, 0, 0.44f, 6);
            UiKit.Lbl("Рецепты", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));

            if (pickedRecipe == null)
            {
                foreach (var recipe in Recipes.All)
                {
                    if (session.KnowsRecipe(recipe.Id) && (pickedRecipe == null || session.FusionInputs(recipe, out _) != null))
                    {
                        pickedRecipe = recipe.Id;
                    }
                }
            }

            foreach (var recipe in Recipes.All)
            {
                var result = GuBook.Get(recipe.Result);
                var id = recipe.Id;

                if (!session.KnowsRecipe(id))
                {
                    ListLine(list, "隐", "Неизвестный рецепт", $"ранг {GuBook.RankNumerals[result.Rank]}", UiKit.Muted, false, null);
                    continue;
                }

                var ready = session.FusionInputs(recipe, out _) != null;
                ListLine(list, result.Seal, result.Name, ready ? "готово" : $"ранг {GuBook.RankNumerals[result.Rank]}", UiKit.Hex(result.Colour), id == pickedRecipe, () => { pickedRecipe = id; Redraw(); },
                    ready ? UiKit.Ok : UiKit.Muted);
            }

            var right = Column(body, 0.5f, 1, 6);
            var picked = pickedRecipe != null ? Recipes.Get(pickedRecipe) : null;

            if (picked == null)
            {
                Para("Вы не знаете ни одного рецепта. Свитки продаёт Баошань, их находят в тайниках и получают за задания. Прочтите свиток в сумке.", right, 16, UiKit.Muted);
                return;
            }

            var made = GuBook.Get(picked.Result);
            GuDetail(right, session.MakeGu(picked.Result, 0, true), false);
            Gap(right, 8);
            UiKit.Lbl("Нужно", right, 11, UiKit.LabelGold).Size(18);
            var used = new List<GuInstance>();

            foreach (var need in picked.Gu)
            {
                var have = session.State.Gu.Find(g => g.Def == need && g.Refined && !used.Contains(g));

                if (have != null)
                {
                    used.Add(have);
                }

                Fact(right, GuBook.Get(need).Name, have != null ? "есть" : "нет", have != null ? UiKit.Ok : UiKit.Danger);
            }

            foreach (var (item, count) in picked.Items)
            {
                var have = session.Count(item);
                Fact(right, ItemBook.Get(item).Name, $"{have} / {count}", have >= count ? UiKit.Ok : UiKit.Danger);
            }

            Fact(right, "Первобытные камни", $"{session.State.Stones} / {picked.Stones}", session.State.Stones >= picked.Stones ? UiKit.Ok : UiKit.Danger);
            Gap(right, 8);
            var inputs = session.FusionInputs(picked, out var why);
            UiKit.HexButton("Слить", right, () =>
            {
                if (session.Fuse(picked.Id) != null)
                {
                    Game.Save();
                }

                Redraw();
            }, inputs != null ? UiKit.HexKind.Solid : UiKit.HexKind.Off, 14, 56);

            if (why != null)
            {
                Para(why, right, 14, UiKit.Danger);
            }
            else
            {
                Para("Качество будет не хуже лучшего из вошедших Гу; каждый четвёртый раз оно поднимается на ступень.", right, 13, UiKit.Muted);
            }
        }
    }
}
