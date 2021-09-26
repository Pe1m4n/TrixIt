using UnityEngine;
using UnityEngine.UI;

namespace Trixit
{
    public class DebugAnimUI : MonoBehaviour
    {
        public static DebugAnimUI Instance;
        [SerializeField] private Text _animNameText;

        private void Awake()
        {
            Instance = this;
        }

        public void SetInfo(string name)
        {
            _animNameText.text = name;
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}