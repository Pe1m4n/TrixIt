using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct HitGroup
{
	public string animPropName;
	public List<Collider> colliders;
}

[System.Serializable]
public struct BoostAnimation
{
	public string animStateName;
	public bool isAction;
	public int actionNumber;
	public RootMotion.Dynamics.Booster booster;
}

public enum FallDirection
{
	forward = 1, // был удар, летим вперёд
	back = 2, // был удар, летим назад
	left = 3, // был удар, летим налево
	right = 4, // был удар, летим направо
}

public class FallingBehaviourControl : MonoBehaviour
{
	#region settings
	[RootMotion.LargeHeader("Object Links")]
	public BodyPartsDictionary partsDictionary;
	public BalanceWatcher balanceWatcher;
	public RootMotion.Dynamics.BehaviourPuppet puppetBehaviour;

	public string startHitAnimProp;
	public string directionAnimProp;
	public string xPosAnimProp;
	public string yPosAnimProp;
	public string forceAnimProp;
	public string startFallAnimProp;
	public string fallDirectionAnimProp;
	public string getUpDirectionAnimProp;
	public string mirrorActionAnimProp;
	public string jumpFinishAnimProp;
	public string inHitFallAnimProp;
	public string isProneAnimProp;
	//public string timeToLandingAnimProp;
	public string[] noHitAnims;

	public int hitAnimLayerIndex;
	public List<string> hitAnimStateNames;
	public List<string> hitTransitionNames;
	public int fallAnimLayerIndex;
	public List<string> fallAnimStateNames;
	public string actionStateName;
	public string actionNumPropName;
	public string toActionTransitionName;
	public string jumpMiddleStateName;
	public string jumpFinishStateName;
	public string fallFromHeightStateName;

	[RootMotion.LargeHeader("Settings")]
	public float hitRegistrationCooldown;
	public bool useGetUpAim;
	public float centerMassKnockoutDistance;
	public float inAirCMKnockoutDistance;
	public float cmKnockoutDistanceSpeedUp;
	public float cmKnockoutDistanceSpeedDown;
	public bool beKinematicWhenNoHit;
	public float kinematicKnockoutDistance; // для тех случаев, когда PM ломается, сука
	public float kinematicLastHitMemory = 0; // сколько держать реакцию на хиты после удара
	public float unpinnedMusclesValue = 0;
	public float absoluteOffsetMlp = 0;
	public float secondHitCheckTreshold;
	public float hitUnpinPower;

	public float denyToSmallHit = 30;
	public float smallToStrongHit = 60;
	public float strongHitToFall = 120;
	public float fallToInstantRagdoll = 250;
	public float instantRagdollCurrentCMTreshold = 80;
	public float smallToStrongHitAnimator = 0.5f;
	public float hullMultiplier = 1;

	public float muscleUpSpeed = 1;
	public float muscleDownSpeed = 1;
	public float muscleIniRagUpSpeed = 1;
	public float muscleIniRagDownSpeed = 1;

	public BoostAnimation[] boosters;


	[RootMotion.LargeHeader("Visualisation")]
	public RectTransform debugPosVisualisation;
	public RectTransform debugSafeCurcleVisualisation;
	public RectTransform debugKinematicSafeCurcleVisualisation;
	public float debugMlp;
	public bool LogDenies = true;
	#endregion

	#region fields
	private float lastReactionTime = 0;

	private int hitTriggerFrames = 0;
	private int hitTriggerCriticalFrames = 5;

	private float initialPmMuscleWeight;

	private Queue<string> HitHtstoryTexts = new Queue<string>();
	private int MaxHitHistoryEntries = 5;



	private bool isWaitingForRagdoll = false;

	private float muscleWeightAim;

	private int forceFallCheckFrames = 0;

	private float muscleWeightUpSpeed = 1;
	private float muscleWeightDownSpeed = 1;

	private float actualCMKnockoutDistance;

	private float prevCMOffset = 0;
	private float prevCMOffsetChange = 0;
	#endregion

	#region properties
	#endregion

	#region unity
	private void Awake()
	{
		if (balanceWatcher == null) Debug.LogError("No BalanceWatcher");
		initialPmMuscleWeight = partsDictionary.puppetMaster.muscleWeight;
		muscleWeightAim = partsDictionary.puppetMaster.muscleWeight;
		muscleWeightUpSpeed = muscleUpSpeed;
		muscleWeightDownSpeed = muscleDownSpeed;
		actualCMKnockoutDistance = centerMassKnockoutDistance;
	}

	private void Update()
	{
		VisualiseDebug();
	}

	private void FixedUpdate()
	{
		InHitCheckFall();
		if (DecideWhetherToFall())
			HandleKnockout();
		else
		{
			// пишем в HitHistory
			if (LogDenies && !IsKinematic)
			{
				if ((puppetBehaviour.state != RootMotion.Dynamics.BehaviourPuppet.State.Unpinned &&
					GetGeneralOffsetFromAnimation() > actualCMKnockoutDistance))
					if (!(balanceWatcher.MinOnLine == 0 || balanceWatcher.MaxOnLine == 0 || !haveGround() ||
					futureMassCenterOnLineToPercents() > denyToSmallHit))
						AddHitHistory($"<color=green>HitDeny | percent={futureMassCenterOnLineToPercents()}</color>");
			}
		}
		CheckWaitForRagdoll();
		UpdateKnockoutDistances();
		UpdateAnimatorData();
		UpdateMusclePowers();
		UpdateRagdollness();
		UpdateMirrorActions();
		FreezeConvexHull();
		CheckLostHitTrigger();
	}
	#endregion

	#region events
	public void BonesTooFarHandler()
	{
	}

	public void FallEvent()
	{
		if (puppetBehaviour.state == RootMotion.Dynamics.BehaviourPuppet.State.Invincible)
			puppetBehaviour.SetState(RootMotion.Dynamics.BehaviourPuppet.State.Puppet);
	}

	public void LosePinsEvent()
	{
		puppetBehaviour.SetState(RootMotion.Dynamics.BehaviourPuppet.State.Unpinned);
	}

	public void LoseMusclesEvent()
	{
		muscleWeightUpSpeed = muscleUpSpeed;
		muscleWeightDownSpeed = muscleDownSpeed;
		muscleWeightAim = unpinnedMusclesValue;
	}

	public void OnGetUpEvent()
	{
		UpdateGetUpDirection();
	}

	public void OnLeaveUnpinnedEvent()
	{
		muscleWeightAim = initialPmMuscleWeight;
	}
	#endregion

	#region helpers
	private static Vector2 v3t2(Vector3 v3)
	{
		return new Vector2(v3.x, v3.z);
	}

	private static string f2s(float f)
	{
		return string.Format("{0:0.00}", f);
	}

	private static Vector2 preparePosToAnimator(Vector2 pos)
	{
		float x = pos.x + 0.5f;
		float y = pos.y + 0.5f;
		Vector2 res = new Vector2(x, y);
		return res;
	}

	private static FallDirection Angle2FallDir(float angle)
	{
		FallDirection fd;
		if (angle < -135) { fd = FallDirection.back; }
		else if (angle < -45) { fd = FallDirection.left; }
		else if (angle < 45) { fd = FallDirection.forward; }
		else if (angle < 135) { fd = FallDirection.right; }
		else { fd = FallDirection.back; }
		return fd;
	}

	private static float Angle2Direction(float angle)
	{
		if (Mathf.Abs(angle) > 180) throw new ArgumentException("Угол больше 180 градусов!");
		float minAngle = -180;
		float maxAngle = 180;
		float directionOffset = 0f;
		float direction = (angle - minAngle) / (maxAngle - minAngle);
		direction += directionOffset;
		if (direction > 1) direction -= 1;
		return direction;
	}
	#endregion

	#region calculations
	private float Get3DOffsetFromAnimation()
	{
		Vector3 cm = balanceWatcher.MassCenter;
		Vector3 acm = balanceWatcher.AnimMassCenter;
		return Vector3.Distance(cm, acm);
	}

	private float GetGeneralOffsetFromAnimation()
	{
		float cmOff = GetCenterMassOffsetFromAnimation();
		float absOff = balanceWatcher.AbsoluteWeightedOffset;
		float cmOffMlp = 1;
		return cmOff * cmOffMlp + absOff * absoluteOffsetMlp;
	}

	private float GetCenterMassOffsetFromAnimation()
	{
		return Vector2.Distance(balanceWatcher.MassCenterProcessed, balanceWatcher.AnimationMassCenterProcessed);
	}

	private int futureMassCenterOnLineToPercents()
	{
		return valueOnLineToPercents(balanceWatcher.FutureMassCenterOnLine);
	}

	private int currentMassCenterOnLineToPercents()
	{
		return valueOnLineToPercents(balanceWatcher.MassCenterOnLine);
	}

	private int valueOnLineToPercents(float value)
	{
		if (balanceWatcher.MaxOnLine > 0 && balanceWatcher.MinOnLine > 0)
		{
			return int.MinValue;
		}
		else if (balanceWatcher.MaxOnLine > 0 && balanceWatcher.MinOnLine < 0)
		{
			float hullSize = balanceWatcher.MaxOnLine - balanceWatcher.MinOnLine;
			if (value > 0)
			{
				return Mathf.RoundToInt((value / (balanceWatcher.MaxOnLine + hullMultiplier / hullSize)) * 100);
			}
			else
			{
				return Mathf.RoundToInt((value / (balanceWatcher.MinOnLine - hullMultiplier / hullSize)) * 100);
			}
		}
		else if (balanceWatcher.MaxOnLine < 0 && balanceWatcher.MinOnLine < 0)
		{
			return int.MaxValue;
		}
		else
		{
			return 0;
		}
	}

	private float GetGeneralImpulseOffsetFromAnimation()
	{
		return Vector2.Distance(balanceWatcher.GeneralImpulseProcessed, balanceWatcher.AnimationGeneralImpulseProcessed);
	}
	#endregion

	#region bool calculations
	public bool IsKinematic
	{
		get
		{
			if (beKinematicWhenNoHit)
			{
				if (balanceWatcher.WasHit)
				{
					return false;
				}
				else
				{
					if (Time.time - balanceWatcher.LastHitTime < kinematicLastHitMemory)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
			}
			else
			{
				return false;
			}
		}
	}

	private bool haveGround()
	{
		return balanceWatcher.MaxOnLine > 0 && balanceWatcher.MinOnLine < 0 && balanceWatcher.IntersectionsCount > 0;
	}

	

	public bool IsInHitReactionAnimation()
	{
		foreach (var hitAnimStateName in hitAnimStateNames)
		{
			if (partsDictionary.characterAnimator.GetCurrentAnimatorStateInfo(hitAnimLayerIndex).IsName(hitAnimStateName)) return true;
		}
		foreach (var fromHitTransitionName in hitTransitionNames)
		{
			if (partsDictionary.characterAnimator.GetAnimatorTransitionInfo(hitAnimLayerIndex).IsName(fromHitTransitionName)) return true;
		}
		return false;
	}

	private bool IsInFallReactionAnimation()
	{
		foreach (var fallSN in fallAnimStateNames)
			if (partsDictionary.characterAnimator.GetCurrentAnimatorStateInfo(fallAnimLayerIndex).IsName(fallSN)) return true;
		return false;
	}
	#endregion

	#region visualization
	private void AddHitHistory(string hh)
	{
		GameManager.Log(hh);
		HitHtstoryTexts.Enqueue(hh);
		while (HitHtstoryTexts.Count() > MaxHitHistoryEntries) HitHtstoryTexts.Dequeue();
	}

	private void VisualiseDebug()
	{
		if (debugPosVisualisation != null)
		{
			Vector2 off = balanceWatcher.MassCenterProcessed - balanceWatcher.AnimationMassCenterProcessed;


			var pp = off;
			debugPosVisualisation.anchoredPosition = new Vector2(pp.y * debugMlp, pp.x * debugMlp);
			float sizeD = 2 * actualCMKnockoutDistance * debugMlp;
			debugSafeCurcleVisualisation.sizeDelta = new Vector2(sizeD, sizeD);
			debugSafeCurcleVisualisation.gameObject.SetActive(true);
			float sizeK = 2 * kinematicKnockoutDistance * debugMlp;
			debugKinematicSafeCurcleVisualisation.sizeDelta = new Vector2(sizeK, sizeK);
			debugKinematicSafeCurcleVisualisation.gameObject.SetActive(true);
		}
	}
	#endregion

	#region update stats
	void UpdateKnockoutDistances()
	{
		float aimCMKnockoutDistance = haveGround() ? centerMassKnockoutDistance : inAirCMKnockoutDistance;
		if (aimCMKnockoutDistance > actualCMKnockoutDistance) actualCMKnockoutDistance += cmKnockoutDistanceSpeedUp * Time.deltaTime;
		else if (aimCMKnockoutDistance < actualCMKnockoutDistance) actualCMKnockoutDistance -= cmKnockoutDistanceSpeedDown * Time.deltaTime;
	}

	void UpdateMirrorActions()
	{
		if (!partsDictionary.characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(actionStateName) &&
			!partsDictionary.characterAnimator.GetAnimatorTransitionInfo(0).IsName(toActionTransitionName))
		{
			partsDictionary.characterAnimator.SetBool(mirrorActionAnimProp, balanceWatcher.LeadLeg == LeftRight.Right);
		}
	}

	private void UpdateRagdollness()
	{
		partsDictionary.puppetMaster.internalCollisions = puppetBehaviour.state == RootMotion.Dynamics.BehaviourPuppet.State.Unpinned;
	}

	private void FreezeConvexHull()
	{
		balanceWatcher.FreezeConvexHull = !IsKinematic && GetGeneralOffsetFromAnimation() > actualCMKnockoutDistance;
	}

	private void UpdateAnimatorData()
	{
		partsDictionary.characterAnimator.SetBool(isProneAnimProp, puppetBehaviour.IsProne());
	}

	private void UpdateMusclePowers()
	{
		if (muscleWeightAim > partsDictionary.puppetMaster.muscleWeight)
		{
			float stepUp = muscleWeightUpSpeed * Time.deltaTime;
			if (stepUp > muscleWeightAim - partsDictionary.puppetMaster.muscleWeight) partsDictionary.puppetMaster.muscleWeight = muscleWeightAim;
			else partsDictionary.puppetMaster.muscleWeight += stepUp;
		}
		else if (muscleWeightAim < partsDictionary.puppetMaster.muscleWeight)
		{
			float stepDown = muscleWeightDownSpeed * Time.deltaTime;
			if (stepDown > partsDictionary.puppetMaster.muscleWeight - muscleWeightAim) partsDictionary.puppetMaster.muscleWeight = muscleWeightAim;
			else partsDictionary.puppetMaster.muscleWeight -= stepDown;
		}
	}
	#endregion

	#region first-class hit reaction
	// возвращает true - инициируем падение
	private bool DecideWhetherToFall()
	{
		if (IsKinematic)
		{
			if (GetGeneralOffsetFromAnimation() > kinematicKnockoutDistance) return true;
			else return false;
		}
		if (puppetBehaviour.state == RootMotion.Dynamics.BehaviourPuppet.State.Unpinned) return false;
		if (haveGround())
		{
			return puppetBehaviour.state != RootMotion.Dynamics.BehaviourPuppet.State.Unpinned
				&& GetGeneralOffsetFromAnimation() > actualCMKnockoutDistance
				&& (balanceWatcher.MinOnLine == 0 || balanceWatcher.MaxOnLine == 0 || !haveGround() ||
					futureMassCenterOnLineToPercents() > denyToSmallHit);
		}
		else
		{
			return puppetBehaviour.state != RootMotion.Dynamics.BehaviourPuppet.State.Unpinned
				&& GetGeneralOffsetFromAnimation() > actualCMKnockoutDistance;
		}
	}

	private void HandleKnockout()
	{
		if (!IsInHitReactionAnimation() && !IsInFallReactionAnimation())
		{
			InitHitBasedOnCM();
		}
	}

	private void InitHitBasedOnCM()
	{
		if (Time.time - lastReactionTime > hitRegistrationCooldown
			&& !Helpers.IsOneOfTheAnimations(partsDictionary.characterAnimator, noHitAnims, 0))
		{
			if (haveGround())
			{
				// получить направление
				Vector2 cmOffVec = balanceWatcher.MassCenterProcessed - balanceWatcher.AnimationMassCenterProcessed;
				Vector2 forward = balanceWatcher.ForwardProcessed;
				float angle = Vector2.SignedAngle(forward, cmOffVec);
				float dir = Angle2Direction(angle);

				// получить проценты
				int percents = futureMassCenterOnLineToPercents();
				int currentPercents = currentMassCenterOnLineToPercents();

				// получить координаты
				//Vector3 mostHitBPCoord = balanceWatcher.GetBPCoordWithMaxOffset();
				TimedMaxOffset maxOffset = balanceWatcher.MaxSavedOffset;
				Vector3 mostHitBPCoord = maxOffset.maxOffsetBPCoord;
				Vector2 pos = balanceWatcher.ProcessPointVertically(mostHitBPCoord);
				Vector2 xyPos = preparePosToAnimator(pos);
				float xPos = xyPos.x;
				float yPos = xyPos.y;

				// передать всё в аниматор
				if (puppetBehaviour.state == RootMotion.Dynamics.BehaviourPuppet.State.GetUp)
				{
					if (percents > denyToSmallHit)
					{
						SwitchToRagdoll($"Hit While Get Up | {percents}%");
					}
				}
				else if (isWaitingForRagdoll)
				{
					// отдельная функция в FixedUpdate
				}
				else if (percents > fallToInstantRagdoll)
				{
					isWaitingForRagdoll = true;
					prevCMOffset = (balanceWatcher.MassCenter - balanceWatcher.AnimMassCenter).magnitude;
					prevCMOffsetChange = 0;
					Debug.Log($"RagdollAwaited | {percents}");
					AddHitHistory($"<color=#ff88ffff>Ragdoll awaited | {percents}%</color>");
				}
				else if (percents < strongHitToFall)
				{
					float percentsToAnimator = percents < smallToStrongHit ?
						Mathf.Lerp(0, smallToStrongHitAnimator, Mathf.InverseLerp(denyToSmallHit, smallToStrongHit, percents)) :
						Mathf.Lerp(smallToStrongHitAnimator, 1, Mathf.InverseLerp(smallToStrongHit, strongHitToFall, percents));

					partsDictionary.characterAnimator.SetFloat(directionAnimProp, dir);
					partsDictionary.characterAnimator.SetFloat(xPosAnimProp, xPos);
					partsDictionary.characterAnimator.SetFloat(yPosAnimProp, yPos);
					partsDictionary.characterAnimator.SetFloat(forceAnimProp, percentsToAnimator);
					partsDictionary.characterAnimator.SetTrigger(startHitAnimProp);
					for (int i = 0; i < partsDictionary.puppetMaster.muscles.Count(); i++) puppetBehaviour.UnPin(i, hitUnpinPower);
					Debug.Log($"Hit Processed: dir={f2s(dir)}; xPos={f2s(xPos)}; yPos={f2s(yPos)}; percents={percents}; percentsToAnimator={f2s(percentsToAnimator)}; angle={f2s(angle)}; bodyPart={maxOffset.maxOffBPName}");
					if (percents < smallToStrongHit)
					{
						AddHitHistory($"<color=yellow>Hit small | {percents}%</color>");
					}
					else
					{
						AddHitHistory($"<color=orange>Hit strong | {percents}%</color>");
					}
				}
				else
				{
					FallDirection fallDir = Angle2FallDir(angle);
					partsDictionary.characterAnimator.SetInteger(fallDirectionAnimProp, (int)fallDir);
					partsDictionary.characterAnimator.SetTrigger(startFallAnimProp);
					LosePinsEvent();
					forceFallCheckFrames = 1; // если более одно, то придётся чистить триггер
					Debug.Log($"Fall Processed: dir={fallDir}; (int)dir={(int)fallDir}; percents={percents}; angle={f2s(angle)}");
					AddHitHistory($"<color=red>Fall anim | {percents}%</color>");
				}
				lastReactionTime = Time.time;
			}
			else
			{
				SwitchToRagdoll("Hit In Air");
			}
		}
	}
	#endregion

	#region supporting hit methods
	private void CheckWaitForRagdoll()
	{
		if (isWaitingForRagdoll)
		{
			float cmOffset = (balanceWatcher.MassCenter - balanceWatcher.AnimMassCenter).magnitude;
			float cmOffsetChange = cmOffset - prevCMOffsetChange;

			if (cmOffsetChange < prevCMOffsetChange) 
			{
				partsDictionary.characterAnimator.SetTrigger(inHitFallAnimProp);
				SwitchToRagdoll($"Ragdoll Got!", true);
				isWaitingForRagdoll = false;
			}
			prevCMOffset = cmOffset;
			prevCMOffsetChange = cmOffsetChange;
		}
	}

	

	private void InHitCheckFall()
	{
		if (IsInHitReactionAnimation() && puppetBehaviour.state != RootMotion.Dynamics.BehaviourPuppet.State.Unpinned || forceFallCheckFrames > 0)
		{
			if (forceFallCheckFrames > 0) forceFallCheckFrames--;
			if (Get3DOffsetFromAnimation() > secondHitCheckTreshold || balanceWatcher.MaxPointsOffsetFromAnimation > secondHitCheckTreshold)
			{
				string source = Get3DOffsetFromAnimation() > balanceWatcher.MaxPointsOffsetFromAnimation ? "MassCenter" : balanceWatcher.PartWithTheMaxOffsetFromAnimation;
				float distance = Mathf.Max(Get3DOffsetFromAnimation(), balanceWatcher.MaxPointsOffsetFromAnimation);
				float treshold = secondHitCheckTreshold;
				partsDictionary.characterAnimator.SetTrigger(inHitFallAnimProp);
				SwitchToRagdoll($"Too far from hit anim | {source} {distance} {treshold}");
			}
		}
	}

	private void UpdateGetUpDirection()
	{
		if (useGetUpAim)
		{
			float angle = Vector2.SignedAngle(v3t2(partsDictionary.characterTransform.forward), v3t2(partsDictionary.getUpAim.position) - v3t2(partsDictionary.characterTransform.position));
			float dir = Angle2Direction(angle);
			partsDictionary.characterAnimator.SetFloat(getUpDirectionAnimProp, dir);
			Debug.Log($"GETUP dir={dir}; angle={angle}");
		}
	}

	private void CheckLostHitTrigger()
	{
		if (partsDictionary.characterAnimator.GetBool(startHitAnimProp))
		{
			hitTriggerFrames++;
			if (hitTriggerFrames > hitTriggerCriticalFrames)
			{
				partsDictionary.characterAnimator.ResetTrigger(startHitAnimProp);
			}
		}
		else
		{
			hitTriggerFrames = 0;
		}
	}

	// Это для интерфейса Юнити
	public void SwitchToRagdoll(string comment)
	{
		SwitchToRagdoll(comment, false);
	}

	public void SwitchToRagdoll(string comment, bool useIniRagSpeeds = false)
	{
		if (useIniRagSpeeds)
		{
			muscleWeightUpSpeed = muscleIniRagUpSpeed;
			muscleWeightDownSpeed = muscleIniRagDownSpeed;
		}
		else
		{
			muscleWeightUpSpeed = muscleUpSpeed;
			muscleWeightDownSpeed = muscleDownSpeed;
		}
		isWaitingForRagdoll = false;
		puppetBehaviour.SetState(RootMotion.Dynamics.BehaviourPuppet.State.Unpinned);
		muscleWeightAim = unpinnedMusclesValue;
		Debug.Log($"InstantRagdoll comment={comment}");
		AddHitHistory($"<color=magenta>Fall ragdoll | {comment}</color>");
	}
	#endregion
}
