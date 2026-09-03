using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;
using Entities;
using Extensions;
using Streets;
using UI;
using UI.Notification;
using UnityEngine;

namespace Helpers;

public static class RealEstateHelper
{
	public const float MinRentRelativeToAvgMarket = 0.8f;

	public const float MaxRentRelativeToAvgMarket = 1.2f;

	private const float PossibilityOfTenantsMoving = 20f;

	private const float PossibilityOfCompetitorBuyingBuilding = 10f;

	private const int SaleThresholdDependingOnPrice = 2;

	private const int TargetOfBuildingsOnSalePerNeighborhood = 3;

	private const int DaysUntilBuildingCanBeOnSaleAgain = 30;

	private const float MinProbabilityOfAIAcceptingBuildingOffer = 0.98f;

	private static readonly List<BuildingForSale> BuildingsForSale = new List<BuildingForSale>();

	public static void RunDaily()
	{
		UpdatePlayerRealEstate();
		if ((float)Random.Range(0, 100) <= 10f)
		{
			SimulateCompetitorBuyingAIBuildings();
		}
		SimulateCompetitorBuyingPlayerBuildings();
		UpdateBuildingsForSale();
	}

	private static void UpdatePlayerRealEstate()
	{
		foreach (RealEstate item in SaveGameManager.Current.realEstate)
		{
			if (item.Building.IsHamptonsHouse())
			{
				continue;
			}
			if (item.daysUntilUpdatingPricePerSqm > 0)
			{
				item.daysUntilUpdatingPricePerSqm--;
				if (item.daysUntilUpdatingPricePerSqm == 0)
				{
					item.pricePerSqm = item.pendingPricePerSqm;
				}
			}
			float buildingDailyMarketRentPerSqm = item.Building.GetBuildingDailyMarketRentPerSqm();
			float num = (buildingDailyMarketRentPerSqm - item.pricePerSqm) / buildingDailyMarketRentPerSqm;
			float num2 = Mathf.Clamp(num * 100f, -20f, 20f);
			float num3 = 20f + num2;
			float num4 = ((num > 0f) ? 0f : 6.6666665f);
			if ((float)Random.Range(0, 100) < num3)
			{
				item.occupancy = Mathf.Min(item.occupancy + (float)Random.Range(2, 8), item.MaxOccupancy);
			}
			if ((float)Random.Range(0, 100) < num4)
			{
				item.occupancy = Mathf.Max(item.occupancy - (float)Random.Range(2, 8), 0f);
			}
			if (item.DailyIncome != 0f)
			{
				Dictionary<string, string> data = new Dictionary<string, string> { 
				{
					"address",
					item.address.ToFormattedString()
				} };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_rentrevenue", data);
				GameManager.ChangeMoneySafe(item.DailyIncome, transactionInfo, SaveGameManager.Current.Day - 1, item.address, force: true);
			}
		}
	}

	private static void SimulateCompetitorBuyingPlayerBuildings()
	{
		BuildingsForSale.Clear();
		foreach (BuildingForSale item in SaveGameManager.Current.buildingsForSale)
		{
			if (item.BuildingRegistration.BuildingOwnedByPlayer)
			{
				BuildingsForSale.Add(item);
			}
		}
		foreach (BuildingForSale buildingForSale in BuildingsForSale)
		{
			float marketValue = buildingForSale.Building.GetMarketValue();
			float num = Mathf.Clamp((marketValue - buildingForSale.buildingPrice) / marketValue * 100f, -2f, 2f) * 5f;
			float num2 = 10f + num;
			if (!((float)Random.Range(0, 100) >= num2))
			{
				float num3 = buildingForSale.buildingPrice;
				float num4 = 0f;
				CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(buildingForSale.address);
				if (buildingForSale.Building.IsHamptonsHouse() && cityBuildingController is CityHamptonsHouseController cityHamptonsHouseController)
				{
					cityHamptonsHouseController.OnBuildingSold();
					num4 = BuildingHelper.SellItemsAndVehiclesInBuilding(buildingForSale.BuildingRegistration);
					num3 += num4;
				}
				Dictionary<string, string> dictionary = new Dictionary<string, string>
				{
					{
						"address",
						buildingForSale.address.ToFormattedString()
					},
					{
						"price",
						num3.ToShortCurrencyFormat()
					},
					{
						"amount",
						num3.ToShortCurrencyFormat()
					}
				};
				if (num4 > 0f)
				{
					dictionary.Add("itemsAmount", num4.ToShortCurrencyFormat());
				}
				string headerKey = ((num4 > 0f) ? "real_estate_building_sold_notification_items_sold" : "real_estate_building_sold_notification");
				Notifications.Show(NotificationType.Success, headerKey, dictionary);
				Dictionary<string, string> data = new Dictionary<string, string> { 
				{
					"address",
					buildingForSale.address.ToFormattedString()
				} };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_buildingsold", data);
				GameManager.ChangeMoneySafe(num3, transactionInfo);
				buildingForSale.BuildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding(buildingForSale.BuildingRegistration.Neighborhood, buildingForSale.Building.IsHamptonsHouse());
				SaveGameManager.Current.realEstate.RemoveAll((RealEstate x) => x.address == buildingForSale.address);
				SaveGameManager.Current.buildingsForSale.Remove(buildingForSale);
				if (buildingForSale.BuildingRegistration.RentedByPlayer)
				{
					buildingForSale.BuildingRegistration.RentPerDay = buildingForSale.Building.GetBuildingDailyMarketRent();
				}
				cityBuildingController?.UpdatePoi();
				InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
				AddNoHomeModifierIfNeeded();
			}
		}
	}

	private static void SimulateCompetitorBuyingAIBuildings()
	{
		BuildingsForSale.Clear();
		foreach (BuildingForSale item in SaveGameManager.Current.buildingsForSale)
		{
			if (!item.BuildingRegistration.BuildingOwnedByPlayer && !item.Building.IsHamptonsHouse())
			{
				BuildingsForSale.Add(item);
			}
		}
		BuildingForSale random = BuildingsForSale.GetRandom();
		if (random != null)
		{
			random.BuildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding(random.BuildingRegistration.Neighborhood);
			SaveGameManager.Current.buildingsForSale.Remove(random);
		}
	}

	private static void UpdateBuildingsForSale()
	{
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			BuildingsForSale.Clear();
			foreach (BuildingForSale item in SaveGameManager.Current.buildingsForSale)
			{
				if (item.Neighbourhood == neighborhood)
				{
					BuildingsForSale.Add(item);
				}
			}
			if (BuildingsForSale.Count < 3)
			{
				List<BuildingRegistration> possibleBuildingsToSetForSale = GetPossibleBuildingsToSetForSale(neighborhood, BuildingsForSale);
				if (possibleBuildingsToSetForSale.Count > 0)
				{
					SetBuildingForSale(possibleBuildingsToSetForSale.GetRandom());
				}
			}
		}
	}

	private static List<BuildingRegistration> GetPossibleBuildingsToSetForSale(string neighborhood, List<BuildingForSale> buildingsForSale)
	{
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.Neighborhood != neighborhood || buildingRegistration.BuildingOwnedByPlayer || buildingRegistration.BuildingCached.SpecialService != null)
			{
				continue;
			}
			bool flag = true;
			foreach (BuildingForSale item in buildingsForSale)
			{
				if (!(item.address != buildingRegistration.Address))
				{
					flag = false;
					break;
				}
			}
			if (flag && IsBuildingAvailableForSale(buildingRegistration))
			{
				list.Add(buildingRegistration);
			}
		}
		return list;
	}

	private static bool IsBuildingAvailableForSale(BuildingRegistration registration)
	{
		if (registration.lastDayOnSale != 0)
		{
			return SaveGameManager.Current.Day - registration.lastDayOnSale > 30;
		}
		return true;
	}

	public static void SetBuildingForSale(BuildingRegistration buildingToSetForSale)
	{
		BuildingForSale item = new BuildingForSale
		{
			address = buildingToSetForSale.Address,
			buildingPrice = buildingToSetForSale.BuildingCached.GetMarketValue(),
			squareMeters = BuildingHelper.GetBuildingSquareMeters(buildingToSetForSale.Address),
			acceptOfferRate = Random.Range(0.98f, 1f)
		};
		buildingToSetForSale.buildingOwnerRivalId = string.Empty;
		buildingToSetForSale.lastDayOnSale = SaveGameManager.Current.Day;
		SaveGameManager.Current.buildingsForSale.Add(item);
	}

	public static void SellBuildingForCompat(BuildingRegistration reg)
	{
		SellBuildingForCompat(reg.RealEstate.purchasePrice, reg.Address);
	}

	public static void SellBuildingForCompat(RealEstate realEstate)
	{
		SellBuildingForCompat(realEstate.purchasePrice, realEstate.address);
	}

	private static void SellBuildingForCompat(double doublePrice, Address address)
	{
		float num = (float)doublePrice;
		BuildingForSale buildingForSale = SaveGameManager.Current.buildingsForSale.FirstOrDefault((BuildingForSale x) => x.address == address);
		if (buildingForSale != null)
		{
			num = buildingForSale.buildingPrice;
			SaveGameManager.Current.buildingsForSale.Remove(buildingForSale);
		}
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"text",
			address.ToFormattedString() + " was sold (caused by compatibility support)"
		} };
		TransactionInfo info = new TransactionInfo("ba:transaction_compatibilityfix", data);
		SaveGameManager.Current.Money += num;
		SaveGameManager.Current.Transactions.Enqueue(new Transaction(info)
		{
			amount = num,
			address = address
		});
		SaveGameManager.Current.realEstate.RemoveAll((RealEstate x) => x.address == address);
	}

	public static bool IsOnSale(this BuildingRegistration buildingRegistration)
	{
		return SaveGameManager.Current.buildingsForSale.Exists((BuildingForSale x) => x.address == buildingRegistration.Address);
	}

	public static void AddNoHomeModifierIfNeeded()
	{
		if (!SaveGameManager.Current.BuildingRegistrations.Exists((BuildingRegistration x) => x.GetBuildingType() == "ba:buildingtype_residential" && x.RentedByPlayer) && !SaveGameManager.Current.realEstate.Exists((RealEstate x) => x.Building.IsHamptonsHouse()))
		{
			HappinessHelper.RemoveModifier("ba:happinessmodifier_first_apartment");
			HappinessHelper.AddModifier("ba:happinessmodifier_no_home");
		}
	}
}
