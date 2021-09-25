using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управление персонажем посредством кнопок!
/// </summary>
public class PlayerController : MonoBehaviour
{
    public Animator animator;

    public string InpHor;
    public string AnimHor;
    public string InpVer;
    public string AnimVer;
    public float speed = 1;

	private void Update()
	{
        animator.SetFloat(AnimHor, speed * Input.GetAxis(InpHor));
        animator.SetFloat(AnimVer, speed * Input.GetAxis(InpVer));
	}
}
