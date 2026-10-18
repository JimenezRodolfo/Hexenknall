using System;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // El nombre del estado y su animación se guardan por separado.
    // Reproduce las animaciones de cada personaje.
    public sealed class CharacterVisual : MonoBehaviour
    {
        [Serializable]
        public struct ClipBinding
        {
            public string state;
            public AnimationClip clip;
            public ClipBinding(string state, AnimationClip clip)
            {
                this.state = state;
                this.clip = clip;
            }
        }
        public static readonly string[] StateNames =
        {
            "Idle", "Run", "PlaceBomb", "CastSeal", "Aim", "CastSpecial", "Slash", "CastArea", "PhaseChange", "DashCharge", "DashStrike", "DashRecover", "Hit", "Death"
        };
        public Animation animationPlayer;
        public Transform upperBody;
        public ClipBinding[] clips = Array.Empty<ClipBinding>();
        GridActor actor;
        string locomotion;
        float actionUntil, deathTime;
        bool fullBody, dying, prepared;
        Quaternion deathRotation;
        readonly HashSet<string> warnings = new HashSet<string>();
        void Awake()
        {
            InitializeClips();
        }
        void OnEnable()
        {
            locomotion = null;
            InitializeClips();
        }
        public void Bind(GridActor owner)
        {
            actor = owner;
            InitializeClips();
        }
        public void RefreshClips()
        {
            prepared = false;
            locomotion = null;
            warnings.Clear();
            InitializeClips();
        }

        // Carga los clips del personaje.
        public bool InitializeClips()
        {
            if (prepared && animationPlayer)return true;
            if (!animationPlayer)animationPlayer = GetComponent<Animation>();
            if (!animationPlayer)return false;
            animationPlayer.enabled = true;
            animationPlayer.playAutomatically = false;
            // Recupera los clips guardados con nombres de versiones anteriores.
            // Copia la lista antes de registrar nuevos clips.
            var inherited = new List<ClipBinding>();
            foreach (AnimationState state in animationPlayer)
            {
                if (state == null || !state.clip)continue;
                string name = CanonicalName(state.name);
                if (name == null)name = CanonicalName(state.clip.name);
                if (name != null)inherited.Add(new ClipBinding(name, state.clip));
            }
            foreach (var binding in inherited)Register(binding);
            // Prioriza los clips asignados en el Inspector.
            if (clips != null)foreach(var binding in clips)Register(binding);
            if (animationPlayer.GetClip("Idle"))animationPlayer.clip = animationPlayer.GetClip("Idle");
            prepared = true;
            return true;
        }

        // Registra una animación con su estado.
        void Register(ClipBinding binding)
        {
            if (string.IsNullOrEmpty(binding.state) || !binding.clip)return;
            if (!binding.clip.legacy)
            {
                WarnOnce(binding.state, "El clip "+binding.clip.name+" debe importarse como Legacy.");
                return;
            }
            if (animationPlayer.GetClip(binding.state) != binding.clip)
            {
                if (animationPlayer.GetClip(binding.state))animationPlayer.RemoveClip(binding.state);
                animationPlayer.AddClip(binding.clip, binding.state);
            }
        }

        // Obtiene el nombre del estado de animación.
        static string CanonicalName(string name)
        {
            if (string.IsNullOrEmpty(name))return null;
            if (name.Equals("ImportedRun", StringComparison.OrdinalIgnoreCase) || name.EndsWith("_ImportedRun", StringComparison.OrdinalIgnoreCase))return "Run";
            foreach (string state in StateNames) if(name.Equals(state, StringComparison.OrdinalIgnoreCase) || name.EndsWith("_"+state, StringComparison.OrdinalIgnoreCase))return state;
            return null;
        }
        void WarnOnce(string state, string message)
        {
            if (warnings.Add(state))Debug.LogWarning(name+": "+message+" Ejecuta HEXENKNALL > Install models and animations fuera de Play.", this);
        }
        AnimationState State(string name, bool report = true)
        {
            if (!InitializeClips())
            {
                if (report)WarnOnce("Animation", "Falta el componente Animation.");
                return null;
            }
            var state = animationPlayer[name];
            if (state == null && report)WarnOnce(name, "Falta la animacion "+name+".");
            return state;
        }

        // Reproduce la animación solicitada.
        public void Play(string clip, float duration = .45f, bool upperOnly = false)
        {
            if (dying)return;
            var state = State(clip);
            if (state == null)return;
            bool mix = upperOnly && upperBody;
            state.layer = mix?1:0;
            state.wrapMode = WrapMode.Once;
            state.speed = state.length/Mathf.Max(.08f, duration);
            state.time = 0;
            if (mix)state.AddMixingTransform(upperBody, true);
            animationPlayer.CrossFade(clip, .08f, mix?PlayMode.StopSameLayer:PlayMode.StopAll);
            // Una reacción del torso no cancela el movimiento principal.
            if (!mix)
            {
                fullBody = true;
                actionUntil = Time.time+duration;
                locomotion = null;
            }
        }
        void LateUpdate()
        {
            if (dying)
            {
                deathTime+=Time.unscaledDeltaTime;
                var death = State("Death", false);
                if (death != null)
                {
                    death.time = Mathf.Min(deathTime, death.length);
                    animationPlayer.Sample();
                }
                transform.rotation = deathRotation*Quaternion.Euler(Mathf.SmoothStep(0, 80, deathTime/1.35f), 0, 0);
                if (deathTime>2.2f)Destroy(gameObject);
                return;
            }
            if (!actor || actor.health<=0 || Time.timeScale == 0)return;
            if (fullBody && Time.time<actionUntil)return;
            fullBody = false;
            var player = actor as PlayerController;
            string next = player && player.Aiming?"Aim":actor.Moving?"Run":"Idle";
            var state = State(next);
            if (state == null && next != "Idle")
            {
                next = "Idle";
                state = State(next);
            }
            if (state == null)return;
            // Evita reproducir un estado inexistente.
            if (next != locomotion || !animationPlayer.IsPlaying(next))
            {
                state.layer = 0;
                state.wrapMode = WrapMode.Loop;
                animationPlayer.CrossFade(next, .12f, PlayMode.StopSameLayer);
                locomotion = next;
            }
            state.speed = next == "Run"?Mathf.Clamp(actor.speed/3.5f, .5f, 2):1;
        }

        // Activa la animación de muerte.
        public void Die()
        {
            if (dying)return;
            dying = true;
            transform.SetParent(null, true);
            deathRotation = transform.rotation;
            var state = State("Death");
            if (state == null)return;
            animationPlayer.Stop();
            state.layer = 0;
            state.speed = 0;
            state.wrapMode = WrapMode.ClampForever;
            animationPlayer.Play("Death");
        }
    }
}
