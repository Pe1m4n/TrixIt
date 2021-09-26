using RootMotion;
using UnityEngine.SceneManagement;

namespace Trixit
{
    public static class GlobalController
    {
        public static int TotalScore;
        public static int CurrentLevelScore;
        public static int CurrentLevel = -1;
        private static bool _menuOpened;

        public static bool MenuOpened
        {
            get => _menuOpened;
            set
            {
                _menuOpened = value;
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.lockCursor = !_menuOpened;
                    CameraController.Instance.preventRotation = _menuOpened;
                }
            }
        }
        
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
            Clear();
            CurrentLevel = index;
            SceneManager.LoadScene(_levels[index]);
            AudioPlayer.Instance.StopLocal();
        }

        private static void Clear()
        {
            CurrentLevelScore = 0;
            MenuOpened = false;
        }
        
        public static void Restart()
        {
            TotalScore -= CurrentLevelScore;
            if (CurrentLevel < 0)
            {
                Clear();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }   
            else
                PlayLevel(CurrentLevel);
        }
    }
}