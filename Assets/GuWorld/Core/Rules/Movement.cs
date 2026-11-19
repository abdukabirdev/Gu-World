using System;
using System.Collections.Generic;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core
{
    /// <summary>How the hero carries their body (ROADMAP 12.3). Not saved: a loaded game stands.</summary>
    public enum Posture { Stand, Crouch, Crawl }

    /// <summary>What the hands and legs do with an edge before the hero (ROADMAP 12.3).</summary>
    public enum LedgeMove
    {
        /// <summary>Nothing to do: a step the legs take, or nothing there.</summary>
        None,
        /// <summary>A low wall with ground beyond: over it, a hand on its top.</summary>
        Vault,
        /// <summary>A low ledge with room on it: up onto it.</summary>
        StepUp,
        /// <summary>A ledge at the chest or the head: hands on it, and up.</summary>
        PullUp,
        /// <summary>In the air, a ledge within the hands' reach: caught, and hung from.</summary>
        Grab,
        /// <summary>Too high to reach standing: jump and catch it, or run up the wall.</summary>
        TooHigh,
    }

    /// <summary>
    /// The rules of the hero's body beyond the fight (ROADMAP 12.3, 12.6): crouching and crawling
    /// (slower, quieter, lower, seen later), falls (the drop a body takes unhurt and what more costs:
    /// health, then the legs), climbing (stamina by the second, the hands let go when it runs out),
    /// ledges and walls (how high the hands catch, what a run up a wall costs), the limbs of a tree
    /// (a thick one bears the hero, a thin one creaks and breaks), the beasts that cannot follow up a
    /// tree, and the view and the nest at the top of a great one.
    /// </summary>
    public static class Movement
    {
        static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

        // ------------------------------------------------------------------ Posture

        /// <summary>The share of the run's speed.</summary>
        public static float Speed(Posture posture) => posture == Posture.Crouch ? 0.3f : posture == Posture.Crawl ? 0.14f : 1f;

        /// <summary>How fast the crouched walk and the crawl go over the ground at their clips' own pace, m/s (anim.py measure): the clips are played faster or slower to match.</summary>
        public const float CrouchClipSpeed = 1.02f, CrawlClipSpeed = 0.48f;

        /// <summary>The body's height, metres (the hero's capsule; crawling, no lower than its width).</summary>
        public static float Height(Posture posture) => posture == Posture.Crouch ? 1.38f : posture == Posture.Crawl ? 0.66f : 1.74f;

        /// <summary>The eyes' height against a standing hero's.</summary>
        public static float Eyes(Posture posture) => posture == Posture.Crouch ? 0.8f : posture == Posture.Crawl ? 0.32f : 1f;

        /// <summary>How loud the steps are, against walking upright.</summary>
        public static float Steps(Posture posture) => posture == Posture.Crouch ? 0.35f : posture == Posture.Crawl ? 0.2f : 1f;

        /// <summary>How far a beast that has not noticed the hero yet sees them, against standing (the night and the weather come on top).</summary>
        public static float Seen(Posture posture) => posture == Posture.Crouch ? 0.55f : posture == Posture.Crawl ? 0.35f : 1f;

        /// <summary>Only an upright hero sprints, strikes in full, guards and jumps; a key for any of them stands them up first.</summary>
        public static bool Upright(Posture posture) => posture == Posture.Stand;

        // ------------------------------------------------------------------ Falls

        /// <summary>A drop the body takes unhurt, metres.</summary>
        public const float SafeFall = 4.5f;

        /// <summary>A fall's share of the most health: none up to the safe drop, a tenth at about 9 m, all of it from about 30 m.</summary>
        public static float FallShare(float drop) => drop <= SafeFall ? 0 : (float)Math.Min(1.5, Math.Pow((drop - SafeFall) / 25.0, 1.4));

        /// <summary>What a fall does to a leg: a bruise from 7 m, a worse one from 12, a break from 17, a bad break from 24 (on the easy path, only a bruise).</summary>
        public static (WoundKind kind, int severity)? FallWound(float drop, Difficulty path)
        {
            if (drop < 7)
            {
                return null;
            }

            if (path == Difficulty.Easy)
            {
                return (WoundKind.Bruise, 1);
            }

            if (drop >= 17)
            {
                return (WoundKind.Fracture, drop >= 24 ? 3 : 2);
            }

            return (WoundKind.Bruise, drop >= 12 ? 2 : 1);
        }

        // ------------------------------------------------------------------ Climbing

        /// <summary>Stamina a second on the bark or hanging from a ledge: holding still, climbing, scrambling.</summary>
        public const float HoldCost = 2.5f, ClimbCost = 7, ScrambleCost = 14;

        /// <summary>Metres a second up the bark, climbing and scrambling.</summary>
        public const float ClimbSpeed = 3.2f, ScrambleSpeed = 5.2f;

        public static float Cost(bool moving, bool fast) => !moving ? HoldCost : fast ? ScrambleCost : ClimbCost;

        /// <summary>A pull up over a ledge; a run up a wall.</summary>
        public const float PullUpCost = 10, WallRunCost = 22;

        // ------------------------------------------------------------------ Ledges and walls

        /// <summary>Above the feet, standing: what is stepped (below the vault), vaulted or stepped onto, pulled up onto.</summary>
        public const float VaultLow = 0.45f, VaultHigh = 1.3f, PullUpHigh = 2.3f;

        /// <summary>In the air: a ledge this high above the feet is caught by the hands.</summary>
        public const float GrabLow = 1.3f, GrabHigh = 2.25f;

        /// <summary>Hanging: the feet this far below the ledge's top (the hands on it; the baked "hang" pose, Tools/V2/models/anim.py measure).</summary>
        public const float HangDrop = 1.75f;

        /// <summary>Hanging: how far the body hangs off the wall (its axis), metres.</summary>
        public const float HangOff = 0.18f;

        /// <summary>The vault's tuck: the soles this high above the body's root at the top of the hop (anim.py measure).</summary>
        public const float VaultTuck = 0.47f;

        /// <summary>A run up a wall: how high it carries, in how long.</summary>
        public const float WallRunRise = 2.3f, WallRunSeconds = 0.5f;

        /// <summary>The deepest drop beyond a low wall that is still vaulted (deeper: stepped onto instead).</summary>
        public const float VaultDrop = 2.5f;

        /// <summary>What to do with an edge whose top is this high above the feet (deep: there is room to stand on it).</summary>
        public static LedgeMove Over(float top, bool grounded, bool deep)
        {
            if (!grounded)
            {
                return top >= GrabLow && top <= GrabHigh ? LedgeMove.Grab : LedgeMove.None;
            }

            if (top < VaultLow)
            {
                return LedgeMove.None;
            }

            if (top <= VaultHigh)
            {
                return deep ? LedgeMove.StepUp : LedgeMove.Vault;
            }

            return top <= PullUpHigh ? LedgeMove.PullUp : LedgeMove.TooHigh;
        }

        /// <summary>
        /// The pull up, as the "pullup" clip has it (anim.py): by its time (0–1), the share of the rise
        /// done and of the way in over the edge — the arms pull the chest to the edge, press, a knee
        /// comes up onto it, the body stands.
        /// </summary>
        static readonly float[,] PullKeys = { { 0, 0, 0 }, { 0.3f, 0.31f, 0 }, { 0.45f, 0.51f, 0 }, { 0.65f, 0.72f, 0.15f }, { 0.85f, 0.95f, 0.6f }, { 1, 1, 1 } };

        /// <summary>The pull up's rise and way in (shares) at a time of it.</summary>
        public static (float rise, float inward) PullUp(float t) => Keyed(PullKeys, t);

        /// <summary>
        /// Where in the pull up (its time, 0–1) a hero begins whose hands are on a ledge this high above
        /// their feet: from a hang, at its start; from lower, part of the rise is done already — the pose
        /// keeps the hands on the top either way.
        /// </summary>
        public static float PullUpStart(float top)
        {
            var done = Clamp01(1 - top / HangDrop);
            float low = 0, high = 1;

            for (var i = 0; i < 24; i++)
            {
                var mid = (low + high) / 2;

                if (PullUp(mid).rise < done)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return (low + high) / 2;
        }

        /// <summary>The vault, as the "vault" clip has it: the rise to the top and the way over (shares; the rise goes past 1 at the top and comes back to the far side's ground).</summary>
        static readonly float[,] VaultKeys = { { 0, 0, 0 }, { 0.35f, 0.85f, 0.3f }, { 0.55f, 1, 0.55f }, { 0.8f, 0.6f, 0.85f }, { 1, 0, 1 } };

        /// <summary>
        /// A vault's height above the line from start to end (a share of the climb to the top) and the way over, at a time of it.
        /// Stepping onto a deep ledge ends on the top, so its rise holds at 1 (see Hero.Mantle).
        /// </summary>
        public static (float rise, float across) Vault(float t) => Keyed(VaultKeys, t);

        static (float, float) Keyed(float[,] keys, float t)
        {
            t = Clamp01(t);

            for (var i = 1; i < keys.GetLength(0); i++)
            {
                if (t <= keys[i, 0])
                {
                    var span = keys[i, 0] - keys[i - 1, 0];
                    var u = span > 0 ? (t - keys[i - 1, 0]) / span : 1;
                    u = u * u * (3 - 2 * u);

                    return (keys[i - 1, 1] + (keys[i, 1] - keys[i - 1, 1]) * u, keys[i - 1, 2] + (keys[i, 2] - keys[i - 1, 2]) * u);
                }
            }

            return (keys[keys.GetLength(0) - 1, 1], keys[keys.GetLength(0) - 1, 2]);
        }

        // ------------------------------------------------------------------ The limbs of a tree

        /// <summary>A limb this thick (its radius, metres) bears the hero; a thinner one creaks and breaks.</summary>
        public const float BearRadius = 0.11f;

        /// <summary>The thinnest limb a tree's solid keeps (TreeGen: thinner is not stood on at all).</summary>
        public const float ThinnestLimb = 0.07f;

        public static bool Bears(float radius) => radius >= BearRadius;

        /// <summary>Seconds a thin limb holds the hero before it breaks: half a second at the thinnest, two at the edge of bearing.</summary>
        public static float CreakSeconds(float radius) => 0.5f + 1.5f * Clamp01((radius - ThinnestLimb) / (BearRadius - ThinnestLimb));

        /// <summary>How much a limb sways under the hero, 0–1 (the view trembles): none from 0.3 m of radius.</summary>
        public static float Sway(float radius) => Clamp01((0.3f - radius) / 0.23f);

        /// <summary>
        /// The climbable limb nearest a point in the tree's own space (TreeModel.Climb): its depth (0 the
        /// trunk), its radius there, and how far the point is from its bark (below 0: inside). False if
        /// the tree has no limbs to climb.
        /// </summary>
        public static bool NearestLimb(IReadOnlyList<TreeLimb> limbs, V3 point, out float radius, out int depth, out float gap)
        {
            radius = 0;
            depth = -1;
            gap = float.MaxValue;

            if (limbs == null)
            {
                return false;
            }

            foreach (var limb in limbs)
            {
                var points = limb.Points;
                var radii = limb.Radii;

                for (var i = 0; i + 1 < points.Length; i++)
                {
                    var a = points[i];
                    var along = points[i + 1] - a;
                    var length2 = V3.Dot(along, along);
                    var t = length2 > 1e-8f ? Math.Max(0, Math.Min(1, V3.Dot(point - a, along) / length2)) : 0;
                    var r = radii[i] + (radii[i + 1] - radii[i]) * t;
                    var d = (point - (a + along * t)).Length - r;

                    if (d < gap)
                    {
                        gap = d;
                        radius = r;
                        depth = limb.Depth;
                    }
                }
            }

            return depth >= 0;
        }

        // ------------------------------------------------------------------ Up a tree

        /// <summary>Above the ground this far, the hero is out of reach of a beast that does not fly, shoot or cast.</summary>
        public const float OutOfReach = 2.6f;

        /// <summary>Seconds such a beast waits below before it gives up and goes home.</summary>
        public const float WaitBelow = 6;

        /// <summary>Whether a beast can still fight a hero up a tree (or on a roof): fliers, shooters, casters and the great ones.</summary>
        public static bool ReachesUp(EnemyDef enemy) => enemy.Boss || enemy.Mind == EnemyMind.Swarm || enemy.Mind == EnemyMind.Shooter || enemy.Mind == EnemyMind.Caster;

        /// <summary>A tree tall enough to have a top worth the climb, metres.</summary>
        public const float GreatTree = 30;

        /// <summary>The hero is at the top of a great tree: this high above its foot.</summary>
        public static bool AtTop(float above, float treeHeight) => treeHeight >= GreatTree && above >= treeHeight * 0.72f;

        /// <summary>How far one sees from the top of a tree this tall, metres (on foot: Session.Sight).</summary>
        public static float TopSight(float treeHeight) => 300 + 6 * treeHeight;

        /// <summary>What the top of a great tree holds, once: stones by its height, now and then wild honey, a nugget a bird brought, high tea from a tall one.</summary>
        public static List<(string item, int count)> Nest(Random random, float treeHeight)
        {
            var find = new List<(string, int)> { ("stones", 3 + (int)(treeHeight / 10) + random.Next(0, 4)) };

            if (random.NextDouble() < 0.35)
            {
                find.Add(("honey", 1 + random.Next(0, 2)));
            }

            if (random.NextDouble() < 0.2)
            {
                find.Add(("rock_1", 1));
            }

            if (treeHeight >= 60 && random.NextDouble() < 0.3)
            {
                find.Add(("cloud_tea", 1));
            }

            return find;
        }

        /// <summary>A tree's key (its place, to the metre) among the tops already reached.</summary>
        public static string TreeKey(float x, float z) => $"{(int)Math.Round(x)}:{(int)Math.Round(z)}";
    }
}
