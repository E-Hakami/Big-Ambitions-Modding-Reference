using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class AnySharedItemName : Conditional
{
	public SharedItemTag itemTag;

	public override TaskStatus OnUpdate()
	{
		List<ItemController> allItemControllers = InstanceBehavior<BuildingManager>.Instance.allItemControllers;
		string[] allWithTag = itemTag.AllWithTag;
		for (int i = 0; i < allItemControllers.Count; i++)
		{
			string itemName = allItemControllers[i].itemName;
			for (int j = 0; j < allWithTag.Length; j++)
			{
				if (allWithTag[j] == itemName)
				{
					return TaskStatus.Success;
				}
			}
		}
		return TaskStatus.Failure;
	}
}
