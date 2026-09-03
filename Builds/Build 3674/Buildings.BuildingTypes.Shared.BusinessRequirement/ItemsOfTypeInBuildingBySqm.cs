using BigAmbitions.Items;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/ItemsOfTypeInBuildingBySqm")]
public class ItemsOfTypeInBuildingBySqm : ItemsOfTypeInBuilding
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

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		int num = GetRequiredItemCount(registration);
		if (num == 0)
		{
			return true;
		}
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			if ((value.ItemCached.type & itemType) != 0)
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
