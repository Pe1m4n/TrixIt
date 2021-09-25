using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum BodyPart
{
	Hips,
	LeftUpLeg,
	LeftLeg,
	LeftFoot,
	RightUpLeg,
	RightLeg,
	RightFoot,
	Spine,
	Head,
	LeftArm,
	LeftForeArm,
	LeftHand,
	RightArm,
	RightForeArm,
	RightHand,
}

public class BodyPartsDictionary : MonoBehaviour
{
	[RootMotion.LargeHeader("Character Links")]
	public Animator characterAnimator;
	public Transform characterTransform;
	public CapsuleCollider characterCollider;
	public CapsuleCollider characterFootCollider;
	public Rigidbody characterRigidbody;
	public RootMotion.Dynamics.PuppetMaster puppetMaster;
	public Transform getUpAim;

	[RootMotion.LargeHeader("Body Parts Dictionary")]
	public Rigidbody Hips;
	public Rigidbody LeftUpLeg;
	public Rigidbody LeftLeg;
	public Rigidbody LeftFoot;
	public Rigidbody RightUpLeg;
	public Rigidbody RightLeg;
	public Rigidbody RightFoot;
	public Rigidbody Spine;
	public Rigidbody Head;
	public Rigidbody LeftArm;
	public Rigidbody LeftForeArm;
	public Rigidbody LeftHand;
	public Rigidbody RightArm;
	public Rigidbody RightForeArm;
	public Rigidbody RightHand;

	[RootMotion.LargeHeader("Colliders Dictionary")]
	public Collider HipsCollider;
	public Collider LeftUpLegCollider;
	public Collider LeftLegCollider;
	public Collider LeftFootCollider;
	public Collider RightUpLegCollider;
	public Collider RightLegCollider;
	public Collider RightFootCollider;
	public Collider SpineCollider;
	public Collider HeadCollider;
	public Collider LeftArmCollider;
	public Collider LeftForeArmCollider;
	public Collider LeftHandCollider;
	public Collider RightArmCollider;
	public Collider RightForeArmCollider;
	public Collider RightHandCollider;

	[HideInInspector]
	public List<Rigidbody> Rigidbodies;
	[HideInInspector]
	public List<Collider> Colliders;

	private List<Tuple<Rigidbody, string>> animatorNames;

	private void Awake()
	{
		animatorNames = new List<Tuple<Rigidbody, string>>
		{
			new Tuple<Rigidbody, string>(Hips, "Hips"),
			new Tuple<Rigidbody, string>(LeftUpLeg, "LeftUpLeg"),
			new Tuple<Rigidbody, string>(LeftLeg, "LeftLeg"),
			new Tuple<Rigidbody, string>(LeftFoot, "LeftFoot"),
			new Tuple<Rigidbody, string>(RightUpLeg, "RightUpLeg"),
			new Tuple<Rigidbody, string>(RightLeg, "RightLeg"),
			new Tuple<Rigidbody, string>(RightFoot, "RightFoot"),
			new Tuple<Rigidbody, string>(Spine, "Spine"),
			new Tuple<Rigidbody, string>(Head, "Head"),
			new Tuple<Rigidbody, string>(LeftArm, "LeftArm"),
			new Tuple<Rigidbody, string>(LeftForeArm, "LeftForeArm"),
			new Tuple<Rigidbody, string>(LeftHand, "LeftHand"),
			new Tuple<Rigidbody, string>(RightArm, "RightArm"),
			new Tuple<Rigidbody, string>(RightForeArm, "RightForeArm"),
			new Tuple<Rigidbody, string>(RightHand, "RightHand")
		};

		Rigidbodies = new List<Rigidbody>
		{
			Hips,
			LeftUpLeg,
			LeftLeg,
			LeftFoot,
			RightUpLeg,
			RightLeg,
			RightFoot,
			Spine,
			Head,
			LeftArm,
			LeftForeArm,
			LeftHand,
			RightArm,
			RightForeArm,
			RightHand
		}; // тут можно было бы из первого списка подтянуть, но пока не вижу смысла

		Colliders = new List<Collider>
		{
			HipsCollider,
			LeftUpLegCollider,
			LeftLegCollider,
			LeftFootCollider,
			RightUpLegCollider,
			RightLegCollider,
			RightFootCollider,
			SpineCollider,
			HeadCollider,
			LeftArmCollider,
			LeftForeArmCollider,
			LeftHandCollider,
			RightArmCollider,
			RightForeArmCollider,
			RightHandCollider
		};
	}

	public void SetRigidbody(BodyPart bp, Rigidbody r)
	{
		switch (bp)
		{
			case BodyPart.Head:
				Head = r;
				break;
			case BodyPart.Hips:
				Hips = r;
				break;
			case BodyPart.LeftArm:
				LeftArm = r;
				break;
			case BodyPart.LeftFoot:
				LeftFoot = r;
				break;
			case BodyPart.LeftForeArm:
				LeftForeArm = r;
				break;
			case BodyPart.LeftHand:
				LeftHand = r;
				break;
			case BodyPart.LeftLeg:
				LeftLeg = r;
				break;
			case BodyPart.LeftUpLeg:
				LeftUpLeg = r;
				break;
			case BodyPart.RightArm:
				RightArm = r;
				break;
			case BodyPart.RightFoot:
				RightFoot = r;
				break;
			case BodyPart.RightForeArm:
				RightForeArm = r;
				break;
			case BodyPart.RightHand:
				RightHand = r;
				break;
			case BodyPart.RightLeg:
				RightLeg = r;
				break;
			case BodyPart.RightUpLeg:
				RightUpLeg = r;
				break;
			case BodyPart.Spine:
				Spine = r;
				break;
		}
	}

	public Rigidbody GetRigidbody(BodyPart bp)
	{
		return bp switch
		{
			BodyPart.Head => Head,
			BodyPart.Hips => Hips,
			BodyPart.LeftArm => LeftArm,
			BodyPart.LeftFoot => LeftFoot,
			BodyPart.LeftForeArm => LeftForeArm,
			BodyPart.LeftHand => LeftHand,
			BodyPart.LeftLeg => LeftLeg,
			BodyPart.LeftUpLeg => LeftUpLeg,
			BodyPart.RightArm => RightArm,
			BodyPart.RightFoot => RightFoot,
			BodyPart.RightForeArm => RightForeArm,
			BodyPart.RightHand => RightHand,
			BodyPart.RightLeg => RightLeg,
			BodyPart.RightUpLeg => RightUpLeg,
			BodyPart.Spine => Spine,
			_ => null,
		};
	}

	public void SetCollider(BodyPart bp, Collider c)
	{
		switch (bp)
		{
			case BodyPart.Head:
				HeadCollider = c;
				break;
			case BodyPart.Hips:
				HipsCollider = c;
				break;
			case BodyPart.LeftArm:
				LeftArmCollider = c;
				break;
			case BodyPart.LeftFoot:
				LeftFootCollider = c;
				break;
			case BodyPart.LeftForeArm:
				LeftForeArmCollider = c;
				break;
			case BodyPart.LeftHand:
				LeftHandCollider = c;
				break;
			case BodyPart.LeftLeg:
				LeftLegCollider = c;
				break;
			case BodyPart.LeftUpLeg:
				LeftUpLegCollider = c;
				break;
			case BodyPart.RightArm:
				RightArmCollider = c;
				break;
			case BodyPart.RightFoot:
				RightFootCollider = c;
				break;
			case BodyPart.RightForeArm:
				RightForeArmCollider = c;
				break;
			case BodyPart.RightHand:
				RightHandCollider = c;
				break;
			case BodyPart.RightLeg:
				RightLegCollider = c;
				break;
			case BodyPart.RightUpLeg:
				RightUpLegCollider = c;
				break;
			case BodyPart.Spine:
				SpineCollider = c;
				break;
		}
	}

	public string GetAnimatorPartNameFromBodyPartName(string bpname)
	{
		if (animatorNames.Any(x => x.Item1.name == bpname))
		{
			return animatorNames.First(x => x.Item1.name == bpname).Item2;
		}
		else
		{
			Debug.LogError($"BodyPartsDictionary: No body parts named \"{bpname}\"");
			return "";
		}
	}
}
