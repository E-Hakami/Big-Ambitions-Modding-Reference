using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UnityEngine;

namespace Dialogs;

public class InteriorInstallationContractsList : MonoBehaviour
{
	[SerializeField]
	private Transform contractEntry;

	private void Start()
	{
		Address address = DialogController.current.contact.Address;
		IEnumerable<InteriorInstallationFirmContract> enumerable = SaveGameManager.Current.interiorInstallationFirmContracts.Where((InteriorInstallationFirmContract x) => x.interiorInstallationFirmAddress == address);
		contractEntry.ResetTemplate();
		foreach (InteriorInstallationFirmContract item in enumerable)
		{
			SetUpContract(item);
		}
	}

	private void SetUpContract(InteriorInstallationFirmContract interiorInstallationFirmContract)
	{
		Transform obj = Object.Instantiate(contractEntry, contractEntry.parent);
		string displayName = BuildingHelper.GetBuildingRegistration(interiorInstallationFirmContract.addressToDoTheInstallation).GetDisplayName();
		string installationTimeFormated = GetInstallationTimeFormated(interiorInstallationFirmContract);
		string designName = interiorInstallationFirmContract.designName;
		SetEntryInfo(obj, displayName, installationTimeFormated, designName);
		obj.gameObject.SetActive(value: true);
		obj.GetButtonByName("Buttons/CancelInstallationButton").onClick.AddListener(delegate
		{
			((InteriorInstallationFirmAgentDialog)DialogController.current.dialog).OnCancelInstallation(interiorInstallationFirmContract).ShowEntry();
		});
	}

	private static void SetEntryInfo(Transform entry, string businessName, string installationTime, string designName)
	{
		entry.GetLanguageChangeEventByName("Info").SetData("dialog_installation_firm_contracts_list_info".Localize(new { businessName, installationTime, designName }));
	}

	private static string GetInstallationTimeFormated(InteriorInstallationFirmContract installationFirmContract)
	{
		int dayOfInstallation = installationFirmContract.dayOfInstallation;
		return "dialog_installation_firm_contract_time_slot".Localize(new
		{
			day = TimeHelper.GetDayOfWeek(dayOfInstallation).GetLocalizeKey(),
			number = dayOfInstallation
		}).ToString();
	}
}
