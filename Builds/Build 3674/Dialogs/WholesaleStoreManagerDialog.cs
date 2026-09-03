using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using UI;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class WholesaleStoreManagerDialog : Dialog
{
	public WholesaleStoreManagerDialog()
	{
		npcNameKey = "dialog_wholesale_store_npc_name";
		bool flag = SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => !string.IsNullOrWhiteSpace(x.BusinessName) && x.RentedByPlayer);
		DialogController.current.ShowEntry((!flag) ? NoBusinesses() : Start());
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_wholesale_store_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_wholesale_store_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_wholesale_start_contract".Localize(),
			OnConfirm = StartContract,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry NoBusinesses()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_wholesale_store_no_businesses", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_wholesale_store_no_businesses".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry StartContract()
	{
		return new DialogEntry
		{
			headerKey = "dialog_wholesale_store_delivery_contract_header",
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "dialog_accept_button".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.DeliveryContractSettings,
			OnConfirm = OnDeliveryContractSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnDeliveryContractSettingsSet()
	{
		DeliveryContractSettings deliveryContractSettings = DialogController.current.GetInputComponent<DeliveryContractSettings>();
		if (deliveryContractSettings.selectedBusiness == null)
		{
			Notifications.ShowError("common_notification_select_business");
			return null;
		}
		if (SaveGameManager.Current.DeliveryContracts.Any((DeliveryContract x) => x.businessAddress == deliveryContractSettings.selectedBusiness.Address && x.wholesaleAddress == DialogController.current.contact.Address))
		{
			Notifications.ShowError("wholesalestoremanagerdialog_notification_already_have_contract", "wholesalestoremanagerdialog_notification_already_have_contract");
			return null;
		}
		if (!deliveryContractSettings.selectedBusiness.itemInstances.Values.Any((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isbusinessstorage)))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"shelf",
				LocalizationHelper.GetItemLabel(ItemsGetter.GetRandomByTag(TagRef.Itemtag.isbusinessstorage)).ToString()
			} };
			Notifications.Show(NotificationType.Error, "wholesalestoremanagerdialog_notification_require_shelf_in_business", notificationData);
			return null;
		}
		WholesaleStoreSettings wholesaleStoreSettings = (WholesaleStoreSettings)BuildingHelper.GetBuilding(DialogController.current.contact.Address).SpecialService.settings;
		DeliveryContract item = new DeliveryContract
		{
			nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay(),
			wholesaleAddress = DialogController.current.contact.Address,
			businessAddress = deliveryContractSettings.selectedBusiness.Address,
			items = new List<DeliveryContractItem>(),
			deliveryFee = wholesaleStoreSettings.deliveryFee
		};
		SaveGameManager.Current.DeliveryContracts.Add(item);
		GameEvent.Invoke("ba:gameevent_newdeliverycontract");
		string businessName = deliveryContractSettings.selectedBusiness.BusinessName;
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", businessName } };
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_wholesale_store_on_contract_settings_set_player", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_wholesale_store_on_contract_settings_set_manager", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_wholesale_store_on_contract_settings_set_manager".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.None,
			OnConfirm = () => OnOpenBizMan(deliveryContractSettings.selectedBusiness),
			OnSecondOption = StartContract,
			SecondOptionTextOverride = "dialog_wholesale_add_another_contract",
			ConfirmTextOverride = "open_in_bizman".Localize(),
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnOpenBizMan(BuildingRegistration registration)
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(registration.Address, "Deliveries");
		return null;
	}
}
