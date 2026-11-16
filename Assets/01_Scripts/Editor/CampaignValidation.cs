using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
namespace Labyrinth.Editor
{
    public static class CampaignValidation
    {
        public const string Folder="Logs/MejoraCampana";
        static void Require(bool value,string text) { if(!value)throw new Exception(text); }
        [MenuItem("HEXENKNALL/Campaign/Checks")]
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            string report="Unity "+Application.unityVersion+"\n";
            Action<string,Action> check=(name,action)=> { try { action(); report+="PASS "+name+"\n"; }catch(Exception e) { report+="FAIL "+name+": "+e+"\n"; } };
            var selected=DifficultyRules.TestSelection;
            DifficultyRules.TestSelection=DifficultyRules.Hard;
            try
            {
                check("Gameplay",GameplayChecks.Run);
                check("Balance and layouts",BalanceChecks.Run);
                check("Save and block regression",ProjectRegressionChecks.Run);
                check("Old Friend and dash",HexenknallChecks.Run);
                check("Integrated art",IntegrationChecks.Validate);
                check("Animation bindings",AnimationBindingChecks.Run);
                check("Campaign rewards migration and difficulty",Rules);
                check("Serialized scenes",Scenes);
            }
            finally { DifficultyRules.TestSelection=selected; }
            File.WriteAllText(Folder+"/checks.txt",report); Debug.Log(report);
            if(report.Contains("FAIL"))throw new Exception("Campaign checks failed");
        }
        static void Rules()
        {
            Require(DifficultyRules.Resolve(true,0,true,1)==0,"Preserve original Hard");
            Require(DifficultyRules.Resolve(true,1,true,1)==1,"Preserve original VeryHard");
            Require(DifficultyRules.Resolve(false,-1,true,1)==2 && DifficultyRules.Resolve(false,-1,false,-1)==2,"New and previous-profile-only default Easy");
            for(int level=1;level<=10;level++)
            {
                var d=RunProgress.NewAdventureData();
                Require(DifficultyRules.RayAvailable(d,level,true),"Guaranteed X");
                Require(DifficultyRules.SpecialAvailable(d,level,true)==(level>=3),"Guaranteed C from Level03");
                Require(!DifficultyRules.RayAvailable(d,level,false) && !DifficultyRules.SpecialAvailable(d,level,false),"No unlock leakage");
            }
            foreach(bool easy in new[]{false,true})foreach(bool pact in new[]{false,true})
            {
                var d=RunProgress.NewAdventureData();
                for(int stage=1;stage<=9;stage++)
                {
                    d.stage=d.checkpointStage=d.rewardStage=stage; d.rewardPhase=1; d.easyReward=easy;
                    d.offers=CardRules.Roll(d,false);
                    Require(RewardManager.ApplyChoice(d,0),"Choose boon");
                    int count=d.acquired.Count;
                    string json=JsonUtility.ToJson(d);
                    d=JsonUtility.FromJson<RunData>(json); RunProgress.Normalize(d);
                    Require(d.acquired.Count==count,"Reload cannot duplicate boon");
                    Require(d.rewardPhase==(easy?3:2),"Persist reward policy");
                    if(easy && !pact)Require(RunProgress.AdvanceAfterReward(d),"Skip optional curse");
                    else Require(RewardManager.ApplyChoice(d,0),"Apply explicit pact or original curse");
                    Require(d.rewardPhase==0 && !RewardManager.ApplyChoice(d,0),"Reward cannot repeat");
                    Require(d.checkpointStage==(stage==4 ? Campaign.OldFriend : stage+1),"Full campaign order");
                    if(stage==4) { Require(!easy || pact || CardLedger.Count(d,true)==0,"Refuge accepts no curses"); d.oldFriendVisited=true; }
                    RunProgress.Normalize(d);
                    Require(d.completedNormalStages>=stage,"Progress independent of curses");
                }
                Require(d.checkpointStage==Campaign.FinalBoss,"Nine levels reach boss");
            }
            var saturated=RunProgress.NewAdventureData();
            saturated.lifeBonus=8;saturated.speedBonus=.8f;saturated.immunityBonus=1.2f;saturated.cooldownReduction=.6f;saturated.bombRadiusBonus=5;saturated.bombCapacityBonus=6;saturated.rayRangeBonus=5;saturated.specialRadiusBonus=5;saturated.rayUnlocked=saturated.specialUnlocked=true;
            Require(CardRules.Roll(saturated,false).Count==0,"No useless saturated cards");
            var modern=RunProgress.NewAdventureData();modern.stage=modern.checkpointStage=6;modern.completedNormalStages=5;modern.layoutSeed=1234;
            modern=JsonUtility.FromJson<RunData>(JsonUtility.ToJson(modern));RunProgress.Normalize(modern);
            Require(modern.stage==6 && modern.layoutSeed==1234,"Seed and curse-free checkpoint persist");
        }
        static void Scenes()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var entry in EditorBuildSettings.scenes)
                {
                    var scene=EditorSceneManager.OpenScene(entry.path);
                    GameDirector director=null;
                    foreach(var root in scene.GetRootGameObjects())
                    {
                        foreach(var t in root.GetComponentsInChildren<Transform>(true))Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script "+entry.path);
                        if(root.GetComponent<GameDirector>())director=root.GetComponent<GameDirector>();
                    }
                    Require(director && director.bombPrefab && director.meleePrefab && director.bomberPrefab && director.wallPrefab,"Scene references "+entry.path);
                    Require(director.bombPrefab.GetComponent<Bomb>() && director.meleePrefab.GetComponent<EnemyController>(),"Prefab scripts");
                }
            }
            finally { if(Array.Exists(setup,s=>s.isActive && s.isLoaded))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); }
        }
        public static void Play()
        {
            SessionState.SetBool("Campaign.PlayChecks",true);
            EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
            var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Show();gameView.Focus();
            EditorApplication.isPlaying=true;
        }
        public static void RunAndBuild() { Run(); Build(); }
        [MenuItem("HEXENKNALL/Campaign/Build Windows x64")]
        public static void Build()
        {
            var path="Builds/MejoraCampana/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(path);
            var scenes=Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled),s=>s.path);
            var result=BuildPipeline.BuildPlayer(scenes,path+"/Hexenknall.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            File.WriteAllText(Folder+"/build.txt",result.summary.result+"\n"+path+"\nErrors: "+result.summary.totalErrors);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed");
        }
    }
}
