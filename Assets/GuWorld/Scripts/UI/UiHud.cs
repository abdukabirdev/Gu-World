using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The HUD: the hero's bars at the top left, the Gu in the slots at the bottom, the quest
    /// in hand at the top right with a mark on the screen where it is, a boss's bar, bars over
    /// hurt enemies, numbers of damage, lines of news, and the prompt for what can be used.
    /// </summary>
    public partial class Ui
    {
        class Meter
        {
            public RectTransform Fill;
            public Text Value;

            public void Set(float share, string value = null)
            {
                Fill.anchorMax = new Vector2(Mathf.Clamp01(share), 1);

                if (Value != null && value != null && Value.text != value)
                {
                    Value.text = value;
                }
            }
        }

        class Floating
        {
            public Text Text;
            public Vector3 At;
            public float Age;
        }

        class OverBar
        {
            public RectTransform Root;
            public RectTransform Fill;
        }

        RectTransform hudRoot, floats;
        Meter health, essence, stamina, insight, bossBar;
        Text heroName, heroRank, stones, pills, questTitle, questStep, promptText, bossName, bossTitle, markText, bannerLabel, bannerTitle, effects;
        RectTransform promptRoot, bossRoot, questRoot, markRoot, targetMark, bannerRoot, toastRoot;
        CanvasGroup bannerGroup;
        readonly Cell[] slotCells = new Cell[4];
        readonly Text[] slotSeals = new Text[4];
        readonly List<Floating> numbers = new List<Floating>();
        readonly List<OverBar> overBars = new List<OverBar>();
        readonly List<(Text text, float age)> toasts = new List<(Text, float)>();
        readonly Queue<(string label, string title)> banners = new Queue<(string, string)>();
        float bannerAge = 99;

        static Meter MakeMeter(Transform parent, float width, float height, Color from, Color to, bool valued)
        {
            var row = UiKit.Rect("Meter", parent);
            row.Size(Mathf.Max(height, valued ? 16 : height), width + (valued ? 90 : 0), 0);
            var track = UiKit.Image("Track", row, new Color(0, 0, 0, 0.5f), UiKit.Plain);
            track.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, -height * 0.5f, -width, -height * 0.5f);
            var fill = UiKit.Image("Fill", track.transform, Color.white, UiKit.Plain);
            fill.rectTransform.Place(Vector2.zero, Vector2.one);
            fill.Gradient(from, to, true);
            var meter = new Meter { Fill = fill.rectTransform };

            if (valued)
            {
                meter.Value = UiKit.Label("", row, 13, UiKit.Text2).Shadowed();
                meter.Value.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), width + 10, 0, -width - 90, 0);
            }

            return meter;
        }

        void BuildHud()
        {
            hudRoot = UiKit.Rect("HUD", hudCanvas.transform);
            hudRoot.Fill();
            var stage = UiKit.Stage(hudRoot);

            // The hero.
            var hero = UiKit.Rect("Hero", stage);
            hero.TopLeft(40, 32, 460, 170);
            var column = hero.Stack<VerticalLayoutGroup>(5);
            column.childForceExpandWidth = false;
            heroName = UiKit.Disp("", hero, 26, UiKit.Text).Shadowed();
            heroName.Size(30, 440, 0);
            heroRank = UiKit.Lbl("", hero, 11, UiKit.LabelGold, 0.2f).Shadowed();
            heroRank.Size(16, 440, 0);
            Gap(hero, 2);
            health = MakeMeter(hero, 330, 9, UiKit.Hex(0x7A1E16), UiKit.Hex(0xF08A74), true);
            essence = MakeMeter(hero, 330, 7, UiKit.Hex(0x1F6E5E), UiKit.Hex(0xA8F0DC), true);
            stamina = MakeMeter(hero, 240, 3, UiKit.Hex(0x6E5028), UiKit.Hex(0xFFF1CF), false);
            insight = MakeMeter(hero, 330, 2, UiKit.Hex(0x8C6A3A), UiKit.Hex(0xFFF1CF), true);
            var purse = UiKit.Rect("Purse", hero);
            var purseRow = purse.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft);
            purseRow.childForceExpandWidth = false;
            purse.Size(24, 440, 0);
            UiKit.Zh("元", purse, 18, UiKit.Gold).Shadowed().Size(24, 22, 0);
            stones = UiKit.Label("", purse, 16, UiKit.GoldText).Shadowed();
            stones.Size(24, 80, 0);
            effects = UiKit.Label("", purse, 13, UiKit.Essence).Shadowed();
            effects.Size(24, 300, 0);

            // The Gu in the slots.
            var slots = UiKit.Rect("Slots", stage);
            slots.At(new Vector2(0.5f, 0), new Vector2(0, 64), new Vector2(430, 72));
            var slotRow = slots.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleCenter);
            slotRow.childForceExpandWidth = false;
            var keys = new[] { "Q", "E", "R", "F" };

            for (var i = 0; i < 4; i++)
            {
                var cell = UiKit.Cell(slots, 68);
                cell.Size(68, 68, 0);
                cell.Key.text = keys[i];
                cell.Mark.enabled = false;
                slotCells[i] = cell;
                slotSeals[i] = UiKit.Zh("", cell.transform, 34, UiKit.Text, TextAnchor.MiddleCenter);
                slotSeals[i].rectTransform.Fill();
            }

            var pill = UiKit.Cell(slots, 52);
            pill.Size(52, 52, 0);
            pill.Key.text = "H";
            pill.Mark.enabled = false;
            UiKit.Zh("药", pill.transform, 24, UiKit.Danger, TextAnchor.MiddleCenter).rectTransform.Fill();
            pills = pill.Count;

            // What can be used here.
            promptRoot = UiKit.Rect("Prompt", stage);
            promptRoot.At(new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(520, 34));
            var promptRow = promptRoot.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleCenter);
            promptRow.childForceExpandWidth = false;
            UiKit.Key("T", promptRoot, UiKit.Gold.A(0.7f), UiKit.GoldLit);
            promptText = UiKit.Label("", promptRoot, 17, UiKit.Text).Shadowed();
            promptText.Size(34);

            // The quest in hand.
            questRoot = UiKit.Rect("Quest", stage);
            questRoot.TopRight(40, 36, 380, 150);
            questRoot.Stack<VerticalLayoutGroup>(6, null, TextAnchor.UpperRight);
            UiKit.Lbl("Задание · J", questRoot, 11, UiKit.LabelGold, 0.3f, TextAnchor.MiddleRight).Shadowed().Size(14);
            questTitle = UiKit.Disp("", questRoot, 23, UiKit.Text, TextAnchor.MiddleRight).Shadowed();
            questTitle.Size(28);
            questStep = Para("", questRoot, 14, UiKit.Text2).Shadowed();
            questStep.alignment = TextAnchor.UpperRight;

            // A boss.
            bossRoot = UiKit.Rect("Boss", stage);
            bossRoot.At(new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(560, 70));
            bossRoot.Stack<VerticalLayoutGroup>(4, null, TextAnchor.UpperCenter).childForceExpandWidth = false;
            bossName = UiKit.Disp("", bossRoot, 26, UiKit.Text, TextAnchor.MiddleCenter).Shadowed();
            bossName.Size(30, 560, 0);
            bossTitle = UiKit.Lbl("", bossRoot, 10, UiKit.Danger, 0.3f, TextAnchor.MiddleCenter).Shadowed();
            bossTitle.Size(14, 560, 0);
            bossBar = MakeMeter(bossRoot, 560, 7, UiKit.Hex(0x7A1E16), UiKit.Hex(0xF08A74), false);

            // News: a banner in the middle, lines at the bottom left.
            bannerRoot = UiKit.Rect("Banner", stage);
            bannerRoot.At(new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(900, 110));
            bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0;
            bannerGroup.blocksRaycasts = false;
            bannerRoot.Stack<VerticalLayoutGroup>(6, null, TextAnchor.UpperCenter);
            bannerLabel = UiKit.Lbl("", bannerRoot, 12, UiKit.LabelGold, 0.4f, TextAnchor.MiddleCenter).Shadowed();
            bannerLabel.Size(16);
            bannerTitle = UiKit.Disp("", bannerRoot, 46, UiKit.GoldText, TextAnchor.MiddleCenter).Shadowed();
            bannerTitle.Size(54);
            UiKit.Image("Rule", bannerRoot, UiKit.Gold, UiKit.FadeBoth).Size(1);

            toastRoot = UiKit.Rect("News", stage);
            toastRoot.BottomLeft(40, 40, 560, 220);
            toastRoot.Stack<VerticalLayoutGroup>(4, null, TextAnchor.LowerLeft);

            // What floats over the world, in the screen's own pixels.
            floats = UiKit.Rect("Floats", hudRoot);
            floats.Fill();
            markRoot = UiKit.Rect("Mark", floats);
            markRoot.sizeDelta = new Vector2(120, 50);
            var gem = UiKit.Image("Gem", markRoot, UiKit.Gold, UiKit.Diamond);
            gem.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -9), new Vector2(18, 18));
            markText = UiKit.Label("", markRoot, 13, UiKit.GoldText, TextAnchor.LowerCenter).Shadowed();
            markText.rectTransform.Fill();
            targetMark = UiKit.Image("Target", floats, UiKit.Danger, UiKit.Diamond).rectTransform;
            targetMark.sizeDelta = new Vector2(14, 14);
            var dot = UiKit.Image("Aim", floats, new Color(1, 1, 1, 0.45f), UiKit.Disc);
            dot.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5));
            hudRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ News

        public void Toast(string line)
        {
            if (toastRoot == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            if (toasts.Count > 0 && toasts[toasts.Count - 1].text.text == line && toasts[toasts.Count - 1].age < 1)
            {
                return;
            }

            var text = UiKit.Label(line, toastRoot, 15, UiKit.Text).Shadowed();
            text.Size(22);
            toasts.Add((text, 0));

            while (toasts.Count > 7)
            {
                Destroy(toasts[0].text.gameObject);
                toasts.RemoveAt(0);
            }
        }

        public void Banner(string label, string title) => banners.Enqueue((label, title));

        public void Place(string name) => Toast("— " + name + " —");

        public void DamageNumber(Vector3 at, float amount, bool critical, bool onHero)
        {
            if (floats == null || numbers.Count > 40)
            {
                return;
            }

            var colour = onHero ? UiKit.Danger : critical ? UiKit.GoldLit : UiKit.Text;
            var text = UiKit.Label(Mathf.Max(1, Mathf.RoundToInt(amount)).ToString(), floats, critical ? 26 : 19, colour, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed();
            text.rectTransform.sizeDelta = new Vector2(120, 30);
            numbers.Add(new Floating { Text = text, At = at + Random.insideUnitSphere * 0.3f });
        }

        // ------------------------------------------------------------------ Every frame

        static bool OnScreen(Camera camera, Vector3 world, out Vector3 screen)
        {
            screen = camera.WorldToScreenPoint(world);

            return screen.z > 0.1f && screen.x > -50 && screen.x < Screen.width + 50 && screen.y > -50 && screen.y < Screen.height + 50;
        }

        void TickHud()
        {
            var dt = Time.unscaledDeltaTime;
            hurt.color = new Color(0.7f, 0.08f, 0.05f, Mathf.MoveTowards(hurt.color.a, 0, dt * 1.6f));

            // The banner: one at a time, each for three seconds.
            bannerAge += dt;

            if (bannerAge > 3.2f && banners.Count > 0 && Open == Page.None)
            {
                var (label, title) = banners.Dequeue();
                bannerLabel.text = label.ToUpperInvariant();
                bannerTitle.text = title;
                bannerTitle.fontSize = title.Length > 34 ? 26 : title.Length > 22 ? 34 : 46;
                bannerAge = 0;
            }

            bannerGroup.alpha = Mathf.Clamp01(Mathf.Min(bannerAge / 0.3f, (3 - bannerAge) / 0.6f));

            for (var i = toasts.Count - 1; i >= 0; i--)
            {
                var (text, age) = toasts[i];
                age += dt;
                toasts[i] = (text, age);
                text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Clamp01((5 - age) / 0.8f));

                if (age > 5)
                {
                    Destroy(text.gameObject);
                    toasts.RemoveAt(i);
                }
            }

            var session = S;
            var hero = Game.Hero;
            var camera = Game.Rig != null ? Game.Rig.Camera : null;

            if (!Game.Playing || session == null || hero == null || camera == null || !hudRoot.gameObject.activeSelf)
            {
                return;
            }

            var state = session.State;
            var stats = session.Stats;
            heroName.text = "Гу Юэ " + state.HeroName;
            heroRank.text = $"{Cultivation.Name(state.Step)} · талант {Cultivation.TalentName(state.Talent)}".ToUpperInvariant();
            health.Set(state.Health / stats.MaxHealth, $"{Mathf.CeilToInt(state.Health)} / {Mathf.RoundToInt(stats.MaxHealth)}");
            essence.Set(state.Essence / stats.MaxEssence, $"{Mathf.RoundToInt(state.Essence / Cultivation.EssenceScale(session.Rank) * 100)}%");
            stamina.Set(session.Stamina / HeroStats.MaxStamina);
            var ready = Cultivation.IsRankUp(state.Step) && state.Insight >= Cultivation.Needs[state.Step];
            insight.Set(session.InsightShare, state.Step >= Cultivation.MaxStep ? "вершина" : ready ? "прорыв у источника" : $"прорыв {(int)state.Insight} / {session.InsightNeeded}");
            stones.text = state.Stones.ToString();
            pills.text = session.Count("heal_pill").ToString();
            effects.text = (session.Shielded ? $"щит {session.ShieldLeft:0}с  " : "") + (session.MeleeUp ? $"сила {session.MeleeLeft:0}с  " : "") + (session.WardReady ? "покров" : "");

            for (var i = 0; i < 4; i++)
            {
                var cell = slotCells[i];
                var open = i < stats.ActiveSlots;
                var gu = open ? session.ActiveGu(i) : null;
                var def = gu != null ? GuBook.Get(gu.Def) : null;
                slotSeals[i].text = def != null ? def.Seal : open ? "" : "隐";
                var can = def != null && state.Essence >= session.CostOf(gu);
                slotSeals[i].color = def == null ? UiKit.Muted.A(0.4f) : can ? UiKit.Hex(def.Colour) : UiKit.Muted;
                cell.SetRim(def != null ? QualityColour(gu.Quality).A(0.8f) : UiKit.Line);
                var total = session.CooldownOf(i);
                cell.SetRest(def != null && total > 0 ? session.CooldownLeft(i) / total : 0);
                cell.SetRank(def != null ? def.Rank : 0);
            }

            promptRoot.gameObject.SetActive(Game.Prompt != null && Open == Page.None);

            if (Game.Prompt != null)
            {
                promptText.text = Game.Prompt;
            }

            // The quest and its mark on the screen.
            var quest = session.MainQuest;
            questRoot.gameObject.SetActive(quest != null);
            var marked = false;

            if (quest != null)
            {
                questTitle.text = Story.Get(quest.Quest).Title;
                questStep.text = session.StepText(quest);

                if (session.MarkerOf(quest, out var point))
                {
                    var world = Game.Ground(new Vector3(point.X, 0, point.Z)) + Vector3.up * 2.4f;
                    var distance = hero.FlatDistance(world);

                    if (distance > 8)
                    {
                        marked = true;
                        var screen = camera.WorldToScreenPoint(world);
                        var centre = new Vector2(Screen.width, Screen.height) * 0.5f;
                        var flat = new Vector2(screen.x, screen.y);

                        if (screen.z < 0)
                        {
                            // Behind the camera: pushed to the edge on the side it is on.
                            flat = centre - (flat - centre).normalized * Screen.width;
                        }

                        var margin = new Vector2(70, 70);
                        flat.x = Mathf.Clamp(flat.x, margin.x, Screen.width - margin.x);
                        flat.y = Mathf.Clamp(flat.y, margin.y + 90, Screen.height - margin.y - 40);
                        markRoot.position = flat;
                        markText.text = $"{Mathf.RoundToInt(distance)} м";
                    }
                }
            }

            markRoot.gameObject.SetActive(marked);

            // A boss that has seen the hero.
            Enemy boss = null;

            foreach (var enemy in Enemy.Living)
            {
                if (enemy.Def.Boss && enemy.Aware && hero.FlatDistance(enemy.transform.position) < 70)
                {
                    boss = enemy;
                }
            }

            bossRoot.gameObject.SetActive(boss != null);

            if (boss != null)
            {
                bossName.text = boss.Def.Name;
                bossTitle.text = (boss.Def.Title ?? "").ToUpperInvariant() + (boss.Shielded ? " · ЩИТ" : "");
                bossBar.Set(boss.Health / boss.MaxHealth);
            }

            // Bars over those who are hurt or held as the target.
            var used = 0;

            foreach (var enemy in Enemy.Living)
            {
                if (enemy.Def.Boss || (enemy.SinceHurt > 5 && enemy != hero.Target) || !OnScreen(camera, enemy.transform.position + Vector3.up * (enemy.Height + 0.35f), out var screen))
                {
                    continue;
                }

                if (used >= overBars.Count)
                {
                    var root = UiKit.Image("Bar", floats, new Color(0, 0, 0, 0.6f), UiKit.Plain);
                    root.rectTransform.sizeDelta = new Vector2(64, 5);
                    var fill = UiKit.Image("Fill", root.transform, UiKit.Danger, UiKit.Plain);
                    fill.rectTransform.Place(Vector2.zero, Vector2.one);
                    overBars.Add(new OverBar { Root = root.rectTransform, Fill = fill.rectTransform });
                }

                var bar = overBars[used++];
                bar.Root.gameObject.SetActive(true);
                bar.Root.position = new Vector2(screen.x, screen.y);
                bar.Fill.anchorMax = new Vector2(Mathf.Clamp01(enemy.Health / enemy.MaxHealth), 1);
            }

            for (var i = used; i < overBars.Count; i++)
            {
                overBars[i].Root.gameObject.SetActive(false);
            }

            var target = hero.Target;
            var held = target != null && OnScreen(camera, target.transform.position + Vector3.up * (target.Height + 0.7f), out var over);
            targetMark.gameObject.SetActive(held);

            if (held)
            {
                targetMark.position = camera.WorldToScreenPoint(target.transform.position + Vector3.up * (target.Height + 0.7f));
            }

            // Numbers rise and fade.
            for (var i = numbers.Count - 1; i >= 0; i--)
            {
                var number = numbers[i];
                number.Age += dt;
                var shown = OnScreen(camera, number.At + Vector3.up * number.Age * 1.4f, out var where);
                number.Text.enabled = shown;
                number.Text.rectTransform.position = new Vector2(where.x, where.y);
                number.Text.color = new Color(number.Text.color.r, number.Text.color.g, number.Text.color.b, Mathf.Clamp01((0.9f - number.Age) / 0.3f));

                if (number.Age > 0.9f)
                {
                    Destroy(number.Text.gameObject);
                    numbers.RemoveAt(i);
                }
            }
        }
    }
}
