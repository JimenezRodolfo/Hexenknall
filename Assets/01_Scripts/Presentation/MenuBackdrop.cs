using UnityEngine;
using UnityEngine.Video;
namespace Labyrinth
{
    // Muestra el fondo animado de los menús.
    public sealed class MenuBackdrop : MonoBehaviour
    {
        VideoPlayer title, defeat;
        RenderTexture output;
        Material material;
        PresentationCatalog catalog;
        bool failed;
        public Texture Frame=>output;

        // Crea el fondo del menú.
        void Start()
        {
            catalog = PresentationCatalog.Current;
            if (!catalog)return;
            title = Create(catalog.titleVideo);
            defeat = Create(catalog.gameOverVideo);
            output = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            output.Create();
            if (catalog.menuShader)material = new Material(catalog.menuShader);
        }
        VideoPlayer Create(VideoClip clip)
        {
            if (!clip)return null;
            var vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.clip = clip;
            vp.isLooping = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.APIOnly;
            vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            vp.waitForFirstFrame = true;
            vp.errorReceived+=(source, message)=>
            {
                if (!failed)
                {
                    Debug.LogWarning("Video: "+message);
                    failed = true;
                }
            };
            vp.Prepare();
            return vp;
        }

        // Actualiza los detalles animados.
        void Update()
        {
            var g = GameDirector.I;
            if (!g || !output)return;
            bool loss = g.ui && g.ui.defeat;
            bool visible = g.stage<0 || loss || g.stage == Campaign.Ending || g.paused || g.dialogue || (g.rewards && g.rewards.Active) || (g.oldFriend && g.oldFriend.Choosing);
            var active = loss?defeat:title;
            var other = loss?title:defeat;
            if (other && other.isPlaying)other.Pause();
            if (!visible)
            {
                if (active && active.isPlaying)active.Pause();
                return;
            }
            if (active && active.isPrepared && !active.isPlaying)active.Play();
            Texture source = active && active.texture?active.texture:Texture2D.blackTexture;
            if (material)
            {
                material.SetFloat("_Clock", Time.unscaledTime);
                material.SetFloat("_Embers", catalog.particles);
                material.SetFloat("_Defeat", loss?1:0);
                material.SetFloat("_Prepared", source != Texture2D.blackTexture?1:0);
                material.SetFloat("_BackdropOnly", g.stage>=0 && !loss?1:0);
                Graphics.Blit(source, output, material);
            }
            else Graphics.Blit(source, output);
        }
        void OnDestroy()
        {
            if (output)
            {
                output.Release();
                Destroy(output);
            }
            if (material)Destroy(material);
        }
    }
}
