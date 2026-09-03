using System;
using Helpers;

[Serializable]
public class ExpressionDataContainer
{
	public string itemName;

	public override string ToString()
	{
		return itemName;
	}

	public object GetLocalizationArgs()
	{
		return new
		{
			itemname = LocalizationHelper.GetItemLabel(itemName)
		};
	}
}
