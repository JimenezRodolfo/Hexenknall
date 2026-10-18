using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth.Editor
{
    // Revisa la configuración general del proyecto.
    public static class HexenknallChecks
    {
        // Inicia el diagnóstico del proyecto.
        static void Check(bool value, string message)
        {
            if (!value)throw new Exception("HEXENKNALL: "+message);
        }
        [MenuItem("HEXENKNALL/Run Old Friend and dash checks")]

        // Revisa las dependencias del juego.
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)return;
            var random = UnityEngine.Random.state;
            try
            {
                var d = RunProgress.NewAdventureData();
                d.rewardPhase = 2;
                d.rewardStage = 4;
                Check(RunProgress.AdvanceAfterReward(d) && d.checkpointStage == Campaign.OldFriend, "Level04 debe llevar al refugio");
                d = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(d));
                RunProgress.Normalize(d);
                Check(RunProgress.ResumeStage(d) == Campaign.OldFriend, "Continuar debe conservar el refugio");
                for (int i = 0;i<3;i++)
                {
                    CardRules.Apply(new CardOffer(CardEffect.Life, Rarity.Mythic, false), d);
                    CardRules.Apply(new CardOffer(CardEffect.EnemySpeed, Rarity.Common, true), d);
                }
                Check(d.lifeBonus == 8 && CardLedger.Count(d, false) == 3, "Las bendiciones deben respetar el limite");
                Check(CardLedger.Exchange(d, 1, new System.Random(710)), "El intercambio debe estar disponible");
                Check(CardLedger.Count(d, false) == 2 && CardLedger.Count(d, true) == 1, "Retirar exactamente una bendicion y dos maldiciones");
                Check(d.lifeBonus == 8 && Mathf.Approximately(d.enemySpeedBonus, .08f), "Recalcular desde las cartas restantes, sin restar sobre un bono truncado");
                d = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(d));
                RunProgress.Normalize(d);
                Check(d.oldFriendResolved && !CardLedger.Exchange(d, 0, new System.Random(1)), "No repetir el intercambio despues de continuar");
                Check(d.oldFriendResult.Count == 3, "Conservar el resultado exacto del intercambio");
                var before = JsonUtility.ToJson(d);
                Check(!CardLedger.Exchange(d, 2, new System.Random(1)) && JsonUtility.ToJson(d) == before, "Rechazar una eleccion repetida sin mutar la partida");
                d.oldFriendVisited = true;
                d.stage = d.checkpointStage = 5;
                RunProgress.Normalize(d);
                Check(RunProgress.ResumeStage(d) == 5, "El portal debe guardar Level05");
                Check(Campaign.AllScenes().Distinct().Count() == 14, "Deben existir 14 escenas unicas");
                foreach (string name in Campaign.AllScenes())Check(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/"+name+".unity"), "Falta escena "+name);
                var scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    var root = new GameObject("Dash regression");
                    SceneManager.MoveGameObjectToScene(root, scene);
                    var board = root.AddComponent<GridBoard>();
                    board.width = 25;
                    board.height = 11;
                    var wall = new GameObject("Wall").AddComponent<Block>();
                    SceneManager.MoveGameObjectToScene(wall.gameObject, scene);
                    wall.cell = new Vector2Int(22, 5);
                    board.blocks[wall.cell] = wall;
                    var path = BossCombat.Trace(board, new Vector2Int(2, 5), Vector2Int.right, 25);
                    Check(path.Count == 19 && path.Last() == new Vector2Int(21, 5), "El dash debe marcar todo el corredor, sin limite de seis casillas");
                    Check(!BossCombat.SecondPhase(3) && BossCombat.SecondPhase(2), "La fase 2 empieza con dos vidas");
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
                Debug.Log("HEXENKNALL: guardado del refugio, intercambios, limites y dash correctos. No se modifico PlayerPrefs. Verifica tambien en Play Mode.");
            }
            finally
            {
                UnityEngine.Random.state = random;
            }
        }
        [MenuItem("HEXENKNALL/Validate music videos and effects")]

        // Comprueba la configuración audiovisual.
        public static void Media()
        {
            var c = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(MediaInstaller.Path);
            Check(c, "Falta el catalogo de presentacion");
            Check(c.titleVideo && c.gameOverVideo, "Faltan los videos");
            Check(new[]
            {
                c.titleMusic, c.combatMusic, c.bossMusic, c.bossDialogueMusic, c.bossDeathMusic, c.cardMusic, c.gameOverMusic, c.oldFriendMusic, c.oneLifeMusic, c.creditsMusic
            }
            .All(x=>x), "Falta una de las diez pistas");
            Check(c.menuShader && c.magicShader, "Faltan shaders propios");
            Check(c.sounds.Length == Enum.GetValues(typeof(SoundCue)).Length, "Faltan espacios de SFX");
            Debug.Log("HEXENKNALL: musica, videos y espacios de SFX presentes. Los clips SFX vacios son pendientes de grabar/asignar; los prefabs VFX externos son opcionales hasta importarlos.");
        }
    }
}
