using System;
using System.Collections.Generic;
using Entities;
using Helpers;
using Localizor;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class MovingServiceDialog : Dialog
{
	private MovingServiceContractSettings _movingServiceContractSettings;

	public MovingServiceDialog()
	{
		npcNameKey = "dialog_moving_company_npc_name";
		if (HasActiveMovingContractForAddress(DialogController.current.contact.Address))
		{
			DialogController.current.ShowEntry(ShowDialogWithExistingContract());
			return;
		}
		DialogEntry entry = (SaveGameManager.Current.BuildingRegistrations.Exists((BuildingRegistration x) => x.RentedByPlayer) ? GetNewContractDialog() : GetNoAvailableBuildingsDialog());
		DialogController.current.ShowEntry(entry);
	}

	private static bool HasActiveMovingContractForAddress(Address address)
	{
		return SaveGameManager.Current.movingServiceContracts.Exists((MovingServiceContract x) => x.movingCompanyRegistration.Address == address);
	}

	private DialogEntry ShowDialogWithExistingContract(bool continueConversation = false)
	{
		string businessName = GetCurrentMovingCompanyRegistration().BusinessName;
		string key = (continueConversation ? "ba:messagetype_dialog_moving_service_continue" : "ba:messagetype_dialog_moving_service_start");
		return new DialogEntry
		{
			messageData = key.Localize(new { businessName }),
			headerKey = npcNameKey,
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = MovingSettings,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = OnShowMovingContractsList,
			SecondOptionTextOverride = "ba:messagetype_dialog_moving_service_manage_contracts"
		};
	}

	private static DialogEntry OnShowMovingContractsList()
	{
		return new DialogEntry
		{
			OnCancel = DialogController.current.CancelDialog,
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.MovingServiceContractsList
		};
	}

	private DialogEntry GetNewContractDialog()
	{
		string businessName = GetCurrentMovingCompanyRegistration().BusinessName;
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", businessName } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_moving_service_start", messageData, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_moving_service_start".Localize(new { businessName }),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(MovingSettings());
			}
		};
	}

	private static BuildingRegistration GetCurrentMovingCompanyRegistration()
	{
		return BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address);
	}

	private static DialogEntry GetNoAvailableBuildingsDialog()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_interior_installation_firm_no_buildings", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_interior_installation_firm_no_buildings".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry MovingSettings()
	{
		return new DialogEntry
		{
			headerKey = "dialog_moving_service_settings_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.MovingServiceSettings,
			OnConfirm = OnMovingSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnMovingSettingsSet()
	{
		_movingServiceContractSettings = DialogController.current.GetInputTransform<MovingServiceContractSettings>(null);
		if (_movingServiceContractSettings.selectedOriginAddress == null || string.IsNullOrEmpty(_movingServiceContractSettings.selectedOriginAddress.streetName))
		{
			Notifications.ShowError("moving_service_notification_select_origin_address");
			return null;
		}
		if (_movingServiceContractSettings.selectedDestinationAddress == null || string.IsNullOrEmpty(_movingServiceContractSettings.selectedDestinationAddress.streetName))
		{
			Notifications.ShowError("moving_service_notification_select_destination_address");
			return null;
		}
		if (_movingServiceContractSettings.selectedDay == -1)
		{
			Notifications.ShowError("dialog_moving_service_select_moving_day");
			return null;
		}
		bool num = BuildingHelper.GetBuildingRegistration(_movingServiceContractSettings.selectedOriginAddress).itemInstances.Count > 0;
		bool flag = SaveGameManager.Current.VehicleInstances.Exists((VehicleInstance vehicle) => vehicle.Address == _movingServiceContractSettings.selectedOriginAddress);
		if (num | flag)
		{
			return new DialogEntry
			{
				messageData = "dialog_moving_service_accept_sell_items_in_building".Localize(),
				Template = DialogEntry.TemplateType.Text,
				OnConfirm = SetMovingContract,
				ConfirmTextOverride = "common_yes".Localize(),
				OnCancel = DialogController.current.CancelDialog
			};
		}
		return SetMovingContract();
	}

	private DialogEntry SetMovingContract()
	{
		MovingServiceContract movingServiceContract = new MovingServiceContract
		{
			movingCompanyRegistration = GetCurrentMovingCompanyRegistration(),
			originMovingAddress = _movingServiceContractSettings.selectedOriginAddress,
			destinationMovingAddress = _movingServiceContractSettings.selectedDestinationAddress,
			movingDay = _movingServiceContractSettings.selectedDay,
			movingHour = _movingServiceContractSettings.selectedHour,
			transferBizManSettings = _movingServiceContractSettings.transferBizManSettings
		};
		SaveGameManager.Current.movingServiceContracts.Add(movingServiceContract);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(movingServiceContract.originMovingAddress);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(movingServiceContract.destinationMovingAddress);
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"businessName",
				BuildingHelper.GetBuildingRegistration(movingServiceContract.originMovingAddress).GetComposedName()
			},
			{
				"businessName2",
				BuildingHelper.GetBuildingRegistration(movingServiceContract.destinationMovingAddress).GetComposedName()
			},
			{
				"day",
				movingServiceContract.movingDay.ToString()
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_moving_service_contract_set_player", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_moving_service_contract_set_manager", messageData, read: true));
		return new DialogEntry
		{
			messageData = "dialog_moving_service_contract_set_manager".Localize(new
			{
				day = movingServiceContract.movingDay
			}),
			Template = DialogEntry.TemplateType.Text,
			OnConfirm = MovingSettings,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = OnShowMovingContractsList,
			SecondOptionTextOverride = "ba:messagetype_dialog_moving_service_manage_contracts",
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	public DialogEntry OnCancelMovingContract(MovingServiceContract movingServiceContract)
	{
		SaveGameManager.Current.movingServiceContracts.Remove(movingServiceContract);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(movingServiceContract.originMovingAddress);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(movingServiceContract.destinationMovingAddress);
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_moving_service_contract_cancelled".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnConfirm = MovingSettings,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = (HasActiveMovingContractForAddress(DialogController.current.contact.Address) ? new Func<DialogEntry>(OnShowMovingContractsList) : null),
			SecondOptionTextOverride = "ba:messagetype_dialog_moving_service_manage_contracts",
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}
}
