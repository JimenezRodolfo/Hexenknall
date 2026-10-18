using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Controla el tiempo de la bomba y su explosión.
    public sealed class Bomb : MonoBehaviour
    {
        public Vector2Int cell;
        public float fuse = 2;
        public int radius = 2, damage = 1;
        public GridActor owner;
        public bool special;
        bool detonated;
        float placedAt;
        public float Age=>Mathf.Max(0, Time.time-placedAt);
        public float Remaining=>Mathf.Max(0, fuse-Age);
        void Awake()
        {
            placedAt = Time.time;
        }

        // Inicia la cuenta atrás de la bomba.
        IEnumerator Start()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer)renderer.enabled = false;
            transform.localScale = Vector3.one;
            name = special?"Invocacion celeste":"Circulo arcano";
            var g = GameDirector.I;
            var glyph = MagicVfx.Circle(g.board.World(cell, .055f), special?MagicKind.Sky:MagicKind.Normal);
            glyph.transform.SetParent(transform, true);
            if (g.sound)g.sound.Play(special?SoundCue.SkyCast:SoundCue.CircleCast);
            yield return new WaitForSeconds(Remaining);
            Detonate();
        }

        // Calcula el alcance y aplica la explosión.
        public void Detonate(HashSet<GridActor> hitActors = null)
        {
            if (detonated)return;
            var g = GameDirector.I;
            if (!g || !g.board)return;
            detonated = true;
            var board = g.board;
            // Guarda los obstáculos antes de romper bloques o activar otra bomba.
            var cells = board.Blast(cell, radius);
            board.bombs.Remove(cell);
            if (hitActors == null)hitActors = new HashSet<GridActor>();
            foreach (var p in cells)
            {
                if (board.blocks.TryGetValue(p, out var block))block.Break(special);
                g.Flash(p, special?Color.cyan:new Color(1, .48f, .13f), 0.38f, damage, true, hitActors, special?MagicKind.Sky:MagicKind.Normal);
            }
            foreach (var p in cells)if(board.bombs.TryGetValue(p, out var other))other.Detonate(hitActors);
            if (g.sound)g.sound.Play(special?SoundCue.SkyImpact:SoundCue.CircleRelease);
            Destroy(gameObject);
        }
    }
}
