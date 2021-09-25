using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrixHelperConfig", menuName = "TRIX/TrixHelperConfig")]
public class TrixHelperConfig : ScriptableObject
{
    public RuntimeAnimatorController characterAnimatorController;
    public GameObject characterBehaviourPrefab;
    public TrixCharacterControllerConfig trixCharacterControllerConfig;
}
