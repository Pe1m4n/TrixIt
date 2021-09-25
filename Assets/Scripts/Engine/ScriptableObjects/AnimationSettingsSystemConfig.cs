using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationSettingsSystemConfig", menuName = "TRIX/AnimationSettingsSystemConfig")]
public class AnimationSettingsSystemConfig : ScriptableObject
{
    public List<TextAsset> AllSettings = new List<TextAsset>();

    [Range(0, 1), Tooltip("Удельная доля, которую точка опоры должна набрать в бленде анимаций, чтобы пройти. Изменяется от 0 до 1")]
    public float fulcrumWeightNeeded;

    [Range(0, 1), Tooltip("Удельная доля, которую анимационный клип должен набрать в бленде анимаций, чтобы его Puppet Master Booster сработал. Изменяется от 0 до 1")]
    public float pmBoosterWeightNeeded;

    [Range(0, 1), Tooltip("Удельная доля, которую анимационный клип должен набрать в бленде анимаций, чтобы его физический бустер сработал. Изменяется от 0 до 1")]
    public float physBoostWeightNeeded;
}
