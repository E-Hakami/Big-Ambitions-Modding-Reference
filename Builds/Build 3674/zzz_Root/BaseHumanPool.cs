using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using Extensions;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/BaseHumanPool")]
public class BaseHumanPool : Pool<BaseHuman>
{
	public AppearanceTag[] appearanceTags;

	public List<RuntimeAnimatorController> animatorControllers;

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/DummyHuman";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override BaseHuman CreateFunc(Transform parent)
	{
		BaseHuman baseHuman = PrefabHelper.CreatePrefab<BaseHuman>(GetPrefabName(), parent);
		InitHuman(baseHuman);
		return baseHuman;
	}

	protected virtual void InitHuman(BaseHuman human)
	{
		human.appearanceSetter.SetRandomAppearance(appearanceTags);
		_poolIndex++;
		human.name = inspectorGameObjectName + _poolIndex;
	}

	protected override void ActionOnGet(BaseHuman human)
	{
		human.gameObject.SetActive(value: true);
		RuntimeAnimatorController random = animatorControllers.GetRandom();
		if ((bool)random)
		{
			human.animator.runtimeAnimatorController = random;
		}
	}

	protected override void ActionOnRelease(BaseHuman human)
	{
		human.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(BaseHuman human)
	{
		if ((bool)human)
		{
			Object.Destroy(human.gameObject);
		}
	}
}
