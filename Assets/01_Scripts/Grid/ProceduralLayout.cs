using System;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // El director crea el tablero al ejecutar el juego.
    // Genera los obstáculos y la distribución del nivel.
    public sealed class ProceduralLayout : MonoBehaviour
    {
        public sealed class Plan
        {
            public readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> crates = new HashSet<Vector2Int>();
        }
        GameDirector g;
        GameObject crateTemplate;
        int expansionIndex;

        // Genera la disposición inicial del nivel.
        public void Initialize(GameDirector director)
        {
            g = director;
            foreach (var block in g.board.blocks.Values)
            {
                if (!block || !block.destructible)continue;
                crateTemplate = Instantiate(block.gameObject, transform);
                crateTemplate.name = "Crate template (inactive)";
                crateTemplate.SetActive(false);
                break;
            }
            if (g.board.randomizeLayout && g.stage>=1 && g.stage<=Campaign.LastNormal)RebuildInitial();
        }
        int Seed(int salt)
        {
            if (g.board.layoutSeed<0)return Guid.NewGuid().GetHashCode();
            return unchecked(g.board.layoutSeed+g.stage*1009+salt*7919);
        }

        // Reconstruye los obstáculos iniciales.
        void RebuildInitial()
        {
            var b = g.board;
            var candidates = Interior(b.width, b.height);
            var protectedCells = new HashSet<Vector2Int>();
            foreach (var actor in FindObjectsByType<GridActor>(FindObjectsSortMode.None)) Protect(protectedCells, actor.cell, 2);
            var plan = Build(b.width, b.height, candidates, new HashSet<Vector2Int>(), protectedCells, b.solidDensity, crateTemplate?b.crateDensity:0, Seed(0));
            var old = new List<Block>(b.blocks.Values);
            foreach (var block in old)
            {
                var p = block.cell;
                if (p.x<=0 || p.y<=0 || p.x>=b.width-1 || p.y>=b.height-1)continue;
                b.blocks.Remove(p);
                block.gameObject.SetActive(false);
                Destroy(block.gameObject);
            }
            foreach (var p in plan.walls)CreateBlock(p, false, .6f);
            foreach (var p in plan.crates)CreateBlock(p, true, .5f);
        }

        // Prepara el área nueva del tablero.
        public Plan ExpansionPlan(int oldW, int oldH, int newW, int newH, HashSet<Vector2Int> exits)
        {
            var candidates = new List<Vector2Int>();
            var protectedCells = new HashSet<Vector2Int>();
            foreach (var p in Interior(newW, newH))
            {
                if (p.x<oldW && p.y<oldH)continue;
                candidates.Add(p);
                // Conecta el terreno nuevo con el tablero anterior.
                if ((p.x<=oldW+1 && p.y<oldH) || (p.y<=oldH+1 && p.x<oldW))protectedCells.Add(p);
            }
            Protect(protectedCells, new Vector2Int(newW-2, newH-2), 2);
            var fixedBlocks = new HashSet<Vector2Int>(g.board.blocks.Keys);
            fixedBlocks.ExceptWith(exits);
            return Build(newW, newH, candidates, fixedBlocks, protectedCells, g.board.solidDensity, crateTemplate?g.board.crateDensity:0, Seed(++expansionIndex));
        }

        // Coloca un bloque en su casilla.
        public Block CreateBlock(Vector2Int p, bool destructible, float y, Transform parent = null)
        {
            GameObject source = destructible?crateTemplate:g.wallPrefab;
            if (!source)return null;
            var obj = Instantiate(source, g.board.World(p, y), Quaternion.identity);
            obj.name = (destructible?"Caja ":"Muro ")+p;
            if (parent)obj.transform.SetParent(parent, true);
            var block = obj.GetComponent<Block>();
            block.cell = p;
            block.destructible = destructible;
            obj.SetActive(true);
            g.board.blocks[p] = block;
            return block;
        }
        public static List<Vector2Int> Interior(int width, int height)
        {
            var result = new List<Vector2Int>();
            for (int y = 1;y<height-1;y++)for(int x = 1;x<width-1;x++)result.Add(new Vector2Int(x, y));
            return result;
        }
        public static void Protect(HashSet<Vector2Int> cells, Vector2Int center, int radius)
        {
            for (int y = -radius;y<=radius;y++)for(int x = -radius;x<=radius;x++) if(Mathf.Abs(x)+Mathf.Abs(y)<=radius)cells.Add(center+new Vector2Int(x, y));
        }
        public static Plan Build(int width, int height, List<Vector2Int> candidates, HashSet<Vector2Int> fixedBlocks, HashSet<Vector2Int> protectedCells, float wallDensity, float crateDensity, int seed)
        {
            var result = new Plan();
            var random = new System.Random(seed);
            var floor = new HashSet<Vector2Int>(Interior(width, height));
            floor.ExceptWith(fixedBlocks);
            var shuffled = new List<Vector2Int>();
            var unique = new HashSet<Vector2Int>();
            foreach (var p in candidates)if(floor.Contains(p) && !protectedCells.Contains(p) && unique.Add(p))shuffled.Add(p);
            for (int i = shuffled.Count-1;i>0;i--)
            {
                int j = random.Next(i+1);
                var p = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = p;
            }
            int wallCount = Mathf.RoundToInt(candidates.Count*Mathf.Clamp01(wallDensity));
            int crateCount = Mathf.RoundToInt(candidates.Count*Mathf.Clamp01(crateDensity));
            Place(shuffled, floor, result.walls, wallCount);
            // Mezcla las posiciones de las cajas para evitar patrones repetidos.
            for (int i = shuffled.Count-1;i>0;i--)
            {
                int j = random.Next(i+1);
                var p = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = p;
            }
            Place(shuffled, floor, result.crates, crateCount);
            return result;
        }
        static void Place(List<Vector2Int> candidates, HashSet<Vector2Int> floor, HashSet<Vector2Int> output, int count)
        {
            foreach (var p in candidates)
            {
                if (output.Count>=count)break;
                if (!floor.Contains(p) || !CanRemove(floor, p))continue;
                floor.Remove(p);
                output.Add(p);
            }
        }

        // Comprueba si se puede retirar un bloque.
        static bool CanRemove(HashSet<Vector2Int> floor, Vector2Int p)
        {
            var neighbors = new List<Vector2Int>();
            foreach (var d in GridBoard.Directions)if(floor.Contains(p+d))neighbors.Add(p+d);
            if (neighbors.Count == 0)return false;
            if (neighbors.Count == 1)return true;
            var seen = new HashSet<Vector2Int>
            {
                neighbors[0]
            };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(neighbors[0]);
            while (queue.Count>0)
            {
                var cell = queue.Dequeue();
                foreach (var d in GridBoard.Directions)
                {
                    var n = cell+d;
                    if (n != p && floor.Contains(n) && seen.Add(n))queue.Enqueue(n);
                }
            }
            foreach (var n in neighbors)if(!seen.Contains(n))return false;
            return true;
        }
    }
}
