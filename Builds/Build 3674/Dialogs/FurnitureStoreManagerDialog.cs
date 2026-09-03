using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Streets;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class FurnitureStoreManagerDialog : Dialog
{
	private const float MinimumPurchaseCost = 2000f;

	public FurnitureStoreManagerDialog()
	{
		npcNameKey = "dialog_furniture_store_npc_name";
		bool flag = SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => x.RentedByPlayer);
		bool flag2 = SaveGameManager.Current.FurnitureDeliveryContracts.Any((FurnitureDeliveryContract x) => x.fromAddress == DialogController.current.contact.Address);
		DialogController.current.ShowEntry((!flag) ? NoAddresses() : (flag2 ? AlreadyHasAPendingDelivery(continueConversation: false) : Start()));
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_furniture_store_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_furniture_store_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(FurnitureDeliveryContract());
			}
		};
	}

	private DialogEntry FurnitureDeliveryContract()
	{
		return new DialogEntry
		{
			messageData = "dialog_furniture_store_player_start".Localize(),
			headerKey = "dialog_furniture_delivery_header",
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "furniture_delivery_dialog_order".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.FurnitureDeliverySettings,
			OnConfirm = OnDeliveryContractSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry NoAddresses()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_furniture_store_no_addresses", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_furniture_store_no_addresses".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry AlreadyHasAPendingDelivery(bool continueConversation)
	{
		return new DialogEntry
		{
			messageData = (continueConversation ? "dialog_recruitment_agency_already_has_campaign_continue" : "dialog_furniture_store_start").Localize(),
			headerKey = npcNameKey,
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = FurnitureDeliveryContract,
			ConfirmTextOverride = "furniture_delivery_dialog_order".Localize(),
			OnSecondOption = OnShowFurnitureDeliveriesList,
			SecondOptionTextOverride = "dialog_furniture_deliveries_manage_deliveries"
		};
	}

	private DialogEntry OnShowFurnitureDeliveriesList()
	{
		return new DialogEntry
		{
			OnCancel = DialogController.current.CancelDialog,
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.FurnitureDeliveriesList
		};
	}

	private DialogEntry OnDeliveryContractSettingsSet()
	{
		FurnitureDeliveryContractSettings inputComponent = DialogController.current.GetInputComponent<FurnitureDeliveryContractSettings>();
		if (inputComponent.selectedAddress == null)
		{
			Notifications.ShowError("common_notification_select_address", "common_notification_select_address");
			return null;
		}
		(int, int) selectedDeliverySlot = inputComponent.selectedDeliverySlot;
		if (selectedDeliverySlot.Item1 == -1 && selectedDeliverySlot.Item2 == -1)
		{
			Notifications.ShowError("common_notification_select_delivery_time", "common_notification_select_delivery_time");
			return null;
		}
		if (inputComponent.TotalItemsToDeliverAmount <= 0)
		{
			Notifications.ShowError("dialog_furniture_delivery_select_items_notification", "dialog_furniture_delivery_select_items_notification");
			return null;
		}
		List<FurnitureDeliveryItem> list = inputComponent.itemsToDeliver.Select((DeliveryContractSettingsBase.ItemToDeliver item) => new FurnitureDeliveryItem
		{
			itemName = item.itemName,
			amount = item.amount,
			pricePerUnit = item.price
		}).ToList();
		FurnitureDeliveryContract furnitureDeliveryContract = new FurnitureDeliveryContract
		{
			fromAddress = DialogController.current.contact.Address,
			toAddress = inputComponent.selectedAddress,
			dayOfDelivery = inputComponent.selectedDeliverySlot.Item1,
			itemsToDeliver = list,
			hourOfDelivery = inputComponent.selectedDeliverySlot.Item2,
			deliveryFee = 250f
		};
		if (furnitureDeliveryContract.TotalDeliveryPrice < 2000f)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"minimumCost",
				2000f.ToShortCurrencyFormat()
			} };
			Notifications.Show(NotificationType.Error, "dialog_furniture_delivery_minimum_price_not_reached", notificationData, 4f, "dialog_furniture_delivery_minimum_price_not_reached");
			return null;
		}
		SaveGameManager.Current.FurnitureDeliveryContracts.Add(furnitureDeliveryContract);
		string businessName = BuildingHelper.GetBuildingRegistration(inputComponent.selectedAddress).BusinessName;
		string text = list.Aggregate("", (string current, FurnitureDeliveryItem item) => current + $"{item.amount}x {item.itemName.GetLocalization()}<br>");
		string value = text.Substring(0, text.Length - 4);
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"amount",
				inputComponent.TotalItemsToDeliverAmount.ToString()
			},
			{
				"address",
				inputComponent.selectedAddress.ToFormattedString()
			},
			{ "text", value },
			{
				"day",
				furnitureDeliveryContract.dayOfDelivery.ToString()
			},
			{
				"hour",
				furnitureDeliveryContract.hourOfDelivery.GetFormattedTime()
			},
			{ "businessName", businessName }
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage(string.IsNullOrEmpty(businessName) ? "ba:messagetype_dialog_furniture_store_on_contract_settings_set_player" : "ba:messagetype_dialog_furniture_store_on_contract_settings_set_player_business_name", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_furniture_store_on_contract_settings_set_manager", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_furniture_store_on_contract_settings_set_manager".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.None,
			OnConfirm = FurnitureDeliveryContract,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = OnShowFurnitureDeliveriesList,
			SecondOptionTextOverride = "dialog_furniture_deliveries_manage_deliveries",
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	public DialogEntry OnCancelFurnitureDelivery(FurnitureDeliveryContract deliveryContract)
	{
		SaveGameManager.Current.FurnitureDeliveryContracts.Remove(deliveryContract);
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_furniture_store_cancel_delivery", null, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_furniture_store_delivery_cancelled", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_furniture_store_delivery_cancelled".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnConfirm = FurnitureDeliveryContract,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}
}
