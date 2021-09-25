using System;
using UnityEngine;

namespace Trixit
{
    public class EntryPoint : MonoBehaviour
    {
        private void Awake()
        {
            GlobalController.Start();
        }
    }
}