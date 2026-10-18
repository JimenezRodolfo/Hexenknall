using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Labyrinth.Editor
{
    // Prepara copias editables de las animaciones.
    public static class AnimationWorkspace
    {
        const string Folder = "Assets/05_Animations/Custom";
        [MenuItem("HEXENKNALL/Animations/Create editable Melee clips")]
        public static void CreateMeleeCopies()=>CreateCopies("Melee");
        [MenuItem("HEXENKNALL/Animations/Create editable Player clips")]
        public static void CreatePlayerCopies()=>CreateCopies("Player");
        [MenuItem("HEXENKNALL/Animations/Create editable Boss clips")]
        public static void CreateBossCopies()=>CreateCopies("Boss");
        [MenuItem("HEXENKNALL/Animations/Create editable Bomber clips")]
        public static void CreateBomberCopies()=>CreateCopies("Bomber");

        // Genera clips editables sin tocar los originales.
        static void CreateCopies(string role)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Detén Play para editar animaciones.");
                return;
            }
            string prefab = "Assets/02_Prefabs/Generated/"+role+"Visual.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
            if (!source)
            {
                Debug.LogError("Ejecuta Install models and animations primero.");
                return;
            }
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var visual = source.GetComponent<CharacterVisual>();
            if (!visual || visual.clips == null || visual.clips.Length == 0)throw new Exception("Reinstala los modelos y animaciones antes de crear copias.");
            foreach (var binding in visual.clips)
            {
                if (!binding.clip)continue;
                string path = Folder+"/"+role+"_"+binding.state+".anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path))continue;
                var copy = UnityEngine.Object.Instantiate(binding.clip);
                copy.legacy = true;
                copy.name = binding.state;
                AssetDatabase.CreateAsset(copy, path);
                copy.name = binding.state;
                EditorUtility.SetDirty(copy);
            }
            var root = PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                visual = root.GetComponent<CharacterVisual>();
                for (int i = 0;i<visual.clips.Length;i++)
                {
                    var binding = visual.clips[i];
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/"+role+"_"+binding.state+".anim");
                    if (clip)visual.clips[i] = new CharacterVisual.ClipBinding(binding.state, clip);
                }
                AnimationUtility.SetAnimationClips(visual.animationPlayer, visual.clips.Select(x=>x.clip).Where(x=>x).Distinct().ToArray());
                visual.RefreshClips();
                PrefabUtility.SaveAsPrefabAsset(root, prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            ArtSceneIntegration.Apply();
            Debug.Log("Copias editables enlazadas en "+Folder+". El instalador conservará tus cambios.");
        }
        [MenuItem("HEXENKNALL/Animations/Open Melee animation editor")]
        public static void OpenMelee()=>Open("Melee", "Slash");
        [MenuItem("HEXENKNALL/Animations/Open Player animation editor")]
        public static void OpenPlayer()=>Open("Player", "Run");
        [MenuItem("HEXENKNALL/Animations/Open Boss animation editor")]
        public static void OpenBoss()=>Open("Boss", "DashStrike");
        [MenuItem("HEXENKNALL/Animations/Open Bomber animation editor")]
        public static void OpenBomber()=>Open("Bomber", "Run");

        // Abre la animación del personaje seleccionado.
        static void Open(string role, string state)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Detén Play antes de editar.");
                return;
            }
            string prefab = "Assets/02_Prefabs/Generated/"+role+"Visual.prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefab))
            {
                Debug.LogError("Instala los modelos primero.");
                return;
            }
            var stage = PrefabStageUtility.OpenPrefab(prefab);
            Selection.activeGameObject = stage.prefabContentsRoot;
            var window = EditorWindow.GetWindow<AnimationWindow>();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/"+role+"_"+state+".anim");
            if (clip)window.animationClip = clip;
            window.Show();
        }
    }
}
