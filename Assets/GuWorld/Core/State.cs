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
        public int Version = 1;
        public string HeroName = "Лань";
        /// <summary>Cultivation step: 0–3 rank 1, 4–7 rank 2, 8–11 rank 3.</summary>
        public int Step;
        public float Insight;
        /// <summary>The share of the aperture the hero's talent fills.</summary>
        public float Talent = 0.44f;
        public float Health = 100;
        public float Essence = 44;
        public int Stones = 10;
        public int NextUid = 1;
        public List<GuInstance> Gu = new List<GuInstance>();
        /// <summary>Uids of the Gu in the active slots (0: empty).</summary>
        public int[] Active = new int[4];
        public int[] Passive = new int[3];
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
        /// <summary>The hour of the day, 0–24.</summary>
        public float Hour = 9;
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
