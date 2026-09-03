using Entities;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/StreetPerformerPool")]
public class StreetPerformerPool : Pool<StreetPerformer>
{
	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/StreetPerformer";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override StreetPerformer CreateFunc(Transform parent)
	{
		StreetPerformer streetPerformer = PrefabHelper.CreatePrefab<StreetPerformer>(GetPrefabName(), parent);
		_poolIndex++;
		streetPerformer.name = inspectorGameObjectName + _poolIndex;
		streetPerformer.InitAppearance();
		return streetPerformer;
	}

	protected override void ActionOnGet(StreetPerformer streetPerformer)
	{
		if (!ObjectPoolHelper.isPrewarming)
		{
			streetPerformer.gameObject.SetActive(value: true);
		}
	}

	protected override void ActionOnRelease(StreetPerformer streetPerformer)
	{
		streetPerformer.gameObject.SetActive(value: false);
		if (!ObjectPoolHelper.isPrewarming)
		{
			streetPerformer.Disable();
		}
	}

	protected override void ActionOnDestroy(StreetPerformer streetPerformer)
	{
		if ((bool)streetPerformer)
		{
			Object.Destroy(streetPerformer.gameObject);
		}
	}
}
