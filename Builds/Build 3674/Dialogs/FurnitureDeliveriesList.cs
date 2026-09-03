using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UnityEngine;

namespace Dialogs;

public class FurnitureDeliveriesList : MonoBehaviour
{
	[SerializeField]
	private Transform contractEntry;

	private void Start()
	{
		Address address = DialogController.current.contact.Address;
		IEnumerable<FurnitureDeliveryContract> enumerable = SaveGameManager.Current.FurnitureDeliveryContracts.Where((FurnitureDeliveryContract x) => x.fromAddress == address);
		contractEntry.ResetTemplate();
		foreach (FurnitureDeliveryContract item in enumerable)
		{
			SetUpContract(item);
		}
	}

	private void SetUpContract(FurnitureDeliveryContract furnitureDeliveryContract)
	{
		Transform obj = Object.Instantiate(contractEntry, contractEntry.parent);
		string displayName = BuildingHelper.GetBuildingRegistration(furnitureDeliveryContract.toAddress).GetDisplayName();
		string deliveryTimeFormated = GetDeliveryTimeFormated(furnitureDeliveryContract);
		StringBuilder itemsToDeliverString = GetItemsToDeliverString(furnitureDeliveryContract);
		SetEntryInfo(obj, displayName, deliveryTimeFormated, itemsToDeliverString);
		obj.gameObject.SetActive(value: true);
		obj.GetButtonByName("Buttons/CancelDeliveryButton").onClick.AddListener(delegate
		{
			Dialog dialog = DialogController.current.dialog;
			MethodInfo method = dialog.GetType().GetMethod("OnCancelFurnitureDelivery", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(method == null) && method.Invoke(dialog, new object[1] { furnitureDeliveryContract }) is DialogEntry dialogEntry)
			{
				dialogEntry.ShowEntry();
			}
		});
	}

	private static void SetEntryInfo(Transform entry, string businessName, string deliveryTime, StringBuilder itemsToDeliver)
	{
		entry.GetLanguageChangeEventByName("Info").SetData("dialog_furniture_deliveries_list_info".Localize(new
		{
			businessName = businessName,
			deliveryTime = deliveryTime,
			itemsToDeliver = itemsToDeliver.ToString()
		}));
	}

	private static string GetDeliveryTimeFormated(FurnitureDeliveryContract furnitureDeliveryContract)
	{
		int dayOfDelivery = furnitureDeliveryContract.dayOfDelivery;
		int hourOfDelivery = furnitureDeliveryContract.hourOfDelivery;
		return "dialog_furniture_delivery_time_slot".Localize(new
		{
			day = TimeHelper.GetDayOfWeek(dayOfDelivery).GetLocalizeKey(),
			number = dayOfDelivery,
			hour = hourOfDelivery.GetFormattedTime()
		}).ToString();
	}

	private static StringBuilder GetItemsToDeliverString(FurnitureDeliveryContract furnitureDeliveryContract)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < furnitureDeliveryContract.itemsToDeliver.Count; i++)
		{
			FurnitureDeliveryItem furnitureDeliveryItem = furnitureDeliveryContract.itemsToDeliver[i];
			stringBuilder.Append($"{furnitureDeliveryItem.amount}x ");
			stringBuilder.Append(furnitureDeliveryItem.itemName.GetLocalization());
			stringBuilder.Append($" ${furnitureDeliveryItem.pricePerUnit}");
			if (i < furnitureDeliveryContract.itemsToDeliver.Count - 1)
			{
				stringBuilder.Append("<br>");
			}
		}
		return stringBuilder;
	}
}
