using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;
using Streets;
using UI.InteriorDesigner;
using UnityEngine;

namespace Entities;

public static class CustomerDemandHelper
{
	private static readonly Dictionary<string, CustomerDemand> CustomerDemands;

	public static string AddressableLabel => "CustomerDemands";

	static CustomerDemandHelper()
	{
		CustomerDemands = new Dictionary<string, CustomerDemand>();
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(ReloadCachedFulfilled));
	}

	public static void OnCustomerDemandsLoaded(IList<CustomerDemand> customerDemands)
	{
		CustomerDemands.Clear();
		CustomerDemands.EnsureCapacity(customerDemands.Count);
		foreach (CustomerDemand customerDemand in customerDemands)
		{
			CustomerDemands.Add(customerDemand.demandType, customerDemand);
		}
	}

	public static void ReloadCachedFulfilled()
	{
		ReloadCachedFulfilled(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
	}

	public static void ReloadCachedFulfilled(Address address)
	{
		if (!(address == null) && !address.IsUndefined())
		{
			ReloadCachedFulfilled(BuildingHelper.GetBuildingRegistration(address));
		}
	}

	public static void ReloadCachedFulfilled(BuildingRegistration registration)
	{
		BusinessType data = BusinessTypeHelper.GetData(registration);
		if (data == null)
		{
			registration.cachedFulfilledCustomerDemands = new List<string>();
			return;
		}
		HashSet<Item> hashSet = new HashSet<Item>();
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			Item itemCached = value.ItemCached;
			hashSet.Add(itemCached);
		}
		List<string> list = new List<string>();
		foreach (CustomerDemandSet customerDemandSet in data.customerDemandSets)
		{
			CustomerDemand byType = GetByType(customerDemandSet.type);
			if (byType.Fulfilled(registration, hashSet))
			{
				list.Add(byType.demandType);
			}
		}
		registration.cachedFulfilledCustomerDemands = list;
	}

	public static CustomerDemand GetByType(string type)
	{
		return CustomerDemands[type];
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		CustomerDemands.Clear();
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(ReloadCachedFulfilled));
	}
}
