using System;
using System.Collections.Generic;
using BigAmbitions.Tags;
using Dialogs;
using Entities;
using Helpers;
using Localizor;
using UI;
using UI.Notification;
using Vehicles;
using Vehicles.VehicleTypes;

namespace Buildings;

public class VehicleStoreDialog : Dialog
{
	private VehicleContractSettings _vehicleContractSettings;

	private readonly bool _isPhoneCall;

	private static Contact CurrentContact => DialogController.current.contact;

	public VehicleStoreDialog()
	{
		npcNameKey = "dialog_vehicle_store_npc_name";
		_isPhoneCall = DialogController.current.dialogType == DialogType.PhoneCall;
		if (HasPendingTruckDelivery())
		{
			DialogController.current.ShowEntry(PendingTruckDeliveries(continueConversation: false));
		}
		else if (_isPhoneCall && !VehicleDeliveryHelper.HasAnyWarehouseAvailableToDeliver())
		{
			DialogController.current.ShowEntry(NoWarehouses());
		}
		else
		{
			DialogController.current.ShowEntry(Start());
		}
	}

	private DialogEntry Start()
	{
		string businessName = BuildingHelper.GetBuildingRegistration(CurrentContact.Address).BusinessName;
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", businessName } };
		CurrentContact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_start", messageData, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_vehicle_store_start".Localize(new { businessName }),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = VehicleContractSettings().ShowEntry
		};
	}

	private DialogEntry NoWarehouses()
	{
		CurrentContact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_no_warehouses", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_vehicle_store_no_warehouses".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = (_isPhoneCall ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((!_isPhoneCall) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry VehicleContractSettings()
	{
		return new DialogEntry
		{
			headerKey = "dialog_vehicle_store_contract_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.VehicleContractSettings,
			OnConfirm = OnVehicleSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry PendingTruckDeliveries(bool continueConversation)
	{
		string businessName = BuildingHelper.GetBuildingRegistration(CurrentContact.Address).BusinessName;
		string key = (continueConversation ? "dialog_recruitment_agency_already_has_campaign_continue" : "dialog_vehicle_store_start");
		return new DialogEntry
		{
			messageData = key.Localize(new { businessName }),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = OnNewOrder,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = OnShowVehicleDeliveriesList,
			SecondOptionTextOverride = "ba:messagetype_dialog_vehicle_store_manage_deliveries"
		};
	}

	private static DialogEntry OnShowVehicleDeliveriesList()
	{
		return new DialogEntry
		{
			OnCancel = DialogController.current.CancelDialog,
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.VehicleDeliveryContractsList
		};
	}

	private DialogEntry OnVehicleSettingsSet()
	{
		_vehicleContractSettings = DialogController.current.GetInputTransform<VehicleContractSettings>(null);
		if (_vehicleContractSettings.selectedVehicleForSale == null)
		{
			Notifications.ShowError("common_notification_select_vehicle");
			return null;
		}
		bool isDelivery = _vehicleContractSettings.isDelivery;
		if (isDelivery && (_vehicleContractSettings.selectedAddress == null || string.IsNullOrEmpty(_vehicleContractSettings.selectedAddress.streetName)))
		{
			Notifications.ShowError("common_notification_select_address");
			return null;
		}
		if (isDelivery)
		{
			return SetVehicleContract();
		}
		if (!_vehicleContractSettings.selectedVehicleForSale.Purchase())
		{
			return null;
		}
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.SetAsset(_vehicleContractSettings.selectedVehicleForSale, initUi: false);
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.RunShowcaseAnimation();
		return OnVehiclePurchased();
	}

	private DialogEntry SetVehicleContract()
	{
		_vehicleContractSettings.selectedVehicleForSale.Order(_vehicleContractSettings.selectedAddress, CurrentContact, showNotification: false);
		VehicleStoreSettings vehicleStoreSettings = VehicleDeliveryHelper.GetVehicleStoreSettings(CurrentContact.Address);
		return new DialogEntry
		{
			messageData = "dialog_vehicle_store_contract_set_manager".Localize(new
			{
				amount = vehicleStoreSettings.hoursToDeliver.ToString()
			}),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = (HasPendingTruckDelivery() ? new Action(PendingTruckDeliveries(continueConversation: true).ShowEntry) : new Action(OnMoreHelpOffered().ShowEntry))
		};
	}

	private DialogEntry OnVehiclePurchased()
	{
		string vehicleName = _vehicleContractSettings.selectedVehicleForSale.VehicleName;
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"vehicleTypeName",
			vehicleName.GetLocalization()
		} };
		CurrentContact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_vehicle_purchased_player", messageData, read: true));
		CurrentContact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_vehicle_purchased_manager", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_vehicle_store_vehicle_purchased_manager".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = OnMoreHelpOffered().ShowEntry
		};
	}

	private DialogEntry OnMoreHelpOffered()
	{
		return new DialogEntry
		{
			messageData = "dialog_marketing_agency_more_help_offered".Localize(),
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = OnNewOrder,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize()
		};
	}

	private DialogEntry OnNewOrder()
	{
		if (_isPhoneCall && !VehicleDeliveryHelper.HasAnyWarehouseAvailableToDeliver())
		{
			return NoWarehouses();
		}
		return VehicleContractSettings();
	}

	public DialogEntry OnCancelVehicleDelivery(VehicleDeliveryContract vehicleDeliveryContract)
	{
		SaveGameManager.Current.vehicleDeliveryContracts.Remove(vehicleDeliveryContract);
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_vehicle_store_delivery_cancelled".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = (HasPendingTruckDelivery() ? new Action(OnShowVehicleDeliveriesList().ShowEntry) : new Action(OnMoreHelpOffered().ShowEntry))
		};
	}

	private static bool HasPendingTruckDelivery()
	{
		return SaveGameManager.Current.vehicleDeliveryContracts.Exists(delegate(VehicleDeliveryContract contract)
		{
			VehicleType vehicleType = VehicleTypeHelper.GetVehicleType(contract.vehicleTypeName);
			bool num = contract.vehicleStoreAddress == CurrentContact.Address;
			bool flag = vehicleType.HasTag(TagRef.Vehicletag.istruck);
			return num & flag;
		});
	}
}
