using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using UnityEngine;

public static class CharacterAnimations
{
	public const int UpperBodyLayerIndex = 7;

	private static Dictionary<AnimationType, int> AnimationTypeNames;

	private static Dictionary<PermanentAnimationType, int> LoopingAnimationTypeNames;

	private static readonly int AnimationSpeed = Animator.StringToHash("AnimationSpeed");

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		AnimationTypeNames = null;
		LoopingAnimationTypeNames = null;
	}

	private static void Init()
	{
		if (AnimationTypeNames != null && LoopingAnimationTypeNames != null)
		{
			return;
		}
		AnimationTypeNames = new Dictionary<AnimationType, int>(Enum.GetValues(typeof(AnimationType)).Length);
		LoopingAnimationTypeNames = new Dictionary<PermanentAnimationType, int>(Enum.GetValues(typeof(PermanentAnimationType)).Length);
		foreach (AnimationType value in Enum.GetValues(typeof(AnimationType)))
		{
			AnimationTypeNames.Add(value, Animator.StringToHash(value.ToStringFast()));
		}
		foreach (PermanentAnimationType value2 in Enum.GetValues(typeof(PermanentAnimationType)))
		{
			LoopingAnimationTypeNames.Add(value2, Animator.StringToHash(value2.ToString()));
		}
	}

	public static IEnumerator RunAnimation(this Animator animator, AnimationType animationType, float speed = 1f)
	{
		float num = animator.RunAnimationLength(animationType, speed);
		if (num != 0f)
		{
			yield return new WaitForSeconds(num);
		}
	}

	public static float RunAnimationLength(this Animator animator, AnimationType animationType, float speed = 1f)
	{
		Init();
		animator.SetFloat(AnimationSpeed, speed);
		animator.SetTrigger(AnimationTypeNames[animationType]);
		return animator.GetAnimationLength(animationType, speed);
	}

	public static float GetAnimationLength(this Animator animator, AnimationType animationType, float speed = 1f)
	{
		return GetAnimationLength(animationType.ToStringFast(), animator, speed);
	}

	public static float GetAnimationLength(this Animator animator, PermanentAnimationType permanentAnimationType, float speed = 1f)
	{
		return GetAnimationLength(permanentAnimationType.ToStringFast(), animator, speed);
	}

	private static float GetAnimationLength(string animationName, Animator animator, float speed = 1f)
	{
		AnimationClip animationClip = animator.runtimeAnimatorController.animationClips.FirstOrDefault((AnimationClip x) => x.name == animationName);
		if (animationClip == null)
		{
			Debug.LogError("Animation " + animationName + " not found on animator '" + animator.name + "'", animator.gameObject);
			return 0f;
		}
		return 1f / speed * animationClip.length;
	}

	public static void SetTrigger(this Animator animator, AnimationType animationType)
	{
		Init();
		animator.SetTrigger(AnimationTypeNames[animationType]);
	}

	public static void ResetTrigger(this Animator animator, AnimationType animationType)
	{
		Init();
		animator.ResetTrigger(AnimationTypeNames[animationType]);
	}

	public static void SetBool(this Animator animator, PermanentAnimationType animationType, bool state = true)
	{
		Init();
		animator.SetBool(LoopingAnimationTypeNames[animationType], state);
	}
}
