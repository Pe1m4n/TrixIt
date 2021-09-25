using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Добавлять к трансформу, прикреплённому к коллайдеру.
/// При обнаружении "под собой" на коротком расстоянии другого коллайдера создаёт джоинт, ограничивающий скольжение по горизонтали.
/// В идеале, должно работать как шипы у футболистов, не давая подошве скользить.
/// 
/// Может быть, переименовать из липкой точки в шипастую?
/// </summary>
public class StickyPoint : MonoBehaviour
{
    public Rigidbody parent;
    public float length;
	private ConfigurableJoint joint = null;

	private void FixedUpdate()
	{
		if (joint == null)
		{
			Ray ray = new Ray(transform.position, transform.forward);
			RaycastHit hit;
			Debug.DrawRay(ray.origin, ray.direction * length, Color.magenta);
			if (Physics.Raycast(ray, out hit, length))
			{
				joint = parent.gameObject.AddComponent<ConfigurableJoint>();
				joint.connectedAnchor = hit.point;
				joint.yMotion = ConfigurableJointMotion.Locked; // уточнить, должна быть ось, которой мы толкаемся
			}
		}
		else
		{
			if((joint.connectedAnchor - transform.position).magnitude > length)
			{
				Destroy(joint);
				joint = null;
			}
		}
	}
}
