using UnityEngine;

namespace Trixit
{
    public class ShootingController : MonoBehaviour
    {
        [SerializeField] private Transform _shootingPivot;
        [SerializeField] private Camera _camera;
        [SerializeField] private Projectile _projectilePrefab;
        
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKeyDown(KeyCode.Mouse1))
            {
                return;
            }

            var turnInto = Input.GetKeyDown(KeyCode.Mouse0)? BoxType.Jumpy : BoxType.Concrete;
            if (StateHolder.Instance.PlayerState.Ammo[turnInto] <= 0)
            {
                return;
            }
            
            var ray = _camera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
            if (!Physics.Raycast(ray, out var hit))
            {
                return;
            }
            
            ShootAt(hit.point, turnInto);
        }

        private void ShootAt(Vector3 position, BoxType turnInto)
        {
            var direction = (position - _shootingPivot.position).normalized;
            var projectile = Instantiate(_projectilePrefab, _shootingPivot.position, Quaternion.LookRotation(direction));
            projectile.Init(turnInto, StateHolder.Instance.PlayerState);
        }
    }
}