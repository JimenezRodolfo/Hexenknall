using System;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    // Define las cartas y sus efectos.
    public enum Rarity
    {
        Common, Rare, Epic, Mythic
    }
    // Conserva los números de las cartas para respetar partidas guardadas.
    public enum CardEffect
    {
        UnlockRay, UnlockSpecial, Life, Speed, Immunity, Cooldowns, BombRadius, BombDamage, BombCapacity, RayRange, RayDamage, SpecialRadius, EnemyHealth, EnemySpeed, EnemyDamage, EnemyAttack, EnemyRadius, FasterChaos, FasterWalls, SlowPlayer, LongerCooldowns
    }
    [Serializable]
    public class CardOffer
    {
        public CardEffect effect;
        public Rarity rarity;
        public bool curse;
        public CardOffer(CardEffect effect, Rarity rarity, bool curse)
        {
            this.effect =  effect;
            this.rarity =  rarity;
            this.curse =  curse;
        }
    }
    public static class CardRules
    {
        public static bool Removed(CardEffect e)=>e == CardEffect.BombDamage || e == CardEffect.RayDamage || e == CardEffect.EnemyDamage;
        public static int Rank(CardOffer card) => (int)card.rarity + 1;
        public static string RarityName(Rarity r) => new[]
        {
            "COMÚN", "RARA", "ÉPICA", "MÍTICA"
        }
        [(int)r];
        public static Color Tint(Rarity r) => new[]
        {
            new Color(.75f, .78f, .84f), new Color(.3f, .65f, 1), new Color(.77f, .4f, 1), new Color(1, .73f, .22f)
        }
        [(int)r];

        // Devuelve el nombre de la carta.
        public static string Title(CardEffect e)
        {
            switch (e)
            {
                case CardEffect.UnlockRay: return "Memoria del rayo";
                case CardEffect.UnlockSpecial: return "Llamado del exiliado";
                case CardEffect.Life: return "Corazón del laberinto";
                case CardEffect.Speed: return "Paso ligero";
                case CardEffect.Immunity: return "Velo protector";
                case CardEffect.Cooldowns: return "Flujo arcano";
                case CardEffect.BombRadius: return "Fuego expansivo";
                case CardEffect.BombDamage: return "Carta retirada";
                case CardEffect.BombCapacity: return "Reserva arcana";
                case CardEffect.RayRange: return "Mirada del horizonte";
                case CardEffect.RayDamage: return "Carta retirada";
                case CardEffect.SpecialRadius: return "Estrella de ruina";
                case CardEffect.EnemyHealth: return "Piel de piedra";
                case CardEffect.EnemySpeed: return "Cacería veloz";
                case CardEffect.EnemyDamage: return "Carta retirada";
                case CardEffect.EnemyAttack: return "Asedio incesante";
                case CardEffect.EnemyRadius: return "Rituales hostiles";
                case CardEffect.FasterChaos: return "Laberinto inquieto";
                case CardEffect.FasterWalls: return "Muros impacientes";
                case CardEffect.SlowPlayer: return "Grilletes invisibles";
                default: return "Magia agotada";
            }
        }

        // Muestra el efecto de la carta.
        public static string Description(CardOffer c)
        {
            int n =  Rank(c);
            switch (c.effect)
            {
                case CardEffect.UnlockRay: return "Desbloquea X: sello que dispara solo. Puedes seguir moviéndote."+(n>1?" Además, +"+(n-1)+" casillas de alcance de X.":"");
                case CardEffect.UnlockSpecial: return "Desbloquea C: apunta hasta 3 casillas y lanza una invocación celeste."+(n>1?" Además, +"+(n-1)+" radio de invocación C.":"");
                case CardEffect.Life: return "+" + n + " de vida máxima (bono máximo +8). Recuperas la vida al entrar al siguiente nivel.";
                case CardEffect.Speed: return "+" + (10*n) + "% de velocidad base (bono máximo +80%).";
                case CardEffect.Immunity: return "+" + (.15f*n).ToString("0.00") + " s de invulnerabilidad tras recibir daño (bono máximo +1.2 s).";
                case CardEffect.Cooldowns: return "Reduce X/C en " + (8*n) + "% de su cooldown base (reducción máxima 60%).";
                case CardEffect.BombRadius: return "+" + n + " casillas de alcance de Z (bono máximo +5). También aumenta tu riesgo de daño propio.";
                case CardEffect.BombDamage: return "Retirada: ya no modifica el daño.";
                case CardEffect.BombCapacity: return "+" + n + " círculos Z simultáneos propios (bono máximo +6).";
                case CardEffect.RayRange: return "+" + n + " casillas de alcance de X (bono máximo +5).";
                case CardEffect.RayDamage: return "Retirada: ya no modifica el daño.";
                case CardEffect.SpecialRadius: return "+" + n + " casillas de alcance de la invocación C (bono máximo +5).";
                case CardEffect.EnemyHealth: return "+" + n + " vida a enemigos comunes (bono acumulado máximo +10).";
                case CardEffect.EnemySpeed: return "+" + (8*n) + "% de velocidad base de enemigos (máximo +80%).";
                case CardEffect.EnemyDamage: return "Retirada: ya no modifica el daño.";
                case CardEffect.EnemyAttack: return "+" + (12*n) + "% de rapidez de ataques y colocación de círculos enemigos (máximo +120%).";
                case CardEffect.EnemyRadius: return "+" + n + " casillas de radio de círculos enemigos (bono máximo +4).";
                case CardEffect.FasterChaos: return "+" + (15*n) + "% de frecuencia de cambios de muros/cámara (máximo +150%).";
                case CardEffect.FasterWalls: return "+" + (25*n) + "% de velocidad de subida/bajada de muros (máximo +200%). Los avisos se conservan.";
                case CardEffect.SlowPlayer: return "−" + (5*n) + "% de tu velocidad base (penalización máxima 40%).";
                default: return "+" + (10*n) + "% a cooldowns base X/C (penalización máxima 80%).";
            }
        }

        // Comprueba si una carta puede aparecer.
        public static bool Eligible(CardEffect e, RunData d)
        {
            switch (e)
            {
                case CardEffect.UnlockRay: return !d.rayUnlocked;
                case CardEffect.UnlockSpecial: return !d.specialUnlocked;
                case CardEffect.Life: return d.lifeBonus<8;
                case CardEffect.Speed: return d.speedBonus<.799f;
                case CardEffect.Immunity: return d.immunityBonus<1.199f;
                case CardEffect.Cooldowns: return d.cooldownReduction<.599f;
                case CardEffect.BombRadius: return d.bombRadiusBonus<5;
                case CardEffect.BombDamage: return false;
                case CardEffect.BombCapacity: return d.bombCapacityBonus<6;
                case CardEffect.RayRange: return d.rayUnlocked && d.rayRangeBonus<5;
                case CardEffect.RayDamage: return false;
                case CardEffect.SpecialRadius: return d.specialUnlocked && d.specialRadiusBonus<5;
                case CardEffect.EnemyHealth: return false;
                case CardEffect.EnemySpeed: return d.enemySpeedBonus<.799f;
                case CardEffect.EnemyDamage: return false;
                case CardEffect.EnemyAttack: return d.enemyAttackHaste<1.199f;
                case CardEffect.EnemyRadius: return d.enemyRadiusBonus<4;
                case CardEffect.FasterChaos: return d.chaosHaste<1.499f;
                case CardEffect.FasterWalls: return d.wallHaste<1.999f;
                case CardEffect.SlowPlayer: return d.playerSlow<.399f;
                default: return d.cooldownPenalty<.799f;
            }
        }

        // Selecciona las cartas disponibles.
        public static List<CardOffer> Roll(RunData d, bool curse, Rarity rarity =  Rarity.Common)
        {
            var pool =  new List<CardEffect>();
            foreach (CardEffect e in Enum.GetValues(typeof(CardEffect))) if (((int)e >= (int)CardEffect.EnemyHealth) == curse && Eligible(e, d)) pool.Add(e);
            var cards =  new List<CardOffer>();
            // Permite recuperar un poder sin obligar a elegirlo.
            if (!curse && (!d.rayUnlocked || !d.specialUnlocked))
            {
                var e =  !d.rayUnlocked && !d.specialUnlocked ? (UnityEngine.Random.value<.5f?CardEffect.UnlockRay:CardEffect.UnlockSpecial) : (!d.rayUnlocked?CardEffect.UnlockRay:CardEffect.UnlockSpecial);
                cards.Add(new CardOffer(e, RollRarity(), false));
                pool.Remove(e);
            }
            while (cards.Count<3 && pool.Count>0)
            {
                int i = UnityEngine.Random.Range(0, pool.Count);
                cards.Add(new CardOffer(pool[i], curse?rarity:RollRarity(), curse));
                pool.RemoveAt(i);
            }
            return cards;
        }
        static Rarity RollRarity()
        {
            int value = UnityEngine.Random.Range(0, 100);
            return value<55?Rarity.Common:value<83?Rarity.Rare:value<96?Rarity.Epic:Rarity.Mythic;
        }

        // Aplica el efecto de la carta elegida.
        public static void Apply(CardOffer c, RunData d, bool record = true)
        {
            if (Removed(c.effect))return;
            if (record)CardLedger.Ensure(d);
            int n = Rank(c);
            switch (c.effect)
            {
                case CardEffect.UnlockRay:d.rayUnlocked = true;
                d.rayRangeBonus = Mathf.Min(5, d.rayRangeBonus+n-1);
                break;
                case CardEffect.UnlockSpecial:d.specialUnlocked = true;
                d.specialRadiusBonus = Mathf.Min(5, d.specialRadiusBonus+n-1);
                break;
                case CardEffect.Life:d.lifeBonus = Mathf.Min(8, d.lifeBonus+n);
                break;
                case CardEffect.Speed:d.speedBonus = Mathf.Min(.8f, d.speedBonus+.1f*n);
                break;
                case CardEffect.Immunity:d.immunityBonus = Mathf.Min(1.2f, d.immunityBonus+.15f*n);
                break;
                case CardEffect.Cooldowns:d.cooldownReduction = Mathf.Min(.6f, d.cooldownReduction+.08f*n);
                break;
                case CardEffect.BombRadius:d.bombRadiusBonus = Mathf.Min(5, d.bombRadiusBonus+n);
                break;
                case CardEffect.BombDamage:break;
                case CardEffect.BombCapacity:d.bombCapacityBonus = Mathf.Min(6, d.bombCapacityBonus+n);
                break;
                case CardEffect.RayRange:d.rayRangeBonus = Mathf.Min(5, d.rayRangeBonus+n);
                break;
                case CardEffect.RayDamage:break;
                case CardEffect.SpecialRadius:d.specialRadiusBonus = Mathf.Min(5, d.specialRadiusBonus+n);
                break;
                case CardEffect.EnemyHealth:d.enemyHealthBonus = Mathf.Min(10, d.enemyHealthBonus+n);
                break;
                case CardEffect.EnemySpeed:d.enemySpeedBonus = Mathf.Min(.8f, d.enemySpeedBonus+.08f*n);
                break;
                case CardEffect.EnemyDamage:break;
                case CardEffect.EnemyAttack:d.enemyAttackHaste = Mathf.Min(1.2f, d.enemyAttackHaste+.12f*n);
                break;
                case CardEffect.EnemyRadius:d.enemyRadiusBonus = Mathf.Min(4, d.enemyRadiusBonus+n);
                break;
                case CardEffect.FasterChaos:d.chaosHaste = Mathf.Min(1.5f, d.chaosHaste+.15f*n);
                break;
                case CardEffect.FasterWalls:d.wallHaste = Mathf.Min(2, d.wallHaste+.25f*n);
                break;
                case CardEffect.SlowPlayer:d.playerSlow = Mathf.Min(.4f, d.playerSlow+.05f*n);
                break;
                case CardEffect.LongerCooldowns:d.cooldownPenalty = Mathf.Min(.8f, d.cooldownPenalty+.1f*n);
                break;
            }
            if (record)
            {
                d.acquired.Add(new CardOffer(c.effect, c.rarity, c.curse));
                d.history.Add((c.curse?"Maldición: ":"Ventaja: ")+Title(c.effect)+" · "+RarityName(c.rarity));
            }
        }
    }
}
