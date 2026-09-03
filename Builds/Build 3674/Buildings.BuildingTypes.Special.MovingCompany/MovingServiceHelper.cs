using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Blueprints;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.BuildingTypes.Special.FurnitureStore;
using Buildings.Indoors;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.Contacts;
using UI.Tasks;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.MovingCompany;

public static class MovingServiceHelper
{
	private const float DirtMinimumDistance = 0.8f;

	public static Func<ItemInstance, ItemInstance, int> itemsSortingMethod;

	private static Address FromAddress;

	private static Address ToAddress;

	private static BuildingRegistration OriginBuildingRegistration;

	private static BuildingRegistration DestinationBuildingRegistration;

	private static BuildingRegistration MovingCompanyRegistration;

	private static bool TransferBizManSettingsEnabled;

	private static Contact MovingCompanyContact;

	private static Transform BuildingTransform;

	private static MultipleHeightsBuildingController MultipleHeightsBuildingController;

	private static readonly List<(Vector3, Vector3)> CollidersSizeAndCenter = new List<(Vector3, Vector3)>();

	private static readonly List<(Vector3, Vector3)> StackedItemsCollidersSizeAndCenter = new List<(Vector3, Vector3)>();

	private static readonly List<VehicleInstance> VehiclesInDestinationAddress = new List<VehicleInstance>();

	private static readonly List<VehicleInstance> VehiclesInOriginAddress = new List<VehicleInstance>();

	[ConsoleMethod("SetMovingCompanyOrigin", "Sets the moving company origin", new string[] { })]
	public static void SetMovingCompanyOrigin()
	{
		FromAddress = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address;
	}

	[ConsoleMethod("SetMovingCompanyDestination", "Sets the moving company destination", new string[] { })]
	public static void SetMovingCompanyDestination()
	{
		ToAddress = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address;
	}

	[ConsoleMethod("MoveBusiness", "Moves business from one company to another", new string[] { }, AutoCompleteMap = new string[] { "fromStreet=StreetNames", "toStreet=StreetNames" })]
	public static void MoveBusiness(int fromNumber, string fromStreet, int toNumber, string toStreet, bool transferBizManSettings)
	{
		FromAddress = new Address(fromStreet, fromNumber);
		ToAddress = new Address(toStreet, toNumber);
		OriginBuildingRegistration = SaveGameManager.Current.BuildingRegistrations.First((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_movingservice");
		TransferBizManSettingsEnabled = transferBizManSettings;
		MoveBusiness();
	}

	[ConsoleMethod("MoveBusiness", "Moves business from one company to another", new string[] { })]
	public static void MoveBusinessCommand(bool transferBizManSettings)
	{
		OriginBuildingRegistration = SaveGameManager.Current.BuildingRegistrations.First((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_movingservice");
		TransferBizManSettingsEnabled = transferBizManSettings;
		MoveBusiness();
	}

	[ConsoleMethod("MoveBusinessInSameBuilding", "Applies movement service to the same building", new string[] { }, AutoCompleteMap = new string[] { "fromStreet=StreetNames" })]
	public static void MoveBusiness(int fromNumber, string fromStreet)
	{
		UseMovingServiceInSameBuilding(BuildingHelper.GetBuildingRegistration(new Address(fromStreet, fromNumber)));
	}

	public static void SetMovingServiceData(Address fromAddress, Address toAddress, BuildingRegistration movingCompanyRegistration, bool transferBizManSettings)
	{
		FromAddress = fromAddress;
		ToAddress = toAddress;
		MovingCompanyRegistration = movingCompanyRegistration;
		TransferBizManSettingsEnabled = transferBizManSettings;
		MovingCompanyContact = Contact.GetContact(movingCompanyRegistration, ContactCategoryName.FurnitureAndEquipment);
	}

	public static void MoveBusiness()
	{
		OriginBuildingRegistration = BuildingHelper.GetBuildingRegistration(FromAddress);
		DestinationBuildingRegistration = BuildingHelper.GetBuildingRegistration(ToAddress);
		List<ItemInstance> list = DestinationBuildingRegistration.itemInstances.Values.ToList();
		GetVehicles();
		if (!PayMove(list, VehiclesInDestinationAddress, out var itemsSoldAmount))
		{
			SendNotEnoughMoneyMessage();
			return;
		}
		BizManSchedule.AbortAutoFillForBusiness(OriginBuildingRegistration);
		BusinessSimulatorHelper.Work.ForceCompleteAllWork();
		ItemInstance itemInstance = list.FirstOrDefault((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isdeliveryspot));
		if (itemInstance != null)
		{
			list.Remove(itemInstance);
			DestinationBuildingRegistration.RemoveItemInstanceFromBuilding(itemInstance);
		}
		DestroyItemsInDestination(DestinationBuildingRegistration, list);
		DestroyVehiclesInDestination(DestinationBuildingRegistration, VehiclesInDestinationAddress);
		ChangePossessionsAddress();
		RelocatePossessions(itemInstance);
		if (TransferBizManSettingsEnabled)
		{
			BizManTransfer.Transfer(OriginBuildingRegistration, DestinationBuildingRegistration);
		}
		else
		{
			UpdateBuildingData(OriginBuildingRegistration);
		}
		UpdateBuildingData(DestinationBuildingRegistration);
		GenerateDirtSpots(DestinationBuildingRegistration.itemInstances.Values.ToList(), DestinationBuildingRegistration);
		SendDoneMessage(itemsSoldAmount);
		if (OriginBuildingRegistration.BuildingCached.IsHamptonsHouse())
		{
			BuildingManager.RequestHamptonsItemReloadIfLoaded(FromAddress);
		}
		if (DestinationBuildingRegistration.BuildingCached.IsHamptonsHouse())
		{
			BuildingManager.RequestHamptonsItemReloadIfLoaded(ToAddress);
		}
	}

	public static void UseMovingServiceInSameBuilding(BuildingRegistration registration)
	{
		OriginBuildingRegistration = registration;
		DestinationBuildingRegistration = registration;
		ToAddress = OriginBuildingRegistration.Address;
		RelocatePossessions(null, relocateVehicles: false);
		UpdateBuildingData(OriginBuildingRegistration);
		GenerateDirtSpots(OriginBuildingRegistration.itemInstances.Values.ToList(), OriginBuildingRegistration);
	}

	private static void SendNotEnoughMoneyMessage()
	{
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"businessName",
				OriginBuildingRegistration.GetComposedName()
			},
			{
				"businessName2",
				DestinationBuildingRegistration.GetComposedName()
			}
		};
		MovingCompanyContact?.SendMessage(new TextMessage("ba:messagetype_dialog_moving_service_not_enough_money", messageData));
	}

	private static void GetVehicles()
	{
		VehiclesInDestinationAddress.Clear();
		VehiclesInOriginAddress.Clear();
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.Address == ToAddress)
			{
				VehiclesInDestinationAddress.Add(vehicleInstance);
			}
			else if (vehicleInstance.Address == FromAddress)
			{
				VehiclesInOriginAddress.Add(vehicleInstance);
			}
		}
		if (OriginBuildingRegistration is Warehouse originWarehouse)
		{
			ExcludeNonSellableVehicles(originWarehouse);
		}
	}

	private static void ExcludeNonSellableVehicles(Warehouse originWarehouse)
	{
		int numberOfAssignedCars = originWarehouse.GetNumberOfAssignedCars();
		Warehouse obj = (Warehouse)DestinationBuildingRegistration;
		int count = obj.vehicleSlots.Count;
		List<string> vehiclesInParkingSpots = obj.GetVehiclesInParkingSpots();
		int count2 = vehiclesInParkingSpots.Count;
		int num = count - count2;
		int num2 = numberOfAssignedCars - num;
		for (int num3 = VehiclesInDestinationAddress.Count - 1; num3 >= 0; num3--)
		{
			VehicleInstance vehicleInstance = VehiclesInDestinationAddress[num3];
			if (vehiclesInParkingSpots.Contains(vehicleInstance.id))
			{
				if (num2 <= 0)
				{
					VehiclesInDestinationAddress.RemoveAt(num3);
				}
				else
				{
					num2--;
				}
			}
		}
	}

	private static bool PayMove(IEnumerable<ItemInstance> itemsInDestinationAddress, IEnumerable<VehicleInstance> vehiclesInDestinationAddress, out float itemsSoldAmount)
	{
		itemsSoldAmount = 0f;
		if (MovingCompanyRegistration == null)
		{
			return true;
		}
		MovingServiceSettings movingServiceSettings = BuildingHelper.GetBuilding(MovingCompanyRegistration.Address).SpecialService.settings as MovingServiceSettings;
		float itemsPrice = GetItemsPrice(FromAddress, movingServiceSettings.feePerItem);
		foreach (ItemInstance item in itemsInDestinationAddress)
		{
			itemsSoldAmount += item.GetSellingPrice();
		}
		foreach (VehicleInstance item2 in vehiclesInDestinationAddress)
		{
			itemsSoldAmount += item2.GetSellingPrice();
		}
		float num = movingServiceSettings.movingFee + itemsPrice - itemsSoldAmount;
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"businessName",
			MovingCompanyRegistration?.BusinessName
		} };
		SpecialService specialService = MovingCompanyRegistration.BuildingCached.SpecialService;
		bool num2 = (object)specialService != null && specialService.hasTaxDeductiblePurchases && BusinessHelper.IsTaxDeductibleBusinessServiceBuilding(DestinationBuildingRegistration);
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_movingservice", data);
		if (num2)
		{
			transactionInfo.SetTaxDeductibleName(MovingCompanyRegistration.BusinessName);
		}
		return GameManager.ChangeMoneySafe(0f - num, transactionInfo);
	}

	public static float GetItemsPrice(Address address, float feePerItem)
	{
		float num = 0f;
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(address).Values)
		{
			if (string.IsNullOrEmpty(value.parentId))
			{
				num += feePerItem;
			}
		}
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.Address == address)
			{
				num += feePerItem;
			}
		}
		return num;
	}

	private static void DestroyItemsInDestination(BuildingRegistration buildingRegistration, List<ItemInstance> itemsInDestinationAddress)
	{
		foreach (ItemInstance item in itemsInDestinationAddress)
		{
			buildingRegistration.RemoveItemInstanceFromBuilding(item);
			item.RemoveFromWorkShifts(buildingRegistration.Address);
		}
	}

	private static void DestroyVehiclesInDestination(BuildingRegistration registration, IEnumerable<VehicleInstance> vehiclesInDestinationAddress)
	{
		bool flag = registration.BuildingCached.IsHamptonsHouse();
		foreach (VehicleInstance item in vehiclesInDestinationAddress)
		{
			VehicleController vehicleController = null;
			if (flag && item.VehicleType.IsMotorVehicle)
			{
				vehicleController = VehicleHelper.GetVehicleController(item);
			}
			item.Delete(vehicleController);
		}
	}

	private static void ChangePossessionsAddress()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(FromAddress);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(ToAddress);
		foreach (ItemInstance item in buildingRegistration.itemInstances.Values.ToList())
		{
			buildingRegistration.RemoveItemInstanceFromBuilding(item);
			if (!TransferBizManSettingsEnabled)
			{
				item.RemoveFromWorkShifts(FromAddress);
			}
			buildingRegistration2.AddItemInstanceToBuilding(item);
		}
		foreach (VehicleInstance item2 in VehiclesInOriginAddress)
		{
			item2.streetName = ToAddress.streetName;
			item2.streetNumber = ToAddress.streetNumber;
		}
	}

	private static void RelocatePossessions(ItemInstance destinationDeliverySpot, bool relocateVehicles = true)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(ToAddress);
		Building buildingCached = buildingRegistration.BuildingCached;
		BuildingTransform = (buildingCached.IsHamptonsHouse() ? InstanceBehavior<CityManager>.Instance.FindCityBuildingController(buildingCached.Address).transform : InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(buildingCached)));
		MultipleHeightsBuildingController = BuildingTransform.GetComponent<MultipleHeightsBuildingController>();
		IReadOnlyList<GroundGrid> groundGrids = BuildingTransform.GetComponent<BuildingGridBase>().GetGroundGrids();
		GridGenerator component = BuildingTransform.GetComponent<GridGenerator>();
		List<GridMatrix> gridMatricesCopy = component.GetGridMatricesCopy();
		List<ItemInstance> list = buildingRegistration.itemInstances.Values.ToList();
		if (itemsSortingMethod != null)
		{
			list.Sort(itemsSortingMethod.Invoke);
		}
		List<ItemInstance> list2 = new List<ItemInstance>();
		List<ItemInstance> list3 = new List<ItemInstance>();
		List<ItemInstance> list4 = new List<ItemInstance>();
		ItemInstance deliverySpot = null;
		List<ItemInstance> list5 = new List<ItemInstance>();
		List<ItemInstance> list6 = new List<ItemInstance>();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			ItemInstance itemInstance = list[num];
			Item itemCached = itemInstance.ItemCached;
			if (itemCached.snapToCeiling)
			{
				list2.Add(itemInstance);
				list.RemoveAt(num);
			}
			if (itemCached.wallMounted)
			{
				list3.Add(itemInstance);
				list.RemoveAt(num);
			}
			if ((itemCached.type & ItemType.FlatDecoration) != 0)
			{
				list4.Add(itemInstance);
				list.RemoveAt(num);
			}
			if (itemCached.HasTag(TagRef.Itemtag.isdeliveryspot))
			{
				deliverySpot = itemInstance;
				list.RemoveAt(num);
			}
			if (itemCached.itemName == "ba:itemname_handtruckspawner")
			{
				list5.Add(itemInstance);
				list.RemoveAt(num);
			}
			if (itemCached.itemName == "ba:itemname_flatbedspawner")
			{
				list6.Add(itemInstance);
				list.RemoveAt(num);
			}
		}
		GridFiller gridFiller = new GridFiller(gridMatricesCopy, component.groundCellSize, component.heightCellSize, groundGrids);
		CreateDeliverySpotAndVehicleSpawnersIfNeeded(ref deliverySpot, buildingRegistration, list5, list6);
		MergeDeliverySpotsCargos(deliverySpot, destinationDeliverySpot);
		Quaternion quaternion = (buildingCached.IsHamptonsHouse() ? BuildingTransform.rotation : gridMatricesCopy[0].rotation);
		if (buildingRegistration.BuildingCached.BuildingType == "ba:buildingtype_residential")
		{
			PlaceDeliverySpotAndHandtruckSpawnersInDefaultPositions(buildingRegistration, deliverySpot, list5);
		}
		else
		{
			List<ItemInstance> list7 = new List<ItemInstance> { deliverySpot };
			list7.AddRange(list5);
			list7.AddRange(list6);
			RelocateItems(buildingRegistration, list7, gridFiller, quaternion);
		}
		RelocateItems(buildingRegistration, list, gridFiller, quaternion, deliverySpot);
		if (relocateVehicles)
		{
			RelocateVehicles(gridFiller, quaternion, deliverySpot);
		}
		WallGridFiller gridFiller2 = new WallGridFiller(gridMatricesCopy, component.heightCellSize);
		RelocateWallItems(buildingRegistration, list3, gridFiller2, deliverySpot);
		gridFiller.SetData(component.GetGridMatricesCopy(), component.groundCellSize, 0f, groundGrids);
		RelocateItems(buildingRegistration, list2, gridFiller, quaternion, deliverySpot);
		gridFiller.SetData(component.GetGridMatricesCopy(), component.groundCellSize, 0f, groundGrids);
		RelocateItems(buildingRegistration, list4, gridFiller, quaternion, deliverySpot);
		GameEvent.Invoke("ba:gameevent_itemcargochanged");
	}

	private static void CreateDeliverySpotAndVehicleSpawnersIfNeeded(ref ItemInstance deliverySpot, BuildingRegistration registration, List<ItemInstance> handTruckSpawners, List<ItemInstance> flatbedSpawners)
	{
		if (deliverySpot == null)
		{
			deliverySpot = FurnitureDeliveryHelper.CreateDeliverySpotInstance(registration);
		}
		if (handTruckSpawners.Count == 0)
		{
			VehicleHelper.CreateVehicleSpawners(registration, isInsideBuilding: false, "ba:itemname_handtruckspawner");
		}
		if (flatbedSpawners.Count == 0 && registration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			VehicleHelper.CreateVehicleSpawners(registration, isInsideBuilding: false, "ba:itemname_flatbedspawner");
		}
	}

	private static void MergeDeliverySpotsCargos(ItemInstance deliverySpot, ItemInstance destinationDeliverySpot)
	{
		if (destinationDeliverySpot != null)
		{
			for (int num = destinationDeliverySpot.cargoInstances.Count - 1; num >= 0; num--)
			{
				destinationDeliverySpot.cargoInstances[num].TryToMoveCargoBetweenHolders(destinationDeliverySpot, deliverySpot);
			}
		}
	}

	private static void PlaceDeliverySpotAndHandtruckSpawnersInDefaultPositions(BuildingRegistration registration, ItemInstance deliverySpot, List<ItemInstance> handTruckSpawners)
	{
		FurnitureDeliveryHelper.PlaceDeliverySpotOnDefaultPosition(registration, deliverySpot);
		PlaceVehicleSpawnersInDefaultPosition(registration, handTruckSpawners, "ba:itemname_handtruckspawner");
	}

	private static void PlaceVehicleSpawnersInDefaultPosition(BuildingRegistration registration, List<ItemInstance> vehicleSpawners, string spawnerName)
	{
		List<Transform> vehicleSpawnerTransformsInBuilding = VehicleHelper.GetVehicleSpawnerTransformsInBuilding(registration, spawnerName);
		for (int i = 0; i < vehicleSpawners.Count; i++)
		{
			ItemInstance itemInstance = vehicleSpawners[i];
			itemInstance.position = vehicleSpawnerTransformsInBuilding[i].position;
			itemInstance.yRotation = vehicleSpawnerTransformsInBuilding[i].eulerAngles.y;
		}
	}

	private static void RelocateItems(BuildingRegistration registration, List<ItemInstance> items, GridFiller gridFiller, Quaternion groundRotation, ItemInstance deliverySpot = null)
	{
		foreach (ItemInstance item in items)
		{
			if (!string.IsNullOrEmpty(item.parentId))
			{
				continue;
			}
			ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(item.itemName);
			if (!TryGetColliderSizeAndCenter(itemController, item, out var colliderSize, out var colliderCenter))
			{
				continue;
			}
			(Vector3, Vector3) tuple = (Vector3.zero, Vector3.zero);
			bool flag = item.stackedItems.Count > 0;
			if (flag)
			{
				tuple = HandleStackedItems(registration, colliderSize, item, itemController);
				(colliderSize, _) = tuple;
			}
			bool snapToCeiling = item.ItemCached.snapToCeiling;
			if (gridFiller.TryPlaceItem(colliderSize, out var placementPosition, checkCarPlacement: false, snapToCeiling))
			{
				SetItemPositionAndRotation(registration, BuildingTransform, groundRotation, item, placementPosition, flag, isWallItem: false, colliderCenter, tuple);
				continue;
			}
			Quaternion quaternion = Quaternion.Euler(0f, 90f, 0f);
			colliderSize = quaternion * colliderSize;
			colliderSize = new Vector3(Mathf.Abs(colliderSize.x), Mathf.Abs(colliderSize.y), Mathf.Abs(colliderSize.z));
			if (gridFiller.TryPlaceItem(colliderSize, out placementPosition, checkCarPlacement: false, snapToCeiling))
			{
				SetItemPositionAndRotation(registration, BuildingTransform, groundRotation * quaternion, item, placementPosition, flag, isWallItem: false, colliderCenter, tuple);
			}
			else if (deliverySpot != null)
			{
				MoveItemAndChildrenToDeliverySpot(deliverySpot, item);
			}
			else
			{
				Debug.LogError("item could not be placed in building pallet due to missing delivery spot");
			}
		}
	}

	private static void MoveItemAndChildrenToDeliverySpot(ItemInstance deliverySpot, ItemInstance itemInstance)
	{
		MoveItemToDeliverySpot(deliverySpot, itemInstance);
		foreach (AttachableChild stackedItem in itemInstance.stackedItems)
		{
			if (TryGetChildItemInstance(DestinationBuildingRegistration, itemInstance, stackedItem, out var attachedInstance))
			{
				MoveItemToDeliverySpot(deliverySpot, attachedInstance);
			}
		}
	}

	private static void MoveItemToDeliverySpot(ItemInstance deliverySpot, ItemInstance itemInstance)
	{
		if (itemInstance.cargoInstances.Count > 0)
		{
			MoveCargoFromHolderToDeliverySpot(itemInstance, deliverySpot);
		}
		itemInstance.RemoveFromWorkShifts(OriginBuildingRegistration.Address);
		DestinationBuildingRegistration.RemoveItemInstanceFromBuilding(itemInstance);
		AddCargoToDeliverySpot(deliverySpot, itemInstance);
	}

	private static void AddCargoToDeliverySpot(ItemInstance deliverySpot, ItemInstance itemInstance)
	{
		CargoInstance cargoInstance = new CargoInstance(itemInstance.itemName, 1, itemInstance.priceOnPurchase);
		deliverySpot.MergeIntoCargo(cargoInstance);
		if (cargoInstance.amount > 0)
		{
			deliverySpot.AddToCargo(cargoInstance);
		}
	}

	private static void MoveCargoFromHolderToDeliverySpot(ICargoHolder cargoHolder, ItemInstance deliverySpot)
	{
		List<CargoInstance> cargoInstances = cargoHolder.GetCargoInstances();
		for (int num = cargoInstances.Count - 1; num >= 0; num--)
		{
			cargoInstances[num].TryToMoveCargoBetweenHolders(cargoHolder, deliverySpot);
		}
	}

	private static void RelocateWallItems(BuildingRegistration registration, List<ItemInstance> items, WallGridFiller gridFiller, ItemInstance deliverySpot = null)
	{
		foreach (ItemInstance item in items)
		{
			if (!string.IsNullOrEmpty(item.parentId))
			{
				continue;
			}
			ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(item.itemName);
			if (TryGetColliderSizeAndCenter(itemController, item, out var colliderSize, out var colliderCenter))
			{
				(Vector3, Vector3) tuple = (Vector3.zero, Vector3.zero);
				bool flag = item.stackedItems.Count > 0;
				if (flag)
				{
					tuple = HandleStackedItems(registration, colliderSize, item, itemController);
					(colliderSize, _) = tuple;
				}
				if (gridFiller.TryPlaceWallItem(colliderSize, out var placementPosition, out var forwardDirection))
				{
					Quaternion rotation = Quaternion.LookRotation(BuildingTransform.rotation * forwardDirection);
					SetItemPositionAndRotation(registration, BuildingTransform, rotation, item, placementPosition, flag, isWallItem: true, colliderCenter, tuple);
				}
				else if (deliverySpot != null)
				{
					MoveItemAndChildrenToDeliverySpot(deliverySpot, item);
				}
				else
				{
					Debug.LogError("wall item could not be placed in building pallet due to missing delivery spot");
				}
			}
		}
	}

	private static void RelocateVehicles(GridFiller gridFiller, Quaternion rotation, ItemInstance deliverySpot)
	{
		WarehouseSlotController[] array = null;
		Warehouse warehouse = null;
		Warehouse warehouse2 = null;
		bool flag = OriginBuildingRegistration is Warehouse;
		bool moveController = DestinationBuildingRegistration.BuildingCached.IsHamptonsHouse();
		if (flag)
		{
			array = BuildingTransform.GetComponentsInChildren<WarehouseSlotController>();
			warehouse = (Warehouse)OriginBuildingRegistration;
			warehouse2 = (Warehouse)DestinationBuildingRegistration;
		}
		foreach (VehicleInstance item3 in VehiclesInOriginAddress)
		{
			if (flag)
			{
				VehicleSlot vehicleSlotByVehicleInstance = warehouse.GetVehicleSlotByVehicleInstance(item3);
				if (vehicleSlotByVehicleInstance != null)
				{
					if (!TransferBizManSettingsEnabled)
					{
						vehicleSlotByVehicleInstance.vehicleInstanceId = null;
					}
					VehicleSlot vehicleFreeSlot = warehouse2.GetVehicleFreeSlot();
					if (vehicleFreeSlot != null)
					{
						vehicleFreeSlot.AssignVehicle(item3);
						int num = warehouse2.GetVehicleSpotIndex(vehicleFreeSlot) + 1;
						WarehouseSlotController[] array2 = array;
						foreach (WarehouseSlotController warehouseSlotController in array2)
						{
							if (warehouseSlotController.slotIndex == num)
							{
								item3.position = warehouseSlotController.vehiclePosition.position;
								item3.rotation = warehouseSlotController.vehiclePosition.rotation;
							}
						}
						continue;
					}
				}
			}
			(Vector3, Vector3) vehicleColliderCenterAndSize = VehicleHelper.GetVehicleColliderCenterAndSize(item3.vehicleTypeName);
			Vector3 item = vehicleColliderCenterAndSize.Item1;
			Vector3 item2 = vehicleColliderCenterAndSize.Item2;
			bool isMotorVehicle = item3.VehicleType.IsMotorVehicle;
			if (gridFiller.TryPlaceItem(item2, out var placementPosition, isMotorVehicle))
			{
				SetVehiclePositionAndRotation(BuildingTransform, rotation, item3, item, placementPosition, moveController);
				continue;
			}
			if (deliverySpot != null)
			{
				MoveCargoFromHolderToDeliverySpot(item3, deliverySpot);
			}
			else
			{
				Debug.LogError("item could not be placed in building pallet due to missing delivery spot");
			}
			SellSingleVehicle(item3);
			item3.Delete();
		}
	}

	private static void SellSingleVehicle(VehicleInstance vehicleInstance)
	{
		if (!vehicleInstance.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			float sellingPrice = vehicleInstance.GetSellingPrice();
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"itemSoldInfo",
				vehicleInstance.vehicleTypeName.GetLocalization()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itemsold", data);
			GameManager.ChangeMoneySafe(sellingPrice, transactionInfo);
		}
	}

	private static bool TryGetColliderSizeAndCenter(ItemController itemController, ItemInstance itemInstance, out Vector3 colliderSize, out Vector3 colliderCenter)
	{
		colliderSize = Vector3.zero;
		colliderCenter = Vector3.zero;
		if (itemController == null)
		{
			Debug.LogError("Can't find an itemController for " + itemInstance.itemName);
			return false;
		}
		if (itemController.Colliders.Length == 0)
		{
			Debug.LogError(itemInstance.itemName + " does not have a collider");
			return false;
		}
		if (itemController.Colliders.Length == 1)
		{
			if (itemController.Colliders[0] is MeshCollider meshCollider)
			{
				colliderSize = meshCollider.sharedMesh.bounds.size;
			}
			else if (itemController.Colliders[0] is SphereCollider sphereCollider)
			{
				colliderSize = Vector3.one * (sphereCollider.radius * 2f);
				colliderCenter = sphereCollider.center;
			}
			else if (itemController.Colliders[0] is BoxCollider boxCollider)
			{
				colliderSize = boxCollider.size;
				colliderCenter = boxCollider.center;
			}
			else
			{
				if (!(itemController.Colliders[0] is CapsuleCollider capsuleCollider))
				{
					Debug.LogError(itemInstance.itemName + " does not have a supported collider");
					return false;
				}
				(colliderCenter, colliderSize) = GetSizeAndCenterFromACapsuleCollider(capsuleCollider);
			}
		}
		else
		{
			CollidersSizeAndCenter.Clear();
			Collider[] colliders = itemController.Colliders;
			foreach (Collider collider in colliders)
			{
				if (collider is BoxCollider boxCollider2)
				{
					CollidersSizeAndCenter.Add((boxCollider2.size, boxCollider2.center));
				}
				else if (collider is SphereCollider sphereCollider2)
				{
					CollidersSizeAndCenter.Add((Vector3.one * (sphereCollider2.radius * 2f), sphereCollider2.center));
				}
				else if (collider is CapsuleCollider capsuleCollider2)
				{
					CollidersSizeAndCenter.Add(GetSizeAndCenterFromACapsuleCollider(capsuleCollider2));
				}
			}
			(colliderSize, colliderCenter) = MergeColliders(CollidersSizeAndCenter);
		}
		return true;
	}

	private static (Vector3, Vector3) GetSizeAndCenterFromACapsuleCollider(CapsuleCollider capsuleCollider)
	{
		Vector3 zero = Vector3.zero;
		Vector3 center = capsuleCollider.center;
		float radius = capsuleCollider.radius;
		float height = capsuleCollider.height;
		switch (capsuleCollider.direction)
		{
		case 0:
			zero.x = height;
			zero.y = 2f * radius;
			zero.z = 2f * radius;
			break;
		case 1:
			zero.x = 2f * radius;
			zero.y = height;
			zero.z = 2f * radius;
			break;
		case 2:
			zero.x = 2f * radius;
			zero.y = 2f * radius;
			zero.z = height;
			break;
		}
		return (zero, center);
	}

	private static (Vector3, Vector3) HandleStackedItems(BuildingRegistration registration, Vector3 colliderSize, ItemInstance itemInstance, ItemController itemController)
	{
		StackedItemsCollidersSizeAndCenter.Clear();
		StackedItemsCollidersSizeAndCenter.Add((colliderSize, Vector3.zero));
		foreach (AttachableChild stackedItem in itemInstance.stackedItems)
		{
			if (TryGetChildItemInstance(registration, itemInstance, stackedItem, out var attachedInstance))
			{
				if (stackedItem.attachmentIndex >= itemController.AttachmentPoints.Length || stackedItem.attachmentIndex < 0)
				{
					stackedItem.attachmentIndex = 0;
				}
				StoreChildColliderAndPosition(attachedInstance, itemInstance, itemController, StackedItemsCollidersSizeAndCenter, stackedItem.attachmentIndex);
			}
		}
		return MergeColliders(StackedItemsCollidersSizeAndCenter);
	}

	private static void SetItemPositionAndRotation(BuildingRegistration registration, Transform buildingTransform, Quaternion rotation, ItemInstance itemInstance, Vector3 placementPosition, bool hasStackedItems, bool isWallItem, Vector3 itemColliderCenter, (Vector3, Vector3) mergedColliderSizeAndCenter)
	{
		placementPosition = buildingTransform.TransformPoint(placementPosition);
		placementPosition.y = GetItemYPosition(registration, itemInstance, placementPosition);
		SerializableVector3 position = itemInstance.position;
		itemInstance.position = new SerializableVector3(placementPosition.x, placementPosition.y, placementPosition.z);
		Vector3 vector = (isWallItem ? new Vector3(itemColliderCenter.x, itemColliderCenter.y, 0f) : new Vector3(itemColliderCenter.x, 0f, itemColliderCenter.z));
		itemInstance.position -= rotation * vector;
		if (hasStackedItems)
		{
			Vector3 vector2 = (isWallItem ? new Vector3(mergedColliderSizeAndCenter.Item2.x, mergedColliderSizeAndCenter.Item2.y, 0f) : new Vector3(mergedColliderSizeAndCenter.Item2.x, 0f, mergedColliderSizeAndCenter.Item2.z));
			itemInstance.position -= rotation * vector2;
			ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(itemInstance.itemName);
			foreach (AttachableChild stackedItem in itemInstance.stackedItems)
			{
				if (TryGetChildItemInstance(registration, itemInstance, stackedItem, out var attachedInstance))
				{
					AttachmentPoint attachmentPoint = itemController.AttachmentPoints[stackedItem.attachmentIndex];
					Vector3 vector3 = attachmentPoint.transform.localPosition;
					if (!attachmentPoint.SnapPositionToAttachment || attachmentPoint is FactoryMachineAttachmentPoint)
					{
						vector3 = (Vector3)attachedInstance.position - (Vector3)position;
						vector3 = Quaternion.Inverse(itemInstance.Rotation) * vector3;
					}
					Vector3 vector4 = rotation * vector3;
					Vector3 vector5 = itemInstance.position + vector4;
					attachedInstance.position = vector5;
					RotateChildToMatchParentRotation(rotation, itemInstance, attachedInstance);
				}
			}
		}
		itemInstance.yRotation = rotation.eulerAngles.y;
	}

	private static float GetItemYPosition(BuildingRegistration registration, ItemInstance itemInstance, Vector3 placementPosition)
	{
		if (!itemInstance.ItemCached.snapToCeiling)
		{
			return placementPosition.y;
		}
		if (MultipleHeightsBuildingController == null)
		{
			return BuildingSizeHelper.GetBuildingRoofPosition(registration.BuildingCached.BuildingSize, 0);
		}
		int positionHeightIndex = MultipleHeightsBuildingController.GetPositionHeightIndex(placementPosition);
		return MultipleHeightsBuildingController.GetCeilingYPositionForRoofObject(placementPosition, positionHeightIndex);
	}

	private static bool TryGetChildItemInstance(BuildingRegistration registration, ItemInstance itemInstance, AttachableChild attachableChild, out ItemInstance attachedInstance)
	{
		if (!registration.itemInstances.TryGetValue(attachableChild.childId, out attachedInstance))
		{
			Debug.LogError("Couldn't find attached item in " + itemInstance.itemName + ". Skipping it from Moving Service");
			return false;
		}
		return true;
	}

	private static void SetVehiclePositionAndRotation(Transform buildingTransform, Quaternion rotation, VehicleInstance vehicleInstance, Vector3 colliderCenter, Vector3 placementPosition, bool moveController)
	{
		placementPosition = buildingTransform.TransformPoint(placementPosition);
		vehicleInstance.position = new SerializableVector3(placementPosition.x, placementPosition.y, placementPosition.z);
		vehicleInstance.position -= rotation * new Vector3(colliderCenter.x, 0f, colliderCenter.z);
		vehicleInstance.rotation = rotation;
		if (moveController)
		{
			VehicleController vehicleController = VehicleHelper.GetVehicleController(vehicleInstance);
			vehicleController.SetFreeze(isFrozen: false);
			vehicleController.transform.SetPositionAndRotation(vehicleInstance.position, vehicleInstance.rotation);
			vehicleController.SetFreeze(isFrozen: true);
		}
	}

	private static void StoreChildColliderAndPosition(ItemInstance childItemInstance, ItemInstance parentItemInstance, ItemController itemController, ICollection<(Vector3, Vector3)> collidersAndCenter, int attachmentIndex)
	{
		if (TryGetColliderSizeAndCenter(PrefabHelper.LoadItemControllerFromPrefab(childItemInstance.itemName), childItemInstance, out var colliderSize, out var colliderCenter))
		{
			AttachmentPoint attachmentPoint = itemController.AttachmentPoints[attachmentIndex];
			Vector3 vector = attachmentPoint.transform.localPosition;
			if (!attachmentPoint.SnapPositionToAttachment || attachmentPoint is FactoryMachineAttachmentPoint)
			{
				vector = (Vector3)childItemInstance.position - (Vector3)parentItemInstance.position;
				vector = Quaternion.Inverse(parentItemInstance.Rotation) * vector;
			}
			colliderCenter = childItemInstance.Rotation * Quaternion.Inverse(parentItemInstance.Rotation) * colliderCenter;
			colliderSize = childItemInstance.Rotation * Quaternion.Inverse(parentItemInstance.Rotation) * colliderSize;
			colliderSize = new Vector3(Mathf.Abs(colliderSize.x), Mathf.Abs(colliderSize.y), Mathf.Abs(colliderSize.z));
			vector = new Vector3(vector.x + colliderCenter.x, vector.y, vector.z + colliderCenter.z);
			collidersAndCenter.Add((colliderSize, vector));
		}
	}

	private static void RotateChildToMatchParentRotation(Quaternion parentFinalRotation, ItemInstance itemInstance, ItemInstance childItemInstance)
	{
		Quaternion rotationFromInitialToFinal = GetRotationFromInitialToFinal(parentFinalRotation, itemInstance);
		childItemInstance.yRotation = (childItemInstance.Rotation * rotationFromInitialToFinal).eulerAngles.y;
	}

	private static Quaternion GetRotationFromInitialToFinal(Quaternion parentFinalRotation, ItemInstance itemInstance)
	{
		Quaternion normalized = itemInstance.Rotation.normalized;
		normalized = Quaternion.Inverse(normalized);
		return parentFinalRotation * normalized;
	}

	private static (Vector3, Vector3) MergeColliders(List<(Vector3, Vector3)> collidersSizeAndCenter)
	{
		if (collidersSizeAndCenter.Count == 0)
		{
			return (Vector3.zero, Vector3.zero);
		}
		Vector3 minPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 maxPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		CalculateMinAndMaxPoints(collidersSizeAndCenter, ref minPoint, ref maxPoint);
		Vector3 item = (minPoint + maxPoint) * 0.5f;
		return (maxPoint - minPoint, item);
	}

	private static void CalculateMinAndMaxPoints(List<(Vector3, Vector3)> collidersSizeAndCenter, ref Vector3 minPoint, ref Vector3 maxPoint)
	{
		foreach (var item3 in collidersSizeAndCenter)
		{
			Vector3 item = item3.Item1;
			Vector3 item2 = item3.Item2;
			Vector3 rhs = item2 - item * 0.5f;
			Vector3 rhs2 = item2 + item * 0.5f;
			minPoint = Vector3.Min(minPoint, rhs);
			maxPoint = Vector3.Max(maxPoint, rhs2);
		}
	}

	private static void UpdateBuildingData(BuildingRegistration buildingRegistration)
	{
		BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
		BusinessHelper.GenerateMissingTodoTasksForBusiness(buildingRegistration);
		CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, TimeHelper.GetDayOfWeek());
		TasksUI.UpdateTasksFromBusiness(buildingRegistration);
		CustomerDemandHelper.ReloadCachedFulfilled(buildingRegistration);
		if (BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft))
		{
			UpdateSecurity(buildingRegistration);
		}
		ResetQueues(buildingRegistration.itemInstances.Values);
		GlobalEvents.onBuildingRegistrationChange?.Invoke(buildingRegistration.Address);
	}

	private static void UpdateSecurity(BuildingRegistration buildingRegistration)
	{
		List<ExitZone> exitZones = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(buildingRegistration)).GetComponentsInChildren<ExitZone>().ToList();
		foreach (ItemInstance item in buildingRegistration.itemInstances.Values.Where((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.issecuritypanel)))
		{
			item.UpdateSecurityPanelCoverage(exitZones);
		}
		BusinessSecurityHelper.UpdateCamerasCoverage(buildingRegistration.Address);
		buildingRegistration.UpdateSecurityLevel();
	}

	private static void ResetQueues(IEnumerable<ItemInstance> itemsInAddress)
	{
		foreach (ItemInstance item in itemsInAddress)
		{
			if (ItemsGetter.GetByName(item.itemName).hasWaitingLine)
			{
				item.customPositions = null;
			}
		}
	}

	private static void SendDoneMessage(float itemsSoldAmount)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{
				"businessName",
				OriginBuildingRegistration.GetComposedName()
			},
			{
				"businessName2",
				DestinationBuildingRegistration.GetComposedName()
			},
			{
				"businessName3",
				MovingCompanyRegistration?.BusinessName
			}
		};
		if (itemsSoldAmount > 0f)
		{
			dictionary.Add("amount", itemsSoldAmount.ToShortCurrencyFormat());
		}
		string messageKey = ((itemsSoldAmount > 0f) ? "ba:messagetype_dialog_moving_service_done_items_sold" : "ba:messagetype_dialog_moving_service_done");
		MovingCompanyContact?.SendMessage(new TextMessage(messageKey, dictionary));
	}

	private static void GenerateDirtSpots(List<ItemInstance> items, BuildingRegistration registration)
	{
		List<DirtSpot> dirtSpotsForBuilding = BuildingCleanlinessHelper.GetDirtSpotsForBuilding(registration.BuildingCached);
		foreach (ItemInstance item2 in items)
		{
			item2.dirtSpotsThatAffects?.Clear();
			Item byName = ItemsGetter.GetByName(item2.itemName);
			if (byName.snapToCeiling || byName.wallMounted)
			{
				continue;
			}
			Vector3[] navMeshTargets = GetNavMeshTargets(item2);
			foreach (Vector3 vector in navMeshTargets)
			{
				Vector3 vector2 = item2.Rotation * vector;
				Vector3 vector3 = item2.position + vector2;
				DirtSpot closestDirtSpot = GetClosestDirtSpot(dirtSpotsForBuilding, vector3);
				if (Vector3.SqrMagnitude(vector3 - new Vector3(closestDirtSpot.x, 0f, closestDirtSpot.z)) <= 0.64000005f)
				{
					int item = dirtSpotsForBuilding.IndexOf(closestDirtSpot);
					ItemInstance itemInstance = item2;
					if (itemInstance.dirtSpotsThatAffects == null)
					{
						itemInstance.dirtSpotsThatAffects = new List<int>();
					}
					if (!item2.dirtSpotsThatAffects.Contains(item))
					{
						item2.dirtSpotsThatAffects.Add(item);
					}
				}
			}
		}
	}

	private static DirtSpot GetClosestDirtSpot(List<DirtSpot> buildingDirtSpots, Vector3 navMeshTargetWorldPosition)
	{
		DirtSpot result = null;
		float num = float.MaxValue;
		foreach (DirtSpot buildingDirtSpot in buildingDirtSpots)
		{
			Vector3 vector = new Vector3(buildingDirtSpot.x, 0f, buildingDirtSpot.z);
			float num2 = Vector3.SqrMagnitude(navMeshTargetWorldPosition - vector);
			if (num2 < num)
			{
				num = num2;
				result = buildingDirtSpot;
			}
		}
		return result;
	}

	private static Vector3[] GetNavMeshTargets(ItemInstance itemInstance)
	{
		ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(itemInstance.itemName);
		if (itemController == null)
		{
			return Array.Empty<Vector3>();
		}
		return itemController.NavMeshTargetsPositions;
	}

	private static IEnumerator RelocateWallItemsCoroutine(BuildingRegistration registration, List<ItemInstance> items, WallGridFiller gridFiller, Transform buildingTransform, ItemInstance deliverySpot = null)
	{
		foreach (ItemInstance item in items)
		{
			if (!string.IsNullOrEmpty(item.parentId))
			{
				continue;
			}
			ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(item.itemName);
			if (TryGetColliderSizeAndCenter(itemController, item, out var colliderSize, out var colliderCenter))
			{
				(Vector3, Vector3) tuple = (Vector3.zero, Vector3.zero);
				bool flag = item.stackedItems.Count > 0;
				if (flag)
				{
					tuple = HandleStackedItems(registration, colliderSize, item, itemController);
					(colliderSize, _) = tuple;
				}
				if (gridFiller.TryPlaceWallItem(colliderSize, out var placementPosition, out var forwardDirection))
				{
					Quaternion rotation = Quaternion.LookRotation(buildingTransform.rotation * forwardDirection);
					SetItemPositionAndRotation(registration, buildingTransform, rotation, item, placementPosition, flag, isWallItem: true, colliderCenter, tuple);
					ItemController itemController2 = PrefabHelper.CreatePrefabItem(item.itemName);
					itemController2.transform.position = item.position;
					itemController2.transform.rotation = item.Rotation;
				}
				else if (deliverySpot != null)
				{
					MoveItemAndChildrenToDeliverySpot(deliverySpot, item);
				}
				else
				{
					Debug.LogError("wall item could not be placed in building pallet due to missing delivery spot");
				}
				yield return new WaitForSeconds(2f);
			}
		}
	}

	private static IEnumerator RelocateItemsCoroutine(BuildingRegistration registration, List<ItemInstance> items, GridFiller gridFiller, Transform buildingTransform, Quaternion groundRotation, ItemInstance deliverySpot = null)
	{
		foreach (ItemInstance item in items)
		{
			if (!string.IsNullOrEmpty(item.parentId))
			{
				continue;
			}
			ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(item.itemName);
			if (!TryGetColliderSizeAndCenter(itemController, item, out var colliderSize, out var colliderCenter))
			{
				continue;
			}
			(Vector3, Vector3) tuple = (Vector3.zero, Vector3.zero);
			bool flag = item.stackedItems.Count > 0;
			if (flag)
			{
				tuple = HandleStackedItems(registration, colliderSize, item, itemController);
				(colliderSize, _) = tuple;
			}
			if (gridFiller.TryPlaceItem(colliderSize, out var placementPosition))
			{
				SetItemPositionAndRotation(registration, buildingTransform, groundRotation, item, placementPosition, flag, isWallItem: false, colliderCenter, tuple);
				ItemController itemController2 = PrefabHelper.CreatePrefabItem(item.itemName);
				itemController2.transform.position = item.position;
				itemController2.transform.rotation = item.Rotation;
			}
			else
			{
				Quaternion quaternion = Quaternion.Euler(0f, 90f, 0f);
				colliderSize = quaternion * colliderSize;
				colliderSize = new Vector3(Mathf.Abs(colliderSize.x), Mathf.Abs(colliderSize.y), Mathf.Abs(colliderSize.z));
				if (gridFiller.TryPlaceItem(colliderSize, out placementPosition))
				{
					SetItemPositionAndRotation(registration, buildingTransform, groundRotation * quaternion, item, placementPosition, flag, isWallItem: false, colliderCenter, tuple);
					ItemController itemController3 = PrefabHelper.CreatePrefabItem(item.itemName);
					itemController3.transform.position = item.position;
					itemController3.transform.rotation = item.Rotation;
				}
				else if (deliverySpot != null)
				{
					MoveItemAndChildrenToDeliverySpot(deliverySpot, item);
				}
				else
				{
					Debug.LogError("item could not be placed in building pallet due to missing delivery spot");
				}
			}
			yield return new WaitForSeconds(2f);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		OriginBuildingRegistration = null;
		DestinationBuildingRegistration = null;
		MovingCompanyRegistration = null;
		MovingCompanyContact = null;
		TransferBizManSettingsEnabled = false;
		VehiclesInOriginAddress.Clear();
		CollidersSizeAndCenter.Clear();
		StackedItemsCollidersSizeAndCenter.Clear();
		itemsSortingMethod = null;
	}
}
