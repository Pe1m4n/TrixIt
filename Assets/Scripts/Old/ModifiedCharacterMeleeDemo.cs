using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.Demos;
using RootMotion.Dynamics;

public class ModifiedCharacterMeleeDemo : RootMotion.Demos.CharacterMeleeDemo
{
	[Header("Our Modifictions")]
	public bool freeseAirRotation = true; // Для полного аута в воздух надо отключать двойной прыжок и Air Control

	protected override void Rotate()
	{
		if (freeseAirRotation && !onGround) return;

		base.Rotate();
	}
}
