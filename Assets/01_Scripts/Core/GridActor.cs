using UnityEngine;
namespace Labyrinth
{
    // Gestiona el movimiento por casillas y el daño.
    public class GridActor : MonoBehaviour
    {
        public Vector2Int cell, previous;
        [HideInInspector]
        public CharacterVisual visual;
        public int maxHealth = 3, health = 3;
        [Tooltip("Casillas por segundo. Se completa cada paso antes de iniciar otro.")]
        public float speed = 4f, invulnerability = .85f;
        public bool player, boss, invincible, receiveBombDamage = true;
        public Vector2Int facing = Vector2Int.up;
        public bool Moving { get; private set; }
        public Vector2Int Destination { get; private set; }
        [Tooltip("Semiancho de la zona de dano en casillas; independiente de las reservas de movimiento.")]
        [Range(.05f, .3f)]
        public float hurtHalfSize = .16f;
        public Vector3 MotionFrom { get; private set; }
        public int MotionFrame { get; private set; } =  -1;
        float immuneUntil;
        Rigidbody body;
        public Vector3 Position=>body?body.position:transform.position;
        public CapsuleCollider HurtCollider { get; private set; }

        // Prepara la posición inicial del personaje.
        protected virtual void Start()
        {
            var g = GameDirector.I;
            if (!g || !g.board)
            {
                enabled = false;
                return;
            }
            var prototype = GetComponent<Renderer>();
            if (prototype)prototype.enabled = false;
            transform.localScale = Vector3.one;
            if (!g.board.actors.Contains(this))g.board.actors.Add(this);
            previous = cell;
            Destination = cell;
            transform.position = g.board.World(cell);
            if (ArtCatalog.Current)ArtCatalog.Current.DressActor(this);
            body = GetComponent<Rigidbody>();
            if (!body)body = gameObject.AddComponent<Rigidbody>();
            body.position = g.board.World(cell);
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;
            HurtCollider = GetComponent<CapsuleCollider>();
            if (!HurtCollider)HurtCollider = gameObject.AddComponent<CapsuleCollider>();
            HurtCollider.isTrigger = true;
            HurtCollider.radius = hurtHalfSize;
            HurtCollider.height = 1.15f;
            HurtCollider.center = Vector3.zero;
        }

        // Actualiza el movimiento y la orientación.
        protected virtual void Update()
        {
        }

        // Aplica el movimiento físico.
        void FixedUpdate()
        {
            if (!GameDirector.I || !GameDirector.I.CanMove || Time.timeScale == 0)return;
            AdvanceMovement(Time.fixedDeltaTime);
        }
        public void AdvanceMovement(float deltaTime)
        {
            if (!Moving || health<=0)return;
            if (MotionFrame != Time.frameCount)MotionFrom = Position;
            MotionFrame = Time.frameCount;
            var end = GameDirector.I.board.World(Destination);
            var next = Vector3.MoveTowards(Position, end, Mathf.Max(.1f, speed)*Mathf.Max(0, deltaTime));
            if (body)body.MovePosition(next);
            else transform.position = next;
            if ((next-end).sqrMagnitude<.000001f)
            {
                previous = cell;
                cell = Destination;
                Moving = false;
                if (player && GameDirector.I && GameDirector.I.sound)GameDirector.I.sound.Play(SoundCue.Footstep);
            }
        }
        // Reserva la casilla para evitar choques con personajes y muros.
        public bool Covers(Vector2Int p) => cell == p || (Moving && Destination == p);

        // Intenta avanzar a una casilla vecina.
        public bool Step(Vector2Int direction)=>BeginStep(direction, false);

        // Avanza durante una embestida.
        public bool DashStep(Vector2Int direction)=>BeginStep(direction, true);
        bool BeginStep(Vector2Int direction, bool dash)
        {
            if (Moving || Mathf.Abs(direction.x)+Mathf.Abs(direction.y) != 1 || health<=0)return false;
            var next = cell+direction;
            var board = GameDirector.I.board;
            if (dash)
            {
                if (!board.Floor(next) || board.bombs.ContainsKey(next))return false;
                var occupant = board.Occupant(next, this);
                if (occupant && !occupant.player)return false;
            }
            else if(!board.Free(next, this))return false;
            facing = direction;
            previous = cell;
            Destination = next;
            Moving = true;
            Face(direction);
            return true;
        }
        public void Face(Vector2Int direction)
        {
            if (direction == Vector2Int.zero)return;
            facing = direction;
            var rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
            if (body)body.rotation = rotation;
            else transform.rotation = rotation;
        }
        // El daño usa la posición real, no la casilla reservada.
        public bool OverlapsDamageCell(Vector2Int p, float flameHalfSize = .44f, bool sweep = false)
        {
            Vector3 to = Position;
            Vector3 from = sweep && MotionFrame == Time.frameCount?MotionFrom:to;
            float half = flameHalfSize+hurtHalfSize;
            float enter = 0, exit = 1;
            return Clip(from.x, to.x-from.x, p.x-half, p.x+half, ref enter, ref exit) && Clip(from.z, to.z-from.z, p.y-half, p.y+half, ref enter, ref exit);
        }
        static bool Clip(float start, float delta, float min, float max, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta)<.000001f)return start>=min && start<=max;
            float a = (min-start)/delta, b = (max-start)/delta;
            if (a>b)
            {
                float temp = a;
                a = b;
                b = temp;
            }
            enter = Mathf.Max(enter, a);
            exit = Mathf.Min(exit, b);
            return enter<=exit;
        }

        // Descuenta vida y activa la reacción al golpe.
        public bool Hit(int damage = 1, bool bombDamage = false)
        {
            if (damage<=0 || health<=0 || invincible || Time.time<immuneUntil || (bombDamage && !receiveBombDamage))return false;
            health = Mathf.Max(0, health-damage);
            immuneUntil = Time.time+invulnerability;
            var g = GameDirector.I;
            if (g && g.sound)g.sound.Play(player?(health == 0?SoundCue.PlayerDeath:SoundCue.PlayerHit):(health == 0?SoundCue.EnemyDeath:SoundCue.EnemyHit));
            if (player && g && g.ui)g.ui.ShakeHUD();
            if (health == 0)
            {
                Moving = false;
                if (visual)visual.Die();
                GameDirector.I.ActorDied(this);
                gameObject.SetActive(false);
            }
            else if(visual)visual.Play("Hit", .18f, true);
            return true;
        }
    }
}
