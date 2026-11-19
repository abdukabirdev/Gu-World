using GuWorld.Actors;
using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>The hero's pages: the aperture (Gu and slots), the bag, the journal and the map.</summary>
    public partial class Ui
    {
        int pickedGu;
        string pickedItem;
        string pickedQuest;
        static Texture2D mapTexture;
        /// <summary>The keys of the six active slots, as bound now.</summary>
        static string[] SlotKeys => new[] { Bindings.LabelOf("slot1"), Bindings.LabelOf("slot2"), Bindings.LabelOf("slot3"), Bindings.LabelOf("slot4"), Bindings.LabelOf("slot5"), Bindings.LabelOf("slot6") };

        // ------------------------------------------------------------------ Aperture

        static string SlotName(Session session, GuInstance gu)
        {
            var active = System.Array.IndexOf(session.State.Active, gu.Uid);

            return active >= 0 ? "слот " + SlotKeys[active] : System.Array.IndexOf(session.State.Passive, gu.Uid) >= 0 ? "пассивный" : gu.Refined ? "" : "дикий";
        }

        void GuCell(Transform parent, GuInstance gu, string key, bool open, System.Action click)
        {
            var cell = UiKit.Cell(parent, 64, click);
            cell.Size(64, 64, 0);
            cell.Mark.enabled = false;
            cell.Key.text = key;
            var def = gu != null ? GuBook.Get(gu.Def) : null;
            UiKit.Zh(def != null ? def.Seal : open ? "" : "隐", cell.transform, 30, def != null ? UiKit.Hex(def.Colour) : UiKit.Muted.A(0.4f), TextAnchor.MiddleCenter).rectTransform.Fill();
            cell.SetRim(gu != null ? QualityColour(gu.Quality).A(0.85f) : UiKit.Line);
            cell.SetRank(def != null ? def.Rank : 0);
            cell.SetRest(0);
        }

        void AperturePage()
        {
            var session = S;
            var state = session.State;
            var stats = session.Stats;
            var body = Frame("窍", "Апертура", $"{Cultivation.Name(state.Step)} · талант {Cultivation.TalentName(state.Talent)}", true, null, Purse,
                new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });

            // The hero and the slots.
            var left = Column(body, 0, 0.28f, 6);
            UiKit.Disp("Гу Юэ " + state.HeroName, left, 30, UiKit.Text).Size(36);
            UiKit.Lbl(Cultivation.Name(state.Step), left, 11, UiKit.LabelGold).Size(16);
            Gap(left, 4);
            UiKit.Bar(left, UiKit.BarKind.Stamina, session.InsightShare, 4);
            Fact(left, "Прорыв", state.Step >= Cultivation.MaxStep ? "вершина" : $"{(int)state.Insight} / {session.InsightNeeded}", UiKit.GoldText);
            Fact(left, "Здоровье", $"{Mathf.CeilToInt(state.Health)} / {Mathf.RoundToInt(stats.MaxHealth)}", UiKit.Danger);
            Fact(left, "Эссенция", $"{Mathf.RoundToInt(state.Essence)} / {Mathf.RoundToInt(stats.MaxEssence)}", UiKit.Essence);
            Fact(left, "Восстановление", $"{stats.EssenceRegen:0.0} в секунду");
            Fact(left, "Сила Гу", $"×{stats.Power:0.00}");
            Fact(left, "Удар ладонью", $"×{stats.Melee:0.00}");
            Fact(left, "Урон по вам", $"×{stats.DamageTaken:0.00}");

            // The harmony of the aperture: three of a path ring together, five paths and more jar.
            if (stats.Resonance.HasValue)
            {
                Fact(left, "Созвучие", $"{GuBook.PathName(stats.Resonance.Value)} +{stats.ResonanceBonus * 100:0}%", UiKit.GoldText);
            }

            if (stats.Discord > 0)
            {
                Fact(left, "Разлад путей", $"{stats.Paths} путей · −{stats.Discord * 100:0}%", UiKit.Danger);
            }

            Gap(left, 10);
            UiKit.Lbl("Активные слоты", left, 11, UiKit.LabelGold).Size(18);
            var active = UiKit.Rect("Active", left);
            active.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            active.Size(64);

            for (var i = 0; i < state.Active.Length; i++)
            {
                var gu = i < stats.ActiveSlots ? session.ActiveGu(i) : null;
                var uid = gu != null ? gu.Uid : 0;
                GuCell(active, gu, SlotKeys[i], i < stats.ActiveSlots, uid != 0 ? () => { pickedGu = uid; Redraw(); } : (System.Action)null);
            }

            Gap(left, 6);
            UiKit.Lbl("Пассивные слоты", left, 11, UiKit.LabelGold).Size(18);
            var passive = UiKit.Rect("Passive", left);
            passive.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            passive.Size(64);

            for (var i = 0; i < state.Passive.Length; i++)
            {
                var gu = i < stats.PassiveSlots ? session.FindGu(state.Passive[i]) : null;
                var uid = gu != null ? gu.Uid : 0;
                GuCell(passive, gu, "", i < stats.PassiveSlots, uid != 0 ? () => { pickedGu = uid; Redraw(); } : (System.Action)null);
            }

            // Every Gu the hero has.
            var middle = Column(body, 0.31f, 0.64f, 6);
            UiKit.Lbl($"Гу · {state.Gu.Count}", middle, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(middle));
            var picked = session.FindGu(pickedGu);

            if (picked == null && state.Gu.Count > 0)
            {
                picked = state.Gu[0];
                pickedGu = picked.Uid;
            }

            var sorted = new List<GuInstance>(state.Gu);
            sorted.Sort((a, b) =>
            {
                var slotted = session.IsSlotted(b.Uid).CompareTo(session.IsSlotted(a.Uid));

                return slotted != 0 ? slotted : GuBook.Get(b.Def).Rank != GuBook.Get(a.Def).Rank ? GuBook.Get(b.Def).Rank.CompareTo(GuBook.Get(a.Def).Rank) : b.Quality.CompareTo(a.Quality);
            });

            foreach (var gu in sorted)
            {
                var def = GuBook.Get(gu.Def);
                var uid = gu.Uid;
                ListLine(list, def.Seal, def.Name, SlotName(session, gu), gu.Refined ? QualityColour(gu.Quality) : UiKit.Muted, gu == picked, () => { pickedGu = uid; Redraw(); },
                    gu.Refined ? UiKit.Essence : UiKit.Hunger);
            }

            if (state.Gu.Count == 0)
            {
                Para("Гу пока нет. Первого даст смотрительница питомника Синь.", list, 15, UiKit.Muted);
            }

            // The one chosen.
            var right = Column(body, 0.67f, 1, 6);

            if (picked != null && temperGu == picked.Uid)
            {
                TemperPanel(right, picked);
            }
            else if (picked != null)
            {
                GuDetail(right, picked, true);
            }
        }

        /// <summary>Everything about one Gu; with `acts`, the buttons to refine it and put it in a slot.</summary>
        /// <summary>
        /// A Gu not in a slot against the one it would most likely take the place of (ROADMAP 3.2): the
        /// slotted Gu of its kind with the same shape of skill (else of its path, else the first); what
        /// would change — power, essence, cooldown, range — better in the gifts' colour, worse in the harms'.
        /// </summary>
        void Compare(RectTransform column, GuInstance gu)
        {
            var session = S;
            var def = GuBook.Get(gu.Def);

            if (def.Skill == null || def.Kind != GuKind.Active)
            {
                return;
            }

            GuInstance against = null;
            var score = -1;

            foreach (var slotted in session.Slotted())
            {
                var other = GuBook.Get(slotted.Def);

                if (slotted.Uid == gu.Uid)
                {
                    return;
                }

                if (other.Kind != GuKind.Active || other.Skill == null)
                {
                    continue;
                }

                var fit = (other.Skill.Shape == def.Skill.Shape ? 2 : 0) + (other.Path == def.Path ? 1 : 0);

                if (fit > score)
                {
                    score = fit;
                    against = slotted;
                }
            }

            if (against == null)
            {
                return;
            }

            var them = GuBook.Get(against.Def);
            Gap(column, 6);
            UiKit.Lbl($"Против «{them.Name}» в слоте", column, 11, UiKit.LabelGold).Size(16);

            float Cooldown(GuInstance g) => GuBook.Get(g.Def).Skill.Cooldown * session.Stats.Cooldown * (1 - Session.AffixOf(g, AffixKind.Quick));
            float Range(GuInstance g) => GuBook.Get(g.Def).Skill.Range * session.Stats.Range * (1 + Session.AffixOf(g, AffixKind.Far));

            void Delta(string name, float mine, float theirs, bool higherBetter, string unit = "", string format = "0")
            {
                var change = mine - theirs;

                if (Mathf.Abs(change) < 0.05f)
                {
                    Fact(column, name, "так же", UiKit.Muted);
                    return;
                }

                var better = higherBetter ? change > 0 : change < 0;
                Fact(column, name, (change > 0 ? "+" : "−") + Mathf.Abs(change).ToString(format) + unit, better ? Accessible.Gift : Accessible.Harm);
            }

            if (def.Skill.Power > 0 || them.Skill.Power > 0)
            {
                Delta("Сила", session.PowerOf(gu), session.PowerOf(against), true);
            }

            Delta("Расход", session.CostOf(gu), session.CostOf(against), false);
            Delta("Перезарядка", Cooldown(gu), Cooldown(against), false, " с", "0.0");

            if (def.Skill.Range > 0 && them.Skill.Range > 0)
            {
                Delta("Дальность", Range(gu), Range(against), true, " м");
            }
        }

        void GuDetail(RectTransform column, GuInstance gu, bool acts)
        {
            var session = S;
            var def = GuBook.Get(gu.Def);
            var head = UiKit.Rect("Head", column);
            head.Size(84);
            var seal = UiKit.Zh(def.Seal, head, 64, UiKit.Hex(def.Colour), TextAnchor.MiddleLeft);
            seal.rectTransform.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -80, 0);
            var name = UiKit.Disp(def.Name, head, 26, QualityColour(gu.Quality));
            name.rectTransform.Place(Vector2.zero, Vector2.one, 92, 34, 0, 8);
            var kind = UiKit.Lbl($"Ранг {GuBook.RankNumerals[def.Rank]} · {GuBook.PathName(def.Path)} · {GuBook.QualityNames[gu.Quality]}", head, 11, UiKit.LabelGold, 0.2f);
            kind.rectTransform.Place(Vector2.zero, Vector2.one, 92, 12, 0, 54);
            UiKit.Rule(column);
            Para(def.Description, column, 15);
            Gap(column, 4);

            if (def.Skill != null)
            {
                var skill = def.Skill;

                if (skill.Power > 0)
                {
                    Fact(column, skill.Shape == SkillShape.Heal ? "Лечение" : "Сила", $"{session.PowerOf(gu):0}" + (skill.Count > 1 && skill.Shape == SkillShape.Fan ? $" × {skill.Count}" : ""), UiKit.GoldText);
                }

                Fact(column, "Расход эссенции", $"{session.CostOf(gu):0}", UiKit.Essence);
                Fact(column, "Перезарядка", $"{skill.Cooldown * session.Stats.Cooldown * (1 - Session.AffixOf(gu, AffixKind.Quick)):0.0} с");

                if (skill.Range > 0)
                {
                    Fact(column, "Дальность", $"{skill.Range * session.Stats.Range * (1 + Session.AffixOf(gu, AffixKind.Far)):0} м");
                }
            }

            foreach (var affix in gu.Affixes)
            {
                Fact(column, GuBook.AffixName(affix.Kind), GuBook.AffixText(affix).Split(':')[1].Trim(), UiKit.SystemBlue);
            }

            Compare(column, gu);

            // What it eats and how hungry it is.
            if (def.Food != "" && session.State.Settings.Hunger != HungerRule.Off && session.FindGu(gu.Uid) != null)
            {
                var food = ItemBook.Get(def.Food);
                var starving = session.IsStarving(gu);
                var hungry = session.IsHungry(gu);
                var days = session.DaysToStarve(gu);
                Fact(column, "Корм", $"{food?.Name} раз в {def.Appetite} дн. · в сумке {session.Count(def.Food)}", starving ? UiKit.Danger : hungry ? UiKit.Hunger : UiKit.Text2);

                if (starving)
                {
                    Fact(column, "Голоден", days >= 0 ? $"слабее на треть · умрёт через {days} дн." : "слабее на треть", UiKit.Danger);
                }

                if (gu.Hunger > 0 && session.Count(def.Food) > 0)
                {
                    var uid = gu.Uid;
                    UiKit.HexButton("Покормить", column, () => { session.Feed(uid); Redraw(); }, hungry ? UiKit.HexKind.Main : UiKit.HexKind.Ghost, 12, 40);
                }
            }

            if (def.Id == HighGu.Cicada)
            {
                var sleep = session.CicadaSleepLeft;
                Fact(column, "Цикада", sleep > 0 ? $"спит ещё {sleep} дн." : session.CicadaReady ? "бодрствует · спасёт от смерти" : "вставьте в пассивный слот", sleep > 0 ? UiKit.Muted : UiKit.Essence);
            }

            if (def.Clan != "")
            {
                Fact(column, "Гу клана", Clans.Get(def.Clan)?.Name ?? def.Clan, UiKit.Hex(Clans.Get(def.Clan)?.Colour ?? 0xC9D3D5));
            }

            if (def.Kind == GuKind.Active)
            {
                Gap(column, 4);
                var level = Cultivation.MasteryLevel(gu.MasteryXp);
                Fact(column, "Мастерство", $"{level} из 5 · сила +{(level - 1) * 12}%");
                UiKit.Bar(column, UiKit.BarKind.Essence, Cultivation.MasteryShare(gu.MasteryXp), 3);
            }

            if (!acts)
            {
                return;
            }

            Gap(column, 12);

            if (!gu.Refined)
            {
                var why = session.WhyNoRefine(gu);
                var uid = gu.Uid;
                UiKit.HexButton($"Очистить · {session.RefineCost(gu):0} эссенции", column, () =>
                {
                    session.Refine(uid);
                    Redraw();
                }, why == null ? UiKit.HexKind.Main : UiKit.HexKind.Off, 13, 52);

                if (why != null)
                {
                    Para(why, column, 14, UiKit.Danger);
                }

                return;
            }

            var passive = def.Kind == GuKind.Passive;
            var count = passive ? session.Stats.PassiveSlots : session.Stats.ActiveSlots;
            var blocked = session.WhyNoEquip(gu, passive, 0);

            if (blocked != null)
            {
                Para(blocked, column, 14, UiKit.Danger);
            }
            else
            {
                UiKit.Lbl(passive ? "Поставить в пассивный слот" : "Поставить в слот", column, 11, UiKit.LabelGold).Size(18);
                var row = UiKit.Rect("Slots", column);
                row.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
                row.Size(46);
                var slots = passive ? session.State.Passive : session.State.Active;

                for (var i = 0; i < count; i++)
                {
                    var slot = i;
                    var uid = gu.Uid;
                    var here = slots[i] == gu.Uid;
                    UiKit.HexButton(passive ? $"{i + 1}" : SlotKeys[i], row, () =>
                    {
                        session.Equip(uid, passive, slot);
                        Redraw();
                    }, here ? UiKit.HexKind.Solid : UiKit.HexKind.Ghost, 14, 46).Size(46, 78, 0);
                }
            }

            var actions = UiKit.Rect("Actions", column);
            actions.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            actions.Size(46);

            if (session.IsSlotted(gu.Uid))
            {
                var uid = gu.Uid;
                UiKit.HexButton("Снять", actions, () =>
                {
                    session.Unslot(uid);
                    session.RefreshQuests();
                    Redraw();
                }, UiKit.HexKind.Ghost, 13, 46).Size(46, 120, 0);
            }

            var forge = gu.Uid;
            UiKit.HexButton("Закалка…", actions, () => { temperGu = forge; Redraw(); }, UiKit.HexKind.Ghost, 13, 46).Size(46, 150, 0);
        }

        int temperGu;

        /// <summary>Tempering a Gu up a quality, reforging its affixes, feeding it a twin.</summary>
        void TemperPanel(RectTransform column, GuInstance gu)
        {
            var session = S;
            var def = GuBook.Get(gu.Def);
            var uid = gu.Uid;
            var head = UiKit.Rect("Head", column);
            head.Size(84);
            var seal = UiKit.Zh(def.Seal, head, 64, UiKit.Hex(def.Colour), TextAnchor.MiddleLeft);
            seal.rectTransform.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -80, 0);
            var name = UiKit.Disp(def.Name, head, 26, QualityColour(gu.Quality));
            name.rectTransform.Place(Vector2.zero, Vector2.one, 92, 34, 0, 8);
            var kind = UiKit.Lbl($"Закалка · {GuBook.QualityNames[gu.Quality]}", head, 11, UiKit.LabelGold, 0.2f);
            kind.rectTransform.Place(Vector2.zero, Vector2.one, 92, 12, 0, 54);
            UiKit.Rule(column);

            // Up a quality.
            if (gu.Quality < GuBook.MaxQuality)
            {
                var cost = session.TemperCostOf(gu);
                var item = ItemBook.Get(cost.Item);
                Fact(column, "Станет", GuBook.QualityNames[gu.Quality + 1], QualityColour(gu.Quality + 1));
                Fact(column, item?.Name ?? cost.Item, $"{cost.Count} · в сумке {session.Count(cost.Item)}", session.Count(cost.Item) >= cost.Count ? UiKit.Text2 : UiKit.Danger);
                Fact(column, "Камни", $"{cost.Stones}", session.State.Stones >= cost.Stones ? UiKit.GoldText : UiKit.Danger);
                Fact(column, "Удача", $"{Mathf.RoundToInt(cost.Chance * 100)}%" + (gu.Tempered > 0 ? $" · неудач {gu.Tempered}" : ""), UiKit.Essence);
                var why = session.WhyNoTemper(gu);
                // What tempering costs is gone whether it holds or not: held to confirm (ROADMAP 2.12).
                var temper = UiKit.HexButton(why == null ? "Удерживать — закалить" : "Закалить", column, () =>
                {
                    var result = session.Temper(uid);
                    Sound.AudioDirector.Play(result.Held ? "temper" : "temper_fail", null, 0.8f, 0);
                    Redraw();
                }, why == null ? UiKit.HexKind.Main : UiKit.HexKind.Off, 13, 48);

                if (why == null)
                {
                    temper.Held(1.2f);
                }
                else
                {
                    Para(why, column, 13, UiKit.Danger);
                }
            }
            else
            {
                Para("Этот Гу совершенен: закалять больше нечего.", column, 14, UiKit.GoldText);
            }

            // Each affix, with a button to forge it again.
            if (gu.Affixes.Count > 0)
            {
                Gap(column, 6);
                UiKit.Lbl($"Перековка · {session.ReforgeStones(gu)} камней и {ItemBook.Get("jade_shard")?.Name} × {session.ReforgeJade(gu)}", column, 11, UiKit.LabelGold).Size(18);

                for (var i = 0; i < gu.Affixes.Count; i++)
                {
                    var at = i;
                    var row = UiKit.Rect("Affix", column);
                    row.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
                    row.Size(40);
                    UiKit.Label(GuBook.AffixText(gu.Affixes[i]), row, 14, UiKit.SystemBlue).Size(40, 220, 0);
                    // The old property is lost for a new one by chance: held to confirm.
                    var ready = session.WhyNoReforge(gu, at) == null;
                    var reforge = UiKit.HexButton(ready ? "Удерживать — перековать" : "Перековать", row, () => { session.Reforge(uid, at); Redraw(); }, ready ? UiKit.HexKind.Ghost : UiKit.HexKind.Off, 12, 36);
                    reforge.Size(36, ready ? 210 : 130, 0);

                    if (ready)
                    {
                        reforge.Held(1f);
                    }
                }
            }

            // A twin to take in.
            var twins = session.TwinsOf(gu);
            Gap(column, 6);

            if (twins.Count > 0)
            {
                // The twin is gone for good: held to confirm.
                UiKit.HexButton($"Удерживать — поглотить двойника · мастерство +{Session.TwinMastery(twins[0])} (их {twins.Count})", column, () => { session.FeedTwin(uid); Redraw(); }, UiKit.HexKind.Ghost, 12, 42).Held(1.2f);
            }
            else
            {
                Para("Второй такой же Гу, не вставленный в апертуру, можно скормить этому — его мастерство перейдёт сюда.", column, 13, UiKit.Muted);
            }

            Gap(column, 6);
            UiKit.HexButton("Назад", column, () => { temperGu = 0; Redraw(); }, UiKit.HexKind.Ghost, 13, 42);
        }

        // ------------------------------------------------------------------ Bag

        static string KindName(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Material: return "Материал";
                case ItemKind.Consumable: return "Расходник";
                case ItemKind.Scroll: return "Свиток рецепта";
                case ItemKind.Quest: return "Сюжетный предмет";
                case ItemKind.Rock: return "Самородок";
                case ItemKind.Food: return "Корм для Гу";
                case ItemKind.Herb: return "Трава";
                case ItemKind.Book: return "Книга";
                case ItemKind.Key: return "Ключ";
                default: return "Камень";
            }
        }

        void BagPage()
        {
            var session = S;
            var body = Frame("宝", "Сумка", $"{session.State.Items.Count} видов вещей", true, null, Purse, new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });
            var left = Column(body, 0, 0.5f, 6);

            // What is shown (by kind) and in what order (ROADMAP 3.2).
            UiKit.Choices(left, BagFilters, bagFilter, i => { bagFilter = i; Redraw(); }, 13);
            var sortRow = UiKit.Rect("Sort", left);
            sortRow.Size(34);
            var sortLine = sortRow.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft);
            sortLine.childForceExpandWidth = false;
            UiKit.Lbl("Порядок", sortRow, 10, UiKit.Muted).Size(30, 80, 0);

            for (var i = 0; i < BagSorts.Length; i++)
            {
                var index = i;
                UiKit.Tb(BagSorts[i], sortRow, i == bagSort, () => { bagSort = index; Redraw(); }, 11).Size(30, 110, 0);
            }

            var list = Scroll(Rest(left));
            var items = new List<ItemStack>();

            foreach (var stack in session.State.Items)
            {
                if (InBagFilter(ItemBook.Get(stack.Item)))
                {
                    items.Add(stack);
                }
            }

            int ByKind(ItemStack a, ItemStack b) => ItemBook.Get(a.Item).Kind != ItemBook.Get(b.Item).Kind ? ItemBook.Get(a.Item).Kind.CompareTo(ItemBook.Get(b.Item).Kind) : string.CompareOrdinal(ItemBook.Get(a.Item).Name, ItemBook.Get(b.Item).Name);

            switch (bagSort)
            {
                case 1: items.Sort((a, b) => string.CompareOrdinal(ItemBook.Get(a.Item).Name, ItemBook.Get(b.Item).Name)); break;
                case 2: items.Sort((a, b) => session.SellPrice(b.Item).CompareTo(session.SellPrice(a.Item))); break;
                case 3: items.Sort((a, b) => b.Count.CompareTo(a.Count)); break;
                default: items.Sort(ByKind); break;
            }

            if (session.Count(pickedItem) <= 0)
            {
                pickedItem = items.Count > 0 ? items[0].Item : null;
            }

            foreach (var stack in items)
            {
                var def = ItemBook.Get(stack.Item);
                var id = stack.Item;
                // What came in since the bag was last looked at is marked new (gold), until the bag is closed.
                var fresh = session.IsFresh(id);
                ListLine(list, def.Seal, def.Name, fresh ? $"новое · × {stack.Count}" : $"× {stack.Count}", UiKit.Hex(def.Colour), id == pickedItem, () => { pickedItem = id; Redraw(); }, fresh ? UiKit.GoldText : UiKit.Text2);
            }

            if (items.Count == 0)
            {
                Para(session.State.Items.Count == 0 ? "Сумка пуста. Добыча падает с врагов, травы растут по всей горе, дикие Гу — над своими местами." : "Таких вещей нет.", list, 15, UiKit.Muted);
            }

            var right = Column(body, 0.56f, 1, 8);
            var picked = ItemBook.Get(pickedItem);

            if (picked == null)
            {
                return;
            }

            UiKit.Zh(picked.Seal, right, 64, UiKit.Hex(picked.Colour)).Size(76);
            UiKit.Disp(picked.Name, right, 28, UiKit.Text).Size(34);
            UiKit.Lbl($"{KindName(picked.Kind)} · в сумке {session.Count(picked.Id)}", right, 11, UiKit.LabelGold).Size(16);
            UiKit.Rule(right);
            Para(picked.Description, right, 16);
            Gap(right, 10);
            var item = picked.Id;

            if (picked.Kind == ItemKind.Consumable)
            {
                UiKit.HexButton("Принять", right, () =>
                {
                    if (!session.UseConsumable(item))
                    {
                        Toast("Сейчас это ни к чему");
                    }

                    Redraw();
                }, UiKit.HexKind.Main, 13, 52);
            }
            else if (picked.Kind == ItemKind.Scroll)
            {
                var known = session.KnowsRecipe(picked.Teaches);
                UiKit.HexButton(known ? "Рецепт уже изучен" : "Прочесть", right, () =>
                {
                    session.ReadScroll(item);
                    Redraw();
                }, known ? UiKit.HexKind.Off : UiKit.HexKind.Main, 13, 52);
            }
            else if (picked.Kind == ItemKind.Book)
            {
                // A manual is read when all its pills are known; a book when its page is in the codex.
                var read = picked.TeachesPills != null && picked.TeachesPills.Length > 0 ? System.Array.TrueForAll(picked.TeachesPills, session.KnowsPill) : session.InCodex(picked.Lore);
                UiKit.HexButton(read ? (picked.TeachesPills != null ? "Рецепты известны" : "Прочитано (в кодексе)") : "Прочесть", right, () =>
                {
                    session.ReadBook(item);
                    Redraw();
                }, read ? UiKit.HexKind.Off : UiKit.HexKind.Main, 13, 52);
            }
            else if (picked.Kind == ItemKind.Food && picked.Heal > 0)
            {
                UiKit.HexButton("Съесть", right, () =>
                {
                    if (!session.UseConsumable(item))
                    {
                        Toast("Вы не голодны");
                    }

                    Redraw();
                }, UiKit.HexKind.Ghost, 12, 44);
            }
            else if (picked.Kind == ItemKind.Rock)
            {
                UiKit.HexButton("Расколоть", right, () =>
                {
                    session.CrackRock(item);
                    Redraw();
                }, UiKit.HexKind.Main, 13, 52);
            }

            if (session.SellPrice(item) > 0)
            {
                Fact(right, "Лавка даст", $"{session.SellPrice(item)} камней", UiKit.GoldText);
            }

            // Who eats it: the Gu whose food it is.
            if (picked.Kind == ItemKind.Food || picked.Kind == ItemKind.Herb || picked.Kind == ItemKind.Material)
            {
                var eaters = new List<string>();

                foreach (var gu in GuBook.All)
                {
                    if (gu.Food == item)
                    {
                        eaters.Add(gu.Name);
                    }
                }

                if (eaters.Count > 0)
                {
                    Para("Корм для: " + string.Join(", ", eaters.GetRange(0, Mathf.Min(6, eaters.Count))) + (eaters.Count > 6 ? $" и ещё {eaters.Count - 6}" : ""), right, 13, UiKit.Muted);
                }
            }

            // Thrown away: held, as the irreversible is (the story's things never).
            if (picked.Kind != ItemKind.Quest && picked.Kind != ItemKind.Key)
            {
                Gap(right, 10);
                var count = session.Count(item);
                UiKit.HexButton(count > 1 ? $"Удерживать — выбросить все {count}" : "Удерживать — выбросить", right, () =>
                {
                    session.RemoveItem(item, session.Count(item));
                    Redraw();
                }, UiKit.HexKind.Bad, 11, 40).Held(1.2f).Size(40, 320, 0);
            }
        }

        int bagFilter, bagSort;
        static readonly string[] BagFilters = { "Все", "Пилюли", "Корм", "Травы", "Материалы", "Свитки", "Прочее" };
        static readonly string[] BagSorts = { "Вид", "Имя", "Цена", "Сколько" };

        bool InBagFilter(ItemDef def)
        {
            switch (bagFilter)
            {
                case 1: return def.Kind == ItemKind.Consumable;
                case 2: return def.Kind == ItemKind.Food;
                case 3: return def.Kind == ItemKind.Herb;
                case 4: return def.Kind == ItemKind.Material || def.Kind == ItemKind.Rock;
                case 5: return def.Kind == ItemKind.Scroll || def.Kind == ItemKind.Book;
                case 6: return def.Kind == ItemKind.Quest || def.Kind == ItemKind.Key || def.Kind == ItemKind.Currency;
                default: return true;
            }
        }

        // ------------------------------------------------------------------ Journal

        /// <summary>Which list the journal shows: what is under way, the boards' errands, what is done.</summary>
        int journalTab;
        BountyState journalBounty;

        /// <summary>
        /// The journal (J, after UI-Journal.dc.html): at the left, what is under way, the errands taken from the
        /// boards (with how far along, and «Оставить»), or what is done; the one picked at the right — where it
        /// stands, what it gives, «Показать на карте»; under it, the clan's merit and its rank.
        /// </summary>
        void JournalPage()
        {
            var session = S;
            var body = Frame("志", "Журнал", $"Глава {session.Chapter} · {Story.ChapterNames[session.Chapter]}", true, null, null, new[] { ("1 2 3", "Списки"), ("Q E", "Страницы"), ("Esc", "Закрыть") });
            var left = Column(body, 0, 0.42f, 6);
            var bounties = session.BountiesTaken();
            var quests = new List<QuestState>(session.ActiveQuests());
            quests.Sort((a, b) => Story.Get(b.Quest).Main.CompareTo(Story.Get(a.Quest).Main));
            var done = new List<QuestDef>();

            foreach (var quest in Story.All)
            {
                if (session.IsDone(quest.Id) && !session.IsActive(quest.Id))
                {
                    done.Add(quest);
                }
            }

            var tabs = UiKit.Rect("Lists", left);
            tabs.Size(36);
            tabs.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = true;
            var names = new[] { $"Сейчас · {quests.Count}", $"Поручения · {bounties.Count}", $"Выполнено · {done.Count}" };

            for (var i = 0; i < names.Length; i++)
            {
                var tab = i;
                UiKit.Tb(names[i], tabs, journalTab == i, () => { journalTab = tab; Redraw(); }, 11).Size(36, -1, 1);
            }

            var list = Scroll(Rest(left));
            var right = Column(body, 0.48f, 1, 8);

            switch (journalTab)
            {
                case 1:
                    JournalBounties(list, right, bounties);
                    break;
                case 2:
                    JournalDone(list, right, done);
                    break;
                default:
                    JournalQuests(list, right, quests);
                    break;
            }

            // The clan's merit (the treasury's coin) and rank, under whatever is shown.
            var merit = UiKit.Panel("Merit", body);
            merit.rectTransform.Place(new Vector2(0.48f, 0), new Vector2(1, 0), 0, 0, 0, -118);
            var inside = UiKit.Rect("Inside", merit.transform).Fill();
            inside.Stack<VerticalLayoutGroup>(4, new RectOffset(20, 20, 12, 10));
            var rank = session.ClanRank;
            var next = rank + 1 < Session.ClanRanks.Length ? Session.ClanRankNeeds[rank + 1] : 0;
            UiKit.Lbl("Клан Гу Юэ · вклад", inside, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp($"{session.State.Merit} вклада · {Session.ClanRanks[rank]}", inside, 20, UiKit.GoldText).Size(26);
            UiKit.Label(next > 0 ? $"Всего заработано {session.State.MeritEarned} · до «{Session.ClanRanks[rank + 1]}» — ещё {Mathf.Max(0, next - session.State.MeritEarned)}" : $"Всего заработано {session.State.MeritEarned} · выше ранга нет",
                inside, 13, UiKit.Muted).Size(18);
            UiKit.Label("Вклад дают задания и поручения клана; казначей в главном зале меняет его на Гу, которых не купить за камни.", inside, 12, UiKit.Muted).Size(18);

            var keys = body.gameObject.AddComponent<PageKeys>();
            keys.On(Key.Digit1, () => { journalTab = 0; Redraw(); });
            keys.On(Key.Digit2, () => { journalTab = 1; Redraw(); });
            keys.On(Key.Digit3, () => { journalTab = 2; Redraw(); });
        }

        void JournalQuests(RectTransform list, RectTransform right, List<QuestState> quests)
        {
            var session = S;

            if (pickedQuest == null || !session.IsActive(pickedQuest))
            {
                pickedQuest = quests.Count > 0 ? quests[0].Quest : null;
            }

            foreach (var quest in quests)
            {
                var def = Story.Get(quest.Quest);
                var id = quest.Quest;
                ListLine(list, def.Main ? "道" : "行", def.Title, def.Main ? $"сюжет · {quest.Step + 1}/{def.Steps.Length}" : $"поручение · {quest.Step + 1}/{def.Steps.Length}", def.Main ? UiKit.GoldText : UiKit.Text2, id == pickedQuest, () => { pickedQuest = id; Redraw(); });
            }

            if (quests.Count == 0)
            {
                Para(session.Has("finished") ? "История рассказана. Доска поручений в деревне всё ещё платит." : "Заданий нет.", list, 15, UiKit.Muted);
            }

            var state = pickedQuest != null ? session.StateOf(pickedQuest) : null;

            if (state == null)
            {
                return;
            }

            var picked = Story.Get(pickedQuest);
            UiKit.Lbl(picked.Main ? $"Глава {picked.Chapter} · {Story.ChapterNames[picked.Chapter]}" : "Поручение", right, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(picked.Title, right, 34, UiKit.Text).Size(42);

            // Where its step leads, on the map (M).
            if (session.MarkerOf(state, out var goal))
            {
                var showRow = UiKit.Rect("Show", right);
                showRow.Size(40);
                var showRowGroup = showRow.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft);
                showRowGroup.childForceExpandWidth = false;
                var target = goal;
                UiKit.HexButton("Показать на карте", showRow, () => { mapFocus = target; mapPick = ("quest", pickedQuest, -1, target); Show(Page.Map); }, UiKit.HexKind.Ghost, 12, 40, "M").Size(40, 240, 0);
            }

            UiKit.Rule(right);
            Para(picked.Summary, right, 16);
            Gap(right, 10);
            UiKit.Lbl("Сейчас", right, 11, UiKit.LabelGold).Size(16);
            Para(session.StepText(state), right, 18, UiKit.GoldText);
            Gap(right, 10);
            UiKit.Lbl($"Шаг {state.Step + 1} из {picked.Steps.Length}", right, 11, UiKit.Muted).Size(16);
            UiKit.Bar(right, UiKit.BarKind.Stamina, state.Step / (float)picked.Steps.Length, 3);
            JournalReward(right, picked.Reward);
        }

        static void JournalReward(RectTransform right, Reward reward)
        {
            var gains = new List<string>();

            if (reward.Stones > 0) { gains.Add($"{reward.Stones} камней"); }
            if (reward.Insight > 0) { gains.Add($"{reward.Insight} прорыва"); }

            foreach (var gu in reward.Gu) { gains.Add(GuBook.Get(gu).Name); }
            foreach (var (item, count) in reward.Items) { gains.Add(ItemBook.Get(item).Name + (count > 1 ? $" × {count}" : "")); }

            if (gains.Count > 0)
            {
                Gap(right, 10);
                UiKit.Lbl("Награда", right, 11, UiKit.LabelGold).Size(16);
                Para(string.Join(" · ", gains), right, 15);
            }
        }

        /// <summary>The errands taken from the boards: how far along each is, what it pays, where its board is; «Оставить» frees its place.</summary>
        void JournalBounties(RectTransform list, RectTransform right, List<BountyState> bounties)
        {
            var session = S;

            if (journalBounty == null || !bounties.Contains(journalBounty))
            {
                journalBounty = bounties.Count > 0 ? bounties[0] : null;
            }

            foreach (var bounty in bounties)
            {
                var one = bounty;
                var ready = session.BountyReady(bounty);
                ListLine(list, ready ? "成" : "任", session.BountyText(bounty), ready ? "готово" : BoardName(bounty), ready ? UiKit.Essence : UiKit.Text2, bounty == journalBounty, () => { journalBounty = one; Redraw(); });
            }

            Para(bounties.Count == 0 ? "Поручений нет. Доски в деревнях каждый день дают новые — до шести сразу." : $"Взято {bounties.Count} из 6. Сдают поручение у той доски, где его взяли.", list, 13, UiKit.Muted);

            if (journalBounty == null)
            {
                return;
            }

            var picked = journalBounty;
            var have = picked.Kind == "kill" ? picked.Progress : session.Count(picked.Target);
            UiKit.Lbl($"Поручение · {BoardName(picked)}", right, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(picked.Kind == "kill" ? Bestiary.Get(picked.Target)?.Name ?? picked.Target : ItemBook.Get(picked.Target)?.Name ?? picked.Target, right, 34, UiKit.Text).Size(42);
            UiKit.Rule(right);
            Para(picked.Kind == "kill" ? $"Победить: {picked.Count}." : $"Принести: {picked.Count} — сдаются из сумки.", right, 16);
            UiKit.Lbl($"Сделано {Mathf.Min(have, picked.Count)} из {picked.Count}", right, 11, UiKit.Muted).Size(16);
            UiKit.Bar(right, UiKit.BarKind.Stamina, Mathf.Clamp01(have / (float)Mathf.Max(1, picked.Count)), 3);
            Para(session.BountyReady(picked) ? "Готово — вернитесь к доске." : $"Осталось {session.BountyDaysLeft(picked)} дн. (взято в день {picked.Day}).", right, 14, session.BountyReady(picked) ? UiKit.Essence : UiKit.Muted);

            if (!session.BountyReady(picked))
            {
                var heroAt = Game.Hero != null ? new MapPoint(Game.Hero.transform.position.x, Game.Hero.transform.position.z) : new MapPoint(0, 0);
                var (where, at) = session.BountyWhere(picked, heroAt);
                Para(where, right, 14, UiKit.Text2);

                if (at != null)
                {
                    var point = at.Value;
                    UiKit.HexButton("Показать на карте", right, () => { mapFocus = point; mapPick = ("point", null, -1, point); Show(Page.Map); }, UiKit.HexKind.Ghost, 11, 40, "M").Size(40, 240, 0);
                }
            }
            Gap(right, 8);
            UiKit.Lbl("Награда", right, 11, UiKit.LabelGold).Size(16);
            Para($"{picked.Stones} камней" + (picked.Insight > 0 ? $" · {picked.Insight} прорыва" : "") + (picked.Reputation > 0 ? $" · репутация +{picked.Reputation}" : ""), right, 15);
            Gap(right, 8);
            UiKit.HexButton("Удерживать — оставить", right, () => { session.AbandonBounty(picked); journalBounty = null; Redraw(); }, UiKit.HexKind.Bad, 11, 40).Held(1.2f).Size(40, 280, 0);
        }

        static string BoardName(BountyState bounty) => Regions.Settlement(bounty.Settlement)?.Name ?? Atlas.Place(bounty.Settlement)?.Name ?? "доска";

        /// <summary>What is done, chapter by chapter: the story's steps and the side quests behind the hero.</summary>
        void JournalDone(RectTransform list, RectTransform right, List<QuestDef> done)
        {
            if (pickedQuest == null || !done.Exists(q => q.Id == pickedQuest))
            {
                pickedQuest = done.Count > 0 ? done[done.Count - 1].Id : null;
            }

            for (var i = done.Count - 1; i >= 0; i--)
            {
                var def = done[i];
                var id = def.Id;
                ListLine(list, def.Main ? "道" : "行", def.Title, def.Main ? $"глава {def.Chapter}" : "поручение", UiKit.Muted, id == pickedQuest, () => { pickedQuest = id; Redraw(); });
            }

            if (done.Count == 0)
            {
                Para("Пока ничего не сделано.", list, 15, UiKit.Muted);
            }

            var picked = done.Find(q => q.Id == pickedQuest);

            if (picked == null)
            {
                return;
            }

            UiKit.Lbl(picked.Main ? $"Глава {picked.Chapter} · {Story.ChapterNames[picked.Chapter]} · выполнено" : "Поручение · выполнено", right, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(picked.Title, right, 34, UiKit.Text2).Size(42);
            UiKit.Rule(right);
            Para(picked.Summary, right, 16, UiKit.Text2);
            JournalReward(right, picked.Reward);
        }

        // ------------------------------------------------------------------ Map

        /// <summary>
        /// The map's colour of a point, one cell across (metres): the ground's own layers darkened, lit
        /// from the north-west so the slopes read, higher a little brighter; rivers blue, roads brown.
        /// The map page and the minimap paint with it. `weights` is scratch (Surface.Layers long).
        /// </summary>
        public static Color MapColour(float x, float z, float cell, float[] weights)
        {
            var plan = Ground.Plan;
            var height = Ground.HeightAt(x, z);
            var dx = Ground.HeightAt(x + cell, z) - height;
            var dz = Ground.HeightAt(x, z + cell) - height;
            Color colour;

            if (plan != null)
            {
                var p = new Core.Gen.Vec2(x, z);
                plan.Surface.Paint(p, height, Mathf.Sqrt(dx * dx + dz * dz) / cell, weights);
                float r = 0, g = 0, b = 0;

                for (var k = 0; k < weights.Length; k++)
                {
                    r += Core.Gen.Shapes.FarColours[k * 3] * weights[k];
                    g += Core.Gen.Shapes.FarColours[k * 3 + 1] * weights[k];
                    b += Core.Gen.Shapes.FarColours[k * 3 + 2] * weights[k];
                }

                colour = new Color(r, g, b) * 0.6f;
            }
            else
            {
                colour = Color.Lerp(UiKit.Hex(0x1C2A26), UiKit.Hex(0x343A40), Mathf.Clamp01((Mathf.Abs(dx) + Mathf.Abs(dz)) / cell * 0.8f));
            }

            colour *= Mathf.Clamp(1 + (-dx + dz) / cell * 0.7f, 0.5f, 1.45f) * (0.85f + Mathf.Clamp01(height / 700) * 0.4f);

            if (Ground.RiverDistance(x, z) < Mathf.Max(cell * 0.8f, 2.5f))
            {
                colour = UiKit.Hex(0x2E6E8C);
            }
            else if (Ground.RoadDistance(x, z) < Mathf.Max(cell * 0.45f, 1.6f))
            {
                colour = UiKit.Hex(0x8C6A3A);
            }

            colour.a = 1;

            return colour;
        }

        static Texture2D MapTexture()
        {
            if (mapTexture != null)
            {
                return mapTexture;
            }

            const int size = 384;
            var half = Ground.Half;
            mapTexture = new Texture2D(size, size, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, name = "Map" };
            var pixels = new Color[size * size];
            var weights = new float[Core.Gen.Surface.Layers];

            for (var j = 0; j < size; j++)
            {
                var z = -half + half * 2 * (j + 0.5f) / size;

                for (var i = 0; i < size; i++)
                {
                    var x = -half + half * 2 * (i + 0.5f) / size;
                    pixels[j * size + i] = MapColour(x, z, half * 2 / size, weights);
                }
            }

            mapTexture.SetPixels(pixels);
            mapTexture.Apply();

            return mapTexture;
        }
    }
}
