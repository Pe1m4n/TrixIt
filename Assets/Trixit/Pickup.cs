using System;
using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour
    {
        public bool Finish;
        [SerializeField] private GameObject _vfx;
        [SerializeField] private AudioClip _sound1;
        [SerializeField] private AudioClip _sound2;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Character"))
            {
                return;
            }

            if (Finish)
            {
                return;
            }
            else
            {
                GlobalController.CurrentLevelScore++;
                GlobalController.TotalScore++;
            }

            if (_vfx != null)
            {
                Instantiate(_vfx, transform.position, transform.rotation);
            }

            if (_sound1 != null)
            {
                AudioPlayer.Instance.PlaySound(_sound1);
            }
            if (_sound2 != null)
            {
                AudioPlayer.Instance.PlaySound(_sound2);
            }
            Destroy(gameObject);
        }
    }
}