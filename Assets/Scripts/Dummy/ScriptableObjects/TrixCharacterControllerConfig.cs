using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrixCharacterControllerConfig", menuName = "TRIX/CharacterControllerConfig")]
public class TrixCharacterControllerConfig : ScriptableObject
{
	[RootMotion.LargeHeader("Jumps")]
	public LayerMask groundLayers;
	public float animFulcrumRagdollTreshold; // Допустмое отклонение точек опоры от земли
	public string[] animsToAutojumpFrom; // анимации, из которых мы спрыгиваем
	public float autoLandingSpeedThreshold; // скорость приземления, выше которой мы спрыгиваем (ниже неё падаем)
	public float jumpRotationLimit;
	public float jumpOffForwardDetectionOffset; // оффсет вперёд второго рейкаста вниз
	public int amountOfIntervalsToCalcSlopeAngle = 10;
	public float thresholdTimeToFallFromWallHit;
	public float thresholdSpeedToFallFromWallHit;
	public float thresholdSlopeAngleToFallFromWallHit = 45;
	public bool logWallHitSuccess;
	public bool logWallHitFail;
	public float WallHitParabolaHeightOffset;


	[RootMotion.LargeHeader("Animator")]
	public string actionAnimProp;
	public string startJumpAnimProp;
	public string inJumpAnimProp;
	public string boringAnimProp;
	public string boringAnimName;
	public string boringIdAnimProp;
	public string idleAnimName;
	public string freeLandingAnimProp;
	public string fulcrumLossCustomHandlingAnimProp;
	public string startHeightFallAnimProp;
	public string jumpOffForwardDetectionOffsetAnimProp;
	public string jumpOffDetectionAnimProp;
	public string estimatedLandspeedAnimProp;
	public string estimatedTimeToLandAnimProp;
	public string slopeAngleAnimProp;
	public string[] startRunAnimNames;


	public int animLayer = 0;
	public List<string> jumpAnimNames;
	public List<string> animsToDisableJumpInput; // в этих анимациях сбрасывается прыжок
	public List<string> rmOnlyOnGroundAnims;
	public List<string> neverRmAnims;
	public List<string> doNotAffectRm;

	public string timeToLandingAnimProp; // TODO: перенести сюда остальные части автоспрыга

	[RootMotion.LargeHeader("Spine Animator")]
	public string stickIntensiveAnimProp;
	public string animProgressAnimProp;
	public string freemovementAnimProp;
	public string fastRunAnimProp;
	public string angleForStartRunAnimProp;
	public string horizontalAnimProp;
	public string verticalAnimProp;
	public float horizontalMlp;

	[RootMotion.LargeHeader("Input")]
	public string horAxis;
	public string verAxis;
	public string jumpBtn;
	public string actionBtn;
	public string sprintBtn;
	public string crouchBtn;
	public string getPropBtn;
	public string dropPropBtn;

	[RootMotion.LargeHeader("Locomotion")]
	public AnimationTurnParams[] turnSpeeds2;
	public AnimationTurnParams[] turnToCamSpeeds;
	public float runForwardUpdSpeed2 = 1;
	public float runTurnUpdSpeed2 = 1;
	public float crouchForwardUpdSpeed2 = 1;
	public float crouchTurnUpdSpeed2 = 1;
	public float strafeUpdSpeed2 = 1;
	public float boringTime = 5;
	public int boringVariantsNum;

	[RootMotion.LargeHeader("Camera")]
	public float camFolowTimeout;
	public string[] animsForCamToFollow;
	public float camOffsetBeforeFollow;
	public AnimationAmalgamation[] animsToRotateWithLimits;

	[RootMotion.LargeHeader("Offsets")]
	public OffsetInfo[] animationOffsets;
}

[System.Serializable]
public struct OffsetInfo
{
	public string animBefore;
	public string animAfter;
	public Vector3 offset;
}

[System.Serializable]
public struct AnimationAmalgamation
{
	public string[] anims;
	public float maximumRotationAngle;
}