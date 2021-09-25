using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DebugGameManager))]
public class DebugGameManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		if (GUILayout.Button("Parse puppet root"))
		{
			var target = (DebugGameManager)serializedObject.targetObject;
			Undo.RecordObject(target, "Game manager - parse puppet");

			target.puppetAnimator = null;
			target.characterController = null;
			target.mainCamera = null;
			target.cameraController = null;
			target.puppet = null;
			target.fallingBehaviourControl = null;

			target.puppetAnimator = target.puppetRoot.GetComponentInChildren<Animator>();
			var character = target.puppetAnimator.transform;
			target.characterController = character.GetComponent<TrixCharacterController>();
			target.mainCamera = character.GetComponentInChildren<Camera>();
			target.cameraController = target.mainCamera.GetComponent<RootMotion.CameraController>();
			target.puppet = target.puppetRoot.GetComponentInChildren<RootMotion.Dynamics.BehaviourPuppet>();
			target.fallingBehaviourControl = target.puppetRoot.GetComponentInChildren<FallingBehaviourControl>();

			PrefabUtility.RecordPrefabInstancePropertyModifications(target);
		}
		base.OnInspectorGUI();
	}
}
