using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Extensions;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class SelfServiceCustomerTryUseScaleInAction : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedBool sharedScaleUsed;

	private ItemController _scaleController;

	public override void OnStart()
	{
		sharedScaleUsed.Value = false;
		_scaleController = FindRandomScale();
		if (!(_scaleController == null) && _scaleController.TryGetRandomAvailableRealNavMeshTargetPosition(out var _))
		{
			BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _scaleController.ItemInstance);
			sharedScaleUsed.Value = true;
		}
	}

	private ItemController FindRandomScale()
	{
		return InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x.Item.HasTag(TagRef.Itemtag.isweighingscale)).GetRandom();
	}
}
