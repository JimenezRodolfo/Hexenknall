using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Maneja la conversación y el intercambio en el refugio.
    public sealed class OldFriendRoom : MonoBehaviour
    {
        public bool Choosing { get; private set; }
        public Vector2Int friendCell = new Vector2Int(3, 9), portalCell = new Vector2Int(9, 7);
        GameDirector g;
        GameObject portal;
        bool leaving;

        // Prepara el refugio y sus referencias.
        public void Initialize(GameDirector director)
        {
            g = director;
            CardLedger.Ensure(RunProgress.Data);
            var d = RunProgress.Data;
            if (d.oldFriendOrder.Count != 3 || !d.oldFriendOrder.Contains(0) || !d.oldFriendOrder.Contains(1) || !d.oldFriendOrder.Contains(2))
            {
                d.oldFriendOrder.Clear();
                d.oldFriendOrder.AddRange(new[]
                {
                    0, 1, 2
                });
                for (int i = 2;i>0;i--)
                {
                    int j = UnityEngine.Random.Range(0, i+1);
                    int n = d.oldFriendOrder[i];
                    d.oldFriendOrder[i] = d.oldFriendOrder[j];
                    d.oldFriendOrder[j] = n;
                }
                RunProgress.Save();
            }
            var catalog = ArtCatalog.Current;
            if (catalog && catalog.oldFriend)
            {
                var friend = Instantiate(catalog.oldFriend, new Vector3(friendCell.x, 1.18f, g.board.height-1), Quaternion.Euler(0, 180, 0));
                friend.name = "Old Friend - sentada en el muro";
            }
            portal = MagicVfx.Circle(g.board.World(portalCell, .065f), MagicKind.Portal, .85f);
            portal.name = "Portal al nivel 5";
            var trigger = portal.AddComponent<BoxCollider>();
            trigger.size = new Vector3(1, 2, 1);
            trigger.center = Vector3.up;
            trigger.isTrigger = true;
        }
        public bool CanTalk=>g && g.player && !g.player.Moving && (g.player.Position-g.board.World(friendCell)).sqrMagnitude<=4.1f;

        // Abre la conversación con el Viejo Amigo.
        public void Interact()
        {
            if (!CanTalk)
            {
                g.ui.Toast("Acércate a Old Friend y pulsa Z.");
                return;
            }
            if (RunProgress.Data.oldFriendResolved)
            {
                g.ui.ShowDialogue(DialogueLibrary.Get("OldFriend.after"), null);
                return;
            }
            g.ui.ShowDialogue(DialogueLibrary.Get("OldFriend.intro"), ()=>
            {
                bool eligible = false;for(int i = 0;i<3;i++)eligible|=CardLedger.CanExchange(RunProgress.Data, i); if(!eligible)
                {
                    RunProgress.Data.oldFriendResolved = true;RunProgress.Save();g.ui.Toast("No tienes pactos que retirar. El portal está abierto.");return;
                }
                Choosing = true;g.paused = true;g.SetTime();
            });
        }

        // Procesa la elección del intercambio.
        public void Choose(int index)
        {
            if (!Choosing || index<0 || index>=3)return;
            var d = RunProgress.Data;
            int option = d.oldFriendOrder[index];
            if (!CardLedger.Exchange(d, option, new System.Random(Guid.NewGuid().GetHashCode())))return;
            Choosing = false;
            RunProgress.Save();
            g.sound.Play(SoundCue.OldFriendGift);
            g.player.RefreshRunBonuses();
            g.paused = false;
            g.SetTime();
            var lines = new List<string>();
            foreach (string template in DialogueLibrary.Get("OldFriend.result"))
            {
                if (template.Contains("{removed}"))foreach(string removed in d.oldFriendResult)lines.Add(template.Replace("{removed}", removed));
                else lines.Add(template);
            }
            g.ui.ShowDialogue(lines.ToArray(), null);
        }

        // Sale del refugio.
        IEnumerator Leave()
        {
            yield return new WaitForSecondsRealtime(.4f);
            g.Load("Level05");
        }
        void Update()
        {
            if (!g || !g.CanMove || !g.player || g.player.Moving || leaving)return;
            if ((g.player.Position-g.board.World(portalCell)).sqrMagnitude>.28f)return;
            if (!RunProgress.Data.oldFriendResolved)
            {
                g.ui.Toast("Habla con Old Friend antes de cruzar el portal.");
                return;
            }
            leaving = true;
            var d = RunProgress.Data;
            d.oldFriendVisited = true;
            d.stage = d.checkpointStage = 5;
            RunProgress.Save();
            g.finished = true;
            g.SetTime();
            g.sound.Play(SoundCue.Portal);
            StartCoroutine(Leave());
        }
    }
}
