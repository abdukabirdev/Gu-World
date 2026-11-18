using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    public enum AffixKind { Sharp, Thrifty, Quick, Far, Leech, Sturdy, Wellspring }

    [Serializable]
    public class Affix
    {
        public AffixKind Kind;
        public float Value;
    }

    /// <summary>One Gu the hero owns.</summary>
    [Serializable]
    public class GuInstance
    {
        /// <summary>Its own number in this save (slots refer to it).</summary>
        public int Uid;
        public string Def;
        /// <summary>0 ordinary, 1 rare, 2 superb.</summary>
        public int Quality;
        public List<Affix> Affixes = new List<Affix>();
        public int MasteryXp;
        /// <summary>A wild Gu obeys only once refined.</summary>
        public bool Refined;
        /// <summary>Days since it last ate (0: fed today).</summary>
        public int Hunger;
        /// <summary>A name the hero gave it, or "".</summary>
        public string Name = "";
        /// <summary>Tempering's failures so far at this quality: each makes the next try surer.</summary>
        public int Tempered;
        /// <summary>Lent in the immortal aperture: gone when the hero leaves it.</summary>
        public bool Borrowed;
    }

    [Serializable]
    public class ItemStack
    {
        public string Item;
        public int Count;
    }

    [Serializable]
    public class QuestState
    {
        public string Quest;
        public int Step;
        public int Progress;
        public bool Done;
    }

    [Serializable]
    public class SaveData
    {
        public int Version = 3;
        public string HeroName = "Лань";
        /// <summary>Cultivation step: 0–3 rank 1, 4–7 rank 2, 8–11 rank 3.</summary>
        public int Step;
        public float Insight;
        /// <summary>The share of the aperture the hero's talent fills.</summary>
        public float Talent = 0.44f;
        /// <summary>Born with the Ten Extreme Physique (the easy path's one chance in ten): talent 100%.</summary>
        public bool ExtremePhysique;
        public float Health = 100;
        public float Essence = 44;
        public int Stones = 10;
        public int NextUid = 1;
        public List<GuInstance> Gu = new List<GuInstance>();
        /// <summary>Uids of the Gu in the active slots (0: empty).</summary>
        public int[] Active = new int[6];
        public int[] Passive = new int[5];
        public List<ItemStack> Items = new List<ItemStack>();
        public List<QuestState> Quests = new List<QuestState>();
        public List<string> Flags = new List<string>();
        public List<string> Recipes = new List<string>();
        /// <summary>Chests opened and one-time things used.</summary>
        public List<string> Used = new List<string>();
        public string Lantern = "lantern_village";
        public float X;
        public float Z = -12;
        public float PlayedSeconds;
        public int Kills;
        public int Deaths;
        /// <summary>How many times the hero was reborn an arc back (the rebirth death rule).</summary>
        public int Rebirths;
        /// <summary>The body's traits as trained (Session.Body): experience by trait.</summary>
        public List<TraitXp> Physique = new List<TraitXp>();
        /// <summary>The wounds the hero carries, by part of the body.</summary>
        public List<Wound> Wounds = new List<Wound>();
        /// <summary>Wild Gu's spots emptied (caught or fled): the spot's key and the day another settles there.</summary>
        public List<Tally> WildTaken = new List<Tally>();
        /// <summary>The hour of the day, 0–24.</summary>
        public float Hour = 9;

        // ---------------------------------------------------------------- The wider world (version 2)

        /// <summary>The day of the game, from 1: Gu get hungry, boards change their errands, shrines bless again.</summary>
        public int Day = 1;
        /// <summary>What each clan thinks of the hero.</summary>
        public List<Reputation> Reputation = new List<Reputation>();
        /// <summary>The pages of the codex opened.</summary>
        public List<string> Codex = new List<string>();
        public List<string> Achievements = new List<string>();
        /// <summary>The lanterns lit (the hero can travel between them).</summary>
        public List<string> Lanterns = new List<string>();
        /// <summary>The pill recipes known.</summary>
        public List<string> Pills = new List<string>();
        /// <summary>The refiner's skill (1–5) and the batches made towards the next level.</summary>
        public int Refining = 1;
        public int Refined;
        public int PillsMade;
        public int Fusions;
        public int Bosses;
        public List<Tally> KillsBy = new List<Tally>();
        public List<BountyState> Bounties = new List<BountyState>();
        /// <summary>Lasting effects of pills and blessings, with the seconds left.</summary>
        public List<Effect> Effects = new List<Effect>();
        /// <summary>The shrines prayed at today ("shrine_id").</summary>
        public List<string> Prayed = new List<string>();
        public Settings Settings = new Settings();
        /// <summary>The killer moves the hero has devised or been taught, and the one ready on V.</summary>
        public List<string> Moves = new List<string>();
        public string Move = "";
        public int MovesCast;
        /// <summary>The day the Spring Autumn Cicada wakes again after saving the hero.</summary>
        public int CicadaDay;
        /// <summary>Attainment on each path (Id: the path's name, Count: its points).</summary>
        public List<Tally> Attainment = new List<Tally>();
        /// <summary>Elites the hero has felled.</summary>
        public int Elites;
        /// <summary>Blows parried.</summary>
        public int Parries;
        /// <summary>Reactions of statuses the hero has set off, and the kinds of them seen.</summary>
        public int Reactions;
        public List<string> ReactionsSeen = new List<string>();
        /// <summary>Wolf tides held.</summary>
        public int Tides;
        public int Raids;
        /// <summary>Merit with the clan: what is left to spend, and all ever earned (the clan rank).</summary>
        public int Merit;
        public int MeritEarned;
        /// <summary>Bouts at the academy: the day of the last, how many that day, how many won in all.</summary>
        public int SparDay;
        public int SparsToday;
        public int SparsWon;
        /// <summary>Hints already shown.</summary>
        public List<string> Hints = new List<string>();
        /// <summary>The houses the hero owns (thing ids), what lies in their store, the Gu kept in their nursery.</summary>
        public List<string> Homes = new List<string>();
        public List<ItemStack> Stash = new List<ItemStack>();
        public List<int> Nursery = new List<int>();
    }

    [Serializable]
    public class Reputation
    {
        public string Clan;
        public int Value;
    }

    [Serializable]
    public class Tally
    {
        public string Id;
        public int Count;
    }

    /// <summary>A lasting effect on the hero: a buff (pills) or a stat (pills, blessings), with its value and the seconds left.</summary>
    [Serializable]
    public class Effect
    {
        public string Source;
        public string Name;
        public BuffKind Buff;
        public StatKind Stat;
        /// <summary>Set when the effect changes a stat (otherwise it is a buff).</summary>
        public bool IsStat;
        public float Value;
        public float Seconds;
    }

    /// <summary>An errand from a settlement's board, made from the day and the settlement (so the same day gives the same errands).</summary>
    [Serializable]
    public class BountyState
    {
        /// <summary>"bounty:settlement:day:n".</summary>
        public string Id;
        public string Settlement;
        public string Kind;
        public string Target;
        public int Count;
        public int Progress;
        public int Stones;
        public int Insight;
        public int Reputation;
        public bool Taken;
        public bool Done;
        public int Day;
    }

    public enum HungerRule { Off, Soft, Canon }

    /// <summary>
    /// The way the hero begins (main's "path"), which is the game's difficulty: the talent the hero is
    /// born with and how hard the mountain is. Saved as a number — never reorder (saves before version 3
    /// had Story 0, Normal 1, Hard 2, Demonic 3; Migrate moves Demonic to Impossible).
    /// </summary>
    public enum Difficulty { Easy, Normal, Hard, Unbearable, Impossible }

    /// <summary>The body's traits (main's physique, improved): each grows from doing it.</summary>
    public enum Trait { Strength, Agility, Endurance, Toughness }

    [Serializable]
    public class TraitXp
    {
        public Trait Trait;
        public float Xp;
    }

    public enum BodyPart { Head, Torso, LeftArm, RightArm, LeftLeg, RightLeg }

    public enum WoundKind { Bruise, Cut, Burn, Fracture }

    /// <summary>A wound on a part of the body: how bad (1–3) and how many hours of the game until it heals.</summary>
    [Serializable]
    public class Wound
    {
        public BodyPart Part;
        public WoundKind Kind;
        public int Severity = 1;
        public float Hours;
    }

    /// <summary>
    /// What a fall costs (chosen at a new game, kept for the whole playthrough): wake at the lantern
    /// with a share of the stones lost; be reborn at the start of the arc before (the story's chapters),
    /// keeping only memories; or the hardcore rule — one life for the whole playthrough.
    /// </summary>
    public enum DeathRule { Lantern, Rebirth, Hardcore }

    /// <summary>The player's choices for how the game plays.</summary>
    [Serializable]
    public class Settings
    {
        public HungerRule Hunger = HungerRule.Soft;
        public Difficulty Difficulty = Difficulty.Normal;
        /// <summary>The path the hero was born on (fixed at a new game; the difficulty may be lowered below it, not raised above it).</summary>
        public Difficulty Born = Difficulty.Normal;
        /// <summary>What a fall costs (fixed for the playthrough, like the path).</summary>
        public DeathRule Death = DeathRule.Lantern;
        /// <summary>Gu eat from the bag by themselves at dawn.</summary>
        public bool AutoFeed = true;
        /// <summary>Travelling between lanterns costs primeval stones.</summary>
        public bool TravelCosts = true;
    }

    /// <summary>What a kill or a chest gives.</summary>
    public class Loot
    {
        public int Stones;
        public List<ItemStack> Items = new List<ItemStack>();
        public List<GuInstance> Gu = new List<GuInstance>();

        public bool Empty => Stones == 0 && Items.Count == 0 && Gu.Count == 0;
    }
}
