using Buildings.BuildingTypes.Special.FoodDelivery;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UI.Dialog;
using UnityEngine;

namespace Dialogs;

public class FoodDeliveriesList : MonoBehaviour
{
	private const string ListEntryInfo = "dialog_furniture_deliveries_list_info";

	[SerializeField]
	private Transform contractEntry;

	private void Start()
	{
		contractEntry.ResetTemplate();
		foreach (FoodDeliveryContract contract in FoodDeliveryHelper.GetContracts())
		{
			SetUpContract(contract);
		}
	}

	private void SetUpContract(FoodDeliveryContract contract)
	{
		Transform obj = Object.Instantiate(contractEntry, contractEntry.parent);
		string displayName = BuildingHelper.GetBuildingRegistration(contract.toAddress).GetDisplayName();
		string deliverySlotLabel = DeliveryContractSettingsBase.GetDeliverySlotLabel(contract.dayOfDelivery, contract.hourOfDelivery);
		obj.GetLanguageChangeEventByName("Info").SetData("dialog_furniture_deliveries_list_info".Localize(new
		{
			businessName = displayName,
			deliveryTime = deliverySlotLabel,
			itemsToDeliver = FoodDeliveryHelper.BuildItemsText(contract, includePrice: true)
		}));
		obj.gameObject.SetActive(value: true);
		obj.GetButtonByName("Buttons/CancelDeliveryButton").onClick.AddListener(delegate
		{
			CancelContract(contract);
		});
	}

	private static void CancelContract(FoodDeliveryContract contract)
	{
		if (DialogController.current.dialog is FoodDeliveryDialog foodDeliveryDialog)
		{
			foodDeliveryDialog.OnCancelFoodDelivery(contract).ShowEntry();
		}
	}
}
