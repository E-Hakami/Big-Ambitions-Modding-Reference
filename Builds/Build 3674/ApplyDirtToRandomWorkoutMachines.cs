using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Shared.Dirtiness;
using UnityEngine;

[TaskCategory("Big Ambitions/Gym")]
public class ApplyDirtToRandomWorkoutMachines : Action
{
	[SharedRequired]
	public SharedInt sharedNumberOfMachines = 1;

	public override void OnStart()
	{
		List<ItemController> list = (from _ in InstanceBehavior<BuildingManager>.Instance.allItemControllers
			where (_.Item.type & ItemType.WorkoutMachine) != 0
			orderby Random.value
			select _).ToList();
		for (int num = 0; num < sharedNumberOfMachines.Value; num++)
		{
			ItemController itemController;
			if (num >= list.Count)
			{
				itemController = list[list.Count - 1];
			}
			else
			{
				itemController = list[num];
			}
			ItemController itemController2 = itemController;
			BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, itemController2.ItemInstance);
		}
	}
}
