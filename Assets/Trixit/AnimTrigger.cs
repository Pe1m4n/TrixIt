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
            _animator.SetTrigger(_triggerName);
        }
    }
}