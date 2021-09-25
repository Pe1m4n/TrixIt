using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Ограничивает максимальную скорость движения тела.
/// </summary>
public class RigidbodyMaxSpeedLimiter : MonoBehaviour
{
    public float maxSpeed = float.PositiveInfinity;
	private new Rigidbody rigidbody = null;

	private void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		if (rigidbody != null && rigidbody.velocity.magnitude > maxSpeed)
		{
			rigidbody.velocity = rigidbody.velocity.normalized * maxSpeed;
		}
	}
}
