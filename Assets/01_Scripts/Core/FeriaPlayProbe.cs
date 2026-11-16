#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Labyrinth
{
    // Fixture opt-in del editor. No forma parte del Player de feria.
    public sealed class FeriaPlayProbe : MonoBehaviour
    {
        Keyboard keyboard;
        InputSettings originalInputSettings, testInputSettings;
        bool botMode;
        string ReportPath => botMode ? "Logs/Feria/bot.txt" : "Logs/Feria/play.txt";
        string report = "Play Mode: controles reales + daños controlados para comprobar transiciones.\n";
        string primary, backup;
        const string KeyName = "Labyrinth.Run.v2";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!SessionState.GetBool("Feria.RunPlayChecks", false))return;
            SessionState.SetBool("Feria.RunPlayChecks", false);
            var obj = new GameObject("Feria play checks (editor only)");
            DontDestroyOnLoad(obj);
            obj.AddComponent<FeriaPlayProbe>();
        }
        void Check(bool value, string message)
        {
            if (!value)throw new Exception(message);
            report += "PASS "+message+"\n";
            File.WriteAllText(ReportPath, report);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Logs/Feria");
            Application.runInBackground = true;
            primary = PlayerPrefs.GetString(KeyName, "");
            backup = PlayerPrefs.GetString(KeyName+".backup", "");
            originalInputSettings = InputSystem.settings;
            testInputSettings = Instantiate(originalInputSettings);
            InputSystem.settings = testInputSettings;
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            bool both = SessionState.GetBool("Feria.Both", false);
            SessionState.SetBool("Feria.Both", false);
            var modes = both ? new[] { false, true } : new[] { SessionState.GetBool("Feria.Bot", false) };
            foreach (bool bot in modes)
            {
                botMode = bot;
                report = bot ? "Unattended bot: normal player health, real keyboard events, no controlled damage.\n" : "Play Mode: controles reales + daños controlados para comprobar transiciones.\n";
                var routine = bot ? RunBot() : Run();
                while (true)
                {
                    object current;
                    try { if (!routine.MoveNext())break; current = routine.Current; }
                    catch (Exception e) { report += "FAIL "+e; File.WriteAllText(ReportPath, report); Cleanup(); yield break; }
                    yield return current;
                }
                report += bot ? "COMPLETE BOT (not human playthrough)\n" : "COMPLETE (fixture, not human playthrough)\n";
                File.WriteAllText(ReportPath, report);
            }
            Cleanup();
        }
        void Cleanup()
        {
            if (keyboard != null)InputSystem.RemoveDevice(keyboard);
            if (originalInputSettings)InputSystem.settings = originalInputSettings;
            if (testInputSettings)Destroy(testInputSettings);
            Time.timeScale = 1;
            if (Application.isBatchMode) { EditorApplication.Exit(report.Contains("FAIL") ? 1 : 0); return; }
            EditorApplication.isPlaying = false;
        }
        IEnumerator Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return null;
        }
        IEnumerator SceneReady()
        {
            yield return null; yield return null; yield return null;
            float until = Time.realtimeSinceStartup+20;
            while ((!GameDirector.I || !GameDirector.I.player) && Time.realtimeSinceStartup<until)yield return null;
            yield return null; yield return null;
            var g = GameDirector.I;
            while (g.dialogue)
            {
                typeof(GameUI).GetMethod("Advance", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g.ui, null);
                yield return null;
            }
            yield return null;
        }
        string Fingerprint() => string.Join(";", GameDirector.I.board.blocks.Keys.OrderBy(p=>p.x).ThenBy(p=>p.y));
        static Key MoveKey(Vector2Int d) => d == Vector2Int.up ? Key.UpArrow : d == Vector2Int.down ? Key.DownArrow : d == Vector2Int.left ? Key.LeftArrow : Key.RightArrow;
        IEnumerator RunBot()
        {
            yield return null; yield return null;
            FeriaSession.Begin(); GameDirector.I.Load("Level01");
            yield return SceneReady();
            float deadline = Time.realtimeSinceStartup+480;
            float bombAt = 0;
            int lastStage = -1, retries = 0;
            while (!FeriaSession.Won && Time.realtimeSinceStartup < deadline)
            {
                var g = GameDirector.I;
                if (!g || !g.player) { yield return null; continue; }
                if (lastStage != g.stage)
                {
                    yield return SceneReady();
                    g = GameDirector.I; lastStage = g.stage;
                    Check(true, "Bot enters stage "+g.stage+" with health "+g.player.health);
                }
                if (g.dialogue) { yield return SceneReady(); continue; }
                if (FeriaSession.Choosing) { FeriaSession.Choose(0); yield return SceneReady(); continue; }
                if (g.ui.defeat)
                {
                    Check(++retries <= 2, "Bot retry budget: "+retries);
                    g.Load(Campaign.Scene(g.stage)); yield return SceneReady(); continue;
                }
                if (g.finished) { yield return null; continue; }
                var p = g.player; var board = g.board;
                if (g.Training)
                {
                    if (g.stage == 1)
                    {
                        if (FeriaSession.Lesson == 0)yield return Press(Key.Z);
                        else yield return Press(Key.X);
                    }
                    else
                    {
                        yield return Press(Key.C);
                        yield return Press(Key.Enter);
                    }
                    continue;
                }
                if (p.Moving) { yield return null; continue; }
                var enemy = board.actors.Where(a=>a && !a.player && a.health>0).OrderBy(a=>(a.cell-p.cell).sqrMagnitude).FirstOrDefault();
                var keys = new List<Key>();
                var next = p.cell;
                bool danger = board.Danger(p.cell) || board.hazards.Any(h=>h && (h.cell-p.cell).sqrMagnitude<2);
                if (danger)next = board.NextStep(p, p.cell, true);
                else if (enemy)
                {
                    var diff = enemy.cell-p.cell;
                    int distance = Mathf.Abs(diff.x)+Mathf.Abs(diff.y);
                    bool aligned = diff.x == 0 || diff.y == 0;
                    if (aligned && distance<=p.rayRange && p.RayRemaining <= 0)
                    {
                        var face = new Vector2Int(Math.Sign(diff.x),Math.Sign(diff.y));
                        keys.Add(MoveKey(face)); keys.Add(Key.X);
                    }
                    if (distance<=3 && Time.time>bombAt && board.BombCount(p)<2 && g.CanEscapeBomb(p,p.normalRadius))
                    {
                        keys.Add(Key.Z); bombAt=Time.time+1.5f;
                    }
                    if (distance<=2)
                    {
                        // Mantiene una salida y deja al enemigo entrar en los sellos.
                        int score = -1;
                        foreach (var d in GridBoard.Directions)
                        {
                            var cell = p.cell+d;
                            if (!board.Free(cell,p) || board.Danger(cell))continue;
                            int candidate = (cell-enemy.cell).sqrMagnitude;
                            if (candidate>score) { score=candidate; next=cell; }
                        }
                    }
                    else if (keys.Count == 0)next = board.NextStep(p,enemy.cell,false);
                }
                if (keys.Count == 0 && next != p.cell)keys.Add(MoveKey(next-p.cell));
                if (keys.Count > 0)yield return Press(keys.ToArray());
                else yield return null;
            }
            Check(FeriaSession.Won, "Bot completes demo to victory using gameplay controls");
            Check(primary == PlayerPrefs.GetString(KeyName, "") && backup == PlayerPrefs.GetString(KeyName+".backup", ""), "Bot leaves campaign unchanged");
        }
        IEnumerator Run()
        {
            yield return null; yield return null;
            FeriaSession.Begin();
            GameDirector.I.Load("Level01");
            yield return SceneReady();
            var g = GameDirector.I;
            Check(g.stage == 1 && g.Training && g.player.health == 5, "Demo enters safe Level01 with five health");
            Check(!g.player.SpecialUnlocked && !g.player.RayUnlocked, "Z taught before X and C");
            Check(g.board.actors.OfType<EnemyController>().All(e=>e.meleeWindup>=.55f && Mathf.Approximately(e.reactionMin,.65f)), "Accessible enemy settings applied");
            string initial = Fingerprint(); int seed = RunProgress.Data.layoutSeed;
            yield return Press(Key.LeftArrow);
            Check(g.player.facing == Vector2Int.left && g.player.cell == new Vector2Int(1,1), "Blocked direction turns without moving");
            yield return Press(Key.Z);
            Check(FeriaSession.Lesson == 1 && g.board.BombCount(g.player) == 1 && g.player.RayUnlocked, "Z input places real bomb and unlocks X");
            yield return Press(Key.X);
            Check(FeriaSession.Lesson == 2 && !g.Training, "X input creates seal and starts first combat");
            g.Load("Level01"); yield return SceneReady(); g = GameDirector.I;
            Check(Fingerprint() == initial && RunProgress.Data.layoutSeed == seed, "Retry retains initial walls and crates");
            yield return Press(Key.Escape);
            Check(g.paused && Time.timeScale == 0, "Escape pauses");
            yield return Press(Key.Escape);
            Check(!g.paused && Time.timeScale == 1 && !g.player.BombQueued, "Unpause consumes input and clears actions");
            yield return new WaitForSecondsRealtime(2.2f);
            g.player.Hit(999, false, "Daño controlado de validación.");
            Check(g.ui.defeat && g.finished && Time.timeScale == 0 && g.deathCause.Contains("controlado"), "Defeat records known cause and freezes time");
            g.Load("Level01"); yield return SceneReady(); g = GameDirector.I;
            Check(!g.ui.defeat && g.player.health == 5 && Fingerprint() == initial, "Defeat retry restores health and layout");
            // Las bajas siguientes usan daño controlado: verifican el grafo, no el balance humano.
            foreach (var enemy in g.board.actors.Where(a=>a && !a.player).ToArray())enemy.Hit(999);
            yield return new WaitForSecondsRealtime(1.5f);
            Check(FeriaSession.Choosing && g.finished && Time.timeScale == 0, "First combat opens free card choice");
            FeriaSession.Choose(2); yield return SceneReady(); g = GameDirector.I;
            Check(g.stage == 2 && RunProgress.Data.shieldChosen, "Shield card advances to changing maze");
            var events = g.GetComponent<StageEventManager>();
            Check(events.dynamicStage && events.PendingWaves == 1 && !events.expansionEnabled, "Changing maze has bounded size and pending wave");
            float angle = Camera.main.GetComponent<OrbitCamera>().angle;
            foreach (var enemy in g.board.actors.Where(a=>a && !a.player).ToArray())enemy.Hit(999);
            yield return new WaitForSecondsRealtime(12);
            Check(GameDirector.I == g && !g.finished && events.PendingWaves == 0 && g.Enemies > 0, "Empty room waits for wall event and live wave");
            Check(Camera.main.GetComponent<OrbitCamera>().angle == angle, "Wall transformation retains stable camera");
            foreach (var enemy in g.board.actors.Where(a=>a && !a.player).ToArray())enemy.Hit(999);
            yield return new WaitForSecondsRealtime(2);
            yield return SceneReady(); g = GameDirector.I;
            Check(g.stage == Campaign.FinalBoss && g.player.SpecialUnlocked && g.Training, "Boss entry teaches C before combat");
            yield return Press(Key.C);
            Check(g.player.Aiming && Mathf.Approximately(Time.timeScale,.2f), "C slows combat while aiming");
            yield return Press(Key.Escape);
            Check(!g.player.Aiming && !g.paused && Time.timeScale == 1, "Aim cancel restores time without accidental pause");
            yield return Press(Key.C);
            g.paused = true; g.SetTime();
            Check(Time.timeScale == 0, "Pause overrides slow aim");
            g.player.CancelAim(); g.paused = false; g.SetTime();
            Check(Time.timeScale == 1, "Resume after cancelling paused aim restores time");
            yield return Press(Key.C);
            yield return Press(Key.Enter);
            Check(!g.player.Aiming && Time.timeScale == 1 && FeriaSession.Lesson == 3, "C confirmation places bomb and ends lesson");
            // Aislar cadenas de los actores para comprobar explosión inmediata compartida.
            Vector2Int? first = null, second = null;
            foreach (var p in ProceduralLayout.Interior(g.board.width,g.board.height))
            {
                if (!g.board.Free(p))continue;
                foreach (var d in GridBoard.Directions)if(g.board.Free(p+d)) { first=p; second=p+d; break; }
                if (first.HasValue)break;
            }
            Check(first.HasValue, "Chain fixture has two free cells");
            g.PlaceBomb(first.Value, 10, 2, 1, g.player);
            g.PlaceBomb(second.Value, 10, 2, 1, null);
            g.board.bombs[first.Value].Detonate();
            Check(!g.board.bombs.ContainsKey(first.Value) && !g.board.bombs.ContainsKey(second.Value), "Bomb blast triggers chain immediately");
            var boss = g.board.actors.OfType<EnemyController>().First(a=>a.boss);
            var combat = boss.GetComponent<BossCombat>();
            Check(combat.warningTime>=1.15f && combat.recoveryTime>=1.5f && combat.dashRange == 4, "Boss announces bounded dash and exploitable recovery");
            foreach (var enemy in g.board.actors.Where(a=>a && !a.player).ToArray())enemy.Hit(999);
            yield return new WaitForSecondsRealtime(1.5f);
            Check(FeriaSession.Won && g.finished && Time.timeScale == 0, "Boss victory completes demo without campaign reward");
            FeriaSession.Begin(); g.Load("Level01"); yield return SceneReady(); g=GameDirector.I;
            Check(FeriaSession.Lesson == 0 && !RunProgress.Data.shieldChosen && g.player.health == 5, "New visitor receives clean demo");
            g.Load("MainMenu"); yield return null; yield return null; yield return null;
            Check(!FeriaSession.Active && Time.timeScale == 1, "Return to menu exits demo and restores time");
            Check(primary == PlayerPrefs.GetString(KeyName, "") && backup == PlayerPrefs.GetString(KeyName+".backup", ""), "Campaign primary and backup unchanged through complete fixture");
        }
    }
}
#endif

