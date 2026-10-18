using System;
using UnityEngine;
using UnityEngine.Video;
namespace Labyrinth
{
    // Identificadores de los efectos que reproduce el juego.
    // Guarda las referencias de música y efectos visuales.
    public enum SoundCue
    {
        CircleCast, CircleRelease, RayCast, RayRelease, SkyCast, SkyImpact, AimMove, AimConfirm, AimCancel, PlayerHit, PlayerDeath, EnemyHit, EnemyDeath, MeleeWindup, MeleeImpact, BossPhase, BossDashWarning, BossDash, BossDashImpact, CrateBreak, WallShift, MapExpand, Portal, DialogueAdvance, OldFriendGift, CardHover, CardChoose, ButtonHover, ButtonClick, Victory, Footstep
    }
    [Serializable]
    // Relaciona cada efecto con su archivo de audio.
    public class CueSlot
    {
        public SoundCue cue;
        public AudioClip clip;
        [Range(0, 1)]
        public float volume = 1;
    }
    [CreateAssetMenu(menuName = "HEXENKNALL/Presentation catalog")]
    public sealed class PresentationCatalog:ScriptableObject
    {
        public VideoClip titleVideo, gameOverVideo;
        public Shader menuShader, magicShader;
        public AudioClip titleMusic, combatMusic, bossMusic, bossDialogueMusic, bossDeathMusic, cardMusic, gameOverMusic, oldFriendMusic, oneLifeMusic, creditsMusic;
        public CueSlot[] sounds = Array.Empty<CueSlot>();
        [Header("Prefabs originales de los paquetes VFX (opcionales hasta importarlos)")]
        public GameObject redWarningCircle, normalCircle, rayCircle, rayBeam, skyCircle, skyBeam, portal;
        [Header("Presentación")]
        [Range(0, 1)]
        public float particles = .65f;
        [Range(0, 10)]
        public float hudShake = 4;
        static PresentationCatalog current;
        public static PresentationCatalog Current=>current?current:(current = Resources.Load<PresentationCatalog>("HexenknallPresentation"));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]

        // Limpia la referencia al catálogo anterior.
        static void Reset()
        {
            current = null;
        }
    }
}
