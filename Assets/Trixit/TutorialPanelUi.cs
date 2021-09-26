using UnityEngine;

namespace Trixit
{
    public class TutorialPanelUi : MonoBehaviour
    {
        private void Start()
        {
            GlobalController.MenuOpened = true;
        }
        
        public void Close()
        {
            GlobalController.MenuOpened = false;
            gameObject.SetActive(false);
        }
    }
}