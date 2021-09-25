using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionController : MonoBehaviour
{
	public float maxDiameter;
	public float explosionSpeed;
	private float currentDiameter = 0;
	private bool isExploding = true;

	private void FixedUpdate()
	{
		currentDiameter += (isExploding ? 1 : -1) * explosionSpeed * Time.deltaTime;

		if (currentDiameter > maxDiameter)
		{
			isExploding = false;
			currentDiameter = maxDiameter;
		}

		if (currentDiameter < 0)
			Destroy(gameObject);
		else
			transform.localScale = new Vector3(currentDiameter, currentDiameter, currentDiameter);
	}
}
