using System.Collections.Generic;
namespace Labyrinth
{
    // Define los niveles y el orden de la campaña.
    public static class Campaign
    {
        // Mantiene los identificadores de nivel de partidas anteriores.
        public const int LastNormal = 9, FinalBoss = 10, Ending = 11, OldFriend = 12;

        // Devuelve la escena de una etapa.
        public static string Scene(int stage)
        {
            if (stage<0)return "MainMenu";
            if (stage == 0)return "PrologueBoss";
            if (stage == OldFriend)return "OldFriendRoom";
            if (stage == FinalBoss)return "FinalBoss";
            if (stage>=Ending)return "Ending";
            return "Level"+stage.ToString("00");
        }

        // Busca la etapa que corresponde a una escena.
        public static bool TryStage(string scene, out int stage)
        {
            for (int value = -1;value<=OldFriend;value++)if(Scene(value) == scene)
            {
                stage = value;
                return true;
            }
            stage = -1;
            return false;
        }

        // Comprueba si la etapa contiene un combate.
        public static bool IsCombat(int stage)=>stage>=0 && stage<=FinalBoss;
        public static string[] AllScenes()
        {
            var scenes = new List<string>
            {
                "MainMenu", "PrologueBoss"
            };
            for (int i = 1;i<=LastNormal;i++)
            {
                scenes.Add(Scene(i));
                if (i == 4)scenes.Add(Scene(OldFriend));
            }
            scenes.Add(Scene(FinalBoss));
            scenes.Add(Scene(Ending));
            return scenes.ToArray();
        }
    }
}
