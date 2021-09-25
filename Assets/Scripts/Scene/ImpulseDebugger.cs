using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpulseDebugger : MonoBehaviour
{
	private void OnCollisionEnter(Collision collision)
	{
		if (collision.impulse.magnitude != 0)
			Debug.Log($"{collision.impulse.magnitude}");
	}

	private void OnCollisionStay(Collision collision)
	{
		if (collision.impulse.magnitude != 0)
			Debug.Log($"{collision.impulse.magnitude}");
	}
}
