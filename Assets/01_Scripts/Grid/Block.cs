using UnityEngine;
namespace Labyrinth
{
    // Gestiona los bloques destructibles del mapa.
    public sealed class Block : MonoBehaviour
    {
        public bool destructible;
        public Vector2Int cell;
        bool broken;

        // Registra el bloque en el tablero.
        void Start()
        {
            if (ArtCatalog.Current)ArtCatalog.Current.DressBlock(this);
            var box = GetComponent<BoxCollider>();
            if (!box)box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(.96f, destructible?1:1.2f, .96f);
            box.center = Vector3.zero;
            box.isTrigger = false;
        }
        // Los ataques Z, X, C y las bombas enemigas rompen cajas.
        // Rompe el bloque si recibe daño suficiente.
        public bool Break(bool magic = false)
        {
            if (!destructible || broken)return false;
            var g = GameDirector.I;
            if (!g || !g.board || !g.board.blocks.TryGetValue(cell, out var registered) || registered != this)return false;
            broken = true;
            if (g.sound)g.sound.Play(SoundCue.CrateBreak);
            g.board.blocks.Remove(cell);
            gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                var fx = new GameObject("Fragmentos arcanos").AddComponent<BreakFragments>();
                fx.Begin(g.board.World(cell, .45f));
                Destroy(gameObject);
            }
            else DestroyImmediate(gameObject);
            return true;
        }
    }
}
