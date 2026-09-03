using System;
using System.Collections.Generic;
using System.Text;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Special.FurnitureStore;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Streets;
using UI.Notification;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.FoodDelivery;

public static class FoodDeliveryHelper
{
	public const string ContactId = "speedy_bites";

	private const string ContactDescription = "contact_description_food_delivery";

	private const string WelcomeMessage = "phone_food_delivery_welcome";

	private const string DeliveredMessage = "phone_food_delivery_delivered";

	private const string CancelledNoBuildingMessage = "phone_food_delivery_cancelled_no_building";

	private const string DeliveredNotification = "notification_delivery_contract_arrived";

	private const string DeliveredNotificationWithBusinessName = "notification_delivery_contract_arrived_business_name";

	public static void Init()
	{
		if (InstanceBehavior<GlobalReferences>.Instance.foodDeliverySettings == null)
		{
			Debug.LogError("FoodDeliverySettings is not assigned on GlobalReferences.");
		}
		else
		{
			GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
		}
	}

	public static List<FoodDeliveryContract> GetContracts()
	{
		GameInstance current = SaveGameManager.Current;
		return current.FoodDeliveryContracts ?? (current.FoodDeliveryContracts = new List<FoodDeliveryContract>());
	}

	public static void RunHourly()
	{
		List<FoodDeliveryContract> contracts = GetContracts();
		for (int num = contracts.Count - 1; num >= 0; num--)
		{
			if (TimeHelper.IsInThePast(contracts[num].dayOfDelivery, contracts[num].hourOfDelivery))
			{
				Deliver(contracts[num]);
				contracts.RemoveAt(num);
			}
		}
	}

	public static void RemoveContractsForAddress(Address address)
	{
		List<FoodDeliveryContract> contracts = GetContracts();
		bool flag = false;
		for (int num = contracts.Count - 1; num >= 0; num--)
		{
			if (!(contracts[num].toAddress != address))
			{
				contracts.RemoveAt(num);
				flag = true;
			}
		}
		if (flag)
		{
			SendCancelledMessage(address);
		}
	}

	public static void MoveContractsToAddress(Address origin, Address destination)
	{
		foreach (FoodDeliveryContract contract in GetContracts())
		{
			if (contract.toAddress == origin)
			{
				contract.toAddress = destination;
			}
		}
	}

	public static string BuildItemsText(FoodDeliveryContract contract, bool includePrice)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < contract.itemsToDeliver.Count; i++)
		{
			FurnitureDeliveryItem furnitureDeliveryItem = contract.itemsToDeliver[i];
			stringBuilder.Append($"{furnitureDeliveryItem.amount}x {furnitureDeliveryItem.itemName.GetLocalization()}");
			if (includePrice)
			{
				stringBuilder.Append(" " + furnitureDeliveryItem.pricePerUnit.ToCurrencyFormat());
			}
			if (i < contract.itemsToDeliver.Count - 1)
			{
				stringBuilder.Append("<br>");
			}
		}
		return stringBuilder.ToString();
	}

	private static Contact GetContact()
	{
		foreach (Contact contact in SaveGameManager.Current.Contacts)
		{
			if (contact.id == "speedy_bites")
			{
				return contact;
			}
		}
		return null;
	}

	private static void OnGameEventTriggered(string gameEvent)
	{
		if (!(gameEvent != "ba:gameevent_rentedbuilding"))
		{
			TryAddWelcomeContact();
		}
	}

	public static void TryAddWelcomeContact()
	{
		if (GetContact() != null)
		{
			GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
			return;
		}
		Contact.GetContact("speedy_bites", ContactCategoryName.General, "contact_description_food_delivery", null, hasWelcomeMessages: false, skipNewNotification: true).SendMessage(new TextMessage("phone_food_delivery_welcome"), notify: true, sendNotificationInstantly: true);
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
	}

	private static void Deliver(FoodDeliveryContract contract)
	{
		if (contract.itemsToDeliver != null && contract.itemsToDeliver.Count != 0)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(contract.toAddress);
			if (buildingRegistration == null || !buildingRegistration.RentedByPlayer)
			{
				SendCancelledMessage(contract.toAddress);
			}
			else if (TryPayDelivery(contract))
			{
				AddBaggedItemsToDeliverySpotCargo(buildingRegistration, contract.itemsToDeliver);
				ShowDeliveredNotification(contract, buildingRegistration);
				SendDeliveredMessage();
			}
		}
	}

	private static void AddBaggedItemsToDeliverySpotCargo(BuildingRegistration registration, List<FurnitureDeliveryItem> itemsToDeliver)
	{
		CargoInstance cargoInstance = new CargoInstance(ItemsGetter.GetRandomBag(), 1, 0f);
		foreach (FurnitureDeliveryItem item in itemsToDeliver)
		{
			cargoInstance.nestedCargoInstances.Add(new NestedCargoInstance(item.itemName, item.amount, item.pricePerUnit, null));
		}
		FurnitureDeliveryHelper.GetDeliverySpotInstance(registration).AddToCargo(cargoInstance);
		GameEvent.Invoke("ba:gameevent_itemcargochanged");
	}

	private static void SendDeliveredMessage()
	{
		GetContact()?.SendMessage(new TextMessage("phone_food_delivery_delivered"));
	}

	private static void SendCancelledMessage(Address address)
	{
		Contact contact = GetContact();
		if (contact != null)
		{
			Dictionary<string, string> messageData = new Dictionary<string, string> { 
			{
				"deliveryAddress",
				address.ToFormattedString()
			} };
			contact.SendMessage(new TextMessage("phone_food_delivery_cancelled_no_building", messageData));
		}
	}

	private static bool TryPayDelivery(FoodDeliveryContract contract)
	{
		float totalDeliveryPrice = contract.TotalDeliveryPrice;
		Dictionary<string, string> data = new Dictionary<string, string>
		{
			{
				"warehouseName",
				"speedy_bites".GetLocalization()
			},
			{
				"businessName",
				contract.toAddress.ToFormattedString()
			}
		};
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_deliverycontract", data);
		if (GameManager.ChangeMoneySafe(0f - totalDeliveryPrice, transactionInfo))
		{
			return true;
		}
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"amount",
				totalDeliveryPrice.ToCurrencyFormat()
			},
			{
				"businessName",
				contract.toAddress.ToFormattedString()
			}
		};
		GetContact()?.SendMessage(new TextMessage("ba:messagetype_phone_wholesale_store_delivery_not_enough_funds", messageData));
		return false;
	}

	private static void ShowDeliveredNotification(FoodDeliveryContract contract, BuildingRegistration registration)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{
				"fromname",
				"speedy_bites".GetLocalization()
			},
			{
				"toname",
				contract.toAddress.ToFormattedString()
			},
			{ "businessName", registration.BusinessName }
		};
		Notifications.Show(NotificationType.Success, string.IsNullOrEmpty(registration.BusinessName) ? "notification_delivery_contract_arrived" : "notification_delivery_contract_arrived_business_name", notificationData, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
	}
}
