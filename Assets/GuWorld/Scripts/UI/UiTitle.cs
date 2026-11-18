using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The pages before a game (ROADMAP 2.2, 2.6, 2.8; main's UI-Title.dc.html): the main menu — night
    /// over Qing Mao, a crescent moon in gold rings, two ranges in mist with a village's lit windows,
    /// motes of essence drifting up, the 蛊 brush mark and the name, the menu under them; the slots
    /// page (load, start anew, empty); the page about the game.
    /// </summary>
    public partial class Ui
    {
        public const string Version = "0.3.0";

        static Texture2D titleLand;
        static Sprite titleMoon;

        /// <summary>A session for pages opened from the main menu (the codex): every slot's pages together. Null in a game.</summary>
        Session guestSession;

        /// <summary>The slots page opened to start a new game in a slot (not to load one).</summary>
        bool slotsForNew;

        void Title()
        {
            guestSession = null;
            var stage = UiKit.Stage(pageRoot);

            // The sky: a gradient and two clouds of light.
            var sky = UiKit.Image("Sky", stage, Color.white, UiKit.Plain);
            sky.rectTransform.Fill();
            sky.raycastTarget = true;
            sky.Gradient(UiKit.Hex(0x060A14), UiKit.Void, false, UiKit.Void, 0.7f);
            UiKit.Cloud(stage, new Vector2(0.74f, 0.70f), new Vector2(1800, 1200), new Color(60 / 255f, 100 / 255f, 160 / 255f, 0.32f));
            UiKit.Cloud(stage, new Vector2(0.5f, 0), new Vector2(2400, 800), new Color(40 / 255f, 160 / 255f, 130 / 255f, 0.22f));
            UiKit.StarField(stage, 0.6f);

            // The moon: its breathing glow, the crescent, two gold rings round it.
            var moon = UiKit.Rect("Moon", stage);
            moon.TopRight(60, 40, 520, 520);
            var haze = UiKit.Image("Haze", moon, new Color(200 / 255f, 220 / 255f, 245 / 255f, 0.32f), UiKit.Radial);
            haze.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(734, 734));
            var breath = haze.gameObject.AddComponent<Pulse>();
            breath.Period = 9;
            breath.Low = 0.6f;
            breath.High = 1;
            breath.Grow = 0.05f;
            UiKit.HaloAt(moon, new Color(1, 1, 1, 0.22f), Vector2.zero, 250);
            var crescent = UiKit.Image("Crescent", moon, Color.white, TitleMoon());
            crescent.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 280));
            UiKit.Orbit(moon, 280, UiKit.Gold.A(0.22f), 0, 0);
            UiKit.Orbit(moon, 380, UiKit.Gold.A(0.12f), 160, 10);

            // The land: the mountains, the village, the mist (one painting).
            var land = UiKit.Rect("Land", stage).gameObject.AddComponent<RawImage>();
            land.texture = TitleLand();
            land.raycastTarget = false;
            land.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -480);

            // A band of fog drifting to and fro, and the motes rising.
            var fog = UiKit.Image("Fog", stage, new Color(150 / 255f, 175 / 255f, 210 / 255f, 0.12f), UiKit.Radial);
            fog.rectTransform.Place(new Vector2(-0.1f, 0), new Vector2(1.1f, 0), -43, 120, 43, -320);
            var sway = fog.gameObject.AddComponent<Bob>();
            sway.Period = 26;
            sway.Rise = 86;
            sway.Way = Vector2.right;

            for (var m = 0; m < 26; m++)
            {
                var x = 500 + Fract(Mathf.Sin(m * 12.9898f) * 43758.5453f) * 900;
                var y = 300 + Fract(Mathf.Sin(m * 78.233f) * 12345.678f) * 500;
                var size = 1 + m % 3 * 0.8f;
                var colour = m % 4 == 0 ? new Color(232 / 255f, 201 / 255f, 138 / 255f, 0.9f) : new Color(170 / 255f, 240 / 255f, 220 / 255f, 0.8f);
                var mote = UiKit.Image("Mote", stage, colour, UiKit.Halo);
                mote.rectTransform.TopLeft(x - size * 2, y - size * 2, size * 5, size * 5);
                var drift = mote.gameObject.AddComponent<Drift>();
                drift.Period = 9 + m % 5 * 2;
                drift.Delay = m * 0.9f;
            }

            UiKit.Image("Vignette", stage, new Color(0, 0, 0, 0.78f), UiKit.Vignette).rectTransform.Fill();

            // The brush mark and the name.
            var mark = UiKit.Brush("蛊", stage, 92, UiKit.Gold);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0, 1);
            mark.rectTransform.anchoredPosition = new Vector2(72 + 46, -96 - 46);
            UiKit.HaloAt(stage, UiKit.Gold.A(0.16f), new Vector2(72 + 46, -96 - 46), 180, new Vector2(0, 1)).transform.SetSiblingIndex(mark.transform.GetSiblingIndex());
            var name = UiKit.Disp("GU WORLD", stage, 76, Color.white);
            name.Track(0.14f);
            name.Gradient(Color.white, UiKit.Hex(0xBFAE8A), false, UiKit.Hex(0xF2EADB));
            name.rectTransform.TopLeft(182, 92, 700, 80);
            UiKit.Lbl("Лунная тень Цинмао · путь мастера Гу", stage, 12, UiKit.LabelGold, 0.5f).rectTransform.TopLeft(184, 168, 600, 18);

            // The menu.
            var menu = UiKit.Rect("Menu", stage);
            menu.TopLeft(72, 300, 460, 560);
            menu.Stack<VerticalLayoutGroup>(2, null, TextAnchor.UpperLeft);
            var keys = menu.gameObject.AddComponent<MenuKeys>();
            var save = Game.ReadSave();

            if (save != null)
            {
                keys.Add(UiKit.Entry("Продолжить", SaveLine(save), menu, () => Game.Instance.Load(Game.Slot)).Restyle(30, 78));
            }

            keys.Add(UiKit.Entry("Новая игра", null, menu, NewGameFromTitle).Restyle(24, 54, "путь и герой"));

            if (AnySave())
            {
                keys.Add(UiKit.Entry("Загрузить", null, menu, () => { slotsForNew = false; Show(Page.Slots); }).Restyle(24, 54, $"{Game.Slots} ячейки"));
            }

            keys.Add(UiKit.Entry("Апертура бессмертного", null, menu, () => Game.Instance.BeginSandbox()).Restyle(24, 54, "полигон"));
            keys.Add(UiKit.Entry("Кодекс Гу", null, menu, OpenGuestCodex).Restyle(24, 54, $"{GuBook.All.Length} Гу"));
            keys.Add(UiKit.Entry("Настройки", null, menu, () => OpenSettings(Page.Title)).Restyle(24, 54));
            keys.Add(UiKit.Entry("Об игре", null, menu, () => Show(Page.About)).Restyle(24, 54));
            var gap = UiKit.Rect("Gap", menu);
            gap.Size(21);
            UiKit.Image("Rule", gap, UiKit.Gold.A(0.3f), UiKit.SoftEdge).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 26, 0, 0, -1);
            keys.Add(UiKit.Entry("Выйти из игры", null, menu, Game.Quit).Quiet(48));

            // The mountain's name.
            var place = UiKit.Rect("Place", stage);
            place.BottomRight(72, 96, 420, 44);
            UiKit.Lbl("Гора Цинмао · Южная граница", place, 11, UiKit.LabelGold, 0.34f, TextAnchor.UpperRight).rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -16, 0, 0);
            UiKit.Label("Клан Гу Юэ · арка первая", place, 13, UiKit.Muted, TextAnchor.LowerRight).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -20);

            UiKit.Footer(stage, new[] { ("↑↓", "Выбор"), ("Enter", "Открыть") }, 72, 28, false, null);
            var version = UiKit.Label($"Версия {Version}", stage, 12, UiKit.Muted);
            version.Track(0.06f);
            version.rectTransform.BottomLeft(72, 28, 300, 30);
        }

        /// <summary>"Гу Юэ Мо · Сложная · Ранг I · средняя ступень · глава 2 · 47 мин".</summary>
        static string SaveLine(SaveData save)
        {
            var minutes = Mathf.RoundToInt(save.PlayedSeconds / 60);

            return $"Гу Юэ {save.HeroName} · {DifficultyRules.Name(DifficultyRules.Of(save))} · {Cultivation.Name(save.Step)} · {minutes} мин";
        }

        static bool AnySave()
        {
            for (var slot = 0; slot < Game.Slots; slot++)
            {
                if (Game.ReadSlot(slot) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>"Новая игра": straight to the path if this slot is free, else the slots to choose where.</summary>
        void NewGameFromTitle()
        {
            if (Game.ReadSave() == null)
            {
                StartNewGame();
            }
            else
            {
                slotsForNew = true;
                Show(Page.Slots);
            }
        }

        static float Fract(float v) => ((v % 1) + 1) % 1;

        // ------------------------------------------------------------------ The slots

        void SlotsPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "档", 560);
            NewGameHeader(stage, 0, slotsForNew ? "Где начать" : "Загрузить",
                slotsForNew ? "Выберите ячейку для нового пути. Занятая ячейка будет перезаписана — удерживайте кнопку, чтобы подтвердить."
                            : "Три ячейки, каждая — своя жизнь. Игра сохраняется сама; рядом с каждой лежит резервная копия.");
            var row = UiKit.Rect("Slots", stage);
            row.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -720, 56, 186);
            var across = row.Stack<HorizontalLayoutGroup>(20, null, TextAnchor.UpperLeft);
            across.childControlHeight = true;
            across.childForceExpandHeight = true;

            for (var slot = 0; slot < Game.Slots; slot++)
            {
                SlotCard(row, slot);
            }

            var foot = UiKit.Rect("Foot", stage);
            foot.Place(new Vector2(0, 0), new Vector2(1, 0), 56, 50, 56, -100);
            var buttons = foot.Stack<HorizontalLayoutGroup>(20, null, TextAnchor.MiddleLeft);
            buttons.childForceExpandWidth = false;
            UiKit.HexButton("Назад", foot, () => Show(Page.Title), UiKit.HexKind.Ghost, 13, 50, "Esc").Size(50, 190, 0);
            UiKit.Footer(stage, new[] { ("Esc", "Назад") });
            stage.gameObject.AddComponent<PageKeys>().On(Key.Escape, () => Show(Page.Title));
        }

        void SlotCard(Transform parent, int slot)
        {
            var save = Game.ReadSlot(slot);
            var current = slot == Game.Slot;
            var card = UiKit.Image($"Slot {slot + 1}", parent, Color.white, UiKit.Plain);
            card.Size(-1, -1, 1);
            card.Gradient(current ? new Color(28 / 255f, 26 / 255f, 22 / 255f, 0.86f) : new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.72f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.86f));
            UiKit.Frame(card.transform, current ? UiKit.GoldLit.A(0.4f) : new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));

            if (current)
            {
                UiKit.GoldCorners(card.transform, 14);
            }

            var column = UiKit.Rect("Column", card.transform).Fill();
            column.Stack<VerticalLayoutGroup>(6, new RectOffset(22, 22, 20, 20));

            // The world as it was at the last save.
            var shot = SlotShot(slot);

            if (shot != null && save != null)
            {
                var frame = UiKit.Rect("Shot", column);
                frame.Size(130);
                var picture = UiKit.Rect("Picture", frame).gameObject.AddComponent<RawImage>();
                picture.texture = shot;
                picture.raycastTarget = false;
                picture.rectTransform.Fill();
                var fadeOut = UiKit.Image("Fade", frame, Color.white, UiKit.Plain);
                fadeOut.rectTransform.Fill();
                fadeOut.raycastTarget = false;
                fadeOut.Gradient(new Color(5 / 255f, 7 / 255f, 12 / 255f, 0), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.85f));
                UiKit.Frame(frame, new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.12f));
            }

            UiKit.Lbl($"Ячейка {slot + 1}{(current ? " · последняя" : "")}", column, 11, UiKit.LabelGold).Size(16);

            if (save == null)
            {
                UiKit.Disp("Пусто", column, 34, UiKit.Muted).Size(44);
                Para("Здесь ещё никто не родился.", column, 14, UiKit.Muted);
                UiKit.Rect("Rest", column).Size(-1).flexibleHeight = 1;
                UiKit.HexButton("Новая игра здесь", column, () => { Game.Slot = slot; StartNewGame(); }, UiKit.HexKind.Main, 13, 50).Size(50);

                return;
            }

            var path = DifficultyRules.Of(save);
            UiKit.Disp($"Гу Юэ {save.HeroName}", column, 32, UiKit.GoldText).Size(42);
            Fact(column, "Путь", $"{DifficultyRules.Name(path)} · талант {Cultivation.TalentName(save.Talent)}");
            Fact(column, "Ступень", Cultivation.Name(save.Step));
            Fact(column, "Смерть", DeathRules.Name(save.Settings?.Death ?? DeathRule.Lantern));
            Fact(column, "В игре", $"{Mathf.RoundToInt(save.PlayedSeconds / 60)} мин · побед {save.Kills} · смертей {save.Deaths}");
            var when = Game.SavedAt(slot);

            if (when != null)
            {
                Fact(column, "Сохранено", when.Value.ToString("dd.MM.yyyy HH:mm"));
            }

            if (Game.OnBackup(slot))
            {
                Para("Основной файл повреждён — загрузится резервная копия.", column, 13, UiKit.Danger);
            }

            UiKit.Rect("Rest", column).Size(-1).flexibleHeight = 1;

            if (slotsForNew)
            {
                UiKit.HexButton("Удерживать — заменить", column, () => { Game.DeleteSlot(slot); Game.Slot = slot; StartNewGame(); }, UiKit.HexKind.Bad, 12, 50).Held(1.2f).Size(50);
            }
            else
            {
                UiKit.HexButton("Загрузить", column, () => Game.Instance.Load(slot), UiKit.HexKind.Solid, 13, 50).Size(50);
                UiKit.HexButton("Удерживать — удалить", column, () => { Game.DeleteSlot(slot); Show(AnySave() ? Page.Slots : Page.Title); }, UiKit.HexKind.Bad, 11, 42).Held(1.2f).Size(42);
            }
        }

        readonly Dictionary<int, (System.DateTime when, Texture2D picture)> slotShots = new Dictionary<int, (System.DateTime, Texture2D)>();

        /// <summary>A slot's picture, loaded once while its file is unchanged (null if there is none).</summary>
        Texture2D SlotShot(int slot)
        {
            var path = Game.ShotPath(slot);

            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            var when = System.IO.File.GetLastWriteTime(path);

            if (slotShots.TryGetValue(slot, out var have) && have.when == when && have.picture != null)
            {
                return have.picture;
            }

            try
            {
                var picture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                picture.LoadImage(System.IO.File.ReadAllBytes(path));
                slotShots[slot] = (when, picture);

                return picture;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ The codex from the main menu

        /// <summary>The codex as every slot has opened it together (the main menu's "Кодекс Гу").</summary>
        void OpenGuestCodex()
        {
            var all = Session.NewGame("Гость");

            for (var slot = 0; slot < Game.Slots; slot++)
            {
                var save = Game.ReadSlot(slot);

                if (save == null)
                {
                    continue;
                }

                foreach (var page in save.Codex ?? new List<string>())
                {
                    if (!all.Codex.Contains(page))
                    {
                        all.Codex.Add(page);
                    }
                }

                foreach (var kills in save.KillsBy ?? new List<Tally>())
                {
                    var mine = all.KillsBy.Find(k => k.Id == kills.Id);

                    if (mine == null)
                    {
                        all.KillsBy.Add(new Tally { Id = kills.Id, Count = kills.Count });
                    }
                    else
                    {
                        mine.Count = Mathf.Max(mine.Count, kills.Count);
                    }
                }
            }

            guestSession = new Session(all, 1);
            Show(Page.Codex);
        }

        // ------------------------------------------------------------------ About

        void AboutPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "蛊", 560);
            NewGameHeader(stage, 0, "Об игре", "GU World 2 · «Лунная тень Цинмао» — экшен-RPG по мотивам новеллы «Преподобный Гу» (Reverend Insanity, Гу Чжэнь Жэнь).");
            var body = UiKit.Rect("Body", stage);
            body.Place(Vector2.zero, Vector2.one, 56, 120, 56, 200);
            var left = Column(body, 0, 0.48f, 8);
            var right = Column(body, 0.52f, 1, 8);

            UiKit.Lbl("Игра", left, 11, UiKit.LabelGold).Size(16);
            Fact(left, "Версия", Version);
            Fact(left, "Автор", "Abdukabir Qurbanaliyev");
            Fact(left, "Движок", "Unity");
            Gap(left, 12);
            UiKit.Lbl("Где лежит", left, 11, UiKit.LabelGold).Size(16);
            Para($"Сохранения и настройки: {Application.persistentDataPath}", left, 13, UiKit.Muted);
            UiKit.HexButton("Открыть папку", left, () => Application.OpenURL("file://" + Application.persistentDataPath), UiKit.HexKind.Ghost, 12, 44).Size(44, 220, 0);

            UiKit.Lbl("Чужие работы", right, 11, UiKit.LabelGold).Size(16);
            Para("Модель лисы: PixelMannen (CC0). Риг и анимация: tomkranis (CC BY 4.0). glTF: AsoboStudio, scurest (CC BY 4.0).", right, 14, UiKit.Text2);
            Para("Шрифты: Cormorant Garamond, Ma Shan Zheng, Zhi Mang Xing (SIL Open Font License).", right, 14, UiKit.Text2);
            Gap(right, 12);
            UiKit.Lbl("Мир", right, 11, UiKit.LabelGold).Size(16);
            Para("Мир, кланы, Гу и пути — из новеллы «Преподобный Гу». Всё остальное — модели, звук, музыка, мир горы — сделано кодом.", right, 14, UiKit.Text2);

            var foot = UiKit.Rect("Foot", stage);
            foot.Place(new Vector2(0, 0), new Vector2(1, 0), 56, 50, 56, -100);
            foot.Stack<HorizontalLayoutGroup>(20, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            UiKit.HexButton("Назад", foot, () => Show(Page.Title), UiKit.HexKind.Ghost, 13, 50, "Esc").Size(50, 190, 0);
            UiKit.Footer(stage, new[] { ("Esc", "Назад") });
            stage.gameObject.AddComponent<PageKeys>().On(Key.Escape, () => Show(Page.Title));
        }

        // ------------------------------------------------------------------ The title's pictures

        /// <summary>The moon of the main menu: a pale disc with a dark one over it, a crescent (280 across, its middle the moon's).</summary>
        static Sprite TitleMoon()
        {
            if (titleMoon != null)
            {
                return titleMoon;
            }

            const int size = 280;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color[size * size];
            var light = new Vector2(140 - 96 + 0.35f * 192, 140 - 96 + 0.35f * 192);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // y down, the moon's middle at 140, 140; the dark disc 36 right and 22 up.
                    var p = new Vector2(x + 0.5f, size - y - 0.5f);
                    var moon = Mathf.Clamp01(96.5f - Vector2.Distance(p, new Vector2(140, 140)));
                    var dark = Mathf.Clamp01(88.5f - Vector2.Distance(p, new Vector2(176, 118)));
                    var t = Mathf.Clamp01(Vector2.Distance(p, light) / (0.7f * 192));
                    var lit = t < 0.6f ? Color.Lerp(Color.white, UiKit.Hex(0xE4ECF6), t / 0.6f) : Color.Lerp(UiKit.Hex(0xE4ECF6), UiKit.Hex(0x9FB4CC), (t - 0.6f) / 0.4f);
                    var colour = Color.Lerp(lit, UiKit.Hex(0x071022), dark);
                    colour.a = Mathf.Max(moon, dark);
                    pixels[y * size + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return titleMoon = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>The main menu's land (the design's 1440 wide, from 420 down to the foot): two ranges, snow, a village, mist.</summary>
        static Texture2D TitleLand()
        {
            if (titleLand != null)
            {
                return titleLand;
            }

            var paint = new UiPainter(1440, 480);

            Vector2[] Shift(IEnumerable<Vector2> points)
            {
                var list = new List<Vector2>();

                foreach (var p in points)
                {
                    list.Add(new Vector2(p.x, p.y - 420));
                }

                return list.ToArray();
            }

            paint.Polygon(Shift(UiPainter.Path(0, 640, 120, 560, 210, 600, 330, 500, 420, 548, 540, 470, 640, 530, 760, 430, 860, 500, 980, 420,
                1100, 520, 1210, 470, 1330, 540, 1440, 500, 1440, 900, 0, 900)), UiKit.Hex(0x16263A), UiKit.Hex(0x07090F));
            paint.Polygon(Shift(UiPainter.Path(760, 430, 800, 470, 776, 466, 760, 446, 744, 470, 724, 470)), new Color(200 / 255f, 220 / 255f, 240 / 255f, 0.25f));
            paint.Polygon(Shift(UiPainter.Path(980, 420, 1010, 450, 992, 448, 980, 436, 966, 452)), new Color(200 / 255f, 220 / 255f, 240 / 255f, 0.2f));
            paint.Soft(new Vector2(760, 600 - 420), new Vector2(700, 50), UiKit.Hex(0x8FB0D0, 0.1f), 22);
            paint.Polygon(Shift(UiPainter.Path(0, 720, 180, 640, 300, 690, 460, 620, 600, 700, 760, 640, 900, 710, 1060, 650, 1200, 720, 1320, 680,
                1440, 710, 1440, 900, 0, 900)), UiKit.Hex(0x0C1622), UiKit.Hex(0x040508));

            // The village: three houses with curved roofs, three lit windows.
            var house = UiKit.Hex(0x05070C);
            paint.Rect(930, 662 - 420, 40, 30, house);
            paint.Polygon(Shift(Roof(912, 666, 950, 650, 988, 666, 976, 660, 970, 652, 930, 652, 924, 660)), house);
            paint.Rect(938, 636 - 420, 24, 18, house);
            paint.Polygon(Shift(Roof(924, 640, 950, 628, 976, 640, 968, 634, 964, 628, 936, 628, 932, 634)), house);
            paint.Rect(1010, 676 - 420, 46, 24, house);
            paint.Polygon(Shift(Roof(992, 680, 1033, 666, 1074, 680, 1062, 674, 1056, 668, 1010, 668, 1004, 674)), house);
            var window = UiKit.Hex(0xF0C878);
            paint.Rect(942, 672 - 420, 4, 7, window.A(0.8f));
            paint.Rect(956, 672 - 420, 4, 7, window.A(0.6f));
            paint.Rect(1026, 684 - 420, 4, 7, window.A(0.7f));

            paint.Soft(new Vector2(720, 760 - 420), new Vector2(800, 60), UiKit.Hex(0x7FE0C8, 0.07f), 22);
            var ground = new List<Vector2> { new Vector2(0, 800) };
            ground.AddRange(UiPainter.Cubic(new Vector2(0, 800), new Vector2(240, 760), new Vector2(480, 790), new Vector2(720, 770)));
            ground.AddRange(UiPainter.Cubic(new Vector2(720, 770), new Vector2(960, 750), new Vector2(1200, 790), new Vector2(1440, 770)));
            ground.Add(new Vector2(1440, 900));
            ground.Add(new Vector2(0, 900));
            paint.Polygon(Shift(ground), UiKit.Hex(0x020306));

            return titleLand = paint.Texture();
        }

        /// <summary>A roof as the design draws it: two curves and the eaves' line.</summary>
        static List<Vector2> Roof(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy, float ex, float ey, float fx, float fy, float gx, float gy)
        {
            var a = new Vector2(ax, ay);
            var points = new List<Vector2> { a };
            points.AddRange(UiPainter.Curve(a, new Vector2(bx, by), new Vector2(cx, cy)));
            points.AddRange(UiPainter.Curve(new Vector2(cx, cy), new Vector2(dx, dy), new Vector2(ex, ey)));
            points.Add(new Vector2(fx, fy));
            points.AddRange(UiPainter.Curve(new Vector2(fx, fy), new Vector2(gx, gy), a));

            return points;
        }
    }
}
