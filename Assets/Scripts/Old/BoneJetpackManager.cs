using RootMotion.Dynamics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Чтобы автоматически создать сразу много джетпаков и управлять ими
/// </summary>
public class BoneJetpackManager : MonoBehaviour
{
	public bool useAcceleration = false;
	public float power;
	public PuppetMaster target;

	private Dictionary<Muscle, Vector3> prevTargetVelocity = new Dictionary<Muscle, Vector3>();


	private void FixedUpdate()
	{
		foreach (var muscle in target.muscles)
		{
			if (prevTargetVelocity.ContainsKey(muscle))
			{
				Vector3 velocity = muscle.targetVelocity;
				Vector3 acceleration = (velocity - prevTargetVelocity[muscle]) / Time.deltaTime;
				Vector3 jet = useAcceleration ? (acceleration * power) : (velocity * power);
				muscle.rigidbody.AddForceAtPosition(jet, muscle.rigidbody.centerOfMass);
			}
			prevTargetVelocity[muscle] = muscle.targetVelocity;
		}
	}
}
