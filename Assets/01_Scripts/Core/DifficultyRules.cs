using UnityEngine;
namespace Labyrinth
{
    // Los números originales 0/1 conservan su significado. Fácil se añade como 2.
    public static class DifficultyRules
    {
        public const int Hard = 0, VeryHard = 1, Easy = 2;
        public const string Key = "HEXENKNALL.Difficulty.v2", LegacyKey = "HEXENKNALL.Difficulty";
        public const int EasyHealth=5, EasyExpansion=4;
        public const float EasyEnemySpeed=.8f, EasyWindup=.6f, EasyReactionMin=.8f, EasyReactionMax=1.15f;
        public const float EasyImmunity=1.2f, EasyGrace=2, EasyZFuse=1.5f, EasyXCharge=.3f, EasyXCooldown=2.5f, EasyCFuse=1, EasyCCooldown=5;
        public const float EasyAimScale=.3f, EasyBossWarning=1.15f, EasyBossRecovery=1.5f, EasyBossDashSpeed=7;
        public const int EasyBossRange=4;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static int? TestSelection;
#endif
        public static int Resolve(bool original, int value, bool oldBalance, int balance) =>
            original && (value == Hard || value == VeryHard) ? value : Easy;
        public static int Selected
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (TestSelection.HasValue)return TestSelection.Value;
#endif
                if (!PlayerPrefs.HasKey(Key))
                {
                    bool legacy = PlayerPrefs.HasKey(LegacyKey);
                    int value = PlayerPrefs.GetInt(LegacyKey, -1);
                    int migrated = Resolve(legacy, value, PlayerPrefs.HasKey("Hexenknall.Balance"), PlayerPrefs.GetInt("Hexenknall.Balance", -1));
                    PlayerPrefs.SetInt(Key, migrated);
                    PlayerPrefs.SetString("HEXENKNALL.DifficultyMigration", legacy && value >= 0 && value <= 1 ? "v2: original 0/1 conservado; balance y giro de la entrega anterior retirados" : "v2: instalación nueva o perfil anterior sin orientación válida -> Fácil");
                    PlayerPrefs.Save();
                }
                return Mathf.Clamp(PlayerPrefs.GetInt(Key, Easy), 0, 2);
            }
        }
        public static int Active => GameDirector.I && GameDirector.I.stage >= 0 ? GameDirector.I.difficulty : Selected;
        public static bool IsEasy => Active == Easy;
        public static string Name(int value) => value == Easy ? "FÁCIL" : value == Hard ? "DIFÍCIL" : "MUY DIFÍCIL";
        public static bool RelativeControls => Active != VeryHard;
        public static bool RotateCamera => !IsEasy;
        public static float AimScale => IsEasy ? EasyAimScale : 1;
        public static bool RayAvailable(RunData d, int stage, bool easy) => stage == 0 || d.rayUnlocked || (easy && stage >= 1);
        public static bool SpecialAvailable(RunData d, int stage, bool easy) => stage == 0 || d.specialUnlocked || (easy && (stage >= 3 || stage == Campaign.OldFriend));
        public static bool IgnoreOwnBomb(bool targetPlayer, bool bomb, bool playerOwned) => IsEasy && targetPlayer && bomb && playerOwned;
        public static void Select(int value) { PlayerPrefs.SetInt(Key, Mathf.Clamp(value,0,2)); PlayerPrefs.Save(); }
    }
}
