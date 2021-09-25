using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform PointA;
    public Transform PointB;
    public Rigidbody Platform;

    public float PauseA;
    public float PauseB;
    public float PathTime;

    public AnimationCurve Path;

    private float startTime;
    private StaticFriction staticFriction;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
        staticFriction = Platform.GetComponent<StaticFriction>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float lifetime = Time.time - startTime;
        float phasetime = lifetime - Mathf.Floor(lifetime / (PauseA + PauseB + 2 * PathTime)) * (PauseA + PauseB + 2 * PathTime);

        Vector3 newPos;
        if (phasetime < PauseA) newPos = PointA.position;
        else if (phasetime < PauseA + PathTime) newPos = PointA.position + (PointB.position - PointA.position) * Path.Evaluate((phasetime - PauseA) / PathTime);
        else if (phasetime < PauseA + PathTime + PauseB) newPos = PointB.position;
        else newPos = PointA.position + (PointB.position - PointA.position) * Path.Evaluate(1 - (phasetime - (PauseA + PathTime + PauseB)) / PathTime);

        Vector3 offset = newPos - Platform.position;
        offset.y = 0;
        Platform.MovePosition(newPos);
        foreach (var fc in staticFriction.frictionedCharacters)
        {
            fc.MovePosition(fc.position + offset);
		}
        staticFriction.frictionedCharacters.Clear();
    }
}
