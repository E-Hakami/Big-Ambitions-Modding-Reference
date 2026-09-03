using System;
using NaughtyAttributes;
using UnityEngine;

namespace Buildings.Retail.Businesses.CinemaTheater;

public class ActorEmployeeAnimationSet : ScriptableObject
{
	[Serializable]
	public class AnimationInfo
	{
		public string trigger;

		public float minDurationBeforeChange;

		public float chanceMale;

		public float chanceFemale;

		public float chanceMultiplier = 1f;

		public float GetChance(bool isFemale)
		{
			if (!isFemale)
			{
				return chanceMale * chanceMultiplier;
			}
			return chanceFemale * chanceMultiplier;
		}
	}

	public RuntimeAnimatorController actorAnimatorController;

	public float intervalMin;

	public float intervalMax = 1f;

	public float intervalAddPerExtraActor = 0.5f;

	public AnimationInfo[] animations;

	public AnimationClip[] clips;

	[Button(null, EButtonEnableMode.Always)]
	public void MakeAnimationInfosFromClips()
	{
	}
}
