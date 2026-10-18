using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Consulta las casillas, obstáculos y explosiones.
    public sealed class GridBoard : MonoBehaviour
    {
        public int width = 13, height = 11;
        [Header("Distribucion aleatoria (niveles normales)")]
        public bool randomizeLayout = true;
        [Tooltip("-1: nueva semilla en cada entrada/reintento. >=0: reproducible.")]
        public int layoutSeed = -1;
        [Range(0, .3f)]
        public float solidDensity = .14f;
        [Range(0, .4f)]
        public float crateDensity = .22f;
        public readonly List<Hazard> hazards = new List<Hazard>();
        public readonly Dictionary<Vector2Int, Block> blocks = new Dictionary<Vector2Int, Block>();
        public readonly Dictionary<Vector2Int, Bomb> bombs = new Dictionary<Vector2Int, Bomb>();
        public readonly List<GridActor> actors = new List<GridActor>();
        // Las nuevas casillas no se usan hasta que termine la animación.
        public readonly HashSet<Vector2Int> terrainReservations = new HashSet<Vector2Int>();
        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        // Convierte una casilla en una posición del mundo.
        public Vector3 World(Vector2Int p, float y = .65f)=>new Vector3(p.x, y, p.y);
        public bool Inside(Vector2Int p)=>p.x>=0 && p.y>=0 && p.x<width && p.y<height;

        // Comprueba si hay suelo libre.
        public bool Floor(Vector2Int p)=>Inside(p) && !blocks.ContainsKey(p) && !terrainReservations.Contains(p);

        // Busca un personaje en la casilla.
        public GridActor Occupant(Vector2Int p, GridActor except = null)
        {
            foreach (var a in actors)if(a && a != except && a.health>0 && a.Covers(p))return a;
            return null;
        }

        // Comprueba si se puede ocupar una casilla.
        public bool Free(Vector2Int p, GridActor except = null)=>Floor(p) && !bombs.ContainsKey(p) && !Occupant(p, except);

        // Calcula hasta dónde llega una explosión.
        public List<Vector2Int> Blast(Vector2Int origin, int radius)
        {
            var cells = new List<Vector2Int>
            {
                origin
            };
            foreach (var d in Directions)for(int i = 1;i<=radius;i++)
            {
                var p = origin+d*i;
                if (!Inside(p) || terrainReservations.Contains(p))break;
                if (blocks.TryGetValue(p, out var b))
                {
                    if (b.destructible)cells.Add(p);
                    break;
                }
                cells.Add(p);
            }
            return cells;
        }
        public bool Danger(Vector2Int p)
        {
            foreach (var b in bombs.Values)if(b && Blast(b.cell, b.radius).Contains(p))return true;
            return false;
        }
        public int BombCount(GridActor owner)
        {
            int count = 0;
            foreach (var b in bombs.Values)if(b && b.owner == owner && !b.special)count++;
            return count;
        }

        // Busca el siguiente paso de una ruta.
        public Vector2Int NextStep(GridActor actor, Vector2Int target, bool escape)
        {
            var q = new Queue<Vector2Int>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            q.Enqueue(actor.cell);
            parent[actor.cell] = actor.cell;
            var found = actor.cell;
            while (q.Count>0)
            {
                var p = q.Dequeue();
                if ((escape && !Danger(p)) || (!escape && (p-target).sqrMagnitude<=1))
                {
                    found = p;
                    break;
                }
                foreach (var d in Directions)
                {
                    var n = p+d;
                    if (parent.ContainsKey(n) || !Free(n, actor) || (!escape && Danger(n)))continue;
                    parent[n] = p;
                    q.Enqueue(n);
                }
            }
            if (found == actor.cell)return actor.cell;
            while (parent[found] != actor.cell)found = parent[found];
            return found;
        }
        HashSet<Vector2Int> Connected(Vector2Int start, Vector2Int excluded)
        {
            var seen = new HashSet<Vector2Int>();
            var q = new Queue<Vector2Int>();
            if (start == excluded || !Floor(start))return seen;
            seen.Add(start);
            q.Enqueue(start);
            while (q.Count>0)
            {
                var p = q.Dequeue();
                foreach (var d in Directions)
                {
                    var n = p+d;
                    if (n != excluded && Floor(n) && seen.Add(n))q.Enqueue(n);
                }
            }
            return seen;
        }

        // Comprueba si es seguro cerrar un paso.
        public bool CanClose(Vector2Int p)
        {
            if (!Free(p) || Danger(p) || p.x<=0 || p.y<=0 || p.x>=width-1 || p.y>=height-1)return false;
            // Comprueba solo la zona afectada al cerrar un paso.
            Vector2Int seed = p;
            foreach (var d in Directions)if(Floor(p+d))
            {
                seed = p+d;
                break;
            }
            if (seed == p)return false;
            var before = Connected(seed, new Vector2Int(-100, -100));
            var after = Connected(seed, p);
            return after.Count>=2 && after.Count == before.Count-1;
        }
    }
}
