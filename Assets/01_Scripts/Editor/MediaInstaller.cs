using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
namespace Labyrinth.Editor
{
    [InitializeOnLoad]
    // Prepara los espacios de audio, vídeo y efectos.
    public static class MediaInstaller
    {
        public const string Path = "Assets/10_Data/Resources/HexenknallPresentation.asset";
        static MediaInstaller()
        {
            EditorApplication.delayCall+=Ensure;
        }

        // Prepara el catálogo audiovisual.
        static void Ensure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall+=Ensure;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)return;
            var c = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(Path);
            if (!c || c.sounds == null || Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>() .Any(cue=>!c.sounds.Any(s=>s != null && s.cue == cue && s.clip)))Configure();
        }
        [MenuItem("HEXENKNALL/Configure music videos and sound slots")]

        // Asigna los medios encontrados.
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)return;
            Directory.CreateDirectory("Assets/10_Data/Resources");
            var c = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(Path);
            if (!c)
            {
                c = ScriptableObject.CreateInstance<PresentationCatalog>();
                AssetDatabase.CreateAsset(c, Path);
            }
            c.titleVideo = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/09_Video/Title_Fire.mp4");
            c.gameOverVideo = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/09_Video/GameOver_Fire.mp4");
            c.menuShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/07_VFX/Shaders/MenuFire.shader");
            c.magicShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/07_VFX/Shaders/MagicGlow.shader");
            c.titleMusic = Music("Title_song.mp3");
            c.combatMusic = Music("Normal_Fight.mp3");
            c.bossMusic = Music("Boss_fight.mp3");
            c.bossDialogueMusic = Music("Dialoge_with_boss.wav");
            c.bossDeathMusic = Music("Boss_Death.mp3");
            c.cardMusic = Music("Card_Selection.mp3");
            c.gameOverMusic = Music("GameOver.mp3");
            c.oldFriendMusic = Music("OldFriend_Room.mp3");
            c.oneLifeMusic = Music("One_live_song.mp3");
            c.creditsMusic = Music("credits.mp3");
            var slots = c.sounds == null?new System.Collections.Generic.List<CueSlot>():c.sounds.ToList();
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {
                var slot = slots.FirstOrDefault(s=>s != null && s.cue == cue);
                if (slot == null)
                {
                    slot = new CueSlot
                    {
                        cue = cue
                    };
                    slots.Add(slot);
                }
                if (!slot.clip)slot.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/08_Audio/SFX/"+cue+".wav");
            }
            c.sounds = slots.ToArray();
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            Debug.Log("HEXENKNALL: musica y efectos preparados: "+slots.Count(s=>s != null && s.clip)+" SFX asignados automaticamente.");
        }

        // Busca las pistas de música.
        static AudioClip Music(string filename)
        {
            string path = "Assets/08_Audio/Music/"+filename;
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer)
            {
                var settings = importer.defaultSampleSettings;
                if (settings.loadType != AudioClipLoadType.Streaming)
                {
                    settings.loadType = AudioClipLoadType.Streaming;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        [MenuItem("HEXENKNALL/Edit dialogue JSON")]

        // Abre el archivo de conversaciones.
        public static void OpenDialogues()
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/10_Data/Resources/HexenknallDialogues.json"));
        }
        [MenuItem("HEXENKNALL/Select sound and VFX slots")]

        // Muestra los espacios para audios y efectos.
        public static void SelectSlots()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(Path);
        }
    }
}
