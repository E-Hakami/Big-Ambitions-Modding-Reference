using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using EmployeeStations;
using Extensions;
using UnityEngine;

public static class WaitingLinesHelper
{
	private static readonly Dictionary<string, List<WaitingLine>> LinesDictionary = new Dictionary<string, List<WaitingLine>>();

	private static readonly List<WaitingLine> WaitingLinesInBuilding = new List<WaitingLine>();

	private static List<ItemController> AllItemControllersInBuilding = new List<ItemController>();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		LinesDictionary.Clear();
		WaitingLinesInBuilding.Clear();
		AllItemControllersInBuilding = new List<ItemController>();
	}

	public static void Init(List<ItemController> allItemControllersInBuilding)
	{
		AllItemControllersInBuilding = allItemControllersInBuilding;
		LinesDictionary.Clear();
		WaitingLinesInBuilding.Clear();
	}

	private static IEnumerable<WaitingLine> LoadWaitingLines(string[] controllerNames)
	{
		if (WaitingLinesInBuilding.Count == 0)
		{
			foreach (ItemController item in AllItemControllersInBuilding)
			{
				if (!item)
				{
					continue;
				}
				PlayerItemPurchaserSettings playerItemPurchaserSettings = item.playerItemPurchaserSettings;
				if ((playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled) && item.TryGetComponent<IWaitingLineHolder>(out var component))
				{
					WaitingLine waitingLine = component.GetWaitingLine();
					if ((bool)waitingLine)
					{
						WaitingLinesInBuilding.Add(waitingLine);
					}
				}
			}
		}
		List<WaitingLine> list = new List<WaitingLine>();
		foreach (WaitingLine waitingLine2 in WaitingLinesInBuilding)
		{
			if (controllerNames.Any((string x) => waitingLine2.MatchesControllerName(x)))
			{
				list.Add(waitingLine2);
			}
		}
		LinesDictionary.Add(controllerNames.Stringify(), list);
		return list;
	}

	public static IEnumerable<WaitingLine> FindWaitingLines(string[] controllerNames)
	{
		if (LinesDictionary.TryGetValue(controllerNames.Stringify(), out var value))
		{
			return value;
		}
		return LoadWaitingLines(controllerNames);
	}

	public static IEnumerable<WaitingLine> GetAvailableWaitingLines(string[] controllerNames)
	{
		return FindWaitingLines(controllerNames).Where(IsWaitingLineAvailable);
	}

	public static WaitingLine GetLessCrowdedWaitingLine(string[] controllerNames)
	{
		return (from x in GetAvailableWaitingLines(controllerNames)
			where x.data.GetAmountOfSpotsAvailable() > 0
			orderby x.data.GetCustomersInWaitingLine(includeGoingToWaitingLine: true), x.data.customers.Count
			select x).FirstOrDefault();
	}

	public static bool IsThereAWaitingLineWithSpotsAvailable(string[] controller)
	{
		return GetAvailableWaitingLines(controller).Any((WaitingLine x) => x.data.GetAmountOfSpotsAvailable() > 0);
	}

	private static bool IsWaitingLineAvailable(WaitingLine waitingLine)
	{
		EmployeeStationController employeeStationController = waitingLine.EmployeeStationController;
		if ((bool)employeeStationController && (!employeeStationController.employee || employeeStationController.employee.IsAway))
		{
			return false;
		}
		return !ItemHelper.HasAnyMissingRequirements(waitingLine.ItemController.ItemInstance);
	}
}
