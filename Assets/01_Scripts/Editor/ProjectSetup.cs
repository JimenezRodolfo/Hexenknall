using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Labyrinth.Editor
{
    [InitializeOnLoad]
    // Prepara y valida la configuración inicial de Unity.
    public static class ProjectSetup
    {
        const string PipelinePath = "Assets/11_Settings/LabyrinthURP.asset";
        static ProjectSetup()
        {
            EditorApplication.delayCall+=Ensure;
        }

        // Comprueba la configuración al abrir Unity.
        static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall+=Ensure;
                return;
            }
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (asset == null)Configure();
            else
            {
                GraphicsSettings.defaultRenderPipeline = asset;
                QualitySettings.renderPipeline = asset;
            }
        }
        [MenuItem("HEXENKNALL/Configure URP and build scenes")]

        // Configura las opciones del proyecto.
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/11_Settings");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/11_Settings/LabyrinthRenderer.asset");
            if (!renderer)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/11_Settings/LabyrinthRenderer.asset");
            }
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            EditorUtility.SetDirty(renderer);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (!pipeline)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/11_Settings/DialogueEffects.asset");
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, "Assets/11_Settings/DialogueEffects.asset");
                var depth = profile.Add<DepthOfField>(true);
                depth.mode.Override(DepthOfFieldMode.Gaussian);
                depth.active = false;
                AssetDatabase.AddObjectToAsset(depth, profile);
                var exposure = profile.Add<ColorAdjustments>(true);
                exposure.postExposure.Override(0);
                AssetDatabase.AddObjectToAsset(exposure, profile);
                EditorUtility.SetDirty(profile);
            }
            pipeline.volumeProfile = profile;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.companyName = "ClubPanitas Studios";
            PlayerSettings.productName = "HEXENKNALL";
            string[] names = Campaign.AllScenes();
            var scenes = new EditorBuildSettingsScene[names.Length];
            for (int i = 0;i<names.Length;i++)scenes[i] = new EditorBuildSettingsScene("Assets/00_Scenes/"+names[i]+".unity", true);
            EditorBuildSettings.scenes = scenes;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log("Labyrinth: URP y escenas configurados. Abre Assets/00_Scenes/MainMenu.unity.");
        }
        [MenuItem("HEXENKNALL/Open Main Menu")]

        // Abre la escena inicial.
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
        }
        [MenuItem("HEXENKNALL/Validate project")]

        // Revisa que las escenas estén preparadas.
        public static void Validate()
        {
            int errors = 0;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!File.Exists(s.path))
                {
                    Debug.LogError("Missing scene: "+s.path);
                    errors++;
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[]
            {
                "Assets/02_Prefabs"
            }
            ))
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var t in obj.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                {
                    Debug.LogError("Missing script: "+obj.name);
                    errors++;
                }
            }
            Debug.Log("Static asset validation: "+errors+" errors. This does not replace Play Mode testing.");
        }
    }
}
