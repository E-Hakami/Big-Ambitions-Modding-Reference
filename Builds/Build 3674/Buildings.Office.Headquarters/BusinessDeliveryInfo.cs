using System;
using System.Collections.Generic;
using System.Text;
using BigAmbitions.Items;
using Localizor;

namespace Buildings.Office.Headquarters;

[Serializable]
public class BusinessDeliveryInfo
{
	public string businessName;

	public List<ItemAmountTarget> itemAmounts = new List<ItemAmountTarget>();

	public BusinessDeliveryInfo(string businessName)
	{
		this.businessName = businessName;
	}

	public BusinessDeliveryInfo(string businessName, List<ItemAmountTarget> itemAmounts)
	{
		this.businessName = businessName;
		this.itemAmounts = itemAmounts;
	}

	public static string GetLocalizedList(List<BusinessDeliveryInfo> deliveryInfoList)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<margin-left=1em>");
		foreach (BusinessDeliveryInfo deliveryInfo in deliveryInfoList)
		{
			stringBuilder.Append("\\u2022<indent=1em>");
			deliveryInfo.AppendLocalizedTo(stringBuilder);
			stringBuilder.Append("</indent><br>");
		}
		return stringBuilder.ToString();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendLocalizedTo(stringBuilder);
		return stringBuilder.ToString();
	}

	private void AppendLocalizedTo(StringBuilder stringBuilder)
	{
		stringBuilder.Append("<b><noparse>" + businessName + "</noparse></b>: ");
		for (int i = 0; i < itemAmounts.Count; i++)
		{
			string localization = itemAmounts[i].itemName.GetLocalization();
			stringBuilder.Append("<i><noparse>" + localization + "</noparse></i>");
			if (i < itemAmounts.Count - 1)
			{
				stringBuilder.Append(", ");
			}
		}
	}
}
