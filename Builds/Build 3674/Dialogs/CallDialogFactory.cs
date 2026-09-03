using System;
using System.Collections.Generic;
using AI.Employees.SalaryNegotiation;
using Buildings;
using UnityEngine;

namespace Dialogs;

public static class CallDialogFactory
{
	private static readonly Dictionary<CallDialogType, Func<Dialog>> DialogConstructors;

	static CallDialogFactory()
	{
		DialogConstructors = new Dictionary<CallDialogType, Func<Dialog>>();
		RegisterDialog(CallDialogType.AutoTowServiceDialog, () => new AutoTowServiceDialog());
		RegisterDialog(CallDialogType.BankDialog, () => new BankDialog());
		RegisterDialog(CallDialogType.DoctorDialog, () => new DoctorDialog());
		RegisterDialog(CallDialogType.FurnitureStoreManagerDialog, () => new FurnitureStoreManagerDialog());
		RegisterDialog(CallDialogType.HealthInsuranceManagerDialog, () => new HealthInsuranceManagerDialog());
		RegisterDialog(CallDialogType.HealthInsuranceNegotiationDialog, () => new HealthInsuranceNegotiationDialog());
		RegisterDialog(CallDialogType.ImportManagerDialog, () => new ImportManagerDialog());
		RegisterDialog(CallDialogType.MarketingAgencyDialog, () => new MarketingAgencyDialog());
		RegisterDialog(CallDialogType.RecruitmentAgencyDialog, () => new RecruitmentAgencyDialog());
		RegisterDialog(CallDialogType.UncleFredDialog, () => new UncleFredDialog());
		RegisterDialog(CallDialogType.WholesaleStoreManagerDialog, () => new WholesaleStoreManagerDialog());
		RegisterDialog(CallDialogType.BlackjackDialog, () => new BlackjackDialog());
		RegisterDialog(CallDialogType.RouletteDialog, () => new RouletteDialog());
		RegisterDialog(CallDialogType.SlotMachineDialog, () => new SlotMachineDialog());
		RegisterDialog(CallDialogType.InteriorInstallationFirm, () => new InteriorInstallationFirmAgentDialog());
		RegisterDialog(CallDialogType.CandidateSalaryNegotiationDialog, () => new CandidateSalaryNegotiationDialog());
		RegisterDialog(CallDialogType.MovingServiceDialog, () => new MovingServiceDialog());
		RegisterDialog(CallDialogType.VehicleStoreDialog, () => new VehicleStoreDialog());
		RegisterDialog(CallDialogType.FoodDeliveryDialog, () => new FoodDeliveryDialog());
		RegisterDialog(CallDialogType.PrivateDriverServiceDialog, () => new PrivateDriverServiceDialog());
	}

	public static void RegisterDialog(CallDialogType callDialogType, Func<Dialog> constructor)
	{
		if (callDialogType == CallDialogType.NotImplemented)
		{
			Debug.LogError($"Cannot register dialog type {callDialogType}");
		}
		else if (constructor == null)
		{
			Debug.LogError($"Cannot register null constructor for dialog type {callDialogType}");
		}
		else
		{
			DialogConstructors[callDialogType] = constructor;
		}
	}

	public static Dialog GetDialog(CallDialogType callDialogType)
	{
		if (callDialogType == CallDialogType.NotImplemented)
		{
			return null;
		}
		if (DialogConstructors.TryGetValue(callDialogType, out var value))
		{
			return value();
		}
		Debug.LogError($"Dialog type {callDialogType} has not been registered");
		return null;
	}
}
