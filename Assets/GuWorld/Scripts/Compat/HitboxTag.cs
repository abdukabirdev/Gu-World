using UnityEngine;

namespace GuWorld.Render
{
    /// <summary>What a collider on one of the artist's prefabs is. The prefabs in Resources/Build carry this; the game reads it to tell a tree from a wall.</summary>
    public enum HitboxKind
    {
        Other,
        Building,
        Prop,
        Tree,
        Branch,
        Person,
        Hero,
        Trigger,
        Moving,
        Ground,
    }

    public class HitboxTag : MonoBehaviour
    {
        public HitboxKind Kind;
    }
}
