using System.Collections.Generic;
using GuWorld.Cultivation;
using GuWorld.Items;
using GuWorld.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The HUD (UI-HUD.dc.html): at the top left the hero — the rank inside a ring of
    /// essence, the name and the share of essence, thin bars of health and stamina, the
    /// place, the hour, the stones, chips for what their Gu keep up; at the top right a
    /// diamond-shaped minimap with the north, where they stand and where the task leads,
    /// and the task's card under it; at the foot the fists' slot (F, with Q and R), the
    /// Gu's slots 1–4 and the stone's (V); the crosshair in the middle, and round it while a
    /// stone is drawn in an arc and the count down; the log of what happened and the advice
    /// at the bottom left; the keys of the hero's pages at the bottom right; and over the bar
    /// what one can do here. Built on a stage in the design's units.
    /// </summary>
    public partial class Menus
    {
        /// <summary>Metres across the minimap, at each of its zooms (+ and − while playing); the first three on the finer map round the hero.</summary>
        static readonly float[] MinimapSpans = { 130, 260, 400, 1500, 4000 };
        int minimapZoom = 1;
        /// <summary>The minimap's diamond, corner to corner (a square of 140 turned on its point).</summary>
        const float MinimapSize = 198;
        const int LogLines = 6;
        const float LogFor = 14;

        RectTransform hud;
        /// <summary>The HUD's stage (the design's units); the plates and the beacon stay on `hud`, in the canvas's.</summary>
        RectTransform hudStage;
        Image essenceRing;
        Image capRing;
        Text rankMark;
        Text heroName;
        Text essenceLine;
        RectTransform lifeFill;
        Text lifeLine;
        RectTransform staminaFill;
        Image staminaImage;
        Text placeLine;
        Text timeLine;
        Text stonesLine;
        RawImage minimap;
        RectTransform minimapArrow;
        RectTransform minimapPin;
        Text coordsLine;
        readonly List<(RectTransform dot, Text label)> minimapMarks = new List<(RectTransform, Text)>();
        /// <summary>Where the minimap's names already are this frame (a name that would cover another is left out, its dot kept).</summary>
        readonly List<Rect> minimapTaken = new List<Rect>();
        RectTransform questCard;
        Text questTitle;
        Text questGoal;
        Text questProgress;
        Text questMore;
        /// <summary>The Gu's slots (keys 1–4; on the immortal's aperture the eight cells at hand, 1–8).</summary>
        GuSlot[] slots = new GuSlot[0];
        /// <summary>The Gu at hand (or the fists' blows), in words, over the bar.</summary>
        Text handLine;
        Cell stoneCell;
        /// <summary>The fists' slot, the dots of their chain, and the special blows' cells (Q, R).</summary>
        GuSlot fistSlot;
        Image[] chainDots;
        Cell heavyCell;
        Cell spinCell;
        /// <summary>The chips under the hero for what their Gu keep up now (held Gu, timed powers, a sense).</summary>
        readonly List<(GameObject chip, Text words, Image edge)> chips = new List<(GameObject, Text, Image)>();
        RectTransform crosshair;
        RectTransform absorbing;
        Image absorbFill;
        Text absorbLine;
        Text promptLine;
        RectTransform promptBar;
        RectTransform promptFill;
        readonly List<(string text, float at)> log = new List<(string, float)>();
        Text[] logTexts;

        void BuildHud()
        {
            hud = UiKit.Rect("HUD", canvas.transform).Fill();
            hudStage = UiKit.Stage(hud);

            // The hero: the rank in a ring of essence.
            var portrait = UiKit.Rect("Portrait", hudStage);
            portrait.TopLeft(32, 28, 84, 84);
            var back = UiKit.Image("Back", portrait, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.75f), UiKit.Disc);
            back.rectTransform.Fill(4);
            var track = UiKit.Image("Track", portrait, UiKit.Essence.A(0.15f), UiKit.ArcRing(80, 4));
            track.rectTransform.Fill(2);
            capRing = UiKit.Image("Cap", portrait, UiKit.Essence.A(0.3f), UiKit.ArcRing(80, 4));
            Radial(capRing);
            capRing.rectTransform.Fill(2);
            essenceRing = UiKit.Image("Essence", portrait, UiKit.Essence, UiKit.ArcRing(80, 4));
            Radial(essenceRing);
            essenceRing.rectTransform.Fill(2);
            var dots = UiKit.Image("Dots", portrait, UiKit.Gold.A(0.35f), UiKit.RingLine(60, 4));
            dots.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            rankMark = UiKit.Disp("I", portrait, 30, UiKit.GoldLit, TextAnchor.MiddleCenter);
            rankMark.rectTransform.Place(Vector2.zero, Vector2.one, 0, 12, 0, 0);
            var rankWord = UiKit.Lbl("Ранг", portrait, 8, UiKit.Hex(0xA8B6B8), 0.24f, TextAnchor.MiddleCenter);
            rankWord.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(60, 12));

            var column = UiKit.Rect("Hero", hudStage);
            column.TopLeft(130, 34, 300, 120);
            column.Stack<VerticalLayoutGroup>(6);
            var names = UiKit.Rect("Name", column);
            var nameRow = names.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.LowerLeft);
            nameRow.childForceExpandWidth = false;
            names.Size(24);
            heroName = UiKit.Disp("", names, 22).Shadowed();
            heroName.horizontalOverflow = HorizontalWrapMode.Overflow;
            essenceLine = UiKit.Label("", names, 11, UiKit.EssenceLit, TextAnchor.LowerLeft).Shadowed();
            essenceLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            var life = UiKit.Bar(column, UiKit.BarKind.Health, 1, 5);
            lifeFill = life.rectTransform;
            var lifeTrack = life.transform.parent;
            lifeTrack.GetComponent<Image>().color = new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.65f);
            UiKit.Frame(lifeTrack, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f));
            lifeLine = UiKit.Label("", lifeTrack, 10, UiKit.Text2, TextAnchor.MiddleLeft).Shadowed();
            lifeLine.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), 8, -8, -90, -8);
            staminaImage = UiKit.Bar(column, UiKit.BarKind.Stamina, 1, 3);
            staminaFill = staminaImage.rectTransform;
            staminaImage.transform.parent.GetComponent<Image>().color = new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.65f);
            var where = UiKit.Rect("Where", column);
            var whereRow = where.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            whereRow.childForceExpandWidth = false;
            where.Size(16);
            placeLine = UiKit.Label("", where, 11, UiKit.Text2).Shadowed();
            placeLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            timeLine = UiKit.Label("", where, 11, UiKit.Muted).Shadowed();
            timeLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            var coin = UiKit.Icon("stone", where, 12, UiKit.EssenceLit);
            coin.Size(12, 12, 0);
            stonesLine = UiKit.Label("", where, 11, UiKit.Text2).Shadowed();
            stonesLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            var chipRow = UiKit.Rect("Chips", column);
            var chipGroup = chipRow.Stack<HorizontalLayoutGroup>(6, new RectOffset(0, 0, 4, 0), TextAnchor.MiddleLeft);
            chipGroup.childForceExpandWidth = false;
            chipRow.Size(26);

            for (var i = 0; i < 5; i++)
            {
                var chip = UiKit.Image("Chip", chipRow, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.6f), UiKit.Plain);
                var edge = UiKit.Frame(chip.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f));
                var pad = chip.rectTransform.Stack<HorizontalLayoutGroup>(0, new RectOffset(8, 8, 0, 0), TextAnchor.MiddleCenter);
                pad.childForceExpandWidth = false;
                chip.Size(22);
                var words = UiKit.Label("", chip.transform, 10, UiKit.Text2, TextAnchor.MiddleCenter);
                words.Track(0.18f);
                words.Size(22);
                chip.gameObject.SetActive(false);
                chips.Add((chip.gameObject, words, edge));
            }

            BuildMinimap();
            BuildQuest();
            BuildBar();
            BuildLog();
            BuildLegend();
            BuildCrosshair();

            promptLine = UiKit.Label("", hudStage, 16, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed();
            promptLine.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -400, 176, -400, -206);
            promptBar = UiKit.Image("Hold", hudStage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Plain).rectTransform;
            promptBar.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -110, 166, -110, -170);
            UiKit.Frame(promptBar, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.12f));
            var hold = UiKit.Image("Fill", promptBar, Color.white, UiKit.Plain);
            hold.Gradient(UiKit.GoldDeep, UiKit.GoldLit, true);
            promptFill = hold.rectTransform;
            promptFill.Place(Vector2.zero, new Vector2(0, 1));
            promptBar.gameObject.SetActive(false);

            // The hero's pages: a row of keys, each with its page's name under it.
            var keys = UiKit.Rect("Keys", hudStage);
            keys.Place(new Vector2(1, 0), new Vector2(1, 0), -700, 122, 32, -164);
            var keyRow = keys.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.LowerRight);
            keyRow.childForceExpandWidth = false;

            var hints = host.Sandbox
                ? new[] { ("Tab", "Все Гу"), ("M", "Карта"), ("T", "Чат"), ("Esc", "Меню") }
                : new[] { ("I", "Сумка"), ("K", "Апертура"), ("U", "Слияние"), ("M", "Карта"), ("J", "Журнал"), ("O", "Статус"), ("B", "Тело"), ("T", "Чат") };

            foreach (var (key, name) in hints)
            {
                var item = UiKit.Rect(name, keys);
                var stack = item.Stack<VerticalLayoutGroup>(4, null, TextAnchor.LowerCenter);
                stack.childForceExpandWidth = false;
                var cap = UiKit.Key(key, item, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.4f), UiKit.Text2);
                var under = UiKit.Image("Under", cap, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Plain);
                under.rectTransform.Fill(1.5f);
                under.transform.SetAsFirstSibling();
                under.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var label = UiKit.Label(name.ToUpperInvariant(), item, 9, UiKit.Muted, TextAnchor.MiddleCenter).Shadowed();
                label.Track(0.14f);
                label.Size(12);
            }

            BuildGuide();
            BuildPlates();
        }

        static void Radial(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.rectTransform.Fill();
        }

        void BuildMinimap()
        {
            // The diamond, 200 across, 62 in from the right and 24 down: its gold edge, a dark band and a faint outer edge.
            var centre = new Vector2(-62 - 100, -24 - 100);
            var outer = UiKit.Image("Minimap Outer", hudStage, UiKit.Gold.A(0.18f), UiKit.Diamond);
            outer.rectTransform.At(new Vector2(1, 1), centre, Vector2.one * (MinimapSize + 16));
            var band = UiKit.Image("Minimap Band", hudStage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.85f), UiKit.Diamond);
            band.rectTransform.At(new Vector2(1, 1), centre, Vector2.one * (MinimapSize + 14));
            var frame = UiKit.Image("Minimap Frame", hudStage, UiKit.Gold.A(0.6f), UiKit.Diamond);
            frame.rectTransform.At(new Vector2(1, 1), centre, Vector2.one * (MinimapSize + 3));
            var under = UiKit.Image("Minimap Under", hudStage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 1), UiKit.Diamond);
            under.rectTransform.At(new Vector2(1, 1), centre, Vector2.one * (MinimapSize + 1));
            var mask = UiKit.Image("Minimap", hudStage, Color.white, UiKit.Diamond);
            mask.rectTransform.At(new Vector2(1, 1), centre, Vector2.one * MinimapSize);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            minimap = UiKit.Rect("Map", mask.transform).gameObject.AddComponent<RawImage>();
            minimap.rectTransform.Fill();
            minimap.raycastTarget = false;
            minimap.color = new Color(0.82f, 0.86f, 0.9f, 1);

            for (var i = 0; i < 6; i++)
            {
                var dot = UiKit.Image("Mark", mask.transform, UiKit.GoldLit, UiKit.Disc).rectTransform;
                dot.sizeDelta = new Vector2(6, 6);
                var label = UiKit.Label("", mask.transform, 10, UiKit.Text, TextAnchor.MiddleCenter).Shadowed();
                label.rectTransform.sizeDelta = new Vector2(140, 16);
                minimapMarks.Add((dot, label));
            }

            // The map's mark (MenusMap): where it is, or at the edge toward it when it lies farther.
            minimapPin = UiKit.Icon("pin", mask.transform, 18, UiKit.Danger).rectTransform;
            minimapPin.pivot = new Vector2(0.5f, 0.04f);
            UiKit.Image("Hole", minimapPin, UiKit.Void, UiKit.Disc).rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(5, 5));
            // Where the task leads (MenusGuide): a gold diamond, breathing; at the edge toward it when farther.
            minimapQuest = UiKit.Image("Quest", mask.transform, UiKit.Gold, UiKit.Diamond).rectTransform;
            minimapQuest.sizeDelta = new Vector2(12, 12);
            UiKit.HaloAt(minimapQuest, UiKit.Gold.A(0.5f), Vector2.zero, 30);
            var breathe = minimapQuest.gameObject.AddComponent<Pulse>();
            breathe.Period = 2;
            breathe.Low = 0.6f;
            breathe.High = 1;
            breathe.Grow = 0.08f;
            minimapArrow = UiKit.Image("Hero", mask.transform, UiKit.GoldLit, UiKit.Arrow).rectTransform;
            minimapArrow.sizeDelta = new Vector2(16, 16);
            var north = UiKit.Disp("С", hudStage, 13, UiKit.Gold, TextAnchor.MiddleCenter).Shadowed();
            north.rectTransform.At(new Vector2(1, 1), centre + new Vector2(0, 100 - 12), new Vector2(20, 16));
            coordsLine = UiKit.Label("", hudStage, 10, UiKit.Muted, TextAnchor.MiddleRight).Shadowed();
            coordsLine.rectTransform.At(new Vector2(1, 1), centre + new Vector2(-82, -100 + 6), new Vector2(300, 16));
            var plus = UiKit.Key("+", hudStage, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.4f));
            plus.anchorMin = plus.anchorMax = new Vector2(1, 1);
            plus.anchoredPosition = centre + new Vector2(84, -100 + 6);
            var minus = UiKit.Key("−", hudStage, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.4f));
            minus.anchorMin = minus.anchorMax = new Vector2(1, 1);
            minus.anchoredPosition = centre + new Vector2(112, -100 + 6);
        }

        void BuildQuest()
        {
            // Under the minimap, against the right edge: dark fading in from the left, a gold line at the right.
            questCard = UiKit.Rect("Quest", hudStage);
            questCard.TopRight(32, 24 + 200 + 16, 260, 120);
            var back = UiKit.Image("Back", questCard, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.75f), UiKit.Plain);
            back.rectTransform.Fill();
            back.Gradient(new Color(1, 1, 1, 0), Color.white, true, Color.white, 0.2f);
            back.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var line = UiKit.Image("Line", questCard, UiKit.Gold, UiKit.Plain);
            line.rectTransform.Place(new Vector2(1, 0), Vector2.one, -1, 0, 0, 0);
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            questCard.Stack<VerticalLayoutGroup>(4, new RectOffset(14, 14, 12, 12), TextAnchor.UpperRight);
            questCard.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            questCaps = UiKit.Lbl("Задание", questCard, 10, UiKit.Gold, 0.34f, TextAnchor.MiddleRight);
            questCaps.Size(14);
            questTitle = UiKit.Disp("", questCard, 19, UiKit.Text, TextAnchor.MiddleRight).Shadowed();
            questTitle.Size(22);
            questGoal = UiKit.Label("", questCard, 12, UiKit.Text2, TextAnchor.UpperRight).Shadowed();
            questGoal.gameObject.AddComponent<ContentSizeFitter>();
            // Who or what, how far and which way (MenusGuide), and how far along.
            questWhere = UiKit.Label("", questCard, 11, UiKit.Muted, TextAnchor.MiddleRight).Shadowed();
            questWhere.Size(16);
            questProgress = questGoal;
            questMore = UiKit.Label("", questCard, 11, UiKit.Muted, TextAnchor.MiddleRight).Shadowed();
            questMore.Size(16);
        }

        /// <summary>
        /// The bar at the foot of the screen: the fists' slot (F) with their special blows beside it (Q, R) and the dots
        /// of their chain; the Gu's slots (1–4, or the immortal's aperture's eight); the stone (V).
        /// </summary>
        void BuildBar()
        {
            const float gap = 10;
            const float small = 52;
            const float mini = 34;
            var keys = host.Sandbox ? GuWorldGame.SandboxCells : Cultivator.BarSize;
            var span = keys * GuSlot.Width + (keys - 1) * gap;
            var width = mini + 8 + small + 16 + span + (host.Sandbox ? 0 : 24 + small);
            var bar = UiKit.Rect("Bar", hudStage).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -width / 2, 52, -width / 2, -(52 + GuSlot.Height));
            var x = 0f;

            // Q and R, stacked, the fists after them.
            heavyCell = MiniCell(bar, "Q", "bolt", x + mini / 2, mini / 2 + 1, mini);
            spinCell = MiniCell(bar, "R", "wing", x + mini / 2, -mini / 2 - 1, mini);
            x += mini + 8;

            fistSlot = GuSlot.Build(bar, "F", small);
            Put(fistSlot.Root, x + small / 2, -(GuSlot.Height - small) / 2);
            chainDots = new Image[GuWorld.Combat.Melee.Chain.Length];

            for (var i = 0; i < chainDots.Length; i++)
            {
                chainDots[i] = UiKit.Image("Chain", fistSlot.Root, UiKit.Muted, UiKit.Diamond);
                chainDots[i].rectTransform.At(new Vector2(0.5f, 1), new Vector2((i - (chainDots.Length - 1) * 0.5f) * 10, 7), new Vector2(6, 6));
            }

            x += small + 16;
            slots = new GuSlot[keys];

            for (var i = 0; i < keys; i++)
            {
                slots[i] = GuSlot.Build(bar, $"{i + 1}");
                Put(slots[i].Root, x + GuSlot.Width / 2, 0);
                x += GuSlot.Width + gap;
            }

            stoneCell = UiKit.Cell(bar, small);
            stoneCell.gameObject.SetActive(!host.Sandbox);
            stoneCell.Back.rectTransform.anchorMin = stoneCell.Back.rectTransform.anchorMax = new Vector2(1, 0.5f);
            stoneCell.Back.rectTransform.anchoredPosition = new Vector2(-small / 2, -(GuSlot.Height - small) / 2);
            stoneCell.Key.text = "V";
            stoneCell.Key.color = UiKit.Text2;
            stoneCell.SetIcon("stone");
            stoneCell.Mark.color = UiKit.EssenceLit;
            var stoneWord = UiKit.Lbl("Камень", stoneCell.transform, 9, UiKit.Muted, 0.2f, TextAnchor.UpperCenter);
            stoneWord.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -40, -16, -40, 4);

            handLine = UiKit.Label("", hudStage, 12, UiKit.Text2, TextAnchor.LowerCenter).Shadowed();
            handLine.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -560, 138, -560, -170);
        }

        /// <summary>A slot's middle `x` along the bar (from its left), `y` from its height's middle.</summary>
        static void Put(RectTransform card, float x, float y)
        {
            card.anchorMin = card.anchorMax = new Vector2(0, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>A small cell for a special blow: its key, and a shade while it rests.</summary>
        static Cell MiniCell(Transform bar, string key, string icon, float x, float y, float size)
        {
            var cell = UiKit.Cell(bar, size);
            cell.Back.rectTransform.anchorMin = cell.Back.rectTransform.anchorMax = new Vector2(0, 0.5f);
            cell.Back.rectTransform.anchoredPosition = new Vector2(x, y);
            cell.Key.text = key;
            cell.Key.color = UiKit.GoldLit;
            cell.SetIcon(icon);
            cell.SetRim(UiKit.GoldDeep);

            return cell;
        }

        /// <summary>The crosshair (a dot and four ticks), and round it while a stone is drawn in an arc filling and the count down under it.</summary>
        void BuildCrosshair()
        {
            crosshair = UiKit.Rect("Crosshair", hudStage).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            UiKit.HaloAt(crosshair, new Color(0, 0, 0, 0.3f), Vector2.zero, 24);
            var dot = UiKit.Image("Dot", crosshair, UiKit.GoldLit, UiKit.Disc);
            dot.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));

            foreach (var (at, size) in new[] { (new Vector2(0, 14), new Vector2(1.4f, 8)), (new Vector2(0, -14), new Vector2(1.4f, 8)), (new Vector2(14, 0), new Vector2(8, 1.4f)), (new Vector2(-14, 0), new Vector2(8, 1.4f)) })
            {
                var tick = UiKit.Image("Tick", crosshair, UiKit.GoldLit.A(0.8f), UiKit.Plain);
                tick.rectTransform.At(new Vector2(0.5f, 0.5f), at, size);
                tick.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);
            }

            absorbing = UiKit.Rect("Absorbing", hudStage).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            var track = UiKit.Image("Track", absorbing, UiKit.Essence.A(0.2f), UiKit.ArcRing(47, 3));
            track.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(47, 47));
            absorbFill = UiKit.Image("Fill", absorbing, UiKit.Essence, UiKit.ArcRing(47, 3));
            Radial(absorbFill);
            absorbFill.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(47, 47));
            absorbLine = UiKit.Label("", absorbing, 11, UiKit.EssenceLit, TextAnchor.MiddleCenter).Shadowed();
            absorbLine.Track(0.18f);
            absorbLine.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -44), new Vector2(400, 18));
            absorbing.gameObject.SetActive(false);
        }

        void BuildLog()
        {
            var box = UiKit.Rect("Log", hudStage).Place(Vector2.zero, Vector2.zero, 32, 150, -432, -330);
            box.Stack<VerticalLayoutGroup>(4, null, TextAnchor.LowerLeft);
            logTexts = new Text[LogLines];

            for (var i = 0; i < LogLines; i++)
            {
                logTexts[i] = UiKit.Label("", box, 12, UiKit.Text2).Shadowed();
                logTexts[i].Size(17);
            }
        }

        /// <summary>Something the game tells the player: a line in the log ("Система: …").</summary>
        public void Notify(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            log.Add((text, Time.unscaledTime));

            if (log.Count > 30)
            {
                log.RemoveAt(0);
            }
        }

        /// <summary>The log's last lines (for checking).</summary>
        public IReadOnlyList<(string text, float at)> Log => log;

        /// <summary>What one can do here, at the foot of the screen (null: nothing); with a share held so far, a bar.</summary>
        public void SetPrompt(string text, float held = -1)
        {
            promptLine.text = text ?? "";
            var holding = text != null && held >= 0;
            promptBar.gameObject.SetActive(holding);

            if (holding)
            {
                promptFill.anchorMax = new Vector2(Mathf.Clamp01(held), 1);
            }
        }

        RectTransform legend;

        /// <summary>The hitbox view's key: what each colour is (shown while the view is on).</summary>
        void BuildLegend()
        {
            legend = UiKit.Panel("Hitbox Legend", hudStage).rectTransform;
            legend.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 32, -130, -260, -150);
            var list = UiKit.Rect("List", legend).Fill();
            list.Stack<VerticalLayoutGroup>(2, new RectOffset(14, 14, 12, 10));
            UiKit.Lbl("Хитбоксы · F7", list).Size(18);

            foreach (var (_, name, colour) in Render.HitboxView.Legend)
            {
                var row = UiKit.Rect(name, list);
                row.Size(18);
                var chip = UiKit.Image("Chip", row, colour);
                chip.rectTransform.At(new Vector2(0, 0.5f), new Vector2(7, 0), new Vector2(12, 3));
                UiKit.Label(name, row, 12, UiKit.Text2).rectTransform.Place(Vector2.zero, Vector2.one, 20, 0, 0, 0);
            }

            UiKit.Label("Части тела — цвета страницы «Тело» (B)", list, 11, UiKit.Muted).Size(28);
            legend.gameObject.SetActive(false);
        }

        void UpdateHud()
        {
            if (legend != null && legend.gameObject.activeSelf != Render.HitboxView.Shown)
            {
                legend.gameObject.SetActive(Render.HitboxView.Shown);
            }

            if (!hud.gameObject.activeSelf || host == null)
            {
                return;
            }

            var gu = host.Cultivator;
            var a = gu.Aperture;
            var rank = Aperture.RankColour(a.Rank);
            essenceRing.color = rank;
            essenceRing.fillAmount = Mathf.Clamp01(a.Essence / 100);
            capRing.color = rank.A(0.3f);
            capRing.fillAmount = Mathf.Clamp01(a.Cap / 100);
            rankMark.text = Aperture.RankNumeral(a.Rank);
            heroName.text = host.Look.FullName;
            essenceLine.text = gu.EndlessEssence ? "эссенция бесконечна" : $"{a.Essence:0}% эссенции";

            var health = host.Health;
            lifeFill.anchorMax = new Vector2(Mathf.Clamp01(health.Current / health.Max), 1);
            lifeLine.text = $"{Mathf.CeilToInt(health.Current)}";
            var body = host.Physique;
            staminaFill.anchorMax = new Vector2(Mathf.Clamp01(body.Stamina / body.MaxStamina), 1);
            // Spent: the bar goes red until a quarter is back.
            staminaImage.color = body.Spent ? UiKit.Danger : Color.white;
            placeLine.text = host.PlaceName;
            timeLine.text = host.TimeLine;
            stonesLine.text = $"{gu.Stones}";

            UpdateChips(gu);
            UpdateMinimap();
            UpdateQuest();
            UpdateGuide();
            UpdateBar(gu);
            UpdatePlates();
            UpdateLog();
        }

        /// <summary>The minimap closer in (-1) or farther out (+1).</summary>
        public void ZoomMinimap(int step)
        {
            minimapZoom = Mathf.Clamp(minimapZoom + step, 0, MinimapSpans.Length - 1);
            Notify($"Миникарта: {SpanName(MinimapSpans[minimapZoom])} в поперечнике");
        }

        static string SpanName(float metres) => metres >= 1000 ? $"{metres / 1000:0.#} км" : $"{metres:0} м";

        void UpdateMinimap()
        {
            var minimapSpan = MinimapSpans[minimapZoom];
            var near = host.NearMap;
            var heroAt = new Vector2(host.HeroAt.x, host.HeroAt.z);
            // The finer map while all that is shown lies on it; farther out, the whole mountain's.
            var onNear = near != null && near.Area.Contains(heroAt - Vector2.one * minimapSpan / 2) && near.Area.Contains(heroAt + Vector2.one * minimapSpan / 2);
            var map = onNear ? near : host.Map ?? near;

            if (map == null)
            {
                minimap.enabled = false;

                return;
            }

            minimap.enabled = true;
            minimap.texture = map.Texture;
            var at = host.HeroAt;
            var centre = map.ToMap(at);
            var span = new Vector2(minimapSpan / map.Area.width, minimapSpan / map.Area.height);
            minimap.uvRect = new Rect(centre - span * 0.5f, span);
            minimapArrow.localEulerAngles = new Vector3(0, 0, -host.HeroFacing * Mathf.Rad2Deg);
            var line = SpanName(minimapSpan).ToUpperInvariant();
            minimapPin.gameObject.SetActive(MapPin.HasValue);
            var quest = host.QuestTarget;
            minimapQuest.gameObject.SetActive(quest.HasValue);

            if (quest.HasValue)
            {
                var toQuest = (map.ToMap(quest.Value.at) - centre) / span * MinimapSize;
                var rim = MinimapSize * 0.5f - 9;
                var round = Mathf.Abs(toQuest.x) + Mathf.Abs(toQuest.y);
                minimapQuest.anchoredPosition = round > rim ? toQuest * (rim / round) : toQuest;
            }

            if (MapPin.HasValue)
            {
                var pin = MapPin.Value;
                var toPin = (map.ToMap(pin) - centre) / span * MinimapSize;
                var edge = MinimapSize * 0.5f - 12;
                var round = Mathf.Abs(toPin.x) + Mathf.Abs(toPin.y);
                minimapPin.anchoredPosition = round > edge ? toPin * (edge / round) : toPin;
                line = $"МЕТКА {Distance(new Vector2(pin.x - at.x, pin.z - at.z).magnitude).ToUpperInvariant()}  ·  " + line;
            }

            if (coordsLine.text != line)
            {
                coordsLine.text = line;
            }

            var shown = 0;
            minimapTaken.Clear();

            foreach (var mark in map.Marks)
            {
                if (!mark.Major || shown >= minimapMarks.Count)
                {
                    continue;
                }

                var offset = (map.ToMap(mark.At) - centre) / span * MinimapSize;

                // Only inside the diamond.
                if (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) > MinimapSize * 0.5f - 6)
                {
                    continue;
                }

                var (dot, label) = minimapMarks[shown++];
                dot.gameObject.SetActive(true);
                dot.anchoredPosition = offset;
                var width = mark.Label.Length * label.fontSize * 0.56f;
                var box = new Rect(offset.x - width * 0.5f, offset.y + 4, width, label.fontSize + 4);
                var fits = !minimapTaken.Exists(r => r.Overlaps(box));
                label.gameObject.SetActive(fits);

                if (fits)
                {
                    minimapTaken.Add(box);
                    label.rectTransform.anchoredPosition = offset + new Vector2(0, 10);
                    label.text = mark.Label;
                }
            }

            for (var i = shown; i < minimapMarks.Count; i++)
            {
                minimapMarks[i].dot.gameObject.SetActive(false);
                minimapMarks[i].label.gameObject.SetActive(false);
            }
        }

        Text questCaps;

        /// <summary>The task's card: its heading (and its colour), title, goal and how far along, and the line about more.</summary>
        void SetQuest(string caps, Color capsColour, string title, string goal, string progress, string more)
        {
            questCard.gameObject.SetActive(true);
            questCaps.text = caps.ToUpperInvariant();
            questCaps.color = capsColour;
            questTitle.text = title;
            var line = string.IsNullOrEmpty(progress) ? goal : $"{goal} · <color=#FFF1CF>{progress}</color>";

            if (questGoal.text != line)
            {
                questGoal.text = line;
            }

            questMore.text = more ?? "";
            questMore.gameObject.SetActive(!string.IsNullOrEmpty(more));
        }

        void UpdateQuest()
        {
            // Something going on (a wolf tide) takes the card over, on the proving ground too.
            if (host.Event is var (title, goal, _, progress))
            {
                SetQuest("Событие", UiKit.Danger, title, goal, progress, null);

                return;
            }

            // The immortal's aperture (the proving ground) has no tasks.
            if (host.Sandbox)
            {
                questCard.gameObject.SetActive(false);

                return;
            }

            var quests = host.Quests;
            var current = quests.Current;
            var more = quests.Sides.Count > 0 ? $"Поручений: {quests.Sides.Count} · J" : null;

            if (current != null)
            {
                SetQuest("Задание академии", UiKit.Gold, current.Title, quests.Ready ? $"Вернитесь к: {current.Giver}" : current.Goal,
                    quests.Ready ? "готово" : $"{quests.Progress} / {current.Target}", more);

                return;
            }

            var next = quests.Next;
            var side = quests.Sides.Count > 0 ? quests.Sides[0] : null;

            if (next != null)
            {
                SetQuest("Задание академии", UiKit.Gold, "Новое поручение", $"Вас ждёт {next.GiverTitle ?? ""} {next.Giver}".Replace("  ", " "), null, more);
            }
            else if (side != null)
            {
                // No task of the academy's in hand: the first side task, and how many more there are.
                var ready = quests.IsReady(side);
                SetQuest("Поручение", UiKit.Gold, side.Title, ready ? $"Вернитесь к: {side.Giver}" : side.Goal,
                    ready ? "готово" : $"{quests.ProgressOf(side)} / {side.Target}", quests.Sides.Count > 1 ? $"Поручение · ещё {quests.Sides.Count - 1} · J" : null);
            }
            else
            {
                questCard.gameObject.SetActive(false);
            }
        }

        void UpdateBar(Cultivator gu)
        {
            var fists = host.Fists;

            if (host.Sandbox)
            {
                UpdateHand(gu);
            }
            else
            {
                for (var i = 0; i < slots.Length; i++)
                {
                    var kind = gu.KindOn(i);
                    ShowSlot(slots[i], gu, kind, false, "пусто");
                }

                handLine.text = "";
            }

            // The fists: lit while ready; their chain's diamonds (the next blow bright); the special blows resting.
            if (fists != null)
            {
                var ready = fists.Ready;
                var blow = fists.Current;
                fistSlot.Show(FistKind, 0, 0, -2, true, false, ready, blow.HasValue ? GuWorld.Combat.Melee.Name(blow.Value) : "Кулаки");

                for (var i = 0; i < chainDots.Length; i++)
                {
                    var lit = ready && (fists.SinceBlow < 0.6f || blow.HasValue) && i < fists.ChainStep;
                    chainDots[i].color = !ready ? UiKit.Muted.A(0.3f) : lit ? UiKit.GoldLit : i == fists.ChainStep ? UiKit.Gold : UiKit.Muted;
                }

                Special(heavyCell, fists, GuWorld.Combat.Blow.Heavy);
                Special(spinCell, fists, GuWorld.Combat.Blow.Spin);

                if (ready)
                {
                    handLine.text = $"<b>Кулаки</b>  ·  ЛКМ — серия: джеб → кросс → хук → пинок  ·  ПКМ — блок (в миг удара — парирование)  ·  Alt — перекат  ·  " +
                                    $"<b>Q</b> апперкот{Rest(fists, GuWorld.Combat.Blow.Heavy)}  ·  <b>R</b> разворот{Rest(fists, GuWorld.Combat.Blow.Spin)}  ·  F — убрать";
                }
            }

            var stones = gu.Stones;
            stoneCell.Count.text = $"{stones}";
            stoneCell.Mark.color = stones > 0 ? UiKit.EssenceLit : UiKit.Muted;
            stoneCell.SetRim(gu.Absorbing ? UiKit.EssenceLit : UiKit.Line);
            stoneCell.SetRest(gu.Absorbing ? 1 - gu.AbsorbProgress : 0);

            // The crosshair only while playing (not in a conversation); the arc round it while a stone is drawn in.
            crosshair.gameObject.SetActive(Current == Page.None);
            absorbing.gameObject.SetActive(gu.Absorbing);

            if (gu.Absorbing)
            {
                absorbFill.fillAmount = gu.AbsorbProgress;
                absorbLine.text = $"ПОГЛОЩЕНИЕ КАМНЯ · {(1 - gu.AbsorbProgress) * Cultivator.AbsorbTime:0.0} С";
            }
        }

        /// <summary>The fists' slot's look (a Gu-like kind only for its mark and its colour).</summary>
        static readonly GuKind FistKind = new GuKind { Id = "fists", Name = "Кулаки", Icon = "fist", Rank = 1, Skill = SkillKind.None };

        /// <summary>A special blow's cell: lit while the fists are ready, shaded while it rests.</summary>
        static void Special(Cell cell, GuWorld.Combat.Melee fists, GuWorld.Combat.Blow blow)
        {
            var rest = fists.RestLeft(blow) / GuWorld.Combat.Melee.RestOf(blow);
            cell.SetRest(fists.Ready ? rest : 0);
            cell.Mark.color = !fists.Ready ? UiKit.Muted.A(0.4f) : rest > 0 ? UiKit.Muted : UiKit.GoldLit;
            cell.SetRim(fists.Ready && rest <= 0 ? UiKit.Gold.A(0.7f) : UiKit.Line);
        }

        /// <summary>A Gu on a slot, live: its rest, its tiredness, its price for this master now.</summary>
        static void ShowSlot(GuSlot slot, Cultivator gu, GuKind kind, bool chosen, string empty = "—")
        {
            if (kind == null)
            {
                slot.Empty(empty, chosen);

                return;
            }

            var owned = gu.First(kind.Id);
            var rest = owned != null && kind.Cooldown > 0 ? gu.CooldownLeft(kind.Id) / kind.Cooldown : 0;
            var held = owned != null && gu.IsHeld(kind.Id);
            var price = kind.Passive ? -1 : gu.EndlessEssence ? 0 : gu.CostOf(kind);
            var affordable = kind.Passive || held || gu.EndlessEssence || gu.Aperture.CanSpend(gu.CostOf(kind));
            slot.Show(kind, rest, owned?.tired ?? 0, price, affordable, held, chosen, GuSlot.Short(kind.Name));
        }

        /// <summary>
        /// The immortal's aperture's eight cells at hand, the chosen one raised — and over them, in words, what the chosen
        /// one is, does and costs. Returns the chosen Gu (null: an empty cell).
        /// </summary>
        GuKind UpdateHand(Cultivator gu)
        {
            var bar = host.SandboxBar;

            for (var i = 0; i < slots.Length; i++)
            {
                ShowSlot(slots[i], gu, GuBook.Find(bar[i]), i == host.SandboxSlot, "пусто · Tab");
            }

            var hand = GuBook.Find(bar[Mathf.Clamp(host.SandboxSlot, 0, bar.Length - 1)]);

            if (hand == null)
            {
                handLine.text = $"Ячейка {host.SandboxSlot + 1} пуста — <b>Tab</b>: выбрать Гу";
            }
            else
            {
                var owned = gu.First(hand.Id);
                var tired = owned?.tired ?? 0;
                var state = hand.Passive ? "работает сам" : owned != null && gu.IsHeld(hand.Id) ? "поднят — ЛКМ снять" :
                    tired >= 100 ? $"<color=#F08A74>устал: через силу — шанс гибели {Cultivator.StrainChance(tired) * 100:0}%</color>" :
                    "ЛКМ — применить";
                handLine.text = $"<b>{hand.Name}</b> <color=#{ColorUtility.ToHtmlStringRGB(Aperture.RankColour(hand.Rank))}>{Aperture.RankNumeral(hand.Rank)}</color>  ·  " +
                                $"{hand.SkillName}  ·  {state}\n<size=11><color=#7E9093>{SkillLine(hand)}</color></size>";
            }

            return hand;
        }

        /// <summary>The chips under the hero: what their Gu keep up now — held Gu, timed powers, meditation, a sense.</summary>
        void UpdateChips(Cultivator gu)
        {
            var parts = HeldParts(gu);

            for (var i = 0; i < chips.Count; i++)
            {
                var (chip, words, edge) = chips[i];
                var on = i < parts.Count;

                if (chip.activeSelf != on)
                {
                    chip.SetActive(on);
                }

                if (!on)
                {
                    continue;
                }

                var (text, colour) = parts[i];
                var upper = text.ToUpperInvariant();

                if (words.text != upper)
                {
                    words.text = upper;
                }

                words.color = colour;
                edge.color = colour.A(0.4f);
            }
        }

        /// <summary>What the hero's Gu keep up now, each with its colour: held Gu, timed powers, meditation, the beasts a sense hears.</summary>
        static List<(string, Color)> HeldParts(Cultivator gu)
        {
            var parts = new List<(string, Color)>();

            foreach (var id in gu.Held)
            {
                var kind = GuBook.Find(id);

                if (kind != null)
                {
                    parts.Add((kind.SkillName, UiKit.Moon));
                }
            }

            foreach (var (skill, name) in new[]
                     {
                         (SkillKind.Might, "сила"), (SkillKind.Haste, "скорость"), (SkillKind.Leap, "крылья"), (SkillKind.Flight, "полёт"),
                         (SkillKind.Berserk, "ярость"), (SkillKind.Ward, "завеса"),
                     })
            {
                var left = gu.BuffLeft(skill);

                if (left > 0)
                {
                    parts.Add(($"{name} · {left:0} с", UiKit.Hex(0xF0C868)));
                }
            }

            if (gu.Meditating)
            {
                parts.Add(($"медитация · {gu.Aperture.Progress * 100:0}%", UiKit.EssenceLit));
            }

            var (count, nearest) = gu.Sensed();

            if (nearest >= 0)
            {
                parts.Add((count > 0 ? $"зверей: {count} · {nearest:0} м" : "зверей не слышно", count > 0 ? UiKit.Danger : UiKit.Text2));
            }

            return parts;
        }

        /// <summary>" (1.2 с)" while a special blow rests, else nothing.</summary>
        static string Rest(GuWorld.Combat.Melee fists, GuWorld.Combat.Blow blow)
        {
            var left = fists.RestLeft(blow);

            return left > 0 ? $" ({left:0.0} с)" : "";
        }

        /// <summary>Shows a Gu's key in a cell: the Gu's icon, its rank strip, hunger and rest.</summary>
        static void ShowEntry(Cell cell, Cultivator gu, string entry, bool live)
        {
            cell.Count.text = "";
            cell.Corner.text = "";
            cell.Dot.enabled = false;
            cell.SetRest(0);
            cell.SetRank(0);
            cell.Mark.color = UiKit.Text;

            if (entry == null || !entry.StartsWith("gu:"))
            {
                cell.SetIcon(null);
                cell.SetRim(UiKit.Line);

                return;
            }

            var kind = GuBook.Find(entry.Substring(3));
            var owned = gu.First(kind?.Id);
            cell.SetIcon(kind?.Icon);
            cell.SetRank(kind?.Rank ?? 0);

            if (owned == null)
            {
                cell.Mark.color = UiKit.Muted;
                cell.SetRim(UiKit.Line);

                return;
            }

            var hunger = gu.HungerOf(owned);
            var up = gu.IsHeld(kind.Id);
            cell.Dot.enabled = hunger >= Hunger.Hungry;
            cell.Dot.color = hunger >= Hunger.Starving ? UiKit.Danger : UiKit.Hunger;
            cell.Mark.color = hunger >= Hunger.Starving ? UiKit.Muted : up ? UiKit.EssenceLit : Icons.Tint(kind.Icon);
            // Spent (tired past its strength): the edge burns red — using it now is forcing it.
            cell.SetRim(owned.tired >= 100 ? UiKit.Danger : up ? UiKit.GoldLit : UiKit.Line);

            // The shade over the cell: how tired it is (or, rested, its own short rest).
            if (live)
            {
                var rest = kind.Cooldown > 0 ? gu.CooldownLeft(kind.Id) / kind.Cooldown : 0;
                cell.SetRest(Mathf.Max(rest, Mathf.Clamp01(owned.tired / 100)));
                cell.Count.text = owned.tired >= 1 ? $"{owned.tired:0}" : "";
                cell.Count.color = owned.tired >= 100 ? UiKit.Danger : owned.tired >= 60 ? UiKit.Hunger : UiKit.Muted;
            }

            if (!up && !gu.Aperture.CanSpend(gu.CostOf(kind)))
            {
                cell.Mark.color = UiKit.Danger;
            }
        }

        void UpdateLog()
        {
            var now = Time.unscaledTime;
            var first = Mathf.Max(0, log.Count - LogLines);
            var shown = log.Count - first;

            for (var i = 0; i < LogLines; i++)
            {
                // The newest at the foot, the older ones fainter.
                var index = i - (LogLines - shown);

                if (index < 0)
                {
                    logTexts[i].text = "";

                    continue;
                }

                var (text, at) = log[first + index];
                // The chat shows the log itself while it is open.
                var alpha = Current == Page.Chat ? 0 : Mathf.Clamp01((LogFor - (now - at)) / 2) * Mathf.Lerp(0.5f, 1, (index + 1f) / shown);
                var said = text.StartsWith("> ") || text.StartsWith("!") || text.StartsWith("<color");
                logTexts[i].text = said ? ChatLine(text) : text;
                logTexts[i].color = UiKit.Text2.A(alpha);
            }
        }
    }
}
