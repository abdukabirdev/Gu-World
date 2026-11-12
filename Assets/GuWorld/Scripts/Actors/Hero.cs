using System.Collections;
using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.Fx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.Actors
{
    /// <summary>
    /// The hero in the world: walks where the keys say (relative to the camera), strikes with
    /// the palm in a combo of three, rolls out of blows, and casts the Gu in the slots. What a
    /// blow or a cast is worth is the Session's business; here it is given a shape.
    /// </summary>
    public class Hero : Actor
    {
        public const float RunSpeed = 5.4f;
        public const float SprintSpeed = 7.8f;

        enum Mode { Free, Striking, Casting, Rolling, Dashing, Hurt, Dead, Sitting }

        Mode mode;
        float modeTime;
        int combo;
        bool comboQueued, struck;
        float sinceStrike = 9;
        Vector3 rollDirection;
        float invulnerable;
        float absorbWait;
        float noteWait;
        string place;
        float placeWait;
        public Enemy Target { get; private set; }
        Session Session => Game.Session;

        public override bool Alive => mode != Mode.Dead;
        /// <summary>The hero is moving under their own legs (the rules: a wound bleeds more).</summary>
        public bool Moving { get; private set; }
        public bool Rolling => mode == Mode.Rolling;
        public bool Airborne => !Grounded && transform.position.y - Game.Ground(transform.position).y > 0.45f;
        public bool Sprinting { get; private set; }
        public bool Sitting => mode == Mode.Sitting;
        /// <summary>Seconds since the hero last struck or was struck (the HUD and the camera ease after a fight).</summary>
        public float SinceFight { get; private set; } = 99;

        public static Hero Spawn(Vector3 at, float yaw)
        {
            var root = new GameObject("Hero");
            root.transform.position = Game.Ground(at) + Vector3.up * 0.1f;
            root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var hero = root.AddComponent<Hero>();
            hero.IsHero = true;
            hero.MakeBody(0.32f, 1.74f);
            hero.MakeModel();

            return hero;
        }

        /// <summary>The model for the quality chosen (the full 626 000-triangle one on high).</summary>
        public void MakeModel()
        {
            if (model != null)
            {
                Destroy(model.gameObject);
            }

            model = Models.Human(transform, "hero", true, Quality.FullHero, out rig).transform;
            model.localScale = Vector3.one * 0.97f;
            rig?.Play("idle", 0);
            springs = Quality.Level >= 1 ? new SpringBones(model) : null;
        }

        void SetMode(Mode next)
        {
            mode = next;
            modeTime = 0;
        }

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            if (Game.Frozen || Session == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            modeTime += dt;
            sinceStrike += dt;
            SinceFight += dt;
            invulnerable -= dt;
            noteWait -= dt;
            Sprinting = false;

            if (mode == Mode.Dead)
            {
                Walk(Vector3.zero, dt);
                return;
            }

            // On the big world, the ground under the hero may not be there yet: they wait for it, without falling.
            if (!World.Ground.Ready(transform.position))
            {
                fallSpeed = 0;
                Session.Tick(dt);
                return;
            }

            if (Target != null && (!Target.Alive || FlatDistance(Target.transform.position) > 34))
            {
                Target = null;
            }

            var free = !Game.Busy;

            if (free)
            {
                WatchPlace(dt);
            }

            // Stunned or frozen: the hero stands where they are.
            if (Session.Held && mode != Mode.Dead)
            {
                Walk(Vector3.zero, dt);
                Animate("hit", 0.1f);
                Session.Tick(dt);
                Moving = false;
                return;
            }

            Aura(dt);

            switch (mode)
            {
                case Mode.Sitting:
                    Walk(Vector3.zero, dt);
                    Animate("sit", 0.35f);
                    break;
                case Mode.Free:
                    Free(dt, free);
                    break;
                case Mode.Striking:
                    Striking(dt, free);
                    break;
                case Mode.Casting:
                    Walk(Vector3.zero, dt);

                    if (modeTime > 0.42f)
                    {
                        SetMode(Mode.Free);
                    }

                    break;
                case Mode.Rolling:
                    var speed = Mathf.Lerp(10.5f, 4.5f, modeTime / 0.55f);
                    Walk(rollDirection * speed, dt);
                    Face(rollDirection, 1400, dt);

                    if (modeTime > 0.55f)
                    {
                        SetMode(Mode.Free);
                    }

                    break;
                case Mode.Dashing:
                    Walk(Vector3.zero, dt);
                    break;
                case Mode.Hurt:
                    Walk(Vector3.zero, dt);

                    if (modeTime > 0.26f)
                    {
                        SetMode(Mode.Free);
                    }

                    break;
            }

            Session.Tick(dt, Sprinting);
        }

        float auraWait;
        float stepWait;

        /// <summary>A footstep now and then while running, by what is underfoot.</summary>
        void Steps(float dt)
        {
            if (!Grounded)
            {
                return;
            }

            stepWait -= dt * (Sprinting ? 1.35f : 1) * Session.SpeedFactor;

            if (stepWait > 0)
            {
                return;
            }

            stepWait = 0.36f;
            Sound.AudioDirector.Play(World.Ground.StepSound(transform.position), transform.position, Sprinting ? 0.55f : 0.4f, 0.1f);
        }

        /// <summary>A burning (or freezing) aura harms everything close, twice a second.</summary>
        void Aura(float dt)
        {
            if (!Session.AuraActive)
            {
                return;
            }

            auraWait -= dt;

            if (auraWait > 0)
            {
                return;
            }

            auraWait = 0.5f;
            var colour = Session.AuraStatus == StatusKind.Chill ? new Color(0.7f, 0.92f, 1f) : new Color(1f, 0.55f, 0.25f);
            Effects.Wave(transform.position, colour, Session.AuraRadius, 0.4f);

            foreach (var enemy in new System.Collections.Generic.List<Enemy>(Enemy.Living))
            {
                if (enemy.Alive && FlatDistance(enemy.transform.position) <= Session.AuraRadius + enemy.Radius)
                {
                    enemy.TakeHit(Session.AuraDamage * 0.5f * Session.Stats.Power, false, transform.position, 0, Session.AuraStatus, Session.AuraStatusPower, 2);
                }
            }
        }

        SpringBones springs;

        void LateUpdate()
        {
            if (rig != null && !Game.Frozen)
            {
                rig.Tick(Time.deltaTime);
                springs?.Tick(Time.deltaTime, SpringBones.Wind);
            }
        }

        void Animate(string clip, float fade = 0.15f, float speed = 1, bool restart = false) => rig?.Play(clip, fade, restart, speed);

        Vector3 MoveWish()
        {
            var keys = Keys.Move;
            var camera = Game.Rig != null ? Game.Rig.transform : transform;
            var forward = camera.forward;
            forward.y = 0;
            forward.Normalize();
            var right = new Vector3(forward.z, 0, -forward.x);

            return forward * keys.y + right * keys.x;
        }

        /// <summary>Where blows and casts go: at the target if one is held, else where the camera looks.</summary>
        public Vector3 AimDirection()
        {
            if (Target != null)
            {
                var to = Target.transform.position - transform.position;
                to.y = 0;

                if (to.sqrMagnitude > 0.01f)
                {
                    return to.normalized;
                }
            }

            var forward = Game.Rig != null ? Game.Rig.transform.forward : transform.forward;
            forward.y = 0;

            return forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        }

        void Free(float dt, bool free)
        {
            var wish = free ? MoveWish() : Vector3.zero;
            var moving = wish.sqrMagnitude > 0.01f;
            Moving = moving;
            // The guard (Ctrl held): slower, no sprint, a parry in its first instant.
            var guarding = free && Grounded && (Keys.Held(Key.LeftCtrl) || Keys.Held(Key.RightCtrl));
            guardSince = guarding ? (Guarding ? guardSince + dt : 0) : 0;
            Guarding = guarding;
            Sprinting = moving && free && !guarding && Keys.Held(Key.LeftShift) && Session.Stamina > 1 && Grounded;
            // Cold and slowness drag, wind hastens.
            var speed = (Sprinting ? SprintSpeed : RunSpeed) * Session.SpeedFactor * (guarding ? 0.45f : 1);
            Walk(wish * speed, dt);

            if (moving)
            {
                Face(wish, 720, dt);
                Steps(dt);
            }
            else if (Target != null && sinceStrike < 2)
            {
                Face(Target.transform.position - transform.position, 540, dt);
            }

            if (!Grounded && Airborne)
            {
                Animate(fallSpeed > 0 ? "jump" : "fall", 0.12f);
            }
            else if (guarding)
            {
                Animate("guard", 0.08f, moving ? 1.6f : 1f);
            }
            else if (moving)
            {
                Animate("run", 0.15f, Sprinting ? 1.35f : 1f);
            }
            else
            {
                Animate(sinceStrike < 2.5f ? "guard" : "idle", 0.25f);
            }

            if (!free)
            {
                return;
            }

            if (Keys.Down(Key.Space) && Grounded)
            {
                fallSpeed = 7.2f;
                Animate("jump", 0.08f, 1, true);
            }

            if ((Keys.RightDown || Keys.Down(Key.LeftAlt)) && Grounded)
            {
                TryRoll(wish);
                return;
            }

            if (Keys.LeftDown && Grounded && !guarding)
            {
                BeginStrike(0);
                return;
            }

            if (Keys.Down(Key.Tab) || Keys.MiddleDown)
            {
                PickTarget();
            }

            if (Keys.Down(Key.Q)) { TryCast(0); }
            else if (Keys.Down(Key.E)) { TryCast(1); }
            else if (Keys.Down(Key.R)) { TryCast(2); }
            else if (Keys.Down(Key.F)) { TryCast(3); }
            // The fourth and fifth ranks open two more slots.
            else if (Keys.Down(Key.Z)) { TryCast(4); }
            else if (Keys.Down(Key.C)) { TryCast(5); }
            // A killer move of the Gu slotted together.
            else if (Keys.Down(Key.V)) { TryKillerMove(); }
            // The next killer move ready.
            else if (Keys.Down(Key.X)) { NextKillerMove(); }

            if (Keys.Down(Key.H))
            {
                if (Session.UseConsumable("heal_pill"))
                {
                    Effects.Aura(transform, new Color(0.95f, 0.55f, 0.45f), 0.8f);
                }
                else
                {
                    Note(Session.Count("heal_pill") > 0 ? "Здоровье полное" : "Пилюль заживления нет");
                }
            }

            absorbWait -= dt;

            if (Keys.Held(Key.G) && absorbWait <= 0)
            {
                absorbWait = 0.45f;

                if (Session.AbsorbStone())
                {
                    Effects.Aura(transform, new Color(0.5f, 0.88f, 0.78f), 0.5f, 1.6f);
                }
                else if (Keys.Down(Key.G))
                {
                    Note(Session.State.Stones > 0 ? "Апертура полна" : "Первобытных камней нет");
                }
            }

            if (Keys.Down(Key.T))
            {
                Game.Interact();
            }
        }

        void Note(string line)
        {
            if (noteWait <= 0)
            {
                noteWait = 1.2f;
                Game.Say(line);
            }
        }

        void TryRoll(Vector3 wish)
        {
            if (!Session.SpendStamina(HeroStats.RollStamina))
            {
                Note("Нет сил на перекат");
                return;
            }

            rollDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
            invulnerable = 0.36f;
            SetMode(Mode.Rolling);
            Animate("roll", 0.06f, 1.1f, true);
            Sound.AudioDirector.Play("roll", transform.position, 0.6f);
        }

        // ------------------------------------------------------------------ The palm

        static readonly float[] StrikeLength = { 0.42f, 0.42f, 0.62f };
        static readonly float[] StrikeAt = { 0.4f, 0.4f, 0.46f };

        void BeginStrike(int step)
        {
            combo = step;
            comboQueued = false;
            struck = false;
            SetMode(Mode.Striking);
            Animate("attack" + (step + 1), 0.07f, 1, true);

            // A strike turns to its mark: the held target, or the nearest enemy roughly ahead.
            var mark = Target != null ? Target : Nearest(4.5f, 100);

            if (mark != null)
            {
                var to = mark.transform.position - transform.position;
                to.y = 0;
                transform.rotation = Quaternion.LookRotation(to.sqrMagnitude > 0.01f ? to : transform.forward);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(AimDirection());
            }
        }

        void Striking(float dt, bool free)
        {
            var length = StrikeLength[combo];
            var t = modeTime / length;

            // A short step into the blow, stopping at arm's length from whoever is ahead.
            var ahead = Nearest(1.5f, 60);
            Walk(t > 0.15f && t < StrikeAt[combo] + 0.1f && ahead == null ? transform.forward * (combo == 2 ? 4.2f : 2.6f) : Vector3.zero, dt);

            if (free && Keys.LeftDown)
            {
                comboQueued = true;
            }

            if (free && (Keys.RightDown || Keys.Down(Key.LeftAlt)) && t > StrikeAt[combo])
            {
                TryRoll(MoveWish());
                return;
            }

            if (!struck && t >= StrikeAt[combo])
            {
                struck = true;
                sinceStrike = 0;
                Land();
            }

            if (t >= 0.72f && comboQueued && combo < 2)
            {
                BeginStrike(combo + 1);
                return;
            }

            if (t >= 1)
            {
                SetMode(Mode.Free);
            }
        }

        void Land()
        {
            var reach = combo == 2 ? 2.9f : 2.3f;
            var arc = combo == 2 ? 80 : 62;
            var hit = false;
            var origin = transform.position;

            foreach (var enemy in new List<Enemy>(Enemy.Living))
            {
                var to = enemy.transform.position - origin;
                to.y = 0;

                if (to.magnitude > reach + enemy.Radius || (to.magnitude > 0.6f && Vector3.Angle(transform.forward, to) > arc))
                {
                    continue;
                }

                var blow = Session.MeleeHit(combo, enemy.Def);
                enemy.TakeHit(blow.Damage, blow.Critical, origin, combo == 2 ? 1.1f : 0.35f);
                Effects.Burst(enemy.Centre - to.normalized * enemy.Radius, new Color(1f, 0.95f, 0.8f), combo == 2 ? 1.8f : 1.1f, 0.18f);
                hit = true;
            }

            Sound.AudioDirector.Play(hit ? (combo == 2 ? "palm_heavy" : "palm") : "whoosh", transform.position + transform.forward, hit ? 1 : 0.5f);

            if (hit)
            {
                SinceFight = 0;
                Game.Shake(combo == 2 ? 0.22f : 0.08f);
            }

            if (combo == 2)
            {
                Effects.Wave(origin + transform.forward * 1.2f, new Color(1f, 0.95f, 0.8f, 0.6f), 2.2f, 0.3f);
            }
        }

        Enemy Nearest(float within, float arc)
        {
            Enemy best = null;
            var bestDistance = within;

            foreach (var enemy in Enemy.Living)
            {
                var to = enemy.transform.position - transform.position;
                to.y = 0;
                var distance = to.magnitude - enemy.Radius;

                if (distance < bestDistance && (distance < 0.5f || Vector3.Angle(transform.forward, to) <= arc))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        void PickTarget()
        {
            if (Target != null)
            {
                Target = null;
                return;
            }

            var look = Game.Rig != null ? Game.Rig.transform.forward : transform.forward;
            look.y = 0;
            Enemy best = null;
            var bestScore = float.MaxValue;

            foreach (var enemy in Enemy.Living)
            {
                if (enemy.Def.Mind == EnemyMind.Still)
                {
                    continue;
                }

                var to = enemy.transform.position - transform.position;
                to.y = 0;
                var angle = Vector3.Angle(look, to);

                if (to.magnitude < 28 && angle < 70)
                {
                    var score = to.magnitude + angle * 0.25f;

                    if (score < bestScore)
                    {
                        best = enemy;
                        bestScore = score;
                    }
                }
            }

            Target = best;
        }

        // ------------------------------------------------------------------ The Gu

        void TryCast(int slot)
        {
            var why = Session.WhyNot(slot);

            if (why != null)
            {
                if (slot < Session.Stats.ActiveSlots)
                {
                    Note(why);
                }

                return;
            }

            var cast = Session.CastSlot(slot);

            if (cast != null)
            {
                Perform(cast);
            }
        }

        void NextKillerMove()
        {
            var next = Session.NextMove();
            Note(next != null ? $"Приём: {next.Name}" : "Нет готовых приёмов: постигайте их в окне приёмов (U)");
        }

        /// <summary>
        /// The killer move on V (X: the next one ready). Cast like a Gu's skill, but louder and
        /// brighter: the core Gu's sound and a gong over it, a ring of the move's colour, its name shown.
        /// </summary>
        void TryKillerMove()
        {
            var move = Session.SelectedMove();
            var why = Session.WhyNoMove(move);

            if (why != null)
            {
                Note(move == null && Session.State.Moves.Count == 0 ? "Вы не знаете убийственных приёмов" : why);

                return;
            }

            var cast = Session.CastMove(move);

            if (cast == null)
            {
                return;
            }

            Sound.AudioDirector.Play("killer_move", transform.position, 0.8f);
            Effects.Wave(transform.position, Models.Rgb(move.Colour), 4.5f, 0.5f);
            Note(move.Name);
            Perform(cast);
        }

        /// <summary>What a cast does in the world: the sound, the animation, the shape of its skill.</summary>
        void Perform(Cast cast)
        {
            Sound.AudioDirector.Play(cast.Skill.Shape == SkillShape.Heal ? "heal" : cast.Skill.Shape == SkillShape.Buff ? "buff" : Core.Audio.Sounds.CastOf(cast.Def.Path), transform.position, 0.9f);

            sinceStrike = 0;
            SinceFight = 0;
            var aim = AimDirection();
            var colour = Models.Rgb(cast.Move != null ? cast.Move.Colour : cast.Def.Colour);
            var shape = cast.Skill.Shape;

            if (shape != SkillShape.Buff && shape != SkillShape.Heal && shape != SkillShape.Nova)
            {
                transform.rotation = Quaternion.LookRotation(aim);
            }

            switch (shape)
            {
                case SkillShape.Buff:
                case SkillShape.Heal:
                    Animate("channel", 0.08f, 1.5f, true);
                    Effects.Aura(transform, colour, shape == SkillShape.Heal ? cast.Skill.Duration : Mathf.Max(1, cast.Skill.Duration), 2.4f);
                    Effects.Wave(transform.position, colour, 2.5f, 0.4f);
                    SetMode(Mode.Casting);
                    modeTime = 0.2f;
                    break;
                case SkillShape.Dash:
                    StartCoroutine(DashThrough(cast, aim, colour));
                    break;
                case SkillShape.Blink:
                    StartCoroutine(BlinkTo(cast, aim, colour));
                    break;
                default:
                    Animate(shape == SkillShape.Nova ? "slam" : "cast", 0.06f, shape == SkillShape.Nova ? 2.2f : 1.3f, true);
                    SetMode(Mode.Casting);
                    StartCoroutine(Release(cast, aim, colour, shape == SkillShape.Nova ? 0.22f : 0.2f));
                    break;
            }
        }

        void Strike(Cast cast, Enemy enemy, float push = 0.4f)
        {
            if (enemy == null || !enemy.Alive)
            {
                return;
            }

            var blow = Session.SkillHit(cast, enemy.Def);
            enemy.TakeHit(blow.Damage, blow.Critical, transform.position, push, cast.Skill.Status, cast.Skill.StatusPower, cast.Skill.StatusTime);
            SinceFight = 0;
        }

        Vector3 Hand => transform.position + Vector3.up * 1.25f + transform.forward * 0.7f;

        /// <summary>The spot a cast at a distance lands on: the held target, or as far as the skill reaches where the hero looks.</summary>
        Vector3 Spot(Cast cast, Vector3 aim)
        {
            if (Target != null && FlatDistance(Target.transform.position) <= cast.Range + 2)
            {
                return Target.transform.position;
            }

            var near = Nearest(cast.Range, 30);

            return near != null ? near.transform.position : Game.Ground(transform.position + aim * cast.Range * 0.7f);
        }

        IEnumerator Release(Cast cast, Vector3 aim, Color colour, float after)
        {
            yield return new WaitForSeconds(after);

            if (!Alive)
            {
                yield break;
            }

            var skill = cast.Skill;
            var moon = cast.Def.Path == GuPath.Moon || cast.Def.Path == GuPath.Blood;

            switch (skill.Shape)
            {
                case SkillShape.Projectile:
                    Projectile.Launch(true, Hand, Lead(aim), skill.Speed, cast.Range, skill.Radius, colour, skill.Pierce, moon, actor => Strike(cast, actor as Enemy));
                    Effects.Burst(Hand, colour, 1.2f, 0.15f);
                    break;

                case SkillShape.Fan:
                    for (var i = 0; i < skill.Count; i++)
                    {
                        var spread = (i - (skill.Count - 1) * 0.5f) * 11;
                        Projectile.Launch(true, Hand, Quaternion.Euler(0, spread, 0) * Lead(aim), skill.Speed, cast.Range, skill.Radius, colour, skill.Pierce, moon, actor => Strike(cast, actor as Enemy));
                    }

                    Effects.Burst(Hand, colour, 1.8f, 0.18f);
                    break;

                case SkillShape.Cone:
                    Effects.Wave(transform.position + aim * cast.Range * 0.5f, colour, cast.Range * 0.6f, 0.4f);
                    Effects.Burst(Hand + aim * 2, colour, 5, 0.35f);

                    foreach (var enemy in new List<Enemy>(Enemy.Living))
                    {
                        var to = enemy.transform.position - transform.position;
                        to.y = 0;

                        if (to.magnitude <= cast.Range + enemy.Radius && Vector3.Angle(aim, to) <= skill.Radius)
                        {
                            Strike(cast, enemy);
                        }
                    }

                    break;

                case SkillShape.Nova:
                    Effects.Wave(transform.position, colour, skill.Radius, 0.4f);
                    Game.Shake(0.3f);

                    foreach (var enemy in new List<Enemy>(Enemy.Living))
                    {
                        if (FlatDistance(enemy.transform.position) <= skill.Radius + enemy.Radius)
                        {
                            Strike(cast, enemy, 1f);
                        }
                    }

                    break;

                case SkillShape.Chain:
                    Chain(cast, aim, colour);
                    break;

                case SkillShape.Pull:
                    Game.Run(Whirl(cast, Spot(cast, aim), colour));
                    break;

                case SkillShape.Storm:
                    Game.Run(Storm(cast, Spot(cast, aim), colour));
                    break;
            }
        }

        /// <summary>A bolt leaves level with the ground unless a target stands higher or lower.</summary>
        Vector3 Lead(Vector3 aim)
        {
            if (Target != null)
            {
                var to = Target.Centre - Hand;

                return to.sqrMagnitude > 0.01f ? to.normalized : aim;
            }

            return aim;
        }

        void Chain(Cast cast, Vector3 aim, Color colour)
        {
            var from = Hand;
            var struckSoFar = new List<Enemy>();
            var current = Target != null && FlatDistance(Target.transform.position) <= cast.Range ? Target : Nearest(cast.Range, 40);

            if (current == null)
            {
                Effects.Bolt(from, from + aim * cast.Range * 0.6f, colour);
                return;
            }

            for (var i = 0; i < cast.Skill.Count && current != null; i++)
            {
                Effects.Bolt(from, current.Centre, colour);
                Strike(cast, current);
                struckSoFar.Add(current);
                from = current.Centre;
                Enemy next = null;
                var best = cast.Skill.Radius;

                foreach (var enemy in Enemy.Living)
                {
                    var distance = (enemy.transform.position - current.transform.position).magnitude;

                    if (!struckSoFar.Contains(enemy) && distance < best)
                    {
                        next = enemy;
                        best = distance;
                    }
                }

                current = next;
            }
        }

        IEnumerator Whirl(Cast cast, Vector3 centre, Color colour)
        {
            var skill = cast.Skill;
            Effects.Wave(centre, colour, skill.Radius, 0.5f);
            var mark = Models.GroundGlow("Whirl", null, new Color(colour.r, colour.g, colour.b, 0.5f), skill.Radius * 2, true);
            mark.transform.position = Game.Ground(centre) + Vector3.up * 0.1f;
            mark.AddComponent<Fx.Hover>().Spin = 300;
            var hit = false;

            for (var t = 0f; t < skill.Duration; t += Time.deltaTime)
            {
                foreach (var enemy in new List<Enemy>(Enemy.Living))
                {
                    if ((enemy.transform.position - centre).magnitude <= skill.Radius + enemy.Radius)
                    {
                        enemy.Drag(centre, 9, Time.deltaTime);

                        if (!hit)
                        {
                            Strike(cast, enemy, 0);
                        }
                    }
                }

                hit = true;
                yield return null;
            }

            Destroy(mark);
        }

        IEnumerator Storm(Cast cast, Vector3 centre, Color colour)
        {
            var skill = cast.Skill;
            var mark = Models.GroundGlow("Storm", null, new Color(colour.r, colour.g, colour.b, 0.6f), skill.Radius * 2, true);
            mark.transform.position = Game.Ground(centre) + Vector3.up * 0.1f;
            var gap = skill.Duration / Mathf.Max(1, skill.Count);

            for (var i = 0; i < skill.Count; i++)
            {
                var around = Random.insideUnitCircle * skill.Radius * 0.7f;
                var at = centre + new Vector3(around.x, 0, around.y);
                Enemy inside = null;

                foreach (var enemy in new List<Enemy>(Enemy.Living))
                {
                    if ((enemy.transform.position - centre).magnitude <= skill.Radius + enemy.Radius)
                    {
                        inside = inside ?? enemy;
                        Strike(cast, enemy, 0);
                    }
                }

                Effects.Pillar(inside != null && i % 2 == 0 ? inside.transform.position : at, colour, 1.6f, 16, 0.3f);
                Game.Shake(0.1f);
                yield return new WaitForSeconds(gap);
            }

            Destroy(mark);
        }

        IEnumerator DashThrough(Cast cast, Vector3 aim, Color colour)
        {
            SetMode(Mode.Dashing);
            invulnerable = 0.3f;
            Animate("run", 0.05f, 2.4f, true);
            var from = transform.position;
            var struckSoFar = new HashSet<Enemy>();
            const float seconds = 0.16f;

            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                body.Move(aim * (cast.Range / seconds) * Time.deltaTime + Vector3.down * 0.05f);
                Effects.Burst(Centre, new Color(colour.r, colour.g, colour.b, 0.5f), 1.4f, 0.2f);

                foreach (var enemy in new List<Enemy>(Enemy.Living))
                {
                    if (!struckSoFar.Contains(enemy) && FlatDistance(enemy.transform.position) <= cast.Skill.Radius + enemy.Radius)
                    {
                        struckSoFar.Add(enemy);
                        Strike(cast, enemy);
                    }
                }

                yield return null;
            }

            Effects.Wave(from, colour, 2, 0.3f);
            SetMode(Mode.Free);
        }

        IEnumerator BlinkTo(Cast cast, Vector3 aim, Color colour)
        {
            SetMode(Mode.Dashing);
            invulnerable = 0.35f;
            var from = transform.position;
            Burst(cast, from, colour);
            var to = Target != null && FlatDistance(Target.transform.position) <= cast.Range + 3
                ? Target.transform.position - aim * (Target.Radius + 1.2f)
                : from + aim * cast.Range;

            // Not up a cliff or through a wall: the leap stops where the ground turns too steep or something solid stands.
            var path = to - from;
            path.y = 0;
            var reach = path.magnitude;

            if (Physics.SphereCast(from + Vector3.up, 0.3f, path.normalized, out var wall, reach, ~0, QueryTriggerInteraction.Ignore) && wall.collider.GetComponentInParent<Actor>() == null)
            {
                reach = Mathf.Max(0, wall.distance - 0.4f);
            }

            var last = Game.Ground(from).y;

            for (var gone = 1f; gone <= reach; gone += 1)
            {
                var here = Game.Ground(from + path.normalized * gone).y;

                if (Mathf.Abs(here - last) > 1.3f)
                {
                    reach = gone - 1;
                    break;
                }

                last = here;
            }

            to = from + path.normalized * reach;
            yield return new WaitForSeconds(0.08f);
            Teleport(Game.Ground(to) + Vector3.up * 0.1f);
            Effects.Bolt(from + Vector3.up, transform.position + Vector3.up, colour, 0.25f);
            Burst(cast, transform.position, colour);
            Game.Shake(0.25f);
            SetMode(Mode.Free);
        }

        void Burst(Cast cast, Vector3 at, Color colour)
        {
            Effects.Pillar(at, colour, 1.4f, 12, 0.3f);
            Effects.Wave(at, colour, cast.Skill.Radius, 0.35f);

            foreach (var enemy in new List<Enemy>(Enemy.Living))
            {
                if ((enemy.transform.position - at).magnitude <= cast.Skill.Radius + enemy.Radius)
                {
                    Strike(cast, enemy);
                }
            }
        }

        // ------------------------------------------------------------------ Being hit, dying, coming back

        /// <summary>A blow on the hero: what of it landed (0 if it was dodged, swallowed by the ward, or the hero is out of reach of blows).</summary>
        /// <summary>The guard is up (Ctrl held).</summary>
        public bool Guarding { get; private set; }
        float guardSince;

        public float TakeHit(float raw, Vector3 from, Enemy attacker = null)
        {
            if (mode == Mode.Dead || invulnerable > 0 || Game.Busy)
            {
                return 0;
            }

            // A raised guard: parried in its first instant, blocked after, broken from behind or when spent.
            if (Guarding)
            {
                var toward = from - transform.position;
                toward.y = 0;
                var facing = toward.sqrMagnitude < 0.01f || Vector3.Angle(transform.forward, toward) < 75;
                var guard = Session.Guard(raw, guardSince, facing);

                if (guard.Parried)
                {
                    Sound.AudioDirector.Play("ward", transform.position, 0.9f);
                    Effects.Burst(Centre + transform.forward * 0.5f, new Color(1f, 0.95f, 0.7f), 1.2f, 0.25f);
                    Game.Ui?.Toast("Парировано");
                    attacker?.Parried();

                    return 0;
                }

                if (guard.Broken)
                {
                    Guarding = false;
                    Game.Ui?.Toast("Блок пробит");
                }
                else if (guard.Share < 1)
                {
                    Sound.AudioDirector.Play("block", transform.position, 0.8f);
                    Effects.Burst(Centre + transform.forward * 0.4f, new Color(0.85f, 0.9f, 1f), 0.8f, 0.15f);
                }

                raw *= guard.Share;
            }

            var damage = Session.TakeDamage(raw);
            SinceFight = 0;

            if (damage <= 0)
            {
                Effects.Aura(transform, Color.white, 0.4f, 2.6f);
                Sound.AudioDirector.Play("ward", transform.position, 0.8f);
                return 0;
            }

            Sound.AudioDirector.Play("hurt", transform.position, Mathf.Clamp(0.5f + damage / Session.Stats.MaxHealth * 3, 0.5f, 1));

            Game.Ui?.DamageNumber(transform.position + Vector3.up * 2, damage, false, true);
            Game.Ui?.Hurt();
            Game.Shake(Mathf.Clamp(damage / Session.Stats.MaxHealth * 2.5f, 0.1f, 0.6f));
            Effects.Burst(Centre, new Color(1, 0.3f, 0.25f), 1.4f, 0.2f);

            if (Session.Dead)
            {
                StopAllCoroutines();
                SetMode(Mode.Dead);
                Target = null;
                Animate("death", 0.1f, 1, true);
                Sound.AudioDirector.Play("death", null, 1, 0);
                Game.HeroDied();
                return damage;
            }

            // A heavy blow breaks what the hero was doing; a light one does not, so a pack cannot lock them down.
            if (!Session.Unstoppable && damage > Session.Stats.MaxHealth * 0.1f && (mode == Mode.Free || mode == Mode.Striking || mode == Mode.Sitting))
            {
                SetMode(Mode.Hurt);
                Animate("hit", 0.05f, 1, true);
                var away = transform.position - from;
                away.y = 0;
                body.Move(away.normalized * 0.4f);
            }

            return damage;
        }

        public void Revive(Vector3 at)
        {
            StopAllCoroutines();
            Teleport(Game.Ground(at) + Vector3.up * 0.1f);
            SetMode(Mode.Free);
            invulnerable = 2;
            Animate("idle", 0, 1, true);
        }

        public void Sit(bool down)
        {
            if (mode == Mode.Dead)
            {
                return;
            }

            SetMode(down ? Mode.Sitting : Mode.Free);
        }

        void WatchPlace(float dt)
        {
            placeWait -= dt;

            if (placeWait > 0)
            {
                return;
            }

            placeWait = 0.5f;
            var point = new MapPoint(transform.position.x, transform.position.z);
            var settlement = Atlas.SettlementAt(point);
            var here = Atlas.PlaceAt(point);
            var id = settlement != null ? settlement.Id : here?.Id;

            if (id != place)
            {
                place = id;

                if (settlement != null || here != null)
                {
                    Game.Ui?.Place(settlement != null ? settlement.Name : here.Name);
                }
            }

            // Every place the hero stands in counts as reached (a small one inside a large one too), and opens its page of the codex.
            foreach (var one in Atlas.Places)
            {
                if ((Atlas.Big || Atlas.InValleyPlace(one)) && one.At.DistanceTo(point) <= one.Radius)
                {
                    Session.Entered(one.Id);
                }
            }

            if (settlement != null)
            {
                Session.Entered(settlement.Id);
            }
        }
    }
}
