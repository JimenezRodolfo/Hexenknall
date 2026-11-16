using UnityEngine;
namespace Labyrinth
{
    // Presenta las recompensas al terminar un nivel.
    public sealed class RewardManager : MonoBehaviour
    {
        int lastChoiceFrame = -1;
        public bool Active { get; private set; }
        public bool ChoosingCurse => RunProgress.Data.rewardPhase == 2;
        public bool OfferingPact => RunProgress.Data.rewardPhase == 3;
        public static void PreparePacts(RunData d)
        {
            d.offers=CardRules.Roll(d,false);
            d.pactCosts.Clear();
            foreach(var boon in d.offers)
            {
                var costs=CardRules.Roll(d,true,boon.rarity);
                if(costs.Count==0) { d.offers.Clear(); d.pactCosts.Clear(); return; }
                d.pactCosts.Add(costs[0]);
            }
        }
        public static bool ApplyChoice(RunData d,int index)
        {
            if(d.rewardPhase==0 || index<0 || index>=d.offers.Count)return false;
            var card=d.offers[index];
            if(!CardRules.Eligible(card.effect,d) || card.curse!=(d.rewardPhase==2))return false;
            if(d.rewardPhase==3)
            {
                if(!d.easyReward || index>=d.pactCosts.Count || !d.pactCosts[index].curse || !CardRules.Eligible(d.pactCosts[index].effect,d))return false;
                CardRules.Apply(card,d); CardRules.Apply(d.pactCosts[index],d);
                d.lastReward="Pacto: "+CardRules.Title(card.effect)+": "+CardRules.Description(card)+" / "+CardRules.Title(d.pactCosts[index].effect)+": "+CardRules.Description(d.pactCosts[index]);
                return RunProgress.AdvanceAfterReward(d);
            }
            CardRules.Apply(card,d);
            d.lastReward=(d.rewardPhase==2 ? d.lastReward+" / " : "")+CardRules.Title(card.effect)+": "+CardRules.Description(card);
            if(d.rewardPhase==1)
            {
                d.chosenRarity=(int)card.rarity;
                if(d.easyReward) { d.rewardPhase=3; PreparePacts(d); }
                else { d.rewardPhase=2; d.offers=CardRules.Roll(d,true,card.rarity); }
            }
            else return RunProgress.AdvanceAfterReward(d);
            return true;
        }
        public void ContinueWithoutPact()
        {
            if(!Active || !OfferingPact || lastChoiceFrame==Time.frameCount)return;
            lastChoiceFrame=Time.frameCount;
            if(!RunProgress.AdvanceAfterReward(RunProgress.Data))return;
            Active=false; RunProgress.Save(); GameDirector.I.Load(Campaign.Scene(RunProgress.Data.stage));
        }
        public void ContinueExhausted()
        {
            var d=RunProgress.Data;
            if(!Active || d.offers.Count!=0)return;
            if(d.rewardPhase==1)
            {
                d.rewardPhase=d.easyReward?3:2;
                if(!d.easyReward)d.offers=CardRules.Roll(d,true,(Rarity)d.chosenRarity);
            }
            if(d.offers.Count==0)
            {
                if(!RunProgress.AdvanceAfterReward(d))return;
                Active=false; RunProgress.Save(); GameDirector.I.Load(Campaign.Scene(d.stage));
            }
            else RunProgress.Save();
        }

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
            if (!ApplyChoice(d,index))return;
            lastChoiceFrame = Time.frameCount;
            GameDirector.I.sound.Play(SoundCue.CardChoose);
            RunProgress.Save();
            if (d.rewardPhase == 0)
            {
                Active = false;
                GameDirector.I.Load(Campaign.Scene(d.stage));
            }
        }
    }
}
