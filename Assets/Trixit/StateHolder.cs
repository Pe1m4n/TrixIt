using System;
using UnityEngine;

namespace Trixit
{
    public class StateHolder : MonoBehaviour
    {
        public static StateHolder Instance;

        public readonly PlayerState PlayerState = new PlayerState();
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}