using UnityEngine;
namespace Labyrinth
{
    // La demo vive en memoria: nunca carga ni escribe claves de campaña.
    public static class FeriaSession
    {
        public static bool Active { get; private set; }
        public static RunData Data { get; private set; }
        public static int Lesson { get; private set; }
        public static bool Won { get; set; }
        public static bool Choosing { get; set; }
        public static bool Accessible => Active || (GameDirector.I && GameDirector.I.stage >= 0 ? GameDirector.I.accessibleProfile : PlayerPrefs.GetInt("Hexenknall.Balance", 0) == 1);
        public static bool RotateCamera => !Active && PlayerPrefs.GetInt("Hexenknall.RotateCamera", 1) == 1;
        public static string Objective => Lesson == 0 ? "Coloca Z y sal de la cruz antes de la explosión." :
            Lesson == 1 ? "Orienta al personaje y coloca un sello X. Puedes moverte mientras carga." :
            GameDirector.I.stage == 1 ? "Derrota a los dos guardianes. Después elige una carta gratuita." :
            GameDirector.I.stage == 2 ? "Vence a los guardianes y a la oleada. Atención a los muros marcados." :
            "C: apunta con flechas, confirma con Enter o cancela con Esc. Vence al jefe.";
        public static void Begin()
        {
            Active = true;
            Data = RunProgress.NewAdventureData();
            Data.rayUnlocked = false;
            Data.specialUnlocked = false;
            Lesson = 0;
            Won = Choosing = false;
        }
        public static void End() { Active = false; Data = null; Won = Choosing = false; }
        public static void UsedZ() { if (Active && Lesson == 0)Lesson = 1; }
        public static void UsedX() { if (Active && Lesson == 1)Lesson = 2; }
        public static void UsedC() { if (Active && Lesson == 2)Lesson = 3; }
        public static void Choose(int index)
        {
            if (!Active || !Choosing || index < 0 || index > 2)return;
            var effects = new[] { CardEffect.Life, CardEffect.Cooldowns, CardEffect.Immunity };
            CardRules.Apply(new CardOffer(effects[index], Rarity.Rare, false), Data);
            Data.shieldChosen = index == 2;
            Choosing = false;
            GameDirector.I.sound.Play(SoundCue.CardChoose);
            GameDirector.I.Load("Level02");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { End(); }
    }
}
