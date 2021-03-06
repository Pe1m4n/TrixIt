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
        [SerializeField] private GameObject _crosshair;

        private void Update()
        {
            if (StateHolder.Instance == null || StateHolder.Instance.PlayerState.Ammo.All(p => p.Value <= 0))
            {
                _root.SetActive(false);
                _crosshair.SetActive(false);
                return;
            }
            _root.SetActive(true);
            var playerState = StateHolder.Instance.PlayerState;

            _greenText.text = $"{playerState.Ammo[BoxType.Jumpy]}";
            _blueText.text = $"{playerState.Ammo[BoxType.Concrete]}";
        }
    }
}