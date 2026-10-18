using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth.Editor
{
    // Coloca los modelos visuales en las escenas.
    public static class ArtSceneIntegration
    {
        [MenuItem("HEXENKNALL/Apply art to prefabs and scenes")]

        // Aplica el catálogo a las escenas y prefabs.
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Deten Play antes de aplicar modelos a escenas.");
                return;
            }
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/10_Data/Resources/LabyrinthArt.asset");
            if (!catalog)
            {
                Debug.LogError("Ejecuta Install models and animations primero.");
                return;
            }
            foreach (string name in new[]
            {
                "Player", "MeleeEnemy", "BomberEnemy", "Boss", "Wall", "DestructibleBlock"
            })
            {
                string path = "Assets/02_Prefabs/"+name+".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Dress(root, catalog);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            for (int i = 0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
            {
                Debug.LogWarning("Prefabs preparados. Guarda tus escenas abiertas y ejecuta Apply art to prefabs and scenes para ver los modelos tambien en Edit Mode.");
                return;
            }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var name in Campaign.AllScenes())
                {
                    var scene = EditorSceneManager.OpenScene("Assets/00_Scenes/"+name+".unity", OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        Dress(root, catalog);
                        if (root.name.StartsWith("Floor "))
                        {
                            if (!root.GetComponent<Collider>())root.AddComponent<BoxCollider>();
                            var r = root.GetComponent<Renderer>();
                            if (r)
                            {
                                r.sharedMaterial = catalog.floor;
                                r.receiveShadows = true;
                            }
                        }
                        var ev = root.GetComponent<StageEventManager>();
                        if (ev)ev.floorMaterial = catalog.floor;
                    }
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                bool valid = setup != null && setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path) && AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) != null) && setup.Count(s=>s.isActive) == 1 && setup.Any(s=>s.isActive && s.isLoaded);
                if (valid)EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity", OpenSceneMode.Single);
            }
        }

        // Reemplaza las referencias visuales correspondientes.
        static void Dress(GameObject root, ArtCatalog catalog)
        {
            var actor = root.GetComponent<GridActor>();
            if (actor)
            {
                var old = root.transform.Find("Character Visual");
                if (old)Object.DestroyImmediate(old.gameObject);
                catalog.DressActor(actor);
                if (actor.boss)actor.health = actor.maxHealth = 4;
                var body = root.GetComponent<Rigidbody>();
                if (!body)body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.constraints = RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;
                var capsule = root.GetComponent<CapsuleCollider>();
                if (!capsule)capsule = root.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;
                capsule.radius = actor.hurtHalfSize;
                capsule.height = 1.15f;
                capsule.center = Vector3.zero;
            }
            var block = root.GetComponent<Block>();
            if (block)
            {
                var old = root.transform.Find("Block Visual");
                if (old)Object.DestroyImmediate(old.gameObject);
                catalog.DressBlock(block);
                var box = root.GetComponent<BoxCollider>();
                if (!box)box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(.96f, block.destructible?1:1.2f, .96f);
                box.isTrigger = false;
            }
        }
    }
}
