using System;
using Helpers;

namespace Entities;

[Serializable]
public class NeighborhoodDemand
{
	public string neighborhood;

	public int demand;

	public int providers = -1;

	public int lastDaySold;

	public int lastDayProvidersExceeded;

	public bool hasPlayerMonopoly;

	public void RecalculateIfPlayerHasMonopoly(string itemName)
	{
		hasPlayerMonopoly = false;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.BuildingCached || buildingRegistration.Neighborhood != neighborhood)
			{
				continue;
			}
			if (buildingRegistration.RentedByPlayer)
			{
				foreach (string cachedAvailableProduct in buildingRegistration.cachedAvailableProducts)
				{
					if (!(cachedAvailableProduct != itemName))
					{
						hasPlayerMonopoly = true;
						break;
					}
				}
			}
			else
			{
				if (string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
				{
					continue;
				}
				foreach (string cachedAvailableProduct2 in buildingRegistration.cachedAvailableProducts)
				{
					if (!(cachedAvailableProduct2 != itemName))
					{
						hasPlayerMonopoly = false;
						return;
					}
				}
			}
		}
	}

	public bool IsHypeEventAvailable()
	{
		return lastDaySold + ProductMarketHelper.ProductMarketSettings.daysItTakesForAnItemNotToBeOnSaleToTriggerHypeEvents <= SaveGameManager.Current.Day;
	}

	public bool IsMaxProvidersExceededEventAvailable()
	{
		if (lastDayProvidersExceeded != 0)
		{
			return lastDayProvidersExceeded + ProductMarketHelper.ProductMarketSettings.daysWithoutExceedingMaxProvidersToCreateEvent <= SaveGameManager.Current.Day;
		}
		return true;
	}
}
