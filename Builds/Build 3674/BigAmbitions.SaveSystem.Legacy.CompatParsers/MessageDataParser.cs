using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using Localizor;
using Streets;

namespace BigAmbitions.SaveSystem.Legacy.CompatParsers;

public static class MessageDataParser
{
	public static Dictionary<string, string> ParseData(TextMessage.MessageData data)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (!string.IsNullOrEmpty(data.amount))
		{
			dictionary["amount"] = data.amount;
		}
		if (!string.IsNullOrEmpty(data.amount2))
		{
			dictionary["amount2"] = data.amount2;
		}
		if (!string.IsNullOrEmpty(data.businessName))
		{
			dictionary["businessName"] = data.businessName;
		}
		if (!string.IsNullOrEmpty(data.businessName2))
		{
			dictionary["businessName2"] = data.businessName2;
		}
		if (!string.IsNullOrEmpty(data.businessName3))
		{
			dictionary["businessName3"] = data.businessName3;
		}
		if (!string.IsNullOrEmpty(data.businessType))
		{
			dictionary["businessType"] = data.businessType;
		}
		if (!string.IsNullOrEmpty(data.selectedEmployee))
		{
			dictionary["selectedEmployee"] = data.selectedEmployee;
		}
		if (data.startingDay != 0)
		{
			dictionary["startingDay"] = data.startingDay.ToString();
		}
		if (data.campaignDurationInDays != 0)
		{
			dictionary["campaignDurationInDays"] = data.campaignDurationInDays.ToString();
		}
		if (data.impressionsPerDay != 0)
		{
			dictionary["impressionsPerDay"] = data.impressionsPerDay.ToString();
		}
		if (data.amountOfCandidates != 0)
		{
			dictionary["amountOfCandidates"] = data.amountOfCandidates.ToString();
		}
		if (!string.IsNullOrEmpty(data.skillKey))
		{
			string text = data.skillKey.Split('_')[^1];
			data.skillKey = "ba:skill_" + text;
			dictionary["skillKey"] = data.skillKey.GetLocalization();
		}
		if (data.days != 0)
		{
			dictionary["days"] = data.days.ToString();
		}
		if (!string.IsNullOrEmpty(data.itemName))
		{
			dictionary["itemName"] = data.itemName.GetLocalization();
		}
		if (data.startingHour != 0)
		{
			dictionary["startingHour"] = data.startingHour.ToString();
		}
		if (data.endingHour != 0)
		{
			dictionary["endingHour"] = data.endingHour.ToString();
		}
		if (!string.IsNullOrEmpty(data.vehicleTypeName))
		{
			dictionary["vehicleTypeName"] = data.vehicleTypeName;
		}
		if (!string.IsNullOrEmpty(data.hour))
		{
			dictionary["hour"] = data.hour;
		}
		if (!string.IsNullOrEmpty(data.minute))
		{
			dictionary["minute"] = data.minute;
		}
		if (data.day != 0)
		{
			dictionary["day"] = data.day.ToString();
		}
		if (!string.IsNullOrEmpty(data.marketingType))
		{
			dictionary["marketingType"] = data.marketingType;
		}
		if (!string.IsNullOrEmpty(data.agencyBusinessName))
		{
			dictionary["agencyBusinessName"] = data.agencyBusinessName;
		}
		if (data.address != null && !data.address.IsUndefined())
		{
			dictionary["address"] = data.address.ToFormattedString();
		}
		if (!string.IsNullOrEmpty(data.autoTowServiceOption))
		{
			dictionary["autoTowServiceOption"] = data.autoTowServiceOption;
		}
		if (!string.IsNullOrEmpty(data.text))
		{
			dictionary["text"] = data.text;
		}
		if (!string.IsNullOrEmpty(data.investmentFund))
		{
			dictionary["investmentFund"] = data.investmentFund.GetLocalization();
		}
		if (!string.IsNullOrEmpty(data.employeeName))
		{
			dictionary["employeeName"] = data.employeeName;
		}
		if (!EqualityComparer<HealthInsurancePlanType>.Default.Equals(data.healthPlanType, HealthInsurancePlanType.Bronze))
		{
			dictionary["healthPlanType"] = data.healthPlanType.GetLocalization();
		}
		if (!string.IsNullOrEmpty(data.jobDemandName))
		{
			dictionary["jobDemandName"] = data.jobDemandName;
		}
		if (!string.IsNullOrEmpty(data.rivalName))
		{
			dictionary["rivalName"] = data.rivalName;
		}
		if (!string.IsNullOrEmpty(data.products))
		{
			dictionary["products"] = data.products;
		}
		if (data.deliveryInfoList != null)
		{
			dictionary["deliveryInfoList"] = BusinessDeliveryInfo.GetLocalizedList(data.deliveryInfoList);
		}
		if (!string.IsNullOrEmpty(data.buildingType))
		{
			dictionary["buildingType"] = data.buildingType;
		}
		if (!string.IsNullOrEmpty(data.sizeInfo))
		{
			dictionary["sizeInfo"] = data.sizeInfo;
		}
		return dictionary;
	}

	public static AdditionalMessageData ParseAdditionalData(TextMessage.MessageData data)
	{
		return new AdditionalMessageData
		{
			contextButtonData = data.contextButtonData,
			listOfLabels = data.listOfLabels,
			taxes = data.taxes
		};
	}
}
