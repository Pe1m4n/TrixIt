using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using System.Linq;


public class FulcrumsGenerator : MonoBehaviour
{
	public Text logText;
	public Button generateButton;
	public AnimationClip idleAnimation;
	public List<AnimationClip> animationsToGenerate;
	public string fileName;
	public Animator animator;
	public RootMotion.Dynamics.PuppetMaster puppetMaster;
	public BalanceWatcher balanceWatcher;
	public BodyPartsDictionary partsDictionary;
	public int minimumAllowedIntervalLengthBeforeTrim = 1;
	public int intervalsTrimStart = 0;
	public int intervalsTrimEnd = 0;
	public int minContactsToCountIn;
	public int maxContactsToCountIn;

	private const int framesToWaitBeforeNextStep = 10;

	public void OnGenerateFulcrums()
	{
		StartCoroutine(nameof(GenerateFulcrums));

		// заиметь управление над персом
		// Вызывать анимацию, писать каждый кадр
		// останавливать анимацию
		// группировать кадры в интервалы
		// проводить постобработку (расширение, сужение)
		// записывать интервалы в файл
	}

	private void Log(string s)
	{
		logText.text += s;
		if (logText.text.Split('\n').Length > 100) logText.text = String.Concat(logText.text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable().Skip(logText.text.Split('\n').Length - 100).Select(s => s + "\n"));
	}
	private IEnumerator GenerateFulcrums()
	{
		Log("\n\nStarted fulcrum generation");
		generateButton.gameObject.SetActive(false);

		AnimationMetadataFile_Simple metadataFile = new AnimationMetadataFile_Simple();

		balanceWatcher.minContactsToCountFulcrumIn = minContactsToCountIn;
		balanceWatcher.maxContactsToCountFulcrumIn = maxContactsToCountIn;

		foreach (var anim in animationsToGenerate)
		{
			Log($"\nAnimation Clip: {anim.name}");
			AnimationMetadata_Simple metadata = new AnimationMetadata_Simple
			{
				Animation = anim.name
			};

			puppetMaster.Teleport(Vector3.zero, Quaternion.identity, true);

			AnimatorOverrideController overridingAnimator = new AnimatorOverrideController(animator.runtimeAnimatorController);
			var animsOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>
			{
				new KeyValuePair<AnimationClip, AnimationClip>(overridingAnimator.animationClips[0], idleAnimation),
				new KeyValuePair<AnimationClip, AnimationClip>(overridingAnimator.animationClips[1], anim)
			};
			overridingAnimator.ApplyOverrides(animsOverrides);
			animator.runtimeAnimatorController = overridingAnimator;
			animator.SetTrigger("PlayAnimation");

			while (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
			{
				yield return null;
			}

			Dictionary<int, HashSet<BodyPart>> points = new Dictionary<int, HashSet<BodyPart>>();

			while (animator.GetCurrentAnimatorStateInfo(0).IsName("Animation"))
			{
				float clampedNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
				clampedNormalizedTime -= Mathf.Floor(clampedNormalizedTime);

				int currentFrame = Mathf.RoundToInt(
					clampedNormalizedTime *
					animator.GetCurrentAnimatorClipInfo(0)[0].clip.frameRate *
					animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);

				if (!points.ContainsKey(currentFrame)) points[currentFrame] = new HashSet<BodyPart>();

				foreach (var f in balanceWatcher.FulcrumMusclesFilteredByNumOfContacts) points[currentFrame].Add((BodyPart)Enum.Parse(typeof(BodyPart), partsDictionary.GetAnimatorPartNameFromBodyPartName(f.name)));

				yield return null;
			}

			// проверка на однородность (что мы не пропустили точки)
			HashSet<int> check_missedPoints = new HashSet<int>();
			foreach (var p in points)
			{
				check_missedPoints.Add(p.Key);
			}
			int check_pointNum = 0;
			foreach(var cp in check_missedPoints)
			{
				if (check_pointNum != cp) Log($"\n<color=red>Missing frame number {check_pointNum} in animation '{anim.name}'!</color>");
				check_pointNum++;
			}

			foreach (BodyPart bp in Enum.GetValues(typeof(BodyPart)))
			{
				AnimationInterval_Simple interval = null;

				for (int i = 0; i < points.Count; i++)
				{
					if (points[i].Contains(bp))
					{
						if (interval == null)
						{
							interval = new AnimationInterval_Simple
							{
								Start = i
							};
							interval.Func_self.Fulcrums.Add(bp);
						}
						else
						{
							if (!interval.Func_self.Fulcrums.Contains(bp)) interval.Func_self.Fulcrums.Add(bp);
						}
					}
					else
					{
						if (interval != null)
						{
							interval.End = i - 1;
							AddInterval(metadata.Intervals, interval);
							interval = null;
						}
					}
				}

				if (interval != null)
				{
					interval.End = points.Count - 1;
					AddInterval(metadata.Intervals, interval);
				}
			}

			metadataFile.Animations.Add(metadata);
			Log("... Success!");
		}

		Serializer serializer = new Serializer();
		string fileText = serializer.Serialize(metadataFile);
		File.WriteAllText(fileName, fileText);

		puppetMaster.Teleport(Vector3.zero, Quaternion.identity, true);
		generateButton.gameObject.SetActive(true);
		Log("\nFinished fulcrum generation");
	}

	private void AddInterval(List<AnimationInterval_Simple> list, AnimationInterval_Simple interval)
	{
		if (interval.End - interval.Start + 1 >= minimumAllowedIntervalLengthBeforeTrim)
		{
			interval.Start += intervalsTrimStart;
			interval.End -= intervalsTrimEnd;
			if (interval.Start <= interval.End)
			{
				list.Add(interval);
			}
		}
	}
}


class AnimationMetadataFile_Simple
{
	public List<AnimationMetadata_Simple> Animations = new List<AnimationMetadata_Simple>();
}

class AnimationMetadata_Simple
{
	public string Animation = "";
	public List<AnimationInterval_Simple> Intervals = new List<AnimationInterval_Simple>();
}

class AnimationInterval_Simple
{
	public int Start = 0;
	public int End = int.MaxValue;
	public AnimationIntervalFuncSelf_Simple Func_self = new AnimationIntervalFuncSelf_Simple();
}

class AnimationIntervalFuncSelf_Simple
{
	public List<BodyPart> Fulcrums = new List<BodyPart>();
}