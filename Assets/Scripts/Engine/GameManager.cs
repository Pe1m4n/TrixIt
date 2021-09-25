using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// Класс, содержащий базовую функциональность GameManager - открытие сцен, логгирование, и прочее
/// </summary>
public class GameManager : MonoBehaviour
{
	#region static
	private static GameManager gm;
	public static GameManager GM { get => gm; }

	public static void Log(string log)
	{
		Debug.Log(log);
		{
			if (GM.LogTextUI != null)
			{
				GM.LogTextUI.text += "\n" + log;
				var s = GM.LogTextUI.text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				if (s.Length > 250)
				{
					GM.LogTextUI.text = string.Concat(s.Skip(s.Length - 250).Select(x => "\n" + x));
				}
			}
		}
	}

	public static void FixUpdLog(string log)
	{
		if (GM) GM.FixUpdLogText += log + "\n";
	}

	public static void UpdLog(string log)
	{
		if (GM) GM.UpdLogText += log + "\n";
	}
	#endregion

	#region settings
	[RootMotion.LargeHeader("Links")]
	public Transform puppetRoot;
	public Transform character;
	public RootMotion.Dynamics.BehaviourPuppet puppet;
	public List<string> levels;
	public string defaultLevel;

	[RootMotion.LargeHeader("Configs")]
	public LayerMask layersToStandOn;

	[RootMotion.LargeHeader("UI")]
	public Dropdown levelDropdown;
	public Text LogTextUI;
	public Text ConstantLogTextUI;
	#endregion

	#region fields
	private string currentLevel = "";
	private string FixUpdLogText = "";
	private string UpdLogText = "";
	private readonly Dictionary<GameObject, BalanceWatcher> correspondingBWs = new Dictionary<GameObject, BalanceWatcher>();
	#endregion

	#region unity
	private void Awake()
	{
		if (gm != null)
		{
			enabled = false;
			throw new Exception("GameManager: Уже существует один GameManager!");
		}
		gm = this;

		if (!levels.Any(l => l == defaultLevel))
		{
			Debug.LogError("Game Manager: Дефолтный левел отсутствует, вместо него будет использован первый уровень");
			defaultLevel = levels[0];
		}
	}

	private void Start()
	{
		var sls = (SceneLoader[])FindObjectsOfType(typeof(SceneLoader));
		if (sls.Length > 1) Debug.LogError("Game Manger: необходимо загрузить сразу несколько сцен - будет загружена только одна");
		if (sls.Length > 0) defaultLevel = sls[0].LoadMe;

		var levelOptions = new List<Dropdown.OptionData>();
		foreach (var l in levels)
		{
			levelOptions.Add(new Dropdown.OptionData(l));
		}
		if (levelDropdown != null)
		{
			levelDropdown.AddOptions(levelOptions);
			levelDropdown.value = levels.FindIndex(l => l == defaultLevel);
			levelDropdown.onValueChanged.AddListener(OnUIChangeLevel);
		}
		SetLevel(defaultLevel);

		foreach (var bp in puppetRoot.gameObject.GetComponentsInChildren<Collider>())
		{
			correspondingBWs[bp.gameObject] = puppetRoot.gameObject.GetComponentInChildren<BalanceWatcher>();
		}
	}

	private void FixedUpdate()
	{
		//PrevFixUpdLogText = FixUpdLogText;
		FixUpdLogText = "";
	}

	private void Update()
	{
		if (ConstantLogTextUI != null) ConstantLogTextUI.text = FixUpdLogText + "\n" + UpdLogText;
		UpdLogText = "";
	}
	#endregion

	#region events
	public void OnUILogToggle(Toggle t) // TODO: подумать о переносе
	{
		if (LogTextUI != null)
		{
			LogTextUI.gameObject.SetActive(t.isOn);
			ConstantLogTextUI.gameObject.SetActive(t.isOn);
		}
	}

	public void OnClearLog() // TODO: подумать о переносе
	{
		if (LogTextUI != null) LogTextUI.text = "";
	}

	public void OnUIChangeLevel(int lvlNum) // TODO: подумать о переносе
	{
		SetLevel(levels[lvlNum]);
	}
	#endregion

	#region helpers
	/// <summary>
	/// Возвращает BalanceWatcher для этой части тела.
	/// Ели это не часть тела, возвращает null.
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public BalanceWatcher GetPartCorrespondingBW(GameObject gameObject)
	{
		if (correspondingBWs.ContainsKey(gameObject))
		{
			return correspondingBWs[gameObject];
		}
		else
		{
			return null;
		}
	}

	public void GetReportOnPuppetCreation(GameObject newPuppet)
	{
		foreach (var bp in newPuppet.GetComponentsInChildren<Collider>())
		{
			correspondingBWs[bp.gameObject] = newPuppet.GetComponentInChildren<BalanceWatcher>();
		}
	}
	public void Restart()
	{
		var scene = SceneManager.GetActiveScene();
		SceneManager.LoadScene(scene.name);
	}

	public void PutCharacterOnSurface(Vector3 pos, Quaternion rot, LayerMask surface) // TODO: переделать в хэлпер
	{
		Vector3 realPos = pos;
		if (Physics.Raycast(new Ray(pos + Vector3.up * 2000, Vector3.down), out RaycastHit hit, 4000, surface))
		{
			realPos = hit.point;
		}
		puppet.puppetMaster.Teleport(realPos, rot, true);
	}
	#endregion

	#region privte methods
	private void SetLevel(string level)
	{
		if (currentLevel != "")
		{
			puppetRoot.gameObject.SetActive(false);
			SceneManager.UnloadSceneAsync(currentLevel);
		}

		currentLevel = level;
		SceneManager.LoadSceneAsync(level, LoadSceneMode.Additive).completed += LevelLoaded;

	}

	private void LevelLoaded(AsyncOperation obj)
	{
		if (!puppetRoot.gameObject.activeInHierarchy) puppetRoot.gameObject.SetActive(true);

		GameObject customStartPoint = GameObject.FindGameObjectWithTag("LevelStartPoint");
		if (customStartPoint != null) puppet.puppetMaster.Teleport(customStartPoint.transform.position, character.rotation, true);
		else PutCharacterOnSurface(character.position, character.rotation, layersToStandOn);
	}
	#endregion
}
