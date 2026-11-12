using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// A house: bought here if it is not the hero's yet; once it is, its store (put from the bag, take
    /// back), its nursery (Gu kept out of the aperture, fed from the store) and its bed.
    /// </summary>
    public partial class Ui
    {
        ThingDef home;

        public void OpenHome(ThingDef def)
        {
            home = def;
            Show(Page.Home);
        }

        void HomePage()
        {
            var session = S;

            if (home == null)
            {
                Close();
                return;
            }

            if (!session.OwnsHome(home.Id))
            {
                var stage = UiKit.Stage(pageRoot);
                UiKit.Backdrop(stage, "家", 520);
                var middle = Centre(stage, 620, 200, 520);
                UiKit.Disp(home.Name, middle, 40, UiKit.Text, TextAnchor.MiddleCenter).Size(50);
                UiKit.Lbl("Продаётся", middle, 12, UiKit.LabelGold, 0.4f, TextAnchor.MiddleCenter).Size(20);
                Gap(middle, 16);
                Para("Кровать, где выспаться до утра и встать целым. Кладовая для того, что не помещается в сумку. Питомник: Гу, вынутые из апертуры, ждут там и сами берут корм из кладовой.", middle, 15, UiKit.Text2);
                Gap(middle, 12);
                Fact(middle, "Цена", $"{session.HomePrice(home)} камней", UiKit.GoldText);
                Fact(middle, "У вас", $"{session.State.Stones} камней");
                var why = session.WhyNoHome(home);
                var keys = middle.gameObject.AddComponent<MenuKeys>();
                keys.Add(UiKit.Entry("Купить", why, middle, () =>
                {
                    if (session.BuyHome(home))
                    {
                        Sound.AudioDirector.Play("coin", null, 0.8f, 0);
                        Show(Page.Home);
                    }
                }).Centre());
                keys.Add(UiKit.Entry("Уйти", null, middle, Close).Centre());
                UiKit.Footer(stage, new[] { ("Enter", "Выбрать"), ("Esc", "Уйти") }).GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

                return;
            }

            var body = Frame("家", home.Name, $"Кладовая: {session.State.Stash.Count} видов · питомник: {session.State.Nursery.Count} из {session.NurserySize}", false, null, Purse, new[] { ("Esc", "Выйти") });

            // The bag, to put things away.
            var left = Column(body, 0, 0.31f, 6);
            UiKit.Lbl("Сумка → в кладовую", left, 11, UiKit.LabelGold).Size(18);
            var bag = Scroll(Rest(left));

            foreach (var stack in new List<ItemStack>(session.State.Items))
            {
                var def = ItemBook.Get(stack.Item);

                if (def == null || def.Kind == ItemKind.Quest || def.Kind == ItemKind.Key)
                {
                    continue;
                }

                var id = stack.Item;
                ListLine(bag, def.Seal, def.Name, $"× {stack.Count}", UiKit.Text, false, () => { session.StashPut(id, Keys.Shift ? 1 : session.Count(id)); Redraw(); });
            }

            // The store, to take things back.
            var middleColumn = Column(body, 0.34f, 0.65f, 6);
            UiKit.Lbl("Кладовая → в сумку", middleColumn, 11, UiKit.LabelGold).Size(18);
            var store = Scroll(Rest(middleColumn));

            foreach (var stack in new List<ItemStack>(session.State.Stash))
            {
                var def = ItemBook.Get(stack.Item);
                var id = stack.Item;
                ListLine(store, def?.Seal ?? "物", def?.Name ?? id, $"× {stack.Count}", UiKit.Text, false, () => { session.StashTake(id, Keys.Shift ? 1 : session.StashCount(id)); Redraw(); });
            }

            if (session.State.Stash.Count == 0)
            {
                Para("Пусто. Нажмите на вещь в сумке, чтобы убрать её сюда (Shift — по одной).", store, 14, UiKit.Muted);
            }

            // The nursery and the bed.
            var right = Column(body, 0.68f, 1, 6);
            UiKit.Lbl("Питомник", right, 11, UiKit.LabelGold).Size(18);
            var nursery = Scroll(Rest(right));

            foreach (var gu in session.State.Gu)
            {
                var def = GuBook.Get(gu.Def);
                var uid = gu.Uid;
                var inside = session.InNursery(uid);
                var food = def.Food != "" ? $"корм {session.StashCount(def.Food)}" : "без корма";
                ListLine(nursery, def.Seal, def.Name, inside ? "в питомнике · " + food : session.IsSlotted(uid) ? "в апертуре" : "", inside ? UiKit.Essence : UiKit.Text2, inside, () =>
                {
                    if (inside)
                    {
                        session.NurseryTake(uid);
                    }
                    else
                    {
                        session.NurseryPut(uid);
                    }

                    Redraw();
                });
            }

            Gap(right, 8);
            UiKit.HexButton("Спать до утра", right, () =>
            {
                session.SleepAtHome();
                World.Atmosphere.Hour = session.State.Hour;
                Close();
                Game.Say("Вы выспались дома. Апертура полна, тело отдохнуло.");
                Game.Save();
            }, UiKit.HexKind.Main, 13, 46);
        }
    }
}
