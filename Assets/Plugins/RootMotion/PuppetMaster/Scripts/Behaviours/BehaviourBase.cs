using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace RootMotion.Dynamics
{

    /// <summary>
    /// The base abstract class for all Puppet Behaviours.
    /// </summary>
    public abstract class BehaviourBase : MonoBehaviour
    {

        /// <summary>
        /// Gets the PuppetMaster associated with this behaviour. Returns null while the behaviour is not initiated by the PuppetMaster.
        /// </summary>
        [HideInInspector] public PuppetMaster puppetMaster;

        public delegate void BehaviourDelegate();
        public delegate void BehaviourUpdateDelegate(float deltaTime);
        public delegate void HitDelegate(MuscleHit hit);
        public delegate void CollisionDelegate(MuscleCollision collision);

        public abstract void OnReactivate();

        public BehaviourDelegate OnPreActivate;
        public BehaviourDelegate OnPreInitiate;
        public BehaviourUpdateDelegate OnPreFixedUpdate;
        public BehaviourUpdateDelegate OnPreUpdate;
        public BehaviourUpdateDelegate OnPreLateUpdate;
        public BehaviourUpdateDelegate OnPreRead;
        public BehaviourUpdateDelegate OnPreWrite;
        public BehaviourDelegate OnPreDeactivate;
        public BehaviourDelegate OnPreFixTransforms;
        public HitDelegate OnPreMuscleHit;
        public CollisionDelegate OnPreMuscleCollision;
        public CollisionDelegate OnPreMuscleCollisionExit;
        public BehaviourDelegate OnHierarchyChanged;

        public virtual void Resurrect() { }
        public virtual void Freeze() { }
        public virtual void Unfreeze() { }
        public virtual void KillStart() { }
        public virtual void KillEnd() { }
        public virtual void OnTeleport(Quaternion deltaRotation, Vector3 deltaPosition, Vector3 pivot, bool moveToTarget) { }
        public virtual void OnMuscleDisconnected(Muscle m) { }
        public virtual void OnMuscleReconnected(Muscle m) { }

        public virtual void OnMuscleAdded(Muscle m)
        {
#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnHierarchyChanged != null) OnHierarchyChanged();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public virtual void OnMuscleRemoved(Muscle m)
        {
#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnHierarchyChanged != null) OnHierarchyChanged();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnInitiate() { }
        protected virtual void OnFixedUpdate(float deltaTime) { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnLateUpdate(float deltaTime) { }
        protected virtual void OnReadBehaviour(float deltaTime) { }
        protected virtual void OnWriteBehaviour(float deltaTime) { }
        protected virtual void OnDrawGizmosBehaviour() { }
        protected virtual void OnFixTransformsBehaviour() { }
        protected virtual void OnMuscleHitBehaviour(MuscleHit hit) { }
        protected virtual void OnMuscleCollisionBehaviour(MuscleCollision collision) { }
        protected virtual void OnMuscleCollisionExitBehaviour(MuscleCollision collision) { }
        
        public BehaviourDelegate OnPostActivate;
        public BehaviourDelegate OnPostInitiate;
        public BehaviourUpdateDelegate OnPostFixedUpdate;
        public BehaviourUpdateDelegate OnPostUpdate;
        public BehaviourUpdateDelegate OnPostLateUpdate;
        public BehaviourUpdateDelegate OnPostRead;
        public BehaviourUpdateDelegate OnPostWrite;
        public BehaviourDelegate OnPostDeactivate;
        public BehaviourDelegate OnPostDrawGizmos;
        public BehaviourDelegate OnPostFixTransforms;
        public HitDelegate OnPostMuscleHit;
        public CollisionDelegate OnPostMuscleCollision;
        public CollisionDelegate OnPostMuscleCollisionExit;

        [HideInInspector] public bool deactivated;
#pragma warning disable IDE1006 // ����� ����������
        public bool forceActive { get; protected set; }
#pragma warning restore IDE1006 // ����� ����������

        private bool initiated = false;

        public void Initiate()
        {
            initiated = true;

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreInitiate != null) OnPreInitiate();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnInitiate();

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostInitiate != null) OnPostInitiate();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void OnFixTransforms()
        {
            if (!initiated) return;
            if (!enabled) return;

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreFixTransforms != null) OnPreFixTransforms();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnFixTransformsBehaviour();

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostFixTransforms != null) OnPostFixTransforms();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void OnRead(float deltaTime)
        {
            if (!initiated) return;
            if (!enabled) return;

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreRead != null) OnPreRead(deltaTime);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnReadBehaviour(deltaTime);

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostRead != null) OnPostRead(deltaTime);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void OnWrite(float deltaTime)
        {
            if (!initiated) return;
            if (!enabled) return;

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreWrite != null) OnPreWrite(deltaTime);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnWriteBehaviour(deltaTime);

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostWrite != null) OnPostWrite(deltaTime);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void OnMuscleHit(MuscleHit hit)
        {
            if (!initiated) return;
#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreMuscleHit != null) OnPreMuscleHit(hit);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnMuscleHitBehaviour(hit);

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostMuscleHit != null) OnPostMuscleHit(hit);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void OnMuscleCollision(MuscleCollision collision)
        {
            if (!initiated) return;
#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreMuscleCollision != null) OnPreMuscleCollision(collision);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnMuscleCollisionBehaviour(collision);

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostMuscleCollision != null) OnPostMuscleCollision(collision);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void OnMuscleCollisionExit(MuscleCollision collision)
        {
            if (!initiated) return;
#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreMuscleCollisionExit != null) OnPreMuscleCollisionExit(collision);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnMuscleCollisionExitBehaviour(collision);

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostMuscleCollisionExit != null) OnPostMuscleCollisionExit(collision);
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        void OnEnable()
        {
            if (!initiated)
            {
                // Discarding Unity's initial OnEnable call, because the starting behaviour will be determined by PuppetMaster
                return;
            }

            Activate();
        }

        public void Activate()
        {
            foreach (BehaviourBase b in puppetMaster.behaviours)
            {
                b.enabled = b == this;
            }

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreActivate != null) OnPreActivate();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnActivate();

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostActivate != null) OnPostActivate();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        void OnDisable()
        {
            if (!initiated) return;
#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPreDeactivate != null) OnPreDeactivate();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.

            OnDeactivate();

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostDeactivate != null) OnPostDeactivate();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        public void FixedUpdateB(float deltaTime)
        {
            if (!initiated) return;
            if (!enabled) return;
            if (puppetMaster.muscles.Length <= 0) return;

            if (OnPreFixedUpdate != null && enabled) OnPreFixedUpdate(deltaTime);

            OnFixedUpdate(deltaTime);

            if (OnPostFixedUpdate != null && enabled) OnPostFixedUpdate(deltaTime);
        }

        public void UpdateB(float deltaTime)
        {
            if (!initiated) return;
            if (!enabled) return;
            if (puppetMaster.muscles.Length <= 0) return;

            if (OnPreUpdate != null && enabled) OnPreUpdate(deltaTime);

            OnUpdate(deltaTime);

            if (OnPostUpdate != null && enabled) OnPostUpdate(deltaTime);
        }

        public void LateUpdateB(float deltaTime)
        {
            if (!initiated) return;
            if (!enabled) return;
            if (puppetMaster.muscles.Length <= 0) return;

            if (OnPreLateUpdate != null && enabled) OnPreLateUpdate(deltaTime);

            OnLateUpdate(deltaTime);

            if (OnPostLateUpdate != null && enabled) OnPostLateUpdate(deltaTime);
        }

        protected virtual void OnDrawGizmos()
        {
            if (!initiated) return;
            OnDrawGizmosBehaviour();

#pragma warning disable IDE1005 // ����� �������� ����� ���������.
            if (OnPostDrawGizmos != null) OnPostDrawGizmos();
#pragma warning restore IDE1005 // ����� �������� ����� ���������.
        }

        protected virtual string GetTypeSpring()
        {
            return typeSpringBase;
        }

        private const string typeSpringBase = "BehaviourBase";

        /// <summary>
        /// Defines actions taken on certain events defined by the Puppet Behaviours.
        /// </summary>
        [System.Serializable]
        public struct PuppetEvent
        {
            [TooltipAttribute("Another Puppet Behaviour to switch to on this event. This must be the exact Type of the the Behaviour, careful with spelling.")]
            /// <summary>
            /// Another Puppet Behaviour to switch to on this event. This must be the exact Type of the the Behaviour, careful with spelling.
            /// </summary>
            public string switchToBehaviour;

            [TooltipAttribute("Animations to cross-fade to on this event. This is separate from the UnityEvent below because UnityEvents can't handle calls with more than one parameter such as Animator.CrossFade.")]
            /// <summary>
            /// Animations to cross-fade to on this event. This is separate from the UnityEvent below because UnityEvents can't handle calls with more than one parameter such as Animator.CrossFade.
            /// </summary>
            public AnimatorEvent[] animations;

            [TooltipAttribute("The UnityEvent to invoke on this event.")]
            /// <summary>
            /// The UnityEvent to invoke on this event.
            /// </summary>
            public UnityEvent unityEvent;

#pragma warning disable IDE1006 // ����� ����������
            public bool switchBehaviour
#pragma warning restore IDE1006 // ����� ����������
            {
                get
                {
                    return switchToBehaviour != string.Empty && switchToBehaviour != empty;
                }
            }
            private const string empty = "";

#pragma warning disable IDE0060 // ������� �������������� ��������
            public void Trigger(PuppetMaster puppetMaster, bool switchBehaviourEnabled = true)
#pragma warning restore IDE0060 // ������� �������������� ��������
            {
                unityEvent.Invoke();
                foreach (AnimatorEvent animatorEvent in animations) animatorEvent.Activate(puppetMaster.targetAnimator, puppetMaster.targetAnimation);

                if (switchBehaviour)
                {
                    bool found = false;

                    foreach (BehaviourBase behaviour in puppetMaster.behaviours)
                    {
                        if (behaviour != null && behaviour.GetTypeSpring() == switchToBehaviour)
                        {
                            found = true;
                            behaviour.enabled = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogError("No Puppet Behaviour of type '" + switchToBehaviour + "' was found. Can not switch to the behaviour, please check the spelling (also for empty spaces).");
                    }
                }
            }
        }

        /// <summary>
        /// Cross-fades to an animation state. UnityEvent can not be used for cross-fading, it requires multiple parameters.
        /// </summary>
        [System.Serializable]
        public class AnimatorEvent
        {

            /// <summary>
            /// The name of the animation state
            /// </summary>
            public string animationState;
            /// <summary>
            /// The crossfading time
            /// </summary>
            public float crossfadeTime = 0.3f;
            /// <summary>
            /// The layer of the animation state (if using Legacy, the animation state will be forced to this layer)
            /// </summary>
            public int layer;
            /// <summary>
            ///  Should the animation always start from 0 normalized time?
            /// </summary>
            public bool resetNormalizedTime;

            private const string empty = "";

            // Activate the animation
            public void Activate(Animator animator, Animation animation)
            {
                if (animator != null) Activate(animator);
                if (animation != null) Activate(animation);
            }

            // Activate a Mecanim animation
            private void Activate(Animator animator)
            {
                if (animationState == empty) return;

                if (resetNormalizedTime)
                {
                    if (crossfadeTime > 0f) animator.CrossFadeInFixedTime(animationState, crossfadeTime, layer, 0f);
                    else animator.Play(animationState, layer, 0f);
                }
                else
                {
                    if (crossfadeTime > 0f)
                    {
                        animator.CrossFadeInFixedTime(animationState, crossfadeTime, layer);
                    }
                    else animator.Play(animationState, layer);
                }
            }

            // Activate a Legacy animation
            private void Activate(Animation animation)
            {
                if (animationState == empty) return;

                if (resetNormalizedTime) animation[animationState].normalizedTime = 0f;

                animation[animationState].layer = layer;

                animation.CrossFade(animationState, crossfadeTime);
            }
        }

        protected void RotateTargetToRootMuscle()
        {
            Vector3 hipsForward = Quaternion.Inverse(puppetMaster.muscles[0].target.rotation) * puppetMaster.targetRoot.forward;
            Vector3 forward = puppetMaster.muscles[0].rigidbody.rotation * hipsForward;
            forward.y = 0f;
            puppetMaster.targetRoot.rotation = Quaternion.LookRotation(forward);
        }

        protected void TranslateTargetToRootMuscle(float maintainY)
        {
            puppetMaster.muscles[0].target.position = new Vector3(
                puppetMaster.muscles[0].transform.position.x,
                Mathf.Lerp(puppetMaster.muscles[0].transform.position.y, puppetMaster.muscles[0].target.position.y, maintainY),
                puppetMaster.muscles[0].transform.position.z);
        }

        protected void RemovePropMuscles()
        {
            while (ContainsRemovablePropMuscle())
            {
                for (int i = 0; i < puppetMaster.muscles.Length; i++)
                {
                    if (puppetMaster.muscles[i].props.group == Muscle.Group.Prop && !puppetMaster.muscles[i].isPropMuscle)
                    {
                        puppetMaster.RemoveMuscleRecursive(puppetMaster.muscles[i].joint, true);
                        break;
                    }
                }
            }
        }

        protected virtual void GroundTarget(LayerMask layers)
        {
            Ray ray = new Ray(puppetMaster.targetRoot.position + puppetMaster.targetRoot.up, -puppetMaster.targetRoot.up);
#pragma warning disable IDE0018 // ���������� ���������� ����������
            RaycastHit hit;
#pragma warning restore IDE0018 // ���������� ���������� ����������
            if (Physics.Raycast(ray, out hit, 4f, layers))
            {
                if (!float.IsNaN(hit.point.x) && !float.IsNaN(hit.point.y) && !float.IsNaN(hit.point.z))
                {
                    puppetMaster.targetRoot.position = hit.point;
                }
                else
                {
                    Debug.LogWarning("Raycasting against a large collider has produced a NaN hit point.", transform);
                }
            }
        }

        protected bool ContainsRemovablePropMuscle()
        {
            foreach (Muscle m in puppetMaster.muscles)
            {
                if (m.props.group == Muscle.Group.Prop && !m.isPropMuscle) return true;
            }
            return false;
        }
    }
}
