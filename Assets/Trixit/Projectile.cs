using UnityEngine;

namespace Trixit
{
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        private BoxType _turnInto;
        private PlayerState _playerState;
        [SerializeField] private float _movementSpeed;
        
        public void Init(BoxType turnInto, PlayerState playerState)
        {
            _turnInto = turnInto;
            _playerState = playerState;
        }

        private void Update()
        {
            transform.position += transform.forward * Time.deltaTime * _movementSpeed;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.TryGetComponent<FloorBox>(out var box) || box.BoxType == _turnInto || box.Immutable)
            {
                Destroy(gameObject);
                return;
            }

            _playerState.Ammo[_turnInto]--;
            box.SetType(_turnInto);
            Destroy(gameObject);
        }
    }
}