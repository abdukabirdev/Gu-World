using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.World;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// Finding one's way on the HUD (ROADMAP 3.1; the author: both, "cool, clear and not in the way").
    /// The compass: a thin strip at the top, half the horizon wide, turning with the camera — the
    /// quarters as letters, a tick every 15°, the task's gold diamond with how far, the lanterns lit
    /// and the villages known; everything fades toward the strip's ends. The minimap: a dark disc at
    /// the bottom right, the land round the hero painted as the map paints it (a tile rebuilt a few
    /// rows a frame as the hero walks on), turning with the camera, the hero's arrow in the middle,
    /// the task, lanterns and enemies near; the task held to the rim when it is farther.
    /// </summary>
    public partial class Ui
    {
        // ------------------------------------------------------------------ The compass

        const float CompassWidth = 560;
        /// <summary>How many degrees of the horizon the strip shows across its width.</summary>
        const float CompassSpan = 180;

        RectTransform compassRoot, compassTicks;
        readonly List<(RectTransform mark, Graphic look, float bearing)> compassFixed = new List<(RectTransform, Graphic, float)>();
        RectTransform compassQuest;
        Text compassQuestText;
        readonly List<(RectTransform mark, Text name)> compassPlaces = new List<(RectTransform, Text)>();

        static readonly string[] Quarters = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };

        void BuildCompass(RectTransform stage)
        {
            compassRoot = UiKit.Rect("Compass", stage);
            compassRoot.At(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(CompassWidth, 40));
            var shade = UiKit.Image("Shade", compassRoot, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.45f), UiKit.FadeBoth);
            shade.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -11, 0, -11);
            UiKit.Image("Line", compassRoot, UiKit.Gold.A(0.35f), UiKit.FadeBoth).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -12, 0, 11);
            compassTicks = UiKit.Rect("Ticks", compassRoot);
            compassTicks.Fill();
            compassTicks.gameObject.AddComponent<RectMask2D>();

            for (var degrees = 0; degrees < 360; degrees += 15)
            {
                if (degrees % 45 == 0)
                {
                    var letter = UiKit.Label(Quarters[degrees / 45], compassTicks, degrees % 90 == 0 ? 15 : 12, degrees == 0 ? UiKit.GoldLit : UiKit.Text2, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed();
                    letter.rectTransform.sizeDelta = new Vector2(40, 22);
                    letter.raycastTarget = false;
                    compassFixed.Add((letter.rectTransform, letter, degrees));
                }
                else
                {
                    var tick = UiKit.Image("Tick", compassTicks, UiKit.Text.A(0.5f), UiKit.Plain);
                    tick.rectTransform.sizeDelta = new Vector2(1, 6);
                    compassFixed.Add((tick.rectTransform, tick, degrees));
                }
            }

            // The middle: where the camera looks.
            var notch = UiKit.Image("Notch", compassRoot, UiKit.GoldLit, UiKit.Diamond);
            notch.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(7, 7));

            for (var i = 0; i < 8; i++)
            {
                var place = UiKit.Rect("Place", compassTicks);
                place.sizeDelta = new Vector2(140, 34);
                var dot = UiKit.Image("Dot", place, UiKit.Moon, UiKit.Disc);
                dot.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(5, 5));
                var name = UiKit.Label("", place, 10, UiKit.Muted, TextAnchor.LowerCenter).Shadowed();
                name.rectTransform.Fill();
                name.rectTransform.offsetMin = new Vector2(0, -16);
                compassPlaces.Add((place, name));
            }

            compassQuest = UiKit.Rect("Quest", compassTicks);
            compassQuest.sizeDelta = new Vector2(120, 40);
            var gem = UiKit.Image("Gem", compassQuest, UiKit.GoldLit, UiKit.Diamond);
            gem.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(12, 12));
            UiKit.Glow(gem.transform, UiKit.Gold.A(0.5f), 10);
            compassQuestText = UiKit.Label("", compassQuest, 11, UiKit.GoldText, TextAnchor.LowerCenter).Shadowed();
            compassQuestText.rectTransform.Fill();
            compassQuestText.rectTransform.offsetMin = new Vector2(0, -18);
        }

        /// <summary>The bearing of a point from another, degrees from the north (+z) toward the east (+x).</summary>
        static float Bearing(Vector3 from, float x, float z) => Mathf.Atan2(x - from.x, z - from.z) * Mathf.Rad2Deg;

        /// <summary>Puts a mark on the strip at its bearing; hides it beyond the strip, fades it toward the ends. False if hidden.</summary>
        static bool OnStrip(RectTransform mark, float bearing, float facing, Graphic look = null)
        {
            var off = Mathf.DeltaAngle(facing, bearing);
            var shown = Mathf.Abs(off) < CompassSpan * 0.5f;
            mark.gameObject.SetActive(shown);

            if (!shown)
            {
                return false;
            }

            mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.5f);
            mark.anchoredPosition = new Vector2(off / CompassSpan * CompassWidth, mark.anchoredPosition.y);
            var fade = Mathf.Clamp01((CompassSpan * 0.5f - Mathf.Abs(off)) / 25f);

            if (look != null)
            {
                var c = look.color;
                look.color = new Color(c.r, c.g, c.b, fade * (look is Text ? 1 : 0.5f));
            }

            return true;
        }

        void TickCompass(Session session, Hero hero, Camera camera)
        {
            compassRoot.gameObject.SetActive(Prefs.Compass && !Game.Busy);

            if (!Prefs.Compass)
            {
                return;
            }

            var forward = camera.transform.forward;
            var facing = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            var at = hero.transform.position;

            foreach (var (mark, look, bearing) in compassFixed)
            {
                OnStrip(mark, bearing, facing, look);
            }

            // The task.
            var quest = session.MainQuest;
            var questShown = false;

            if (quest != null && Prefs.QuestMark <= 1 && session.MarkerOf(quest, out var point))
            {
                questShown = OnStrip(compassQuest, Bearing(at, point.X, point.Z), facing);
                var metres = Vector2.Distance(new Vector2(at.x, at.z), new Vector2(point.X, point.Z));
                compassQuestText.text = metres >= 1000 ? $"{metres / 1000:0.0} км" : $"{metres:0} м";
            }

            compassQuest.gameObject.SetActive(questShown);

            // The lanterns lit and the villages known, nearest first, as many as there are marks.
            var near = new List<(float distance, float bearing, string name, Color colour)>();

            foreach (var thing in Atlas.Things)
            {
                if (thing.Kind == ThingKind.Lantern && session.Lit(thing.Id))
                {
                    var distance = Vector2.Distance(new Vector2(at.x, at.z), new Vector2(thing.At.X, thing.At.Z));

                    if (distance > 12 && distance < 900)
                    {
                        near.Add((distance, Bearing(at, thing.At.X, thing.At.Z), "", UiKit.Moon));
                    }
                }
            }

            if (Ground.Big)
            {
                var revealed = session.Has("map_revealed");

                foreach (var settlement in Regions.Settlements)
                {
                    var distance = Vector2.Distance(new Vector2(at.x, at.z), new Vector2(settlement.At.X, settlement.At.Z));

                    if ((revealed || session.InCodex("place:" + settlement.Id)) && distance > settlement.Radius && distance < 3000)
                    {
                        near.Add((distance, Bearing(at, settlement.At.X, settlement.At.Z), settlement.Name, UiKit.Hex(settlement.Colour)));
                    }
                }
            }

            near.Sort((a, b) => a.distance.CompareTo(b.distance));

            for (var i = 0; i < compassPlaces.Count; i++)
            {
                var (mark, name) = compassPlaces[i];

                if (i >= near.Count)
                {
                    mark.gameObject.SetActive(false);
                    continue;
                }

                var (_, bearing, label, colour) = near[i];
                OnStrip(mark, bearing, facing);
                name.text = label;
                name.color = colour.A(0.85f);
                mark.GetChild(0).GetComponent<Image>().color = colour.A(label == "" ? 0.9f : 0.6f);
            }
        }

        // ------------------------------------------------------------------ The minimap

        const float MinimapSize = 210;
        /// <summary>Metres from the middle to the rim.</summary>
        const float MinimapReach = 110;
        /// <summary>The tile painted round the hero: this many pixels, this many metres across.</summary>
        const int TilePixels = 128;
        const float TileMetres = 384;

        RectTransform minimapRoot, minimapTurn, minimapMarks;
        RawImage minimapPicture;
        Image minimapHero, minimapQuest;
        RectTransform minimapNorth;
        readonly List<Image> minimapDots = new List<Image>();

        Texture2D tileShown, tileMaking;
        Color[] tilePixels;
        Vector2 tileCentre = new Vector2(float.MaxValue, 0), makingCentre;
        int makingRow = -1;
        readonly float[] tileWeights = new float[Core.Gen.Surface.Layers];

        void BuildMinimap(RectTransform stage)
        {
            minimapRoot = UiKit.Rect("Minimap", stage);
            minimapRoot.BottomRight(40, 40, MinimapSize, MinimapSize);
            var glow = UiKit.Image("Glow", minimapRoot, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.6f), UiKit.Halo);
            glow.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MinimapSize * 1.35f, MinimapSize * 1.35f));

            // The disc that cuts the map round.
            var disc = UiKit.Image("Disc", minimapRoot, Color.white, UiKit.Disc);
            disc.rectTransform.Fill();
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            minimapTurn = UiKit.Rect("Turn", disc.transform);
            minimapTurn.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MinimapSize, MinimapSize));
            minimapPicture = UiKit.Rect("Land", minimapTurn).gameObject.AddComponent<RawImage>();
            minimapPicture.raycastTarget = false;
            minimapPicture.color = new Color(0.82f, 0.86f, 0.9f, 1);
            var scale = MinimapSize / (MinimapReach * 2);
            minimapPicture.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TileMetres * scale, TileMetres * scale));
            minimapMarks = UiKit.Rect("Marks", minimapTurn);
            minimapMarks.Fill();

            for (var i = 0; i < 24; i++)
            {
                var dot = UiKit.Image("Dot", minimapMarks, UiKit.Danger, UiKit.Disc);
                dot.rectTransform.sizeDelta = new Vector2(6, 6);
                dot.raycastTarget = false;
                minimapDots.Add(dot);
            }

            // A soft dark edge inside the rim, and the rims.
            UiKit.Image("Edge", disc.transform, new Color(0, 0, 0, 0.55f), UiKit.Vignette).rectTransform.Fill(-6);
            UiKit.Orbit(minimapRoot, MinimapSize + 2, UiKit.Gold.A(0.45f), 0, 0);
            UiKit.Orbit(minimapRoot, MinimapSize + 14, UiKit.Gold.A(0.15f), 120, 8);

            // The task (held to the rim when farther), the north on the rim, the hero in the middle.
            minimapQuest = UiKit.Image("Quest", minimapRoot, UiKit.GoldLit, UiKit.Diamond);
            minimapQuest.rectTransform.sizeDelta = new Vector2(12, 12);
            UiKit.Glow(minimapQuest.transform, UiKit.Gold.A(0.5f), 10);
            var north = UiKit.Label("С", minimapRoot, 13, UiKit.GoldLit, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed();
            minimapNorth = north.rectTransform;
            minimapNorth.sizeDelta = new Vector2(22, 22);
            minimapHero = UiKit.Image("Hero", minimapRoot, UiKit.Essence, UiKit.Arrow);
            minimapHero.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
            UiKit.HaloAt(minimapHero.transform, UiKit.Essence.A(0.5f), Vector2.zero, 30);
        }

        void TickMinimap(Session session, Hero hero, Camera camera)
        {
            var shown = Prefs.Minimap && !Game.Busy;
            minimapRoot.gameObject.SetActive(shown);

            if (!shown)
            {
                return;
            }

            var at = hero.transform.position;
            var here = new Vector2(at.x, at.z);
            PaintTile(here);

            if (tileShown == null)
            {
                return;
            }

            // The map turns with the camera: what is ahead on screen is up on the map.
            var forward = camera.transform.forward;
            var facing = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            minimapTurn.localRotation = Quaternion.Euler(0, 0, facing);
            var scale = MinimapSize / (MinimapReach * 2);

            Vector2 OnMap(float x, float z) => new Vector2(x - here.x, z - here.y) * scale;

            minimapPicture.rectTransform.anchoredPosition = (tileCentre - here) * scale;

            // The hero's arrow points where they face, against the camera.
            var heroFacing = Mathf.Atan2(hero.transform.forward.x, hero.transform.forward.z) * Mathf.Rad2Deg;
            minimapHero.rectTransform.localRotation = Quaternion.Euler(0, 0, facing - heroFacing);

            // The north on the rim.
            var northAngle = facing * Mathf.Deg2Rad;
            minimapNorth.anchoredPosition = new Vector2(-Mathf.Sin(northAngle), Mathf.Cos(northAngle)) * (MinimapSize * 0.5f + 12);
            minimapNorth.anchorMin = minimapNorth.anchorMax = new Vector2(0.5f, 0.5f);

            // Enemies near (red; the aware brighter, bosses bigger) and lanterns lit (blue).
            var used = 0;

            foreach (var enemy in Enemy.Living)
            {
                if (used >= minimapDots.Count)
                {
                    break;
                }

                var e = enemy.transform.position;

                if ((new Vector2(e.x, e.z) - here).sqrMagnitude > MinimapReach * MinimapReach * 0.9f)
                {
                    continue;
                }

                var dot = minimapDots[used++];
                dot.gameObject.SetActive(true);
                dot.color = enemy.Aware ? UiKit.Danger : UiKit.Danger.A(0.55f);
                dot.rectTransform.anchoredPosition = OnMap(e.x, e.z);
                dot.rectTransform.sizeDelta = enemy.Def.Boss ? new Vector2(10, 10) : new Vector2(6, 6);
            }

            foreach (var thing in Atlas.Things)
            {
                if (used >= minimapDots.Count)
                {
                    break;
                }

                if (thing.Kind != ThingKind.Lantern || !session.Lit(thing.Id) || (new Vector2(thing.At.X, thing.At.Z) - here).sqrMagnitude > MinimapReach * MinimapReach * 0.9f)
                {
                    continue;
                }

                var dot = minimapDots[used++];
                dot.gameObject.SetActive(true);
                dot.color = UiKit.Moon;
                dot.rectTransform.anchoredPosition = OnMap(thing.At.X, thing.At.Z);
                dot.rectTransform.sizeDelta = new Vector2(7, 7);
            }

            // Wild Gu out near: small lights of their colour.
            if (WildGuField.Instance != null)
            {
                foreach (var (wild, colour) in WildGuField.Instance.Lights)
                {
                    if (used >= minimapDots.Count)
                    {
                        break;
                    }

                    if ((new Vector2(wild.x, wild.z) - here).sqrMagnitude > MinimapReach * MinimapReach * 0.9f)
                    {
                        continue;
                    }

                    var dot = minimapDots[used++];
                    dot.gameObject.SetActive(true);
                    dot.color = colour;
                    dot.rectTransform.anchoredPosition = OnMap(wild.x, wild.z);
                    dot.rectTransform.sizeDelta = new Vector2(5, 5);
                }
            }

            for (var i = used; i < minimapDots.Count; i++)
            {
                minimapDots[i].gameObject.SetActive(false);
            }

            // The task: on the map, or held to the rim pointing at it.
            var quest = session.MainQuest;

            if (quest != null && Prefs.QuestMark <= 1 && session.MarkerOf(quest, out var point))
            {
                minimapQuest.gameObject.SetActive(true);
                var offset = OnMap(point.X, point.Z);
                var turned = (Vector2)(Quaternion.Euler(0, 0, facing) * offset);
                var rim = MinimapSize * 0.5f - 8;

                if (turned.magnitude > rim)
                {
                    turned = turned.normalized * rim;
                }

                minimapQuest.rectTransform.anchorMin = minimapQuest.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                minimapQuest.rectTransform.anchoredPosition = turned;
            }
            else
            {
                minimapQuest.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Keeps a painted tile round the hero: when they walk a quarter of it from its middle, a new
        /// one is painted round where they are — a few rows a frame, the old shown till it is done.
        /// </summary>
        void PaintTile(Vector2 here)
        {
            // A pixel costs some microseconds (the ground's height thrice, its layers, rivers, roads):
            // rows are painted while a frame's share lasts, so a weak laptop takes a second or two per tile.
            const double budgetMs = 1.2;
            var started = System.Diagnostics.Stopwatch.StartNew();

            if (makingRow < 0)
            {
                // A new tile once the hero is 48 m from this one's middle (the rim shows 110 m; the tile reaches 192).
                if (tileShown != null && (here - tileCentre).magnitude < 48)
                {
                    return;
                }

                makingCentre = new Vector2(Mathf.Round(here.x / 32) * 32, Mathf.Round(here.y / 32) * 32);
                makingRow = 0;
                tilePixels = tilePixels ?? new Color[TilePixels * TilePixels];
            }

            var cell = TileMetres / TilePixels;

            for (; makingRow < TilePixels && started.Elapsed.TotalMilliseconds < budgetMs; makingRow++)
            {
                var z = makingCentre.y - TileMetres * 0.5f + cell * (makingRow + 0.5f);

                for (var i = 0; i < TilePixels; i++)
                {
                    var x = makingCentre.x - TileMetres * 0.5f + cell * (i + 0.5f);
                    tilePixels[makingRow * TilePixels + i] = MapColour(x, z, cell, tileWeights);
                }
            }

            if (makingRow < TilePixels)
            {
                return;
            }

            tileMaking = tileMaking ?? new Texture2D(TilePixels, TilePixels, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, name = "Minimap" };
            tileMaking.SetPixels(tilePixels);
            tileMaking.Apply();
            (tileShown, tileMaking) = (tileMaking, tileShown);
            tileCentre = makingCentre;
            minimapPicture.texture = tileShown;
            makingRow = -1;
        }
    }
}
