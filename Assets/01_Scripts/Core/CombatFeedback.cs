using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Un contexto por explosión/cadena; conserva la autoría de cada bomba individual.
    public sealed class ExplosionContext
    {
        public readonly HashSet<GridActor> hits = new HashSet<GridActor>();
        public readonly HashSet<GridActor> kills = new HashSet<GridActor>();
        public int bombs;
        public bool announced;
        public float lastKill;
        public void Kill(GridActor actor, bool playerOwned)
        {
            if (!playerOwned || actor.player || !kills.Add(actor))return;
            announced=false;
            lastKill = Time.unscaledTime;
            if (GameDirector.I && GameDirector.I.feedback)GameDirector.I.feedback.Track(this);
        }
    }
    public sealed class CombatFeedback : MonoBehaviour
    {
        readonly List<ExplosionContext> pending = new List<ExplosionContext>();
        float soundAt, chainAt;
        public string combo = "";
        float comboUntil;
        public void Track(ExplosionContext context) { if (!pending.Contains(context))pending.Add(context); }
        public void Impact(GridActor actor, bool killed)
        {
            var g = GameDirector.I;
            if (!g || !Application.isPlaying)return;
            if (!actor.player)
            {
                MagicVfx.Circle(g.board.World(actor.cell,.08f), killed ? MagicKind.Sky : MagicKind.Ray, killed ? .55f : .25f, killed ? .32f : .12f);
                if (Time.unscaledTime >= soundAt && g.sound)
                {
                    g.sound.Play(killed ? SoundCue.EnemyDeath : SoundCue.EnemyHit);
                    soundAt = Time.unscaledTime+.07f;
                }
            }
        }
        public void Chain(Vector2Int cell)
        {
            var g = GameDirector.I;
            if (g && Application.isPlaying)MagicVfx.Circle(g.board.World(cell,.09f),MagicKind.Ray,.6f,.25f);
            if (g && g.sound && Time.unscaledTime >= chainAt) { g.sound.Play(SoundCue.RayRelease); chainAt=Time.unscaledTime+.12f; }
        }
        void Update()
        {
            for (int i=pending.Count-1;i>=0;i--)
            {
                var c=pending[i];
                if (Time.unscaledTime-c.lastKill < .12f)continue;
                if (c.kills.Count>=2 && !c.announced) { combo = c.kills.Count>=3 ? "¡TRIPLE!" : "¡DOBLE!"; comboUntil=Time.unscaledTime+1.1f; c.announced=true; }
                pending.RemoveAt(i);
            }
            if (Time.unscaledTime>comboUntil)combo="";
        }
    }
}
