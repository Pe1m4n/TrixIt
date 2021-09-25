using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassScaler : MonoBehaviour
{
	float actualMassScale;
    public float massScale = 1;

	Dictionary<Rigidbody, float> RigiMasses = new Dictionary<Rigidbody, float>();

    public GameObject rootObject;

	private void Awake()
	{
		var rigidbodies = rootObject.GetComponentsInChildren<Rigidbody>();
		foreach(var rb in rigidbodies)
		{
			RigiMasses[rb] = rb.mass;
		}
		actualMassScale = massScale + 1;
	}

	private void Update()
	{
		if (massScale != actualMassScale)
		{
			foreach(var r in RigiMasses)
			{
				r.Key.mass = r.Value * massScale;
			}
		}
		actualMassScale = massScale;
	}
}
