using System;
using System.Collections.Generic;
using UnityEngine;
namespace Labyrinth
{
    [Serializable]
    // Guarda la partida y el progreso de la campaña.
    public class RunData
    {
        public int version = 2, stage;
        public int ledgerRevision;
        public bool oldFriendResolved, oldFriendVisited;
        public List<CardOffer> acquired = new List<CardOffer>();
        public List<int> oldFriendOrder = new List<int>();
        public List<string> oldFriendResult = new List<string>();
        public int balanceRevision, checkpointRevision;
        public int checkpointStage;
        public bool adventureStarted, finalBossDefeated;
        public bool rayUnlocked, specialUnlocked;
        public int lifeBonus, bombRadiusBonus, bombDamageBonus, bombCapacityBonus;
        public int rayRangeBonus, rayDamageBonus, specialRadiusBonus;
        public float speedBonus, immunityBonus, cooldownReduction;
        public int enemyHealthBonus, enemyDamageBonus, enemyRadiusBonus;
        public float enemySpeedBonus, enemyAttackHaste, chaosHaste, wallHaste, playerSlow, cooldownPenalty;
        public int rewardStage, rewardPhase, chosenRarity;
        public List<CardOffer> offers = new List<CardOffer>();
        public List<string> history = new List<string>();
    }
    public static class RunProgress
    {
        const string Key = "Labyrinth.Run.v2", BackupKey = Key+".backup";
        static RunData data;
        public static RunData Data
        {
            get
            {
                if (data == null)
                {
                    LegacySaveMigration.TryCopy(Key);
                    string primary = PlayerPrefs.GetString(Key, "");
                    if (!TryRead(primary, out data) && !TryRead(PlayerPrefs.GetString(BackupKey, ""), out data))data = new RunData();
                    int before = data.stage;
                    Normalize(data);
                    if (before == Campaign.Ending && data.stage != Campaign.Ending) Debug.LogWarning("Guardado recuperado: el epilogo no tenia una victoria registrada. Continuar abrira "+Campaign.Scene(ResumeStage(data))+".");
                    if (PlayerPrefs.HasKey(Key) || PlayerPrefs.HasKey(BackupKey))Write(data);
                }
                return data;
            }
        }

        // Carga y valida el progreso guardado.
        static bool TryRead(string json, out RunData result)
        {
            result = null;
            if (string.IsNullOrEmpty(json))return false;
            try
            {
                result = JsonUtility.FromJson<RunData>(json);
                return result != null && result.version == 2;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // Valida los datos sin modificar el guardado del jugador.
        // Corrige datos antiguos o incompletos.
        public static void Normalize(RunData d)
        {
            if (d.offers == null)d.offers = new List<CardOffer>();
            if (d.history == null)d.history = new List<string>();
            int completed = 0;
            foreach (string line in d.history)if(line != null && line.StartsWith("Maldición: "))completed++;
            completed = Mathf.Clamp(completed, 0, Campaign.LastNormal);
            if (d.balanceRevision<3)RemoveDamageModifiers(d);
            if (d.checkpointRevision<4)
            {
                // Las partidas antiguas registran una maldición por nivel completado.
                d.adventureStarted = d.stage>0 || completed>0 || d.rewardPhase>0;
                d.finalBossDefeated = d.stage == Campaign.Ending && completed == Campaign.LastNormal;
                d.checkpointStage = d.stage>=0 && d.stage<=Campaign.FinalBoss?d.stage: d.finalBossDefeated?Campaign.Ending:d.adventureStarted?Mathf.Clamp(completed+1, 1, Campaign.FinalBoss):0;
                d.checkpointRevision = 4;
            }
            if (d.checkpointStage<0 || (d.checkpointStage>Campaign.Ending && d.checkpointStage != Campaign.OldFriend) || (d.checkpointStage == Campaign.Ending && !d.finalBossDefeated)) d.checkpointStage = d.adventureStarted?Mathf.Clamp(completed+1, 1, Campaign.FinalBoss):0;
            if (d.finalBossDefeated)d.checkpointStage = Campaign.Ending;
            bool pending = d.rewardPhase == 1 || d.rewardPhase == 2;
            if (!pending || d.rewardStage<1 || d.rewardStage>Campaign.LastNormal || d.finalBossDefeated)
            {
                d.rewardPhase = 0;
                d.rewardStage = 0;
                d.offers.Clear();
            }
            else
            {
                d.checkpointStage = d.rewardStage;
                d.adventureStarted = true;
                d.chosenRarity = Mathf.Clamp(d.chosenRarity, 0, 3);
                bool valid = d.offers.Count == 3;
                var used = new HashSet<CardEffect>();
                foreach (var card in d.offers) if(card == null || CardRules.Removed(card.effect) || !used.Add(card.effect) || card.curse != (d.rewardPhase == 2) || (int)card.rarity<0 || (int)card.rarity>3 || (d.rewardPhase == 2 && (int)card.rarity != d.chosenRarity))valid = false;
                if (!valid)d.offers = CardRules.Roll(d, d.rewardPhase == 2, (Rarity)d.chosenRarity);
            }
            CardLedger.Ensure(d);
            d.stage = d.checkpointStage;
        }
        public static int ResumeStage(RunData d)
        {
            return d.rewardPhase>0?d.rewardStage:d.checkpointStage;
        }
        public static void RemoveDamageModifiers(RunData d)
        {
            d.bombDamageBonus = d.rayDamageBonus = d.enemyDamageBonus = 0;
            if (d.offers == null)d.offers = new List<CardOffer>();
            if (d.history == null)d.history = new List<string>();
            var used = new HashSet<CardEffect>();
            foreach (var c in d.offers)if(c != null && !CardRules.Removed(c.effect))used.Add(c.effect);
            foreach (var c in d.offers)
            {
                if (c == null || !CardRules.Removed(c.effect))continue;
                foreach (CardEffect e in Enum.GetValues(typeof(CardEffect)))
                {
                    bool curse = (int)e>=(int)CardEffect.EnemyHealth;
                    if (curse != c.curse || used.Contains(e) || !CardRules.Eligible(e, d))continue;
                    c.effect = e;
                    used.Add(e);
                    break;
                }
            }
            d.history.RemoveAll(line=>line != null && (line.StartsWith("Ventaja: Núcleo ardiente ·") || line.StartsWith("Ventaja: Rayo devastador ·") || line.StartsWith("Maldición: Furia del enemigo ·")));
            d.balanceRevision = 3;
        }
        public static bool HasSave
        {
            get
            {
                LegacySaveMigration.TryCopy(Key);
                return PlayerPrefs.HasKey(Key) || PlayerPrefs.HasKey(BackupKey);
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            data = null;
        }
        static void Write(RunData value)
        {
            string previous = PlayerPrefs.GetString(Key, "");
            if (TryRead(previous, out _))PlayerPrefs.SetString(BackupKey, previous);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(value));
            PlayerPrefs.Save();
        }

        // Guarda el estado actual de la partida.
        public static void Save()
        {
            var d = Data;
            Normalize(d);
            Write(d);
        }

        // Reinicia los datos de una partida nueva.
        public static void NewGame()
        {
            data = new RunData
            {
                checkpointRevision = 4, balanceRevision = 3
            };
            Save();
        }
        public static RunData NewAdventureData()=>new RunData
        {
            stage = 1, checkpointStage = 1, checkpointRevision = 4, balanceRevision = 3, adventureStarted = true
        };

        // Prepara una nueva aventura.
        public static void BeginAdventure()
        {
            data = NewAdventureData();
            Save();
        }
        // Entrar a una escena no debe adelantar el progreso.
        // Registra la entrada a un nivel.
        public static void Enter(int stage)
        {
            if (HasSave && stage == ResumeStage(Data))Save();
        }

        // Guarda las recompensas del nivel.
        public static void PrepareReward(int clearedStage)
        {
            if (clearedStage<1 || clearedStage>Campaign.LastNormal)return;
            var d = Data;
            if (d.rewardPhase != 0)return;
            d.stage = d.checkpointStage = clearedStage;
            d.adventureStarted = true;
            d.rewardStage = clearedStage;
            d.rewardPhase = 1;
            d.offers = CardRules.Roll(d, false);
            Save();
        }
        public static void CompleteReward()
        {
            var d = Data;
            if (AdvanceAfterReward(d))Save();
        }
        public static bool AdvanceAfterReward(RunData d)
        {
            if (d.rewardPhase != 2 || d.rewardStage<1 || d.rewardStage>Campaign.LastNormal)return false;
            d.stage = d.checkpointStage = d.rewardStage == 4 && !d.oldFriendVisited?Campaign.OldFriend:d.rewardStage+1;
            d.rewardPhase = 0;
            d.rewardStage = 0;
            d.offers.Clear();
            return true;
        }

        // Marca la campaña como terminada.
        public static void CompleteCampaign()
        {
            var d = Data;
            d.finalBossDefeated = true;
            d.adventureStarted = true;
            d.stage = d.checkpointStage = Campaign.Ending;
            d.rewardPhase = 0;
            Save();
        }
    }
}
