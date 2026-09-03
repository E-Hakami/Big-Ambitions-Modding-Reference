using System;
using System.Collections.Generic;

namespace UI.Smartphone.Apps.MyEmployees;

public static class EmployeeMassActionHelper
{
	public const string AddressableLabel = "EmployeeMassActions";

	private static readonly Dictionary<string, EmployeeMassAction> MassActionsDict = new Dictionary<string, EmployeeMassAction>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string[]> MassActionsByTabDict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

	public static void OnMassActionsLoaded(IList<EmployeeMassAction> massActions)
	{
		MassActionsDict.Clear();
		MassActionsByTabDict.Clear();
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(massActions.Count, StringComparer.OrdinalIgnoreCase);
		foreach (EmployeeMassAction massAction in massActions)
		{
			MassActionsDict[massAction.type] = massAction;
			foreach (string supportedTab in massAction.supportedTabs)
			{
				if (!dictionary.TryGetValue(supportedTab, out var value))
				{
					value = new List<string>(4);
					dictionary.Add(supportedTab, value);
				}
				value.Add(massAction.type);
			}
		}
		foreach (KeyValuePair<string, List<string>> item in dictionary)
		{
			MassActionsByTabDict.Add(item.Key, item.Value.ToArray());
		}
	}

	public static EmployeeMassAction GetMassAction(string type)
	{
		return MassActionsDict.GetValueOrDefault(type);
	}

	public static string[] GetMassActionsByTab(string tab)
	{
		return MassActionsByTabDict.GetValueOrDefault(tab);
	}
}
