using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ContactPoint[] - точки контакта
/// float - относительная скорость
/// </summary>
[System.Serializable]
public class ContactSetEvent : UnityEvent<ContactPoint[], float> { }

/// <summary>
/// Класс, ретранслирующий наружу события столкновений
/// </summary>
public class BalanceWatcherColliderBridge : MonoBehaviour
{
	public ContactSetEvent collisionEvent = new ContactSetEvent();

	private void OnCollisionStay(Collision collision)
	{
		ContactPoint[] contacts = new ContactPoint[collision.contactCount];
		collision.GetContacts(contacts);
		collisionEvent.Invoke(contacts, collision.relativeVelocity.magnitude);
		//Debug.Log(collision.relativeVelocity.magnitude);
	}
}
