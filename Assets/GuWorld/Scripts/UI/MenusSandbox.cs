using GuWorld.Cultivation;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The immortal's aperture's page (Tab): every Gu there is, as a grid of marks — a click
    /// puts the one clicked in the chosen cell at hand (the eight cells across the top, 1–8),
    /// the pointer over one tells what it is and does — and on the left the modes: immortal or
    /// mortal, an endless or a true sea, tiring and hungry Gu or not, the rank, the small
    /// realm, the talent, the hour, wolves called up or sent away. No ring, no refining: a Gu
    /// put at hand is ready to use.
    /// </summary>
    public partial class Menus
    {
        /// <summary>The grid's rank filter (0: all) and where it was scrolled to.</summary>
        int arsenalRank;
        float arsenalScroll;
        Text arsenalInfo;

        void SandboxPage()
        {
            var gu = host.Cultivator;
            var a = gu.Aperture;
            var body = BookFrame("ring", "Апертура бессмертного", Page.Sandbox, "Полигон · ничего не сохраняется");
            var ring = UiKit.Rect("Ring", bookStage);
            ring.TopLeft(640, 160, 760, 760);
            UiKit.Orbit(ring, 760, UiKit.Gold.A(0.08f), 240, 0);
            ring.SetSiblingIndex(1);

            // ── The modes, at the left, scrolling ──
            var left = UiKit.Rect("Modes", body);
            left.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -360, 0);
            left.Stack<VerticalLayoutGroup>(8);
            var modes = Scroll(left, 0);
            modes.GetComponent<VerticalLayoutGroup>().spacing = 4;

            void Toggle(string name, bool on, string yes, string no, System.Action<bool> set)
            {
                Heading(modes, name);
                UiKit.Choices(modes, new[] { yes, no }, on ? 0 : 1, i =>
                {
                    set(i == 0);
                    Show(Page.Sandbox);
                }, 14);
            }

            Toggle("Герой", host.Health.Immortal, "Бессмертный", "Смертный", on => host.Health.Immortal = on);
            Toggle("Эссенция", gu.EndlessEssence, "Бесконечная", "Как у таланта", on => gu.EndlessEssence = on);
            Toggle("Усталость Гу", !gu.Untiring, "Есть (может убить)", "Нет", on => gu.Untiring = !on);
            Toggle("Голод Гу", !gu.Sated, "Есть", "Нет", on => gu.Sated = !on);

            Heading(modes, $"Ранг · {a.Rank}, {Aperture.Name(a.Stage)}");
            UiKit.Choices(modes, new[] { "I", "II", "III", "IV", "V" }, a.Rank - 1, i =>
            {
                host.Cultivator.SetRealm(i + 1, host.Cultivator.Aperture.Stage);
                Show(Page.Sandbox);
            }, 14);
            UiKit.Choices(modes, new[] { "Начальная", "Средняя", "Верхняя", "Пик" }, (int)a.Stage, i =>
            {
                host.Cultivator.SetRealm(host.Cultivator.Aperture.Rank, (Stage)i);
                Show(Page.Sandbox);
            }, 13);

            Heading(modes, $"Талант · {a.TalentName}");
            float[] talents = { 30, 50, 70, 88, 99, 100 };
            UiKit.Choices(modes, new[] { "D 30", "C 50", "B 70", "A 88", "A 99", "100" }, System.Array.FindIndex(talents, t => Mathf.Abs(t - a.Talent) < 0.5f), i =>
            {
                host.Cultivator.SetTalent(talents[i]);
                host.Cultivator.Aperture.Essence = host.Cultivator.Aperture.Cap;
                Show(Page.Sandbox);
            }, 13);

            Heading(modes, "Время");
            float[] hours = { 7, 13, 19, 23 };
            UiKit.Choices(modes, new[] { "Утро", "День", "Вечер", "Ночь" }, -1, i =>
            {
                host.SetHour(hours[i]);
                Show(Page.Sandbox);
            }, 13);

            Heading(modes, $"Враги · живых: {LivingWolves()}");
            var g = GuWorld.Creatures.WolfKinds.Grey;
            var l = GuWorld.Creatures.WolfKinds.Lightning;
            UiKit.Choices(modes, new[] { "Серые ×3", "Молниевые ×3", "Убрать всех" }, -1, i =>
            {
                if (i == 2)
                {
                    host.ClearWolves();
                    Notify("Волки убраны");
                }
                else
                {
                    Notify($"Волков пришло: {host.SpawnPack(i == 0 ? g : l, 3)}");
                }

                host.Resume();
            }, 12);
            // The beast kings, each with its pack.
            var kings = new[] { GuWorld.Creatures.WolfKinds.Bold, GuWorld.Creatures.WolfKinds.Frenzy, GuWorld.Creatures.WolfKinds.ThunderCrown };
            UiKit.Choices(modes, new[] { "Отважный", "Неистовый", "Гром. корона" }, -1, i =>
            {
                Notify($"{kings[i].Name} идёт со стаей ({host.SpawnPack(kings[i], 1)})");
                host.Resume();
            }, 12);
            // The other beasts of the mountain: boars, a bear, stone monkeys, snakes — and the boars' and the monkeys' kings.
            var beasts = new[] { GuWorld.Creatures.BeastKinds.Boar, GuWorld.Creatures.BeastKinds.Bear, GuWorld.Creatures.BeastKinds.Monkey, GuWorld.Creatures.BeastKinds.Snake };
            int[] counts = { 3, 1, 3, 2 };
            UiKit.Choices(modes, new[] { "Кабаны", "Медведь", "Обезьяны", "Змеи" }, -1, i =>
            {
                Notify($"{beasts[i].Name}: пришло {host.SpawnPack(beasts[i], counts[i])}");
                host.Resume();
            }, 12);
            var beastKings = new[] { GuWorld.Creatures.BeastKinds.BoarKing, GuWorld.Creatures.BeastKinds.MonkeyKing };
            UiKit.Choices(modes, new[] { "Кор. кабанов", "Кор. обезьян" }, -1, i =>
            {
                Notify($"{beastKings[i].Name} идёт со своими ({host.SpawnPack(beastKings[i], 1)})");
                host.Resume();
            }, 12);
            UiKit.Choices(modes, new[] { "Волчий прилив", "Великий прилив" }, -1, i =>
            {
                host.StartTide(i == 1);
                host.Resume();
            }, 12);
            // Gu Masters of the clans: a Bai with ice, a Xiong with the bear's strength, a Gu Yue with moonblades — a pair of
            // youths of rank one, a master of rank two, an elder of rank three.
            string[] clans = { "bai", "xiong", "gu_yue" };
            Heading(modes, "Гу-мастера");
            UiKit.Choices(modes, new[] { "Бай: лёд", "Сюн: медведь", "Гу Юэ: луна" }, -1, i =>
            {
                Notify($"Гу-мастера пришли: {host.SpawnMasters(clans[i], 1, 2)}");
                host.Resume();
            }, 12);
            UiKit.Choices(modes, new[] { "Ранг 2", "Старейшина 3", "Бай+Сюн" }, -1, i =>
            {
                var came = i == 2
                    ? host.SpawnMasters("bai", 1, 1) + host.SpawnMasters("xiong", 2, 1)
                    : host.SpawnMasters(Random.value < 0.5f ? "bai" : "xiong", i == 0 ? 2 : 3, 1);
                Notify($"Гу-мастера пришли: {came}");
                host.Resume();
            }, 12);

            Heading(modes, "Герой и место");
            UiKit.Choices(modes, new[] { "Вылечить · отдых Гу", "На старт" }, -1, i =>
            {
                if (i == 0)
                {
                    host.Health.Set(host.Health.Max);
                    host.Cultivator.RestAll();
                    host.Cultivator.Aperture.Essence = host.Cultivator.Aperture.Cap;
                    Notify("Здоровье, эссенция и Гу — в порядке");
                    Show(Page.Sandbox);
                }
                else
                {
                    host.BackToStart();
                    host.Resume();
                }
            }, 12);
            UiKit.HexButton("Покинуть апертуру", left, () => FadeThrough(host.LeaveSandbox), UiKit.HexKind.Bad, 11, 42).Size(42);

            // ── All the Gu, at the right ──
            var right = UiKit.Rect("All Gu", body);
            right.Place(Vector2.zero, Vector2.one, 414, 64, 0, 0);
            right.Stack<VerticalLayoutGroup>(14);

            // The eight cells at hand: a click chooses where the next Gu goes.
            var hand = UiKit.Rect("Hand", right);
            var handRow = hand.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            handRow.childForceExpandWidth = false;
            hand.Size(62);
            UiKit.Lbl("Под рукой", hand).Size(62, 90, 0);

            for (var i = 0; i < GuWorldGame.SandboxCells; i++)
            {
                var slot = i;
                var kind = GuBook.Find(host.SandboxBar[i]);
                var chosen = i == host.SandboxSlot;
                var cell = UiKit.Cell(hand, 52, () =>
                {
                    host.SandboxSlot = slot;
                    Show(Page.Sandbox);
                });
                cell.Size(52, 52, 0);
                cell.Key.text = $"{i + 1}";
                cell.SetIcon(kind?.Icon);
                cell.Mark.color = Icons.Tint(kind?.Icon);
                cell.SetRank(kind?.Rank ?? 0);
                cell.SetRim(chosen ? UiKit.GoldLit : UiKit.Line);

                if (chosen)
                {
                    UiKit.Glow(cell.transform, UiKit.GoldLit.A(0.3f), 12);
                }

                cell.Hovered = () => ShowArsenalInfo(kind, slot);
            }

            UiKit.Tb("Очистить", hand, false, () =>
            {
                host.PutInSlot(host.SandboxSlot, null);
                Show(Page.Sandbox);
            }, 11).Size(34, 110, 0);

            var filters = UiKit.Rect("Ranks", right);
            var filterRow = filters.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            filterRow.childForceExpandWidth = false;
            filters.Size(34);
            UiKit.Lbl($"Все Гу · {GuBook.Count}", filters).Size(34, 120, 0);

            for (var rank = 0; rank <= 5; rank++)
            {
                var r = rank;
                UiKit.Tb(rank == 0 ? "Все" : Aperture.RankNumeral(rank), filters, rank == arsenalRank, () =>
                {
                    arsenalRank = r;
                    arsenalScroll = 0;
                    Show(Page.Sandbox);
                }).Size(34, rank == 0 ? 64 : 48, 0);
            }

            UiKit.Rect("Space", filters).Size(34, -1, 1);
            var tip = UiKit.Label($"ЛКМ — в выбранную ячейку ({host.SandboxSlot + 1})", filters, 12, UiKit.Muted, TextAnchor.MiddleRight);
            tip.Size(34, 260, 0);

            var grid = GridScroll(right, arsenalScroll, y => arsenalScroll = y);

            foreach (var kind in GuBook.All)
            {
                if (arsenalRank != 0 && kind.Rank != arsenalRank)
                {
                    continue;
                }

                var k = kind;
                var item = UiKit.Rect(kind.Id, grid);
                var cell = UiKit.Cell(item, 62, () =>
                {
                    host.PutInSlot(host.SandboxSlot, k);
                    Notify($"{k.Name} — в ячейке {host.SandboxSlot + 1}");
                    Show(Page.Sandbox);
                });
                cell.Back.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -31), new Vector2(62, 62));
                cell.SetIcon(kind.Icon);
                cell.Mark.color = Icons.Tint(kind.Icon);
                cell.SetRank(kind.Rank);
                var inHand = System.Array.IndexOf(host.SandboxBar, kind.Id) >= 0;
                cell.SetRim(inHand ? UiKit.GoldLit : UiKit.Line);
                cell.Hovered = () => ShowArsenalInfo(k, -1);
                var label = UiKit.Label(ShortName(kind.Name), item, 9, inHand ? UiKit.GoldLit : UiKit.Muted, TextAnchor.UpperCenter);
                label.rectTransform.Place(Vector2.zero, new Vector2(1, 0), -6, 0, -6, -16);
            }

            // What the pointer is over (or the chosen cell's Gu): a band at the foot.
            var band = UiKit.Image("Info", body, Color.white, UiKit.Plain);
            band.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 414, -6, 0, -56);
            band.Gradient(UiKit.Gold.A(0.08f), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.6f), true);
            UiKit.Frame(band.transform, UiKit.Gold.A(0.25f));
            var gem = UiKit.Image("Gem", band.transform, UiKit.Gold, UiKit.Diamond);
            gem.rectTransform.At(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(9, 9));
            arsenalInfo = UiKit.Label("", band.transform, 13, UiKit.Hex(0xA8B6B8));
            arsenalInfo.rectTransform.Place(Vector2.zero, Vector2.one, 44, 0, 16, 0);
            ShowArsenalInfo(GuBook.Find(host.SandboxBar[host.SandboxSlot]), host.SandboxSlot);

            UiKit.Footer(bookStage, new[] { ("1–8", "Ячейка"), ("ЛКМ", "Положить Гу"), ("Tab", "Закрыть") });
        }

        /// <summary>A Gu's name without the word "Гу" (the grid's labels are small).</summary>
        static string ShortName(string name) => name.StartsWith("Гу ") ? name.Substring(3) : name;

        void ShowArsenalInfo(GuKind kind, int slot)
        {
            if (arsenalInfo == null)
            {
                return;
            }

            if (kind == null)
            {
                arsenalInfo.text = slot >= 0 ? $"Ячейка {slot + 1} пуста — щёлкните по Гу в сетке, чтобы положить его сюда" : "";

                return;
            }

            var use = kind.OneUse ? "разовый: срабатывает по ЛКМ, здесь не кончается" : kind.Passive ? "пассивный: работает сам, пока в ячейке" :
                kind.Held ? "держится: ЛКМ — поднять, ещё раз — снять" : "ЛКМ — применить";
            arsenalInfo.text = $"<size=22><color=#EAF0F2>{kind.Name}</color></size>  <color=#{ColorUtility.ToHtmlStringRGB(Aperture.RankColour(kind.Rank))}>РАНГ {Aperture.RankNumeral(kind.Rank)}</color>  ·  {use}  ·  {kind.SkillName} — {SkillLine(kind)}";
        }

        /// <summary>A scrolling grid (the Gu are far more than fit), scrolled `down` points from its top at first.</summary>
        static RectTransform GridScroll(Transform parent, float down, System.Action<float> scrolled)
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
            grid.cellSize = new Vector2(72, 82);
            grid.spacing = new Vector2(4, 4);
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

        static int LivingWolves()
        {
            return GuWorld.Creatures.Foes.Living();
        }
    }
}
