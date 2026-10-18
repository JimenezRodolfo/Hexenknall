using System;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Lleva el registro de las cartas del jugador.
    public static class CardLedger
    {
        // Prepara el historial de cartas.
        public static void Ensure(RunData d)
        {
            if (d.acquired == null)d.acquired = new List<CardOffer>();
            if (d.oldFriendOrder == null)d.oldFriendOrder = new List<int>();
            if (d.oldFriendResult == null)d.oldFriendResult = new List<string>();
            if (d.ledgerRevision>=1)return;
            foreach (string line in d.history)
            {
                if (string.IsNullOrEmpty(line))continue;
                foreach (CardEffect effect in Enum.GetValues(typeof(CardEffect)))
                {
                    if (CardRules.Removed(effect))continue;
                    string title = CardRules.Title(effect);
                    bool match = line.Contains(": "+title+" · ");
                    if (effect == CardEffect.UnlockSpecial)match|=line.Contains(": Bomba del exiliado · ");
                    if (effect == CardEffect.BombCapacity)match|=line.Contains(": Reserva de pólvora · ");
                    if (effect == CardEffect.EnemyRadius)match|=line.Contains(": Explosiones hostiles · ");
                    if (!match)continue;
                    for (int rarity = 0;rarity<4;rarity++)if(line.EndsWith(" · "+CardRules.RarityName((Rarity)rarity))) d.acquired.Add(new CardOffer(effect, (Rarity)rarity, (int)effect>=(int)CardEffect.EnemyHealth));
                    break;
                }
            }
            d.ledgerRevision = 1;
        }

        // Cuenta las cartas de un tipo.
        public static int Count(RunData d, bool curse)
        {
            Ensure(d);
            return d.acquired.FindAll(c=>c != null && c.curse == curse && !CardRules.Removed(c.effect)).Count;
        }

        // Comprueba si se permite un intercambio.
        public static bool CanExchange(RunData d, int option)
        {
            return option>=0 && option<=2 && Count(d, false)>=option && Count(d, true)>=option+1;
        }

        // Intercambia las cartas seleccionadas.
        public static bool Exchange(RunData d, int option, System.Random random)
        {
            Ensure(d);
            if (d.oldFriendResolved || !CanExchange(d, option))return false;
            d.oldFriendResult.Clear();
            Remove(d, false, option, random);
            Remove(d, true, option+1, random);
            Rebuild(d);
            d.oldFriendResolved = true;
            return true;
        }
        static void Remove(RunData d, bool curse, int count, System.Random random)
        {
            for (int n = 0;n<count;n++)
            {
                var indices = new List<int>();
                for (int i = 0;i<d.acquired.Count;i++)if(d.acquired[i].curse == curse)indices.Add(i);
                int index = indices[random.Next(indices.Count)];
                var card = d.acquired[index];
                d.oldFriendResult.Add((curse?"Maldición retirada: ":"Bendición retirada: ")+CardRules.Title(card.effect)+" · "+CardRules.RarityName(card.rarity));
                d.acquired.RemoveAt(index);
            }
        }

        // Reconstruye los efectos de las cartas.
        public static void Rebuild(RunData d)
        {
            d.rayUnlocked = d.specialUnlocked = false;
            d.lifeBonus = d.bombRadiusBonus = d.bombDamageBonus = d.bombCapacityBonus = d.rayRangeBonus = d.rayDamageBonus = d.specialRadiusBonus = 0;
            d.enemyHealthBonus = d.enemyDamageBonus = d.enemyRadiusBonus = 0;
            d.speedBonus = d.immunityBonus = d.cooldownReduction = d.enemySpeedBonus = d.enemyAttackHaste = d.chaosHaste = d.wallHaste = d.playerSlow = d.cooldownPenalty = 0;
            d.history.Clear();
            foreach (var card in d.acquired)
            {
                CardRules.Apply(card, d, false);
                d.history.Add((card.curse?"Maldición: ":"Ventaja: ")+CardRules.Title(card.effect)+" · "+CardRules.RarityName(card.rarity));
            }
        }
    }
}
