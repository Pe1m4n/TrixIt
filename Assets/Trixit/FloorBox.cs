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
        
        public void SetType(BoxType boxType)
        {
            BoxType = boxType;

            switch (boxType)
            {
                case BoxType.Simple:
                    break;
                case BoxType.Jumpy:
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
                var dir =Vector3.Lerp(other.collider.transform.forward.normalized, other.collider.transform.up.normalized, AnglesFromForward).normalized;
                var rb = other.collider.gameObject.GetComponent<Rigidbody>();
                rb.AddForce(dir * JumpForce, ForceMode);
            }
        }

        public void TryScheduleDestroy()
        {
            if (Immutable || BoxType == BoxType.Concrete)
                return;
            
            _scheduledDestroy = true;
            _timeUntilDestroy = DestroyTime;
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
        Jumpy
    }
}