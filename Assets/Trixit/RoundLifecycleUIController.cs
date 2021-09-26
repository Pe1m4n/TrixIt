using System;
using UnityEngine;
using UnityEngine.UI;

namespace Trixit
{
    public class RoundLifecycleUIController : MonoBehaviour
    {
        public static RoundLifecycleUIController Instance;
        
        [SerializeField] private GameObject _finishLevelObjet;
        [SerializeField] private GameObject _loseObject;
        [SerializeField] private Text _loseCountTotal;
        [SerializeField] private Text _loseCountCurrent;
        [SerializeField] private Text _finishTotal;
        [SerializeField] private Text _finishCurrent;

        public void ShowFinishLevel()
        {
            _finishTotal.text = $"{StateHolder.Instance.KeysThisRound}";
            _finishCurrent.text = $"{GlobalController.CurrentLevelScore}";
            _finishLevelObjet.SetActive(true);
            GlobalController.MenuOpened = true;
        }
        
        public void ShowLose()
        {
            _loseCountTotal.text = $"{FloorBox.SCORE_FOR_PRIZE}";
            _loseCountCurrent.text = $"{GlobalController.TotalScore}";
            _loseObject.SetActive(true);
            GlobalController.MenuOpened = true;
        }

        public void RestartGame()
        {
            GlobalController.Start();
        }

        public void Exit()
        {
            Application.Quit();
        }

        public void NextLevel()
        {
            GlobalController.PlayLevel(++GlobalController.CurrentLevel);
        }
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}