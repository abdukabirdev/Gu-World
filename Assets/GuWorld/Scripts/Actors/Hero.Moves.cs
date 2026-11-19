using GuWorld.Core;
using GuWorld.Fx;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// The hero's body beyond the fight (ROADMAP 12.3, 12.6): crouching and crawling; ledges caught in the
    /// air, hung from and pulled up onto; low walls hopped over; a run up a wall; the thin limbs of a tree,
    /// that break; the tops of great trees; and the falls. The numbers are Core's (Movement); a move over
    /// an edge carries the body along the path its clip was made to (Tools/V2/models/anim.py), so the hands
    /// stay on the edge and nothing is put anywhere at once.
    /// </summary>
    public partial class Hero
    {
        // ------------------------------------------------------------------ Posture

        /// <summary>Standing, crouched or crawling (ROADMAP 12.3).</summary>
        public Posture Posture { get; private set; }

        /// <summary>The eyes' height against standing, eased: the camera sinks as the hero crouches.</summary>
        public float EyeFactor { get; private set; } = 1;

        /// <summary>Up a tree, on its bark or its limbs, well off the ground: a beast that cannot climb, fly, shoot or cast cannot reach them.</summary>
        public bool OutOfReach { get; private set; }

        /// <summary>Hanging from a ledge by the hands.</summary>
        public bool Hanging => mode == Mode.Hanging;

        /// <summary>The crouch and crawl keys: each turns its posture on and off; or (a setting) crouched only while the key is held.</summary>
        void Postures()
        {
            var want = Posture;

            if (Prefs.CrouchHold)
            {
                var held = Bindings.Held("crouch");

                if (held && want == Posture.Stand)
                {
                    want = Posture.Crouch;
                }
                else if (!held && want == Posture.Crouch)
                {
                    // Let go under something low: still crouched, quietly, until there is room.
                    SetPosture(Posture.Stand);
                    want = Posture;
                }
            }
            else if (Bindings.Down("crouch"))
            {
                want = want == Posture.Crouch ? Posture.Stand : Posture.Crouch;
            }

            if (Bindings.Down("crawl"))
            {
                want = want == Posture.Crawl ? Posture.Stand : Posture.Crawl;
            }

            if (want != Posture)
            {
                ChangePosture(want);
            }
        }

        /// <summary>Into a posture, or as near as there is room for (a crawl that cannot stand crouches), with a word if it could not.</summary>
        bool ChangePosture(Posture want, bool quiet = false)
        {
            if (SetPosture(want))
            {
                return true;
            }

            if (want == Posture.Stand && Posture == Posture.Crawl)
            {
                SetPosture(Posture.Crouch);
            }

            if (!quiet)
            {
                Note("Здесь не выпрямиться");
            }

            return false;
        }

        /// <summary>Upright for what needs it (a jump, a sprint, a guard, a blow): true if the hero stands now.</summary>
        bool Rise(bool quiet = false) => Posture == Posture.Stand || ChangePosture(Posture.Stand, quiet);

        /// <summary>The body made as tall as a posture, if there is room above the head for it.</summary>
        bool SetPosture(Posture next)
        {
            if (next == Posture)
            {
                return true;
            }

            if (body == null)
            {
                return false;
            }

            var height = Movement.Height(next);

            if (height > body.height + 0.01f && !Headroom(height))
            {
                return false;
            }

            Resize(height);
            Posture = next;

            return true;
        }

        /// <summary>Standing again at once, whatever is overhead (a revival, a new place).</summary>
        void Stand()
        {
            if (body != null)
            {
                Resize(Movement.Height(Posture.Stand));
            }

            Posture = Posture.Stand;
        }

        void Resize(float height)
        {
            body.height = Mathf.Max(height, body.radius * 2);
            body.center = new Vector3(0, body.height * 0.5f + 0.02f, 0);
            Height = height;
        }

        /// <summary>Nothing but air above the head, up to a height (the body as it is is known to fit).</summary>
        bool Headroom(float height)
        {
            var radius = body.radius * 0.92f;
            var from = transform.position + Vector3.up * (body.height + 0.02f - body.radius);
            var to = transform.position + Vector3.up * (height + 0.02f - body.radius);

            if (to.y <= from.y)
            {
                return true;
            }

            foreach (var hit in Physics.OverlapCapsule(from, to, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit != body && hit.GetComponentInParent<Actor>() == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>A body this tall fits standing at a point (nothing solid but the ground under it).</summary>
        static bool Fits(Vector3 feet, float height, float radius = 0.3f)
        {
            var low = feet + Vector3.up * (radius + 0.06f);
            var high = feet + Vector3.up * Mathf.Max(radius + 0.06f, height - radius);

            foreach (var hit in Physics.OverlapCapsule(low, high, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.GetComponentInParent<Actor>() == null)
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------------ Falls

        float airPeak;
        bool inAir;
        Vector3 lastAt;

        /// <summary>The highest point of a time in the air; on landing, the drop from it is paid for (ROADMAP 12.6).</summary>
        void TrackFall()
        {
            var at = transform.position;
            // Put somewhere else at once (a door, a blink, the net under the world): no fall.
            var moved = (at - lastAt).sqrMagnitude > 16;
            lastAt = at;

            if (moved || mode == Mode.Climbing || mode == Mode.Hanging || mode == Mode.Mantling || mode == Mode.WallRunning || mode == Mode.Dead || body == null || !body.enabled)
            {
                airPeak = at.y;
                inAir = false;

                return;
            }

            if (!Grounded)
            {
                airPeak = Mathf.Max(airPeak, at.y);
                inAir = true;

                return;
            }

            if (inAir)
            {
                inAir = false;
                var drop = airPeak - at.y;

                if (drop > Movement.SafeFall)
                {
                    Dropped(drop);
                }
                else if (drop > 1.5f)
                {
                    Sound.AudioDirector.Play("land", transform.position, Mathf.Clamp01(drop / 6), 0.1f);
                }
            }

            airPeak = at.y;
        }

        /// <summary>A fall paid for: into water, nothing; onto anything else, health by the drop and from 7 m a leg.</summary>
        void Dropped(float drop)
        {
            if (World.Ground.StepSound(transform.position) == "step_water")
            {
                Sound.AudioDirector.Play("step_water", transform.position, 0.9f, 0.1f);

                return;
            }

            Sound.AudioDirector.Play("land", transform.position, 1, 0.1f);
            var damage = Session.Fall(drop);

            if (damage <= 0)
            {
                return;
            }

            Sound.AudioDirector.Play("hurt", transform.position, Mathf.Clamp(0.5f + damage / Session.Stats.MaxHealth * 3, 0.5f, 1));
            Game.Ui?.DamageNumber(transform.position + Vector3.up * 2, damage, false, true);
            Game.Ui?.Hurt();
            Game.Shake(Mathf.Clamp(damage / Session.Stats.MaxHealth * 2.5f, 0.15f, 0.7f));

            if (Session.Dead)
            {
                Die();

                return;
            }

            SetMode(Mode.Hurt);
            Animate("hit", 0.05f, 1, true);
        }

        // ------------------------------------------------------------------ Trees: the limbs underfoot, the tops

        /// <summary>The tree the hero is on (its bark or its limbs), for its top and for the beasts below.</summary>
        World.TreeSolid treeHere;
        float limbWait, limbRadius, creak, creakSound, topWait;
        Collider limbCollider, brokenFrom;
        float brokenWait;

        /// <summary>
        /// What is underfoot, five times a second: a tree's limb (how thick), its trunk or roots, or the ground.
        /// A thin limb creaks and breaks; the top of a great tree gives its view and its nest once.
        /// </summary>
        void Underfoot(float dt)
        {
            // The tree a limb broke from is solid again once the hero is down, or after a while.
            if (brokenFrom != null)
            {
                brokenWait -= dt;

                if ((Grounded && brokenWait < 3.2f) || brokenWait <= 0)
                {
                    if (body != null)
                    {
                        Physics.IgnoreCollision(body, brokenFrom, false);
                    }

                    brokenFrom = null;
                }
            }

            if (mode != Mode.Climbing && mode != Mode.Hanging && mode != Mode.Mantling)
            {
                limbWait -= dt;

                if (limbWait <= 0)
                {
                    limbWait = 0.2f;
                    treeHere = null;
                    limbRadius = 0;
                    limbCollider = null;

                    if (Grounded && Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 0.8f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var tree = hit.collider.GetComponentInParent<World.TreeSolid>();

                        if (tree != null)
                        {
                            treeHere = tree;

                            if (tree.LimbAt(hit.point, out var radius, out var depth) && depth > 0)
                            {
                                limbRadius = radius;
                                limbCollider = hit.collider;
                            }
                        }
                    }
                }

                // A limb too thin to bear the hero creaks under them, and breaks.
                if (limbRadius > 0 && !Movement.Bears(limbRadius) && Grounded && (mode == Mode.Free || mode == Mode.Sitting))
                {
                    creak += dt;
                    creakSound -= dt;

                    if (creakSound <= 0)
                    {
                        creakSound = 0.45f;
                        Sound.AudioDirector.Play("creak", transform.position, 0.7f, 0.15f);
                        Game.Shake(0.05f * Movement.Sway(limbRadius));
                    }

                    if (creak >= Movement.CreakSeconds(limbRadius))
                    {
                        Break();
                    }
                }
                else
                {
                    creak = 0;
                }
            }

            // The top of a great tree: the land far round, and what the birds keep up there (once a tree).
            topWait -= dt;

            if (treeHere != null && topWait <= 0 && Movement.AtTop(transform.position.y - treeHere.transform.position.y, treeHere.Height))
            {
                topWait = 0.5f;
                var foot = treeHere.transform.position;
                var words = Session.Treetop(treeHere.Key, foot.x, foot.z, treeHere.Height);

                if (words != null)
                {
                    Game.Ui?.Banner("Вершина великого дерева", $"{treeHere.Height:0} м над землёй");
                    Game.Ui?.Toast(words);
                    Sound.AudioDirector.Play("quest", null, 0.7f, 0);
                }
            }
        }

        /// <summary>The limb gives way: the hero falls through the tree (its solid is one piece) to whatever is below.</summary>
        void Break()
        {
            creak = 0;

            if (body == null || limbCollider == null)
            {
                return;
            }

            Physics.IgnoreCollision(body, limbCollider, true);
            brokenFrom = limbCollider;
            brokenWait = 4;
            Sound.AudioDirector.Play("crack", transform.position, 1, 0.1f);
            Effects.Burst(transform.position + Vector3.up * 0.15f, new Color(0.45f, 0.32f, 0.2f), 1.3f, 0.45f);
            Game.Shake(0.2f);
            Note("Ветка не выдержала");
            limbRadius = 0;
            limbCollider = null;
            treeHere = null;
            fallSpeed = 0;
        }

        // ------------------------------------------------------------------ Edges: what is before the hero

        /// <summary>An edge before the hero: a wall's face, its top, and what lies on and beyond it.</summary>
        struct Edge
        {
            /// <summary>The top's surface, a hand's breadth in from the face.</summary>
            public Vector3 Top;
            /// <summary>The face's outward normal, flat.</summary>
            public Vector3 Out;
            /// <summary>The edge itself: the face at the top's height.</summary>
            public Vector3 Lip;
            /// <summary>Where the hero stands on the top once over it (when it is deep enough for them).</summary>
            public Vector3 Stand;
            public bool Deep;
            /// <summary>For a thin wall: the ground beyond it.</summary>
            public Vector3 Beyond;
            public bool HasBeyond;
        }

        static readonly float[] Probes = { 0.3f, 0.9f, 1.5f };

        /// <summary>A wall, not a beast or a tree (a tree's bark is climbed instead).</summary>
        static bool Wall(Collider collider) => collider != null && collider.GetComponentInParent<Actor>() == null && collider.GetComponentInParent<World.TreeSolid>() == null;

        /// <summary>
        /// Looks for an edge before a body standing at a point: a wall within reach (probed at the knees, the
        /// chest and the head), its top no higher than asked, nothing over it, and room on it to stand (deep)
        /// or ground not far below beyond it (thin). False if there is no such edge.
        /// </summary>
        bool FindEdge(Vector3 feet, Vector3 toward, float highest, out Edge edge)
        {
            edge = default;
            toward.y = 0;

            if (toward.sqrMagnitude < 0.001f)
            {
                return false;
            }

            toward.Normalize();
            var reach = Radius + 0.75f;
            RaycastHit face = default;
            var found = false;

            foreach (var height in Probes)
            {
                if (Physics.Raycast(feet + Vector3.up * height, toward, out face, reach, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(face.normal.y) < 0.4f && Wall(face.collider))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            var outward = face.normal;
            outward.y = 0;

            if (outward.sqrMagnitude < 0.01f || Vector3.Dot(-outward.normalized, toward) < 0.5f)
            {
                return false;
            }

            outward.Normalize();
            // The top: straight down onto it, a hand's breadth in from the face.
            var from = face.point - outward * 0.12f;
            from.y = feet.y + highest + 0.4f;

            if (!Physics.Raycast(from, Vector3.down, out var top, highest + 0.15f, ~0, QueryTriggerInteraction.Ignore) || top.normal.y < 0.7f || !Wall(top.collider))
            {
                return false;
            }

            // An edge, not a wall going on: nothing just over the top (a wall higher up, a beam, eaves).
            var over = new Vector3(feet.x, top.point.y + 0.3f, feet.z);

            if (Physics.Raycast(over, toward, reach + 0.3f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(top.point + Vector3.up * 0.05f, Vector3.up, 1.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            var lip = new Vector3(face.point.x, top.point.y, face.point.z);
            edge = new Edge { Top = top.point, Out = outward, Lip = lip, Stand = lip - outward * (Radius + 0.16f) };

            // Deep: the top goes on far enough in to stand on, with room above it for a crouch at least.
            if (Physics.Raycast(edge.Stand + Vector3.up * 0.5f, Vector3.down, out var floor, 0.8f, ~0, QueryTriggerInteraction.Ignore) && floor.normal.y > 0.7f
                && Mathf.Abs(floor.point.y - top.point.y) < 0.3f && Fits(floor.point, Movement.Height(Posture.Crouch)))
            {
                edge.Stand = floor.point;
                edge.Deep = true;
            }
            else
            {
                // Thin: the ground beyond it, not too far down, with room to land.
                var past = lip - outward * 1.1f + Vector3.up * 0.3f;

                if (Physics.Raycast(past, Vector3.down, out var beyond, Movement.VaultDrop + 0.6f, ~0, QueryTriggerInteraction.Ignore) && beyond.normal.y > 0.6f
                    && beyond.point.y < top.point.y - 0.25f && Fits(beyond.point, Movement.Height(Posture.Stand)))
                {
                    edge.Beyond = beyond.point;
                    edge.HasBeyond = true;
                }
            }

            return edge.Deep || edge.HasBeyond;
        }

        /// <summary>
        /// On the ground, at an edge (the jump key): a low wall hopped over, a low ledge stepped up onto, one at
        /// the chest or the head pulled up onto; running at a high wall, up it. False if there is nothing to do.
        /// </summary>
        bool TryLedge(Vector3 toward)
        {
            if (FindEdge(transform.position, toward, Movement.PullUpHigh, out var edge))
            {
                switch (Movement.Over(edge.Top.y - transform.position.y, true, edge.Deep))
                {
                    case LedgeMove.Vault:
                        return edge.HasBeyond && Vault(edge);
                    case LedgeMove.StepUp:
                    case LedgeMove.PullUp:
                        return edge.Deep && PullUp(edge, true);
                }
            }

            return Sprinting && TryWallRun(toward);
        }

        // ------------------------------------------------------------------ Over an edge: the pull up and the vault

        Vector3 mantleFrom, mantleHang, mantleTo;
        float mantleStart, mantleSeconds, mantleTop;
        bool mantleVault;
        Posture mantleEnd;

        /// <summary>
        /// Up onto a ledge with the hands on its top: from a hang, the whole pull; from lower, the part of it
        /// left (the clip begins where the hands are on the top). It costs a little stamina, unless asked not to.
        /// </summary>
        bool PullUp(Edge edge, bool costs)
        {
            if (costs && !Session.Godmode && !Session.SpendStamina(Movement.PullUpCost))
            {
                Note("Нет сил подтянуться");

                return false;
            }

            mantleStart = Movement.PullUpStart(edge.Stand.y - transform.position.y);
            mantleTop = edge.Stand.y;
            mantleFrom = transform.position;
            mantleHang = edge.Lip + edge.Out * Movement.HangOff;
            mantleTo = edge.Stand;
            mantleVault = false;
            mantleSeconds = Mathf.Max(0.15f, (1 - mantleStart) * ClipLength("pullup", 0.95f));
            mantleEnd = Fits(edge.Stand, Movement.Height(Posture.Stand)) ? Posture.Stand : Posture.Crouch;
            transform.rotation = Quaternion.LookRotation(-edge.Out);
            BeginMantle("pullup", 0.1f, mantleStart);
            Session.SetFlag("climbed");

            return true;
        }

        /// <summary>From the bark up onto what the climb came to (a limb's back, a stump's top): the pull up's end, free.</summary>
        bool ClimbOnto(Vector3 top)
        {
            var outward = climbNormal;
            outward.y = 0;

            if (outward.sqrMagnitude < 0.01f || !Fits(top, Movement.Height(Posture.Crouch)))
            {
                return false;
            }

            outward.Normalize();
            var here = transform.position;
            // The path's own frame: the edge a hang's breadth before the body, the top where the climb comes over.
            var lip = new Vector3(here.x, top.y, here.z) - outward * Movement.HangOff;

            return PullUp(new Edge { Top = top, Out = outward, Lip = lip, Stand = top, Deep = true }, false);
        }

        /// <summary>Over a low wall in a tucked hop, landing beyond it.</summary>
        bool Vault(Edge edge)
        {
            mantleFrom = transform.position;
            mantleTo = edge.Beyond;
            // The body's root at the top of the hop: the tucked soles just over the wall.
            mantleTop = edge.Top.y + 0.05f - Movement.VaultTuck;
            mantleVault = true;
            mantleStart = 0;
            mantleSeconds = ClipLength("vault", 0.6f);
            mantleEnd = Posture.Stand;
            transform.rotation = Quaternion.LookRotation(-edge.Out);
            BeginMantle("vault", 0.08f, 0);
            Sound.AudioDirector.Play("whoosh", transform.position, 0.4f, 0.1f);

            return true;
        }

        void BeginMantle(string clip, float fade, float startAt)
        {
            if (body != null)
            {
                body.enabled = false;
            }

            fallSpeed = 0;
            Moving = false;
            Guarding = false;
            Sprinting = false;
            SetMode(Mode.Mantling);
            Animate(clip, fade, 1, true, startAt);
        }

        /// <summary>The body carried along the path of the move over the edge, as its clip has it; then on its feet beyond.</summary>
        void Mantle()
        {
            // Put somewhere else meanwhile (a door, a blink): the move is over.
            if (body != null && body.enabled)
            {
                SetMode(Mode.Free);

                return;
            }

            fallSpeed = 0;
            var share = Mathf.Clamp01(modeTime / mantleSeconds);
            var t = Mathf.Lerp(mantleStart, 1, share);
            Vector3 at;

            if (mantleVault)
            {
                var (rise, across) = Movement.Vault(t);
                var flat = Vector3.Lerp(mantleFrom, mantleTo, across);
                var line = Mathf.Lerp(mantleFrom.y, mantleTo.y, across);
                at = new Vector3(flat.x, line + rise * Mathf.Max(0, mantleTop - line), flat.z);
            }
            else
            {
                var (rise, inward) = Movement.PullUp(t);
                var inward0 = Movement.PullUp(mantleStart).inward;
                var way = inward0 >= 0.999f ? 1 : Mathf.Clamp01((inward - inward0) / (1 - inward0));
                var flat = Vector3.Lerp(mantleHang, mantleTo, way);
                at = new Vector3(flat.x, mantleTop - Movement.HangDrop + rise * Movement.HangDrop, flat.z);
                // From where the hero was onto the path, in its first moment (a hand reaches the top, the body follows).
                at = Vector3.Lerp(mantleFrom, at, Mathf.Clamp01(modeTime / 0.12f));
            }

            transform.position = at;

            if (share >= 1)
            {
                transform.position = mantleTo + Vector3.up * 0.02f;
                SetMode(Mode.Free);
                fallSpeed = 0;

                if (mantleEnd == Posture.Stand)
                {
                    ChangePosture(Posture.Stand, true);
                }
                else
                {
                    SetPosture(Posture.Crouch);
                }
            }
        }

        // ------------------------------------------------------------------ Hanging from a ledge

        float letGo = -9;
        Vector3 hangOut, hangFrom, hangAt;
        Edge hangEdge;

        /// <summary>In the air, a ledge before the hero within the hands' reach: caught.</summary>
        bool TryGrab(Vector3 toward)
        {
            if (Time.time - letGo < 0.6f || fallSpeed < -16 || mode != Mode.Free)
            {
                return false;
            }

            if (!FindEdge(transform.position, toward, Movement.GrabHigh, out var edge) || !edge.Deep || Movement.Over(edge.Top.y - transform.position.y, false, true) != LedgeMove.Grab)
            {
                return false;
            }

            if (Posture != Posture.Stand && !SetPosture(Posture.Stand))
            {
                return false;
            }

            return Grab(edge);
        }

        /// <summary>Hands on the edge, the body hanging below it off the wall; or, too near the ground for that, straight up onto it.</summary>
        bool Grab(Edge edge)
        {
            var at = edge.Lip + edge.Out * Movement.HangOff;
            at.y = edge.Top.y - Movement.HangDrop;

            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, 0.62f, ~0, QueryTriggerInteraction.Ignore))
            {
                return PullUp(edge, true);
            }

            hangEdge = edge;
            hangOut = edge.Out;
            hangFrom = transform.position;
            hangAt = at;

            if (body != null)
            {
                body.enabled = false;
            }

            fallSpeed = 0;
            Moving = false;
            Sprinting = false;
            transform.rotation = Quaternion.LookRotation(-edge.Out);
            SetMode(Mode.Hanging);
            Animate("hang", 0.1f, 1, true);
            Sound.AudioDirector.Play("grab", edge.Lip, 0.7f, 0.1f);
            Session.SetFlag("climbed");

            return true;
        }

        /// <summary>
        /// Hanging: the arms tire by the second; up (forward or the jump key) pulls the hero over, down (back or
        /// the crouch key) lets go; left and right go along the edge hand over hand while it goes on.
        /// </summary>
        void Hang(float dt, bool free)
        {
            // Put somewhere else meanwhile (a door, a blink): no longer hanging.
            if (body != null && body.enabled)
            {
                SetMode(Mode.Free);

                return;
            }

            fallSpeed = 0;
            var settle = Mathf.Clamp01(modeTime / 0.15f);

            if (settle < 1)
            {
                transform.position = Vector3.Lerp(hangFrom, hangAt, settle * settle * (3 - 2 * settle));

                return;
            }

            var keys = free ? Keys.Move : Vector2.zero;
            var along = Mathf.Abs(keys.x) > 0.3f;

            if (!Session.Climb(dt, along, false))
            {
                Note("Руки разжались");
                LetGo();

                return;
            }

            if (free && (keys.y > 0.5f || Bindings.Down("jump")))
            {
                PullUp(hangEdge, true);

                return;
            }

            if (free && (keys.y < -0.5f || Bindings.Down("crouch") || Bindings.Down("crawl")))
            {
                LetGo();

                return;
            }

            // Along the edge, while it goes on at much the same height.
            if (along)
            {
                var side = Vector3.Cross(Vector3.up, -hangOut).normalized * Mathf.Sign(keys.x);
                var next = transform.position + side * 0.9f * dt;

                if (FindEdge(next, -hangOut, Movement.HangDrop + 0.3f, out var edge) && edge.Deep && Mathf.Abs(edge.Top.y - hangEdge.Top.y) < 0.25f)
                {
                    hangEdge = edge;
                    hangOut = edge.Out;
                    var to = edge.Lip + edge.Out * Movement.HangOff;
                    to.y = edge.Top.y - Movement.HangDrop;
                    transform.position = Vector3.MoveTowards(transform.position, to, 1.2f * dt);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(-hangOut), 180 * dt);
                }
                else
                {
                    along = false;
                }
            }

            Moving = along;
            Animate("hang", 0.15f, along ? 1.8f : 1f);
        }

        void LetGo()
        {
            letGo = Time.time;
            SetMode(Mode.Free);
            fallSpeed = 0;
            Animate("fall", 0.15f);
        }

        // ------------------------------------------------------------------ Up a wall

        Vector3 wallOut;

        /// <summary>Running at a high wall (the jump key): a few steps up it, for stamina; a ledge met on the way is caught.</summary>
        bool TryWallRun(Vector3 toward)
        {
            toward.y = 0;

            if (toward.sqrMagnitude < 0.001f)
            {
                return false;
            }

            toward.Normalize();

            if (!Physics.Raycast(transform.position + Vector3.up, toward, out var wall, Radius + 0.6f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(wall.normal.y) > 0.35f || !Wall(wall.collider))
            {
                return false;
            }

            // A wall worth running up goes on above the head.
            if (!Physics.Raycast(transform.position + Vector3.up * 2.2f, toward, Radius + 0.9f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (!Session.Godmode && !Session.SpendStamina(Movement.WallRunCost))
            {
                Note("Нет сил взбежать по стене");

                return false;
            }

            wallOut = wall.normal;
            wallOut.y = 0;
            wallOut.Normalize();
            transform.rotation = Quaternion.LookRotation(-wallOut);
            fallSpeed = 0;
            Sprinting = false;
            SetMode(Mode.WallRunning);
            Animate("climb", 0.08f, 2.4f, true);
            Session.SetFlag("climbed");

            return true;
        }

        void WallRun(float dt, bool free)
        {
            fallSpeed = 0;

            if (body != null && body.enabled)
            {
                body.Move((Vector3.up * (Movement.WallRunRise / Movement.WallRunSeconds) - wallOut * 0.6f) * dt);
            }

            if (free && FindEdge(transform.position, -wallOut, Movement.GrabHigh, out var edge) && edge.Deep
                && Movement.Over(edge.Top.y - transform.position.y, false, true) == LedgeMove.Grab && Grab(edge))
            {
                return;
            }

            if (modeTime >= Movement.WallRunSeconds)
            {
                SetMode(Mode.Free);
                fallSpeed = 0;
                body?.Move(wallOut * 0.2f);
                Animate("fall", 0.15f);
            }
        }
    }
}
