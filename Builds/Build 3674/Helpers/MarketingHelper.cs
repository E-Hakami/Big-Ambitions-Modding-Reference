using System.Collections.Generic;
using Entities;
using UI.Notification;

namespace Helpers;

public static class MarketingHelper
{
	private static readonly List<BuildingRegistration> RegistrationsThatCouldNotBePaid = new List<BuildingRegistration>();

	public static void RunDaily()
	{
		RegistrationsThatCouldNotBePaid.Clear();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
			bool num = !string.IsNullOrEmpty(GetTaxDeductibleMarketingAgencyName(buildingRegistration));
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_marketing", "ba:transactioncategory_marketing", data);
			if (num)
			{
				transactionInfo.SetTaxDeductibleName("tax_marketing");
			}
			if (GameManager.ChangeMoneySafe(0f - buildingRegistration.GetDailyMarketingExpenses(), transactionInfo, SaveGameManager.Current.Day - 1, buildingRegistration.Address))
			{
				continue;
			}
			foreach (MarketingCampaign marketingCampaign in buildingRegistration.marketingCampaigns)
			{
				marketingCampaign.enabled = false;
			}
			BusinessHelper.UpdatePromotion(buildingRegistration);
			RegistrationsThatCouldNotBePaid.Add(buildingRegistration);
		}
		ShowMarketingCampaignNotifications();
	}

	private static void ShowMarketingCampaignNotifications()
	{
		if (RegistrationsThatCouldNotBePaid.Count > 3)
		{
			ShowMarketingCampaignDisabledMultipleNotification();
			return;
		}
		foreach (BuildingRegistration item in RegistrationsThatCouldNotBePaid)
		{
			ShowMarketingCampaignDisabledNotification(item);
		}
	}

	private static void ShowMarketingCampaignDisabledNotification(BuildingRegistration registration)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { { "businessName", registration.BusinessName } };
		Notifications.Show(NotificationType.Error, "marketing_notification_no_money_campaigns_disabled", notificationData);
	}

	private static void ShowMarketingCampaignDisabledMultipleNotification()
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"amount",
			RegistrationsThatCouldNotBePaid.Count.ToString()
		} };
		Notifications.Show(NotificationType.Error, "marketing_notification_no_money_campaigns_disabled_multiple", notificationData);
	}

	private static string GetTaxDeductibleMarketingAgencyName(BuildingRegistration registration)
	{
		for (int i = 0; i < registration.marketingCampaigns.Count; i++)
		{
			MarketingCampaign marketingCampaign = registration.marketingCampaigns[i];
			if (marketingCampaign.enabled)
			{
				BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(marketingCampaign.agencyAddress);
				if (buildingRegistration != null && buildingRegistration.BuildingCached.SpecialService?.hasTaxDeductiblePurchases == true)
				{
					return buildingRegistration.BusinessName;
				}
			}
		}
		return string.Empty;
	}
}
