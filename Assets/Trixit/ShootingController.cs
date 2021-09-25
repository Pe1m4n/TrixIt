using UnityEngine;

namespace Trixit
{
    public class ShootingController : MonoBehaviour
    {
        [SerializeField] private Transform _shootingPivot;
        [SerializeField] private Camera _camera;
        [SerializeField] private Projectile _projectilePrefab;

        private BoxType _turnInto;
        
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Mouse0))
            {
                return;
            }
            
            // if (StateHolder.Instance.PlayerState.Ammo[_turnInto] <= 0)
            // {
            //     return;
            // }
            
            var ray = _camera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
            if (!Physics.Raycast(ray, out var hit))
            {
                return;
            }
            
            ShootAt(hit.point);
        }

        private void ShootAt(Vector3 position)
        {
            var direction = (position - _shootingPivot.position).normalized;
            var projectile = Instantiate(_projectilePrefab, _shootingPivot.position, Quaternion.LookRotation(direction));
            projectile.Init(_turnInto, StateHolder.Instance.PlayerState);
        }
    }
}