using System.Collections.Generic;
using System.IO;
using GuWorld.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuWorld.Editor
{
    /// <summary>
    /// The game's build and a check of what it needs. The game makes its whole world in code
    /// when it starts, so the one scene it is built from is empty.
    ///
    /// From the menu (Gu World → …) or from the command line:
    ///   Unity -batchmode -projectPath . -executeMethod GuWorld.Editor.GuWorldBuild.Windows -quit
    ///   Unity -batchmode -projectPath . -executeMethod GuWorld.Editor.GuWorldBuild.Check -quit
    /// </summary>
    public static class GuWorldBuild
    {
        public const string ScenePath = "Assets/GuWorld/Scenes/Valley.unity";

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("Gu World/Open the game's scene", priority = 10)]
        public static void OpenScene()
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Everything the game loads by name is where it looks for it. Run this first after pulling.</summary>
        [MenuItem("Gu World/Check content", priority = 20)]
        public static void Check()
        {
            var missing = new List<string>();

            void Need<T>(string path) where T : Object
            {
                if (Resources.Load<T>(path) == null)
                {
                    missing.Add($"Resources/{path} ({typeof(T).Name})");
                }
            }

            foreach (var model in new[] { "Models/gu-hero", "Models/gu-hero-game", "Models/gu-npc", "Models/canid" })
            {
                Need<GameObject>(model);
            }

            Need<TextAsset>("Anim/human");
            Need<TextAsset>("Anim/canid");
            Need<Material>("Terrain/Terrain");
            Need<Material>("Mats/Water");
            Need<Material>("Mats/Waterfall");
            Need<Material>("Mats/Boulder");
            Need<Shader>("Shaders/Sky");

            foreach (var layer in new[] { "Grass", "Forest", "Flagstone", "Path", "Rock", "Gravel", "Moss", "Earth", "Mud", "Dirt" })
            {
                Need<TerrainLayer>("Terrain/" + layer);
            }

            foreach (var font in new[] { "InterTight-Regular", "InterTight-Medium", "InterTight-SemiBold", "CormorantGaramond-SemiBold", "MaShanZheng-Regular", "ZhiMangXing-Regular" })
            {
                Need<Font>("Fonts/" + font);
            }

            var looks = new HashSet<string> { "canid_fox" };

            foreach (var npc in Atlas.AllNpcs)
            {
                looks.Add(Wardrobe.Dress(npc.Look).texture);
            }

            foreach (var enemy in Bestiary.All)
            {
                if (enemy.Body == EnemyBody.Human)
                {
                    looks.Add(Wardrobe.Dress(enemy.Look).texture);
                }
                else if (enemy.Body == EnemyBody.Canid && Wardrobe.CanidHasTexture(enemy.Look))
                {
                    looks.Add("canid_" + enemy.Look);
                }
            }

            foreach (var look in looks)
            {
                Need<Texture2D>("Looks/" + look);
            }

            var models = new HashSet<string>();

            foreach (var building in Valley.Buildings) { models.Add(building.Model); }
            foreach (var patch in Valley.Scatter) { models.Add(patch.Model); }
            foreach (var thing in Atlas.AllThings) { if (thing.Model != "") { models.Add(thing.Model); } }

            foreach (var model in models)
            {
                Need<GameObject>("Build/" + model);
            }

            // The seals: every hanzi the interface shows must be in the seal font.
            var seal = Resources.Load<Font>("Fonts/MaShanZheng-Regular");
            var glyphs = new HashSet<char>();

            foreach (var gu in GuBook.All) { foreach (var c in gu.Seal) { glyphs.Add(c); } }
            foreach (var item in ItemBook.All) { foreach (var c in item.Seal) { glyphs.Add(c); } }
            foreach (var c in "元药隐道行守隐蛊窍宝志图商运族炼术息死血魔月") { glyphs.Add(c); }

            if (seal != null)
            {
                var lost = "";

                foreach (var c in glyphs)
                {
                    if (!seal.HasCharacter(c))
                    {
                        lost += c;
                    }
                }

                if (lost != "")
                {
                    Debug.LogWarning($"GU World: the seal font has no glyphs for: {lost} (they will show in the system's fallback font, or as boxes)");
                }
            }

            if (missing.Count == 0)
            {
                Debug.Log($"GU World check: all {models.Count} models, {looks.Count} looks and the rest are in place.");
            }
            else
            {
                Debug.LogError("GU World check: missing\n  " + string.Join("\n  ", missing));

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        [MenuItem("Gu World/Build for Windows", priority = 40)]
        public static void Windows()
        {
            EnsureScene();
            Check();
            // The colliders of the artist's models are baked at build time.
            PlayerSettings.bakeCollisionMeshes = true;
            var development = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-development") >= 0;
            var folder = Path.Combine("Builds", development ? "GuWorld2-Dev" : "GuWorld2");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(folder, "GuWorld.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None,
            });
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"GU World: built {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime.TotalSeconds:0} s");
            }
            else
            {
                Debug.LogError($"GU World: the build {summary.result} with {summary.totalErrors} errors");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
