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
    /// The aperture page (UI-Aperture.dc.html): at the left the rank — its numeral big, its
    /// stage, its metal — the sea of essence as a number and a bar, the aperture's numbers, the
    /// stone and the breakthrough; in the middle the sea itself, filled to its share with a
    /// wave on top, in slow gold rings with eight round cells around it — the Gu refined into
    /// it, the empty cells, the ones still closed to this rank — and under it the Gu at hand
    /// (1–4); at the right the chosen cell (a Gu's hunger, tiredness and food; an empty cell's Gu
    /// to refine from the bag) and the feeding of all of them.
    /// </summary>
    public partial class Menus
    {
        const float RingRadius = 252;
        const float RingCell = 84;

        int ringChosen;

        /// <summary>The ranks' metals, as the essence of each is called.</summary>
        static readonly string[] RankMetals = { "зелёная медь", "красная сталь", "белое серебро", "жёлтое золото", "пурпурный кристалл" };

        void AperturePage()
        {
            var gu = host.Cultivator;
            var a = gu.Aperture;
            var body = BookFrame("ring", "Апертура", Page.Aperture);
            var rank = Aperture.RankColour(a.Rank);
            var brush = UiKit.Brush("竅", bookStage, 600, UiKit.Essence.A(0.035f));
            brush.rectTransform.anchorMin = brush.rectTransform.anchorMax = new Vector2(0.5f, 1);
            brush.rectTransform.anchoredPosition = new Vector2(0, -100 - 300);
            brush.transform.SetSiblingIndex(1);

            // The rank and the sea's numbers, at the left.
            var left = UiKit.Rect("Rank", body);
            left.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -320, 0);
            left.Stack<VerticalLayoutGroup>(18);
            var head = UiKit.Rect("Head", left);
            var headRow = head.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.LowerLeft);
            headRow.childForceExpandWidth = false;
            head.Size(90);
            var numeral = UiKit.Disp(Aperture.RankNumeral(a.Rank), head, 96, UiKit.GoldLit, TextAnchor.LowerLeft);
            numeral.horizontalOverflow = HorizontalWrapMode.Overflow;
            numeral.Size(90, numeral.preferredWidth + 6, 0);
            var words = UiKit.Rect("Words", head);
            words.Stack<VerticalLayoutGroup>(4, new RectOffset(0, 0, 0, 10), TextAnchor.LowerLeft);
            words.Size(90, 200, 0);
            UiKit.Lbl("Ранг", words).Size(14);
            UiKit.Disp(Upper(Aperture.Name(a.Stage)), words, 22).Size(24);
            UiKit.Label(RankMetals[Mathf.Clamp(a.Rank - 1, 0, 4)], words, 12, rank).Size(16);

            var sea = UiKit.Rect("Sea", left);
            sea.Stack<VerticalLayoutGroup>(8);
            sea.Size(84);
            var seaHead = UiKit.Rect("Head", sea);
            seaHead.Size(36);
            UiKit.Lbl("Море эссенции", seaHead).rectTransform.Fill();
            UiKit.Disp($"{a.Essence:0}%", seaHead, 34, UiKit.EssenceLit, TextAnchor.LowerRight).rectTransform.Fill();
            UiKit.Bar(sea, UiKit.BarKind.Essence, a.Essence / 100);
            UiKit.Label($"предел {a.Cap:0}% · +{a.RecoveryPerHour:0.#}% в час · полное за {Talents.Span(Aperture.RefillHours(a.Talent))}", sea, 12, UiKit.Muted).Size(16);

            var stats = UiKit.Rect("Stats", left);
            stats.Stack<VerticalLayoutGroup>(0);
            Stat(stats, "Талант", a.Extreme ? "Десять Крайностей · 100%" : $"класс {a.Grade} · {a.Talent:0}%");
            Stat(stats, "Плотность эссенции", $"×{a.Density:0.##}" + (a.Refined ? " · винный червь" : ""));
            Stat(stats, "Сила · крепость", $"×{gu.FistPower:0.##} · ×{gu.ToughnessBoost:0.##}");
            Stat(stats, "К след. ступени", $"{a.Progress * 100:0}% · {a.StageCost:0}% эссенции", UiKit.GoldLit);
            var atPeak = a.Stage == Stage.Peak && a.Rank < 5;

            if (atPeak && a.Progress >= 1)
            {
                Stat(stats, "Закалка апертуры", $"{a.Readiness * 100:0}%", UiKit.GoldLit);
                Stat(stats, "Шанс прорыва", $"{gu.BreakChance * 100:0.##}%", UiKit.Hex(0xF0C868));
            }

            UiKit.Rect("Space", left).Size(-1).flexibleHeight = 1;
            UiKit.HexButton("Поглотить камень", left, AbsorbFromPage, gu.Stones > 0 ? UiKit.HexKind.Solid : UiKit.HexKind.Off, 12, 52, "V").Size(52);

            // From the peak of a rank, the walls worn through: a try at the next, the whole sea spent on it.
            if (atPeak && a.Progress >= 1)
            {
                var tryBreak = UiKit.HexButton(a.CanBreakThrough ? $"Прорыв · шанс {gu.BreakChance * 100:0.#}%" : "Прорыв — нужно полное море", left, () =>
                {
                    host.Cultivator.BreakThrough();
                    Show(Page.Aperture);
                }, a.CanBreakThrough ? UiKit.HexKind.Main : UiKit.HexKind.Off, 12, 52);
                tryBreak.Size(52);

                if (a.CanBreakThrough)
                {
                    tryBreak.Held(1.2f);
                }

                UiKit.Label($"Провал сжигает всё море. {Aperture.BaseChance(a.Rank) * 100:0.#}% × талант {a.TalentFactor:0.##} + закалка {a.TemperingBonus * 100:0.#}% + " +
                            $"опыт неудач {a.InsightBonus * 100:0.#}% + дух {gu.BreakBonus * 100:0.#}%.", left, 11, UiKit.Muted, TextAnchor.UpperLeft).Size(30);
            }
            else
            {
                UiKit.Label(atPeak
                    ? "Пик ранга: медитируйте (X), пока стенки апертуры не истончатся до 100%, — тогда можно пытаться прорваться."
                    : "Ступень растёт в медитации (сидя, X) или от реликтового Гу; с пика — прорыв полным морем, и удаётся он редко.",
                    left, 12, UiKit.Muted, TextAnchor.UpperLeft).Size(32);
            }

            // The ring, in the middle.
            var ring = UiKit.Rect("Ring", body);
            ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 1);
            ring.pivot = new Vector2(0.5f, 1);
            ring.anchoredPosition = new Vector2(0, 12);
            ring.sizeDelta = new Vector2(600, 600);
            SeaRing(ring, gu);

            // The Gu at hand, under the ring.
            var bar = UiKit.Rect("Bar", body);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0);
            bar.pivot = new Vector2(0.5f, 0);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(600, 90);

            for (var i = 0; i < Cultivator.BarSize; i++)
            {
                var key = i;
                var cell = UiKit.Cell(bar, 64, () =>
                {
                    host.Cultivator.Unbind(key);
                    Show(Page.Aperture);
                });
                cell.Back.rectTransform.anchorMin = cell.Back.rectTransform.anchorMax = new Vector2(0.5f, 1);
                cell.Back.rectTransform.anchoredPosition = new Vector2((i - (Cultivator.BarSize - 1) * 0.5f) * 74, -32);
                cell.Key.text = $"{i + 1}";
                cell.Key.color = UiKit.Text2;
                ShowEntry(cell, gu, gu.Bar[i], false);
            }

            var caption = UiKit.Rect("Caption", bar);
            caption.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -20);
            var captionRow = caption.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleCenter);
            captionRow.childForceExpandWidth = false;
            UiKit.Label("Гу под рукой ·", caption, 11, UiKit.Muted).Size(20);
            UiKit.Key("1", caption);
            UiKit.Label("–", caption, 11, UiKit.Muted).Size(20, 8, 0);
            UiKit.Key("4", caption);
            UiKit.Label("· щелчок убирает", caption, 11, UiKit.Muted).Size(20);

            // The chosen cell and the feeding, at the right.
            var right = UiKit.Rect("Chosen", body);
            right.Place(new Vector2(1, 0), Vector2.one, -330, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(12);
            ChosenSlot(right, gu);
            UiKit.Rect("Space", right).Size(-1).flexibleHeight = 1;
            var feeding = UiKit.Rect("Feeding", right);
            feeding.Stack<VerticalLayoutGroup>(10, new RectOffset(0, 0, 14, 0));
            feeding.Size(104);
            UiKit.Image("Rule", feeding, UiKit.Gold.A(0.18f), UiKit.Plain).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            feeding.GetChild(0).GetComponent<RectTransform>().Place(new Vector2(0, 1), Vector2.one, 0, -1, 0, 0);
            var feedHead = UiKit.Rect("Head", feeding);
            feedHead.Size(16);
            UiKit.Lbl("Кормление", feedHead).rectTransform.Fill();
            var hungry = 0;

            foreach (var owned in gu.Gu)
            {
                hungry += gu.HungerOf(owned) >= Hunger.Hungry ? 1 : 0;
            }

            UiKit.Label(gu.Gu.Count == 0 ? "в апертуре нет Гу" : hungry > 0 ? $"голодных Гу: {hungry}" : "все сыты", feedHead, 12,
                hungry > 0 ? UiKit.Hex(0xF0C868) : UiKit.Muted, TextAnchor.MiddleRight).rectTransform.Fill();
            UiKit.HexButton("Накормить всех", feeding, () =>
            {
                host.Cultivator.FeedAll();
                Show(Page.Aperture);
            }, gu.Gu.Count > 0 ? UiKit.HexKind.Main : UiKit.HexKind.Off, 12, 52).Size(52);

            UiKit.Footer(bookStage, new[] { ("ЛКМ", "Ячейка"), ("1–4", "На панель"), ("V", "Камень"), ("Q+E", "Страница"), ("Esc", "Закрыть") });
            bookStage.GetComponent<PageKeys>().On(Key.V, AbsorbFromPage);
        }

        /// <summary>A stone drawn in: three seconds in the world, so back to it.</summary>
        void AbsorbFromPage()
        {
            if (host.Cultivator.StartAbsorbing())
            {
                host.Resume();
            }
            else
            {
                Show(Page.Aperture);
            }
        }

        static string Upper(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        /// <summary>
        /// The sea in its rings, 600 across: a slow gold ring with a diamond riding it, a dashed one turning back, spokes
        /// to the cells, a breathing glow, the sea filled to its share with a wave, its share in numbers; the eight cells.
        /// </summary>
        void SeaRing(RectTransform ring, Cultivator gu)
        {
            var a = gu.Aperture;
            var outer = UiKit.Orbit(ring, 600, UiKit.Gold.A(0.16f), 180, 0);
            var rider = UiKit.Image("Rider", outer.transform, UiKit.Gold, UiKit.Diamond);
            rider.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(9, 9));
            UiKit.HaloAt(rider.transform, UiKit.Gold.A(0.5f), Vector2.zero, 28);
            UiKit.Orbit(ring, 528, UiKit.Gold.A(0.14f), -120, 4);

            for (var i = 0; i < Cultivator.Ring; i++)
            {
                var angle = 90 - i * 360f / Cultivator.Ring;
                var spoke = UiKit.Image("Spoke", ring, new Color(160 / 255f, 240 / 255f, 220 / 255f, 0.1f), UiKit.Plain);
                spoke.rectTransform.anchorMin = spoke.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                spoke.rectTransform.pivot = new Vector2(0, 0.5f);
                spoke.rectTransform.anchoredPosition = Vector2.zero;
                spoke.rectTransform.sizeDelta = new Vector2(RingRadius, 1);
                spoke.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
            }

            UiKit.Orbit(ring, 428, new Color(160 / 255f, 240 / 255f, 220 / 255f, 0.12f), 0, 0);
            var glow = UiKit.Image("Glow", ring, UiKit.Essence.A(0.45f), UiKit.Radial);
            glow.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 400));
            var breathe = glow.gameObject.AddComponent<Pulse>();
            breathe.Period = 6;
            breathe.Low = 0.5f;
            breathe.High = 0.95f;
            breathe.Grow = 0.06f;

            // The sea: dark water space, filled from the foot to the share, a wave on its top.
            var water = UiKit.Rect("Sea", ring);
            water.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
            UiKit.Image("Deep", water, new Color(2 / 255f, 8 / 255f, 12 / 255f, 0.9f), UiKit.Disc).rectTransform.Fill(10);
            var share = Mathf.Clamp01(a.Essence / 100);
            var fill = UiKit.Image("Water", water, Color.white, UiKit.Disc);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = share;
            fill.rectTransform.Fill(10);
            fill.Gradient(UiKit.Hex(0x7FE0C8, 0.85f), UiKit.Hex(0x1F6E5E, 0.95f));
            var level = -100 + 200 * share;
            var half = Mathf.Sqrt(Mathf.Max(0, 100 * 100 - level * level));

            if (half > 6)
            {
                var wave = UiKit.Rect("Wave", water).gameObject.AddComponent<RawImage>();
                wave.texture = WaveTexture();
                wave.color = UiKit.Hex(0xE6FFF8, 0.8f);
                wave.raycastTarget = false;
                wave.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, level), new Vector2(half * 2, 10));
                var run = wave.gameObject.AddComponent<WaveRun>();
                run.Period = 5;
                run.Across = half * 2 / 80f;
            }

            var rim = UiKit.Image("Rim", water, UiKit.Essence, UiKit.RingLine(202));
            rim.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(202, 202));
            UiKit.Glow(rim.transform, UiKit.Essence.A(0.15f), 8);
            UiKit.Orbit(water, 214, UiKit.Gold.A(0.4f), 0, 5);
            var number = UiKit.Disp($"{a.Essence:0}%", ring, 46, Color.white, TextAnchor.MiddleCenter).Shadowed();
            number.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(200, 50));
            var refill = UiKit.Lbl(a.Essence >= a.Cap - 0.5f ? "Море полно" : "Море полнится", ring, 10, UiKit.Hex(0xDFFFF6), 0.34f, TextAnchor.MiddleCenter).Shadowed();
            refill.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(240, 14));

            // The eight cells.
            for (var i = 0; i < Cultivator.Ring; i++)
            {
                var slot = i;
                var angle = (90 - i * 360f / Cultivator.Ring) * Mathf.Deg2Rad;
                RingSlot(ring, gu, slot, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RingRadius, slot == ringChosen, () =>
                {
                    ringChosen = slot;
                    Show(Page.Aperture);
                });
            }
        }

        /// <summary>A round cell of the ring: its Gu ringed in its rank's colour (and a hunger dot), empty (+), or closed to this rank (a lock); its name under it.</summary>
        static void RingSlot(RectTransform ring, Cultivator gu, int slot, Vector2 at, bool chosen, System.Action pick)
        {
            var owned = gu.At(slot);
            var locked = slot >= gu.Open;
            var kind = owned?.Kind;
            var edge = chosen ? UiKit.GoldLit : kind != null ? Aperture.RankColour(kind.Rank) : new Color(200 / 255f, 240 / 255f, 255 / 255f, 1);
            var disc = UiKit.Node(ring, RingCell, edge);
            var node = (RectTransform)disc.transform.parent;
            node.anchorMin = node.anchorMax = new Vector2(0.5f, 0.5f);
            node.anchoredPosition = at;
            disc.raycastTarget = true;
            disc.gameObject.AddComponent<Clickable>().Click = pick;
            node.GetComponent<Image>().color = edge.A(chosen ? 0.35f : 0.1f);

            if (chosen)
            {
                UiKit.HaloAt(node, UiKit.GoldLit.A(0.25f), Vector2.zero, RingCell + 40).transform.SetAsFirstSibling();
            }

            if (locked)
            {
                var lockMark = UiKit.Icon("lock", node, 22, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.3f));
                lockMark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            }
            else if (kind == null)
            {
                var plus = UiKit.Label("+", node, 26, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.35f), TextAnchor.MiddleCenter);
                plus.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            }
            else
            {
                var hunger = gu.HungerOf(owned);
                var mark = UiKit.Icon(kind.Icon, node, 50, hunger >= Hunger.Starving ? UiKit.Muted : Icons.Tint(kind.Icon));
                mark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 50));
                var dotColour = hunger >= Hunger.Starving ? UiKit.Danger : hunger >= Hunger.Hungry ? UiKit.Hex(0xF0C868) : UiKit.Ok;
                var ringDot = UiKit.Image("Hunger Edge", node, UiKit.Hex(0x020309), UiKit.Disc);
                ringDot.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(30, 30), new Vector2(14, 14));
                var dot = UiKit.Image("Hunger", node, dotColour, UiKit.Disc);
                dot.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(30, 30), new Vector2(10, 10));

                if (owned.vital)
                {
                    var heart = UiKit.Icon("heart", node, 14, UiKit.Danger);
                    heart.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-30, 30), new Vector2(14, 14));
                }
            }

            var name = UiKit.Label(locked ? "закрыто" : kind == null ? "пусто" : GuSlot.Short(kind.Name), node, 11,
                locked || kind == null ? UiKit.Muted.A(0.7f) : chosen ? UiKit.GoldLit : UiKit.Text2, TextAnchor.UpperCenter).Shadowed();
            name.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -RingCell / 2 - 14), new Vector2(130, 16));
        }

        void ChosenSlot(Transform details, Cultivator gu)
        {
            var owned = gu.At(ringChosen);

            if (ringChosen >= gu.Open)
            {
                var need = ringChosen >= Cultivator.OpenSlots(2) ? 3 : 2;
                UiKit.Lbl($"Ячейка {ringChosen + 1} · закрыта", details).Size(14);
                UiKit.Disp("Закрыто", details, 34, UiKit.Text2).Size(36);
                UiKit.Lbl($"Нужен ранг {Aperture.RankNumeral(need)}", details, 12, UiKit.Muted, 0.2f).Size(16);
                UiKit.Label("Ячейка откроется, когда апертура вырастет до этого ранга.", details, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(44);

                return;
            }

            if (owned == null)
            {
                UiKit.Lbl($"Ячейка {ringChosen + 1} · свободна", details).Size(14);
                UiKit.Disp("Пустая ячейка", details, 34).Size(36);
                var any = false;

                for (var cell = 0; cell < Bag.Cells; cell++)
                {
                    var stack = gu.Bag[cell];

                    if (stack?.Kind?.Use != ItemUse.Refine)
                    {
                        continue;
                    }

                    if (!any)
                    {
                        UiKit.Label("Очистить Гу из сумки — он займёт эту ячейку и станет вашим:", details, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(40);
                        any = true;
                    }

                    var index = cell;
                    var kind = GuBook.Find(stack.Kind.Gu);
                    var price = gu.RefineCostOf(kind);
                    UiKit.Tb($"{kind.Name} · {price:0.#}%", details, false, () =>
                    {
                        host.Cultivator.Refine(index);
                        Show(Page.Aperture);
                    }, 11).Size(38);
                }

                if (!any)
                {
                    UiKit.Label("В сумке нет Гу. Их продают в лавке на площади.", details, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(40);
                }

                return;
            }

            var gk = owned.Kind;
            var hunger = gu.HungerOf(owned);
            var food = ItemBook.Find(gk.Food);
            UiKit.Lbl($"Ячейка {ringChosen + 1}" + (owned.vital ? " · жизненный Гу" : ""), details).Size(14);
            var name = UiKit.Disp(gk.Name, details, 34);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiKit.Lbl($"Ранг {Aperture.RankNumeral(gk.Rank)} · {gk.Kind}", details, 12, Aperture.RankColour(gk.Rank), 0.2f).Size(16);
            var about = UiKit.Label(ItemBook.Find(gk.Item)?.About ?? gk.About ?? "", details, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.lineSpacing = 1.15f;
            about.Size(64);

            if (food != null)
            {
                var due = gu.MealIn(owned);
                var colour = hunger >= Hunger.Starving ? UiKit.Danger : hunger >= Hunger.Hungry ? UiKit.Hex(0xF0C868) : UiKit.Ok;
                var full = owned.Kind.MealEvery > 0 ? Mathf.Clamp01((float)(due / owned.Kind.MealEvery)) : 1;
                Gauge(details, "Сытость", due > 0 ? $"{GuBook.Name(hunger)} · еда через {Span(due)}" : $"{GuBook.Name(hunger)} · просрочена на {Span(-due)}", full, colour);
            }

            if (!gk.Passive)
            {
                Gauge(details, "Усталость", owned.tired >= 100 ? $"{owned.tired:0} — через силу, гибель {Cultivator.StrainChance(owned.tired) * 100:0}%" : $"{owned.tired:0} из 100",
                    owned.tired / 100, owned.tired >= 100 ? UiKit.Danger : UiKit.Moon);
            }

            var stats = UiKit.Rect("Stats", details);
            stats.Stack<VerticalLayoutGroup>(0);
            Stat(stats, "Навык", gk.SkillName);

            if (!gk.Passive)
            {
                Stat(stats, "Вам стоит", gk.Held ? $"{gu.CostOf(gk):0.#}% · {gu.UpkeepOf(gk):0.##}%/с" : $"{gu.CostOf(gk):0.#}% эссенции", UiKit.EssenceLit);
            }

            if (food != null)
            {
                Stat(stats, "Пища", $"{food.Name.ToLowerInvariant()} ×{gk.Meal} · в сумке {gu.Bag.Count(gk.Food)}");
            }

            // Fusions this Gu goes into: what they make, and whether all is there.
            foreach (var recipe in GuBook.RecipesFor(gk.Id))
            {
                if (System.Array.IndexOf(recipe.Gu, gk.Id) < 0)
                {
                    continue;
                }

                var result = GuBook.Find(recipe.Result);
                var lacks = gu.Lacks(recipe);
                UiKit.Label(lacks == null
                        ? $"<color=#9FEFC4>Слияние готово:</color> {result.Name} · {gu.FuseChance(recipe) * 100:0}% · U"
                        : $"Слияние в {result.Name}: не хватает {lacks}", details, 12, UiKit.Muted, TextAnchor.UpperLeft).Size(32);
            }

            var chosenGu = owned;
            UiKit.HexButton("Накормить", details, () =>
            {
                host.Cultivator.Feed(chosenGu);
                Show(Page.Aperture);
            }, food != null ? UiKit.HexKind.Solid : UiKit.HexKind.Off, 12, 44).Size(44, 200, 0);
        }

        /// <summary>A named bar with its words at the right (the chosen Gu's fullness and tiredness).</summary>
        static void Gauge(Transform parent, string name, string value, float share, Color colour)
        {
            var holder = UiKit.Rect(name, parent);
            holder.Stack<VerticalLayoutGroup>(6);
            holder.Size(30);
            var line = UiKit.Rect("Line", holder);
            line.Size(18);
            UiKit.Label(name, line, 13, UiKit.Muted).rectTransform.Fill();
            UiKit.Label(value, line, 12, colour, TextAnchor.MiddleRight).rectTransform.Fill();
            var track = UiKit.Image("Track", holder, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), UiKit.Plain);
            track.Size(4);
            var fill = UiKit.Image("Fill", track.transform, colour, UiKit.Plain);
            fill.rectTransform.Place(Vector2.zero, new Vector2(Mathf.Clamp01(share), 1));
            UiKit.Glow(fill.transform, colour.A(0.3f), 6);
        }
    }
}
