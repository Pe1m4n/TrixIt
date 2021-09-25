using RootMotion.Dynamics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reanimator : MonoBehaviour
{
	public BehaviourPuppet puppet;

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.A))
		{
			puppet.SetState(BehaviourPuppet.State.Puppet);
		}
	}
}
