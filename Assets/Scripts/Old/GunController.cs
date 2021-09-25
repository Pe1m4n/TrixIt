using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public Rigidbody bulletPrefab;
	public float power;

	private void Update()
	{
		if (Input.GetMouseButtonUp(0))
		{
			var b = Instantiate(bulletPrefab);
			b.transform.SetPositionAndRotation(transform.position, transform.rotation);
			b.AddForce(transform.up * power, ForceMode.Impulse);
		}
	}
}
