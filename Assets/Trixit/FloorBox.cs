using System;
using UniRx;
using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class FloorBox : MonoBehaviour
    {
        public BoxType BoxType;
        public bool Immutable;
        public float AnglesFromForward = 30f;
        public float JumpForce = 12.4f;
        public float JumpSecond = 11f;
        public ForceMode ForceMode = ForceMode.Acceleration;
        public float DestroyTime = 2f;
        [SerializeField] private AudioClip _bounceSound;
        [SerializeField] private AudioClip _landingHeavy;
        [SerializeField] private AudioClip _landing;
        [SerializeField] private AudioClip _finishSound;

        private bool _scheduledDestroy;
        private float _timeUntilDestroy;
        private Renderer _renderer;
        private Animator _animator;
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _animator = GetComponent<Animator>();
        }

        public void SetType(BoxType boxType)
        {
            BoxType = boxType;

            switch (boxType)
            {
                case BoxType.Concrete:
                    _renderer.material =  MaterialsHolder.Instance.ConcreteMaterial;
                    break;
                case BoxType.Jumpy:
                    _renderer.material = MaterialsHolder.Instance.JumpMaterial;
                    break;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.collider.gameObject.layer != LayerMask.NameToLayer("Character"))
            {
                return;
            }

            var controller = other.collider.GetComponent<TrixCharacterController>();
            if (BoxType == BoxType.Jumpy)
            {
                var dir = Vector3.Lerp(other.collider.transform.forward.normalized, other.collider.transform.up.normalized, AnglesFromForward).normalized;
                var rb = other.collider.gameObject.GetComponent<Rigidbody>();
                rb.AddForce(dir * (controller.WasBounced? JumpSecond : JumpForce), ForceMode);
                TryScheduleDestroy();
                controller.WasBounced = true;
                AudioPlayer.Instance.PlaySound(_bounceSound);
                return;
            }

            var clip = controller.WasBounced ? _landingHeavy : _landing;
            AudioPlayer.Instance.PlaySound(clip);
            controller.WasBounced = false;
            
            if (BoxType == BoxType.Finish)
            {
                AudioPlayer.Instance.PlaySound(_finishSound, true);
                Observable.Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(u => GlobalController.PlayLevel(++GlobalController.CurrentLevel));
                if (_animator != null)
                {
                    _animator.SetTrigger("ReadyToWatch");
                }
            }
        }

        public void TryScheduleDestroy()
        {
            if (Immutable || BoxType == BoxType.Concrete)
                return;
            
            _scheduledDestroy = true;
            _timeUntilDestroy = DestroyTime;
            if (_animator != null)
                _animator.SetTrigger("Destroy");
        }

        private void Update()
        {
            if (!_scheduledDestroy)
                return;

            _timeUntilDestroy -= Time.deltaTime;
            
            if (_timeUntilDestroy < 0f)
                Destroy(gameObject);
        }
    }

    public enum BoxType
    {
        Concrete,
        Simple,
        Jumpy,
        Finish
    }
}