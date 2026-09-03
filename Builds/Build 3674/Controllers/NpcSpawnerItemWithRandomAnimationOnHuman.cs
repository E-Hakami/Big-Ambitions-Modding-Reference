using BigAmbitions.Characters;
using Extensions;
using UnityEngine;

namespace Controllers;

public class NpcSpawnerItemWithRandomAnimationOnHuman : NpcSpawnerItem
{
	[SerializeField]
	private PermanentAnimationType[] animationTypes;

	[SerializeField]
	private RuntimeAnimatorController[] animatorControllers;

	private BaseHuman _baseHuman;

	public override void OnNpcSpawn(BaseHuman baseHuman)
	{
		base.OnNpcSpawn(baseHuman);
		if (animatorControllers.Length != 0)
		{
			RuntimeAnimatorController random = animatorControllers.GetRandom();
			if ((bool)random)
			{
				baseHuman.animator.runtimeAnimatorController = random;
			}
		}
		if (animationTypes.Length != 0)
		{
			PermanentAnimationType random2 = animationTypes.GetRandom();
			PermanentAnimationType[] array = animationTypes;
			foreach (PermanentAnimationType permanentAnimationType in array)
			{
				baseHuman.animator.SetBool(permanentAnimationType, random2 == permanentAnimationType);
			}
		}
		_baseHuman = baseHuman;
	}

	public override void OnNpcDespawn()
	{
		if (occupied)
		{
			PermanentAnimationType[] array = animationTypes;
			foreach (PermanentAnimationType animationType in array)
			{
				_baseHuman.animator.SetBool(animationType, state: false);
			}
		}
		base.OnNpcDespawn();
	}
}
