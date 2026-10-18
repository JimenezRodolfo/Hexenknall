using UnityEngine;
using UnityEngine.InputSystem;
namespace Labyrinth
{
    // Gestiona el movimiento y los tres poderes del jugador.
    public sealed class PlayerController : GridActor
    {
        [Header("Z / círculo arcano")]
        public float normalFuse = 2, placementCooldown = .3f;
        public int normalRadius = 2, normalDamage = 1, maxBombs = 4;
        [Header("X / sello autonomo")]
        public float chargeTime = 1.2f, rayCooldown = 5;
        public int rayRange = 3, rayDamage = 1;
        [Header("C / apuntado")]
        public float specialFuse = 3.5f, specialCooldown = 8;
        public int aimRange = 3, specialRadius = 2, specialDamage = 1;
        public bool RayUnlocked { get; private set; }
        public bool SpecialUnlocked { get; private set; }
        public bool Aiming { get; private set; }
        public bool BombQueued=>bombQueued;
        public float RayRemaining=>Mathf.Max(0, nextRay-Time.time);
        public float SpecialRemaining=>Mathf.Max(0, nextSpecial-Time.time);
        public Vector2Int target;
        float nextRay, nextSpecial, nextNormal, nextAim;
        bool bombQueued, rayQueued, aimQueued;
        GameObject marker;
        int baseHealth, baseNormalRadius, baseCapacity, baseRayRange, baseSpecialRadius;
        float baseSpeed, baseImmunity, baseRayCooldown, baseSpecialCooldown;

        // Guarda los valores base del personaje.
        protected override void Start()
        {
            baseHealth = maxHealth;
            baseSpeed = speed;
            baseImmunity = invulnerability;
            baseNormalRadius = normalRadius;
            baseCapacity = maxBombs;
            baseRayRange = rayRange;
            baseSpecialRadius = specialRadius;
            baseRayCooldown = rayCooldown;
            baseSpecialCooldown = specialCooldown;
            RefreshRunBonuses();
            health = maxHealth;
            base.Start();
        }

        // Aplica las mejoras y maldiciones guardadas.
        public void RefreshRunBonuses()
        {
            var g = GameDirector.I;
            var d = RunProgress.Data;
            bool adventure = g.stage>0;
            RayUnlocked = g.stage == 0 || d.rayUnlocked;
            SpecialUnlocked = g.stage == 0 || d.specialUnlocked;
            maxHealth = baseHealth+(adventure?d.lifeBonus:0);
            health = Mathf.Min(health, maxHealth);
            speed = baseSpeed*(adventure?Mathf.Max(.35f, 1+d.speedBonus-d.playerSlow):1);
            invulnerability = baseImmunity+(adventure?d.immunityBonus:0);
            normalRadius = baseNormalRadius+(adventure?d.bombRadiusBonus:0);
            maxBombs = baseCapacity+(adventure?d.bombCapacityBonus:0);
            rayRange = baseRayRange+(adventure?d.rayRangeBonus:0);
            specialRadius = baseSpecialRadius+(adventure?d.specialRadiusBonus:0);
            float factor = adventure?Mathf.Max(.3f, 1-d.cooldownReduction+d.cooldownPenalty):1;
            rayCooldown = baseRayCooldown*factor;
            specialCooldown = baseSpecialCooldown*factor;
            if (!SpecialUnlocked)CancelAim();
            ClearQueuedActions();
        }
        // 0 = Dificil (direcciones relativas a la camara).
        // 1 = Muy dificil (direcciones fijas originales).
        public const string DifficultyPrefKey = "HEXENKNALL.Difficulty";
        public static int Difficulty=>Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 1);
        public static string DifficultyName=>Difficulty == 0?"DIFICIL":"MUY DIFICIL";

        // Lee las teclas y adapta la dirección a la dificultad.
        public static Vector2Int Direction()
        {
            var k = Keyboard.current;
            if (k == null)return Vector2Int.zero;
            Vector2Int input = Vector2Int.zero;
            if (k.wKey.isPressed || k.upArrowKey.isPressed)input = Vector2Int.up;
            else if(k.sKey.isPressed || k.downArrowKey.isPressed)input = Vector2Int.down;
            else if(k.aKey.isPressed || k.leftArrowKey.isPressed)input = Vector2Int.left;
            else if(k.dKey.isPressed || k.rightArrowKey.isPressed)input = Vector2Int.right;
            if (input == Vector2Int.zero || Difficulty != 0)return input;
            // Usamos la orientacion REAL de la camara, no el angulo objetivo,
            // para adaptar movimiento y apuntado mientras se anima la rotacion.
            var cam = Camera.main;
            if (!cam)return input;
            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            if (forward.sqrMagnitude<.0001f)return input;
            Vector2Int up = Mathf.Abs(forward.x)>Mathf.Abs(forward.z) ?new Vector2Int(forward.x>0?1:-1, 0) :new Vector2Int(0, forward.z>0?1:-1);
            Vector2Int right = new Vector2Int(up.y, -up.x);
            return right*input.x+up*input.y;
        }

        // Lee el movimiento y los poderes Z, X y C.
        protected override void Update()
        {
            base.Update();
            var g = GameDirector.I;
            var k = Keyboard.current;
            if (k == null || !g || !g.CanMove || health<=0 || (g.ui && g.ui.InputConsumedFrame == Time.frameCount))return;
            if (g.stage == Campaign.OldFriend)
            {
                if (k.zKey.wasPressedThisFrame && g.oldFriend)g.oldFriend.Interact();
                if (!Moving && g.CanMove)Step(Direction());
                return;
            }
            if (Aiming)
            {
                AimInput(k);
                return;
            }
            // Lee las teclas incluso durante el movimiento entre casillas.
            if (k.zKey.wasPressedThisFrame)bombQueued = true;
            if (k.xKey.wasPressedThisFrame)
            {
                if (RayUnlocked && RayRemaining<=0)rayQueued = true;
                else if(!RayUnlocked)g.ui.Toast("X bloqueada: recupera el rayo con una carta.");
            }
            if (k.cKey.wasPressedThisFrame)
            {
                if (SpecialUnlocked && SpecialRemaining<=0)aimQueued = true;
                else if(!SpecialUnlocked)g.ui.Toast("C bloqueada: recupera la invocación celeste con una carta.");
            }
            if (Moving)return;
            // Ejecuta las acciones pendientes al llegar al centro de la casilla.
            if (bombQueued && Time.time>=nextNormal)
            {
                bombQueued = false;
                if (g.board.BombCount(this)>=maxBombs)g.ui.Toast("Límite de círculos Z activos.");
                else if(g.PlaceBomb(cell, normalFuse, normalRadius, normalDamage, this))
                {
                    nextNormal = Time.time+placementCooldown;
                    if (visual)visual.Play("PlaceBomb", .3f, true);
                }
            }
            if (rayQueued)
            {
                rayQueued = false;
                nextRay = Time.time+rayCooldown;
                MagicSeal.Create(g, cell, facing, chargeTime, rayRange, rayDamage);
                if (visual)visual.Play("CastSeal", .4f, true);
            }
            if (aimQueued)
            {
                aimQueued = false;
                Aiming = true;
                target = cell;
                marker = MagicVfx.Circle(g.board.World(target, .055f), MagicKind.Sky);
                return;
            }
            Step(Direction());
        }

        // Mueve la mira de la habilidad C.
        void AimInput(Keyboard k)
        {
            var g = GameDirector.I;
            if (k.escapeKey.wasPressedThisFrame)
            {
                g.sound.Play(SoundCue.AimCancel);
                CancelAim();
                return;
            }
            var d = Direction();
            if (d != Vector2Int.zero && Time.time>=nextAim)
            {
                var p = target+d;
                if (g.board.Inside(p) && Mathf.Abs(p.x-cell.x)+Mathf.Abs(p.y-cell.y)<=aimRange)
                {
                    target = p;
                    g.sound.Play(SoundCue.AimMove);
                }
                nextAim = Time.time+.14f;
            }
            marker.transform.position = g.board.World(target, .1f);
            MagicVfx.SetColor(marker, ValidTarget()?MagicVfx.ColorFor(MagicKind.Sky):Color.gray);
            if ((k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) && ValidTarget())
            {
                if (g.PlaceBomb(target, specialFuse, specialRadius, specialDamage, this, true))
                {
                    g.sound.Play(SoundCue.AimConfirm);
                    nextSpecial = Time.time+specialCooldown;
                    CancelAim();
                    if (visual)visual.Play("CastSpecial", .45f, true);
                }
            }
        }

        // Comprueba si la invocación puede colocarse.
        bool ValidTarget()
        {
            var b = GameDirector.I.board;
            return b.Floor(target) && !b.bombs.ContainsKey(target);
        }

        // Cierra el modo de apuntado.
        public void CancelAim()
        {
            Aiming = false;
            aimQueued = false;
            if (marker)Destroy(marker);
        }

        // Limpia las acciones pendientes.
        public void ClearQueuedActions()
        {
            bombQueued = rayQueued = aimQueued = false;
        }
    }
}
