using System.Collections.Generic;
using UnityEngine.UI;

namespace BlueprintsUI;

public class BlueprintFilter
{
	public Toggle allFilterToggle;

	public List<BlueprintFilterOption> filterOptions;

	public string localizationKey;

	public string label;

	public bool localizeLabel = true;

	public override int GetHashCode()
	{
		int num = 17;
		foreach (BlueprintFilterOption filterOption in filterOptions)
		{
			if (filterOption.toggled)
			{
				num = num * 31 + filterOption.GetHashCode();
			}
		}
		return num * 31 + (allFilterToggle.isOn ? 1 : 0);
	}
}
