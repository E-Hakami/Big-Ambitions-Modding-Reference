using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Extensions;

[TaskCategory("Big Ambitions/ItemController")]
public class GetItemControllerByItemNames : Action
{
	[SharedRequired]
	public SharedItemController itemController;

	public SharedItemTag itemTag;

	public SharedBool checkForOccupied;

	public SharedBool useClosest;

	public override void OnStart()
	{
		string[] itemsWithTag = itemTag.AllWithTag;
		IEnumerable<ItemController> source = InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => itemsWithTag.Contains(x.itemName));
		if (checkForOccupied.Value)
		{
			source = source.Where((ItemController x) => !x.Occupied);
		}
		IOrderedEnumerable<ItemController> source2 = source.OrderBy((ItemController x) => MathHelper.DistanceSqr(x.transform.position, transform.position));
		itemController.Value = (useClosest.Value ? source2.FirstOrDefault() : source2.Take(5).GetRandom());
	}

	public override TaskStatus OnUpdate()
	{
		if (!(itemController.Value == null))
		{
			return TaskStatus.Success;
		}
		return TaskStatus.Failure;
	}
}
