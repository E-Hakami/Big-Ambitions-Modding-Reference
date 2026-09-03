using BigAmbitions.Items;
using Entities;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/SpecificItemsInBuildingBySqm")]
public class SpecificItemsInBuildingBySqm : SpecificItemsInBuilding
{
	public int squareMetersPerItem;

	public int maxItems;

	public int GetRequiredItemCount(BuildingRegistration registration)
	{
		int num = Mathf.CeilToInt((float)BuildingSizeHelper.GetData(registration).squareMeters / (float)squareMetersPerItem);
		if (maxItems > 0)
		{
			num = Mathf.Min(num, maxItems);
		}
		return num;
	}

	public override TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.MissingRequiredItemCount;
	}

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		int num = GetRequiredItemCount(registration);
		if (num == 0)
		{
			return true;
		}
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			bool flag = false;
			string[] array = items;
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i] != value.itemName))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				num--;
				if (num == 0)
				{
					return true;
				}
			}
		}
		return false;
	}
}
