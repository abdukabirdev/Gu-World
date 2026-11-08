using System.Collections.Generic;
using GuWorld.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The map page: the mountain as the HUD's minimap draws it, whole, with its
    /// places and the hero; beside it the world of the atlas, with where Qing Mao
    /// Mountain lies in it, and what the atlas says of its region.
    ///
    /// The mountain's map zooms (the wheel, + and −, the buttons on it) toward the
    /// pointer and is dragged about with the left button. Closer in, the part in
    /// sight is drawn again finely — streets and roofs, as on the minimap — a few
    /// rows a frame, and laid over the whole map's picture once it is done. A grid
    /// of round distances lies over it, the coordinates under the pointer show in
    /// its corner, and a click (the button let go where it was pressed) puts a
    /// mark there: its coordinates, its height and how far it is show in the
    /// corner, with a button to go there at once. The mark stays (the minimap
    /// shows it too) until it is taken off.
    /// </summary>
    public partial class Menus
    {
        const float MapSize = 700;
        const float WorldSize = 180;
        /// <summary>The closest the map comes: about a metre and a half of the mountain to a point of the screen.</summary>
        const float MapZoomMax = 16;
        /// <summary>From this close the part in sight is drawn finely.</summary>
        const float MapDetailFrom = 2.5f;
        const int MapDetailSize = 1024;
        const int MapDetailRowsPerFrame = 96;
        /// <summary>A press that moves less than this (screen points) before it is let go is a click, not a drag.</summary>
        const float MapClickSlop = 6;
        const int MapGridLines = 24;

        // Kept between openings: the map opens where it was left.
        float mapZoom = 1;
        Vector2 mapCentre = new Vector2(0.5f, 0.5f);

        /// <summary>The mark put on the map (x and z in the world's metres, y its ground's height or NaN), or none.</summary>
        public Vector3? MapPin { get; private set; }

        RectTransform mapView;
        RectTransform mapContent;
        RawImage mapNearImage;
        RawImage mapDetailImage;
        RectTransform mapHero;
        RectTransform mapPinMark;
        RectTransform mapQuestMark;
        Text mapQuestLabel;
        RectTransform mapPinPanel;
        Text mapPinLine;
        Text mapPinDetail;
        Text mapCursorLine;
        Text mapScaleLine;
        RectTransform mapScaleBar;
        readonly List<(MapMark mark, RectTransform dot, Text label)> mapMarks = new List<(MapMark, RectTransform, Text)>();
        readonly List<RectTransform> mapGrid = new List<RectTransform>();
        readonly List<Text> mapGridLabels = new List<Text>();
        readonly List<RaycastResult> mapHits = new List<RaycastResult>();
        bool mapDragging;
        Vector2 mapDragFrom;
        Vector2 mapPressedAt;
        float mapPressMoved;
        float mapSettledAt;

        // The fine drawing of the part in sight (kept while the game runs; one at a time).
        LocalMap mapDetail;
        LocalMap.Painting mapPainting;

        void MapPage()
        {
            var body = BookFrame("map", "Карта", Page.Map);
            var map = host.Map;
            mapMarks.Clear();
            mapGrid.Clear();
            mapGridLabels.Clear();
            mapView = null;

            // The map: a square window onto the mountain at the left, in a frame with gold corners.
            var left = UiKit.Panel("Mountain", body);
            left.rectTransform.TopLeft(0, 0, MapSize + 8, MapSize + 8);
            var frame = UiKit.Rect("Frame", left.transform).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapSize, MapSize));

            if (map != null)
            {
                // The window onto the map (it catches the wheel and the drag), and the map inside it, as big as the zoom makes it.
                var under = UiKit.Image("View", frame, UiKit.Hex(0x04060A));
                under.rectTransform.Fill();
                under.raycastTarget = true;
                under.gameObject.AddComponent<RectMask2D>();
                mapView = under.rectTransform;
                mapContent = UiKit.Rect("Content", mapView);

                var picture = UiKit.Rect("Map", mapContent).gameObject.AddComponent<RawImage>();
                picture.texture = map.Texture;
                picture.rectTransform.Fill();
                picture.raycastTarget = false;
                picture.color = new Color(0.86f, 0.9f, 0.94f, 1);

                mapNearImage = UiKit.Rect("Near", mapContent).gameObject.AddComponent<RawImage>();
                mapNearImage.raycastTarget = false;
                mapDetailImage = UiKit.Rect("Detail", mapContent).gameObject.AddComponent<RawImage>();
                mapDetailImage.raycastTarget = false;

                // The grid (its lines on the map, their distances along the view's top and left edges).
                for (var i = 0; i < MapGridLines; i++)
                {
                    mapGrid.Add(UiKit.Image("Grid", mapContent, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f)).rectTransform);
                    var number = UiKit.Label("", mapView, 10, UiKit.Muted, TextAnchor.MiddleCenter).Shadowed();
                    number.rectTransform.sizeDelta = new Vector2(64, 16);
                    mapGridLabels.Add(number);
                }

                // The big places first (they keep their names where names would cover each other): diamonds, as the design's constellation.
                var marks = new List<MapMark>(map.Marks);
                marks.Sort((x, y) => y.Major.CompareTo(x.Major));

                foreach (var mark in marks)
                {
                    var dot = UiKit.Image("Mark", mapContent, mark.Major ? UiKit.Gold : UiKit.Text2, UiKit.Diamond);
                    dot.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (mark.Major ? 14 : 8));

                    if (mark.Major)
                    {
                        UiKit.HaloAt(dot.transform, UiKit.Gold.A(0.35f), Vector2.zero, 34);
                    }

                    var label = UiKit.Label(mark.Label, mapContent, mark.Major ? 14 : 11, mark.Major ? UiKit.Text : UiKit.Text2, TextAnchor.MiddleCenter).Shadowed();
                    label.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 20));
                    mapMarks.Add((mark, dot.rectTransform, label));
                }

                // Where the task leads: a gold diamond, breathing.
                mapQuestMark = UiKit.Image("Quest", mapContent, UiKit.Gold, UiKit.Diamond).rectTransform;
                mapQuestMark.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
                UiKit.HaloAt(mapQuestMark, UiKit.Gold.A(0.5f), Vector2.zero, 44);
                var breathe = mapQuestMark.gameObject.AddComponent<Pulse>();
                breathe.Period = 1.6f;
                breathe.Low = 0.6f;
                breathe.High = 1;
                breathe.Grow = 0.08f;
                mapQuestLabel = UiKit.Label("", mapQuestMark, 13, UiKit.GoldLit, TextAnchor.LowerCenter).Shadowed();
                mapQuestLabel.rectTransform.anchorMin = mapQuestLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
                mapQuestLabel.rectTransform.pivot = new Vector2(0.5f, 0);
                mapQuestLabel.rectTransform.anchoredPosition = new Vector2(0, 4);
                mapQuestLabel.rectTransform.sizeDelta = new Vector2(260, 20);

                // The mark: a pin, its point on the spot.
                mapPinMark = UiKit.Icon("pin", mapContent, 30, UiKit.Danger).rectTransform;
                mapPinMark.pivot = new Vector2(0.5f, 0.04f);
                var pinHole = UiKit.Image("Hole", mapPinMark, UiKit.Void, UiKit.Disc).rectTransform;
                pinHole.At(new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(8, 8));

                var hero = UiKit.Image("Hero", mapContent, UiKit.GoldLit, UiKit.Arrow);
                mapHero = hero.rectTransform;
                mapHero.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
                mapHero.localEulerAngles = new Vector3(0, 0, -host.HeroFacing * Mathf.Rad2Deg);
                UiKit.HaloAt(mapHero, UiKit.GoldLit.A(0.4f), Vector2.zero, 40).transform.SetAsFirstSibling();
                var you = UiKit.Lbl("Вы", mapHero, 10, UiKit.GoldLit, 0.2f, TextAnchor.MiddleCenter).Shadowed();
                you.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(40, 14));

                // The edges darken toward the frame.
                var edge = UiKit.Image("Vignette", frame, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Vignette);
                edge.rectTransform.Fill();

                // North, the zoom, the hero, the whole mountain: at the top left, as the design has them.
                var tools = UiKit.Rect("Tools", frame);
                tools.TopLeft(14, 14, 40, 200);
                tools.Stack<VerticalLayoutGroup>(6);
                MapButton(tools, "+", null, () => ZoomMap(mapZoom * 1.6f, Vector2.zero));
                MapButton(tools, "−", null, () => ZoomMap(mapZoom / 1.6f, Vector2.zero));
                MapButton(tools, null, "ring", CentreMapOnHero);
                MapButton(tools, null, "map", () => { mapZoom = 1; mapCentre = new Vector2(0.5f, 0.5f); LayOutMap(); });
                var north = UiKit.Image("North", frame, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.6f), UiKit.Disc);
                north.rectTransform.TopRight(14, 14, 40, 40);
                var northRing = UiKit.Image("Ring", north.transform, UiKit.Gold.A(0.45f), UiKit.RingLine(40));
                northRing.rectTransform.Fill();
                UiKit.Disp("С", north.transform, 15, UiKit.Gold, TextAnchor.MiddleCenter).rectTransform.Fill();

                // Where the pointer is, at the top right under the north.
                mapCursorLine = UiKit.Label("", frame, 12, UiKit.Text2, TextAnchor.MiddleRight).Shadowed();
                mapCursorLine.rectTransform.Place(new Vector2(0.5f, 1), Vector2.one, 0, -78, 16, 62);

                // The scale: a bracket of a round length, and what it is, at the bottom left.
                mapScaleBar = UiKit.Image("Scale Bar", frame, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.5f)).rectTransform;
                mapScaleBar.anchorMin = mapScaleBar.anchorMax = new Vector2(0, 0);
                mapScaleBar.pivot = new Vector2(0, 0.5f);
                mapScaleBar.anchoredPosition = new Vector2(16, 24);
                mapScaleLine = UiKit.Label("", frame, 11, UiKit.Muted, TextAnchor.MiddleLeft).Shadowed();
                mapScaleLine.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 16, 32, 60, -50);

                // The mark's corner (shown while there is one): where it is, going there.
                mapPinPanel = UiKit.Panel("Pin", frame).rectTransform;
                mapPinPanel.GetComponent<Image>().raycastTarget = true;
                mapPinPanel.Place(new Vector2(1, 0), new Vector2(1, 0), -330, 14, 14, -148);
                var pinIcon = UiKit.Icon("pin", mapPinPanel, 20, UiKit.Danger);
                pinIcon.rectTransform.At(new Vector2(0, 1), new Vector2(24, -24), new Vector2(20, 20));
                UiKit.Lbl("Метка", mapPinPanel).rectTransform.Place(new Vector2(0, 1), Vector2.one, 42, -32, 40, 16);
                mapPinLine = UiKit.Disp("", mapPinPanel, 20, UiKit.Text);
                mapPinLine.rectTransform.Place(new Vector2(0, 1), Vector2.one, 16, -62, 16, 36);
                mapPinDetail = UiKit.Label("", mapPinPanel, 12, UiKit.Muted);
                mapPinDetail.rectTransform.Place(new Vector2(0, 1), Vector2.one, 16, -84, 16, 64);
                var teleport = UiKit.HexButton("Телепорт", mapPinPanel, TeleportToPin, UiKit.HexKind.Main, 11, 40);
                ((RectTransform)teleport.transform).Place(Vector2.zero, new Vector2(1, 0), 12, 12, 104, -52);
                var centreOn = MapButton(mapPinPanel, null, "ring", CentreMapOnPin);
                centreOn.Place(new Vector2(1, 0), new Vector2(1, 0), -96, 12, 56, -52);
                var remove = MapButton(mapPinPanel, "×", null, () => SetMapPin(null));
                remove.Place(new Vector2(1, 0), new Vector2(1, 0), -50, 12, 10, -52);

                ShowMapPin();
                LayOutMap();
            }
            else
            {
                UiKit.Label("Карты этого места нет.", frame, 18, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Fill();
            }

            // The place, and the world of the atlas, at the right.
            var right = UiKit.Rect("Side", body);
            right.Place(Vector2.zero, Vector2.one, MapSize + 32, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(20);
            var place = Atlas.QingMao;
            var region = place.Region;

            var here = UiKit.Panel("Place", right);
            here.Size(-1).flexibleHeight = 1;
            var about = here.rectTransform.Stack<VerticalLayoutGroup>(10, new RectOffset(26, 26, 24, 22));
            about.childControlHeight = true;
            UiKit.Lbl(region.Name, here.transform).Size(14);
            UiKit.Disp(place.Name, here.transform, 34).Size(36);
            UiKit.Label(host.PlaceName, here.transform, 13, UiKit.Hex(0xA8B6B8)).Size(18);
            UiKit.Label($"Около {place.Width / 1000:0} км в поперечнике, вершина {place.Top:0} м, {place.Rock}. Ранги мастеров здесь — {place.RankMin}–{place.RankMax}. " +
                        "Три деревни делят одну гору и враждуют: Гу Юэ — на поясе горы, Сюн — на передней горе, Бай — у омута под водопадом на задней.",
                here.transform, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(92);
            var facts = UiKit.Rect("Facts", here.transform);
            facts.Stack<VerticalLayoutGroup>(0);
            Stat(facts, "Климат", region.Climate);
            Stat(facts, "Погода", string.Join(", ", region.Weather));
            Stat(facts, "Опасности", string.Join(", ", region.Hazards), UiKit.Hex(0xF0C868));

            var atlas = UiKit.Panel("Atlas", right);
            atlas.Size(WorldSize + 64);
            var row = atlas.rectTransform.Stack<HorizontalLayoutGroup>(20, new RectOffset(22, 22, 22, 22), TextAnchor.UpperLeft);
            row.childForceExpandWidth = false;
            var world = UiKit.Rect("World Map", atlas.transform);
            world.Size(WorldSize, WorldSize, 0);
            var texture = Atlas.WorldMap();

            if (texture != null)
            {
                var picture = UiKit.Rect("Atlas", world).gameObject.AddComponent<RawImage>();
                picture.texture = texture;
                picture.rectTransform.Fill();
                picture.raycastTarget = false;
                UiKit.Frame(world, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f));
                var at = (Atlas.ToMap(Atlas.QingMao.At) - new Vector2(0.5f, 0.5f)) * WorldSize;
                var gem = UiKit.Image("Here", world, UiKit.Gold, UiKit.Diamond);
                gem.rectTransform.At(new Vector2(0.5f, 0.5f), at, new Vector2(11, 11));
                UiKit.HaloAt(gem.transform, UiKit.Gold.A(0.5f), Vector2.zero, 32);
            }

            var atlasWords = UiKit.Rect("Words", atlas.transform);
            atlasWords.Stack<VerticalLayoutGroup>(8);
            atlasWords.Size(WorldSize, -1, 1);
            UiKit.Lbl($"{Atlas.WorldName} · атлас", atlasWords).Size(14);
            UiKit.Label($"Гора Цинмао лежит на Южной границе. {region.Climate}.", atlasWords, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(54);
            UiKit.Label("Играть можно только на горе Цинмао: на карте — вся гора, двадцать километров с запада на восток. Остальной мир атласа откроется в следующих арках.",
                atlasWords, 12, UiKit.Hex(0x6E7E81), TextAnchor.UpperLeft).Size(80);

            UiKit.Footer(bookStage, new[] { ("Колесо", "Масштаб"), ("ЛКМ", "Двигать · метка"), ("Q+E", "Страница"), ("Esc", "Закрыть") });
        }

        /// <summary>A small square button on the map (.zb): a thin edge, a sign or an icon.</summary>
        static RectTransform MapButton(Transform parent, string text, string icon, System.Action click)
        {
            var box = UiKit.Image("Button", parent, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Plain);
            box.raycastTarget = true;
            UiKit.Frame(box.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f));
            box.Size(40, 40, 0);
            Graphic mark;

            if (icon != null)
            {
                var image = UiKit.Icon(icon, box.transform, 20, UiKit.Text2);
                image.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
                mark = image;
            }
            else
            {
                var label = UiKit.Label(text, box.transform, 20, UiKit.Text2, TextAnchor.MiddleCenter);
                label.rectTransform.Fill();
                mark = label;
            }

            var clickable = box.gameObject.AddComponent<Clickable>();
            clickable.Click = click;
            clickable.Pointed = over => mark.color = over ? UiKit.GoldLit : UiKit.Text2;

            return box.rectTransform;
        }

        /// <summary>The map page's wheel, drag and keys, and the fine drawing of what is in sight (each frame while it is open).</summary>
        void UpdateMapPage()
        {
            var map = host.Map;

            if (mapView == null || map == null)
            {
                return;
            }

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (mouse != null)
            {
                var pointer = mouse.position.ReadValue();
                // On the map itself, not on a button or the mark's corner over it.
                var over = RectTransformUtility.RectangleContainsScreenPoint(mapView, pointer, null) && PointerOnMap(pointer);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(mapView, pointer, null, out var local);
                var scroll = mouse.scroll.ReadValue().y;

                if (over && Mathf.Abs(scroll) > 0.01f)
                {
                    ZoomMap(mapZoom * (scroll > 0 ? 1.25f : 0.8f), local);
                }

                if (over && mouse.leftButton.wasPressedThisFrame)
                {
                    mapDragging = true;
                    mapDragFrom = local;
                    mapPressedAt = pointer;
                    mapPressMoved = 0;
                }

                if (mapDragging)
                {
                    mapPressMoved = Mathf.Max(mapPressMoved, (pointer - mapPressedAt).magnitude);
                }

                // Let go where it was pressed: a click, the mark put there.
                if (mapDragging && mouse.leftButton.wasReleasedThisFrame && mapPressMoved < MapClickSlop)
                {
                    SetMapPin(MapPointAt(local));
                }

                if (!mouse.leftButton.isPressed)
                {
                    mapDragging = false;
                }

                if (mapDragging && mapPressMoved >= MapClickSlop && local != mapDragFrom)
                {
                    mapCentre -= (local - mapDragFrom) / (MapSize * mapZoom);
                    mapDragFrom = local;
                    LayOutMap();
                }
                else if (mapDragging)
                {
                    mapDragFrom = local;
                }

                if (mapCursorLine != null)
                {
                    var under = MapPointAt(local);
                    mapCursorLine.text = over ? $"<color=#7E9093>X</color> {under.x:0}   <color=#7E9093>Z</color> {under.z:0}" : "";
                }
            }

            if (keyboard != null)
            {
                if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
                {
                    ZoomMap(mapZoom * 1.6f, Vector2.zero);
                }
                else if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
                {
                    ZoomMap(mapZoom / 1.6f, Vector2.zero);
                }
            }

            DrawMapDetail(map);
        }

        /// <summary>Zooms to `zoom`, keeping the point of the map under `at` (the view's own units from its middle) where it is.</summary>
        void ZoomMap(float zoom, Vector2 at)
        {
            zoom = Mathf.Clamp(zoom, 1, MapZoomMax);
            var under = mapCentre + at / (MapSize * mapZoom);
            mapZoom = zoom;
            mapCentre = under - at / (MapSize * mapZoom);
            LayOutMap();
        }

        void CentreMapOnHero()
        {
            if (host.Map == null)
            {
                return;
            }

            mapZoom = Mathf.Max(mapZoom, 6);
            mapCentre = host.Map.ToMap(host.HeroAt);
            LayOutMap();
        }

        void CentreMapOnPin()
        {
            if (host.Map == null || !MapPin.HasValue)
            {
                return;
            }

            mapZoom = Mathf.Max(mapZoom, 4);
            mapCentre = host.Map.ToMap(MapPin.Value);
            LayOutMap();
        }

        /// <summary>The topmost thing of the menus under the pointer is the map's view (not a button or a panel over it).</summary>
        bool PointerOnMap(Vector2 pointer)
        {
            var events = EventSystem.current;

            if (events == null)
            {
                return true;
            }

            mapHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = pointer }, mapHits);

            return mapHits.Count > 0 && mapHits[0].gameObject == mapView.gameObject;
        }

        /// <summary>The world's point (x, z; y its ground's height where it is known, else NaN) under a point of the view (its own units from its middle).</summary>
        Vector3 MapPointAt(Vector2 local)
        {
            var map = host.Map;
            var uv = mapCentre + local / (MapSize * mapZoom);
            var x = map.Area.x + uv.x * map.Area.width;
            var z = map.Area.y + uv.y * map.Area.height;

            return new Vector3(x, QingMao.GroundAt(new Vector3(x, float.NaN, z)), z);
        }

        /// <summary>Puts the mark at a point (or takes it off, null) and shows it in its corner.</summary>
        void SetMapPin(Vector3? at)
        {
            MapPin = at;
            ShowMapPin();
            LayOutMap();
        }

        void ShowMapPin()
        {
            if (mapPinPanel == null)
            {
                return;
            }

            mapPinPanel.gameObject.SetActive(MapPin.HasValue);
            mapPinMark.gameObject.SetActive(MapPin.HasValue);

            if (!MapPin.HasValue)
            {
                return;
            }

            var pin = MapPin.Value;
            var hero = host.HeroAt;
            var distance = new Vector2(pin.x - hero.x, pin.z - hero.z).magnitude;
            mapPinLine.text = $"<color=#7E9093>X</color> {pin.x:0}   <color=#7E9093>Z</color> {pin.z:0}";
            mapPinDetail.text = (float.IsNaN(pin.y) ? "" : $"высота {pin.y:0} м · ") + $"от вас {Distance(distance)}";
        }

        /// <summary>"640 м", "2,4 км".</summary>
        static string Distance(float metres) => metres >= 1000 ? $"{metres / 1000:0.0} км" : $"{metres:0} м";

        /// <summary>Puts the mark at a point of the world (the checks' `-pin`).</summary>
        public void PinMap(float x, float z) => SetMapPin(new Vector3(x, QingMao.GroundAt(new Vector3(x, float.NaN, z)), z));

        public void TeleportToMapPin() => TeleportToPin();

        /// <summary>The hero put on the mark at once (on the ground there), the map closed.</summary>
        void TeleportToPin()
        {
            if (!MapPin.HasValue)
            {
                return;
            }

            var pin = MapPin.Value;
            host.Resume();
            host.Teleport(new Vector3(pin.x, float.NaN, pin.z));
            Notify($"Перенесены к метке: {pin.x:0}, {pin.z:0}");
        }

        /// <summary>Where a point of the map (0…1) falls in the content, from its middle.</summary>
        Vector2 OnMap(Vector2 uv) => (uv - new Vector2(0.5f, 0.5f)) * (MapSize * mapZoom);

        /// <summary>Puts the map, its finer drawings, the places and the hero where the zoom and the drag have them.</summary>
        void LayOutMap()
        {
            var map = host.Map;

            if (mapView == null || map == null)
            {
                return;
            }

            // Never past the map's edges.
            var half = 0.5f / mapZoom;
            mapCentre = new Vector2(Mathf.Clamp(mapCentre.x, half, 1 - half), Mathf.Clamp(mapCentre.y, half, 1 - half));
            var size = MapSize * mapZoom;
            mapContent.At(new Vector2(0.5f, 0.5f), -OnMap(mapCentre), new Vector2(size, size));
            mapSettledAt = Time.unscaledTime;

            PlaceOver(mapNearImage, host.NearMap, map, mapZoom >= 1.8f);
            PlaceOver(mapDetailImage, mapDetail, map, mapZoom >= MapDetailFrom);

            LayOutMapGrid(map, size);

            // The places: the big ones' names always, the others' where they fit (closer in, they all do); a name
            // at the view's edge is moved in to be read whole.
            var taken = new List<Rect>();
            var inView = OnMap(mapCentre);

            foreach (var (mark, dot, label) in mapMarks)
            {
                var at = OnMap(map.ToMap(mark.At));
                dot.anchoredPosition = at;
                var letters = label.fontSize;
                var width = mark.Label.Length * letters * 0.56f;
                var edge = MapSize * 0.5f - 6 - width * 0.5f;
                var x = Mathf.Abs(at.x - inView.x) < MapSize * 0.5f ? Mathf.Clamp(at.x, inView.x - edge, inView.x + edge) : at.x;
                label.rectTransform.anchoredPosition = new Vector2(x, at.y + 16);
                var box = new Rect(x - width * 0.5f, at.y + 6, width, letters + 6);
                var fits = !taken.Exists(r => r.Overlaps(box));
                label.gameObject.SetActive(fits);

                if (fits)
                {
                    taken.Add(box);
                }
            }

            if (mapQuestMark != null)
            {
                var quest = host.QuestTarget;
                mapQuestMark.gameObject.SetActive(quest.HasValue);

                if (quest.HasValue)
                {
                    mapQuestMark.anchoredPosition = OnMap(map.ToMap(quest.Value.at));
                    mapQuestLabel.text = "Задание: " + quest.Value.what;
                    mapQuestMark.SetAsLastSibling();
                }
            }

            if (mapPinMark != null && MapPin.HasValue)
            {
                mapPinMark.anchoredPosition = OnMap(map.ToMap(MapPin.Value));
                mapPinMark.SetAsLastSibling();
            }

            mapHero.anchoredPosition = OnMap(map.ToMap(host.HeroAt));
            mapHero.SetAsLastSibling();

            // A bar of a round length, about a sixth of the view.
            var metresPerUnit = map.Area.width / size;
            var want = metresPerUnit * MapSize / 6;
            var step = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(want)));
            var nice = want / step >= 5 ? 5 * step : want / step >= 2 ? 2 * step : step;
            mapScaleBar.sizeDelta = new Vector2(nice / metresPerUnit, 2);
            mapScaleLine.text = (nice >= 1000 ? $"{nice / 1000:0.#} км" : $"{nice:0} м") + $"   ·   ×{mapZoom:0.#}";
        }

        /// <summary>
        /// The grid: lines a round distance apart (some 140 points of the screen), across the part in sight, each
        /// with its coordinate at the view's top (x) or left (z) edge.
        /// </summary>
        void LayOutMapGrid(LocalMap map, float size)
        {
            var want = map.Area.width / size * 140;
            var step = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(want)));
            var spacing = want / step >= 5 ? 5 * step : want / step >= 2 ? 2 * step : step;
            var half = 0.5f / mapZoom;
            var inView = OnMap(mapCentre);
            var used = 0;

            for (var axis = 0; axis < 2; axis++)
            {
                var start = axis == 0 ? map.Area.x : map.Area.y;
                var span = axis == 0 ? map.Area.width : map.Area.height;
                var middle = axis == 0 ? mapCentre.x : mapCentre.y;
                var lo = start + (middle - half) * span;
                var hi = start + (middle + half) * span;

                for (var v = Mathf.Ceil(lo / spacing) * spacing; v <= hi && used < mapGrid.Count; v += spacing)
                {
                    var line = mapGrid[used];
                    var number = mapGridLabels[used];
                    used++;
                    line.gameObject.SetActive(true);
                    number.gameObject.SetActive(true);
                    var p = ((v - start) / span - 0.5f) * size;
                    number.text = spacing >= 1000 ? $"{v / 1000:0.#} км" : $"{v:0}";

                    if (axis == 0)
                    {
                        line.At(new Vector2(0.5f, 0.5f), new Vector2(p, 0), new Vector2(1, size));
                        number.rectTransform.At(new Vector2(0.5f, 1), new Vector2(p - inView.x, -46), new Vector2(64, 16));
                    }
                    else
                    {
                        line.At(new Vector2(0.5f, 0.5f), new Vector2(0, p), new Vector2(size, 1));
                        number.rectTransform.At(new Vector2(0, 0.5f), new Vector2(34, p - inView.y), new Vector2(64, 16));
                    }
                }
            }

            for (var i = used; i < mapGrid.Count; i++)
            {
                mapGrid[i].gameObject.SetActive(false);
                mapGridLabels[i].gameObject.SetActive(false);
            }
        }

        /// <summary>A finer map laid over the whole one where it lies (hidden when there is none, or not this close).</summary>
        void PlaceOver(RawImage image, LocalMap over, LocalMap map, bool shown)
        {
            image.enabled = shown && over != null && over.Texture != null;

            if (!image.enabled)
            {
                return;
            }

            image.texture = over.Texture;
            var min = OnMap(map.ToMap(new Vector3(over.Area.xMin, 0, over.Area.yMin)));
            var max = OnMap(map.ToMap(new Vector3(over.Area.xMax, 0, over.Area.yMax)));
            image.rectTransform.At(new Vector2(0.5f, 0.5f), (min + max) / 2, max - min);
        }

        /// <summary>
        /// Close in, once the view has stood still a moment: the part in sight (and some round it) drawn finely, unless
        /// what is drawn already covers it closely enough. Drawn a few rows a frame; the old one stays until it is done.
        /// </summary>
        void DrawMapDetail(LocalMap map)
        {
            if (mapPainting != null)
            {
                var done = mapPainting.Step(MapDetailRowsPerFrame);

                if (done != null)
                {
                    if (mapDetail != null)
                    {
                        Destroy(mapDetail.Texture);
                    }

                    mapDetail = done;
                    mapPainting = null;
                    LayOutMap();
                }

                return;
            }

            if (mapZoom < MapDetailFrom || Time.unscaledTime - mapSettledAt < 0.25f || WorldPlan.Current == null)
            {
                return;
            }

            // The ground in sight, in metres.
            var seen = map.Area.width / mapZoom;
            var middle = new Vector2(map.Area.x + mapCentre.x * map.Area.width, map.Area.y + mapCentre.y * map.Area.height);
            var sight = new Rect(middle - Vector2.one * seen / 2, Vector2.one * seen);

            bool Covers(LocalMap fine) => fine != null && fine.Area.Contains(sight.min) && fine.Area.Contains(sight.max) &&
                                          fine.Area.width / MapDetailSize <= seen / MapSize * 1.6f;

            if (Covers(mapDetail) || (mapZoom < 6 && host.NearMap != null && host.NearMap.Area.Contains(sight.min) && host.NearMap.Area.Contains(sight.max)))
            {
                return;
            }

            // Half again round what is seen, so that a little dragging does not need it drawn again.
            var span = seen * 1.5f;
            var area = new Rect(middle - Vector2.one * span / 2, Vector2.one * span);
            mapPainting = new LocalMap.Painting(area, MapDetailSize, WorldPlan.Current, true);
        }
    }
}
