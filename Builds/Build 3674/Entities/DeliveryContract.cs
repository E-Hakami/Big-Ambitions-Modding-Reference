using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;

namespace Entities;

[Serializable]
public class DeliveryContract
{
	public bool enabled;

	public bool isUrgentOrder;

	public int nextDeliveryDay;

	public bool repeatingOrder;

	public Address wholesaleAddress;

	public Address businessAddress;

	public float deliveryFee;

	public List<DeliveryContractItem> items;

	public float TotalPricePerDelivery
	{
		get
		{
			float num = (float)wholesaleAddress.GetPriceIndex() / 100f;
			float num2 = deliveryFee;
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(wholesaleAddress);
			foreach (DeliveryContractItem item in items)
			{
				if (!ProductMarketHelper.IsProductInMarketEvent(item.itemName, MarketEventType.ProductBackorder, buildingRegistration.Neighborhood))
				{
					Item byName = ItemsGetter.GetByName(item.itemName);
					float num3 = (float)DeliveryHelper.GetOrderAmount(item, byName, buildingRegistration) * byName.GetWholesalePrice() * num * ProductMarketHelper.GetProductMarketEventMultiplier(item.itemName, buildingRegistration.Neighborhood);
					num2 += num3;
				}
			}
			if (isUrgentOrder)
			{
				num2 *= DeliveryHelper.GetWholesaleUrgentFeeMultiplier();
			}
			return num2;
		}
	}

	public bool HasItemsToDeliver()
	{
		foreach (DeliveryContractItem item in items)
		{
			if (item.amount > 0)
			{
				return true;
			}
		}
		return false;
	}

	public void UpdateNextDeliveryDay()
	{
		nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay();
	}

	public void Remove()
	{
		SaveGameManager.Current.DeliveryContracts.Remove(this);
	}
}
