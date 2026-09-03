using BigAmbitions.SaveSystem.Legacy;
using BusinessLayoutSets;

namespace Blueprints.Compatibility.BlueprintFixesEA11;

public class UpdateWorkstationLegacyIds : IBlueprintCompatibilityFix
{
	public void Apply(Blueprint blueprint, BusinessLayoutSet layout, CompatibilityFixScope scope)
	{
		if ((scope & CompatibilityFixScope.Layout) == 0)
		{
			return;
		}
		foreach (Item item in layout.Items)
		{
			if (item is FactoryItem factoryItem && int.TryParse(factoryItem.workstationType, out var result))
			{
				factoryItem.workstationType = LegacyHelper.Map<FactoryWorkstationTypeLegacyMap>(result, logErrors: false);
			}
		}
	}
}
