using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Blueprints;
using Entities;
using Extensions;
using Helpers;
using Services;
using Streets;
using UI.Notification;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.FurnitureStore;

public static class FurnitureDeliveryHelper
{
	private static List<FurnitureDeliveryContract> Deliveries => SaveGameManager.Current.FurnitureDeliveryContracts;

	public static void Init()
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
	}

	private static void OnEnterBuilding(Address address)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration.RentedByPlayer && !buildingRegistration.itemInstances.Values.Any((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isdeliveryspot)))
		{
			ItemInstance itemInstance = CreateDeliverySpotInstance(buildingRegistration);
			InstanceBehavior<BuildingManager>.Instance.InstantiateSingleInstance(itemInstance);
		}
	}

	public static void RunHourly()
	{
		DoDeliveriesInDayAndHour(SaveGameManager.Current.Day, SaveGameManager.Current.Hour);
	}

	private static void DoDeliveriesInDayAndHour(int day, int hour)
	{
		foreach (FurnitureDeliveryContract item in Deliveries.Where((FurnitureDeliveryContract delivery) => day >= delivery.dayOfDelivery && hour >= delivery.hourOfDelivery))
		{
			DeliverFurnitureContract(item);
		}
		Deliveries.RemoveAll((FurnitureDeliveryContract x) => day >= x.dayOfDelivery && hour >= x.hourOfDelivery);
	}

	private static void DeliverFurnitureContract(FurnitureDeliveryContract delivery)
	{
		if (delivery == null || delivery.toAddress == null || delivery.itemsToDeliver == null || delivery.itemsToDeliver.Count == 0)
		{
			Debug.LogWarning("Skipping invalid furniture delivery contract due to missing source, target, or items.");
			return;
		}
		BuildingRegistration buildingRegistration = ((ContractItemsForSaleService.TryGetContactIdForAddress(delivery.fromAddress, out var contactId) && !string.IsNullOrEmpty(contactId)) ? null : BuildingHelper.GetBuildingRegistration(delivery.fromAddress));
		Contact storeManagerContact = ((buildingRegistration != null) ? Contact.GetContact(buildingRegistration, ContactCategoryName.FurnitureAndEquipment) : GetContactForContractSourceAddress(delivery.fromAddress));
		if (!PayDelivery(delivery, buildingRegistration))
		{
			ShowNotEnoughMoneyMessage(delivery, storeManagerContact);
			return;
		}
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(delivery.toAddress);
		if (buildingRegistration2 == null)
		{
			Debug.LogWarning($"Skipping furniture delivery because target registration was not found: {delivery.toAddress}");
			return;
		}
		UpdateDeliverySpot(buildingRegistration2, delivery.itemsToDeliver);
		ShowDeliverySuccessNotification(delivery, buildingRegistration2, buildingRegistration);
	}

	private static bool PayDelivery(FurnitureDeliveryContract delivery, BuildingRegistration furnitureStore)
	{
		string text = ((furnitureStore == null) ? GetSourceDisplayName(delivery.fromAddress) : (string.IsNullOrEmpty(furnitureStore.BusinessName) ? delivery.fromAddress.ToFormattedString() : furnitureStore.BusinessName));
		Dictionary<string, string> data = new Dictionary<string, string>
		{
			{ "warehouseName", text },
			{
				"businessName",
				delivery.toAddress.ToFormattedString()
			}
		};
		bool num = furnitureStore != null && furnitureStore.BuildingCached.SpecialService?.hasTaxDeductiblePurchases == true;
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_deliverycontract", data);
		if (num)
		{
			transactionInfo.SetTaxDeductibleName(text);
		}
		return GameManager.ChangeMoneySafe(0f - delivery.TotalDeliveryPrice, transactionInfo);
	}

	private static void ShowNotEnoughMoneyMessage(FurnitureDeliveryContract delivery, Contact storeManagerContact)
	{
		if (storeManagerContact != null)
		{
			Dictionary<string, string> messageData = new Dictionary<string, string>
			{
				{
					"amount",
					delivery.TotalDeliveryPrice.ToCurrencyFormat()
				},
				{
					"businessName",
					delivery.toAddress.ToFormattedString()
				}
			};
			storeManagerContact.SendMessage(new TextMessage("ba:messagetype_phone_wholesale_store_delivery_not_enough_funds", messageData));
		}
	}

	private static Contact GetContactForContractSourceAddress(Address sourceAddress)
	{
		if (!ContractItemsForSaleService.TryGetContactIdForAddress(sourceAddress, out var contactId) || string.IsNullOrEmpty(contactId))
		{
			return null;
		}
		return SaveGameManager.Current.Contacts.Find((Contact x) => x.id == contactId);
	}

	public static void UpdateDeliverySpot(BuildingRegistration registration, List<FurnitureDeliveryItem> itemsToDeliver)
	{
		ItemInstance deliverySpotInstance = GetDeliverySpotInstance(registration);
		AddDeliveryItemsToDeliverySpotCargo(itemsToDeliver, deliverySpotInstance);
	}

	public static ItemInstance GetDeliverySpotInstance(BuildingRegistration registration)
	{
		ItemInstance itemInstance = registration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isdeliveryspot));
		if (itemInstance == null)
		{
			itemInstance = CreateDeliverySpotInstance(registration);
		}
		return itemInstance;
	}

	private static void AddDeliveryItemsToDeliverySpotCargo(List<FurnitureDeliveryItem> itemsToDeliver, ItemInstance deliverySpotInstance)
	{
		foreach (FurnitureDeliveryItem item in itemsToDeliver)
		{
			int boxSize = ItemsGetter.GetByName(item.itemName).boxSize;
			for (int i = 0; i < item.amount; i += boxSize)
			{
				int amount = ((i + boxSize > item.amount) ? (item.amount - i) : boxSize);
				CargoInstance cargoInstance = new CargoInstance(item.itemName, amount, item.pricePerUnit);
				deliverySpotInstance.AddToCargo(cargoInstance);
			}
		}
		GameEvent.Invoke("ba:gameevent_itemcargochanged");
	}

	public static ItemInstance CreateDeliverySpotInstance(BuildingRegistration registration)
	{
		ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomByTag(TagRef.Itemtag.isdeliveryspot));
		PlaceDeliverySpotOnDefaultPosition(registration, itemInstance);
		registration.AddItemInstanceToBuilding(itemInstance);
		return itemInstance;
	}

	public static void PlaceDeliverySpotOnDefaultPosition(BuildingRegistration registration, ItemInstance instance)
	{
		Transform deliverySpotTransform = GetDeliverySpotTransform(registration);
		instance.position = deliverySpotTransform.position;
		instance.yRotation = deliverySpotTransform.eulerAngles.y;
	}

	private static Transform GetDeliverySpotTransform(BuildingRegistration registration)
	{
		Transform transform = ((!registration.BuildingCached.IsHamptonsHouse()) ? InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(registration)) : InstanceBehavior<CityManager>.Instance.FindCityBuildingController(registration.Address)?.transform)?.GetComponentInChildren<DeliverySpot>(includeInactive: true)?.transform;
		if (transform != null)
		{
			return transform;
		}
		Debug.LogWarning($"No delivery spot found for {registration.BuildingCached.BuildingSize} {registration.BuildingCached.BuildingVersion}");
		return new GameObject("NullDeliverySpot").transform;
	}

	private static void ShowDeliverySuccessNotification(FurnitureDeliveryContract delivery, BuildingRegistration registration, BuildingRegistration furnitureStore)
	{
		string value = ((furnitureStore == null) ? GetSourceDisplayName(delivery.fromAddress) : (string.IsNullOrEmpty(furnitureStore.BusinessName) ? delivery.fromAddress.ToFormattedString() : furnitureStore.BusinessName));
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{ "fromname", value },
			{
				"toname",
				delivery.toAddress.ToFormattedString()
			},
			{ "businessName", registration.BusinessName }
		};
		Notifications.Show(NotificationType.Success, string.IsNullOrEmpty(registration.BusinessName) ? "notification_delivery_contract_arrived" : "notification_delivery_contract_arrived_business_name", notificationData);
	}

	private static string GetSourceDisplayName(Address sourceAddress)
	{
		if (sourceAddress == null)
		{
			return string.Empty;
		}
		if (ContractItemsForSaleService.TryGetContactIdForAddress(sourceAddress, out var contactId) && !string.IsNullOrEmpty(contactId))
		{
			return contactId;
		}
		string arg = sourceAddress.streetName ?? string.Empty;
		return $"{arg} {sourceAddress.streetNumber}".Trim();
	}
}
