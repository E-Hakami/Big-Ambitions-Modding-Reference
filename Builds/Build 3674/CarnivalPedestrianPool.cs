using BigAmbitions.Characters.Appearance;
using Extensions;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/CarnivalPedestrianPool")]
public class CarnivalPedestrianPool : Pool<CarnivalPedestrian>
{
	private static readonly AppearanceTag[] AppearanceTags = new AppearanceTag[1] { AppearanceTag.Casual };

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/CarnivalPedestrian";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override CarnivalPedestrian CreateFunc(Transform parent)
	{
		Vector3 navmeshSafePositionForNpcs = NavMeshHelper.GetNavmeshSafePositionForNpcs();
		CarnivalPedestrian carnivalPedestrian = PrefabHelper.CreatePrefab<CarnivalPedestrian>(GetPrefabName(), navmeshSafePositionForNpcs, Quaternion.identity, parent);
		InitHuman(carnivalPedestrian);
		return carnivalPedestrian;
	}

	private void InitHuman(CarnivalPedestrian carnivalPedestrian)
	{
		carnivalPedestrian.tpc.appearanceSetter.SetRandomAppearance(AppearanceTags);
		_poolIndex++;
		carnivalPedestrian.name = inspectorGameObjectName + _poolIndex;
		carnivalPedestrian.skinnedMeshRenderer = carnivalPedestrian.tpc.appearanceSetter.meshCombiner.GetCombinedMeshSkinnedMeshRenderer();
	}

	protected override void ActionOnGet(CarnivalPedestrian carnivalPedestrian)
	{
		carnivalPedestrian.gameObject.SetActive(value: true);
		carnivalPedestrian.tpc.navmeshAgent.enabled = true;
		carnivalPedestrian.skinnedMeshRenderer.enabled = true;
	}

	protected override void ActionOnRelease(CarnivalPedestrian carnivalPedestrian)
	{
		carnivalPedestrian.gameObject.SetActive(value: false);
		carnivalPedestrian.ResetCarnivalPedestrian();
	}

	protected override void ActionOnDestroy(CarnivalPedestrian carnivalPedestrian)
	{
		if ((bool)carnivalPedestrian)
		{
			Object.Destroy(carnivalPedestrian.gameObject);
		}
	}
}
