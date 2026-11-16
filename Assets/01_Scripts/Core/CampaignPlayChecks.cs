#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Labyrinth
{
    // Fixture de campaña: daño controlado para transiciones, sin tocar partidas reales.
    public sealed class CampaignPlayChecks:MonoBehaviour
    {
        const string Folder="Logs/MejoraCampana";
        string report="Campaign Play Mode fixture: real keyboard events, controlled kills for progression; not human play.\n";
        string save,backup;
        Keyboard keyboard;
        InputSettings original,temporary;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(!SessionState.GetBool("Campaign.PlayChecks",false))return;
            SessionState.SetBool("Campaign.PlayChecks",false);
            var obj=new GameObject("Campaign verification (editor only)");DontDestroyOnLoad(obj);obj.AddComponent<CampaignPlayChecks>();
        }
        void Check(bool value,string message)
        {
            if(!value)throw new Exception(message);
            report+="PASS "+message+"\n";File.WriteAllText(Folder+"/play.txt",report);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            save=PlayerPrefs.GetString("Labyrinth.Run.v2","");backup=PlayerPrefs.GetString("Labyrinth.Run.v2.backup","");
            DifficultyRules.TestSelection=DifficultyRules.Easy;
            RunProgress.TestData=RunProgress.NewAdventureData();
            original=InputSystem.settings;temporary=Instantiate(original);InputSystem.settings=temporary;
            temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();Application.runInBackground=true;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object current=null;bool more=false;Exception failure=null;
                try { more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current; }catch(Exception e) { failure=e; }
                if(failure!=null) { report+="FAIL "+failure+"\n"; Finish(false);yield break; }
                if(!more) { stack.Pop();continue; }
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            Check(save==PlayerPrefs.GetString("Labyrinth.Run.v2","") && backup==PlayerPrefs.GetString("Labyrinth.Run.v2.backup",""),"Real campaign primary and backup unchanged");
            Finish(true);
        }
        void Finish(bool success)
        {
            report+=success?"COMPLETE CAMPAIGN FIXTURE\n":"INCOMPLETE\n";File.WriteAllText(Folder+"/play.txt",report);
            // Mantiene el aislamiento hasta cerrar el proceso: OnApplicationQuit no debe guardar datos reales.
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(original)InputSystem.settings=original;
            if(temporary)Destroy(temporary);Time.timeScale=1;
            EditorApplication.Exit(success?0:1);
        }
        IEnumerator Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator Ready(bool closeDialogue=true)
        {
            yield return null;yield return null;yield return null;yield return null;
            var g=GameDirector.I;Check(g!=null,"Scene director loaded");
            if(closeDialogue)while(g.dialogue)
            {
                typeof(GameUI).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g.ui,null);yield return null;
            }
            yield return null;
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            // Módulo incorporado del editor; no cambia dependencias del proyecto.
            var assembly=Assembly.LoadFrom(Path.Combine(EditorApplication.applicationContentsPath,"Managed/UnityEngine/UnityEngine.ScreenCaptureModule.dll"));
            var capture=assembly.GetType("UnityEngine.ScreenCapture").GetMethod("CaptureScreenshot",new[]{typeof(string),typeof(int)});
            capture.Invoke(null,new object[]{Folder+"/"+name+".png",1});
            yield return new WaitForSecondsRealtime(.35f);
        }
        string Layout()=>string.Join(";",GameDirector.I.board.blocks.Keys.OrderBy(p=>p.x).ThenBy(p=>p.y));
        void ClearDanger()
        {
            var g=GameDirector.I;
            foreach(var b in g.board.bombs.Values.ToArray())if(b)Destroy(b.gameObject);
            g.board.bombs.Clear();
            foreach(var h in g.board.hazards.ToArray())if(h)Destroy(h.gameObject);
            g.board.hazards.Clear();
        }
        IEnumerator Run()
        {
            yield return Ready();var g=GameDirector.I;
            yield return Capture("01-menu");
            typeof(GameUI).GetField("settings",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.ui,true);
            yield return Capture("01b-settings");
            typeof(GameUI).GetField("settings",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.ui,false);
            // Menú y Jugar reales; datos de prueba redirigidos en memoria.
            g.NewGame();yield return Ready();g=GameDirector.I;
            Check(g.stage==0,"Jugar enters original prologue");
            yield return Capture("02-prologue");
            typeof(GameDirector).GetField("elapsed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,g.prologueDuration);
            yield return null;yield return Ready();yield return Ready();g=GameDirector.I;
            Check(g.stage==1 && !g.ui.defeat,"Narrative prologue fall opens Level01 without defeat screen");
            Check(g.player.health==5 && Mathf.Approximately(g.player.speed,4) && g.player.invulnerability>=1.2f,"Easy player base health speed immunity");
            Check(g.player.RayUnlocked && !g.player.SpecialUnlocked && !RunProgress.Data.rayUnlocked,"Easy X guaranteed without permanent unlock");
            Check(Mathf.Approximately(g.player.normalFuse,1.5f) && Mathf.Approximately(g.player.chargeTime,.3f) && Mathf.Approximately(g.player.rayCooldown,2.5f),"Easy Z and X timings");
            var enemy=g.board.actors.OfType<EnemyController>().First();
            Check(Mathf.Approximately(enemy.speed,1.92f) && enemy.meleeWindup>=.6f && Mathf.Approximately(enemy.reactionMin,.8f) && Mathf.Approximately(enemy.reactionMax,1.15f),"Easy enemy speed and reaction profile");
            string layout=Layout();int seed=RunProgress.Data.layoutSeed;
            yield return Press(Key.LeftArrow);Check(g.player.facing==Vector2Int.left && !g.player.Moving,"Blocked direction turns player");
            yield return Press(Key.X);Check(g.player.RayRemaining>0,"X casts in displayed direction");
            yield return Press(Key.Escape);Check(g.paused && Time.timeScale==0,"Pause freezes time");
            yield return Press(Key.Escape);Check(!g.paused && Time.timeScale==1 && !g.player.BombQueued,"Resume consumes menu input");
            g.Load("Level01");yield return Ready(false);g=GameDirector.I;
            Check(!g.dialogue && Layout()==layout && RunProgress.Data.layoutSeed==seed,"Retry preserves seed and skips repeated opening");
            g.Continue();yield return Ready();g=GameDirector.I;
            Check(g.stage==1 && Layout()==layout && RunProgress.Data.layoutSeed==seed,"Continue enters campaign with same initial distribution");
            yield return new WaitForSeconds(2.1f);
            ClearDanger();
            var shared=new HashSet<GridActor>();
            var own=new GameObject("Own hazard test").AddComponent<Hazard>();own.bombDamage=true;own.playerOwned=true;own.hitActors=shared;
            var rival=new GameObject("Enemy hazard test").AddComponent<Hazard>();rival.bombDamage=true;rival.playerOwned=false;rival.hitActors=shared;rival.cause="Bomba rival verificada";
            int life=g.player.health;
            Check(!own.ApplyTo(g.player) && !shared.Contains(g.player) && g.player.health==life,"Easy own explosion harmless and does not consume shared hit");
            Check(rival.ApplyTo(g.player) && g.player.health==life-1,"Mixed chain rival explosion still damages");
            Destroy(own.gameObject);Destroy(rival.gameObject);ClearDanger();
            yield return new WaitForSeconds(1.3f);
            foreach(var actor in g.board.actors.OfType<EnemyController>())actor.enabled=false;
            var neighbor=g.board.Blast(g.player.cell,1).First(p=>p!=g.player.cell && g.board.Floor(p) && !g.board.blocks.ContainsKey(p));
            life=g.player.health;
            Check(g.PlaceBomb(g.player.cell,20,2,1,g.player) && g.PlaceBomb(neighbor,20,2,1,enemy),"Place real mixed-chain bombs");
            var ownBomb=g.board.bombs[g.player.cell];var enemyBomb=g.board.bombs[neighbor];
            ownBomb.Detonate();var chain=ownBomb.explosion;
            Check(chain.bombs==2 && ownBomb.playerOwned && !enemyBomb.playerOwned && enemyBomb.explosion==chain,"Actual chain retains each bomb owner");
            yield return null;yield return null;
            Check(g.player.health==life-1,"Actual mixed-chain blast damages only via enemy source");yield return Capture("03b-chain");ClearDanger();
            yield return new WaitForSeconds(1.3f);
            g.player.health=2;g.orbChance=0;
            var crate=new GameObject("Player-broken crate fixture").AddComponent<Block>();crate.cell=g.player.cell;crate.destructible=true;g.board.blocks[crate.cell]=crate;
            Check(crate.Break(false,true),"Real player-broken crate produces recovery opportunity");
            var orb=FindObjectsByType<RecoveryOrb>().FirstOrDefault();Check(orb!=null,"Low-health help guaranteed when crate drops");
            Check((!orb.GetComponent<Collider>() || !orb.GetComponent<Collider>().enabled) && g.board.Floor(orb.cell),"Orb has valid cell and no blocking collider from first frame");
            g.Flash(orb.cell,Color.cyan,.85f,1,true,null,MagicKind.Normal,null,true);
            yield return new WaitForSeconds(.6f);Check(orb && g.player.health==2,"Orb survives creating explosion and waits for flame to finish");
            yield return new WaitForSeconds(.4f);Check(g.player.health==3,"Orb heals exactly one and does not block movement");
            g.player.health=2;g.TryDropOrb(g.player.cell);Check(FindObjectsByType<RecoveryOrb>().Length==0,"Low-health guarantee only once per room");
            g.player.health=5;
            yield return Capture("03-combat-easy");
            // Mort et retry réel, après expiration de l'immunité.
            yield return new WaitForSeconds(1.3f);g.player.Hit(99,false,"Daño controlado de comprobación");
            Check(g.ui.defeat && Time.timeScale==0,"Known death cause and defeat freeze");
            yield return Press(Key.Enter);yield return Ready();g=GameDirector.I;
            Check(g.player.health==5 && Layout()==layout,"Defeat retry restores same room");
            // Difficultés historiques : scènes rechargées, aucune carte.
            foreach(int difficulty in new[]{DifficultyRules.Hard,DifficultyRules.VeryHard})
            {
                DifficultyRules.TestSelection=difficulty;g.Load("Level01");yield return Ready();g=GameDirector.I;
                Check(g.player.maxHealth==3 && Mathf.Approximately(g.player.normalFuse,2) && Mathf.Approximately(g.player.chargeTime,1.2f) && Mathf.Approximately(g.player.rayCooldown,5) && Mathf.Approximately(g.player.specialFuse,3.5f),"Original timings and health difficulty "+difficulty);
                Check(!g.player.RayUnlocked && !g.player.SpecialUnlocked,"Original unlock policy "+difficulty);
                enemy=g.board.actors.OfType<EnemyController>().First();Check(Mathf.Approximately(enemy.speed,2.4f) && enemy.meleeWindup<.6f,"Original enemy balance "+difficulty);
                var h=new GameObject("Original self damage").AddComponent<Hazard>();h.bombDamage=true;h.playerOwned=true;
                int hp=g.player.health;Check(h.ApplyTo(g.player) && g.player.health==hp-1,"Original own-bomb damage "+difficulty);Destroy(h.gameObject);
                Check(DifficultyRules.RelativeControls==(difficulty==0) && DifficultyRules.RotateCamera,"Original controls and camera "+difficulty);
                g.orbChance=1;g.TryDropOrb(g.player.cell);Check(FindObjectsByType<RecoveryOrb>().Length==0,"Orbs disabled by default difficulty "+difficulty);
            }
            DifficultyRules.TestSelection=DifficultyRules.Easy;g.Load("Level01");yield return Ready();
            // Campagne complète : vraies morts ennemies contrôlées et événements non supprimés.
            for(int stage=1;stage<=10;stage++)
            {
                g=GameDirector.I;Check(g.stage==stage,"Campaign entered stage "+stage);
                if(stage>=3)Check(g.player.SpecialUnlocked,"Guaranteed C stage "+stage);
                if(stage==3)
                {
                    float cardFactor=Mathf.Max(.3f,1-RunProgress.Data.cooldownReduction+RunProgress.Data.cooldownPenalty);
                    Check(Mathf.Approximately(g.player.specialFuse,1) && Mathf.Approximately(g.player.specialCooldown,5*cardFactor),"Easy C base timing before acquired card modifiers");
                    yield return Press(Key.C);Check(g.player.Aiming && Mathf.Approximately(Time.timeScale,.3f),"C aims at 0.3 time");
                    yield return Press(Key.Escape);Check(!g.player.Aiming && !g.paused && Time.timeScale==1,"Cancel aim restores time without pause");
                    yield return Press(Key.C);g.paused=true;g.SetTime();Check(Time.timeScale==0,"Pause overrides slow aim");g.player.CancelAim();g.paused=false;g.SetTime();
                    yield return Press(Key.C);yield return Press(Key.Enter);Check(!g.player.Aiming && Time.timeScale==1,"Confirm C restores time");ClearDanger();
                    CardLedger.Rebuild(RunProgress.Data);g.player.RefreshRunBonuses();Check(g.player.SpecialUnlocked && g.player.RayUnlocked,"Guaranteed powers survive refuge recalculation");
                }
                if(stage==10)yield return Capture("06-boss");
                if(stage==10)
                {
                    var boss=g.board.actors.OfType<EnemyController>().First(a=>a.boss);
                    var attacks=boss.GetComponent<BossCombat>();
                    Check(boss.maxHealth==4 && attacks.warningTime>=1.15f && attacks.recoveryTime>=1.5f && attacks.dashRange==4,"Easy boss preserves health and readable attack profile");
                }
                // El jugador del fixture queda inmune solo durante la prueba del grafo completo.
                g.player.invincible=true;
                float deadline=Time.realtimeSinceStartup+65;
                int width=g.board.width,height=g.board.height;
                ExplosionContext context=new ExplosionContext();
                bool capturedExpansion=false;
                while(!g.finished && Time.realtimeSinceStartup<deadline)
                {
                    foreach(var a in g.board.actors.Where(a=>a && !a.player && a.health>0).ToArray())a.Hit(999,false,null,true,context);
                    yield return null;
                    if(g.board.width>width && !capturedExpansion) { capturedExpansion=true;yield return Capture("09-expanded-stage-"+stage); }
                }
                Check(g.finished,"Stage completes with all waves and expansions "+stage);
                Check(g.board.width<=width+4 && g.board.height<=height+4,"Easy expansion bounded +4 stage "+stage);
                yield return new WaitForSecondsRealtime(.6f);
                if(stage<10)yield return Ready();
                if(stage==1) { Check(context.kills.Count==2,"Player explosion context counts distinct real kills");yield return Capture("04-reward-free"); }
                if(stage<10)
                {
                    Check(g.rewards.Active && RunProgress.Data.rewardPhase==1,"Campaign reward opens stage "+stage);
                    if(stage==1) { g.Continue();yield return Ready();g=GameDirector.I;Check(g.rewards.Active && RunProgress.Data.rewardPhase==1,"Continue restores unfinished free-boon selection"); }
                    g.rewards.Choose(0);yield return null;
                    Check(RunProgress.Data.rewardPhase==3,"Free boon leads to optional pact stage "+stage);
                    if(stage==1)yield return Capture("05-pact-preview");
                    int acquired=RunProgress.Data.acquired.Count;
                    RunProgress.TestData=JsonUtility.FromJson<RunData>(JsonUtility.ToJson(RunProgress.Data));RunProgress.Normalize(RunProgress.Data);
                    Check(RunProgress.Data.acquired.Count==acquired && RunProgress.Data.rewardPhase==3,"Pending pact survives serialization stage "+stage);
                    if(stage==1) { g.Continue();yield return Ready();g=GameDirector.I;Check(g.rewards.Active && RunProgress.Data.rewardPhase==3 && RunProgress.Data.acquired.Count==acquired,"Continue restores pact without duplicating free boon"); }
                    if(stage%2==0)g.rewards.Choose(0);else g.rewards.ContinueWithoutPact();
                    yield return Ready();g=GameDirector.I;
                    if(stage==4)
                    {
                        Check(g.stage==Campaign.OldFriend,"Refuge retained in campaign order");yield return Capture("07-refuge");
                        g.player.cell=g.oldFriend.friendCell;
                        g.player.transform.position=g.board.World(g.oldFriend.friendCell);
                        g.player.GetComponent<Rigidbody>().position=g.board.World(g.oldFriend.friendCell);
                        Physics.SyncTransforms();
                        var refugeData=JsonUtility.ToJson(RunProgress.Data);
                        RunProgress.Data.acquired.RemoveAll(card=>card.curse);CardLedger.Rebuild(RunProgress.Data);
                        g.oldFriend.Interact();yield return Ready();
                        Check(RunProgress.Data.oldFriendResolved && !g.oldFriend.Choosing,"Curse-free refuge opens portal without requiring exchange");
                        RunProgress.TestData=JsonUtility.FromJson<RunData>(refugeData);g.player.RefreshRunBonuses();
                        g.oldFriend.Interact();yield return Ready();
                        if(g.oldFriend.Choosing) { g.oldFriend.Choose(0);yield return Ready(); }
                        Check(RunProgress.Data.oldFriendResolved,"Refuge conversation and exchange resolve");
                        g.player.cell=g.oldFriend.portalCell;
                        g.player.transform.position=g.board.World(g.oldFriend.portalCell);
                        // Le Rigidbody est cinématique, déplacer la position physique également.
                        g.player.GetComponent<Rigidbody>().position=g.board.World(g.oldFriend.portalCell);
                        Physics.SyncTransforms();
                        float portalDeadline=Time.realtimeSinceStartup+4;
                        while(GameDirector.I==g && Time.realtimeSinceStartup<portalDeadline)yield return null;
                        Check(GameDirector.I!=g,"Refuge portal loads Level05");yield return Ready();
                    }
                }
                else
                {
                    yield return Ready();g=GameDirector.I;
                    Check(g.stage==Campaign.Ending && RunProgress.Data.finalBossDefeated,"Full campaign reaches confirmed ending");
                    yield return Capture("08-ending");
                }
            }
        }
    }
}
#endif
