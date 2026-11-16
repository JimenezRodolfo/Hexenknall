using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Labyrinth.Editor
{
    // Buzón local y acotado para repetir comprobaciones en el editor ya abierto.
    [InitializeOnLoad]
    public static class FeriaValidation
    {
        const string Request = "Temp/feria-validation.request";
        static double nextPoll;
        static FeriaValidation() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            nextPoll = EditorApplication.timeSinceStartup+1;
            if (!File.Exists(Request))return;
            string command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (command == "checks")Run();
                else if (command == "build")Build();
                else if ((command == "play" || command == "bot") && !EditorApplication.isPlaying)
                {
                    SessionState.SetBool("Feria.RunPlayChecks", true);
                    SessionState.SetBool("Feria.Bot", command == "bot");
                    EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
                    EditorApplication.isPlaying = true;
                }
                else if (command == "stop")EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Directory.CreateDirectory("Logs/Feria"); File.WriteAllText("Logs/Feria/"+command+"-failure.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("HEXENKNALL/Feria/Run checks")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)throw new Exception("Stop Play before checks.");
            Directory.CreateDirectory("Logs/Feria");
            string report = "Unity "+Application.unityVersion+"\n";
            Action<string, Action> check = (name, action) =>
            {
                try { action(); report += "PASS "+name+"\n"; }
                catch (Exception e) { report += "FAIL "+name+": "+e+"\n"; }
            };
            check("Gameplay", GameplayChecks.Run);
            check("Balance/layout", BalanceChecks.Run);
            check("Save/block regression", ProjectRegressionChecks.Run);
            check("Old Friend/dash", HexenknallChecks.Run);
            check("Integrated art", IntegrationChecks.Validate);
            check("Animation bindings", AnimationBindingChecks.Run);
            check("Feria isolation and choices", CheckFeria);
            check("Serialized scenes and prefabs", CheckScenes);
            File.WriteAllText("Logs/Feria/checks.txt", report);
            Debug.Log(report);
            if (report.Contains("FAIL "))throw new Exception("Feria checks failed; see Logs/Feria/checks.txt");
        }
        public static void RunAndBuild() { Run(); Build(); }
        static void Require(bool value, string message) { if (!value)throw new Exception(message); }
        public static void RunPlayBatch()
        {
            SessionState.SetBool("Feria.Both", false);
            SessionState.SetBool("Feria.RunPlayChecks", true);
            SessionState.SetBool("Feria.Bot", false);
            EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
            EditorApplication.isPlaying = true;
        }
        public static void RunBotBatch()
        {
            SessionState.SetBool("Feria.Both", false);
            SessionState.SetBool("Feria.RunPlayChecks", true);
            SessionState.SetBool("Feria.Bot", true);
            EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
            EditorApplication.isPlaying = true;
        }
        public static void RunAllPlayBatch()
        {
            RunPlayBatch();
            SessionState.SetBool("Feria.Both", true);
        }
        static void CheckScenes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    var loaded = EditorSceneManager.OpenScene(scene.path);
                    GameDirector director = null;
                    foreach (var root in loaded.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script in "+scene.path);
                        var candidate = root.GetComponent<GameDirector>();
                        if (candidate)director = candidate;
                    }
                    Require(director && director.GetComponent<GridBoard>() && director.GetComponent<GameUI>(), "Scene services: "+scene.path);
                    Require(director.bombPrefab && director.meleePrefab && director.bomberPrefab && director.wallPrefab, "Prefab references: "+scene.path);
                    Require(director.bombPrefab.GetComponent<Bomb>() && director.meleePrefab.GetComponent<EnemyController>(), "Prefab components");
                }
            }
            finally
            {
                if (Array.Exists(setup, s=>s.isLoaded && s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
        }
        static void CheckFeria()
        {
            const string key = "Labyrinth.Run.v2";
            string primary = PlayerPrefs.GetString(key, ""), backup = PlayerPrefs.GetString(key+".backup", "");
            try
            {
                FeriaSession.Begin();
                Require(!RunProgress.HasSave && FeriaSession.Accessible && !FeriaSession.RotateCamera, "Demo flags");
                RunProgress.Data.lifeBonus = 2;
                RunProgress.Save();
                Require(primary == PlayerPrefs.GetString(key, "") && backup == PlayerPrefs.GetString(key+".backup", ""), "Campaign modified");
                FeriaSession.UsedZ(); Require(FeriaSession.Lesson == 1, "Z lesson");
                FeriaSession.UsedX(); Require(FeriaSession.Lesson == 2, "X lesson");
                FeriaSession.Begin(); Require(RunProgress.Data.lifeBonus == 0 && FeriaSession.Lesson == 0, "Demo reset");
                var eventObject = new GameObject("Pending transformation check");
                try
                {
                    var events = eventObject.AddComponent<StageEventManager>();
                    events.dynamicStage = true;
                    Require(events.HasPendingWork, "The first transformation must remain pending even after all kills");
                }
                finally { UnityEngine.Object.DestroyImmediate(eventObject); }
                var d = RunProgress.NewAdventureData();
                d.layoutSeed = 1837;
                var restored = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(d));
                RunProgress.Normalize(restored);
                Require(restored.layoutSeed == 1837, "Seed persistence");
                var old = JsonUtility.FromJson<RunData>("{\"version\":2,\"stage\":1}");
                RunProgress.Normalize(old); Require(old.stage == 1, "Legacy compatibility");
                foreach (var effect in new[] { CardEffect.Life, CardEffect.Cooldowns, CardEffect.Immunity })
                {
                    var cardData = RunProgress.NewAdventureData();
                    CardRules.Apply(new CardOffer(effect, Rarity.Rare, false), cardData);
                    Require(cardData.lifeBonus > 0 || cardData.cooldownReduction > 0 || cardData.immunityBonus > 0, "Ineffective card");
                }
            }
            finally { FeriaSession.End(); }
        }
        [MenuItem("HEXENKNALL/Feria/Build Windows x64")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)throw new Exception("Stop Play before building.");
            string folder = "Builds/Feria/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(folder);
            var scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path);
            var report = BuildPipeline.BuildPlayer(scenes, folder+"/Hexenknall.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            Directory.CreateDirectory("Logs/Feria");
            File.WriteAllText("Logs/Feria/build.txt", report.summary.result+"\n"+folder+"\nErrors: "+report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded)throw new Exception("Build failed");
        }
    }
}
