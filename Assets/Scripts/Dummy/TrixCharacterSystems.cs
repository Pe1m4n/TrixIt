using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrixCharacterSystems : MonoBehaviour
{
	#region settings
	[RootMotion.LargeHeader("Links")]
	public BodyPartsDictionary partsDictionary;
	public RootMotion.Dynamics.BehaviourPuppet puppetBehaviour; // TODO: убрать всё в PartsDictionary
	public BalanceWatcher balanceWatcher;
	public FallingBehaviourControl fallingBehaviourControl;

	[RootMotion.LargeHeader("Fulcrums")]
	public float fulcrumRegainPinSpeed;
	public float fulcrumReactionRegainPinSpeed;
	public float fulcrumCollisionResistance;
	public float fulcrumUnpinParents;
	public float fulcrumUnpinChildren;
	public float fulcrumUnpinGroup;
	public float fulcrumUnpinSelf;
	public float fulcrumKinshipBase;
	#endregion

	#region fields
	private float defaultDefaultRegainPinSpeed;
	private float defaultDefaultCollisionResistance;
	private float defaultDefaultUnpinParents;
	private float defaultDefaultUnpinChildren;
	private float defaultDefaultUnpinGroup;
	private float defaultDefaultUnpinSelf;
	private Dictionary<int, float> defaultRegainPinSpeeds = new Dictionary<int, float>(); // <номер в groupOverrides, дефолтное значение>
	private Dictionary<int, float> defaultCollisionResistances = new Dictionary<int, float>();
	private Dictionary<int, float> defaultUnpinParents = new Dictionary<int, float>();
	private Dictionary<int, float> defaultUnpinChildren = new Dictionary<int, float>();
	private Dictionary<int, float> defaultUnpinGroup = new Dictionary<int, float>();
	private Dictionary<int, float> defaultUnpinSelf = new Dictionary<int, float>();
	private float defaultCharacterColliderHeight;
	private float defaultCharacterColliderRadius;
	private float defaultCharacterFootColliderHeight;
	private float defaultCharacterFootColliderRadius;
	#endregion

	#region unity
	private void Start()
	{
		InitDefaultPinRegainSpeeds();
		InitListeners();
		InitDefaultCapsuleSize();
	}

	private void FixedUpdate()
	{
		BoostPuppetMaster();
		UpdatePinPowers();
		UpdateCapsuleSize();
		LogMetadatas();
	}
	#endregion

	#region events
	private void InitListeners()
	{
		puppetBehaviour.OnCollision += onPMCollision;
		puppetBehaviour.OnCollisionImpulse += onPMCollisionImpulse;
	}

	private void onPMCollision(RootMotion.Dynamics.MuscleCollision collision)
	{
		BoostAnimationForce(collision);
	}

	private void onPMCollisionImpulse(RootMotion.Dynamics.MuscleCollision collision, float impulse)
	{
		EnableDynamic(collision);
	}
	#endregion

	#region log
	private void LogMetadatas()
	{
		var settings = AnimationSettingsSystem.ASS.GetCompiledSettings(partsDictionary.characterAnimator, 0); // TODO: убрать магическое число

		foreach (var tag in settings.TAG)
		{
			GameManager.FixUpdLog(tag);
		}
		foreach (var f in settings.Func_self.Fulcrums)
		{
			GameManager.FixUpdLog($"Fulcrum {f.ToString()}");
		}
	}
	#endregion

	#region capsule size
	private void InitDefaultCapsuleSize()
	{
		defaultCharacterColliderHeight = partsDictionary.characterCollider.height;
		defaultCharacterColliderRadius = partsDictionary.characterCollider.radius;
		defaultCharacterFootColliderHeight = partsDictionary.characterFootCollider.height;
		defaultCharacterFootColliderRadius = partsDictionary.characterFootCollider.radius;
	}

	private void UpdateCapsuleSize() // TODO: проверить, что только тут меняются эти величины, особенно посмотреть на капсулу для вставания
	{
		var settings = AnimationSettingsSystem.ASS.GetCompiledSettings(partsDictionary.characterAnimator, 0); // TODO: убрать магическое число
		GameManager.FixUpdLog($"settings.Func_self.CapsuleHeightScale: {settings.Func_self.CapsuleHeightScale}");
		if (Mathf.Abs(partsDictionary.characterCollider.height - defaultCharacterColliderHeight * settings.Func_self.CapsuleHeightScale) > Mathf.Epsilon) 
			partsDictionary.characterCollider.height = defaultCharacterColliderHeight * settings.Func_self.CapsuleHeightScale;
		if (Mathf.Abs(partsDictionary.characterCollider.radius - defaultCharacterColliderRadius * settings.Func_self.CapsuleWidthScale) > Mathf.Epsilon)
			partsDictionary.characterCollider.radius = defaultCharacterColliderRadius * settings.Func_self.CapsuleWidthScale;

		if (Mathf.Abs(partsDictionary.characterFootCollider.height - defaultCharacterFootColliderHeight * settings.Func_self.CapsuleHeightScale) > Mathf.Epsilon)
			partsDictionary.characterFootCollider.height = defaultCharacterFootColliderHeight * settings.Func_self.CapsuleHeightScale;
		if (Mathf.Abs(partsDictionary.characterFootCollider.radius - defaultCharacterFootColliderRadius * settings.Func_self.CapsuleWidthScale) > Mathf.Epsilon)
			partsDictionary.characterFootCollider.radius = defaultCharacterFootColliderRadius * settings.Func_self.CapsuleWidthScale;
	}
	#endregion

	#region animation boosts
	private void BoostAnimationForce(RootMotion.Dynamics.MuscleCollision collision)
	{
		var settings = AnimationSettingsSystem.ASS.GetCompiledSettings(partsDictionary.characterAnimator, 0); // TODO: убрать магическое число

		if (settings.Func_coll.physBoost != 1 && settings.Func_coll.physBoost > 0)
		{
			if (collision.collision.rigidbody)
			{
				//collision.collision.rigidbody.AddForceAtPosition(- collision.collision.impulse * (settings.Func_coll.physBoost - 1), collision.collision.contacts[0].point, ForceMode.Impulse);
				if (collision.collision.rigidbody.name != "Ground") Debug.DrawRay(collision.collision.contacts[0].point, 
					collision.collision.impulse.magnitude * (settings.Func_coll.physBoost - 1) * partsDictionary.puppetMaster.muscles[collision.muscleIndex].rigidbody.velocity.normalized, Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), 10f);
				collision.collision.rigidbody.AddForceAtPosition(collision.collision.impulse.magnitude * (settings.Func_coll.physBoost - 1) * partsDictionary.puppetMaster.muscles[collision.muscleIndex].rigidbody.velocity.normalized, collision.collision.contacts[0].point, ForceMode.Impulse); // очень грубо, но лучше чем раньше
				//if (collision.collision.rigidbody.name != "Ground") GameManager.Log($"PhysBoost: name = {collision.collision.rigidbody.name} | normal = {collision.collision.contacts[0].normal} | Boost = {(settings.Func_coll.physBoost - 1)} | imp = {collision.collision.impulse * (settings.Func_coll.physBoost - 1)}");
			}
		}
	}

	private void BoostPuppetMaster()
	{
		var settings = AnimationSettingsSystem.ASS.GetCompiledSettings(partsDictionary.characterAnimator, 0); // TODO: убрать магическое число

		if (settings.Func_self.pmBooster != null)
		{
			settings.Func_self.pmBooster.Boost(puppetBehaviour);
		}
	}
	#endregion

	#region fulcrums
	private void InitDefaultPinRegainSpeeds()
	{
		defaultDefaultRegainPinSpeed = puppetBehaviour.defaults.regainPinSpeed;
		defaultDefaultCollisionResistance = puppetBehaviour.defaults.collisionResistance;
		defaultDefaultUnpinParents = puppetBehaviour.defaults.unpinParents;
		defaultDefaultUnpinChildren = puppetBehaviour.defaults.unpinChildren;
		defaultDefaultUnpinGroup = puppetBehaviour.defaults.unpinGroup;
		defaultDefaultUnpinSelf = puppetBehaviour.defaults.unpinSelf;
		for (int i = 0; i < puppetBehaviour.groupOverrides.Count(); i++)
		{
			defaultRegainPinSpeeds[i] = puppetBehaviour.groupOverrides[i].props.regainPinSpeed;
			defaultCollisionResistances[i] = puppetBehaviour.groupOverrides[i].props.collisionResistance;
			defaultUnpinParents[i] = puppetBehaviour.groupOverrides[i].props.unpinParents;
			defaultUnpinChildren[i] = puppetBehaviour.groupOverrides[i].props.unpinChildren;
			defaultUnpinGroup[i] = puppetBehaviour.groupOverrides[i].props.unpinGroup;
			defaultUnpinSelf[i] = puppetBehaviour.groupOverrides[i].props.unpinSelf;
		}
	}

	private void UpdatePinPowers()
	{
		// получить точки опоры
		var fs = balanceWatcher.Fulcrums;
		var fgroups = new HashSet<RootMotion.Dynamics.Muscle.Group>();
		foreach (var f in fs)
		{
			fgroups.Add(f.props.group);
		}

		// построить от них пути 
		float[] additionalFulcrumCoeff = new float[partsDictionary.puppetMaster.muscles.Count()]; // Общий множитель, для мускулов
		foreach (var f in fs)
		{
			for (int i = 0; i < additionalFulcrumCoeff.Length; i++)
			{
				int distance = f.kinshipDegrees[i];
				additionalFulcrumCoeff[i] += Mathf.Pow(fulcrumKinshipBase, distance);
			}
		}

		// перевести множители отдельных мускулов в групповые
		float defaultAFC = 0;
		int defaultAFC_num = 0;
		float[] groupAFCs = new float[puppetBehaviour.groupOverrides.Length];
		int[] groupAFCs_num = new int[puppetBehaviour.groupOverrides.Length];
		for (int i = 0; i < additionalFulcrumCoeff.Length; i++)
		{
			var mGroup = partsDictionary.puppetMaster.muscles[i].props.group;
			bool isInAnyGroup = false;
			for (int j = 0; j < puppetBehaviour.groupOverrides.Count(); j++)
			{
				if (puppetBehaviour.groupOverrides[j].groups.Contains(mGroup))
				{
					isInAnyGroup = true;
					groupAFCs[j] += additionalFulcrumCoeff[i];
					groupAFCs_num[j]++;
				}
			}
			if (isInAnyGroup == false)
			{
				defaultAFC += additionalFulcrumCoeff[i];
				defaultAFC_num++;
			}
		}
		for (int j = 0; j < groupAFCs.Length; j++) if (groupAFCs_num[j] != 0) groupAFCs[j] = groupAFCs[j] / groupAFCs_num[j];
		if (defaultAFC_num != 0) defaultAFC = defaultAFC / defaultAFC_num;


		// обновить все пины нашими значениями
		for (int j = 0; j < groupAFCs.Length; j++)
		{
			puppetBehaviour.groupOverrides[j].props.regainPinSpeed = defaultRegainPinSpeeds[j] + groupAFCs[j] * (fallingBehaviourControl.IsInHitReactionAnimation() ? fulcrumReactionRegainPinSpeed : fulcrumRegainPinSpeed);
			puppetBehaviour.groupOverrides[j].props.collisionResistance = defaultCollisionResistances[j] + groupAFCs[j] * fulcrumCollisionResistance;
			puppetBehaviour.groupOverrides[j].props.unpinParents = Mathf.Lerp(defaultUnpinParents[j], fulcrumUnpinParents, groupAFCs[j]);
			puppetBehaviour.groupOverrides[j].props.unpinChildren = Mathf.Lerp(defaultUnpinChildren[j], fulcrumUnpinChildren, groupAFCs[j]);
			puppetBehaviour.groupOverrides[j].props.unpinGroup = Mathf.Lerp(defaultUnpinGroup[j], fulcrumUnpinGroup, groupAFCs[j]);
			puppetBehaviour.groupOverrides[j].props.unpinSelf = Mathf.Lerp(defaultUnpinSelf[j], fulcrumUnpinSelf, groupAFCs[j]);
		}
		puppetBehaviour.defaults.regainPinSpeed = defaultDefaultRegainPinSpeed + defaultAFC * (fallingBehaviourControl.IsInHitReactionAnimation() ? fulcrumReactionRegainPinSpeed : fulcrumRegainPinSpeed);
		puppetBehaviour.defaults.collisionResistance = defaultDefaultCollisionResistance + defaultAFC * fulcrumCollisionResistance;
		puppetBehaviour.defaults.unpinParents = Mathf.Lerp(defaultDefaultUnpinParents, fulcrumUnpinParents, defaultAFC);
		puppetBehaviour.defaults.unpinChildren = Mathf.Lerp(defaultDefaultUnpinChildren, fulcrumUnpinChildren, defaultAFC);
		puppetBehaviour.defaults.unpinGroup = Mathf.Lerp(defaultDefaultUnpinGroup, fulcrumUnpinGroup, defaultAFC);
		puppetBehaviour.defaults.unpinSelf = Mathf.Lerp(defaultDefaultUnpinSelf, fulcrumUnpinSelf, defaultAFC);
	}
	#endregion

	#region misc
	private void EnableDynamic(RootMotion.Dynamics.MuscleCollision collision) // TODO: сделать
	{
		/*var intervals = partsDictionary.animationMetadataSystem.GetMetadataIntervalsForCurrentAnim(partsDictionary.characterAnimator, 0);

		foreach (var interval in intervals)
		{
			if (interval.TAG.Contains(AnimationMetadataSystem.tag_enableDynamic))
			{
				var bw = GameManager.GM.GetPartCorrespondingBW(collision.collision.gameObject);
				if (bw != null)
				{
					bw.IncreaseContactCount();
				}
			}
		}*/
	}
	#endregion
}
