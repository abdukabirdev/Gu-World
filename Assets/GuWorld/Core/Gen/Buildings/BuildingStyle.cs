using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>A roof's form: two slopes; four slopes; four slopes with a gable above each end (the halls' roof); slopes to a point.</summary>
    public enum RoofKind { Gable, Hip, HipGable, Pyramid }

    /// <summary>What a roof is covered with.</summary>
    public enum RoofCover { Tile, Thatch, Shingle }

    /// <summary>What fills the frame between the posts.</summary>
    public enum WallKind { Plaster, Mud, Plank, Brick, Stone, Bamboo }

    /// <summary>
    /// A kind of building as a set of numbers: a timber frame of bays on a plinth (or stilts), walls
    /// between its posts, storeys with skirt roofs and balconies, and a roof. The generator
    /// (BuildingGen) raises a building from a style, a seed and BuildingParams; every number here
    /// is a direction the generator varies within.
    /// </summary>
    public sealed class BuildingStyle
    {
        public string Id, Name;
        /// <summary>Bays across the front and from front to back (a bay: the space between two posts), each a range.</summary>
        public int MinBaysX = 3, MaxBaysX = 3, MinBaysZ = 2, MaxBaysZ = 2;
        /// <summary>A bay's width in metres (the middle bay of the front is a little wider).</summary>
        public float Bay = 3.2f;
        public int MinStoreys = 1, MaxStoreys = 1;
        /// <summary>A storey's height to the top of its posts, metres.</summary>
        public float StoreyHeight = 3.2f;
        /// <summary>The stone plinth's height; with Stilts, how high the floor stands on its posts.</summary>
        public float Plinth = 0.45f;
        public bool Stilts;
        public RoofKind Roof = RoofKind.Gable;
        public RoofCover Cover = RoofCover.Tile;
        /// <summary>The roof's rise as a share of its half depth.</summary>
        public float Pitch = 0.62f;
        /// <summary>How hollow the slope is (0 straight, 0.6 steep at the ridge and flat at the eave).</summary>
        public float Curve = 0.3f;
        /// <summary>How far the corners of the eaves turn up, metres.</summary>
        public float Upturn = 0.5f;
        /// <summary>How far the eaves stand out past the posts, metres.</summary>
        public float Overhang = 1.1f;
        public WallKind Walls = WallKind.Plaster;
        /// <summary>The wall under the windows of the ground storey (a dado), if different.</summary>
        public WallKind Dado = WallKind.Plaster;
        /// <summary>A gallery under the roof along the front, this deep (0: none).</summary>
        public float Veranda;
        /// <summary>The chance of a balcony round each upper storey.</summary>
        public float Balcony = 0.5f;
        /// <summary>The front of the ground storey stands open (a shop, a tea house).</summary>
        public bool OpenFront;
        /// <summary>No walls at all (a pavilion, a gate).</summary>
        public bool Open;
        /// <summary>The share of side and back bays with a window.</summary>
        public float Windows = 0.5f;
        /// <summary>The front's bays are lattice doors from post to post (a hall), not a door between windows.</summary>
        public bool LatticeFront;
        /// <summary>Bracket sets under the eaves: 0 none, 1 plain, 2 stepped.</summary>
        public int Brackets;
        /// <summary>How rich the ridges are: 0 a plain cap, 1 horns, beasts and a finial.</summary>
        public float Ornament = 0.3f;
        /// <summary>How much each upper storey draws in on every side, metres.</summary>
        public float Shrink = 0.9f;
        /// <summary>The ground storey is a blind stone base this high in storeys (a watchtower), 0 none.</summary>
        public int StoneBase;
        public int Lanterns = 2;
        public bool Sign;
        /// <summary>The frame's wood: lacquered in the clan's colour (true) or left dark.</summary>
        public bool Lacquer;
    }

    /// <summary>What is asked of one building.</summary>
    public struct BuildingParams
    {
        /// <summary>The lot to fill, metres (0: the style's own size). The bays are fitted to it.</summary>
        public float Width, Depth;
        /// <summary>Storeys (0: the style's own).</summary>
        public int Storeys;
        /// <summary>0 poor … 1 rich: brackets, ornament, lattice, the plinth's height.</summary>
        public float Wealth;
        /// <summary>0 new … 1 old: stains, moss on the roof, sagging ridge.</summary>
        public float Wear;
        /// <summary>The clan whose colours it wears: "gu_yue" (red lacquer, gold moon), "xiong" (raw dark timber, ochre), "bai" (white), "" (none).</summary>
        public string Clan;
    }

    /// <summary>A door of a building: where it is and which way it faces (the model's space).</summary>
    public struct BuildingDoor
    {
        public V3 At, Facing;
    }

    /// <summary>A building as made: its levels of detail (0 near … 3 the horizon), each a set of parts by material, and a solid for feet.</summary>
    public sealed class BuildingModel
    {
        public BuildingStyle Style;
        public int Seed;
        public BuildingParams Asked;
        /// <summary>The frame's size on the ground and the height to the roof's top, metres; the reach of the eaves.</summary>
        public float Width, Depth, Height, EaveWidth, EaveDepth;
        public int Storeys, BaysX, BaysZ;
        /// <summary>
        /// Parts by material: "tex:name" (a tiling texture of Resources/Buildings, tinted by the
        /// vertices), "plain" (vertex colours only: gilding, bronze), "glow" (lit paper and lanterns).
        /// </summary>
        public readonly TreeLod[] Lods = new TreeLod[4];
        public MeshData Collision;
        public readonly List<BuildingDoor> Doors = new List<BuildingDoor>();
    }

    /// <summary>The kinds of building the world has.</summary>
    public static class Buildings
    {
        public static readonly BuildingStyle Hut = new BuildingStyle
        {
            // A poor family's hut: mud walls, a thick thatch, hardly a window.
            Id = "hut", Name = "Mud hut", MinBaysX = 2, MaxBaysX = 3, MinBaysZ = 1, MaxBaysZ = 2, Bay = 2.6f, StoreyHeight = 2.5f, Plinth = 0.2f,
            Roof = RoofKind.Hip, Cover = RoofCover.Thatch, Pitch = 0.85f, Curve = 0.08f, Upturn = 0, Overhang = 0.8f, Walls = WallKind.Mud, Dado = WallKind.Mud,
            Windows = 0.25f, Ornament = 0, Lanterns = 0,
        };

        public static readonly BuildingStyle Cottage = new BuildingStyle
        {
            // A villager's house of planks under shingles, a porch along its front.
            Id = "cottage", Name = "Plank cottage", MinBaysX = 3, MaxBaysX = 3, MinBaysZ = 2, MaxBaysZ = 2, Bay = 2.8f, StoreyHeight = 2.7f, Plinth = 0.35f,
            Roof = RoofKind.Gable, Cover = RoofCover.Shingle, Pitch = 0.7f, Curve = 0.12f, Upturn = 0.12f, Overhang = 0.9f, Walls = WallKind.Plank, Dado = WallKind.Plank,
            Veranda = 1.5f, Windows = 0.45f, Ornament = 0.05f, Lanterns = 1,
        };

        public static readonly BuildingStyle Stilt = new BuildingStyle
        {
            // A mountain house of bamboo on stilts, a ladder to its gallery.
            Id = "stilt", Name = "Stilt house", MinBaysX = 2, MaxBaysX = 3, MinBaysZ = 2, MaxBaysZ = 2, Bay = 2.7f, StoreyHeight = 2.6f, Plinth = 2.1f, Stilts = true,
            Roof = RoofKind.HipGable, Cover = RoofCover.Thatch, Pitch = 0.8f, Curve = 0.12f, Upturn = 0.1f, Overhang = 1.0f, Walls = WallKind.Bamboo, Dado = WallKind.Bamboo,
            Veranda = 1.4f, Windows = 0.5f, Ornament = 0, Lanterns = 1,
        };

        public static readonly BuildingStyle House = new BuildingStyle
        {
            // A clansman's house: white plaster in a dark frame, grey tiles.
            Id = "house", Name = "Clan house", MinBaysX = 3, MaxBaysX = 5, MinBaysZ = 2, MaxBaysZ = 2, Bay = 3.0f, StoreyHeight = 3.0f, Plinth = 0.45f,
            Roof = RoofKind.Gable, Cover = RoofCover.Tile, Pitch = 0.62f, Curve = 0.28f, Upturn = 0.35f, Overhang = 1.0f, Walls = WallKind.Plaster, Dado = WallKind.Brick,
            Windows = 0.5f, Brackets = 0, Ornament = 0.3f, Lanterns = 2,
        };

        public static readonly BuildingStyle Townhouse = new BuildingStyle
        {
            // Two storeys on a street: a balcony over the door, a skirt roof between.
            Id = "townhouse", Name = "Two-storey house", MinBaysX = 3, MaxBaysX = 3, MinBaysZ = 2, MaxBaysZ = 3, Bay = 3.0f, MinStoreys = 2, MaxStoreys = 2, StoreyHeight = 2.9f, Plinth = 0.4f,
            Roof = RoofKind.HipGable, Cover = RoofCover.Tile, Pitch = 0.62f, Curve = 0.3f, Upturn = 0.5f, Overhang = 1.0f, Walls = WallKind.Plaster, Dado = WallKind.Brick,
            Balcony = 0.8f, Windows = 0.6f, Brackets = 1, Ornament = 0.4f, Shrink = 0.5f, Lanterns = 2, Lacquer = true,
        };

        public static readonly BuildingStyle Shop = new BuildingStyle
        {
            // A shop: its front open to the street under a deep eave, a board over the counter, rooms above.
            Id = "shop", Name = "Shop", MinBaysX = 3, MaxBaysX = 4, MinBaysZ = 2, MaxBaysZ = 3, Bay = 3.0f, MinStoreys = 1, MaxStoreys = 2, StoreyHeight = 3.1f, Plinth = 0.3f,
            Roof = RoofKind.Gable, Cover = RoofCover.Tile, Pitch = 0.6f, Curve = 0.28f, Upturn = 0.4f, Overhang = 1.3f, Walls = WallKind.Plank, Dado = WallKind.Plank,
            OpenFront = true, Balcony = 0.5f, Windows = 0.4f, Brackets = 1, Ornament = 0.35f, Shrink = 0.4f, Lanterns = 3, Sign = true, Lacquer = true,
        };

        public static readonly BuildingStyle Inn = new BuildingStyle
        {
            // An inn or a tavern: broad, three storeys of galleries, lanterns at every post.
            Id = "inn", Name = "Inn", MinBaysX = 5, MaxBaysX = 5, MinBaysZ = 3, MaxBaysZ = 3, Bay = 3.2f, MinStoreys = 2, MaxStoreys = 3, StoreyHeight = 3.1f, Plinth = 0.6f,
            Roof = RoofKind.HipGable, Cover = RoofCover.Tile, Pitch = 0.66f, Curve = 0.34f, Upturn = 0.8f, Overhang = 1.3f, Walls = WallKind.Plank, Dado = WallKind.Brick,
            Veranda = 1.8f, Balcony = 1, Windows = 0.7f, Brackets = 2, Ornament = 0.6f, Shrink = 0.7f, Lanterns = 6, Sign = true, Lacquer = true,
        };

        public static readonly BuildingStyle Hall = new BuildingStyle
        {
            // A hall of the clan: a broad front of lattice doors behind a colonnade, on a high plinth, a great curved roof.
            Id = "hall", Name = "Clan hall", MinBaysX = 5, MaxBaysX = 7, MinBaysZ = 3, MaxBaysZ = 4, Bay = 3.8f, StoreyHeight = 4.6f, Plinth = 1.3f,
            Roof = RoofKind.HipGable, Cover = RoofCover.Tile, Pitch = 0.72f, Curve = 0.42f, Upturn = 1.3f, Overhang = 1.9f, Walls = WallKind.Plaster, Dado = WallKind.Brick,
            Veranda = 2.6f, Windows = 0.5f, LatticeFront = true, Brackets = 2, Ornament = 1, Lanterns = 4, Lacquer = true,
        };

        public static readonly BuildingStyle GreatHall = new BuildingStyle
        {
            // The clan's great hall: two eaves, nine bays, the highest plinth in the village.
            Id = "great_hall", Name = "Great hall", MinBaysX = 7, MaxBaysX = 9, MinBaysZ = 4, MaxBaysZ = 5, Bay = 4.2f, MinStoreys = 2, MaxStoreys = 2, StoreyHeight = 5.2f, Plinth = 2.2f,
            Roof = RoofKind.HipGable, Cover = RoofCover.Tile, Pitch = 0.74f, Curve = 0.45f, Upturn = 1.7f, Overhang = 2.2f, Walls = WallKind.Plaster, Dado = WallKind.Brick,
            Veranda = 3.0f, Balcony = 0, Windows = 0.6f, LatticeFront = true, Brackets = 2, Ornament = 1, Shrink = 2.4f, Lanterns = 6, Lacquer = true,
        };

        public static readonly BuildingStyle Tower = new BuildingStyle
        {
            // A pagoda tower: square, storey on storey each with its own eave, a spire at the top.
            Id = "tower", Name = "Pagoda tower", MinBaysX = 3, MaxBaysX = 3, MinBaysZ = 3, MaxBaysZ = 3, Bay = 2.8f, MinStoreys = 4, MaxStoreys = 7, StoreyHeight = 3.4f, Plinth = 1.1f,
            Roof = RoofKind.Pyramid, Cover = RoofCover.Tile, Pitch = 0.95f, Curve = 0.45f, Upturn = 1.0f, Overhang = 1.5f, Walls = WallKind.Plaster, Dado = WallKind.Brick,
            Balcony = 1, Windows = 0.8f, LatticeFront = true, Brackets = 2, Ornament = 1, Shrink = 0.55f, Lanterns = 4, Lacquer = true,
        };

        public static readonly BuildingStyle Watchtower = new BuildingStyle
        {
            // A watchtower of the wall: a blind stone shaft, an open lookout under a pointed roof.
            Id = "watchtower", Name = "Watchtower", MinBaysX = 2, MaxBaysX = 2, MinBaysZ = 2, MaxBaysZ = 2, Bay = 3.4f, MinStoreys = 3, MaxStoreys = 4, StoreyHeight = 3.6f, Plinth = 0.3f,
            Roof = RoofKind.Pyramid, Cover = RoofCover.Tile, Pitch = 0.8f, Curve = 0.4f, Upturn = 1.1f, Overhang = 1.6f, Walls = WallKind.Stone, Dado = WallKind.Stone,
            Balcony = 1, Windows = 0.2f, Brackets = 1, Ornament = 0.7f, Shrink = 0.2f, StoneBase = 2, Lanterns = 2, Lacquer = true,
        };

        public static readonly BuildingStyle Pavilion = new BuildingStyle
        {
            // A garden pavilion: posts, a bench rail, a pointed roof with long horns.
            Id = "pavilion", Name = "Pavilion", MinBaysX = 1, MaxBaysX = 1, MinBaysZ = 1, MaxBaysZ = 1, Bay = 4.2f, StoreyHeight = 3.3f, Plinth = 0.6f,
            Roof = RoofKind.Pyramid, Cover = RoofCover.Tile, Pitch = 0.72f, Curve = 0.5f, Upturn = 1.0f, Overhang = 1.4f, Open = true,
            Windows = 0, Brackets = 1, Ornament = 0.9f, Lanterns = 0, Lacquer = true,
        };

        public static readonly BuildingStyle Gate = new BuildingStyle
        {
            // A gatehouse: one deep bay to pass through between two closed ones, a hall's roof over it.
            Id = "gate", Name = "Gatehouse", MinBaysX = 3, MaxBaysX = 3, MinBaysZ = 1, MaxBaysZ = 1, Bay = 3.6f, StoreyHeight = 4.2f, Plinth = 0.35f,
            Roof = RoofKind.HipGable, Cover = RoofCover.Tile, Pitch = 0.7f, Curve = 0.4f, Upturn = 1.1f, Overhang = 1.6f, Walls = WallKind.Plaster, Dado = WallKind.Stone,
            Open = true, Windows = 0, Brackets = 2, Ornament = 0.9f, Lanterns = 2, Sign = true, Lacquer = true,
        };

        public static readonly BuildingStyle Granary = new BuildingStyle
        {
            // A storehouse: brick, few openings, a plain heavy roof.
            Id = "granary", Name = "Storehouse", MinBaysX = 3, MaxBaysX = 4, MinBaysZ = 2, MaxBaysZ = 2, Bay = 3.3f, StoreyHeight = 3.6f, Plinth = 0.7f,
            Roof = RoofKind.Gable, Cover = RoofCover.Tile, Pitch = 0.58f, Curve = 0.2f, Upturn = 0.2f, Overhang = 0.8f, Walls = WallKind.Brick, Dado = WallKind.Stone,
            Windows = 0.15f, Ornament = 0.15f, Lanterns = 1,
        };

        public static readonly BuildingStyle[] All = { Hut, Cottage, Stilt, House, Townhouse, Shop, Inn, Hall, GreatHall, Tower, Watchtower, Pavilion, Gate, Granary };

        public static BuildingStyle Get(string id)
        {
            foreach (var style in All)
            {
                if (style.Id == id)
                {
                    return style;
                }
            }

            return House;
        }
    }
}
