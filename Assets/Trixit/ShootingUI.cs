using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Trixit
{
    public class ShootingUI : MonoBehaviour
    {
        [SerializeField] private Text _greenText;
        [SerializeField] private Text _blueText;
        [SerializeField] private GameObject _root;

        private void Update()
        {
            if (StateHolder.Instance == null || StateHolder.Instance.PlayerState.Ammo.All(p => p.Value <= 0))
            {
                _root.SetActive(false);
                return;
            }

            var playerState = StateHolder.Instance.PlayerState;

            _greenText.text = $"{playerState.Ammo[BoxType.Jumpy]}";
            _blueText.text = $"{playerState.Ammo[BoxType.Concrete]}";
        }
    }
}