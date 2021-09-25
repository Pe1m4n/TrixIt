using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Скрипт для подгрузки GM из любого скрипта.
/// Только этим он и занимается.
/// </summary>
public class SceneLoader : MonoBehaviour
{
	[HideInInspector]
	public string LoadMe;
	public string MainSceneName = "MainScene";

	private void Start()
	{
		var gms = FindObjectsOfType(typeof(GameManager));
		if (gms.Length == 0)
		{
			LoadMe = SceneManager.GetActiveScene().name;
			DontDestroyOnLoad(gameObject);
			SceneManager.LoadScene(MainSceneName);
		}
	}
}
