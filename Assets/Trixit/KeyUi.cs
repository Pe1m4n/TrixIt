using UnityEngine;
using UnityEngine.UI;

namespace Trixit
{
    public class KeyUi : MonoBehaviour
    {
        [SerializeField] private Text _counter;

        private void Update()
        {
            _counter.text = $"{GlobalController.CurrentLevelScore}";
        }
    }
}