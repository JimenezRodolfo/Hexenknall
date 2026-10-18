using UnityEngine;
namespace Labyrinth
{
    // Presenta las recompensas al terminar un nivel.
    public sealed class RewardManager : MonoBehaviour
    {
        int lastChoiceFrame = -1;
        public bool Active { get; private set; }
        public bool ChoosingCurse => RunProgress.Data.rewardPhase == 2;

        // Muestra las cartas al terminar un nivel.
        public void Begin(int clearedStage)
        {
            if (clearedStage<1 || clearedStage>Campaign.LastNormal)return;
            RunProgress.PrepareReward(clearedStage);
            Active = true;
            GameDirector.I.finished = true;
            GameDirector.I.SetTime();
        }

        // Aplica la recompensa seleccionada.
        public void Choose(int index)
        {
            if (!Active || lastChoiceFrame == Time.frameCount)return;
            var d = RunProgress.Data;
            if (index<0 || index>=d.offers.Count)return;
            var card = d.offers[index];
            if (card == null || card.curse != (d.rewardPhase == 2))return;
            lastChoiceFrame = Time.frameCount;
            GameDirector.I.sound.Play(SoundCue.CardChoose);
            CardRules.Apply(card, d);
            if (d.rewardPhase == 1)
            {
                d.chosenRarity = (int)card.rarity;
                d.rewardPhase = 2;
                d.offers = CardRules.Roll(d, true, card.rarity);
                RunProgress.Save();
            }
            else
            {
                Active = false;
                RunProgress.CompleteReward();
                GameDirector.I.Load(Campaign.Scene(d.stage));
            }
        }
    }
}
