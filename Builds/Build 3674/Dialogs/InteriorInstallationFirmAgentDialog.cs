using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blueprints;
using Entities;
using Helpers;
using Localizor;
using Streets;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class InteriorInstallationFirmAgentDialog : Dialog
{
	private InteriorInstallationFirmDesignSettings _interiorInstallationFirmDesignSettings;

	public InteriorInstallationFirmAgentDialog()
	{
		npcNameKey = "dialog_interior_installation_firm_npc_name";
		if (SaveGameManager.Current.interiorInstallationFirmContracts.Any((InteriorInstallationFirmContract x) => x.interiorInstallationFirmAddress == DialogController.current.contact.Address))
		{
			DialogController.current.ShowEntry(AlreadyHasContractActive(continueConversation: false));
			return;
		}
		bool flag = SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => x.RentedByPlayer);
		DialogController.current.ShowEntry(flag ? Start(delegate
		{
			DialogController.current.ShowEntry(DesignSettings());
		}) : NoBuildings());
	}

	private DialogEntry Start(Action nextDialog)
	{
		string businessName = BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address).BusinessName;
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", businessName } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_interior_installation_firm_start", messageData, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_interior_installation_firm_start".Localize(new { businessName }),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = nextDialog
		};
	}

	private DialogEntry AlreadyHasContractActive(bool continueConversation)
	{
		return new DialogEntry
		{
			messageData = (continueConversation ? "dialog_recruitment_agency_already_has_campaign_continue" : "dialog_recruitment_agency_already_has_campaign_new").Localize(),
			headerKey = npcNameKey,
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = DesignSettings,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = OnShowRecruitmentList,
			SecondOptionTextOverride = "dialog_installation_firm_manage_contracts"
		};
	}

	private DialogEntry OnShowRecruitmentList()
	{
		return new DialogEntry
		{
			OnCancel = DialogController.current.CancelDialog,
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.InstallationContractsList
		};
	}

	private DialogEntry NoBuildings()
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

	private DialogEntry DesignSettings()
	{
		return new DialogEntry
		{
			headerKey = "dialog_interior_installation_firm_design_settings_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.InteriorInstallationFirmDesignSettings,
			OnConfirm = OnDesignSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnDesignSettingsSet()
	{
		_interiorInstallationFirmDesignSettings = DialogController.current.GetInputTransform<InteriorInstallationFirmDesignSettings>(null);
		if (_interiorInstallationFirmDesignSettings.selectedAddress == null)
		{
			Notifications.ShowError("common_notification_select_address");
			return null;
		}
		if (_interiorInstallationFirmDesignSettings.selectedDay == -1)
		{
			Notifications.ShowError("dialog_interior_installation_firm_select_notification_select_installation_day");
			return null;
		}
		if (_interiorInstallationFirmDesignSettings.designName == null)
		{
			Notifications.ShowError("dialog_interior_installation_firm_select_notification_select_design");
			return null;
		}
		if (_interiorInstallationFirmDesignSettings.BuildingRegistration.itemInstances.Count > 0)
		{
			return new DialogEntry
			{
				messageData = "dialog_interior_installation_firm_accept_sell_items_in_building".Localize(),
				Template = DialogEntry.TemplateType.Text,
				OnConfirm = SetDesignContract,
				ConfirmTextOverride = "common_yes".Localize(),
				OnCancel = DialogController.current.CancelDialog
			};
		}
		return SetDesignContract();
	}

	private DialogEntry SetDesignContract()
	{
		InteriorInstallationFirmContract interiorInstallationFirmContract = new InteriorInstallationFirmContract
		{
			interiorInstallationFirmAddress = DialogController.current.contact.Address,
			addressToDoTheInstallation = _interiorInstallationFirmDesignSettings.selectedAddress,
			dayOfInstallation = _interiorInstallationFirmDesignSettings.selectedDay,
			designName = _interiorInstallationFirmDesignSettings.designName,
			isBlueprint = _interiorInstallationFirmDesignSettings.isBlueprint,
			businessTypeName = _interiorInstallationFirmDesignSettings.BuildingRegistration.businessTypeName,
			isCompatBlueprint = _interiorInstallationFirmDesignSettings.isCompatBlueprint,
			hasDiscontinuedItems = _interiorInstallationFirmDesignSettings.hasDiscontinuedItems
		};
		SaveGameManager.Current.interiorInstallationFirmContracts.Add(interiorInstallationFirmContract);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(interiorInstallationFirmContract.addressToDoTheInstallation);
		if (interiorInstallationFirmContract.isBlueprint)
		{
			UpdateBlueprintCompatStatus();
		}
		GameEvent.Invoke("ba:gameevent_blueprintordered");
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{ "text", interiorInstallationFirmContract.designName },
			{
				"address",
				interiorInstallationFirmContract.addressToDoTheInstallation.ToFormattedString()
			},
			{
				"day",
				interiorInstallationFirmContract.dayOfInstallation.ToString()
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_interior_installation_firm_contract_set_player", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_interior_installation_firm_contract_set_manager", messageData, read: true));
		return new DialogEntry
		{
			messageData = "dialog_interior_installation_firm_contract_set_manager".Localize(new
			{
				day = interiorInstallationFirmContract.dayOfInstallation
			}),
			Template = DialogEntry.TemplateType.Text,
			OnConfirm = DesignSettings,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = OnShowRecruitmentList,
			SecondOptionTextOverride = "dialog_installation_firm_manage_contracts",
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	public DialogEntry OnCancelInstallation(InteriorInstallationFirmContract installationFirmContract)
	{
		SaveGameManager.Current.interiorInstallationFirmContracts.Remove(installationFirmContract);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(installationFirmContract.addressToDoTheInstallation);
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_on_cancel_campaign_player", null, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_installation_firm_on_cancel_contract_firm", null, read: true));
		if (installationFirmContract.isCompatBlueprint)
		{
			Blueprint result = BlueprintsFolderLoader.GetBlueprint(installationFirmContract.designName).Result;
			result.metadata.otherData.Add(new BlueprintDataElement(DataElement.CompatBlueprint, SaveGameManager.Current.characterId));
			result.UpdateMetadata();
		}
		return new DialogEntry
		{
			messageData = "dialog_interior_installation_firm_on_cancel_installation".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnConfirm = DesignSettings,
			ConfirmTextOverride = "dialog_more_help_new_order".Localize(),
			OnSecondOption = (SaveGameManager.Current.interiorInstallationFirmContracts.Any((InteriorInstallationFirmContract x) => x.interiorInstallationFirmAddress == DialogController.current.contact.Address) ? new Func<DialogEntry>(OnShowRecruitmentList) : null),
			SecondOptionTextOverride = "dialog_installation_firm_manage_contracts",
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private async Task UpdateBlueprintCompatStatus()
	{
		Blueprint obj = await BlueprintsFolderLoader.GetBlueprint(_interiorInstallationFirmDesignSettings.designName);
		obj.metadata.RemoveDataElement(DataElement.CompatBlueprint);
		await obj.UpdateMetadata();
	}
}
