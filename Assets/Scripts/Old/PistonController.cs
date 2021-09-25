using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistonController : MonoBehaviour
{
    public Rigidbody piston;
    public Animator animator;
    public bool push;

    // Update is called once per frame
    void Update()
    {
        if (push) Push();
    }

    public void Push()
	{
        animator.SetTrigger("Push");
        push = false;
	}
}
