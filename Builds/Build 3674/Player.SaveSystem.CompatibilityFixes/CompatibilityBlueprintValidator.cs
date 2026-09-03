using Blueprints;
using BusinessLayoutSets;

namespace Player.SaveSystem.CompatibilityFixes;

public static class CompatibilityBlueprintValidator
{
	private const int BuildNumber010 = 3430;

	public static bool ShowInGallery(Blueprint blueprint)
	{
		if (!(blueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName) != "ba:businesstype_factory"))
		{
			return blueprint.metadata.buildNumber >= 3430;
		}
		return true;
	}

	public static bool ContainsInvalidItems(BusinessLayoutSet layoutSet)
	{
		for (int num = layoutSet.Items.Count - 1; num >= 0; num--)
		{
			Item item = layoutSet.Items[num];
			if (!string.IsNullOrEmpty(item.itemName) && !CompatibilityItemValidator.IsValidItemName(item.itemName))
			{
				return true;
			}
		}
		return false;
	}

	public static BusinessLayoutSet ValidateLayout(BusinessLayoutSet layoutSet)
	{
		CompatibilityItemValidator.ClearCache();
		for (int num = layoutSet.Items.Count - 1; num >= 0; num--)
		{
			Item item = layoutSet.Items[num];
			if (string.IsNullOrEmpty(item.itemName))
			{
				layoutSet.Items.RemoveAt(num);
			}
			else if (!CompatibilityItemValidator.IsValidItemName(item.itemName))
			{
				layoutSet.Items.RemoveAt(num);
			}
		}
		return layoutSet;
	}
}
