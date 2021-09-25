using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyCharacterController : MonoBehaviour
{
	public Animator animator;
	public string forward;
	public string turn;

	private void Update()
	{
		animator.SetFloat(forward, Input.GetAxis("Vertical"));
		animator.SetFloat(turn, Input.GetAxis("Horizontal"));
	}
}
