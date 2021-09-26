using System;
using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour
    {
        public bool Finish;
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Character"))
            {
                return;
            }

            if (Finish)
            {
                return;
            }
            else
            {
                GlobalController.CurrentLevelScore++;
                GlobalController.TotalScore++;
            }
            Destroy(gameObject);
        }
    }
}