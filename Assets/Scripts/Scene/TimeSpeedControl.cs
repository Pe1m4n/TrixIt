using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Этот компонент позволяет замедлять скорость течения времени
/// </summary>
public class TimeSpeedControl : MonoBehaviour
{
	public UnityEngine.UI.Slider timeSlider;
	public string pauseBtn;
	public RootMotion.CameraController characterCamera;

	private float onPauseVal = 0;
	private bool uiIsUpdating = false;

	private void Update()
	{
		if (timeSlider != null && Input.GetButtonDown(pauseBtn))
		{
			var t = onPauseVal;
			onPauseVal = Time.timeScale;
			ChangeTimeSpeedAndUpdateUI(t);
		}
	}

	public void ChangeTimeSpeedFromUI(float timeSpeed)
	{
		if (uiIsUpdating == false)
		{
			SetTimeScale(timeSpeed);
			onPauseVal = 0;
		}
	}

	private void ChangeTimeSpeedAndUpdateUI(float timeSpeed)
	{
		uiIsUpdating = true;
		SetTimeScale(timeSpeed);
		if (timeSlider != null) timeSlider.value = timeSpeed;
		uiIsUpdating = false;
	}

	private void SetTimeScale(float val)
	{
		Time.timeScale = val;
		if (val / Time.fixedDeltaTime < 25)
		{
			characterCamera.updateMode = RootMotion.CameraController.UpdateMode.LateUpdate;
		}
		else
		{
			characterCamera.updateMode = RootMotion.CameraController.UpdateMode.FixedLateUpdate;
		}
	}
}
