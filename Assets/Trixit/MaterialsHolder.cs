using UnityEngine;

namespace Trixit
{
    public class MaterialsHolder : MonoBehaviour
    {
        public static MaterialsHolder Instance;

        public Material JumpMaterial;
        public Material ConcreteMaterial;

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