using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class AnimTrigger : MonoBehaviour
    {
        [SerializeField] private string _triggerName;
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioClip _soundGlobal;
        [SerializeField] private AudioClip _soundLocal;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Character"))
            {
                return;
            }

            if (_soundGlobal != null)
            {
                AudioPlayer.Instance.StopLocal();
                AudioPlayer.Instance.PlaySound(_soundGlobal, true);
            }

            if (_soundLocal != null)
                AudioPlayer.Instance.PlaySound(_soundLocal);
            
            if (_animator != null)
                _animator.SetTrigger(_triggerName);
        }
    }
}