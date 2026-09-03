using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class BizManDeliveries : MonoBehaviour
{
	[SerializeField]
	private DeliveryContractEntry contractEntry;

	[SerializeField]
	private Transform contractList;

	[SerializeField]
	private BizManContractSettings contractSettings;

	[SerializeField]
	private GameObject noContractsWarning;

	private DeliveryContractEntry _selectedContractEntry;

	private BizManBusiness _bizManBusiness;

	private readonly List<DeliveryContractEntry> _instantiatedEntries = new List<DeliveryContractEntry>();

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
		contractEntry.gameObject.SetActive(value: false);
		contractSettings.Initialize();
	}

	private void OnEnable()
	{
		RefreshData();
	}

	public void RefreshData()
	{
		contractSettings.Hide();
		RefreshContracts();
	}

	private void RefreshContracts()
	{
		_selectedContractEntry = null;
		foreach (DeliveryContractEntry instantiatedEntry in _instantiatedEntries)
		{
			Object.Destroy(instantiatedEntry.gameObject);
		}
		_instantiatedEntries.Clear();
		List<DeliveryContract> list = SaveGameManager.Current.DeliveryContracts.FindAll((DeliveryContract x) => x.businessAddress == _bizManBusiness.buildingRegistration.Address);
		if (list.Count == 0)
		{
			contractList.gameObject.SetActive(value: false);
			noContractsWarning.SetActive(value: true);
			return;
		}
		contractList.gameObject.SetActive(value: true);
		noContractsWarning.SetActive(value: false);
		for (int num = 0; num < list.Count; num++)
		{
			SetUpContractEntry(list[num], num == 0);
		}
	}

	private void SetUpContractEntry(DeliveryContract contract, bool selectByDefault)
	{
		DeliveryContractEntry deliveryContractEntry = Object.Instantiate(contractEntry, contractEntry.transform.parent);
		deliveryContractEntry.Initialize(contract, SelectContract);
		_instantiatedEntries.Add(deliveryContractEntry);
		if (selectByDefault)
		{
			SelectContract(deliveryContractEntry, contract);
		}
	}

	public void RefreshSelectedContractEntry(bool shouldShow)
	{
		if ((bool)_selectedContractEntry)
		{
			_selectedContractEntry.UpdateWarningSign(shouldShow);
		}
	}

	private void SelectContract(DeliveryContractEntry entry, DeliveryContract contract)
	{
		UnselectCurrentContract();
		_selectedContractEntry = entry;
		_selectedContractEntry.SetSelected(isSelected: true);
		contractSettings.ShowContractSettings(contract);
	}

	private void UnselectCurrentContract()
	{
		if ((bool)_selectedContractEntry)
		{
			_selectedContractEntry.SetSelected(isSelected: false);
			_selectedContractEntry = null;
		}
	}
}
