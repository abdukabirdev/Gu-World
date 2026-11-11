using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>A point on the valley's map: X to the east, Z to the north, metres from the village well.</summary>
    [Serializable]
    public struct MapPoint
    {
        public float X;
        public float Z;

        public MapPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float DistanceTo(MapPoint other)
        {
            var dx = X - other.X;
            var dz = Z - other.Z;

            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public override string ToString() => $"({X:0}, {Z:0})";
    }

    public enum GuPath { Moon, Earth, Wind, Wood, Lightning, Light, Wine, Ice, Strength, Blood }

    /// <summary>Where a Gu can sit: an active slot (a skill on a key) or a passive one (a standing bonus).</summary>
    public enum GuKind { Active, Passive }

    public enum SkillShape
    {
        None,
        /// <summary>One bolt flying forward.</summary>
        Projectile,
        /// <summary>Several bolts spread in a fan.</summary>
        Fan,
        /// <summary>Everything in a cone in front.</summary>
        Cone,
        /// <summary>Everything round the caster.</summary>
        Nova,
        /// <summary>A quick run forward through enemies.</summary>
        Dash,
        /// <summary>A leap to a point, striking where it leaves and lands.</summary>
        Blink,
        /// <summary>A lasting change to the caster.</summary>
        Buff,
        /// <summary>Health back over time.</summary>
        Heal,
        /// <summary>Drags enemies to a point.</summary>
        Pull,
        /// <summary>A storm over a place: strikes for a while.</summary>
        Storm,
        /// <summary>Jumps from enemy to enemy.</summary>
        Chain
    }

    public enum StatusKind { None, Slow, Stun, Freeze }

    public enum BuffKind { None, Shield, MeleeUp, Unstoppable }

    public enum StatKind
    {
        /// <summary>Essence regeneration, share added (0.5 = +50%).</summary>
        EssenceRegen,
        /// <summary>Essence cost of skills, share added (-0.1 = 10% cheaper).</summary>
        Cost,
        /// <summary>Cooldowns, share added.</summary>
        Cooldown,
        /// <summary>Power of all skills, share added.</summary>
        Power,
        /// <summary>Power of Moon path skills, share added.</summary>
        MoonPower,
        /// <summary>Damage taken, share added (-0.15 = 15% less).</summary>
        DamageTaken,
        /// <summary>Health, points added.</summary>
        Health,
        /// <summary>Range of skills, share added.</summary>
        Range,
        /// <summary>Share of damage dealt that comes back as health.</summary>
        Leech,
        /// <summary>A ward that swallows one blow every this many seconds (the smallest wins).</summary>
        WardPeriod
    }

    [Serializable]
    public class StatBonus
    {
        public StatKind Stat;
        public float Value;

        public StatBonus() { }

        public StatBonus(StatKind stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }

    public class SkillDef
    {
        public SkillShape Shape;
        /// <summary>Damage (or health, for a heal) before the caster's bonuses.</summary>
        public float Power;
        /// <summary>Essence it takes, points (a rank 1 aperture at 100% holds 100).</summary>
        public float Cost;
        public float Cooldown;
        public float Range;
        public float Radius;
        public float Duration;
        public float Speed = 26;
        public int Count = 1;
        public bool Pierce;
        public StatusKind Status;
        public float StatusPower;
        public float StatusTime;
        public BuffKind Buff;
        public float BuffValue;
        /// <summary>Share of the damage that heals the caster.</summary>
        public float Leech;
    }

    public class GuDef
    {
        public string Id;
        public string Name;
        /// <summary>One hanzi for its seal in the interface.</summary>
        public string Seal;
        public int Rank;
        public GuPath Path;
        public GuKind Kind;
        public string Description;
        public SkillDef Skill;
        public StatBonus[] Passive = new StatBonus[0];
        /// <summary>Colour of its light, 0xRRGGBB.</summary>
        public int Colour = 0xA0D2FF;
        /// <summary>What a shop pays for it, primeval stones.</summary>
        public int Price;
    }

    public enum ItemKind { Currency, Material, Consumable, Scroll, Quest, Rock }

    public class ItemDef
    {
        public string Id;
        public string Name;
        public string Seal;
        public ItemKind Kind;
        public string Description;
        /// <summary>What a shop sells it for; it buys at 40% of this.</summary>
        public int Price;
        /// <summary>Health a consumable gives back.</summary>
        public float Heal;
        /// <summary>Essence a consumable gives back, share of the aperture.</summary>
        public float Essence;
        /// <summary>The recipe a scroll teaches.</summary>
        public string Teaches;
        /// <summary>A rock's tier, for the gamble.</summary>
        public int Tier;
        public int Colour = 0xE8C98A;
    }

    public enum EnemyBody { Dummy, Canid, Human, Bug }

    public enum EnemyMind
    {
        /// <summary>Stands still.</summary>
        Still,
        /// <summary>Bites and hops back.</summary>
        Skirmisher,
        /// <summary>Circles with the pack, then lunges.</summary>
        PackHunter,
        /// <summary>Keeps its distance and shoots.</summary>
        Shooter,
        /// <summary>Walks in and swings.</summary>
        Brawler,
        /// <summary>Hovers and darts in.</summary>
        Swarm,
        /// <summary>A boss with a pattern of moves.</summary>
        Boss
    }

    /// <summary>One move in a boss's pattern.</summary>
    public enum BossMove { Lunge, Howl, Charge, Rings, Leap, Bolts, Slam, Blades, Summon, Shield, BloodMoon }

    public class EnemyDef
    {
        public string Id;
        public string Name;
        public string Title;
        public EnemyBody Body;
        public EnemyMind Mind;
        /// <summary>Which look of the body: a texture variant ("grey", "storm", "blood", "bandit", "xiong", "bai", "elder"…).</summary>
        public string Look = "";
        public float Scale = 1;
        public int Level = 1;
        public float Health = 40;
        public float Damage = 8;
        public float Speed = 4.5f;
        public float AttackRange = 1.8f;
        public float Sight = 18;
        /// <summary>Seconds between attacks.</summary>
        public float AttackPeriod = 2.2f;
        /// <summary>Share of damage it shrugs off.</summary>
        public float Armour;
        /// <summary>Blows that stagger it do so only past this much damage at once.</summary>
        public float Poise = 10;
        public int Insight = 10;
        public bool Boss;
        public string Loot;
        public BossMove[] Moves = new BossMove[0];
        /// <summary>Whom a howl or a summon calls.</summary>
        public string Summons;
        public int Colour = 0xFFFFFF;
    }

    public class LootEntry
    {
        public string Item;
        public float Chance = 1;
        public int Min = 1;
        public int Max = 1;
    }

    public class LootTable
    {
        public string Id;
        public int StonesMin;
        public int StonesMax;
        public LootEntry[] Items = new LootEntry[0];
        /// <summary>Chance of a wild Gu, and the pool it comes from.</summary>
        public float GuChance;
        public string[] GuPool = new string[0];
        /// <summary>The least quality of that Gu (bosses give rare or better).</summary>
        public int GuMinQuality;
    }

    public class RecipeDef
    {
        public string Id;
        public string Result;
        public string[] Gu = new string[0];
        public (string item, int count)[] Items = new (string, int)[0];
        public int Stones;
        /// <summary>Known from the start, without a scroll.</summary>
        public bool Known;
    }

    public enum StepKind
    {
        /// <summary>Speak to someone (Target: the NPC).</summary>
        Talk,
        /// <summary>Kill Count of Target (an enemy id).</summary>
        Kill,
        /// <summary>Have Count of Target (an item id) in the bag.</summary>
        Have,
        /// <summary>Come to a place (Target: a place id).</summary>
        Reach,
        /// <summary>Use a thing in the world (Target: its id).</summary>
        Use,
        /// <summary>Put a Gu in a slot (Target: the Gu id, or empty for any).</summary>
        Equip,
        /// <summary>Hit Count times with a skill (Target: the Gu id).</summary>
        Cast,
        /// <summary>Reach a cultivation step (Count: the step, 0–11).</summary>
        Cultivate,
        /// <summary>Win an encounter (Target: its id).</summary>
        Encounter,
        /// <summary>Fuse a Gu (Target: the result id, or empty for any).</summary>
        Fuse,
        /// <summary>Choose an ending (Target: the flag the choice sets is checked by prefix).</summary>
        Choose
    }

    public class Line
    {
        public string Who;
        public string Text;

        public Line(string who, string text)
        {
            Who = who;
            Text = text;
        }
    }

    public class StepDef
    {
        public StepKind Kind;
        public string Target = "";
        public int Count = 1;
        public string Text;
        /// <summary>Where the compass points: a place, NPC or thing id (default: the target).</summary>
        public string Marker;
        /// <summary>What is said when a Talk step is done.</summary>
        public Line[] Lines = new Line[0];
        /// <summary>Items given when the step begins (a Gu id is given as a Gu).</summary>
        public string[] Gives = new string[0];
        /// <summary>Items taken from the bag when the step is done (for Have steps).</summary>
        public bool Takes;
        /// <summary>A Choose step's answers: what is said and the flag it sets.</summary>
        public (string text, string flag)[] Choices = new (string, string)[0];
    }

    public class Reward
    {
        public int Stones;
        public int Insight;
        public (string item, int count)[] Items = new (string, int)[0];
        public string[] Gu = new string[0];
        public string[] Flags = new string[0];
        /// <summary>The talent the hero rises to (share of the aperture), 0 for none.</summary>
        public float Talent;
    }

    public class QuestDef
    {
        public string Id;
        public string Title;
        public int Chapter;
        public bool Main;
        public string Summary;
        /// <summary>Who gives a side quest (talking to them starts it).</summary>
        public string Giver;
        /// <summary>The flag that must be set before a side quest is offered.</summary>
        public string Needs;
        public Line[] Offer = new Line[0];
        public StepDef[] Steps = new StepDef[0];
        public Reward Reward = new Reward();
        /// <summary>The main quest that follows by itself.</summary>
        public string Next;
        public bool Repeatable;
    }

    public class NpcDef
    {
        public string Id;
        public string Name;
        public string Title;
        public string Look = "villager";
        public MapPoint At;
        /// <summary>The way they face, degrees clockwise from north.</summary>
        public float Facing;
        /// <summary>What they say when they have nothing to tell.</summary>
        public string[] Barks = new string[0];
        /// <summary>The service they offer: "", "shop", "gamble", "board".</summary>
        public string Service = "";
        /// <summary>The flag after which they are gone (or "" for never).</summary>
        public string GoneAfter = "";
    }

    public enum ThingKind { Lantern, Spring, FusionAltar, Chest, Herb, Door, QuestObject, Dummy }

    /// <summary>A thing in the world the hero can use.</summary>
    public class ThingDef
    {
        public string Id;
        public string Name;
        public ThingKind Kind;
        public MapPoint At;
        public float Facing;
        /// <summary>A herb's item, a chest's loot table, a door's destination (a place id).</summary>
        public string Content = "";
        /// <summary>The flag that must be set for it to be usable ("" for always).</summary>
        public string Needs = "";
        /// <summary>The line shown when it is locked.</summary>
        public string Locked = "";
        /// <summary>The model that stands there (a prefab of Resources/Build), or "" for one made in code.</summary>
        public string Model = "";
        /// <summary>Seconds until a herb grows back (0: never).</summary>
        public float Regrow;
        /// <summary>Where a door leads.</summary>
        public MapPoint To;
    }

    /// <summary>Models strewn over a patch of ground (a grove, a wood, boulders).</summary>
    public class ScatterDef
    {
        public string Model;
        public MapPoint At;
        public float Radius;
        public int Count;
        public float MinScale = 0.8f;
        public float MaxScale = 1.2f;
        /// <summary>Nothing is put closer than this to a road, a building or a place that is levelled.</summary>
        public float Clear = 3;

        public ScatterDef(string model, float x, float z, float radius, int count, float minScale = 0.8f, float maxScale = 1.2f, float clear = 3)
        {
            Model = model;
            At = new MapPoint(x, z);
            Radius = radius;
            Count = count;
            MinScale = minScale;
            MaxScale = maxScale;
            Clear = clear;
        }
    }

    public class PlaceDef
    {
        public string Id;
        public string Name;
        public MapPoint At;
        public float Radius;
        /// <summary>The ground is levelled here, to this height (metres); NaN to leave the hills alone.</summary>
        public float Level = float.NaN;
        /// <summary>How far past its radius the levelled ground eases back into the hills, metres.</summary>
        public float Blend = 30;
        /// <summary>Levelled to the land's own height round it (when no Level is given).</summary>
        public bool Flatten;
        /// <summary>A cave's mouth: the way (yaw) its ring of rock opens to a ramp down; NaN for a pit entered by a door.</summary>
        public float Mouth = float.NaN;
        /// <summary>A pocket shut in by cliffs (a cave): dark, with its own light.</summary>
        public bool Cave;
        public int Colour = 0x7E9093;
    }

    public class SpawnDef
    {
        public string Id;
        public string Enemy;
        public int Count = 1;
        public MapPoint At;
        public float Radius = 10;
        /// <summary>Seconds until the group comes back after it is wiped out (0: never).</summary>
        public float Respawn = 180;
        public string Needs = "";
        public string GoneAfter = "";
    }

    public class WaveDef
    {
        public (string enemy, int count)[] Enemies = new (string, int)[0];
        public string Shout = "";
    }

    /// <summary>A staged fight: waves that come one after another once the hero is there.</summary>
    public class EncounterDef
    {
        public string Id;
        public string Name;
        public MapPoint At;
        public float Radius = 30;
        public WaveDef[] Waves = new WaveDef[0];
    }

    public class BuildingDef
    {
        public string Model;
        public MapPoint At;
        public float Facing;
        public float Scale = 1;

        public BuildingDef(string model, float x, float z, float facing = 0, float scale = 1)
        {
            Model = model;
            At = new MapPoint(x, z);
            Facing = facing;
            Scale = scale;
        }
    }

    public class ShopEntry
    {
        public string Item;
        /// <summary>A Gu id instead of an item, sold as an ordinary Gu.</summary>
        public string Gu;
        public int Price;
        /// <summary>The chapter from which it is on the shelf.</summary>
        public int Chapter = 1;
    }
}
