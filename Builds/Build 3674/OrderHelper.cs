using AI.Citizens;
using Entities;
using UnityEngine;

public static class OrderHelper
{
	public static float CalculateOrderDemandScore(Order order, BuildingRegistration buildingRegistration)
	{
		if (order.customerDemandTypes.Count == 0)
		{
			return 100f;
		}
		int num = 0;
		foreach (string customerDemandType in order.customerDemandTypes)
		{
			if (buildingRegistration.cachedFulfilledCustomerDemands.Contains(customerDemandType))
			{
				num++;
			}
		}
		return Mathf.RoundToInt((float)num * 100f / (float)order.customerDemandTypes.Count);
	}

	public static void Validate(CitizenData citizenData, OrderEntry orderEntry, ItemController itemController, bool payIfAcceptable = false)
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			orderEntry.available = !itemController || itemController.IsAmountAvailable(1);
			orderEntry.price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
			orderEntry.priceAccceptable = citizenData.IsPriceAcceptable(orderEntry.itemName, orderEntry.price);
			if ((orderEntry.available && orderEntry.priceAccceptable) & payIfAcceptable)
			{
				orderEntry.paid = true;
			}
			orderEntry.wholesalePrice = (itemController ? itemController.ItemInstance.GetStockInstance().pricePerUnit : 0f);
		}
		else
		{
			orderEntry.available = true;
			orderEntry.priceAccceptable = true;
		}
	}
}
