using UnityEngine;

namespace Trixit
{
    public class ShootingController : MonoBehaviour
    {
        [SerializeField] private Transform _shootingPivot;
        [SerializeField] private Camera _camera;
        [SerializeField] private Projectile _jumpProjectile;
        [SerializeField] private Projectile _concreteProjectile;
        [SerializeField] private Animator _animator;

        public bool WasBounced;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Time.timeScale = 0.2f;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Time.timeScale = 1f;
            }

            if (DebugAnimUI.Instance != null)
            {
                DebugAnimUI.Instance.SetInfo(_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            }
            
            if (_shootingPivot.transform.position.y < -20f)
            {
                GlobalController.Restart();
                return;
            }
            
            if (GlobalController.MenuOpened || !Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKeyDown(KeyCode.Mouse1))
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
            var projectile = Instantiate(turnInto == BoxType.Jumpy? _jumpProjectile :_concreteProjectile, _shootingPivot.position, Quaternion.LookRotation(direction));
            projectile.Init(turnInto, StateHolder.Instance.PlayerState);
        }
    }
}