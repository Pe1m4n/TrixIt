using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.Demos;

public class ModifiedUserControlMelee : UserControlMelee
{
	public KeyCode customKey1;
	public KeyCode customKey2;
	public KeyCode customKey3;
	public KeyCode customKey4;

	protected override void Update()
	{
		base.Update();

		state.actionIndex = state.actionIndex == 1 ? 1 :
			Input.GetKey(customKey1) ? 2 :
			Input.GetKey(customKey2) ? 3 :
			Input.GetKey(customKey3) ? 4 :
			Input.GetKey(customKey4) ? 5 : 0;
	}
}
