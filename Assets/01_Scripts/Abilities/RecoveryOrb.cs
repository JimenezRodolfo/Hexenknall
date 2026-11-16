using UnityEngine;
namespace Labyrinth
{
    public sealed class RecoveryOrb : MonoBehaviour
    {
        public Vector2Int cell;
        public float readyAt;
        void Update()
        {
            var g=GameDirector.I;
            if (!g || !g.Combat || Time.time<readyAt || !g.board.Floor(cell) || !g.player || g.player.health<=0)return;
            // La llama que abrió la caja termina antes de permitir recoger el orbe.
            foreach (var h in g.board.hazards)if(h && h.duration>0 && h.cell==cell)return;
            if ((g.player.Position-g.board.World(cell)).sqrMagnitude > .4f || g.player.health>=g.player.maxHealth)return;
            g.player.health=Mathf.Min(g.player.maxHealth,g.player.health+1);
            g.ui.Toast("Orbe: +1 de vida");
            g.sound.Play(SoundCue.OldFriendGift);
            Destroy(gameObject);
        }
    }
}
