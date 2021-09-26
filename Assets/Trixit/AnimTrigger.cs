using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class AnimTrigger : MonoBehaviour
    {
        [SerializeField] private string _triggerName;
        [SerializeField] private Animator _animator;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Character"))
            {
                return;
            }
            
            _animator.SetTrigger(_triggerName);
        }
    }
}