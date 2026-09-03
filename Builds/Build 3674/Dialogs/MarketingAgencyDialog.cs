using System;
using System.Collections.Generic;
using System.Linq;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class MarketingAgencyDialog : Dialog
{
	public MarketingAgencyDialog()
	{
		npcNameKey = "dialog_marketing_agency_npc_name";
		if (!SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => !string.IsNullOrWhiteSpace(x.BusinessName) && x.RentedByPlayer))
		{
			DialogController.current.ShowEntry(NoBusinesses());
			return;
		}
		DialogController.current.ShowEntry(Start(delegate
		{
			DialogController.current.ShowEntry(CampaignSettings());
		}));
	}

	private DialogEntry Start(Action nextDialog)
	{
		string text = BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address).BusinessName;
		if (text.EndsWith("."))
		{
			text = text.Substring(0, text.Length - 1);
		}
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", text } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_start", messageData, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_marketing_agency_start".Localize(new
			{
				businessName = text
			}),
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			OnVisible = nextDialog
		};
	}

	private DialogEntry CampaignSettings()
	{
		return new DialogEntry
		{
			headerKey = "dialog_marketing_agency_campaign_settings_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.MarketingCampaignSettings,
			OnConfirm = OnMarketingSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry NoBusinesses()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_no_businesses", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_marketing_agency_no_businesses".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry OnMarketingSettingsSet()
	{
		MarketingCampaignSettings inputTransform = DialogController.current.GetInputTransform<MarketingCampaignSettings>(null);
		if (inputTransform.selectedBusiness == null)
		{
			Notifications.ShowError("common_notification_select_business");
			return null;
		}
		string businessName = inputTransform.selectedBusiness.BusinessName;
		Address agencyAddress = DialogController.current.contact.Address;
		Dictionary<string, string> dictionary = new Dictionary<string, string> { { "businessName", businessName } };
		if (inputTransform.marketingTypeNamesSelected.Count == 0)
		{
			if (inputTransform.selectedBusiness.marketingCampaigns.Count((MarketingCampaign x) => x.agencyAddress == agencyAddress) == 0)
			{
				Notifications.ShowError("marketingagencydialog_notification_select_website");
				return null;
			}
			inputTransform.selectedBusiness.marketingCampaigns.RemoveAll((MarketingCampaign x) => x.agencyAddress == agencyAddress);
			BusinessHelper.UpdatePromotion(inputTransform.selectedBusiness);
			DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_on_campaign_settings_remove_player", dictionary, read: true));
			DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_on_campaign_settings_remove_manager", null, read: true));
			return new DialogEntry
			{
				messageData = "dialog_marketing_agency_on_campaign_settings_remove_manager".Localize(),
				Template = DialogEntry.TemplateType.Text,
				OnVisible = OnMoreHelpOffered().ShowEntry
			};
		}
		MarketingAgencySettings obj = BuildingHelper.GetBuilding(agencyAddress).SpecialService.settings as MarketingAgencySettings;
		List<MarketingTypeName> list = new List<MarketingTypeName>();
		List<MarketingTypeName> list2 = new List<MarketingTypeName>();
		foreach (MarketingTypeName marketingTypeName in obj.marketingTypesAvailable)
		{
			MarketingCampaign marketingCampaign = inputTransform.selectedBusiness.marketingCampaigns.FirstOrDefault((MarketingCampaign x) => x.agencyAddress == agencyAddress && x.marketingTypeName == marketingTypeName);
			if (inputTransform.marketingTypeNamesSelected.Contains(marketingTypeName))
			{
				if (marketingCampaign != null)
				{
					if (marketingCampaign.enabled)
					{
						continue;
					}
					marketingCampaign.enabled = true;
				}
				else
				{
					inputTransform.selectedBusiness.marketingCampaigns.Add(new MarketingCampaign
					{
						agencyAddress = DialogController.current.contact.Address,
						marketingTypeName = marketingTypeName,
						enabled = true
					});
				}
				list.Add(marketingTypeName);
			}
			else if (marketingCampaign != null && inputTransform.selectedBusiness.marketingCampaigns.RemoveAll((MarketingCampaign x) => x.marketingTypeName == marketingTypeName) > 0)
			{
				list2.Add(marketingTypeName);
			}
		}
		if (list.Count == 0 && list2.Count == 0)
		{
			Notifications.ShowError("marketingagencydialog_notification_no_campaign_changes");
			return null;
		}
		GameEvent.Invoke("ba:gameevent_newmarketing");
		BusinessHelper.UpdatePromotion(inputTransform.selectedBusiness);
		foreach (MarketingTypeName item in list)
		{
			Dictionary<string, string> dictionary2 = dictionary.ToDictionary((KeyValuePair<string, string> x) => x.Key, (KeyValuePair<string, string> x) => x.Value);
			dictionary2.Add("marketingType", item.GetLocalizeKey());
			DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_on_campaign_settings_set_player", dictionary2, read: true));
		}
		foreach (MarketingTypeName item2 in list2)
		{
			Dictionary<string, string> dictionary3 = dictionary.ToDictionary((KeyValuePair<string, string> x) => x.Key, (KeyValuePair<string, string> x) => x.Value);
			dictionary3.Add("marketingType", item2.GetLocalizeKey());
			DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_on_campaign_settings_unset_player", dictionary3, read: true));
		}
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_marketing_agency_on_campaign_settings_set_manager", null, read: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_marketing_agency_on_campaign_settings_set_manager".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = OnMoreHelpOffered().ShowEntry
		};
	}

	private DialogEntry OnMoreHelpOffered()
	{
		return new DialogEntry
		{
			messageData = "dialog_marketing_agency_more_help_offered".Localize(),
			OnCancel = DialogController.current.CancelDialog,
			OnConfirm = CampaignSettings,
			ConfirmTextOverride = "dialog_marketing_agency_start_new_campaign".Localize()
		};
	}
}
