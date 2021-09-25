using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestructOnHit : MonoBehaviour
{
	public float delay;
	private void OnCollisionEnter(Collision collision)
	{
		Destroy(gameObject, delay);
	}
}
