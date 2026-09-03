using Localizor;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class EmployeeNameOverlay : IOverlay
{
	[SerializeField]
	private TMP_Text employeeNameField;

	public override bool IsValid(EntityController entityController)
	{
		if (!(entityController is EmployeeStationController))
		{
			return entityController is BusinessEmployeeController;
		}
		return true;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController is EmployeeStationController employeeStationController)
		{
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				return !string.IsNullOrEmpty(employeeStationController?.employeeInstance?.characterData?.name);
			}
			return false;
		}
		if (entityController is BusinessEmployeeController businessEmployeeController)
		{
			return !string.IsNullOrEmpty(businessEmployeeController.EmployeeName);
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		employeeNameField.text = GetEmployeeName(entityController);
	}

	private static string GetEmployeeName(EntityController entityController)
	{
		if (entityController is EmployeeStationController employeeStationController)
		{
			return employeeStationController.employeeInstance.characterData.name;
		}
		if (entityController is BusinessEmployeeController businessEmployeeController)
		{
			return businessEmployeeController.EmployeeName.Localize().ToString();
		}
		return string.Empty;
	}
}
