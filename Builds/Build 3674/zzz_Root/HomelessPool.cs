using Entities;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/HomelessPool")]
public class HomelessPool : Pool<Homeless>
{
	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/Homeless";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override Homeless CreateFunc(Transform parent)
	{
		Homeless homeless = PrefabHelper.CreatePrefab<Homeless>(GetPrefabName(), parent);
		_poolIndex++;
		homeless.name = inspectorGameObjectName + _poolIndex;
		homeless.Init();
		return homeless;
	}

	protected override void ActionOnGet(Homeless homeless)
	{
		homeless.gameObject.SetActive(value: true);
		if (!ObjectPoolHelper.isPrewarming)
		{
			homeless.Enable();
		}
	}

	protected override void ActionOnRelease(Homeless homeless)
	{
		homeless.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(Homeless homeless)
	{
		if ((bool)homeless)
		{
			Object.Destroy(homeless.gameObject);
		}
	}
}
