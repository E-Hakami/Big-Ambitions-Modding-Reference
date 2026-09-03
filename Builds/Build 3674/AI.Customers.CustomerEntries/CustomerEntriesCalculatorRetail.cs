using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace AI.Customers.CustomerEntries;

public class CustomerEntriesCalculatorRetail : CustomerEntriesCalculator
{
	private const int BuildNumberToStartCappingInitialCustomers = 2847;

	public override float GetInitialCustomers()
	{
		if (SaveGameManager.Current.buildNumberAtStart >= 2847)
		{
			return base.GetInitialCustomers();
		}
		float maxHcpsqmForRegistration = BusinessHelper.GetMaxHcpsqmForRegistration(registration);
		int buildingSquareMeters = BuildingHelper.GetBuildingSquareMeters(registration.Address);
		return maxHcpsqmForRegistration * (float)buildingSquareMeters;
	}

	public override int GetCustomersByHour(int hour, DayOfWeekOrdered day, float initialCustomers)
	{
		return Mathf.CeilToInt(Mathf.Min(initialCustomers * GetPromotionMultiplier() * GetDayAndHourMultiplier(hour, day), registration.customerCapacity));
	}

	protected override void AddProductsToCustomersEntriesList()
	{
		float customerSatisfactionMultiplier = GetCustomerSatisfactionMultiplier();
		foreach (string cachedAvailableProduct in registration.cachedAvailableProducts)
		{
			if (cachedAvailableProduct == businessType.defaultEntranceFee || cachedAvailableProduct == businessType.weekendOnlyEntranceFee || ItemsGetter.GetByName(cachedAvailableProduct).HasTag(TagRef.Itemtag.isticket))
			{
				continue;
			}
			Item byName = ItemsGetter.GetByName(cachedAvailableProduct);
			float productImpact = businessType.GetProductImpact(cachedAvailableProduct);
			float num = (byName.isADemandedProduct ? ((float)ProductMarketHelper.GetNeighborhoodDemand(cachedAvailableProduct, registration.Neighborhood).demand) : 100f);
			float num2 = ((!(businessType.maxAmountPerProduct > 1f)) ? businessType.maxAmountPerProduct : (businessType.IsPrimaryProduct(cachedAvailableProduct) ? Random.Range(1f, businessType.maxAmountPerProduct) : 1f));
			float f = (float)customersEntryList.Count * ((byName.productSalesRatio == 0f) ? 1f : byName.productSalesRatio) * (num / 100f) * customerSatisfactionMultiplier * productImpact * num2;
			int num3 = ((Random.Range(0f, 1f) > 0.3f) ? Mathf.RoundToInt(f) : Mathf.CeilToInt(f));
			if ((byName.type & ItemType.ServiceProduct) != 0)
			{
				while (num3 > 0)
				{
					int num4 = Random.Range(0, customersEntryList.Count);
					for (int i = 0; i < customersEntryList.Count; i++)
					{
						int num5 = i + num4;
						if (num5 >= customersEntryList.Count)
						{
							num5 -= customersEntryList.Count;
						}
						bool flag = true;
						foreach (OrderEntry entry in customersEntryList[num5].order.entries)
						{
							if (entry.itemName == cachedAvailableProduct)
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							customersEntryList[num5].order.entries.Add(new OrderEntry
							{
								itemName = cachedAvailableProduct
							});
							break;
						}
					}
					num3--;
				}
			}
			else
			{
				while (num3 > 0)
				{
					customersEntryList.GetRandom().order.entries.Add(new OrderEntry
					{
						itemName = cachedAvailableProduct
					});
					num3--;
				}
			}
		}
	}
}
