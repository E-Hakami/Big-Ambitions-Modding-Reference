using Localizor;
using Tooltip;
using UnityEngine;

public class BasicTooltip : TooltipTarget
{
	public string titleKey;

	public string descriptionKey;

	protected override void ShowTooltip()
	{
		bool num = !string.IsNullOrEmpty(titleKey);
		bool flag = !string.IsNullOrEmpty(descriptionKey);
		if (num)
		{
			TooltipSystem.AddHeader(titleKey.Localize(localizationArguments));
			if (flag)
			{
				TooltipSystem.AddSplitter();
			}
		}
		if (flag)
		{
			string[] array = descriptionKey.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				TooltipSystem.AddLabel(array[i].Localize(localizationArguments), Color.white);
			}
		}
	}
}
