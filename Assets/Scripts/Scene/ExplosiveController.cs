using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExplosionType
{
	enlarging = 0,
	forceBased = 1
}

public class ExplosiveController : MonoBehaviour
{
	public ExplosionController explosionPrefab;
	public ForceplosionController forceplosionPrefab;
	public ExplosionType explosionType;
	public float explosionDiameter;
	public float explosionSpeed;
	public float explosionForce;

	private void OnTriggerEnter(Collider other)
	{
		switch (explosionType)
		{
			case ExplosionType.enlarging:
				var explosion = Instantiate(explosionPrefab);
				explosion.transform.position = transform.position;
				explosion.transform.localScale = new Vector3(0, 0, 0);
				explosion.explosionSpeed = explosionSpeed;
				explosion.maxDiameter = explosionDiameter;
				Destroy(gameObject);
				break;
			case ExplosionType.forceBased:
				var forceplosion = Instantiate(forceplosionPrefab);
				forceplosion.transform.position = transform.position;
				forceplosion.transform.localScale = (explosionDiameter) * Vector3.one;
				forceplosion.explosionRadius = explosionDiameter / 2;
				forceplosion.explosionForce = explosionForce;
				Destroy(gameObject);
				break;
			default:
				Debug.LogError("ExplosiveController: Неизвестный тип взрыва");
				break;
		}
	}
}
