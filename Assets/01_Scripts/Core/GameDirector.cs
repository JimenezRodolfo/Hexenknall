using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth
{
    [DefaultExecutionOrder(-100)]
    // Coordina la partida, los niveles y las transiciones.
    public sealed class GameDirector:MonoBehaviour
    {
        public static GameDirector I;
        public int stage;
        // -1 menu, 0 prologue, 1..9 normals, 10 boss, 11 ending, 12 refuge
        public string levelTitle = "El despertar";
        public string nextScene = "Level01";
        public GameObject bombPrefab, meleePrefab, bomberPrefab, wallPrefab;
        [TextArea]
        public string[] opening =
        {
            "PROTAGONISTA|[DIÁLOGO INICIAL]"
        };
        [TextArea]
        public string[] closing =
        {
            "PROTAGONISTA|[DIÁLOGO DE VICTORIA]"
        };
        public float prologueDuration = 25;
        [HideInInspector]public GridBoard board;
        [HideInInspector]public PlayerController player;
        [HideInInspector]public GameUI ui;
        [HideInInspector]public SoundSystem sound;
        public Material effectMaterialAsset;
        public Material EffectMaterial=>effectMaterial;
        public RewardManager rewards;
        public OldFriendRoom oldFriend;
        public bool CanMove=>(Campaign.IsCombat(stage) || stage == Campaign.OldFriend) && !loading && !paused && !dialogue && !finished;
        public bool paused, dialogue, finished;
        public bool Combat=>Campaign.IsCombat(stage) && !loading && !paused && !dialogue && !finished;
        public int Enemies
        {
            get
            {
                int n = 0;
                foreach (var a in board.actors)if(a && !a.player && a.health>0)n++;
                return n;
            }
        }
        float elapsed;
        bool ready, loading;
        Material effectMaterial;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance()
        {
            I = null;
        }

        // Busca las referencias y prepara la escena.
        void Awake()
        {
            if (!Application.isPlaying)return;
            if (!SceneRuntimeGuard.Claim(this))return;
            if (Campaign.TryStage(gameObject.scene.name, out var sceneStage))stage = sceneStage;
            I = this;
            Time.timeScale = 1;
            board = GetComponent<GridBoard>();
            ui = GetComponent<GameUI>();
            sound = GetComponent<SoundSystem>();
            foreach (var candidate in SceneRuntimeGuard.InScene<PlayerController>(gameObject.scene))
            {
                player = candidate;
                break;
            }
            foreach (var b in SceneRuntimeGuard.InScene<Block>(gameObject.scene))board.blocks[b.cell] = b;
            effectMaterial = effectMaterialAsset?new Material(effectMaterialAsset):new Material(Shader.Find("HEXENKNALL/Prototype"));
            if (!GetComponent<MenuBackdrop>())gameObject.AddComponent<MenuBackdrop>();
            foreach (var root in gameObject.scene.GetRootGameObjects())if(root.name.StartsWith("Floor ") && !root.GetComponent<Collider>())root.AddComponent<BoxCollider>();
            opening = DialogueLibrary.Get(gameObject.scene.name+".opening", opening);
            closing = DialogueLibrary.Get(gameObject.scene.name+".closing", closing);
            rewards = GetComponent<RewardManager>();
            if (!rewards)rewards = gameObject.AddComponent<RewardManager>();
            if (RunProgress.Data.rewardPhase>0 && RunProgress.Data.rewardStage == stage)finished = true;
            if (ArtCatalog.Current)
            {
                var events = GetComponent<StageEventManager>();
                if (events)events.floorMaterial = ArtCatalog.Current.floor;
                if (!GetComponent<RuinsEnvironment>())gameObject.AddComponent<RuinsEnvironment>();
            }
            else Debug.LogError("Falta configurar arte. Ejecuta HEXENKNALL > Install models and animations antes de Play.");
            if (stage == Campaign.OldFriend)
            {
                oldFriend = gameObject.AddComponent<OldFriendRoom>();
                oldFriend.Initialize(this);
            }
            if (Campaign.IsCombat(stage))
            {
                var layout = GetComponent<ProceduralLayout>();
                if (!layout)layout = gameObject.AddComponent<ProceduralLayout>();
                layout.Initialize(this);
            }
        }

        // Carga el progreso y los diálogos iniciales.
        IEnumerator Start()
        {
            if (I != this)yield break;
            if (stage == Campaign.Ending && !RunProgress.Data.finalBossDefeated)
            {
                Load(Campaign.Scene(RunProgress.ResumeStage(RunProgress.Data)));
                yield break;
            }
            yield return null;
            ready = true;
            if (RunProgress.Data.rewardPhase>0 && RunProgress.Data.rewardStage == stage)
            {
                rewards.Begin(stage);
                yield break;
            }
            if (stage>=1)RunProgress.Enter(stage);
            if ((Campaign.IsCombat(stage) || stage == Campaign.OldFriend) && opening.Length>0)ui.ShowDialogue(opening, null);
        }

        // Revisa las condiciones del combate.
        void Update()
        {
            if (I != this || loading || !ready || !Combat)return;
            elapsed+=Time.deltaTime;
            if (stage == 0 && elapsed>=prologueDuration)
            {
                player.health = 0;
                if (player.visual)player.visual.Die();
                ActorDied(player);
                return;
            }
            var events = GetComponent<StageEventManager>();
            if (stage>=1 && Enemies == 0 && (events == null || !events.HasPendingWork))StartCoroutine(Win());
        }

        // Pausa o reanuda el tiempo del juego.
        public void SetTime()
        {
            Time.timeScale = (paused || dialogue || finished)?0:1;
        }

        // Cambia a otra escena.
        public void Load(string scene)
        {
            if (loading)return;
            loading = true;
            if (player)player.ClearQueuedActions();
            if (RunProgress.HasSave)RunProgress.Save();
            Time.timeScale = 1;
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        // Comienza una partida nueva.
        public void NewGame()
        {
            RunProgress.NewGame();
            Load("PrologueBoss");
        }

        // Retoma la partida guardada.
        public void Continue()
        {
            var d = RunProgress.Data;
            Load(Campaign.Scene(RunProgress.ResumeStage(d)));
        }

        // Procesa la derrota de un personaje.
        public void ActorDied(GridActor actor)
        {
            if (!actor.player || finished)return;
            player.CancelAim();
            player.ClearQueuedActions();
            finished = true;
            SetTime();
            if (stage == 0)
            {
                RunProgress.BeginAdventure();
                ui.ShowDialogue(closing, ()=>Load("Level01"));
            }
            else
            {
                RunProgress.Save();
                ui.defeat = true;
            }
        }

        // Finaliza el nivel y abre las recompensas.
        IEnumerator Win()
        {
            finished = true;
            SetTime();
            ui.notice = "VICTORIA";
            if (sound)sound.Play(SoundCue.Victory);
            if (stage == Campaign.FinalBoss)RunProgress.CompleteCampaign();
            else RunProgress.PrepareReward(stage);
            yield return new WaitForSecondsRealtime(1.2f);
            ui.notice = "";
            if (stage == Campaign.FinalBoss)
            {
                ui.ShowDialogue(closing, ()=>Load("Ending"));
            }
            else ui.ShowDialogue(closing, ()=>rewards.Begin(stage));
        }

        // Coloca una bomba en una casilla válida.
        public bool PlaceBomb(Vector2Int p, float fuse, int radius, int damage, GridActor owner = null, bool special = false)
        {
            if (!board.Floor(p) || board.bombs.ContainsKey(p))return false;
            var obj = Instantiate(bombPrefab, board.World(p, 0.35f), Quaternion.identity);
            var b = obj.GetComponent<Bomb>();
            b.cell = p;
            b.fuse = fuse;
            b.radius = radius;
            b.damage = damage;
            b.owner = owner;
            b.special = special;
            board.bombs[p] = b;
            return true;
        }
        public GameObject Visual(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var o = GameObject.CreatePrimitive(type);
            o.transform.position = position;
            o.transform.localScale = scale;
            Destroy(o.GetComponent<Collider>());
            var r = o.GetComponent<Renderer>();
            r.sharedMaterial = effectMaterial;
            Tint(o, color);
            return o;
        }
        public void Tint(GameObject obj, Color color)
        {
            var m = new MaterialPropertyBlock();
            m.SetColor("_Color", color);
            m.SetColor("_BaseColor", color);
            obj.GetComponent<Renderer>().SetPropertyBlock(m);
        }
        public void Flash(Vector2Int p, Color color, float duration, int damage, bool isBomb, HashSet<GridActor> hitActors = null, MagicKind kind = MagicKind.Normal)
        {
            var obj = new GameObject("Impacto magico");
            obj.transform.position = board.World(p);
            MagicVfx.Burst(board.World(p, .15f), kind, duration);
            var h = obj.AddComponent<Hazard>();
            h.cell = p;
            h.duration = duration;
            h.damage = damage;
            h.bombDamage = isBomb;
            h.hitActors = hitActors;
            if (h.hitActors == null)h.hitActors = new HashSet<GridActor>();
        }
        public IEnumerator Telegraph(Vector2Int p, float delay, int damage = 1)
        {
            if (!board.Floor(p))yield break;
            var marker = MagicVfx.Circle(board.World(p, .055f), MagicKind.Warning);
            yield return new WaitForSeconds(delay);
            Destroy(marker);
            if (!finished)Flash(p, Color.magenta, 0.4f, damage, true, null, MagicKind.Sky);
        }
        public bool CanEscapeBomb(GridActor a, int radius)
        {
            var danger = new HashSet<Vector2Int>(board.Blast(a.cell, radius));
            var q = new Queue<Vector2Int>();
            var seen = new HashSet<Vector2Int>
            {
                a.cell
            };
            q.Enqueue(a.cell);
            while (q.Count>0)
            {
                var p = q.Dequeue();
                if (!danger.Contains(p) && !board.Danger(p))return true;
                foreach (var d in GridBoard.Directions)if(board.Free(p+d, a) && seen.Add(p+d))q.Enqueue(p+d);
            }
            return false;
        }

        // Crea un enemigo en el tablero.
        public bool SpawnEnemy(Vector2Int p, bool bomber)
        {
            if (!board.Free(p) || board.Danger(p))return false;
            var o = Instantiate(bomber?bomberPrefab:meleePrefab, board.World(p), Quaternion.identity);
            var enemy = o.GetComponent<EnemyController>();
            enemy.cell = p;
            board.actors.Add(enemy);
            return true;
        }
        void OnApplicationPause(bool value)
        {
            if (value && I == this && RunProgress.HasSave)RunProgress.Save();
        }
        void OnApplicationQuit()
        {
            if (I == this && RunProgress.HasSave)RunProgress.Save();
        }
        void OnDestroy()
        {
            if (effectMaterial)Destroy(effectMaterial);
            if (I == this)
            {
                I = null;
                Time.timeScale = 1;
            }
        }
    }
}
