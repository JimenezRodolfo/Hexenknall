using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    [DefaultExecutionOrder(200)]
    // Aplica el daño de los rayos y las explosiones.
    public sealed class Hazard:MonoBehaviour
    {
        public Vector2Int cell;
        public float duration = .35f;
        public int damage = 1;
        public bool bombDamage;
        // Todas las casillas del mismo rayo comparten el registro de daño.
        public HashSet<GridActor> hitActors = new HashSet<GridActor>();
        GridBoard board;
        int bornFrame;
        void Awake()
        {
            bornFrame = Time.frameCount;
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(.88f, 1.25f, .88f);
            if (GameDirector.I)
            {
                board = GameDirector.I.board;
                board.hazards.Add(this);
            }
        }

        // Actualiza el tiempo de vida del efecto.
        void LateUpdate()
        {
            var g = GameDirector.I;
            if (!g || Time.timeScale == 0 || !g.Combat)return;
            if (duration<=0)
            {
                Destroy(gameObject);
                return;
            }
            foreach (var a in board.actors.ToArray())
            {
                if (!a || !a.isActiveAndEnabled || a.health<=0 || (!bombDamage && a.player) || hitActors.Contains(a))continue;
                // Solo revisa movimientos hechos mientras la llama estaba activa.
                if (!a.OverlapsDamageCell(cell, .44f, Time.frameCount>bornFrame))continue;
                hitActors.Add(a);
                a.Hit(damage, bombDamage);
            }
            duration-=Time.deltaTime;
            if (duration<=0)Destroy(gameObject);
        }

        // Comprueba si un personaje sigue dentro del daño.
        void OnTriggerStay(Collider other)
        {
            var g = GameDirector.I;
            var a = other.GetComponent<GridActor>();
            if (!g || !g.Combat || duration<=0 || !a || a.health<=0 || (!bombDamage && a.player) || hitActors.Contains(a))return;
            if (a.OverlapsDamageCell(cell, .44f))
            {
                hitActors.Add(a);
                a.Hit(damage, bombDamage);
            }
        }
        void OnDestroy()
        {
            if (board)board.hazards.Remove(this);
        }
    }
}
