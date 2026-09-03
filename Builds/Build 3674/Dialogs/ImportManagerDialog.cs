using System;
using System.Collections.Generic;
using System.Linq;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using UI;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class ImportManagerDialog : Dialog
{
	public ImportManagerDialog()
	{
		npcNameKey = "dialog_import_npc_name";
		bool flag = SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => x.RentedByPlayer && x.GetBuildingType() == "ba:buildingtype_warehouse" && x.businessTypeName != "ba:businesstype_empty");
		bool flag2 = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withSkills = new string[1] { "ba:skill_purchasingagent" },
			excludeBeingReplaced = true
		}).Any((EmployeeInstance x) => x.IsAssignedToAnyWorkShift() && !SaveGameManager.Current.importPartnerships.Exists((ImportPartnership i) => i.employeeInstanceId == x.id));
		if (!flag)
		{
			DialogController.current.ShowEntry(NoWarehouse());
		}
		else if (!flag2)
		{
			DialogController.current.ShowEntry(NoPurchasingAgent());
		}
		else
		{
			DialogController.current.ShowEntry(Start());
		}
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_import_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_import_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_import_create_new_partnership".Localize(),
			OnConfirm = StartContract,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry NoWarehouse()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_import_no_warehouses", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_import_no_warehouses".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry NoPurchasingAgent()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_import_no_purchasing_agents", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_import_no_purchasing_agents".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry StartContract()
	{
		return new DialogEntry
		{
			headerKey = "dialog_import_partnership_header",
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "dialog_accept_button".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.ImportPartnershipSettings,
			OnConfirm = OnImportPartnershipSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnImportPartnershipSettingsSet()
	{
		ImportPartnershipSettings inputComponent = DialogController.current.GetInputComponent<ImportPartnershipSettings>();
		if (inputComponent.selectedEmployeeInstance == null)
		{
			Notifications.ShowError("importmanagerdialog_notification_select_agent");
			return null;
		}
		_ = (ImportExportSettings)BuildingHelper.GetBuilding(DialogController.current.contact.Address).SpecialService.settings;
		ImportPartnership importPartnership = new ImportPartnership
		{
			importAddress = DialogController.current.contact.Address,
			employeeInstanceId = inputComponent.selectedEmployeeInstance.id,
			headquartersAddress = inputComponent.selectedEmployeeInstance.assignedAddress,
			nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay()
		};
		SaveGameManager.Current.importPartnerships.Add(importPartnership);
		GameEvent.Invoke("ba:gameevent_newimportpartnership");
		string name = inputComponent.selectedEmployeeInstance.characterData.name;
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "selectedEmployee", name } };
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_import_on_partnership_settings_set_player", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_import_on_partnership_settings_set_manager", messageData, read: true));
		return new DialogEntry
		{
			messageData = "dialog_import_on_partnership_settings_set_manager".Localize(new
			{
				selectedEmployee = name
			}),
			InputTemplate = DialogEntry.InputTemplateName.None,
			OnConfirm = () => OnOpenBizMan(importPartnership),
			ConfirmTextOverride = "open_in_bizman".Localize(),
			OnCancel = DialogController.current.FinishDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnOpenBizMan(ImportPartnership importPartnership)
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(importPartnership.headquartersAddress, "PurchasingAgents");
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.purchasingAgentsPlanList.SelectPlan(null, importPartnership);
		return null;
	}
}
