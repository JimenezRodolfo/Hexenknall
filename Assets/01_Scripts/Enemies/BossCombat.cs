using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Controla los ataques y las fases del jefe.
    public sealed class BossCombat : MonoBehaviour
    {
        public enum AttackState
        {
            Ready, PhaseChange, Warning, Dash, Recovery
        }
        public AttackState State { get; private set; }
        public bool PhaseTwo { get; private set; }
        public float warningTime = .8f, dashSpeed = 11, recoveryTime = .65f, dashCooldown = 7;
        [Tooltip("0 = todas las casillas hasta el obstaculo") ]
        public int dashRange = 0;
        float dashDuration;
        EnemyController actor;
        Vector2Int direction, end;
        float until, nextDash, walkSpeed;
        bool hit;
        int damageFrame = -1;
        readonly List<GameObject> markers = new List<GameObject>();

        // Conecta el jefe con su personaje y el tablero.
        public void Bind(EnemyController owner)
        {
            actor = owner;
            nextDash = Time.time+4;
        }

        // Activa el segundo patrón de combate.
        public static bool SecondPhase(int health)=>health>0 && health<=2;

        // Prepara el recorrido de la embestida.
        public static List<Vector2Int> Trace(GridBoard board, Vector2Int start, Vector2Int direction, int range)
        {
            var path = new List<Vector2Int>();
            for (int i = 1;i<=range;i++)
            {
                var p = start+direction*i;
                if (!board.Floor(p) || board.bombs.ContainsKey(p))break;
                var occupant = board.Occupant(p);
                if (occupant && !occupant.player)break;
                path.Add(p);
            }
            return path;
        }

        // Actualiza el ataque activo.
        public bool Tick()
        {
            if (!actor || actor.health<=0)return false;
            var g = GameDirector.I;
            if (!PhaseTwo && SecondPhase(actor.health) && !actor.Moving)
            {
                PhaseTwo = true;
                State = AttackState.PhaseChange;
                until = Time.time+1;
                if (actor.visual)actor.visual.Play("PhaseChange", 1);
                g.sound.Play(SoundCue.BossPhase);
                g.ui.Toast("El guardián entra en su segunda fase");
                nextDash = until+.4f;
                return true;
            }
            switch (State)
            {
                case AttackState.PhaseChange:if(Time.time>=until)State = AttackState.Ready;
                return true;
                case AttackState.Warning: if(Time.time<until)return true;
                ClearMarkers();
                State = AttackState.Dash;
                walkSpeed = actor.speed;
                actor.speed = dashSpeed;
                hit = false;
                g.sound.Play(SoundCue.BossDash);
                if (actor.visual)actor.visual.Play("DashStrike", dashDuration);
                return true;
                case AttackState.Dash: damageFrame = Time.frameCount;
                if (actor.Moving)return true;
                if (actor.cell == end || !actor.DashStep(direction))BeginRecovery();
                return true;
                case AttackState.Recovery:if(Time.time>=until)State = AttackState.Ready;
                return true;
            }
            if (!PhaseTwo || actor.Moving || Time.time<nextDash)return false;
            Vector3 difference = g.player.Position-actor.Position;
            direction = Mathf.Abs(difference.x)>Mathf.Abs(difference.z)?new Vector2Int(difference.x>=0?1:-1, 0):new Vector2Int(0, difference.z>=0?1:-1);
            var path = Trace(g.board, actor.cell, direction, dashRange>0?dashRange:Mathf.Max(g.board.width, g.board.height));
            dashDuration = path.Count/Mathf.Max(.1f, dashSpeed)+.1f;
            if (path.Count<1)
            {
                nextDash = Time.time+1;
                return false;
            }
            end = path[path.Count-1];
            actor.facing = direction;
            actor.Face(direction);
            g.sound.Play(SoundCue.BossDashWarning);
            State = AttackState.Warning;
            until = Time.time+Mathf.Max(.55f, warningTime);
            if (actor.visual)actor.visual.Play("DashCharge", warningTime);
            foreach (var p in path)markers.Add(MagicVfx.Circle(g.board.World(p, .055f), MagicKind.Warning));
            return true;
        }
        void LateUpdate()
        {
            if (!actor || actor.health<=0 || Time.timeScale == 0 || !GameDirector.I.Combat || hit)return;
            if (State != AttackState.Dash && damageFrame != Time.frameCount)return;
            var target = GameDirector.I.player;
            if (!target || target.health<=0)return;
            Vector3 actorFrom = actor.MotionFrame == Time.frameCount?actor.MotionFrom:actor.Position;
            Vector3 targetFrom = target.MotionFrame == Time.frameCount?target.MotionFrom:target.Position;
            Vector3 from = actorFrom-targetFrom, to = actor.Position-target.Position;
            from.y = to.y = 0;
            Vector3 delta = to-from;
            Vector3 closest = from+delta*Mathf.Clamp01(-Vector3.Dot(from, delta)/Mathf.Max(.00001f, delta.sqrMagnitude));
            if (closest.sqrMagnitude<=.48f*.48f)
            {
                hit = true;
                if (target.Hit(1))GameDirector.I.sound.Play(SoundCue.BossDashImpact);
            }
        }

        // Inicia la recuperación después del ataque.
        void BeginRecovery()
        {
            actor.speed = walkSpeed;
            State = AttackState.Recovery;
            until = Time.time+recoveryTime;
            nextDash = until+dashCooldown;
            if (actor.visual)actor.visual.Play("DashRecover", recoveryTime);
        }
        void ClearMarkers()
        {
            foreach (var m in markers)if(m)Destroy(m);
            markers.Clear();
        }
        void OnDisable()
        {
            ClearMarkers();
            if (actor && State == AttackState.Dash)actor.speed = walkSpeed;
        }
    }
}
