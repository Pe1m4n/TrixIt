using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticFriction : MonoBehaviour
{
    public LayerMask characterLayers;
	public List<Rigidbody> frictionedCharacters = new List<Rigidbody>();

	private void OnTriggerStay(Collider other)
	{
		if ((characterLayers.value & 1 << other.gameObject.layer) != 0 && other.attachedRigidbody != null)
		{
			frictionedCharacters.Add(other.attachedRigidbody);
		}

	}
}
