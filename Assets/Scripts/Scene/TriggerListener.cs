using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Вызывает событие, если в триггер кто-то вошёл
/// </summary>
public class TriggerListener : MonoBehaviour
{
    public LayerMask allowedLayers;
    public UnityEvent enterTriggerEvent;
    public float cooldown;

    private float lastEventTime = 0;

    private void OnTriggerEnter(Collider other)
	{
        if ((allowedLayers.value & 1 << other.gameObject.layer) != 0 && Time.time - lastEventTime > cooldown)
        {
            lastEventTime = Time.time;
            enterTriggerEvent.Invoke();
        }
    }
}
