using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestructor : MonoBehaviour
{
    public float timeOfMyLife;
	private float birthTime;

	private void Start()
	{
		birthTime = Time.time;
	}

	private void Update()
	{
		if (Time.time - birthTime > timeOfMyLife) Destroy(gameObject);
	}
}
