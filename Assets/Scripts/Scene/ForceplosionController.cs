using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceplosionController : MonoBehaviour
{
	public float explosionRadius;
	public float explosionForce;
	public LayerMask explosedLayers;

	private void Start()
	{
		Vector3 explosionPos = transform.position;
		Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius, explosedLayers);
		string debug = "";
		foreach (Collider hit in colliders)
		{
			debug += $"{hit.gameObject.name} ";
			var mcb = hit.GetComponent<RootMotion.Dynamics.MuscleCollisionBroadcaster>();

			if (mcb != null) // надо либо через честные коллайдеры, либо через функцию хита ПМ, иначе пины не отпинываются
			{
				mcb.Hit(explosionForce, (mcb.transform.position - explosionPos).normalized * explosionForce, mcb.transform.position);
				//rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius, 0, ForceMode.Impulse); //, 3.0F);
			}
		}
		Debug.Log(debug);
	}
}
