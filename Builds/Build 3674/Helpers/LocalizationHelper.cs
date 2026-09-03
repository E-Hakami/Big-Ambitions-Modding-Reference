using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Blueprints;
using BlueprintsUI;
using Dancing;
using Entities;
using Enums;
using Localizor;
using Localizor.LanguageChangeEvent;
using Parking.UndergroundParking;
using PlayerActivity;
using UI.Smartphone.Apps.Contacts;

namespace Helpers;

public static class LocalizationHelper
{
	public static string GetLocalizeKey(this BoatTypeName boatTypeName)
	{
		return "boattypename_" + boatTypeName.ToStringFast();
	}

	public static string GetLocalizeKey(this MarketingTypeName marketingTypeName)
	{
		return "marketingtypename_" + marketingTypeName.ToStringFast();
	}

	public static string GetLocalizeKey(this Priority priority)
	{
		return "priority_" + priority.ToStringFast();
	}

	public static string GetLocalizeKey(this MarketEventType marketEventType)
	{
		return "marketeventtype_" + marketEventType.ToStringFast();
	}

	public static string GetLocalizeKey(this DayOfWeekOrdered dayOfWeek)
	{
		return "common_" + dayOfWeek.ToStringFast();
	}

	public static string GetLocalizeKey(this DiplomaName diplomaName)
	{
		return "diplomaname_" + diplomaName.ToStringFast();
	}

	public static string GetLocalizeKey(this Transaction.AmountOption amountOption)
	{
		return "amountoption_" + amountOption.ToStringFast();
	}

	public static string GetLocalizeKey(this Floor floor)
	{
		return "elevator_go_to_" + floor.ToStringFast();
	}

	public static string GetLocalizeKey(this Gender gender)
	{
		return "gender_" + gender.ToStringFast();
	}

	public static string GetLocalizeKey(this WorkoutType workoutType)
	{
		return "workouttype_" + workoutType.ToStringFast();
	}

	public static string GetLocalizeKey(this HealthInsurancePlanType planType)
	{
		return "healthinsuranceplantype_" + planType.ToStringFast();
	}

	public static string GetLocalizeKey(this BlueprintCategory blueprintCategory)
	{
		return "blueprintcategory_" + blueprintCategory.ToStringFast();
	}

	public static string GetLocalizeKey(this DataElement dataElement)
	{
		return "blueprintdata_" + dataElement.ToStringFast();
	}

	public static string GetLocalizeKey(this SortByOption sortByOption)
	{
		return "sortby_" + sortByOption.ToStringFast();
	}

	public static string GetLocalizeKey(this DanceType danceType)
	{
		return "dancetype_" + danceType.ToStringFast();
	}

	public static string GetLocalizeKey(this AppName appName)
	{
		return "smartphone_" + appName.ToStringFast();
	}

	public static LanguageChangeEventDataHolder GetLabel(this ItemInstance instance)
	{
		if (instance.ItemCached.HasTag(TagRef.Itemtag.isbox))
		{
			CargoInstance cargoInstance = instance.cargoInstances.FirstOrDefault();
			if (cargoInstance != null)
			{
				LanguageChangeEventDataHolder itemLabel = GetItemLabel(cargoInstance.itemName, cargoInstance.amount);
				if (cargoInstance.itemName != "ba:itemname_sealedcardboardbox")
				{
					itemLabel.Suffix = "itemoverlay_packed";
				}
				return itemLabel;
			}
		}
		return GetItemLabel(instance.itemName);
	}

	public static LanguageChangeEventDataHolder GetLabel(this CargoInstance instance)
	{
		return GetItemLabel(instance.itemName, instance.amount);
	}

	public static LanguageChangeEventDataHolder GetItemLabel(string itemName, int quantity = 0)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return LanguageChangeEventDataHolder.Create("ba:itemname_undefined");
		}
		LanguageChangeEventDataHolder result = LanguageChangeEventDataHolder.Create(itemName);
		if (quantity > 1)
		{
			result.Prefix = quantity + "x ";
		}
		return result;
	}

	public static string GetAmountLabel(int count, int amount)
	{
		if (count * amount != 1)
		{
			return $"{count}x{amount}";
		}
		return string.Empty;
	}

	public static string GetLocalizeKey(this SystemRequirement.SystemRequirements requirement)
	{
		return "systemrequirements_" + requirement.ToStringFast();
	}

	public static string GetLocalizeKey(this ContactCategoryName category)
	{
		return "contactcategoryname_" + category;
	}

	public static string GetLocalization(this BoatTypeName boatTypeName)
	{
		return boatTypeName.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this MarketingTypeName marketingTypeName)
	{
		return marketingTypeName.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this Priority priority)
	{
		return priority.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this MarketEventType marketEventType)
	{
		return marketEventType.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this DayOfWeekOrdered dayOfWeek)
	{
		return dayOfWeek.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this DiplomaName diplomaName)
	{
		return diplomaName.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this Transaction.AmountOption amountOption)
	{
		return amountOption.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this Floor floor)
	{
		return floor.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this Gender gender)
	{
		return gender.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this WorkoutType workoutType)
	{
		return workoutType.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this HealthInsurancePlanType planType)
	{
		return planType.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this BlueprintCategory blueprintCategory)
	{
		return blueprintCategory.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this DataElement dataElement)
	{
		return dataElement.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this SortByOption sortByOption)
	{
		return sortByOption.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this DanceType danceType)
	{
		return danceType.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this AppName appName)
	{
		return appName.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this SystemRequirement.SystemRequirements requirement)
	{
		return requirement.GetLocalizeKey().GetLocalization();
	}

	public static string GetLocalization(this ContactCategoryName category)
	{
		return category.GetLocalizeKey().GetLocalization();
	}
}
