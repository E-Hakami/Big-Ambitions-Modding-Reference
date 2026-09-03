using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Player.PlayerMissions;
using UI.Notification;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace Vehicles.DeliveryDriverJob;

public static class DeliveryJobHelper
{
	private static readonly List<string> DestinationItemPool = new List<string>();

	private static readonly List<DeliveryJobDestination> GeneratedDestinations = new List<DeliveryJobDestination>();

	private static readonly List<ItemAmountTarget> GeneratedItems = new List<ItemAmountTarget>();

	private static readonly List<CargoInstance> ItemsToRemove = new List<CargoInstance>();

	private static bool OnlyLowProductDestinations;

	public static List<DeliveryJobDestination> GenerateDestinations(DeliveryJobStartLocation location, Vector3 startPosition)
	{
		int num = 0;
		GeneratedDestinations.Clear();
		int num2 = VehicleTypeHelper.GetVehicleType(location.vehicleTypeName).maxCargoCapacity - 1;
		int num3 = Random.Range(location.destinationsCountMin, location.destinationsCountMax + 1);
		for (int i = 0; i < num3 * 10; i++)
		{
			if (num >= num2)
			{
				break;
			}
			if (GeneratedDestinations.Count >= num3)
			{
				break;
			}
			Address randomAddress = GetRandomAddress(location, startPosition);
			if (randomAddress == null || AddressExists(randomAddress))
			{
				continue;
			}
			FillDestinationItemPool(location, randomAddress);
			GeneratedItems.Clear();
			int a = Random.Range(location.minBoxesPerDestination, location.maxBoxesPerDestination + 1);
			a = Mathf.Min(a, DestinationItemPool.Count);
			for (int j = 0; j < a; j++)
			{
				ItemAmountTarget item = GenerateItemAmountTarget(DestinationItemPool);
				GeneratedItems.Add(item);
				num++;
				if (num >= num2)
				{
					break;
				}
			}
			if (GeneratedItems.Count != 0)
			{
				GeneratedDestinations.Add(new DeliveryJobDestination(randomAddress, GeneratedItems.ToArray()));
				GeneratedItems.Clear();
			}
		}
		List<DeliveryJobDestination> result = new List<DeliveryJobDestination>(GeneratedDestinations);
		GeneratedDestinations.Clear();
		return result;
	}

	public static float GetTotalLinearDistance(List<DeliveryJobDestination> destinations)
	{
		float num = 0f;
		Vector3 a = PlayerHelper.GetCityPosition();
		SortDestinations(destinations);
		foreach (DeliveryJobDestination destination in destinations)
		{
			Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(destination.address);
			if ((bool)addressEntranceTransform)
			{
				num += Vector3.Distance(a, addressEntranceTransform.position);
				a = addressEntranceTransform.position;
			}
		}
		return num;
	}

	private static bool AddressExists(Address address)
	{
		foreach (DeliveryJobDestination generatedDestination in GeneratedDestinations)
		{
			if (generatedDestination.address == address)
			{
				return true;
			}
		}
		return false;
	}

	private static ItemAmountTarget GenerateItemAmountTarget(List<string> itemPool)
	{
		string random = itemPool.GetRandom();
		itemPool.Remove(random);
		int boxSize = ItemsGetter.GetByName(random).boxSize;
		int num = ((boxSize <= 1) ? 1 : Random.Range(1, boxSize + 1));
		if (boxSize >= 20)
		{
			num = Mathf.RoundToInt(Mathf.Round((float)num / 5f) * 5f);
		}
		num = Mathf.Max(num, 1);
		return new ItemAmountTarget(random, num);
	}

	private static void FillDestinationItemPool(DeliveryJobStartLocation location, Address address)
	{
		DestinationItemPool.Clear();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration != null && buildingRegistration.businessTypeName != "ba:businesstype_empty")
		{
			foreach (string primaryRetailProduct in BusinessTypeHelper.GetPrimaryRetailProducts(buildingRegistration.businessTypeName))
			{
				DestinationItemPool.Add(primaryRetailProduct);
			}
		}
		if (DestinationItemPool.Count == 0)
		{
			DestinationItemPool.AddRange(location.possibleItems);
		}
	}

	private static Address GetRandomAddress(DeliveryJobStartLocation startLocation, Vector3 startPosition)
	{
		float radiusSqr = startLocation.radius * startLocation.radius;
		CityBuildingController random = InstanceBehavior<CityManager>.Instance.cityBuildingControllers.Where(delegate(CityBuildingController cbc)
		{
			if (cbc.building != null && startLocation.destinationBuildingTypes.Contains(cbc.building.BuildingType))
			{
				BuildingRegistration buildingRegistration = cbc.buildingRegistration;
				if (buildingRegistration != null && !buildingRegistration.RentedByPlayer && cbc.buildingRegistration.businessTypeName != "ba:businesstype_empty" && (cbc.entranceDoors.First().doorTransform.position - startPosition).sqrMagnitude <= radiusSqr)
				{
					if (OnlyLowProductDestinations)
					{
						return IsLowProductDestination(cbc.buildingRegistration.businessTypeName, startLocation);
					}
					return true;
				}
			}
			return false;
		}).GetRandom();
		if (!random)
		{
			return null;
		}
		return random.building.Address;
	}

	private static bool IsLowProductDestination(string businessTypeName, DeliveryJobStartLocation location)
	{
		List<string> primaryRetailProducts = BusinessTypeHelper.GetPrimaryRetailProducts(businessTypeName);
		return ((primaryRetailProducts.Count > 0) ? primaryRetailProducts.Count : location.possibleItems.Length) < location.maxBoxesPerDestination;
	}

	public static void SortDestinations(List<DeliveryJobDestination> destinations = null)
	{
		if (destinations == null)
		{
			if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
			{
				return;
			}
			destinations = deliveryDriverMission.destinations;
		}
		Vector3 cityPosition = PlayerHelper.GetCityPosition();
		foreach (DeliveryJobDestination destination in destinations)
		{
			Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(destination.address);
			if ((bool)addressEntranceTransform)
			{
				float playerDistanceCached = Vector3.Distance(cityPosition, addressEntranceTransform.position);
				destination.playerDistanceCached = playerDistanceCached;
			}
		}
		destinations.Sort(delegate(DeliveryJobDestination x, DeliveryJobDestination y)
		{
			bool flag = x.IsCompleted();
			bool flag2 = y.IsCompleted();
			return (flag != flag2) ? (flag ? 1 : (-1)) : x.playerDistanceCached.CompareTo(y.playerDistanceCached);
		});
	}

	public static bool TryDeliverToAddress(Address address)
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission) || !deliveryDriverMission.IsOngoing())
		{
			return false;
		}
		DeliveryJobDestination deliveryJobDestination = null;
		foreach (DeliveryJobDestination destination in deliveryDriverMission.destinations)
		{
			if (!(destination.address != address))
			{
				deliveryJobDestination = destination;
				break;
			}
		}
		if (deliveryJobDestination == null || deliveryJobDestination.IsCompleted())
		{
			return false;
		}
		HandTruck componentInChildren = InstanceBehavior<GameManager>.Instance.playerController.GetComponentInChildren<HandTruck>();
		if ((bool)componentInChildren && TryDeliverToDestination(deliveryJobDestination, componentInChildren.vehicleInstance))
		{
			return true;
		}
		if (PlayerHelper.IsHoldingItem)
		{
			return TryDeliverToDestination(deliveryJobDestination, PlayerHelper.ItemInstanceInHands);
		}
		return false;
	}

	private static bool TryDeliverToDestination(DeliveryJobDestination destination, ICargoHolder cargoHolder)
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
		{
			return false;
		}
		ItemsToRemove.Clear();
		List<CargoInstance> cargoInstances = cargoHolder.GetCargoInstances();
		if (cargoInstances == null || cargoInstances.Count == 0)
		{
			return false;
		}
		foreach (CargoInstance item in cargoInstances)
		{
			for (int i = 0; i < destination.itemAmounts.Length; i++)
			{
				if (!destination.itemAmountsDelivered[i])
				{
					ItemAmountTarget target = destination.itemAmounts[i];
					if (CargoMatchesItemAmount(item, target))
					{
						ItemsToRemove.Add(item);
						destination.itemAmountsDelivered[i] = true;
						break;
					}
				}
			}
		}
		if (ItemsToRemove.Count == 0)
		{
			Notifications.ShowError("notification_delivery_job_wrong_items");
			return true;
		}
		foreach (CargoInstance item2 in ItemsToRemove)
		{
			cargoHolder.RemoveFromCargo(item2);
		}
		ItemsToRemove.Clear();
		if (destination.IsCompleted())
		{
			deliveryDriverMission.earnings += deliveryDriverMission.deliveryReward;
			Notifications.Show(NotificationType.Success, "notification_delivery_job_complete");
			if (deliveryDriverMission.pinnedAddress == destination.address)
			{
				deliveryDriverMission.pinnedAddress = null;
			}
		}
		else
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{
					"index",
					destination.GetDeliveredCount().ToString()
				},
				{
					"amount",
					destination.itemAmounts.Length.ToString()
				}
			};
			Notifications.Show(NotificationType.Info, "notification_delivery_job_partial", notificationData);
		}
		return true;
	}

	private static bool CargoMatchesItemAmount(CargoInstance cargoInstance, ItemAmountTarget target)
	{
		if (cargoInstance.itemName == target.itemName && cargoInstance.amount == target.targetAmount)
		{
			return true;
		}
		if (cargoInstance.nestedCargoInstances != null)
		{
			foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
			{
				if (nestedCargoInstance.itemName == target.itemName && nestedCargoInstance.amount == target.targetAmount)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void DiscardSealedBoxes()
	{
		if (PlayerHelper.IsHoldingItem)
		{
			DiscardSealedBoxesFrom(PlayerHelper.ItemInstanceInHands);
		}
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			DiscardSealedBoxesFrom(allPlayerVehicle.vehicleInstance);
		}
	}

	private static void DiscardSealedBoxesFrom(ICargoHolder cargoHolder)
	{
		ItemsToRemove.Clear();
		foreach (CargoInstance cargoInstance in cargoHolder.GetCargoInstances())
		{
			if (cargoInstance.IsSealed)
			{
				cargoInstance.itemName = "ba:itemname_closedcardboardbox";
				ItemsToRemove.Add(cargoInstance);
			}
		}
		foreach (CargoInstance item in ItemsToRemove)
		{
			cargoHolder.RemoveFromCargo(item);
		}
		ItemsToRemove.Clear();
	}

	[ConsoleMethod("OnlyLowProductDeliveryDestinations", "Temporary: only businesses with fewer products than the box cap generate as delivery destinations.", new string[] { })]
	public static void Command_OnlyLowProductDestinations(bool value)
	{
		OnlyLowProductDestinations = value;
		Debug.Log($"OnlyLowProductDeliveryDestinations set to {value}");
	}
}
