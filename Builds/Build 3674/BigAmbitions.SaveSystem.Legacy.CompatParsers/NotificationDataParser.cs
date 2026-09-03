using System;
using System.Collections.Generic;
using UI.Notification;

namespace BigAmbitions.SaveSystem.Legacy.CompatParsers;

public static class NotificationDataParser
{
	public static Dictionary<string, string> ParseData(NotificationData data)
	{
		if (data == null)
		{
			return null;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		Add(dictionary, "price", data.price);
		Add(dictionary, "name", data.name);
		Add(dictionary, "address", data.address);
		Add(dictionary, "itemname", data.itemname);
		Add(dictionary, "skill", data.skill);
		Add(dictionary, "type", data.type);
		Add(dictionary, "warehouseName", data.warehouseName);
		Add(dictionary, "goal", data.goal);
		Add(dictionary, "amount", data.amount);
		Add(dictionary, "index", data.index);
		Add(dictionary, "slotNumber", data.slotNumber);
		Add(dictionary, "employeeName", data.employeeName);
		Add(dictionary, "vehicle", data.vehicle);
		Add(dictionary, "businessName", data.businessName);
		Add(dictionary, "fromname", data.fromname);
		Add(dictionary, "toname", data.toname);
		Add(dictionary, "minimumCost", data.minimumCost);
		Add(dictionary, "towAddress", data.towAddress);
		Add(dictionary, "shelf", data.shelf);
		Add(dictionary, "sender", data.sender);
		Add(dictionary, "fromTime", data.fromTime);
		Add(dictionary, "toTime", data.toTime);
		Add(dictionary, "dlc", data.dlc);
		Add(dictionary, "dayOfWeek", data.dayOfWeek);
		Add(dictionary, "healthPlanType", data.healthPlanType);
		if (dictionary.Count != 0)
		{
			return dictionary;
		}
		return null;
	}

	private static void Add(Dictionary<string, string> parsedData, string key, object value)
	{
		if (value == null)
		{
			return;
		}
		if (value is string value2)
		{
			if (!string.IsNullOrEmpty(value2))
			{
				parsedData.Add(key, value2);
			}
		}
		else if (value is IFormattable formattable)
		{
			parsedData.Add(key, formattable.ToString(null, null));
		}
		else
		{
			parsedData.Add(key, value.ToString());
		}
	}
}
