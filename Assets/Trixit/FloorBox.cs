using System;
using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class FloorBox : MonoBehaviour
    {
        public BoxType BoxType;
        public bool Immutable;
        public float AnglesFromForward = 30f;
        public float JumpForce = 11f;
        public ForceMode ForceMode = ForceMode.Acceleration;
        public float DestroyTime = 2f;

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

            if (BoxType == BoxType.Jumpy)
            {
                var dir = Vector3.Lerp(other.collider.transform.forward.normalized, other.collider.transform.up.normalized, AnglesFromForward).normalized;
                var rb = other.collider.gameObject.GetComponent<Rigidbody>();
                rb.AddForce(dir * JumpForce, ForceMode);
                TryScheduleDestroy();
            }

            if (BoxType == BoxType.Finish)
            {
                GlobalController.PlayLevel(++GlobalController.CurrentLevel);
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