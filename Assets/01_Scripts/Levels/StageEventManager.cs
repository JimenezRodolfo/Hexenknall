using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Activa las oleadas y los cambios del escenario.
    public sealed class StageEventManager : MonoBehaviour
    {
        public bool dynamicStage;
        public int waves, enemiesPerWave = 2, enemyThreshold = 2;
        public float eventInterval = 12, warningTime = 1.1f, wallMoveDuration = 1.1f;
        public Vector2Int[] changeCells =
        {
            new Vector2Int(6, 5), new Vector2Int(3, 4), new Vector2Int(9, 6)
        };
        [Header("Expansion real del tablero")]
        public bool expansionEnabled;
        public int expansionAmount = 8, maxExpansions = 1, enemiesPerExpansion = 2;
        public float expansionAfter = 22, expansionDuration = 2.4f;
        public Material floorMaterial;
        public int PendingWaves=>waves;
        public bool HasPendingWork=>waves>0 || busy || (expansionEnabled && expansions<maxExpansions);
        float elapsed, stageTime;
        bool busy;
        int index, expansions;
        GameDirector G=>GameDirector.I;
        float WallDuration=>Mathf.Max(.35f, wallMoveDuration/(1+RunProgress.Data.wallHaste));

        // Controla cuándo ocurre un evento.
        void Update()
        {
            if (!G.Combat || busy)return;
            elapsed+=Time.deltaTime;
            stageTime+=Time.deltaTime;
            if (expansionEnabled && expansions<maxExpansions && (stageTime>=expansionAfter || G.Enemies<=enemyThreshold))
            {
                StartCoroutine(Expand());
                return;
            }
            if (waves>0 && G.Enemies == 0)
            {
                StartCoroutine(Wave());
                return;
            }
            if (dynamicStage && (elapsed>=eventInterval/(1+RunProgress.Data.chaosHaste) || (index == 0 && G.Enemies<=enemyThreshold)))
            {
                elapsed = 0;
                StartCoroutine(ChangeWalls());
            }
        }

        // Inicia una oleada de enemigos.
        IEnumerator Wave()
        {
            busy = true;
            G.ui.notice = "Una nueva oleada se acerca…";
            yield return new WaitForSeconds(warningTime);
            int spawned = SpawnSafe(enemiesPerWave, false);
            if (spawned>0)waves--;
            G.ui.notice = "";
            busy = false;
        }
        int SpawnSafe(int desired, bool outer, int oldWidth = 0, int oldHeight = 0)
        {
            int count = 0;
            var b = G.board;
            var cells = ProceduralLayout.Interior(b.width, b.height);
            for (int i = cells.Count-1;i>0;i--)
            {
                int j = Random.Range(0, i+1);
                var p = cells[i];
                cells[i] = cells[j];
                cells[j] = p;
            }
            foreach (var p in cells)
            {
                if (count>=desired)break;
                int x = p.x, y = p.y;
                if (outer && x<oldWidth && y<oldHeight)continue;
                if ((p-G.player.cell).sqrMagnitude<25)continue;
                if (G.SpawnEnemy(p, count%2 == 1))count++;
            }
            return count;
        }
        bool Interior(Vector2Int p)=>p.x>0 && p.y>0 && p.x<G.board.width-1 && p.y<G.board.height-1;
        List<Vector2Int> Candidates()
        {
            var result = new List<Vector2Int>();
            foreach (var p in changeCells)if(Interior(p) && !result.Contains(p))result.Add(p);
            for (int y = 1;y<G.board.height-1;y++)for(int x = 1;x<G.board.width-1;x++)
            {
                var p = new Vector2Int(x, y);
                if (!result.Contains(p))result.Add(p);
            }
            return result;
        }

        // Cambia muros sin bloquear el mapa.
        IEnumerator ChangeWalls()
        {
            busy = true;
            var b = G.board;
            index++;
            var choices = Candidates();
            // Elige un muro interior, sin tocar el borde del mapa.
            Block lowering = null;
            for (int i = 0;i<choices.Count;i++)
            {
                var p = choices[(i+index*7)%choices.Count];
                if (b.blocks.TryGetValue(p, out var block) && !block.destructible)
                {
                    lowering = block;
                    break;
                }
            }
            Vector2Int? loweredCell = lowering?(Vector2Int?)lowering.cell:null;
            if (lowering)
            {
                G.ui.notice = "El muro marcado va a bajar";
                yield return Warning(lowering.cell, new Color(.2f, .8f, 1));
                yield return Lower(lowering);
            }
            // Abre un paso antes de cerrar otro sin dividir el mapa.
            Vector2Int? rising = null;
            for (int i = 0;i<choices.Count;i++)
            {
                var p = choices[(i+index*11)%choices.Count];
                if (loweredCell.HasValue && p == loweredCell.Value)continue;
                if (b.CanClose(p))
                {
                    rising = p;
                    break;
                }
            }
            if (rising.HasValue)
            {
                var p = rising.Value;
                G.ui.notice = "El muro marcado va a subir";
                yield return Warning(p, Color.magenta);
                // Comprueba otra vez la casilla después del aviso.
                if (b.CanClose(p))
                {
                    var wall = CreateWall(p, -1.5f);
                    // El muro bloquea la casilla desde que empieza el movimiento.
                    yield return AnimateY(wall.transform, .6f, WallDuration);
                }
                else G.ui.Toast("Cambio aplazado: la casilla está ocupada o es un paso necesario.");
            }
            var camera = Camera.main?Camera.main.GetComponent<OrbitCamera>():null;
            if (camera)
            {
                camera.angle+=90;
                camera.elevation = index%2 == 0?52:62;
                camera.Pulse(.35f);
            }
            G.sound.Play(SoundCue.WallShift);
            G.ui.notice = "";
            busy = false;
        }

        // Muestra el aviso antes de mover un muro.
        IEnumerator Warning(Vector2Int p, Color color)
        {
            var marker = G.Visual(PrimitiveType.Cylinder, G.board.World(p, .07f), new Vector3(.95f, .025f, .95f), color);
            float t = 0;
            while (t<warningTime)
            {
                t+=Time.deltaTime;
                marker.transform.localScale = new Vector3(.8f+.16f*Mathf.Sin(t*16), .025f, .8f+.16f*Mathf.Sin(t*16));
                yield return null;
            }
            Destroy(marker);
        }
        IEnumerator Lower(Block wall)
        {
            Vector2Int p = wall.cell;
            yield return AnimateY(wall.transform, -1.5f, WallDuration);
            G.board.blocks.Remove(p);
            Destroy(wall.gameObject);
        }
        Block CreateWall(Vector2Int p, float y)
        {
            var obj = Instantiate(G.wallPrefab, G.board.World(p, y), Quaternion.identity);
            obj.name = "Muro cambiante "+p;
            var wall = obj.GetComponent<Block>();
            wall.cell = p;
            wall.destructible = false;
            G.board.blocks[p] = wall;
            return wall;
        }
        static IEnumerator AnimateY(Transform obj, float target, float duration)
        {
            float start = obj.position.y, t = 0;
            while (t<duration)
            {
                t+=Time.deltaTime;
                var pos = obj.position;
                pos.y = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t/duration));
                obj.position = pos;
                yield return null;
            }
            var final = obj.position;
            final.y = target;
            obj.position = final;
        }

        // Amplía el tablero durante la partida.
        IEnumerator Expand()
        {
            busy = true;
            var b = G.board;
            int oldW = b.width, oldH = b.height;
            int add = Mathf.Clamp(expansionAmount, 4, 12);
            if (add%2 != 0)add++;
            int newW = oldW+add, newH = oldH+add;
            G.ui.notice = "RUPTURA: el laberinto se expande";
            G.sound.Play(SoundCue.MapExpand);
            var camera = Camera.main?Camera.main.GetComponent<OrbitCamera>():null;
            if (camera)camera.Pulse(1);
            yield return new WaitForSeconds(warningTime);
            // El tablero crece sin desplazar a los personajes.
            var exits = new List<Block>();
            var exitCells = new HashSet<Vector2Int>();
            foreach (var pair in b.blocks) if((pair.Key.x == oldW-1 && pair.Key.y>0 && pair.Key.y<oldH) || (pair.Key.y == oldH-1 && pair.Key.x>0 && pair.Key.x<oldW))
            {
                exits.Add(pair.Value);
                exitCells.Add(pair.Key);
            }
            var layout = G.GetComponent<ProceduralLayout>();
            var plan = layout.ExpansionPlan(oldW, oldH, newW, newH, exitCells);
            b.width = newW;
            b.height = newH;
            var group = new GameObject("Expansion "+(expansions+1));
            var added = new List<Vector2Int>();
            for (int y = 0;y<newH;y++)for(int x = 0;x<newW;x++)
            {
                if (x<oldW && y<oldH)continue;
                var p = new Vector2Int(x, y);
                added.Add(p);
                b.terrainReservations.Add(p);
                var floor = G.Visual(PrimitiveType.Cube, b.World(p, -.15f), new Vector3(.98f, .25f, .98f), new Color(.14f, .16f, .23f));
                floor.name = "Floor "+x+" "+y;
                floor.AddComponent<BoxCollider>();
                if (floorMaterial)
                {
                    floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                    floor.GetComponent<Renderer>().SetPropertyBlock(null);
                }
                floor.transform.SetParent(group.transform, true);
                bool border = x == 0 || y == 0 || x == newW-1 || y == newH-1;
                if (border || plan.walls.Contains(p))
                {
                    var wall = CreateWall(p, .6f);
                    wall.transform.SetParent(group.transform, true);
                }
                else if(plan.crates.Contains(p))layout.CreateBlock(p, true, .5f, group.transform);
            }
            group.transform.position = Vector3.down*1.8f;
            // Los bordes anteriores bajan mientras sube el piso nuevo.
            float t = 0;
            Vector3[] starts = new Vector3[exits.Count];
            for (int i = 0;i<exits.Count;i++)starts[i] = exits[i].transform.position;
            while (t<expansionDuration)
            {
                t+=Time.deltaTime;
                float f = Mathf.SmoothStep(0, 1, t/expansionDuration);
                group.transform.position = Vector3.down*(1.8f*(1-f));
                for (int i = 0;i<exits.Count;i++)if(exits[i])exits[i].transform.position = starts[i]+Vector3.down*(2.1f*f);
                yield return null;
            }
            group.transform.position = Vector3.zero;
            foreach (var wall in exits)if(wall)
            {
                b.blocks.Remove(wall.cell);
                Destroy(wall.gameObject);
            }
            foreach (var p in added)b.terrainReservations.Remove(p);
            expansions++;
            elapsed = 0;
            SpawnSafe(enemiesPerExpansion, true, oldW, oldH);
            G.ui.notice = "Tablero expandido: "+b.width+" × "+b.height;
            yield return new WaitForSeconds(1.5f);
            G.ui.notice = "";
            busy = false;
        }
    }
}
