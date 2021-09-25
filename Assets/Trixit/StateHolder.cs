using System;
using UnityEngine;

namespace Trixit
{
    public class StateHolder : MonoBehaviour
    {
        public static StateHolder Instance;

        public readonly PlayerState PlayerState = new PlayerState();
        [SerializeField] private int _jumpAmmo;
        [SerializeField] private int _hardAmmo;
        
        private void Awake()
        {
            Instance = this;
            PlayerState.Ammo[BoxType.Jumpy] = _jumpAmmo;
            PlayerState.Ammo[BoxType.Concrete] = _hardAmmo;
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}