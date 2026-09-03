using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/SpecialEmployeeType")]
public class TutorialPointerDataWorldSpecialEmployeeType : TutorialPointerDataWorldItem
{
	[SerializeField]
	[SearchableEnum]
	protected SpecialEmployeeController.SpecialEmployeeType employeeType;

	public override void FindEntityController()
	{
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (allItemController is SpecialEmployeeController specialEmployeeController && specialEmployeeController.GetEmployeeType == employeeType)
			{
				ItemController itemController = allItemController;
				entityControllerTarget = allItemController;
				while (itemController.parentItemController != null)
				{
					itemController = (ItemController)(entityControllerTarget = itemController.parentItemController);
				}
				break;
			}
		}
	}
}
