using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BulletType
{
	standart = 0,
	explosive = 1,
	forceplosive = 2
}

/// <summary>
/// Класс, содержащий функционал для отладки и разработки Trix, не должен применяться в играх
/// </summary>
public class DebugGameManager : MonoBehaviour
{
	#region settings
	[RootMotion.LargeHeader("Links")]
	public Transform puppetRoot;
	public RootMotion.Dynamics.BehaviourPuppet puppet;
	public Animator puppetAnimator;
	public GameObject ball;
	public TrixCharacterController characterController;
	public RootMotion.CameraController cameraController;
	public Transform visTransform;
	public Camera mainCamera;
	public FallingBehaviourControl fallingBehaviourControl;

	[RootMotion.LargeHeader("Prefabs")]
	public GameObject characterPrefab;
	public Rigidbody bulletPrefab;
	public Rigidbody explosivePrefab;

	[RootMotion.LargeHeader("Animator")]
	public string puppetDefailtAnimState;
	public string UseLocomotionAnimProp;
	public string ChangePoseTriggerAnimProp;
	public string PoseNumberAnimProp;
	public string forceAnimProp;
	public string directionAnimProp;
	public string yPosAnimProp;
	public string startHitAnimProp;

	[RootMotion.LargeHeader("Configs")]
	public LayerMask layersToStandOn;

	[RootMotion.LargeHeader("UI")]
	public Slider bulletMassS;
	public Slider bulletSpeedS;
	public Dropdown bulletTypeD;
	public Slider explosionDiameterS;
	public Slider explosionSpeedS;
	public Slider explosionForceS;
	public Toggle bulletDisappearToggle;
	public Toggle useLocomotionToggle;
	public Toggle followingCameraToggle;
	public Toggle ballToggle;
	public Slider ballMassS;
	public Slider ballDiameterS;
	public Text stateText;
	public Text bulletMassT;
	public Text bulletSpeedT;
	public Text explosionDiameterT;
	public Text explosionSpeedT;
	public Text explosionForceT;
	public Text ballMassT;
	public Text ballDiameterT;
	public InputField forceI;
	public InputField directionI;
	public InputField yPosI;

	[RootMotion.LargeHeader("Unsorted")] // TODO: подумать о закрытии
	public float bulletMass;
	public float bulletSpeed;
	public BulletType bulletType;
	public float explosionDiameter;
	public float explosionSpeed;
	public float explosionForce;
	public bool bulletDisappear = false;
	#endregion

	#region fields
	private Vector3 initialPuppetPosition;
	private Quaternion initialPuppetRotation;
	private Vector3 ballSpawnPosition;

	private readonly List<Tuple<GameObject, Transform>> enemies = new List<Tuple<GameObject, Transform>>();
	private bool enemiesSpawned = false;
	private bool enemiesActive = false;

	private bool isUIInited = false;
	#endregion

	#region unity
	private void Awake()
	{
		if (puppetRoot != null)
		{
			initialPuppetPosition = puppetRoot.position;
			initialPuppetRotation = puppetRoot.rotation;
		}

		ballSpawnPosition = ball.transform.position;
	}

	private void Start()
	{
		UpdateBulletUI();
	}

	private void Update()
	{
		UpdateFire();
		UpdateStateText();
	}
	#endregion

	#region events
	public void OnFireHit()
	{
		puppetAnimator.SetFloat(forceAnimProp, Helpers.ParseFloat(forceI.text));
		puppetAnimator.SetFloat(directionAnimProp, Helpers.ParseFloat(directionI.text));
		puppetAnimator.SetFloat(yPosAnimProp, Helpers.ParseFloat(yPosI.text));
		puppetAnimator.SetTrigger(startHitAnimProp);
	}

	public void OnResetSim()
	{
		puppet.SetState(RootMotion.Dynamics.BehaviourPuppet.State.Puppet);
		puppetAnimator.Play(puppetDefailtAnimState, 0);
		GameManager.GM.PutCharacterOnSurface(initialPuppetPosition, initialPuppetRotation, layersToStandOn);

		if (enemiesActive)
		{
			foreach (var e in enemies)
			{
				e.Item1.GetComponentInChildren<RootMotion.Dynamics.PuppetMaster>().Teleport(e.Item2.position, e.Item2.rotation, true);
			}
		}

		if (ball.activeInHierarchy)
		{
			ball.transform.position = ballSpawnPosition;
			ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
			ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
		}
	}

	public void OnUIEvent()
	{
		if (isUIInited)
		{
			bulletMass = Mathf.Round(Mathf.Pow(5, bulletMassS.value));
			bulletSpeed = Mathf.Round(bulletSpeedS.value);
			bulletType = (BulletType)bulletTypeD.value;
			explosionDiameter = explosionDiameterS.value / 10;
			explosionSpeed = Mathf.Round(explosionSpeedS.value * 100) / 100;
			explosionForce = Mathf.Round(explosionForceS.value * 100) / 100;
			bulletDisappear = bulletDisappearToggle.isOn;
			puppetAnimator.SetBool(UseLocomotionAnimProp, useLocomotionToggle.isOn);
			characterController.rotateAfterCam = useLocomotionToggle.isOn;
			cameraController.follow = followingCameraToggle.isOn;
			ball.SetActive(ballToggle.isOn);
			ball.GetComponent<Rigidbody>().mass = ballMassS.value;
			ball.transform.localScale = Vector3.one * ballDiameterS.value;
			UpdateBulletUI();
		}
	}

	public void OnUISpawnEnemies()
	{
		if (enemiesSpawned == false)
		{
			var enemySpawns = GameObject.FindGameObjectsWithTag("EnemySpawn");
			foreach (var es in enemySpawns)
			{
				var enemy = Instantiate(characterPrefab);
				var enemyPM = enemy.GetComponentInChildren<RootMotion.Dynamics.PuppetMaster>();
				enemyPM.Teleport(es.transform.position, es.transform.rotation, true);

				enemy.GetComponentInChildren<TrixCharacterController>().enabled = false;
				enemy.GetComponentInChildren<BalanceWatcher>().visTransform = visTransform;
				enemy.gameObject.SetActive(true);

				GameManager.GM.GetReportOnPuppetCreation(enemy);

				enemies.Add(new Tuple<GameObject, Transform>(enemy, es.transform));
			}
			enemiesActive = true;
			enemiesSpawned = true;
		}
		else
		{
			enemiesActive = !enemiesActive;
			foreach (var e in enemies)
			{
				e.Item1.SetActive(enemiesActive);
			}
		}
	}

	public void OnUIChangePose(int poseNum)
	{
		puppetAnimator.SetFloat(PoseNumberAnimProp, poseNum);
		puppetAnimator.SetTrigger(ChangePoseTriggerAnimProp);
	}
	#endregion

	#region developer functionality
	private void UpdateFire()
	{
		if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

		if (Input.GetButtonDown("Fire1"))
		{
			Rigidbody b = Instantiate(bulletType == BulletType.standart ? bulletPrefab : explosivePrefab);
			b.transform.position = mainCamera.transform.position;
			b.mass = bulletMass;
			// 100% нужен рефактор
			if (bulletType == BulletType.explosive || bulletType == BulletType.forceplosive) b.GetComponent<ExplosiveController>().explosionType = bulletType == BulletType.explosive ? ExplosionType.enlarging : ExplosionType.forceBased;
			if (bulletType == BulletType.explosive || bulletType == BulletType.forceplosive) b.GetComponent<ExplosiveController>().explosionDiameter = explosionDiameter;
			if (bulletType == BulletType.explosive) b.GetComponent<ExplosiveController>().explosionSpeed = explosionSpeed;
			if (bulletType == BulletType.forceplosive) b.GetComponent<ExplosiveController>().explosionForce = explosionForce;
			if (bulletSpeed < 10) b.GetComponent<SelfDestructor>().timeOfMyLife = 10 * b.GetComponent<SelfDestructor>().timeOfMyLife / bulletSpeed;
			if (bulletType != BulletType.standart || !bulletDisappear) Destroy(b.GetComponent<SelfDestructOnHit>());
			b.AddForce(mainCamera.ScreenPointToRay(Input.mousePosition).direction * bulletSpeed, ForceMode.VelocityChange);
		}
	}
	#endregion

	#region UI
	private void UpdateStateText()
	{
		if (puppet != null)
		{
			string t = puppet.state switch
			{
				RootMotion.Dynamics.BehaviourPuppet.State.Puppet => "<color=lime>Puppet</color>",
				RootMotion.Dynamics.BehaviourPuppet.State.Invincible => "<color=yellow>Invincible</color>",
				RootMotion.Dynamics.BehaviourPuppet.State.Unpinned => "<color=red>Unpinned</color>",
				RootMotion.Dynamics.BehaviourPuppet.State.GetUp => "<color=blue>GetUp</color>",
				_ => "",
			};
			t += "\n" + (fallingBehaviourControl.IsKinematic ? "<color=lime>Kinematic</color>" : "<color=red>Dynamic</color>");
			stateText.text = t;
		}
	}

	private void UpdateBulletUI()
	{
		isUIInited = false;
		bulletMassT.text = $"Масса пуль = {bulletMass}";
		bulletSpeedT.text = $"Скорость пуль =  {bulletSpeed}";
		explosionDiameterT.text = $"Диаметр взрыва = {explosionDiameter}";
		explosionSpeedT.text = $"Скорость взрыва = {explosionSpeed}";
		explosionForceT.text = $"Сила взрыва = {explosionForce}";
		switch (bulletType)
		{
			case BulletType.standart:
				bulletMassT.gameObject.SetActive(true);
				bulletMassS.gameObject.SetActive(true);
				explosionDiameterS.gameObject.SetActive(false);
				explosionDiameterT.gameObject.SetActive(false);
				explosionSpeedT.gameObject.SetActive(false);
				explosionSpeedS.gameObject.SetActive(false);
				explosionForceT.gameObject.SetActive(false);
				explosionForceS.gameObject.SetActive(false);
				bulletDisappearToggle.gameObject.SetActive(true);
				break;
			case BulletType.explosive:
				bulletMassT.gameObject.SetActive(false);
				bulletMassS.gameObject.SetActive(false);
				explosionDiameterS.gameObject.SetActive(true);
				explosionDiameterT.gameObject.SetActive(true);
				explosionSpeedT.gameObject.SetActive(true);
				explosionSpeedS.gameObject.SetActive(true);
				explosionForceT.gameObject.SetActive(false);
				explosionForceS.gameObject.SetActive(false);
				bulletDisappearToggle.gameObject.SetActive(false);
				break;
			case BulletType.forceplosive:
				bulletMassT.gameObject.SetActive(false);
				bulletMassS.gameObject.SetActive(false);
				explosionDiameterS.gameObject.SetActive(true);
				explosionDiameterT.gameObject.SetActive(true);
				explosionSpeedT.gameObject.SetActive(false);
				explosionSpeedS.gameObject.SetActive(false);
				explosionForceT.gameObject.SetActive(true);
				explosionForceS.gameObject.SetActive(true);
				bulletDisappearToggle.gameObject.SetActive(false);
				break;
			default:
				Debug.LogError("GameManager: Неизвестный тип снаряда");
				break;
		}
		ballMassT.gameObject.SetActive(ballToggle.isOn);
		ballMassS.gameObject.SetActive(ballToggle.isOn);
		ballDiameterT.gameObject.SetActive(ballToggle.isOn);
		ballDiameterS.gameObject.SetActive(ballToggle.isOn);
		ballMassT.text = $"Масса мяча = {ball.GetComponent<Rigidbody>().mass}";
		ballDiameterT.text = $"Диаметр мяча = {ball.transform.localScale.x}";

		isUIInited = true;
	}
	#endregion
}
