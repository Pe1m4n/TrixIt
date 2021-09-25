using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Этот компонент позволяет изменять скорость течения времени
/// Управляется из инспектора
/// </summary>
public class TimeSpeedControlInspectorVer : MonoBehaviour
{
	public float timeScale = 1;

	private void Update()
	{
		if (timeScale != Time.timeScale) Time.timeScale = timeScale;
	}
}
