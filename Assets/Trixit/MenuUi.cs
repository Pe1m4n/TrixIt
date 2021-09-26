using UnityEngine;

namespace Trixit
{
    public class MenuUi : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        
        public void Restart()
        {
            GlobalController.Restart();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _root.SetActive(!_root.activeInHierarchy);
                GlobalController.MenuOpened = _root.activeInHierarchy;
            }
        }
    }
}