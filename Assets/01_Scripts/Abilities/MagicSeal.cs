using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Crea y activa el sello mágico.
    public sealed class MagicSeal : MonoBehaviour
    {
        Vector2Int origin, direction;
        float delay;
        int range, damage;

        // Prepara el sello sobre el tablero.
        public static void Create(GameDirector g, Vector2Int cell, Vector2Int direction, float delay, int range, int damage)
        {
            var obj = new GameObject("Sello de luz autonomo");
            obj.transform.position = g.board.World(cell, .9f);
            var seal = obj.AddComponent<MagicSeal>();
            seal.origin = cell;
            seal.direction = direction;
            seal.delay = delay;
            seal.range = range;
            seal.damage = damage;
        }

        // Activa el sello cuando termina la carga.
        IEnumerator Start()
        {
            var g = GameDirector.I;
            Vector3 forward = new Vector3(direction.x, 0, direction.y);
            Vector3 source = g.board.World(origin, .9f)+forward*.42f;
            var glyph = MagicVfx.Circle(source, MagicKind.Ray, .42f);
            glyph.transform.SetParent(transform, true);
            glyph.transform.rotation = Quaternion.FromToRotation(Vector3.up, forward);
            g.sound.Play(SoundCue.RayCast);
            float elapsed = 0;
            while (elapsed<delay)
            {
                if (!g || g.finished)
                {
                    Destroy(gameObject);
                    yield break;
                }
                elapsed+=Time.deltaTime;
                yield return null;
            }
            var hits = new HashSet<GridActor>();
            Vector3 end = source;
            bool fired = false;
            if (g.board.Floor(origin))for(int i = 1;i<=range;i++)
            {
                var p = origin+direction*i;
                if (!g.board.Inside(p) || g.board.terrainReservations.Contains(p))break;
                if (g.board.blocks.TryGetValue(p, out var block))
                {
                    if (block.destructible)
                    {
                        block.Break(true);
                        g.Flash(p, Color.cyan, .3f, damage, false, hits, MagicKind.Ray);
                        end = g.board.World(p, .9f);
                        fired = true;
                    }
                    break;
                }
                g.Flash(p, Color.cyan, .3f, damage, false, hits, MagicKind.Ray);
                end = g.board.World(p, .9f)+forward*.42f;
                fired = true;
            }
            if (fired)MagicVfx.Beam(source, end, MagicKind.Ray, .3f);
            g.sound.Play(SoundCue.RayRelease);
            Destroy(gameObject);
        }
    }
}
