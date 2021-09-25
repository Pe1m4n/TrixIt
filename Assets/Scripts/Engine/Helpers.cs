using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers
{
	public static bool IsOneOfTheAnimations(Animator animator, string[] animations, int layer)
	{
		var state = animator.GetCurrentAnimatorStateInfo(layer);
		foreach (var anim in animations)
		{
			if (state.IsName(anim)) return true;
		}
		return false;
	}

	public static float ParseFloat(string s)
	{
		float f;
		if (float.TryParse(s, out f)) return f;
		if (float.TryParse(s.Replace('.', ','), out f)) return f;
		if (float.TryParse(s.Replace(',', '.'), out f)) return f;
		return float.Parse(s);
	}
}
