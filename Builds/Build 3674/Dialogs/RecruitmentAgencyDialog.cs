using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Tags;
using Entities;
using Helpers;
using Localizor;
using UI.Dialog;
using UI.Notification;
using UnityEngine;

namespace Dialogs;

public class RecruitmentAgencyDialog : Dialog
{
	public RecruitmentAgencyDialog()
	{
		npcNameKey = "dialog_recruitment_agency_npc_name";
		if (SaveGameManager.Current.RecruitmentCampaigns.Any((RecruitmentCampaign x) => x.agencyAddress == DialogController.current.contact.Address))
		{
			DialogController.current.ShowEntry(AlreadyHasCampaignActive(continueConversation: false));
			return;
		}
		bool flag = SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => !string.IsNullOrWhiteSpace(x.BusinessName) && x.RentedByPlayer);
		if (!SaveGameManager.Current.PlayerDiplomas.Any((Diploma x) => x.name == DiplomaName.BasicHr && x.completed))
		{
			DialogController.current.ShowEntry(RequiredDiplomaNotFound());
			return;
		}
		if (!flag)
		{
			DialogController.current.ShowEntry(NoBusinesses());
			return;
		}
		Action nextDialog = delegate
		{
			DialogController.current.ShowEntry(CandidateProperties());
		};
		DialogController.current.ShowEntry(Start(nextDialog));
	}

	private DialogEntry Start(Action nextDialog)
	{
		string text = BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address).BusinessName;
		if (text.EndsWith("."))
		{
			string text2 = text;
			text = text2.Substring(0, text2.Length - 1);
		}
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", text } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_start", messageData, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_recruitment_agency_start".Localize(new
			{
				businessName = text
			}),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = nextDialog
		};
	}

	private DialogEntry CandidateProperties()
	{
		return new DialogEntry
		{
			headerKey = "dialog_recruitment_agency_candidate_properties_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.RecruitmentSettings,
			OnConfirm = OnRecruitmentSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry RequiredDiplomaNotFound()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_required_diploma_not_found", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_recruitment_agency_required_diploma_not_found".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry NoBusinesses()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_no_businesses", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_recruitment_agency_no_businesses".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry AlreadyHasCampaignActive(bool continueConversation)
	{
		Action candidatePropertyDialog = delegate
		{
			DialogController.current.ShowEntry(CandidateProperties());
		};
		return new DialogEntry
		{
			messageData = (continueConversation ? "dialog_recruitment_agency_already_has_campaign_continue" : "dialog_recruitment_agency_already_has_campaign_new").Localize(),
			headerKey = npcNameKey,
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = () => Start(candidatePropertyDialog),
			ConfirmTextOverride = "dialog_recruitment_agency_start_new_campaign".Localize(),
			OnSecondOption = OnShowRecruitmentList,
			SecondOptionTextOverride = "dialog_recruitment_agency_manage_campaigns"
		};
	}

	private DialogEntry OnMoreHelpOffered()
	{
		return new DialogEntry
		{
			messageData = "dialog_recruitment_agency_already_has_campaign_continue".Localize(),
			OnCancel = DialogController.current.FinishDialog,
			OnConfirm = () => Start(delegate
			{
				DialogController.current.ShowEntry(CandidateProperties());
			}),
			ConfirmTextOverride = "dialog_recruitment_agency_start_new_campaign".Localize()
		};
	}

	private DialogEntry OnShowRecruitmentList()
	{
		return new DialogEntry
		{
			OnCancel = DialogController.current.CancelDialog,
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.RecruitmentCampaignsList
		};
	}

	private DialogEntry OnRecruitmentSettingsSet()
	{
		RecruitmentSettings inputTransform = DialogController.current.GetInputTransform<RecruitmentSettings>(null);
		if (inputTransform.selectedBusiness == null)
		{
			Notifications.ShowError("common_notification_select_business");
			return null;
		}
		if (!inputTransform.hasSelectedSkill)
		{
			Notifications.ShowError("recruitmentagencydialog_notification_select_skill");
			return null;
		}
		if (!string.IsNullOrEmpty(inputTransform.selectedSkill) && SkillHelper.GetData(inputTransform.selectedSkill).HasTag(TagRef.Skilltag.hashoursperweekdemand) && !inputTransform.partTimeToggle.isOn && !inputTransform.fullTimeToggle.isOn)
		{
			Notifications.ShowError("recruitmentagencydialog_notification_select_schedule");
			return null;
		}
		RecruitmentCampaign recruitmentCampaign = new RecruitmentCampaign
		{
			agencyAddress = DialogController.current.contact.Address,
			businessAddress = inputTransform.selectedBusiness.Address,
			skillRequirement = new RecruitmentCampaign.SkillRequirement
			{
				skillName = inputTransform.selectedSkill,
				percentage = 20f
			},
			fullTime = inputTransform.fullTimeToggle.isOn,
			partTime = inputTransform.partTimeToggle.isOn,
			amountOfCandidates = (int)inputTransform.candidatesAmountSlider.value,
			price = inputTransform.totalPrice
		};
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address);
		string businessName = buildingRegistration.BusinessName;
		bool num = buildingRegistration.BuildingCached.SpecialService?.hasTaxDeductiblePurchases ?? false;
		Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", businessName } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_recruitmentcampaign", data);
		if (num)
		{
			transactionInfo.SetTaxDeductibleName(businessName);
		}
		float amount = 0f - recruitmentCampaign.price;
		Address address = inputTransform.selectedBusiness.Address;
		if (!GameManager.ChangeMoneySafe(amount, transactionInfo, null, address, force: false, showNotification: true))
		{
			return null;
		}
		int days = Mathf.RoundToInt(inputTransform.deadlineSlider.value);
		recruitmentCampaign.candidateFindTimes = Enumerable.Range(0, recruitmentCampaign.amountOfCandidates).Select((Func<int, Timestamp>)delegate
		{
			int day = UnityEngine.Random.Range(SaveGameManager.Current.Day + 1, SaveGameManager.Current.Day + days + 1);
			int hour = UnityEngine.Random.Range(8, 18);
			return new Timestamp(day, hour, 0f);
		}).ToList();
		SaveGameManager.Current.RecruitmentCampaigns.Add(recruitmentCampaign);
		GameEvent.Invoke("ba:gameevent_startedrecruitmentcampaign");
		int amountOfCandidates = recruitmentCampaign.amountOfCandidates;
		string businessName2 = inputTransform.selectedBusiness.BusinessName;
		string skillName = recruitmentCampaign.skillRequirement.skillName;
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{ "businessName", businessName2 },
			{
				"amountOfCandidates",
				amountOfCandidates.ToString()
			},
			{ "skillKey", skillName },
			{
				"days",
				days.ToString()
			},
			{
				"text",
				recruitmentCampaign.GetScheduleTypesInfo()
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_on_recruitment_settings_set_player", messageData, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_on_recruitment_settings_set_recruiter", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_recruitment_agency_on_recruitment_settings_set_recruiter".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.None,
			OnVisible = delegate
			{
				AlreadyHasCampaignActive(continueConversation: true).ShowEntry();
			}
		};
	}

	public DialogEntry OnCancelCampaign(RecruitmentCampaign campaign)
	{
		SaveGameManager.Current.RecruitmentCampaigns.Remove(campaign);
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_on_cancel_campaign_player", null, read: true));
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_recruitment_agency_on_cancel_campaign_recruiter", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_recruitment_agency_on_cancel_campaign_recruiter".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = (SaveGameManager.Current.RecruitmentCampaigns.Any((RecruitmentCampaign x) => x.agencyAddress == DialogController.current.contact.Address) ? new Action(OnShowRecruitmentList().ShowEntry) : new Action(OnMoreHelpOffered().ShowEntry))
		};
	}
}
