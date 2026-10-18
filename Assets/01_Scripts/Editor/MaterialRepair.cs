using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth.Editor
{
    // Repara referencias de materiales importados.
    public static class MaterialRepair
    {
        [MenuItem("HEXENKNALL/Repair missing materials")]

        // Busca y corrige materiales faltantes.
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Deten Play antes de reparar los materiales.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup =  EditorSceneManager.GetSceneManagerSetup();
            int repaired =  0;
            try
            {
                // Resolve through AssetDatabase so Unity writes its own reference format.
                var shader =  AssetDatabase.LoadAssetAtPath<Shader>("Assets/06_Materials/Prototype/Prototype.shader");
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[]
                {
                    "Assets/06_Materials"
                }
                ))
                {
                    var mat =  AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (mat != null && mat.shader == null && shader != null)
                    {
                        mat.shader =  shader;
                        EditorUtility.SetDirty(mat);
                    }
                }
                string[] scenes =  Campaign.AllScenes();
                foreach (string name in scenes)
                {
                    string path =  "Assets/00_Scenes/" + name + ".unity";
                    if (!File.Exists(path)) continue;
                    var scene =  EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    int count =  0;
                    foreach (var root in scene.GetRootGameObjects()) count += RepairRoot(root);
                    if (count > 0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("No se pudo guardar " + path);
                        repaired += count;
                    }
                }
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[]
                {
                    "Assets/02_Prefabs"
                }
                ))
                {
                    string path =  AssetDatabase.GUIDToAssetPath(guid);
                    var root =  PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        int count =  RepairRoot(root);
                        if (count > 0)
                        {
                            PrefabUtility.SaveAsPrefabAsset(root, path);
                            repaired += count;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
                AssetDatabase.SaveAssets();
                Debug.Log("Labyrinth: materiales reasignados en " + repaired + " renderers. Revisa MainMenu y pulsa Play.");
            }
            finally
            {
                bool valid = setup != null && setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path) && AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) != null) && setup.Count(s=>s.isActive) == 1 && setup.Any(s=>s.isActive && s.isLoaded);
                if (valid)EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity", OpenSceneMode.Single);
            }
        }

        // Revisa los materiales de un prefab.
        static int RepairRoot(GameObject root)
        {
            int count =  0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var slots =  renderer.sharedMaterials;
                bool missing =  slots.Length == 0;
                foreach (var material in slots) if (material == null) missing =  true;
                if (!missing) continue;
                // Conserva las referencias que ya funcionan.
                string materialName =  MaterialName(renderer.gameObject);
                if (materialName == null) continue;
                var replacement =  AssetDatabase.LoadAssetAtPath<Material>("Assets/06_Materials/Prototype/" + materialName + ".mat");
                if (replacement == null)
                {
                    Debug.LogWarning("No se encontro el material: " + materialName, renderer);
                    continue;
                }
                if (slots.Length == 0) slots =  new Material[1];
                for (int i =  0; i < slots.Length; i++) if (slots[i] == null) slots[i] =  replacement;
                renderer.sharedMaterials =  slots;
                EditorUtility.SetDirty(renderer);
                count++;
            }
            return count;
        }
        static string MaterialName(GameObject obj)
        {
            var actor =  obj.GetComponent<GridActor>();
            if (actor != null)
            {
                if (actor.player) return "Player";
                if (actor.boss) return "Boss";
                var enemy =  obj.GetComponent<EnemyController>();
                return enemy != null && enemy.bomber ? "Bomber" : "Melee";
            }
            var block =  obj.GetComponent<Block>();
            if (block != null) return block.destructible ? "Crate" : "Wall";
            if (obj.GetComponent<Bomb>() != null) return "Bomb";
            if (obj.name.StartsWith("Floor", StringComparison.Ordinal)) return "Floor";
            return null;
        }
    }
}
