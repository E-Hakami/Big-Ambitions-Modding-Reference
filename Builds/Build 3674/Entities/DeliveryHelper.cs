using System;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Factories;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Helpers;
using UI.Notification;
using UnityEngine;

namespace Entities;

public static class DeliveryHelper
{
	public const DayOfWeekOrdered DeliveryDay = DayOfWeekOrdered.Monday;

	public const DayOfWeekOrdered LockPeriodStartingDay = DayOfWeekOrdered.Sunday;

	public const int DeliveryHour = 8;

	public const int LockPeriodStartingHour = 20;

	public const float ProductShortageAmountMultiplier = 0.66f;

	public const float WholesalePriceMultiplier = 1.05f;

	public static bool IsRegularDeliveryHour()
	{
		return TimeHelper.CurrentHour == 8;
	}

	public static bool IsLockPeriod()
	{
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay);
		if ((dayOfWeek == DayOfWeekOrdered.Sunday && TimeHelper.CurrentHour >= 20) || (dayOfWeek == DayOfWeekOrdered.Monday && TimeHelper.CurrentHour < 8))
		{
			return true;
		}
		return false;
	}

	public static Timestamp GetNextLockPeriodStart()
	{
		int num = TimeHelper.GetNextDayOfWeekNumber(DayOfWeekOrdered.Sunday);
		if (IsLockPeriod() && TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay) == DayOfWeekOrdered.Sunday)
		{
			num += 7;
		}
		return new Timestamp(num, 20, 0f);
	}

	public static int GetNextDeliveryDay()
	{
		int num = TimeHelper.GetNextDayOfWeekNumber(DayOfWeekOrdered.Monday);
		if (IsLockPeriod() || TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay) == DayOfWeekOrdered.Monday)
		{
			num += 7;
		}
		return num;
	}

	public static int EnsureDeliveryDayIsNotInPast(int deliveryDay)
	{
		if (deliveryDay >= TimeHelper.CurrentDay)
		{
			return deliveryDay;
		}
		int num = TimeHelper.CurrentDay + 1;
		Debug.LogError($"Delivery day {deliveryDay} is in the past. Moving delivery to day {num}.");
		return num;
	}

	public static bool CanModifyContract(int contractDeliveryDay)
	{
		if (IsLockPeriod())
		{
			return contractDeliveryDay != TimeHelper.GetNextDayOfWeekNumber(DayOfWeekOrdered.Monday);
		}
		return true;
	}

	public static int GetOrderAmount(DeliveryContractItem deliveryItem, Item item, BuildingRegistration wholesaleRegistration)
	{
		int amount = deliveryItem.amount;
		if (AreWholesaleAndImportLimitsDisabled() || !ProductMarketHelper.IsProductInMarketEvent(deliveryItem.itemName, MarketEventType.ProductShortage, wholesaleRegistration.Neighborhood))
		{
			return amount;
		}
		int max = Mathf.RoundToInt((float)item.maxWholesaleOrderAmount * 0.66f);
		return Math.Clamp(amount, 0, max);
	}

	public static bool ShouldLimitImporterMaxAmount(string itemName, Address importerAddress = null)
	{
		if (ItemsGetter.GetByName(itemName).HasTag(TagRef.Itemtag.isbag))
		{
			return false;
		}
		if (!FactoriesHelper.IsFactoryIngredient(itemName))
		{
			return true;
		}
		if (importerAddress == null)
		{
			return false;
		}
		return !BuildingHelper.GetBuilding(importerAddress).SpecialService.isRawMaterialsImporter;
	}

	public static void ShowCantModifyContractNotification()
	{
		Notifications.ShowError("bizman_contract_settings_notification_cannot_modify_contract", "bizman_contract_settings_notification_cannot_modify_contract");
	}

	public static float GetWholesaleUrgentFeeMultiplier()
	{
		return 1f + SaveGameManager.Current.gameVariables.wholesaleUrgentFeeMultiplier;
	}

	public static float GetImporterUrgentFeeMultiplier()
	{
		return 1f + SaveGameManager.Current.gameVariables.importerUrgentFeeMultiplier;
	}

	public static bool AreWholesaleAndImportLimitsDisabled()
	{
		return SaveGameManager.Current.gameVariables.disableWholesaleAndImportLimits;
	}
}
