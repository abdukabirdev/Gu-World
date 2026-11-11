using UnityEngine;

namespace GuWorld
{
    /// <summary>Calls whose form changed between Unity versions: the game is built with Unity 6, the cloud's compile check uses 2021.3's reference assemblies.</summary>
    public static class Engine
    {
        public static T[] FindAll<T>() where T : Object
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindObjectsByType<T>();
#else
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#endif
        }

        /// <summary>A number that tells one object from another while the game runs (for caches' keys).</summary>
        public static int Key(Object thing) => thing == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(thing);
    }
}
