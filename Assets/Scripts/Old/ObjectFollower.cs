using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFollower : MonoBehaviour
{
    public Transform aim;
    private float yAngleOffset;
    // Start is called before the first frame update
    void Start()
    {
        if ((aim.transform.position - transform.position).magnitude > 0.001)
		{
            Debug.LogError("Aim position must be the same as follower position. Other cases aren't implemented yet.");
            aim = null;
		}
        yAngleOffset = transform.rotation.eulerAngles.y - aim.rotation.eulerAngles.y;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = aim.position;
        transform.eulerAngles = aim.eulerAngles + new Vector3(0, yAngleOffset, 0);
    }
}
