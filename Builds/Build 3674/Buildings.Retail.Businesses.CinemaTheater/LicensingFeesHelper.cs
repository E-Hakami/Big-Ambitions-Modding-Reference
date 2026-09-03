using System.Collections.Generic;
using Helpers;
using JimmysUnityUtilities;
using UI.Notification;
using UI.Smartphone.Apps.BizMan.Schedule;
using UnityEngine;

namespace Buildings.Retail.Businesses.CinemaTheater;

public class LicensingFeesHelper : MonoBehaviour
{
	public static readonly List<BuildingRegistration> ShownLicensingFeeWarnings = new List<BuildingRegistration>();

	private static readonly List<BuildingRegistration> PendingLicensingFeeWarnings = new List<BuildingRegistration>();

	public static void PayLicensingFees(BuildingRegistration registration, bool noCharge = false)
	{
		if (registration == null || !registration.RentedByPlayer)
		{
			return;
		}
		string businessTypeName = registration.businessTypeName;
		if ((businessTypeName == "ba:businesstype_theater" || businessTypeName == "ba:businesstype_cinema") && (noCharge || BusinessHelper.IsBusinessOpen(registration)))
		{
			PendingLicensingFeeWarnings.RemoveAll((BuildingRegistration x) => x.Address == registration.Address);
			if (registration.businessTypeName == "ba:businesstype_theater")
			{
				PayTheaterLicensingFees(registration, noCharge);
			}
			else if (registration.businessTypeName == "ba:businesstype_cinema")
			{
				PayCinemaLicensingFees(registration, noCharge);
			}
			if (PendingLicensingFeeWarnings.Count != 0)
			{
				CoroutineUtility.RunAfterOneFrame(ShowUnpaidFeesNotifications);
			}
		}
	}

	private static void PayCinemaLicensingFees(BuildingRegistration registration, bool noCharge = false)
	{
		bool flag = false;
		int num = 0;
		foreach (var (itemId, itemInstance2) in registration.itemInstances)
		{
			if (itemInstance2.itemName != "ba:itemname_screencinema" || !ScheduleHelper.IsLicensingFeeActive(registration.Address, itemId, TimeHelper.GetDayOfWeek()) || IsLicensingFeePaid(registration, itemId))
			{
				continue;
			}
			flag = true;
			float num2 = CinemaTheaterHelper.CinemaScreenLicensingFee * (float)(num + 1);
			if (!noCharge && SaveGameManager.Current.Money < num2)
			{
				if (!ShownLicensingFeeWarnings.Contains(registration))
				{
					PendingLicensingFeeWarnings.Add(registration);
				}
				break;
			}
			ScheduleHelper.SetLicensingFeePaidToday(registration.Address, itemId);
			num++;
		}
		if (!noCharge && num > 0)
		{
			Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", registration.BusinessName } };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_licensingfee", "ba:transactioncategory_licensingfees", data);
			transactionInfo.SetTaxDeductibleName("ba:transaction_licensingfee_label");
			GameManager.ChangeMoneySafe((0f - CinemaTheaterHelper.CinemaScreenLicensingFee) * (float)num, transactionInfo, SaveGameManager.Current.Day, registration.Address, force: true);
		}
		if (flag)
		{
			BusinessHelper.UpdateCustomerCapacity(registration);
		}
	}

	private static void PayTheaterLicensingFees(BuildingRegistration registration, bool noCharge = false)
	{
		if (ScheduleHelper.IsLicensingFeeActive(registration.Address, string.Empty, TimeHelper.GetDayOfWeek()) && !IsLicensingFeePaid(registration, string.Empty))
		{
			Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", registration.BusinessName } };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_licensingfee", "ba:transactioncategory_licensingfees", data);
			transactionInfo.SetTaxDeductibleName("ba:transaction_licensingfee_label");
			if (noCharge || GameManager.ChangeMoneySafe(0f - CinemaTheaterHelper.TheaterStageLicensingFee, transactionInfo, SaveGameManager.Current.Day, registration.Address))
			{
				ScheduleHelper.SetLicensingFeePaidToday(registration.Address, string.Empty);
			}
			else if (!ShownLicensingFeeWarnings.Contains(registration))
			{
				PendingLicensingFeeWarnings.Add(registration);
			}
		}
	}

	public static bool IsLicensingFeePaid(BuildingRegistration registration, string itemId)
	{
		return SaveGameManager.Current.paidLicensingFeesToday.Exists(((Address, string) x) => x.Item1 == registration.Address && x.Item2 == itemId);
	}

	private static void ShowUnpaidFeesNotifications()
	{
		if (PendingLicensingFeeWarnings.Count == 0)
		{
			return;
		}
		PendingLicensingFeeWarnings.RemoveAll(delegate(BuildingRegistration registration)
		{
			if (registration.RentedByPlayer)
			{
				string businessTypeName = registration.businessTypeName;
				if (!(businessTypeName == "ba:businesstype_theater"))
				{
					return !(businessTypeName == "ba:businesstype_cinema");
				}
				return false;
			}
			return true;
		});
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (PendingLicensingFeeWarnings.Count > 1)
		{
			dictionary.Add("amount", PendingLicensingFeeWarnings.Count.ToString());
			Notifications.Show(NotificationType.Error, "notification_unpaid_licensing_fee_multiple", dictionary);
		}
		else if (PendingLicensingFeeWarnings.Count > 0)
		{
			BuildingRegistration buildingRegistration = PendingLicensingFeeWarnings[0];
			dictionary.Add("businessName", buildingRegistration.BusinessName);
			Notifications.Show(NotificationType.Error, "notification_unpaid_licensing_fee", dictionary);
		}
		ShownLicensingFeeWarnings.AddRange(PendingLicensingFeeWarnings);
		PendingLicensingFeeWarnings.Clear();
	}
}
