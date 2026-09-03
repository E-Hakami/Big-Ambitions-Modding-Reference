using System.Collections.Generic;
using BigAmbitions.Factories;
using BigAmbitions.Items;
using Extensions;
using Localizor;
using TMPro;
using UnityEngine;

namespace Tooltip;

public class ItemInfoTooltip : TooltipTarget
{
	public Item targetItem;

	protected override void ShowTooltip()
	{
		string localization = targetItem.itemName.GetLocalization();
		int addedCustomersPerHour = targetItem.addedCustomersPerHour;
		bool flag = (targetItem.type & ItemType.FactoryMachine) != 0;
		List<string> list = new List<string>();
		if (flag)
		{
			foreach (string possibleWorkstation in FactoriesHelper.GetPossibleWorkstations(targetItem.itemName))
			{
				list.Add(possibleWorkstation.GetLocalization());
			}
		}
		else
		{
			string[] itemsThatCanShowcase = targetItem.itemsThatCanShowcase;
			foreach (string label in itemsThatCanShowcase)
			{
				list.Add(label.GetLocalization());
			}
		}
		TooltipSystem.AddLabel(localization, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
		if (addedCustomersPerHour > 0)
		{
			TooltipSystem.AddLabel(string.Format("{0}: {1}", "bizman_customers_capacity".GetLocalization(), addedCustomersPerHour), Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
		}
		if (list.Count != 0)
		{
			TooltipSystem.AddLabel((flag ? "blueprintdata_workstations" : "common_products").GetLocalization() + ":\n" + list.Listify(), Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
		}
	}
}
