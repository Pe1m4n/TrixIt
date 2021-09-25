using System;
using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class FloorBox : MonoBehaviour
    {
        public BoxType BoxType;
        public float JumpForce = 300f;
        public ForceMode ForceMode;
        
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

            if (BoxType == BoxType.Simple)
            {
                return;
            }

            var dir = (other.transform.up.normalized + other.transform.forward.normalized).normalized;
            var rb = other.collider.gameObject.GetComponent<Rigidbody>();
            rb.AddForce(dir * JumpForce, ForceMode);
        }
    }

    public enum BoxType
    {
        Simple,
        Jumpy
    }
}