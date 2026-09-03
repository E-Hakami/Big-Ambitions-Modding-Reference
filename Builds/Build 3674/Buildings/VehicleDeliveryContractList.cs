using System.Collections.Generic;
using BigAmbitions.Tags;
using Extensions;
using UnityEngine;
using Vehicles;
using Vehicles.VehicleTypes;

namespace Buildings;

public class VehicleDeliveryContractList : MonoBehaviour
{
	[SerializeField]
	private VehicleDeliveryContractEntry contractEntry;

	private void Start()
	{
		contractEntry.transform.ResetTemplate();
		ShowContractsForAddress();
	}

	private void ShowContractsForAddress()
	{
		foreach (VehicleDeliveryContract currentContract in GetCurrentContracts())
		{
			SetUpContract(currentContract);
		}
	}

	private static List<VehicleDeliveryContract> GetCurrentContracts()
	{
		Address address = DialogController.current.contact.Address;
		return SaveGameManager.Current.vehicleDeliveryContracts.FindAll(delegate(VehicleDeliveryContract contract)
		{
			VehicleType vehicleType = VehicleTypeHelper.GetVehicleType(contract.vehicleTypeName);
			return contract.vehicleStoreAddress == address && vehicleType.HasTag(TagRef.Vehicletag.istruck);
		});
	}

	private void SetUpContract(VehicleDeliveryContract vehicleDeliveryContract)
	{
		VehicleDeliveryContractEntry vehicleDeliveryContractEntry = Object.Instantiate(contractEntry, contractEntry.transform.parent);
		vehicleDeliveryContractEntry.SetupEntry(vehicleDeliveryContract);
		vehicleDeliveryContractEntry.gameObject.SetActive(value: true);
	}
}
