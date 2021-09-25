using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TrixHelper : EditorWindow
{
    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/TrixHelper")]
    public static void ShowWindow()
    {
		//Show existing window instance. If one doesn't exist, make one.
		GetWindow(typeof(TrixHelper));
    }

    GameObject jntSrc;
    GameObject jntDst;
    bool jntCopyJoints = true;
    bool jntCopyRigidbodies = true;
    bool jntCopyColliders = true;

    GameObject characterRoot;
    Transform cameraTransform;
    RootMotion.CameraController cameraController;

    TrixHelperConfig trixHelperConfig;

    private void SetupTrixCharacter()
    {
        Debug.Log("Начата сборка персонажа Trix");
        // Ещё открыт вопрос о проп-мускулах
        // Проверить нужность коллайдеров персонажа в настройках

        if (characterRoot == null) throw new Exception("Значение поля Character Root не установлено");
        if (trixHelperConfig == null) throw new Exception("Пожалуйста, установите поле Trix Helper Config, файл конфига можно найти в папке Configs");


        // 0. Поиск основных объектов
        Debug.Log("Поиск объектов начат");
        var animator = characterRoot.GetComponentInChildren<Animator>();
        if (animator == null) throw new Exception("Не удалось обнаружить аниматор, проверьте, что вы его создали");
        GameObject characterObject = animator.gameObject;
        var pm = characterRoot.GetComponentInChildren<RootMotion.Dynamics.PuppetMaster>();
        if (pm == null) throw new Exception("Не удалось обнаружить Puppet Master, проверьте, что вы его создали");
        GameObject pmObject = pm.gameObject;
        GameObject behavioursRootObject = characterRoot.transform.Find("Behaviours").gameObject; // это всегда создаётся с PM, можно не проверять
        Debug.Log("Поиск объектов завершён");

        // 1. Добавление скриптов и объектов, поиск компонентов
        Debug.Log("Добавление скриптов и объектов начато");
        List<Transform> oldBehaviours = new List<Transform>();
        foreach (Transform child in behavioursRootObject.transform) oldBehaviours.Add(child);
        foreach(Transform child in oldBehaviours) DestroyImmediate(child.gameObject);
        var characterBehaviourObject = Instantiate(trixHelperConfig.characterBehaviourPrefab, behavioursRootObject.transform);
        var fbc = characterBehaviourObject.GetComponent<FallingBehaviourControl>();
        var bw = characterBehaviourObject.GetComponent<BalanceWatcher>();
        var bpd = characterBehaviourObject.GetComponent<BodyPartsDictionary>();
        var tcs = characterBehaviourObject.GetComponent<TrixCharacterSystems>();
        var bp = characterBehaviourObject.GetComponent<RootMotion.Dynamics.BehaviourPuppet>();
        var animationEventBridge = characterObject.GetComponent<AnimationEventBridge>();
        if (animationEventBridge == null) animationEventBridge = characterObject.AddComponent<AnimationEventBridge>();
        var trixCharacterController = characterObject.GetComponent<TrixCharacterController>();
        if (trixCharacterController == null) trixCharacterController = characterObject.AddComponent<TrixCharacterController>();
        // rigidbody - требуем
        var characterRigidbody = characterObject.GetComponent<Rigidbody>();
        if (characterRigidbody == null) throw new Exception("Не удалось обнаружить Rigidbody, проверьте, что вы его создали");
        // caps coll - требуем
        var capsuleCollider = characterObject.GetComponent<CapsuleCollider>();
        if (capsuleCollider == null) throw new Exception("Не удалось обнаружить капсульный коллайдер, проверьте, что вы его создали");
        // sph coll - не требуем
        //var sphereCollider = characterObject.GetComponent<SphereCollider>();
        //if (sphereCollider == null) throw new Exception("Не удалось обнаружить сферичиский коллайдер, проверьте, что вы его создали");
        Debug.Log("Добавление скриптов и объектов завершено");

        // 2. Настройка Rigidbody
        Debug.Log("Настройка Rigidbody начата");
        characterRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        Debug.Log("Настройка Rigidbody завершена");

        // 3. Настройка аниматора
        Debug.Log("Настройка аниматора начата");
        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
        animator.applyRootMotion = true;
        animator.runtimeAnimatorController = trixHelperConfig.characterAnimatorController;
        Debug.Log("Настройка аниматора завершена");

        // 4. Настройка Animation Event Bridge
        Debug.Log("Настройка Animation Event Bridge начата");
        animationEventBridge.fallingBehaviourControl = fbc;
        animationEventBridge.trixCharacterController = trixCharacterController;
        Debug.Log("Настройка Animation Event Bridge завершена");

        // 5. Настройка Trix Character Controller
        Debug.Log("Настройка Trix Character Controller начата");
        trixCharacterController.animator = animator;
        trixCharacterController.charRigidbody = characterRigidbody;
        trixCharacterController.charTransform = characterObject.transform;
        trixCharacterController.charPuppet = bp;
        trixCharacterController.camTransform = cameraTransform;
        trixCharacterController.camController = cameraController;
        trixCharacterController.balanceWatcher = bw;
        trixCharacterController.fallingBehaviourControl = fbc;
        //trixCharacterController.rightHandPropMuscle = null; ----  вот это у нас тут ещё не сделано, в плане, эту мышцу надо ещё создать. Думаю, будем требовать
        trixCharacterController.config = trixHelperConfig.trixCharacterControllerConfig;
        trixCharacterController.state = TrixCharacterControllerState.normal;
        Debug.Log("Настройка Trix Character Controller завершена");

        // 6. Настройка Body Part Dictionary
        Debug.Log("Настройка Body Part Dictionary начата");
        BodyPartsDictionaryEditor.ParsePuppetRoot(characterRoot.transform, bpd);
        bpd.characterFootCollider = capsuleCollider;
        Debug.Log("Настройка Body Part Dictionary завершена");

        Debug.Log("Завершена сборка персонажа Trix");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Trix Character", EditorStyles.boldLabel);
        characterRoot = (GameObject)EditorGUILayout.ObjectField("Character Root", characterRoot, typeof(GameObject));
        cameraTransform = (Transform)EditorGUILayout.ObjectField("Camera Transform", cameraTransform, typeof(Transform));
        cameraController = (RootMotion.CameraController)EditorGUILayout.ObjectField("Camera Controller", cameraController, typeof(RootMotion.CameraController));
        trixHelperConfig = (TrixHelperConfig)EditorGUILayout.ObjectField("Trix Helper Config", trixHelperConfig, typeof(TrixHelperConfig));

        if (GUILayout.Button("Generate Trix Character"))
        {
            try
            {
                SetupTrixCharacter();
            }
            catch(Exception ex)
			{
                Debug.LogException(ex);
			}
        }

        GUILayout.Label("Copy joints settings", EditorStyles.boldLabel);
        jntSrc = (GameObject)EditorGUILayout.ObjectField("Source character", jntSrc, typeof(GameObject));
        jntDst = (GameObject)EditorGUILayout.ObjectField("Destination character", jntDst, typeof(GameObject));
        jntCopyJoints = EditorGUILayout.Toggle("Copy joints", jntCopyJoints);
        jntCopyRigidbodies = EditorGUILayout.Toggle("Copy rigidbodies", jntCopyRigidbodies);
        jntCopyColliders = EditorGUILayout.Toggle("Copy colliders", jntCopyColliders);
        if (GUILayout.Button("Copy joints settings"))
		{
            var jntSrcPD = jntSrc.GetComponentInChildren<BodyPartsDictionary>();
            var jntDstPD = jntDst.GetComponentInChildren<BodyPartsDictionary>();

            if (jntSrcPD != null && jntDstPD != null)
            {

                Undo.RegisterFullObjectHierarchyUndo(jntDst, "Copy joint settings");

                foreach (BodyPart bp in Enum.GetValues(typeof(BodyPart)))
                {
                    if (jntSrcPD.GetRigidbody(bp) != null && jntDstPD.GetRigidbody(bp) != null)
                    {
                        var srcObject = jntSrcPD.GetRigidbody(bp).gameObject;
                        var dstObject = jntDstPD.GetRigidbody(bp).gameObject;

                        if (jntCopyJoints)
                        {
                            var srcJoint = srcObject.GetComponent<ConfigurableJoint>();
                            var dstJoint = dstObject.GetComponent<ConfigurableJoint>();

                            var dstJointConnectedBody = dstJoint.connectedBody;

                            CopyValues(srcJoint, dstJoint);
                            dstJoint.connectedBody = dstJointConnectedBody;
                        }

                        if (jntCopyRigidbodies)
                        {
                            var srcRig = srcObject.GetComponent<Rigidbody>(); // lul, effectvness
                            var dstRig = dstObject.GetComponent<Rigidbody>();

                            CopyValues(srcRig, dstRig);
                        }

                        if (jntCopyColliders)
                        {
                            var srcCol = srcObject.GetComponent<Collider>();
                            var dstCol = dstObject.GetComponent<Collider>();

                            CopyValues(srcCol, dstCol);
                        }
                    }
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(jntDst);
            }
        }
    }

    // https://forum.unity.com/threads/how-to-copy-component-value-from-an-object-to-another.697397/#post-4666991
    void CopyValues<T>(T from, T to)
    {
        var json = EditorJsonUtility.ToJson(from);
        EditorJsonUtility.FromJsonOverwrite(json, to);
    }
}
