using System;
using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class FloorBox : MonoBehaviour
    {
        public BoxType BoxType;
        public float JumpForce = 300f;
        
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

            
            var rb = other.collider.gameObject.GetComponent<Rigidbody>();
            rb.AddForce(transform.up * JumpForce);
        }
    }

    public enum BoxType
    {
        Simple,
        Jumpy
    }
}