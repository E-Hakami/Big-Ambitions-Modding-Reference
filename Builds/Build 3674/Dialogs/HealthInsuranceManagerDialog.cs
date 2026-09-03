using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using Localizor;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class HealthInsuranceManagerDialog : Dialog
{
	public HealthInsuranceManagerDialog()
	{
		npcNameKey = "dialog_health_insurance_manager_npc_name";
		DialogController.current.ShowEntry(HasHrManagerPlansAvailable() ? Start() : NoHrManagers());
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_manager_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "ba:messagetype_dialog_health_insurance_manager_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_health_insurance_manager_new_partnership".Localize(),
			OnConfirm = StartPartnership,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry NoHrManagers()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_manager_no_hr_managers", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_health_insurance_manager_no_hr_managers".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry StartPartnership()
	{
		return new DialogEntry
		{
			headerKey = "dialog_health_insurance_partnership_header",
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "dialog_accept_button".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.HealthInsurancePartnershipSettings,
			OnConfirm = OnHealthInsurancePartnershipSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnHealthInsurancePartnershipSettingsSet()
	{
		HealthInsurancePartnershipSettings inputComponent = DialogController.current.GetInputComponent<HealthInsurancePartnershipSettings>();
		if (inputComponent.selectedHrManagerPlan == null)
		{
			Notifications.ShowError("healthinsurancemanagerdialog_notification_select_manager");
			return null;
		}
		if (!inputComponent.hasSelectedPlan)
		{
			Notifications.ShowError("healthinsurancemanagerdialog_notification_select_plan_type");
			return null;
		}
		HealthInsurancePlanOffer item = new HealthInsurancePlanOffer(inputComponent.selectedHrManagerPlan.id, inputComponent.selectedPlanType);
		SaveGameManager.Current.healthInsurancePlanOffers.Add(item);
		GameEvent.Invoke("ba:gameevent_newhealthinsuranceplanoffer");
		string name = inputComponent.selectedHrManagerPlan.HrManagerInstance.characterData.name;
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{ "selectedEmployee", name },
			{
				"healthPlanType",
				inputComponent.selectedPlanType.GetLocalization()
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_health_insurance_manager_on_partnership_settings_set_player", messageData, read: true));
		Dictionary<string, string> messageData2 = new Dictionary<string, string> { { "selectedEmployee", name } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_manager_on_partnership_settings_set_manager", messageData2, read: true));
		if (HasHrManagerPlansAvailable())
		{
			return new DialogEntry
			{
				messageData = "ba:messagetype_dialog_health_insurance_manager_on_partnership_settings_set_manager".Localize(new
				{
					selectedEmployee = name
				}),
				InputTemplate = DialogEntry.InputTemplateName.None,
				OnVisible = OnMoreHelpOffered().ShowEntry
			};
		}
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_health_insurance_manager_on_partnership_settings_set_manager".Localize(new
			{
				selectedEmployee = name
			}),
			InputTemplate = DialogEntry.InputTemplateName.None,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry OnMoreHelpOffered()
	{
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_health_insurance_manager_more_help_offered".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_health_insurance_manager_new_partnership".Localize(),
			OnConfirm = StartPartnership,
			OnCancel = DialogController.current.FinishDialog
		};
	}

	private static bool HasHrManagerPlansAvailable()
	{
		return SaveGameManager.Current.hrManagerPlans.Any((HrManagerPlan x) => x.CanHaveHealthInsurancePlan);
	}
}
