using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Indoors;
using Buildings.Outdoors;
using BusinessLayoutSets;
using Entities;
using Extensions;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using Streets;
using UI;
using UI.Guiders;
using UI.Smartphone.Apps.BizMan.Schedule;
using UnityEngine;
using UnityEngine.AI;

namespace Helpers;

public static class BuildingHelper
{
	public delegate bool BuildingRegistrationFilterDelegate(BuildingRegistration buildingRegistration);

	public delegate int BuildingRegistrationSortDelegate(BuildingRegistration x, BuildingRegistration y);

	public const int BuildingCapacityInfiniteNumber = 9999;

	public static List<Building> allBuildings = new List<Building>();

	public static readonly Dictionary<Address, Building> SpecialServiceBuildings = new Dictionary<Address, Building>();

	public static readonly Dictionary<string, List<Building>> AllNeighbourhoodBuildings = new Dictionary<string, List<Building>>();

	private static readonly List<BuildingRegistration> ImportExportRegistrations = new List<BuildingRegistration>();

	private static readonly List<BuildingRegistration> WholesaleRegistrations = new List<BuildingRegistration>();

	private static readonly HashSet<string> ShelfItemNames = new HashSet<string>();

	private static readonly Dictionary<Address, Building> AllBuildingDictionary = new Dictionary<Address, Building>();

	private static readonly Dictionary<Address, BuildingRegistration> AllBuildingRegistrationDictionary = new Dictionary<Address, BuildingRegistration>();

	private static readonly Dictionary<CargoInstance, ItemInstance> CargoInstanceParents = new Dictionary<CargoInstance, ItemInstance>();

	private static readonly Dictionary<(Address, string, bool, bool, bool), (int, int)> TotalResourcesInStockCache = new Dictionary<(Address, string, bool, bool, bool), (int, int)>();

	public const string AddressableLabel = "BuildingsDefinitions";

	public static void OnBuildingsLoaded(IList<Building> buildings)
	{
		AllBuildingRegistrationDictionary.Clear();
		ImportExportRegistrations.Clear();
		WholesaleRegistrations.Clear();
		allBuildings.Clear();
		allBuildings.AddRange(buildings);
		AllBuildingDictionary.Clear();
		AllBuildingDictionary.EnsureCapacity(allBuildings.Count);
		SpecialServiceBuildings.Clear();
		SpecialServiceBuildings.EnsureCapacity(allBuildings.Count / 8);
		AllNeighbourhoodBuildings.Clear();
		foreach (Building allBuilding in allBuildings)
		{
			AllBuildingDictionary.Add(allBuilding.Address, allBuilding);
			if (!AllNeighbourhoodBuildings.ContainsKey(allBuilding.Neighbourhood))
			{
				AllNeighbourhoodBuildings.Add(allBuilding.Neighbourhood, new List<Building>());
			}
			AllNeighbourhoodBuildings[allBuilding.Neighbourhood].Add(allBuilding);
			if (allBuilding.SpecialService != null)
			{
				SpecialServiceBuildings.Add(allBuilding.Address, allBuilding);
			}
		}
		ClosestBuildingFromPlayer.Init();
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Remove(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(OnWallsVisibilityChanged));
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Combine(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(OnWallsVisibilityChanged));
	}

	private static void OnWallsVisibilityChanged(WallsVisibility newWallsVisibility)
	{
		if (SaveGameManager.Current != null)
		{
			SaveGameManager.Current.wallsVisibility = newWallsVisibility;
		}
	}

	public static Transform GetAddressEntranceTransform(Address address)
	{
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address);
		if (!cityBuildingController || !cityBuildingController.building)
		{
			return null;
		}
		if (cityBuildingController.entranceDoors.Length == 0)
		{
			return null;
		}
		BuildingEntranceDoor buildingEntranceDoor = cityBuildingController.entranceDoors[0];
		if (buildingEntranceDoor == null || !buildingEntranceDoor.doorTransform)
		{
			return null;
		}
		return buildingEntranceDoor.doorTransform;
	}

	public static Building GetBuilding(Address address)
	{
		if (address == null || address.IsUndefined())
		{
			return null;
		}
		AllBuildingDictionary.TryGetValue(address, out var value);
		return value;
	}

	public static float GetBuildingSalePrice(this Address address)
	{
		return SaveGameManager.Current.buildingsForSale.FirstOrDefault((BuildingForSale x) => x.address == address)?.buildingPrice ?? 0f;
	}

	public static BuildingRegistration GetBuildingRegistration(Address address)
	{
		if (!CanTheAddressHaveARegistration(address))
		{
			return null;
		}
		if (AllBuildingRegistrationDictionary.TryGetValue(address, out var value))
		{
			return value;
		}
		BuildingRegistration buildingRegistration = SaveGameManager.Current.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == address.streetName && x.StreetNumber == address.streetNumber);
		if (buildingRegistration != null)
		{
			AllBuildingRegistrationDictionary.Add(address, buildingRegistration);
			return buildingRegistration;
		}
		Building building = GetBuilding(address);
		BuildingRegistration buildingRegistration2 = ((building.BuildingType == "ba:buildingtype_warehouse") ? new Warehouse() : new BuildingRegistration());
		buildingRegistration2.StreetName = address.streetName;
		buildingRegistration2.StreetNumber = address.streetNumber;
		SaveGameManager.Current.BuildingRegistrations.Add(buildingRegistration2);
		AllBuildingRegistrationDictionary.Add(address, buildingRegistration2);
		if (SpecialServiceBuildings.ContainsKey(address))
		{
			buildingRegistration2.BusinessName = building.SpecialService.businessName;
			buildingRegistration2.BusinessDescription = building.SpecialService.businessDescription;
			buildingRegistration2.businessTypeName = building.SpecialService.businessTypeName;
			buildingRegistration2.Layout = building.SpecialService.layout;
			buildingRegistration2.scheduleDays = building.SpecialService.scheduleDays;
			buildingRegistration2.signAppearanceSettings = building.SpecialService.signAppearanceSettings;
			buildingRegistration2.logoSettings = building.SpecialService.logoSettings;
			buildingRegistration2.customerCapacity = CompetitionHelper.GetAiBusinessCustomerCapacity(new BuildingSizeInfo(building), building.BuildingType);
			buildingRegistration2.RentedByPlayer = false;
			buildingRegistration2.AvailableForRent = false;
		}
		return buildingRegistration2;
	}

	private static bool CanTheAddressHaveARegistration(Address address)
	{
		if (address != null && !address.IsUndefined() && !string.IsNullOrEmpty(address.streetName))
		{
			return address.streetName != "ba:street_parking";
		}
		return false;
	}

	public static bool CanEnterBuilding(Address address)
	{
		BuildingRegistration buildingRegistration = GetBuildingRegistration(address);
		if (buildingRegistration.RentedByPlayer)
		{
			return true;
		}
		if (!BuildingTypeHelper.GetData(GetBuilding(address)).HasTag(TagRef.Buildingtypetag.cantenterunlessrented))
		{
			return BusinessHelper.IsBusinessOpen(buildingRegistration);
		}
		return false;
	}

	public static int GetBuildingSquareMeters(Address address)
	{
		return BuildingSizeHelper.GetData(GetBuilding(address)).squareMeters;
	}

	public static ScheduleDay GetTodaySchedule(BuildingRegistration registration)
	{
		if (registration.scheduleDays == null)
		{
			return null;
		}
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		foreach (ScheduleDay scheduleDay in registration.scheduleDays)
		{
			if (scheduleDay.day == dayOfWeek)
			{
				return scheduleDay;
			}
		}
		return null;
	}

	public static int CalculateDeposit(BuildingRegistration registration, float dailyRent = 0f)
	{
		if (registration.BuildingOwnedByPlayer)
		{
			return 0;
		}
		if (dailyRent == 0f)
		{
			dailyRent = registration.BuildingCached.GetBuildingDailyMarketRent();
		}
		int daysToCalculateDeposit = BuildingTypeHelper.GetData(registration).daysToCalculateDeposit;
		return Mathf.CeilToInt(dailyRent * (float)daysToCalculateDeposit);
	}

	public static int CalculateDefaultLayoutPrice(Address address)
	{
		Building building = GetBuilding(address);
		float num = 0f;
		foreach (BusinessLayoutSets.Item item in BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet("ba:businesstype_empty", new BuildingSizeInfo(building), "default").Items)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.itemName);
			num += ((byName.GetWholesalePrice() != 0f) ? byName.GetWholesalePrice() : byName.DefaultMarketPrice);
		}
		return Mathf.CeilToInt(num);
	}

	public static int GetPriceIndex(this BuildingRegistration buildingRegistration)
	{
		return buildingRegistration.BuildingCached.GetPriceIndex();
	}

	public static int GetPriceIndex(this Address address)
	{
		return GetBuilding(address).GetPriceIndex();
	}

	public static int GetPriceIndex(this Building building)
	{
		if (building.SpecialService == null)
		{
			return 100;
		}
		if (building.SpecialService.businessTypeName == "ba:businesstype_wholesalestore")
		{
			return Mathf.RoundToInt(104.99999f);
		}
		if (building.SpecialService.businessTypeName == "ba:businesstype_importexport")
		{
			return 100;
		}
		return building.SpecialService?.priceIndex ?? 100;
	}

	public static int CountTotalResourcesInStockCached(BuildingRegistration buildingRegistration, string itemName, bool includeProducers = true, bool includePalletShelves = true, bool includeBoxItemInstances = true)
	{
		return CountTotalResourcesInStockCached(buildingRegistration.Address, itemName, includeProducers, includePalletShelves, includeBoxItemInstances);
	}

	private static int CountTotalResourcesInStockCached(Address address, string itemName, bool includeProducers = true, bool includePalletShelves = true, bool includeBoxItemInstances = true)
	{
		(Address, string, bool, bool, bool) key = (address, itemName, includeProducers, includePalletShelves, includeBoxItemInstances);
		if (TotalResourcesInStockCache.TryGetValue(key, out var value))
		{
			var (num, result) = value;
			if (num == Time.frameCount)
			{
				return result;
			}
		}
		int num2 = CountTotalResourcesInStock(address, itemName, includeProducers, includePalletShelves, includeBoxItemInstances);
		if (TotalResourcesInStockCache.ContainsKey(key))
		{
			TotalResourcesInStockCache[key] = (Time.frameCount, num2);
		}
		else
		{
			TotalResourcesInStockCache.Add(key, (0, 0));
		}
		return num2;
	}

	public static int CountTotalResourcesInStock(BuildingRegistration buildingRegistration, string itemName, bool includeProducers = true, bool includePallets = true, bool includeBoxItemInstances = true)
	{
		return CountTotalResourcesInStock(buildingRegistration.Address, itemName, includeProducers, includePallets, includeBoxItemInstances);
	}

	public static int CountTotalResourcesInStock(Address address, string itemName)
	{
		return CountTotalResourcesInStock(address, itemName, includeProducers: true);
	}

	private static int CountTotalResourcesInStock(Address address, string itemName, bool includeProducers, bool includePallets = true, bool includeBoxItemInstances = true)
	{
		Dictionary<string, ItemInstance>.ValueCollection values = ItemHelper.GetItemsByAddress(address).Values;
		int num = 0;
		foreach (ItemInstance item in values)
		{
			if ((!includeBoxItemInstances && item.ItemCached.HasTag(TagRef.Itemtag.isbox)) || !ShouldCountResources(item, includeProducers, includePallets))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in item.cargoInstances)
			{
				if (cargoInstance.itemName == itemName)
				{
					num += cargoInstance.amount;
				}
			}
		}
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance == null || vehicleInstance.Address != address || SaveGameManager.Current.ActiveVehicleId == vehicleInstance.id)
			{
				continue;
			}
			foreach (CargoInstance cargoInstance2 in vehicleInstance.cargoInstances)
			{
				if (cargoInstance2.itemName == itemName)
				{
					num += cargoInstance2.amount;
				}
			}
		}
		return num;
	}

	public static bool HasResourcesInStock(Address address, string itemName, bool includeProducers = true, bool includePallets = true)
	{
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(address).Values)
		{
			if (!ShouldCountResources(value, includeProducers, includePallets))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				if (cargoInstance.itemName == itemName && cargoInstance.amount > 0)
				{
					return true;
				}
			}
		}
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance == null || vehicleInstance.Address != address || SaveGameManager.Current.ActiveVehicleId == vehicleInstance.id)
			{
				continue;
			}
			foreach (CargoInstance cargoInstance2 in vehicleInstance.cargoInstances)
			{
				if (cargoInstance2.itemName == itemName && cargoInstance2.amount > 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool ShouldCountResources(ItemInstance itemInstance, bool includeProducers, bool includePallets)
	{
		if (itemInstance.ItemCached.HasTag(TagRef.Itemtag.isstockcontainer))
		{
			return true;
		}
		if (includePallets && itemInstance.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
		{
			return true;
		}
		if (includeProducers && (itemInstance.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0)
		{
			return true;
		}
		return false;
	}

	public static int CountResourcesInPallets(Address address, string resourceName)
	{
		if (address == null || address.IsUndefined())
		{
			return 0;
		}
		int num = 0;
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(address).Values)
		{
			if (!value.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				if (cargoInstance.itemName == resourceName)
				{
					num += cargoInstance.amount;
				}
			}
		}
		return num;
	}

	public static void GetPrimaryItemsForSaleWithStockOrSales(BuildingRegistration registration, List<string> primaryProducts, List<string> results)
	{
		results.Clear();
		results.AddRange(registration.GetListOfItemsForSale());
		for (int num = results.Count - 1; num >= 0; num--)
		{
			string text = results[num];
			if (!primaryProducts.Contains(text))
			{
				results.RemoveAt(num);
			}
			else if (!HasResourcesInStock(registration.Address, text) && !BusinessHelper.CheckIfItemWasSold(registration, text))
			{
				results.RemoveAt(num);
			}
		}
	}

	public static void GetItemsWithStock(BuildingRegistration registration, string itemName, List<CargoInstance> result)
	{
		result.Clear();
		CargoInstanceParents.Clear();
		if (ShelfItemNames.Count == 0)
		{
			foreach (BigAmbitions.Items.Item item in from x in ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.iswarehousestorage).Select(ItemsGetter.GetByName)
				orderby x.cargoCapacity descending
				select x)
			{
				ShelfItemNames.Add(item.itemName);
			}
		}
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			if (!ShelfItemNames.Contains(value.itemName))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				if (!(cargoInstance.itemName != itemName) && cargoInstance.nestedCargoInstances.Count <= 0 && !CargoInstanceParents.ContainsKey(cargoInstance))
				{
					result.Add(cargoInstance);
					CargoInstanceParents.Add(cargoInstance, value);
				}
			}
		}
	}

	public static CargoInstance WithdrawFromCargo(List<CargoInstance> cargoInstances, int amount)
	{
		if (cargoInstances == null || cargoInstances.Count == 0 || amount <= 0)
		{
			return null;
		}
		CargoInstance cargoInstance = new CargoInstance(cargoInstances[0].itemName, 0, cargoInstances[0].pricePerUnit);
		for (int num = cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance2 = cargoInstances[num];
			if (cargoInstance2.amount > 0)
			{
				int num2 = Mathf.Min(amount, cargoInstance2.amount);
				cargoInstance.MergeAmount(cargoInstance2, num2);
				amount -= num2;
				CargoInstanceParents[cargoInstance2].OnItemsInCargoUpdated()?.Invoke();
				if (amount == 0)
				{
					break;
				}
			}
		}
		return cargoInstance;
	}

	public static void ReturnToCargo(List<CargoInstance> cargoInstances, CargoInstance returnedCargoInstance)
	{
		if (returnedCargoInstance == null || returnedCargoInstance.amount <= 0)
		{
			return;
		}
		if (cargoInstances != null)
		{
			foreach (CargoInstance cargoInstance in cargoInstances)
			{
				if (cargoInstance.itemName != returnedCargoInstance.itemName)
				{
					continue;
				}
				ItemInstance itemInstance = CargoInstanceParents[cargoInstance];
				int maxStockCapacity = cargoInstance.GetMaxStockCapacity(itemInstance);
				int num = Mathf.Min(returnedCargoInstance.amount, maxStockCapacity - cargoInstance.amount);
				if (num > 0)
				{
					cargoInstance.MergeAmount(returnedCargoInstance, num);
					returnedCargoInstance.amount -= num;
					itemInstance.OnItemsInCargoUpdated()?.Invoke();
					if (returnedCargoInstance.amount == 0)
					{
						break;
					}
				}
			}
		}
		if (returnedCargoInstance.amount > 0 && Application.isEditor)
		{
			Debug.LogWarning("Returned cargo instance still has amount left after trying to merge back into cargoInstances. This means it couldn't be fully merged back and might be lost if not handled properly. Remaining amount: " + returnedCargoInstance.amount);
		}
	}

	public static void RemoveEmptyCargo(List<CargoInstance> cargoInstances)
	{
		if (cargoInstances == null || cargoInstances.Count == 0)
		{
			return;
		}
		for (int num = cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance = cargoInstances[num];
			if (cargoInstance.amount <= 0)
			{
				CargoInstanceParents[cargoInstance].RemoveFromCargo(cargoInstance);
			}
		}
	}

	public static float CalculateBuildingSellingPrice(BuildingRegistration registration)
	{
		float num = registration.RentPerDay + registration.lastDeposit;
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			num += value.GetSellingPrice();
		}
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.Address == registration.Address)
			{
				num += vehicleInstance.GetSellingPrice();
			}
		}
		return num;
	}

	public static float SellItemsAndVehiclesInBuilding(BuildingRegistration registration)
	{
		bool flag = registration.BuildingCached.IsHamptonsHouse();
		Dictionary<string, ItemInstance>.ValueCollection values = registration.itemInstances.Values;
		float num = 0f;
		List<VehicleInstance> vehicleInstances = SaveGameManager.Current.VehicleInstances;
		for (int num2 = vehicleInstances.Count - 1; num2 >= 0; num2--)
		{
			VehicleInstance vehicleInstance = vehicleInstances[num2];
			if (!(vehicleInstance.Address != registration.Address))
			{
				num += vehicleInstance.GetSellingPrice();
				VehicleController vehicleController = null;
				if (flag && vehicleInstance.VehicleType.IsMotorVehicle)
				{
					vehicleController = VehicleHelper.GetVehicleController(vehicleInstance);
				}
				vehicleInstance.Delete(vehicleController);
			}
		}
		foreach (ItemInstance item in values)
		{
			num += item.GetSellingPrice();
		}
		registration.itemInstances.Clear();
		return num;
	}

	public static Address ParseAddressString(string value)
	{
		string[] array = value.Trim().Split(" ");
		return new Address(streetNumber: int.Parse(array[0]), streetName: AddressHelper.GetStreetNameByAbbreviation(array[1]));
	}

	public static bool VehicleSlotIsUsed(CityBuildingController cbc, int vehicleSlot)
	{
		if (!cbc.buildingRegistration.RentedByPlayer)
		{
			return false;
		}
		if (!(cbc.buildingRegistration is Warehouse warehouse))
		{
			return false;
		}
		int num = vehicleSlot - 1;
		if (num < 0 || num >= warehouse.vehicleSlots.Count)
		{
			return false;
		}
		VehicleSlot vehicleSlot2 = warehouse.vehicleSlots[num];
		if (vehicleSlot2.vehicleInstanceId.IsNullOrEmpty())
		{
			return false;
		}
		if (warehouse.HasDuplicateSlotAssignment(vehicleSlot2.vehicleInstanceId))
		{
			return false;
		}
		return IsVehicleAtAddress(vehicleSlot2.vehicleInstanceId, cbc.building.Address);
	}

	public static bool IsVehicleAtAddress(string vehicleId, Address address)
	{
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.id == vehicleId)
			{
				return vehicleInstance.IsAtAddress(address);
			}
		}
		return false;
	}

	public static void SellBuilding(Address address, string transactionText)
	{
		BuildingRegistration buildingRegistration = GetBuildingRegistration(address);
		BizManSchedule.AbortAutoFillForBusiness(buildingRegistration);
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = address
		}))
		{
			EmployeeHelper.UnassignEmployeeFromAllWorkshifts(employeeInstance);
			employeeInstance.assignedAddress = null;
		}
		float num = CalculateBuildingSellingPrice(buildingRegistration);
		Dictionary<string, string> data = new Dictionary<string, string> { { "text", transactionText } };
		TransactionInfo info = new TransactionInfo("ba:transaction_compatibilityfix", data);
		SaveGameManager.Current.Money += num;
		SaveGameManager.Current.Transactions.Enqueue(new Transaction(info)
		{
			amount = num,
			address = buildingRegistration.Address
		});
		SaveGameManager.Current.VehicleInstances.RemoveAll((VehicleInstance x) => x.Address == address);
		buildingRegistration.Reset();
		buildingRegistration.AvailableForRent = true;
	}

	public static bool CustomersNeedPaperBagsInCurrentBuilding()
	{
		if (InstanceBehavior<BuildingManager>.Instance.businessType == null)
		{
			return false;
		}
		return InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.customersneedpaperbags);
	}

	public static bool IsAnyCarBlockingTheEntrance(VehicleController currentVehicle, int vehicleSlot, Building building)
	{
		int num = Math.Max(vehicleSlot - 1, 0);
		ExitZone exitZone = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(building)).GetComponentsInChildren<ExitZone>()[num];
		if (!(currentVehicle.vehicleCollider is MeshCollider meshCollider))
		{
			return false;
		}
		Vector3 forward = exitZone.playerSpawnPoint.forward;
		Vector3 position = exitZone.playerSpawnPoint.position + forward * (meshCollider.sharedMesh.bounds.size.z * 0.5f);
		NavMeshObstacle navMeshObstacle = currentVehicle.navMeshObstacle;
		Bounds bounds = BoundsHelper.CreateBounds(navMeshObstacle.size, navMeshObstacle.center, position, Quaternion.LookRotation(forward), Vector3.one);
		foreach (VehicleInstance item in SaveGameManager.Current.VehicleInstances.Where((VehicleInstance x) => x.Address == building.Address))
		{
			int autoDestroyAfterMinutes = item.VehicleType.autoDestroyAfterMinutes;
			if (autoDestroyAfterMinutes <= 0 || !AutoDestroyVehicle.ShouldDestroyVehicle(item, autoDestroyAfterMinutes))
			{
				(Vector3, Vector3) vehicleColliderCenterAndSize = VehicleHelper.GetVehicleColliderCenterAndSize(item.vehicleTypeName);
				if (BoundsHelper.CreateBounds(vehicleColliderCenterAndSize.Item2, vehicleColliderCenterAndSize.Item1, item.position, item.rotation, Vector3.one).Intersects(bounds))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void RentBuilding(Building building, float dailyRent, float lastDeposit)
	{
		BuildingRegistration registration = building.GetRegistration();
		if (!registration.AvailableForRent && building.BuildingType != "ba:buildingtype_residential")
		{
			CompetitionHelper.ShutdownBusinessesImmediate(new BuildingRegistration[1] { registration });
		}
		if (building.IsHamptonsHouse() || registration.GetBuildingType() == "ba:buildingtype_residential")
		{
			HappinessHelper.RemoveModifier("ba:happinessmodifier_no_home");
			HappinessHelper.AddModifier("ba:happinessmodifier_first_apartment");
		}
		ResetRegistrationOnRent(building, dailyRent, lastDeposit, registration);
		BusinessLayoutSetHelper.InsertBusinessLayoutSet(building.Address, "ba:businesstype_empty", new BuildingSizeInfo(building), "default");
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(building.Address);
		cityBuildingController?.UpdatePoi();
		if (cityBuildingController is CityHamptonsHouseController cityHamptonsHouseController)
		{
			cityHamptonsHouseController.OnRentedBuilding();
		}
		InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		GuidersManager.UpdateGuidersWithAddress(registration.Address);
		GameEvent.Invoke("ba:gameevent_rentedbuilding");
		GlobalEvents.onBuildingRegistrationChange?.Invoke(building.Address);
		ProductMarketHelper.UpdateMarketDemands();
	}

	private static void ResetRegistrationOnRent(Building building, float dailyRent, float lastDeposit, BuildingRegistration buildingRegistration)
	{
		buildingRegistration.ResetScheduleDays();
		buildingRegistration.ResetBuildingSpecific();
		buildingRegistration.RentPerDay = dailyRent;
		buildingRegistration.lastDeposit = lastDeposit;
		buildingRegistration.RentedByPlayer = true;
		buildingRegistration.AvailableForRent = false;
		buildingRegistration.businessTypeName = "ba:businesstype_empty";
		buildingRegistration.BusinessName = null;
		buildingRegistration.logoSettings = new LogoSettings();
		buildingRegistration.dirtSpots = BuildingCleanlinessHelper.GetDirtSpotsForBuilding(building);
		buildingRegistration.cachedAvailableProducts.Clear();
	}

	public static List<BuildingRegistration> GetPlayerBuildingRegistrations(BuildingRegistrationFilterDelegate filterDelegate = null, BuildingRegistrationSortDelegate sortDelegate = null)
	{
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && (filterDelegate == null || filterDelegate(buildingRegistration)))
			{
				list.Add(buildingRegistration);
			}
		}
		if (sortDelegate == null)
		{
			sortDelegate = DefaultBuildingRegistrationSort;
		}
		list.Sort((BuildingRegistration x, BuildingRegistration y) => sortDelegate(x, y));
		return list;
	}

	public static List<BuildingRegistration> GetImportExportBuildingRegistrations()
	{
		if (ImportExportRegistrations.Count > 0)
		{
			return ImportExportRegistrations;
		}
		foreach (Building allBuilding in allBuildings)
		{
			if (BuildingTypeHelper.GetData(allBuilding).HasTag(TagRef.Buildingtypetag.canimportexport))
			{
				BuildingRegistration registration = allBuilding.GetRegistration();
				if (registration.businessTypeName == "ba:businesstype_importexport")
				{
					ImportExportRegistrations.Add(registration);
				}
			}
		}
		return ImportExportRegistrations;
	}

	public static IReadOnlyList<BuildingRegistration> GetWholesaleBuildingRegistrations()
	{
		if (WholesaleRegistrations.Count > 0)
		{
			return WholesaleRegistrations;
		}
		for (int i = 0; i < allBuildings.Count; i++)
		{
			BuildingRegistration registration = allBuildings[i].GetRegistration();
			if (registration.businessTypeName == "ba:businesstype_wholesalestore")
			{
				WholesaleRegistrations.Add(registration);
			}
		}
		return WholesaleRegistrations;
	}

	public static BuildingRegistration FindClosestWholesaleStore(Address address)
	{
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address);
		if (cityBuildingController == null)
		{
			return null;
		}
		IReadOnlyList<BuildingRegistration> wholesaleBuildingRegistrations = GetWholesaleBuildingRegistrations();
		BuildingRegistration result = null;
		float num = float.MaxValue;
		foreach (BuildingRegistration item in wholesaleBuildingRegistrations)
		{
			CityBuildingController cityBuildingController2 = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(item.Address);
			if (!(cityBuildingController2 == null))
			{
				float num2 = Vector3.SqrMagnitude(cityBuildingController.transform.position - cityBuildingController2.transform.position);
				if (!(num2 >= num))
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	private static int DefaultBuildingRegistrationSort(BuildingRegistration x, BuildingRegistration y)
	{
		return string.Compare(x.GetDisplayName().ToLower(), y.GetDisplayName().ToLower(), StringComparison.Ordinal);
	}

	public static void SetBuildingAreaLocalization(TextLocalizationComponent textLocalizationComponent, Building building)
	{
		string text = GetBuildingSquareMeters(building.Address).ToFormattedArea();
		if (building.IsHamptonsAIVilla())
		{
			textLocalizationComponent.Key = string.Empty;
			textLocalizationComponent.TextContainer.SetText(text);
		}
		else
		{
			textLocalizationComponent.SetData(LanguageChangeEventDataHolder.Create("bizman_building_area_value", new
			{
				squaremeters = text,
				buildingSize = building.BuildingSize.GetIdWithoutType().CapitalizeFirstChar(),
				buildingVersion = building.BuildingVersion
			}));
		}
	}

	public static bool IsHamptonsBuildingOwnedByRival(Building building)
	{
		foreach (SpecialRival specialRival in RivalsHelper.GetSpecialRivals())
		{
			if (!(specialRival.hamptonsBuilding != building))
			{
				return true;
			}
		}
		return false;
	}

	[ConsoleMethod("RerollAIBusiness", "Re-rolls the AI business at the specified address", new string[] { }, AutoCompleteMap = new string[] { "streetName=StreetNames", "businessType=BusinessTypes" })]
	public static void RerollAiBusiness(int streetNumber, string streetName, string businessType)
	{
		if (string.IsNullOrEmpty(businessType) || businessType == "ba:businesstype_empty" || streetNumber < 1 || string.IsNullOrEmpty(streetName))
		{
			return;
		}
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.cityBuildingControllers.SingleOrDefault((CityBuildingController x) => x.building.Address.streetName.Equals(streetName) && x.building.Address.streetNumber.Equals(streetNumber));
		if ((bool)cityBuildingController)
		{
			BuildingRegistration registration = cityBuildingController.building.GetRegistration();
			if (!registration.RentedByPlayer && !registration.BuildingOwnedByPlayer)
			{
				AiBusinessDefault randomBusinessDefault = CompetitionHelper.GetBusinessDefaultsByType(businessType).GetRandomBusinessDefault(registration);
				string rivalIdForBusinessDefault = CompetitionHelper.GetRivalIdForBusinessDefault(randomBusinessDefault);
				CompetitionHelper.StartNewCompetitorBusiness(businessType, registration, impactMarket: true, randomBusinessDefault, rivalIdForBusinessDefault);
				cityBuildingController.UpdateSign();
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		allBuildings = new List<Building>();
		SpecialServiceBuildings.Clear();
		AllNeighbourhoodBuildings.Clear();
		ImportExportRegistrations.Clear();
		AllBuildingDictionary.Clear();
		AllBuildingRegistrationDictionary.Clear();
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Remove(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(OnWallsVisibilityChanged));
	}
}
