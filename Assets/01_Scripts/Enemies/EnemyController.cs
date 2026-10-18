using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Controla el movimiento y los ataques de los enemigos.
    public sealed class EnemyController : GridActor
    {
        public enum State
        {
            Patrol, Chase, Attack, Escape, Dead
        }
        public State state;
        public bool bomber;
        public int detection = 30;
        public float decisionInterval = .12f, attackCooldown = 1.4f, bombCooldown = 3.5f;
        public float phaseSpeedIncrease = .45f, patternCooldown = 5f, patternWarning = .9f;
        [Header("Reaccion y escape")]
        public float reactionMin = .35f, reactionMax = .65f;
        public float escapeLead = .9f, safetyMargin = .10f;
        public int bombAwareness = 7;
        [Header("Golpe cuerpo a cuerpo")]
        public float meleeWindup = .28f, meleeReach = 1.04f;
        float decideAt, attackAt, bombAt, patternAt, baseSpeed, swingAt;
        int bombRadius = 2;
        bool swinging;
        BossCombat bossCombat;
        Vector2Int? escapeGoal;
        Vector2Int swingCell;
        Bomb ownBomb;
        GameObject swingMarker;
        readonly Dictionary<Bomb, float> noticed = new Dictionary<Bomb, float>();
        readonly Dictionary<Vector2Int, List<Vector2>> dangers = new Dictionary<Vector2Int, List<Vector2>>();
        static int Distance(Vector2Int a, Vector2Int b)=>Mathf.Abs(a.x-b.x)+Mathf.Abs(a.y-b.y);

        // Inicializa el comportamiento del enemigo.
        protected override void Start()
        {
            var d = RunProgress.Data;
            if (GameDirector.I.stage>0)
            {
                if (!boss)maxHealth+=d.enemyHealthBonus;
                health = maxHealth;
                speed*=1+d.enemySpeedBonus;
                bombRadius+=d.enemyRadiusBonus;
                attackCooldown/=1+d.enemyAttackHaste;
                bombCooldown/=1+d.enemyAttackHaste;
                patternCooldown/=1+d.enemyAttackHaste;
            }
            if (boss)
            {
                maxHealth = health = 4;
                bossCombat = GetComponent<BossCombat>();
                if (!bossCombat)bossCombat = gameObject.AddComponent<BossCombat>();
                bossCombat.Bind(this);
            }
            baseSpeed = speed;
            base.Start();
        }

        // Decide cuándo moverse o atacar.
        protected override void Update()
        {
            base.Update();
            var g = GameDirector.I;
            if (!g || !g.Combat || !g.player || g.player.health<=0)return;
            if (health<=0)
            {
                state = State.Dead;
                return;
            }
            if (bossCombat && bossCombat.Tick())
            {
                CancelSwing();
                return;
            }
            // Revisa el peligro incluso mientras camina.
            ObserveBombs(g.board);
            if (Moving)return;
            if (Time.time<decideAt)return;
            BuildForecast(g.board);
            bool threatened = UnsafeDuring(cell, 0, Mathf.Max(.1f, escapeLead));
            if (ownBomb && !UnsafeDuring(cell, 0, 10))ownBomb = null;
            if (threatened || ownBomb)
            {
                CancelSwing();
                state = State.Escape;
                var step = FindStep(g.player.cell, true);
                if (step != cell && Step(step-cell))return;
                if (!UnsafeDuring(cell, 0, 10))ownBomb = null;
                // Si no hay ruta segura, vuelve a intentar sin teletransportarse.
                decideAt = Time.time+Mathf.Max(.03f, decisionInterval);
                return;
            }
            escapeGoal = null;
            if (swinging)
            {
                if (Time.time>=swingAt)
                {
                    if (CanMelee(g.player) && g.player.OverlapsDamageCell(swingCell, .4f))
                    {
                        g.player.Hit(1);
                        g.sound.Play(SoundCue.MeleeImpact);
                    }
                    CancelSwing();
                    attackAt = Time.time+attackCooldown;
                }
                return;
            }
            int phase = boss && health<=2?1:0;
            if (boss)
            {
                speed = baseSpeed+phase*phaseSpeedIncrease;
                if (phase>0 && Time.time>=patternAt)
                {
                    patternAt = Time.time+patternCooldown;
                    g.StartCoroutine(g.Telegraph(g.player.cell, patternWarning, 1));
                    if (visual)visual.Play("CastArea", .5f, true);
                }
            }
            if (CanMelee(g.player))
            {
                state = State.Attack;
                if (Time.time>=attackAt)
                {
                    Vector3 aim = g.player.Position-Position;
                    facing = Mathf.Abs(aim.x)>Mathf.Abs(aim.z)?new Vector2Int(aim.x>=0?1:-1, 0):new Vector2Int(0, aim.z>=0?1:-1);
                    Face(facing);
                    g.sound.Play(SoundCue.MeleeWindup);
                    float windup = Mathf.Max(.12f, meleeWindup);
                    swinging = true;
                    swingAt = Time.time+windup;
                    if (visual)visual.Play("Slash", windup*2, true);
                    swingCell = new Vector2Int(Mathf.RoundToInt(g.player.Position.x), Mathf.RoundToInt(g.player.Position.z));
                    swingMarker = MagicVfx.Circle(g.board.World(swingCell, .055f), MagicKind.Warning, .38f);
                }
                return;
            }
            var target = g.player.Moving?g.player.Destination:g.player.cell;
            int distance = Distance(cell, target);
            // Busca una salida antes de colocar la bomba.
            if ((bomber || boss) && distance<=4 && Time.time>=bombAt)
            {
                AddBlast(g.board.Blast(cell, bombRadius), 2, .38f);
                escapeGoal = null;
                var escape = FindStep(target, true);
                if (escape != cell && g.PlaceBomb(cell, 2, bombRadius, 1, this))
                {
                    bombAt = Time.time+bombCooldown;
                    ownBomb = g.board.bombs[cell];
                    state = State.Escape;
                    if (visual)visual.Play("PlaceBomb", .3f, true);
                    Step(escape-cell);
                    return;
                }
                BuildForecast(g.board);
            }
            Vector2Int next;
            if (distance<=detection)
            {
                state = State.Chase;
                next = FindStep(target, false);
            }
            else
            {
                state = State.Patrol;
                next = cell;
                int offset = Random.Range(0, 4);
                for (int i = 0;i<4;i++)
                {
                    var p = cell+GridBoard.Directions[(offset+i)%4];
                    if (g.board.Free(p, this) && SafeTransition(cell, p, 0, 1/Mathf.Max(.1f, speed)))
                    {
                        next = p;
                        break;
                    }
                }
            }
            if (!Step(next-cell))decideAt = Time.time+Mathf.Max(.03f, decisionInterval);
        }

        // Revisa las bombas cercanas.
        void ObserveBombs(GridBoard board)
        {
            var removed = new List<Bomb>();
            foreach (var pair in noticed)if(!pair.Key || !board.bombs.ContainsValue(pair.Key))removed.Add(pair.Key);
            foreach (var bomb in removed)noticed.Remove(bomb);
            foreach (var bomb in board.bombs.Values)
            {
                if (!bomb || noticed.ContainsKey(bomb))continue;
                if (bomb.owner != this && Distance(cell, bomb.cell)>bombAwareness)continue;
                float delay = bomb.owner == this?0:Random.Range(Mathf.Max(0, reactionMin), Mathf.Max(reactionMin, reactionMax));
                noticed[bomb] = Time.time+delay;
            }
        }
        void AddWindow(Vector2Int p, float start, float end)
        {
            if (!dangers.TryGetValue(p, out var windows))
            {
                windows = new List<Vector2>();
                dangers[p] = windows;
            }
            windows.Add(new Vector2(start, end));
        }
        void AddBlast(List<Vector2Int> cells, float time, float duration)
        {
            foreach (var p in cells)AddWindow(p, time, time+duration);
        }

        // Calcula las casillas que estarán en peligro.
        void BuildForecast(GridBoard board)
        {
            dangers.Clear();
            var known = new List<Bomb>();
            var times = new List<float>();
            var blasts = new List<List<Vector2Int>>();
            foreach (var pair in noticed) if(pair.Key && pair.Value<=Time.time)
            {
                known.Add(pair.Key);
                times.Add(pair.Key.Remaining);
                blasts.Add(board.Blast(pair.Key.cell, pair.Key.radius));
            }
            // Las bombas encadenadas usan el primer tiempo de detonación.
            for (int pass = 0;pass<known.Count;pass++)
            {
                bool changed = false;
                for (int i = 0;i<known.Count;i++)for(int j = 0;j<known.Count;j++) if(times[i]<times[j] && blasts[i].Contains(known[j].cell))
                {
                    times[j] = times[i];
                    changed = true;
                }
                if (!changed)break;
            }
            for (int i = 0;i<known.Count;i++)AddBlast(blasts[i], times[i], .38f);
            foreach (var hazard in board.hazards) if(hazard && hazard.duration>0)AddWindow(hazard.cell, 0, hazard.duration);
        }
        bool UnsafeDuring(Vector2Int p, float from, float until)
        {
            if (!dangers.TryGetValue(p, out var windows))return false;
            foreach (var window in windows)if(until>=window.x-safetyMargin && from<=window.y+safetyMargin)return true;
            return false;
        }
        bool SafeTransition(Vector2Int from, Vector2Int to, float elapsed, float travel)
        {
            // Revisa la zona central del daño durante cada paso.
            return !UnsafeDuring(from, elapsed, elapsed+travel*.6f) && !UnsafeDuring(to, elapsed+travel*.4f, elapsed+travel+.06f);
        }

        // Busca un paso seguro.
        Vector2Int FindStep(Vector2Int target, bool escape)
        {
            var board = GameDirector.I.board;
            float travel = 1/Mathf.Max(.1f, speed);
            var queue = new Queue<Vector2Int>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var depth = new Dictionary<Vector2Int, int>();
            queue.Enqueue(cell);
            parent[cell] = cell;
            depth[cell] = 0;
            Vector2Int best = cell;
            int bestDistance = Distance(cell, target);
            Vector2Int? firstSafe = null;
            while (queue.Count>0)
            {
                var p = queue.Dequeue();
                float arrival = depth[p]*travel;
                if (escape && !UnsafeDuring(p, arrival, Mathf.Max(arrival+.5f, 10)))
                {
                    if (!firstSafe.HasValue)firstSafe = p;
                    if (!escapeGoal.HasValue || escapeGoal.Value == p)
                    {
                        best = p;
                        break;
                    }
                }
                if (!escape)
                {
                    int distance = Distance(p, target);
                    if (distance<bestDistance)
                    {
                        best = p;
                        bestDistance = distance;
                    }
                    if (distance<=1)break;
                }
                foreach (var d in GridBoard.Directions)
                {
                    var n = p+d;
                    if (parent.ContainsKey(n) || !board.Free(n, this) || !SafeTransition(p, n, arrival, travel))continue;
                    parent[n] = p;
                    depth[n] = depth[p]+1;
                    queue.Enqueue(n);
                }
            }
            if (escape)
            {
                if (best == cell && firstSafe.HasValue)best = firstSafe.Value;
                if (best != cell)escapeGoal = best;
            }
            if (best == cell)
            {
                if (!escape && bestDistance>1)
                {
                    // Si la ruta se bloquea, busca otra sin quedarse parado.
                    int score = int.MaxValue;
                    Vector2Int lateral = cell;
                    foreach (var d in GridBoard.Directions)
                    {
                        var n = cell+d;
                        if (!board.Free(n, this) || !SafeTransition(cell, n, 0, travel))continue;
                        int cost = Distance(n, target)+(n == previous?2:0);
                        if (cost<score)
                        {
                            score = cost;
                            lateral = n;
                        }
                    }
                    return lateral;
                }
                return cell;
            }
            while (parent[best] != cell)best = parent[best];
            return best;
        }

        // Comprueba el alcance de un golpe.
        public bool CanMelee(GridActor target)
        {
            if (!target || target.health<=0)return false;
            Vector3 a = Position, b = target.Position;
            float dx = Mathf.Abs(a.x-b.x), dz = Mathf.Abs(a.z-b.z);
            if (Mathf.Min(dx, dz)>.22f || dx*dx+dz*dz>meleeReach*meleeReach)return false;
            var middle = new Vector2Int(Mathf.RoundToInt((a.x+b.x)*.5f), Mathf.RoundToInt((a.z+b.z)*.5f));
            return GameDirector.I.board.Floor(middle);
        }

        // Cancela un ataque pendiente.
        void CancelSwing()
        {
            swinging = false;
            if (swingMarker)Destroy(swingMarker);
        }
        void OnDisable()
        {
            CancelSwing();
        }
    }
}
