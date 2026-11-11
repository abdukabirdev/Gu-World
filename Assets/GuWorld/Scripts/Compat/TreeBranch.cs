using UnityEngine;

namespace GuWorld.World
{
    /// <summary>A limb on the tree prefabs of Resources/Build (kept for the prefabs).</summary>
    public class TreeBranch : MonoBehaviour
    {
        public float Length = 8;
        public float RootThickness = 0.8f;
        public float TipThickness = 0.25f;
        public float Health = 300;
        public ClimbTree Tree;
    }
}
