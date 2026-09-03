using System.Collections.Generic;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.MovingCompany;

public class MovingServiceContractList : MonoBehaviour
{
	[SerializeField]
	private MovingContractEntry contractEntry;

	private void Start()
	{
		contractEntry.transform.ResetTemplate();
		ShowContractsForAddress();
	}

	private void ShowContractsForAddress()
	{
		foreach (MovingServiceContract currentContract in GetCurrentContracts())
		{
			SetUpContract(currentContract);
		}
	}

	private static List<MovingServiceContract> GetCurrentContracts()
	{
		Address address = DialogController.current.contact.Address;
		return SaveGameManager.Current.movingServiceContracts.FindAll((MovingServiceContract movingContract) => movingContract.movingCompanyRegistration.Address == address);
	}

	private void SetUpContract(MovingServiceContract movingServiceContract)
	{
		MovingContractEntry movingContractEntry = Object.Instantiate(contractEntry, contractEntry.transform.parent);
		string displayName = BuildingHelper.GetBuildingRegistration(movingServiceContract.originMovingAddress).GetDisplayName();
		string displayName2 = BuildingHelper.GetBuildingRegistration(movingServiceContract.destinationMovingAddress).GetDisplayName();
		movingContractEntry.SetupEntry(movingServiceContract, displayName, displayName2);
		movingContractEntry.gameObject.SetActive(value: true);
	}
}
