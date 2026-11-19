using System;
using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The map (M, ROADMAP 3.2, after UI-Map.dc.html): the mountain under the fog of what the hero has not
    /// seen yet, zoomed by the wheel (or + −) about the pointer, dragged by the left button (or the arrows),
    /// the land drawn again in finer detail where it is looked at closely. The places known, the lanterns
    /// lit, where the quests lead, the hero, and the player's own marks (right click puts one, or takes it
    /// away). A click picks what is under it: its card says what it is, how far, how high, what can be done.
    /// </summary>
    public partial class Ui
    {
        /// <summary>Where the map opens next (the journal's «Показать на карте»), or null: on the hero.</summary>
        MapPoint? mapFocus;
        /// <summary>The map's zoom and middle as the player left them (kept while the game runs).</summary>
        float mapZoom = 2.5f;
        Vector2? mapCentre;
        /// <summary>What the map's card shows: a kind, an id or a mark, and the point.</summary>
        (string kind, string id, int mark, MapPoint at)? mapPick;

        const float MapSide = 700;
        Texture2D fogTexture;

        void MapPage()
        {
            var session = S;
            var hero = Game.Hero;
            var heroAt = hero != null ? new MapPoint(hero.transform.position.x, hero.transform.position.z) : new MapPoint(0, 0);
            var here = Atlas.PlaceAt(heroAt);
            var body = Frame("图", "Карта", here != null ? here.Name : "Гора Цинмао · Южная граница", true, null, null,
                new[] { ("Колесо", "Масштаб"), ("ЛКМ", "Двигать · выбрать"), ("ПКМ", "Метка"), ("C", "К герою"), ("Q E", "Страницы"), ("Esc", "Закрыть") });

            var frame = UiKit.Panel("Map", body, UiKit.Ink, UiKit.Gold.A(0.25f));
            frame.rectTransform.At(new Vector2(0, 0.5f), new Vector2(MapSide * 0.5f + 20, 0), new Vector2(MapSide + 16, MapSide + 16));
            var viewRect = UiKit.Rect("View", frame.transform);
            viewRect.Fill(8);
            viewRect.gameObject.AddComponent<RectMask2D>();
            var catcher = viewRect.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);
            catcher.raycastTarget = true;

            var content = UiKit.Rect("Mountain", viewRect);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(MapSide, MapSide);
            var picture = UiKit.Rect("Land", content).gameObject.AddComponent<RawImage>();
            picture.texture = MapTexture();
            picture.rectTransform.Fill();
            picture.raycastTarget = false;
            var detail = UiKit.Rect("Detail", content).gameObject.AddComponent<RawImage>();
            detail.raycastTarget = false;
            detail.enabled = false;
            var fog = UiKit.Rect("Fog", content).gameObject.AddComponent<RawImage>();
            if (fogTexture != null)
            {
                Destroy(fogTexture);
            }

            fogTexture = FogTexture(session);
            fog.texture = fogTexture;
            fog.rectTransform.Fill();
            fog.raycastTarget = false;
            var marks = UiKit.Rect("Marks", content);
            marks.Fill();

            var view = viewRect.gameObject.AddComponent<MapView>();
            view.Content = content;
            view.Detail = detail;
            view.Side = MapSide;
            view.Zoom = mapZoom;
            // An asked-for point (the journal's), else where the map was left, else the hero.
            var focus = mapFocus;
            mapFocus = null;
            view.Centre = focus != null ? Normalised(focus.Value) : mapCentre ?? Normalised(heroAt);
            view.Moved = () => { mapZoom = view.Zoom; mapCentre = view.Centre; };
            view.HeroAt = () => Game.Hero != null ? Normalised(new MapPoint(Game.Hero.transform.position.x, Game.Hero.transform.position.z)) : new Vector2(0.5f, 0.5f);

            // What can be picked on the map, by its place (0–1 across the mountain).
            var picks = new List<(Vector2 at, string kind, string id, int mark)>();

            RectTransform Upright(string name, Vector2 at)
            {
                var holder = UiKit.Rect(name, marks);
                holder.anchorMin = holder.anchorMax = at;
                holder.anchoredPosition = Vector2.zero;
                holder.sizeDelta = Vector2.zero;
                view.Upright.Add(holder);

                return holder;
            }

            Text Label(RectTransform holder, string words, int size, Color colour, float below)
            {
                var label = UiKit.Lbl(words, holder, size, colour, 0.1f, TextAnchor.MiddleCenter).Shadowed();
                label.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, below), new Vector2(240, size + 6));

                return label;
            }

            // The valley's places (the small world) or the mountain's settlements and places the hero knows.
            foreach (var place in Valley.Places)
            {
                if (place.Id == "yard" || place.Id == "forest_edge" || (place.Cave && !session.Has(place.Id == "hermit_cave" ? "hermit_open" : "blood_open")) || (Ground.Big && place.Radius < 30))
                {
                    continue;
                }

                var at = Normalised(place.At);
                Label(Upright("Place", at), place.Name, 10, UiKit.Hex(place.Colour), 12);
                picks.Add((at, "place", place.Id, -1));
            }

            if (Ground.Big)
            {
                foreach (var settlement in Regions.Settlements)
                {
                    if (!session.MapRevealed && !session.InCodex("place:" + settlement.Id))
                    {
                        continue;
                    }

                    var at = Normalised(settlement.At);
                    var holder = Upright("Settlement", at);
                    UiKit.Image("Dot", holder, UiKit.Hex(settlement.Colour), UiKit.Diamond).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9, 9));
                    Label(holder, settlement.Name, settlement.City ? 12 : 10, UiKit.Hex(settlement.Colour), 12);
                    picks.Add((at, "settlement", settlement.Id, -1));
                }

                foreach (var place in Regions.Places)
                {
                    if (!session.MapRevealed && !session.InCodex("place:" + place.Id))
                    {
                        continue;
                    }

                    var at = Normalised(place.At);
                    Label(Upright("Place", at), place.Name, 9, UiKit.Hex(place.Colour).A(0.85f), 10);
                    picks.Add((at, "place", place.Id, -1));
                }
            }

            foreach (var thing in Atlas.Things)
            {
                if (thing.Kind == ThingKind.Lantern && (Atlas.InValley(thing) || session.Lit(thing.Id)))
                {
                    var at = Normalised(thing.At);
                    var colour = thing.Id == session.State.Lantern ? UiKit.Moon : session.Lit(thing.Id) ? UiKit.Moon.A(0.8f) : UiKit.Moon.A(0.3f);
                    UiKit.Image("Lantern", Upright("Lantern", at), colour, UiKit.Disc).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8));
                    picks.Add((at, "lantern", thing.Id, -1));
                }
            }

            // The side quests taken: small rings where their steps lead; the main quest's gold ring over everything.
            foreach (var taken in session.ActiveQuests())
            {
                if (taken != session.MainQuest && session.MarkerOf(taken, out var point))
                {
                    var at = Normalised(point);
                    UiKit.Image("Side", Upright("Side quest", at), UiKit.Essence.A(0.85f), UiKit.Ring).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(15, 15));
                    picks.Add((at, "quest", taken.Quest, -1));
                }
            }

            // The player's marks: gold pins.
            for (var i = 0; i < session.State.Marks.Count; i++)
            {
                var mark = session.State.Marks[i];
                var at = Normalised(new MapPoint(mark.X, mark.Z));
                var holder = Upright("Mark", at);
                var pin = UiKit.Image("Pin", holder, UiKit.GoldLit, UiKit.Diamond);
                pin.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 7), new Vector2(10, 14));
                UiKit.Image("Foot", holder, UiKit.GoldLit.A(0.6f), UiKit.Disc).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4));

                if (!string.IsNullOrEmpty(mark.Name))
                {
                    Label(holder, mark.Name, 10, UiKit.GoldLit, 20);
                }

                picks.Add((at, "mark", null, i));
            }

            var quest = session.MainQuest;

            if (quest != null && session.MarkerOf(quest, out var goal))
            {
                var at = Normalised(goal);
                var holder = Upright("Quest", at);
                var ring = UiKit.Image("Ring", holder, UiKit.Gold, UiKit.Ring);
                ring.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
                ring.gameObject.AddComponent<Pulse>();
                UiKit.Image("Gem", holder, UiKit.GoldLit, UiKit.Diamond).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
                picks.Add((at, "quest", quest.Quest, -1));
            }

            if (hero != null)
            {
                var me = Upright("Hero", Normalised(heroAt));
                var arrow = UiKit.Image("Hero", me, UiKit.Essence, UiKit.Arrow);
                arrow.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
                // The map's north is up: the hero's arrow turns as they face (the arrow points up at yaw 0, north).
                arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -hero.transform.eulerAngles.y);
                UiKit.HaloAt(me, UiKit.Essence.A(0.6f), Vector2.zero, 42);
            }

            // The selection ring (moved onto what is picked).
            var picked = Upright("Picked", new Vector2(-1, -1));
            UiKit.Image("Ring", picked, UiKit.GoldLit.A(0.9f), UiKit.RingLine(30)).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));

            view.Picked = at =>
            {
                // The nearest thing within a finger's breadth on screen; or the point itself.
                var reach = 16 / (MapSide * view.Zoom);
                var best = -1;
                var bestDistance = reach * reach;

                for (var i = 0; i < picks.Count; i++)
                {
                    var d = (picks[i].at - at).sqrMagnitude;

                    if (d < bestDistance)
                    {
                        best = i;
                        bestDistance = d;
                    }
                }

                var world = Unnormalised(best >= 0 ? picks[best].at : at);
                mapPick = best >= 0 ? (picks[best].kind, picks[best].id, picks[best].mark, world) : ("point", null, -1, world);
                Redraw();
            };

            view.Marked = at =>
            {
                var world = Unnormalised(at);
                var near = session.MarkNear(world.X, world.Z, 18 * Ground.Half * 2 / (MapSide * view.Zoom));

                if (near >= 0)
                {
                    session.RemoveMark(near);
                    mapPick = null;
                }
                else
                {
                    var index = session.AddMark(world.X, world.Z);
                    mapPick = ("mark", null, index, world);
                }

                Sound.AudioDirector.Play("hover", null, 0.5f);
                Redraw();
            };

            // The controls on the map's corner: nearer, farther, back to the hero; north; the scale.
            var tools = UiKit.Rect("Tools", frame.transform);
            tools.TopRight(16, 16, 40, 140);
            tools.Stack<VerticalLayoutGroup>(6);
            UiKit.HexButton("+", tools, () => view.ZoomBy(1.6f), UiKit.HexKind.Ghost, 16, 40).Size(40, 40, 0);
            UiKit.HexButton("−", tools, () => view.ZoomBy(1 / 1.6f), UiKit.HexKind.Ghost, 16, 40).Size(40, 40, 0);
            UiKit.HexButton("◎", tools, view.ToHero, UiKit.HexKind.Ghost, 14, 40).Size(40, 40, 0);
            var north = UiKit.Lbl("С ↑", frame.transform, 12, UiKit.LabelGold, 0.2f, TextAnchor.MiddleCenter).Shadowed();
            north.rectTransform.TopLeft(18, 16, 50, 20);
            var scale = UiKit.Rect("Scale", frame.transform);
            scale.BottomLeft(24, 22, 200, 30);
            var bar = UiKit.Image("Bar", scale, UiKit.Text2, UiKit.Plain);
            var barWords = UiKit.Lbl("", scale, 11, UiKit.Text2, 0.1f).Shadowed();
            view.Scale = (bar, barWords);

            if (mapPick != null)
            {
                picked.anchorMin = picked.anchorMax = Normalised(mapPick.Value.at);
            }

            MapCard(body, session, heroAt);
        }

        static Vector2 Normalised(MapPoint point) => new Vector2((point.X + Ground.Half) / (Ground.Half * 2), (point.Z + Ground.Half) / (Ground.Half * 2));

        static MapPoint Unnormalised(Vector2 at) => new MapPoint(at.x * Ground.Half * 2 - Ground.Half, at.y * Ground.Half * 2 - Ground.Half);

        /// <summary>The fog: a texel a cell of the hero's sight record — dark ink where nothing is seen, clear where it is; its edge softened.</summary>
        static Texture2D FogTexture(Session session)
        {
            var cells = Ground.Big ? Session.ExploreCells : 1;
            var texture = new Texture2D(cells, cells, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Fog", hideFlags = HideFlags.DontSave };
            var pixels = new Color32[cells * cells];

            // The small valley has no fog: all of it is home.
            if (!Ground.Big || session.MapRevealed)
            {
                texture.SetPixels32(new Color32[cells * cells]);
                texture.Apply(false, true);

                return texture;
            }

            for (var row = 0; row < cells; row++)
            {
                for (var column = 0; column < cells; column++)
                {
                    var seen = 0;

                    for (var dz = -1; dz <= 1; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            seen += session.ExploredCell(column + dx, row + dz) ? 1 : 0;
                        }
                    }

                    var own = session.ExploredCell(column, row);
                    var dark = own ? (9 - seen) / 9f * 0.45f : 1 - seen / 9f * 0.4f;
                    pixels[row * cells + column] = new Color32(7, 9, 13, (byte)(Mathf.Clamp01(dark) * 238));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return texture;
        }

        /// <summary>The right of the map: what is picked (or where the hero is), what can be done there, the legend and the mountain in the world.</summary>
        void MapCard(RectTransform body, Session session, MapPoint heroAt)
        {
            var right = UiKit.Rect("Card", body);
            right.Place(new Vector2(0, 0), new Vector2(1, 1), MapSide + 70, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(8);

            var (kind, title, about, at) = Describe(session, heroAt);
            var distance = at.DistanceTo(heroAt);
            UiKit.Lbl(kind, right, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp(title, right, 28, UiKit.Text).Size(34);
            var height = Ground.HeightAt(at.X, at.Z);
            UiKit.Label(distance < 15 ? $"вы здесь · высота {height:0} м" : $"{(distance >= 1000 ? $"{distance / 1000:0.0} км" : $"{distance:0} м")} от вас · высота {height:0} м", right, 14, UiKit.Text2).Size(20);

            if (!string.IsNullOrEmpty(about))
            {
                Para(about, right, 14, UiKit.Muted);
            }

            var actions = UiKit.Rect("Actions", right);
            actions.Size(44);
            var row = actions.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            var pick = mapPick;

            if (pick != null && pick.Value.kind == "mark")
            {
                var index = pick.Value.mark;
                UiKit.HexButton("Убрать метку", actions, () => { session.RemoveMark(index); mapPick = null; Redraw(); }, UiKit.HexKind.Ghost, 12, 42).Size(42, 190, 0);
            }
            else if (pick != null)
            {
                var point = pick.Value.at;
                UiKit.HexButton("Поставить метку", actions, () =>
                {
                    var index = session.AddMark(point.X, point.Z, pick.Value.kind == "point" ? "" : title);
                    mapPick = ("mark", null, index, point);
                    Redraw();
                }, UiKit.HexKind.Ghost, 12, 42).Size(42, 210, 0);
            }

            if (pick != null && pick.Value.kind == "lantern" && session.Lit(pick.Value.id) && session.State.Lanterns.Count > 1 && !session.InSandbox)
            {
                UiKit.HexButton("К фонарю", actions, () => Show(Page.Travel), UiKit.HexKind.Ghost, 12, 42).Size(42, 160, 0);
            }

            if (pick != null)
            {
                UiKit.HexButton("Снять выбор", actions, () => { mapPick = null; Redraw(); }, UiKit.HexKind.Ghost, 11, 42).Size(42, 170, 0);
            }

            Gap(right, 6);
            UiKit.Rule(right);

            var quest = session.MainQuest;

            if (quest != null)
            {
                UiKit.Lbl("Задание", right, 11, UiKit.LabelGold).Size(16);
                UiKit.Disp(Story.Get(quest.Quest).Title, right, 20, UiKit.GoldText).Size(26);
                Para(session.StepText(quest), right, 14);
            }

            Gap(right, 4);
            Fact(right, "Золотое кольцо · бирюзовые", "задание · поручения", UiKit.GoldText);
            Fact(right, "Бирюзовая стрелка", "вы и куда смотрите", UiKit.Essence);
            Fact(right, "Голубые точки", "лунные фонари", UiKit.Moon);
            Fact(right, "Золотые булавки", $"ваши метки · {session.State.Marks.Count} из {Session.MostMarks}", UiKit.GoldLit);

            if (Ground.Big)
            {
                Fact(right, "Увидено", session.MapRevealed ? "вся гора (благословение вершины)" : $"{session.ExploredShare * 100:0.0}% горы", UiKit.Text2);
            }

            var sky = Weather.At(session.State.Day, session.State.Hour, Game.Hero != null ? Game.Hero.transform.position.y : 0);
            Fact(right, "День · погода", $"{session.State.Day} · {(int)session.State.Hour}:{(int)(session.State.Hour % 1 * 60):00} · {Weather.Name(sky).ToLowerInvariant()}");

            Gap(right, 6);
            UiKit.Lbl("По атласу мира", right, 11, UiKit.LabelGold).Size(16);
            Para("Гора Цинмао лежит на Южной границе; ранги мастеров здесь — первый–третий. Северные равнины, Западная пустыня и Восточное море откроются в следующих арках.", right, 13, UiKit.Muted);
        }

        /// <summary>What the map's card says about what is picked (nothing picked: where the hero is).</summary>
        (string kind, string title, string about, MapPoint at) Describe(Session session, MapPoint heroAt)
        {
            if (mapPick == null)
            {
                var place = Atlas.PlaceAt(heroAt);
                var settlement = Atlas.SettlementAt(heroAt);

                return ("Вы здесь", settlement?.Name ?? place?.Name ?? "Склоны Цинмао", "Колесо — ближе и дальше, левая кнопка — тянуть карту или выбрать, правая — поставить или снять метку.", heroAt);
            }

            var (kind, id, mark, at) = mapPick.Value;

            switch (kind)
            {
                case "settlement":
                {
                    var settlement = Regions.Settlement(id);
                    var clan = Clans.Get(settlement?.Clan ?? "")?.Name;
                    var what = settlement == null ? "" : settlement.City ? "город" : settlement.Style == SettlementStyle.Camp ? "лагерь" : settlement.Style == SettlementStyle.Mortal ? "хутор" : "деревня";

                    return (clan != null ? $"{what} · {clan}".ToUpperInvariant() : what.ToUpperInvariant(), settlement?.Name ?? "", null, at);
                }
                case "place":
                {
                    var place = Atlas.Place(id);

                    return (place != null && place.Cave ? "ПЕЩЕРА" : "МЕСТО", place?.Name ?? "", null, at);
                }
                case "lantern":
                {
                    var here = session.State.Lantern == id;

                    return ("ЛУННЫЙ ФОНАРЬ", here ? "Ваш фонарь" : session.Lit(id) ? "Зажжённый фонарь" : "Фонарь", here ? "Здесь вы очнётесь, если падёте." : session.Lit(id) ? "Между зажжёнными фонарями можно переходить." : "Коснитесь его, чтобы зажечь.", at);
                }
                case "quest":
                {
                    var state = session.StateOf(id);
                    var def = Story.Get(id);

                    return (def != null && def.Main ? "ЦЕЛЬ ЗАДАНИЯ" : "ПОРУЧЕНИЕ", def?.Title ?? "", state != null ? session.StepText(state) : null, at);
                }
                case "mark":
                {
                    var marks = session.State.Marks;
                    var name = mark >= 0 && mark < marks.Count && !string.IsNullOrEmpty(marks[mark].Name) ? marks[mark].Name : "Ваша метка";

                    return ("МЕТКА", name, "Видна на карте, пока вы её не уберёте (правая кнопка по ней).", at);
                }
                default:
                    return ("МЕСТО НА КАРТЕ", session.IsExplored(at.X, at.Z) || !Ground.Big ? "Склон горы" : "Неизведанное", session.IsExplored(at.X, at.Z) || !Ground.Big ? null : "Здесь вы ещё не бывали.", at);
            }
        }
    }

    /// <summary>
    /// The map's view: the mountain at a zoom about a middle, moved by the wheel (about the pointer), a drag,
    /// the keys; what stands on it (marks, names) kept upright and of one size; the land drawn again finer
    /// where the view rests close in.
    /// </summary>
    public class MapView : MonoBehaviour
    {
        public RectTransform Content;
        public RawImage Detail;
        public float Side = 700;
        public float Zoom = 2;
        /// <summary>The view's middle on the mountain, 0–1 across and up.</summary>
        public Vector2 Centre = new Vector2(0.5f, 0.5f);
        public readonly List<RectTransform> Upright = new List<RectTransform>();
        public Action<Vector2> Picked;
        public Action<Vector2> Marked;
        public Action Moved;
        public Func<Vector2> HeroAt;
        public (Image bar, Text words) Scale;

        public const float Least = 1, Most = 14;

        RectTransform rect;
        bool dragging;
        Vector2 pressedAt, lastPointer;
        float still;
        bool detailFresh;
        Texture2D detailTexture;
        Color[] detailPixels;
        int detailRow = -1;
        Rect detailArea;
        float[] weights;

        void Start()
        {
            rect = (RectTransform)transform;
            weights = new float[Core.Gen.Surface.Layers];
            Apply();
        }

        void OnDestroy()
        {
            if (detailTexture != null)
            {
                Destroy(detailTexture);
            }
        }

        public void ZoomBy(float factor) => ZoomAt(factor, Centre);

        public void ToHero()
        {
            if (HeroAt != null)
            {
                Centre = HeroAt();
                Apply();
            }
        }

        /// <summary>Zooms keeping the point `at` (0–1 on the mountain) where it is in the view.</summary>
        void ZoomAt(float factor, Vector2 at)
        {
            var zoom = Mathf.Clamp(Zoom * factor, Least, Most);
            // The point under the pointer stays under it: the middle moves toward it by what the zoom took.
            Centre = at + (Centre - at) * (Zoom / zoom);
            Zoom = zoom;
            Apply();
        }

        /// <summary>A point of the screen on the mountain (0–1), and whether it is inside the view.</summary>
        bool OnMountain(Vector2 screen, out Vector2 at)
        {
            at = default;

            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null))
            {
                return false;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(Content, screen, null, out var local);
            at = local / Side + new Vector2(0.5f, 0.5f);

            return true;
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            var moved = false;

            if (mouse != null)
            {
                var pointer = mouse.position.ReadValue();
                var inside = OnMountain(pointer, out var at);
                var wheel = mouse.scroll.ReadValue().y;

                if (inside && Mathf.Abs(wheel) > 0.01f)
                {
                    ZoomAt(wheel > 0 ? 1.25f : 0.8f, at);
                    moved = true;
                }

                if (mouse.leftButton.wasPressedThisFrame && inside)
                {
                    dragging = true;
                    pressedAt = lastPointer = pointer;
                }

                if (dragging && mouse.leftButton.isPressed)
                {
                    var delta = pointer - lastPointer;
                    lastPointer = pointer;

                    if ((pointer - pressedAt).sqrMagnitude > 25 && delta.sqrMagnitude > 0)
                    {
                        // Screen pixels to the mountain's share: through the content's own scale on screen.
                        Content.GetWorldCorners(corners);
                        var across = Mathf.Max(1, corners[2].x - corners[0].x);
                        Centre -= delta / across;
                        Apply();
                        moved = true;
                    }
                }

                if (dragging && mouse.leftButton.wasReleasedThisFrame)
                {
                    dragging = false;

                    // A click, not a drag: pick what is there.
                    if ((pointer - pressedAt).sqrMagnitude <= 25 && OnMountain(pointer, out var picked))
                    {
                        Picked?.Invoke(picked);

                        return;
                    }
                }

                if (mouse.rightButton.wasPressedThisFrame && inside)
                {
                    Marked?.Invoke(at);

                    return;
                }
            }

            if (keyboard != null)
            {
                var step = 0.12f / Zoom;
                var pan = Vector2.zero;

                if (keyboard.leftArrowKey.isPressed || keyboard[Key.A].isPressed) { pan.x -= 1; }
                if (keyboard.rightArrowKey.isPressed || keyboard[Key.D].isPressed) { pan.x += 1; }
                if (keyboard.upArrowKey.isPressed || keyboard[Key.W].isPressed) { pan.y += 1; }
                if (keyboard.downArrowKey.isPressed || keyboard[Key.S].isPressed) { pan.y -= 1; }

                if (pan != Vector2.zero)
                {
                    Centre += pan * step * Time.unscaledDeltaTime * 6;
                    Apply();
                    moved = true;
                }

                if (keyboard[Key.Equals].wasPressedThisFrame || keyboard[Key.NumpadPlus].wasPressedThisFrame)
                {
                    ZoomBy(1.6f);
                    moved = true;
                }

                if (keyboard[Key.Minus].wasPressedThisFrame || keyboard[Key.NumpadMinus].wasPressedThisFrame)
                {
                    ZoomBy(1 / 1.6f);
                    moved = true;
                }

                if (keyboard[Key.C].wasPressedThisFrame || keyboard[Key.Home].wasPressedThisFrame)
                {
                    ToHero();
                    moved = true;
                }
            }

            if (moved)
            {
                still = 0;
                detailFresh = false;
                detailRow = -1;
                Detail.enabled = false;
            }
            else
            {
                still += Time.unscaledDeltaTime;
            }

            PaintDetail();
        }

        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>The content placed for the zoom and the middle; what stands on it kept upright; the scale's bar.</summary>
        void Apply()
        {
            if (Content == null)
            {
                return;
            }

            var half = 0.5f / Zoom;
            Centre = new Vector2(Mathf.Clamp(Centre.x, half, 1 - half), Mathf.Clamp(Centre.y, half, 1 - half));
            Content.localScale = Vector3.one * Zoom;
            Content.anchoredPosition = (new Vector2(0.5f, 0.5f) - Centre) * Side * Zoom;

            foreach (var upright in Upright)
            {
                if (upright != null)
                {
                    upright.localScale = Vector3.one / Zoom;
                }
            }

            // The scale: the roundest distance that fits in 160 units.
            if (Scale.bar != null)
            {
                var metresPerUnit = Ground.Half * 2 / (Side * Zoom);
                var steps = new[] { 25f, 50, 100, 200, 500, 1000, 2000, 5000 };
                var metres = steps[0];

                foreach (var candidate in steps)
                {
                    if (candidate / metresPerUnit <= 160)
                    {
                        metres = candidate;
                    }
                }

                var length = metres / metresPerUnit;
                Scale.bar.rectTransform.At(new Vector2(0, 0), new Vector2(length * 0.5f, 3), new Vector2(length, 2));
                Scale.words.text = metres >= 1000 ? $"{metres / 1000:0} км" : $"{metres:0} м";
                Scale.words.rectTransform.At(new Vector2(0, 0), new Vector2(length + 40, 6), new Vector2(70, 16));
            }

            Moved?.Invoke();
        }

        /// <summary>
        /// Close in and still for a moment: the land in view drawn again at a finer step (a few rows a frame, a
        /// couple of milliseconds at most), laid over the coarse picture.
        /// </summary>
        void PaintDetail()
        {
            if (!Ground.Big && Zoom < 2.5f || Zoom < 2.2f || still < 0.25f || detailFresh)
            {
                return;
            }

            const int size = 192;

            if (detailRow < 0)
            {
                var half = 0.5f / Zoom;
                detailArea = Rect.MinMaxRect(Mathf.Clamp01(Centre.x - half), Mathf.Clamp01(Centre.y - half), Mathf.Clamp01(Centre.x + half), Mathf.Clamp01(Centre.y + half));
                detailPixels = detailPixels ?? new Color[size * size];
                detailRow = 0;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var span = Ground.Half * 2;

            while (detailRow < size && watch.Elapsed.TotalMilliseconds < 2.5)
            {
                var z = -Ground.Half + span * Mathf.Lerp(detailArea.yMin, detailArea.yMax, (detailRow + 0.5f) / size);

                for (var i = 0; i < size; i++)
                {
                    var x = -Ground.Half + span * Mathf.Lerp(detailArea.xMin, detailArea.xMax, (i + 0.5f) / size);
                    detailPixels[detailRow * size + i] = Ui.MapColour(x, z, span * detailArea.width / size, weights);
                }

                detailRow++;
            }

            if (detailRow < size)
            {
                return;
            }

            if (detailTexture == null)
            {
                detailTexture = new Texture2D(size, size, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, name = "Map detail", hideFlags = HideFlags.DontSave };
            }

            detailTexture.SetPixels(detailPixels);
            detailTexture.Apply(false);
            Detail.texture = detailTexture;
            Detail.rectTransform.anchorMin = detailArea.min;
            Detail.rectTransform.anchorMax = detailArea.max;
            Detail.rectTransform.offsetMin = Detail.rectTransform.offsetMax = Vector2.zero;
            Detail.enabled = true;
            detailFresh = true;
        }
    }
}
