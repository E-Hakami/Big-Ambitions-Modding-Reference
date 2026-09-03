using System;
using System.Collections.Generic;
using Buildings.BuildingTypes.Special.FoodDelivery;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Streets;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class FoodDeliveryDialog : Dialog
{
	private const string StartMessage = "dialog_food_delivery_start";

	private const string NoAddressesMessage = "dialog_food_delivery_no_addresses";

	private const string AnythingElseMessage = "dialog_food_delivery_anything_else";

	private const string OrderHeader = "dialog_food_delivery_header";

	private const string OrderButton = "furniture_delivery_dialog_order";

	private const string ManageDeliveriesButton = "dialog_furniture_deliveries_manage_deliveries";

	private const string OrderSetPlayerMessage = "dialog_food_delivery_on_contract_set_player";

	private const string OrderSetManagerMessage = "dialog_food_delivery_on_contract_set_manager";

	private const string MinimumNotReachedMessage = "dialog_food_delivery_minimum_not_reached";

	private const string CancelPlayerMessage = "dialog_food_delivery_cancel_player";

	private const string CancelledManagerMessage = "dialog_food_delivery_cancelled_manager";

	private const string SelectAddressNotification = "common_notification_select_address";

	private const string SelectDeliveryTimeNotification = "common_notification_select_delivery_time";

	private const string SelectItemsNotification = "dialog_furniture_delivery_select_items_notification";

	private static bool HasPendingDelivery => FoodDeliveryHelper.GetContracts().Count > 0;

	public FoodDeliveryDialog()
	{
		npcNameKey = "speedy_bites";
		DialogController.current.ShowEntry(CreateOpeningEntry());
	}

	private DialogEntry CreateOpeningEntry()
	{
		if (!PlayerHasRentedBuilding())
		{
			return NoAddresses();
		}
		if (!HasPendingDelivery)
		{
			return Start();
		}
		return AlreadyHasAPendingDelivery(continueConversation: false);
	}

	private static bool PlayerHasRentedBuilding()
	{
		return BuildingHelper.GetPlayerBuildingRegistrations().Count > 0;
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("dialog_food_delivery_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_food_delivery_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(OrderEntry());
			}
		};
	}

	private static DialogEntry NoAddresses()
	{
		DialogController.current.contact.SendMessage(new TextMessage("dialog_food_delivery_no_addresses", null, read: true, isNewInteraction: true));
		return CreateClosingEntry("dialog_food_delivery_no_addresses");
	}

	private DialogEntry OrderEntry()
	{
		return new DialogEntry
		{
			headerKey = "dialog_food_delivery_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.FoodDeliverySettings,
			ConfirmTextOverride = "furniture_delivery_dialog_order".Localize(),
			OnConfirm = OnOrderSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry AlreadyHasAPendingDelivery(bool continueConversation)
	{
		string key = (continueConversation ? "dialog_food_delivery_anything_else" : "dialog_food_delivery_start");
		return new DialogEntry
		{
			messageData = key.Localize(),
			headerKey = npcNameKey,
			OnConfirm = OrderEntry,
			ConfirmTextOverride = "furniture_delivery_dialog_order".Localize(),
			OnSecondOption = OnShowDeliveriesList,
			SecondOptionTextOverride = "dialog_furniture_deliveries_manage_deliveries",
			OnCancel = DialogController.current.FinishDialog
		};
	}

	private static DialogEntry OnShowDeliveriesList()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.FoodDeliveriesList,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry OnOrderSettingsSet()
	{
		FoodDeliveryContractSettings inputComponent = DialogController.current.GetInputComponent<FoodDeliveryContractSettings>();
		if (!ValidateOrder(inputComponent))
		{
			return null;
		}
		FoodDeliveryContract foodDeliveryContract = CreateContract(inputComponent);
		FoodDeliveryHelper.GetContracts().Add(foodDeliveryContract);
		SendOrderMessages(foodDeliveryContract);
		return new DialogEntry
		{
			messageData = "dialog_food_delivery_on_contract_set_manager".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.None,
			OnConfirm = OrderEntry,
			ConfirmTextOverride = "furniture_delivery_dialog_order".Localize(),
			OnSecondOption = OnShowDeliveriesList,
			SecondOptionTextOverride = "dialog_furniture_deliveries_manage_deliveries",
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	public DialogEntry OnCancelFoodDelivery(FoodDeliveryContract contract)
	{
		FoodDeliveryHelper.GetContracts().Remove(contract);
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("dialog_food_delivery_cancel_player", null, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("dialog_food_delivery_cancelled_manager", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_food_delivery_cancelled_manager".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnConfirm = OrderEntry,
			ConfirmTextOverride = "furniture_delivery_dialog_order".Localize(),
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private static bool ValidateOrder(FoodDeliveryContractSettings orderSettings)
	{
		if (orderSettings.selectedAddress == null)
		{
			Notifications.ShowError("common_notification_select_address", "common_notification_select_address", trackOnSaveGame: false);
			return false;
		}
		if (!orderSettings.HasSelectedDeliverySlot)
		{
			Notifications.ShowError("common_notification_select_delivery_time", "common_notification_select_delivery_time", trackOnSaveGame: false);
			return false;
		}
		if (orderSettings.TotalItemsToDeliverAmount <= 0)
		{
			Notifications.ShowError("dialog_furniture_delivery_select_items_notification", "dialog_furniture_delivery_select_items_notification", trackOnSaveGame: false);
			return false;
		}
		if (orderSettings.MinimumOrderCost > 0f && orderSettings.TotalPrice < orderSettings.MinimumOrderCost)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"minimumOrderCost",
				orderSettings.MinimumOrderCost.ToShortCurrencyFormat()
			} };
			Notifications.Show(NotificationType.Error, "dialog_food_delivery_minimum_not_reached", notificationData, 4f, "dialog_food_delivery_minimum_not_reached", null, notificationSound: true, trackOnSaveGame: false);
			return false;
		}
		return true;
	}

	private static FoodDeliveryContract CreateContract(FoodDeliveryContractSettings orderSettings)
	{
		List<FurnitureDeliveryItem> list = new List<FurnitureDeliveryItem>(orderSettings.itemsToDeliver.Count);
		foreach (DeliveryContractSettingsBase.ItemToDeliver item in orderSettings.itemsToDeliver)
		{
			list.Add(new FurnitureDeliveryItem
			{
				itemName = item.itemName,
				amount = item.amount,
				pricePerUnit = item.price
			});
		}
		return new FoodDeliveryContract
		{
			toAddress = orderSettings.selectedAddress,
			dayOfDelivery = orderSettings.selectedDeliverySlot.Item1,
			hourOfDelivery = orderSettings.selectedDeliverySlot.Item2,
			deliveryFee = orderSettings.DeliveryFee,
			itemsToDeliver = list
		};
	}

	private static void SendOrderMessages(FoodDeliveryContract contract)
	{
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"orderItemsText",
				FoodDeliveryHelper.BuildItemsText(contract, includePrice: false)
			},
			{
				"deliveryAddress",
				contract.toAddress.ToFormattedString()
			},
			{
				"deliveryTimeSlot",
				DeliveryContractSettingsBase.GetDeliverySlotLabel(contract.dayOfDelivery, contract.hourOfDelivery)
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("dialog_food_delivery_on_contract_set_player", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("dialog_food_delivery_on_contract_set_manager", null, read: true));
	}

	private static DialogEntry CreateClosingEntry(string messageKey)
	{
		bool flag = DialogController.current.dialogType == DialogType.PhoneCall;
		return new DialogEntry
		{
			messageData = messageKey.Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = (flag ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = (flag ? null : new Action(DialogController.current.FinishDialog))
		};
	}
}
