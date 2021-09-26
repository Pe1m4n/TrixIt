using UnityEngine.SceneManagement;

namespace Trixit
{
    public static class GlobalController
    {
        public static int TotalScore;
        public static int CurrentLevelScore;
        public static int CurrentLevel = -1;
        
        private static readonly string[] _levels = new[]
        {
            "Tutorial_1",
            "Tutorial_2",
            "Tutorial_3",
            "Tutorial_4",
            "Tutorial_5",
            "Level_1",
            "Level_2",
            "Level_3",
            "Level_4",
            "Level_5",
            "Level_6",
            "Level_7",
            "Level_8",
            "Level_9",
        };

        public static void Start()
        {
            PlayLevel(0);
        }

        public static void PlayLevel(int index)
        {
            CurrentLevel = index;
            CurrentLevelScore = 0;
            SceneManager.LoadScene(_levels[index]);
        }
        
        public static void Restart()
        {
            TotalScore -= CurrentLevelScore;
            PlayLevel(CurrentLevel);
        }
    }
}