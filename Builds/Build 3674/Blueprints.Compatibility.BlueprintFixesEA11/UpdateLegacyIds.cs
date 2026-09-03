using System.Collections.Generic;
using BigAmbitions.SaveSystem.Legacy;
using BusinessLayoutSets;

namespace Blueprints.Compatibility.BlueprintFixesEA11;

public class UpdateLegacyIds : IBlueprintCompatibilityFix
{
	private const string BuildingSizePrefix = "ba:buildingsize_";

	private const string BuildingTypePrefix = "ba:buildingtype_";

	private const string BusinessTypePrefix = "ba:businesstype_";

	public void Apply(Blueprint blueprint, BusinessLayoutSet layout, CompatibilityFixScope scope)
	{
		if (scope.HasFlag(CompatibilityFixScope.Metadata))
		{
			ApplyMetadata(blueprint?.metadata);
		}
		if (scope.HasFlag(CompatibilityFixScope.Layout) && layout != null)
		{
			ApplyLayout(layout);
		}
	}

	private static void ApplyMetadata(BlueprintMetadata metadata)
	{
		if (metadata != null)
		{
			if (metadata.buildingSizeInfo?.buildingSize != null)
			{
				metadata.buildingSizeInfo.buildingSize = NormalizeBuildingSizeId(metadata.buildingSizeInfo.buildingSize);
			}
			string dataElementValue = metadata.GetDataElementValue(DataElement.BusinessTypeName);
			if (!string.IsNullOrEmpty(dataElementValue))
			{
				SetDataElementValue(metadata, DataElement.BusinessTypeName, NormalizeBusinessTypeId(dataElementValue));
			}
			if (!string.IsNullOrEmpty(metadata?.buildingType))
			{
				metadata.buildingType = NormalizeBuildingTypeId(metadata.buildingType);
			}
		}
	}

	private static void SetDataElementValue(BlueprintMetadata metadata, DataElement dataElement, string value)
	{
		if (metadata != null)
		{
			if (metadata.otherData == null)
			{
				metadata.otherData = new List<BlueprintDataElement>();
			}
			BlueprintDataElement blueprintDataElement = metadata.otherData.Find((BlueprintDataElement x) => x.dataElement == dataElement);
			if (blueprintDataElement != null)
			{
				blueprintDataElement.value = value;
			}
			else
			{
				metadata.otherData.Add(new BlueprintDataElement(dataElement, value));
			}
		}
	}

	private static void ApplyLayout(BusinessLayoutSet layout)
	{
		if (!string.IsNullOrEmpty(layout.BuildingSize) && !layout.BuildingSize.Contains('_'))
		{
			layout.BuildingSize = GetBuildingSizeId(layout.BuildingSize);
		}
		if (!string.IsNullOrEmpty(layout.BusinessType) && !layout.BusinessType.Contains('_'))
		{
			layout.BusinessType = GetBusinessTypeId(layout.BusinessType);
		}
		if (layout.Items == null)
		{
			return;
		}
		foreach (Item item in layout.Items)
		{
			if (!string.IsNullOrEmpty(item?.itemName) && !item.itemName.Contains('_'))
			{
				item.itemName = GetMigratedItemName(item.itemName);
			}
			if (!string.IsNullOrEmpty(item?.linkedItemName) && !item.linkedItemName.Contains('_'))
			{
				item.linkedItemName = GetMigratedItemName(item.linkedItemName);
			}
			if (!string.IsNullOrEmpty(item?.playerItemPurchaserSettings?.itemName) && !item.playerItemPurchaserSettings.itemName.Contains('_'))
			{
				item.playerItemPurchaserSettings.itemName = GetMigratedItemName(item.playerItemPurchaserSettings.itemName);
			}
			if (item?.stackedItems == null)
			{
				continue;
			}
			foreach (AttachableChild stackedItem in item.stackedItems)
			{
				if (!string.IsNullOrEmpty(stackedItem?.childItemName) && !stackedItem.childItemName.Contains('_'))
				{
					stackedItem.childItemName = GetMigratedItemName(stackedItem.childItemName);
				}
			}
		}
	}

	private static string GetNewId(string oldId, string prefix)
	{
		return prefix + oldId.ToLower();
	}

	private static string GetMigratedItemName(string oldId)
	{
		if (string.IsNullOrEmpty(oldId))
		{
			return null;
		}
		if (!int.TryParse(oldId, out var result))
		{
			return null;
		}
		return LegacyHelper.Map<ItemNameLegacyMap>(result, logErrors: false);
	}

	private static string NormalizeBuildingSizeId(string value)
	{
		value = value.Trim();
		if (!value.Contains('_'))
		{
			return GetBuildingSizeId(value);
		}
		return value;
	}

	private static string NormalizeBusinessTypeId(string value)
	{
		value = value.Trim();
		if (value.Contains('_'))
		{
			return value;
		}
		if (int.TryParse(value, out var result))
		{
			return LegacyHelper.Map<BusinessTypeLegacyMap>(result, logErrors: false);
		}
		return "ba:businesstype_" + value.ToLower();
	}

	private static string NormalizeBuildingTypeId(string value)
	{
		value = value.Trim();
		if (!value.Contains('_'))
		{
			return GetBuildingTypeId(value);
		}
		return value;
	}

	private static string GetBuildingSizeId(string legacyValue)
	{
		if (int.TryParse(legacyValue, out var result))
		{
			return LegacyHelper.Map<BuildingSizeLegacyMap>(result, logErrors: false);
		}
		return GetNewId(legacyValue, "ba:buildingsize_");
	}

	private static string GetBusinessTypeId(string legacyValue)
	{
		if (int.TryParse(legacyValue, out var result))
		{
			return LegacyHelper.Map<BusinessTypeLegacyMap>(result, logErrors: false);
		}
		return GetNewId(legacyValue, "ba:businesstype_");
	}

	private static string GetBuildingTypeId(string legacyValue)
	{
		if (int.TryParse(legacyValue, out var result))
		{
			return LegacyHelper.Map<BuildingTypeLegacyMap>(result, logErrors: false);
		}
		return GetNewId(legacyValue, "ba:buildingtype_");
	}
}
