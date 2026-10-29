using System.Globalization;
using System.Linq;
using GuWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The builder's page (F9; WorldBuilder does the work), laid out as a strategy game's world builder: no dimming —
    /// the world is what one looks at. Along the top the modes (things, ground, woods), the views (from above, 3D),
    /// taking back and doing again, resetting, saving, leaving. On the left the mode's tools and their settings, and
    /// for the things what is chosen (its place, a turn, a size, a height, copying, removing, putting back). On the
    /// right, for the things, the catalog with a search. Along the bottom what the mouse and keys do, what was done,
    /// and where the mouse points.
    /// </summary>
    public partial class Menus
    {
        string builderSection = "Дома";
        string builderSearch = "";
        float builderScroll = 1;
        bool builderReset;

        static readonly string[][] BuilderSectionRows =
        {
            new[] { "Дома", "Клан", "Крепость" },
            new[] { "Лестницы и тропы", "Декор", "Деревья" },
            new[] { "Внутри", "Гу", "Прочее" },
        };

        static readonly float[] BuilderGrids = { 0, 0.5f, 1, 2, 5 };

        /// <summary>
        /// Keeps the page with the builder: made again when the builder's state asks (the mode, the tool, the chosen
        /// things), and its live lines (status, help, where the mouse is, the chosen thing's place) every frame.
        /// </summary>
        class BuilderFeed : MonoBehaviour
        {
            public Menus Owner;
            public int Version;
            public Text Status;
            public Text Help;
            public Text ViewHelp;
            public Text Under;
            public Text Info;
            public Slider Size;
            public Text SizeNumber;
            public Text Flatten;

            void Update()
            {
                var builder = WorldBuilder.Current;

                if (builder == null || Owner == null || Owner.Current != Page.Builder)
                {
                    return;
                }

                if (builder.PageVersion != Version)
                {
                    Version = builder.PageVersion;
                    Owner.Show(Page.Builder);

                    return;
                }

                if (Status != null) Status.text = builder.Status;
                if (Help != null) Help.text = builder.Help;
                if (ViewHelp != null) ViewHelp.text = builder.ViewHelp;
                if (Under != null) Under.text = builder.Under;
                if (Info != null) Info.text = builder.ChosenInfo;
                if (Flatten != null) Flatten.text = FlattenText(builder);

                // The size slider follows the keys (not while it is being dragged).
                if (Size != null && Mouse.current != null && !Mouse.current.leftButton.isPressed)
                {
                    Size.SetValueWithoutNotify(builder.ChosenSize);

                    if (SizeNumber != null)
                    {
                        SizeNumber.text = builder.ChosenSize.ToString("0.00", CultureInfo.InvariantCulture);
                    }
                }
            }
        }

        static string FlattenText(WorldBuilder builder) => float.IsNaN(builder.FlattenTo)
            ? "Высота: где начнёте вести кистью"
            : $"Высота: {builder.FlattenTo.ToString("0.0", CultureInfo.InvariantCulture)} м";

        void BuilderPage()
        {
            var builder = WorldBuilder.Current;

            if (builder == null || !builder.Ready)
            {
                Notify("Строитель работает на горе Цинмао, когда мир загружен");
                host.Resume();

                return;
            }

            builder.Begin();
            // The feed goes with the page (a child of its root): it never brings the builder back once it is closed.
            var feed = UiKit.Rect("Builder Feed", pageRoot).gameObject.AddComponent<BuilderFeed>();
            feed.Owner = this;
            feed.Version = builder.PageVersion;

            BuilderTop(builder);
            BuilderLeft(builder, feed);

            if (builder.CurrentMode == WorldBuilder.Mode.Things)
            {
                BuilderCatalog(builder);
            }

            BuilderBottom(builder, feed);

            if (builderReset)
            {
                BuilderResetPanel(builder);
            }
        }

        // ── Along the top ──────────────────────────────────────────────

        void BuilderTop(WorldBuilder builder)
        {
            var top = UiKit.Panel("Builder Top", pageRoot);
            top.rectTransform.Place(new Vector2(0, 1), Vector2.one, 12, -70, 12, 12);
            var row = UiKit.Rect("Inside", top.transform).Fill();
            row.Stack<HorizontalLayoutGroup>(8, new RectOffset(12, 12, 7, 7), TextAnchor.MiddleLeft).childForceExpandWidth = false;

            var title = UiKit.Label("СТРОИТЕЛЬ", row, 20, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.Size(44, 150);

            void ModeButton(string text, WorldBuilder.Mode mode)
            {
                UiKit.Button(text, row, () => builder.SetMode(mode), builder.CurrentMode == mode ? ButtonKind.Tab : ButtonKind.Plain, 17).Size(44, 130);
            }

            ModeButton("Объекты", WorldBuilder.Mode.Things);
            ModeButton("Земля", WorldBuilder.Mode.Ground);
            ModeButton("Лес", WorldBuilder.Mode.Woods);
            Gap(row, 18);

            void ViewButton(string text, WorldBuilder.View view)
            {
                UiKit.Button(text, row, () => builder.SetView(view), builder.CurrentView == view ? ButtonKind.Tab : ButtonKind.Plain, 16).Size(44, 104);
            }

            ViewButton("Сверху", WorldBuilder.View.Above);
            ViewButton("3D", WorldBuilder.View.Around);
            Gap(row, 18);

            UiKit.Button("Отменить · Ctrl+Z", row, builder.Undo, ButtonKind.Plain, 15).Size(44, 170);
            UiKit.Button("Повторить · Ctrl+Y", row, builder.Redo, ButtonKind.Plain, 15).Size(44, 178);

            var spacer = UiKit.Rect("Spacer", row);
            spacer.Size(10, 10, 1);

            UiKit.Button("Сбросить…", row, () =>
            {
                builderReset = !builderReset;
                Show(Page.Builder);
            }, builderReset ? ButtonKind.Tab : ButtonKind.Plain, 15).Size(44, 120);
            UiKit.Button("Сохранить", row, builder.Save, ButtonKind.Plain, 15).Size(44, 116);
            UiKit.Button("Герой сюда", row, () =>
            {
                var at = builder.GroundAhead();
                builder.End();
                host.Teleport(at);
                host.Resume();
            }, ButtonKind.Plain, 15).Size(44, 124);
            UiKit.Button("Выйти · F9", row, () =>
            {
                builder.End();
                host.Resume();
            }, ButtonKind.Gold, 15).Size(44, 124);
        }

        static void Gap(Transform row, float width)
        {
            UiKit.Rect("Gap", row).Size(10, width);
        }

        // ── On the left ────────────────────────────────────────────────

        void BuilderLeft(WorldBuilder builder, BuilderFeed feed)
        {
            var left = UiKit.Panel("Builder Tools", pageRoot);
            left.rectTransform.Place(Vector2.zero, new Vector2(0, 1), 12, 104, -352, 82);
            var column = UiKit.Rect("Inside", left.transform).Fill();
            column.Stack<VerticalLayoutGroup>(6, new RectOffset(14, 14, 12, 12));

            switch (builder.CurrentMode)
            {
                case WorldBuilder.Mode.Things: ThingsTools(builder, feed, column); break;
                case WorldBuilder.Mode.Ground: GroundTools(builder, feed, column); break;
                default: WoodsTools(builder, column); break;
            }
        }

        static Text Heading(string text, Transform parent)
        {
            var label = UiKit.Label(text, parent, 15, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.Size(26);

            return label;
        }

        static Text Note(string text, Transform parent, float height)
        {
            var label = UiKit.Label(text, parent, 14, UiKit.Muted, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.Size(height);

            return label;
        }

        /// <summary>A row of small buttons, evenly wide.</summary>
        static void Buttons(Transform parent, params (string text, System.Action click)[] buttons)
        {
            var row = UiKit.Rect("Buttons", parent);
            row.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = true;
            row.Size(38);

            foreach (var (text, click) in buttons)
            {
                UiKit.Button(text, row, () => click(), ButtonKind.Plain, 14).Size(38, -1, 1);
            }
        }

        /// <summary>A named slider (its value shown beside it).</summary>
        static Slider Setting(string name, Transform parent, float min, float max, float value, System.Func<float, string> show, System.Action<float> change)
        {
            UiKit.Label(name, parent, 14, UiKit.Muted).Size(20);
            var slider = UiKit.Slider(parent, min, max, value, show, change);
            slider.Size(36);

            return slider;
        }

        void ThingsTools(WorldBuilder builder, BuilderFeed feed, Transform column)
        {
            Heading("ОБЪЕКТЫ", column);

            if ((builder.CurrentTool == WorldBuilder.Tool.Place || builder.CurrentTool == WorldBuilder.Tool.Stretch) && builder.Chosen != null)
            {
                UiKit.Label((builder.CurrentTool == WorldBuilder.Tool.Place ? "Ставите: " : "Протягиваете: ") + builder.Chosen.Label, column, 17, UiKit.GoldLit).Size(28);
                Note(builder.CurrentTool == WorldBuilder.Tool.Place
                    ? "Щёлкайте по земле — ставится каждый раз. R/T — повернуть, +/− — размер, PgUp/PgDn — выше/ниже."
                    : "Щёлкните начало, потом конец. +/− — ширина.", column, 58);
                Buttons(column, ("Хватит · Esc", () => builder.Choose(WorldBuilder.Tool.Select)));
            }

            UiKit.Button(builder.FollowGround ? "Сдвиг по земле: да" : "Сдвиг по земле: нет", column, () =>
            {
                builder.FollowGround = !builder.FollowGround;
                Show(Page.Builder);
            }, builder.FollowGround ? ButtonKind.Tab : ButtonKind.Plain, 14).Size(36);
            UiKit.Button(builder.RandomTurn ? "Случайный поворот: да" : "Случайный поворот: нет", column, () =>
            {
                builder.RandomTurn = !builder.RandomTurn;
                Show(Page.Builder);
            }, builder.RandomTurn ? ButtonKind.Tab : ButtonKind.Plain, 14).Size(36);
            UiKit.Label("Сетка", column, 14, UiKit.Muted).Size(20);
            UiKit.Choices(column, BuilderGrids.Select(g => g == 0 ? "нет" : g.ToString("0.#", CultureInfo.InvariantCulture) + " м").ToList(),
                System.Array.IndexOf(BuilderGrids, builder.GridStep), i =>
                {
                    builder.GridStep = BuilderGrids[i];
                    Show(Page.Builder);
                }, 14);

            Heading(builder.ChosenCount == 0 ? "ВЫБРАНО: НИЧЕГО" : builder.ChosenName.ToUpperInvariant(), column);

            if (builder.ChosenCount == 0)
            {
                Note("Щёлкните дом или вещь — выберется.\nShift+щелчок — добавить к выбранным.\nПотяните по пустому месту — рамка.\nТяните выбранное — сдвинется.\nСправа каталог: щёлкните модель и ставьте.", column, 120);

                return;
            }

            var info = UiKit.Label(builder.ChosenInfo, column, 14, UiKit.Text, TextAnchor.UpperLeft);
            info.Size(62);
            feed.Info = info;

            UiKit.Label("Поворот", column, 14, UiKit.Muted).Size(20);
            Buttons(column, ("−90°", () => builder.Turn(-90)), ("−15°", () => builder.Turn(-15)), ("+15°", () => builder.Turn(15)), ("+90°", () => builder.Turn(90)));

            UiKit.Label("Размер", column, 14, UiKit.Muted).Size(20);
            var sizeRow = UiKit.Rect("Size", column);
            sizeRow.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            sizeRow.Size(38);
            UiKit.Button("−", sizeRow, () => builder.ScaleBy(1 / 1.1f), ButtonKind.Plain, 18).Size(38, 40);
            var slider = UiKit.Slider(sizeRow, 0.1f, 5, builder.ChosenSize, v => v.ToString("0.00", CultureInfo.InvariantCulture), builder.SetChosenSize);
            slider.Size(38, -1, 1);
            UiKit.Button("+", sizeRow, () => builder.ScaleBy(1.1f), ButtonKind.Plain, 18).Size(38, 40);
            feed.Size = slider;
            feed.SizeNumber = slider.GetComponentsInChildren<Text>().LastOrDefault();

            UiKit.Label("Высота", column, 14, UiKit.Muted).Size(20);
            Buttons(column, ("−1 м", () => builder.Lift(-1)), ("−0.1", () => builder.Lift(-0.1f)), ("+0.1", () => builder.Lift(0.1f)), ("+1 м", () => builder.Lift(1)));

            Buttons(column, ("На землю", builder.DropToGround), ("На своё место", builder.PutBack));
            Buttons(column, ("Копия · Ctrl+D", builder.Duplicate), ("Убрать · Del", builder.RemoveChosen));
            Buttons(column, ("Копировать · Ctrl+C", builder.Copy), ("Показать · F", builder.LookAtChosen));
        }

        void GroundTools(WorldBuilder builder, BuilderFeed feed, Transform column)
        {
            Heading("ЗЕМЛЯ", column);
            ToolGrid(builder, column);

            Setting("Кисть", column, 1, 120, builder.Radius, v => $"{v:0} м", v => builder.Radius = v);
            Setting("Сила", column, 0.05f, 4, builder.Strength, v => v.ToString("0.00", CultureInfo.InvariantCulture), v => builder.Strength = v);
            Setting("Мягкость края", column, 0, 1, builder.Softness, v => v.ToString("0.00", CultureInfo.InvariantCulture), v => builder.Softness = v);

            switch (builder.CurrentTool)
            {
                case WorldBuilder.Tool.Raise:
                    Note("Держите левую кнопку — земля растёт под кистью.", column, 40);
                    break;
                case WorldBuilder.Tool.Lower:
                    Note("Держите левую кнопку — земля опускается.", column, 40);
                    break;
                case WorldBuilder.Tool.Flatten:
                    feed.Flatten = UiKit.Label(FlattenText(builder), column, 15, UiKit.Text);
                    feed.Flatten.Size(24);
                    Buttons(column, ("−1 м", () => NudgeFlatten(builder, -1)), ("−0.1", () => NudgeFlatten(builder, -0.1f)),
                        ("+0.1", () => NudgeFlatten(builder, 0.1f)), ("+1 м", () => NudgeFlatten(builder, 1)));
                    Buttons(column, ("Где начну", () => builder.FlattenTo = float.NaN));
                    Note("Shift+щелчок по земле — взять её высоту. Потом ведите кистью — всё под ней встанет на эту высоту (площадка под дом).", column, 76);
                    break;
                case WorldBuilder.Tool.Smooth:
                    Note("Ведите кистью — неровности сглаживаются.", column, 40);
                    break;
                case WorldBuilder.Tool.Restore:
                    Note("Ведите кистью — земля (и её цвет) возвращается такой, какой была построена.", column, 58);
                    break;
                case WorldBuilder.Tool.Paint:
                    Heading("ЧЕМ КРАСИТЬ", column);
                    var layers = builder.PaintLayers;
                    var paints = UiKit.Rect("Paints", column);
                    var grid = paints.gameObject.AddComponent<GridLayoutGroup>();
                    grid.cellSize = new Vector2(146, 36);
                    grid.spacing = new Vector2(6, 6);
                    paints.Size(Mathf.CeilToInt(layers.Length / 2f) * 42);

                    foreach (var layer in layers)
                    {
                        UiKit.Button(WorldBuilder.LayerLabel(layer), paints, () =>
                        {
                            builder.PaintLayer = layer;
                            Show(Page.Builder);
                        }, builder.PaintLayer == layer ? ButtonKind.Tab : ButtonKind.Plain, 14);
                    }

                    break;
            }
        }

        static void NudgeFlatten(WorldBuilder builder, float metres)
        {
            if (float.IsNaN(builder.FlattenTo))
            {
                var at = builder.GroundAhead();
                builder.FlattenTo = at.y;
            }

            builder.FlattenTo += metres;
        }

        /// <summary>The mode's tools, two to a row, each with its number key.</summary>
        void ToolGrid(WorldBuilder builder, Transform column)
        {
            var tools = WorldBuilder.ToolsOf(builder.CurrentMode);
            var holder = UiKit.Rect("Tools", column);
            var grid = holder.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(146, 40);
            grid.spacing = new Vector2(6, 6);
            holder.Size(Mathf.CeilToInt(tools.Length / 2f) * 46);

            for (var i = 0; i < tools.Length; i++)
            {
                var tool = tools[i];
                UiKit.Button($"{i + 1} · {WorldBuilder.NameOf(tool)}", holder, () => builder.Choose(tool), builder.CurrentTool == tool ? ButtonKind.Tab : ButtonKind.Plain, 15);
            }
        }

        void WoodsTools(WorldBuilder builder, Transform column)
        {
            Heading("ЛЕС", column);
            ToolGrid(builder, column);
            Setting("Кисть", column, 1, 120, builder.Radius, v => $"{v:0} м", v => builder.Radius = v);

            if (builder.CurrentTool == WorldBuilder.Tool.Fell)
            {
                Note("Держите левую кнопку — деревья, кусты и камни под кистью вырубаются. Ctrl+Z — вернуть.", column, 58);

                return;
            }

            Setting("Не ближе друг к другу", column, 2, 40, builder.Spacing, v => $"{v:0} м", v => builder.Spacing = v);
            Setting("Размер", column, 0.4f, 2, builder.PlantSize, v => v.ToString("0.00", CultureInfo.InvariantCulture), v => builder.PlantSize = v);
            Heading("ЧТО САЖАТЬ", column);
            var kinds = builder.PlantKinds;
            var holder = UiKit.Rect("Kinds", column);
            var grid = holder.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(146, 36);
            grid.spacing = new Vector2(6, 6);
            holder.Size(Mathf.CeilToInt(kinds.Length / 2f) * 42);

            foreach (var kind in kinds)
            {
                UiKit.Button(WorldBuilder.KindLabel(kind), holder, () =>
                {
                    builder.PlantKind = kind;
                    Show(Page.Builder);
                }, builder.PlantKind == kind ? ButtonKind.Tab : ButtonKind.Plain, 14);
            }

            Note("Держите левую кнопку — сажается под кистью, не теснее, чем задано.", column, 40);
        }

        // ── On the right: the catalog ─────────────────────────────────

        void BuilderCatalog(WorldBuilder builder)
        {
            var catalog = BuildCatalog.Load();
            var right = UiKit.Panel("Builder Catalog", pageRoot);
            right.rectTransform.Place(new Vector2(1, 0), Vector2.one, -352, 104, 12, 82);
            var column = UiKit.Rect("Inside", right.transform).Fill();
            column.Stack<VerticalLayoutGroup>(6, new RectOffset(14, 14, 12, 12));
            Heading("КАТАЛОГ", column);

            var listHolder = (RectTransform)null;
            var content = (RectTransform)null;

            void Fill()
            {
                for (var i = content.childCount - 1; i >= 0; i--)
                {
                    Destroy(content.GetChild(i).gameObject);
                }

                var search = builderSearch.Trim().ToLowerInvariant();
                var entries = search.Length > 0
                    ? catalog.Entries.Where(e => e.Label.ToLowerInvariant().Contains(search) || e.Id.ToLowerInvariant().Contains(search))
                    : catalog.Entries.Where(e => e.Section == builderSection);

                foreach (var entry in entries)
                {
                    var chosen = builder.Chosen == entry && builder.CurrentTool != WorldBuilder.Tool.Select;
                    var label = entry.Stretch || entry.Repeat ? entry.Label + " (протянуть)" : entry.Label;
                    UiKit.Button(label, content, () => builder.Choose(entry.Stretch || entry.Repeat ? WorldBuilder.Tool.Stretch : WorldBuilder.Tool.Place, entry),
                        chosen ? ButtonKind.Tab : ButtonKind.Plain, 15).Size(34);
                }
            }

            UiKit.Field(column, builderSearch, "Поиск по названию…", 40, v =>
            {
                builderSearch = v;
                Fill();
            }).Size(40);

            if (builderSearch.Trim().Length == 0)
            {
                foreach (var row in BuilderSectionRows)
                {
                    UiKit.Choices(column, row, System.Array.IndexOf(row, builderSection), i =>
                    {
                        builderSection = row[i];
                        builderScroll = 1;
                        Show(Page.Builder);
                    }, 13);
                }
            }

            listHolder = UiKit.Rect("List", column);
            listHolder.Size(-1, -1, 1).flexibleHeight = 1;
            var scroll = listHolder.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            var viewport = UiKit.Rect("Viewport", listHolder).Fill();
            viewport.gameObject.AddComponent<RectMask2D>();
            UiKit.Image("Catch", viewport, new Color(0, 0, 0, 0.01f)).rectTransform.Fill();
            content = UiKit.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.Stack<VerticalLayoutGroup>(4);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            Fill();

            // The list where it was scrolled to (the page is made again at every choice).
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition = builderScroll;
            scroll.onValueChanged.AddListener(v => builderScroll = v.y);
        }

        // ── Along the bottom ───────────────────────────────────────────

        void BuilderBottom(WorldBuilder builder, BuilderFeed feed)
        {
            var bottom = UiKit.Panel("Builder Help", pageRoot);
            bottom.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 12, 12, 12, -92);
            var lines = UiKit.Rect("Inside", bottom.transform).Fill();
            lines.Stack<VerticalLayoutGroup>(1, new RectOffset(16, 16, 6, 6));
            feed.Help = UiKit.Label(builder.Help, lines, 14, UiKit.Muted);
            feed.Help.Size(22);
            feed.ViewHelp = UiKit.Label(builder.ViewHelp, lines, 14, UiKit.Muted);
            feed.ViewHelp.Size(22);

            var last = UiKit.Rect("Status", lines);
            last.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            last.Size(26);
            feed.Status = UiKit.Label(builder.Status, last, 16, UiKit.Text);
            feed.Status.Size(26, -1, 1);
            feed.Under = UiKit.Label(builder.Under, last, 15, UiKit.GoldLit, TextAnchor.MiddleRight);
            feed.Under.Size(26, 420);
        }

        // ── Resetting ──────────────────────────────────────────────────

        void BuilderResetPanel(WorldBuilder builder)
        {
            var panel = UiKit.Panel("Builder Reset", pageRoot, UiKit.Ink, UiKit.Gold);
            panel.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 470));
            var column = UiKit.Rect("Inside", panel.transform).Fill();
            column.Stack<VerticalLayoutGroup>(8, new RectOffset(24, 24, 18, 18));
            UiKit.Label("СБРОСИТЬ", column, 22, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold).Size(32);
            Note("Любой сброс можно отменить: Ctrl+Z или «Отменить» вверху.", column, 26);

            void Reset(string text, System.Action action)
            {
                UiKit.Button(text, column, () =>
                {
                    action();
                    builderReset = false;
                    Show(Page.Builder);
                }, ButtonKind.Danger, 16).Size(44);
            }

            Reset("Постройки деревни — на свои места", builder.ResetVillage);
            Reset("Убрать всё, что поставлено", builder.ResetPlaced);
            Reset("Землю — как была", builder.ResetGroundAll);
            Reset("Лес — как был", builder.ResetWoods);
            Reset("Всё сразу — как было", builder.ResetEverything);
            UiKit.Button("Закрыть", column, () =>
            {
                builderReset = false;
                Show(Page.Builder);
            }, ButtonKind.Plain, 16).Size(44);
        }
    }
}
