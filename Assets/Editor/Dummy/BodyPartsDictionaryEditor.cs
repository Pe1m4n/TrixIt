using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(BodyPartsDictionary))]
public class BodyPartsDictionaryEditor : Editor
{
	public Transform puppetRoot;
	
	public override void OnInspectorGUI()
	{
		puppetRoot = (Transform) EditorGUILayout.ObjectField("Puppet Root", puppetRoot, typeof(Transform), true);
		if (GUILayout.Button("Parse puppet root"))
		{
			var target = (BodyPartsDictionary)serializedObject.targetObject;
			ParsePuppetRoot(puppetRoot, target);
		}
		base.OnInspectorGUI(); // вот эта строчка делает обычный gui
	}

	public static void ParsePuppetRoot(Transform puppetRoot, BodyPartsDictionary target)
	{
		if (puppetRoot != null)
		{
			var animatorPartsDictionary = new Dictionary<string, BodyPart>(); // имя в аниматоре -  часть тела
			animatorPartsDictionary["Hips"] = BodyPart.Hips;
			animatorPartsDictionary["LeftUpperLeg"] = BodyPart.LeftUpLeg;
			animatorPartsDictionary["LeftLowerLeg"] = BodyPart.LeftLeg;
			animatorPartsDictionary["LeftFoot"] = BodyPart.LeftFoot;
			animatorPartsDictionary["RightUpperLeg"] = BodyPart.RightUpLeg;
			animatorPartsDictionary["RightLowerLeg"] = BodyPart.RightLeg;
			animatorPartsDictionary["RightFoot"] = BodyPart.RightFoot;
			animatorPartsDictionary["Spine"] = BodyPart.Spine;
			animatorPartsDictionary["Chest"] = BodyPart.Spine;
			animatorPartsDictionary["Head"] = BodyPart.Head;
			animatorPartsDictionary["LeftUpperArm"] = BodyPart.LeftArm;
			animatorPartsDictionary["LeftLowerArm"] = BodyPart.LeftForeArm;
			animatorPartsDictionary["LeftHand"] = BodyPart.LeftHand;
			animatorPartsDictionary["RightUpperArm"] = BodyPart.RightArm;
			animatorPartsDictionary["RightLowerArm"] = BodyPart.RightForeArm;
			animatorPartsDictionary["RightHand"] = BodyPart.RightHand;

			// надо впихнуть сюда проверку на мультиобъекты
			Undo.RecordObject(target, "Body parts dictionary - parse puppet");

			var character = puppetRoot.gameObject.GetComponentInChildren<Animator>().gameObject;
			var pm = puppetRoot.GetComponentInChildren<RootMotion.Dynamics.PuppetMaster>();
			target.characterAnimator = character.GetComponent<Animator>();
			target.characterTransform = character.transform;
			target.characterRigidbody = character.GetComponent<Rigidbody>();
			target.characterCollider = character.GetComponent<CapsuleCollider>();
			target.puppetMaster = pm;

			foreach (var ap in animatorPartsDictionary)
			{
				target.SetRigidbody(ap.Value, null);
				target.SetCollider(ap.Value, null);
			}

			for (int i = 0; i < target.characterAnimator.avatar.humanDescription.human.Length; i++)
			{

				if (animatorPartsDictionary.ContainsKey(target.characterAnimator.avatar.humanDescription.human[i].humanName) &&
					pm.muscles.Any(x => x.name == target.characterAnimator.avatar.humanDescription.human[i].boneName))
				{
					var bodyPart = animatorPartsDictionary[target.characterAnimator.avatar.humanDescription.human[i].humanName];
					var muscle = pm.muscles.First(x => x.name == target.characterAnimator.avatar.humanDescription.human[i].boneName);
					target.SetRigidbody(bodyPart, muscle.joint.gameObject.GetComponent<Rigidbody>());
					target.SetCollider(bodyPart, muscle.joint.gameObject.GetComponent<Collider>());
				}
			}

			PrefabUtility.RecordPrefabInstancePropertyModifications(target);
		}
	}
}
