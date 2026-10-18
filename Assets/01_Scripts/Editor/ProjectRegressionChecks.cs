using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth.Editor
{
    // Comprueba que los cambios no rompan mecánicas anteriores.
    public static class ProjectRegressionChecks
    {
        static int checks;

        // Ejecuta la revisión de regresiones.
        static void Check(bool condition, string description)
        {
            if (!condition)throw new Exception("Regression failed: "+description);
            checks++;
        }
        [MenuItem("HEXENKNALL/Show saved checkpoint")]

        // Muestra el estado de una partida de prueba.
        public static void ShowCheckpoint()
        {
            if (!RunProgress.HasSave)
            {
                Debug.Log("No hay partida guardada.");
                return;
            }
            var d = RunProgress.Data;
            Debug.Log("Continuar: "+Campaign.Scene(RunProgress.ResumeStage(d))+" | recompensa: "+d.rewardPhase+" | pactos: "+d.history.Count+" | jefe derrotado: "+d.finalBossDefeated);
        }
        [MenuItem("HEXENKNALL/Run save and block checks")]

        // Prueba la progresión de la campaña.
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Deten Play primero.");
                return;
            }
            checks = 0;
            var random = UnityEngine.Random.state;
            var previous = GameDirector.I;
            float scale = Time.timeScale;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                // Estas pruebas no modifican el punto de control guardado.
                var start = RunProgress.NewAdventureData();
                Check(start.stage == 1 && RunProgress.ResumeStage(start) == 1, "prologue defeat commits Level01");
                Check(!start.rayUnlocked && !start.specialUnlocked, "powers are lost at the adventure start");
                var restored = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(start));
                RunProgress.Normalize(restored);
                Check(Campaign.Scene(RunProgress.ResumeStage(restored)) == "Level01", "serialized continuation returns to Level01");
                restored.stage = Campaign.Ending;
                RunProgress.Normalize(restored);
                Check(restored.stage == 1, "a scene stage cannot overwrite an established checkpoint");
                var broken = new RunData
                {
                    stage = Campaign.Ending, lifeBonus = 2, speedBonus = .2f
                };
                RunProgress.Normalize(broken);
                Check(broken.stage == 1 && !broken.finalBossDefeated, "legacy Ending with zero pacts is recovered to Level01");
                Check(broken.lifeBonus == 2 && Mathf.Approximately(broken.speedBonus, .2f), "recovery preserves existing benefits");
                var pending = RunProgress.NewAdventureData();
                pending.rewardStage = 1;
                pending.rewardPhase = 2;
                pending.chosenRarity = (int)Rarity.Rare;
                pending.offers = CardRules.Roll(pending, true, Rarity.Rare);
                restored = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(pending));
                RunProgress.Normalize(restored);
                Check(restored.rewardPhase == 2 && restored.offers.Count == 3, "pending curse survives reload");
                for (int i = 0;i<3;i++)Check(restored.offers[i].effect == pending.offers[i].effect && restored.offers[i].rarity == Rarity.Rare, "pending offers are not rerolled");
                Check(RunProgress.AdvanceAfterReward(restored) && restored.stage == 2 && restored.rewardPhase == 0, "completed pair commits next stage");
                restored = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(restored));
                RunProgress.Normalize(restored);
                Check(RunProgress.ResumeStage(restored) == 2, "next stage survives serialization");
                var finished = RunProgress.NewAdventureData();
                finished.finalBossDefeated = true;
                RunProgress.Normalize(finished);
                Check(RunProgress.ResumeStage(finished) == Campaign.Ending, "confirmed boss victory permits ending");
                foreach (string name in Campaign.AllScenes()) Check(Campaign.TryStage(name, out var stage) && Campaign.Scene(stage) == name, "scene name/stage mapping: "+name);
                Check(!Campaign.TryStage("Unknown", out _), "unrelated scene is not a campaign stage");
                var root = new GameObject("Block regression");
                SceneManager.MoveGameObjectToScene(root, scene);
                var director = root.AddComponent<GameDirector>();
                var board = root.AddComponent<GridBoard>();
                director.board = board;
                GameDirector.I = director;
                Block Put(Vector2Int p, bool destructible)
                {
                    var obj = new GameObject("Test block");
                    obj.transform.SetParent(root.transform);
                    var block = obj.AddComponent<Block>();
                    block.cell = p;
                    block.destructible = destructible;
                    board.blocks[p] = block;
                    return block;
                }
                var cell = new Vector2Int(2, 1);
                var crate = Put(cell, true);
                var blast = board.Blast(new Vector2Int(1, 1), 3);
                Check(blast.Contains(cell) && !blast.Contains(new Vector2Int(3, 1)), "crate receives blast and stops propagation");
                Check(crate.Break(false) && !board.blocks.ContainsKey(cell), "normal Z bomb destroys a crate and releases its cell");
                crate = Put(cell, true);
                Check(crate.Break(true) && !board.blocks.ContainsKey(cell), "magic destroys a crate");
                var wall = Put(cell, false);
                Check(!wall.Break(false) && !wall.Break(true) && board.blocks.ContainsKey(cell), "perimeter/interior walls resist bombs and magic");
                var stale = Put(new Vector2Int(3, 1), true);
                var replacement = Put(stale.cell, false);
                Check(!stale.Break(false) && board.blocks[stale.cell] == replacement, "stale block cannot delete its replacement");
                Debug.Log("PASS: "+checks+" comprobaciones de guardado y bloques. No se ha modificado la partida del usuario. Revisa tambien Play Mode.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                GameDirector.I = previous;
                Time.timeScale = scale;
                UnityEngine.Random.state = random;
            }
        }
    }
}
