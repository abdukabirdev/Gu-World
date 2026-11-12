using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.People
{
    public enum Sex { Male, Female }

    public enum AgeGroup { Child, Teen, Adult, Elder }

    /// <summary>
    /// Who a generated person is: what the world knows of them (sex, age, trade, clan, how well off)
    /// and their seed. Everything about their body, face, hair and clothes follows from this; the
    /// same spec always makes the same person.
    /// </summary>
    public sealed class PersonSpec
    {
        public int Seed;
        public Sex Sex;
        public AgeGroup Age = AgeGroup.Adult;
        /// <summary>Their trade or look: guard, farmer, porter, trader, merchant, student, elder, child, cook, herbalist, smith, scribe, bandit, gambler, keeper, judge, villager, watch, idler...</summary>
        public string Role = "villager";
        /// <summary>The clan they belong to (its colours): gu_yue, bai, xiong, or none.</summary>
        public string Clan = "gu_yue";
        /// <summary>How well off: 0 poor (hemp, patches, bare feet), 1 rich (silk, trims, embroidery).</summary>
        public float Wealth = 0.4f;

        public override string ToString() => $"{Sex} {Age} {Role} of {Clan}, wealth {Wealth:0.0}, seed {Seed}";

        /// <summary>A spec from a look's name and a seed, the way the game's people are described (sex and age by the seed where the look leaves them open).</summary>
        public static PersonSpec FromLook(string look, int seed, string clan = "gu_yue")
        {
            var rng = new Rng(seed * 92821 + 17);
            var spec = new PersonSpec { Seed = seed, Role = string.IsNullOrEmpty(look) ? "villager" : look, Clan = clan };
            var role = spec.Role;
            var female = rng.Chance(0.45f);

            switch (role)
            {
                case "guard": case "watch": case "bandit": case "smith": case "judge":
                    female = rng.Chance(0.12f);
                    break;
                case "bai":
                    spec.Clan = "bai";
                    spec.Role = "villager";
                    break;
                case "xiong":
                    spec.Clan = "xiong";
                    spec.Role = "villager";
                    break;
                case "blood":
                    spec.Clan = "none";
                    female = rng.Chance(0.3f);
                    break;
            }

            spec.Sex = female ? Sex.Female : Sex.Male;
            spec.Age = role == "child" ? AgeGroup.Child : role == "elder" || role == "judge" ? AgeGroup.Elder : role == "student" ? AgeGroup.Teen
                : rng.Chance(0.12f) ? AgeGroup.Elder : rng.Chance(0.15f) ? AgeGroup.Teen : AgeGroup.Adult;

            switch (role)
            {
                case "merchant": case "judge": case "elder": case "keeper": spec.Wealth = rng.Range(0.6f, 1f); break;
                case "farmer": case "porter": case "bandit": case "child": case "idler": spec.Wealth = rng.Range(0f, 0.35f); break;
                case "gambler": spec.Wealth = rng.Range(0.1f, 0.9f); break;
                default: spec.Wealth = rng.Range(0.2f, 0.65f); break;
            }

            return spec;
        }
    }

    /// <summary>A generated person: their mesh (vertex coloured), its skin, and the skeleton they were built on.</summary>
    public sealed class PersonModel
    {
        public PersonSpec Spec;
        public MeshData Mesh;
        /// <summary>Per vertex: four bone indices (into <see cref="Skeleton.Bones"/>) and four weights adding up to one.</summary>
        public int[] Joints;
        public float[] Weights;
        /// <summary>Where every bone of the skeleton stands at rest in this body (the engine's axes).</summary>
        public V3[] BoneAt;
        public float Height;
        /// <summary>What was chosen, in words (for the viewer and the logs).</summary>
        public string Description;
    }
}
