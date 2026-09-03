using BigAmbitions.Characters.Appearance;
using Extensions;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/WaterPedestrianPool")]
public class WaterPedestrianPool : Pool<WaterPedestrian>
{
	private static readonly AppearanceTag[] AppearanceTags = new AppearanceTag[1] { AppearanceTag.SwimmingPool };

	private static readonly int Swimming = Animator.StringToHash("Swimming");

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/WaterPedestrian";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override WaterPedestrian CreateFunc(Transform parent)
	{
		Vector3 navmeshSafePositionForNpcs = NavMeshHelper.GetNavmeshSafePositionForNpcs();
		WaterPedestrian waterPedestrian = PrefabHelper.CreatePrefab<WaterPedestrian>(GetPrefabName(), navmeshSafePositionForNpcs, Quaternion.identity, parent);
		InitHuman(waterPedestrian);
		return waterPedestrian;
	}

	private void InitHuman(WaterPedestrian waterPedestrian)
	{
		waterPedestrian.tpc.appearanceSetter.SetRandomAppearance(AppearanceTags);
		_poolIndex++;
		waterPedestrian.name = inspectorGameObjectName + _poolIndex;
	}

	protected override void ActionOnGet(WaterPedestrian waterPedestrian)
	{
		waterPedestrian.gameObject.SetActive(value: true);
		waterPedestrian.tpc.animator.SetBool(Swimming, value: true);
		waterPedestrian.tpc.navmeshAgent.enabled = true;
	}

	protected override void ActionOnRelease(WaterPedestrian waterPedestrian)
	{
		waterPedestrian.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(WaterPedestrian waterPedestrian)
	{
		if ((bool)waterPedestrian)
		{
			Object.Destroy(waterPedestrian.gameObject);
		}
	}
}
