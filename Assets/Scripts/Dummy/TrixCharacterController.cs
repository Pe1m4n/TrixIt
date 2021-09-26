using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public enum TrixCharacterControllerState
{
	normal = 0, // нормальный локомоушен
	alwaysFalling = 1 // всегда падет, как будто с высоты (если начнёт дёргаться, выключить Can Get Up в puppetBehaviour)
}

public class TrixCharacterController : MonoBehaviour
{
	#region settings
	[RootMotion.LargeHeader("Links")]
	public Animator animator;
	public Rigidbody charRigidbody;
	public Transform charTransform;
	public RootMotion.Dynamics.BehaviourPuppet charPuppet;
	public Transform camTransform;
	public RootMotion.CameraController camController;
	public BalanceWatcher balanceWatcher;
	public FallingBehaviourControl fallingBehaviourControl;
	public RootMotion.Dynamics.PropMuscle rightHandPropMuscle;
	public bool WasBounced;

	[RootMotion.LargeHeader("Config")]
	public TrixCharacterControllerConfig config;
	[RootMotion.LargeHeader("States")]
	public TrixCharacterControllerState state = TrixCharacterControllerState.normal;
	#endregion

	#region fields
	private LerpingFloat runForward = new LerpingFloat();
	private LerpingFloat runTurn = new LerpingFloat();
	private LerpingFloat crouchForward = new LerpingFloat();
	private LerpingFloat crouchTurn = new LerpingFloat();
	private LerpingFloat strafeForward = new LerpingFloat();
	private LerpingFloat strafeRight = new LerpingFloat();

	private float lastTimeCamNoFollow; // последнее время, когда персонаж бежал на нас

	private float lastNonIdleTime;
	private bool prevBoring = false;
	private int boringId = 1;

	private Dictionary<OffsetInfo, bool> previousAnimationsAreOn = new Dictionary<OffsetInfo, bool>();

	private bool cam_isFollowingByOffset;
	private Vector3 cam_movementStartPoint;

	private bool jumpInput;
	private bool actionInput;

	private float jumpStartRotation;

	[HideInInspector]
	public bool rotateAfterCam = true;

	private Vector3 charTransformForwardAtTheBeginningOfTHeStartRun = Vector3.forward;

	private HashSet<RootMotion.Dynamics.PuppetMasterProp> nearbyProps = new HashSet<RootMotion.Dynamics.PuppetMasterProp>();
	#endregion

	#region unity
	private void Awake()
	{
		lastTimeCamNoFollow = Time.time;

		foreach (var ao in config.animationOffsets)
		{
			previousAnimationsAreOn[ao] = false;
		}
	}

	private void Start()
	{
		cam_movementStartPoint = charTransform.position;
		cam_isFollowingByOffset = false;
	}



	private void FixedUpdate()
	{
		switch (state)
		{
			case TrixCharacterControllerState.normal:

				bool isStartAnim = false;
				foreach (var san in config.startRunAnimNames)
				{
					if (animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(san)
					|| animator.GetNextAnimatorStateInfo(config.animLayer).IsName(san))
						isStartAnim = true;
				}
				if (!isStartAnim) charTransformForwardAtTheBeginningOfTHeStartRun = charTransform.forward;

				float hor;
				float ver;
				bool sprint;

				UpdateLerpingFloatsConfigs();
				GetInput(out hor, out ver, out sprint);

				float stickEpsilon = 0.01f;
				bool input = Mathf.Abs(hor) > stickEpsilon || Mathf.Abs(ver) > stickEpsilon || sprint; // || action || jumpInput
				if (input) lastNonIdleTime = Time.time;
				if (!animator.GetCurrentAnimatorStateInfo(0).IsName(config.idleAnimName) && !animator.GetCurrentAnimatorStateInfo(0).IsName(config.boringAnimName)) lastNonIdleTime = Time.time;
				bool boring = Time.time - lastNonIdleTime > config.boringTime;
				if (prevBoring == false && boring == true) boringId = Random.Range(1, config.boringVariantsNum + 1);
				prevBoring = boring;

				bool ground = DetectGround();
				UpdateCameraFollow(hor, ver);
				float aimAngle = ClampByAngle(Vector2.SignedAngle(new Vector2(charTransform.forward.x, charTransform.forward.z), new Vector2(camTransform.forward.x, camTransform.forward.z))
							+ Vector2.SignedAngle(Vector2.up, new Vector2(hor, ver)));
				float camAngle = ClampByAngle(Vector2.SignedAngle(new Vector2(charTransform.forward.x, charTransform.forward.z), new Vector2(camTransform.forward.x, camTransform.forward.z)));
				float aimAngleForStartRun = ClampByAngle(Vector2.SignedAngle(new Vector2(charTransformForwardAtTheBeginningOfTHeStartRun.x, charTransformForwardAtTheBeginningOfTHeStartRun.z), new Vector2(camTransform.forward.x, camTransform.forward.z))
							+ Vector2.SignedAngle(Vector2.up, new Vector2(hor, ver)));
				float strafeOffsetAngle = -Vector2.SignedAngle(new Vector2(camTransform.forward.x, camTransform.forward.z), new Vector2(charTransform.forward.x, charTransform.forward.z));
				UpdateLerpingFloats(hor, ver, strafeOffsetAngle, aimAngle);

				bool rootMotion = true;
				bool groundRm = config.rmOnlyOnGroundAnims.Any(x => animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(x));
				bool neverRm = config.neverRmAnims.Any(x => animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(x)) || config.neverRmAnims.Any(x => animator.GetNextAnimatorStateInfo(config.animLayer).IsName(x));
				bool doNotAffectRm = config.doNotAffectRm.Any(x => animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(x));
				if (groundRm) rootMotion = ground;
				if (neverRm) rootMotion = false;
				if (doNotAffectRm) rootMotion = animator.applyRootMotion;

				SetAnimator(rootMotion, hor, ver, sprint, boring, boringId);
				RotateCharacter(aimAngle, camAngle);

				CheckNoGroundFall();
				UpdateJump();
				UpdateAirControl(hor, ver);

				animator.SetFloat(config.angleForStartRunAnimProp, aimAngleForStartRun);
				animator.SetFloat(config.timeToLandingAnimProp, GetEstimatedTimeBeforeLanding());
				break;
			case TrixCharacterControllerState.alwaysFalling:
				break;
		}
	}

	private void Update()
	{
		switch (state)
		{
			case TrixCharacterControllerState.normal:
				foreach (var paio in previousAnimationsAreOn)
				{
					if (paio.Value)
					{
						if (animator.GetCurrentAnimatorStateInfo(0).IsName(paio.Key.animAfter))
						{
							charRigidbody.MovePosition(charRigidbody.position + charRigidbody.rotation * paio.Key.offset);
						}
					}
				}
				foreach (var ao in config.animationOffsets)
				{
					previousAnimationsAreOn[ao] = animator.GetCurrentAnimatorStateInfo(0).IsName(ao.animBefore);
				}

				GetInputInUpdate();
				SetAnimatorInUpdate();
				UpdateProps();

				break;
			case TrixCharacterControllerState.alwaysFalling:
				animator.SetTrigger(config.startHeightFallAnimProp);
				break;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		RootMotion.Dynamics.PuppetMasterProp otherProp = null;
		try
		{
			otherProp = other.transform.parent.GetComponent<RootMotion.Dynamics.PuppetMasterProp>();
		}
		catch { }
		if (otherProp != null)
		{
			nearbyProps.Add(otherProp);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		RootMotion.Dynamics.PuppetMasterProp otherProp = null;
		try
		{
			otherProp = other.transform.parent.GetComponent<RootMotion.Dynamics.PuppetMasterProp>();
		}
		catch { }
		if (otherProp != null)
		{
			nearbyProps.Remove(otherProp);
		}
	}
	#endregion

	private void UpdateProps()
	{
		if (rightHandPropMuscle != null)
		{
			if (Input.GetButtonDown(config.getPropBtn))
			{
				if (rightHandPropMuscle.currentProp == null && nearbyProps.Count > 0)
				{
					rightHandPropMuscle.currentProp = nearbyProps.First();
				}
			}
			else if (Input.GetButtonDown(config.dropPropBtn))
			{
				if (rightHandPropMuscle.currentProp != null)
				{
					rightHandPropMuscle.currentProp = null;
				}
			}
		}
	}

	private void UpdateAirControl(float hor, float ver)
	{
		var setting = AnimationSettingsSystem.ASS.GetCompiledSettings(animator, config.animLayer);

		/*if (false && setting != null && setting.Func_air != null) GameManager.FixUpdLog($"" +
			$"FlightControlForceEnabled: {setting.Func_air.FlightControlForceEnabled}\n" +
			$"FlightForwardControlForce: {setting.Func_air.FlightForwardControlForce}\n" +
			$"FlightBackControlForce: {setting.Func_air.FlightBackControlForce}\n" +
			$"FlightSideControlForce: {setting.Func_air.FlightSideControlForce}\n" +
			$"FlightVerticalControlForceEnabled: {setting.Func_air.FlightVerticalControlForceEnabled}\n" +
			$"FlightUpControlForce: {setting.Func_air.FlightUpControlForce}\n" +
			$"FlightDownControlForce: {setting.Func_air.FlightDownControlForce}\n" +
			$"InAirYawTurnControlEnabled: {setting.Func_air.InAirYawTurnControlEnabled}\n" +
			$"InAirYawControlTurnForce: {setting.Func_air.InAirYawControlTurnForce}\n" +
			$"FlightForwardForce: {setting.Func_air.FlightForwardForce}\n" +
			$"FlightUpForce: {setting.Func_air.FlightUpForce}\n" +
			$"FlightSideForce: {setting.Func_air.FlightSideForce}\n" +
			$"ForwardControlForceSpeedLimit: {setting.Func_air.ForwardControlForceSpeedLimit}\n" +
			$"BackControlForceSpeedLimit: {setting.Func_air.BackControlForceSpeedLimit}\n" +
			$"SideControlForceSpeedLimit: {setting.Func_air.SideControlForceSpeedLimit}\n" +
			$"UpControlForceSpeedLimit: {setting.Func_air.UpControlForceSpeedLimit}\n" +
			$"DownControlForceSpeedLimit: {setting.Func_air.DownControlForceSpeedLimit}\n" +
			$"PassiveForwardForceSpeedLimit: {setting.Func_air.PassiveForwardForceSpeedLimit}\n" +
			$"PassiveBackForceSpeedLimit: {setting.Func_air.PassiveBackForceSpeedLimit}\n" +
			$"PassiveSideForceSpeedLimit: {setting.Func_air.PassiveSideForceSpeedLimit}\n" +
			$"PassiveUpForceSpeedLimit: {setting.Func_air.PassiveUpForceSpeedLimit}\n" +
			$"PassiveDownForceSpeedLimit: {setting.Func_air.PassiveDownForceSpeedLimit}\n");*/
		Vector3 passivePositiveForceInCharCoords = Vector3.zero;
		Vector3 passiveNegativeForceInCharCoords = Vector3.zero;
		Vector3 controlForceInCharCoords = Vector3.zero;
		Vector3 velocityInLocalCoords = new Vector3(Vector3.Dot(charTransform.right, charRigidbody.velocity), Vector3.Dot(charTransform.up, charRigidbody.velocity), Vector3.Dot(charTransform.forward, charRigidbody.velocity));

		if (setting.Func_air.ControlForceEnabled)
		{
			float verForce = ver > 0 ? setting.Func_air.ForwardControlForce : setting.Func_air.BackControlForce;
			controlForceInCharCoords += Vector3.forward * verForce * ver + Vector3.right * hor * setting.Func_air.SideControlForce;
			if (Mathf.Abs(verForce * ver) > 0) GameManager.FixUpdLog($"AirControl forward force (controlled): {verForce * ver}");
			if (Mathf.Abs(hor * setting.Func_air.SideControlForce) > 0) GameManager.FixUpdLog($"AirControl right force (controlled): {hor * setting.Func_air.SideControlForce}");
		}
		if (setting.Func_air.VerticalControlForceEnabled)
		{
			float up = 0;
			if (Input.GetButton(config.jumpBtn)) up = 1;
			controlForceInCharCoords += Vector3.up * up * setting.Func_air.UpControlForce;
			if (Mathf.Abs(up * setting.Func_air.UpControlForce) > 0) GameManager.FixUpdLog($"AirControl up force (controlled): {up * setting.Func_air.UpControlForce}");
			float down = 0;
			if (Input.GetButton(config.crouchBtn)) down = 1;
			controlForceInCharCoords += Vector3.up * (-1) * down * setting.Func_air.DownControlForce;
			if (Mathf.Abs(down * setting.Func_air.DownControlForce) > 0) GameManager.FixUpdLog($"AirControl up force (controlled): {down * setting.Func_air.DownControlForce}");
		}
		passivePositiveForceInCharCoords += Vector3.forward * setting.Func_air.PassiveForwardForce + Vector3.up * setting.Func_air.PassiveUpForce + Vector3.right * setting.Func_air.PassiveRightForce;
		passiveNegativeForceInCharCoords += Vector3.back * setting.Func_air.PassiveBackForce + Vector3.down * setting.Func_air.PassiveDownForce + Vector3.left * setting.Func_air.PassiveLeftForce;

		GameManager.FixUpdLog($"AirControl positive passive raw force: {passivePositiveForceInCharCoords}");
		GameManager.FixUpdLog($"AirControl negative passive raw force: {passiveNegativeForceInCharCoords}");
		GameManager.FixUpdLog($"Control AirControl raw force: {controlForceInCharCoords}");
		GameManager.FixUpdLog($"Character velocity: {velocityInLocalCoords}");

		UpdateForceWithLimits(ref passivePositiveForceInCharCoords, velocityInLocalCoords,
			setting.Func_air.PassiveForwardForceSpeedLimit,
			setting.Func_air.PassiveBackForceSpeedLimit,
			setting.Func_air.PassiveUpForceSpeedLimit,
			setting.Func_air.PassiveDownForceSpeedLimit,
			setting.Func_air.PassiveLeftForceSpeedLimit,
			setting.Func_air.PassiveRightForceSpeedLimit);

		UpdateForceWithLimits(ref passiveNegativeForceInCharCoords, velocityInLocalCoords,
			setting.Func_air.PassiveForwardForceSpeedLimit,
			setting.Func_air.PassiveBackForceSpeedLimit,
			setting.Func_air.PassiveUpForceSpeedLimit,
			setting.Func_air.PassiveDownForceSpeedLimit,
			setting.Func_air.PassiveLeftForceSpeedLimit,
			setting.Func_air.PassiveRightForceSpeedLimit);

		UpdateForceWithLimits(ref controlForceInCharCoords, velocityInLocalCoords,
			setting.Func_air.ForwardControlForceSpeedLimit,
			setting.Func_air.BackControlForceSpeedLimit,
			setting.Func_air.UpControlForceSpeedLimit,
			setting.Func_air.DownControlForceSpeedLimit,
			setting.Func_air.SideControlForceSpeedLimit,
			setting.Func_air.SideControlForceSpeedLimit);

		GameManager.FixUpdLog($"AirControl positive passive force: {passivePositiveForceInCharCoords}");
		GameManager.FixUpdLog($"AirControl negative passive force: {passiveNegativeForceInCharCoords}");
		GameManager.FixUpdLog($"Control AirControl force: {controlForceInCharCoords}");

		var forceInCharCoords = passivePositiveForceInCharCoords + passiveNegativeForceInCharCoords + controlForceInCharCoords;

		Vector3 force = charTransform.right * forceInCharCoords.x + charTransform.up * forceInCharCoords.y + charTransform.forward * forceInCharCoords.z;

		GameManager.FixUpdLog($"AirControl force final: {forceInCharCoords}");
		if (force.sqrMagnitude > 0)
		{
			charRigidbody.AddForce(force, ForceMode.Force);
		}
	}

	private void UpdateForceWithLimits(ref Vector3 force, Vector3 velocity, float limForw, float limBack, float limUp, float limDown, float limLeft, float limRight)
	{
		UpdateForceAxisWithLimits(ref force.x, velocity.x, limLeft, limRight);
		UpdateForceAxisWithLimits(ref force.y, velocity.y, limDown, limUp);
		UpdateForceAxisWithLimits(ref force.z, velocity.z, limBack, limForw);
	}

	private void UpdateForceAxisWithLimits(ref float force, float velocity, float limMin, float limMax)
	{
		if (limMax < 0) throw new System.ArgumentException("AirControl: limMax < 0"); // TODO: Сделать нормальное сообщение об этом
		if (limMin < 0) throw new System.ArgumentException("AirControl: limMin < 0"); // TODO: Сделать нормальное сообщение об этом

		if (force > 0)
		{
			if (0 < velocity && velocity < limMax) force = Mathf.Lerp(force, 0, Mathf.InverseLerp(0, limMax, velocity));
			else if (limMax < velocity) force = 0;
		}
		else if (force < 0)
		{
			if (velocity <= -limMin) force = 0;
			else if (velocity < -limMin && velocity < 0) force = Mathf.Lerp(force, 0, Mathf.InverseLerp(-limMin, 0, velocity));
		}
	}

	private void SetAnimatorInUpdate()
	{
		bool isInJumpRightNow = IsInAnyOfAnims(animator, config.animLayer, config.jumpAnimNames.ToArray());
		bool canJump = !config.animsToDisableJumpInput.Any(x => animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(x))
			|| !config.animsToDisableJumpInput.Any(x => animator.GetNextAnimatorStateInfo(config.animLayer).IsName(x));
		if (jumpInput && !isInJumpRightNow)
		{
			animator.SetTrigger(config.startJumpAnimProp);
			animator.applyRootMotion = true;
		}
		else if (jumpInput && isInJumpRightNow && canJump) // Для повторного прыжка в будущем
		{
			animator.SetTrigger(config.startJumpAnimProp);
		}
		if (!canJump) animator.ResetTrigger(config.startJumpAnimProp);
		animator.SetBool(config.inJumpAnimProp, isInJumpRightNow);
		if (actionInput) animator.SetTrigger(config.actionAnimProp);
	}

	private void GetInputInUpdate()
	{
		jumpInput = Input.GetButtonDown(config.jumpBtn);
		actionInput = Input.GetButtonDown(config.actionBtn);
	}

	private float GetVerDistanceToTheEstimatedLandingPoint(float verticalOffset = 0) // TODO: оптимизация
	{
		var downDist = GetDistanceToTheGround();
		if (downDist < Mathf.Epsilon) return 0;

		Vector3 pos = charRigidbody.position + Vector3.up * verticalOffset;
		Vector3 vel = charRigidbody.velocity;
		Vector3 acc = Physics.gravity;
		float time = 0f;
		float timeStep = 0.1f;
		Vector3 pos1;
		Vector3 pos2;

		do
		{
			float timeNext = time + timeStep;
			pos1 = pos + vel * time + acc * time * time;
			pos2 = pos + vel * timeNext + acc * timeNext * timeNext;

			Ray ray = new Ray(pos1, pos2 - pos1);
			RaycastHit hit;
			Debug.DrawLine(pos1, pos2, Color.red);
			if (Physics.Raycast(ray, out hit, (pos2 - pos1).magnitude, config.groundLayers))
			{
				return Mathf.Abs((hit.point - pos).y);
			}
			time += timeStep;
		}
		while (Mathf.Abs((pos2 - pos).y) < 50);

		return 50;
	}

	private float GetEstimatedLandingSurfaceAngle(float verticalOffset = 0) // TODO: повторение кода, нужен рефактор
	{
		var downDist = GetDistanceToTheGround();
		if (downDist < Mathf.Epsilon) return 0;

		Vector3 pos = charRigidbody.position + Vector3.up * verticalOffset;
		Vector3 vel = charRigidbody.velocity;
		Vector3 acc = Physics.gravity;
		float time = 0f;
		float timeStep = 0.1f;
		Vector3 pos1;
		Vector3 pos2;

		do
		{
			float timeNext = time + timeStep;
			pos1 = pos + vel * time + acc * time * time;
			pos2 = pos + vel * timeNext + acc * timeNext * timeNext;

			Ray ray = new Ray(pos1, pos2 - pos1);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, (pos2 - pos1).magnitude, config.groundLayers))
			{
				return Vector3.Angle(Vector3.up, hit.normal);
			}
			time += timeStep;
		}
		while (Mathf.Abs((pos2 - pos).y) < 50);

		return 0;
	}

	private float GetDistanceToTheGround()
	{
		Ray ray = new Ray(charTransform.position + Vector3.up, Vector3.down);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 11, config.groundLayers))
		{
			return hit.distance - 1;
		}
		else return 10;
	}

	private float GetVerticalSpeed()
	{
		return -charRigidbody.velocity.y;
	}

	private float GetEstimatedVerticalLandingSpeed(float verticalOffset = 0)
	{
		float estimatedTime = GetEstimatedTimeBeforeLanding(verticalOffset);
		float verticalSpeed = GetVerticalSpeed();
		float verticalAcc = Mathf.Abs(Physics.gravity.y);

		float estimatedSpeed = verticalSpeed + verticalAcc * estimatedTime;
		return estimatedSpeed;
	}

	private void CheckNoGroundFall()
	{
		if (charPuppet.state == RootMotion.Dynamics.BehaviourPuppet.State.Puppet
			&& balanceWatcher.MaxDissatisfactionOfAnimationFulcrums > config.animFulcrumRagdollTreshold
			&& !haveInstantGround())
		{
			if (AnimationSettingsSystem.ASS.GetCompiledSettings(animator, config.animLayer).Func_self.FulcrumLossCustomHandling)
			{
				//if (GetEstimatedVerticalLandingSpeed() > config.autoLandingSpeedThreshold)
				//{
				GameManager.Log($"<color=#ADD8E6>No ground jump | landspeed={GetEstimatedVerticalLandingSpeed()} | distance from fulcrum to ground = {balanceWatcher.MaxDissatisfactionOfAnimationFulcrums} | bp = {balanceWatcher.MaxFulcrumDissatisfactionBodyPartName}</color>");
				animator.SetBool(config.fulcrumLossCustomHandlingAnimProp, true);
				//}
			}
			else
			{
				GameManager.Log($"<color=red>No ground ragdoll | landspeed={GetEstimatedVerticalLandingSpeed()} | distance from fulcrum to ground = {balanceWatcher.MaxDissatisfactionOfAnimationFulcrums} | bp = {balanceWatcher.MaxFulcrumDissatisfactionBodyPartName}</color>");
				fallingBehaviourControl.LosePinsEvent();
				animator.SetTrigger(config.startHeightFallAnimProp);
				animator.SetBool(config.fulcrumLossCustomHandlingAnimProp, false);
			}
		}
		else
		{
			animator.SetBool(config.fulcrumLossCustomHandlingAnimProp, false);
		}
	}

	public void TryStartLanding()
	{
		var landspeed = GetEstimatedVerticalLandingSpeed();

		GameManager.Log($"<color=#ADD8E6>Jump Free | landspeed={landspeed}</color>");
		animator.SetTrigger(config.freeLandingAnimProp);
		animator.applyRootMotion = false;
	}

	private void UpdateJump()
	{
		// estimated time and speed
		float estimatedVerSpeed = GetEstimatedVerticalLandingSpeed();
		float estimatedSpeed = new Vector3(charRigidbody.velocity.x, estimatedVerSpeed, charRigidbody.velocity.z).magnitude;
		float estimatedTime = GetEstimatedTimeBeforeLanding();

		float offsettedEstimatedVerSpeed = GetEstimatedVerticalLandingSpeed(config.WallHitParabolaHeightOffset);
		float offsettedEstimatedSpeed = new Vector3(charRigidbody.velocity.x, offsettedEstimatedVerSpeed, charRigidbody.velocity.z).magnitude;
		float offsettedEstimatedTime = GetEstimatedTimeBeforeLanding(config.WallHitParabolaHeightOffset);
		float offsettedEstimatedLandingSurfaceAngle = GetEstimatedLandingSurfaceAngle(config.WallHitParabolaHeightOffset);

		if (charPuppet.state == RootMotion.Dynamics.BehaviourPuppet.State.Puppet && Helpers.IsOneOfTheAnimations(animator, config.jumpAnimNames.ToArray(), config.animLayer) && offsettedEstimatedTime < config.thresholdTimeToFallFromWallHit && offsettedEstimatedSpeed > config.thresholdSpeedToFallFromWallHit && offsettedEstimatedLandingSurfaceAngle > config.thresholdSlopeAngleToFallFromWallHit)
		{
			if (config.logWallHitSuccess) GameManager.Log($"Wall Hit! speed = {offsettedEstimatedSpeed} | surfaceAngle = {offsettedEstimatedLandingSurfaceAngle}");
			fallingBehaviourControl.SwitchToRagdoll("Wall Hit");
		}
		else if (charPuppet.state == RootMotion.Dynamics.BehaviourPuppet.State.Puppet && Helpers.IsOneOfTheAnimations(animator, config.jumpAnimNames.ToArray(), config.animLayer) && offsettedEstimatedTime < config.thresholdTimeToFallFromWallHit)
		{
			if (config.logWallHitFail) GameManager.Log($"Wall Hit Failed. speed = {offsettedEstimatedSpeed} | surfaceAngle = {offsettedEstimatedLandingSurfaceAngle}");
		}

		animator.SetFloat(config.estimatedLandspeedAnimProp, estimatedVerSpeed);
		animator.SetFloat(config.estimatedTimeToLandAnimProp, estimatedTime);

		// distances to ground
		float jumpOffForwardDetectionOffset = 20;
		if (Physics.Raycast(charTransform.position + charTransform.forward * config.jumpOffForwardDetectionOffset + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 20, config.groundLayers))
		{
			jumpOffForwardDetectionOffset = hit.distance - 0.2f;
		}
		if (Physics.Raycast(charTransform.position + Vector3.up * 0.2f, charTransform.forward, out RaycastHit hitFrw, config.jumpOffForwardDetectionOffset, config.groundLayers)) // на случай, если перед нами стена
		{
			jumpOffForwardDetectionOffset = 0;
		}
		animator.SetFloat(config.jumpOffForwardDetectionOffsetAnimProp, jumpOffForwardDetectionOffset);

		animator.SetFloat(config.jumpOffDetectionAnimProp, balanceWatcher.DistanceToGround);

		// slope angle
		float maxSlopeAngle = 0;
		for (int i = 0; i < config.amountOfIntervalsToCalcSlopeAngle; i++)
		{
			float discStep = config.jumpOffForwardDetectionOffset / config.amountOfIntervalsToCalcSlopeAngle;
			if (Physics.Raycast(charTransform.position + charTransform.forward * i * discStep + Vector3.up, Vector3.down, out RaycastHit hit1, 20, config.groundLayers)
				&& Physics.Raycast(charTransform.position + charTransform.forward * (i + 1) * discStep + Vector3.up, Vector3.down, out RaycastHit hit2, 20, config.groundLayers))
			{
				float dist1 = hit1.distance - 1;
				float dist2 = hit2.distance - 1;

				float angle = Mathf.Atan2(Mathf.Abs(dist1 - dist2), discStep) * Mathf.Rad2Deg;
				if (angle > maxSlopeAngle) maxSlopeAngle = angle;
			}
			else
			{
				maxSlopeAngle = 90;
			}
		}
		animator.SetFloat(config.slopeAngleAnimProp, maxSlopeAngle);
	}

	public float GetEstimatedTimeBeforeLanding(float verticalOffset = 0)
	{
		float verticalDist = GetVerDistanceToTheEstimatedLandingPoint(verticalOffset);
		float verticalSpeed = GetVerticalSpeed();
		float verticalAcc = Mathf.Abs(Physics.gravity.y);

		float D = verticalSpeed * verticalSpeed + 4 * verticalAcc * verticalDist;
		float estimatedTime = (-verticalSpeed + Mathf.Sqrt(D)) / (2 * verticalAcc);
		return estimatedTime;
	}

	private bool haveInstantGround()
	{
		return balanceWatcher.InstantMaxOnLine > 0 && balanceWatcher.InstantMinOnLine < 0 && balanceWatcher.IntersectionsCount > 0;

	}

	#region FixedUpdate parts
	private void UpdateLerpingFloatsConfigs()
	{
		runForward.Speed = config.runForwardUpdSpeed2;
		runTurn.Speed = config.runTurnUpdSpeed2;
		crouchForward.Speed = config.crouchForwardUpdSpeed2;
		crouchTurn.Speed = config.crouchTurnUpdSpeed2;
		strafeForward.Speed = config.strafeUpdSpeed2;
		strafeRight.Speed = config.strafeUpdSpeed2;
	}

	// TODO: Вытащить чтение и обработку ввода (вплоть до aim, наверное) в Update, это должно исправить залипание кнопок в SlowMo и на быстром компьютере
	private void GetInput(out float hor, out float ver, out bool sprint)
	{
		hor = Input.GetAxisRaw(config.horAxis);
		ver = Input.GetAxisRaw(config.verAxis);
		sprint = Input.GetButton(config.sprintBtn);
	}

	private bool DetectGround()
	{
		RaycastHit hit;
		float offset = 0.5f;
		bool fact = Physics.Raycast(charRigidbody.position + Vector3.up * offset, Vector3.down, out hit, 1, config.groundLayers);
		if (fact == false) return false;
		return hit.distance < offset + 0.05f;
	}

	private void UpdateCameraFollow(float hor, float ver)
	{
		if (camController != null && camController.follow == true)
		{
			if (ver < -Mathf.Epsilon && Mathf.Abs(ver) > Mathf.Abs(hor))
			{
				lastTimeCamNoFollow = Time.time;
				camController.secondaryFollow = false;
			}
			else
			{
				camController.secondaryFollow = Time.time - lastTimeCamNoFollow > config.camFolowTimeout;
			}

			if (cam_isFollowingByOffset && !config.animsForCamToFollow.Any(x => animator.GetCurrentAnimatorStateInfo(0).IsName(x)))
			{
				cam_isFollowingByOffset = false;
				cam_movementStartPoint = charTransform.position;
			}

			if (!cam_isFollowingByOffset && (cam_movementStartPoint - charTransform.position).magnitude > config.camOffsetBeforeFollow)
			{
				cam_isFollowingByOffset = true;
			}

			if (!cam_isFollowingByOffset)
			{
				camController.secondaryFollow = false;
			}


		}
	}

	private void UpdateLerpingFloats(float hor, float ver, float strafeOffsetAngle, float aimAngle)
	{
		Vector2 strafe2 = new Vector2(hor, ver);
		strafe2 = RotateVector2(strafe2, strafeOffsetAngle);
		strafeForward.Val = strafe2.y;
		strafeRight.Val = strafe2.x;
		strafeForward.Upd(Time.deltaTime);
		strafeRight.Upd(Time.deltaTime);

		float loc_runForward2 = Mathf.Min(1, new Vector2(hor, ver).magnitude);
		runForward.Val = loc_runForward2;
		runTurn.Val = loc_runForward2 < Mathf.Epsilon && !Helpers.IsOneOfTheAnimations(animator, config.jumpAnimNames.ToArray(), 0) ? 0 : (-Mathf.Sign(aimAngle) * Mathf.Abs(aimAngle / 180));
		runForward.Upd(Time.deltaTime);
		runTurn.Upd(Time.deltaTime);

		crouchForward.Val = loc_runForward2;
		crouchTurn.Val = loc_runForward2 < Mathf.Epsilon ? 0 : (-Mathf.Sign(aimAngle) * Mathf.Abs(aimAngle / 180));
		crouchForward.Upd(Time.deltaTime);
		crouchTurn.Upd(Time.deltaTime);
	}

	private void SetAnimator(bool rootMotion, float hor, float ver, bool sprint, bool boring, int boringId)
	{
		animator.applyRootMotion = rootMotion;

		animator.SetBool(config.boringAnimProp, boring);
		animator.SetFloat(config.boringIdAnimProp, boringId);

		// spine
		animator.SetFloat(config.horizontalAnimProp, runTurn.Val * config.horizontalMlp);
		animator.SetFloat(config.verticalAnimProp, runForward.Val);
		animator.SetFloat(config.stickIntensiveAnimProp, Mathf.Max(Mathf.Abs(hor), Mathf.Abs(ver))); // тут надо иначе, но пока что так
		animator.SetBool(config.freemovementAnimProp, Mathf.Max(Mathf.Abs(hor), Mathf.Abs(ver)) > 0.01 && !sprint);
		animator.SetBool(config.fastRunAnimProp, sprint);

		var state = animator.IsInTransition(config.animLayer) ? animator.GetNextAnimatorStateInfo(config.animLayer) : animator.GetCurrentAnimatorStateInfo(config.animLayer);
		animator.SetFloat(config.animProgressAnimProp, state.normalizedTime % 1);
	}

	private float NormalizeAngle(float angle)
	{
		while (angle > 180) angle -= 360;
		while (angle < -180) angle += 360;
		return angle;
	}

	private void RotateCharacter(float aimAngle, float camAngle)
	{
		if (!Helpers.IsOneOfTheAnimations(animator, config.jumpAnimNames.ToArray(), 0))
		{
			jumpStartRotation = NormalizeAngle(charRigidbody.rotation.eulerAngles.y);
		}

		for (int i = 0; i < config.turnSpeeds2.Length; i++)
		{
			if (animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(config.turnSpeeds2[i].AnimationName))
			{
				float deltaRotation = config.turnSpeeds2[i].RotationSpeed * Time.deltaTime;
				if (deltaRotation > 180) deltaRotation = 180;
				float rotation = runTurn.AimVal * deltaRotation;
				float deb1 = rotation;

				if (Helpers.IsOneOfTheAnimations(animator, config.jumpAnimNames.ToArray(), 0))
				{
					float lim = config.jumpRotationLimit;
					float eul = NormalizeAngle(charRigidbody.rotation.eulerAngles.y);
					float curRot = NormalizeAngle(eul - jumpStartRotation + rotation);
					if (curRot > lim) rotation = NormalizeAngle(rotation - curRot + lim);
					else if (curRot < -lim) rotation = NormalizeAngle(rotation - curRot - lim);
				}
				charTransform.Rotate(Vector3.up, rotation);
				break;
			}
		}

		if (rotateAfterCam)
		{
			for (int i = 0; i < config.turnToCamSpeeds.Length; i++)
			{
				if (animator.GetCurrentAnimatorStateInfo(config.animLayer).IsName(config.turnToCamSpeeds[i].AnimationName))
				{
					float deltaRotation = config.turnToCamSpeeds[i].RotationSpeed * Time.deltaTime;
					if (deltaRotation > 180) deltaRotation = 180;
					charTransform.Rotate(Vector3.up, -camAngle * deltaRotation);
					break;
				}
			}
		}
	}
	#endregion

	#region helpers
	// http://answers.unity.com/answers/734946/view.html
	private static Vector2 RotateVector2(Vector2 v, float degrees)
	{
		float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
		float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

		float tx = v.x;
		float ty = v.y;
		v.x = (cos * tx) - (sin * ty);
		v.y = (sin * tx) + (cos * ty);
		return v;
	}

	private static float ClampByAngle(float angle)
	{
		while (angle > 180) angle -= 360;
		while (angle < -180) angle += 360;
		return angle;
	}

	private static bool IsInAnyOfAnims(Animator animator, int animLayer, string[] animNames)
	{
		foreach (var a in animNames)
		{
			if (animator.GetCurrentAnimatorStateInfo(animLayer).IsName(a))
			{
				return true;
			}
		}
		return false;
	}
	#endregion
}


public class LerpingFloat
{
	private float actualVal = 0;
	private float aimVal = 0;

	public float Speed { get; set; }

	public float Val
	{
		get => actualVal;
		set => aimVal = value;
	}

	public float AimVal { get => aimVal; }

	public void Upd(float deltaTime)
	{
		actualVal = Mathf.Lerp(actualVal, aimVal, Speed * deltaTime);
	}
}

[System.Serializable]
public struct AnimationTurnParams
{
	public string AnimationName;
	public float RotationSpeed;
}
