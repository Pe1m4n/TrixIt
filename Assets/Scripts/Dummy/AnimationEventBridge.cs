using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class AnimationEventBridge : MonoBehaviour
{
	public FallingBehaviourControl fallingBehaviourControl;
	public TrixCharacterController trixCharacterController;

    public void FallEvent()
	{
		fallingBehaviourControl.FallEvent();
	}

	public void LosePins()
	{
		fallingBehaviourControl.LosePinsEvent();
	}

	public void LoseMuscles()
	{
		fallingBehaviourControl.LoseMusclesEvent();
	}

	public void StartLanding()
	{
		trixCharacterController.TryStartLanding();
	}
}
