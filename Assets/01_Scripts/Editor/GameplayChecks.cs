using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth.Editor
{
    // Prueba las mecánicas en una escena temporal del editor.
    // Prueba las reglas del juego desde el editor.
    public static class GameplayChecks
    {
        static int checks;

        // Ejecuta las pruebas de juego.
        static void Check(bool condition, string message)
        {
            if (!condition)throw new Exception("FAIL: "+message);
            checks++;
        }
        [MenuItem("HEXENKNALL/Run gameplay logic checks")]

        // Revisa las mecánicas en una escena temporal.
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Deten Play para ejecutar las comprobaciones.");
                return;
            }
            checks = 0;
            var savedRandom = UnityEngine.Random.state;
            var previousDirector = GameDirector.I;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Logic checks");
                SceneManager.MoveGameObjectToScene(root, preview);
                var board = root.AddComponent<GridBoard>();
                board.width = 7;
                board.height = 5;
                var director = root.AddComponent<GameDirector>();
                director.board = board;
                GameDirector.I = director;
                var a = Child<GridActor>(root, "Actor A");
                a.cell = new Vector2Int(1, 1);
                a.speed = 4;
                a.health = 3;
                a.transform.position = board.World(a.cell);
                board.actors.Add(a);
                var b = Child<GridActor>(root, "Actor B");
                b.cell = new Vector2Int(3, 1);
                b.health = 3;
                board.actors.Add(b);
                Check(!a.Step(new Vector2Int(1, 1)), "No diagonal steps");
                Check(a.Step(Vector2Int.right), "First cardinal step starts");
                Check(a.cell == new Vector2Int(1, 1) && a.Destination == new Vector2Int(2, 1), "Cell changes only on arrival");
                Check(!a.Step(Vector2Int.up), "No mid-step direction changes");
                Check(!b.Step(Vector2Int.left), "Moving destination reserved");
                a.AdvanceMovement(.1f);
                Check(Mathf.Abs(a.transform.position.x-1.4f)<.001f && a.Moving, "Continuous travel at four cells/second");
                a.AdvanceMovement(.15f);
                Check(a.cell == new Vector2Int(2, 1) && !a.Moving, "Arrival commits cell");
                Check(board.Free(new Vector2Int(1, 1)), "Old cell released");
                board.actors.Clear();
                Check(board.CanClose(new Vector2Int(2, 2)), "Safe closure on open grid");
                for (int x = 0;x<7;x++)if(x != 3)PutBlock(root, board, new Vector2Int(x, 2), false);
                Check(!board.CanClose(new Vector2Int(3, 2)), "Reject closure of sole connecting path");
                PutBlock(root, board, new Vector2Int(3, 2), false);
                Check(board.CanClose(new Vector2Int(2, 1)), "Unrelated disconnected area does not veto safe closure");
                board.blocks.Clear();
                PutBlock(root, board, new Vector2Int(3, 2), true);
                PutBlock(root, board, new Vector2Int(2, 4), false);
                var blast = board.Blast(new Vector2Int(2, 2), 4);
                Check(blast.Contains(new Vector2Int(3, 2)) && !blast.Contains(new Vector2Int(4, 2)), "Crate stops blast after receiving damage");
                Check(!blast.Contains(new Vector2Int(2, 4)), "Indestructible wall stops blast");
                board.terrainReservations.Add(new Vector2Int(4, 1));
                Check(!board.Free(new Vector2Int(4, 1)), "Expansion cells unavailable until ready");
                Check(Campaign.AllScenes().Length == 14 && Campaign.Scene(9) == "Level09" && Campaign.Scene(10) == "FinalBoss", "Expanded campaign mapping");
                UnityEngine.Random.InitState(86291);
                for (int run = 0;run<200;run++)
                {
                    var data = new RunData();
                    Check(!data.rayUnlocked && !data.specialUnlocked, "Adventure starts without X/C");
                    for (int stage = 1;stage<=9;stage++)
                    {
                        var boons = CardRules.Roll(data, false);
                        Check(boons.Count == 3, "Three benefit cards");
                        Check(new HashSet<CardEffect>
                        {
                            boons[0].effect, boons[1].effect, boons[2].effect
                        }
                        .Count == 3, "Distinct benefit choices");
                        var chosen = boons[UnityEngine.Random.Range(0, 3)];
                        CardRules.Apply(chosen, data);
                        var curses = CardRules.Roll(data, true, chosen.rarity);
                        Check(curses.Count == 3, "Three curse cards");
                        foreach (var curse in curses)Check(curse.curse && curse.rarity == chosen.rarity, "Every curse matches benefit rarity");
                        Check(new HashSet<CardEffect>
                        {
                            curses[0].effect, curses[1].effect, curses[2].effect
                        }
                        .Count == 3, "Distinct curse choices");
                        data.offers = curses;
                        data.rewardStage = stage;
                        data.rewardPhase = 2;
                        var restored = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(data));
                        Check(restored.offers.Count == 3 && restored.rewardPhase == 2 && restored.offers[0].effect == curses[0].effect, "Pending curse survives serialization");
                        CardRules.Apply(curses[UnityEngine.Random.Range(0, 3)], data);
                    }
                }
                Debug.Log("PASS: "+checks+" gameplay logic assertions. Rendering, input timing, animation and Play Mode still require a manual test.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                GameDirector.I = previousDirector;
                UnityEngine.Random.state = savedRandom;
            }
        }
        static T Child<T>(GameObject root, string name) where T:Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(root.transform);
            return obj.AddComponent<T>();
        }
        static void PutBlock(GameObject root, GridBoard board, Vector2Int cell, bool destructible)
        {
            var block = Child<Block>(root, "Block");
            block.cell = cell;
            block.destructible = destructible;
            board.blocks[cell] = block;
        }
    }
}
