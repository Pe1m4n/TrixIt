using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using UnityEditor;

public class AnimationSettingsSystem : MonoBehaviour
{
	public AnimationSettingsSystemConfig Config;
	
	private static AnimationSettingsSystem ass = null;
	public static AnimationSettingsSystem ASS { get => ass; }

	private bool settingsCacheIsUpToDate = false;
	private readonly Dictionary<string, AnimationSettings> settingsCache = new Dictionary<string, AnimationSettings>();

#if(UNITY_EDITOR)
	[MenuItem("Customs/Update Animation Settings")]
	static void UpdateAssets()
	{
		ASS.settingsCacheIsUpToDate = false;
	}
#endif

	private void Awake()
	{
		if (ass != null)
		{
			Debug.LogError("There already is an instance of ASS. Self-destructing...");
			Destroy(gameObject);
		}
		else
		{
			ass = this;
		}
	}

	/// <summary>
	/// На данный момент готово:
	/// Func_self.Fulcrums
	/// Func_self.pmBooster
	/// Func_coll.physBoost
	/// Func_air
	/// Func_self.CapsuleHeightScale
	/// Func_self.CapsuleWidthScale
	/// 
	/// Отдаёт полностью собранный сеттинг для текущего состояния аниматора
	/// Предполагаем, что все клипы в BlendTree воспроизводятся одинаково
	/// </summary>
	/// <param name="animator">Целевой аниматор</param>
	/// <param name="layer">Целевой слой аниматора</param>
	/// <returns>Собранный сеттинг</returns>
	public AnimationInterval GetCompiledSettings(Animator animator, int layer)
	{
		// первоначальный прогон
		Dictionary<BodyPart, float> fulcrumWeights = new Dictionary<BodyPart, float>();
		Dictionary<RootMotion.Dynamics.Booster, float> pmBoostersWeights = new Dictionary<RootMotion.Dynamics.Booster, float>();
		Dictionary<float, float> physBoostWeights = new Dictionary<float, float>();
		Dictionary<AnimationIntervalAirControl, float> func_airWeights = new Dictionary<AnimationIntervalAirControl, float>();
		Dictionary<float, float> capsuleHeightWeights = new Dictionary<float, float>();
		Dictionary<float, float> capsuleWidthWeights = new Dictionary<float, float>();
		bool FulcrumLossCustomHandling = false;

		AnimatorClipInfo[] currentClips = animator.GetCurrentAnimatorClipInfo(layer);
		float currentNormalizedTime = animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
		currentNormalizedTime -= Mathf.Floor(currentNormalizedTime);
		float currentStateWeight = animator.IsInTransition(layer) ? (1 - animator.GetAnimatorTransitionInfo(layer).normalizedTime) : 1;

		ProcessAnimationClips(currentClips, currentNormalizedTime, currentStateWeight, fulcrumWeights, pmBoostersWeights, physBoostWeights, func_airWeights, capsuleHeightWeights, capsuleWidthWeights, ref FulcrumLossCustomHandling);

		if (animator.IsInTransition(layer))
		{
			AnimatorClipInfo[] futureClips = animator.GetNextAnimatorClipInfo(layer);
			float futureNormalizedTime = animator.GetNextAnimatorStateInfo(layer).normalizedTime;
			futureNormalizedTime -= Mathf.Floor(futureNormalizedTime);
			float futureStateWeight = animator.GetAnimatorTransitionInfo(layer).normalizedTime;

			ProcessAnimationClips(futureClips, futureNormalizedTime, futureStateWeight, fulcrumWeights, pmBoostersWeights, physBoostWeights, func_airWeights, capsuleHeightWeights, capsuleWidthWeights, ref FulcrumLossCustomHandling);
		}

		// сборка результата
		AnimationInterval result = new AnimationInterval();
		result.Name = "%compiled%";
		result.Start = 0; // just in case
		result.End = int.MaxValue;

		// Func_self.Fulcrums
		foreach (var fulcrum in fulcrumWeights)
		{
			if (fulcrum.Value >= Config.fulcrumWeightNeeded) result.Func_self.Fulcrums.Add(fulcrum.Key);
		}

		// Func_self.pmBooster
		result.Func_self.pmBooster = GetMostWeightedValue(pmBoostersWeights, null, Config.pmBoosterWeightNeeded);

		// Func_self.CapsuleHeightScale
		result.Func_self.CapsuleHeightScale = GetWeightedSum(capsuleHeightWeights, 1);

		// Func_self.CapsuleWidthScale
		result.Func_self.CapsuleWidthScale = GetWeightedSum(capsuleWidthWeights, 1);

		// Func_coll.physBoost
		result.Func_coll.physBoost = GetMostWeightedValue(physBoostWeights, -1, Config.physBoostWeightNeeded);

		// Func_air
		result.Func_air = GetMostWeightedValue(func_airWeights, new AnimationIntervalAirControl(), Mathf.Epsilon);

		// Func_self.FulcrumLossCustomHandling
		result.Func_self.FulcrumLossCustomHandling = FulcrumLossCustomHandling;

		return result;
	}

	/// <summary>
	/// Возвращает взвешенную сумму всех частей
	/// </summary>
	/// <param name="vals">Словарь с элементами</param>
	/// <param name="defVal">Элемент для того случая, когда возвращать нечего</param>
	/// <returns>Взвешенная сумма всех частей</returns>
	private float GetWeightedSum(Dictionary<float, float> vals, float defVal)
	{
		if (vals.Count == 0) return defVal;
		else
		{
			float result = 0;
			foreach (var v in vals)
			{
				result += v.Key * v.Value;
			}
			return result;
		}
	}

	/// <summary>
	/// Возвращает элемент с наибольшим весом
	/// </summary>
	/// <typeparam name="T">Тип элемента</typeparam>
	/// <param name="vals">Словарь с элементами</param>
	/// <param name="defVal">Элемент для того случая, когда возвращать нечего</param>
	/// <param name="minNeededWeight">Минимальный допустимый вес элемента</param>
	/// <returns>Элемент с наибольшим весом, если есть</returns>
	private T GetMostWeightedValue<T>(Dictionary<T, float> vals, T defVal, float minNeededWeight)
	{
		var result = new KeyValuePair<T, float>(defVal, 0);
		foreach (var val in vals)
		{
			if (val.Value >= minNeededWeight && result.Value < val.Value) result = val;
		}
		return result.Key;
	}

	/// <summary>
	/// На данный момент готово:
	/// Func_self.Fulcrums
	/// Func_self.pmBooster
	/// Func_coll.physBoost
	/// Func_air
	/// Func_self.CapsuleHeightScale
	/// Func_self.CapsuleWidthScale
	/// 
	/// Проводит разбор анимационного состояния. Подфункция GetCompiledSettings.
	/// </summary>
	/// <param name="clips">Клипы анимационного состояния</param>
	/// <param name="normalizedTime">Время в анимационном состоянии</param>
	/// <param name="stateWeight">Вес анимационного состояния</param>
	/// <param name="fulcrumWeights">Веса точек опоры - точка опоры, вес</param>
	/// <param name="pmBoostersWeights">Веса бустеров - бустер, вес</param>
	/// <param name="physBoostWeights">Веса физических бустеров - бустер, вес</param>
	/// <param name="capsuleHeightWeights">Высоты капсулы - высота, вес</param>
	/// <param name="capsuleWidthWeights">Ширины капсулы - ширина, вес</param>
	/// <param name="FulcrumLossCustomHandling">Сшагивание вместо падения</param>
	private void ProcessAnimationClips(AnimatorClipInfo[] clips, float normalizedTime, float stateWeight,
		Dictionary<BodyPart, float> fulcrumWeights, Dictionary<RootMotion.Dynamics.Booster, float> pmBoostersWeights, Dictionary<float, float> physBoostWeights,
		Dictionary<AnimationIntervalAirControl, float> func_airWeights, Dictionary<float, float> capsuleHeightWeights, Dictionary<float, float> capsuleWidthWeights,
		ref bool FulcrumLossCustomHandling)
	{
		foreach (var clip in clips)
		{
			var animationSettings = GetSettingsForAnim(clip.clip.name);
			foreach (var interval in animationSettings.Intervals)
			{
				int frame = Mathf.RoundToInt(
					normalizedTime *
					clip.clip.frameRate *
					clip.clip.length);

				if (interval.Start <= frame && interval.End >= frame)
				{
					// Func_self.Fulcrums
					foreach (var fulcrum in interval.Func_self.Fulcrums)
					{
						if (fulcrumWeights.ContainsKey(fulcrum)) // TODO: загнать в функцию
							fulcrumWeights[fulcrum] += clip.weight * stateWeight;
						else
							fulcrumWeights[fulcrum] = clip.weight * stateWeight;
					}

					// Func_self.pmBooster
					if (interval.Func_self.pmBooster != null && clip.weight * stateWeight > 0) pmBoostersWeights[interval.Func_self.pmBooster] = clip.weight * stateWeight;

					// Func_self.CapsuleHeightScale
					if (!float.IsNaN(interval.Func_self.CapsuleHeightScale) && clip.weight * stateWeight > 0)
					{
						if (capsuleHeightWeights.ContainsKey(interval.Func_self.CapsuleHeightScale))
							capsuleHeightWeights[interval.Func_self.CapsuleHeightScale] += clip.weight * stateWeight;
						else
							capsuleHeightWeights[interval.Func_self.CapsuleHeightScale] = clip.weight * stateWeight;
					}

					// Func_self.CapsuleWidthScale
					if (!float.IsNaN(interval.Func_self.CapsuleWidthScale) && clip.weight * stateWeight > 0)
					{
						if (capsuleWidthWeights.ContainsKey(interval.Func_self.CapsuleWidthScale))
							capsuleWidthWeights[interval.Func_self.CapsuleWidthScale] += clip.weight * stateWeight;
						else
							capsuleWidthWeights[interval.Func_self.CapsuleWidthScale] = clip.weight * stateWeight;
					}


					// Func_coll.physBoost
					if (!float.IsNaN(interval.Func_coll.physBoost) && clip.weight * stateWeight > 0)
					{
						if (physBoostWeights.ContainsKey(interval.Func_coll.physBoost))
							physBoostWeights[interval.Func_coll.physBoost] += clip.weight * stateWeight;
						else 
							physBoostWeights[interval.Func_coll.physBoost] = clip.weight * stateWeight;
					}

					// Func_air
					if (interval.Func_air != null && clip.weight * stateWeight > 0) func_airWeights[interval.Func_air] = clip.weight * stateWeight; //TODO: мерджим почленно, а не полностью

					// Func_self.FulcrumLossCustomHandling
					if (interval.Func_self.FulcrumLossCustomHandling == true) FulcrumLossCustomHandling = true;
				}
			}
		}
	}

	/// <summary>
	/// Выдаёт сеттинги для требуемого клипа
	/// </summary>
	/// <param name="animClipName">Имя клипа</param>
	/// <returns>Сеттинги</returns>
	private AnimationSettings GetSettingsForAnim(string animClipName)
	{
		if (!settingsCacheIsUpToDate)
		{
			settingsCache.Clear();
			LoadSettingsToCache();
			settingsCacheIsUpToDate = true;
		}
		if (settingsCache.ContainsKey(animClipName)) return settingsCache[animClipName];
		else return new AnimationSettings() { Animation = animClipName };
	}

	/// <summary>
	/// загружает сеттинги в кеш
	/// Не занимается очисткой кеша
	/// </summary>
	private void LoadSettingsToCache()
	{
		var deserializer = new Deserializer();
		foreach (var settingsFile in Config.AllSettings)
		{
			AnimationSettingsFile data = deserializer.Deserialize<AnimationSettingsFile>(settingsFile.text);
			if (data.Animations == null) continue;
			foreach (var animSettings in data.Animations)
			{
				if (animSettings.Animation == "")
				{
					Debug.LogError($"Metadata with empty name will be ignored! Text asset {settingsFile.name}");
				}
				else
				{
					if (!settingsCache.ContainsKey(animSettings.Animation)) settingsCache[animSettings.Animation] = new AnimationSettings() { Animation = animSettings.Animation };
					settingsCache[animSettings.Animation].Intervals.AddRange(animSettings.Intervals);
				}
			}
		}
	}
}


#region setting file structure
public class AnimationSettingsFile
{
	public List<AnimationSettings> Animations = new List<AnimationSettings>();
}

public class AnimationSettings
{
	public string Animation = "";
	public List<AnimationInterval> Intervals = new List<AnimationInterval>();
}

public class AnimationInterval
{
	public string Name = "";
	public int Start = 0;
	public int End = int.MaxValue;
	public string[] TAG = new string[0];
	public AnimationIntervalFuncSelf Func_self = new AnimationIntervalFuncSelf();
	public AnimationIntervalFuncCollision Func_coll = new AnimationIntervalFuncCollision();
	public AnimationIntervalAirControl Func_air = new AnimationIntervalAirControl();

}

public class AnimationIntervalFuncSelf
{
	public RootMotion.Dynamics.Booster pmBooster = null;
	public List<BodyPart> Fulcrums = new List<BodyPart>();
	public float CapsuleHeightScale = float.NaN;
	public float CapsuleWidthScale = float.NaN;
	public bool FulcrumLossCustomHandling = false;
}

public class AnimationIntervalFuncCollision
{
	public float physBoost = float.NaN;
}

public class AnimationIntervalAirControl
{
	public bool ControlForceEnabled = false;
	public float ForwardControlForce = 0;
	public float BackControlForce = 0;
	public float SideControlForce = 0;

	public bool VerticalControlForceEnabled = false;
	public float UpControlForce = 0;
	public float DownControlForce = 0;

	public float ForwardControlForceSpeedLimit = 0;
	public float BackControlForceSpeedLimit = 0;
	public float SideControlForceSpeedLimit = 0;
	public float UpControlForceSpeedLimit = 0;
	public float DownControlForceSpeedLimit = 0;

	public bool YawTurnControlEnabled = false;
	public float YawControlTurnForce = 0;
	public float YawControlTurnForceSpeedLimit = 0;

	public float PassiveForwardForce = 0;
	public float PassiveUpForce = 0;
	public float PassiveRightForce = 0;
	public float PassiveBackForce = 0;
	public float PassiveDownForce = 0;
	public float PassiveLeftForce = 0;

	public float PassiveForwardForceSpeedLimit = 0;
	public float PassiveBackForceSpeedLimit = 0;
	public float PassiveLeftForceSpeedLimit = 0;
	public float PassiveRightForceSpeedLimit = 0;
	public float PassiveUpForceSpeedLimit = 0;
	public float PassiveDownForceSpeedLimit = 0;

	public float PassiveYawRightTurnForceSpeedLimit = 0;
	public float PassiveYawRightTurnForce = 0;
	public float PassiveYawLeftTurnForceSpeedLimit = 0;
	public float PassiveYawLeftTurnForce = 0;
}
#endregion