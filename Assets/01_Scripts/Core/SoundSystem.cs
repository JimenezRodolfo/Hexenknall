using UnityEngine;
namespace Labyrinth
{
    // Reproduce música y efectos del juego.
    public sealed class SoundSystem : MonoBehaviour
    {
        AudioSource first, second, sfx;
        AudioClip desired;
        bool useFirst = true;
        PresentationCatalog catalog;

        // Carga los sonidos y el volumen guardado.
        void Start()
        {
            SceneRuntimeGuard.KeepOneAudioListener(gameObject.scene);
            catalog = PresentationCatalog.Current;
            first = gameObject.AddComponent<AudioSource>();
            second = gameObject.AddComponent<AudioSource>();
            sfx = gameObject.AddComponent<AudioSource>();
            first.playOnAwake = second.playOnAwake = sfx.playOnAwake = false;
            first.loop = second.loop = true;
            first.volume = second.volume = 0;
            Apply();
            int r = Mathf.Clamp(PlayerPrefs.GetInt("Resolution", 1), 0, 2);
            Screen.SetResolution(new[]
            {
                1280, 1600, 1920
            }
            [r], new[]
            {
                720, 900, 1080
            }
            [r], PlayerPrefs.GetInt("Fullscreen", 0) == 1);
        }

        // Mantiene actualizada la reproducción.
        void Update()
        {
            var g = GameDirector.I;
            if (!g || !catalog || !first)return;
            AudioClip next = ChooseMusic(g);
            if (next != desired)
            {
                desired = next;
                useFirst = !useFirst;
                var source = useFirst?first:second;
                source.Stop();
                source.clip = next;
                if (next)source.Play();
            }
            float volume = PlayerPrefs.GetFloat("Music", .45f), step = Time.unscaledDeltaTime*1.6f;
            first.volume = Mathf.MoveTowards(first.volume, useFirst?volume:0, step);
            second.volume = Mathf.MoveTowards(second.volume, useFirst?0:volume, step);
        }
        AudioClip ChooseMusic(GameDirector g)
        {
            if (g.ui && g.ui.defeat)return catalog.gameOverMusic;
            if (g.stage == Campaign.Ending || (g.ui && g.ui.ShowingCredits))return catalog.creditsMusic;
            if (g.stage<0)return catalog.titleMusic;
            if ((g.rewards && g.rewards.Active) || (g.oldFriend && g.oldFriend.Choosing))return catalog.cardMusic;
            if (g.stage == Campaign.OldFriend)return catalog.oldFriendMusic;
            if (g.finished && g.stage == Campaign.FinalBoss)return catalog.bossDeathMusic;
            if (g.dialogue && (g.stage == 0 || g.stage == Campaign.FinalBoss))return catalog.bossDialogueMusic;
            if (g.player && g.player.health == 1)return catalog.oneLifeMusic;
            return g.stage == 0 || g.stage == Campaign.FinalBoss?catalog.bossMusic:catalog.combatMusic;
        }

        // Aplica el volumen configurado.
        public void Apply()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("Master", .65f);
            if (sfx)sfx.volume = PlayerPrefs.GetFloat("SFX", .7f);
        }

        // Reproduce un efecto del catálogo.
        public void Play(SoundCue cue)
        {
            if (!sfx || !catalog || catalog.sounds == null)return;
            foreach (var slot in catalog.sounds)if(slot != null && slot.cue == cue && slot.clip)
            {
                sfx.PlayOneShot(slot.clip, slot.volume);
                return;
            }
        }
        public void Effect(float hz, float duration)
        {
            Play(SoundCue.CircleRelease);
        }
    }
}
