using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.World;
using UnityEngine;
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
        static readonly string[] SlotKeys = { "Q", "E", "R", "F" };

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
            Gap(left, 10);
            UiKit.Lbl("Активные слоты", left, 11, UiKit.LabelGold).Size(18);
            var active = UiKit.Rect("Active", left);
            active.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            active.Size(64);

            for (var i = 0; i < 4; i++)
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

            for (var i = 0; i < 3; i++)
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

            if (picked != null)
            {
                GuDetail(right, picked, true);
            }
        }

        /// <summary>Everything about one Gu; with `acts`, the buttons to refine it and put it in a slot.</summary>
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

            if (session.IsSlotted(gu.Uid))
            {
                var uid = gu.Uid;
                UiKit.HexButton("Снять", column, () =>
                {
                    session.Unslot(uid);
                    session.RefreshQuests();
                    Redraw();
                }, UiKit.HexKind.Ghost, 13, 46);
            }
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
                default: return "Камень";
            }
        }

        void BagPage()
        {
            var session = S;
            var body = Frame("宝", "Сумка", $"{session.State.Items.Count} видов вещей", true, null, Purse, new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });
            var left = Column(body, 0, 0.5f, 6);
            UiKit.Lbl("Вещи", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));
            var items = new List<ItemStack>(session.State.Items);
            items.Sort((a, b) => ItemBook.Get(a.Item).Kind != ItemBook.Get(b.Item).Kind ? ItemBook.Get(a.Item).Kind.CompareTo(ItemBook.Get(b.Item).Kind) : string.CompareOrdinal(a.Item, b.Item));

            if (session.Count(pickedItem) <= 0)
            {
                pickedItem = items.Count > 0 ? items[0].Item : null;
            }

            foreach (var stack in items)
            {
                var def = ItemBook.Get(stack.Item);
                var id = stack.Item;
                ListLine(list, def.Seal, def.Name, $"× {stack.Count}", UiKit.Hex(def.Colour), id == pickedItem, () => { pickedItem = id; Redraw(); }, UiKit.Text2);
            }

            if (items.Count == 0)
            {
                Para("Сумка пуста. Добыча падает с врагов и лежит в тайниках.", list, 15, UiKit.Muted);
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
        }

        // ------------------------------------------------------------------ Journal

        void JournalPage()
        {
            var session = S;
            var body = Frame("志", "Журнал", $"Глава {session.Chapter} · {Story.ChapterNames[session.Chapter]}", true, null, null, new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });
            var left = Column(body, 0, 0.42f, 6);
            UiKit.Lbl("Задания", left, 11, UiKit.LabelGold).Size(18);
            var list = Scroll(Rest(left));
            var quests = new List<QuestState>(session.ActiveQuests());
            quests.Sort((a, b) => Story.Get(b.Quest).Main.CompareTo(Story.Get(a.Quest).Main));

            if (pickedQuest == null || !session.IsActive(pickedQuest))
            {
                pickedQuest = quests.Count > 0 ? quests[0].Quest : null;
            }

            foreach (var quest in quests)
            {
                var def = Story.Get(quest.Quest);
                var id = quest.Quest;
                ListLine(list, def.Main ? "道" : "行", def.Title, def.Main ? "сюжет" : "поручение", def.Main ? UiKit.GoldText : UiKit.Text2, id == pickedQuest, () => { pickedQuest = id; Redraw(); });
            }

            if (quests.Count == 0)
            {
                Para(session.Has("finished") ? "История рассказана. Доска поручений в деревне всё ещё платит." : "Заданий нет.", list, 15, UiKit.Muted);
            }

            var right = Column(body, 0.48f, 1, 8);
            var state = pickedQuest != null ? session.StateOf(pickedQuest) : null;

            if (state == null)
            {
                return;
            }

            var picked = Story.Get(pickedQuest);
            UiKit.Lbl(picked.Main ? $"Глава {picked.Chapter} · {Story.ChapterNames[picked.Chapter]}" : "Поручение", right, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(picked.Title, right, 34, UiKit.Text).Size(42);
            UiKit.Rule(right);
            Para(picked.Summary, right, 16);
            Gap(right, 10);
            UiKit.Lbl("Сейчас", right, 11, UiKit.LabelGold).Size(16);
            Para(session.StepText(state), right, 18, UiKit.GoldText);
            Gap(right, 10);
            UiKit.Lbl($"Шаг {state.Step + 1} из {picked.Steps.Length}", right, 11, UiKit.Muted).Size(16);
            UiKit.Bar(right, UiKit.BarKind.Stamina, state.Step / (float)picked.Steps.Length, 3);
            var reward = picked.Reward;
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

        // ------------------------------------------------------------------ Map

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
            var plan = Ground.Plan;
            var weights = new float[Core.Gen.Surface.Layers];
            var water = UiKit.Hex(0x2E6E8C);
            var road = UiKit.Hex(0x8C6A3A);

            for (var j = 0; j < size; j++)
            {
                var z = -half + half * 2 * (j + 0.5f) / size;

                for (var i = 0; i < size; i++)
                {
                    var x = -half + half * 2 * (i + 0.5f) / size;
                    var cell = half * 2 / size;
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

                    // Lit from the north-west, so the mountain reads.
                    colour *= Mathf.Clamp(1 + (-dx + dz) / cell * 0.7f, 0.5f, 1.45f) * (0.85f + Mathf.Clamp01(height / 700) * 0.4f);

                    if (Ground.RiverDistance(x, z) < cell * 0.8f)
                    {
                        colour = water;
                    }
                    else if (Ground.RoadDistance(x, z) < cell * 0.45f)
                    {
                        colour = road;
                    }

                    pixels[j * size + i] = colour;
                }
            }

            mapTexture.SetPixels(pixels);
            mapTexture.Apply();

            return mapTexture;
        }

        void MapPage()
        {
            var session = S;
            var hero = Game.Hero;
            var here = Valley.PlaceAt(new MapPoint(hero.transform.position.x, hero.transform.position.z));
            var body = Frame("图", "Карта", here != null ? here.Name : "Долина Цинмао", true, null, null, new[] { ("Q E", "Страницы"), ("Esc", "Закрыть") });
            const float side = 700;
            var frame = UiKit.Panel("Map", body, UiKit.Ink, UiKit.Gold.A(0.25f));
            frame.rectTransform.At(new Vector2(0, 0.5f), new Vector2(side * 0.5f + 20, 0), new Vector2(side + 16, side + 16));
            var picture = UiKit.Rect("Picture", frame.transform).gameObject.AddComponent<RawImage>();
            picture.texture = MapTexture();
            picture.rectTransform.Fill(8);
            picture.raycastTarget = false;
            var over = UiKit.Rect("Marks", frame.transform);
            over.Fill(8);

            Vector2 On(MapPoint point) => new Vector2((point.X + Ground.Half) / (Ground.Half * 2), (point.Z + Ground.Half) / (Ground.Half * 2));

            foreach (var place in Valley.Places)
            {
                if (place.Id == "yard" || place.Id == "forest_edge" || (place.Cave && !session.Has(place.Id == "hermit_cave" ? "hermit_open" : "blood_open")) || (Ground.Big && place.Radius < 30))
                {
                    continue;
                }

                var label = UiKit.Lbl(place.Name, over, 10, UiKit.Hex(place.Colour), 0.1f, TextAnchor.MiddleCenter).Shadowed();
                label.rectTransform.At(On(place.At), new Vector2(0, 12), new Vector2(220, 16));
            }

            if (Ground.Big)
            {
                foreach (var settlement in Core.Regions.Settlements)
                {
                    var label = UiKit.Lbl(settlement.Name, over, 10, UiKit.Hex(settlement.Colour), 0.1f, TextAnchor.MiddleCenter).Shadowed();
                    label.rectTransform.At(On(settlement.At), new Vector2(0, 10), new Vector2(220, 16));
                    var dot = UiKit.Image("Settlement", over, UiKit.Hex(settlement.Colour), UiKit.Diamond);
                    dot.rectTransform.At(On(settlement.At), Vector2.zero, new Vector2(8, 8));
                }

                foreach (var place in Core.Regions.Places)
                {
                    var label = UiKit.Lbl(place.Name, over, 9, UiKit.Hex(place.Colour).A(0.8f), 0.1f, TextAnchor.MiddleCenter).Shadowed();
                    label.rectTransform.At(On(place.At), new Vector2(0, 10), new Vector2(220, 14));
                }
            }

            foreach (var thing in Valley.Things)
            {
                if (thing.Kind == ThingKind.Lantern)
                {
                    var dot = UiKit.Image("Lantern", over, thing.Id == session.State.Lantern ? UiKit.Moon : UiKit.Moon.A(0.45f), UiKit.Disc);
                    dot.rectTransform.At(On(thing.At), Vector2.zero, new Vector2(7, 7));
                }
            }

            var quest = session.MainQuest;

            if (quest != null && session.MarkerOf(quest, out var mark))
            {
                var ring = UiKit.Image("Quest", over, UiKit.Gold, UiKit.Ring);
                ring.rectTransform.At(On(mark), Vector2.zero, new Vector2(26, 26));
                ring.gameObject.AddComponent<Pulse>();
                var gem = UiKit.Image("Gem", over, UiKit.GoldLit, UiKit.Diamond);
                gem.rectTransform.At(On(mark), Vector2.zero, new Vector2(10, 10));
            }

            var me = UiKit.Image("Hero", over, UiKit.Essence, UiKit.Diamond);
            me.rectTransform.At(On(new MapPoint(hero.transform.position.x, hero.transform.position.z)), Vector2.zero, new Vector2(14, 14));
            UiKit.HaloAt(me.transform, UiKit.Essence.A(0.6f), Vector2.zero, 40);

            var right = UiKit.Rect("Legend", body);
            right.Place(new Vector2(0, 0), new Vector2(1, 1), side + 80, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(8);
            UiKit.Lbl("Вы здесь", right, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(here != null ? here.Name : "Долина", right, 30, UiKit.Text).Size(36);
            Gap(right, 8);

            if (quest != null)
            {
                UiKit.Lbl("Задание", right, 11, UiKit.LabelGold).Size(16);
                UiKit.Disp(Story.Get(quest.Quest).Title, right, 22, UiKit.GoldText).Size(28);
                Para(session.StepText(quest), right, 15);
                Gap(right, 8);
            }

            UiKit.Rule(right);
            Fact(right, "Золотое кольцо", "куда ведёт задание", UiKit.GoldText);
            Fact(right, "Бирюзовый ромб", "вы", UiKit.Essence);
            Fact(right, "Голубые точки", "лунные фонари", UiKit.Moon);
            Gap(right, 8);
            Para("Дороги ведут из деревни: на север — к воротам, опушке и Волчьему гребню; на запад — в бамбуковую рощу; на восток — к броду и водопаду; на юг — к тракту и пограничному камню.", right, 14, UiKit.Muted);
        }
    }
}
