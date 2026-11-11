using UnityEngine;

namespace GuWorld.World
{
    /// <summary>The trunk's shape on the tree prefabs of Resources/Build (kept for the prefabs; this game does not climb).</summary>
    public class ClimbTree : MonoBehaviour
    {
        public float BaseRadius = 2;
        public float TopRadius = 0.8f;
        public float ClimbHeight = 30;
        public TreeBranch[] Branches = new TreeBranch[0];
    }
}
