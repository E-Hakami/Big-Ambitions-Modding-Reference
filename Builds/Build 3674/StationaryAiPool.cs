using BigAmbitions.Characters.Appearance;
using Entities;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/StationaryAiPool")]
public class StationaryAiPool : Pool<StationaryAiBehavior>
{
	[SerializeField]
	private bool initializeOnCreate = true;

	[SerializeField]
	private AppearanceTag[] appearanceTags;

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/DummyAi";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override StationaryAiBehavior CreateFunc(Transform parent)
	{
		StationaryAiBehavior stationaryAiBehavior = PrefabHelper.CreatePrefab<StationaryAiBehavior>(GetPrefabName(), parent);
		stationaryAiBehavior.data.appearanceTags = appearanceTags;
		if (initializeOnCreate)
		{
			stationaryAiBehavior.Initialize(initAppearance: true, initAnimations: false);
		}
		_poolIndex++;
		stationaryAiBehavior.name = inspectorGameObjectName + _poolIndex;
		return stationaryAiBehavior;
	}

	protected override void ActionOnGet(StationaryAiBehavior stationaryAiBehavior)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			stationaryAiBehavior.gameObject.SetActive(value: true);
			return;
		}
		stationaryAiBehavior.gameObject.SetActive(value: true);
		stationaryAiBehavior.Enable();
	}

	protected override void ActionOnRelease(StationaryAiBehavior stationaryAiBehavior)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			stationaryAiBehavior.gameObject.SetActive(value: false);
			return;
		}
		stationaryAiBehavior.Disable();
		stationaryAiBehavior.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(StationaryAiBehavior stationaryAiBehavior)
	{
		if ((bool)stationaryAiBehavior)
		{
			Object.Destroy(stationaryAiBehavior.gameObject);
		}
	}
}
