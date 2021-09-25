using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimatorParameterType
{
	Float,
	Int,
	Bool,
	Trigger
}

public enum AnimatorType
{
	MainSceneGeneral,
	LoadedSceneGeneral,
	MainCharacterGeneral,
	Custom
}

[Serializable]
public class AnimatorSmartLink
{
	public AnimatorType AnimatorType;
	public Animator CustomAnimator;
	public override string ToString()
	{
		return AnimatorType.ToString();
	}
}

/// <summary>
/// Меняет параметры аниматора при вызове функциии Influence
/// </summary>
public class AnimatorInfluencer : MonoBehaviour
{
	public List<AnimatorSmartLink> Animators = new List<AnimatorSmartLink>();
	public AnimatorParameterType ParameterType;
	public string ParameterName;
	public float FloatValue;
	public int IntValue;
	public bool BoolValue;
	public bool TriggerValue = true;

    public void Influence()
	{
		foreach (var animatorLink in Animators)
		{
			Animator animator = null;
			switch (animatorLink.AnimatorType)
			{
				case AnimatorType.MainSceneGeneral:
					var mroot = GameObject.FindGameObjectWithTag("MainSceneRoot");
					if (mroot)
						animator = mroot.GetComponent<Animator>();
					break;
				case AnimatorType.LoadedSceneGeneral:
					var lroot = GameObject.FindGameObjectWithTag("LoadedSceneRoot");
					if (lroot)
						animator = lroot.GetComponent<Animator>();
					break;
				case AnimatorType.MainCharacterGeneral:
					animator = GameManager.GM.character.GetComponent<Animator>();
					break;
				case AnimatorType.Custom:
					animator = animatorLink.CustomAnimator;
					break;
			}

			if (animator != null)
			{
				switch (ParameterType)
				{
					case AnimatorParameterType.Float:
						animator.SetFloat(ParameterName, FloatValue);
						break;
					case AnimatorParameterType.Int:
						animator.SetInteger(ParameterName, IntValue);
						break;
					case AnimatorParameterType.Bool:
						animator.SetBool(ParameterName, BoolValue);
						break;
					case AnimatorParameterType.Trigger:
						if (TriggerValue)
							animator.SetTrigger(ParameterName);
						else
							animator.ResetTrigger(ParameterName);
						break;
				}
			}
			else
			{
				Debug.LogError($"Не смог найти аниматор по указанным настройкам: Type = {animatorLink.AnimatorType}, Custom = {animatorLink.CustomAnimator}");
			}
		}
	}
}
