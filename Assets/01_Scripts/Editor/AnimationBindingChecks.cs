using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Labyrinth.Editor
{
    // Comprueba que las animaciones estén bien enlazadas.
    public static class AnimationBindingChecks
    {
        // Inicia la revisión de clips.
        static void Check(bool condition, string message)
        {
            if (!condition)throw new Exception("Animation check: "+message);
        }
        [MenuItem("HEXENKNALL/Run animation binding checks")]

        // Comprueba las referencias de animación.
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Deten Play antes de comprobar animaciones.");
                return;
            }
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/__LabyrinthAnimationCheck");
            GameObject root = null, instance = null, empty = null;
            float scale = Time.timeScale;
            int missingWarnings = 0;
            Application.LogCallback listener = (message, stack, type)=>
            {
                if (message.Contains("Falta la animacion Idle"))missingWarnings++;
            };
            try
            {
                AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
                var idle = new AnimationClip
                {
                    legacy = true, name = "UnrelatedIdleAsset"
                };
                var run = new AnimationClip
                {
                    legacy = true, name = "UnrelatedRunAsset"
                };
                idle.SetCurve("Rig/LeftLeg", typeof(Transform), "localPosition.z", AnimationCurve.Constant(0, 1, 0));
                run.SetCurve("Rig/LeftLeg", typeof(Transform), "localPosition.z", new AnimationCurve(new Keyframe(0, 0), new Keyframe(.5f, .2f), new Keyframe(1, 0)));
                AssetDatabase.CreateAsset(idle, folder+"/Saved_A.anim");
                AssetDatabase.CreateAsset(run, folder+"/Saved_B.anim");
                root = new GameObject("RoundTrip");
                var rig = new GameObject("Rig");
                rig.transform.SetParent(root.transform, false);
                new GameObject("LeftLeg").transform.SetParent(rig.transform, false);
                var animation = root.AddComponent<Animation>();
                var visual = root.AddComponent<CharacterVisual>();
                visual.animationPlayer = animation;
                visual.clips = new[]
                {
                    new CharacterVisual.ClipBinding("Idle", idle), new CharacterVisual.ClipBinding("Run", run)
                };
                AnimationUtility.SetAnimationClips(animation, new[]
                {
                    idle, run
                });
                animation.clip = idle;
                string path = folder+"/RoundTrip.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
                root = null;
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                visual = instance.GetComponent<CharacterVisual>();
                visual.RefreshClips();
                animation = visual.animationPlayer;
                Check(visual.clips.Length == 2, "clip references must survive prefab serialization");
                Check(animation.GetClip("Idle") == idle, "saved reference must restore Idle regardless of the asset name");
                Check(animation.GetClip("Run") == run, "saved reference must restore Run regardless of the asset name");
                Check(animation["Idle"] != null && animation["Run"] != null, "both AnimationStates must exist after loading");
                Check(AnimationUtility.GetCurveBindings(run).Any(b=>instance.transform.Find(b.path)), "running curve must bind to the loaded skeleton");
                // Comprueba el caso de clips de movimiento faltantes.
                empty = new GameObject("Missing clips regression");
                empty.AddComponent<Animation>();
                var actor = empty.AddComponent<GridActor>();
                var missing = empty.AddComponent<CharacterVisual>();
                missing.Bind(actor);
                Time.timeScale = 1;
                Application.logMessageReceived+=listener;
                var late = typeof(CharacterVisual).GetMethod("LateUpdate", BindingFlags.Instance|BindingFlags.NonPublic);
                late.Invoke(missing, null);
                late.Invoke(missing, null);
                Check(missingWarnings == 1, "missing Idle must warn once, without throwing or flooding the console");
                Debug.Log("PASS: referencias Idle/Run conservadas al guardar/cargar prefab; alias restaurados; curvas enlazadas; ausencia de clips sin NullReferenceException. Falta revisar la animacion visual en Play.");
            }
            finally
            {
                Application.logMessageReceived-=listener;
                Time.timeScale = scale;
                if (root)UnityEngine.Object.DestroyImmediate(root);
                if (instance)UnityEngine.Object.DestroyImmediate(instance);
                if (empty)UnityEngine.Object.DestroyImmediate(empty);
                if (AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);
            }
        }

        // Revisa los estados de un personaje.
        public static void ValidateCharacter(GameObject prefab)
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var visual = instance.GetComponent<CharacterVisual>();
                if (!visual || !visual.animationPlayer)throw new Exception("Falta Animation/CharacterVisual en "+prefab.name);
                visual.RefreshClips();
                foreach (string state in CharacterVisual.StateNames)
                {
                    var clip = visual.animationPlayer.GetClip(state);
                    if (!clip || !clip.legacy || visual.animationPlayer[state] == null)throw new Exception("Falta estado Legacy "+state+" en "+prefab.name);
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    if (!bindings.Any(b=>b.type == typeof(Transform) && instance.transform.Find(b.path))) throw new Exception("El clip "+state+" no enlaza con el esqueleto de "+prefab.name);
                    if (state == "Run" && !bindings.Any(b=>b.type == typeof(Transform) && b.path.ToLowerInvariant().Contains("leg") && instance.transform.Find(b.path) && AnimationUtility.GetEditorCurve(clip, b).keys.Select(k=>k.value).Distinct().Count()>1)) throw new Exception("Run no contiene movimiento de piernas enlazado en "+prefab.name);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
