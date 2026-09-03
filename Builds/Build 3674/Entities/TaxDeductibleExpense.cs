using System;
using System.Collections.Generic;
using Localizor;

namespace Entities;

[Serializable]
public class TaxDeductibleExpense
{
	public string key;

	public float amount;

	public Dictionary<string, string> values;

	public static Dictionary<string, string> CopyValuesForArguments(string key, Dictionary<string, string> sourceValues, string excludedKey)
	{
		if (sourceValues == null || sourceValues.Count <= 1 || !LocalizorManager.IsLocalizedKey(key))
		{
			return null;
		}
		string localization = key.GetLocalization();
		Dictionary<string, string> dictionary = null;
		int num = 0;
		while (num < localization.Length)
		{
			int num2 = localization.IndexOf('{', num);
			if (num2 < 0)
			{
				return dictionary;
			}
			int num3 = localization.IndexOf('}', num2 + 1);
			if (num3 < 0)
			{
				return dictionary;
			}
			num = num3 + 1;
			int num4 = num3 - num2 - 1;
			if (num4 <= 0)
			{
				continue;
			}
			string text = localization.Substring(num2 + 1, num4);
			if (!(text == excludedKey) && sourceValues.TryGetValue(text, out var value))
			{
				if (dictionary == null)
				{
					dictionary = new Dictionary<string, string>();
				}
				dictionary[text] = value;
			}
		}
		return dictionary;
	}

	public static bool HasMatchingKeyAndValues(TaxDeductibleExpense expense, string key, Dictionary<string, string> values)
	{
		if (expense.key == key)
		{
			return AreValuesSame(expense.values, values);
		}
		return false;
	}

	private static bool AreValuesSame(Dictionary<string, string> first, Dictionary<string, string> second)
	{
		int num = first?.Count ?? 0;
		int num2 = second?.Count ?? 0;
		if (num != num2)
		{
			return false;
		}
		if (num == 0)
		{
			return true;
		}
		foreach (KeyValuePair<string, string> item in first)
		{
			if (second == null || !second.TryGetValue(item.Key, out var value) || value != item.Value)
			{
				return false;
			}
		}
		return true;
	}
}
